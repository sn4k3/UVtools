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
using System.Threading.Tasks;
using UVtools.Core.Converters;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

/// <summary>
/// This file format is based on B9Creator job file (.b9j) with defined version = 3
/// and added several new fields (as example, preview image).
/// Some of the format features are not recommended to use (BaseLayersCount and FilledBaseLayersCount).
/// </summary>
public class AnetN4File : FileFormat
{
    #region Constants

    public const ushort RESOLUTION_X = 1440;
    public const ushort RESOLUTION_Y = 2560;

    public const float DISPLAY_WIDTH = 68.04f;
    public const float DISPLAY_HEIGHT = 120.96f;
    public const float MACHINE_Z = 135f;

    #endregion

    #region Members

    private uint _resolutionX = RESOLUTION_X;
    private uint _resolutionY = RESOLUTION_Y;

    // Printer uses incorrect BMP header for preview image so we need to use it as-is instead of generating.
    private byte[] _bmpHeader = { 66, 77, 162, 4, 0, 0, 0, 0, 0, 0, 66, 0, 0, 0, 40, 0, 0, 0, 4, 1, 0, 0, 140, 0, 0, 0, 1, 0, 16, 0, 3, 0, 0, 0, 96, 4, 0, 0, 18, 11, 0, 0, 18, 11, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 248, 0, 0, 224, 7, 0, 0, 31, 0, 0, 0 };

    #endregion

    #region Sub Classes

    #region Header

    public class Header
    {
        [FieldOrder(0)][FieldEndianness(Endianness.Big)] public int VersionLen { get; set; }
        [FieldOrder(1)][FieldEncoding("UTF-16BE")][FieldLength(nameof(VersionLen))][SerializeAs(SerializedType.SizedString)] public string? Version { get; set; } = "3";
        [FieldOrder(2)][FieldEndianness(Endianness.Big)] public int NameLength { get; set; }
        [FieldOrder(3)][FieldEncoding("UTF-16BE")][FieldLength(nameof(NameLength))][SerializeAs(SerializedType.SizedString)] public string? Name { get; set; }
        [FieldOrder(4)][FieldEndianness(Endianness.Big)] public int DescriptionLength { get; set; }
        [FieldOrder(5)][FieldEncoding("UTF-16BE")][FieldLength(nameof(DescriptionLength))][SerializeAs(SerializedType.SizedString)] public string? Description { get; set; }
        [FieldOrder(6)][FieldEndianness(Endianness.Big)] public double XYPixelSize { get; set; } = 0.04725; // mm
        [FieldOrder(7)][FieldEndianness(Endianness.Big)] public double LayerHeight { get; set; } // mm; from 0.03 to 0.08
        [FieldOrder(8)][FieldEndianness(Endianness.Big)] public uint BaseLayersCount { get; set; } = 0; // Number of extent filled additional first layers; do not use!
        [FieldOrder(9)][FieldEndianness(Endianness.Big)] public uint FilledBaseLayersCount { get; set; } = 0; // Number of fully filled first layers inside BaseLayersCount; do not use!
        [FieldOrder(10)][FieldEndianness(Endianness.Big)] public uint ExposureSeconds { get; set; } // from 3 to 25
        [FieldOrder(11)][FieldEndianness(Endianness.Big)] public uint BottomExposureSeconds { get; set; } // from 60 to 120
        [FieldOrder(12)][FieldEndianness(Endianness.Big)] public uint BottomLayerCount { get; set; } // from 2 to 10
        [FieldOrder(13)][FieldEndianness(Endianness.Big)] public uint LiftSpeed { get; set; } = (uint)Math.Ceiling(SpeedConverter.Convert(DefaultLiftSpeed, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond)); // mm/s, from 1 to 10
        [FieldOrder(14)][FieldEndianness(Endianness.Big)] public uint LiftHeight { get; set; } = (uint)DefaultLiftHeight; // mm, from 3 to 10

        [FieldOrder(15)][FieldEndianness(Endianness.Big)] public uint PreviewResolutionX { get; set; } = 260;
        [FieldOrder(16)][FieldEndianness(Endianness.Big)] public uint PreviewResolutionY { get; set; } = 140;
        [FieldOrder(17)][FieldEndianness(Endianness.Big)] public uint PreviewSize { get; set; }
        [FieldOrder(18)][FieldEndianness(Endianness.Big)][FieldLength(nameof(PreviewSize))] public byte[]? PreviewContent { get; set; } // BMP image, BGR565
        [FieldOrder(19)][FieldEndianness(Endianness.Big)] public double VolumeMicroL { get; set; } // µl
        [FieldOrder(20)][FieldEndianness(Endianness.Big)] public int EncodedPrintTime { get; set; } = 0; // s; for unknown reason always broken in original slicer
        [FieldOrder(21)][FieldEndianness(Endianness.Big)] public uint LayersCount { get; set; } // Not include BaseLayers

