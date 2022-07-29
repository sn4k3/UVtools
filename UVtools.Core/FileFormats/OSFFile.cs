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
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public class OSFFile : FileFormat
{
    #region Sub Classes
        
    #region Header

    public class Header
    {
        [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public uint HeaderLength { get; set; } = 350001;
        [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public ushort Version { get; set; } = 4;
        [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public byte ImageLog { get; set; } = 2;

        /// <summary>
        /// 148 * 80 * 2
        /// </summary>
        //[FieldOrder(3)] public Uint24BitBigEndian Preview1Length { get; set; } = new(23680);
        [FieldOrder(3)] [FieldEndianness(Endianness.Big)] [FieldBitLength(24)] public uint Preview1Length { get; set; } = 23680;

        /// <summary>
        /// RGB565
        /// </summary>
        //[FieldOrder(4)] [FieldLength(nameof(Preview1Length.Value))] public byte[] Preview1Data { get; set; }

        /*/// <summary>
        /// 300 * 140 * 2
        /// </summary>
        [FieldOrder(5)][FieldBitLength(24)] public uint Preview2Length { get; set; } = 84000;*/

        /*/// <summary>
        /// RGB565
        /// </summary>
        [FieldOrder(6)] [FieldLength(nameof(Preview2Length))] public byte[] Preview2Data { get; set; }*/

        
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort ResolutionX { get; set; }
        [FieldOrder(11)] [FieldEndianness(Endianness.Big)] public ushort ResolutionY { get; set; }
        [FieldOrder(12)] [FieldEndianness(Endianness.Big)] public ushort PixelUmMagnified100Times { get; set; }
        
        /// <summary>
        /// (0x00 not mirrored, 0x01 X-axis mirroring, 0x02 Y-axis mirroring, 0x03 XY-axis mirroring)
        /// </summary>
        [FieldOrder(13)] [FieldEndianness(Endianness.Big)] public byte Mirror { get; set; }
        [FieldOrder(14)] [FieldEndianness(Endianness.Big)] public byte BottomLightPWM { get; set; } = DefaultBottomLightPWM;
        [FieldOrder(15)] [FieldEndianness(Endianness.Big)] public byte LightPWM { get; set; } = DefaultLightPWM;
        [FieldOrder(16)] [FieldEndianness(Endianness.Big)] public byte AntiAliasEnabled { get; set; }
        [FieldOrder(17)] [FieldEndianness(Endianness.Big)] public byte DistortionEnabled { get; set; }
        [FieldOrder(18)] [FieldEndianness(Endianness.Big)] public byte DelayedExposureActivationEnabled { get; set; }
        [FieldOrder(19)] [FieldEndianness(Endianness.Big)] public uint LayerCount { get; set; }
        [FieldOrder(20)] [FieldEndianness(Endianness.Big)] public ushort NumberParameterSets { get; set; } = 1;
        [FieldOrder(21)] [FieldEndianness(Endianness.Big)] public uint LastLayerIndex { set; get; }
        [FieldOrder(22)] [FieldEndianness(Endianness.Big)] public uint LayerHeightUmMagnified100Times { set; get; }
        [FieldOrder(23)] [FieldEndianness(Endianness.Big)] public byte BottomLayerCount { set; get; }
        [FieldOrder(24)] [FieldEndianness(Endianness.Big)] public uint ExposureTimeMagnified100Times { set; get; }
        [FieldOrder(25)] [FieldEndianness(Endianness.Big)] public uint BottomExposureTimeMagnified100Times { set; get; }
        [FieldOrder(26)] [FieldEndianness(Endianness.Big)] public uint SupportDelayTimeMagnified100Times { set; get; }
        [FieldOrder(27)] [FieldEndianness(Endianness.Big)] public uint BottomSupportDelayTimeMagnified100Times { set; get; }
        [FieldOrder(28)] [FieldEndianness(Endianness.Big)] public byte TransitionLayers { set; get; }
        /// <summary>
        /// （0x00 linear transition）
        /// </summary>
        [FieldOrder(29)] [FieldEndianness(Endianness.Big)] public byte TransitionType { set; get; }
        /*
         [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint TransitionLayerIntervalTimeDifferenceMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint WaitTimeAfterCureMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint WaitTimeAfterLiftMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint WaitTimeBeforeCureMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint BottomLiftHeightSlowMagnified1000Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint BottomLiftHeightTotalMagnified1000Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint LiftHeightSlowMagnified1000Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint LiftHeightTotalMagnified1000Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint BottomRetractHeightTotalMagnified1000Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint RetractHeightSlowMagnified1000Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public uint RetractHeightTotalMagnified1000Times { set; get; }

        /// <summary>
        /// (0x00: S-shaped acceleration, 0x01: T-shaped acceleration, Default Value: S-shaped acceleration, currently only supports S-shaped acceleration)
        /// </summary>
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public byte AccelerationType { set; get; }

        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort BottomLiftSpeedStartMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort BottomLiftSpeedSlowMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort BottomLiftSpeedFastMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public byte BottomLiftAccelerationChange { set; get; } // 5

        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort LiftSpeedStartMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort LiftSpeedSlowMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort LiftSpeedFastMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public byte LiftAccelerationChange { set; get; } // 5

        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort BottomRetractSpeedStartMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort BottomRetractSpeedSlowMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort BottomRetractFastMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public byte BottomRetractAccelerationChange { set; get; } // 5

        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort RetractSpeedStartMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort RetractSpeedSlowMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort RetractFastMagnified100Times { set; get; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public byte RetractAccelerationChange { set; get; } // 5

        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] [FieldCount(23)] public byte[] Reserved { set; get; } = new byte[23]; // 23
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public byte ProtocolType { set; get; } // 0
        */

    }

    #endregion

    #region LayerDef

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
            List<byte> rawData = new();
            List<byte> chunk = new();

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

    public Header HeaderSettings { get; protected internal set; } = new();
    public override FileFormatType FileType => FileFormatType.Binary;

    public override FileExtension[] FileExtensions { get; } = {
        new (typeof(OSFFile), "osf", "Vlare Open File Format"),
    };

    public override PrintParameterModifier[]? PrintParameterModifiers { get; } =
    {
        PrintParameterModifier.BottomLayerCount,

        PrintParameterModifier.BottomLightOffDelay,
        PrintParameterModifier.LightOffDelay,

        PrintParameterModifier.BottomExposureTime,
        PrintParameterModifier.ExposureTime,

        PrintParameterModifier.BottomLiftHeight,
        PrintParameterModifier.BottomLiftSpeed,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed,
            
    };

    public override Size[]? ThumbnailsOriginalSize { get; } =
    {
        new(148, 80),
        new(300, 140),
        new(208, 116),
        new(404, 240),
    };

    public override uint ResolutionX
    {
        get => HeaderSettings.ResolutionX;
        set
        {
            HeaderSettings.ResolutionX = (ushort) value;
            RaisePropertyChanged();
        }
    }

    public override uint ResolutionY
    {
        get => (uint)HeaderSettings.ResolutionY;
        set
        {
            HeaderSettings.ResolutionY = (ushort)value;
            RaisePropertyChanged();
        }
    }



    public override FlipDirection DisplayMirror
    {
        get => HeaderSettings.Mirror switch
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
        }
    }      

    public override float LayerHeight
    {
        get => Layer.RoundHeight(HeaderSettings.LayerHeightUmMagnified100Times / 100000f);
        set
        {
            HeaderSettings.LayerHeightUmMagnified100Times = (ushort)(value * 100000);
            RaisePropertyChanged();
        }
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set
        {
            base.LayerCount = HeaderSettings.LayerCount = base.LayerCount;
            HeaderSettings.LastLayerIndex = HeaderSettings.LayerCount - 1;
        }
    }

    public override ushort BottomLayerCount
    {
        get => HeaderSettings.BottomLayerCount;
        set
        {
            HeaderSettings.BottomLayerCount = (byte)value;
            base.BottomLayerCount = value;
        }
    }

    /*public override float BottomLightOffDelay
    {
        get => TimeConverter.MillisecondsToSeconds(HeaderSettings.BottomLightOffDelayMs);
        set
        {
            HeaderSettings.BottomLightOffDelayMs = TimeConverter.SecondsToMilliseconds(value);
            base.BottomLightOffDelay = value;
        }
    }

    public override float LightOffDelay
    {
        get => TimeConverter.MillisecondsToSeconds(HeaderSettings.LightOffDelayMs);
        set
        {
            HeaderSettings.LightOffDelayMs = TimeConverter.SecondsToMilliseconds(value);
            base.LightOffDelay = value;
        }
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
    */
    public override float BottomRetractSpeed => RetractSpeed;

    public override float RetractSpeed => LiftSpeed;


    public override object[] Configs => new object[] { HeaderSettings };

    #endregion

    #region Constructors
    public OSFFile()
    {
    }
    #endregion

    #region Methods

    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Create, FileAccess.Write);
        outputFile.WriteSerialize(HeaderSettings);
        outputFile.WriteBytes(EncodeImage(DATATYPE_RGB565_BE, Thumbnails[0]!));


        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        var layerData = new LayerDef[LayerCount];

        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                /*using (var mat = this[layerIndex].LayerMat)
                {
                    layerData[layerIndex] = new LayerDef(this);
                    layerData[layerIndex].Encode(mat);
                }*/
                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                progress.ThrowIfCancellationRequested();
                outputFile.WriteSerialize(layerData[layerIndex]);
                layerData[layerIndex].EncodedRle = null!; // Free this
            }
        }


        progress.ItemName = "Saving layers";
        progress.ProcessedItems = 0;

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            progress.ThrowIfCancellationRequested();
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

        //var previewSize = HeaderSettings.PreviewSizeX * HeaderSettings.PreviewSizeY * 2;
        //var previewData = inputFile.ReadBytes(previewSize);
        //Thumbnails[0] = DecodeImage(DATATYPE_RGB565_BE, previewData, HeaderSettings.PreviewSizeX, HeaderSettings.PreviewSizeY);

       
        Init(HeaderSettings.LayerCount, DecodeType == FileDecodeType.Partial);
        var layerData = new LayerDef[LayerCount];

        if (DecodeType == FileDecodeType.Full)
        {
            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
            foreach (var batch in BatchLayersIndexes())
            {
                foreach (var layerIndex in batch)
                {
                    progress.ThrowIfCancellationRequested();

                    layerData[layerIndex] = Helpers.Deserialize<LayerDef>(inputFile);
                }

                Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
                {
                    using var mat = layerData[layerIndex].Decode();
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
        Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);
    }
        
    #endregion
}