/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

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
    public class PhotonSFile : FileFormat
    {
        public const byte RLEEncodingLimit = 128;

        #region Sub Classes

        #region Header

        public class Header
        {
            public const uint ResolutionX = 1440;
            public const uint ResolutionY = 2560;

            public const float DisplayWidth = 68.04f;
            public const float DisplayHeight = 120.96f;
            public const float BuildZ = 165f;

            public const uint TAG1 = 2;
            public const ushort TAG2 = 49;


            [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public uint Tag1 { get; set; } = TAG1; // 2
            [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public ushort Tag2 { get; set; } = TAG2; // 49
            [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public double XYPixelSize { get; set; } = 0.04725; // 0.04725
            [FieldOrder(3)] [FieldEndianness(Endianness.Big)] public double LayerHeight { get; set; }
            [FieldOrder(4)] [FieldEndianness(Endianness.Big)] public double ExposureSeconds { get; set; }
            [FieldOrder(5)] [FieldEndianness(Endianness.Big)] public double LightOffDelay { get; set; }
            [FieldOrder(6)] [FieldEndianness(Endianness.Big)] public double BottomExposureSeconds { get; set; }
            [FieldOrder(7)] [FieldEndianness(Endianness.Big)] public uint BottomLayerCount { get; set; }
            [FieldOrder(8)] [FieldEndianness(Endianness.Big)] public double LiftHeight { get; set; } // mm
            [FieldOrder(9)] [FieldEndianness(Endianness.Big)] public double LiftSpeed { get; set; } // mm/s
            [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public double RetractSpeed { get; set; } // mm/s
            [FieldOrder(11)] [FieldEndianness(Endianness.Big)] public double VolumeMl { get; set; } // ml
            [FieldOrder(12)] [FieldEndianness(Endianness.Big)] public uint PreviewResolutionX { get; set; } = 224;
            [FieldOrder(13)] [FieldEndianness(Endianness.Big)] public uint Unknown2 { get; set; } = 42;
            [FieldOrder(14)] [FieldEndianness(Endianness.Big)] public uint PreviewResolutionY { get; set; } = 168;
            [FieldOrder(15)] [FieldEndianness(Endianness.Big)] public uint Unknown4 { get; set; } = 10;

            public override string ToString()
            {
                return $"{nameof(Tag1)}: {Tag1}, {nameof(Tag2)}: {Tag2}, {nameof(XYPixelSize)}: {XYPixelSize}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(ExposureSeconds)}: {ExposureSeconds}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(VolumeMl)}: {VolumeMl}, {nameof(PreviewResolutionX)}: {PreviewResolutionX}, {nameof(Unknown2)}: {Unknown2}, {nameof(PreviewResolutionY)}: {PreviewResolutionY}, {nameof(Unknown4)}: {Unknown4}";
            }
        }

        public class LayerHeader
        {
            [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public uint LayerCount { get; set; }

            public override string ToString()
            {
                return $"{nameof(LayerCount)}: {LayerCount}";
            }
        }

        #endregion

        #region LayerDef

        public class LayerData
        {
            [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public uint Unknown1 { get; set; } = 44944;
            [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public uint Unknown2 { get; set; } = 0;
            [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public uint Unknown3 { get; set; } = 0;
            [FieldOrder(3)] [FieldEndianness(Endianness.Big)] public uint ResolutionX { get; set; } = 1440;
            [FieldOrder(4)] [FieldEndianness(Endianness.Big)] public uint ResolutionY { get; set; } = 2560;
            [FieldOrder(5)] [FieldEndianness(Endianness.Big)] public uint DataSize { get; set; }
            [Ignore] public uint RleDataSize
            {
                get => (DataSize >> 3) - 4;
                set => DataSize = (value + 4) << 3;
                //get => DataSize / 8 - 4;
                //set => DataSize = (value + 4) * 8;
            }

            [FieldOrder(6)] [FieldEndianness(Endianness.Big)] public uint Unknown5 { get; set; } = 2684702720;

            [Ignore] public byte[] EncodedRle { get; set; }

            public override string ToString()
            {
                return $"{nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(DataSize)}: {DataSize}, {nameof(RleDataSize)}: {RleDataSize}, {nameof(Unknown5)}: {Unknown5}, {nameof(EncodedRle)}: {EncodedRle.Length}";
            }

            public unsafe byte[] Encode(Mat mat)
            {
                List<byte> rawData = new();
                var spanMat = mat.GetBytePointer();
                var imageLength = mat.GetLength();

                int rep = 0;
                byte color = 0;
                int totalPixels = 0;

                void AddRep()
                {
                    if (rep <= 0) return;

                    totalPixels += rep;
                    rep--;
                    byte rle = (byte) (((rep & 1) > 0 ? 128 : 0) |
                                       ((rep & 2) > 0 ? 64 : 0) |
                                       ((rep & 4) > 0 ? 32 : 0) |
                                       ((rep & 8) > 0 ? 16 : 0) |
                                       ((rep & 16) > 0 ? 8 : 0) |
                                       ((rep & 32) > 0 ? 4 : 0) |
                                       ((rep & 64) > 0 ? 2 : 0) | color);

                    rawData.Add(rle);
                }

                for (int i = 0; i < imageLength; i++)
                {
                    byte thisColor = spanMat[i] <= 127 ? (byte)0 : (byte)1; // Sanitize no AA
                    if (thisColor != color)
                    {
                        AddRep();
                        color = thisColor; // Sanitize no AA
                        rep = 1;
                    }
                    else
                    {
                        rep++;
                        if (rep == RLEEncodingLimit)
                        {
                            AddRep();
                            rep = 0;
                        }
                    }
                }

                AddRep();

                if (totalPixels != imageLength)
                {
                    throw new FileLoadException($"Error image ran shortly or off the end, expecting {imageLength} pixels, got {totalPixels} pixels.");
                }

                EncodedRle = rawData.ToArray();
                RleDataSize = (uint) EncodedRle.Length;
                return EncodedRle;
            }

            public unsafe Mat Decode(bool consumeRle = true)
            {
                var mat = EmguExtensions.InitMat(new Size((int) ResolutionX, (int) ResolutionY));
                //var matSpan = mat.GetBytePointer();
                var imageLength = mat.GetLength();

                int pixelPos = 0;
                foreach (var run in EncodedRle)
                {
                    if (pixelPos > imageLength)
                    {
                        mat.Dispose();
                        throw new FileLoadException($"Error image ran off the end, expecting {imageLength} pixels.");
                    }

                    byte brightness = (byte) ((run & 0x01) * 255);

                    int numPixelsInRun =
                             (((run & 128) > 0 ? 1 : 0) |
                              ((run & 64) > 0 ? 2 : 0) |
                              ((run & 32) > 0 ? 4 : 0) |
                              ((run & 16) > 0 ? 8 : 0) |
                              ((run &  8) > 0 ? 16 : 0) |
                              ((run &  4) > 0 ? 32 : 0) |
                              ((run &  2) > 0 ? 64 : 0)) + 1;

                    mat.FillSpan(ref pixelPos, numPixelsInRun, brightness);

                    /*if (brightness == 0) // Don't fill black pixels
                    {
                        pixelPos += numPixelsInRun;
                        continue;
                    }

                    for (; numPixelsInRun > 0; numPixelsInRun--)
                    {
                        if (pixelPos > imageLength)
                        {
                            mat.Dispose();
                            throw new FileLoadException($"Error image ran off the end, expecting {imageLength} pixels.");
                        }
                        matSpan[pixelPos++] = brightness;
                    }*/
                }

                if (pixelPos != imageLength && pixelPos-1 != imageLength)
                {
                    mat.Dispose();
                    throw new FileLoadException($"Error image ran shortly or off the end, expecting {imageLength} pixels, got {pixelPos} pixels.");
                }

                // Not required as mat is all black by default
                //for (;pixel < imageLength; pixel++) matSpan[pixel] = 0;

                if (consumeRle)
                    EncodedRle = null;

                return mat;
            }
        }
        #endregion

        #endregion

        #region Properties

        public Header HeaderSettings { get; protected internal set; } = new();
        public LayerHeader LayerSettings { get; protected internal set; } = new();
        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new(typeof(PhotonSFile), "photons", "Chitubox PhotonS"),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,

            PrintParameterModifier.LightOffDelay,

            PrintParameterModifier.BottomExposureTime,
            PrintParameterModifier.ExposureTime,

            //PrintParameterModifier.BottomLightOffDelay,
            
            //PrintParameterModifier.BottomLiftHeight,
            //PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
        };


        public override Size[] ThumbnailsOriginalSize { get; } = {new(224, 168) };

        public override uint ResolutionX
        {
            get => Header.ResolutionX;
            set { }
        }

        public override uint ResolutionY
        {
            get => Header.ResolutionY;
            set { }
        }

        public override float DisplayWidth
        {
            get => Header.DisplayWidth;
            set { }
        }

        public override float DisplayHeight
        {
            get => Header.DisplayHeight;
            set { }
        }

        public override bool DisplayMirror
        {
            get => true;
            set { }
        }

        public override byte AntiAliasing
        {
            get => 1;
            set { }
        }

        public override float LayerHeight
        {
            get => (float) Layer.RoundHeight(HeaderSettings.LayerHeight);
            set
            {
                HeaderSettings.LayerHeight = Layer.RoundHeight(value);
                RaisePropertyChanged();
            }
        }

        public override float MachineZ
        {
            get => Header.BuildZ;
            set { }
        }

        public override uint LayerCount
        {
            get => base.LayerCount;
            set => base.LayerCount = LayerSettings.LayerCount = LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => (ushort) HeaderSettings.BottomLayerCount;
            set => base.BottomLayerCount = (ushort) (HeaderSettings.BottomLayerCount = value);
        }

        public override float BottomLightOffDelay => LightOffDelay;

        public override float LightOffDelay
        {
            get => (float)HeaderSettings.LightOffDelay;
            set => base.LightOffDelay = (float)(HeaderSettings.LightOffDelay = Math.Round(value, 2));
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
            get => (float) HeaderSettings.BottomExposureSeconds;
            set => base.BottomExposureTime = (float) (HeaderSettings.BottomExposureSeconds = Math.Round(value, 2));
        }

        public override float ExposureTime
        {
            get => (float) HeaderSettings.ExposureSeconds;
            set => base.ExposureTime = (float) (HeaderSettings.ExposureSeconds = Math.Round(value, 2));
        }

        public override float BottomLiftHeight => LiftHeight;
        
        public override float LiftHeight
        {
            get => (float) HeaderSettings.LiftHeight;
            set => base.LiftHeight = (float) (HeaderSettings.LiftHeight = Math.Round(value, 2));
        }

        public override float BottomLiftSpeed => LiftSpeed;

        public override float LiftSpeed
        {
            get => (float) Math.Round(HeaderSettings.LiftSpeed * 60.0, 2);
            set => base.LiftSpeed = (float) (HeaderSettings.LiftSpeed = Math.Round(value / 60.0, 2));
        }

        public override float BottomRetractSpeed => RetractSpeed;

        public override float RetractSpeed
        {
            get => (float)Math.Round(HeaderSettings.RetractSpeed * 60.0, 2);
            set => base.RetractSpeed = (float) (HeaderSettings.RetractSpeed = (float) Math.Round(value / 60.0, 2));
        }

        
        public override float MaterialMilliliters
        {
            get => base.MaterialMilliliters;
            set
            {
                base.MaterialMilliliters = value;
                HeaderSettings.VolumeMl = base.MaterialMilliliters;
            }
        }

        public override string MachineName => "Anycubic Photon S";
        
        public override object[] Configs => new object[] { HeaderSettings };

        #endregion

        #region Constructors
        public PhotonSFile()
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
                byte r = span[i + 2]; // 60
                byte g = span[i + 1];
                byte b = span[i];

                ushort color = (ushort)(((b & 0xF8) << 8) | ((g & 0xFC) << 3) | (r >> 3)); 

                bytes[index++] = (byte)color;
                bytes[index++] = (byte)(color >> 8);
            }

            if (index != bytes.Length)
            {
                throw new FileLoadException($"Preview encode incomplete encode, expected: {bytes.Length}, encoded: {index}");
            }

            return bytes;
        }

        public unsafe Mat PreviewDecode(byte[] data)
        {
            Mat mat = new((int)HeaderSettings.PreviewResolutionY, (int)HeaderSettings.PreviewResolutionX, DepthType.Cv8U, 3);
            var span = mat.GetBytePointer();
            int spanIndex = 0;
            for (int i = 0; i < data.Length; i += 2)
            {
                ushort color16 = BitExtensions.ToUShortLittleEndian(data[i], data[i + 1]);

                //var r = (byte)((color16 & 0x1F) << 3);
                //var g = (byte)(((color16 >> 5) & 0x3F) << 2);
                //var b = (byte)(((color16 >> 11) & 0x1f) << 3);
                var r = (byte)((color16 << 3) & 0xF8); // Mask: 11111000
                var g = (byte)((color16 >> 3) & 0xFC); // Mask: 11111100
                var b = (byte)((color16 >> 8) & 0xF8); // Mask: 11111000

                span[spanIndex++] = b;
                span[spanIndex++] = g;
                span[spanIndex++] = r;
            }

            return mat;
        }

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            //throw new NotSupportedException("PhotonS is read-only format, please use pws instead!");
            //uint currentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
            using var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
            outputFile.WriteSerialize(HeaderSettings);
            outputFile.WriteBytes(PreviewEncode(Thumbnails[0]));
            outputFile.WriteSerialize(LayerSettings);

            var layerData = new LayerData[LayerCount];

            Parallel.For(0, LayerCount, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                using (var mat = this[layerIndex].LayerMat)
                {
                    layerData[layerIndex] = new LayerData();
                    layerData[layerIndex].Encode(mat);
                }

                progress.LockAndIncrement();
            });

            progress.ItemName = "Saving layers";
            progress.ProcessedItems = 0;

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                progress.Token.ThrowIfCancellationRequested();
                outputFile.WriteSerialize(layerData[layerIndex]);
                outputFile.WriteBytes(layerData[layerIndex].EncodedRle);
                progress++;
            }

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine("-End-");
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);
            HeaderSettings = Helpers.Deserialize<Header>(inputFile);
            if (HeaderSettings.Tag1 != Header.TAG1 || HeaderSettings.Tag2 != Header.TAG2)
            {
                throw new FileLoadException("Not a valid PHOTONS file! TAGs doesn't match", fileFullPath);
            }

            int previewSize = (int) (HeaderSettings.PreviewResolutionX * HeaderSettings.PreviewResolutionY * 2);
            byte[] previewData = new byte[previewSize];


            uint currentOffset = (uint) Helpers.Serializer.SizeOf(HeaderSettings);
            currentOffset += inputFile.ReadBytes(previewData);
            Thumbnails[0] = PreviewDecode(previewData);

            LayerSettings = Helpers.Deserialize<LayerHeader>(inputFile);
            currentOffset += (uint)Helpers.Serializer.SizeOf(LayerSettings);

            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine(LayerSettings);
  

            var layerData = new LayerData[LayerSettings.LayerCount];
            progress.Reset(OperationProgress.StatusGatherLayers, LayerSettings.LayerCount);

            for (int layerIndex = 0; layerIndex < LayerSettings.LayerCount; layerIndex++)
            {
                progress.Token.ThrowIfCancellationRequested();
                layerData[layerIndex] = Helpers.Deserialize<LayerData>(inputFile);
                layerData[layerIndex].EncodedRle = new byte[layerData[layerIndex].RleDataSize];
                currentOffset += inputFile.ReadBytes(layerData[layerIndex].EncodedRle);
                Debug.WriteLine($"Layer {layerIndex} -> {layerData[layerIndex]}");
            }

            LayerManager.Init(LayerSettings.LayerCount);
            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

            Parallel.For(0, LayerCount, 
                //new ParallelOptions{MaxDegreeOfParallelism = 1},
                layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;

                    using var image = layerData[layerIndex].Decode();
                    this[layerIndex] = new Layer((uint) layerIndex, image, this);
                    progress.LockAndIncrement();
                });

            LayerManager.RebuildLayersProperties();
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

            using var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write);
            outputFile.Seek(0, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);
        }

        #endregion
    }
}
