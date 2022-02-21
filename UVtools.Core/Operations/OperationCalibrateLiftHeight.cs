/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationCalibrateLiftHeight : Operation
    {
        #region Members
        private decimal _layerHeight;
        private ushort _bottomLayers = 3;
        private ushort _normalLayers = 2;
        private decimal _bottomExposure;
        private decimal _normalExposure;
        private decimal _bottomLiftHeight;
        private decimal _liftHeight;
        private decimal _bottomLiftSpeed;
        private decimal _liftSpeed;
        private decimal _retractSpeed;
        private ushort _leftRightMargin = 200;
        private ushort _topBottomMargin = 200;
        private bool _decreaseImage = true;
        private byte _decreaseImageFactor = 10;
        private byte _minimumImageFactor = 10;

        #endregion

        #region Overrides

        public override bool CanROI => false;

        public override bool CanCancel => false;

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override string Title => "Lift height";
        public override string Description =>
            "Generates test models with various strategies and increments to measure the optimal lift height or peel forces for layers given the printed area.\n" +
            "You must have a tool to measure the lift height / forces as it moves up, record the values to determine the lowest safe value for the lift.\n" +
            "After find the height where it peels, you must give from 1mm to 2mm more for safeness.\n" +
            "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
            "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

        public override string ConfirmationText =>
            $"generate the lift height test?";

        public override string ProgressTitle =>
            $"Generating the lift height test";

        public override string ProgressAction => "Generated";

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            if (SlicerFile.ResolutionX - _leftRightMargin * 2 <= 0)
                sb.AppendLine("The top/bottom margin is too big, it overlaps the screen resolution.");
            
            if (SlicerFile.ResolutionY - _topBottomMargin * 2 <= 0)
                sb.AppendLine("The top/bottom margin is too big, it overlaps the screen resolution.");

            if (_decreaseImage)
            {
                if(_decreaseImageFactor is 0 or >= 100)
                    sb.AppendLine("The image decrease factor must be between 1 and 99%");

                if(_minimumImageFactor is 0 or >= 100)
                    sb.AppendLine("The minimum image decrease factor must be between 1 and 99%");
            }
            
            return sb.ToString();
        }

        public override string ToString()
        {
            var result = $"[Layer Height: {_layerHeight}] " +
                         $"[Layers: {_bottomLayers}/{_normalLayers}] " +
                         $"[Exposure: {_bottomExposure}/{_normalExposure}s] " +
                         $"[Lift: {_bottomLiftHeight}/{_liftHeight}mm @ {_bottomLiftSpeed}/{_liftSpeed}mm/min]" +
                         $"[Retract speed: {_retractSpeed}mm/min]" +
                         $"[Decrease image: {_decreaseImage} @ {_decreaseImageFactor}-{_minimumImageFactor}%]";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Properties

        public decimal LayerHeight
        {
            get => _layerHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _layerHeight, Layer.RoundHeight(value))) return;
                RaisePropertyChanged(nameof(BottomHeight));
                RaisePropertyChanged(nameof(NormalHeight));
                RaisePropertyChanged(nameof(TotalHeight));
            }
        }
        
        public ushort Microns => (ushort) (LayerHeight * 1000);

        public ushort BottomLayers
        {
            get => _bottomLayers;
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottomLayers, value)) return;
                RaisePropertyChanged(nameof(BottomHeight));
                RaisePropertyChanged(nameof(TotalHeight));
                RaisePropertyChanged(nameof(LayerCount));
            }
        }

        public ushort NormalLayers
        {
            get => _normalLayers;
            set
            {
                if (!RaiseAndSetIfChanged(ref _normalLayers, value)) return;
                RaisePropertyChanged(nameof(NormalHeight));
                RaisePropertyChanged(nameof(TotalHeight));
                RaisePropertyChanged(nameof(LayerCount));
            }
        }

        public uint LayerCount
        {
            get
            {
                uint layerCount = (uint)(_bottomLayers + _normalLayers);
                if (_decreaseImage)
                {
                    layerCount += (100u - _minimumImageFactor) / _decreaseImageFactor;
                    //layerCount += (uint)Math.Ceiling((100.0 - _minimumImageFactor - _decreaseImageFactor) / _decreaseImageFactor);
                    //for (int factor = 100 - _decreaseImageFactor; factor >= _minimumImageFactor; factor -= _decreaseImageFactor) 
                    //    layerCount++;
                }
                return layerCount;
            }
        }

        public decimal BottomHeight => Layer.RoundHeight(_layerHeight * _bottomLayers);
        public decimal NormalHeight => Layer.RoundHeight(_layerHeight * (LayerCount - _bottomLayers));

        public decimal TotalHeight => BottomHeight + NormalHeight;

        public decimal BottomExposure
        {
            get => _bottomExposure;
            set => RaiseAndSetIfChanged(ref _bottomExposure, Math.Round(value, 2));
        }

        public decimal NormalExposure
        {
            get => _normalExposure;
            set => RaiseAndSetIfChanged(ref _normalExposure, Math.Round(value, 2));
        }

        public decimal BottomLiftHeight
        {
            get => _bottomLiftHeight;
            set => RaiseAndSetIfChanged(ref _bottomLiftHeight, value);
        }

        public decimal LiftHeight
        {
            get => _liftHeight;
            set => RaiseAndSetIfChanged(ref _liftHeight, value);
        }

        public decimal BottomLiftSpeed
        {
            get => _bottomLiftSpeed;
            set => RaiseAndSetIfChanged(ref _bottomLiftSpeed, value);
        }

        public decimal LiftSpeed
        {
            get => _liftSpeed;
            set => RaiseAndSetIfChanged(ref _liftSpeed, value);
        }

        public decimal RetractSpeed
        {
            get => _retractSpeed;
            set => RaiseAndSetIfChanged(ref _retractSpeed, value);
        }

        public ushort LeftRightMargin
        {
            get => _leftRightMargin;
            set => RaiseAndSetIfChanged(ref _leftRightMargin, value);
        }

        public ushort MaxLeftRightMargin => (ushort)((SlicerFile.ResolutionX - 100) / 2);

        public ushort TopBottomMargin
        {
            get => _topBottomMargin;
            set => RaiseAndSetIfChanged(ref _topBottomMargin, value);
        }

        public ushort MaxTopBottomMargin => (ushort) ((SlicerFile.ResolutionY - 100) / 2);

        public bool DecreaseImage
        {
            get => _decreaseImage;
            set
            {
                RaiseAndSetIfChanged(ref _decreaseImage, value);
                RaisePropertyChanged(nameof(TotalHeight));
                RaisePropertyChanged(nameof(LayerCount));
            }
        }

        public byte DecreaseImageFactor
        {
            get => _decreaseImageFactor;
            set
            {
                RaiseAndSetIfChanged(ref _decreaseImageFactor, value);
                RaisePropertyChanged(nameof(TotalHeight));
                RaisePropertyChanged(nameof(LayerCount));
            }
        }

        public byte MinimumImageFactor
        {
            get => _minimumImageFactor;
            set
            {
                RaiseAndSetIfChanged(ref _minimumImageFactor, value);
                RaisePropertyChanged(nameof(TotalHeight));
                RaisePropertyChanged(nameof(LayerCount));
            }
        }

        public Rectangle WhiteBlock
        {
            get
            {
                int width = (int) (SlicerFile.ResolutionX - _leftRightMargin * 2);
                int height = (int) (SlicerFile.ResolutionY - _topBottomMargin * 2);
                int x = (int) (SlicerFile.ResolutionX - width) / 2;
                int y = (int) (SlicerFile.ResolutionY - height) / 2;

                return new Rectangle(x, y, width, height);
            }
        }

        #endregion

        #region Constructor

        public OperationCalibrateLiftHeight() { }

        public OperationCalibrateLiftHeight(FileFormat slicerFile) : base(slicerFile)
        { }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();
            if(_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
            if(_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
            if(_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
            if (_bottomLiftHeight <= 0) _bottomLiftHeight = (decimal)SlicerFile.BottomLiftHeight;
            if (_liftHeight <= 0) _liftHeight = (decimal)SlicerFile.LiftHeight;
            if (_bottomLiftSpeed <= 0) _bottomLiftSpeed = (decimal) SlicerFile.BottomLiftSpeed;
            if (_liftSpeed <= 0) _liftSpeed = (decimal) SlicerFile.LiftSpeed;
            if (_retractSpeed <= 0) _retractSpeed = (decimal) SlicerFile.RetractSpeed;
        }

        #endregion

        #region Equality

        private bool Equals(OperationCalibrateLiftHeight other)
        {
            return _layerHeight == other._layerHeight && _bottomLayers == other._bottomLayers && _normalLayers == other._normalLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _bottomLiftHeight == other._bottomLiftHeight && _liftHeight == other._liftHeight && _bottomLiftSpeed == other._bottomLiftSpeed && _liftSpeed == other._liftSpeed && _retractSpeed == other._retractSpeed && _leftRightMargin == other._leftRightMargin && _topBottomMargin == other._topBottomMargin && _decreaseImage == other._decreaseImage && _decreaseImageFactor == other._decreaseImageFactor && _minimumImageFactor == other._minimumImageFactor;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationCalibrateLiftHeight other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_layerHeight);
            hashCode.Add(_bottomLayers);
            hashCode.Add(_normalLayers);
            hashCode.Add(_bottomExposure);
            hashCode.Add(_normalExposure);
            hashCode.Add(_bottomLiftHeight);
            hashCode.Add(_liftHeight);
            hashCode.Add(_bottomLiftSpeed);
            hashCode.Add(_liftSpeed);
            hashCode.Add(_retractSpeed);
            hashCode.Add(_leftRightMargin);
            hashCode.Add(_topBottomMargin);
            hashCode.Add(_decreaseImage);
            hashCode.Add(_decreaseImageFactor);
            hashCode.Add(_minimumImageFactor);
            return hashCode.ToHashCode();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the bottom and normal layers, 0 = bottom | 1 = normal
        /// </summary>
        /// <returns></returns>
        public Mat[] GetLayers()
        {
            var layers = new Mat[1];

            layers[0] = EmguExtensions.InitMat(SlicerFile.Resolution);
            CvInvoke.Rectangle(layers[0], WhiteBlock, EmguExtensions.WhiteColor, -1);

            return layers;
        }

        public Mat GetThumbnail()
        {
            Mat thumbnail = EmguExtensions.InitMat(new Size(400, 200), 3);
            var fontFace = FontFace.HersheyDuplex;
            var fontScale = 1;
            var fontThickness = 2;
            const byte xSpacing = 45;
            const byte ySpacing = 45;
            CvInvoke.PutText(thumbnail, "UVtools", new Point(140, 35), fontFace, fontScale, new MCvScalar(255, 27, 245), fontThickness + 1);
            CvInvoke.Line(thumbnail, new Point(xSpacing, 0), new Point(xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.Line(thumbnail, new Point(xSpacing, ySpacing + 5), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.Line(thumbnail, new Point(thumbnail.Width - xSpacing, 0), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.PutText(thumbnail, "Lift Height Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
            CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);
            //CvInvoke.PutText(thumbnail, $"{ObjectCount} Objects", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);
            
            return thumbnail;
        }

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            progress.ItemCount = LayerCount;
            
            var newLayers = new Layer[LayerCount];

            var layers = GetLayers();
            progress++;
            
            var layer = new Layer(0, layers[0], SlicerFile.LayerManager)
            {
                IsModified = true
            };

            uint layerIndex = 0;
            for (; layerIndex < _bottomLayers + _normalLayers; layerIndex++)
            {
                newLayers[layerIndex] = layer.Clone();
                progress++;
            }

            if (_decreaseImage)
            {
                var rect = WhiteBlock;
                for (int factor = 100 - _decreaseImageFactor; factor >= _minimumImageFactor; factor -= _decreaseImageFactor)
                {
                    using var mat = layers[0].NewBlank();

                    // size -  100
                    //  x   - factor

                    int width = rect.Width * factor / 100;
                    int height = rect.Height * factor / 100;
                    int x = (int)(SlicerFile.ResolutionX - width) / 2;
                    int y = (int)(SlicerFile.ResolutionY - height) / 2;
                    
                    CvInvoke.Rectangle(mat,
                        new Rectangle(x, y, width, height), 
                        EmguExtensions.WhiteColor, -1);

                    newLayers[layerIndex] = new Layer(0, mat, SlicerFile.LayerManager)
                    {
                        IsModified = true
                    };

                    layerIndex++;
                    progress++;
                }
            }

            foreach (var mat in layers)
            {
                mat.Dispose();
            }

            
            if (SlicerFile.ThumbnailsCount > 0)
                SlicerFile.SetThumbnails(GetThumbnail());

            progress++;

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.LayerHeight = (float)_layerHeight;
                SlicerFile.BottomExposureTime = (float)_bottomExposure;
                SlicerFile.ExposureTime = (float)_normalExposure;
                SlicerFile.BottomLiftHeight = (float)_bottomLiftHeight;
                SlicerFile.LiftHeight = (float)_liftHeight;
                SlicerFile.BottomLiftSpeed = (float)_bottomLiftSpeed;
                SlicerFile.LiftSpeed = (float)_liftSpeed;
                SlicerFile.RetractSpeed = (float)_retractSpeed;
                SlicerFile.BottomLayerCount = _bottomLayers;
                SlicerFile.TransitionLayerCount = 0;

                SlicerFile.LayerManager.Layers = newLayers;
            }, true);
            
            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
