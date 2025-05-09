/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using UVtools.Core.Converters;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using ZLinq;

namespace UVtools.Core.FileFormats;

#region Wanhao


[XmlRoot(ElementName = "manifest")]
public sealed class CWSManifest
{
    public const string FileName = "manifest.xml";

    [XmlAttribute]
    public byte FileVersion { get; set; } = 1;

    public sealed class Model
    {
        [XmlElement("name")]
        public string? Name { get; set; }

        [XmlElement("tag")]
        public string? Tag { get; set; }
    }

    public sealed class Slice
    {
        [XmlElement("name")]
        public string? Name { get; set; }

        public Slice()
        {
        }

        public Slice(string name)
        {
            Name = name;
        }
    }

    [XmlArrayItem("model")]
    public Model[]? Models { get; set; }
    public Slice[]? Slices { get; set; }

    public Slice SliceProfile { get; set; } = new();
    public Slice GCode { get; set; } = new();
}


[XmlRoot(ElementName = "SliceBuildConfig")]
public sealed class CWSSliceBuildConfig
{
    public const string GCODESTART =
        ";********** Header Start ********\r\n" +
        "G21 ; Set units to be mm\r\n" +
        "G91 ; Relative Positioning\r\n" +
        "M17 ; Enable motors\r\n" +
        "; G28 Z ; Auto-home on print - commented by default";

    public const string GCODEEND =
        ";********** Footer ********\r\n" +
        "@cmdCloseShutter ; UV off\r\n" +
        "G4 P0            ; wait for last lift to complete\r\n" +
        "G1 Z40.0 F150.0  ; lift model clear of resin\r\n" +
        "G4 P0            ; sync\r\n" +
        "M18              ; Disable Motors\r\n" +
        ";&lt;Completed&gt;";

    public const string GCODELIFT =
        ";********** Lift Sequence %d$CURSLICE ********\r\n" +
        "G1 Z($ZLiftDist * $ZDir) F{$CURSLICE &lt; $NumFirstLayers?$ZBottomLiftRate:$ZLiftRate}\r\n;" +
        "&lt;Takes&gt; {$CURSLICE&lt;$NumFirstLayers?%d($ZLiftDist*1000*60/$ZBottomLiftRate):%d($ZLiftDist*1000*60/$ZLiftRate)}\r\n" +
        "G4 P0            ; Wait for lift rise to complete\r\n;" +
        "&lt;Delay&gt; %d$TopTime\r\n" +
        "G1 Z(($LayerThickness-$ZLiftDist) * $ZDir) F$ZRetractRate\r\n" +
        ";&lt;Takes&gt; %d(($ZLiftDist*1000-$LayerThickness*1000)*60/$ZRetractRate)";

    public const string GCODELAYER =
        ";********** Layer %d$CURSLICE ********\r\n" +
        ";&lt;Slice&gt; %d$CURSLICE\r\n" +
        "@cmdOpenShutter  ; UV on\r\n" +
        ";{$CURSLICE&lt;$NumFirstLayers?&lt;Delay&gt; %d$FirstLayerTime:&lt;Delay&gt; %d$LayerTime}\r\n" +
        "@cmdCloseShutter ; UV off\r\n" +
        ";&lt;Slice&gt; Blank\r\n" +
        ";&lt;Delay&gt; %d$BlankTime";

    public sealed class InkConfig
    {
        public string? Name { get; set; }
        public float SliceHeight { get; set; }
        public uint LayerTime { get; set; }
        public uint FirstLayerTime { get; set; }
        public ushort NumberofBottomLayers { get; set; }
        public float ResinPriceL { get; set; }
    }

    [XmlAttribute]
    public byte FileVersion { get; set; } = 2;

    public float DotsPermmX { get; set; } = 21.164f;
    public float DotsPermmY { get; set; } = 21.164f;

