/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using BinarySerialization;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UVtools.Core.Converters;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using UVtools.Core.Printer;

namespace UVtools.Core.FileFormats;

/// <summary>
/// This file format is based on B9Creator job file (.b9j) with defined version = 3
/// and added several new fields (as example, preview image).
/// Some of the format features are not recommended to use (BaseLayersCount and FilledBaseLayersCount).
/// </summary>
public sealed class AnetFile : FileFormat
{
    #region Constants

    private const uint DEFAULT_VERSION = 3;

    public const ushort RESOLUTION_N4_X = 1440;
    public const ushort RESOLUTION_N4_Y = 2560;

    public const float DISPLAY_N4_WIDTH = 68.04f;
    public const float DISPLAY_N4_HEIGHT = 120.96f;
    public const float MACHINE_N4_Z = 135f;

    public const ushort RESOLUTION_N7_X = 2560;
    public const ushort RESOLUTION_N7_Y = 1600;

    public const float DISPLAY_N7_WIDTH = 192f;
    public const float DISPLAY_N7_HEIGHT = 120f;
    public const float MACHINE_N7_Z = 300f;

    /// <summary>
    /// Printer uses incorrect BMP header for preview image so we need to use it as-is instead of generating.
    /// </summary>
    private static readonly byte[] BmpHeader = { 66, 77, 162, 4, 0, 0, 0, 0, 0, 0, 66, 0, 0, 0, 40, 0, 0, 0, 4, 1, 0, 0, 140, 0, 0, 0, 1, 0, 16, 0, 3, 0, 0, 0, 96, 4, 0, 0, 18, 11, 0, 0, 18, 11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 248, 0, 0, 224, 7, 0, 0, 31, 0, 0, 0 };

    #endregion

    #region Enums

    public enum AnetPrinter : byte
    {
        N4,
        N7
    }

    #endregion

    #region Sub Classes

    #region Header

    public class Header
    {
        [FieldOrder(0)][FieldEndianness(Endianness.Big)] public int VersionLength { get; set; } = 2;
        [FieldOrder(1)][FieldEncoding("UTF-16BE")][FieldLength(nameof(VersionLength))] public string Version { get; set; } = "3";
        [FieldOrder(2)][FieldEndianness(Endianness.Big)] public int NameLength { get; set; } = About.SoftwareWithVersion.Length * 2;
        [FieldOrder(3)][FieldEncoding("UTF-16BE")][FieldLength(nameof(NameLength))] public string Name { get; set; } = About.SoftwareWithVersion;
        [FieldOrder(4)][FieldEndianness(Endianness.Big)] public int DescriptionLength { get; set; }
        [FieldOrder(5)][FieldEncoding("UTF-16BE")][FieldLength(nameof(DescriptionLength))] public string Description { get; set; } = string.Empty; // Printer crashes for non-empty description (FW 1.65)
        [FieldOrder(6)][FieldEndianness(Endianness.Big)] public double XYPixelSize { get; set; } = 0.04725; // mm
        [FieldOrder(7)][FieldEndianness(Endianness.Big)] public double LayerHeight { get; set; } // mm; from 0.03 to 0.08
        [FieldOrder(8)][FieldEndianness(Endianness.Big)] public uint BaseLayersCount { get; set; } // Number of extent filled additional first layers; do not use!
        [FieldOrder(9)][FieldEndianness(Endianness.Big)] public uint FilledBaseLayersCount { get; set; } // Number of fully filled first layers inside BaseLayersCount; do not use!
        [FieldOrder(10)][FieldEndianness(Endianness.Big)] public uint ExposureTime { get; set; } = 6; // from 3 to 25
        [FieldOrder(11)][FieldEndianness(Endianness.Big)] public uint BottomExposureTime { get; set; } = 90; // from 60 to 120
        [FieldOrder(12)][FieldEndianness(Endianness.Big)] public uint BottomLayerCount { get; set; } = DefaultBottomLayerCount; // from 2 to 10
        [FieldOrder(13)][FieldEndianness(Endianness.Big)] public uint LiftSpeed { get; set; } = (uint)Math.Ceiling(SpeedConverter.Convert(DefaultLiftSpeed, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond)); // mm/s, from 1 to 10
        [FieldOrder(14)][FieldEndianness(Endianness.Big)] public uint LiftHeight { get; set; } = (uint)DefaultLiftHeight; // mm, from 3 to 10

