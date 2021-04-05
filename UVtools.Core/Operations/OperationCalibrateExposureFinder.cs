/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationCalibrateExposureFinder : Operation
    {
        #region Constants

        const byte TextMarkingSpacing = 60;
        const byte TextMarkingLineBreak = 30;
        const FontFace TextMarkingFontFace = Emgu.CV.CvEnum.FontFace.HersheyDuplex;
        const byte TextMarkingStartX = 10;
        //const byte TextStartY = 50;
        const double TextMarkingScale = 0.8;
        const byte TextMarkingThickness = 2;

        #endregion

        #region Members
        private decimal _displayWidth;
        private decimal _displayHeight;
        private decimal _layerHeight = 0.05M;
        private ushort _bottomLayers = 3;
        private decimal _bottomExposure = 60;
        private decimal _normalExposure = 12;
        private decimal _topBottomMargin = 5;
        private decimal _leftRightMargin = 10;
        private byte _chamferLayers = 5;
        private byte _erodeBottomIterations = 0;
        private decimal _partMargin = 0;
        private bool _enableAntiAliasing = true;
        private bool _mirrorOutput;
        private decimal _baseHeight = 1;
        private decimal _featuresHeight = 1;
        private decimal _featuresMargin = 1.5m;
        private Measures _unitOfMeasure = Measures.Pixels;
        private string _holeDiametersPx = "2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13";
        private string _holeDiametersMm = "0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.2, 1.4";
        private decimal _barSpacing = 1.5m;
        private decimal _barLength = 4;
        private sbyte _barVerticalSplitter = 0;
        private string _barThicknessesPx = "4, 6, 8, 10, 12, 14, 16, 18, 20";
        private string _barThicknessesMm = "0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1, 1.2";
        private FontFace _textFont = TextMarkingFontFace;
        private double _textScale = 1;
        private byte _textThickness = 2;
        private string _text = "ABGHJKLMQRSTUVWXZ%&#";

        private bool _multipleBrightness;
        private CalibrateExposureFinderMultipleBrightnessExcludeFrom _multipleBrightnessExcludeFrom = CalibrateExposureFinderMultipleBrightnessExcludeFrom.BottomAndBase;
        private string _multipleBrightnessValues = "255, 242, 230, 217, 204, 191";
        private decimal _multipleBrightnessGenExposureTime;


        private bool _multipleLayerHeight;
        private decimal _multipleLayerHeightMaximum = 0.1m;
        private decimal _multipleLayerHeightStep = 0.01m;

        private bool _dontLiftSamePositionedLayers;
        private bool _multipleExposures;
        private ExposureGenTypes _exposureGenType = ExposureGenTypes.Linear;
        private bool _exposureGenIgnoreBaseExposure;
        private decimal _exposureGenBottomStep = 0.5m;
        private decimal _exposureGenNormalStep = 0.2m;
        private byte _exposureGenTests = 4;
        private decimal _exposureGenManualLayerHeight;
        private decimal _exposureGenManualBottom;
        private decimal _exposureGenManualNormal;
        private ObservableCollection<ExposureItem> _exposureTable = new();
        private CalibrateExposureFinderShapes _holeShape = CalibrateExposureFinderShapes.Square;

        #endregion

        #region Overrides

        public override bool CanROI => false;

        //public override bool CanCancel => false;

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override string Title => "Exposure time finder";
        public override string Description =>
            "Generates test models with various strategies and increments to verify the best exposure time for a given layer height.\n" +
            "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
            "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

        public override string ConfirmationText =>
            $"generate the exposure time finder test?";

        public override string ProgressTitle =>
            $"Generating the exposure time finder test";

        public override string ProgressAction => "Generated layers";

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

            if (Holes.Length <= 0)
            {
                sb.AppendLine("No objects to output.");
            }

            if (_multipleExposures)
            {
                var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
                for (decimal layerHeight = _layerHeight;
                    layerHeight <= endLayerHeight;
                    layerHeight += _multipleLayerHeightStep)
                {
                    bool found = false;
                    foreach (var exposureItem in _exposureTable)
                    {
                        if (exposureItem.LayerHeight == layerHeight && exposureItem.IsValid)
                        {
                            found = true;
                            break;
                        }
                    }
                    if(!found)
                        sb.AppendLine($"[ME]: {Layer.ShowHeight(layerHeight)}mm layer height have no exposure(s).");
                }
            }

            if (_multipleBrightness)
            {
                if (MultipleBrightnessValuesArray.Length == 0)
                {
                    sb.AppendLine($"[MB]: Multiple brightness tests are enabled but no valid values are set, use from 1 to 255.");
                }
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            var result = $"[Layer Height: {_layerHeight}] " +
                         $"[Bottom layers: {_bottomLayers}] " +
                         $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                         $"[TB:{_topBottomMargin} LR:{_leftRightMargin} PM:{_partMargin} FM:{_featuresMargin}]  " +
                         $"[Chamfer: {_chamferLayers}] [Erode: {_erodeBottomIterations}] " +
                         $"[Obj height: {_featuresHeight}] " +
                         $"[Holes: {Holes.Length}] [Bars: {Bars.Length}] [Text: {!string.IsNullOrWhiteSpace(_text)}]" +
                         $"[AA: {_enableAntiAliasing}] [Mirror: {_mirrorOutput}]";
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
        public decimal Ppmm => Math.Max(Xppmm, Yppmm);

        public decimal LayerHeight
        {
            get => _layerHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _layerHeight, Layer.RoundHeight(value))) return;
                RaisePropertyChanged(nameof(BottomLayersMM));
                RaisePropertyChanged(nameof(AvailableLayerHeights));
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
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottomExposure, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(MultipleBrightnessTable));
            }
        }

        public decimal NormalExposure
        {
            get => _normalExposure;
            set
            {
                if(!RaiseAndSetIfChanged(ref _normalExposure, Math.Round(value, 2))) return;
                RaisePropertyChanged(nameof(MultipleBrightnessTable));
            }
        }

        public decimal TopBottomMargin
        {
            get => _topBottomMargin;
            set => RaiseAndSetIfChanged(ref _topBottomMargin, Math.Round(value, 2));
        }

        public decimal LeftRightMargin
        {
            get => _leftRightMargin;
            set => RaiseAndSetIfChanged(ref _leftRightMargin, Math.Round(value, 2));
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

        public decimal PartMargin
        {
            get => _partMargin;
            set => RaiseAndSetIfChanged(ref _partMargin, Math.Round(value, 2));
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

        public decimal BaseHeight
        {
            get => _baseHeight;
            set => RaiseAndSetIfChanged(ref _baseHeight, Math.Round(value, 2));
        }

        public decimal FeaturesHeight
        {
            get => _featuresHeight;
            set => RaiseAndSetIfChanged(ref _featuresHeight, Math.Round(value, 2));
        }

        public decimal TotalHeight => _baseHeight + _featuresHeight;
        
        public decimal FeaturesMargin
        {
            get => _featuresMargin;
            set => RaiseAndSetIfChanged(ref _featuresMargin, Math.Round(value, 2));
        }

        public CalibrateExposureFinderShapes HoleShape
        {
            get => _holeShape;
            set => RaiseAndSetIfChanged(ref _holeShape, value);
        }

        public Measures UnitOfMeasure
        {
            get => _unitOfMeasure;
            set
            {
                if (!RaiseAndSetIfChanged(ref _unitOfMeasure, value)) return;
                RaisePropertyChanged(nameof(IsUnitOfMeasureMm));
            }
        }

        public bool IsUnitOfMeasureMm => _unitOfMeasure == Measures.Millimeters;

        public string HoleDiametersMm
        {
            get => _holeDiametersMm;
            set => RaiseAndSetIfChanged(ref _holeDiametersMm, value);
        }

        public string HoleDiametersPx
        {
            get => _holeDiametersPx;
            set => RaiseAndSetIfChanged(ref _holeDiametersPx, value);
        }

        /// <summary>
        /// Gets all holes in pixels and ordered
        /// </summary>
        public int[] Holes
        {
            get
            {
                List<int> holes = new();

                if (_unitOfMeasure == Measures.Millimeters)
                {
                    var split = _holeDiametersMm.Split(',', StringSplitOptions.TrimEntries);
                    foreach (var mmStr in split)
                    {
                        if (string.IsNullOrWhiteSpace(mmStr)) continue;
                        if (!decimal.TryParse(mmStr, out var mm)) continue;
                        var mmPx = (int)Math.Floor(mm * Ppmm);
                        if (mmPx <= 0 || mmPx > 500) continue;
                        if(holes.Contains(mmPx)) continue;
                        holes.Add((int)Math.Floor(mm * Ppmm));
                    }
                }
                else
                {
                    var split = _holeDiametersPx.Split(',', StringSplitOptions.TrimEntries);
                    foreach (var pxStr in split)
                    {
                        if (string.IsNullOrWhiteSpace(pxStr)) continue;
                        if (!int.TryParse(pxStr, out var px)) continue;
                        if (px <= 0 || px > 500) continue;
                        if (holes.Contains(px)) continue;
                        holes.Add(px);
                    }
                }

                return holes.OrderBy(pixels => pixels).ToArray();
            }
        }

        public int GetHolesLength(int[] holes)
        {
            return (int) (holes.Sum() + (holes.Length-1) * _featuresMargin * Yppmm);
        }

        public decimal BarSpacing
        {
            get => _barSpacing;
            set => RaiseAndSetIfChanged(ref _barSpacing, value);
        }

        public decimal BarLength
        {
            get => _barLength;
            set => RaiseAndSetIfChanged(ref _barLength, value);
        }

        public sbyte BarVerticalSplitter
        {
            get => _barVerticalSplitter;
            set => RaiseAndSetIfChanged(ref _barVerticalSplitter, value);
        }

        public string BarThicknessesPx
        {
            get => _barThicknessesPx;
            set => RaiseAndSetIfChanged(ref _barThicknessesPx, value);
        }

        public string BarThicknessesMm
        {
            get => _barThicknessesMm;
            set => RaiseAndSetIfChanged(ref _barThicknessesMm, value);
        }

        /// <summary>
        /// Gets all holes in pixels and ordered
        /// </summary>
        public int[] Bars
        {
            get
            {
                List<int> bars = new();

                if (_unitOfMeasure == Measures.Millimeters)
                {
                    var split = _barThicknessesMm.Split(',', StringSplitOptions.TrimEntries);
                    foreach (var mmStr in split)
                    {
                        if (string.IsNullOrWhiteSpace(mmStr)) continue;
                        if (!decimal.TryParse(mmStr, out var mm)) continue;
                        var mmPx = (int)Math.Floor(mm * Xppmm);
                        if (mmPx <= 0 || mmPx > 500) continue;
                        if (bars.Contains(mmPx)) continue;
                        bars.Add((int)Math.Floor(mm * Xppmm));
                    }
                }
                else
                {
                    var split = _barThicknessesPx.Split(',', StringSplitOptions.TrimEntries);
                    foreach (var pxStr in split)
                    {
                        if (string.IsNullOrWhiteSpace(pxStr)) continue;
                        if (!int.TryParse(pxStr, out var px)) continue;
                        if (px <= 0 || px > 500) continue;
                        if (bars.Contains(px)) continue;
                        bars.Add(px);
                    }
                }

                return bars.OrderBy(pixels => pixels).ToArray();
            }
        }

        public int GetBarsWidth(int[] bars)
        {
            return (int)(bars.Sum() + (bars.Length + 1) * _barSpacing * Yppmm);
        }

        public static Array TextFonts => Enum.GetValues(typeof(FontFace));

        public FontFace TextFont
        {
            get => _textFont;
            set => RaiseAndSetIfChanged(ref _textFont, value);
        }

        public double TextScale
        {
            get => _textScale;
            set => RaiseAndSetIfChanged(ref _textScale, Math.Round(value, 2));
        }

        public byte TextThickness
        {
            get => _textThickness;
            set => RaiseAndSetIfChanged(ref _textThickness, value);
        }

        public string Text
        {
            get => _text;
            set => RaiseAndSetIfChanged(ref _text, value);
        }

        public Size TextSize
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_text)) return Size.Empty;
                int baseline = 0;
                return CvInvoke.GetTextSize(_text, _textFont, _textScale, _textThickness, ref baseline);
            }
        }

        public bool MultipleBrightness
        {
            get => _multipleBrightness;
            set => RaiseAndSetIfChanged(ref _multipleBrightness, value);
        }

        public CalibrateExposureFinderMultipleBrightnessExcludeFrom MultipleBrightnessExcludeFrom
        {
            get => _multipleBrightnessExcludeFrom;
            set => RaiseAndSetIfChanged(ref _multipleBrightnessExcludeFrom, value);
        }

        public string MultipleBrightnessValues
        {
            get => _multipleBrightnessValues;
            set
            {
                if(!RaiseAndSetIfChanged(ref _multipleBrightnessValues, value)) return;
                RaisePropertyChanged(nameof(MultipleBrightnessTable));
            }
        }

        public List<ExposureItem> MultipleBrightnessTable
        {
            get
            {
                var brightnesses = MultipleBrightnessValuesArray;
                return brightnesses.Select(brightness => (ExposureItem) 
                    new(
                        _layerHeight,
                        Math.Round(brightness * _bottomExposure / byte.MaxValue, 2),
                        Math.Round(brightness * _normalExposure / byte.MaxValue, 2),
                        brightness)).ToList();
            }
        }

        public decimal MultipleBrightnessGenExposureTime
        {
            get => _multipleBrightnessGenExposureTime;
            set => RaiseAndSetIfChanged(ref _multipleBrightnessGenExposureTime, value);
        }

        /// <summary>
        /// Gets all holes in pixels and ordered
        /// </summary>
        public byte[] MultipleBrightnessValuesArray
        {
            get
            {
                List<byte> values = new();

                var split = _multipleBrightnessValues.Split(',', StringSplitOptions.TrimEntries);
                foreach (var brightnessStr in split)
                {
                    if (string.IsNullOrWhiteSpace(brightnessStr)) continue;
                    if (!byte.TryParse(brightnessStr, out var brightness)) continue;
                    if (brightness <= 0 || brightness > 255) continue;
                    if (values.Contains(brightness)) continue;
                    values.Add(brightness);
                }

                return values.OrderByDescending(brightness => brightness).ToArray();
            }
        }


        public bool MultipleLayerHeight
        {
            get => _multipleLayerHeight;
            set
            {
                if(!RaiseAndSetIfChanged(ref _multipleLayerHeight, value)) return;
                RaisePropertyChanged(nameof(AvailableLayerHeights));
            }
        }

        public decimal MultipleLayerHeightMaximum
        {
            get => _multipleLayerHeightMaximum;
            set
            {
                if(!RaiseAndSetIfChanged(ref _multipleLayerHeightMaximum, value)) return;
                RaisePropertyChanged(nameof(AvailableLayerHeights));
            }
        }

        public decimal MultipleLayerHeightStep
        {
            get => _multipleLayerHeightStep;
            set
            {
                if(!RaiseAndSetIfChanged(ref _multipleLayerHeightStep, value)) return;
                RaisePropertyChanged(nameof(AvailableLayerHeights));
            }
        }

        public bool DontLiftSamePositionedLayers
        {
            get => _dontLiftSamePositionedLayers;
            set => RaiseAndSetIfChanged(ref _dontLiftSamePositionedLayers, value);
        }

        public bool MultipleExposures
        {
            get => _multipleExposures;
            set => RaiseAndSetIfChanged(ref _multipleExposures, value);
        }

        public ExposureGenTypes ExposureGenType
        {
            get => _exposureGenType;
            set => RaiseAndSetIfChanged(ref _exposureGenType, value);
        }

        public bool ExposureGenIgnoreBaseExposure
        {
            get => _exposureGenIgnoreBaseExposure;
            set => RaiseAndSetIfChanged(ref _exposureGenIgnoreBaseExposure, value);
        }

        public decimal ExposureGenBottomStep
        {
            get => _exposureGenBottomStep;
            set => RaiseAndSetIfChanged(ref _exposureGenBottomStep, Math.Round(value, 2));
        }

        public decimal ExposureGenNormalStep
        {
            get => _exposureGenNormalStep;
            set => RaiseAndSetIfChanged(ref _exposureGenNormalStep, Math.Round(value, 2));
        }

        public byte ExposureGenTests
        {
            get => _exposureGenTests;
            set => RaiseAndSetIfChanged(ref _exposureGenTests, value);
        }

        public decimal ExposureGenManualLayerHeight
        {
            get => _exposureGenManualLayerHeight;
            set => RaiseAndSetIfChanged(ref _exposureGenManualLayerHeight, value);
        }

        public decimal[] AvailableLayerHeights
        {
            get
            {
                List<decimal> layerHeights = new List<decimal>();
                var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
                List<ExposureItem> list = new();
                for (decimal layerHeight = _layerHeight; layerHeight <= endLayerHeight; layerHeight += _multipleLayerHeightStep)
                {
                    layerHeights.Add(Layer.RoundHeight(layerHeight));
                }

                return layerHeights.ToArray();
            }
        }

        public decimal ExposureGenManualBottom
        {
            get => _exposureGenManualBottom;
            set => RaiseAndSetIfChanged(ref _exposureGenManualBottom, value);
        }

        public decimal ExposureGenManualNormal
        {
            get => _exposureGenManualNormal;
            set => RaiseAndSetIfChanged(ref _exposureGenManualNormal, value);
        }

        public ExposureItem ExposureManualEntry => new (_exposureGenManualLayerHeight, _exposureGenManualBottom, _exposureGenManualNormal);


        public ObservableCollection<ExposureItem> ExposureTable
        {
            get => _exposureTable;
            set => RaiseAndSetIfChanged(ref _exposureTable, value);
        }

        #endregion

        #region Constructor

        public OperationCalibrateExposureFinder() { }

        public OperationCalibrateExposureFinder(FileFormat slicerFile) : base(slicerFile)
        {
            _layerHeight    = (decimal)slicerFile.LayerHeight;
            _bottomLayers   = slicerFile.BottomLayerCount;
            _bottomExposure = (decimal)slicerFile.BottomExposureTime;
            _normalExposure = (decimal)slicerFile.ExposureTime;
            _mirrorOutput   = slicerFile.MirrorDisplay;
        }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();
            if (SlicerFile.DisplayWidth > 0)
                DisplayWidth = (decimal)SlicerFile.DisplayWidth;
            if (SlicerFile.DisplayHeight > 0)
                DisplayHeight = (decimal)SlicerFile.DisplayHeight;

            if (_exposureGenManualBottom == 0)
                _exposureGenManualBottom = (decimal) SlicerFile.BottomExposureTime;
            if (_exposureGenManualNormal == 0)
                _exposureGenManualNormal = (decimal)SlicerFile.ExposureTime;
            if (_multipleBrightnessGenExposureTime == 0)
            {
                _multipleBrightnessGenExposureTime = (decimal)SlicerFile.ExposureTime;
            }

            if (!SlicerFile.HavePrintParameterPerLayerModifier(FileFormat.PrintParameterModifier.ExposureSeconds))
            {
                _multipleLayerHeight = false;
                _multipleExposures = false;
            }
        }

        #endregion

        #region Enums

        public enum CalibrateExposureFinderShapes : byte
        {
            Square,
            Circle
        }
        public static Array ShapesItems => Enum.GetValues(typeof(CalibrateExposureFinderShapes));

        public enum Measures : byte
        {
            Pixels,
            Millimeters,
        }

        public static Array MeasuresItems => Enum.GetValues(typeof(Measures));

        public enum CalibrateExposureFinderMultipleBrightnessExcludeFrom : byte
        {
            None,
            Bottom,
            BottomAndBase
        }
        public static Array MultipleBrightnessExcludeFromItems => Enum.GetValues(typeof(CalibrateExposureFinderMultipleBrightnessExcludeFrom));

        public enum ExposureGenTypes : byte
        {
            Linear,
            Multiplier
        }

        public static Array ExposureGenTypeItems => Enum.GetValues(typeof(ExposureGenTypes));
        #endregion

        #region Equality

        private bool Equals(OperationCalibrateExposureFinder other)
        {
            return _layerHeight == other._layerHeight && _bottomLayers == other._bottomLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _topBottomMargin == other._topBottomMargin && _leftRightMargin == other._leftRightMargin && _chamferLayers == other._chamferLayers && _erodeBottomIterations == other._erodeBottomIterations && _partMargin == other._partMargin && _enableAntiAliasing == other._enableAntiAliasing && _mirrorOutput == other._mirrorOutput && _baseHeight == other._baseHeight && _featuresHeight == other._featuresHeight && _featuresMargin == other._featuresMargin && _unitOfMeasure == other._unitOfMeasure && _holeDiametersPx == other._holeDiametersPx && _holeDiametersMm == other._holeDiametersMm && _barSpacing == other._barSpacing && _barLength == other._barLength && _barVerticalSplitter == other._barVerticalSplitter && _barThicknessesPx == other._barThicknessesPx && _barThicknessesMm == other._barThicknessesMm && _textFont == other._textFont && _textScale.Equals(other._textScale) && _textThickness == other._textThickness && _text == other._text && _multipleBrightness == other._multipleBrightness && _multipleBrightnessExcludeFrom == other._multipleBrightnessExcludeFrom && _multipleBrightnessValues == other._multipleBrightnessValues && _dontLiftSamePositionedLayers == other._dontLiftSamePositionedLayers && _multipleLayerHeight == other._multipleLayerHeight && _multipleLayerHeightMaximum == other._multipleLayerHeightMaximum && _multipleLayerHeightStep == other._multipleLayerHeightStep && _multipleExposures == other._multipleExposures && _exposureGenType == other._exposureGenType && _exposureGenIgnoreBaseExposure == other._exposureGenIgnoreBaseExposure && _exposureGenBottomStep == other._exposureGenBottomStep && _exposureGenNormalStep == other._exposureGenNormalStep && _exposureGenTests == other._exposureGenTests && Equals(_exposureTable, other._exposureTable) && _holeShape == other._holeShape;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is OperationCalibrateExposureFinder other && Equals(other);
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
            hashCode.Add(_chamferLayers);
            hashCode.Add(_erodeBottomIterations);
            hashCode.Add(_partMargin);
            hashCode.Add(_enableAntiAliasing);
            hashCode.Add(_mirrorOutput);
            hashCode.Add(_baseHeight);
            hashCode.Add(_featuresHeight);
            hashCode.Add(_featuresMargin);
            hashCode.Add((int) _unitOfMeasure);
            hashCode.Add(_holeDiametersPx);
            hashCode.Add(_holeDiametersMm);
            hashCode.Add(_barSpacing);
            hashCode.Add(_barLength);
            hashCode.Add(_barVerticalSplitter);
            hashCode.Add(_barThicknessesPx);
            hashCode.Add(_barThicknessesMm);
            hashCode.Add((int) _textFont);
            hashCode.Add(_textScale);
            hashCode.Add(_textThickness);
            hashCode.Add(_text);
            hashCode.Add(_multipleBrightness);
            hashCode.Add((int) _multipleBrightnessExcludeFrom);
            hashCode.Add(_multipleBrightnessValues);
            hashCode.Add(_multipleLayerHeight);
            hashCode.Add(_multipleLayerHeightMaximum);
            hashCode.Add(_multipleLayerHeightStep);
            hashCode.Add(_dontLiftSamePositionedLayers);
            hashCode.Add(_multipleExposures);
            hashCode.Add((int) _exposureGenType);
            hashCode.Add(_exposureGenIgnoreBaseExposure);
            hashCode.Add(_exposureGenBottomStep);
            hashCode.Add(_exposureGenNormalStep);
            hashCode.Add(_exposureGenTests);
            hashCode.Add(_exposureTable);
            hashCode.Add((int) _holeShape);
            return hashCode.ToHashCode();
        }

        #endregion

        #region Methods

        public void SortExposureTable()
        {
            var list = _exposureTable.ToList();
            list.Sort();
            ExposureTable = new(list);
        }

        public void SanitizeExposureTable()
        {
            List<ExposureItem> list = _exposureTable.ToList().Distinct().ToList();
            list.Sort();
            ExposureTable = new(list);
        }

        public void GenerateExposure()
        {
            var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
            List<ExposureItem> list = new();
            for (decimal layerHeight = _layerHeight;
                layerHeight <= endLayerHeight;
                layerHeight += _multipleLayerHeightStep)
            {
                if(!_exposureGenIgnoreBaseExposure)
                    list.Add(new ExposureItem(layerHeight, _bottomExposure, _normalExposure));
                for (ushort testN = 1; testN <= _exposureGenTests; testN++)
                {
                    decimal bottomExposureTime = 0;
                    decimal exposureTime = 0;

                    switch (_exposureGenType)
                    {
                        case ExposureGenTypes.Linear:
                            bottomExposureTime = _bottomExposure + _exposureGenBottomStep * testN; 
                            exposureTime = _normalExposure + _exposureGenNormalStep * testN; 
                            break;
                        case ExposureGenTypes.Multiplier:
                            bottomExposureTime = _bottomExposure + _bottomExposure * layerHeight * _exposureGenBottomStep * testN;
                            exposureTime = _normalExposure + _normalExposure * layerHeight * _exposureGenNormalStep * testN;
                            break;
                    }

                    ExposureItem item = new(layerHeight, bottomExposureTime, exposureTime);
                    if(list.Contains(item)) continue; // Already on list, skip
                    list.Add(item);
                }
            }

            ExposureTable = new(list);
        }

        public Mat[] GetLayers()
        {
            var holes = Holes;
            if (holes.Length == 0) return null;
            var bars = Bars;
            var textSize = TextSize;

            int featuresMarginX = (int)(Xppmm * _featuresMargin);
            int featuresMarginY = (int)(Yppmm * _featuresMargin);

            int holePanelWidth = featuresMarginX * 2 + holes[^1];
            

            int yMaxSize = Math.Max(Math.Max(GetHolesLength(holes), GetBarsWidth(bars)), textSize.Width);

            int xSize = holePanelWidth * 2;
            int ySize = featuresMarginX * 3 + yMaxSize + TextMarkingSpacing;

            int barLengthPx = (int) (_barLength * Xppmm);
            int barSpacingPx = (int) (_barSpacing * Yppmm);
            int barsPanelWidth = 0;
            if (bars.Length > 0)
            {
                barsPanelWidth = barLengthPx * 2 + _barVerticalSplitter;
                xSize += barsPanelWidth + featuresMarginX;
                
            }

            

            if (!textSize.IsEmpty)
            {
                xSize += textSize.Height + featuresMarginX;
            }

            int positiveSideWidth = xSize - holePanelWidth;

            Rectangle rect = new Rectangle(new Point(1, 1), new Size(xSize, ySize));
            var layers = new Mat[2];
            layers[0] = EmguExtensions.InitMat(rect.Size.Inflate(2));

            CvInvoke.Rectangle(layers[0], rect, EmguExtensions.WhiteByte, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            layers[1] = layers[0].CloneBlank();
            CvInvoke.Rectangle(layers[1], new Rectangle(rect.Size.Width - holePanelWidth, 0, rect.Size.Width, layers[0].Height), EmguExtensions.WhiteByte, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

            // Print holes
            int xPos = 0;
            int yPos = 0;
            for (var layerIndex = 0; layerIndex < layers.Length; layerIndex++)
            {
                var layer = layers[layerIndex];
                yPos = featuresMarginY;
                for (int i = 0; i < holes.Length; i++)
                {
                    var diameter = holes[i];
                    var radius = diameter / 2;
                    xPos = layers[0].Width - holePanelWidth - featuresMarginX;

                    CalibrateExposureFinderShapes effectiveShape = _holeShape == CalibrateExposureFinderShapes.Square || diameter < 6 ?
                        CalibrateExposureFinderShapes.Square : CalibrateExposureFinderShapes.Circle;

                    switch (effectiveShape)
                    {
                        case CalibrateExposureFinderShapes.Square:
                            xPos -= diameter;
                            break;
                        case CalibrateExposureFinderShapes.Circle:
                            xPos -= radius;
                            yPos += radius;
                            break;
                    }


                    // Left side
                    if (layerIndex == 1)
                    {
                        if (diameter == 1)
                        {
                            layer.SetByte(xPos, yPos, 255);
                        }
                        else
                        {
                            switch (effectiveShape)
                            {
                                case CalibrateExposureFinderShapes.Square:
                                    CvInvoke.Rectangle(layers[layerIndex],
                                        new Rectangle(new Point(xPos, yPos), new Size(diameter-1, diameter-1)),
                                        EmguExtensions.WhiteByte, -1,
                                        _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                    break;
                                case CalibrateExposureFinderShapes.Circle:
                                    CvInvoke.Circle(layers[layerIndex],
                                        new Point(xPos, yPos),
                                        radius, EmguExtensions.WhiteByte, -1,
                                        _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                    break;
                            }

                        }
                    }

                    //holeXPos = layers[0].Width - holeXPos;
                    switch (effectiveShape)
                    {
                        case CalibrateExposureFinderShapes.Square:
                            xPos = layers[0].Width - rect.X - featuresMarginX - holes[^1];
                            break;
                        case CalibrateExposureFinderShapes.Circle:
                            xPos = layers[0].Width - rect.X - featuresMarginX - holes[^1] + radius;
                            break;
                    }

                    // Right side
                    if (diameter == 1)
                    {
                        layer.SetByte(xPos, yPos, 0);
                    }
                    else
                    {
                        switch (effectiveShape)
                        {
                            case CalibrateExposureFinderShapes.Square:
                                CvInvoke.Rectangle(layers[layerIndex],
                                    new Rectangle(new Point(xPos, yPos), new Size(diameter-1, diameter-1)),
                                    EmguExtensions.BlackByte, -1,
                                    _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                break;
                            case CalibrateExposureFinderShapes.Circle:
                                CvInvoke.Circle(layers[layerIndex],
                                    new Point(xPos, yPos),
                                    radius, EmguExtensions.BlackByte, -1,
                                    _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                break;
                        }
                    }


                    yPos += featuresMarginY;

                    switch (effectiveShape)
                    {
                        case CalibrateExposureFinderShapes.Square:
                            yPos += diameter;
                            break;
                        case CalibrateExposureFinderShapes.Circle:
                            yPos += radius;
                            break;
                    }
                }
            }

            xPos = featuresMarginX;
            
            // Print Zebra bars
            if (bars.Length > 0)
            {
                yPos = featuresMarginY;
                for (int i = 0; i < bars.Length; i++)
                {
                    // Print positive bottom
                    CvInvoke.Rectangle(layers[1], new Rectangle(xPos, yPos, barLengthPx - 1, barSpacingPx - 1),
                        EmguExtensions.WhiteByte, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    // Print positive top
                    yPos += barSpacingPx;
                    CvInvoke.Rectangle(layers[1], new Rectangle(xPos + barLengthPx + _barVerticalSplitter, yPos, barLengthPx - 1, bars[i] - 1),
                        EmguExtensions.WhiteByte, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    yPos += bars[i];
                }

                // Left over
                CvInvoke.Rectangle(layers[1], new Rectangle(xPos, yPos, barLengthPx - 1, barSpacingPx - 1),
                    EmguExtensions.WhiteByte, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

                xPos += featuresMarginX;
            }

            if (!textSize.IsEmpty)
            {
                CvInvoke.Rotate(layers[1], layers[1], RotateFlags.Rotate90CounterClockwise);
                CvInvoke.PutText(layers[1], _text, new Point(featuresMarginX, layers[1].Height - barsPanelWidth - xPos), _textFont, _textScale, EmguExtensions.WhiteByte, _textThickness, _enableAntiAliasing ? LineType.AntiAlias :  LineType.EightConnected);
                CvInvoke.Rotate(layers[1], layers[1], RotateFlags.Rotate90Clockwise);
            }

            // Print a hardcoded spiral if have space
            if (positiveSideWidth >= 250)
            {
                var mat = layers[0].CloneBlank();
                var matMask = layers[0].CloneBlank();
                xPos = (int) ((layers[0].Width - holePanelWidth) / 1.8);
                yPos = layers[0].Height - featuresMarginY - TextMarkingSpacing / 2;
                byte circleThickness = 5;
                byte radiusStep = 13;
                int count = -1;
                int maxRadius = 0;
                //bool white = true;

                for (int radius = radiusStep;radius <= 100; radius += (radiusStep + count))
                {
                    count++;
                    CvInvoke.Circle(mat, new Point(xPos, yPos), radius, EmguExtensions.WhiteByte, circleThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    maxRadius = radius;
                    /*for (int i = 0; i < 360; i+=90)
                    {
                        CvInvoke.Ellipse(layers[1], new Point(xPos, yPos), new Size(radius, radius), 0, i, i+90, white ? EmguExtensions.WhiteByte : EmguExtensions.BlackByte, 5, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                        white = !white;
                    }
                    white = !white;*/
                }

                CvInvoke.Circle(mat, new Point(xPos, yPos), 5, EmguExtensions.WhiteByte, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                CvInvoke.Circle(matMask, new Point(xPos, yPos), maxRadius+2, EmguExtensions.WhiteByte, -1);

                var matRoi1 = new Mat(mat, new Rectangle(xPos, yPos - maxRadius-1, maxRadius+2, maxRadius+1));
                var matRoi2 = new Mat(mat, new Rectangle(xPos-maxRadius-1, yPos, maxRadius+1, Math.Min(mat.Height- yPos, maxRadius)));

                CvInvoke.BitwiseNot(matRoi1, matRoi1);
                CvInvoke.BitwiseNot(matRoi2, matRoi2);

                CvInvoke.BitwiseAnd(layers[0], mat, layers[1], matMask);

                Point anchor = new Point(-1, -1);
                //CvInvoke.MorphologyEx(layers[1], layers[1], MorphOp.Open, CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3,3), anchor), anchor, 1, BorderType.Reflect101, default);
                
                mat.Dispose();
                matMask.Dispose();
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
            CvInvoke.PutText(thumbnail, "Exposure Time Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
            CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguExtensions.White3Byte, fontThickness);
            CvInvoke.PutText(thumbnail, $"Features: {Holes.Length+Bars.Length}", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.White3Byte, fontThickness);

            return thumbnail;
        }

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            var layers = GetLayers();
            if (layers is null) return false;
            progress.ItemCount = 0;
            SanitizeExposureTable();
            if (layers[0].Width > SlicerFile.ResolutionX || layers[0].Height > SlicerFile.ResolutionY)
            {
                return false;
            }

            List<Layer> newLayers = new();

            Dictionary<ExposureItem, Point> table = new(); 
            var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
            var totalHeight = TotalHeight;
            int sideMarginPx = (int)Math.Floor(_leftRightMargin * Xppmm);
            int topBottomMarginPx = (int)Math.Floor(_topBottomMargin * Yppmm);
            uint layerIndex = 0;
            int currentX = sideMarginPx;
            int currentY = topBottomMarginPx;
            int featuresMarginX = (int)(Xppmm * _featuresMargin);
            int featuresMarginY = (int)(Yppmm * _featuresMargin);

            var holes = Holes;

            var anchor = new Point(-1, -1);
            using var kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3, 3), anchor);

            var brightnesses = MultipleBrightnessValuesArray;

            if (brightnesses.Length == 0 || !_multipleBrightness) brightnesses = new[] {byte.MaxValue};

            void AddLayer(decimal currentHeight, decimal layerHeight, decimal bottomExposure, decimal normalExposure)
            {
                var layerDifference = currentHeight / layerHeight;

                if (!layerDifference.IsInteger()) return; // Not at right height to process with layer height
                                                            //Debug.WriteLine($"{currentHeight} / {layerHeight} = {layerDifference}, Floor={Math.Floor(layerDifference)}");

                using Mat mat = EmguExtensions.InitMat(SlicerFile.Resolution);
                int layerCountOnHeight = (int)Math.Floor(currentHeight / layerHeight);
                bool isBottomLayer = layerCountOnHeight <= _bottomLayers;
                bool isBaseLayer = currentHeight <= _baseHeight;
                ushort microns = (ushort)Math.Floor(layerHeight * 1000);
                Point position;
                bool addSomething = false;

                foreach(var brightness in brightnesses)
                {
                    var bottomExposureTemp = bottomExposure;
                    var normalExposureTemp = normalExposure;
                    ExposureItem key = new(layerHeight, bottomExposure, normalExposure, brightness);

                    if (table.TryGetValue(key, out var pos))
                    {
                        position = pos;
                    }
                    else
                    {
                        if (currentX + layers[0].Width + sideMarginPx > SlicerFile.ResolutionX)
                        {
                            currentX = sideMarginPx;
                            currentY += layers[0].Height + (int)Math.Floor(_partMargin * Yppmm);
                        }

                        if (currentY + layers[0].Height + topBottomMarginPx > SlicerFile.ResolutionY)
                        {
                            break; // Reach the end
                        }

                        position = new Point(currentX, currentY);
                        table.Add(key, new Point(currentX, currentY));

                        currentX += layers[0].Width + (int)Math.Floor(_partMargin * Xppmm);
                    }

                    
                    Mat matRoi = new(mat, new Rectangle(position, layers[0].Size));

                    layers[isBaseLayer ? 0 : 1].CopyTo(matRoi);

                    if (isBottomLayer && _erodeBottomIterations > 0)
                    {
                        CvInvoke.Erode(matRoi, matRoi, kernel, anchor, _erodeBottomIterations, BorderType.Reflect101, default);
                    }

                    if (layerCountOnHeight < _chamferLayers)
                    {
                        CvInvoke.Erode(matRoi, matRoi, kernel, anchor, _chamferLayers - layerCountOnHeight, BorderType.Reflect101, default);
                    }

                    if (_multipleBrightness && brightness < 255)
                    {
                        // normalExposure - 255
                        //       x        - brightness
                        normalExposureTemp = Math.Round(normalExposure * brightness / byte.MaxValue, 2);
                        if (_multipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.None)
                        {
                            bottomExposureTemp = Math.Round(bottomExposure * brightness / byte.MaxValue, 2);
                        }
                    }

                    var textHeightStart = matRoi.Height - featuresMarginY - TextMarkingSpacing;
                    CvInvoke.PutText(matRoi, $"{microns}u", new Point(TextMarkingStartX, textHeightStart), TextMarkingFontFace, TextMarkingScale, EmguExtensions.WhiteByte, TextMarkingThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    CvInvoke.PutText(matRoi, $"{bottomExposureTemp}s", new Point(TextMarkingStartX, textHeightStart + TextMarkingLineBreak), TextMarkingFontFace, TextMarkingScale, EmguExtensions.WhiteByte, TextMarkingThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    CvInvoke.PutText(matRoi, $"{normalExposureTemp}s", new Point(TextMarkingStartX, textHeightStart + TextMarkingLineBreak * 2), TextMarkingFontFace, TextMarkingScale, EmguExtensions.WhiteByte, TextMarkingThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    CvInvoke.PutText(matRoi, $"{microns}u", new Point(matRoi.Width - featuresMarginX * 2 - holes[^1] + TextMarkingStartX, textHeightStart), TextMarkingFontFace, TextMarkingScale, EmguExtensions.BlackByte, TextMarkingThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    CvInvoke.PutText(matRoi, $"{bottomExposureTemp}s", new Point(matRoi.Width - featuresMarginX * 2 - holes[^1] + TextMarkingStartX, textHeightStart + TextMarkingLineBreak), TextMarkingFontFace, TextMarkingScale, EmguExtensions.BlackByte, TextMarkingThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    CvInvoke.PutText(matRoi, $"{normalExposureTemp}s", new Point(matRoi.Width - featuresMarginX * 2 - holes[^1] + TextMarkingStartX, textHeightStart + TextMarkingLineBreak * 2), TextMarkingFontFace, TextMarkingScale, EmguExtensions.BlackByte, TextMarkingThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

                    if(_multipleBrightness)
                    {
                        CvInvoke.PutText(matRoi, brightness.ToString(), new Point(matRoi.Width / 3, 35), TextMarkingFontFace, TextMarkingScale, EmguExtensions.WhiteByte, TextMarkingThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                        if (brightness < 255 &&
                            (_multipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.None ||
                             _multipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.Bottom && !isBottomLayer ||
                             _multipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.BottomAndBase && !isBottomLayer && !isBaseLayer)
                        )
                        {
                            using var pattern = matRoi.CloneBlank();
                            pattern.SetTo(new MCvScalar(brightness));
                            CvInvoke.BitwiseAnd(matRoi, pattern, matRoi, matRoi);
                        }
                    }

                    addSomething = true;
                }

                if (!addSomething) return;

                Layer layer = new(layerIndex++, mat, SlicerFile)
                {
                    PositionZ = (float)currentHeight,
                    ExposureTime = isBottomLayer ? (float)bottomExposure : (float)normalExposure,
                    LiftHeight = isBottomLayer ? SlicerFile.BottomLiftHeight : SlicerFile.LiftHeight,
                    LiftSpeed = isBottomLayer ? SlicerFile.BottomLiftSpeed : SlicerFile.LiftSpeed,
                    RetractSpeed = SlicerFile.RetractSpeed,
                    LightOffDelay = isBottomLayer ? SlicerFile.BottomLightOffDelay : SlicerFile.LightOffDelay,
                    LightPWM = isBottomLayer ? SlicerFile.BottomLightPWM : SlicerFile.LightPWM,
                    IsModified = true
                };
                newLayers.Add(layer);

                progress++;
            }
            
            for (decimal currentHeight = _layerHeight; currentHeight <= totalHeight; currentHeight += 0.01m)
            {
                currentHeight = Layer.RoundHeight(currentHeight);
                for (decimal layerHeight = _layerHeight; layerHeight <= endLayerHeight; layerHeight += _multipleLayerHeightStep)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    layerHeight = Layer.RoundHeight(layerHeight);
                    
                    if (_multipleExposures)
                    {
                        foreach (var exposureItem in _exposureTable)
                        {
                            if (exposureItem.IsValid && exposureItem.LayerHeight == layerHeight)
                            {
                                AddLayer(currentHeight, layerHeight, exposureItem.BottomExposure, exposureItem.Exposure);
                            }
                        }
                    }
                    else
                    {
                        AddLayer(currentHeight, layerHeight, _bottomExposure, _normalExposure);
                    }
                }
            }

            if (SlicerFile.ThumbnailsCount > 0)
                SlicerFile.SetThumbnails(GetThumbnail());

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.LayerHeight = (float)LayerHeight;
                SlicerFile.BottomExposureTime = (float)BottomExposure;
                SlicerFile.ExposureTime = (float)NormalExposure;
                SlicerFile.BottomLayerCount = BottomLayers;
                SlicerFile.LayerManager.Layers = newLayers.ToArray();
            });

            if (_dontLiftSamePositionedLayers)
            {
                SlicerFile.LayerManager.SetNoLiftForSamePositionedLayers();
            }

            if (_mirrorOutput)
            {
                new OperationFlip(SlicerFile){FlipDirection = Enumerations.FlipDirection.Horizontally}.Execute(progress);
            }

            new OperationMove(SlicerFile).Execute(progress);

            return !progress.Token.IsCancellationRequested;
        }

        #endregion
    }
}