        public override string ToString()
        {
            return $"{nameof(Version)}: {Version}, {nameof(Name)}: {Name}, {nameof(Description)}: {Description}, {nameof(XYPixelSize)}: {XYPixelSize}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(ExposureSeconds)}: {ExposureSeconds}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(VolumeMicroL)}: {VolumeMicroL}, {nameof(PreviewResolutionX)}: {PreviewResolutionX}, {nameof(PreviewResolutionY)}: {PreviewResolutionY}, {nameof(LayersCount)}: {LayersCount}";
        }
    }

    #endregion

    #region LayerDef

    public class LayerDef
    {
        [FieldOrder(0)][FieldEndianness(Endianness.Big)] public uint WhitePixelsCount { get; set; }
        // White pixels region (border including corner pixels)
        [FieldOrder(1)][FieldEndianness(Endianness.Big)] public int XMin { get; set; } = 0;
        [FieldOrder(2)][FieldEndianness(Endianness.Big)] public int YMin { get; set; } = 0;
        [FieldOrder(3)][FieldEndianness(Endianness.Big)] public int XMax { get; set; } = RESOLUTION_X - 1;
        [FieldOrder(4)][FieldEndianness(Endianness.Big)] public int YMax { get; set; } = RESOLUTION_Y - 1;
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

        public LayerDef(Mat mat)
        {
            XMax = mat.Width - 1;
            YMax = mat.Height - 1;
        }

        public override string ToString()
        {
            return $"{nameof(WhitePixelsCount)}: {WhitePixelsCount}, {nameof(XMin)}: {XMin}, {nameof(YMin)}: {YMin}, {nameof(XMax)}: {XMax}, {nameof(YMax)}: {YMax}, {nameof(BitsCount)}: {BitsCount}, {nameof(RleBytesCount)}: {RleBytesCount}, {nameof(EncodedRle)}: {EncodedRle?.Length}";
        }

        public unsafe byte[] Encode(Mat mat)
        {
            uint computeRepeatsSize(uint repeats)
            {
                return (uint)Math.Ceiling(Math.Log2(repeats));
            }

            void setBits(List<byte> data, uint pos, uint value, uint count = 1)
            {
                if (data.Count * 8 < pos + count)
                {
                    data.AddRange(new byte[(pos + count + 7 - data.Count * 8) / 8]);
                }

                for (int off = (int)(pos + count - 1); off + 1 > pos; --off)
                {
                    byte tmp = data[(int)off / 8];
                    byte mask = (byte)(1 << ((int)off % 8));
                    data[(int)off / 8] = ((int)value & 1) == 1 ? (byte)(tmp | mask) : (byte)(tmp & ~mask);
                    value >>= 1;
                }
            }

            if (mat.Width != RESOLUTION_X || mat.Height != RESOLUTION_Y)
            {
                throw new ArgumentException($"Anet N4 support only {RESOLUTION_X}x{RESOLUTION_Y} image, got {mat.Width}x{mat.Height}");
            }

            Rectangle rec = CvInvoke.BoundingRectangle(mat);
            XMin = rec.Left;
            XMax = rec.Right - 1;
            YMin = rec.Top;
            YMax = rec.Bottom - 1;

            WhitePixelsCount = 0;

            uint singleColorLenght = 0;
            uint compressedPos = 33;

            List<byte> rawData = new();
            var spanMat = mat.GetBytePointer();

            bool isWhitePrev = spanMat[0] > 127;

            setBits(rawData, 0, (uint)mat.Width, 16);
            setBits(rawData, 16, (uint)mat.Height, 16);
            setBits(rawData, 32, isWhitePrev ? 1u : 0u);

            for (int pixel_ind = 0; pixel_ind < mat.GetLength(); ++pixel_ind)
            {
                bool isWhiteCurrent = spanMat[pixel_ind] > 127; // No AA

                if (isWhiteCurrent)
                {
                    WhitePixelsCount++;
                }

                if (isWhiteCurrent == isWhitePrev)
                {
                    singleColorLenght++;
                }

                if (isWhiteCurrent != isWhitePrev || pixel_ind == mat.GetLength() - 1)
                {
                    isWhitePrev = isWhiteCurrent;
                    var repeatsSize = computeRepeatsSize(singleColorLenght);
                    setBits(rawData, compressedPos, repeatsSize, 5);
                    setBits(rawData, compressedPos + 5, singleColorLenght, repeatsSize + 1);
                    compressedPos += 6 + repeatsSize;
                    singleColorLenght = 1;
                }
            }

            EncodedRle = rawData.ToArray();
            RleBytesCount = (uint)EncodedRle.Length;
            BitsCount = compressedPos;

            return EncodedRle;
        }

        public Mat Decode(bool consumeRle = true)
        {
            uint GetBits(uint pos, uint count = 1)
            {
                if (pos + count > BitsCount)
                {
                    throw new IndexOutOfRangeException($"Trying to read {count} bits from pos {pos}, but total size is {BitsCount} bits.");
                }

                uint res = 0;
                for (uint i = pos; i < pos + count; ++i)
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

            uint RleWidth = GetBits(0, 16);
            uint RleHeight = GetBits(16, 16);

            var mat = EmguExtensions.InitMat(new Size((int)RleWidth, (int)RleHeight));
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

            if (consumeRle)
                EncodedRle = null!;

            return mat;
        }
    }
    #endregion