        [FieldOrder(15)][FieldEndianness(Endianness.Big)] public uint PreviewResolutionX { get; set; } = 260;
        [FieldOrder(16)][FieldEndianness(Endianness.Big)] public uint PreviewResolutionY { get; set; } = 140;
        [FieldOrder(17)][FieldEndianness(Endianness.Big)] public uint PreviewSize { get; set; } = 72866;
        [FieldOrder(18)][FieldEndianness(Endianness.Big)][FieldLength(nameof(PreviewSize))] public byte[] PreviewContent { get; set; } = Array.Empty<byte>(); // BMP image, BGR565
        [FieldOrder(19)][FieldEndianness(Endianness.Big)] public double VolumeMicroL { get; set; } // µl
        [FieldOrder(20)][FieldEndianness(Endianness.Big)] public uint EncodedPrintTime { get; set; } // s; for unknown reason always broken in original slicer
        [FieldOrder(21)][FieldEndianness(Endianness.Big)] public uint LayersCount { get; set; } // Not include BaseLayers

        public override string ToString()
        {
            return $"{nameof(VersionLength)}: {VersionLength}, {nameof(Version)}: {Version}, {nameof(NameLength)}: {NameLength}, {nameof(Name)}: {Name}, {nameof(DescriptionLength)}: {DescriptionLength}, {nameof(Description)}: {Description}, {nameof(XYPixelSize)}: {XYPixelSize}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(BaseLayersCount)}: {BaseLayersCount}, {nameof(FilledBaseLayersCount)}: {FilledBaseLayersCount}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(BottomExposureTime)}: {BottomExposureTime}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(PreviewResolutionX)}: {PreviewResolutionX}, {nameof(PreviewResolutionY)}: {PreviewResolutionY}, {nameof(PreviewSize)}: {PreviewSize}, {nameof(PreviewContent)}: {PreviewContent}, {nameof(VolumeMicroL)}: {VolumeMicroL}, {nameof(EncodedPrintTime)}: {EncodedPrintTime}, {nameof(LayersCount)}: {LayersCount}";
        }
    }

    #endregion

    #region LayerDef

    public class LayerDef
    {
        /// <summary>
        /// White pixels region (border including corner pixels)
        /// </summary>
        [FieldOrder(0)][FieldEndianness(Endianness.Big)] public uint WhitePixelsCount { get; set; }
        [FieldOrder(1)][FieldEndianness(Endianness.Big)] public int XMin { get; set; }
        [FieldOrder(2)][FieldEndianness(Endianness.Big)] public int YMin { get; set; }
        [FieldOrder(3)][FieldEndianness(Endianness.Big)] public int XMax { get; set; }
        [FieldOrder(4)][FieldEndianness(Endianness.Big)] public int YMax { get; set; }
        [FieldOrder(5)][FieldEndianness(Endianness.Big)] public uint BitsCount { get; set; }
        [Ignore]
        public uint RleBytesCount
        {
            get => (BitsCount + 7) >> 3;
            set => BitsCount = value << 3;
        }

        [Ignore] public byte[] EncodedRle { get; set; } = null!;

        public LayerDef()
        {
        }
        
        public void SetFrom(Layer layer)
        {
            WhitePixelsCount = layer.NonZeroPixelCount; // To be re-set latter while encoding
            XMin = layer.BoundingRectangle.X;
            YMin = layer.BoundingRectangle.Y;
            if (layer.BoundingRectangle.Right > 0) XMax = layer.BoundingRectangle.Right - 1;
            if (layer.BoundingRectangle.Bottom > 0) YMax = layer.BoundingRectangle.Bottom - 1;
        }

        public override string ToString()
        {
            return $"{nameof(WhitePixelsCount)}: {WhitePixelsCount}, {nameof(XMin)}: {XMin}, {nameof(YMin)}: {YMin}, {nameof(XMax)}: {XMax}, {nameof(YMax)}: {YMax}, {nameof(BitsCount)}: {BitsCount}, {nameof(RleBytesCount)}: {RleBytesCount}, {nameof(EncodedRle)}: {EncodedRle?.Length}";
        }

