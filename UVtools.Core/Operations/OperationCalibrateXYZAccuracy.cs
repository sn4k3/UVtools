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
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationCalibrateXYZAccuracy : Operation
    {
        #region Members
        private decimal _layerHeight = 0.05M;
        private ushort _bottomLayers = 3;
        private decimal _bottomExposure = 60;
        private decimal _normalExposure = 12;
        private ushort _topBottomMargin = 100;
        private ushort _leftRightMargin = 100;
        private decimal _displayWidth;
        private decimal _displayHeight;
        private decimal _xSize = 15;
        private decimal _ySize = 15;
        private decimal _zSize = 15;
        private bool _centerHoleRelief = true;
        private bool _hollowModel = true;
        private bool _mirrorOutput;
        private decimal _wallThickness = 2.5M;
        private decimal _observedXSize;
        private decimal _observedYSize;
        private decimal _observedZSize;
        private bool _outputTLObject;
        private bool _outputTCObject;
        private bool _outputTRObject;
        private bool _outputMLObject;
        private bool _outputMCObject = true;
        private bool _outputMRObject;
        private bool _outputBLObject;
        private bool _outputBCObject;
        private bool _outputBRObject;

        #endregion

        #region Overrides

        public override bool CanROI => false;

        public override bool CanCancel => false;

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override string Title => "XYZ Accuracy";
        public override string Description =>
            "Generates test models with various strategies and increments to verify the XYZ accuracy.\n" +
            "XYZ are accurate when the printed model match the expected size.\n" +
            "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
            "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

        public override string ConfirmationText =>
            $"generate the XYZ accuracy test?";

        public override string ProgressTitle =>
            $"Generating the XYZ accuracy test";

        public override string ProgressAction => "Generated";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (DisplayWidth <= 0)
            {
                sb.AppendLine("Display width must be a positive value.");
            }

            if (DisplayHeight <= 0)
            {
                sb.AppendLine("Display height must be a positive value.");
            }
            
            if (OutputObjects <= 0)
            {
                sb.AppendLine("No objects to output.");
            }
            
            return new StringTag(sb.ToString());
        }

        public override string ToString()
        {
            var result = $"[Layer Height: {_layerHeight}] " +
                         $"[Bottom layers: {_bottomLayers}] " +
                         $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                         $"[X: {_xSize} Y:{_ySize} Z:{_zSize}] " +
                         $"[TB:{_topBottomMargin} LR:{_leftRightMargin}] " +
                         $"[Model: {_outputTLObject.ToByte()}{_outputTCObject.ToByte()}{_outputTRObject.ToByte()}" +
                         $"|{_outputMLObject.ToByte()}{_outputMCObject.ToByte()}{_outputMRObject.ToByte()}" +
                         $"|{_outputBLObject.ToByte()}{_outputBCObject.ToByte()}{_outputBRObject.ToByte()}] " +
                         $"[Hollow: {_hollowModel} @ {_wallThickness}mm] [Relief: {_centerHoleRelief}] [Mirror: {_mirrorOutput}]";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }

        #endregion

        #region Properties

        public decimal DisplayWidth
        {
            get => _displayWidth;
            set
            {
                if(!RaiseAndSetIfChanged(ref _displayWidth, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(Xppmm));
            }
        }

        public decimal DisplayHeight
        {
            get => _displayHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _displayHeight, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(Yppmm));
            }
        }

        public decimal Xppmm => DisplayWidth > 0 ? Math.Round(SlicerFile.Resolution.Width / DisplayWidth, 2) : 0;
        public decimal Yppmm => DisplayHeight > 0 ? Math.Round(SlicerFile.Resolution.Height / DisplayHeight, 2) : 0;

        public decimal LayerHeight
        {
            get => _layerHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _layerHeight, Layer.RoundHeight(value))) return;
                RaisePropertyChanged(nameof(BottomLayersMM));
                RaisePropertyChanged(nameof(LayerCount));
                RaisePropertyChanged(nameof(RealZSize));
                RaisePropertyChanged(nameof(ObservedZSize));
            }
        }

        public ushort Microns => (ushort)(LayerHeight * 1000);

        public ushort BottomLayers
        {
            get => _bottomLayers;
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottomLayers, value)) return;
                RaisePropertyChanged(nameof(BottomLayersMM));
            }
        }

        public decimal BottomLayersMM => Layer.RoundHeight(LayerHeight * BottomLayers);

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

        public ushort TopBottomMargin
        {
            get => _topBottomMargin;
            set => RaiseAndSetIfChanged(ref _topBottomMargin, value);
        }

        public ushort LeftRightMargin
        {
            get => _leftRightMargin;
            set => RaiseAndSetIfChanged(ref _leftRightMargin, value);
        }

        public decimal XSize
        {
            get => _xSize;
            set
            {
                if(!RaiseAndSetIfChanged(ref _xSize, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(RealXSize));
                RaisePropertyChanged(nameof(ObservedXSize));
            }
        }

        public decimal YSize
        {
            get => _ySize;
            set
            {
                if(!RaiseAndSetIfChanged(ref _ySize, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(RealYSize));
                RaisePropertyChanged(nameof(ObservedYSize));
            }
        }

        public decimal ZSize
        {
            get => _zSize;
            set
            {
                if(!RaiseAndSetIfChanged(ref _zSize, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(LayerCount));
                RaisePropertyChanged(nameof(RealZSize));
                RaisePropertyChanged(nameof(ObservedZSize));
            }
        }

        public decimal RealXSize
        {
            get
            {
                decimal pixels = _xSize * Xppmm;
                if (pixels <= 0) return 0;
                return Math.Round(_xSize - (pixels - Math.Truncate(pixels)) / Xppmm, 2);
            }

        }

        public decimal RealYSize
        {
            get
            {
                decimal pixels = _ySize * Yppmm;
                if (pixels <= 0) return 0;
                return Math.Round(_ySize - (pixels - Math.Truncate(pixels)) / Yppmm, 2);
            }
        }

        public decimal RealZSize => LayerCount * _layerHeight;
        
        public uint XPixels => (uint)Math.Floor(XSize * Xppmm);
        public uint YPixels => (uint)Math.Floor(YSize * Yppmm);

        public uint LayerCount => (uint) Math.Floor(ZSize / LayerHeight);

        public bool CenterHoleRelief
        {
            get => _centerHoleRelief;
            set => RaiseAndSetIfChanged(ref _centerHoleRelief, value);
        }

        public bool HollowModel
        {
            get => _hollowModel;
            set => RaiseAndSetIfChanged(ref _hollowModel, value);
        }

        public bool MirrorOutput
        {
            get => _mirrorOutput;
            set => RaiseAndSetIfChanged(ref _mirrorOutput, value);
        }

        public decimal WallThickness
        {
            get => _wallThickness;
            set
            {
                if(!RaiseAndSetIfChanged(ref _wallThickness, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(WallThicknessRealXSize));
                RaisePropertyChanged(nameof(WallThicknessRealYSize));
            }
        }

        public decimal WallThicknessRealXSize
        {
            get
            {
                decimal pixels = _wallThickness * Xppmm;
                if (pixels <= 0) return 0;
                return Math.Round(_wallThickness - (pixels - Math.Truncate(pixels)) / Xppmm, 2);
            }
        }

        public decimal WallThicknessRealYSize
        {
            get
            {
                decimal pixels = _wallThickness * Yppmm;
                if (pixels <= 0) return 0;
                return Math.Round(_wallThickness - (pixels - Math.Truncate(pixels)) / Yppmm, 2);
            }
        }

        public uint WallThicknessXPixels => (uint)Math.Floor(WallThickness * Xppmm);
        public uint WallThicknessYPixels => (uint)Math.Floor(WallThickness * Yppmm);

        public bool OutputTLObject
        {
            get => _outputTLObject;
            set
            {
                if(!RaiseAndSetIfChanged(ref _outputTLObject, value)) return;
                RaisePropertyChanged(nameof(OutputObjects));
            }
        }

        public bool OutputTCObject
        {
            get => _outputTCObject;
            set
            {
                if (!RaiseAndSetIfChanged(ref _outputTCObject, value)) return;
                RaisePropertyChanged(nameof(OutputObjects));
            }
        }

        public bool OutputTRObject
        {
            get => _outputTRObject;
            set
            {
                if (!RaiseAndSetIfChanged(ref _outputTRObject, value)) return;
                RaisePropertyChanged(nameof(OutputObjects));
            }
        }

        public bool OutputMLObject
        {
            get => _outputMLObject;
            set
            {
                if (!RaiseAndSetIfChanged(ref _outputMLObject, value)) return;
                RaisePropertyChanged(nameof(OutputObjects));
            }
        }

        public bool OutputMCObject
        {
            get => _outputMCObject;
            set
            {
                if (!RaiseAndSetIfChanged(ref _outputMCObject, value)) return;
                RaisePropertyChanged(nameof(OutputObjects));
            }
        }

        public bool OutputMRObject
        {
            get => _outputMRObject;
            set
            {
                if (!RaiseAndSetIfChanged(ref _outputMRObject, value)) return;
                RaisePropertyChanged(nameof(OutputObjects));
            }
        }

        public bool OutputBLObject
        {
            get => _outputBLObject;
            set
            {
                if (!RaiseAndSetIfChanged(ref _outputBLObject, value)) return;
                RaisePropertyChanged(nameof(OutputObjects));
            }
        }

        public bool OutputBCObject
        {
            get => _outputBCObject;
            set
            {
                if (!RaiseAndSetIfChanged(ref _outputBCObject, value)) return;
                RaisePropertyChanged(nameof(OutputObjects));
            }
        }

        public bool OutputBRObject
        {
            get => _outputBRObject;
            set
            {
                if (!RaiseAndSetIfChanged(ref _outputBRObject, value)) return;
                RaisePropertyChanged(nameof(OutputObjects));
            }
        }
        
        public byte OutputObjects => (byte) (_outputTLObject.ToByte() +
                                             _outputTCObject.ToByte() +
                                             _outputTRObject.ToByte() +
                                             _outputMLObject.ToByte() +
                                             _outputMCObject.ToByte() +
                                             _outputMRObject.ToByte() +
                                             _outputBLObject.ToByte() +
                                             _outputBCObject.ToByte() +
                                             _outputBRObject.ToByte());

        public decimal ObservedXSize
        {
            get => _observedXSize;
            set
            {
                if(!RaiseAndSetIfChanged(ref _observedXSize, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(ScaleXFactor));
            }
        }

        public decimal ObservedYSize
        {
            get => _observedYSize;
            set
            {
                if(!RaiseAndSetIfChanged(ref _observedYSize, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(ScaleYFactor));
            }
        }

        public decimal ObservedZSize
        {
            get => _observedZSize;
            set
            {
                if(!RaiseAndSetIfChanged(ref _observedZSize, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(ScaleZFactor));
            }
        }

        // 15 - x
        // 14 - 100
        public decimal ScaleXFactor => ObservedXSize > 0 && RealXSize > 0 ? Math.Round(RealXSize * 100 / ObservedXSize, 2) : 100;
        public decimal ScaleYFactor => ObservedYSize > 0 && RealYSize > 0 ? Math.Round(RealYSize * 100 / ObservedYSize, 2) : 100;
        public decimal ScaleZFactor => ObservedZSize > 0 && RealZSize > 0 ? Math.Round(RealZSize * 100 / ObservedZSize, 2) : 100;

        #endregion

        #region Constructor

        public OperationCalibrateXYZAccuracy() { }

        public OperationCalibrateXYZAccuracy(FileFormat slicerFile) : base(slicerFile)
        {
            _layerHeight = (decimal)slicerFile.LayerHeight;
            _bottomLayers = slicerFile.BottomLayerCount;
            _bottomExposure = (decimal)slicerFile.BottomExposureTime;
            _normalExposure = (decimal)slicerFile.ExposureTime;
            _mirrorOutput = slicerFile.MirrorDisplay;
        }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();
            if (SlicerFile.DisplayWidth > 0)
                DisplayWidth = (decimal)SlicerFile.DisplayWidth;
            if (SlicerFile.DisplayHeight > 0)
                DisplayHeight = (decimal)SlicerFile.DisplayHeight;
        }

        #endregion

        #region Enums

        #endregion

        #region Properties



        /*public override string ToString()
        {
            var result = $"[{_blurOperation}] [Size: {_size}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }*/

        #endregion

        #region Equality

        private bool Equals(OperationCalibrateXYZAccuracy other)
        {
            return _layerHeight == other._layerHeight && _bottomLayers == other._bottomLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _topBottomMargin == other._topBottomMargin && _leftRightMargin == other._leftRightMargin && _xSize == other._xSize && _ySize == other._ySize && _zSize == other._zSize && _centerHoleRelief == other._centerHoleRelief && _hollowModel == other._hollowModel && _wallThickness == other._wallThickness && _observedXSize == other._observedXSize && _observedYSize == other._observedYSize && _observedZSize == other._observedZSize && _outputTLObject == other._outputTLObject && _outputTCObject == other._outputTCObject && _outputTRObject == other._outputTRObject && _outputMLObject == other._outputMLObject && _outputMCObject == other._outputMCObject && _outputMRObject == other._outputMRObject && _outputBLObject == other._outputBLObject && _outputBCObject == other._outputBCObject && _outputBRObject == other._outputBRObject && _mirrorOutput == other._mirrorOutput;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationCalibrateXYZAccuracy other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_layerHeight);
            hashCode.Add(_bottomLayers);
            hashCode.Add(_bottomExposure);
            hashCode.Add(_normalExposure);
            hashCode.Add(_topBottomMargin);
            hashCode.Add(_leftRightMargin);
            hashCode.Add(_xSize);
            hashCode.Add(_ySize);
            hashCode.Add(_zSize);
            hashCode.Add(_centerHoleRelief);
            hashCode.Add(_hollowModel);
            hashCode.Add(_mirrorOutput);
            hashCode.Add(_wallThickness);
            hashCode.Add(_observedXSize);
            hashCode.Add(_observedYSize);
            hashCode.Add(_observedZSize);
            hashCode.Add(_outputTLObject);
            hashCode.Add(_outputTCObject);
            hashCode.Add(_outputTRObject);
            hashCode.Add(_outputMLObject);
            hashCode.Add(_outputMCObject);
            hashCode.Add(_outputMRObject);
            hashCode.Add(_outputBLObject);
            hashCode.Add(_outputBCObject);
            hashCode.Add(_outputBRObject);
            return hashCode.ToHashCode();
        }

        #endregion

        #region Methods

        public void SelectNoneObjects()
        {
            OutputTLObject = false;
            OutputTCObject = false;
            OutputTRObject = false;
            OutputMLObject = false;
            OutputMCObject = false;
            OutputMRObject = false;
            OutputBLObject = false;
            OutputBCObject = false;
            OutputBRObject = false;
        }

        public void SelectAllObjects()
        {
            OutputTLObject = true;
            OutputTCObject = true;
            OutputTRObject = true;
            OutputMLObject = true;
            OutputMCObject = true;
            OutputMRObject = true;
            OutputBLObject = true;
            OutputBCObject = true;
            OutputBRObject = true;
        }

        public void SelectCrossedObjects()
        {
            OutputTLObject = false;
            OutputTCObject = true;
            OutputTRObject = false;
            OutputMLObject = true;
            OutputMCObject = true;
            OutputMRObject = true;
            OutputBLObject = false;
            OutputBCObject = true;
            OutputBRObject = false;
        }

        public void SelectCenterObject()
        {
            OutputTLObject = false;
            OutputTCObject = false;
            OutputTRObject = false;
            OutputMLObject = false;
            OutputMCObject = true;
            OutputMRObject = false;
            OutputBLObject = false;
            OutputBCObject = false;
            OutputBRObject = false;
        }

        public void Sanitize()
        {
            for (ushort i = 0; i < 10000 && (_xSize * Xppmm).DecimalDigits() >= 1; i++)
            {
                XSize += 0.01M;
            }

            for (ushort i = 0; i < 10000 && (_ySize * Yppmm).DecimalDigits() >= 1; i++)
            {
                YSize += 0.01M;
            }

            for (ushort i = 0; i < 10000 && (_zSize / LayerHeight).DecimalDigits() >= 1; i++)
            {
                ZSize += 0.01M;
            }
        }

        public Mat[] GetLayers()
        {
            var layers = new Mat[2];
            for (byte i = 0; i < layers.Length; i++)
            {
                layers[i] = EmguExtensions.InitMat(SlicerFile.Resolution);
            }

            
            int currentX = 0;
            int currentY = 0;
            string positionYStr = string.Empty;
            string positionStr = string.Empty;

            const FontFace fontFace = FontFace.HersheyDuplex;
            const byte fontStartX = 30;
            const byte fontStartY = 50;
            const double fontScale = 1.3;
            const byte fontThickness = 3;

            for (int y = 0; y < 3; y++)
            {
                switch (y)
                {
                    case 0:
                        currentY = _topBottomMargin;
                        positionYStr = "T";
                        break;
                    case 1:
                        currentY = (int)(SlicerFile.Resolution.Height / 2 - YPixels / 2);
                        positionYStr = "M";
                        break;
                    case 2:
                        currentY = (int)(SlicerFile.Resolution.Height - YPixels - _topBottomMargin);
                        positionYStr = "B";
                        break;
                }
                for (int x = 0; x < 3; x++)
                {
                    switch (x)
                    {
                        case 0:
                            currentX = _leftRightMargin;
                            positionStr = $"{positionYStr}L";
                            break;
                        case 1:
                            currentX = (int)(SlicerFile.Resolution.Width / 2 - XPixels / 2);
                            positionStr = $"{positionYStr}C";
                            break;
                        case 2:
                            currentX = (int)(SlicerFile.Resolution.Width - XPixels - _leftRightMargin);
                            positionStr = $"{positionYStr}R";
                            break;
                    }


                    for (var i = 0; i < layers.Length; i++)
                    {
                        if(y == 0 && x == 0 && !_outputTLObject) continue;
                        if(y == 0 && x == 1 && !_outputTCObject) continue;
                        if(y == 0 && x == 2 && !_outputTRObject) continue;
                        if(y == 1 && x == 0 && !_outputMLObject) continue;
                        if(y == 1 && x == 1 && !_outputMCObject) continue;
                        if(y == 1 && x == 2 && !_outputMRObject) continue;
                        if(y == 2 && x == 0 && !_outputBLObject) continue;
                        if(y == 2 && x == 1 && !_outputBCObject) continue;
                        if(y == 2 && x == 2 && !_outputBRObject) continue;
                        var layer = layers[i];
                        CvInvoke.Rectangle(layer, 
                            new Rectangle(currentX, currentY, (int) XPixels, (int) YPixels), 
                            EmguExtensions.WhiteByte, -1);
                        
                        CvInvoke.PutText(layer, positionStr, 
                            new Point(currentX + fontStartX, currentY + fontStartY), fontFace, fontScale, 
                            EmguExtensions.BlackByte, fontThickness);

                        CvInvoke.PutText(layer, $"{XSize},{YSize},{ZSize}",
                            new Point(currentX + fontStartX, (int) (currentY + YPixels - fontStartY + 25)), fontFace, fontScale,
                            EmguExtensions.BlackByte, fontThickness);

                        if (CenterHoleRelief)
                        {
                            CvInvoke.Circle(layer,
                                new Point((int) (currentX + XPixels / 2), (int) (currentY + YPixels / 2)),
                                (int) (Math.Min(XPixels, YPixels) / 4),
                                EmguExtensions.Black3Byte, -1);
                        }

                        if (_hollowModel && i != 0 && _wallThickness > 0)
                        {
                            Size rectSize = new Size((int) (XPixels - WallThicknessXPixels * 2), (int) (YPixels - WallThicknessYPixels * 2));
                            Point rectLocation = new Point((int) (currentX + WallThicknessXPixels), (int) (currentY + WallThicknessYPixels));
                            CvInvoke.Rectangle(layers[i], new Rectangle(rectLocation, rectSize),
                                EmguExtensions.Black3Byte, -1);
                        }
                    }
                }
            }

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
            CvInvoke.PutText(thumbnail, "XYZ Accuracy Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
            CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguExtensions.White3Byte, fontThickness);
            CvInvoke.PutText(thumbnail, $"{XSize} x {YSize} x {ZSize} mm", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.White3Byte, fontThickness);

            /*thumbnail.SetTo(EmguExtensions.Black3Byte);
                
                CvInvoke.Circle(thumbnail, new Point(400/2, 200/2), 200/2, EmguExtensions.White3Byte, -1);
                for (int angle = 0; angle < 360; angle+=20)
                {
                    CvInvoke.Line(thumbnail, new Point(400 / 2, 200 / 2), new Point((int)(400 / 2 + 100 * Math.Cos(angle * Math.PI / 180)), (int)(200 / 2 + 100 * Math.Sin(angle * Math.PI / 180))), new MCvScalar(255, 27, 245), 3);
                }
                
                thumbnail.Save("D:\\Thumbnail.png");*/
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
            var layer = new Layer(0, layers[1], SlicerFile.LayerManager)
            {
                IsModified = true
            };

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                newLayers[layerIndex] = SlicerFile.GetInitialLayerValueOrNormal(layerIndex, bottomLayer.Clone(), layer.Clone());
                progress++;
            }

            foreach (var mat in layers)
            {
                mat.Dispose();
            }

            if (SlicerFile.ThumbnailsCount > 0)
                SlicerFile.SetThumbnails(GetThumbnail());

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.LayerHeight = (float)LayerHeight;
                SlicerFile.BottomExposureTime = (float)BottomExposure;
                SlicerFile.ExposureTime = (float)NormalExposure;
                SlicerFile.BottomLayerCount = BottomLayers;
                SlicerFile.LayerManager.Layers = newLayers;
            }, true);
            
            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
