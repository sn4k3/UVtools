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
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public sealed class VDTFile : FileFormat
{
    #region Constants

    private const string FileManifestName = "manifest.json";
    private static readonly string[] FilePreviewNames = {
        "Preview_Top.png",
        "Preview_Top_48.png",
        "Preview_Right.png",
        "Preview_Right_48.png",
        "Preview_Left.png",
        "Preview_Left_48.png",
        "Preview_Front.png",
        "Preview_Front_48.png",
        "Preview_FLT.png",
        "Preview_FLT_48.png",
    };
    #endregion

    #region Sub Classes

    public sealed class VDTManifest
    {
        [JsonPropertyName("application_name")] public string ApplicationName { get; set; } = About.Software;

        [JsonPropertyName("application_version")] public string ApplicationVersion { get; set; } = About.VersionStr;
        //2021-04-09 17:48:46
        [JsonPropertyName("create_datetime")] public string CreateDateTime { get; set; } = DateTime.UtcNow.ToString("u");

        [JsonPropertyName("file_version")] public byte FileVersion { get; set; } = 1;

        [JsonPropertyName("project_name")] public string ProjectName { get; set; } = "UVtools";

        [JsonPropertyName("machine")] public VDTMachine Machine { get; set; } = new();
        [JsonPropertyName("advanced_parameters")] public VDTAdvancedParameters AdvancedParameters { get; set; } = new();
        [JsonPropertyName("resin")] public VDTResin Resin { get; set; } = new();
        [JsonPropertyName("print")] public VDTPrint Print { get; set; } = new();
        [JsonPropertyName("print_statistics")] public VDTPrintStatistics PrintStatistics { get; set; } = new();
        [JsonPropertyName("layers")] public VDTLayer[]? Layers { get; set; }
    }

    public sealed class VDTAdvancedParameters
    {
        [JsonPropertyName("antialasing_level")] public byte AntialasingLevel { get; set; } = 1;
        [JsonPropertyName("grey_level")] public byte GreyLevel { get; set; }
        [JsonPropertyName("image_blur_level")] public byte ImageBlurLevel { get; set; }

        [JsonPropertyName("bottom_light_pwm")] public byte BottomLightPWM { get; set; } = DefaultBottomLightPWM;
        [JsonPropertyName("light_pwm")] public byte LightPWM { get; set; } = DefaultLightPWM;

        [JsonPropertyName("beam_compensation")] public float BeamCompensation { get; set; }
    }

    public sealed class VDTMachine
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "Unknown";
        [JsonPropertyName("type")] public string Type { get; set; } = "Default";
        [JsonPropertyName("lcd_width")] public float DisplayWidth { get; set; }
        [JsonPropertyName("lcd_height")] public float DisplayHeight { get; set; }
        [JsonPropertyName("z_height")] public float ZHeight { get; set; }
        [JsonPropertyName("resolution_x")] public ushort ResolutionX { get; set; }
        [JsonPropertyName("resolution_y")] public ushort ResolutionY { get; set; }
        [JsonPropertyName("x_mirror")] public bool XMirror { get; set; }
        [JsonPropertyName("y_mirror")] public bool YMirror { get; set; }
        [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;
        [JsonPropertyName("uvtools_convert_to")] public string UVToolsConvertTo { get; set; } = string.Empty;
    }

    public sealed class VDTResin
    {
        [JsonPropertyName("name")] public string? Name { get; set; } = string.Empty;
        [JsonPropertyName("cost")] public float Cost { get; set; }
        [JsonPropertyName("density")] public float Density { get; set; } = 1;
        [JsonPropertyName("notes")] public string Notes { get; set; } = string.Empty;
    }

    public sealed class VDTPrint
    {
        [JsonPropertyName("layer_thickness")] public float LayerHeight { get; set; }
        [JsonPropertyName("bottom_layers")] public ushort BottomLayerCount { get; set; } = DefaultBottomLayerCount;
        [JsonPropertyName("transition_layer")] public ushort TransitionLayerCount { get; set; } = DefaultTransitionLayerCount;
        [JsonPropertyName("bottom_light_off_delay")] public float BottomLightOffDelay { get; set; }
        [JsonPropertyName("light_off_delay")] public float LightOffDelay { get; set; }
        [JsonPropertyName("bottom_wait_time_before_cure")] public float BottomWaitTimeBeforeCure { get; set; }
        [JsonPropertyName("wait_time_before_cure")] public float WaitTimeBeforeCure { get; set; }
        [JsonPropertyName("bottom_exposure_time")] public float BottomExposureTime { get; set; } = DefaultBottomExposureTime;
        [JsonPropertyName("exposure_time")] public float ExposureTime { get; set; } = DefaultExposureTime;
        [JsonPropertyName("bottom_wait_time_after_cure")] public float BottomWaitTimeAfterCure { get; set; }
        [JsonPropertyName("wait_time_after_cure")] public float WaitTimeAfterCure { get; set; }
        [JsonPropertyName("bottom_lift_distance")] public float BottomLiftHeight { get; set; } = DefaultBottomLiftHeight;
        [JsonPropertyName("bottom_lift_speed")] public float BottomLiftSpeed { get; set; } = DefaultBottomLiftSpeed;
        [JsonPropertyName("lift_distance")] public float LiftHeight { get; set; } = DefaultLiftHeight;
        [JsonPropertyName("lift_speed")] public float LiftSpeed { get; set; } = DefaultLiftSpeed;
        [JsonPropertyName("bottom_lift_distance2")] public float BottomLiftHeight2 { get; set; } = DefaultBottomLiftHeight2;
        [JsonPropertyName("bottom_lift_speed2")] public float BottomLiftSpeed2 { get; set; } = DefaultBottomLiftSpeed2;
        [JsonPropertyName("lift_distance2")] public float LiftHeight2 { get; set; } = DefaultLiftHeight2;
        [JsonPropertyName("lift_speed2")] public float LiftSpeed2 { get; set; } = DefaultLiftSpeed2;
        [JsonPropertyName("bottom_wait_time_after_lift")] public float BottomWaitTimeAfterLift { get; set; }
        [JsonPropertyName("wait_time_after_lift")] public float WaitTimeAfterLift { get; set; }
        [JsonPropertyName("bottom_retract_speed")] public float BottomRetractSpeed { get; set; } = DefaultBottomRetractSpeed;
        [JsonPropertyName("retract_speed")] public float RetractSpeed { get; set; } = DefaultRetractSpeed;
        [JsonPropertyName("bottom_retract_distance2")] public float BottomRetractHeight2 { get; set; } = DefaultBottomRetractHeight2;
        [JsonPropertyName("bottom_retract_speed2")] public float BottomRetractSpeed2 { get; set; } = DefaultBottomRetractSpeed2;
        [JsonPropertyName("retract_distance2")] public float RetractHeight2 { get; set; } = DefaultRetractHeight2;
        [JsonPropertyName("retract_speed2")] public float RetractSpeed2 { get; set; } = DefaultRetractSpeed2;
    }

    public sealed class VDTPrintStatistics
    {
        [JsonPropertyName("price")] public float Price { get; set; }
        [JsonPropertyName("price_currency")] public string PriceCurrency { get; set; } = "€";
        [JsonPropertyName("estimated_time")] public uint EstimatedTime { get; set; }
        [JsonPropertyName("volume")] public float Volume { get; set; }
        [JsonPropertyName("weight")] public float Weight { get; set; }
    }

    public sealed class VDTLayer
    {
        [JsonPropertyName("height")] public float PositionZ { get; set; }
        [JsonPropertyName("light_off_delay")] public float LightOffDelay { get; set; }
        [JsonPropertyName("wait_time_before_cure")] public float WaitTimeBeforeCure { get; set; }
        [JsonPropertyName("exposure_time")] public float ExposureTime { get; set; } = DefaultExposureTime;
        [JsonPropertyName("wait_time_after_cure")] public float WaitTimeAfterCure { get; set; }
        [JsonPropertyName("lift_distance")] public float LiftHeight { get; set; } = DefaultLiftHeight;
        [JsonPropertyName("lift_speed")] public float LiftSpeed { get; set; } = DefaultLiftSpeed;
        [JsonPropertyName("lift_distance2")] public float LiftHeight2 { get; set; } = DefaultLiftHeight2;
        [JsonPropertyName("lift_speed2")] public float LiftSpeed2 { get; set; } = DefaultLiftSpeed2;
        [JsonPropertyName("wait_time_after_lift")] public float WaitTimeAfterLift { get; set; }
        [JsonPropertyName("retract_speed")] public float RetractSpeed { get; set; } = DefaultRetractSpeed;
        [JsonPropertyName("retract_distance2")] public float RetractHeight2 { get; set; } = DefaultRetractHeight2;
        [JsonPropertyName("retract_speed2")] public float RetractSpeed2 { get; set; } = DefaultRetractSpeed2;
        [JsonPropertyName("light_pwm")] public byte LightPWM { get; set; } = DefaultLightPWM;

        public void SetFrom(Layer layer)
        {
            PositionZ = layer.PositionZ;
            LightOffDelay = layer.LightOffDelay;
            WaitTimeBeforeCure = layer.WaitTimeBeforeCure;
            ExposureTime = layer.ExposureTime;
            WaitTimeAfterCure = layer.WaitTimeAfterCure;
            LiftHeight = layer.LiftHeight;
            LiftSpeed = layer.LiftSpeed;
            LiftHeight2 = layer.LiftHeight2;
            LiftSpeed2 = layer.LiftSpeed2;
            WaitTimeAfterLift = layer.WaitTimeAfterLift;
            RetractSpeed = layer.RetractSpeed;
            RetractHeight2 = layer.RetractHeight2;
            RetractSpeed2 = layer.RetractSpeed2;
            LightPWM = layer.LightPWM;
        }

        public void CopyTo(Layer layer)
        {
            layer.PositionZ = PositionZ;
            layer.LightOffDelay = LightOffDelay;
            layer.WaitTimeBeforeCure = WaitTimeBeforeCure;
            layer.ExposureTime = ExposureTime;
            layer.WaitTimeAfterCure = WaitTimeAfterCure;
            layer.LiftHeight = LiftHeight;
            layer.LiftSpeed = LiftSpeed;
            layer.LiftHeight2 = LiftHeight2;
            layer.LiftSpeed2 = LiftSpeed2;
            layer.WaitTimeAfterLift = WaitTimeAfterLift;
            layer.RetractSpeed = RetractSpeed;
            layer.RetractHeight2 = RetractHeight2;
            layer.RetractSpeed2 = RetractSpeed2;
            layer.LightPWM = LightPWM;
        }
    }

    #endregion

    #region Properties
    public VDTManifest ManifestFile { get; set; } = new();

    public override FileFormatType FileType => FileFormatType.Archive;

    public override string ConvertMenuGroup => "Voxeldance";

    public override FileExtension[] FileExtensions { get; } = {
        new(typeof(VDTFile), "vdt", "Voxeldance Tango (VDT)")
    };

    public override PrintParameterModifier[]? PrintParameterModifiers { get; } = {
        PrintParameterModifier.BottomLayerCount,
        PrintParameterModifier.TransitionLayerCount,

        PrintParameterModifier.BottomLightOffDelay,
        PrintParameterModifier.LightOffDelay,

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
        PrintParameterModifier.LightOffDelay,
        PrintParameterModifier.WaitTimeBeforeCure,
        PrintParameterModifier.ExposureTime,
        PrintParameterModifier.BottomWaitTimeAfterCure,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed,
        PrintParameterModifier.LiftHeight2,
        PrintParameterModifier.LiftSpeed2,
        PrintParameterModifier.WaitTimeAfterLift,
        PrintParameterModifier.RetractSpeed,
        PrintParameterModifier.RetractHeight2,
        PrintParameterModifier.RetractSpeed2,
            
        PrintParameterModifier.BottomLightPWM,
        PrintParameterModifier.LightPWM,
    };

    public override Size[]? ThumbnailsOriginalSize { get; } =
    {
        new(200, 200),
        new(48, 48),
        new(200, 200),
        new(48, 48),
        new(200, 200),
        new(48, 48),
        new(200, 200),
        new(48, 48),
        new(200, 200),
        new(48, 48),
    };

    public override uint[] AvailableVersions { get; } = { 1 };

    public override uint DefaultVersion => 1;

    public override uint Version
    {
        get => ManifestFile.FileVersion;
        set
        {
            base.Version = value;
            ManifestFile.FileVersion = (byte)base.Version;
        }
    }

    public override uint ResolutionX
    {
        get => ManifestFile.Machine.ResolutionX;
        set => base.ResolutionX = ManifestFile.Machine.ResolutionX = (ushort) value;
    }

    public override uint ResolutionY
    {
        get => ManifestFile.Machine.ResolutionY;
        set => base.ResolutionY = ManifestFile.Machine.ResolutionY = (ushort) value;
    }

    public override float DisplayWidth
    {
        get => ManifestFile.Machine.DisplayWidth;
        set => base.DisplayWidth = ManifestFile.Machine.DisplayWidth = (float) Math.Round(value, 2);
    }

    public override float DisplayHeight
    {
        get => ManifestFile.Machine.DisplayHeight;
        set => base.DisplayHeight = ManifestFile.Machine.DisplayHeight = (float)Math.Round(value, 2);
    }

    public override float MachineZ
    {
        get => ManifestFile.Machine.ZHeight > 0 ? ManifestFile.Machine.ZHeight : base.MachineZ;
        set => base.MachineZ = ManifestFile.Machine.ZHeight = (float)Math.Round(value, 2);
    }

    public override FlipDirection DisplayMirror
    {
        get
        {
            if (ManifestFile.Machine.XMirror && ManifestFile.Machine.YMirror) return FlipDirection.Both;
            if (ManifestFile.Machine.XMirror) return FlipDirection.Horizontally;
            if (ManifestFile.Machine.YMirror) return FlipDirection.Vertically;
            return FlipDirection.None;
        }
        set
        {
            ManifestFile.Machine.XMirror = value is FlipDirection.Horizontally or FlipDirection.Both;
            ManifestFile.Machine.YMirror = value is FlipDirection.Vertically or FlipDirection.Both;
            RaisePropertyChanged();
        }
    }

    public override byte AntiAliasing
    {
        get => ManifestFile.AdvancedParameters.AntialasingLevel;
        set => base.AntiAliasing = ManifestFile.AdvancedParameters.AntialasingLevel = value.Clamp(1, 16);
    }

    public override float LayerHeight
    {
        get => ManifestFile.Print.LayerHeight;
        set
        {
            ManifestFile.Print.LayerHeight = Layer.RoundHeight(value);
            RaisePropertyChanged();
        }
    }

    /* public override uint LayerCount
     {
         get => base.LayerCount;
         set => base.LayerCount = base.LayerCount;
     }*/

    public override ushort BottomLayerCount
    {
        get => ManifestFile.Print.BottomLayerCount;
        set => base.BottomLayerCount = ManifestFile.Print.BottomLayerCount = value;
    }

    public override TransitionLayerTypes TransitionLayerType => TransitionLayerTypes.Software;

    public override ushort TransitionLayerCount
    {
        get => ManifestFile.Print.TransitionLayerCount;
        set => base.TransitionLayerCount = ManifestFile.Print.TransitionLayerCount = (ushort)Math.Min(value, MaximumPossibleTransitionLayerCount);
    }

    public override float BottomLightOffDelay
    {
        get => ManifestFile.Print.BottomLightOffDelay;
        set => base.BottomLightOffDelay = ManifestFile.Print.BottomLightOffDelay = (float)Math.Round(value, 2);
    }

    public override float LightOffDelay
    {
        get => ManifestFile.Print.LightOffDelay;
        set => base.LightOffDelay = ManifestFile.Print.LightOffDelay = (float)Math.Round(value, 2);
    }

    public override float BottomWaitTimeBeforeCure
    {
        get => ManifestFile.Print.BottomWaitTimeBeforeCure;
        set => base.BottomWaitTimeBeforeCure = ManifestFile.Print.BottomWaitTimeBeforeCure = (float)Math.Round(value, 2);
    }
    public override float WaitTimeBeforeCure
    {
        get => ManifestFile.Print.WaitTimeBeforeCure;
        set => base.WaitTimeBeforeCure = ManifestFile.Print.WaitTimeBeforeCure = (float)Math.Round(value, 2);
    }

    public override float BottomExposureTime
    {
        get => ManifestFile.Print.BottomExposureTime;
        set => base.BottomExposureTime = ManifestFile.Print.BottomExposureTime = (float)Math.Round(value, 2);
    }

    public override float ExposureTime
    {
        get => ManifestFile.Print.ExposureTime;
        set => base.ExposureTime = ManifestFile.Print.ExposureTime = (float)Math.Round(value, 2);
    }

    public override float BottomWaitTimeAfterCure
    {
        get => ManifestFile.Print.BottomWaitTimeAfterCure;
        set => base.BottomWaitTimeAfterCure = ManifestFile.Print.BottomWaitTimeAfterCure = (float)Math.Round(value, 2);
    }
    public override float WaitTimeAfterCure
    {
        get => ManifestFile.Print.WaitTimeAfterCure;
        set => base.WaitTimeAfterCure = ManifestFile.Print.WaitTimeAfterCure = (float)Math.Round(value, 2);
    }

    public override float BottomLiftHeight
    {
        get => ManifestFile.Print.BottomLiftHeight;
        set => base.BottomLiftHeight = ManifestFile.Print.BottomLiftHeight = (float)Math.Round(value, 2);
    }

    public override float BottomLiftSpeed
    {
        get => ManifestFile.Print.BottomLiftSpeed;
        set => base.BottomLiftSpeed = ManifestFile.Print.BottomLiftSpeed = (float)Math.Round(value, 2);
    }

    public override float LiftHeight
    {
        get => ManifestFile.Print.LiftHeight;
        set => base.LiftHeight = ManifestFile.Print.LiftHeight = (float)Math.Round(value, 2);
    }
        
    public override float LiftSpeed
    {
        get => ManifestFile.Print.LiftSpeed;
        set => base.LiftSpeed = ManifestFile.Print.LiftSpeed = (float)Math.Round(value, 2);
    }

    public override float BottomLiftHeight2
    {
        get => ManifestFile.Print.BottomLiftHeight2;
        set => base.BottomLiftHeight2 = ManifestFile.Print.BottomLiftHeight2 = (float)Math.Round(value, 2);
    }

    public override float BottomLiftSpeed2
    {
        get => ManifestFile.Print.BottomLiftSpeed2;
        set => base.BottomLiftSpeed2 = ManifestFile.Print.BottomLiftSpeed2 = (float)Math.Round(value, 2);
    }

    public override float LiftHeight2
    {
        get => ManifestFile.Print.LiftHeight2;
        set => base.LiftHeight2 = ManifestFile.Print.LiftHeight2 = (float)Math.Round(value, 2);
    }
        
    public override float LiftSpeed2
    {
        get => ManifestFile.Print.LiftSpeed2;
        set => base.LiftSpeed2 = ManifestFile.Print.LiftSpeed2 = (float)Math.Round(value, 2);
    }

    public override float BottomWaitTimeAfterLift
    {
        get => ManifestFile.Print.BottomWaitTimeAfterLift;
        set => base.BottomWaitTimeAfterLift = ManifestFile.Print.BottomWaitTimeAfterLift = (float)Math.Round(value, 2);
    }
    public override float WaitTimeAfterLift
    {
        get => ManifestFile.Print.WaitTimeAfterLift;
        set => base.WaitTimeAfterLift = ManifestFile.Print.WaitTimeAfterLift = (float)Math.Round(value, 2);
    }

    public override float BottomRetractSpeed
    {
        get => ManifestFile.Print.BottomRetractSpeed;
        set => base.BottomRetractSpeed = ManifestFile.Print.BottomRetractSpeed = (float)Math.Round(value, 2);
    }

    public override float RetractSpeed
    {
        get => ManifestFile.Print.RetractSpeed;
        set => base.RetractSpeed = ManifestFile.Print.RetractSpeed = (float)Math.Round(value, 2);
    }

    public override float BottomRetractHeight2
    {
        get => ManifestFile.Print.BottomRetractHeight2;
        set
        {
            value = Math.Clamp((float)Math.Round(value, 2), 0, BottomRetractHeightTotal);
            base.BottomRetractHeight2 = ManifestFile.Print.BottomRetractHeight2 = value;
        }
    }
        
    public override float BottomRetractSpeed2
    {
        get => ManifestFile.Print.BottomRetractSpeed2;
        set => base.BottomRetractSpeed2 = ManifestFile.Print.BottomRetractSpeed2 = (float)Math.Round(value, 2);
    }

    public override float RetractHeight2
    {
        get => ManifestFile.Print.RetractHeight2;
        set
        {
            value = Math.Clamp((float)Math.Round(value, 2), 0, RetractHeightTotal);
            base.RetractHeight2 = ManifestFile.Print.RetractHeight2 = value;
        }
    }
        
    public override float RetractSpeed2
    {
        get => ManifestFile.Print.RetractSpeed2;
        set => base.RetractSpeed2 = ManifestFile.Print.RetractSpeed2 = (float)Math.Round(value, 2);
    }

    public override byte BottomLightPWM
    {
        get => ManifestFile.AdvancedParameters.BottomLightPWM;
        set => base.BottomLightPWM = ManifestFile.AdvancedParameters.BottomLightPWM = value;
    }

    public override byte LightPWM
    {
        get => ManifestFile.AdvancedParameters.LightPWM;
        set => base.LightPWM = ManifestFile.AdvancedParameters.LightPWM = value;
    }

    public override float PrintTime
    {
        get => base.PrintTime;
        set
        {
            base.PrintTime = value;
            ManifestFile.PrintStatistics.EstimatedTime = (uint)base.PrintTime;
        }
    }

    public override float MaterialMilliliters
    {
        get => base.MaterialMilliliters;
        set
        {
            base.MaterialMilliliters = value;
            ManifestFile.PrintStatistics.Volume = base.MaterialMilliliters;
        }
    }

    public override float MaterialGrams
    {
        get => (float)Math.Round(ManifestFile.PrintStatistics.Weight, 3);
        set => base.MaterialGrams = ManifestFile.PrintStatistics.Weight = (float)Math.Round(value, 3);
    }

    public override string? MaterialName
    {
        get => ManifestFile.Resin.Name;
        set => base.MaterialName = ManifestFile.Resin.Name = value;
    }

    public override float MaterialCost
    {
        get => (float)Math.Round(ManifestFile.Resin.Cost, 3);
        set => base.MaterialCost = ManifestFile.Resin.Cost = (float)Math.Round(value, 3);
    }

    public override string MachineName
    {
        get => ManifestFile.Machine.Name;
        set => base.MachineName = ManifestFile.Machine.Name = value;
    }

    public override object[] Configs => new[] {(object)ManifestFile, ManifestFile.Machine, ManifestFile.AdvancedParameters, ManifestFile.Resin, ManifestFile.Print, ManifestFile.PrintStatistics};
    #endregion

    #region Methods

    public void RebuildVDTLayers()
    {
        ManifestFile.CreateDateTime = DateTime.UtcNow.ToString("u");
        ManifestFile.Layers = new VDTLayer[LayerCount];
        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            var layer = this[layerIndex];
            ManifestFile.Layers[layerIndex] = new VDTLayer
            {
                PositionZ = layer.PositionZ,
                LightOffDelay = layer.LightOffDelay,
                WaitTimeBeforeCure = layer.WaitTimeBeforeCure,
                ExposureTime = layer.ExposureTime,
                WaitTimeAfterCure = layer.WaitTimeAfterCure,
                LiftHeight = layer.LiftHeight,
                LiftSpeed = layer.LiftSpeed,
                LiftHeight2 = layer.LiftHeight2,
                LiftSpeed2 = layer.LiftSpeed2,
                WaitTimeAfterLift = layer.WaitTimeAfterLift,
                RetractSpeed = layer.RetractSpeed,
                RetractHeight2 = layer.RetractHeight2,
                RetractSpeed2 = layer.RetractSpeed2,
                LightPWM = layer.LightPWM
            };
        }
    }

    protected override bool OnBeforeConvertTo(FileFormat output)
    {
        int fileVersion = LookupCustomValue(SL1File.Keyword_FileVersion, int.MinValue);
        if (fileVersion > 0)
        {
            output.Version = (uint)fileVersion;
        }

        return true;
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        // Redo layer data
        RebuildVDTLayers();

        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Create);
        outputFile.PutFileContent(FileManifestName, JsonSerializer.SerializeToUtf8Bytes(ManifestFile, JsonExtensions.SettingsIndent), ZipArchiveMode.Create);

        if (CreatedThumbnailsCount > 0)
        {
            for (int i = 0; i < FilePreviewNames.Length; i++)
            {
                if(Thumbnails.Length <= i) break;
                if(Thumbnails[i] is null) continue;

                using var stream = outputFile.CreateEntry(FilePreviewNames[i]).Open();
                stream.WriteBytes(Thumbnails[i]!.GetPngByes());
                stream.Close();
            }
        }

        EncodeLayersInZip(outputFile, progress);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Read);
        var entry = inputFile.GetEntry(FileManifestName);
        if (entry is null)
        {
            Clear();
            throw new FileLoadException($"{FileManifestName} not found", FileFullPath);
        }

        ManifestFile = JsonSerializer.Deserialize<VDTManifest>(entry.Open())!;
                
        Init((uint) ManifestFile.Layers!.Length, DecodeType == FileDecodeType.Partial);

        for (int i = 0; i < FilePreviewNames.Length; i++)
        {
            if (Thumbnails.Length <= i) break;

            entry = inputFile.GetEntry(FilePreviewNames[i]);
            if (entry is null) continue;
            using var stream = entry.Open();
            Mat image = new();
            CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, image);
            Thumbnails[i] = image;
            stream.Close();
        }

        DecodeLayersFromZip(inputFile, progress);

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            var manifestLayer = ManifestFile.Layers[layerIndex];
            manifestLayer.CopyTo(this[layerIndex]);
        }
                
        progress.ProcessedItems++;
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        RebuildVDTLayers();
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Update);
        outputFile.PutFileContent(FileManifestName, JsonSerializer.SerializeToUtf8Bytes(ManifestFile, JsonExtensions.SettingsIndent), ZipArchiveMode.Update);

        //Decode(FileFullPath, progress);
    }

    public T? LookupCustomValue<T>(string name, T? defaultValue, bool existsOnly = false)
    {
        //if (string.IsNullOrEmpty(PrinterSettings.PrinterNotes)) return defaultValue;
        var result = string.Empty;
        if (!existsOnly) name += '_';

        foreach (var notes in new[] { ManifestFile.Machine.Notes })
        {
            if (string.IsNullOrWhiteSpace(notes)) continue;

            var lines = notes.Split(new[] { "\\r\\n", "\\r", "\\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var line in lines)
            {
                if (!line.StartsWith(name)) continue;
                if (existsOnly || line == name) return "true".Convert<T>();
                var value = line.Remove(0, name.Length);
                foreach (var c in value)
                {
                    if (typeof(T) == typeof(string))
                    {
                        if (char.IsWhiteSpace(c)) break;
                    }
                    else
                    {
                        if (!char.IsLetterOrDigit(c) && c != '.')
                        {
                            break;
                        }
                    }


                    result += c;
                }
            }

            if (!string.IsNullOrEmpty(result)) break; // Found a candidate
        }

        return string.IsNullOrWhiteSpace(result) ? defaultValue : result.Convert<T>();
    }
    #endregion
}