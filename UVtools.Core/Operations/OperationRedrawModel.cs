/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationRedrawModel : Operation
    {
        #region Members

        private string _filePath;
        private byte _brightness = 220;
        private bool _contactPointsOnly = true;
        private RedrawTypes _redrawType = RedrawTypes.Supports;
        private bool _ignoreContactLessPixels = true;

        #endregion
        
        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection { get; } = Enumerations.LayerRangeSelection.None;
        
        public override string Title => "Redraw model/supports";

        public override string Description =>
            "Redraw the model or supports with a set brightness. This requires an extra sliced file from same object but without any supports and raft, straight to the build plate.\n" +
            "Note: Run this tool prior to any made modification. You must find the optimal exposure/brightness combo, or supports can fail.";

        public override string ConfirmationText => "redraw the "+ (_redrawType == RedrawTypes.Supports ? "supports" : "model") + 
                                                   $" with an brightness of {_brightness}?";

        public override string ProgressTitle => "Redrawing " + (_redrawType == RedrawTypes.Supports ? "supports" : "model");

        public override string ProgressAction => "Redraw layers";

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            if (IsFileValid() is null)
            {
                sb.AppendLine("The selected file is not valid.");
            }

            return sb.ToString();
        }


        public override string ToString()
        {
            var result = $"[{_redrawType}] [B: {_brightness}] [CS: {_contactPointsOnly}] [ICLP: {_ignoreContactLessPixels}]";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }
        

        #endregion

        #region Enums
        public enum RedrawTypes : byte
        {
            Supports,
            Model,
        }
        #endregion

        #region Constructor

        public OperationRedrawModel() { }

        public OperationRedrawModel(FileFormat slicerFile) : base(slicerFile) { }

        #endregion

        #region Properties

        [XmlIgnore]
        public string FilePath
        {
            get => _filePath;
            set => RaiseAndSetIfChanged(ref _filePath, value);
        }

        public RedrawTypes RedrawType
        {
            get => _redrawType;
            set => RaiseAndSetIfChanged(ref _redrawType, value);
        }

        public static Array RedrawTypesItems => Enum.GetValues(typeof(RedrawTypes));

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

        public bool ContactPointsOnly
        {
            get => _contactPointsOnly;
            set => RaiseAndSetIfChanged(ref _contactPointsOnly, value);
        }

        public bool IgnoreContactLessPixels
        {
            get => _ignoreContactLessPixels;
            set => RaiseAndSetIfChanged(ref _ignoreContactLessPixels, value);
        }

        #endregion

        #region Equality

        protected bool Equals(OperationRedrawModel other)
        {
            return _brightness == other._brightness && _contactPointsOnly == other._contactPointsOnly && _redrawType == other._redrawType && _ignoreContactLessPixels == other._ignoreContactLessPixels;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationRedrawModel) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_brightness, _contactPointsOnly, (int) _redrawType, _ignoreContactLessPixels);
        }

        #endregion

        #region Methods

        public FileFormat IsFileValid(bool returnNewInstance = false) =>
            FileFormat.FindByExtension(_filePath, true, returnNewInstance);

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            var otherFile = IsFileValid(true);
            otherFile.Decode(_filePath, progress);

            progress.Reset(ProgressAction, otherFile.LayerCount);

            int startLayerIndex = (int)(SlicerFile.LayerCount - otherFile.LayerCount);
            if (startLayerIndex < 0) return false;
            Parallel.For(0, otherFile.LayerCount, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                var fullMatLayerIndex = startLayerIndex + layerIndex;
                using var fullMat = SlicerFile[fullMatLayerIndex].LayerMat;
                using var original = fullMat.Clone();
                using var bodyMat = otherFile[layerIndex].LayerMat;
                using var fullMatRoi = GetRoiOrDefault(fullMat);
                using var bodyMatRoi = GetRoiOrDefault(bodyMat);
                using var patternMat = EmguExtensions.InitMat(fullMatRoi.Size, new MCvScalar(255 - _brightness));
                using var supportsMat = new Mat();

                bool modified = false;
                if (_redrawType == RedrawTypes.Supports && _contactPointsOnly)
                {
                    if (layerIndex + 1 >= otherFile.LayerCount) return;
                    CvInvoke.Subtract(fullMatRoi, bodyMatRoi, supportsMat); // Supports
                    using var contours = new VectorOfVectorOfPoint();
                    using var hierarchyMat = new Mat();
                    CvInvoke.FindContours(supportsMat, contours, hierarchyMat, RetrType.List, ChainApproxMethod.ChainApproxSimple);
                    if (contours.Size <= 0) return;
                    using var nextLayerMat = otherFile[layerIndex + 1].LayerMat;
                    using var nextLayerMatRoi = GetRoiOrDefault(nextLayerMat);
                    var fullSpan = fullMatRoi.GetPixelSpan<byte>();
                    var supportsSpan = supportsMat.GetPixelSpan<byte>();
                    var nextSpan = nextLayerMatRoi.GetPixelSpan<byte>();
                    for (int i = 0; i < contours.Size; i++)
                    {
                        var foundContour = false;
                        var rectangle = CvInvoke.BoundingRectangle(contours[i]);
                        for (int y = rectangle.Y; y < rectangle.Bottom && !foundContour; y++)
                        for (int x = rectangle.X; x < rectangle.Right; x++)
                        {
                            var pos = supportsMat.GetPixelPos(x, y);
                            if (_ignoreContactLessPixels)
                            {
                                if (supportsSpan[pos] <= 10) continue;
                                if (nextSpan[pos] <= 0) continue;
                                modified = true;
                                fullSpan[pos] = _brightness;
                            }
                            else
                            {
                                if (supportsSpan[pos] <= 100) continue;
                                if (nextSpan[pos] <= 150) continue;
                                CvInvoke.DrawContours(fullMatRoi, contours, i, new MCvScalar(_brightness), -1, LineType.AntiAlias);
                                modified = true;
                                foundContour = true;
                                break;
                            }
                               
                        }
                    }
                }
                else
                {
                    switch (_redrawType)
                    {
                        case RedrawTypes.Supports:
                            CvInvoke.Subtract(fullMatRoi, bodyMatRoi, supportsMat); // Supports
                            break;
                        case RedrawTypes.Model:
                            CvInvoke.BitwiseAnd(fullMatRoi, bodyMatRoi, supportsMat); // Model
                            break;
                    }

                    CvInvoke.Subtract(fullMatRoi, patternMat, fullMatRoi, supportsMat);
                    modified = true;
                }

                if (modified)
                {
                    ApplyMask(original, fullMat);
                    SlicerFile[fullMatLayerIndex].LayerMat = fullMat;
                }

                progress.LockAndIncrement();
            });

            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
