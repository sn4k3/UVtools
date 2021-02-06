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
using System.Threading.Tasks;
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
    public sealed class OperationCalibrateGrayscale : Operation
    {
        #region Members
        private decimal _layerHeight = 0.05M;
        private ushort _bottomLayers = 10;
        private ushort _interfaceLayers = 5;
        private ushort _normalLayers = 30;
        private decimal _bottomExposure = 60;
        private decimal _normalExposure = 12;
        private ushort _outerMargin = 200;
        private ushort _innerMargin = 50;
        private bool _enableAntiAliasing = true;
        private bool _mirrorOutput;
        private byte _startBrightness = 175;
        private byte _endBrightness = 255;
        private byte _brightnessSteps = 10;
        private bool _enableCenterHoleRelief = true;
        private ushort _centerHoleDiameter = 200;
        private bool _enableLineDivisions = true;
        private byte _lineDivisionThickness = 30;
        private byte _lineDivisionBrightness = 255;
        private short _textXOffset;

        #endregion

        #region Overrides

        public override bool CanROI => false;

        public override bool CanCancel => false;

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override string Title => "Grayscale";
        public override string Description =>
            "Generates test models with various strategies and increments to verify the LED power against the grayscale levels.\n" +
            "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
            "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

        public override string ConfirmationText =>
            $"generate the grayscale test?";

        public override string ProgressTitle =>
            $"Generating the grayscale test";

        public override string ProgressAction => "Generated";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (_startBrightness > _endBrightness)
            {
                sb.AppendLine("Start brightness must be lower or equal to end brightness.");
            }
            if (Divisions <= 0)
            {
                sb.AppendLine("No divisions to output.");
            }

            return new StringTag(sb.ToString());
        }

        public override string ToString()
        {
            var result = $"[Layer Height: {_layerHeight}] " +
                         $"[Layers: {_bottomLayers}/{_interfaceLayers}/{_normalLayers}] " +
                         $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                         $"[Margin: {_outerMargin}/{_innerMargin}] " +
                         $"[B: {_startBrightness}-{_endBrightness} S{_brightnessSteps}] " +
                         $"[AA: {_enableAntiAliasing}] [Mirror: {_mirrorOutput}]";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Constructor

        public OperationCalibrateGrayscale() { }

        public OperationCalibrateGrayscale(FileFormat slicerFile) : base(slicerFile)
        {
            _layerHeight = (decimal)slicerFile.LayerHeight;
            _bottomLayers = slicerFile.BottomLayerCount;
            _bottomExposure = (decimal)slicerFile.BottomExposureTime;
            _normalExposure = (decimal)slicerFile.ExposureTime;
            _mirrorOutput = slicerFile.MirrorDisplay;
        }
        #endregion

        #region Properties

        public decimal LayerHeight
        {
            get => _layerHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _layerHeight, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(BottomHeight));
                RaisePropertyChanged(nameof(InterfaceHeight));
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

        public ushort InterfaceLayers
        {
            get => _interfaceLayers;
            set
            {
                if(!RaiseAndSetIfChanged(ref _interfaceLayers, value)) return;
                RaisePropertyChanged(nameof(InterfaceHeight));
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

        public uint LayerCount => (uint) (_bottomLayers + _interfaceLayers + _normalLayers);

        public decimal BottomHeight => Math.Round(LayerHeight * _bottomLayers, 2);
        public decimal InterfaceHeight => Math.Round(LayerHeight * _interfaceLayers, 2);
        public decimal NormalHeight => Math.Round(LayerHeight * _normalLayers, 2);

        public decimal TotalHeight => BottomHeight + InterfaceHeight + NormalHeight;

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

        public ushort OuterMargin
        {
            get => _outerMargin;
            set => RaiseAndSetIfChanged(ref _outerMargin, value);
        }

        public ushort InnerMargin
        {
            get => _innerMargin;
            set => RaiseAndSetIfChanged(ref _innerMargin, value);
        }

        public bool EnableAntiAliasing
        {
            get => _enableAntiAliasing;
            set => RaiseAndSetIfChanged(ref _enableAntiAliasing, value);
        }

        public bool MirrorOutput
        {
            get => _mirrorOutput;
            set => RaiseAndSetIfChanged(ref _mirrorOutput, value);
        }

        public byte StartBrightness
        {
            get => _startBrightness;
            set
            {
                if (!RaiseAndSetIfChanged(ref _startBrightness, value)) return;
                RaisePropertyChanged(nameof(StartBrightnessPercent));
                RaisePropertyChanged(nameof(Divisions));
                RaisePropertyChanged(nameof(AngleStep));
            }
        }

        public float StartBrightnessPercent => (float)Math.Round(_startBrightness * 100 / 255M, 2);

        public byte EndBrightness
        {
            get => _endBrightness;
            set
            {
                if (!RaiseAndSetIfChanged(ref _endBrightness, value)) return;
                RaisePropertyChanged(nameof(EndBrightnessPercent));
                RaisePropertyChanged(nameof(Divisions));
                RaisePropertyChanged(nameof(AngleStep));
            }
        }

        public float EndBrightnessPercent => (float)Math.Round(_endBrightness * 100 / 255M, 2);

        public byte BrightnessSteps
        {
            get => _brightnessSteps;
            set
            {
                if (!RaiseAndSetIfChanged(ref _brightnessSteps, value)) return;
                RaisePropertyChanged(nameof(Divisions));
                RaisePropertyChanged(nameof(AngleStep));
            }
        }

        public int Divisions => (int)Math.Floor((_endBrightness - _startBrightness) / (decimal)_brightnessSteps) + 1;
        public float AngleStep => 360f / Divisions;

        public bool EnableCenterHoleRelief
        {
            get => _enableCenterHoleRelief;
            set => RaiseAndSetIfChanged(ref _enableCenterHoleRelief, value);
        }

        public ushort CenterHoleDiameter
        {
            get => _centerHoleDiameter;
            set => RaiseAndSetIfChanged(ref _centerHoleDiameter, value);
        }

        public bool EnableLineDivisions
        {
            get => _enableLineDivisions;
            set => RaiseAndSetIfChanged(ref _enableLineDivisions, value);
        }

        public byte LineDivisionThickness
        {
            get => _lineDivisionThickness;
            set => RaiseAndSetIfChanged(ref _lineDivisionThickness, value);
        }

        public byte LineDivisionBrightness
        {
            get => _lineDivisionBrightness;
            set
            {
                if(!RaiseAndSetIfChanged(ref _lineDivisionBrightness, value)) return;
                RaisePropertyChanged(nameof(LineDivisionBrightnessPercent));
            }
        }
        
        public float LineDivisionBrightnessPercent => (float)Math.Round(_lineDivisionBrightness * 100 / 255M, 2);

        public short TextXOffset
        {
            get => _textXOffset;
            set => RaiseAndSetIfChanged(ref _textXOffset, value);
        }

        #endregion

        #region Equality

        private bool Equals(OperationCalibrateGrayscale other)
        {
            return _layerHeight == other._layerHeight && _bottomLayers == other._bottomLayers && _normalLayers == other._normalLayers && _interfaceLayers == other._interfaceLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _outerMargin == other._outerMargin && _innerMargin == other._innerMargin && _startBrightness == other._startBrightness && _endBrightness == other._endBrightness && _brightnessSteps == other._brightnessSteps && _enableCenterHoleRelief == other._enableCenterHoleRelief && _centerHoleDiameter == other._centerHoleDiameter && _enableLineDivisions == other._enableLineDivisions && _lineDivisionThickness == other._lineDivisionThickness && _lineDivisionBrightness == other._lineDivisionBrightness && _textXOffset == other._textXOffset && _enableAntiAliasing == other._enableAntiAliasing && _mirrorOutput == other._mirrorOutput;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationCalibrateGrayscale other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_layerHeight);
            hashCode.Add(_bottomLayers);
            hashCode.Add(_normalLayers);
            hashCode.Add(_interfaceLayers);
            hashCode.Add(_bottomExposure);
            hashCode.Add(_normalExposure);
            hashCode.Add(_outerMargin);
            hashCode.Add(_innerMargin);
            hashCode.Add(_enableAntiAliasing);
            hashCode.Add(_mirrorOutput);
            hashCode.Add(_startBrightness);
            hashCode.Add(_endBrightness);
            hashCode.Add(_brightnessSteps);
            hashCode.Add(_enableCenterHoleRelief);
            hashCode.Add(_centerHoleDiameter);
            hashCode.Add(_enableLineDivisions);
            hashCode.Add(_lineDivisionThickness);
            hashCode.Add(_lineDivisionBrightness);
            hashCode.Add(_textXOffset);
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
            Mat[] layers = new Mat[3];

            layers[0] = EmguExtensions.InitMat(SlicerFile.Resolution);

            int radius = Math.Max(100, Math.Min(SlicerFile.Resolution.Width, SlicerFile.Resolution.Height) - _outerMargin * 2) / 2 ;
            Point center = new Point(SlicerFile.Resolution.Width / 2, SlicerFile.Resolution.Height / 2);
            int innerRadius = Math.Max(100, radius - _innerMargin);
            double topLineLength = 0;

            CvInvoke.Circle(layers[0], center, radius, EmguExtensions.WhiteByte, -1, LineType.AntiAlias);
            layers[1] = layers[0].Clone();
            layers[2] = layers[0].Clone();

            LineType lineType = _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected;

            int i = 0;
            for (ushort brightness = _startBrightness; brightness <= _endBrightness; brightness += _brightnessSteps)
            {
                var radians = new float[2];
                var degrees = new SizeF[2];
                for (int n = 0; n < 2; n++)
                {
                    radians[n] = -AngleStep * (i + n);
                    degrees[n] = new SizeF((float) Math.Cos(radians[n] * Math.PI / 180), (float) Math.Sin(radians[n] * Math.PI / 180));
                }

                Point[] points = new Point[3];
                points[0] = center;
                points[1] = new(center.X + (int) (innerRadius * degrees[0].Width), center.Y + (int) (innerRadius * degrees[0].Height));
                points[2] = new(center.X + (int) (innerRadius * degrees[1].Width), center.Y + (int) (innerRadius * degrees[1].Height));
                using var vec = new VectorOfPoint(points);

                if (topLineLength == 0) topLineLength = PointExtensions.FindLength(points[1], points[2]);

                CvInvoke.FillPoly(layers[2], vec, new MCvScalar(brightness), lineType);
                
                if (_enableLineDivisions && _lineDivisionThickness > 0)
                {
                    CvInvoke.Polylines(layers[2], vec, false, new MCvScalar(_lineDivisionBrightness), _lineDivisionThickness, lineType);
                }

                i++;
            }
            
            FontFace fontFace = FontFace.HersheyDuplex;
            double fontScale = 2;
            int fontThickness = 5;
            Point fontPoint = new Point(center.X + radius / 2 + _textXOffset, (int)(center.Y + AngleStep / 1.1));

            var halfAngleStep = AngleStep / 2;
            var rotatedAngle = halfAngleStep;


            layers[2].Rotate(halfAngleStep);
            for (ushort brightness = _startBrightness; brightness <= _endBrightness; brightness += _brightnessSteps)
            {
                CvInvoke.PutText(layers[2], brightness.ToString(), fontPoint, fontFace, fontScale, EmguExtensions.BlackByte, fontThickness, lineType);
                rotatedAngle += AngleStep;
                layers[2].Rotate(AngleStep);
            }
            
            layers[2].Rotate(-rotatedAngle);

            if (_enableCenterHoleRelief && _centerHoleDiameter > 1)
            {
                var holeRadius = Math.Min(radius, _centerHoleDiameter) / 2;
                if (_innerMargin > 0)
                {
                    CvInvoke.Circle(layers[2], center, holeRadius + _innerMargin, EmguExtensions.WhiteByte, -1, lineType);
                }

                foreach (var layer in layers)
                {
                    CvInvoke.Circle(layer, center, holeRadius, EmguExtensions.BlackByte, -1, lineType);
                }
            }

            fontScale = 1.5;
            fontThickness = 3;
            CvInvoke.PutText(layers[0], $"{Microns}um at {_bottomExposure}s/{_normalExposure}s", 
                new Point(center.X - radius / 2, center.Y + radius / 2 +40), 
                fontFace, fontScale, EmguExtensions.BlackByte, fontThickness, lineType, true);

            CvInvoke.PutText(layers[0], $"{_startBrightness}-{_endBrightness} S:{_brightnessSteps}",
                new Point(center.X - radius / 2, center.Y + radius / 2 - 40),
                fontFace, fontScale, EmguExtensions.BlackByte, fontThickness, lineType, true);

            if (_mirrorOutput)
            {
                Parallel.ForEach(layers, mat => CvInvoke.Flip(mat, mat, FlipType.Horizontal));
            }

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
            CvInvoke.PutText(thumbnail, "Grayscale Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
            CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguExtensions.White3Byte, fontThickness);
            CvInvoke.PutText(thumbnail, $"Divs:{Divisions} Angle:{AngleStep}", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.White3Byte, fontThickness);

            return thumbnail;
        }

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            progress.ItemCount = LayerCount;
            var newLayers = new Layer[LayerCount];

            var layers = GetLayers();

            var bottomLayer = new Layer(0, layers[0], SlicerFile.LayerManager)
            {
                IsModified = true
            };
            var interfaceLayer = InterfaceLayers > 0 && layers[1] is not null ? new Layer(0, layers[1], SlicerFile.LayerManager)
            {
                IsModified = true
            } : null;
            var layer = new Layer(0, layers[2], SlicerFile.LayerManager)
            {
                IsModified = true
            };

            uint layerIndex = 0;
            for (uint i = 0; i < BottomLayers; i++)
            {
                newLayers[layerIndex] = bottomLayer.Clone();
                progress++;
                layerIndex++;
            }

            for (uint i = 0; i < InterfaceLayers; i++)
            {
                newLayers[layerIndex] = interfaceLayer.Clone();
                progress++;
                layerIndex++;
            }


            for (uint i = 0; i < NormalLayers; i++)
            {
                newLayers[layerIndex] = layer.Clone();
                progress++;
                layerIndex++;
            }

            foreach (var mat in layers)
            {
                mat?.Dispose();
            }


            if (SlicerFile.ThumbnailsCount > 0)
                SlicerFile.SetThumbnails(GetThumbnail());

            SlicerFile.SuppressRebuildProperties = true;
            SlicerFile.LayerHeight = (float)LayerHeight;
            SlicerFile.BottomExposureTime = (float)BottomExposure;
            SlicerFile.ExposureTime = (float)NormalExposure;
            SlicerFile.BottomLayerCount = BottomLayers;

            SlicerFile.LayerManager.Layers = newLayers;
            SlicerFile.LayerManager.RebuildLayersProperties();
            SlicerFile.SuppressRebuildProperties = false;

            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
