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
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public sealed class GR1File : FileFormat
{
    #region Constants

    private const uint SlicerInfoAddress = 4 + 7 + 290 * 290 * 2 + 116 * 116 * 2 + 4;
    #endregion

    #region Sub Classes

    #region Header

    public sealed class Header
    {
        public const byte HEADER_SIZE = 7;
        public const string HEADER_VALUE = "MKSDLP";

        [FieldOrder(0)]
        [FieldEndianness(Endianness.Big)]
        public uint HeaderSize { get; set; } = HEADER_SIZE;

        /// <summary>
        /// Gets the file tag = MKSDLP
        /// </summary>
        [FieldOrder(1)]
        [FieldLength(HEADER_SIZE)]
        [SerializeAs(SerializedType.TerminatedString)]
        public string HeaderValue { get; set; } = HEADER_VALUE;
    }
    #endregion

    #region SlicerInfo

    public sealed class SlicerInfo
    {
        // 290 * 290 * 2 + 116 * 116 * 2 + 4
        //[FieldOrder(0)] [FieldLength(195116)] public byte[] PreviewData { get; set; }

        [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public ushort LayerCount { get; set; }
        [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public ushort ResolutionX { get; set; }
        [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public ushort ResolutionY { get; set; }
        [FieldOrder(3)][FieldEndianness(Endianness.Big)] public uint DisplayWidthLength { get; set; }
        [FieldOrder(4)][FieldEncoding("UTF-16BE")][FieldLength(nameof(DisplayWidthLength))] public string DisplayWidth { get; set; } = string.Empty;
        [FieldOrder(5)][FieldEndianness(Endianness.Big)] public uint DisplayHeightLength { get; set; }
        [FieldOrder(6)][FieldEncoding("UTF-16BE")][FieldLength(nameof(DisplayHeightLength))] public string DisplayHeight { get; set; } = string.Empty;
        [FieldOrder(7)][FieldEndianness(Endianness.Big)] public uint LayerHeightLength { get; set; } = 8;
        [FieldOrder(8)][FieldEncoding("UTF-16BE")][FieldLength(nameof(LayerHeightLength))] public string LayerHeight { get; set; } = DefaultLayerHeight.ToString(CultureInfo.InvariantCulture);
        [FieldOrder(9)] [FieldEndianness(Endianness.Big)] public ushort ExposureTime { get; set; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort LightOffDelay { get; set; }
        [FieldOrder(11)] [FieldEndianness(Endianness.Big)] public ushort BottomExposureTime { get; set; }
        [FieldOrder(12)] [FieldEndianness(Endianness.Big)] public ushort BottomLayers { get; set; }
        [FieldOrder(13)] [FieldEndianness(Endianness.Big)] public ushort BottomLiftHeight { get; set; }
        [FieldOrder(14)] [FieldEndianness(Endianness.Big)] public ushort BottomLiftSpeed { get; set; }
        [FieldOrder(15)] [FieldEndianness(Endianness.Big)] public ushort LiftHeight { get; set; }
        [FieldOrder(16)] [FieldEndianness(Endianness.Big)] public ushort LiftSpeed { get; set; }
        [FieldOrder(17)] [FieldEndianness(Endianness.Big)] public ushort RetractSpeed { get; set; }
        [FieldOrder(18)] [FieldEndianness(Endianness.Big)] public ushort BottomLightPWM { get; set; }
        [FieldOrder(19)] [FieldEndianness(Endianness.Big)] public ushort LightPWM { get; set; }

        public override string ToString()
        {
            return $"{nameof(LayerCount)}: {LayerCount}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(DisplayWidthLength)}: {DisplayWidthLength}, {nameof(DisplayWidth)}: {DisplayWidth}, {nameof(DisplayHeightLength)}: {DisplayHeightLength}, {nameof(DisplayHeight)}: {DisplayHeight}, {nameof(LayerHeightLength)}: {LayerHeightLength}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(BottomExposureTime)}: {BottomExposureTime}, {nameof(BottomLayers)}: {BottomLayers}, {nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(LightPWM)}: {LightPWM}";
        }
    }
    #endregion

    #region LayerDef

    public sealed class LayerDef
    {
        [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public uint LineCount { get; set; }
        [FieldOrder(1)] [FieldCount(nameof(LineCount))] public LayerLine[] Lines { get; set; } = Array.Empty<LayerLine>();
        [FieldOrder(2)] public PageBreak PageBreak { get; set; } = new();

        public LayerDef() { }

        public LayerDef(uint lineCount, LayerLine[] lines)
        {
            LineCount = lineCount;
            Lines = lines;
        }
    }

    public sealed class LayerLine
    {
        [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public ushort StartY { get; set; }
        [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public ushort EndY { get; set; }
        [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public ushort StartX { get; set; }
        //[FieldOrder(3)] [FieldEndianness(Endianness.Big)] public byte Gray { get; set; }


        public static byte[] GetBytes(ushort startY, ushort endY, ushort startX)
        {
            var bytes = new byte[6];
            BitExtensions.ToBytesBigEndian(startY, bytes);
            BitExtensions.ToBytesBigEndian(endY, bytes, 2);
            BitExtensions.ToBytesBigEndian(startX, bytes, 4);
            return bytes;
        }

        public LayerLine()
        { }

        public LayerLine(ushort startY, ushort endY, ushort startX)
        {
            StartY = startY;
            EndY = endY;
            StartX = startX;
        }
    }

    public sealed class PageBreak
    {
        public static byte[] Bytes => new byte[] {0x0D, 0x0A};
        [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public byte Line { get; set; } = 0x0D;
        [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public byte Break { get; set; } = 0x0A;
    }

    #endregion

    #endregion

    #region Properties

    public Header HeaderSettings { get; private set; } = new();
    public SlicerInfo SlicerInfoSettings { get; private set; } = new();
    public override FileFormatType FileType => FileFormatType.Binary;

    public override FileExtension[] FileExtensions { get; } = {
        new (typeof(GR1File), "gr1", "GR1 Workshop")
    };

    public override PrintParameterModifier[] PrintParameterModifiers { get; } =
    {
        PrintParameterModifier.BottomLayerCount,
        PrintParameterModifier.LightOffDelay,
        PrintParameterModifier.BottomExposureTime,
        PrintParameterModifier.ExposureTime,

        PrintParameterModifier.BottomLiftHeight,
        PrintParameterModifier.BottomLiftSpeed,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed,
        PrintParameterModifier.RetractSpeed,

        PrintParameterModifier.BottomLightPWM,
        PrintParameterModifier.LightPWM,
    };

    public override Size[] ThumbnailsOriginalSize { get; } =
    {
        new (116, 116),
        new (290, 290)
    };

    public override bool SupportAntiAliasing => false;

    public override uint ResolutionX
    {
        get => SlicerInfoSettings.ResolutionX;
        set => base.ResolutionX = SlicerInfoSettings.ResolutionX = (ushort) value;
    }

    public override uint ResolutionY
    {
        get => SlicerInfoSettings.ResolutionY;
        set => base.ResolutionY = SlicerInfoSettings.ResolutionY = (ushort)value;
    }

    public override float DisplayWidth
    {
        get => (float)Math.Round(float.Parse(SlicerInfoSettings.DisplayWidth, CultureInfo.InvariantCulture), 2);
        set
        {
            value = (float)Math.Round(value, 2);
            SlicerInfoSettings.DisplayWidth = value.ToString(CultureInfo.InvariantCulture);
            base.DisplayWidth = value;
        }
    }

    public override float DisplayHeight
    {
        get => (float)Math.Round(float.Parse(SlicerInfoSettings.DisplayHeight, CultureInfo.InvariantCulture), 3);
        set
        {
            value = (float)Math.Round(value, 2);
            SlicerInfoSettings.DisplayHeight = value.ToString(CultureInfo.InvariantCulture);
            base.DisplayHeight = value;
        }
    }

    public override float LayerHeight
    {
        get => Layer.RoundHeight(float.Parse(SlicerInfoSettings.LayerHeight, CultureInfo.InvariantCulture));
        set
        {
            SlicerInfoSettings.LayerHeight = Layer.RoundHeight(value).ToString(CultureInfo.InvariantCulture);
            base.LayerHeight = value;
        }
    }

    public override FlipDirection DisplayMirror { get; set; }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = SlicerInfoSettings.LayerCount = (ushort)base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => SlicerInfoSettings.BottomLayers;
        set => base.BottomLayerCount = SlicerInfoSettings.BottomLayers = value;
    }

    public override float BottomLightOffDelay => SlicerInfoSettings.LightOffDelay;

    public override float LightOffDelay
    {
        get => SlicerInfoSettings.LightOffDelay;
        set => base.LightOffDelay = SlicerInfoSettings.LightOffDelay = (ushort)value;
    }

    public override float BottomWaitTimeBeforeCure
    {
        get => base.BottomWaitTimeBeforeCure;
        set
        {
            SetBottomLightOffDelay(value);
            base.BottomWaitTimeBeforeCure = value;
        }
    }

    public override float WaitTimeBeforeCure
    {
        get => base.WaitTimeBeforeCure;
        set
        {
            SetNormalLightOffDelay(value);
            base.WaitTimeBeforeCure = value;
        }
    }

    public override float BottomExposureTime
    {
        get => SlicerInfoSettings.BottomExposureTime;
        set => base.BottomExposureTime = SlicerInfoSettings.BottomExposureTime = (ushort)value;
    }

    public override float ExposureTime
    {
        get => SlicerInfoSettings.ExposureTime;
        set => base.ExposureTime = SlicerInfoSettings.ExposureTime = (ushort)value;
    }

    public override float BottomLiftHeight
    {
        get => SlicerInfoSettings.BottomLiftHeight;
        set => base.BottomLiftHeight = SlicerInfoSettings.BottomLiftHeight = (ushort)value;
    }

    public override float LiftHeight
    {
        get => SlicerInfoSettings.LiftHeight;
        set => base.LiftHeight = SlicerInfoSettings.BottomLiftHeight = (ushort)value;
    }

    public override float BottomLiftSpeed
    {
        get => SlicerInfoSettings.BottomLiftSpeed;
        set => base.BottomLiftSpeed = SlicerInfoSettings.BottomLiftSpeed = (ushort)value;
    }

    public override float LiftSpeed
    {
        get => SlicerInfoSettings.LiftSpeed;
        set => base.LiftSpeed = SlicerInfoSettings.LiftSpeed = (ushort)value;
    }

    public override float BottomRetractSpeed => RetractSpeed;

    public override float RetractSpeed
    {
        get => SlicerInfoSettings.RetractSpeed;
        set => base.RetractSpeed = SlicerInfoSettings.RetractSpeed = (ushort)value;
    }

    public override byte BottomLightPWM
    {
        get => (byte)SlicerInfoSettings.BottomLightPWM;
        set => base.BottomLightPWM = (byte)(SlicerInfoSettings.BottomLightPWM = value);
    }

    public override byte LightPWM
    {
        get => (byte)SlicerInfoSettings.LightPWM;
        set => base.LightPWM = (byte)(SlicerInfoSettings.LightPWM = value);
    }

    public override object[] Configs => new[] { (object)HeaderSettings, SlicerInfoSettings };

    #endregion

    #region Constructors

    #endregion

    #region Methods
    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Create, FileAccess.Write);
        var pageBreak = PageBreak.Bytes;

        outputFile.WriteSerialize(HeaderSettings);

        var previews = new byte[ThumbnailCountFileShouldHave][];

        // Previews
        Parallel.For(0, previews.Length, CoreSettings.GetParallelOptions(progress), previewIndex =>
        {
            progress.PauseIfRequested();
            var encodeLength = ThumbnailsOriginalSize[previewIndex].Area() * 2;

            previews[previewIndex] = EncodeImage(DATATYPE_RGB565_BE, Thumbnails[previewIndex]);

            if (encodeLength != previews[previewIndex].Length)
            {
                throw new FileLoadException($"Preview encode incomplete encode, expected: {previews[previewIndex].Length}, encoded: {encodeLength}");
            }
        });

        for (int i = 0; i < previews.Length; i++)
        {
            outputFile.WriteSerialize(previews[i]);
            outputFile.WriteBytes(pageBreak);
            previews[i] = null!;
        }
        outputFile.WriteSerialize(SlicerInfoSettings);
            
        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
            
        var layerBytes = new List<byte>[LayerCount];
        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                var layer = this[layerIndex];
                using (var mat = layer.LayerMat)
                {
                    var span = mat.GetDataByteReadOnlySpan();

                    layerBytes[layerIndex] = new();

                    uint lineCount = 0;

                    for (int x = layer.BoundingRectangle.X; x < layer.BoundingRectangle.Right; x++)
                    {
                        int y = layer.BoundingRectangle.Y;
                        int startY = -1;
                        for (; y < layer.BoundingRectangle.Bottom; y++)
                        {
                            int pos = mat.GetPixelPos(x, y);
                            if (span[pos] < 128) // Black pixel
                            {
                                if (startY == -1) continue; // Keep ignoring
                                layerBytes[layerIndex].AddRange(LayerLine.GetBytes((ushort)startY, (ushort)(y - 1), (ushort)x));
                                startY = -1;
                                lineCount++;
                            }
                            else
                            {
                                if (startY >= 0) continue; // Keep sum
                                startY = y;
                            }
                        }

                        if (startY >= 0)
                        {
                            layerBytes[layerIndex]
                                .AddRange(LayerLine.GetBytes((ushort)startY, (ushort)(y - 1), (ushort)x));
                            lineCount++;
                        }
                    }

                    layerBytes[layerIndex].InsertRange(0, BitExtensions.ToBytesBigEndian(lineCount));
                    layerBytes[layerIndex].AddRange(pageBreak);
                }

                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                outputFile.WriteBytes(layerBytes[layerIndex].ToArray());
                layerBytes[layerIndex] = null!;
            }
        }


        outputFile.WriteSerialize(HeaderSettings);

        Debug.WriteLine("Encode Results:");
        Debug.WriteLine(HeaderSettings);
        Debug.WriteLine("-End-");
    }



    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = new FileStream(FileFullPath!, FileMode.Open, FileAccess.Read);
        HeaderSettings = Helpers.Deserialize<Header>(inputFile);
        if (HeaderSettings.HeaderValue != Header.HEADER_VALUE)
        {
            throw new FileLoadException("Not a valid Makerbase file!", FileFullPath);
        }

        for (int i = 0; i < ThumbnailCountFileShouldHave; i++)
        {
            progress.PauseOrCancelIfRequested();
            var bytes = inputFile.ReadBytes(ThumbnailsOriginalSize[i].Area() * 2);
            inputFile.Seek(2, SeekOrigin.Current);
            Thumbnails.Add(DecodeImage(DATATYPE_RGB565_BE, bytes, ThumbnailsOriginalSize[i]));
        }


        SlicerInfoSettings = Helpers.Deserialize<SlicerInfo>(inputFile);

        Init(SlicerInfoSettings.LayerCount, DecodeType == FileDecodeType.Partial);


        if (DecodeType == FileDecodeType.Full)
        {
            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
            var linesBytes = new byte[LayerCount][];
            foreach (var batch in BatchLayersIndexes())
            {
                foreach (var layerIndex in batch)
                {
                    progress.PauseOrCancelIfRequested();
                    var lineCount = BitExtensions.ToUIntBigEndian(inputFile.ReadBytes(4));

                    linesBytes[layerIndex] = new byte[lineCount * 6];
                    inputFile.ReadBytes(linesBytes[layerIndex]);
                    inputFile.Seek(2, SeekOrigin.Current);
                }

                Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
                {
                    progress.PauseIfRequested();
                    
                    using (var mat = EmguExtensions.InitMat(Resolution))
                    {

                        for (int i = 0; i < linesBytes[layerIndex].Length; i++)
                        {
                            var startY = BitExtensions.ToUShortBigEndian(linesBytes[layerIndex][i++], linesBytes[layerIndex][i++]);
                            var endY = BitExtensions.ToUShortBigEndian(linesBytes[layerIndex][i++], linesBytes[layerIndex][i++]);
                            var startX = BitExtensions.ToUShortBigEndian(linesBytes[layerIndex][i++], linesBytes[layerIndex][i]);

                            CvInvoke.Line(mat, new Point(startX, startY), new Point(startX, endY), EmguExtensions.WhiteColor);
                        }

                        linesBytes[layerIndex] = null!;

                        _layers[layerIndex] = new Layer((uint)layerIndex, mat, this);
                    }

                    progress.LockAndIncrement();
                });
            }
        }
        else // Partial read
        {
            inputFile.Seek(-Helpers.Serializer.SizeOf(HeaderSettings), SeekOrigin.End);
        }

        HeaderSettings = Helpers.Deserialize<Header>(inputFile);
        if (HeaderSettings.HeaderValue != Header.HEADER_VALUE)
        {
            throw new FileLoadException("Not a valid Makerbase file!", FileFullPath);
        }
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Open, FileAccess.Write);
        outputFile.Seek(SlicerInfoAddress, SeekOrigin.Begin);
        outputFile.WriteSerialize(SlicerInfoSettings);
    }

    #endregion
}