        public byte[] Encode(Mat mat)
        {
            uint ComputeRepeatsSize(uint repeats)
            {
                return (uint)Math.Ceiling(Math.Log2(repeats));
            }

            void SetBits(List<byte> data, uint pos, uint value, uint count = 1)
            {
                if (data.Count * 8 < pos + count)
                {
                    data.AddRange(new byte[(pos + count + 7 - data.Count * 8) / 8]);
                }

                for (int off = (int)(pos + count - 1); off + 1 > pos; --off)
                {
                    byte tmp = data[off / 8];
                    byte mask = (byte)(1 << (off % 8));
                    data[off / 8] = ((int)value & 1) == 1 ? (byte)(tmp | mask) : (byte)(tmp & ~mask);
                    value >>= 1;
                }
            }

            // Format does not have a global resolution field but supports multiple resolutions
            /*if (mat.Width != RESOLUTION_X || mat.Height != RESOLUTION_Y)
            {
                throw new ArgumentException($"Anet N4 support only {RESOLUTION_X}x{RESOLUTION_Y} image, got {mat.Width}x{mat.Height}");
            }*/

            WhitePixelsCount = 0;
            uint singleColorLength = 0;
            uint compressedPos = 33;

            var rawData = new List<byte>();
            var spanMat = mat.GetDataByteSpan();

            bool isWhitePrev = spanMat[0] > 127;

            SetBits(rawData, 0, (uint)mat.Width, 16);
            SetBits(rawData, 16, (uint)mat.Height, 16);
            SetBits(rawData, 32, isWhitePrev ? 1u : 0u);

            for (int i = 0; i < spanMat.Length; i++)
            {
                bool isWhiteCurrent = spanMat[i] > 127; // No AA

                if (isWhiteCurrent)
                {
                    WhitePixelsCount++;
                }

                if (isWhiteCurrent == isWhitePrev)
                {
                    singleColorLength++;
                }

                if (isWhiteCurrent != isWhitePrev || i == spanMat.Length - 1)
                {
                    isWhitePrev = isWhiteCurrent;
                    var repeatsSize = ComputeRepeatsSize(singleColorLength);
                    SetBits(rawData, compressedPos, repeatsSize, 5);
                    SetBits(rawData, compressedPos + 5, singleColorLength, repeatsSize + 1);
                    compressedPos += 6 + repeatsSize;
                    singleColorLength = 1;
                }
            }

            EncodedRle = rawData.ToArray();
            RleBytesCount = (uint)EncodedRle.Length;
            BitsCount = compressedPos;

            return EncodedRle;
        }

        public Mat Decode(out uint resolutionX, out uint resolutionY, bool consumeRle = true)
        {
            uint GetBits(uint pos, uint count = 1)
            {
                if (pos + count > BitsCount)
                {
                    throw new IndexOutOfRangeException($"Trying to read {count} bits from pos {pos}, but total size is {BitsCount} bits.");
                }

                uint res = 0;
                for (uint i = pos; i < pos + count; i++)
                {
                    res <<= 1;
                    if ((EncodedRle[i >> 0x3] & (byte)(0x1u << (int)(i & 0x7u))) != 0)
                    {
                        res |= 1;
                    }
                }
                return res;
            }

            if ((BitsCount + 7) / 8 != EncodedRle.Length)
            {
                throw new IndexOutOfRangeException($"Incorrect RLE data size {EncodedRle.Length * 8}, except {BitsCount} bits.");
            }

            resolutionX = GetBits(0, 16);
            resolutionY = GetBits(16, 16);

            var mat = EmguExtensions.InitMat(new Size((int)resolutionX, (int)resolutionY));
            var imageLength = mat.GetLength();

            byte brightness = (byte)((GetBits(32) == 1) ? 0xff : 0x0);

            int pixelPos = 0;
            uint bitPos = 33;
            while (pixelPos < imageLength)
            {
                uint keySize = GetBits(bitPos, 5);
                uint stripSize = GetBits(bitPos + 5, keySize + 1);
                bitPos += keySize + 6;
                mat.FillSpan(ref pixelPos, (int)stripSize, brightness);
                brightness = (byte)~brightness;
            }

            if (consumeRle) EncodedRle = null!;

            return mat;
        }
    }
    #endregion

    #endregion

    #region Properties

    public Header HeaderSettings { get; private set; } = new();

    public override FileFormatType FileType => FileFormatType.Binary;

    public override string ConvertMenuGroup => "Anet";

    public override FileExtension[] FileExtensions { get; } = {
        new(typeof(AnetFile), "n4", "Anet N4"),
        new(typeof(AnetFile), "n7", "Anet N7"),
    };

    public override SpeedUnit FormatSpeedUnit => SpeedUnit.MillimetersPerSecond;

    public override byte AntiAliasing => 1; // Format does not support anti-aliasing

