/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public sealed class KlipperFile : FileFormat
{
    #region Constants

    public const string KlipperFileIdentifier = ".klipper";

    public static readonly string[] ThumbnailsEntryNames = { "preview.png", "preview_cropping.png" };
    #endregion

    #region Sub Classes

    public class Header
    {
        // ;(****Build and Slicing Parameters****)
        [DisplayName("FileName")] public string Filename { get; set; } = string.Empty;
        [DisplayName("MachineName")] public string MachineName { get; set; } = DefaultMachineName;
        [DisplayName("PrintTime")] public float PrintTime { get; set; }
        [DisplayName("Volume")] public float Volume { get; set; }
        [DisplayName("MaterialName")] public string MaterialName { get; set; } = DefaultResinName;
        [DisplayName("MaterialGrams")] public float MaterialGrams { get; set; }
        [DisplayName("MaterialMl")] public float MaterialMilliliters { get; set; }
        [DisplayName("Price")] public float Price { get; set; }
        [DisplayName("LayerHeight")] public float LayerHeight { get; set; }
        [DisplayName("ResolutionX")] public uint ResolutionX { get; set; }
        [DisplayName("ResolutionY")] public uint ResolutionY { get; set; }
        [DisplayName("MachineX")] public float DisplayWidth { get; set; }
        [DisplayName("DisplayHeight")] public float DisplayHeight { get; set; }
        [DisplayName("MachineZ")] public float MachineZ { get; set; }
        [DisplayName("Mirror")] public byte Mirror { get; set; }
        [DisplayName("LayerCount")] public uint LayerCount { get; set; }
        [DisplayName("BottomLayerCount")] public ushort BottomLayerCount { get; set; } = 4;
        [DisplayName("TransitionLayerCount")] public ushort TransitionLayerCount { get; set; } = 4;
        [DisplayName("BottomWaitTimeBeforeCure")] public float BottomWaitTimeBeforeCure { get; set; }
        [DisplayName("WaitTimeBeforeCure")] public float WaitTimeBeforeCure { get; set; }
        [DisplayName("BottomExposureTime")] public float BottomExposureTime { get; set; } = DefaultBottomExposureTime;
        [DisplayName("ExposureTime")] public float ExposureTime { get; set; } = DefaultExposureTime;
        [DisplayName("BottomWaitTimeAfterCure")] public float BottomWaitTimeAfterCure { get; set; }
        [DisplayName("WaitTimeAfterCure")] public float WaitTimeAfterCure { get; set; }
        [DisplayName("BottomLiftHeight")] public float BottomLiftHeight { get; set; } = DefaultBottomLiftHeight;
        [DisplayName("BottomLiftSpeed")] public float BottomLiftSpeed { get; set; } = DefaultBottomLiftSpeed;
        [DisplayName("BottomLiftAcceleration")] public float BottomLiftAcceleration { get; set; }
        [DisplayName("BottomLiftHeight2")] public float BottomLiftHeight2 { get; set; } = DefaultBottomLiftHeight2;
        [DisplayName("BottomLiftSpeed2")] public float BottomLiftSpeed2 { get; set; } = DefaultBottomLiftSpeed2;
        [DisplayName("BottomLiftAcceleration2")] public float BottomLiftAcceleration2 { get; set; }
        [DisplayName("BottomRetractSpeed")] public float BottomRetractSpeed { get; set; } = DefaultBottomRetractSpeed;
        [DisplayName("BottomRetractAcceleration")] public float BottomRetractAcceleration { get; set; }
        [DisplayName("BottomRetractHeight2")] public float BottomRetractHeight2 { get; set; } = DefaultBottomRetractHeight2;
        [DisplayName("BottomRetractSpeed2")] public float BottomRetractSpeed2 { get; set; } = DefaultBottomRetractSpeed;
        [DisplayName("BottomRetractAcceleration2")] public float BottomRetractAcceleration2 { get; set; }
        [DisplayName("LiftHeight")] public float LiftHeight { get; set; } = DefaultLiftHeight;
        [DisplayName("LiftSpeed")] public float LiftSpeed { get; set; } = DefaultLiftSpeed;
        [DisplayName("LiftAcceleration")] public float LiftAcceleration { get; set; }
        [DisplayName("LiftHeight2")] public float LiftHeight2 { get; set; } = DefaultLiftHeight2;
        [DisplayName("LiftSpeed2")] public float LiftSpeed2 { get; set; } = DefaultLiftSpeed2;
        [DisplayName("LiftAcceleration2")] public float LiftAcceleration2 { get; set; }
        [DisplayName("RetractSpeed")] public float RetractSpeed { get; set; } = DefaultRetractSpeed;
        [DisplayName("RetractAcceleration")] public float RetractAcceleration { get; set; }
        [DisplayName("RetractHeight2")] public float RetractHeight2 { get; set; } = DefaultRetractHeight2;
        [DisplayName("RetractSpeed2")] public float RetractSpeed2 { get; set; } = DefaultRetractSpeed;
        [DisplayName("RetractAcceleration2")] public float RetractAcceleration2 { get; set; }
        [DisplayName("BottomWaitTimeAfterLift")] public float BottomWaitTimeAfterLift { get; set; }
        [DisplayName("WaitTimeAfterLift")] public float WaitTimeAfterLift { get; set; }

        [DisplayName("BottomLightPWM")] public byte BottomLightPWM { get; set; } = DefaultBottomLightPWM;
        [DisplayName("LightPWM")] public byte LightPWM { get; set; } = DefaultLightPWM;
        [DisplayName("AntiAliasing")] public byte AntiAliasing { get; set; } = 1;
        [DisplayName("BoundingRectanglePx")] public Rectangle BoundingRectanglePx { get; set; }
        [DisplayName("BoundingRectangleMm")] public RectangleF BoundingRectangleMm { get; set; }
    }

    #endregion

    #region Properties
    public Header HeaderSettings { get; } = new();
        
    public override FileFormatType FileType => FileFormatType.Archive;

    public override string? ConvertMenuGroup => "Klipper";

    public override FileExtension[] FileExtensions { get; } = {
        new(typeof(KlipperFile), "zip", "Klipper (Mono)"),
        new(typeof(KlipperFile), "rgb.zip", "Klipper (RGB)")
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
        PrintParameterModifier.BottomLiftAcceleration,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed,
        PrintParameterModifier.LiftAcceleration,

        PrintParameterModifier.BottomLiftHeight2,
        PrintParameterModifier.BottomLiftSpeed2,
        PrintParameterModifier.BottomLiftAcceleration2,
        PrintParameterModifier.LiftHeight2,
        PrintParameterModifier.LiftSpeed2,
        PrintParameterModifier.LiftAcceleration2,

        PrintParameterModifier.BottomWaitTimeAfterLift,
        PrintParameterModifier.WaitTimeAfterLift,

        PrintParameterModifier.BottomRetractSpeed,
        PrintParameterModifier.BottomRetractAcceleration,
        PrintParameterModifier.RetractSpeed,
        PrintParameterModifier.RetractAcceleration,

        PrintParameterModifier.BottomRetractHeight2,
        PrintParameterModifier.BottomRetractSpeed2,
        PrintParameterModifier.BottomRetractAcceleration2,
        PrintParameterModifier.RetractHeight2,
        PrintParameterModifier.RetractSpeed2,
        PrintParameterModifier.RetractAcceleration2,

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
        PrintParameterModifier.LiftAcceleration,
        PrintParameterModifier.LiftHeight2,
        PrintParameterModifier.LiftSpeed2,
        PrintParameterModifier.LiftAcceleration2,
        PrintParameterModifier.WaitTimeAfterLift,
        PrintParameterModifier.RetractSpeed,
        PrintParameterModifier.RetractAcceleration,
        PrintParameterModifier.RetractHeight2,
        PrintParameterModifier.RetractSpeed2,
        PrintParameterModifier.RetractAcceleration2,
        PrintParameterModifier.LightPWM,
        PrintParameterModifier.Pause,
        PrintParameterModifier.ChangeResin,
    };

    public override Size[] ThumbnailsOriginalSize { get; } =
    {
        new(32, 32), 
        new(400, 300)
    };

    public override uint ResolutionX
    {
        get => HeaderSettings.ResolutionX;
        set => base.ResolutionX = HeaderSettings.ResolutionX = value;
    }

    public override uint ResolutionY
    {
        get => HeaderSettings.ResolutionY;
        set => base.ResolutionY = HeaderSettings.ResolutionY = value;
    }

    public override float DisplayWidth
    {
        get => HeaderSettings.DisplayWidth;
        set => base.DisplayWidth = HeaderSettings.DisplayWidth = RoundDisplaySize(value);
    }

    public override float DisplayHeight
    {
        get => HeaderSettings.DisplayHeight;
        set => base.DisplayHeight = HeaderSettings.DisplayHeight = RoundDisplaySize(value); 
    }

    public override float MachineZ
    {
        get => HeaderSettings.MachineZ > 0 ? HeaderSettings.MachineZ : base.MachineZ;
        set => base.MachineZ = HeaderSettings.MachineZ = (float)Math.Round(value, 2);
    }

    public override FlipDirection DisplayMirror
    {
        get =>
            HeaderSettings.Mirror switch
            {
                1 => FlipDirection.Horizontally,
                2 => FlipDirection.Vertically,
                3 => FlipDirection.Both,
                _ => FlipDirection.None
            };
        set
        {
            HeaderSettings.Mirror = value switch
            {
                FlipDirection.None => 0,
                FlipDirection.Horizontally => 1,
                FlipDirection.Vertically => 2,
                FlipDirection.Both => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };

            RaisePropertyChanged();
        }
    }

    public override byte AntiAliasing
    {
        get => HeaderSettings.AntiAliasing;
        set => base.AntiAliasing = HeaderSettings.AntiAliasing = Math.Clamp(value, (byte)1, (byte)16);
    }

    public override float LayerHeight
    {
        get => HeaderSettings.LayerHeight;
        set => base.LayerHeight = HeaderSettings.LayerHeight = Layer.RoundHeight(value);
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = HeaderSettings.LayerCount = base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => HeaderSettings.BottomLayerCount;
        set => base.BottomLayerCount = HeaderSettings.BottomLayerCount = value;
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
        get => HeaderSettings.BottomWaitTimeBeforeCure;
        set => base.BottomWaitTimeBeforeCure = HeaderSettings.BottomWaitTimeBeforeCure = (float)Math.Round(value, 2);
    }

    public override float WaitTimeBeforeCure
    {
        get => HeaderSettings.WaitTimeBeforeCure;
        set => base.WaitTimeBeforeCure = HeaderSettings.WaitTimeBeforeCure = (float)Math.Round(value, 2);
    }

    public override float BottomExposureTime
    {
        get => HeaderSettings.BottomExposureTime;
        set => base.BottomExposureTime = HeaderSettings.BottomExposureTime = (float)Math.Round(value, 2);
    }

    public override float ExposureTime
    {
        get => HeaderSettings.ExposureTime;
        set => base.ExposureTime = HeaderSettings.ExposureTime = (float)Math.Round(value, 2);
    }

    public override float BottomWaitTimeAfterCure
    {
        get => HeaderSettings.BottomWaitTimeAfterCure;
        set => base.BottomWaitTimeAfterCure = HeaderSettings.BottomWaitTimeAfterCure = (float)Math.Round(value, 2);
    }

    public override float WaitTimeAfterCure
    {
        get => HeaderSettings.WaitTimeAfterCure;
        set => base.WaitTimeAfterCure = HeaderSettings.WaitTimeAfterCure = (float)Math.Round(value, 2);
    }

    public override float BottomLiftHeight
    {
        get => HeaderSettings.BottomLiftHeight;
        set => base.BottomLiftHeight = HeaderSettings.BottomLiftHeight = (float)Math.Round(value, 2);
    }

    public override float BottomLiftSpeed
    {
        get => HeaderSettings.BottomLiftSpeed;
        set => base.BottomLiftSpeed = HeaderSettings.BottomLiftSpeed = (float)Math.Round(value, 2);
    }

    public override float BottomLiftAcceleration
    {
        get => HeaderSettings.BottomLiftAcceleration;
        set => base.BottomLiftAcceleration = HeaderSettings.BottomLiftAcceleration = (float)Math.Round(Math.Max(0, value), 2);
    }

    public override float BottomLiftHeight2
    {
        get => HeaderSettings.BottomLiftHeight2;
        set => base.BottomLiftHeight2 = HeaderSettings.BottomLiftHeight2 = (float)Math.Round(value, 2);
    }

    public override float BottomLiftSpeed2
    {
        get => HeaderSettings.BottomLiftSpeed2;
        set => base.BottomLiftSpeed2 = HeaderSettings.BottomLiftSpeed2 = (float)Math.Round(value, 2);
    }

    public override float BottomLiftAcceleration2
    {
        get => HeaderSettings.BottomLiftAcceleration2;
        set => base.BottomLiftAcceleration2 = HeaderSettings.BottomLiftAcceleration2 = (float)Math.Round(Math.Max(0, value), 2);
    }

    public override float LiftHeight
    {
        get => HeaderSettings.LiftHeight;
        set => base.LiftHeight = HeaderSettings.LiftHeight = (float)Math.Round(value, 2);
    }

    public override float LiftSpeed
    {
        get => HeaderSettings.LiftSpeed;
        set => base.LiftSpeed = HeaderSettings.LiftSpeed = (float)Math.Round(value, 2);
    }

    public override float LiftAcceleration
    {
        get => HeaderSettings.LiftAcceleration;
        set => base.LiftAcceleration = HeaderSettings.LiftAcceleration = (float)Math.Round(Math.Max(0, value), 2);
    }

    public override float LiftHeight2
    {
        get => HeaderSettings.LiftHeight2;
        set => base.LiftHeight2 = HeaderSettings.LiftHeight2 = (float)Math.Round(value, 2);
    }

    public override float LiftSpeed2
    {
        get => HeaderSettings.LiftSpeed2;
        set => base.LiftSpeed2 = HeaderSettings.LiftSpeed2 = (float)Math.Round(value, 2);
    }

    public override float LiftAcceleration2
    {
        get => HeaderSettings.LiftAcceleration2;
        set => base.LiftAcceleration2 = HeaderSettings.LiftAcceleration2 = (float)Math.Round(Math.Max(0, value), 2);
    }

    public override float BottomWaitTimeAfterLift
    {
        get => HeaderSettings.BottomWaitTimeAfterLift;
        set => base.BottomWaitTimeAfterLift = HeaderSettings.BottomWaitTimeAfterLift = (float)Math.Round(value, 2);
    }

    public override float WaitTimeAfterLift
    {
        get => HeaderSettings.WaitTimeAfterLift;
        set => base.WaitTimeAfterLift = HeaderSettings.WaitTimeAfterLift = (float)Math.Round(value, 2);
    }

    public override float BottomRetractSpeed
    {
        get => HeaderSettings.BottomRetractSpeed;
        set => base.BottomRetractSpeed = HeaderSettings.BottomRetractSpeed = (float)Math.Round(value, 2);
    }

    public override float BottomRetractAcceleration
    {
        get => HeaderSettings.BottomRetractAcceleration;
        set => base.BottomRetractAcceleration = HeaderSettings.BottomRetractAcceleration = (float)Math.Round(Math.Max(0, value), 2);
    }

    public override float BottomRetractHeight2
    {
        get => HeaderSettings.BottomRetractHeight2;
        set => base.BottomRetractHeight2 = HeaderSettings.BottomRetractHeight2 = (float)Math.Round(value, 2);
    }

    public override float BottomRetractSpeed2
    {
        get => HeaderSettings.BottomRetractSpeed2;
        set => base.BottomRetractSpeed2 = HeaderSettings.BottomRetractSpeed2 = (float)Math.Round(value, 2);
    }

    public override float BottomRetractAcceleration2
    {
        get => HeaderSettings.BottomRetractAcceleration2;
        set => base.BottomRetractAcceleration2 = HeaderSettings.BottomRetractAcceleration2 = (float)Math.Round(Math.Max(0, value), 2);
    }

    public override float RetractSpeed
    {
        get => HeaderSettings.RetractSpeed;
        set => base.RetractSpeed = HeaderSettings.RetractSpeed = (float)Math.Round(value, 2);
    }

    public override float RetractAcceleration
    {
        get => HeaderSettings.RetractAcceleration;
        set => base.RetractAcceleration = HeaderSettings.RetractAcceleration = (float)Math.Round(Math.Max(0, value), 2);
    }

    public override float RetractHeight2
    {
        get => HeaderSettings.RetractHeight2;
        set => base.RetractHeight2 = HeaderSettings.RetractHeight2 = (float)Math.Round(value, 2);
    }

    public override float RetractSpeed2
    {
        get => HeaderSettings.RetractSpeed2;
        set => base.RetractSpeed2 = HeaderSettings.RetractSpeed2 = (float)Math.Round(value, 2);
    }

    public override float RetractAcceleration2
    {
        get => HeaderSettings.RetractAcceleration2;
        set => base.RetractAcceleration2 = HeaderSettings.RetractAcceleration2 = (float)Math.Round(Math.Max(0, value), 2);
    }

    public override byte BottomLightPWM
    {
        get => HeaderSettings.BottomLightPWM;
        set => base.BottomLightPWM = HeaderSettings.BottomLightPWM = value;
    }

    public override byte LightPWM
    {
        get => HeaderSettings.LightPWM;
        set => base.LightPWM = HeaderSettings.LightPWM = value;
    }

    public override float PrintTime
    {
        get => base.PrintTime;
        set
        {
            base.PrintTime = value;
            HeaderSettings.PrintTime = base.PrintTime;
        }
    }

    public override float MaterialGrams
    {
        get => (float) Math.Round(HeaderSettings.MaterialGrams, 3);
        set => base.MaterialGrams = HeaderSettings.MaterialGrams = (float)Math.Round(value, 3);
    }

    public override float MaterialCost
    {
        get => (float) Math.Round(HeaderSettings.Price, 3);
        set => base.MaterialCost = HeaderSettings.Price = (float)Math.Round(value, 3);
    }

    public override string? MaterialName
    {
        get => HeaderSettings.MaterialName;
        set => base.MaterialName = HeaderSettings.MaterialName = value ?? string.Empty;
    }

    public override string MachineName
    {
        get => HeaderSettings.MachineName;
        set => base.MachineName = HeaderSettings.MachineName = value;
    }

    public override object[] Configs => new object[] { HeaderSettings };

    #endregion

    #region Constructor
    public KlipperFile()
    {
        GCode = new GCodeBuilder
        {
            UseComments = true,
            GCodePositioningType = GCodeBuilder.GCodePositioningTypes.Absolute,
            GCodeSpeedUnit = GCodeBuilder.GCodeSpeedUnits.MillimetersPerMinute,
            GCodeTimeUnit = GCodeBuilder.GCodeTimeUnits.Milliseconds,
            GCodeShowImageType = GCodeBuilder.GCodeShowImageTypes.FilenamePng1Started,
            LayerMoveCommand = GCodeBuilder.GCodeMoveCommands.G1,
            EndGCodeMoveCommand = GCodeBuilder.GCodeMoveCommands.G1,
        };

        GCode.SetKlipperStandard();

        GCode.BeforeRebuildGCode += (sender, e) =>
        {
            OnBeforeEncode(true);
            GCode.AppendComment($" generated by PrusaSlicer 2.7.4+{RuntimeInformation.RuntimeIdentifier} on {DateTime.UtcNow.ToShortDateString()} at {DateTime.UtcNow.ToShortTimeString()} UTC");
        };

        GCode.BeforeWriteStartGCode += (sender, e) =>
        {
            GCode.AppendLine("MSLA_DISPLAY_VALIDATE RESOLUTION={0},{1} PIXEL={2},{3}", ResolutionX, ResolutionY, PixelWidth, PixelHeight);
            GCode.AppendLine($"SET_PRINT_STATS_INFO TOTAL_LAYER={LayerCount} LAYER=1 MATERIAL_TOTAL={MaterialMilliliters}" +
                             (string.IsNullOrWhiteSpace(MaterialName) ? string.Empty : $" MATERIAL_NAME=\"{MaterialName}\""));
            GCode.AppendLine($"PRINT_START " +
                             $"LAYERS={LayerCount} " +
                             $"HEIGHT={PrintHeight} " +
                             $"BOUNDS={BoundingRectangleMillimeters.X},{BoundingRectangleMillimeters.Y},{BoundingRectangleMillimeters.Width},{BoundingRectangleMillimeters.Height} " +
                             $"VOLUME={Volume} " +
                             $"MATERIALML={MaterialMilliliters}"
                             );
            
            e.Cancel = true;
        };

        GCode.BeforeWriteLayerGCode += (sender, e) =>
        {
            var layer = this[e.LayerIndex];
            GCode.AppendLine($"SET_PRINT_STATS_INFO CURRENT_LAYER={layer.Number}");
            GCode.AppendLine($"BEFORE_LAYER_CHANGE " +
                             $"LAYER={layer.Number} " +
                             $"HEIGHT={layer.PositionZ} " +
                             $"PIXELS={layer.NonZeroPixelCount} " +
                             $"BOUNDS={layer.BoundingRectangleMillimeters.X},{layer.BoundingRectangleMillimeters.Y},{layer.BoundingRectangleMillimeters.Width},{layer.BoundingRectangleMillimeters.Height} " +
                             $"AREA={layer.Area} " +
                             $"VOLUME={layer.Volume} " +
                             $"MATERIALML={layer.MaterialMilliliters}"
                             );
        };

        GCode.AfterWriteLayerGCode += (sender, e) =>
        {
            var layer = this[e.LayerIndex];
            GCode.AppendLine($"SET_PRINT_STATS_INFO CONSUME_MATERIAL={layer.MaterialMilliliters}");
            GCode.AppendLine("AFTER_LAYER_CHANGE");
        };

        GCode.BeforeWriteEndGCode += (sender, e) =>
        {
            GCode.AppendLine("PRINT_END");
            e.Cancel = true;
        };
        
        GCode.AfterRebuildGCode += (sender, e) =>
        {
            GCode.AppendLine();
            GCode.AppendComment($" total layers count = {LayerCount}");
            GCode.AppendComment($" layer_height = {LayerHeight}");
            GCode.AppendComment($" first_layer_height = {FirstLayer?.PositionZ ?? 0}");
            GCode.AppendComment($" resin name = {MaterialName}");
            GCode.AppendComment($" resin used [ml] = {MaterialMilliliters}");
            GCode.AppendComment($" resin used [g] = {MaterialGrams}");
            GCode.AppendComment($" resin cost = {MaterialCost}");
            GCode.AppendComment($" estimated printing time = {PrintTimeString}");
            GCode.AppendComment($" estimated first layer printing time = {FirstLayer?.PrintTimeString ?? "0"}");
        };
    }
    #endregion

    #region Methods

    public override bool CanProcess(string? fileFullPath)
    {
        if (!base.CanProcess(fileFullPath)) return false;

        try
        {
            using var zip = ZipFile.Open(fileFullPath!, ZipArchiveMode.Read);

            var foundKlipper = false;
            var foundGcode = false;
            foreach (var entry in zip.Entries)
            {
                if (entry.Name.EndsWith(KlipperFileIdentifier)) foundKlipper = true;
                else if (entry.Name.EndsWith(".gcode")) foundGcode = true;
            }

            return foundKlipper && foundGcode;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }


        return false;
    }

    protected override void OnBeforeEncode(bool isPartialEncode)
    {
        HeaderSettings.Filename = Filename!;
        HeaderSettings.Volume = Volume;
        HeaderSettings.MaterialMilliliters = MaterialMilliliters;
        HeaderSettings.BoundingRectanglePx = BoundingRectangle;
        HeaderSettings.BoundingRectangleMm = BoundingRectangleMillimeters;

        if (!isPartialEncode)
        {
            if (FileEndsWith(".rgb.zip"))
            {
                LayerImageFormat = ImageFormat.Png24;
            }
        }
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Create);

        // Dummy file for UVtools to recognize
        outputFile.CreateEntryFromContent(".klipper", string.Empty, ZipArchiveMode.Create);

        RebuildGCode();
        outputFile.CreateEntryFromContent($"{FilenameNoExt}.gcode", GCodeStr, ZipArchiveMode.Create);

        EncodeThumbnailsInZip(outputFile, progress, ThumbnailsEntryNames);
        EncodeLayersInZip(outputFile, IndexStartNumber.One, progress);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Read);
        var entry = inputFile.Entries.FirstOrDefault(entry => entry.Name.EndsWith(".gcode"));
        if (entry is null)
        {
            Clear();
            throw new FileLoadException("No gcode file found", FileFullPath);
        }

        using (var stream = entry.Open())
        {
            using TextReader tr = new StreamReader(stream);
            GCode!.Clear();
            while (tr.ReadLine() is { } line)
            {
                GCode.AppendLine(line);
                if (string.IsNullOrEmpty(line)) continue;

                if (line[0] == ';')
                {
                    var splitLine = line.Split(':', StringSplitOptions.TrimEntries);
                    if (splitLine.Length < 2) continue;

                    foreach (var propertyInfo in HeaderSettings.GetType()
                                 .GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                        if (displayNameAttribute is null) continue;
                        if (!splitLine[0].Trim(' ', ';').Equals(displayNameAttribute.DisplayName, StringComparison.OrdinalIgnoreCase)) continue;
                        propertyInfo.SetValueFromString(HeaderSettings, splitLine[1].Trim());
                    }
                    continue;
                }

                if (line.StartsWith("MSLA_DISPLAY_VALIDATE ", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(line, string.Format("RESOLUTION={0},{0} PIXEL={1},{1}", @"(\d+)", "([+-]?([0-9]*[.])?[0-9]+)"));
                    if (match is { Success: true, Groups.Count: >= 3 })
                    {
                        if (uint.TryParse(match.Groups[1].ValueSpan, out var resolutionX)) ResolutionX = resolutionX;
                        if (uint.TryParse(match.Groups[2].ValueSpan, out var resolutionY)) ResolutionY = resolutionY;
                        if (uint.TryParse(match.Groups[3].ValueSpan, out var pixelWidth)) DisplayWidth = resolutionX * pixelWidth;
                        if (uint.TryParse(match.Groups[4].ValueSpan, out var pixelHeight)) DisplayHeight = resolutionY * pixelHeight;
                    }

                    continue;
                }

                if (line.StartsWith("SET_PRINT_STATS_INFO ", StringComparison.OrdinalIgnoreCase) && LayerCount == 0)
                {
                    var match = Regex.Match(line, $"SET_PRINT_STATS_INFO TOTAL_LAYER={@"(\d+)"}");
                    if (match is { Success: true, Groups.Count: >= 3 })
                    {
                        if (uint.TryParse(match.Groups[1].ValueSpan, out var layerCount)) HeaderSettings.LayerCount = layerCount;
                    }

                    continue;
                }

                if (line.StartsWith("PRINT_START ", StringComparison.OrdinalIgnoreCase) && LayerCount == 0)
                {
                    var match = Regex.Match(line, $"LAYERS={@"(\d+)"}");
                    if (match is { Success: true, Groups.Count: >= 3 })
                    {
                        if (uint.TryParse(match.Groups[1].ValueSpan, out var layerCount)) HeaderSettings.LayerCount = layerCount;
                    }

                    continue;
                }
            }
        }

        if (HeaderSettings.LayerCount == 0)
        {
            foreach (var zipEntry in inputFile.Entries)
            {
                if(!zipEntry.Name.EndsWith(".png")) continue;
                var filename = Path.GetFileNameWithoutExtension(zipEntry.Name);
                if (!filename.All(char.IsDigit)) continue;
                if (!uint.TryParse(filename, out var layerIndex)) continue;
                HeaderSettings.LayerCount = Math.Max(HeaderSettings.LayerCount, layerIndex);
            }
        }

        Init(HeaderSettings.LayerCount, DecodeType == FileDecodeType.Partial);

        // Must discover png depth grayscale or color
        if (DecodeType == FileDecodeType.Full)
        {
            LayerImageFormat = FetchImageFormat(inputFile, ImageFormat.Png24RgbAA);
            DecodeLayersFromZip(inputFile, IndexStartNumber.One, progress);
        }
        

        GCode?.ParseLayersFromGCode(this);

        if (ThumbnailsCount == 0)
        {
            DecodeThumbnailsFromZip(inputFile, progress, ThumbnailsEntryNames);
        }
    }

    public override void RebuildGCode()
    {
        if (!SupportGCode || SuppressRebuildGCode) return;
        GCode?.RebuildGCode(this, new object[]{ HeaderSettings });
        RaisePropertyChanged(nameof(GCodeStr));
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Update);
        var entriesToRemove = outputFile.Entries.Where(zipEntry => zipEntry.Name.EndsWith(".gcode")).ToArray();
        foreach (var zipEntry in entriesToRemove)
        {
            zipEntry.Delete();
        }

        outputFile.CreateEntryFromContent($"{FilenameNoExt}.gcode", GCodeStr, ZipArchiveMode.Update);

        //Decode(FileFullPath, progress);
    }
    #endregion
}