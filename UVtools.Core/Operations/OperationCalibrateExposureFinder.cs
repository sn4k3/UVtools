/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
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
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using ZLinq;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public sealed partial class OperationCalibrateExposureFinder : Operation
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
    private decimal _bottomExposure;
    private decimal _normalExposure;
    private decimal _topBottomMargin = 5;
    private decimal _leftRightMargin = 10;
    private decimal _partMargin = 0;
    private decimal _baseHeight = 1;
    private decimal _featuresHeight = 1;
    private decimal _featuresMargin = 2m;




    private double _textScale = 1;



    private decimal _samePositionedLayersLiftHeight;
    private decimal _samePositionedLayersWaitTimeBeforeCure;
    private decimal _exposureGenBottomStep = 0;
    private decimal _exposureGenNormalStep = 0.2m;




    #endregion

    #region Overrides

    public override bool CanROI => false;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "TimerCog";
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

        if (ChamferLayers * _layerHeight > _baseHeight)
        {
            sb.AppendLine("The chamfer can't be higher than the base height, lower the chamfer layer count.");
        }

        if (MultipleExposures)
        {
            var endLayerHeight = MultipleLayerHeight ? MultipleLayerHeightMaximum : _layerHeight;
            for (decimal layerHeight = _layerHeight;
                 layerHeight <= endLayerHeight;
                 layerHeight += MultipleLayerHeightStep)
            {
                bool found = false;
                foreach (var exposureItem in ExposureTable)
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

        if (MultipleBrightness)
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

                        var increment = 255f / MultipleBrightnessGenEmulatedAALevel;

                        byte[] validAA = GC.AllocateUninitializedArray<byte>(MultipleBrightnessGenEmulatedAALevel);

                        for (byte frac = 0; frac < MultipleBrightnessGenEmulatedAALevel; frac++)
                        {
                            validAA[frac] = (byte)(byte.MaxValue - increment * frac);
                        }

                        string invalidAA = string.Empty;

                        foreach (var brightness in brightnessValues)
                        {
                            if (!validAA.AsValueEnumerable().Contains(brightness))
                                invalidAA += $"{brightness}, ";
                        }

                        invalidAA = invalidAA.Trim().TrimEnd(',');

                        if (!string.IsNullOrWhiteSpace(invalidAA))
                        {
                            sb.AppendLine($"[ME] This format uses time fractions to emulate AntiAliasing, only some levels greys/brightness are permitted, and everything outside that is thresholded.");
                            sb.AppendLine($" - your input have the following wrong levels: {invalidAA}");
                            sb.AppendLine($" - AntiAliasing level: {MultipleBrightnessGenEmulatedAALevel} with usable values of: {string.Join(", ", validAA)}");
                        }
                    }
                }
            }
        }

        if (PatternModel)
        {
            if (!CanPatternModel)
            {
                sb.AppendLine($"Unable to pattern the loaded model within the available space.");
            }

            if (!MultipleBrightness && !MultipleExposures)
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
                     $"[Bottom layers: {BottomLayers}] " +
                     $"[Exposure: {_bottomExposure}/{_normalExposure}] " +
                     $"[TB:{_topBottomMargin} LR:{_leftRightMargin} PM:{_partMargin} FM:{_featuresMargin}]  " +
                     $"[Chamfer: {ChamferLayers}] [Erode: {ErodeBottomIterations}] " +
                     $"[Obj height: {_featuresHeight}] " +
                     $"[Holes: {Holes.Length}] [Bars: {Bars.Length}] [BE: {BullsEyes.Length}] [Text: {!string.IsNullOrWhiteSpace(Text)}]" +
                     $"[AA: {EnableAntiAliasing}] [Mirror: {MirrorOutput}]";
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
            if(!SetProperty(ref _displayWidth, FileFormat.RoundDisplaySize(value))) return;
            OnPropertyChanged(nameof(Xppmm));
        }
    }

    public decimal DisplayHeight
    {
        get => _displayHeight;
        set
        {
            if(!SetProperty(ref _displayHeight, FileFormat.RoundDisplaySize(value))) return;
            OnPropertyChanged(nameof(Yppmm));
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
            if(!SetProperty(ref _layerHeight, Layer.RoundHeight(value))) return;
            OnPropertyChanged(nameof(BottomLayersMM));
            OnPropertyChanged(nameof(AvailableLayerHeights));
        }
    }

    public ushort Microns => (ushort)(LayerHeight * 1000);

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BottomLayersMM))]
    public partial ushort BottomLayers { get; set; }

    public decimal BottomLayersMM => Layer.RoundHeight(LayerHeight * BottomLayers);

    public decimal BottomExposure
    {
        get => _bottomExposure;
        set
        {
            if(!SetProperty(ref _bottomExposure, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(MultipleBrightnessTable));
        }
    }

    public decimal NormalExposure
    {
        get => _normalExposure;
        set
        {
            if(!SetProperty(ref _normalExposure, Math.Round(value, 2))) return;
            OnPropertyChanged(nameof(MultipleBrightnessTable));
        }
    }

    public decimal TopBottomMargin
    {
        get => _topBottomMargin;
        set => SetProperty(ref _topBottomMargin, Math.Round(value, 2));
    }

    public decimal LeftRightMargin
    {
        get => _leftRightMargin;
        set => SetProperty(ref _leftRightMargin, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial byte ChamferLayers { get; set; } = 0;

    [ObservableProperty]
    public partial byte ErodeBottomIterations { get; set; } = 0;

    public decimal PartMargin
    {
        get => _partMargin;
        set => SetProperty(ref _partMargin, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial bool EnableAntiAliasing { get; set; } = false;

    [ObservableProperty]
    public partial bool MirrorOutput { get; set; }

    public decimal BaseHeight
    {
        get => _baseHeight;
        set => SetProperty(ref _baseHeight, Math.Round(value, 2));
    }

    public decimal FeaturesHeight
    {
        get => _featuresHeight;
        set => SetProperty(ref _featuresHeight, Math.Round(value, 2));
    }

    public decimal TotalHeight => _baseHeight + _featuresHeight;

    public decimal FeaturesMargin
    {
        get => _featuresMargin;
        set => SetProperty(ref _featuresMargin, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial ushort StaircaseThicknessPx { get; set; } = 40;

    [ObservableProperty]
    public partial decimal StaircaseThicknessMm { get; set; } = 2;

    public ushort StaircaseThickness => UnitOfMeasure == CalibrateExposureFinderMeasures.Pixels
        ? StaircaseThicknessPx
        : (ushort)(StaircaseThicknessMm * Yppmm);

    [ObservableProperty]
    public partial bool CounterTrianglesEnabled { get; set; } = true;

    [ObservableProperty]
    public partial sbyte CounterTrianglesTipOffset { get; set; } = 3;

    [ObservableProperty]
    public partial bool CounterTrianglesFence { get; set; } = false;

    [ObservableProperty]
    public partial bool HolesEnabled { get; set; } = false;

    [ObservableProperty]
    public partial CalibrateExposureFinderShapes HoleShape { get; set; } = CalibrateExposureFinderShapes.Square;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsUnitOfMeasureMm))]
    public partial CalibrateExposureFinderMeasures UnitOfMeasure { get; set; } = CalibrateExposureFinderMeasures.Pixels;

    public bool IsUnitOfMeasureMm => UnitOfMeasure == CalibrateExposureFinderMeasures.Millimeters;

    [ObservableProperty]
    public partial string HoleDiametersMm { get; set; } = "0.2, 0.3, 0.4, 0.5, 0.6, 0.7, 0.8, 0.9, 1.0, 1.2";

    [ObservableProperty]
    public partial string HoleDiametersPx { get; set; } = "2, 3, 4, 5, 6, 7, 8, 9, 10, 11";

    /// <summary>
    /// Gets all holes in pixels and ordered
    /// </summary>
    public int[] Holes
    {
        get
        {
            if (!HolesEnabled)
            {
                return [];
            }

            List<int> holes = [];

            if (UnitOfMeasure == CalibrateExposureFinderMeasures.Millimeters)
            {
                var split = HoleDiametersMm.Split(',', StringSplitOptions.TrimEntries);
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
                var split = HoleDiametersPx.Split(',', StringSplitOptions.TrimEntries);
                foreach (var pxStr in split)
                {
                    if (string.IsNullOrWhiteSpace(pxStr)) continue;
                    if (!int.TryParse(pxStr, out var px)) continue;
                    if (px is <= 0 or > 500) continue;
                    if (holes.Contains(px)) continue;
                    holes.Add(px);
                }
            }

            return holes.AsValueEnumerable().OrderBy(pixels => pixels).ToArray();
        }
    }

    public int GetHolesHeight(int[] holes)
    {
        if (holes.Length == 0) return 0;
        return (int) (holes.AsValueEnumerable().Sum() + (holes.Length-1) * _featuresMargin * Yppmm);
    }

    [ObservableProperty]
    public partial bool BarsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial decimal BarSpacing { get; set; } = 1.5m;

    [ObservableProperty]
    public partial decimal BarLength { get; set; } = 4;

    [ObservableProperty]
    public partial sbyte BarVerticalSplitter { get; set; } = 0;

    [ObservableProperty]
    public partial byte BarFenceThickness { get; set; } = 10;

    [ObservableProperty]
    public partial sbyte BarFenceOffset { get; set; } = 4;

    [ObservableProperty]
    public partial string BarThicknessesPx { get; set; } = "4, 6, 8, 60";

    [ObservableProperty]
    public partial string BarThicknessesMm { get; set; } = "0.2, 0.3, 0.4, 3";

    /// <summary>
    /// Gets all holes in pixels and ordered
    /// </summary>
    public int[] Bars
    {
        get
        {
            if (!BarsEnabled)
            {
                return [];
            }

            List<int> bars = [];

            if (UnitOfMeasure == CalibrateExposureFinderMeasures.Millimeters)
            {
                var split = BarThicknessesMm.Split(',', StringSplitOptions.TrimEntries);
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
                var split = BarThicknessesPx.Split(',', StringSplitOptions.TrimEntries);
                foreach (var pxStr in split)
                {
                    if (string.IsNullOrWhiteSpace(pxStr)) continue;
                    if (!int.TryParse(pxStr, out var px)) continue;
                    if (px is <= 0 or > 500) continue;
                    if (bars.Contains(px)) continue;
                    bars.Add(px);
                }
            }

            return bars.AsValueEnumerable().OrderBy(pixels => pixels).ToArray();
        }
    }

    public int GetBarsLength(int[] bars)
    {
        if (bars.Length == 0) return 0;
        int len = (int) (bars.AsValueEnumerable().Sum() + (bars.Length + 1) * BarSpacing * Yppmm);
        if (BarFenceThickness > 0)
        {
            len = Math.Max(len, len + BarFenceThickness * 2 + BarFenceOffset * 2);
        }
        return len;
    }

    [ObservableProperty]
    public partial bool TextEnabled { get; set; } = true;

    public static Array TextFonts => Enum.GetValues(typeof(FontFace));

    [ObservableProperty]
    public partial FontFace TextFont { get; set; } = TextMarkingFontFace;

    public double TextScale
    {
        get => _textScale;
        set => SetProperty(ref _textScale, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial byte TextThickness { get; set; } = 2;

    [ObservableProperty]
    public partial string Text { get; set; } = "ABHJQRWZ%&#";

    public Size TextSize
    {
        get
        {
            if (!TextEnabled || string.IsNullOrWhiteSpace(Text)) return Size.Empty;
            int baseline = 0;
            return CvInvoke.GetTextSize(Text, TextFont, _textScale, TextThickness, ref baseline);
        }
    }

    [ObservableProperty]
    public partial bool MultipleBrightness { get; set; }

    [ObservableProperty]
    public partial CalibrateExposureFinderMultipleBrightnessExcludeFrom MultipleBrightnessExcludeFrom { get; set; } = CalibrateExposureFinderMultipleBrightnessExcludeFrom.BottomAndBase;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MultipleBrightnessTable))]
    public partial string MultipleBrightnessValues { get; set; } = null!;

    public List<ExposureItem> MultipleBrightnessTable
    {
        get
        {
            var brightnesses = MultipleBrightnessValuesArray;
            return brightnesses.AsValueEnumerable().Select(brightness => (ExposureItem)
                new(
                    _layerHeight,
                    Math.Round(brightness * _bottomExposure / byte.MaxValue, 2),
                    Math.Round(brightness * _normalExposure / byte.MaxValue, 2),
                    brightness)).ToList();
        }
    }

    [ObservableProperty]
    public partial decimal MultipleBrightnessGenExposureTime { get; set; }

    public byte MaximumAntiAliasing => FileFormat.MaximumAntiAliasing;

    [ObservableProperty]
    public partial byte MultipleBrightnessGenEmulatedAALevel { get; set; } = FileFormat.MaximumAntiAliasing;

    [ObservableProperty]
    public partial byte MultipleBrightnessGenExposureFractions { get; set; } = 8;

    partial void OnMultipleBrightnessGenEmulatedAALevelChanged(byte value) => GenerateBrightnessExposureFractions();
    partial void OnMultipleBrightnessGenExposureFractionsChanged(byte value) => GenerateBrightnessExposureFractions();

    /// <summary>
    /// Gets all holes in pixels and ordered
    /// </summary>
    public byte[] MultipleBrightnessValuesArray
    {
        get
        {
            List<byte> values = [];
            if (!string.IsNullOrWhiteSpace(MultipleBrightnessValues))
            {
                var split = MultipleBrightnessValues.Split(',', StringSplitOptions.TrimEntries);
                foreach (var brightnessStr in split)
                {
                    if (string.IsNullOrWhiteSpace(brightnessStr)) continue;
                    if (!byte.TryParse(brightnessStr, out var brightness)) continue;
                    if (brightness is <= 0 or > 255) continue;
                    if (values.Contains(brightness)) continue;
                    values.Add(brightness);
                }
            }

            return values.AsValueEnumerable().OrderByDescending(brightness => brightness).ToArray();
        }
    }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableLayerHeights))]
    public partial bool MultipleLayerHeight { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableLayerHeights))]
    public partial decimal MultipleLayerHeightMaximum { get; set; } = 0.1m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvailableLayerHeights))]
    public partial decimal MultipleLayerHeightStep { get; set; } = 0.01m;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMultipleExposuresBaseLayersPrintModeCustom))]
    public partial CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes MultipleExposuresBaseLayersPrintMode { get; set; }

    public bool IsMultipleExposuresBaseLayersPrintModeCustom => MultipleExposuresBaseLayersPrintMode == CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.Custom;

    [ObservableProperty]
    public partial decimal MultipleExposuresBaseLayersCustomExposure { get; set; }

    [ObservableProperty]
    public partial bool DifferentSettingsForSamePositionedLayers { get; set; }

    [ObservableProperty]
    public partial bool SamePositionedLayersLiftHeightEnabled { get; set; } = true;

    public decimal SamePositionedLayersLiftHeight
    {
        get => _samePositionedLayersLiftHeight;
        set => SetProperty(ref _samePositionedLayersLiftHeight, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial bool SamePositionedLayersWaitTimeBeforeCureEnabled { get; set; } = true;

    public decimal SamePositionedLayersWaitTimeBeforeCure
    {
        get => _samePositionedLayersWaitTimeBeforeCure;
        set => SetProperty(ref _samePositionedLayersWaitTimeBeforeCure, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial bool MultipleExposures { get; set; }

    [ObservableProperty]
    public partial CalibrateExposureFinderExposureGenTypes ExposureGenType { get; set; } = CalibrateExposureFinderExposureGenTypes.Linear;

    [ObservableProperty]
    public partial bool ExposureGenIgnoreBaseExposure { get; set; }

    public decimal ExposureGenBottomStep
    {
        get => _exposureGenBottomStep;
        set => SetProperty(ref _exposureGenBottomStep, Math.Round(value, 2));
    }

    public decimal ExposureGenNormalStep
    {
        get => _exposureGenNormalStep;
        set => SetProperty(ref _exposureGenNormalStep, Math.Round(value, 2));
    }

    [ObservableProperty]
    public partial byte ExposureGenTests { get; set; } = 4;

    [ObservableProperty]
    public partial decimal ExposureGenManualLayerHeight { get; set; }

    public decimal[] AvailableLayerHeights
    {
        get
        {
            List<decimal> layerHeights = [];
            var endLayerHeight = MultipleLayerHeight ? MultipleLayerHeightMaximum : _layerHeight;
            for (decimal layerHeight = _layerHeight; layerHeight <= endLayerHeight; layerHeight += MultipleLayerHeightStep)
            {
                layerHeights.Add(Layer.RoundHeight(layerHeight));
            }

            return layerHeights.ToArray();
        }
    }

    [ObservableProperty]
    public partial decimal ExposureGenManualBottom { get; set; }

    [ObservableProperty]
    public partial decimal ExposureGenManualNormal { get; set; }

    public ExposureItem ExposureManualEntry => new (ExposureGenManualLayerHeight, ExposureGenManualBottom, ExposureGenManualNormal);


    [ObservableProperty]
    public partial RangeObservableCollection<ExposureItem> ExposureTable { get; set; } = [];

    [ObservableProperty]
    public partial bool BullsEyeEnabled { get; set; } = true;

    [ObservableProperty]
    public partial string BullsEyeConfigurationPx { get; set; } = "26:5, 60:10, 116:15, 190:20";

    [ObservableProperty]
    public partial string BullsEyeConfigurationMm { get; set; } = "1.3:0.25, 3:0.5, 5.8:0.75, 9.5:1";

    [ObservableProperty]
    public partial byte BullsEyeFenceThickness { get; set; } = 10;

    [ObservableProperty]
    public partial sbyte BullsEyeFenceOffset { get; set; }

    [ObservableProperty]
    public partial bool BullsEyeInvertQuadrants { get; set; } = true;

    /// <summary>
    /// Gets all holes in pixels and ordered
    /// </summary>
    public BullsEyeCircle[] BullsEyes
    {
        get
        {
            if (!BullsEyeEnabled)
            {
                return [];
            }

            List<BullsEyeCircle> bulleyes = [];

            if (UnitOfMeasure == CalibrateExposureFinderMeasures.Millimeters)
            {
                var splitGroup = BullsEyeConfigurationMm.Split(',', StringSplitOptions.TrimEntries);
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
                var splitGroup = BullsEyeConfigurationPx.Split(',', StringSplitOptions.TrimEntries);
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

            return bulleyes.AsValueEnumerable().OrderBy(circle => circle.Diameter).DistinctBy(circle => circle.Diameter).ToArray();
        }
    }
    public int GetBullsEyeMaxPanelDiameter(BullsEyeCircle[] bullseyes)
    {
        if (!BullsEyeEnabled || bullseyes.Length == 0) return 0;
        var diameter = GetBullsEyeMaxDiameter(bullseyes);
        return Math.Max(diameter, diameter + BullsEyeFenceThickness + BullsEyeFenceOffset * 2);
    }

    public int GetBullsEyeMaxDiameter(BullsEyeCircle[] bullseyes)
    {
        if (!BullsEyeEnabled || bullseyes.Length == 0) return 0;
        return bullseyes[^1].Diameter + bullseyes[^1].Thickness / 2;
    }

    [ObservableProperty]
    public partial bool PatternModel { get; set; }

    partial void OnPatternModelChanged(bool value)
    {
        if (!value) return;
        if (SlicerFile is not null) LayerHeight = (decimal)SlicerFile.LayerHeight;
        MultipleLayerHeight = false;
    }

    [ObservableProperty]
    public partial bool PatternModelGlueBottomLayers { get; set; } = true;

    [ObservableProperty]
    public partial bool PatternModelTextEnabled { get; set; } = true;


    public bool CanPatternModel => SlicerFile.BoundingRectangle.Width * 2 + _leftRightMargin * 2 + _partMargin * Xppmm < SlicerFile.ResolutionX ||
                                   SlicerFile.BoundingRectangle.Height * 2 + _topBottomMargin * 2 + _partMargin * Yppmm < SlicerFile.ResolutionY;

    #endregion

    #region Constructor

    public OperationCalibrateExposureFinder() { }

    public OperationCalibrateExposureFinder(FileFormat slicerFile) : base(slicerFile)
    {
        if (SlicerFile.SupportPerLayerSettings)
        {
            DifferentSettingsForSamePositionedLayers = true;
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

        MirrorOutput = SlicerFile.DisplayMirror != FlipDirection.None;

        if (SlicerFile.DisplayWidth > 0)
            DisplayWidth = (decimal)SlicerFile.DisplayWidth;
        if (SlicerFile.DisplayHeight > 0)
            DisplayHeight = (decimal)SlicerFile.DisplayHeight;

        if(_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if(BottomLayers <= 0) BottomLayers = SlicerFile.BottomLayerCount;
        if(_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
        if(_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;

        if (ExposureGenManualBottom == 0)
            ExposureGenManualBottom = (decimal) SlicerFile.BottomExposureTime;
        if (ExposureGenManualNormal == 0)
            ExposureGenManualNormal = (decimal)SlicerFile.ExposureTime;
        if (MultipleBrightnessGenExposureTime == 0)
            MultipleBrightnessGenExposureTime = (decimal)SlicerFile.ExposureTime;

        if (MultipleExposuresBaseLayersCustomExposure <= 0) MultipleExposuresBaseLayersCustomExposure = (decimal)SlicerFile.ExposureTime;

        if (!SlicerFile.CanUseLayerExposureTime)
        {
            MultipleLayerHeight = false;
            MultipleExposures = false;
        }

        if (string.IsNullOrWhiteSpace(MultipleBrightnessValues))
        {
            MultipleBrightnessValues =
                SlicerFile.IsAntiAliasingEmulated
                    ? "255, 239, 223, 207, 191, 175, 159, 143"
                    : "255, 242, 230, 217, 204, 191";
        }
    }

    #endregion

    #region Equality

    private bool Equals(OperationCalibrateExposureFinder other)
    {
        return _displayWidth == other._displayWidth && _displayHeight == other._displayHeight && _layerHeight == other._layerHeight && BottomLayers == other.BottomLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _topBottomMargin == other._topBottomMargin && _leftRightMargin == other._leftRightMargin && ChamferLayers == other.ChamferLayers && ErodeBottomIterations == other.ErodeBottomIterations && _partMargin == other._partMargin && EnableAntiAliasing == other.EnableAntiAliasing && MirrorOutput == other.MirrorOutput && _baseHeight == other._baseHeight && _featuresHeight == other._featuresHeight && _featuresMargin == other._featuresMargin && StaircaseThicknessPx == other.StaircaseThicknessPx && StaircaseThicknessMm == other.StaircaseThicknessMm && HolesEnabled == other.HolesEnabled && HoleShape == other.HoleShape && UnitOfMeasure == other.UnitOfMeasure && HoleDiametersPx == other.HoleDiametersPx && HoleDiametersMm == other.HoleDiametersMm && BarsEnabled == other.BarsEnabled && BarSpacing == other.BarSpacing && BarLength == other.BarLength && BarVerticalSplitter == other.BarVerticalSplitter && BarFenceThickness == other.BarFenceThickness && BarFenceOffset == other.BarFenceOffset && BarThicknessesPx == other.BarThicknessesPx && BarThicknessesMm == other.BarThicknessesMm && TextEnabled == other.TextEnabled && TextFont == other.TextFont && _textScale.Equals(other._textScale) && TextThickness == other.TextThickness && Text == other.Text && MultipleBrightness == other.MultipleBrightness && MultipleBrightnessExcludeFrom == other.MultipleBrightnessExcludeFrom && MultipleBrightnessValues == other.MultipleBrightnessValues && MultipleBrightnessGenExposureTime == other.MultipleBrightnessGenExposureTime && MultipleBrightnessGenEmulatedAALevel == other.MultipleBrightnessGenEmulatedAALevel && MultipleBrightnessGenExposureFractions == other.MultipleBrightnessGenExposureFractions && MultipleLayerHeight == other.MultipleLayerHeight && MultipleLayerHeightMaximum == other.MultipleLayerHeightMaximum && MultipleLayerHeightStep == other.MultipleLayerHeightStep && MultipleExposuresBaseLayersPrintMode == other.MultipleExposuresBaseLayersPrintMode && MultipleExposuresBaseLayersCustomExposure == other.MultipleExposuresBaseLayersCustomExposure && DifferentSettingsForSamePositionedLayers == other.DifferentSettingsForSamePositionedLayers && SamePositionedLayersLiftHeightEnabled == other.SamePositionedLayersLiftHeightEnabled && _samePositionedLayersLiftHeight == other._samePositionedLayersLiftHeight && SamePositionedLayersWaitTimeBeforeCureEnabled == other.SamePositionedLayersWaitTimeBeforeCureEnabled && _samePositionedLayersWaitTimeBeforeCure == other._samePositionedLayersWaitTimeBeforeCure && MultipleExposures == other.MultipleExposures && ExposureGenType == other.ExposureGenType && ExposureGenIgnoreBaseExposure == other.ExposureGenIgnoreBaseExposure && _exposureGenBottomStep == other._exposureGenBottomStep && _exposureGenNormalStep == other._exposureGenNormalStep && ExposureGenTests == other.ExposureGenTests && ExposureGenManualLayerHeight == other.ExposureGenManualLayerHeight && ExposureGenManualBottom == other.ExposureGenManualBottom && ExposureGenManualNormal == other.ExposureGenManualNormal && Equals(ExposureTable, other.ExposureTable) && BullsEyeEnabled == other.BullsEyeEnabled && BullsEyeConfigurationPx == other.BullsEyeConfigurationPx && BullsEyeConfigurationMm == other.BullsEyeConfigurationMm && BullsEyeInvertQuadrants == other.BullsEyeInvertQuadrants && CounterTrianglesEnabled == other.CounterTrianglesEnabled && CounterTrianglesTipOffset == other.CounterTrianglesTipOffset && CounterTrianglesFence == other.CounterTrianglesFence && PatternModel == other.PatternModel && BullsEyeFenceThickness == other.BullsEyeFenceThickness && BullsEyeFenceOffset == other.BullsEyeFenceOffset && PatternModelGlueBottomLayers == other.PatternModelGlueBottomLayers && PatternModelTextEnabled == other.PatternModelTextEnabled;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationCalibrateExposureFinder other && Equals(other);
    }

    #endregion

    #region Methods

    public void SortExposureTable()
    {
        ExposureTable.Sort();
    }

    public void SanitizeExposureTable()
    {
        ExposureTable.ReplaceCollection(GetSanitizedExposureTable());
    }

    public List<ExposureItem> GetSanitizedExposureTable()
    {
        var list = ExposureTable.AsValueEnumerable().Distinct().ToList();
        list.Sort();
        return list;
    }

    public void GenerateBrightnessExposureFractions()
    {
        var fractions = MultipleBrightnessGenExposureFractions;
        var increment = 255f / MultipleBrightnessGenEmulatedAALevel;

        byte[] validAA = GC.AllocateUninitializedArray<byte>(fractions);

        for (byte frac = 0; frac < fractions; frac++)
        {
            validAA[frac] = (byte)(byte.MaxValue - increment * frac);
        }

        MultipleBrightnessValues = string.Join(", ", validAA);
    }

    public void GenerateExposureTable()
    {
        var endLayerHeight = MultipleLayerHeight ? MultipleLayerHeightMaximum : _layerHeight;
        List<ExposureItem> list = [];
        for (decimal layerHeight = _layerHeight;
             layerHeight <= endLayerHeight;
             layerHeight += MultipleLayerHeightStep)
        {
            if(!ExposureGenIgnoreBaseExposure)
                list.Add(new ExposureItem(layerHeight, _bottomExposure, _normalExposure));
            for (ushort testN = 1; testN <= ExposureGenTests; testN++)
            {
                decimal bottomExposureTime = 0;
                decimal exposureTime = 0;

                switch (ExposureGenType)
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
        var markingTextSize = EmguCvExtensions.GetTextSizeExtended("100u\n20.00s\n3.00s",
            TextFont, _textScale, TextThickness, 10, ref baseLine);
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

        int barLengthPx = (int) (BarLength * Xppmm);
        int barSpacingPx = (int) (BarSpacing * Yppmm);
        int barsPanelWidth = 0;

        if (bars.Length > 0)
        {
            barsPanelWidth = barLengthPx * 2 + BarVerticalSplitter;
            if (BarFenceThickness > 0)
            {
                barsPanelWidth = Math.Max(barsPanelWidth, barsPanelWidth + BarFenceThickness * 2 + BarFenceOffset * 2);
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
        layers[0] = EmguCvExtensions.InitMat(rect.Size);

        CvInvoke.Rectangle(layers[0], rect, EmguCvExtensions.WhiteColor, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
        layers[1] = layers[0].NewZeros();
        if (holes.Length > 0)
        {
            CvInvoke.Rectangle(layers[1],
                new Rectangle(rect.Size.Width - holePanelWidth, 0, rect.Size.Width, layers[0].Height),
                EmguCvExtensions.WhiteColor, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
        }


        int xPos = 0;
        int yPos = 0;

        // Print staircase
        if (isPreview && startCaseThickness > 0)
        {
            CvInvoke.Rectangle(layers[1],
                new Rectangle(0, 0, layers[1].Size.Width-holePanelWidth, startCaseThickness),
                EmguCvExtensions.WhiteColor, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
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

                var effectiveShape = HoleShape == CalibrateExposureFinderShapes.Square || diameter < 6 ?
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
                                    EmguCvExtensions.WhiteColor, -1,
                                    EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                                break;
                            case CalibrateExposureFinderShapes.Circle:
                                layers[layerIndex].DrawCircle(new Point(xPos, yPos),
                                    SlicerFile.PixelsToNormalizedPitch(radius), EmguCvExtensions.WhiteColor, -1,
                                    EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
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
                                EmguCvExtensions.BlackColor, -1,
                                EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                            break;
                        case CalibrateExposureFinderShapes.Circle:
                            layers[layerIndex].DrawCircle(new Point(xPos, yPos),
                                SlicerFile.PixelsToNormalizedPitch(radius), EmguCvExtensions.BlackColor, -1,
                                EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
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
            yPos = yStartPos + BarFenceThickness / 2 + BarFenceOffset;
            xPos += BarFenceThickness / 2 + BarFenceOffset;
            for (int i = 0; i < bars.Length; i++)
            {
                // Print positive bottom
                CvInvoke.Rectangle(layers[1], new Rectangle(xPos, yPos, barLengthPx - 1, barSpacingPx - 1),
                    EmguCvExtensions.WhiteColor, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                // Print positive top
                yPos += barSpacingPx;
                CvInvoke.Rectangle(layers[1], new Rectangle(xPos + barLengthPx + BarVerticalSplitter, yPos, barLengthPx - 1, bars[i] - 1),
                    EmguCvExtensions.WhiteColor, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                yPos += bars[i];
            }

            // Left over
            CvInvoke.Rectangle(layers[1], new Rectangle(xPos, yPos, barLengthPx - 1, barSpacingPx - 1),
                EmguCvExtensions.WhiteColor, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

            yPos += barSpacingPx;

            if (BarFenceThickness > 0)
            {
                CvInvoke.Rectangle(layers[1], new Rectangle(
                        xStartPos - 1,
                        yStartPos - 1,
                        barsPanelWidth - BarFenceThickness + 1,
                        yPos - yStartPos + BarFenceThickness / 2 + BarFenceOffset + 1),
                    EmguCvExtensions.WhiteColor, BarFenceThickness, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

                yPos += BarFenceThickness * 2 + BarFenceOffset * 2;
            }

            xPos += featuresMarginX;
        }

        if (!textSize.IsEmpty)
        {
            CvInvoke.Rotate(layers[1], layers[1], RotateFlags.Rotate90CounterClockwise);
            CvInvoke.PutText(layers[1], Text, new Point(startCaseThickness + featuresMarginY, layers[1].Height - barsPanelWidth - featuresMarginX * (barsPanelWidth > 0 ? 2 : 1)), TextFont, _textScale, EmguCvExtensions.WhiteColor, TextThickness, EnableAntiAliasing ? LineType.AntiAlias :  LineType.EightConnected);
            CvInvoke.Rotate(layers[1], layers[1], RotateFlags.Rotate90Clockwise);
        }

        // Print bullseye
        if (bulleyes.Length > 0)
        {
            yPos = bullseyeYPos;
            foreach (var circle in bulleyes)
            {
                CvInvoke.Circle(layers[1], new Point(bullseyeXPos, yPos), circle.Radius, EmguCvExtensions.WhiteColor, circle.Thickness, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            }

            if (BullsEyeInvertQuadrants)
            {
                var matRoi1 = new Mat(layers[1], new Rectangle(bullseyeXPos, yPos - bulleyesRadius - 5, bulleyesRadius + 6, bulleyesRadius + 5));
                var matRoi2 = new Mat(layers[1], new Rectangle(bullseyeXPos - bulleyesRadius - 5, yPos, bulleyesRadius + 5, bulleyesRadius + 6));
                //using var mask = matRoi1.CloneBlank();

                //CvInvoke.Circle(mask, new Point(mask.Width / 2, mask.Height / 2), bulleyesRadius, EmguCvExtensions.WhiteByte, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                //CvInvoke.Circle(mask, new Point(mask.Width / 2, mask.Height / 2), BullsEyes[^1].Radius, EmguCvExtensions.WhiteByte, BullsEyes[^1].Thickness, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

                CvInvoke.BitwiseNot(matRoi1, matRoi1);
                CvInvoke.BitwiseNot(matRoi2, matRoi2);
            }

            if (BullsEyeFenceThickness > 0)
            {
                CvInvoke.Rectangle(layers[1],
                    new Rectangle(
                        new Point(
                            bullseyeXPos - bulleyesRadius - 5 - BullsEyeFenceOffset - BullsEyeFenceThickness / 2,
                            yPos - bulleyesRadius - 5 - BullsEyeFenceOffset - BullsEyeFenceThickness / 2),
                        new Size(
                            bulleyesDiameter + 10 + BullsEyeFenceOffset*2 + BullsEyeFenceThickness,
                            bulleyesDiameter + 10 + BullsEyeFenceOffset*2 + BullsEyeFenceThickness)),
                    EmguCvExtensions.WhiteColor,
                    BullsEyeFenceThickness,
                    EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
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
                TextFont, _textScale, EmguCvExtensions.WhiteColor, TextThickness, 10, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);

            if (holes.Length > 0)
            {
                layers[1].PutTextExtended($"{Microns}u\n{_bottomExposure}s\n{_normalExposure}s", markingTextNegativePosition,
                    TextFont, _textScale, EmguCvExtensions.BlackColor, TextThickness, 10, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            }
        }

        if (negativeSideWidth >= 200 && CounterTrianglesEnabled)
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
                    new(xPos + triangleWidth, yHalfPos - CounterTrianglesTipOffset) // Middle
                ];
                triangles[3] =
                [
                    new(xPos + triangleWidth - triangleWidthQuarter, yPosEnd),  // Bottom Left
                    new(xPos + triangleWidth + triangleWidthQuarter, yPosEnd),  // Bottom Right
                    new(xPos + triangleWidth, yHalfPos + CounterTrianglesTipOffset) // Middle
                ];

                foreach (var triangle in triangles)
                {
                    using var vec = new VectorOfPoint(triangle);
                    CvInvoke.FillPoly(layers[1], vec, EmguCvExtensions.WhiteColor,
                        EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                }

                /*byte size = 60;
                var matRoi = new Mat(layers[1], new Rectangle(
                    new Point(xPos + triangleWidth - size / 2, yHalfPos - size / 2),
                    new Size(size, size)));

                CvInvoke.BitwiseNot(matRoi, matRoi);*/


                if (CounterTrianglesFence)
                {
                    byte outlineThickness = 8;
                    //byte outlineThicknessHalf = (byte)(outlineThickness / 2);

                    CvInvoke.Rectangle(layers[1], new Rectangle(
                            new Point(triangles[0][0].X - 0, triangles[0][0].Y - 0),
                            new Size(triangleWidth * 2 + 0, triangleHeight + 0)
                        ), EmguCvExtensions.WhiteColor, outlineThickness,
                        EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
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
                CvInvoke.Circle(mat, new Point(xPos, yPos), radius, EmguCvExtensions.WhiteByte, circleThickness, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                maxRadius = radius;
            }

            CvInvoke.Circle(mat, new Point(xPos, yPos), 5, EmguCvExtensions.WhiteByte, -1, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
            CvInvoke.Circle(matMask, new Point(xPos, yPos), maxRadius+2, EmguCvExtensions.WhiteByte, -1);

            var matRoi1 = new Mat(mat, new Rectangle(xPos, yPos - maxRadius-1, maxRadius+2, maxRadius+1));
            var matRoi2 = new Mat(mat, new Rectangle(xPos-maxRadius-1, yPos, maxRadius+1, Math.Min(mat.Height- yPos, maxRadius)));

            CvInvoke.BitwiseNot(matRoi1, matRoi1);
            CvInvoke.BitwiseNot(matRoi2, matRoi2);

            CvInvoke.BitwiseAnd(layers[0], mat, layers[1], matMask);

            //CvInvoke.MorphologyEx(layers[1], layers[1], MorphOp.Open, CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(3,3), EmguCvExtensions.AnchorCenter), EmguCvExtensions.AnchorCenter, 1, BorderType.Reflect101, default);

            mat.Dispose();
            matMask.Dispose();
        }*/

        return layers;
    }

    public Mat GetThumbnail()
    {
        Mat thumbnail = EmguCvExtensions.InitMat(new Size(400, 200), 3);
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

        if (MultipleLayerHeight)
        {
            text += $"{Microns}um-{(ushort)(MultipleLayerHeightMaximum *1000)}um/{(ushort)(MultipleLayerHeightStep *1000)}um\n";
        }
        else
        {
            text += $"Layer height: {Microns}um\n";
        }

        if (MultipleExposures)
        {
            text += $"{ExposureTable[0].Exposure}s-{ExposureTable[^1].Exposure}s/{_exposureGenNormalStep}s";
            if (!PatternModel)
            {
                text += $"\nObjects: {ExposureTable.Count}";
            }
        }
        else
        {
            text += $"{_bottomExposure}s/{_normalExposure}s";
        }

        if (PatternModel)
        {
            text += "\nPatterned Model";
        }


        thumbnail.PutTextExtended(text, new Point(xSpacing, ySpacing * 3 - 20), fontFace, 0.8, EmguCvExtensions.WhiteColor, 2, 10);


        /*CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);
        if (PatternModel)
        {
            CvInvoke.PutText(thumbnail, $"Patterned Model", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);
        }
        else
        {
            CvInvoke.PutText(thumbnail, $"Features: {(_staircaseThickness > 0 ? 1 : 0) + Holes.Length + Bars.Length + BullsEyes.Length + (CounterTrianglesEnabled ? 1 : 0)}", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguCvExtensions.WhiteColor, fontThickness);
        }*/


        return thumbnail;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        int sideMarginPx = (int)(_leftRightMargin * Xppmm);
        int topBottomMarginPx = (int)(_topBottomMargin * Yppmm);
        int partMarginXPx = (int)(_partMargin * Xppmm);
        int partMarginYPx = (int)(_partMargin * Yppmm);

        var kernel = EmguCvExtensions.Kernel3X3Rectangle;

        if (PatternModel)
        {
            ConcurrentBag<Layer> parallelLayers = [];
            Dictionary<ExposureItem, Point> table = new();
            var boundingRectangle = SlicerFile.BoundingRectangle;
            int xHalf = boundingRectangle.Width / 2;
            int yHalf = boundingRectangle.Height / 2;

            var baseLine = 0;
            var markingTextSize = EmguCvExtensions.GetTextSizeExtended("100u\n20.00s\n3.00s",
                TextFont, _textScale, TextThickness, 10, ref baseLine);

            var brightnesses = MultipleBrightnessValuesArray;
            var multipleExposures = ExposureTable.AsValueEnumerable().Where(item => item.IsValid && item.LayerHeight == (decimal) SlicerFile.LayerHeight).ToArray();
            if (brightnesses.Length == 0 || !MultipleBrightness) brightnesses = [byte.MaxValue];
            if (multipleExposures.Length == 0 || !MultipleExposures) multipleExposures = [new ExposureItem((decimal)SlicerFile.LayerHeight, _bottomExposure, _normalExposure)
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
            SlicerFile.BottomLayerCount = BottomLayers;
            ushort bottomLayerCount = 0;
            progress.ItemCount = (uint) (SlicerFile.LayerCount * table.Count);
            Parallel.For(0, SlicerFile.LayerCount, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                var layer = SlicerFile[layerIndex];
                using var mat = layer.LayerMat;
                var matRoi = new Mat(mat, boundingRectangle);
                int layerCountOnHeight = (int)(layer.PositionZ / SlicerFile.LayerHeight);
                bool isBottomLayer = layerCountOnHeight <= BottomLayers;

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
                            if (PatternModelGlueBottomLayers)
                            {
                                newMatRoi.SetTo(EmguCvExtensions.WhiteColor);
                            }
                        }

                        if (layerCountOnHeight < ChamferLayers)
                        {
                            CvInvoke.Erode(newMatRoi, newMatRoi, kernel, EmguCvExtensions.AnchorCenter, ChamferLayers - layerCountOnHeight, BorderType.Reflect101, default);
                        }

                        if (layer.IsBottomLayer)
                        {
                            if (ErodeBottomIterations > 0)
                            {
                                CvInvoke.Erode(newMatRoi, newMatRoi, kernel, EmguCvExtensions.AnchorCenter, ErodeBottomIterations, BorderType.Reflect101, default);
                            }

                            if(PatternModelTextEnabled)
                            {
                                newMatRoi.PutTextExtended((MultipleBrightness ? $"{brightness.ToString()}\n" : string.Empty)
                                                          + $"{microns}u\n{group.Key.BottomExposure}s\n{group.Key.Exposure}s", new Point(xHalf - markingTextSize.Width / 2, yHalf - markingTextSize.Height / 2),
                                    TextFont, _textScale, EmguCvExtensions.BlackColor, TextThickness, 10, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                            }
                        }

                        if (brightness < 255)
                        {
                            if (MultipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.None ||
                                MultipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.Bottom && !layer.IsBottomLayer ||
                                MultipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.BottomAndBase && !layer.IsBottomLayer)
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
            var layers = parallelLayers.AsValueEnumerable().OrderBy(layer => layer.PositionZ).ThenBy(layer => layer.ExposureTime).ToList();

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
            var endLayerHeight = MultipleLayerHeight ? MultipleLayerHeightMaximum : _layerHeight;
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
            if (brightnesses.Length == 0 || !MultipleBrightness) brightnesses = [byte.MaxValue];

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
                bool isBottomLayer = layerCountOnHeight <= BottomLayers;
                bool isBaseLayer = currentHeight <= _baseHeight;
                ushort microns = (ushort)(layerHeight * 1000);
                bool addSomething = false;

                bool reUseLastLayer =
                    lastExposureItem is not null &&
                    lastCurrentHeight == currentHeight &&
                    lastExposureItem.LayerHeight == layerHeight &&
                    (
                        ((isBottomLayer && lastExposureItem.BottomExposure == bottomExposure) || (!isBottomLayer && lastExposureItem.Exposure == normalExposure)) ||
                        (!isBottomLayer && isBaseLayer && MultipleExposuresBaseLayersPrintMode != CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.Iterative)
                    );

                using var mat = reUseLastLayer ? newLayers[^1].LayerMat : EmguCvExtensions.InitMat(SlicerFile.Resolution);

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
                                EmguCvExtensions.WhiteColor, -1,
                                EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                        }
                    }

                    if (isBottomLayer && ErodeBottomIterations > 0)
                    {
                        CvInvoke.Erode(matRoi, matRoi, kernel, EmguCvExtensions.AnchorCenter, ErodeBottomIterations, BorderType.Reflect101, default);
                    }

                    if (layerCountOnHeight < ChamferLayers)
                    {
                        CvInvoke.Erode(matRoi, matRoi, kernel, EmguCvExtensions.AnchorCenter, ChamferLayers - layerCountOnHeight, BorderType.Reflect101, default);
                    }

                    if (MultipleBrightness && brightness < 255)
                    {
                        // normalExposure - 255
                        //       x        - brightness
                        normalExposureTemp = Math.Round(normalExposure * brightness / byte.MaxValue, 2);
                        if (MultipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.None)
                        {
                            bottomExposureTemp = Math.Round(bottomExposure * brightness / byte.MaxValue, 2);
                        }
                    }

                    matRoi.PutTextExtended($"{microns}u\n{bottomExposureTemp}s\n{normalExposureTemp}s", markingTextPositivePosition,
                        TextFont, _textScale, EmguCvExtensions.WhiteColor, TextThickness, 10, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    if (holes.Length > 0)
                    {
                        matRoi.PutTextExtended($"{microns}u\n{bottomExposureTemp}s\n{normalExposureTemp}s", markingTextNegativePosition,
                            TextFont, _textScale, EmguCvExtensions.BlackColor, TextThickness, 10, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                    }


                    if (MultipleBrightness)
                    {
                        CvInvoke.PutText(matRoi, brightness.ToString(), new Point(matRoi.Width / 3, 35), TextMarkingFontFace, TextMarkingScale, EmguCvExtensions.WhiteColor, TextMarkingThickness, EnableAntiAliasing ? LineType.AntiAlias : LineType.EightConnected);
                        if (brightness < 255 &&
                            (MultipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.None ||
                             MultipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.Bottom && !isBottomLayer ||
                             MultipleBrightnessExcludeFrom == CalibrateExposureFinderMultipleBrightnessExcludeFrom.BottomAndBase && !isBottomLayer && !isBaseLayer)
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
                        layer.ExposureTime = MultipleExposuresBaseLayersPrintMode switch
                        {
                            CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.Iterative => (float)normalExposure,
                            CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.UseLowest => (float) ExposureTable[0].Exposure,
                            CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.UseMiddle => (float) ExposureTable[(int) Math.Ceiling((ExposureTable.Count - 1) / 2.0)].Exposure,
                            CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.UseHighest => (float) ExposureTable[^1].Exposure,
                            CalibrateExposureFinderMultipleExposuresBaseLayersPrintModes.Custom => (float) MultipleExposuresBaseLayersCustomExposure,
                            _ => throw new ArgumentOutOfRangeException($"Unhandled type for {MultipleExposuresBaseLayersPrintMode}")
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
                for (decimal layerHeight = _layerHeight; layerHeight <= endLayerHeight; layerHeight += MultipleLayerHeightStep)
                {
                    progress.PauseOrCancelIfRequested();
                    layerHeight = Layer.RoundHeight(layerHeight);

                    if (MultipleExposures)
                    {
                        foreach (var exposureItem in ExposureTable)
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

            if (MirrorOutput)
            {
                var flip = SlicerFile.DisplayMirror;
                if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
                new OperationFlip(SlicerFile) { FlipDirection = (FlipType)flip }.Execute(progress);
            }
        }

        if (MultipleBrightness && SlicerFile.IsAntiAliasingEmulated)
        {
            /*SlicerFile.AntiAliasing = MultipleBrightnessValuesArray.Length switch
            {
                <= 2 => 2,
                <= 4 => 4,
                <= 8 => 8,
                <= 16 => 16,
                _ => 16
            };*/
            SlicerFile.AntiAliasing = MultipleBrightnessGenEmulatedAALevel;
        }

        if (SlicerFile.ThumbnailsCount > 0)
        {
            using var thumbnail = GetThumbnail();
            SlicerFile.SetThumbnails(thumbnail);
        }

        if (DifferentSettingsForSamePositionedLayers)
        {
            var layers = SlicerFile.SamePositionedLayers;
            foreach (var layer in layers)
            {
                if(SamePositionedLayersLiftHeightEnabled)    layer.LiftHeightTotal = (float) _samePositionedLayersLiftHeight;
                if(SamePositionedLayersWaitTimeBeforeCureEnabled) layer.SetWaitTimeBeforeCureOrLightOffDelay((float) _samePositionedLayersWaitTimeBeforeCure);
            }
        }

        SlicerFile.WaitTimeAfterCure = 0;
        SlicerFile.WaitTimeAfterLift = 0;

        new OperationMove(SlicerFile).Execute(progress);

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}
