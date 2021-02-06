/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

// https://github.com/cbiffle/catibo/blob/master/doc/cbddlp-ctb.adoc

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class LGSFile : FileFormat
    {
        #region Sub Classes
        
        #region Header

        public class Header
        {
            public const string NameValue = "Longer3D";
            //[FieldOrder(0)]  public uint Offset1     { get; set; }

            /// <summary>
            /// Gets the file tag = MKSDLP
            /// </summary>
            [FieldOrder(0)] [FieldLength(8)] public string Name { get; set; } = NameValue; // 0x00:
            [FieldOrder(1)] public uint Uint_08 { get; set; } = 4278190081; // 0x08: 0xff000001 ?
            [FieldOrder(2)] public uint Uint_0c { get; set; } = 1; // 0x0c: 1 ?
            [FieldOrder(3)] public uint Uint_10 { get; set; } = 30; // 0x10: 30 ?
            [FieldOrder(4)] public uint Uint_14 { get; set; } = 0; // 0x14: 0 ?
            [FieldOrder(5)] public uint Uint_18 { get; set; } = 34; // 0x18: 34 ?
            [FieldOrder(6)] public float PixelPerMmX { get; set; } = 15.404f;
            [FieldOrder(7)] public float PixelPerMmY { get; set; } = 4.866f;
            [FieldOrder(8)] public float ResolutionX { get; set; }
            [FieldOrder(9)] public float ResolutionY { get; set; }
            [FieldOrder(10)] public float LayerHeight { get; set; }
            [FieldOrder(11)] public float ExposureTimeMs { get; set; }
            [FieldOrder(12)] public float BottomExposureTimeMs { get; set; }
            [FieldOrder(13)] public float Float_38 { get; set; } = 10; // 0x38: 10
            [FieldOrder(14)] public float LightOffDelayMs { get; set; } = 2000;
            [FieldOrder(15)] public float BottomLightOffDelayMs { get; set; }
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
            [FieldOrder(36)] public float Float_94 { get; set; } = 140;// 0x94: 140 for Longer 10, 170 for Longer 30?
            [FieldOrder(37)] public uint Uint_98 { get; set; } // 0x98: 0 ?
            [FieldOrder(38)] public uint Uint_9c { get; set; } // 0x9c: 0 ?
            [FieldOrder(39)] public uint Uint_a0 { get; set; } // 0xa0: 0 ?
            [FieldOrder(40)] public uint LayerCount { get; set; }
            [FieldOrder(41)] public uint Uint_a8 { get; set; } = 4; // 0xa8: 4 ?
            [FieldOrder(42)] public uint PreviewSizeX { get; set; } = 120;
            [FieldOrder(43)] public uint PreviewSizeY { get; set; } = 150;
        }

        #endregion

        #region LayerData

        public class LayerData
        {
            [Ignore] public LGSFile Parent { get; set; }

            [FieldOrder(0)]
            public uint DataSize { get; set; }

            [FieldOrder(1)]
            [FieldLength(nameof(DataSize))]
            public byte[] EncodedRle { get; set; }

            public unsafe byte[] Encode(Mat mat)
            {
                List<byte> rawData = new List<byte>();
                List<byte> chunk = new List<byte>();
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
                return EncodedRle;
            }

            public unsafe Mat Decode(bool consumeRle = true)
            {
                var mat = EmguExtensions.InitMat(Parent.Resolution);
                var matSpan = mat.GetBytePointer();
                var imageLength = mat.GetLength();

                byte last = 0;
                int span = 0;
                int index = 0;

                foreach (var b in EncodedRle)
                {
                    byte color = (byte) ((b & 0xf0) | (b >> 4));

                    if (color == last)
                    {
                        span = (span << 4) | (b & 0xf);
                    }
                    else
                    {
                        for(; span > 0; span--)
                        {
                            if (index >= imageLength)
                            {
                                throw new FileLoadException($"'{span}' bytes to many");
                            }

                            matSpan[index++] = last;
                        }

                        span = b & 0xf;

                    }

                    last = color;
                }

                for (; span > 0; span--)
                {
                    if (index >= imageLength)
                    {
                        throw new FileLoadException($"'{span}' bytes to many");
                    }

                    matSpan[index++] = last;
                }

                if (index != imageLength)
                {
                    throw new FileLoadException($"Incomplete buffer, expected: {imageLength}, got: {index}");
                }


                if (consumeRle)
                    EncodedRle = null;

                return mat;
            }
        }
        #endregion

        #endregion

        #region Properties

        public Header HeaderSettings { get; protected internal set; } = new Header();
        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("lgs", "Longer Orange 10"),
            new FileExtension("lgs30", "Longer Orange 30"),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
            PrintParameterModifier.ExposureSeconds,

            PrintParameterModifier.BottomLightOffDelay,
            PrintParameterModifier.LightOffDelay,
            PrintParameterModifier.BottomLiftHeight,
            PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
        };

        public override byte ThumbnailsCount { get; } = 1;

        public override Size[] ThumbnailsOriginalSize { get; } = {new Size(120, 150)};

        public override uint ResolutionX
        {
            get => (uint) HeaderSettings.ResolutionX;
            set
            {
                HeaderSettings.ResolutionX = value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => (uint)HeaderSettings.ResolutionY;
            set
            {
                HeaderSettings.ResolutionY = value;
                RaisePropertyChanged();
            }
        }

        public override float Xppmm
        {
            get => HeaderSettings.PixelPerMmX > 0 ? HeaderSettings.PixelPerMmX : base.Xppmm;
            set
            {
                HeaderSettings.PixelPerMmX = value;
                base.Xppmm = value;
            }
        }

        public override float Yppmm
        {
            get => HeaderSettings.PixelPerMmY > 0 ? HeaderSettings.PixelPerMmY : base.Yppmm;
            set
            {
                HeaderSettings.PixelPerMmY = value;
                base.Yppmm = value;
            }
        }

        public override float DisplayWidth
        {
            get => ResolutionX / HeaderSettings.PixelPerMmX;
            set { }
        }

        public override float DisplayHeight
        {
            get => ResolutionY / HeaderSettings.PixelPerMmY;
            set { }
        }

        public override bool MirrorDisplay
        {
            get => true;
            set { }
        }      

        public override byte AntiAliasing
        {
            get => 4;
            set { }
        }

        public override float LayerHeight
        {
            get => HeaderSettings.LayerHeight;
            set
            {
                HeaderSettings.LayerHeight = value;
                RaisePropertyChanged();
            }
        }

        public override uint LayerCount
        {
            set
            {
                HeaderSettings.LayerCount = LayerCount;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(NormalLayerCount));
            }
        }

        public override ushort BottomLayerCount
        {
            get => (ushort)(HeaderSettings.BottomHeight / LayerHeight);
            set
            {
                HeaderSettings.BottomHeight = value * LayerHeight;
                RaisePropertyChanged();
            }
        }

        public override float BottomExposureTime
        {
            get => (float)Math.Round(HeaderSettings.BottomExposureTimeMs / 1000, 2);
            set
            {
                HeaderSettings.BottomExposureTimeMs = value * 1000;
                RaisePropertyChanged();
            }
        }

        public override float ExposureTime
        {
            get => (float)Math.Round(HeaderSettings.ExposureTimeMs / 1000, 2);
            set
            {
                HeaderSettings.ExposureTimeMs = value * 1000;
                RaisePropertyChanged();
            }
        }

        public override float BottomLightOffDelay
        {
            get => HeaderSettings.BottomLightOffDelayMs;
            set
            {
                HeaderSettings.BottomLightOffDelayMs = value;
                RaisePropertyChanged();
            }
        }

        public override float LightOffDelay
        {
            get => HeaderSettings.LightOffDelayMs;
            set
            {
                HeaderSettings.LightOffDelayMs = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomLiftHeight
        {
            get => HeaderSettings.BottomLiftHeight;
            set
            {
                HeaderSettings.BottomLiftHeight = value;
                RaisePropertyChanged();
            }
        }

        public override float LiftHeight
        {
            get => HeaderSettings.LiftHeight;
            set
            {
                HeaderSettings.LiftHeight = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomLiftSpeed
        {
            get => HeaderSettings.BottomLiftSpeed;
            set
            {
                HeaderSettings.BottomLiftSpeed = HeaderSettings.BottomLiftSpeed_ = value;
                RaisePropertyChanged();
            }
        }

        public override float LiftSpeed
        {
            get => HeaderSettings.LiftSpeed;
            set
            {
                HeaderSettings.LiftSpeed = HeaderSettings.LiftSpeed_ = value;
                RaisePropertyChanged();
            }
        }

        /*public override float PrintTime => 0;

        public override float UsedMaterial => 0;

        public override float MaterialCost => 0;

        public override string MaterialName => "Unknown";
        public override string MachineName => null;*/
        
        public override object[] Configs => new object[] { HeaderSettings };

        #endregion

        #region Constructors
        public LGSFile()
        {
        }
        #endregion

        #region Methods

        public unsafe byte[] PreviewEncode(Mat mat)
        {
            byte[] bytes = new byte[mat.Width * mat.Height * 2];
            var span = mat.GetBytePointer();
            var imageLength = mat.GetLength();

            int index = 0;
            for (int i = 0; i < imageLength; i+=3)
            {
                byte b = span[i];
                byte g = span[i+1];
                byte r = span[i+2];

                ushort rgb15 = (ushort) (((r >> 3) << 11) | ((g >> 3) << 6) | ((b >> 3) << 0));

                bytes[index++] = (byte) (rgb15 >> 8);
                bytes[index++] = (byte) (rgb15 & 0xff);
            }

            if (index != bytes.Length)
            {
                throw new FileLoadException($"Preview encode incomplete encode, expected: {bytes.Length}, encoded: {index}");
            }

            return bytes;
        }

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            if (ResolutionY >= 2560) // Longer Orange 30
            {
                HeaderSettings.Float_94 = 170;
            }
            
            //uint currentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
            using (var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write))
            {
                outputFile.WriteSerialize(HeaderSettings);
                outputFile.WriteBytes(PreviewEncode(Thumbnails[0]));

                LayerData[] layerData = new LayerData[LayerCount];

                Parallel.For(0, LayerCount, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    using (var mat = this[layerIndex].LayerMat)
                    {
                        layerData[layerIndex] = new LayerData();
                        layerData[layerIndex].Encode(mat);
                    }

                    lock (progress.Mutex)
                    {
                        progress++;
                    }
                });

                progress.ItemName = "Saving layers";
                progress.ProcessedItems = 0;

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    outputFile.WriteSerialize(layerData[layerIndex]);
                    progress++;
                }
            }

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine("-End-");
        }

        public unsafe Mat PreviewDecode(byte []data)
        {
            Mat mat = new Mat((int) HeaderSettings.PreviewSizeY, (int)HeaderSettings.PreviewSizeX, DepthType.Cv8U, 3);
            var span = mat.GetBytePointer();
            int spanIndex = 0;
            for (int i = 0; i < data.Length; i+=2)
            {
                ushort rgb15 = (ushort) ((ushort)(data[i + 0] << 8) | data[i + 1]);
                byte r = (byte) ((((rgb15 >> 11) & 0x1f) << 3) | 0x7);
                byte g = (byte) ((((rgb15 >> 6) & 0x1f) << 3) | 0x7);
                byte b = (byte) ((((rgb15 >> 0) & 0x1f) << 3) | 0x7);

                span[spanIndex++] = b;
                span[spanIndex++] = g;
                span[spanIndex++] = r;
            }

            return mat;
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using (var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
            {
                HeaderSettings = Helpers.Deserialize<Header>(inputFile);
                if (HeaderSettings.Name != Header.NameValue)
                {
                    throw new FileLoadException("Not a valid LGS file!", fileFullPath);
                }

                // Fix inconsistencies found of different version of plugin and slicers
                if (ResolutionX > ResolutionY)
                {
                    var oldX = ResolutionX;
                    ResolutionX = ResolutionY;
                    ResolutionY = oldX;
                }

                int previewSize = (int) (HeaderSettings.PreviewSizeX * HeaderSettings.PreviewSizeY * 2);
                byte[] previewData = new byte[previewSize];


                uint currentOffset = (uint) Helpers.Serializer.SizeOf(HeaderSettings);
                currentOffset += inputFile.ReadBytes(previewData);
                Thumbnails[0] = PreviewDecode(previewData);
  

                LayerData[] layerData = new LayerData[HeaderSettings.LayerCount];
                progress.Reset(OperationProgress.StatusGatherLayers, HeaderSettings.LayerCount);

                for (int layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    layerData[layerIndex] = Helpers.Deserialize<LayerData>(inputFile);
                    layerData[layerIndex].Parent = this;
                }

                LayerManager = new LayerManager(HeaderSettings.LayerCount, this);
                progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

                Parallel.For(0, LayerCount, 
                    //new ParallelOptions{MaxDegreeOfParallelism = 1},
                    layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;

                    using var image = layerData[layerIndex].Decode();
                    this[layerIndex] = new Layer((uint) layerIndex, image, LayerManager);

                    lock (progress.Mutex)
                    {
                        progress++;
                    }
                });

                LayerManager.RebuildLayersProperties();
            }
        }

        public override void SaveAs(string filePath = null, OperationProgress progress = null)
        {
            if (RequireFullEncode)
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    FileFullPath = filePath;
                }
                Encode(FileFullPath, progress);
                return;
            }


            if (!string.IsNullOrEmpty(filePath))
            {
                File.Copy(FileFullPath, filePath, true);
                FileFullPath = filePath;
            }

            using (var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write))
            {

                outputFile.Seek(0, SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);
            }
        }
        
        #endregion
    }
}
