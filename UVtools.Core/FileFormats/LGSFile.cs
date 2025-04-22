/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
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

public sealed class LGSFile : FileFormat
{
    #region Sub Classes
        
    #region Header

    public class Header
    {
        public const string NameValue = "Longer3D";
            
        /// <summary>
        /// Gets the model name
        /// </summary>
        [FieldOrder(0)] [FieldLength(8)] public string Name { get; set; } = NameValue; // 0x00:
        [FieldOrder(1)] public uint Uint_08 { get; set; } = 1; // 0x08: 0xff000001 ?
        [FieldOrder(2)] public uint Uint_0c { get; set; } = 1; // 0x0c: 1 ?
        [FieldOrder(3)] public uint PrinterModel { get; set; } = 30; // 10, 30, 120, 4000 (4k), 4500 (4k mono)
        [FieldOrder(4)] public uint Uint_14 { get; set; } = 0; // 0x14: 0 ?
        [FieldOrder(5)] public uint MagicKey { get; set; } = 34; // 0x18: 34
        [FieldOrder(6)] public float PixelPerMmX { get; set; } = 15.404f;
        [FieldOrder(7)] public float PixelPerMmY { get; set; } = 4.866f;
        [FieldOrder(8)] public float ResolutionX { get; set; }
        [FieldOrder(9)] public float ResolutionY { get; set; }
        [FieldOrder(10)] public float LayerHeight { get; set; }
        [FieldOrder(11)] public float ExposureTimeMs { get; set; }
        [FieldOrder(12)] public float BottomExposureTimeMs { get; set; }
        [FieldOrder(13)] public float Float_38 { get; set; } = 10; // 0x38: 10
        [FieldOrder(14)] public float WaiTimeBeforeCureMs { get; set; } = 1000;
        [FieldOrder(15)] public float BottomWaiTimeBeforeCureMs { get; set; } = 2000;
        [FieldOrder(16)] public float BottomHeight { get; set; }
        [FieldOrder(17)] public float Float_48 { get; set; } = 0.6f; // 0x48: 0.6
        [FieldOrder(18)] public float BottomLiftHeight { get; set; } = 4;
        [FieldOrder(19)] public float LiftHeight { get; set; }
        [FieldOrder(20)] public float LiftSpeed { get; set; } = 150;
        [FieldOrder(21)] public float LiftSpeed_ { get; set; } = 150;
        [FieldOrder(22)] public float BottomLiftSpeed { get; set; } = 90;
        [FieldOrder(23)] public float BottomLiftSpeed_ { get; set; } = 90;
        [FieldOrder(24)] public float Float_64 { get; set; } = 5; // 0x64: 5?
        [FieldOrder(25)] public float Float_68 { get; set; } = 60; // 0x68: 60?
        [FieldOrder(26)] public float Float_6c { get; set; } = 10; // 0x6c: 10?
        [FieldOrder(27)] public float Float_70 { get; set; } = 600; // 0x70: 600?
        [FieldOrder(28)] public float Float_74 { get; set; } = 600; // 0x70: 600?
        [FieldOrder(29)] public float Float_78 { get; set; } = 2; // 0x78: 2?
        [FieldOrder(30)] public float Float_7c { get; set; } = 0.2f; // 0x7c: 0.2?
        [FieldOrder(31)] public float Float_80 { get; set; } = 60; // 0x80: 60?
        [FieldOrder(32)] public float Float_84 { get; set; } = 1; // 0x84: 1?
        [FieldOrder(33)] public float Float_88 { get; set; } = 6; // 0x88: 6?
        [FieldOrder(34)] public float Float_8c { get; set; } = 150; // 0x8c: 150 ?
        [FieldOrder(35)] public float Float_90 { get; set; } = 1001; // 0x90: 1001 ?
        [FieldOrder(36)] public float MachineZ { get; set; } = 140;// 0x94: 140 for lgs10, 170 for lgs30, 150 for lgs120, 190 for lgs4k
        [FieldOrder(37)] public uint Uint_98 { get; set; } // 0x98: 0 ?
        [FieldOrder(38)] public uint Uint_9c { get; set; } // 0x9c: 0 ?
        [FieldOrder(39)] public uint Uint_a0 { get; set; } // 0xa0: 0 ?
        [FieldOrder(40)] public uint LayerCount { get; set; }
        [FieldOrder(41)] public uint Uint_a8 { get; set; } = 4; // 0xa8: 4 ?
        [FieldOrder(42)] public uint PreviewSizeX { get; set; } = 120;
        [FieldOrder(43)] public uint PreviewSizeY { get; set; } = 150;
    }

    #endregion

    #region LGS120PngPreview

    public class LGS120PngPreview
    {
        public const ushort ResolutionX = 1200;
        public const ushort ResolutionY = 1600;