    #endregion

    #region Properties

    public Header HeaderSettings { get; protected internal set; } = new();
    public override FileFormatType FileType => FileFormatType.Binary;

    public override string ConvertMenuGroup => "Anet";

    public override FileExtension[] FileExtensions { get; } = {
        new(typeof(AnetN4File), "N4", "Anet N4"),
    };

    public override SpeedUnit FormatSpeedUnit => SpeedUnit.MillimetersPerSecond;

    public override byte AntiAliasing => 1; // Format does not support antialiasing

    public override PrintParameterModifier[]? PrintParameterModifiers { get; } =
    {
        PrintParameterModifier.ExposureTime,
        PrintParameterModifier.BottomExposureTime,
        PrintParameterModifier.BottomLayerCount,
        PrintParameterModifier.LiftSpeed,
        PrintParameterModifier.LiftHeight,
    };

    public override Size[]? ThumbnailsOriginalSize { get; } = { new(260, 140) };

    public override uint ResolutionX
    {
        get => _resolutionX;
        set
        {
            if (!RaiseAndSetIfChanged(ref _resolutionX, value)) return;
            HeaderSettings.XYPixelSize = PixelSizeMax;
        }
    }

    public override uint ResolutionY
    {
        get => _resolutionY;
        set
        {
            if (!RaiseAndSetIfChanged(ref _resolutionY, value)) return;
            HeaderSettings.XYPixelSize = PixelSizeMax;
        }
    }

    public override float DisplayWidth
    {
        get => DISPLAY_WIDTH;
        set { }
    }

    public override float DisplayHeight
    {
        get => DISPLAY_HEIGHT;
        set { }
    }

    public override FlipDirection DisplayMirror
    {
        get => FlipDirection.Horizontally;
        set { }
    }

    public override float LayerHeight
    {
        get => (float)Layer.RoundHeight(HeaderSettings.LayerHeight);
        set
        {
            HeaderSettings.LayerHeight = Layer.RoundHeight(value);
            RaisePropertyChanged();
        }
    }

    public override float MachineZ
    {
        get => MACHINE_Z;
        set { }
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

    public override float BottomWaitTimeBeforeCure
    {
        get => 0;
        set
        {
            SetBottomLightOffDelay(0);
            base.BottomWaitTimeBeforeCure = 0;
        }
    }

    public override float WaitTimeBeforeCure
    {
        get => 0;
        set
        {
            SetNormalLightOffDelay(0);
            base.WaitTimeBeforeCure = 0;
        }
    }

    public override float BottomExposureTime
    {
        get => HeaderSettings.BottomExposureSeconds;
        set => base.BottomExposureTime = HeaderSettings.BottomExposureSeconds = (uint)Math.Round(value);
    }

    public override float ExposureTime
    {
        get => HeaderSettings.ExposureSeconds;
        set => base.ExposureTime = HeaderSettings.ExposureSeconds = (uint)Math.Round(value);
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
            HeaderSettings.EncodedPrintTime = (int)base.PrintTime;
        }
    }

    public override float MaterialMilliliters
    {
        get => base.MaterialMilliliters;
        set
        {
            base.MaterialMilliliters = value;
            HeaderSettings.VolumeMicroL = base.MaterialMilliliters * 1000.0;
        }
    }

    public override string MachineName => "Anet N4";

    public override object[] Configs => new object[] { HeaderSettings };

    #endregion

    #region Constructors
    public AnetN4File()
    {
    }
    #endregion

    #region Methods

    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Create, FileAccess.Write);

        byte[] previewBuffer = new byte[72866];
        _bmpHeader.CopyTo(previewBuffer, 0);
        EncodeImage(DATATYPE_BGR565, Thumbnails[0]!).CopyTo(previewBuffer, 66);
        HeaderSettings.PreviewContent = previewBuffer;

        outputFile.WriteSerialize(HeaderSettings);

        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        var layerData = new LayerDef[LayerCount];

        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                using (var mat = this[layerIndex].LayerMat)
                {
                    layerData[layerIndex] = new LayerDef(mat);
                    layerData[layerIndex].Encode(mat);
                }
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

        outputFile.WriteSerialize(new UInt32()); // Supports count, always 0

        Debug.WriteLine("Encode Results:");
        Debug.WriteLine(HeaderSettings);
        Debug.WriteLine("-End-");
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = new FileStream(FileFullPath!, FileMode.Open, FileAccess.Read);
        HeaderSettings = Helpers.Deserialize<Header>(inputFile);
        if (HeaderSettings.Version == null || !HeaderSettings.Version.Equals("3"))
        {
            throw new FileLoadException("Not a valid N4 file: Version doesn't match", FileFullPath);
        }

        if (HeaderSettings.PreviewSize != 72866 || HeaderSettings.PreviewContent == null)
        {
            throw new FileLoadException("Not a valid N4 file: incorrect preview format", FileFullPath);
        }

        Thumbnails[0] = DecodeImage(DATATYPE_BGR565, HeaderSettings.PreviewContent[66..], ThumbnailsOriginalSize![0]);

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
                    using var mat = layersDefinitions[layerIndex].Decode();
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
