/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

#region Wanhao

[Serializable]
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

[Serializable]
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

public class CWSFile : FileFormat
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

    public enum PrinterType : byte
    {
        Unknown,
        Elfin,
        BeneMono,
        Wanhao,
    }

    public PrinterType Printer { get; set; } = PrinterType.Unknown;

    public override FileExtension[] FileExtensions { get; } = {
        new (typeof(CWSFile), "cws", "NovaMaker CWS"),
        new (typeof(CWSFile), "rgb.cws", "NovaMaker Bene4 Mono / Elfin2 Mono SE (CWS)"),
        new (typeof(CWSFile), "xml.cws", "Creation Workshop X (CWS)"),
    };

    public override PrintParameterModifier[]? PrintParameterModifiers { get; } = {
        PrintParameterModifier.BottomLayerCount,

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
        PrintParameterModifier.LightPWM,
    };

    public override PrintParameterModifier[]? PrintParameterPerLayerModifiers { get; } = {
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
        PrintParameterModifier.LightPWM,
    };

    public override Size[]? ThumbnailsOriginalSize => null;

    public override uint ResolutionX
    {
        get => SliceSettings.Xres > 0 ? SliceSettings.Xres : SliceBuildConfig.XResolution;
        set
        {
            SliceBuildConfig.XResolution = 
                OutputSettings.XResolution = 
                    SliceSettings.Xres = (ushort) value;
            RaisePropertyChanged();
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
            RaisePropertyChanged();
        }
    }

    public override float DisplayWidth
    {
        get => OutputSettings.PlatformXSize;
        set
        {
            OutputSettings.PlatformXSize = (float)Math.Round(value, 2);
            RaisePropertyChanged();
        }
    }

    public override float DisplayHeight
    {
        get => OutputSettings.PlatformYSize;
        set
        {
            OutputSettings.PlatformYSize = (float)Math.Round(value, 2);
            RaisePropertyChanged();
        }
    }

    public override float MachineZ
    {
        get => OutputSettings.PlatformZSize > 0 ? OutputSettings.PlatformZSize : base.MachineZ;
        set => base.MachineZ = OutputSettings.PlatformZSize = (float)Math.Round(value, 2);
    }

    public override Enumerations.FlipDirection DisplayMirror
    {
        get
        {
            if (OutputSettings.FlipX && OutputSettings.FlipY) return Enumerations.FlipDirection.Both;
            if (OutputSettings.FlipX) return Enumerations.FlipDirection.Horizontally;
            if (OutputSettings.FlipY) return Enumerations.FlipDirection.Vertically;
            return Enumerations.FlipDirection.None;
        }
        set
        {
            OutputSettings.FlipX = value is Enumerations.FlipDirection.Horizontally or Enumerations.FlipDirection.Both;
            OutputSettings.FlipY = value is Enumerations.FlipDirection.Vertically or Enumerations.FlipDirection.Both;
            RaisePropertyChanged();
        }
    }

    public override byte AntiAliasing
    {
        get => (byte) OutputSettings.AntiAliasingValue;
        set
        {
            base.AntiAliasing = (byte)(OutputSettings.AntiAliasingValue = value.Clamp(1, 16));
            OutputSettings.AntiAliasing = OutputSettings.AntiAliasingValue > 1;
        }
    }

    public override float LayerHeight
    {
        get => SliceSettings.Thickness > 0 ? SliceSettings.Thickness : OutputSettings.LayerThickness;
        set
        {
            OutputSettings.LayerThickness = SliceSettings.Thickness = Layer.RoundHeight(value);
            RaisePropertyChanged();
        }
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
        get => TimeExtensions.MillisecondsToSeconds(SliceSettings.WaitBeforeExpoMs);
        set
        {
            SliceSettings.WaitBeforeExpoMs = TimeExtensions.SecondsToMillisecondsUint(value);
            base.WaitTimeBeforeCure = base.LightOffDelay = value;
        }
    }

    public override float BottomExposureTime
    {
        get => TimeExtensions.MillisecondsToSeconds(OutputSettings.BottomLayersTime);
        set
        {
            OutputSettings.BottomLayersTime =
                SliceSettings.HeadLayersExpoMs = TimeExtensions.SecondsToMillisecondsUint(value);
            base.BottomExposureTime = value;
        }
    }

    public override float ExposureTime
    {
        get => TimeExtensions.MillisecondsToSeconds(OutputSettings.LayerTime);
        set
        {
            OutputSettings.LayerTime = 
                SliceSettings.LayersExpoMs = TimeExtensions.SecondsToMillisecondsUint(value);
            base.ExposureTime = value;
        }
    }

    public override float LiftHeight
    {
        get => OutputSettings.LiftDistance;
        set => base.LiftHeight = OutputSettings.LiftDistance = SliceSettings.LiftDistance = (float)Math.Round(value, 2);
    }

    public override float BottomLiftSpeed
    {
        get => OutputSettings.ZBottomLiftFeedRate;
        set => base.BottomLiftSpeed = OutputSettings.ZBottomLiftFeedRate = (float)Math.Round(value, 2);
    }
        

    public override float LiftSpeed
    {
        get => OutputSettings.ZLiftFeedRate;
        set =>
            base.LiftSpeed = 
                OutputSettings.ZLiftFeedRate =
                    SliceSettings.LiftUpSpeed = (float)Math.Round(value, 2);
    }

    public override float RetractSpeed
    {
        get => OutputSettings.ZLiftRetractRate;
        set => base.RetractSpeed = OutputSettings.ZLiftRetractRate = SliceSettings.LiftDownSpeed = (float)Math.Round(value, 2);
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


    public override object[] Configs => Printer == PrinterType.Wanhao ? new object[] { SliceBuildConfig, OutputSettings } : new object[] { SliceSettings, OutputSettings};
    #endregion

    #region Constructor
    public CWSFile()
    {
        GCode = new GCodeBuilder
        {
            SyncMovementsWithDelay = true,
            UseComments = true,
            GCodePositioningType = GCodeBuilder.GCodePositioningTypes.Partial,
            GCodeSpeedUnit = GCodeBuilder.GCodeSpeedUnits.MillimetersPerMinute,
            GCodeTimeUnit = GCodeBuilder.GCodeTimeUnits.Milliseconds,
            GCodeShowImageType = GCodeBuilder.GCodeShowImageTypes.LayerIndex0Started,
            LayerMoveCommand = GCodeBuilder.GCodeMoveCommands.G1,
            EndGCodeMoveCommand = GCodeBuilder.GCodeMoveCommands.G1
        };
        GCode.CommandShowImageM6054.Set(";<Slice>", "{0}");
        GCode.CommandWaitG4.Set(";<Delay>", "{0}");
    }
    #endregion

    #region Methods

    protected override void EncodeInternally(OperationProgress progress)
    {
        //var filename = fileFullPath.EndsWith(TemporaryFileAppend) ? Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(fileFullPath)) : Path.GetFileNameWithoutExtension(fileFullPath);

        if (Printer == PrinterType.Unknown)
        {
            Printer = PrinterType.Elfin;
            for (int i = 0; i < FileExtensions.Length; i++)
            {
                if (!FileEndsWith(FileExtensions[i].Extension)) continue;
                Printer = (PrinterType) i+1;
            }
        }

        if (Printer == PrinterType.BeneMono)
        {
            if (ResolutionX % 3 != 0)
            {
                throw new InvalidOperationException($"Resolution width of {ResolutionX}px is invalid. Width must be in multiples of 3.\n" +
                                                    "Fix your printer slicing settings with the correct width that is multiple of 3.");
            }
        }

        /*var filename = Path.GetFileNameWithoutExtension(fileFullPath);
        if (fileFullPath.EndsWith(TemporaryFileAppend))
        {
            filename = Path.GetFileNameWithoutExtension(filename);
        }*/
        var filename = About.Software;

        if (char.IsDigit(filename[^1]))
        {
            throw new InvalidOperationException($"Filename for this format should not end with a digit: {filename}");
        }

        using var outputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Create);
        if (Printer == PrinterType.Wanhao)
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

            var entry = outputFile.CreateEntry(CWSManifest.FileName);
            using (var stream = entry.Open())
            {
                XmlExtensions.Serialize(manifest, stream, XmlExtensions.SettingsIndent, true);
            }

            entry = outputFile.CreateEntry($"{filename}.slicing");
            using (var stream = entry.Open())
            {
                XmlExtensions.Serialize(SliceBuildConfig, stream, XmlExtensions.SettingsIndent, true);
            }
        }
        else
        {
            string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
            var entry = outputFile.CreateEntry("slice.conf");

            using TextWriter tw = new StreamWriter(entry.Open());
            tw.WriteLine($"# {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.UtcNow}");
            tw.WriteLine("# conf version 1.0");
            tw.WriteLine("");

            foreach (var propertyInfo in SliceSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                if (displayNameAttribute is null) continue;
                tw.WriteLine($"{displayNameAttribute.DisplayName.PadRight(24)}= {propertyInfo.GetValue(SliceSettings)}");
            }
        }

        if (Printer == PrinterType.BeneMono)
        {
            EncodeLayersInZip(outputFile, filename, LayerDigits, Enumerations.IndexStartNumber.Zero, progress, matGenFunc:
                (_, mat) =>
                {
                    var matEncode = new Mat(mat.Height, mat.GetRealStep() / 3, DepthType.Cv8U, 3);
                    var span = mat.GetDataByteSpan();
                    var spanEncode = matEncode.GetDataByteSpan();
                    for (int i = 0; i < span.Length; i++)
                    {
                        spanEncode[i] = span[i];
                    }

                    return matEncode;
                });
            /*Parallel.For(0, LayerCount, CoreSettings.GetParallelOptions(progress),
                //new ParallelOptions { MaxDegreeOfParallelism = Printer == PrinterType.BeneMono ? 1 : 1 },
                layerIndex =>
                {
                    var layer = this[layerIndex];
                    var layerImagePath = layer.FormatFileNameWithLayerDigits(filename);

                    using var mat = layer.LayerMat;
                    using var matEncode = new Mat(mat.Height, mat.GetRealStep() / 3, DepthType.Cv8U, 3);
                    var span = mat.GetDataByteSpan();
                    var spanEncode = matEncode.GetDataByteSpan();
                    for (int i = 0; i < span.Length; i++)
                    {
                        spanEncode[i] = span[i];
                    }

                    var bytes = matEncode.GetPngByes();
                    lock (progress.Mutex)
                    {
                        outputFile.PutFileContent(layerImagePath, bytes, ZipArchiveMode.Create);
                        progress++;
                    }
                });*/
        }
        else
        {
            EncodeLayersInZip(outputFile, filename, LayerDigits, Enumerations.IndexStartNumber.Zero, progress);
        }

        RebuildGCode();
        outputFile.PutFileContent($"{filename}.gcode", GCodeStr, ZipArchiveMode.Create);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Read);
        var entry = inputFile.GetEntry("manifest.xml");
        if (entry is not null) // Wanhao
        {
            //DecodeXML(fileFullPath, inputFile, progress);
            Printer = PrinterType.Wanhao;

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
                    

            entry = inputFile.Entries.FirstOrDefault(e => e.Name.EndsWith(".slicing"));

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
            string? line;
            while ((line = tr.ReadLine()) != null)
            {
                line = line.Replace("# ", string.Empty);
                if (string.IsNullOrEmpty(line)) continue;
                //if(line[0] == '#') continue;

                var splitLine = line.Split('=');
                if (splitLine.Length < 2) continue;

                foreach (var propertyInfo in SliceSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                    if (displayNameAttribute is null) continue;
                    if (!splitLine[0].Trim().Equals(displayNameAttribute.DisplayName)) continue;
                    Helpers.SetPropertyValue(propertyInfo, SliceSettings, splitLine[1].Trim());
                }
            }
            tr.Close();
        }

        entry = inputFile.Entries.FirstOrDefault(e => e.Name.EndsWith(".gcode"));
        if (entry is null)
        {
            Clear();
            throw new FileLoadException("Unable to find .gcode file", FileFullPath);
        }

        using (TextReader tr = new StreamReader(entry.Open()))
        {
            string? line;
            GCode!.Clear();
            while ((line = tr.ReadLine()) != null)
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
                    var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                    if (displayNameAttribute is null) continue;
                    if (!splitLine[0].Trim(' ', ';', '(').Equals(displayNameAttribute.DisplayName)) continue;
                    try
                    {
                        Helpers.SetPropertyValue(propertyInfo, OutputSettings, splitLine[1].Trim(' ', ')', 'p', 'x', 'm', 'n', 's', '/'));
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
        if (DecodeType == FileDecodeType.Full && Printer == PrinterType.Unknown)
        {
            var inputFilename = Path.GetFileNameWithoutExtension(FileFullPath)!;
            foreach (var pngEntry in inputFile.Entries)
            {
                if (!pngEntry.Name.EndsWith(".png")) continue;
                var match = Regex.Match(pngEntry.Name, @"(\d+).png");
                if (!match.Success || match.Groups.Count < 2) continue;
                if (!uint.TryParse(match.Groups[1].Value, out var layerIndex)) continue;
                
                /*var filename = Path.GetFileNameWithoutExtension(pngEntry.Name).Replace(inputFilename, string.Empty, StringComparison.Ordinal);

                var layerIndexStr = string.Empty;
                var layerStr = filename;
                for (int i = layerStr.Length - 1; i >= 0; i--)
                {
                    if (layerStr[i] < '0' || layerStr[i] > '9') break;
                    layerIndexStr = $"{layerStr[i]}{layerIndexStr}";
                }

                if (string.IsNullOrEmpty(layerIndexStr)) continue;
                if (!uint.TryParse(layerIndexStr, out var layerIndex)) continue;*/
                
                using var stream = pngEntry.Open();
                using var mat = new Mat();
                CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, mat);
                Printer = mat.NumberOfChannels == 1 ? PrinterType.Elfin : PrinterType.BeneMono;
                break;
            }
        }

        if (Printer == PrinterType.BeneMono)
        {
            DecodeLayersFromZipRegex(inputFile, @"(\d+).png", Enumerations.IndexStartNumber.Zero, progress,
                (layerIndex, pngBytes) =>
                {
                    using Mat mat = new();
                    CvInvoke.Imdecode(pngBytes, ImreadModes.AnyColor, mat);
                    var matDecode = new Mat(mat.Height, mat.GetRealStep(), DepthType.Cv8U, 1);
                    var span = mat.GetDataByteSpan();
                    var spanDecode = matDecode.GetDataByteSpan();
                    for (int i = 0; i < span.Length; i++)
                    {
                        spanDecode[i] = span[i];
                    }

                    return matDecode;
                });
        }
        else
        {
            DecodeLayersFromZipRegex(inputFile, @"(\d+).png", Enumerations.IndexStartNumber.Zero, progress);
        }

        GCode.ParseLayersFromGCode(this);
        
    }

    public override void RebuildGCode()
    {
        if (!SupportsGCode || SuppressRebuildGCode) return;
        //string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
        //GCode.Clear();
        //GCode.AppendLine($"; {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.UtcNow}");
        StringBuilder sb = new();
        sb.AppendLine(";(**** Build and Slicing Parameters ****)");

        foreach (var propertyInfo in OutputSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
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
        using var outputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Update);
        var arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
        var entry = outputFile.GetPutFile("slice.conf");
        var stream = entry.Open();
        stream.SetLength(0);

        using (TextWriter tw = new StreamWriter(stream))
        {

            tw.WriteLine($"# {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.UtcNow}");
            tw.WriteLine("# conf version 1.0");
            tw.WriteLine("");

            foreach (var propertyInfo in SliceSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                if (displayNameAttribute is null) continue;
                tw.WriteLine($"{displayNameAttribute.DisplayName.PadRight(24)}= {propertyInfo.GetValue(SliceSettings)}");
            }
        }

        var entriesToRemove = outputFile.Entries.Where(zipEntry => zipEntry.Name.EndsWith(".gcode")).ToArray();
        foreach (var zipEntry in entriesToRemove)
        {
            zipEntry.Delete();
        }

        outputFile.PutFileContent($"{About.Software}.gcode", GCodeStr, ZipArchiveMode.Update);

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