    public ushort XResolution { get; set; }
    public ushort YResolution { get; set; }
    public uint BlankTime { get; set; }
    public uint TopTime { get; set; }
    public uint PlatformTemp { get; set; } = 75;
    public byte ExportSVG { get; set; }
    public string Export { get; set; } = "False";
    public string ExportPNG { get; set; } = "False";
    public string CalcSliceSize { get; set; } = "False";
    public uint XOffset { get; set; }
    public uint YOffset { get; set; }
    public string Direction { get; set; } = "Bottom_Up";
    public float LiftDistance { get; set; } = 6;
    public float SlideTiltValue { get; set; }
    public uint SettleTime { get; set; } = 1600;
    public string AntiAliasing { get; set; } = "True";
    public string UseMainLiftGCode { get; set; } = "False";
    public uint AntiAliasingValue { get; set; } = 4;
    public float LiftFeedRate { get; set; } = 40;
    public float BottomLiftFeedRate { get; set; } = 25;
    public float LiftRetractRate { get; set; } = 80;
    public string ExportOption { get; set; } = "ZIP";
    public string RenderOutlines { get; set; } = "False";
    public uint OutlineWidth_Inset { get; set; } = 2;
    public uint OutlineWidth_Outset { get; set; }
    public string FlipX { get; set; } = "False";
    public string FlipY { get; set; } = "False";

    public string Notes { get; set; } =
        "Use these as a place to start; tune them for your specific printer and resins.\r\nAlways run a calibration on your resin to dial in for your machine.\r\nProfiles created by Earl Miller for CW v1.0.0.75.";

    public string GCodeHeader { get; set; } = GCODESTART;
    public string GCodeFooter { get; set; } = GCODEEND;
    public string GCodeLift { get; set; } = GCODELIFT;
    public string GCodeLayer { get; set; } = GCODELAYER;

    [XmlElement("InkConfig")]
    public List<InkConfig>? InkConfigs { get; set; }

    public string? SelectedInk { get; set; }
    public uint MinTestExposure { get; set; } = 5000;
    public uint TestExposureStep { get; set; } = 100;
    public string ExportPreview { get; set; } = "None";
    public string UseMask { get; set; } = "False";
    public string MaskFilename { get; set; } = "False";
}

#endregion

public sealed class CWSFile : FileFormat
{
    #region Constants

    public const string GCodeStart = "G28 ;Auto Home{0}" +
                                     "G21 ;Set units to be mm{0}" +
                                     "G91 ;Relative Positioning{0}" +
                                     "M17 ;Enable motors{0}" +
                                     "<Slice> Blank{0}" +
                                     "M106 S0{0}{0}";

    public const string GCodeEnd = "M106 S0 ;UV off{0}" +
                                   "G1 Z{1}{0}" +
                                   "{0}M18 ;Disable Motors{0}" +
                                   ";<Completed>{0}";

    public const string GCodeKeywordSlice = ";<Slice>";
    public const string GCodeKeywordSliceBlank = ";<Slice> Blank";
    public const string GCodeKeywordDelay = ";<Delay>";
    public const string GCodeKeywordTakes = ";<Takes>";

    public const string WanhaoStartGCode =
        ";********** Header Start ********{0}" +
        "G21 ; Set units to be mm{0}" +
        "G91 ; Relative Positioning{0}" +
        "M17 ; Enable motors{0}" +
        "; G28 Z ; Auto-home on print - commented by default{0}";

    public const string WanhaoPreSliceGCode =
        ";********** Pre-Slice {1} ********{0}" +
        "G4 P0; Make sure any previous relative moves are complete{0}" +
        ";<Delay> {2}{0}" +
        ";********** Layer {1} ********{0}";

    public const string WanhaoGCodeEnd =
        ";********** Footer ********{0}" +
        "M106 S0        ; UV off{0}" +
        "G4 P0            ; wait for last lift to complete{0}" +
        "G1 Z40.0 F150.0  ; lift model clear of resin{0}" +
        "G4 P0            ; sync{0}" +
        "M18              ; Disable Motors{0}" +
        ";<Completed>{0}";

    #endregion

    #region Sub Classes