    public override PrintParameterModifier[]? PrintParameterModifiers { get; } =
    {
        PrintParameterModifier.ExposureTime,
        PrintParameterModifier.BottomExposureTime,
        PrintParameterModifier.BottomLayerCount,
        PrintParameterModifier.LiftSpeed,
        PrintParameterModifier.LiftHeight,
    };

    public override Size[]? ThumbnailsOriginalSize { get; } = { new(260, 140) };
    public override uint[] AvailableVersions { get; } = { 3 };

    public override uint DefaultVersion => DEFAULT_VERSION;

    public override uint Version
    {
        get => uint.Parse(HeaderSettings.Version);
        set
        {
            base.Version = value;
            HeaderSettings.Version = base.Version.ToString();
        }
    }

    public override FlipDirection DisplayMirror
    {
        get => FlipDirection.Horizontally;
        set { }
    }

    public override float LayerHeight
    {
        get => Layer.RoundHeight((float)HeaderSettings.LayerHeight);
        set
        {
            HeaderSettings.LayerHeight = Layer.RoundHeight((double)value);
            RaisePropertyChanged();
        }
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = HeaderSettings.LayersCount = LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => (ushort)HeaderSettings.BottomLayerCount;
        set => base.BottomLayerCount = (ushort)(HeaderSettings.BottomLayerCount = value);
    }

    public override float BottomExposureTime
    {
        get => HeaderSettings.BottomExposureTime;
        set => base.BottomExposureTime = HeaderSettings.BottomExposureTime = (uint)Math.Round(value);
    }

    public override float ExposureTime
    {
        get => HeaderSettings.ExposureTime;
        set => base.ExposureTime = HeaderSettings.ExposureTime = (uint)Math.Round(value);
    }

    public override float BottomLiftHeight => LiftHeight;

    public override float LiftHeight
    {
        get => HeaderSettings.LiftHeight;
        set => base.LiftHeight = HeaderSettings.LiftHeight = (uint)Math.Round(value);
    }

    public override float BottomLiftSpeed => LiftSpeed;

    public override float LiftSpeed
    {
        get => SpeedConverter.Convert(HeaderSettings.LiftSpeed, SpeedUnit.MillimetersPerSecond, CoreSpeedUnit);
        set
        {
            HeaderSettings.LiftSpeed = (uint)Math.Round(SpeedConverter.Convert(value, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond));
            base.LiftSpeed = SpeedConverter.Convert(HeaderSettings.LiftSpeed, SpeedUnit.MillimetersPerSecond, CoreSpeedUnit);
        }
    }

    public override float BottomRetractSpeed => LiftSpeed;

    public override float RetractSpeed => LiftSpeed;

    public override float PrintTime
    {
        get => base.PrintTime;
        set
        {
            base.PrintTime = value;
            HeaderSettings.EncodedPrintTime = (uint)base.PrintTime;
        }
    }

    public override float MaterialMilliliters
    {
        get => base.MaterialMilliliters;
        set
        {
            base.MaterialMilliliters = value;
            HeaderSettings.VolumeMicroL = Math.Round(base.MaterialMilliliters * 1000.0, 3);
        }
    }

    public override object[] Configs => new object[] { HeaderSettings };

    public AnetPrinter PrinterModel
    {
        get
        {
            if (FileEndsWith(".n4"))
            {
                return AnetPrinter.N4;
            }

            if (FileEndsWith(".n7"))
            {
                return AnetPrinter.N7;
            }

            return AnetPrinter.N4;
        }
    }

    #endregion

    #region Constructors

    public AnetFile() { }
    #endregion

    #region Methods