        [FieldOrder(0)] public uint DataSize { get; set; }

        [FieldOrder(1)]
        [FieldLength(nameof(DataSize))]
        public byte[] PngBytes { get; set; } = null!;

        [FieldOrder(2)] public ushort Padding { get; set; }

        public void Encode(Mat? mat)
        {
            mat ??= EmguExtensions.InitMat(new Size(ResolutionX, ResolutionY), 3);

            if (mat.Width != ResolutionX || mat.Height != ResolutionY)
            {
                using var resizeMat = new Mat();
                CvInvoke.Resize(mat, resizeMat, new Size(ResolutionX, ResolutionY));
                PngBytes = resizeMat.GetPngByes();
            }
            else
            {
                PngBytes = mat.GetPngByes();
            }
        }

        public Mat Decode(bool consumeRle = true)
        {
            var mat = new Mat();
            CvInvoke.Imdecode(PngBytes, ImreadModes.Unchanged, mat);
            if (consumeRle)
                PngBytes = null!;
            return mat;
        }
    }

    #endregion

    #region LayerData

    public class LayerDef
    {
        [Ignore] public LGSFile Parent { get; set; } = null!;

        [FieldOrder(0)]
        public uint DataSize { get; set; }

        [FieldOrder(1)]
        [FieldLength(nameof(DataSize))]
        public byte[] EncodedRle { get; set; } = null!;

        public LayerDef() { }

        public LayerDef(LGSFile parent)
        {
            Parent = parent;
        }

        public unsafe byte[] Encode(Mat mat)
        {
            List<byte> rawData = [];
            List<byte> chunk = [];

            if (Parent.HeaderSettings.PrinterModel is 4000 or 4500)
            {
                CvInvoke.Rotate(mat, mat, RotateFlags.Rotate90Clockwise);
            }

            var spanMat = mat.GetBytePointer();
            var imageLength = mat.GetLength();

            uint span = 0;
            byte lc = 0;

            void addSpan(){
                chunk.Clear();
                for (; span > 0; span >>= 4) {
                    chunk.Insert(0, (byte)((byte)(span & 0xf) | (lc & 0xf0)));
                }
                rawData.AddRange(chunk.ToArray());
            }

            for (int i = 0; i < imageLength; i++)
            {
                byte c = (byte) (spanMat[i] & 0xf0);
                    
                if (c == lc)
                {
                    span++;
                }
                else
                {
                    addSpan();
                    span = 1;
                }

                lc = c;
            }

            addSpan();
            EncodedRle = rawData.ToArray();
            DataSize = (uint) EncodedRle.Length;

            if (Parent.HeaderSettings.PrinterModel is 4000 or 4500)
            {
                CvInvoke.Rotate(mat, mat, RotateFlags.Rotate90CounterClockwise);
            }

            return EncodedRle;
        }

        public Mat Decode(bool consumeRle = true)
        {
            // lgs10/30 -------->
            // lgs120/4k From Y bottom to top Y
            var mat = EmguExtensions.InitMat(Parent.HeaderSettings.PrinterModel is 4000 or 4500 ? Parent.Resolution.Exchange() : Parent.Resolution);
            //var matSpan = mat.GetBytePointer();
            var imageLength = mat.GetLength();
                
            int pixelPos = 0;

            for (var i = 0; i < EncodedRle.Length; i++)
            {
                var b = EncodedRle[i];
                byte colorNibble = (byte)(b >> 4);
                byte color = (byte)(colorNibble << 4 | colorNibble);
                int repeat = b & 0xf;

                while (i + 1 < EncodedRle.Length && (EncodedRle[i + 1] >> 4) == colorNibble)
                {
                    i++;
                    repeat = (repeat << 4) | (EncodedRle[i] & 0xf);
                }

                if (pixelPos >= imageLength)
                {
                    throw new FileLoadException($"Too much buffer, expected: {imageLength}, got: {pixelPos}");
                }

                mat.FillSpan(ref pixelPos, repeat, color);

                //if (repeat <= 0) continue;
                /*while (repeat-- > 0)
                {
                    matSpan[pixel++] = color;
                }*/

            }

            if (pixelPos != imageLength)
            {
                throw new FileLoadException($"Incomplete buffer, expected: {imageLength}, got: {pixelPos}");
            }

            if (consumeRle)
                EncodedRle = null!;

            if (Parent.HeaderSettings.PrinterModel is 4000 or 4500)
            {
                CvInvoke.Rotate(mat, mat, RotateFlags.Rotate90CounterClockwise);
            }

            return mat;
        }
    }
    #endregion

    #endregion

    #region Properties

    public Header HeaderSettings { get; private set; } = new();
    public override FileFormatType FileType => FileFormatType.Binary;

    public override string ConvertMenuGroup => "Longer3D";