    public class Output
    {
        // ;(****Build and Slicing Parameters****)
        [DisplayName("Pix per mm X")] public float PixPermmX { get; set; } = 19.324f;
        [DisplayName("Pix per mm Y")] public float PixPermmY { get; set; } = 19.324f;
        [DisplayName("X Resolution")] public ushort XResolution { get; set; }
        [DisplayName("Y Resolution")] public ushort YResolution { get; set; }
        [DisplayName("Layer Thickness")] public float LayerThickness { get; set; }
        [DisplayName("Layer Time")] public uint LayerTime { get; set; } = 5500;
        [DisplayName("Render Outlines")] public bool RenderOutlines { get; set; } = false;
        [DisplayName("Outline Width Inset")] public ushort OutlineWidthInset { get; set; } = 2;
        [DisplayName("Outline Width Outset")] public ushort OutlineWidthOutset { get; set; } = 0;
        [DisplayName("Bottom Layers Time")] public uint BottomLayersTime { get; set; } = 35000;
        [DisplayName("Number of Bottom Layers")] public ushort NumberBottomLayers { get; set; } = 3;
        [DisplayName("Blanking Layer Time")] public uint BlankingLayerTime { get; set; } = 1000;
        [DisplayName("Build Direction")] public string BuildDirection { get; set; } = "Bottom_Up";
        [DisplayName("Lift Distance")] public float LiftDistance { get; set; } = 4;
        [DisplayName("Slide/Tilt Value")] public byte TiltValue { get; set; }
        [DisplayName("Use Mainlift GCode Tab")] public bool UseMainliftGCodeTab { get; set; }
        [DisplayName("Settle Time")] public uint SettleTime { get; set; } = 1600;
        [DisplayName("Anti Aliasing")] public bool AntiAliasing { get; set; } = true;
        [DisplayName("Anti Aliasing Value")] public float AntiAliasingValue { get; set; } = 2;
        [DisplayName("Z Lift Feed Rate")] public float ZLiftFeedRate { get; set; } = 120;
        [DisplayName("Z Bottom Lift Feed Rate")] public float ZBottomLiftFeedRate { get; set; } = 120;
        [DisplayName("Z Lift Retract Rate")] public float ZLiftRetractRate { get; set; } = 120;
        [DisplayName("Flip X")] public bool FlipX { get; set; }
        [DisplayName("Flip Y")] public bool FlipY { get; set; }
        [DisplayName("Number of Slices")] public uint LayersNum { get; set; }

        // ;(****Machine Configuration ******)
        [DisplayName("Platform X Size")] public float PlatformXSize { get; set; }
        [DisplayName("Platform Y Size")] public float PlatformYSize { get; set; }
        [DisplayName("Platform Z Size")] public float PlatformZSize { get; set; }
        [DisplayName("Max X Feedrate")] public ushort MaxXFeedrate { get; set; } = 200;
        [DisplayName("Max Y Feedrate")] public ushort MaxYFeedrate { get; set; } = 200;
        [DisplayName("Max Z Feedrate")] public ushort MaxZFeedrate { get; set; } = 200;
        [DisplayName("Machine Type")] public string MachineType { get; set; } = "UV_LCD";

        // ;(****UVtools Configuration ******)
        [DisplayName("Bottom Layer Light PWM")] public byte BottomLightPWM { get; set; } = 255;
        [DisplayName("Layer Light PWM")] public byte LightPWM { get; set; } = 255;
    }

    public class Slice
    {
        [DisplayName("xppm")] public float Xppm { get; set; } = 19.324f;
        [DisplayName("yppm")] public float Yppm { get; set; } = 19.324f;
        [DisplayName("xres")] public ushort Xres { get; set; }
        [DisplayName("yres")] public ushort Yres { get; set; }
        [DisplayName("thickness")] public float Thickness { get; set; }
        [DisplayName("layers_num")] public uint LayersNum { get; set; }
        [DisplayName("head_layers_num")] public ushort HeadLayersNum { get; set; } = 3;
        [DisplayName("layers_expo_ms")] public uint LayersExpoMs { get; set; } = 5500;
        [DisplayName("head_layers_expo_ms")] public uint HeadLayersExpoMs { get; set; } = 35000;
        [DisplayName("wait_before_expo_ms")] public uint WaitBeforeExpoMs { get; set; } = 2000;
        [DisplayName("lift_distance")] public float LiftDistance { get; set; } = 4;
        [DisplayName("lift_up_speed")] public float LiftUpSpeed { get; set; } = 120;
        [DisplayName("lift_down_speed")] public float LiftDownSpeed { get; set; } = 120;
        [DisplayName("lift_when_finished")] public byte LiftWhenFinished { get; set; } = 80;
    }



    #endregion

    #region Properties
    public Slice SliceSettings { get; } = new();
    public Output OutputSettings { get; } = new();
    public CWSSliceBuildConfig SliceBuildConfig { get; set; } = new();

    public override FileFormatType FileType => FileFormatType.Archive;

    public override string ConvertMenuGroup => "CWS";

    public override FileExtension[] FileExtensions { get; } =
    [
        new (typeof(CWSFile), "cws", "NovaMaker CWS"),
        new (typeof(CWSFile), "rgb.cws", "NovaMaker Bene4|5 Mono / Elfin2 Mono SE / Whale1|2 (RGB.CWS)"),
        new (typeof(CWSFile), "xml.cws", "Creation Workshop X (XML.CWS)")
    ];

