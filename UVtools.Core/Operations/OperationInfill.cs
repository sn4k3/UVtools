/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationInfill : Operation
    {
        #region Members
        private InfillAlgorithm _infillType = InfillAlgorithm.CubicDynamicLink;
        private ushort _wallThickness = 64;
        private ushort _infillThickness = 45;
        private ushort _infillSpacing = 160;
        private ushort _infillBrightness = 255;
        #endregion

        #region Overrides

        public override string Title => "Infill";

        public override string Description =>
            $"Generate infill patterns in the model.\n\nNOTES:\n1) You must exclude floor and ceil layers from the range.\n2) You must take care of drain holes after the operation.";

        public override string ConfirmationText =>
            $"infill model with {InfillType} from layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Infill model with {InfillType} from layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Infilled layers";

        #endregion

        #region Enums
        public enum InfillAlgorithm
        {
            //Rhombus,
            Cubic,
            CubicCenterLink,
            CubicDynamicLink,
            CubicInterlinked,
            Honeycomb
        }
        #endregion

        #region Properties
        public static Array InfillAlgorithmTypes => Enum.GetValues(typeof(InfillAlgorithm));
        public InfillAlgorithm InfillType
        {
            get => _infillType;
            set => RaiseAndSetIfChanged(ref _infillType, value);
        }

        public ushort WallThickness
        {
            get => _wallThickness;
            set => RaiseAndSetIfChanged(ref _wallThickness, value);
        }

        public ushort InfillBrightness
        {
            get => _infillBrightness;
            set => RaiseAndSetIfChanged(ref _infillBrightness, value);
        }

        public ushort InfillThickness
        {
            get => _infillThickness;
            set => RaiseAndSetIfChanged(ref _infillThickness, value);
        }

        public ushort InfillSpacing
        {
            get => _infillSpacing;
            set => RaiseAndSetIfChanged(ref _infillSpacing, value);
        }

        public override string ToString()
        {
            var result = $"[{_infillType}] [Wall: {_wallThickness}px] [B: {_infillBrightness}px] [T: {_infillThickness}px] [S: {_infillSpacing}px]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Constructor

        public OperationInfill() { }

        public OperationInfill(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Equality

        private bool Equals(OperationInfill other)
        {
            return _infillType == other._infillType && _wallThickness == other._wallThickness && _infillThickness == other._infillThickness && _infillSpacing == other._infillSpacing && _infillBrightness == other._infillBrightness;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationInfill other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) _infillType, _wallThickness, _infillThickness, _infillSpacing, _infillBrightness);
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            Mat mask = null;
            if (_infillType == InfillAlgorithm.Honeycomb)
            {
                mask = GetHoneycombMask(GetRoiSizeOrDefault());
            }

            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, CoreSettings.ParallelOptions, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                using var mat = SlicerFile[layerIndex].LayerMat;
                Execute(mat, layerIndex, mask);
                SlicerFile[layerIndex].LayerMat = mat;

                progress.LockAndIncrement();
            });
            mask?.Dispose();
            return !progress.Token.IsCancellationRequested;
        }

        public override bool Execute(Mat mat, params object[] arguments)
        {
            if (arguments is null || arguments.Length < 1) return false;
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            uint index = Convert.ToUInt32(arguments[0]);
            uint layerIndex = index - LayerIndexStart;
            var infillColor = new MCvScalar(_infillBrightness);

            Mat patternMask = null;
            using Mat erode = new ();
            using Mat diff = new ();
            var target = GetRoiOrDefault(mat);
            using var mask = GetMask(mat);
            bool disposeTargetMask = true;
             
            if (_infillType is InfillAlgorithm.Cubic 
                or InfillAlgorithm.CubicCenterLink 
                or InfillAlgorithm.CubicDynamicLink 
                or InfillAlgorithm.CubicInterlinked)
            {
                using var infillPattern = EmguExtensions.InitMat(new Size(_infillSpacing, _infillSpacing));
                using var matPattern = mat.NewBlank();
                bool firstPattern = true;
                uint accumulator = 0;
                bool dynamicCenter = false;
                while (accumulator < layerIndex)
                {
                    dynamicCenter = !dynamicCenter;
                    firstPattern = true;
                    accumulator += _infillSpacing;

                    if (accumulator >= layerIndex) break;
                    firstPattern = false;
                    accumulator += _infillThickness;
                }

                if (firstPattern)
                {
                    int thickness = _infillThickness / 2;
                    // Top Left
                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(0, 0, thickness, thickness),
                        infillColor, -1);

                    // Top Right
                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(infillPattern.Width - thickness, 0, thickness, thickness),
                        infillColor, -1);

                    // Bottom Left
                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(0, infillPattern.Height - thickness, thickness, thickness),
                        infillColor, -1);

                    // Bottom Right
                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(infillPattern.Width - thickness, infillPattern.Height - thickness,
                            thickness, thickness),
                        infillColor, -1);

                    // Center cross
                    int margin = (int) (InfillSpacing - accumulator + layerIndex) - thickness;
                    int marginInv = (int) (accumulator - layerIndex) - thickness;

                    if (_infillType == InfillAlgorithm.CubicCenterLink ||
                        (_infillType == InfillAlgorithm.CubicDynamicLink &&
                         dynamicCenter) ||
                        _infillType == InfillAlgorithm.CubicInterlinked)
                    {

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(margin, margin, _infillThickness, _infillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(marginInv, marginInv, _infillThickness,
                                _infillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(margin, marginInv, _infillThickness,
                                _infillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(marginInv, margin, _infillThickness,
                                _infillThickness),
                            infillColor, -1);
                    }


                    if (_infillType == InfillAlgorithm.CubicInterlinked ||
                        (_infillType == InfillAlgorithm.CubicDynamicLink &&
                         !dynamicCenter))
                    {
                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(margin, -thickness, _infillThickness,
                                _infillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(marginInv, -thickness, _infillThickness,
                                _infillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(-thickness, margin, _infillThickness,
                                _infillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(-thickness, marginInv, _infillThickness,
                                _infillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(InfillSpacing - thickness, margin,
                                _infillThickness, _infillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(InfillSpacing - thickness, marginInv,
                                _infillThickness, _infillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(margin, InfillSpacing - thickness,
                                _infillThickness, _infillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(marginInv, InfillSpacing - thickness,
                                _infillThickness, _infillThickness),
                            infillColor, -1);
                    }
                }
                else
                {
                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(0, 0, _infillSpacing, _infillSpacing),
                        infillColor, _infillThickness);
                }


                CvInvoke.Repeat(infillPattern, target.Rows / infillPattern.Rows + 1,
                    target.Cols / infillPattern.Cols + 1, matPattern);
                patternMask = new Mat(matPattern, new Rectangle(0, 0, target.Width, target.Height));
                disposeTargetMask = true;
            }
            else if (_infillType == InfillAlgorithm.Honeycomb)
            {
                if (arguments.Length >= 2)
                {
                    patternMask = (Mat)arguments[1];
                    disposeTargetMask = false;
                }
                else
                {
                    patternMask = GetHoneycombMask(target.Size);
                    disposeTargetMask = true;
                }
            }
            //patternMask.Save("D:\\pattern.png");
            CvInvoke.Erode(target, erode, kernel, anchor, WallThickness, BorderType.Reflect101,
                default);
            CvInvoke.Subtract(target, erode, diff);

            CvInvoke.BitwiseAnd(erode, patternMask, target, mask);
            CvInvoke.Add(target, diff, target, mask);

            if (disposeTargetMask)
            {
                patternMask.Dispose();
            }

            return true;
        }

        public Mat GetHoneycombMask(Size targetSize)
        {
            var patternMask = EmguExtensions.InitMat(targetSize);

            var halfInfillSpacing = _infillSpacing / 2;
            var halfThickenss = _infillThickness / 2;
            int width = (int)Math.Round(4 * (_infillSpacing / 2.0 / Math.Sqrt(3)));
            var infillColor = new MCvScalar(_infillBrightness);

            for (int col = 0; col <= targetSize.Width / _infillSpacing; col++)
            {
                for (int row = 0; row <= targetSize.Height / _infillSpacing; row++)
                {
                    // Move over for the column number.
                    int x = (int)Math.Round(col * (width * 0.75f));

                    // Move down the required number of rows.
                    int y = row * _infillSpacing;

                    // If the column is odd, move down half a hex more.
                    if (col % 2 == 1) y += halfInfillSpacing;

                    var points = new Point[]
                    {
                        new(x, y),
                        new((int) Math.Round(x + width * 0.25f), y - _infillSpacing / 2),
                        new((int) Math.Round(x + width * 0.75f), y - _infillSpacing / 2),
                        new(x + width, y),
                        new((int) Math.Round(x + width * 0.75f), y + _infillSpacing / 2),
                        new((int) Math.Round(x + width * 0.25f), y + _infillSpacing / 2),
                    };

                    CvInvoke.Polylines(patternMask, points, true, infillColor, _infillThickness);
                }
            }

            return patternMask;
        }

        #endregion
    }
}