    public override FileExtension[] FileExtensions { get; } =
    [
        new (typeof(LGSFile), "lgs", "Longer Orange 10 (LGS)"),
        new (typeof(LGSFile), "lgs30", "Longer Orange 30 (LGS30)"),
        new (typeof(LGSFile), "lgs120", "Longer Orange 120 (LGS120)"),
        new (typeof(LGSFile), "lgs4k", "Longer Orange 4k (LGS4K)")
    ];

    public override PrintParameterModifier[] PrintParameterModifiers { get; } =
    [
        PrintParameterModifier.BottomLayerCount,

        PrintParameterModifier.BottomWaitTimeBeforeCure,
        PrintParameterModifier.WaitTimeBeforeCure,

        PrintParameterModifier.BottomExposureTime,
        PrintParameterModifier.ExposureTime,

        PrintParameterModifier.BottomLiftHeight,
        PrintParameterModifier.BottomLiftSpeed,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed

    ];

    public override Size[] ThumbnailsOriginalSize { get; } = [new(120, 150)];

    public override uint ResolutionX
    {
        get => (uint) HeaderSettings.ResolutionX;
        set
        {
            HeaderSettings.ResolutionX = value;
            base.ResolutionX = value;
            HeaderSettings.PixelPerMmX = Xppmm;
        }
    }

    public override uint ResolutionY
    {
        get => (uint)HeaderSettings.ResolutionY;
        set
        {
            HeaderSettings.ResolutionY = value;
            base.ResolutionY = value;
            HeaderSettings.PixelPerMmY = Yppmm;
        }
    }

    public override float DisplayWidth
    {
        get => MathF.Round(ResolutionX / HeaderSettings.PixelPerMmX, 3);
        set
        {
            base.DisplayWidth = value;
            HeaderSettings.PixelPerMmX = Xppmm;
        }
    }

    public override float DisplayHeight
    {
        get => MathF.Round(ResolutionY / HeaderSettings.PixelPerMmY, 3);
        set
        {
            base.DisplayHeight = value;
            HeaderSettings.PixelPerMmY = Yppmm;
        }
    }

    public override float MachineZ
    {
        get => HeaderSettings.MachineZ > 0 ? HeaderSettings.MachineZ : base.MachineZ;
        set => base.MachineZ = HeaderSettings.MachineZ = MathF.Round(value, 2);
    }

    public override FlipDirection DisplayMirror
    {
        get => FlipDirection.Horizontally;
        set { }
    }      

    public override float LayerHeight
    {
        get => HeaderSettings.LayerHeight;
        set => HeaderSettings.LayerHeight = Layer.RoundHeight(value);
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = HeaderSettings.LayerCount = base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => (ushort) (HeaderSettings.BottomHeight / LayerHeight);
        set
        {
            if(LayerHeight > 0) HeaderSettings.BottomHeight = value * LayerHeight;
            base.BottomLayerCount = value;
        }
    }

    public override float BottomWaitTimeBeforeCure
    {
        get => TimeConverter.MillisecondsToSeconds(HeaderSettings.BottomWaiTimeBeforeCureMs);
        set
        {
            HeaderSettings.BottomWaiTimeBeforeCureMs = TimeConverter.SecondsToMilliseconds(value);
            base.BottomWaitTimeBeforeCure = value;
        }
    }

    public override float WaitTimeBeforeCure
    {
        get => TimeConverter.MillisecondsToSeconds(HeaderSettings.WaiTimeBeforeCureMs);
        set
        {
            HeaderSettings.WaiTimeBeforeCureMs = TimeConverter.SecondsToMilliseconds(value);
            base.WaitTimeBeforeCure = value;
        }
    }

    public override float BottomExposureTime
    {
        get => TimeConverter.MillisecondsToSeconds(HeaderSettings.BottomExposureTimeMs);
        set
        {
            HeaderSettings.BottomExposureTimeMs = TimeConverter.SecondsToMilliseconds(value);
            base.BottomExposureTime = value;
        }
    }

    public override float ExposureTime
    {
        get => TimeConverter.MillisecondsToSeconds(HeaderSettings.ExposureTimeMs);
        set
        {
            HeaderSettings.ExposureTimeMs = TimeConverter.SecondsToMilliseconds(value);
            base.ExposureTime = value;
        }
    }

    public override float BottomLiftHeight
    {
        get => HeaderSettings.BottomLiftHeight;
        set => base.BottomLiftHeight = HeaderSettings.BottomLiftHeight = value;
    }

    public override float LiftHeight
    {
        get => HeaderSettings.LiftHeight;
        set => base.LiftHeight = HeaderSettings.LiftHeight = value;
    }

