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
    public class OperationRaftRelief : Operation
    {
        #region Enums
        public enum RaftReliefTypes : byte
        {
            Relief,
            Dimming,
            Decimate
        }
        #endregion
        
        #region Members
        private RaftReliefTypes _reliefType = RaftReliefTypes.Relief;
        private uint _maskLayerIndex;
        private byte _ignoreFirstLayers;
        private byte _brightness;
        private byte _dilateIterations = 15;// +/- 1.5mm radius
        private byte _wallMargin = 40;      // +/- 2mm
        private byte _holeDiameter = 80;    // +/- 4mm
        private byte _holeSpacing = 40;     // +/- 2mm

        #endregion

        #region Overrides
        public override string Title => "Raft relief";
        public override string Description =>
            "Relief raft by adding holes in between to reduce FEP suction, save resin and easier to remove the prints.";

        public override string ConfirmationText =>
            $"relief the raft";

        public override string ProgressTitle =>
            $"Relieving raft";

        public override string ProgressAction => "Relieved layers";

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection =>
            Enumerations.LayerRangeSelection.None;
        
        public override string ToString()
        {
            var result = $"[{_reliefType}] [Mask layer: {_maskLayerIndex}] [Ignore: {_ignoreFirstLayers}] [B: {_brightness}] [Dilate: {_dilateIterations}] [Wall margin: {_wallMargin}] [Hole diameter: {_holeDiameter}] [Hole spacing: {_holeSpacing}]";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }
        #endregion

        #region Constructor

        public OperationRaftRelief() { }

        public OperationRaftRelief(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Properties
        public static Array RaftReliefItems => Enum.GetValues(typeof(RaftReliefTypes));

        public RaftReliefTypes ReliefType
        {
            get => _reliefType;
            set
            {
                if(!RaiseAndSetIfChanged(ref _reliefType, value)) return;
                RaisePropertyChanged(nameof(IsRelief));
                RaisePropertyChanged(nameof(IsDimming));
                RaisePropertyChanged(nameof(IsDecimate));
            }
        }

        public bool IsRelief => _reliefType == RaftReliefTypes.Relief;
        public bool IsDimming => _reliefType == RaftReliefTypes.Dimming;
        public bool IsDecimate => _reliefType == RaftReliefTypes.Decimate;

        public uint MaskLayerIndex
        {
            get => _maskLayerIndex;
            set => RaiseAndSetIfChanged(ref _maskLayerIndex, value);
        }

        public byte IgnoreFirstLayers
        {
            get => _ignoreFirstLayers;
            set => RaiseAndSetIfChanged(ref _ignoreFirstLayers, value);
        }

        public byte Brightness
        {
            get => _brightness;
            set
            {
                if (!RaiseAndSetIfChanged(ref _brightness, value)) return;
                RaisePropertyChanged(nameof(BrightnessPercent));
            }
        }

        public decimal BrightnessPercent => Math.Round(_brightness * 100 / 255M, 2);

        public byte DilateIterations
        {
            get => _dilateIterations;
            set => RaiseAndSetIfChanged(ref _dilateIterations, value);
        }

        public byte WallMargin
        {
            get => _wallMargin;
            set => RaiseAndSetIfChanged(ref _wallMargin, value);
        }

        public byte HoleDiameter
        {
            get => _holeDiameter;
            set => RaiseAndSetIfChanged(ref _holeDiameter, value);
        }

        public byte HoleSpacing
        {
            get => _holeSpacing;
            set => RaiseAndSetIfChanged(ref _holeSpacing, value);
        }
        #endregion

        #region Equality

        protected bool Equals(OperationRaftRelief other)
        {
            return _reliefType == other._reliefType && _maskLayerIndex == other._maskLayerIndex && _ignoreFirstLayers == other._ignoreFirstLayers && _brightness == other._brightness && _dilateIterations == other._dilateIterations && _wallMargin == other._wallMargin && _holeDiameter == other._holeDiameter && _holeSpacing == other._holeSpacing;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationRaftRelief) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int) _reliefType, _maskLayerIndex, _ignoreFirstLayers, _brightness, _dilateIterations, _wallMargin, _holeDiameter, _holeSpacing);
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            progress.ItemCount = 0;
            const uint minSupportsRequired = 5;
            const uint maxLayerCount = 1000;

            Mat supportsMat = null;
            var anchor = new Point(-1, -1);
            var kernel = EmguExtensions.Kernel3x3Rectangle;


            uint firstSupportLayerIndex = _maskLayerIndex;
            if (firstSupportLayerIndex <= 0)
            {
                uint layerCount = Math.Min(SlicerFile.LayerCount, maxLayerCount);
                progress.Reset("Tracing raft", layerCount, firstSupportLayerIndex);
                for (; firstSupportLayerIndex < layerCount; firstSupportLayerIndex++)
                {
                    if (progress.Token.IsCancellationRequested) return false;
                    progress++;
                    supportsMat = GetRoiOrDefault(SlicerFile[firstSupportLayerIndex].LayerMat);
                    var circles = CvInvoke.HoughCircles(supportsMat, HoughModes.Gradient, 1, 5, 80, 35, 5, 200);
                    if (circles.Length >= minSupportsRequired) break;
                    
                    supportsMat.Dispose();
                    supportsMat = null;
                }
            }
            else
            {
                supportsMat = GetRoiOrDefault(SlicerFile[firstSupportLayerIndex].LayerMat);
            }

            if (supportsMat is null || /*firstSupportLayerIndex == 0 ||*/ _ignoreFirstLayers >= firstSupportLayerIndex) return false;
            Mat patternMat = null;

            if (DilateIterations > 0)
            {
                CvInvoke.Dilate(supportsMat, supportsMat,
                    CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), new Point(-1, -1)),
                    new Point(-1, -1), DilateIterations, BorderType.Reflect101, new MCvScalar());
            }

            var color = new MCvScalar(255 - Brightness);

            switch (ReliefType)
            {
                case RaftReliefTypes.Relief:
                    patternMat = EmguExtensions.InitMat(supportsMat.Size);
                    int shapeSize = HoleDiameter + HoleSpacing;
                    using (var shape = EmguExtensions.InitMat(new Size(shapeSize, shapeSize)))
                    {

                        int center = HoleDiameter / 2;
                        //int centerTwo = operation.HoleDiameter + operation.HoleSpacing + operation.HoleDiameter / 2;
                        int radius = center;
                        CvInvoke.Circle(shape, new Point(shapeSize / 2, shapeSize / 2), radius, color, -1);
                        CvInvoke.Circle(shape, new Point(0, 0), radius / 2, color, -1);
                        CvInvoke.Circle(shape, new Point(0, shapeSize), radius / 2, color, -1);
                        CvInvoke.Circle(shape, new Point(shapeSize, 0), radius / 2, color, -1);
                        CvInvoke.Circle(shape, new Point(shapeSize, shapeSize), radius / 2, color, -1);

                        CvInvoke.Repeat(shape, supportsMat.Height / shape.Height + 1, supportsMat.Width / shape.Width + 1, patternMat);

                        patternMat = new Mat(patternMat, new Rectangle(0, 0, supportsMat.Width, supportsMat.Height));
                    }

                    break;
                case RaftReliefTypes.Dimming:
                    patternMat = EmguExtensions.InitMat(supportsMat.Size, color);
                    break;
            }

            progress.Reset(ProgressAction, firstSupportLayerIndex - _ignoreFirstLayers);
            Parallel.For(_ignoreFirstLayers, firstSupportLayerIndex, CoreSettings.ParallelOptions, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                using var mat = SlicerFile[layerIndex].LayerMat;
                using var original = mat.Clone();
                var target = GetRoiOrDefault(mat);

                switch (ReliefType)
                {
                    case RaftReliefTypes.Relief:
                    case RaftReliefTypes.Dimming:
                        using (Mat mask = new())
                        {
                            /*CvInvoke.Subtract(target, supportsMat, mask);
                                CvInvoke.Erode(mask, mask, kernel, anchor, operation.WallMargin, BorderType.Reflect101, new MCvScalar());
                                CvInvoke.Subtract(target, patternMat, target, mask);*/

                            CvInvoke.Erode(target, mask, kernel, anchor, WallMargin, BorderType.Reflect101, default);
                            CvInvoke.Subtract(mask, supportsMat, mask);
                            CvInvoke.Subtract(target, patternMat, target, mask);
                        }

                        break;
                    case RaftReliefTypes.Decimate:
                        supportsMat.CopyTo(target);
                        break;
                }

                ApplyMask(original, target);
                SlicerFile[layerIndex].LayerMat = mat;

                progress.LockAndIncrement();
            });


            supportsMat.Dispose();
            patternMat?.Dispose();

            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
