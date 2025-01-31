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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public sealed class UVJFile : FileFormat
{
    #region Constants

    private const string FileConfigName = "config.json";
    private const string FolderImageName = "slice";
    private const string FolderPreviewName = "preview";
    private const string FilePreviewHugeName = "preview/huge.png";
    private const string FilePreviewTinyName = "preview/tiny.png";
    #endregion

    #region Sub Classes

    public sealed class Millimeter
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    public sealed class Size
    {
        public ushort X { get; set; }
        public ushort Y { get; set; }

        public Millimeter Millimeter { get; set; } = new();

        public uint Layers { get; set; }
        public float LayerHeight { get; set; }

        public override string ToString()
        {
            return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Millimeter)}: {Millimeter}, {nameof(Layers)}: {Layers}, {nameof(LayerHeight)}: {LayerHeight}";
        }
    }

    public class Exposure
    {
        public float WaitTimeBeforeCure { get; set; }
        public float LightOffTime { get; set; }
        public float LightOnTime { get; set; }
        public byte LightPWM { get; set; } = DefaultLightPWM;
        public float WaitTimeAfterCure { get; set; }
        public float LiftHeight { get; set; } = DefaultLiftHeight;
        public float LiftSpeed { get; set; } = DefaultLiftSpeed;
        public float LiftHeight2 { get; set; } = DefaultLiftHeight2;
        public float LiftSpeed2 { get; set; } = DefaultLiftSpeed2;
        public float WaitTimeAfterLift { get; set; }
        public float RetractHeight { get; set; }
        public float RetractSpeed { get; set; } = DefaultRetractSpeed;
        public float RetractHeight2 { get; set; }
        public float RetractSpeed2 { get; set; } = DefaultRetractSpeed2;

        public override string ToString()
        {
            return $"{nameof(WaitTimeBeforeCure)}: {WaitTimeBeforeCure}, {nameof(LightOffTime)}: {LightOffTime}, {nameof(LightOnTime)}: {LightOnTime}, {nameof(LightPWM)}: {LightPWM}, {nameof(WaitTimeAfterCure)}: {WaitTimeAfterCure}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(LiftHeight2)}: {LiftHeight2}, {nameof(LiftSpeed2)}: {LiftSpeed2}, {nameof(WaitTimeAfterLift)}: {WaitTimeAfterLift}, {nameof(RetractHeight)}: {RetractHeight}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(RetractHeight2)}: {RetractHeight2}, {nameof(RetractSpeed2)}: {RetractSpeed2}";
        }
    }

    public sealed class Bottom : Exposure
    {
        public ushort Count { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(Count)}: {Count}";
        }
    }

    public sealed class UVtoolsVendor
    {
        public string Version { get; set; } = About.VersionString;
        public string FirstModified = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
        public string LastModified = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);

        public void Update()
        {
            LastModified = DateTime.UtcNow.ToString(CultureInfo.InvariantCulture);
        }
    }

    public sealed class LayerDef
    {
        public float Z { get; set; }
        public Exposure Exposure { get; set; } = new();

        public LayerDef() { }

        public LayerDef(Layer layer)
        {
            SetFrom(layer);
        }

        public override string ToString()
        {
            return $"{nameof(Z)}: {Z}, {nameof(Exposure)}: {Exposure}";
        }

        public void SetFrom(Layer layer)
        {
            Z = layer.PositionZ;
            Exposure.WaitTimeBeforeCure = layer.WaitTimeBeforeCure;
            Exposure.LightOffTime = layer.LightOffDelay;
            Exposure.LightOnTime = layer.ExposureTime;
            Exposure.WaitTimeAfterCure = layer.WaitTimeAfterCure;
            Exposure.LiftHeight = layer.LiftHeight;
            Exposure.LiftSpeed = layer.LiftSpeed;
            Exposure.LiftHeight2 = layer.LiftHeight2;
            Exposure.LiftSpeed2 = layer.LiftSpeed2;
            Exposure.WaitTimeAfterLift = layer.WaitTimeAfterLift;
            Exposure.RetractHeight = layer.RetractHeight;
            Exposure.RetractSpeed = layer.RetractSpeed;
            Exposure.RetractHeight2 = layer.RetractHeight2;
            Exposure.RetractSpeed2 = layer.RetractSpeed2;
            Exposure.LightPWM = layer.LightPWM;
        }

        public void CopyTo(Layer layer)
        {
            layer.PositionZ = Z;
            layer.WaitTimeBeforeCure = Exposure.WaitTimeBeforeCure;
            layer.LightOffDelay = Exposure.LightOffTime;
            layer.ExposureTime = Exposure.LightOnTime;
            layer.WaitTimeAfterCure = Exposure.WaitTimeAfterCure;
            layer.LiftHeight = Exposure.LiftHeight;
            layer.LiftSpeed = Exposure.LiftSpeed;
            layer.LiftHeight2 = Exposure.LiftHeight2;
            layer.LiftSpeed2 = Exposure.LiftSpeed2;
            layer.WaitTimeAfterLift = Exposure.WaitTimeAfterLift;
            layer.RetractSpeed = Exposure.RetractSpeed;
            layer.RetractHeight2 = Exposure.RetractHeight2;
            layer.RetractSpeed2 = Exposure.RetractSpeed2;
            layer.LightPWM = Exposure.LightPWM;
        }
    }

    public sealed class Properties
    {
        public Size Size { get; set; } = new ();
        public Exposure Exposure { get; set; } = new ();
        public Bottom Bottom { get; set; } = new ();
        public Dictionary<string, dynamic> Vendor { get; set; } = new()
        {
            //{About.Software, new UVtoolsVendor()}
        };

        public byte AntiAliasLevel { get; set; } = 1;

        public override string ToString()
        {
            return $"{nameof(Size)}: {Size}, {nameof(Exposure)}: {Exposure}, {nameof(Bottom)}: {Bottom}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}";
        }
    }

    public sealed class Settings
    {
        public Properties Properties { get; set; } = new();
        public List<LayerDef>? Layers { get; set; } = new();

        public override string ToString()
        {
            return $"{nameof(Properties)}: {Properties}, {nameof(Layers)}: {Layers?.Count}";
        }
    }

    #endregion

    #region Properties
    public Settings JsonSettings { get; set; } = new();

    public override FileFormatType FileType => FileFormatType.Archive;

    public override FileExtension[] FileExtensions { get; } = {
        new(typeof(UVJFile), "uvj", "Vendor-neutral format (UVJ)")
    };

    public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
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
        PrintParameterModifier.LightPWM
    };

    public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
        PrintParameterModifier.PositionZ,
        PrintParameterModifier.LightOffDelay,
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
    };

    public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } =
    {
        new(400, 400),
        new(800, 480)
    };

    public override uint ResolutionX
    {
        get => JsonSettings.Properties.Size.X;
        set => base.ResolutionX = JsonSettings.Properties.Size.X = (ushort) value;
    }

    public override uint ResolutionY
    {
        get => JsonSettings.Properties.Size.Y;
        set => base.ResolutionY = JsonSettings.Properties.Size.Y = (ushort)value;
    }

    public override float DisplayWidth
    {
        get => JsonSettings.Properties.Size.Millimeter.X;
        set => base.DisplayWidth = JsonSettings.Properties.Size.Millimeter.X = RoundDisplaySize(value);
    }

    public override float DisplayHeight
    {
        get => JsonSettings.Properties.Size.Millimeter.Y;
        set => base.DisplayHeight = JsonSettings.Properties.Size.Millimeter.Y = RoundDisplaySize(value);
    }

    public override FlipDirection DisplayMirror { get; set; }

    public override byte AntiAliasing
    {
        get => JsonSettings.Properties.AntiAliasLevel;
        set => base.AntiAliasing = JsonSettings.Properties.AntiAliasLevel = Math.Clamp(value, (byte)1, (byte)16);
    }

    public override float LayerHeight
    {
        get => JsonSettings.Properties.Size.LayerHeight;
        set => base.LayerHeight = JsonSettings.Properties.Size.LayerHeight = Layer.RoundHeight(value);
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = JsonSettings.Properties.Size.Layers = base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => JsonSettings.Properties.Bottom.Count;
        set => base.BottomLayerCount = JsonSettings.Properties.Bottom.Count = value;
    }

    public override float BottomLightOffDelay
    {
        get => JsonSettings.Properties.Bottom.LightOffTime;
        set
        {
            base.BottomLightOffDelay = JsonSettings.Properties.Bottom.LightOffTime = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomWaitTimeBeforeCure = 0;
                BottomWaitTimeAfterCure = 0;
                BottomWaitTimeAfterLift = 0;
            }
        }
    }

    public override float LightOffDelay
    {
        get => JsonSettings.Properties.Exposure.LightOffTime;
        set
        {
            base.LightOffDelay = JsonSettings.Properties.Exposure.LightOffTime = MathF.Round(value, 2);
            if (value > 0)
            {
                WaitTimeBeforeCure = 0;
                WaitTimeAfterCure = 0;
                WaitTimeAfterLift = 0;
            }
        }
    }

    public override float BottomWaitTimeBeforeCure
    {
        get => base.BottomWaitTimeBeforeCure;
        set
        {
            base.BottomWaitTimeBeforeCure = JsonSettings.Properties.Bottom.WaitTimeBeforeCure = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
        }
    }

    public override float WaitTimeBeforeCure
    {
        get => base.WaitTimeBeforeCure;
        set
        {
            base.WaitTimeBeforeCure = JsonSettings.Properties.Exposure.WaitTimeBeforeCure = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
        }
    }

    public override float BottomExposureTime
    {
        get => JsonSettings.Properties.Bottom.LightOnTime;
        set => base.BottomExposureTime = JsonSettings.Properties.Bottom.LightOnTime = MathF.Round(value, 2);
    }

    public override float ExposureTime
    {
        get => JsonSettings.Properties.Exposure.LightOnTime;
        set => base.ExposureTime = JsonSettings.Properties.Exposure.LightOnTime = MathF.Round(value, 2);
    }

    public override float BottomWaitTimeAfterCure
    {
        get => base.BottomWaitTimeAfterCure;
        set
        {
            base.BottomWaitTimeAfterCure = JsonSettings.Properties.Bottom.WaitTimeAfterCure = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
        }
    }

    public override float WaitTimeAfterCure
    {
        get => base.WaitTimeAfterCure;
        set
        {
            base.WaitTimeAfterCure = JsonSettings.Properties.Exposure.WaitTimeAfterCure = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
        }
    }

    public override float BottomLiftHeight
    {
        get => JsonSettings.Properties.Bottom.LiftHeight;
        set => base.BottomLiftHeight = JsonSettings.Properties.Bottom.LiftHeight = MathF.Round(value, 2);
    }

    public override float BottomLiftSpeed
    {
        get => JsonSettings.Properties.Bottom.LiftSpeed;
        set => base.BottomLiftSpeed = JsonSettings.Properties.Bottom.LiftSpeed = MathF.Round(value, 2);
    }

    public override float LiftHeight
    {
        get => JsonSettings.Properties.Exposure.LiftHeight;
        set => base.LiftHeight = JsonSettings.Properties.Exposure.LiftHeight = MathF.Round(value, 2);
    }

    public override float LiftSpeed
    {
        get => JsonSettings.Properties.Exposure.LiftSpeed;
        set => base.LiftSpeed = JsonSettings.Properties.Exposure.LiftSpeed = MathF.Round(value, 2);
    }

    public override float BottomLiftHeight2
    {
        get => JsonSettings.Properties.Bottom.LiftHeight2;
        set => base.BottomLiftHeight2 = JsonSettings.Properties.Bottom.LiftHeight2 = MathF.Round(value, 2);
    }

    public override float BottomLiftSpeed2
    {
        get => JsonSettings.Properties.Bottom.LiftSpeed2;
        set => base.BottomLiftSpeed2 = JsonSettings.Properties.Bottom.LiftSpeed2 = MathF.Round(value, 2);
    }

    public override float BottomWaitTimeAfterLift
    {
        get => base.BottomWaitTimeAfterLift;
        set
        {
            base.BottomWaitTimeAfterLift = JsonSettings.Properties.Bottom.WaitTimeAfterLift = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
        }
    }

    public override float WaitTimeAfterLift
    {
        get => base.WaitTimeAfterLift;
        set
        {
            base.WaitTimeAfterLift = JsonSettings.Properties.Exposure.WaitTimeAfterLift = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
        }
    }

    public override float LiftHeight2
    {
        get => JsonSettings.Properties.Exposure.LiftHeight2;
        set => base.LiftHeight2 = JsonSettings.Properties.Exposure.LiftHeight2 = MathF.Round(value, 2);
    }

    public override float LiftSpeed2
    {
        get => JsonSettings.Properties.Exposure.LiftSpeed2;
        set => base.LiftSpeed2 = JsonSettings.Properties.Exposure.LiftSpeed2 = MathF.Round(value, 2);
    }

    public override float BottomRetractSpeed
    {
        get => JsonSettings.Properties.Bottom.RetractSpeed;
        set => base.BottomRetractSpeed = JsonSettings.Properties.Bottom.RetractSpeed = MathF.Round(value, 2);
    }

    public override float RetractSpeed
    {
        get => JsonSettings.Properties.Exposure.RetractSpeed;
        set => base.RetractSpeed = JsonSettings.Properties.Exposure.RetractSpeed = MathF.Round(value, 2);
    }

    public override float BottomRetractHeight2
    {
        get => JsonSettings.Properties.Bottom.RetractHeight2;
        set => base.BottomRetractHeight2 = JsonSettings.Properties.Bottom.RetractHeight2 = MathF.Round(value, 2);
    }

    public override float BottomRetractSpeed2
    {
        get => JsonSettings.Properties.Bottom.RetractSpeed2;
        set => base.BottomRetractSpeed2 = JsonSettings.Properties.Bottom.RetractSpeed2 = MathF.Round(value, 2);
    }

    public override float RetractHeight2
    {
        get => JsonSettings.Properties.Exposure.RetractHeight2;
        set => base.RetractHeight2 = JsonSettings.Properties.Exposure.RetractHeight2 = MathF.Round(value, 2);
    }

    public override float RetractSpeed2
    {
        get => JsonSettings.Properties.Exposure.RetractSpeed2;
        set => base.RetractSpeed2 = JsonSettings.Properties.Exposure.RetractSpeed2 = MathF.Round(value, 2);
    }


    public override byte BottomLightPWM
    {
        get => JsonSettings.Properties.Bottom.LightPWM;
        set => base.BottomLightPWM = JsonSettings.Properties.Bottom.LightPWM = value;
    }

    public override byte LightPWM
    {
        get => JsonSettings.Properties.Exposure.LightPWM;
        set => base.LightPWM = JsonSettings.Properties.Exposure.LightPWM = value;
    }

    public override object[] Configs => new[] {(object) JsonSettings.Properties.Size, JsonSettings.Properties.Size.Millimeter, JsonSettings.Properties.Bottom, JsonSettings.Properties.Exposure};
    #endregion

    #region Methods

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BottomRetractHeight)) JsonSettings.Properties.Bottom.RetractHeight = BottomRetractHeight;
        else if (e.PropertyName == nameof(RetractHeight)) JsonSettings.Properties.Exposure.RetractHeight = RetractHeight;

        base.OnPropertyChanged(e);
    }

    public override void Clear()
    {
        base.Clear();
        JsonSettings.Layers = new List<LayerDef>();
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        // Redo layer data
        if (JsonSettings.Layers is null)
        {
            JsonSettings.Layers = new List<LayerDef>();
        }
        else
        {
            JsonSettings.Layers.Clear();
        }
            
        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            JsonSettings.Layers.Add(new LayerDef(this[layerIndex]));
        }

        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Create);
        outputFile.CreateEntryFromSerializeJson(FileConfigName, JsonSettings, ZipArchiveMode.Create, JsonExtensions.SettingsIndent);

        EncodeThumbnailsInZip(outputFile, progress, FilePreviewTinyName, FilePreviewHugeName);
        EncodeLayersInZip(outputFile, 8, IndexStartNumber.Zero, progress, FolderImageName);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Read);
        var entry = inputFile.GetEntry(FileConfigName);
        if (entry is null)
        {
            Clear();
            throw new FileLoadException($"{FileConfigName} not found", FileFullPath);
        }

        JsonSettings = JsonSerializer.Deserialize<Settings>(entry.Open())!;
        Init(JsonSettings.Properties.Size.Layers, DecodeType == FileDecodeType.Partial);

        DecodeThumbnailsFromZip(inputFile, progress, FilePreviewTinyName, FilePreviewHugeName);
        DecodeLayersFromZip(inputFile, progress);

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            if (JsonSettings.Layers?.Count > layerIndex)
            {
                JsonSettings.Layers[(int)layerIndex].CopyTo(this[layerIndex]);
            }
        }
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        if (JsonSettings.Layers is null)
        {
            JsonSettings.Layers = new List<LayerDef>();
        }
        else
        {
            JsonSettings.Layers.Clear();
        }

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            JsonSettings.Layers.Add(new LayerDef(this[layerIndex]));
        }

        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Update);
        outputFile.CreateEntryFromSerializeJson(FileConfigName, JsonSettings, ZipArchiveMode.Update, JsonExtensions.SettingsIndent);
    }
    #endregion
}