    public override float BottomLiftSpeed
    {
        get => HeaderSettings.BottomLiftSpeed;
        set => base.BottomLiftSpeed = HeaderSettings.BottomLiftSpeed = HeaderSettings.BottomLiftSpeed_ = value;
    }

    public override float LiftSpeed
    {
        get => HeaderSettings.LiftSpeed;
        set => base.LiftSpeed = HeaderSettings.LiftSpeed = HeaderSettings.LiftSpeed_ = value;
    }

    public override float BottomRetractSpeed => RetractSpeed;

    public override float RetractSpeed => LiftSpeed;

    public override object[] Configs => [HeaderSettings];

    #endregion

    #region Constructors
    public LGSFile()
    { }
    #endregion

    #region Methods

    protected override void EncodeInternally(OperationProgress progress)
    {
        if (FileEndsWith(".lgs")) // Longer Orange 10
        {
            MachineZ = 140;
            HeaderSettings.PrinterModel = 10;
        }
        else if (FileEndsWith(".lgs30")) // Longer Orange 30
        {
            MachineZ = 170;
            HeaderSettings.PrinterModel = 30;
        }
        else if (FileEndsWith(".lgs120")) // Longer Orange 120
        {
            MachineZ = 150;
            HeaderSettings.PrinterModel = 120;
        }
        else if (FileEndsWith(".lgs4k")) // Longer Orange 4K & Mono
        {
            MachineZ = 190;
            if(HeaderSettings.PrinterModel is not 4000 and not 4500) HeaderSettings.PrinterModel = 4500;
        }

        //uint currentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Create, FileAccess.Write);
        outputFile.WriteSerialize(HeaderSettings);
        outputFile.WriteBytes(EncodeImage(DATATYPE_RGB565_BE, Thumbnails[0]));

        if (HeaderSettings.PrinterModel == 120)
        {
            // Insert PNG here
            var mat = GetLargestThumbnail();
            var pngPreview = new LGS120PngPreview();
            pngPreview.Encode(mat!);
            outputFile.WriteSerialize(pngPreview);
        }

        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        var layerData = new LayerDef[LayerCount];

        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                using (var mat = this[layerIndex].LayerMat)
                {
                    layerData[layerIndex] = new LayerDef(this);
                    layerData[layerIndex].Encode(mat);
                }
                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                progress.PauseOrCancelIfRequested();
                outputFile.WriteSerialize(layerData[layerIndex]);
                layerData[layerIndex].EncodedRle = null!; // Free this
            }
        }


        progress.ItemName = "Saving layers";
        progress.ProcessedItems = 0;

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            progress.PauseOrCancelIfRequested();
            outputFile.WriteSerialize(layerData[layerIndex]);
            progress++;
        }

        Debug.WriteLine("Encode Results:");
        Debug.WriteLine(HeaderSettings);
        Debug.WriteLine("-End-");
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = new FileStream(FileFullPath!, FileMode.Open, FileAccess.Read);
        HeaderSettings = Helpers.Deserialize<Header>(inputFile);
        if (HeaderSettings.Name != Header.NameValue)
        {
            throw new FileLoadException("Not a valid LGS file!", FileFullPath);
        }
        
        //if (HeaderSettings.PrinterModel is 10 or 30 or 120)
        //{
        // Fix inconsistencies found of different version of plugin and slicers
        if (ResolutionX > ResolutionY)
        {
            (ResolutionX, ResolutionY) = (ResolutionY, ResolutionX);
        }
        //}

        var previewSize = HeaderSettings.PreviewSizeX * HeaderSettings.PreviewSizeY * 2;
        var previewData = inputFile.ReadBytes(previewSize);
        Thumbnails.Add(DecodeImage(DATATYPE_RGB565_BE, previewData, HeaderSettings.PreviewSizeX, HeaderSettings.PreviewSizeY));

        if (HeaderSettings.PrinterModel == 120)
        {
            var pngPreview = Helpers.Deserialize<LGS120PngPreview>(inputFile);

            var mat = new Mat();
            CvInvoke.Imdecode(pngPreview.PngBytes, ImreadModes.Unchanged, mat);
            Thumbnails.Add(mat);
        }


        Init(HeaderSettings.LayerCount, DecodeType == FileDecodeType.Partial);
        var layerData = new LayerDef[LayerCount];

        if (DecodeType == FileDecodeType.Full)
        {
            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
            foreach (var batch in BatchLayersIndexes())
            {
                foreach (var layerIndex in batch)
                {
                    progress.PauseOrCancelIfRequested();

                    layerData[layerIndex] = Helpers.Deserialize<LayerDef>(inputFile);
                    layerData[layerIndex].Parent = this;
                }

                Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
                {
                    progress.PauseIfRequested();

                    using (var mat = layerData[layerIndex].Decode())
                    {
                        _layers[layerIndex] = new Layer((uint)layerIndex, mat, this);
                    }

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