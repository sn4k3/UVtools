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
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using UVtools.Core.Converters;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public sealed class JXSFile : FileFormat
{
    #region Constants

    public const uint RESOLUTION_X = 4920;
    public const uint RESOLUTION_Y = 2880;

    public const string ConfigFileName = "config.ini";
    public const string ControlFilename = "control.json";

    #endregion

    #region Sub Classes

    public sealed class JXSConfig
    {
        public string Action { get; set; } = "print";

        [DisplayName("FirstLayerTime")] public uint BottomExposureTimeMs { get; set; } = TimeConverter.SecondsToMillisecondsUint(DefaultBottomExposureTime);
        [DisplayName("LayerTime")] public uint ExposureTimeMs { get; set; } = TimeConverter.SecondsToMillisecondsUint(DefaultExposureTime);
        public uint NumFade { get; set; }
        [DisplayName("NumLayers")] public uint LayerCount { get; set; }
        [DisplayName("UsedMaterial")] public float MaterialMl { get; set; }

        [DisplayName("layerHeight")] public float LayerHeight { get; set; }

        [DisplayName("nJobName")] public string JobName { get; set; } = string.Empty;
        [DisplayName("NumBottomLayers")] public ushort BottomLayerCount { get; set; } = DefaultBottomLayerCount;
        [DisplayName("LiftDistance1")] public float LiftHeight1 { get; set; } = DefaultBottomLiftHeight;
        [DisplayName("LiftFeedrate1")] public float LiftSpeed1 { get; set; } = DefaultLiftSpeed;
        [DisplayName("LiftDistance2")] public float LiftHeight2 { get; set; } = DefaultLiftHeight2;
        [DisplayName("LiftFeedrate2")] public float LiftSpeed2 { get; set; } = DefaultLiftSpeed2;
        [DisplayName("BottomLiftFeedrate")] public float BottomLiftSpeed { get; set; } = DefaultBottomLiftSpeed;
        [DisplayName("RetractFeedrate")] public float RetractSpeed { get; set; } = DefaultRetractSpeed;
        [DisplayName("ZMoveTimeCompensation")] public uint WaitTimeBeforeCure { get; set; } = 1500;
        [DisplayName("ZMoveTimeCompensationBottom")] public uint BottomWaitTimeBeforeCure { get; set; } = 8000;
        public string OnLightCode { get; set; } = "M106 S255";
        public string OffLightCode { get; set; } = "M106 S0";
        public string GcodeHeader { get; set; } = "G21|G91|M17|M106 S0";
        public string GcodeFooter { get; set; } = "G0 Z100 F150";
        public string ApplicationName { get; set; } = "GKone-Slicer";
    }

    public sealed class JXSControl
    {
        [JsonPropertyName("total_time")] public float PrintTime { get; set; }

        [JsonPropertyName("total_slices")] public uint LayerCount { get; set; }

        [JsonPropertyName("used_material")] public float MaterialMl { get; set; }

        [JsonPropertyName("action_list")] public List<object[]> Actions { get; set; } = new();
    }
    #endregion

    #region Properties
    public JXSConfig ConfigFile { get; set; } = new();
    public JXSControl ControlFile { get; set; } = new();

    public override FileFormatType FileType => FileFormatType.Archive;

    public override FileExtension[] FileExtensions { get; } = {
        new(typeof(JXSFile), "jxs", "Uniformation GKone (JXS)")
    };

    public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
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
        PrintParameterModifier.LightPWM,
    };

    public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
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

    //public override Size[]? ThumbnailsOriginalSize { get; } = {new(640, 480)};

    public override uint ResolutionX
    {
        get => base.ResolutionX;
        set
        {
            if (value != RESOLUTION_X) throw new ArgumentOutOfRangeException(nameof(ResolutionX), $"{nameof(ResolutionX)} can not be different from {RESOLUTION_X}");
            base.ResolutionX = value;
        }
    }

    public override uint ResolutionY
    {
        get => base.ResolutionY;
        set
        {
            if (value != RESOLUTION_Y) throw new ArgumentOutOfRangeException(nameof(ResolutionY), $"{nameof(ResolutionY)} can not be different from {RESOLUTION_Y}");
            base.ResolutionY = value;
        }
    }

    public override FlipDirection DisplayMirror => FlipDirection.Vertically;

    public override float LayerHeight
    {
        get => ConfigFile.LayerHeight;
        set => base.LayerHeight = ConfigFile.LayerHeight = Layer.RoundHeight(value);
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = ConfigFile.LayerCount = ControlFile.LayerCount = base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => ConfigFile.BottomLayerCount;
        set => base.BottomLayerCount = ConfigFile.BottomLayerCount = value;
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
        get => TimeConverter.MillisecondsToSeconds(ConfigFile.BottomWaitTimeBeforeCure);
        set
        {
            ConfigFile.BottomWaitTimeBeforeCure = TimeConverter.SecondsToMillisecondsUint(value);
            base.BottomWaitTimeBeforeCure = base.BottomLightOffDelay = value;
        }
    }

    public override float WaitTimeBeforeCure
    {
        get => TimeConverter.MillisecondsToSeconds(ConfigFile.WaitTimeBeforeCure);
        set
        {
            ConfigFile.WaitTimeBeforeCure = TimeConverter.SecondsToMillisecondsUint(value);
            base.WaitTimeBeforeCure = base.LightOffDelay = value;
        }
    }

    public override float BottomExposureTime
    {
        get => TimeConverter.MillisecondsToSeconds(ConfigFile.BottomExposureTimeMs);
        set
        {
            ConfigFile.BottomExposureTimeMs = TimeConverter.SecondsToMillisecondsUint(value);
            base.BottomExposureTime = value;
        }
    }

    public override float ExposureTime
    {
        get => TimeConverter.MillisecondsToSeconds(ConfigFile.ExposureTimeMs);
        set
        {
            ConfigFile.ExposureTimeMs = TimeConverter.SecondsToMillisecondsUint(value);
            base.ExposureTime = value;
        }
    }

    public override float BottomLiftHeight
    {
        get => ConfigFile.LiftHeight1;
        set => base.BottomLiftHeight = MathF.Round(value, 2);
    }

    public override float LiftHeight
    {
        get => ConfigFile.LiftHeight1;
        set => base.LiftHeight = ConfigFile.LiftHeight1 = MathF.Round(value, 2);
    }

    public override float BottomLiftSpeed
    {
        get => ConfigFile.BottomLiftSpeed;
        set => base.BottomLiftSpeed = ConfigFile.BottomLiftSpeed = MathF.Round(value, 2);
    }

    public override float LiftSpeed
    {
        get => ConfigFile.LiftSpeed1;
        set => base.LiftSpeed = ConfigFile.LiftSpeed1 = MathF.Round(value, 2);
    }

    public override float PrintTime
    {
        get => base.PrintTime;
        set
        {
            base.PrintTime = value;
            ControlFile.PrintTime = base.PrintTime;
        }
    }

    public override float MaterialMilliliters
    {
        get => base.MaterialMilliliters;
        set
        {
            base.MaterialMilliliters = value;
            ConfigFile.MaterialMl = ControlFile.MaterialMl = base.MaterialMilliliters;
        }
    }

    public override object[] Configs => new object[] { ConfigFile };

    #endregion

    #region Constructor
    public JXSFile()
    {
        _layerImageFormat = ImageFormat.Png24RgbAA;
        ResolutionX = RESOLUTION_X;
        ResolutionY = RESOLUTION_Y;
        DisplayWidth = 221.40f;
        DisplayHeight = 129.60f;
        MachineZ = 245;
        MachineName = "Uniformation GKone";
        GCode = new GCodeBuilder
        {
            UseTailComma = true,
            UseComments = true,
            GCodePositioningType = GCodeBuilder.GCodePositioningTypes.Relative,
            GCodeSpeedUnit = GCodeBuilder.GCodeSpeedUnits.MillimetersPerMinute,
            GCodeTimeUnit = GCodeBuilder.GCodeTimeUnits.Milliseconds,
            GCodeShowImageType = GCodeBuilder.GCodeShowImageTypes.FilenamePng0Started,
            LayerMoveCommand = GCodeBuilder.GCodeMoveCommands.G0,
            EndGCodeMoveCommand = GCodeBuilder.GCodeMoveCommands.G0,
            CommandWaitSyncDelay =
            {
                Enabled = true
            }
        };
    }
    #endregion

    #region Methods

    private void RebuildFileProperties()
    {
        ConfigFile.JobName = FilenameNoExt!;
        ConfigFile.ApplicationName = About.SoftwareWithVersion;

        RebuildGCode();
        ControlFile.Actions.Clear();

        using var tw = new StringReader(GCodeStr!);
        var lastG0 = "G1 Z100 F150";
        while (tw.ReadLine()?.Trim() is { } line)
        {
            if (line == string.Empty) continue;

            if (line.StartsWith(GCode!.CommandClearImage.Command))
            {
                ControlFile.Actions.Add(new object[]
                {
                    "slice", "<BLANK>"
                });
                continue;
            }

            if(line[0] == ';') continue;

            var index = line.IndexOf(';');
            if (index >= 0)
            {
                line = line[..index].TrimEnd();
            }

            if (line == string.Empty) continue;

            if (line.StartsWith(GCode.CommandShowImageM6054.Command))
            {
                var match = Regex.Match(line, GCode.GetShowImageString(@"([0-9]+)"));
                
                if (!match.Success || match.Groups.Count <= 1)
                {
                    throw new InvalidDataException($"Unable to parse layer index from: {line}");
                }

                var layerIndex = match.Groups[1].Value.PadLeft(5, '0');
                if(!uint.TryParse(layerIndex,  out var layerNumber))
                {
                    throw new InvalidDataException($"Unable to parse layer number from: {line}");
                }
                layerNumber++;

                ControlFile.Actions.Add(new object[]
                {
                    "layerno", layerNumber,
                });
                ControlFile.Actions.Add(new object[]
                {
                    "slice", $"{{PWD}}/{layerIndex}.png"
                });
                continue;
            }

            if (line.StartsWith(GCode.CommandWaitG4.Command))
            {
                var match = Regex.Match(line, GCode.CommandWaitG4.ToStringWithoutComments(@"([0-9]+)"));

                if (!match.Success || match.Groups.Count <= 1)
                {
                    throw new InvalidDataException($"Unable to delay from: {line}");
                }

                var delayStr = match.Groups[1].Value;
                if (!uint.TryParse(delayStr, out var delay))
                {
                    throw new InvalidDataException($"Unable to parse delay number from: {line}");
                }

                if(delay <= 0) continue;

                ControlFile.Actions.Add(new object[]
                {
                    "delay", delay
                });
                continue;
            }

            if (line.StartsWith("G0") || line.StartsWith("G1"))
            {
                lastG0 = line;
            }
            
            ControlFile.Actions.Add(new object[]
            {
                "gcode", line
            });
        }

        ConfigFile.GcodeFooter = lastG0;
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Read);
        var entry = inputFile.GetEntry(ConfigFileName);
        if (entry is null)
        {
            Clear();
            throw new FileLoadException($"{ConfigFileName} not found", FileFullPath);
        }

        try
        {
            using var stream = entry.Open();
            using TextReader reader = new StreamReader(stream);
            while(reader.ReadLine() is { } line)
            {
                var keyValue = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if(keyValue.Length < 2) continue;

                foreach (var propertyInfo in ConfigFile.GetType().GetProperties())
                {
                    if (!propertyInfo.CanWrite) continue;
                    var customAttributes = propertyInfo.GetCustomAttributes();
                    if (customAttributes.Any(attribute =>
                        {
                            var type = attribute.GetType();
                            if (type == typeof(XmlIgnoreAttribute)) return true;
                            if (type == typeof(JsonIgnoreAttribute)) return true;
                            return false;
                        })) continue;

                    var property = propertyInfo.Name;
                    foreach (var attribute in customAttributes)
                    {
                        if (attribute.GetType() != typeof(DisplayNameAttribute)) continue;
                        property = ((DisplayNameAttribute)attribute).DisplayName;
                        break;
                    }

                    if(property != keyValue[0]) continue;

                    //Debug.WriteLine(attribute.Name);
                    propertyInfo.SetValueFromString(ConfigFile, keyValue[1]);
                }
            }

        }
        catch (Exception e)
        {
            Clear();
            throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
        }

        entry = inputFile.GetEntry(ControlFilename);
        if (entry is null)
        {
            Clear();
            throw new FileLoadException($"{ControlFilename} not found", FileFullPath);
        }

        try
        {
            using var stream = entry.Open();
            ControlFile = JsonSerializer.Deserialize<JXSControl>(stream)!;
            if (ControlFile is null)
            {
                Clear();
                throw new FileLoadException($"Unable to deserialize '{entry.Name}'", FileFullPath);
            }
        }
        catch (Exception e)
        {
            Clear();
            throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
        }

        Init(ConfigFile.LayerCount, DecodeType == FileDecodeType.Partial);

        DecodeLayersFromZip(inputFile, IndexStartNumber.Zero, progress);

        GCode!.Clear();

        string lastCommand = string.Empty;
        // Rebuild gcode from json
        foreach (var action in ControlFile.Actions)
        {
            var key = action[0].ToString()!;
            var value = action[1].ToString()!;

            switch (key)
            {
                case "gcode":
                    lastCommand = value;
                    if (value.StartsWith("M106 S0")) GCode.AppendWaitG4(0);
                    GCode.AppendLine(value);
                    break;
                case "slice":
                    if (value.StartsWith("{PWD}/"))
                    {
                        GCode.AppendShowImageM6054(value.Remove(0, "{PWD}/".Length));
                    }
                    else if (value == "<BLANK>")
                    {
                        GCode.AppendClearImage();
                    }
                    break;
                case "delay":
                    if (lastCommand.StartsWith("G0") || lastCommand.StartsWith("G1"))
                    {
                        lastCommand = $"G4 0{value}";
                        GCode.AppendWaitSyncDelay(value);
                        break;
                    }
                    GCode.AppendWaitG4(value);
                    break;
                case "layerno":
                    GCode.AppendLine();
                    GCode.AppendLine($";LAYER_NUMBER:{value}");
                    break;
            }
        }

        GCode!.ParseLayersFromGCode(this);

        if (!ConfigFile.ApplicationName.StartsWith(About.Software))
        {
            BottomWaitTimeBeforeCure = TimeConverter.MillisecondsToSeconds(ConfigFile.BottomWaitTimeBeforeCure);
            WaitTimeBeforeCure = TimeConverter.MillisecondsToSeconds(ConfigFile.WaitTimeBeforeCure);
        }
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Create);

        EncodeLayersInZip(outputFile, 5, IndexStartNumber.Zero, progress);

        RebuildFileProperties();

        using (var stream = outputFile.CreateEntryStream(ConfigFileName))
        using (var tw = new StreamWriter(stream))
        {
            foreach (var propertyInfo in ConfigFile.GetType()
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.Name.Equals("Item")) continue;

                var customAttributes = propertyInfo.GetCustomAttributes();
                if (customAttributes.Any(attribute =>
                {
                    var type = attribute.GetType();
                    if (type == typeof(XmlIgnoreAttribute)) return true;
                    if (type == typeof(JsonIgnoreAttribute)) return true;
                    return false;
                })) continue;

                var property = propertyInfo.Name;
                foreach (var attribute in customAttributes)
                {
                    if (attribute.GetType() != typeof(DisplayNameAttribute)) continue;
                    property = ((DisplayNameAttribute)attribute).DisplayName;
                    break;
                }

                tw.WriteLine($"{property}={propertyInfo.GetValue(ConfigFile)}");
            }
        }

        outputFile.CreateEntryFromSerializeJson(ControlFilename, ConfigFile, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Update);
        var entriesToRemove = outputFile.Entries.Where(zipEntry => zipEntry.Name.EndsWith(".ini") || zipEntry.Name.EndsWith(".json")).ToArray();
        foreach (var zipEntry in entriesToRemove)
        {
            zipEntry.Delete();
        }


        RebuildFileProperties();

        using (var stream = outputFile.CreateEntryStream(ConfigFileName))
        using (var tw = new StreamWriter(stream))
        {
            foreach (var propertyInfo in ConfigFile.GetType()
                         .GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (propertyInfo.Name.Equals("Item")) continue;

                var customAttributes = propertyInfo.GetCustomAttributes();
                if (customAttributes.Any(attribute =>
                    {
                        var type = attribute.GetType();
                        if (type == typeof(XmlIgnoreAttribute)) return true;
                        if (type == typeof(JsonIgnoreAttribute)) return true;
                        return false;
                    })) continue;

                var property = propertyInfo.Name;
                foreach (var attribute in customAttributes)
                {
                    if (attribute.GetType() != typeof(DisplayNameAttribute)) continue;
                    property = ((DisplayNameAttribute)attribute).DisplayName;
                    break;
                }

                tw.WriteLine($"{property}={propertyInfo.GetValue(ConfigFile)}");
            }
        }

        outputFile.CreateEntryFromSerializeJson(ControlFilename, ConfigFile, ZipArchiveMode.Update, JsonExtensions.SettingsIndent);
    }
    #endregion
}