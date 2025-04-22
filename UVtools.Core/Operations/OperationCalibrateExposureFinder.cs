/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed class OperationCalibrateExposureFinder : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Enums
    public enum CalibrateExposureFinderShapes : byte
    {
        Square,
        Circle
    }
    public static Array ShapesItems => Enum.GetValues(typeof(CalibrateExposureFinderShapes));

    public enum CalibrateExposureFinderMeasures : byte
    {
        Pixels,
        Millimeters,
    }

    public static Array MeasuresItems => Enum.GetValues(typeof(CalibrateExposureFinderMeasures));

    public enum CalibrateExposureFinderMultipleBrightnessExcludeFrom : byte
    {
        None,
        Bottom,
        BottomAndBase
    }
    public static Array MultipleBrightnessExcludeFromItems => Enum.GetValues(typeof(CalibrateExposureFinderMultipleBrightnessExcludeFrom));

    public enum CalibrateExposureFinderExposureGenTypes : byte
    {
        Linear,
        Multiplier
    }

    public static Array ExposureGenTypeItems => Enum.GetValues(typeof(CalibrateExposureFinderExposureGenTypes));

    public enum CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes : byte
    {
        [Description("Iterative exposure: Print each base layer at it own exposure time")]
        Iterative,

        [Description("Lowest exposure: Base layers will print at the lowest defined exposure time")]
        UseLowest,

        [Description("Middle exposure: Base layers will print at the middle defined exposure time")]
        UseMiddle,

        [Description("Highest exposure: Base layers will print at the highest defined exposure time")]
        UseHighest,

        [Description("Custom exposure: Base layers will print at a custom defined exposure time")]
        Custom
    }
    #endregion

    #region Subclasses

    public sealed class BullsEyeCircle
    {
        public ushort Diameter { get; set; }
        public ushort Radius => (ushort) (Diameter / 2);
        public ushort Thickness { get; set; } = 10;

        public BullsEyeCircle() {}

        public BullsEyeCircle(ushort diameter, ushort thickness)
        {
            Diameter = diameter;
            Thickness = thickness;
        }
    }
    #endregion

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
    private decimal _layerHeight;
    private ushort _bottomLayers;
    private decimal _bottomExposure;
    private decimal _normalExposure;
    private decimal _topBottomMargin = 5;
    private decimal _leftRightMargin = 10;
    private byte _chamferLayers = 0;
    private byte _erodeBottomIterations = 0;
    private decimal _partMargin = 0;
    private bool _enableAntiAliasing = false;
    private bool _mirrorOutput;
    private decimal _baseHeight = 1;
    private decimal _featuresHeight = 1;
    private decimal _featuresMargin = 2m;
        
    private ushort _staircaseThicknessPx = 40;
    private decimal _staircaseThicknessMm = 2;
        
    private bool _holesEnabled = false;
    private CalibrateExposureFinderShapes _holeShape = CalibrateExposureFinderShapes.Square;
    private CalibrateExposureFinderMeasures _unitOfMeasure = CalibrateExposureFinderMeasures.Pixels;
    private string _holeDiametersPx = "2, 3, 4, 5, 6, 7, 8, 9, 10, 11";
    private string _holeDiametersMm = "0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.2";

    private bool _barsEnabled = true;
    private decimal _barSpacing = 1.5m;
    private decimal _barLength = 4;
    private sbyte _barVerticalSplitter = 0;
    private byte _barFenceThickness = 10;
    private sbyte _barFenceOffset = 4;
    private string _barThicknessesPx = "4, 6, 8, 60"; //"4, 6, 8, 10, 12, 14, 16, 18, 20";
    private string _barThicknessesMm = "0.2, 0.3, 0.4, 3"; //"0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1, 1.2";

    private bool _textEnabled = true;
    private FontFace _textFont = TextMarkingFontFace;
    private double _textScale = 1;
    private byte _textThickness = 2;
    private string _text = "ABHJQRWZ%&#"; //"ABGHJKLMQRSTUVWXZ%&#";

    private bool _multipleBrightness;
    private CalibrateExposureFinderMultipleBrightnessExcludeFrom _multipleBrightnessExcludeFrom = CalibrateExposureFinderMultipleBrightnessExcludeFrom.BottomAndBase;
    private string _multipleBrightnessValues = null!;
    private decimal _multipleBrightnessGenExposureTime;
    private byte _multipleBrightnessGenEmulatedAALevel = FileFormat.MaximumAntiAliasing;
    private byte _multipleBrightnessGenExposureFractions = 8;

    private bool _multipleLayerHeight;
    private decimal _multipleLayerHeightMaximum = 0.1m;
    private decimal _multipleLayerHeightStep = 0.01m;

    private CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes _multipleExposuresBaseLayersPrintMode;
    private decimal _multipleExposuresBaseLayersCustomExposure;
    private bool _differentSettingsForSamePositionedLayers;
    private bool _samePositionedLayersLiftHeightEnabled = true;
    private decimal _samePositionedLayersLiftHeight;
    private bool _samePositionedLayersWaitTimeBeforeCureEnabled = true;
    private decimal _samePositionedLayersWaitTimeBeforeCure;
    private bool _multipleExposures;
    private CalibrateExposureFinderExposureGenTypes _exposureGenType = CalibrateExposureFinderExposureGenTypes.Linear;
    private bool _exposureGenIgnoreBaseExposure;
    private decimal _exposureGenBottomStep = 0;
    private decimal _exposureGenNormalStep = 0.2m;
    private byte _exposureGenTests = 4;
    private decimal _exposureGenManualLayerHeight;
    private decimal _exposureGenManualBottom;
    private decimal _exposureGenManualNormal;
    private RangeObservableCollection<ExposureItem> _exposureTable = [];

    private bool _bullsEyeEnabled = true;
    private string _bullsEyeConfigurationPx = "26:5, 60:10, 116:15, 190:20";
    private string _bullsEyeConfigurationMm = "1.3:0.25, 3:0.5, 5.8:0.75, 9.5:1";
    private bool _bullsEyeInvertQuadrants = true;

    private bool _counterTrianglesEnabled = true;
    private sbyte _counterTrianglesTipOffset = 3;
    private bool _counterTrianglesFence = false;

    private bool _patternModel;
    private byte _bullsEyeFenceThickness = 10;
    private sbyte _bullsEyeFenceOffset;
    private bool _patternModelGlueBottomLayers = true;
    private bool _patternModelTextEnabled = true;

    #endregion

    #region Overrides

    public override bool CanROI => false;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "mdi-timer-cog";
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

    public override string? ValidateInternally()
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

        if (_chamferLayers * _layerHeight > _baseHeight)
        {
            sb.AppendLine("The chamfer can't be higher than the base height, lower the chamfer layer count.");
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
                    sb.AppendLine($"[ME]: The {Layer.ShowHeight(layerHeight)}mm layer height have no set exposure(s).");
            }
        }

        if (_multipleBrightness)
        {
            var brightnessValues = MultipleBrightnessValuesArray;
            if (brightnessValues.Length == 0)
            {
                sb.AppendLine($"Multiple brightness tests are enabled but no valid values are set, use from 1 to 255.");
            }
            else
            {
                if (SlicerFile.IsAntiAliasingEmulated)
                {
                    if (brightnessValues.Length > 16)
                    {
                        sb.AppendLine(
                            "[ME] This format uses time fractions to emulate AntiAliasing, only up 16 levels of greys/brightness are permitted.");
                    }
                    else
                    {
                        /*byte aalevel = brightnessValues.Length switch
                        {
                            <= 2 => 2,
                            <= 4 => 4,
                            <= 8 => 8,
                            <= 16 => 16,
                            _ => 2
                        };*/

                        var increment = 255f / _multipleBrightnessGenEmulatedAALevel;

                        byte[] validAA = new byte[_multipleBrightnessGenEmulatedAALevel];

                        for (byte frac = 0; frac < _multipleBrightnessGenEmulatedAALevel; frac++)
                        {
                            validAA[frac] = (byte)(byte.MaxValue - increment * frac);
                        }

                        string invalidAA = string.Empty;

                        foreach (var brightness in brightnessValues)
                        {
                            if (!validAA.Contains(brightness))
                                invalidAA += $"{brightness}, ";
                        }

                        invalidAA = invalidAA.Trim().TrimEnd(',');

                        if (!string.IsNullOrWhiteSpace(invalidAA))
                        {
                            sb.AppendLine($"[ME] This format uses time fractions to emulate AntiAliasing, only some levels greys/brightness are permitted, and everything outside that is thresholded.");
                            sb.AppendLine($" - your input have the following wrong levels: {invalidAA}");
                            sb.AppendLine($" - AntiAliasing level: {_multipleBrightnessGenEmulatedAALevel} with usable values of: {string.Join(", ", validAA)}");
                        }
                    }
                }
            }
        }

        if (_patternModel)
        {
            if (!CanPatternModel)
            {
                sb.AppendLine($"Unable to pattern the loaded model within the available space.");
            }

            if (!_multipleBrightness && !_multipleExposures)
            {
                sb.AppendLine($"Pattern the loaded model requires either multiple brightness or multiple exposures to use with.");
            }
        }
        else
        {
            if (Bars.Length <= 0 && Holes.Length <= 0 && BullsEyes.Length <= 0 && TextSize.IsEmpty)
            {
                sb.AppendLine("No objects to output, enable at least 1 feature.");
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
                     $"[Holes: {Holes.Length}] [Bars: {Bars.Length}] [BE: {BullsEyes.Length}] [Text: {!string.IsNullOrWhiteSpace(_text)}]" +
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
            if(!RaiseAndSetIfChanged(ref _displayWidth, FileFormat.RoundDisplaySize(value))) return;
            RaisePropertyChanged(nameof(Xppmm));
        }
    }

    public decimal DisplayHeight
    {
        get => _displayHeight;
        set
        {
            if(!RaiseAndSetIfChanged(ref _displayHeight, FileFormat.RoundDisplaySize(value))) return;
            RaisePropertyChanged(nameof(Yppmm));
        }
    }

    public decimal Xppmm => DisplayWidth > 0 ? Math.Round(SlicerFile.ResolutionX / DisplayWidth, 3) : 0;
    public decimal Yppmm => DisplayHeight > 0 ? Math.Round(SlicerFile.ResolutionY / DisplayHeight, 3) : 0;
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

    public ushort StaircaseThicknessPx
    {
        get => _staircaseThicknessPx;
        set => RaiseAndSetIfChanged(ref _staircaseThicknessPx, value);
    }

    public decimal StaircaseThicknessMm
    {
        get => _staircaseThicknessMm;
        set => RaiseAndSetIfChanged(ref _staircaseThicknessMm, value);
    }

    public ushort StaircaseThickness => _unitOfMeasure == CalibrateExposureFinderMeasures.Pixels
        ? _staircaseThicknessPx
        : (ushort)(_staircaseThicknessMm * Yppmm);

    public bool CounterTrianglesEnabled
    {
        get => _counterTrianglesEnabled;
        set => RaiseAndSetIfChanged(ref _counterTrianglesEnabled, value);
    }

    public sbyte CounterTrianglesTipOffset
    {
        get => _counterTrianglesTipOffset;
        set => RaiseAndSetIfChanged(ref _counterTrianglesTipOffset, value);
    }

    public bool CounterTrianglesFence
    {
        get => _counterTrianglesFence;
        set => RaiseAndSetIfChanged(ref _counterTrianglesFence, value);
    }

    public bool HolesEnabled
    {
        get => _holesEnabled;
        set => RaiseAndSetIfChanged(ref _holesEnabled, value);
    }

    public CalibrateExposureFinderShapes HoleShape
    {
        get => _holeShape;
        set => RaiseAndSetIfChanged(ref _holeShape, value);
    }

    public CalibrateExposureFinderMeasures UnitOfMeasure
    {
        get => _unitOfMeasure;
        set
        {
            if (!RaiseAndSetIfChanged(ref _unitOfMeasure, value)) return;
            RaisePropertyChanged(nameof(IsUnitOfMeasureMm));
        }
    }

    public bool IsUnitOfMeasureMm => _unitOfMeasure == CalibrateExposureFinderMeasures.Millimeters;

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
            if (!_holesEnabled)
            {
                return [];
            }

            List<int> holes = [];

            if (_unitOfMeasure == CalibrateExposureFinderMeasures.Millimeters)
            {
                var split = _holeDiametersMm.Split(',', StringSplitOptions.TrimEntries);
                foreach (var mmStr in split)
                {
                    if (string.IsNullOrWhiteSpace(mmStr)) continue;
                    if (!decimal.TryParse(mmStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var mm)) continue;
                    var mmPx = (int)(mm * Ppmm);
                    if (mmPx is <= 0 or > 500) continue;
                    if(holes.Contains(mmPx)) continue;
                    holes.Add(mmPx);
                }
            }
            else
            {
                var split = _holeDiametersPx.Split(',', StringSplitOptions.TrimEntries);
                foreach (var pxStr in split)
                {
                    if (string.IsNullOrWhiteSpace(pxStr)) continue;
                    if (!int.TryParse(pxStr, out var px)) continue;
                    if (px is <= 0 or > 500) continue;
                    if (holes.Contains(px)) continue;
                    holes.Add(px);
                }
            }

            return holes.OrderBy(pixels => pixels).ToArray();
        }
    }

    public int GetHolesHeight(int[] holes)
    {
        if (holes.Length == 0) return 0;
        return (int) (holes.Sum() + (holes.Length-1) * _featuresMargin * Yppmm);
    }

    public bool BarsEnabled
    {
        get => _barsEnabled;
        set => RaiseAndSetIfChanged(ref _barsEnabled, value);
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

    public byte BarFenceThickness
    {
        get => _barFenceThickness;
        set => RaiseAndSetIfChanged(ref _barFenceThickness, value);
    }

    public sbyte BarFenceOffset
    {
        get => _barFenceOffset;
        set => RaiseAndSetIfChanged(ref _barFenceOffset, value);
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
            if (!_barsEnabled)
            {
                return [];
            }

            List<int> bars = [];

            if (_unitOfMeasure == CalibrateExposureFinderMeasures.Millimeters)
            {
                var split = _barThicknessesMm.Split(',', StringSplitOptions.TrimEntries);
                foreach (var mmStr in split)
                {
                    if (string.IsNullOrWhiteSpace(mmStr)) continue;
                    if (!decimal.TryParse(mmStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var mm)) continue;
                    var mmPx = (int)(mm * Yppmm);
                    if (mmPx is <= 0 or > 500) continue;
                    if (bars.Contains(mmPx)) continue;
                    bars.Add(mmPx);
                }
            }
            else
            {
                var split = _barThicknessesPx.Split(',', StringSplitOptions.TrimEntries);
                foreach (var pxStr in split)
                {
                    if (string.IsNullOrWhiteSpace(pxStr)) continue;
                    if (!int.TryParse(pxStr, out var px)) continue;
                    if (px is <= 0 or > 500) continue;
                    if (bars.Contains(px)) continue;
                    bars.Add(px);
                }
            }

            return bars.OrderBy(pixels => pixels).ToArray();
        }
    }

    public int GetBarsLength(int[] bars)
    {
        if (bars.Length == 0) return 0;
        int len = (int) (bars.Sum() + (bars.Length + 1) * _barSpacing * Yppmm);
        if (_barFenceThickness > 0)
        {
            len = Math.Max(len, len + _barFenceThickness * 2 + _barFenceOffset * 2);
        }
        return len;
    }

    public bool TextEnabled
    {
        get => _textEnabled;
        set => RaiseAndSetIfChanged(ref _textEnabled, value);
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
            if (!_textEnabled || string.IsNullOrWhiteSpace(_text)) return Size.Empty;
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

    public byte MaximumAntiAliasing => FileFormat.MaximumAntiAliasing;

    public byte MultipleBrightnessGenEmulatedAALevel
    {
        get => _multipleBrightnessGenEmulatedAALevel;
        set
        {
            if(!RaiseAndSetIfChanged(ref _multipleBrightnessGenEmulatedAALevel, value)) return;
            GenerateBrightnessExposureFractions();
        }
    }

    public byte MultipleBrightnessGenExposureFractions
    {
        get => _multipleBrightnessGenExposureFractions;
        set
        {
            if(!RaiseAndSetIfChanged(ref _multipleBrightnessGenExposureFractions, value)) return;
            GenerateBrightnessExposureFractions();
        }
    }

    /// <summary>
    /// Gets all holes in pixels and ordered
    /// </summary>
    public byte[] MultipleBrightnessValuesArray
    {
        get
        {
            List<byte> values = [];
            if (!string.IsNullOrWhiteSpace(_multipleBrightnessValues))
            {
                var split = _multipleBrightnessValues.Split(',', StringSplitOptions.TrimEntries);
                foreach (var brightnessStr in split)
                {
                    if (string.IsNullOrWhiteSpace(brightnessStr)) continue;
                    if (!byte.TryParse(brightnessStr, out var brightness)) continue;
                    if (brightness is <= 0 or > 255) continue;
                    if (values.Contains(brightness)) continue;
                    values.Add(brightness);
                }
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

    public CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes MultipleExposuresBaseLayersPrintMode
    {
        get => _multipleExposuresBaseLayersPrintMode;
        set
        {
            if(!RaiseAndSetIfChanged(ref _multipleExposuresBaseLayersPrintMode, value)) return;
            RaisePropertyChanged(nameof(IsMultipleExposuresBaseLayersPrintModeCustom));
        }
    }

    public bool IsMultipleExposuresBaseLayersPrintModeCustom => _multipleExposuresBaseLayersPrintMode == CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.Custom;

    public decimal MultipleExposuresBaseLayersCustomExposure
    {
        get => _multipleExposuresBaseLayersCustomExposure;
        set => RaiseAndSetIfChanged(ref _multipleExposuresBaseLayersCustomExposure, value);
    }

    public bool DifferentSettingsForSamePositionedLayers
    {
        get => _differentSettingsForSamePositionedLayers;
        set => RaiseAndSetIfChanged(ref _differentSettingsForSamePositionedLayers, value);
    }

    public bool SamePositionedLayersLiftHeightEnabled
    {
        get => _samePositionedLayersLiftHeightEnabled;
        set => RaiseAndSetIfChanged(ref _samePositionedLayersLiftHeightEnabled, value);
    }

    public decimal SamePositionedLayersLiftHeight
    {
        get => _samePositionedLayersLiftHeight;
        set => RaiseAndSetIfChanged(ref _samePositionedLayersLiftHeight, Math.Round(value, 2));
    }

    public bool SamePositionedLayersWaitTimeBeforeCureEnabled
    {
        get => _samePositionedLayersWaitTimeBeforeCureEnabled;
        set => RaiseAndSetIfChanged(ref _samePositionedLayersWaitTimeBeforeCureEnabled, value);
    }

    public decimal SamePositionedLayersWaitTimeBeforeCure
    {
        get => _samePositionedLayersWaitTimeBeforeCure;
        set => RaiseAndSetIfChanged(ref _samePositionedLayersWaitTimeBeforeCure, Math.Round(value, 2));
    }

    public bool MultipleExposures
    {
        get => _multipleExposures;
        set => RaiseAndSetIfChanged(ref _multipleExposures, value);
    }

    public CalibrateExposureFinderExposureGenTypes ExposureGenType
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
            List<decimal> layerHeights = [];
            var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
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


    public RangeObservableCollection<ExposureItem> ExposureTable
    {
        get => _exposureTable;
        set => RaiseAndSetIfChanged(ref _exposureTable, value);
    }

    public bool BullsEyeEnabled
    {
        get => _bullsEyeEnabled;
        set => RaiseAndSetIfChanged(ref _bullsEyeEnabled, value);
    }

    public string BullsEyeConfigurationPx
    {
        get => _bullsEyeConfigurationPx;
        set => RaiseAndSetIfChanged(ref _bullsEyeConfigurationPx, value);
    }

    public string BullsEyeConfigurationMm
    {
        get => _bullsEyeConfigurationMm;
        set => RaiseAndSetIfChanged(ref _bullsEyeConfigurationMm, value);
    }

    public byte BullsEyeFenceThickness
    {
        get => _bullsEyeFenceThickness;
        set => RaiseAndSetIfChanged(ref _bullsEyeFenceThickness, value);
    }

    public sbyte BullsEyeFenceOffset
    {
        get => _bullsEyeFenceOffset;
        set => RaiseAndSetIfChanged(ref _bullsEyeFenceOffset, value);
    }

    public bool BullsEyeInvertQuadrants
    {
        get => _bullsEyeInvertQuadrants;
        set => RaiseAndSetIfChanged(ref _bullsEyeInvertQuadrants, value);
    }

    /// <summary>
    /// Gets all holes in pixels and ordered
    /// </summary>
    public BullsEyeCircle[] BullsEyes
    {
        get
        {
            if (!_bullsEyeEnabled)
            {
                return [];
            }

            List<BullsEyeCircle> bulleyes = [];
                
            if (_unitOfMeasure == CalibrateExposureFinderMeasures.Millimeters)
            {
                var splitGroup = _bullsEyeConfigurationMm.Split(',', StringSplitOptions.TrimEntries);
                foreach (var group in splitGroup)
                {
                    var splitDiameterThickness = group.Split(':', StringSplitOptions.TrimEntries);
                    if (splitDiameterThickness.Length < 2) continue;

                    if (string.IsNullOrWhiteSpace(splitDiameterThickness[0]) ||
                        string.IsNullOrWhiteSpace(splitDiameterThickness[1])) continue;
                    if (!decimal.TryParse(splitDiameterThickness[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var diameterMm)) continue;
                    if (!decimal.TryParse(splitDiameterThickness[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var thicknessMm)) continue;
                    var diameter = (int)(diameterMm * Ppmm);
                    if (diameterMm is <= 0 or > 500) continue;
                    var thickness = (int)(thicknessMm * Ppmm);
                    if (thickness is <= 0 or > 500) continue;
                    if (bulleyes.Exists(circle => circle.Diameter == diameter)) continue;
                    bulleyes.Add(new BullsEyeCircle((ushort)diameter, (ushort)thickness));
                }
            }
            else
            {
                var splitGroup = _bullsEyeConfigurationPx.Split(',', StringSplitOptions.TrimEntries);
                foreach (var group in splitGroup)
                {
                    var splitDiameterThickness = group.Split(':', StringSplitOptions.TrimEntries);
                    if (splitDiameterThickness.Length < 2) continue;

                    if (string.IsNullOrWhiteSpace(splitDiameterThickness[0]) ||
                        string.IsNullOrWhiteSpace(splitDiameterThickness[1])) continue;
                    if (!int.TryParse(splitDiameterThickness[0], out var diameter)) continue;
                    if (!int.TryParse(splitDiameterThickness[1], out var thickness)) continue;
                    if (diameter is <= 0 or > 500) continue;
                    if (thickness is <= 0 or > 500) continue;
                    if (bulleyes.Exists(circle => circle.Diameter == diameter)) continue;
                    bulleyes.Add(new BullsEyeCircle((ushort) diameter, (ushort) thickness));
                }
            }
                
            return bulleyes.OrderBy(circle => circle.Diameter).DistinctBy(circle => circle.Diameter).ToArray();
        }
    }
    public int GetBullsEyeMaxPanelDiameter(BullsEyeCircle[] bullseyes)
    {
        if (!_bullsEyeEnabled || bullseyes.Length == 0) return 0;
        var diameter = GetBullsEyeMaxDiameter(bullseyes);
        return Math.Max(diameter, diameter + _bullsEyeFenceThickness + _bullsEyeFenceOffset * 2);
    }

    public int GetBullsEyeMaxDiameter(BullsEyeCircle[] bullseyes)
    {
        if (!_bullsEyeEnabled || bullseyes.Length == 0) return 0;
        return bullseyes[^1].Diameter + bullseyes[^1].Thickness / 2;
    }

    public bool PatternModel
    {
        get => _patternModel;
        set
        {
            if(!RaiseAndSetIfChanged(ref _patternModel, value)) return;
            if (_patternModel)
            {
                if(SlicerFile is not null) LayerHeight = (decimal) SlicerFile.LayerHeight;
                MultipleLayerHeight = false;
            }
        }
    }

    public bool PatternModelGlueBottomLayers
    {
        get => _patternModelGlueBottomLayers;
        set => RaiseAndSetIfChanged(ref _patternModelGlueBottomLayers, value);
    }

    public bool PatternModelTextEnabled
    {
        get => _patternModelTextEnabled;
        set => RaiseAndSetIfChanged(ref _patternModelTextEnabled, value);
    }


    public bool CanPatternModel => SlicerFile.BoundingRectangle.Width * 2 + _leftRightMargin * 2 + _partMargin * Xppmm < SlicerFile.ResolutionX ||
                                   SlicerFile.BoundingRectangle.Height * 2 + _topBottomMargin * 2 + _partMargin * Yppmm < SlicerFile.ResolutionY;

    #endregion

    #region Constructor

    public OperationCalibrateExposureFinder() { }

    public OperationCalibrateExposureFinder(FileFormat slicerFile) : base(slicerFile)
    {
        if (SlicerFile.SupportPerLayerSettings)
        {
            _differentSettingsForSamePositionedLayers = true;
            if (SlicerFile.SupportGCode)
            {
                _samePositionedLayersLiftHeight = 0;
                _samePositionedLayersWaitTimeBeforeCure = 0;
            }
            else
            {
                _samePositionedLayersLiftHeight = 0.1m;
                _samePositionedLayersWaitTimeBeforeCure = 1;
            }
        }

    }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
           
        _mirrorOutput = SlicerFile.DisplayMirror != FlipDirection.None;

        if (SlicerFile.DisplayWidth > 0)
            DisplayWidth = (decimal)SlicerFile.DisplayWidth;
        if (SlicerFile.DisplayHeight > 0)
            DisplayHeight = (decimal)SlicerFile.DisplayHeight;

        if(_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if(_bottomLayers <= 0) _bottomLayers = SlicerFile.BottomLayerCount;
        if(_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
        if(_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
            
        if (_exposureGenManualBottom == 0)
            _exposureGenManualBottom = (decimal) SlicerFile.BottomExposureTime;
        if (_exposureGenManualNormal == 0)
            _exposureGenManualNormal = (decimal)SlicerFile.ExposureTime;
        if (_multipleBrightnessGenExposureTime == 0)
            _multipleBrightnessGenExposureTime = (decimal)SlicerFile.ExposureTime;

        if (_multipleExposuresBaseLayersCustomExposure <= 0) _multipleExposuresBaseLayersCustomExposure = (decimal)SlicerFile.ExposureTime;

        if (!SlicerFile.CanUseLayerExposureTime)
        {
            _multipleLayerHeight = false;
            _multipleExposures = false;
        }

        if (string.IsNullOrWhiteSpace(_multipleBrightnessValues))
        {
            _multipleBrightnessValues = 
                SlicerFile.IsAntiAliasingEmulated 
                    ? "255, 239, 223, 207, 191, 175, 159, 143"
                    : "255, 242, 230, 217, 204, 191";
        }
    }

    #endregion

    #region Equality
        
    private bool Equals(OperationCalibrateExposureFinder other)
    {
        return _displayWidth == other._displayWidth && _displayHeight == other._displayHeight && _layerHeight == other._layerHeight && _bottomLayers == other._bottomLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _topBottomMargin == other._topBottomMargin && _leftRightMargin == other._leftRightMargin && _chamferLayers == other._chamferLayers && _erodeBottomIterations == other._erodeBottomIterations && _partMargin == other._partMargin && _enableAntiAliasing == other._enableAntiAliasing && _mirrorOutput == other._mirrorOutput && _baseHeight == other._baseHeight && _featuresHeight == other._featuresHeight && _featuresMargin == other._featuresMargin && _staircaseThicknessPx == other._staircaseThicknessPx && _staircaseThicknessMm == other._staircaseThicknessMm && _holesEnabled == other._holesEnabled && _holeShape == other._holeShape && _unitOfMeasure == other._unitOfMeasure && _holeDiametersPx == other._holeDiametersPx && _holeDiametersMm == other._holeDiametersMm && _barsEnabled == other._barsEnabled && _barSpacing == other._barSpacing && _barLength == other._barLength && _barVerticalSplitter == other._barVerticalSplitter && _barFenceThickness == other._barFenceThickness && _barFenceOffset == other._barFenceOffset && _barThicknessesPx == other._barThicknessesPx && _barThicknessesMm == other._barThicknessesMm && _textEnabled == other._textEnabled && _textFont == other._textFont && _textScale.Equals(other._textScale) && _textThickness == other._textThickness && _text == other._text && _multipleBrightness == other._multipleBrightness && _multipleBrightnessExcludeFrom == other._multipleBrightnessExcludeFrom && _multipleBrightnessValues == other._multipleBrightnessValues && _multipleBrightnessGenExposureTime == other._multipleBrightnessGenExposureTime && _multipleBrightnessGenEmulatedAALevel == other._multipleBrightnessGenEmulatedAALevel && _multipleBrightnessGenExposureFractions == other._multipleBrightnessGenExposureFractions && _multipleLayerHeight == other._multipleLayerHeight && _multipleLayerHeightMaximum == other._multipleLayerHeightMaximum && _multipleLayerHeightStep == other._multipleLayerHeightStep && _multipleExposuresBaseLayersPrintMode == other._multipleExposuresBaseLayersPrintMode && _multipleExposuresBaseLayersCustomExposure == other._multipleExposuresBaseLayersCustomExposure && _differentSettingsForSamePositionedLayers == other._differentSettingsForSamePositionedLayers && _samePositionedLayersLiftHeightEnabled == other._samePositionedLayersLiftHeightEnabled && _samePositionedLayersLiftHeight == other._samePositionedLayersLiftHeight && _samePositionedLayersWaitTimeBeforeCureEnabled == other._samePositionedLayersWaitTimeBeforeCureEnabled && _samePositionedLayersWaitTimeBeforeCure == other._samePositionedLayersWaitTimeBeforeCure && _multipleExposures == other._multipleExposures && _exposureGenType == other._exposureGenType && _exposureGenIgnoreBaseExposure == other._exposureGenIgnoreBaseExposure && _exposureGenBottomStep == other._exposureGenBottomStep && _exposureGenNormalStep == other._exposureGenNormalStep && _exposureGenTests == other._exposureGenTests && _exposureGenManualLayerHeight == other._exposureGenManualLayerHeight && _exposureGenManualBottom == other._exposureGenManualBottom && _exposureGenManualNormal == other._exposureGenManualNormal && Equals(_exposureTable, other._exposureTable) && _bullsEyeEnabled == other._bullsEyeEnabled && _bullsEyeConfigurationPx == other._bullsEyeConfigurationPx && _bullsEyeConfigurationMm == other._bullsEyeConfigurationMm && _bullsEyeInvertQuadrants == other._bullsEyeInvertQuadrants && _counterTrianglesEnabled == other._counterTrianglesEnabled && _counterTrianglesTipOffset == other._counterTrianglesTipOffset && _counterTrianglesFence == other._counterTrianglesFence && _patternModel == other._patternModel && _bullsEyeFenceThickness == other._bullsEyeFenceThickness && _bullsEyeFenceOffset == other._bullsEyeFenceOffset && _patternModelGlueBottomLayers == other._patternModelGlueBottomLayers && _patternModelTextEnabled == other._patternModelTextEnabled;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationCalibrateExposureFinder other && Equals(other);
    }

    #endregion

    #region Methods

    public void SortExposureTable()
    {
        _exposureTable.Sort();
    }

    public void SanitizeExposureTable()
    {
        _exposureTable.ReplaceCollection(GetSanitizedExposureTable());
    }

    public List<ExposureItem> GetSanitizedExposureTable()
    {
        var list = _exposureTable.ToList().Distinct().ToList();
        list.Sort();
        return list;
    }

    public void GenerateBrightnessExposureFractions()
    {
        var fractions = _multipleBrightnessGenExposureFractions;
        var increment = 255f / _multipleBrightnessGenEmulatedAALevel;

        byte[] validAA = new byte[fractions];

        for (byte frac = 0; frac < fractions; frac++)
        {
            validAA[frac] = (byte)(byte.MaxValue - increment * frac);
        }

        MultipleBrightnessValues = string.Join(", ", validAA);
    }

    public void GenerateExposureTable()
    {
        var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
        List<ExposureItem> list = [];
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
                    case CalibrateExposureFinderExposureGenTypes.Linear:
                        bottomExposureTime = _bottomExposure + _exposureGenBottomStep * testN; 
                        exposureTime = _normalExposure + _exposureGenNormalStep * testN; 
                        break;
                    case CalibrateExposureFinderExposureGenTypes.Multiplier:
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

    public Mat[] GetLayers(out Point markingTextPositivePosition, out Point markingTextNegativePosition, bool isPreview = false)
    {
        var holes = Holes;
        var bars = Bars;
        var bulleyes = BullsEyes;
        var textSize = TextSize;

        int baseLine = 0;
        var markingTextSize = EmguExtensions.GetTextSizeExtended("100u\n20.00s\n3.00s",
            _textFont, _textScale, _textThickness, 10, ref baseLine);
        markingTextPositivePosition = Point.Empty;
        markingTextNegativePosition = Point.Empty;

        int featuresMarginX = (int)(Xppmm * _featuresMargin);
        int featuresMarginY = (int)(Yppmm * _featuresMargin);
        ushort startCaseThickness = StaircaseThickness;

        int holePanelWidth = holes.Length > 0 ? featuresMarginX * 2 + holes[^1] : 0;
        if (holePanelWidth > 0)
        {
            holePanelWidth = Math.Max(holePanelWidth, markingTextSize.Width + featuresMarginX * 2);
        }
        
        int holePanelHeight = GetHolesHeight(holes);
        int barsPanelHeight = GetBarsLength(bars);
        int bulleyesDiameter = GetBullsEyeMaxDiameter(bulleyes);
        int bulleyesPanelDiameter = GetBullsEyeMaxPanelDiameter(bulleyes);
        int bulleyesRadius = bulleyesDiameter / 2;
        int yLeftMaxSize = startCaseThickness + featuresMarginY + Math.Max(barsPanelHeight, textSize.Width) + bulleyesPanelDiameter;
        int yRightMaxSize = startCaseThickness + holePanelHeight + markingTextSize.Height + featuresMarginY * 2;
            
        int xSize = featuresMarginX;
        int ySize = TextMarkingSpacing + featuresMarginY;

        if (barsPanelHeight > 0 || textSize.Width > 0)
        {
            yLeftMaxSize += featuresMarginY;
        }

        int barLengthPx = (int) (_barLength * Xppmm);
        int barSpacingPx = (int) (_barSpacing * Yppmm);
        int barsPanelWidth = 0;

        if (bars.Length > 0)
        {
            barsPanelWidth = barLengthPx * 2 + _barVerticalSplitter;
            if (_barFenceThickness > 0)
            {
                barsPanelWidth = Math.Max(barsPanelWidth, barsPanelWidth + _barFenceThickness * 2 + _barFenceOffset * 2);
            }
            xSize += barsPanelWidth + featuresMarginX;
        }

        if (!textSize.IsEmpty)
        {
            xSize += textSize.Height + featuresMarginX;
        }

        int bullseyeYPos = yLeftMaxSize - bulleyesPanelDiameter / 2;
        markingTextPositivePosition.Y = (int)(bullseyeYPos - markingTextSize.Height / 3.5);
        
        if (bulleyes.Length > 0)
        {
            xSize = Math.Max(xSize, markingTextSize.Width + bulleyesPanelDiameter + featuresMarginX * 3);
            yLeftMaxSize += featuresMarginY + 24;
            markingTextPositivePosition.X = featuresMarginX;
        }
        else
        {
            xSize = Math.Max(xSize, markingTextSize.Width + featuresMarginX * 2);
            yLeftMaxSize += featuresMarginY + 24;
            markingTextPositivePosition.X = xSize / 2 - markingTextSize.Width / 2;
        }

        
        int bullseyeXPos = (int)(xSize / 1.5);
            
        if (holePanelWidth > 0)
        {
            xSize += featuresMarginX + holes[^1];
        }
            
        int negativeSideWidth = xSize;
        xSize += holePanelWidth;

        int positiveSideWidth = xSize - holePanelWidth;

        ySize += Math.Max(yLeftMaxSize, yRightMaxSize+10);

        Rectangle rect = new(new Point(0, 0), new Size(xSize, ySize));
        var layers = new Mat[2];
        layers[0] = EmguExtensions.InitMat(rect.Size);

        CvInvoke.Rectangle(layers[0], rect, EmguExtensions.WhiteColor, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
        layers[1] = layers[0].NewZeros();
        if (holes.Length > 0)
        {
            CvInvoke.Rectangle(layers[1],
                new Rectangle(rect.Size.Width - holePanelWidth, 0, rect.Size.Width, layers[0].Height),
                EmguExtensions.WhiteColor, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
        }

            
        int xPos = 0;
        int yPos = 0;

        // Print staircase
        if (isPreview && startCaseThickness > 0)
        {
            CvInvoke.Rectangle(layers[1],
                new Rectangle(0, 0, layers[1].Size.Width-holePanelWidth, startCaseThickness),
                EmguExtensions.WhiteColor, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
        }

        // Print holes
        for (var layerIndex = 0; layerIndex < layers.Length; layerIndex++)
        {
            var layer = layers[layerIndex];
            yPos = featuresMarginY + startCaseThickness;
            for (int i = 0; i < holes.Length; i++)
            {
                var diameter = holes[i];
                var radius = diameter / 2;
                xPos = layers[0].Width - holePanelWidth - featuresMarginX - holes[^1] / 2;

                var effectiveShape = _holeShape == CalibrateExposureFinderShapes.Square || diameter < 6 ?
                    CalibrateExposureFinderShapes.Square : CalibrateExposureFinderShapes.Circle;

                switch (effectiveShape)
                {
                    case CalibrateExposureFinderShapes.Square:
                        xPos -= radius;
                        break;
                    case CalibrateExposureFinderShapes.Circle:
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
                                    EmguExtensions.WhiteColor, -1,
                                    _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                break;
                            case CalibrateExposureFinderShapes.Circle:
                                layers[layerIndex].DrawCircle(new Point(xPos, yPos),
                                    SlicerFile.PixelsToNormalizedPitch(radius), EmguExtensions.WhiteColor, -1,
                                    _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                break;
                        }

                    }
                }

                //holeXPos = layers[0].Width - holeXPos;
                switch (effectiveShape)
                {
                    case CalibrateExposureFinderShapes.Square:
                        //xPos = layers[0].Width - rect.X - featuresMarginX - holes[^1];
                        xPos = layers[0].Width - holePanelWidth / 2 - radius;
                        break;
                    case CalibrateExposureFinderShapes.Circle:
                        xPos = layers[0].Width - holePanelWidth / 2;
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
                                EmguExtensions.BlackColor, -1,
                                _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                            break;
                        case CalibrateExposureFinderShapes.Circle:
                            layers[layerIndex].DrawCircle(new Point(xPos, yPos),
                                SlicerFile.PixelsToNormalizedPitch(radius), EmguExtensions.BlackColor, -1,
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
            int yStartPos = startCaseThickness + featuresMarginY;
            int xStartPos = xPos;
            yPos = yStartPos + _barFenceThickness / 2 + _barFenceOffset;
            xPos += _barFenceThickness / 2 + _barFenceOffset;
            for (int i = 0; i < bars.Length; i++)
            {
                // Print positive bottom
                CvInvoke.Rectangle(layers[1], new Rectangle(xPos, yPos, barLengthPx - 1, barSpacingPx - 1),
                    EmguExtensions.WhiteColor, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                // Print positive top
                yPos += barSpacingPx;
                CvInvoke.Rectangle(layers[1], new Rectangle(xPos + barLengthPx + _barVerticalSplitter, yPos, barLengthPx - 1, bars[i] - 1),
                    EmguExtensions.WhiteColor, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                yPos += bars[i];
            }

            // Left over
            CvInvoke.Rectangle(layers[1], new Rectangle(xPos, yPos, barLengthPx - 1, barSpacingPx - 1),
                EmguExtensions.WhiteColor, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

            yPos += barSpacingPx;

            if (_barFenceThickness > 0)
            {
                CvInvoke.Rectangle(layers[1], new Rectangle(
                        xStartPos - 1, 
                        yStartPos - 1, 
                        barsPanelWidth - _barFenceThickness + 1,
                        yPos - yStartPos + _barFenceThickness / 2 + _barFenceOffset + 1),
                    EmguExtensions.WhiteColor, _barFenceThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

                yPos += _barFenceThickness * 2 + _barFenceOffset * 2;
            }

            xPos += featuresMarginX;
        }

        if (!textSize.IsEmpty)
        {
            CvInvoke.Rotate(layers[1], layers[1], RotateFlags.Rotate90CounterClockwise);
            CvInvoke.PutText(layers[1], _text, new Point(startCaseThickness + featuresMarginY, layers[1].Height - barsPanelWidth - featuresMarginX * (barsPanelWidth > 0 ? 2 : 1)), _textFont, _textScale, EmguExtensions.WhiteColor, _textThickness, _enableAntiAliasing ? LineType.AntiAlias :  LineType.EightConnected);
            CvInvoke.Rotate(layers[1], layers[1], RotateFlags.Rotate90Clockwise);
        }

        // Print bullseye
        if (bulleyes.Length > 0)
        {
            yPos = bullseyeYPos;
            foreach (var circle in bulleyes)
            {
                CvInvoke.Circle(layers[1], new Point(bullseyeXPos, yPos), circle.Radius, EmguExtensions.WhiteColor, circle.Thickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            }

            if (_bullsEyeInvertQuadrants)
            {
                var matRoi1 = new Mat(layers[1], new Rectangle(bullseyeXPos, yPos - bulleyesRadius - 5, bulleyesRadius + 6, bulleyesRadius + 5));
                var matRoi2 = new Mat(layers[1], new Rectangle(bullseyeXPos - bulleyesRadius - 5, yPos, bulleyesRadius + 5, bulleyesRadius + 6));
                //using var mask = matRoi1.CloneBlank();

                //CvInvoke.Circle(mask, new Point(mask.Width / 2, mask.Height / 2), bulleyesRadius, EmguExtensions.WhiteByte, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                //CvInvoke.Circle(mask, new Point(mask.Width / 2, mask.Height / 2), BullsEyes[^1].Radius, EmguExtensions.WhiteByte, BullsEyes[^1].Thickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

                CvInvoke.BitwiseNot(matRoi1, matRoi1);
                CvInvoke.BitwiseNot(matRoi2, matRoi2);
            }

            if (_bullsEyeFenceThickness > 0)
            {
                CvInvoke.Rectangle(layers[1],
                    new Rectangle(
                        new Point(
                            bullseyeXPos - bulleyesRadius - 5 - _bullsEyeFenceOffset - _bullsEyeFenceThickness / 2, 
                            yPos - bulleyesRadius - 5 - _bullsEyeFenceOffset - _bullsEyeFenceThickness / 2), 
                        new Size(
                            bulleyesDiameter + 10 + _bullsEyeFenceOffset*2 + _bullsEyeFenceThickness, 
                            bulleyesDiameter + 10 + _bullsEyeFenceOffset*2 + _bullsEyeFenceThickness)), 
                    EmguExtensions.WhiteColor,
                    _bullsEyeFenceThickness, 
                    _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            }
                

            yPos += bulleyesRadius;
        }

        if (holes.Length > 0)
        {
            markingTextNegativePosition.X = layers[1].Width - holePanelWidth / 2 - markingTextSize.Width / 3;
            markingTextNegativePosition.Y = layers[1].Height - featuresMarginY - markingTextSize.Height;
        }

        if (isPreview)
        {
            layers[1].PutTextExtended($"{Microns}u\n{_bottomExposure}s\n{_normalExposure}s", markingTextPositivePosition,
                _textFont, _textScale, EmguExtensions.WhiteColor, _textThickness, 10, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

            if (holes.Length > 0)
            {
                layers[1].PutTextExtended($"{Microns}u\n{_bottomExposure}s\n{_normalExposure}s", markingTextNegativePosition,
                    _textFont, _textScale, EmguExtensions.BlackColor, _textThickness, 10, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            }
        }

        if (negativeSideWidth >= 200 && _counterTrianglesEnabled)
        {
            xPos = featuresMarginX;
            int triangleHeight = TextMarkingSpacing + 19;
            int triangleWidth = (negativeSideWidth - xPos - featuresMarginX) / 2;
            int triangleWidthQuarter = triangleWidth / 4;

            if (triangleWidth > 5)
            {
                yPos = layers[1].Height - featuresMarginY - triangleHeight + 1;
                int yHalfPos = yPos + triangleHeight / 2;
                int yPosEnd = layers[1].Height - featuresMarginY + 1;

                var triangles = new Point[4][];

                triangles[0] =
                [
                    new(xPos, yPos), // Top Left
                    new(xPos + triangleWidth, yHalfPos), // Middle
                    new(xPos, yPosEnd) // Bottom Left
                ];
                triangles[1] =
                [
                    new(xPos + triangleWidth * 2, yPos), // Top Right
                    new(xPos + triangleWidth, yHalfPos), // Middle
                    new(xPos + triangleWidth * 2, yPosEnd) // Bottom Right
                ];
                triangles[2] =
                [
                    new(xPos + triangleWidth - triangleWidthQuarter, yPos),  // Top Left
                    new(xPos + triangleWidth + triangleWidthQuarter, yPos),  // Top Right
                    new(xPos + triangleWidth, yHalfPos - _counterTrianglesTipOffset) // Middle
                ];
                triangles[3] =
                [
                    new(xPos + triangleWidth - triangleWidthQuarter, yPosEnd),  // Bottom Left
                    new(xPos + triangleWidth + triangleWidthQuarter, yPosEnd),  // Bottom Right
                    new(xPos + triangleWidth, yHalfPos + _counterTrianglesTipOffset) // Middle
                ];

                foreach (var triangle in triangles)
                {
                    using var vec = new VectorOfPoint(triangle);
                    CvInvoke.FillPoly(layers[1], vec, EmguExtensions.WhiteColor,
                        _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                }

                /*byte size = 60;
                var matRoi = new Mat(layers[1], new Rectangle(
                    new Point(xPos + triangleWidth - size / 2, yHalfPos - size / 2),
                    new Size(size, size)));

                CvInvoke.BitwiseNot(matRoi, matRoi);*/
                    

                if (_counterTrianglesFence)
                {
                    byte outlineThickness = 8;
                    //byte outlineThicknessHalf = (byte)(outlineThickness / 2);

                    CvInvoke.Rectangle(layers[1], new Rectangle(
                            new Point(triangles[0][0].X - 0, triangles[0][0].Y - 0),
                            new Size(triangleWidth * 2 + 0, triangleHeight + 0)
                        ), EmguExtensions.WhiteColor, outlineThickness,
                        _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                }
            }
        }
        // Print a hardcoded spiral if have space
        /*if (positiveSideWidth >= 250000)
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
            }

            CvInvoke.Circle(mat, new Point(xPos, yPos), 5, EmguExtensions.WhiteByte, -1, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            CvInvoke.Circle(matMask, new Point(xPos, yPos), maxRadius+2, EmguExtensions.WhiteByte, -1);

            var matRoi1 = new Mat(mat, new Rectangle(xPos, yPos - maxRadius-1, maxRadius+2, maxRadius+1));
            var matRoi2 = new Mat(mat, new Rectangle(xPos-maxRadius-1, yPos, maxRadius+1, Math.Min(mat.Height- yPos, maxRadius)));

            CvInvoke.BitwiseNot(matRoi1, matRoi1);
            CvInvoke.BitwiseNot(matRoi2, matRoi2);

            CvInvoke.BitwiseAnd(layers[0], mat, layers[1], matMask);

            //CvInvoke.MorphologyEx(layers[1], layers[1], MorphOp.Open, CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3,3), EmguExtensions.AnchorCenter), EmguExtensions.AnchorCenter, 1, BorderType.Reflect101, default);
            
            mat.Dispose();
            matMask.Dispose();
        }*/

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
        CvInvoke.PutText(thumbnail, "Exposure Time Cal.", new Point(xSpacing, ySpacing * 2 - 10), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);


        string text = string.Empty;

        if (_multipleLayerHeight)
        {
            text += $"{Microns}um-{(ushort)(_multipleLayerHeightMaximum *1000)}um/{(ushort)(_multipleLayerHeightStep *1000)}um\n";
        }
        else
        {
            text += $"Layer height: {Microns}um\n";
        }

        if (_multipleExposures)
        {
            text += $"{_exposureTable[0].Exposure}s-{_exposureTable[^1].Exposure}s/{_exposureGenNormalStep}s";
            if (!_patternModel)
            {
                text += $"\nObjects: {_exposureTable.Count}";
            }
        }
        else
        {
            text += $"{_bottomExposure}s/{_normalExposure}s";
        }

        if (_patternModel)
        {
            text += "\nPatterned Model";
        }


        thumbnail.PutTextExtended(text, new Point(xSpacing, ySpacing * 3 - 20), fontFace, 0.8, EmguExtensions.WhiteColor, 2, 10);


        /*CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);
        if (_patternModel)
        {
            CvInvoke.PutText(thumbnail, $"Patterned Model", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);
        }
        else
        {
            CvInvoke.PutText(thumbnail, $"Features: {(_staircaseThickness > 0 ? 1 : 0) + Holes.Length + Bars.Length + BullsEyes.Length + (_counterTrianglesEnabled ? 1 : 0)}", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);
        }*/
            

        return thumbnail;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        int sideMarginPx = (int)(_leftRightMargin * Xppmm);
        int topBottomMarginPx = (int)(_topBottomMargin * Yppmm);
        int partMarginXPx = (int)(_partMargin * Xppmm);
        int partMarginYPx = (int)(_partMargin * Yppmm);

        var kernel = EmguExtensions.Kernel3x3Rectangle;

        if (_patternModel)
        {
            ConcurrentBag<Layer> parallelLayers = [];
            Dictionary<ExposureItem, Point> table = new();
            var boundingRectangle = SlicerFile.BoundingRectangle;
            int xHalf = boundingRectangle.Width / 2;
            int yHalf = boundingRectangle.Height / 2;

            var baseLine = 0;
            var markingTextSize = EmguExtensions.GetTextSizeExtended("100u\n20.00s\n3.00s",
                _textFont, _textScale, _textThickness, 10, ref baseLine);

            var brightnesses = MultipleBrightnessValuesArray;
            var multipleExposures = _exposureTable.Where(item => item.IsValid && item.LayerHeight == (decimal) SlicerFile.LayerHeight).ToArray();
            if (brightnesses.Length == 0 || !_multipleBrightness) brightnesses = [byte.MaxValue];
            if (multipleExposures.Length == 0 || !_multipleExposures) multipleExposures = [new ExposureItem((decimal)SlicerFile.LayerHeight, _bottomExposure, _normalExposure)
            ];

            int currentX = sideMarginPx;
            int currentY = topBottomMarginPx;
            Rectangle glueBottomLayerRectangle = new(new Point(currentX, currentY), Size.Empty);
            foreach (var multipleExposure in multipleExposures)
            {
                foreach (var brightness in brightnesses)
                {
                    if (currentX + boundingRectangle.Width + sideMarginPx >= SlicerFile.ResolutionX)
                    {
                        currentX = sideMarginPx;
                        currentY += boundingRectangle.Height + partMarginYPx;
                    }

                    if (currentY + boundingRectangle.Height + topBottomMarginPx >= SlicerFile.ResolutionY) break;

                    var item = multipleExposure.Clone();
                    item.Brightness = brightness;
                    table.Add(item, new Point(currentX, currentY));

                    glueBottomLayerRectangle.Size = new Size(currentX + boundingRectangle.Width, currentY + boundingRectangle.Height);

                    currentX += boundingRectangle.Width + partMarginXPx;
                }
            }

            if (table.Count <= 1) return false;
            ushort microns = SlicerFile.LayerHeightUm;

            var tableGrouped = table.GroupBy(pair => new {pair.Key.LayerHeight, pair.Key.BottomExposure, pair.Key.Exposure}).Distinct();
            SlicerFile.BottomLayerCount = _bottomLayers;
            ushort bottomLayerCount = 0;
            progress.ItemCount = (uint) (SlicerFile.LayerCount * table.Count); 
            Parallel.For(0, SlicerFile.LayerCount, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                var layer = SlicerFile[layerIndex];
                using var mat = layer.LayerMat;
                var matRoi = new Mat(mat, boundingRectangle);
                int layerCountOnHeight = (int)(layer.PositionZ / SlicerFile.LayerHeight);
                bool isBottomLayer = layerCountOnHeight <= _bottomLayers;
                foreach (var group in tableGrouped)
                {
                    var newLayer = layer.Clone();
                    newLayer.ExposureTime = (float)(newLayer.IsBottomLayer ? group.Key.BottomExposure : group.Key.Exposure);
                    using var newMat = mat.NewZeros();
                    foreach (var brightness in brightnesses)
                    {
                        ExposureItem item = new(group.Key.LayerHeight, group.Key.BottomExposure, group.Key.Exposure, brightness);
                        if(!table.TryGetValue(item, out var point)) continue;
                            
                        var newMatRoi = new Mat(newMat, new Rectangle(point, matRoi.Size));
                        matRoi.CopyTo(newMatRoi);

                        if (layer.IsBottomLayer)
                        {
                            if (_patternModelGlueBottomLayers)
                            {
                                newMatRoi.SetTo(EmguExtensions.WhiteColor);
                            }
                        }

                        if (layerCountOnHeight < _chamferLayers)
                        {
                            CvInvoke.Erode(newMatRoi, newMatRoi, kernel, EmguExtensions.AnchorCenter, _chamferLayers - layerCountOnHeight, BorderType.Reflect101, default);
                        }

                        if (layer.IsBottomLayer)
                        {
                            if (_erodeBottomIterations > 0)
                            {
                                CvInvoke.Erode(newMatRoi, newMatRoi, kernel, EmguExtensions.AnchorCenter, _erodeBottomIterations, BorderType.Reflect101, default);
                            }

                            if(_patternModelTextEnabled)
                            {
                                newMatRoi.PutTextExtended((_multipleBrightness ? $"{brightness.ToString()}\n" : string.Empty) 
                                                          + $"{microns}u\n{group.Key.BottomExposure}s\n{group.Key.Exposure}s", new Point(xHalf - markingTextSize.Width / 2, yHalf - markingTextSize.Height / 2),
                                    _textFont, _textScale, EmguExtensions.BlackColor, _textThickness, 10, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                            }
                        }

                        if (brightness < 255)
                        {
                            if (_multipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.None ||
                                _multipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.Bottom && !layer.IsBottomLayer ||
                                _multipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.BottomAndBase && !layer.IsBottomLayer)
                            {
                                using var pattern = matRoi.New();
                                pattern.SetTo(new MCvScalar(byte.MaxValue - brightness));
                                CvInvoke.Subtract(newMatRoi, pattern, newMatRoi);
                            }
                        }
                    }

                    newLayer.LayerMat = newMat;
                    parallelLayers.Add(newLayer);
                    progress.LockAndIncrement();
                }
            });

            if (parallelLayers.IsEmpty) return false;
            var layers = parallelLayers.OrderBy(layer => layer.PositionZ).ThenBy(layer => layer.ExposureTime).ToList();

            progress.ResetNameAndProcessed("Optimized layers");

            Layer currentLayer = layers[0];
            if (currentLayer.IsBottomLayerByHeight) bottomLayerCount++;
            for (var layerIndex = 1; layerIndex < layers.Count; layerIndex++)
            {
                progress.PauseOrCancelIfRequested();
                progress++;
                var layer = layers[layerIndex];
                if (currentLayer.PositionZ != layer.PositionZ ||
                    currentLayer.ExposureTime != layer.ExposureTime) // Different layers, cache and continue
                {
                    currentLayer = layer;
                    if (currentLayer.IsBottomLayerByHeight) bottomLayerCount++;
                    continue;
                }

                using var matCurrent = currentLayer.LayerMat;
                using var mat = layer.LayerMat;

                CvInvoke.Add(matCurrent, mat, matCurrent); // Sum layers
                currentLayer.LayerMat = matCurrent;

                layers[layerIndex] = null!; // Discard
            }

            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            layers.RemoveAll(layer => layer is null); // Discard equal layers

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.BottomLayerCount = bottomLayerCount;
                SlicerFile.BottomExposureTime = (float)BottomExposure;
                SlicerFile.ExposureTime = (float)NormalExposure;
                SlicerFile.Layers = layers.ToArray();
            });
        }
        else // No patterned
        {
            var layers = GetLayers(out var markingTextPositivePosition, out var markingTextNegativePosition);
            progress.ItemCount = 0;
            //SanitizeExposureTable();
            if (layers[0].Width+sideMarginPx > SlicerFile.ResolutionX || layers[0].Height+topBottomMarginPx > SlicerFile.ResolutionY)
            {
                throw new InvalidOperationException("The used configuration can not produce a test due insufficient space.\n" +
                                                    "Try to adjust sides and/or top/bottom margins to gain space or object features to shorten the test size.");
                //return false;
            }

            List<Layer> newLayers = [];

            Dictionary<ExposureItem, Point> table = new();
            var endLayerHeight = _multipleLayerHeight ? _multipleLayerHeightMaximum : _layerHeight;
            var totalHeight = TotalHeight;
            uint layerIndex = 0;
            int currentX = sideMarginPx;
            int currentY = topBottomMarginPx;
            int featuresMarginX = (int)(Xppmm * _featuresMargin);
            int featuresMarginY = (int)(Yppmm * _featuresMargin);
            ushort startCaseThickness = StaircaseThickness;

            var holes = Holes;
            int holePanelWidth = holes.Length > 0 ? featuresMarginX * 2 + holes[^1] : 0;
            int staircaseWidth = layers[0].Width - holePanelWidth;

            var brightnesses = MultipleBrightnessValuesArray;
            if (brightnesses.Length == 0 || !_multipleBrightness) brightnesses = [byte.MaxValue];

            ExposureItem? lastExposureItem = null;
            decimal lastCurrentHeight = 0;

            ushort bottomLayerCount = 0;

            void AddLayer(decimal currentHeight, decimal layerHeight, decimal bottomExposure, decimal normalExposure)
            {
                var layerDifference = currentHeight / layerHeight;

                if (!layerDifference.IsInteger()) return; // Not at right height to process with layer height
                //Debug.WriteLine($"{currentHeight} / {layerHeight} = {layerDifference}, Floor={Math.Floor(layerDifference)}");

                int firstFeatureLayer = (int)(_baseHeight / layerHeight);
                int lastLayer = (int)((_baseHeight + _featuresHeight) / layerHeight);
                int layerCountOnHeight = (int)(currentHeight / layerHeight);
                bool isBottomLayer = layerCountOnHeight <= _bottomLayers;
                bool isBaseLayer = currentHeight <= _baseHeight;
                ushort microns = (ushort)(layerHeight * 1000);
                bool addSomething = false;

                bool reUseLastLayer =
                    lastExposureItem is not null &&
                    lastCurrentHeight == currentHeight &&
                    lastExposureItem.LayerHeight == layerHeight &&
                    (
                        ((isBottomLayer && lastExposureItem.BottomExposure == bottomExposure) || (!isBottomLayer && lastExposureItem.Exposure == normalExposure)) ||
                        (!isBottomLayer && isBaseLayer && _multipleExposuresBaseLayersPrintMode != CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.Iterative)
                    );

                using var mat = reUseLastLayer ? newLayers[^1].LayerMat : EmguExtensions.InitMat(SlicerFile.Resolution);

                lastCurrentHeight = currentHeight;

                foreach (var brightness in brightnesses)
                {
                    var bottomExposureTemp = bottomExposure;
                    var normalExposureTemp = normalExposure;
                    ExposureItem key = new(layerHeight, bottomExposure, normalExposure, brightness);
                    lastExposureItem = key;

                    Point position;
                    if (table.TryGetValue(key, out var pos))
                    {
                        position = pos;
                    }
                    else
                    {
                        if (currentX + layers[0].Width + sideMarginPx > SlicerFile.ResolutionX)
                        {
                            currentX = sideMarginPx;
                            currentY += layers[0].Height + partMarginYPx;
                        }

                        if (currentY + layers[0].Height + topBottomMarginPx > SlicerFile.ResolutionY)
                        {
                            break; // Reach the end
                        }

                        position = new Point(currentX, currentY);
                        table.Add(key, new Point(currentX, currentY));

                        currentX += layers[0].Width + partMarginXPx;
                    }


                    Mat matRoi = new(mat, new Rectangle(position, layers[0].Size));

                    layers[isBaseLayer ? 0 : 1].CopyTo(matRoi);

                    if (!isBaseLayer && startCaseThickness > 0)
                    {
                        int staircaseWidthIncrement = (int) Math.Ceiling(staircaseWidth / (_featuresHeight / layerHeight-1));
                        int staircaseLayer = layerCountOnHeight - firstFeatureLayer - 1;
                        int staircaseWidthForLayer = staircaseWidth - staircaseWidthIncrement * staircaseLayer;
                        if (staircaseWidthForLayer >= 0 && layerCountOnHeight != lastLayer)
                        {
                            CvInvoke.Rectangle(matRoi,
                                new Rectangle(staircaseWidth - staircaseWidthForLayer, 0, staircaseWidthForLayer, startCaseThickness),
                                EmguExtensions.WhiteColor, -1,
                                _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                        }
                    }

                    if (isBottomLayer && _erodeBottomIterations > 0)
                    {
                        CvInvoke.Erode(matRoi, matRoi, kernel, EmguExtensions.AnchorCenter, _erodeBottomIterations, BorderType.Reflect101, default);
                    }

                    if (layerCountOnHeight < _chamferLayers)
                    {
                        CvInvoke.Erode(matRoi, matRoi, kernel, EmguExtensions.AnchorCenter, _chamferLayers - layerCountOnHeight, BorderType.Reflect101, default);
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

                    matRoi.PutTextExtended($"{microns}u\n{bottomExposureTemp}s\n{normalExposureTemp}s", markingTextPositivePosition,
                        _textFont, _textScale, EmguExtensions.WhiteColor, _textThickness, 10, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    if (holes.Length > 0)
                    {
                        matRoi.PutTextExtended($"{microns}u\n{bottomExposureTemp}s\n{normalExposureTemp}s", markingTextNegativePosition,
                            _textFont, _textScale, EmguExtensions.BlackColor, _textThickness, 10, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    }

                    
                    if (_multipleBrightness)
                    {
                        CvInvoke.PutText(matRoi, brightness.ToString(), new Point(matRoi.Width / 3, 35), TextMarkingFontFace, TextMarkingScale, EmguExtensions.WhiteColor, TextMarkingThickness, _enableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                        if (brightness < 255 &&
                            (_multipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.None ||
                             _multipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.Bottom && !isBottomLayer ||
                             _multipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.BottomAndBase && !isBottomLayer && !isBaseLayer)
                           )
                        {
                            using var pattern = matRoi.New();
                            //pattern.SetTo(new MCvScalar(brightness)); OLD
                            //CvInvoke.BitwiseAnd(matRoi, pattern, matRoi, matRoi); OLD

                            pattern.SetTo(new MCvScalar(byte.MaxValue - brightness));
                            CvInvoke.Subtract(matRoi, pattern, matRoi);
                        }
                    }

                    addSomething = true;
                }

                if (!addSomething) return;

                if (reUseLastLayer)
                {
                    var layer = newLayers[^1];
                    layer.LayerMat = mat;

                    if (!isBottomLayer && isBaseLayer)
                    {
                        layer.ExposureTime = _multipleExposuresBaseLayersPrintMode switch
                        {
                            CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.Iterative => (float)normalExposure,
                            CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.UseLowest => (float) _exposureTable[0].Exposure,
                            CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.UseMiddle => (float) _exposureTable[(int) Math.Ceiling((_exposureTable.Count - 1) / 2.0)].Exposure,
                            CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.UseHighest => (float) _exposureTable[^1].Exposure,
                            CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.Custom => (float) _multipleExposuresBaseLayersCustomExposure,
                            _ => throw new ArgumentOutOfRangeException($"Unhandled type for {_multipleExposuresBaseLayersPrintMode}")
                        };
                    }
                }
                else
                {
                    var layer = new Layer(layerIndex++, mat, SlicerFile)
                    {
                        PositionZ = (float)currentHeight,
                        ExposureTime = isBottomLayer ? (float)bottomExposure : (float)normalExposure,
                        IsModified = true
                    };
                    newLayers.Add(layer);

                    if (isBottomLayer) bottomLayerCount++;
                }


                progress++;
            }

            for (decimal currentHeight = _layerHeight; currentHeight <= totalHeight; currentHeight += Layer.HeightPrecisionIncrement)
            {
                currentHeight = Layer.RoundHeight(currentHeight);
                for (decimal layerHeight = _layerHeight; layerHeight <= endLayerHeight; layerHeight += _multipleLayerHeightStep)
                {
                    progress.PauseOrCancelIfRequested();
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

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.LayerHeight = (float)LayerHeight;
                SlicerFile.BottomExposureTime = (float)BottomExposure;
                SlicerFile.ExposureTime = (float)NormalExposure;
                SlicerFile.BottomLayerCount = bottomLayerCount;
                SlicerFile.TransitionLayerCount = 0;
                SlicerFile.Layers = newLayers.ToArray();
            });

            if (_mirrorOutput)
            {
                var flip = SlicerFile.DisplayMirror;
                if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
                new OperationFlip(SlicerFile) { FlipDirection = (FlipType)flip }.Execute(progress);
            }
        }

        if (_multipleBrightness && SlicerFile.IsAntiAliasingEmulated)
        {
            /*SlicerFile.AntiAliasing = MultipleBrightnessValuesArray.Length switch
            {
                <= 2 => 2,
                <= 4 => 4,
                <= 8 => 8,
                <= 16 => 16,
                _ => 16
            };*/
            SlicerFile.AntiAliasing = _multipleBrightnessGenEmulatedAALevel;
        }

        if (SlicerFile.ThumbnailsCount > 0)
        {
            using var thumbnail = GetThumbnail();
            SlicerFile.SetThumbnails(thumbnail);
        }

        if (_differentSettingsForSamePositionedLayers)
        {
            var layers = SlicerFile.SamePositionedLayers;
            foreach (var layer in layers)
            {
                if(_samePositionedLayersLiftHeightEnabled)    layer.LiftHeightTotal = (float) _samePositionedLayersLiftHeight;
                if(_samePositionedLayersWaitTimeBeforeCureEnabled) layer.SetWaitTimeBeforeCureOrLightOffDelay((float) _samePositionedLayersWaitTimeBeforeCure);
            }
        }

        SlicerFile.WaitTimeAfterCure = 0;
        SlicerFile.WaitTimeAfterLift = 0;

        new OperationMove(SlicerFile).Execute(progress);

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}