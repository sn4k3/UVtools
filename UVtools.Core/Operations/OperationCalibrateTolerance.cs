/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationCalibrateTolerance : Operation
    {
        #region Members
        private decimal _displayWidth;
        private decimal _displayHeight;
        private decimal _layerHeight;
        private ushort _bottomLayers;
        private decimal _bottomExposure;
        private decimal _normalExposure;
        private decimal _zSize = 10;
        private ushort _topBottomMargin = 100;
        private ushort _leftRightMargin = 100;
        private byte _chamferLayers = 4;
        private byte _erodeBottomIterations = 4;
        private Shapes _shape = Shapes.Circle;
        private ushort _partMargin = 50;
        private bool _outputSameDiameterPart = true;
        private bool _fuseParts;
        private bool _enableAntiAliasing = true;
        private bool _mirrorOutput;

        private decimal _femaleDiameter = 16;
        private decimal _femaleHoleDiameter = 10;

        private ushort _maleThinnerModels = 5;
        private decimal _maleThinnerOffset;
        private decimal _maleThinnerStep = -0.1M;
        private ushort _maleThickerModels;
        private decimal _maleThickerOffset;
        private decimal _maleThickerStep = 0.1M;
        #endregion

        #region Overrides

        public override bool CanROI => false;

        public override bool CanCancel => false;

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override string Title => "Tolerance";
        public override string Description =>
            "Generates test models with various strategies and increments to verify the part tolerances.\n" +
            "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
            "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

        public override string ConfirmationText =>
            $"generate the tolerance test?";

        public override string ProgressTitle =>
            $"Generating the tolerance test";

        public override string ProgressAction => "Generated";

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            if (_displayWidth <= 0)
            {
                sb.AppendLine("Display width must be a positive value.");
            }

            if (_displayHeight <= 0)
            {
                sb.AppendLine("Display height must be a positive value.");
            }

            if (_femaleHoleDiameter >= _femaleDiameter)
            {
                sb.AppendLine("Hole diameter must be smaller than female diameter.");
            }
            
            if (OutputObjects <= 0)
            {
                sb.AppendLine("No objects to output.");
            }
            
            return sb.ToString();
        }

        public override string ToString()
        {
            var result = $"[Layer Height: {_layerHeight}] " +
                         $"[Bottom layers: {_bottomLayers}] " +
                         $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                         $"[Z: {_zSize}] " +
                         $"[TB:{_topBottomMargin} LR:{_leftRightMargin} PM:{_partMargin}] " +
                         $"[Chamfer: {_chamferLayers}] [Erode: {_erodeBottomIterations}] " +
                         $"[OSHD: {_outputSameDiameterPart}] [Fuse: {_fuseParts}] [AA: {_enableAntiAliasing}] [Mirror: {_mirrorOutput}]" +
                         $"[{_shape}, {_femaleDiameter}/{_femaleHoleDiameter}] " +
                         $"[tM: {_maleThinnerModels} O:{_maleThinnerOffset} S:{_maleThinnerStep}] " +
                         $"[TM: {_maleThickerModels} O:{_maleThickerOffset} S:{_maleThickerStep}] ";
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
                //RaisePropertyChanged(nameof(ObservedZSize));
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

        public decimal ZSize
        {
            get => _zSize;
            set
            {
                if(!RaiseAndSetIfChanged(ref _zSize, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(LayerCount));
                RaisePropertyChanged(nameof(RealZSize));
                //RaisePropertyChanged(nameof(ObservedZSize));
            }
        }

        public uint LayerCount => (uint)(ZSize / LayerHeight);

        public decimal RealZSize => LayerCount * _layerHeight;

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

        public byte ChamferLayers
        {
            get => _chamferLayers;
            set => RaiseAndSetIfChanged(ref _chamferLayers, value);
        }

        public byte ErodeBottomIterations
        {
            get => _erodeBottomIterations;
            set => RaiseAndSetIfChanged(ref _erodeBottomIterations, value);
        }

        public Shapes Shape
        {
            get => _shape;
            set => RaiseAndSetIfChanged(ref _shape, value);
        }

        public ushort PartMargin
        {
            get => _partMargin;
            set => RaiseAndSetIfChanged(ref _partMargin, value);
        }

        public bool OutputSameDiameterPart
        {
            get => _outputSameDiameterPart;
            set => RaiseAndSetIfChanged(ref _outputSameDiameterPart, value);
        }

        public bool FuseParts
        {
            get => _fuseParts;
            set
            {
                if(!RaiseAndSetIfChanged(ref _fuseParts, value)) return;
                if (value)
                {
                    OutputSameDiameterPart = false;
                    MaleThickerModels = 0;
                }
            }
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

        public decimal FemaleDiameter
        {
            get => _femaleDiameter;
            set
            {
                if(!RaiseAndSetIfChanged(ref _femaleDiameter, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(FemaleDiameterXPixels));
                RaisePropertyChanged(nameof(FemaleDiameterYPixels));
                RaisePropertyChanged(nameof(FemaleDiameterRealXSize));
                RaisePropertyChanged(nameof(FemaleDiameterRealYSize));
            }
        }

        public uint FemaleDiameterXPixels => (uint)(_femaleDiameter * Xppmm);
        public uint FemaleDiameterYPixels => (uint)(_femaleDiameter * Yppmm);

        public decimal FemaleDiameterRealXSize
        {
            get
            {
                decimal pixels = _femaleDiameter * Xppmm;
                return pixels <= 0 ? 0 : Math.Round(_femaleDiameter - (pixels - Math.Truncate(pixels)) / Xppmm, 2);
            }
        }

        public decimal FemaleDiameterRealYSize
        {
            get
            {
                decimal pixels = _femaleDiameter * Yppmm;
                return pixels <= 0 ? 0 : Math.Round(_femaleDiameter - (pixels - Math.Truncate(pixels)) / Yppmm, 2);
            }
        }

        public decimal FemaleHoleDiameter
        {
            get => _femaleHoleDiameter;
            set
            {
                if(!RaiseAndSetIfChanged(ref _femaleHoleDiameter, value)) return;
                RaisePropertyChanged(nameof(FemaleHoleDiameterXPixels));
                RaisePropertyChanged(nameof(FemaleHoleDiameterYPixels));
                RaisePropertyChanged(nameof(FemaleHoleDiameterRealXSize));
                RaisePropertyChanged(nameof(FemaleHoleDiameterRealYSize));
            }
        }

        public uint FemaleHoleDiameterXPixels => (uint)(_femaleHoleDiameter * Xppmm);
        public uint FemaleHoleDiameterYPixels => (uint)(_femaleHoleDiameter * Yppmm);

        public decimal FemaleHoleDiameterRealXSize
        {
            get
            {
                decimal pixels = _femaleHoleDiameter * Xppmm;
                return pixels <= 0 ? 0 : Math.Round(_femaleHoleDiameter - (pixels - Math.Truncate(pixels)) / Xppmm, 2);
            }
        }

        public decimal FemaleHoleDiameterRealYSize
        {
            get
            {
                decimal pixels = _femaleHoleDiameter * Yppmm;
                return pixels <= 0 ? 0 : Math.Round(_femaleHoleDiameter - (pixels - Math.Truncate(pixels)) / Yppmm, 2);
            }
        }

        public ushort MaleThinnerModels
        {
            get => _maleThinnerModels;
            set => RaiseAndSetIfChanged(ref _maleThinnerModels, value);
        }

        public decimal MaleThinnerOffset
        {
            get => _maleThinnerOffset;
            set => RaiseAndSetIfChanged(ref _maleThinnerOffset, Math.Round(value, 2));
        }

        public decimal MaleThinnerStep
        {
            get => _maleThinnerStep;
            set => RaiseAndSetIfChanged(ref _maleThinnerStep, Math.Round(value, 2));
        }

        public ushort MaleThickerModels
        {
            get => _maleThickerModels;
            set => RaiseAndSetIfChanged(ref _maleThickerModels, value);
        }

        public decimal MaleThickerOffset
        {
            get => _maleThickerOffset;
            set => RaiseAndSetIfChanged(ref _maleThickerOffset, Math.Round(value, 2));
        }

        public decimal MaleThickerStep
        {
            get => _maleThickerStep;
            set => RaiseAndSetIfChanged(ref _maleThickerStep, Math.Round(value, 2));
        }


        public uint OutputObjects =>
            (_outputSameDiameterPart ? 1u : 0) +
            _maleThinnerModels +
            _maleThickerModels +
            (_fuseParts ? 0 : 1u);

        /*public decimal ObservedXSize
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


        public decimal ScaleXFactor => ObservedXSize > 0 && RealXSize > 0 ? Math.Round(RealXSize * 100 / ObservedXSize, 2) : 100;
        public decimal ScaleYFactor => ObservedYSize > 0 && RealYSize > 0 ? Math.Round(RealYSize * 100 / ObservedYSize, 2) : 100;
        public decimal ScaleZFactor => ObservedZSize > 0 && RealZSize > 0 ? Math.Round(RealZSize * 100 / ObservedZSize, 2) : 100;
        */
        #endregion

        #region Constructor

        public OperationCalibrateTolerance() { }

        public OperationCalibrateTolerance(FileFormat slicerFile) : base(slicerFile)
        { }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();
            if (_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
            if (_bottomLayers <= 0) _bottomLayers = SlicerFile.BottomLayerCount;
            if (_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
            if (_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
            _mirrorOutput = SlicerFile.DisplayMirror != Enumerations.FlipDirection.None;

            if (SlicerFile.DisplayWidth > 0)
                DisplayWidth = (decimal)SlicerFile.DisplayWidth;
            if (SlicerFile.DisplayHeight > 0)
                DisplayHeight = (decimal)SlicerFile.DisplayHeight;
        }

        #endregion

        #region Enums

        public enum Shapes : byte
        {
            Circle,
            Square
        }

        public static Array ShapesItems => Enum.GetValues(typeof(Shapes));
        #endregion

        #region Equality

        private bool Equals(OperationCalibrateTolerance other)
        {
            return _layerHeight == other._layerHeight && _bottomLayers == other._bottomLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _zSize == other._zSize && _topBottomMargin == other._topBottomMargin && _leftRightMargin == other._leftRightMargin && _chamferLayers == other._chamferLayers && _erodeBottomIterations == other._erodeBottomIterations && _shape == other._shape && _partMargin == other._partMargin && _outputSameDiameterPart == other._outputSameDiameterPart && _fuseParts == other._fuseParts && _enableAntiAliasing == other._enableAntiAliasing && _femaleDiameter == other._femaleDiameter && _femaleHoleDiameter == other._femaleHoleDiameter && _maleThinnerModels == other._maleThinnerModels && _maleThinnerOffset == other._maleThinnerOffset && _maleThinnerStep == other._maleThinnerStep && _maleThickerModels == other._maleThickerModels && _maleThickerOffset == other._maleThickerOffset && _maleThickerStep == other._maleThickerStep && _mirrorOutput == other._mirrorOutput;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationCalibrateTolerance other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(_layerHeight);
            hashCode.Add(_bottomLayers);
            hashCode.Add(_bottomExposure);
            hashCode.Add(_normalExposure);
            hashCode.Add(_zSize);
            hashCode.Add(_topBottomMargin);
            hashCode.Add(_leftRightMargin);
            hashCode.Add(_chamferLayers);
            hashCode.Add(_erodeBottomIterations);
            hashCode.Add((int) _shape);
            hashCode.Add(_partMargin);
            hashCode.Add(_outputSameDiameterPart);
            hashCode.Add(_fuseParts);
            hashCode.Add(_enableAntiAliasing);
            hashCode.Add(_mirrorOutput);
            hashCode.Add(_femaleDiameter);
            hashCode.Add(_femaleHoleDiameter);
            hashCode.Add(_maleThinnerModels);
            hashCode.Add(_maleThinnerOffset);
            hashCode.Add(_maleThinnerStep);
            hashCode.Add(_maleThickerModels);
            hashCode.Add(_maleThickerOffset);
            hashCode.Add(_maleThickerStep);
            return hashCode.ToHashCode();
        }

        #endregion

        #region Methods
        public Mat[] GetLayers()
        {
            var layers = new Mat[LayerCount];
            var layer = EmguExtensions.InitMat(SlicerFile.Resolution);

            ushort startX = Math.Max((ushort)2, _leftRightMargin);
            ushort startY = Math.Max((ushort)2, _topBottomMargin);
            int currentX = startX;
            int currentY = startY;

            const FontFace fontFace = FontFace.HersheyDuplex;
            const double fontScale = 1;
            const byte fontThickness = 2;
            LineType lineType = _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected;

            var anchor = new Point(-1, -1);
            var kernel = EmguExtensions.Kernel3x3Rectangle;

            var pointTextList = new List<KeyValuePair<Point, string>>();

            if (!_fuseParts)
            {
                switch (Shape)
                {
                    case Shapes.Circle:
                        currentX += (int) FemaleDiameterXPixels / 2;
                        currentY += (int) FemaleDiameterXPixels / 2;
                        CvInvoke.Circle(layer, new Point(currentX, currentY), (int) (FemaleDiameterXPixels / 2), EmguExtensions.WhiteColor, -1, lineType);
                        CvInvoke.Circle(layer, new Point(currentX, currentY), (int) (FemaleHoleDiameterXPixels / 2), EmguExtensions.BlackColor, -1, lineType);
                        currentX += (int) FemaleDiameterXPixels / 2 + PartMargin;
                        
                        break;
                    case Shapes.Square:
                        int offsetX = (int) ((FemaleDiameterXPixels - FemaleHoleDiameterXPixels) / 2);
                        int offsetY = (int) ((FemaleDiameterYPixels - FemaleHoleDiameterYPixels) / 2);
                        CvInvoke.Rectangle(layer, new Rectangle(currentX, currentY, (int) FemaleDiameterXPixels, (int) FemaleDiameterXPixels), EmguExtensions.WhiteColor, -1, lineType);
                        CvInvoke.Rectangle(layer, new Rectangle(currentX + offsetX, currentY + offsetY, (int) FemaleHoleDiameterXPixels, (int) FemaleHoleDiameterYPixels), EmguExtensions.BlackColor, -1, lineType);
                        currentX += (int)FemaleDiameterXPixels + PartMargin;
                        currentY = startY + (int) FemaleDiameterYPixels / 2;
                        break;
                }
            }

            bool addPart(decimal step)
            {
                decimal millimeters = Math.Round(_femaleHoleDiameter + step, 2);
                if (millimeters <= 0) return false;
                int xPixels = (int)(millimeters * Xppmm);
                int yPixels = (int)(millimeters * Yppmm);
                Point partCenterText;

                if (_fuseParts)
                {
                    if (xPixels >= FemaleHoleDiameterXPixels || yPixels >= FemaleHoleDiameterYPixels) return false;
                    if (currentX + FemaleDiameterXPixels + _leftRightMargin >= SlicerFile.Resolution.Width)
                    {
                        currentX = startX;
                        currentY += (int)FemaleDiameterYPixels + PartMargin;
                    }

                    if (currentY + FemaleDiameterYPixels + _topBottomMargin >= SlicerFile.Resolution.Height)
                    {
                        return false; // Insufficient size
                    }

                    int halfDiameterX = (int)(FemaleDiameterXPixels / 2);
                    int halfDiameterY = (int)(FemaleDiameterYPixels / 2);

                    switch (Shape)
                    {
                        case Shapes.Circle:
                            CvInvoke.Circle(layer, new Point(currentX + halfDiameterX, currentY + halfDiameterY), (int)(FemaleDiameterXPixels / 2), EmguExtensions.WhiteColor, -1, lineType);
                            CvInvoke.Circle(layer, new Point(currentX + halfDiameterX, currentY + halfDiameterY), (int)(FemaleHoleDiameterXPixels / 2), EmguExtensions.BlackColor, -1, lineType);
                            CvInvoke.Circle(layer, new Point(currentX + halfDiameterX, currentY + halfDiameterY), xPixels / 2, EmguExtensions.WhiteColor, -1, lineType);
                            break;
                        case Shapes.Square:
                            int offsetX = (int)((FemaleDiameterXPixels - FemaleHoleDiameterXPixels) / 2);
                            int offsetY = (int)((FemaleDiameterYPixels - FemaleHoleDiameterYPixels) / 2);
                            CvInvoke.Rectangle(layer, new Rectangle(currentX, currentY, (int)FemaleDiameterXPixels, (int)FemaleDiameterXPixels), EmguExtensions.WhiteColor, -1, lineType);
                            CvInvoke.Rectangle(layer, new Rectangle(currentX + offsetX, currentY + offsetY, (int)FemaleHoleDiameterXPixels, (int)FemaleHoleDiameterYPixels), EmguExtensions.BlackColor, -1, lineType);
                            offsetX = (int)((FemaleDiameterXPixels - xPixels) / 2);
                            offsetY = (int)((FemaleDiameterYPixels - yPixels) / 2);
                            CvInvoke.Rectangle(layer, new Rectangle(currentX + offsetX, currentY + offsetY, xPixels, yPixels), EmguExtensions.WhiteColor, -1, lineType);
                            break;
                    }

                    partCenterText = new Point(currentX + halfDiameterX - 60, currentY + halfDiameterY + 10);
                    currentX += (int)FemaleDiameterXPixels + PartMargin;
                }
                else
                {
                    if (currentX + xPixels + _leftRightMargin >= SlicerFile.Resolution.Width)
                    {
                        currentX = startX;
                        currentY += yPixels + PartMargin;
                    }

                    if (currentY + yPixels + _topBottomMargin >= SlicerFile.Resolution.Height)
                    {
                        return false; // Insufficient size
                    }

                    int halfDiameterX = xPixels / 2;
                    int halfDiameterY = yPixels / 2;

                    switch (Shape)
                    {
                        case Shapes.Circle:
                            CvInvoke.Circle(layer, new Point(currentX + halfDiameterX, currentY + halfDiameterY), halfDiameterX, EmguExtensions.WhiteColor, -1, lineType);
                            break;
                        case Shapes.Square:
                            CvInvoke.Rectangle(layer, new Rectangle(currentX, currentY, xPixels, yPixels), EmguExtensions.WhiteColor, -1, lineType);
                            break;
                    }

                    partCenterText = new Point(currentX + halfDiameterX - 60, currentY + halfDiameterY + 10);

                    currentX += xPixels + PartMargin;
                }

                pointTextList.Add(new KeyValuePair<Point, string>(partCenterText, step > 0 ? $"+{step:F2}" : $"{step:F2}"));

                return true;
            }

            if (!_fuseParts && _outputSameDiameterPart)
            {
                addPart(0);
            }

            for (int i = 1; i <= _maleThinnerModels; i++)
            {
                var step = _maleThinnerOffset + _maleThinnerStep * i;
                if (!addPart(step)) break;
            }

            for (int i = 1; i <= _maleThickerModels; i++)
            {
                var step = _maleThickerOffset + _maleThickerStep * i;
                if (!addPart(step)) break;
            }

            Parallel.For(0, layers.Length, CoreSettings.ParallelOptions, layerIndex =>
                //for (var i = 0; i < layers.Length; i++)
            {
                layers[layerIndex] = layer.Clone();
            });

            if (_erodeBottomIterations > 0)
            {
                Parallel.For(0, _bottomLayers, CoreSettings.ParallelOptions, layerIndex => 
                { 
                    CvInvoke.Erode(layers[layerIndex], layers[layerIndex], kernel, anchor, _erodeBottomIterations, BorderType.Reflect101, default);
                });
            }

            if (_chamferLayers > 0)
            {
                Parallel.For(0, _chamferLayers, CoreSettings.ParallelOptions, layerIndexOffset =>
                {
                    var iteration = _chamferLayers - layerIndexOffset;
                    CvInvoke.Erode(layers[layerIndexOffset], layers[layerIndexOffset], kernel, anchor, iteration, BorderType.Reflect101, default);

                    var layerIndex = layers.Length - 1 - layerIndexOffset;
                    CvInvoke.Erode(layers[layerIndex], layers[layerIndex], kernel, anchor, iteration, BorderType.Reflect101, default);
                });
                /*byte iterations = _chamferLayers;
                var layerIndex = 0;
                for (; layerIndex < LayerCount && iterations > 0; layerIndex++)
                {
                    CvInvoke.Erode(layers[layerIndex], layers[layerIndex], kernel, anchor, iterations--, BorderType.Reflect101, default);
                }

                iterations = _chamferLayers;
                for (int i = (int) (LayerCount - 1); i >= 0 && i > layerIndex && iterations > 0; i--)
                {
                    CvInvoke.Erode(layers[i], layers[i], kernel, anchor, iterations--, BorderType.Reflect101, default);
                }*/
            }

            Parallel.For(Math.Max(0u, LayerCount - 15), LayerCount, CoreSettings.ParallelOptions, layerIndex =>
            {
                foreach (var keyValuePair in pointTextList)
                {
                    CvInvoke.PutText(layers[layerIndex], keyValuePair.Value, keyValuePair.Key, fontFace, fontScale, EmguExtensions.BlackColor, fontThickness, lineType);
                }
            });

            if (_mirrorOutput)
            {
                var flip = SlicerFile.DisplayMirror;
                if (flip == Enumerations.FlipDirection.None) flip = Enumerations.FlipDirection.Horizontally;
                Parallel.ForEach(layers, CoreSettings.ParallelOptions, mat => CvInvoke.Flip(mat, mat, Enumerations.ToOpenCVFlipType(flip)));
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
            CvInvoke.PutText(thumbnail, "Tolerance Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
            CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);
            CvInvoke.PutText(thumbnail, $"Objects: {OutputObjects}", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);

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

            Parallel.For(0, LayerCount, CoreSettings.ParallelOptions, layerIndex =>
            {
                newLayers[layerIndex] = new Layer((uint)layerIndex, layers[layerIndex], SlicerFile.LayerManager) {IsModified = true};
                layers[layerIndex].Dispose();
                progress.LockAndIncrement();
            });

            if (SlicerFile.ThumbnailsCount > 0)
                SlicerFile.SetThumbnails(GetThumbnail());

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.LayerHeight = (float)LayerHeight;
                SlicerFile.BottomExposureTime = (float)BottomExposure;
                SlicerFile.ExposureTime = (float)NormalExposure;
                SlicerFile.BottomLayerCount = BottomLayers;
                SlicerFile.TransitionLayerCount = 0;
                SlicerFile.LayerManager.Layers = newLayers;
            }, true);
            
            var moveOp = new OperationMove(SlicerFile);
            moveOp.Execute(progress);


            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