    protected override void OnBeforeEncode(bool isPartialEncode)
    {
        switch (PrinterModel)
        {
            case AnetPrinter.N4:
                if (ResolutionX != RESOLUTION_N4_X || ResolutionY != RESOLUTION_N4_Y)
                    throw new ArgumentException($"Anet N4 support only {RESOLUTION_N4_X}x{RESOLUTION_N4_Y} image, got {ResolutionX}x{ResolutionY}");
                break;
            case AnetPrinter.N7:
                if (ResolutionX != RESOLUTION_N7_X || ResolutionY != RESOLUTION_N7_Y)
                    throw new ArgumentException($"Anet N7 support only {RESOLUTION_N7_X}x{RESOLUTION_N7_Y} image, got {ResolutionX}x{ResolutionY}");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(PrinterModel), FileExtension);
        }
        HeaderSettings.XYPixelSize = Math.Round(PixelSizeMax, 3);
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Create, FileAccess.Write);

        HeaderSettings.Name = FilenameNoExt!;
        HeaderSettings.Description = string.Empty; // $"{About.SoftwareWithVersion} @ {DateTime.Now}";

        var previewBuffer = new byte[72866]; // 72866
        BmpHeader.CopyTo(previewBuffer, 0);

        if (CreatedThumbnailsCount > 0)
        {
            EncodeImage(DATATYPE_BGR565, Thumbnails[0]!).CopyTo(previewBuffer, BmpHeader.Length);
        }

        HeaderSettings.PreviewContent = previewBuffer;

        outputFile.WriteSerialize(HeaderSettings);

        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        var layerData = new LayerDef[LayerCount];

        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                var layer = this[layerIndex];
                using var mat = layer.LayerMat;
                layerData[layerIndex] = new LayerDef();
                layerData[layerIndex].SetFrom(layer);
                layerData[layerIndex].Encode(mat);
                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                progress.ThrowIfCancellationRequested();

                outputFile.WriteSerialize(layerData[layerIndex]);
                outputFile.WriteBytes(layerData[layerIndex].EncodedRle);

                layerData[layerIndex].EncodedRle = null!; // Free
            }
        }

        outputFile.WriteBytes(new byte[4]); // Supports count, always 0

        Debug.WriteLine("Encode Results:");
        Debug.WriteLine(HeaderSettings);
        Debug.WriteLine("-End-");
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = new FileStream(FileFullPath!, FileMode.Open, FileAccess.Read);
        HeaderSettings = Helpers.Deserialize<Header>(inputFile);
        if (HeaderSettings.Version is not "3")
        {
            throw new FileLoadException($"Not a valid N4 file: Version doesn't match, got {HeaderSettings.Version} instead of 3)", FileFullPath);
        }

        if (HeaderSettings.PreviewSize != 72866 || HeaderSettings.PreviewContent is null)
        {
            throw new FileLoadException($"Not a valid N4 file: incorrect preview format ({HeaderSettings.PreviewSize})", FileFullPath);
        }

        Thumbnails[0] = DecodeImage(DATATYPE_BGR565, HeaderSettings.PreviewContent[BmpHeader.Length..], ThumbnailsOriginalSize![0]);

        Debug.WriteLine(HeaderSettings);

        Init(HeaderSettings.LayersCount, DecodeType == FileDecodeType.Partial);
        var layersDefinitions = new LayerDef[HeaderSettings.LayersCount];

        progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
        foreach (var batch in BatchLayersIndexes())
        {
            foreach (var layerIndex in batch)
            {
                progress.ThrowIfCancellationRequested();

                var layerDef = Helpers.Deserialize<LayerDef>(inputFile);
                layersDefinitions[layerIndex] = layerDef;

                if (DecodeType == FileDecodeType.Full)
                {
                    layerDef.EncodedRle = inputFile.ReadBytes(layerDef.RleBytesCount);
                }
                else
                {
                    inputFile.Seek(layerDef.RleBytesCount, SeekOrigin.Current);
                }

                Debug.Write($"LAYER {layerIndex} -> ");
                Debug.WriteLine(layerDef);
            }

            if (DecodeType == FileDecodeType.Full)
            {
                Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
                {
                    using var mat = layersDefinitions[layerIndex].Decode(out var resolutionX, out var resolutionY);
                    if (layerIndex == 0) // Set file resolution from first layer RLE. Figure out other properties after that
                    {
                        ResolutionX = resolutionX;
                        ResolutionY = resolutionY;

                        var machine = Machine.Machines.FirstOrDefault(machine =>
                            machine.Brand == PrinterBrand.Anet && machine.ResolutionX == resolutionX &&
                            machine.ResolutionY == resolutionY);

                        if (machine is not null)
                        {
                            DisplayWidth = machine.DisplayWidth;
                            DisplayHeight = machine.DisplayHeight;
                            MachineZ = machine.MachineZ;
                            MachineName = machine.Name;
                        }
                    }
                    _layers[layerIndex] = new Layer((uint)layerIndex, mat, this);
                    progress.LockAndIncrement();
                });
            }
        }

        RebuildLayersProperties();
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Open, FileAccess.Write);
        outputFile.Seek(0, SeekOrigin.Begin);
        outputFile.WriteSerialize(HeaderSettings);
    }

    #endregion
}