    public override PrintParameterModifier[] PrintParameterModifiers { get; } =
    [
        PrintParameterModifier.BottomLayerCount,
        PrintParameterModifier.TransitionLayerCount,

        PrintParameterModifier.BottomWaitTimeBeforeCure,
        PrintParameterModifier.WaitTimeBeforeCure,

        PrintParameterModifier.BottomExposureTime,
        PrintParameterModifier.ExposureTime,

        PrintParameterModifier.BottomWaitTimeAfterCure,
        PrintParameterModifier.WaitTimeAfterCure,

        PrintParameterModifier.BottomLiftHeight,
        PrintParameterModifier.BottomLiftSpeed,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed,

        PrintParameterModifier.BottomLiftHeight2,
        PrintParameterModifier.BottomLiftSpeed2,
        PrintParameterModifier.LiftHeight2,
        PrintParameterModifier.LiftSpeed2,

        PrintParameterModifier.BottomWaitTimeAfterLift,
        PrintParameterModifier.WaitTimeAfterLift,

        PrintParameterModifier.BottomRetractSpeed,
        PrintParameterModifier.RetractSpeed,

        PrintParameterModifier.BottomRetractHeight2,
        PrintParameterModifier.BottomRetractSpeed2,
        PrintParameterModifier.RetractHeight2,
        PrintParameterModifier.RetractSpeed2,

        PrintParameterModifier.BottomLightPWM,
        PrintParameterModifier.LightPWM
    ];

