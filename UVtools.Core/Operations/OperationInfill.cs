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
            $"Generate infill patterns in the model\n\nNOTES:\n1) You must exclude floor and ceil layers from the range.\n2) You must take care of drain holes after the operation.";

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
            Parallel.For(LayerIndexStart, LayerIndexEnd + 1, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;

                using var mat = SlicerFile[layerIndex].LayerMat;
                Execute(mat, layerIndex);
                SlicerFile[layerIndex].LayerMat = mat;
                
                lock (progress.Mutex)
                {
                    progress++;
                }
            });

            return !progress.Token.IsCancellationRequested;
        }

        public override bool Execute(Mat mat, params object[] arguments)
        {
            if (arguments is null || arguments.Length < 1) return false;
            var anchor = new Point(-1, -1);
            var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);
            uint index = Convert.ToUInt32(arguments[0]);
            uint layerIndex = index - LayerIndexStart;
            var infillColor = new MCvScalar(InfillBrightness);

            Mat patternMask = null;
            using Mat erode = new ();
            using Mat diff = new ();
            Mat target = GetRoiOrDefault(mat);
            using var mask = GetMask(mat);

             
            if (InfillType == InfillAlgorithm.Cubic ||
                InfillType == InfillAlgorithm.CubicCenterLink ||
                InfillType == InfillAlgorithm.CubicDynamicLink ||
                InfillType == InfillAlgorithm.CubicInterlinked)
            {
                using var infillPattern = EmguExtensions.InitMat(new Size(InfillSpacing, InfillSpacing));
                using Mat matPattern = mat.CloneBlank();
                bool firstPattern = true;
                uint accumulator = 0;
                bool dynamicCenter = false;
                while (accumulator < layerIndex)
                {
                    dynamicCenter = !dynamicCenter;
                    firstPattern = true;
                    accumulator += InfillSpacing;

                    if (accumulator >= layerIndex) break;
                    firstPattern = false;
                    accumulator += InfillThickness;
                }

                if (firstPattern)
                {
                    int thickness = InfillThickness / 2;
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

                    if (InfillType == InfillAlgorithm.CubicCenterLink ||
                        (InfillType == InfillAlgorithm.CubicDynamicLink &&
                         dynamicCenter) ||
                        InfillType == InfillAlgorithm.CubicInterlinked)
                    {

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(margin, margin, InfillThickness, InfillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(marginInv, marginInv, InfillThickness,
                                InfillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(margin, marginInv, InfillThickness,
                                InfillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(marginInv, margin, InfillThickness,
                                InfillThickness),
                            infillColor, -1);
                    }


                    if (InfillType == InfillAlgorithm.CubicInterlinked ||
                        (InfillType == InfillAlgorithm.CubicDynamicLink &&
                         !dynamicCenter))
                    {
                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(margin, -thickness, InfillThickness,
                                InfillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(marginInv, -thickness, InfillThickness,
                                InfillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(-thickness, margin, InfillThickness,
                                InfillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(-thickness, marginInv, InfillThickness,
                                InfillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(InfillSpacing - thickness, margin,
                                InfillThickness, InfillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(InfillSpacing - thickness, marginInv,
                                InfillThickness, InfillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(margin, InfillSpacing - thickness,
                                InfillThickness, InfillThickness),
                            infillColor, -1);

                        CvInvoke.Rectangle(infillPattern,
                            new Rectangle(marginInv, InfillSpacing - thickness,
                                InfillThickness, InfillThickness),
                            infillColor, -1);
                    }


                }
                else
                {
                    CvInvoke.Rectangle(infillPattern,
                        new Rectangle(0, 0, InfillSpacing, InfillSpacing),
                        infillColor, InfillThickness);
                }


                {
                    CvInvoke.Repeat(infillPattern, target.Rows / infillPattern.Rows + 1,
                        target.Cols / infillPattern.Cols + 1, matPattern);
                    patternMask = new Mat(matPattern, new Rectangle(0, 0, target.Width, target.Height));
                }
            }

            CvInvoke.Erode(target, erode, kernel, anchor, WallThickness, BorderType.Reflect101,
                default);
            CvInvoke.Subtract(target, erode, diff);
            
            CvInvoke.BitwiseAnd(erode, patternMask, target, mask);
            CvInvoke.Add(target, diff, target, mask);
            patternMask?.Dispose();

            return true;
        }

        #endregion
    }
}