    public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } =
    [
        PrintParameterModifier.PositionZ,
        PrintParameterModifier.WaitTimeBeforeCure,
        PrintParameterModifier.ExposureTime,
        PrintParameterModifier.WaitTimeAfterCure,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed,
        PrintParameterModifier.LiftHeight2,
        PrintParameterModifier.LiftSpeed2,
        PrintParameterModifier.WaitTimeAfterLift,
        PrintParameterModifier.RetractSpeed,
        PrintParameterModifier.RetractHeight2,
        PrintParameterModifier.RetractSpeed2,
        PrintParameterModifier.LightPWM
    ];

    public override uint ResolutionX
    {
        get => SliceSettings.Xres > 0 ? SliceSettings.Xres : SliceBuildConfig.XResolution;
        set
        {
            SliceBuildConfig.XResolution =
                OutputSettings.XResolution =
                    SliceSettings.Xres = (ushort) value;
            base.ResolutionX = value;
        }
    }

    public override uint ResolutionY
    {
        get => SliceSettings.Yres > 0 ? SliceSettings.Yres : SliceBuildConfig.YResolution;
        set
        {
            SliceBuildConfig.YResolution =
                OutputSettings.YResolution =
                    SliceSettings.Yres = (ushort) value;
            base.ResolutionY = value;
        }
    }

    public override float DisplayWidth
    {
        get => OutputSettings.PlatformXSize;
        set => base.DisplayWidth = OutputSettings.PlatformXSize = RoundDisplaySize(value);
    }

    public override float DisplayHeight
    {
        get => OutputSettings.PlatformYSize;
        set => base.DisplayHeight = OutputSettings.PlatformYSize = RoundDisplaySize(value);
    }

    public override float MachineZ
    {
        get => OutputSettings.PlatformZSize > 0 ? OutputSettings.PlatformZSize : base.MachineZ;
        set => base.MachineZ = OutputSettings.PlatformZSize = MathF.Round(value, 2);
    }

    public override FlipDirection DisplayMirror
    {
        get
        {
            if (OutputSettings is {FlipX: true, FlipY: true}) return FlipDirection.Both;
            if (OutputSettings.FlipX) return FlipDirection.Horizontally;
            if (OutputSettings.FlipY) return FlipDirection.Vertically;
            return FlipDirection.None;
        }
        set
        {
            OutputSettings.FlipX = value is FlipDirection.Horizontally or FlipDirection.Both;
            OutputSettings.FlipY = value is FlipDirection.Vertically or FlipDirection.Both;
            RaisePropertyChanged();
        }
    }

    public override byte AntiAliasing
    {
        get => (byte) OutputSettings.AntiAliasingValue;
        set
        {
            base.AntiAliasing = (byte)(OutputSettings.AntiAliasingValue = Math.Clamp(value, 1u, 16u));
            OutputSettings.AntiAliasing = OutputSettings.AntiAliasingValue > 1;
        }
    }

    public override float LayerHeight
    {
        get => SliceSettings.Thickness > 0 ? SliceSettings.Thickness : OutputSettings.LayerThickness;
        set => base.LayerHeight = OutputSettings.LayerThickness = SliceSettings.Thickness = Layer.RoundHeight(value);
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = OutputSettings.LayersNum = SliceSettings.LayersNum = base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => SliceSettings.HeadLayersNum;
        set => base.BottomLayerCount = SliceSettings.HeadLayersNum = value;
    }

    public override float BottomLightOffDelay
    {
        get => BottomWaitTimeBeforeCure;
        set => BottomWaitTimeBeforeCure = value;
    }

    public override float LightOffDelay
    {
        get => WaitTimeBeforeCure;
        set => WaitTimeBeforeCure = value;
    }

    public override float BottomWaitTimeBeforeCure
    {
        get => base.BottomWaitTimeBeforeCure;
        set => base.BottomWaitTimeBeforeCure = base.BottomLightOffDelay = value;
    }

    public override float WaitTimeBeforeCure
    {
        get => TimeConverter.MillisecondsToSeconds(SliceSettings.WaitBeforeExpoMs);
        set
        {
            SliceSettings.WaitBeforeExpoMs = TimeConverter.SecondsToMillisecondsUint(value);
            base.WaitTimeBeforeCure = base.LightOffDelay = value;
        }
    }

    public override float BottomExposureTime
    {
        get => TimeConverter.MillisecondsToSeconds(OutputSettings.BottomLayersTime);
        set
        {
            OutputSettings.BottomLayersTime =
                SliceSettings.HeadLayersExpoMs = TimeConverter.SecondsToMillisecondsUint(value);
            base.BottomExposureTime = value;
        }
    }

    public override float ExposureTime
    {
        get => TimeConverter.MillisecondsToSeconds(OutputSettings.LayerTime);
        set
        {
            OutputSettings.LayerTime =
                SliceSettings.LayersExpoMs = TimeConverter.SecondsToMillisecondsUint(value);
            base.ExposureTime = value;
        }
    }

    public override float LiftHeight
    {
        get => OutputSettings.LiftDistance;
        set => base.LiftHeight = OutputSettings.LiftDistance = SliceSettings.LiftDistance = MathF.Round(value, 2);
    }

    public override float BottomLiftSpeed
    {
        get => OutputSettings.ZBottomLiftFeedRate;
        set => base.BottomLiftSpeed = OutputSettings.ZBottomLiftFeedRate = MathF.Round(value, 2);
    }


    public override float LiftSpeed
    {
        get => OutputSettings.ZLiftFeedRate;
        set =>
            base.LiftSpeed =
                OutputSettings.ZLiftFeedRate =
                    SliceSettings.LiftUpSpeed = MathF.Round(value, 2);
    }

    public override float RetractSpeed
    {
        get => OutputSettings.ZLiftRetractRate;
        set => base.RetractSpeed = OutputSettings.ZLiftRetractRate = SliceSettings.LiftDownSpeed = MathF.Round(value, 2);
    }

    public override byte BottomLightPWM
    {
        get => OutputSettings.BottomLightPWM;
        set => base.BottomLightPWM = OutputSettings.BottomLightPWM = value;
    }

    public override byte LightPWM
    {
        get => OutputSettings.LightPWM;
        set => base.LightPWM = OutputSettings.LightPWM = value;
    }


    public override object[] Configs => LayerImageFormat == ImageFormat.Png32 ? [SliceBuildConfig, OutputSettings] :
    [
        SliceSettings, OutputSettings
    ];
    #endregion

    #region Constructor
    public CWSFile()
    {
        GCode = new GCodeBuilder
        {
            UseComments = true,
            GCodePositioningType = GCodeBuilder.GCodePositioningTypes.Relative,
            GCodeSpeedUnit = GCodeBuilder.GCodeSpeedUnits.MillimetersPerMinute,
            GCodeTimeUnit = GCodeBuilder.GCodeTimeUnits.Milliseconds,
            GCodeShowImageType = GCodeBuilder.GCodeShowImageTypes.LayerIndex0Started,
            //GCodeShowImagePosition = GCodeBuilder.GCodeShowImagePositions.WhenRequired,
            LayerMoveCommand = GCodeBuilder.GCodeMoveCommands.G1,
            EndGCodeMoveCommand = GCodeBuilder.GCodeMoveCommands.G1,
            CommandSyncMovements =
            {
                Enabled = true
            },
            CommandWaitSyncDelay =
            {
                Enabled = true,
            }
        };

        GCode.CommandShowImageM6054.Set(";<Slice>", "{0}");
        GCode.CommandWaitSyncDelay.Set(";<Delay>", "0{0}");
        GCode.CommandWaitG4.Set(";<Delay>", "{0}");
    }
    #endregion

    #region Methods

    protected override void OnBeforeEncode(bool isPartialEncode)
    {
        SliceSettings.Xppm = Xppmm;
        SliceSettings.Yppm = Yppmm;

        if (!isPartialEncode)
        {
            if (FileEndsWith(".rgb.cws"))
            {
                LayerImageFormat = ImageFormat.Png24BgrAA;
            }
            if (FileEndsWith(".xml.cws"))
            {
                LayerImageFormat = ImageFormat.Png32;
            }
        }
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        var filename = About.Software;

        if (char.IsDigit(filename[^1]))
        {
            throw new InvalidOperationException($"Filename for this format should not end with a digit: {filename}");
        }

        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Create);
        if (LayerImageFormat == ImageFormat.Png32)
        {
            var manifest = new CWSManifest
            {
                GCode = {Name = $"{filename}.gcode"},
                Slices = new CWSManifest.Slice[LayerCount],
                SliceProfile = {Name = $"{filename}.slicing"}
            };

            for (int layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                manifest.Slices[layerIndex] = new CWSManifest.Slice(this[layerIndex].FormatFileNameWithLayerDigits(filename));
            }

            using (var stream = outputFile.CreateEntryStream(CWSManifest.FileName))
            {
                XmlExtensions.Serialize(manifest, stream, XmlExtensions.SettingsIndent, true);
            }

            using (var stream = outputFile.CreateEntryStream($"{filename}.slicing"))
            {
                XmlExtensions.Serialize(SliceBuildConfig, stream, XmlExtensions.SettingsIndent, true);
            }
        }
        else
        {
            string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
            using var stream = outputFile.CreateEntryStream("slice.conf");
            using TextWriter tw = new StreamWriter(stream);
            tw.WriteLine($"# {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.UtcNow}");
            tw.WriteLine("# conf version 1.0");
            tw.WriteLine("");

            foreach (var propertyInfo in SliceSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var displayNameAttribute = propertyInfo.GetCustomAttributes(false).AsValueEnumerable().OfType<DisplayNameAttribute>().FirstOrDefault();
                if (displayNameAttribute is null) continue;
                tw.WriteLine($"{displayNameAttribute.DisplayName.PadRight(24)}= {propertyInfo.GetValue(SliceSettings)}");
            }
        }

        EncodeLayersInZip(outputFile, filename, LayerDigits, IndexStartNumber.Zero, progress);

        RebuildGCode();
        outputFile.CreateEntryFromContent($"{filename}.gcode", GCodeStr, ZipArchiveMode.Create);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Read);
        var entry = inputFile.GetEntry("manifest.xml");
        if (entry is not null) // Wanhao
        {
            //DecodeXML(fileFullPath, inputFile, progress);
            LayerImageFormat = ImageFormat.Png32;

            try
            {
                using var stream = entry.Open();
                var manifest = XmlExtensions.DeserializeFromStream<CWSManifest>(stream);
                if (manifest.Slices is null)
                {
                    throw new NullReferenceException("Slices information are not present on the manifest file");
                }
                OutputSettings.LayersNum = (uint) manifest.Slices.Length;
            }
            catch (Exception e)
            {
                Clear();
                throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
            }


            entry = inputFile.Entries.AsValueEnumerable().FirstOrDefault(e => e.Name.EndsWith(".slicing"));

            if (entry is not null)
            {
                //Clear();
                //throw new FileLoadException(".slicing file not found, unable to proceed.", fileFullPath);
                try
                {
                    using var stream = entry.Open();
                    SliceBuildConfig = XmlExtensions.DeserializeFromStream<CWSSliceBuildConfig>(stream);
                }
                catch (Exception e)
                {
                    Clear();
                    throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
                }
            }
        }
        else // Novamaker
        {
            entry = inputFile.GetEntry("slice.conf");
            if (entry is null)
            {
                Clear();
                throw new FileLoadException("slice.conf not found", FileFullPath);
            }

            using TextReader tr = new StreamReader(entry.Open());
            while (tr.ReadLine() is { } line)
            {
                line = line.Replace("# ", string.Empty);
                if (string.IsNullOrEmpty(line)) continue;
                //if(line[0] == '#') continue;

                var splitLine = line.Split('=');
                if (splitLine.Length < 2) continue;

                foreach (var propertyInfo in SliceSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var displayNameAttribute = propertyInfo.GetCustomAttributes(false).AsValueEnumerable().OfType<DisplayNameAttribute>().FirstOrDefault();
                    if (displayNameAttribute is null) continue;
                    if (!splitLine[0].Trim().Equals(displayNameAttribute.DisplayName)) continue;
                    propertyInfo.SetValueFromString(SliceSettings, splitLine[1].Trim());
                }
            }
            tr.Close();
        }

        entry = inputFile.Entries.AsValueEnumerable().FirstOrDefault(e => e.Name.EndsWith(".gcode"));
        if (entry is null)
        {
            Clear();
            throw new FileLoadException("Unable to find .gcode file", FileFullPath);
        }

        using (TextReader tr = new StreamReader(entry.Open()))
        {
            GCode!.Clear();
            while (tr.ReadLine() is { } line)
            {
                GCode.AppendLine(line);
                if (string.IsNullOrEmpty(line)) continue;

                if (line[0] != ';')
                {
                    continue;
                }

                var splitLine = line.Split('=');
                if (splitLine.Length < 2) continue;

                foreach (var propertyInfo in OutputSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var displayNameAttribute = propertyInfo.GetCustomAttributes(false).AsValueEnumerable().OfType<DisplayNameAttribute>().FirstOrDefault();
                    if (displayNameAttribute is null) continue;
                    if (!splitLine[0].Trim(' ', ';', '(').Equals(displayNameAttribute.DisplayName)) continue;
                    try
                    {
                        propertyInfo.SetValueFromString(OutputSettings, splitLine[1].Trim(' ', ')', 'p', 'x', 'm', 'n', 's', '/'));
                    }
                    catch
                    {
                        // ignored
                    }

                    //Debug.WriteLine(splitLine[1].Trim(' ', ')', 'm', 'n', '/'));
                }
            }
            tr.Close();
        }

        Init(OutputSettings.LayersNum, DecodeType == FileDecodeType.Partial);

        if (LayerCount <= 0) return;

        // Must discover png depth grayscale or color
        if (DecodeType == FileDecodeType.Full)
        {
            LayerImageFormat = FetchImageFormat(inputFile, ImageFormat.Png24BgrAA);
        }

        DecodeLayersFromZipIgnoreFilename(inputFile, progress);
        GCode.ParseLayersFromGCode(this);
    }

    public override void RebuildGCode()
    {
        if (!SupportGCode || SuppressRebuildGCode) return;

        switch (LayerImageFormat)
        {
            case ImageFormat.Png32:
                GCode!.CommandWaitSyncDelay.Command = ";<Takes>";
                break;
            default:
                GCode!.CommandWaitSyncDelay.Command = ";<Delay>";
                break;
        }

        //string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
        //GCode.Clear();
        //GCode.AppendLine($"; {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.UtcNow}");
        StringBuilder sb = new();
        sb.AppendLine(";(**** Build and Slicing Parameters ****)");

        foreach (var propertyInfo in OutputSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var displayNameAttribute = propertyInfo.GetCustomAttributes(false).AsValueEnumerable().OfType<DisplayNameAttribute>().FirstOrDefault();
            if (displayNameAttribute is null) continue;
            if (propertyInfo.Name.Equals(nameof(OutputSettings.LayersNum)))
            {
                sb.AppendLine($";{displayNameAttribute.DisplayName} = {propertyInfo.GetValue(OutputSettings)}");
            }
            else
            {
                sb.AppendLine($";({displayNameAttribute.DisplayName} = {propertyInfo.GetValue(OutputSettings)})");
            }
        }

        GCode?.RebuildGCode(this, sb);
        /*GCode.AppendLine();

        GCode.AppendFormat(Printer == PrinterType.Wanhao ? WanhaoStartGCode : GCodeStart, Environment.NewLine);

        float lastZPosition = 0;

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            Layer layer = this[layerIndex];

            if (Printer == PrinterType.Wanhao)
            {
                GCode.AppendFormat(WanhaoPreSliceGCode, Environment.NewLine, layerIndex, OutputSettings.SettleTime);
            }

            GCode.AppendLine($"{GCodeKeywordSlice} {layerIndex}");
            if (layer.ExposureTime > 0 && layer.LightPWM > 0)
            {
                GCode.AppendLine($"M106 S{layer.LightPWM} ; UV on");
                GCode.AppendLine($"{GCodeKeywordDelay} {layer.ExposureTime * 1000}");
                GCode.AppendLine("M106 S0 ; UV off");
            }
            GCode.AppendLine(GCodeKeywordSliceBlank);

            if (Printer == PrinterType.Wanhao)
            {
                GCode.AppendLine($"{GCodeKeywordDelay} 0");
            }

            if (lastZPosition != layer.PositionZ)
            {
                if (layer.LiftHeight > 0)
                {
                    float downPos = Layer.RoundHeight(layer.LiftHeight - layer.PositionZ + lastZPosition);
                    if (Printer == PrinterType.Wanhao)
                    {
                        GCode.AppendLine($";********** Lift Sequence {layerIndex} ********");
                    }
                    GCode.AppendLine($"G1 Z{layer.LiftHeight} F{layer.LiftSpeed}");
                    if (Printer == PrinterType.Wanhao)
                    {
                        GCode.AppendLine($"{GCodeKeywordTakes} {OperationCalculator.LightOffDelayC.CalculateMillisecondsLiftOnly(layer.LiftHeight, layer.LiftSpeed)}");
                        GCode.AppendLine($"G4 P0 ; Wait for lift rise to complete");
                        GCode.AppendLine($"{GCodeKeywordDelay} 0");
                    }
                    GCode.AppendLine($"G1 Z-{downPos} F{layer.RetractSpeed}");
                    if (Printer == PrinterType.Wanhao)
                    {
                        GCode.AppendLine($"{GCodeKeywordTakes} {OperationCalculator.LightOffDelayC.CalculateMillisecondsLiftOnly(downPos, layer.RetractSpeed)}");
                    }
                }
                else
                {
                    GCode.AppendLine($"G1 Z{Layer.RoundHeight(layer.PositionZ - lastZPosition)} F{layer.LiftSpeed}");
                }
            }

            if (Printer != PrinterType.Wanhao)
            {
                // delay = max(extra['wait'], 500) + int(((int(lift)/(extra['lift_feed']/60)) + (int(lift)/(extra['lift_retract']/60)))*1000)
                /*uint extraDelay =
                    OperationCalculator.LightOffDelayC.CalculateMilliseconds(layer.LiftHeight, layer.LiftSpeed,
                        layer.RetractSpeed);
                if (layerIndex < BottomLayerCount)
                {
                    extraDelay = (uint) Math.Max(extraDelay + 10000, layer.ExposureTime * 1000);
                }
                else
                {
                    extraDelay += Math.Max(OutputSettings.BlankingLayerTime, 500);
                }

                GCode.AppendLine($"{GCodeKeywordDelay} {layer.LightOffDelay * 1000}");
                GCode.AppendLine();
            }

            lastZPosition = layer.PositionZ;
        }

        GCode.AppendFormat(Printer == PrinterType.Wanhao ? WanhaoGCodeEnd : GCodeEnd, Environment.NewLine, SliceSettings.LiftWhenFinished);*/
        RaisePropertyChanged(nameof(GCodeStr));
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Update);
        var arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
        using var stream = outputFile.GetOrCreateStream("slice.conf");
        stream.SetLength(0);

        using (TextWriter tw = new StreamWriter(stream))
        {

            tw.WriteLine($"# {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.UtcNow}");
            tw.WriteLine("# conf version 1.0");
            tw.WriteLine("");

            foreach (var propertyInfo in SliceSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var displayNameAttribute = propertyInfo.GetCustomAttributes(false).AsValueEnumerable().OfType<DisplayNameAttribute>().FirstOrDefault();
                if (displayNameAttribute is null) continue;
                tw.WriteLine($"{displayNameAttribute.DisplayName.PadRight(24)}= {propertyInfo.GetValue(SliceSettings)}");
            }
        }

        var entriesToRemove = outputFile.Entries.AsValueEnumerable().Where(zipEntry => zipEntry.Name.EndsWith(".gcode")).ToArray();
        foreach (var zipEntry in entriesToRemove)
        {
            zipEntry.Delete();
        }

        outputFile.CreateEntryFromContent($"{About.Software}.gcode", GCodeStr, ZipArchiveMode.Update);

        /*foreach (var layer in this)
            {
                if (!layer.IsModified) continue;
                outputFile.PutFileContent(layer.Filename, layer.CompressedBytes, ZipArchiveMode.Update);
                layer.IsModified = false;
            }*/

        //Decode(FileFullPath, progress);
    }



    #endregion
}