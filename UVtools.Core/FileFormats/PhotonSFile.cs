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
    public class PhotonSFile : FileFormat
    {
        public const byte RLEEncodingLimit = 128; // 128;

        #region Sub Classes

        #region Header

        public class Header
        {
            public const uint ResolutionX = 1440;
            public const uint ResolutionY = 2560;

            public const float DisplayWidth = 68.04f;
            public const float DisplayHeight = 120.96f;
            public const float BuildZ = 150f;

            public const uint TAG1 = 2;
            public const ushort TAG2 = 49;


            [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public uint Tag1 { get; set; } = TAG1; // 2
            [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public ushort Tag2 { get; set; } = TAG2; // 49
            [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public double XYPixelSize { get; set; } = 0.04725; // 0.04725
            [FieldOrder(3)] [FieldEndianness(Endianness.Big)] public double LayerHeight { get; set; }
            [FieldOrder(4)] [FieldEndianness(Endianness.Big)] public double ExposureSeconds { get; set; }
            [FieldOrder(5)] [FieldEndianness(Endianness.Big)] public double LayerOffSeconds { get; set; }
            [FieldOrder(6)] [FieldEndianness(Endianness.Big)] public double BottomExposureSeconds { get; set; }
            [FieldOrder(7)] [FieldEndianness(Endianness.Big)] public uint BottomLayerCount { get; set; }
            [FieldOrder(8)] [FieldEndianness(Endianness.Big)] public double LiftHeight { get; set; } // mm
            [FieldOrder(9)] [FieldEndianness(Endianness.Big)] public double LiftSpeed { get; set; } // mm/s
            [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public double RetractSpeed { get; set; } // mm/s
            [FieldOrder(11)] [FieldEndianness(Endianness.Big)] public double Volume { get; set; } // ml
            [FieldOrder(12)] [FieldEndianness(Endianness.Big)] public uint PreviewResolutionX { get; set; } = 225;
            [FieldOrder(13)] [FieldEndianness(Endianness.Big)] public uint Unknown2 { get; set; } = 42;
            [FieldOrder(14)] [FieldEndianness(Endianness.Big)] public uint PreviewResolutionY { get; set; } = 168;
            [FieldOrder(15)] [FieldEndianness(Endianness.Big)] public uint Unknown4 { get; set; } = 10;

            public override string ToString()
            {
                return $"{nameof(Tag1)}: {Tag1}, {nameof(Tag2)}: {Tag2}, {nameof(XYPixelSize)}: {XYPixelSize}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(ExposureSeconds)}: {ExposureSeconds}, {nameof(LayerOffSeconds)}: {LayerOffSeconds}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(Volume)}: {Volume}, {nameof(PreviewResolutionX)}: {PreviewResolutionX}, {nameof(Unknown2)}: {Unknown2}, {nameof(PreviewResolutionY)}: {PreviewResolutionY}, {nameof(Unknown4)}: {Unknown4}";
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
            [Ignore] public uint RleDataSize => (DataSize >> 3) - 4;
            [FieldOrder(6)] [FieldEndianness(Endianness.Big)] public uint Unknown5 { get; set; } = 2684702720;

            [Ignore] public byte[] EncodedRle { get; set; }

            public override string ToString()
            {
                return $"{nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(DataSize)}: {DataSize}, {nameof(RleDataSize)}: {RleDataSize}, {nameof(Unknown5)}: {Unknown5}, {nameof(EncodedRle)}: {EncodedRle.Length}";
            }

            public unsafe byte[] Encode(Mat mat)
            {
                List<byte> rawData = new List<byte>();
                List<byte> chunk = new List<byte>();
                var spanMat = mat.GetBytePointer();
                var imageLength = mat.GetLength();

                
                EncodedRle = rawData.ToArray();
                DataSize = (uint)(EncodedRle.Length * 8 + 32);
                return EncodedRle;
            }

            public unsafe Mat Decode(bool consumeRle = true)
            {
                var mat = EmguExtensions.InitMat(new Size((int) ResolutionX, (int) ResolutionY));
                var matSpan = mat.GetBytePointer();
                var imageLength = mat.GetLength();

                int pixel = 0;
                foreach (var run in EncodedRle)
                {
                    byte col = (byte) ((run & 0x01) * 255);

                    var numPixelsInRun =
                        (((run & 128) > 0 ? 1 : 0) |
                         ((run & 64) > 0 ? 2 : 0) |
                         ((run & 32) > 0 ? 4 : 0) |
                         ((run & 16) > 0 ? 8 : 0) |
                         ((run &  8) > 0 ? 16 : 0) |
                         ((run &  4) > 0 ? 32 : 0) |
                         ((run &  2) > 0 ? 64 : 0)) + 1;
                    
                    for (; numPixelsInRun > 0; numPixelsInRun--)
                    {
                        if (pixel > imageLength)
                        {
                            mat.Dispose();
                            throw new FileLoadException($"Error image ran off the end, expecting {imageLength} pixels");
                        }
                        matSpan[pixel++] = col;
                    }
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

        public Header HeaderSettings { get; protected internal set; } = new Header();
        public LayerHeader LayerSettings { get; protected internal set; } = new LayerHeader();
        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("photons", "Chitubox PhotonS Files"),
        };

        public override Type[] ConvertToFormats { get; } =
        {
            //typeof(UVJFile),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
            PrintParameterModifier.ExposureSeconds,

            //PrintParameterModifier.BottomLayerOffTime,
            PrintParameterModifier.LayerOffTime,
            //PrintParameterModifier.BottomLiftHeight,
            //PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
        };

        public override byte ThumbnailsCount { get; } = 1;

        public override Size[] ThumbnailsOriginalSize { get; } = {new Size(225, 168) };

        public override uint ResolutionX
        {
            get => Header.ResolutionX;
            set
            {
                
            }
        }

        public override uint ResolutionY
        {
            get => Header.ResolutionY;
            set
            {
            }
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

        public override byte AntiAliasing => 1;

        public override float LayerHeight
        {
            get => (float) Math.Round(HeaderSettings.LayerHeight);
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
                LayerSettings.LayerCount = LayerCount;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(NormalLayerCount));
            }
        }

        public override ushort BottomLayerCount
        {
            get => (ushort) HeaderSettings.BottomLayerCount;
            set
            {
                HeaderSettings.BottomLayerCount = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomExposureTime
        {
            get => (float) HeaderSettings.BottomExposureSeconds;
            set
            {
                HeaderSettings.BottomExposureSeconds = value;
                RaisePropertyChanged();
            }
        }

        public override float ExposureTime
        {
            get => (float)HeaderSettings.ExposureSeconds;
            set
            {
                HeaderSettings.ExposureSeconds = value;
                RaisePropertyChanged();
            }
        }

        /*public override float BottomLayerOffTime
        {
            get => HeaderSettings.BottomLightOffDelayMs;
            set
            {
                HeaderSettings.BottomLightOffDelayMs = value;
                RaisePropertyChanged();
            }
        }*/

        public override float LayerOffTime
        {
            get => (float) HeaderSettings.LayerOffSeconds;
            set
            {
                HeaderSettings.LayerOffSeconds = value;
                RaisePropertyChanged();
            }
        }

        /*public override float BottomLiftHeight
        {
            get => HeaderSettings.BottomLiftHeight;
            set
            {
                HeaderSettings.BottomLiftHeight = value;
                RaisePropertyChanged();
            }
        }*/

        public override float LiftHeight
        {
            get => (float) HeaderSettings.LiftHeight;
            set
            {
                HeaderSettings.LiftHeight = value;
                RaisePropertyChanged();
            }
        }

        /*public override float BottomLiftSpeed
        {
            get => HeaderSettings.BottomLiftSpeed;
            set
            {
                HeaderSettings.BottomLiftSpeed = HeaderSettings.BottomLiftSpeed_ = value;
                RaisePropertyChanged();
            }
        }*/

        public override float LiftSpeed
        {
            get => (float) Math.Round(HeaderSettings.LiftSpeed * 60.0);
            set
            {
                HeaderSettings.LiftSpeed = Math.Round(value / 60.0);
                RaisePropertyChanged();
            }
        }

        public override float RetractSpeed
        {
            get => (float)Math.Round(HeaderSettings.RetractSpeed * 60.0);
            set
            {
                HeaderSettings.RetractSpeed = Math.Round(value / 60.0);
                RaisePropertyChanged();
            }
        }

        public override float PrintTime => 0;

        public override float UsedMaterial => (float) HeaderSettings.Volume;

        public override float MaterialCost => 0;

        public override string MaterialName => "Unknown";
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
        public override void Encode(string fileFullPath, OperationProgress progress = null)
        {
            base.Encode(fileFullPath, progress);

            //uint currentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
            using (var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write))
            {
                outputFile.WriteSerialize(HeaderSettings);
                outputFile.WriteBytes(PreviewEncode(Thumbnails[0]));
                outputFile.WriteSerialize(LayerSettings);

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
                    outputFile.WriteBytes(layerData[layerIndex].EncodedRle);
                    progress++;
                }
            }

            AfterEncode();

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine("-End-");
        }

        public unsafe Mat PreviewDecode(byte []data)
        {
            Mat mat = new Mat((int) HeaderSettings.PreviewResolutionX, (int)HeaderSettings.PreviewResolutionY, DepthType.Cv8U, 3);
            var span = mat.GetBytePointer();
            int spanIndex = 0;
            for (int i = 0; i < data.Length; i+=2)
            {
                ushort color16 = (ushort)(data[i] + (data[i + 1] << 8));

                var r = (color16 >> 11) & 0x1F;
                var g = (color16 >> 5) & 0x3F;
                var b = (color16 >> 0) & 0x1F;

                /*span[spanIndex++] = (byte)(b << 3);
                span[spanIndex++] = (byte)(g << 2);
                span[spanIndex++] = (byte)(r << 3);*/

                span[spanIndex++] = (byte)((b << 3) | (b & 0x7));
                span[spanIndex++] = (byte)((g << 2) | (g & 0x3));
                span[spanIndex++] = (byte)((r << 3) | (r & 0x7));
            }

            return mat;
        }

        public override void Decode(string fileFullPath, OperationProgress progress = null)
        {
            base.Decode(fileFullPath, progress);

            using (var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
            {
                HeaderSettings = Helpers.Deserialize<Header>(inputFile);
                if (HeaderSettings.Tag1 != Header.TAG1 || HeaderSettings.Tag2 != Header.TAG2)
                {
                    throw new FileLoadException("Not a valid PHOTONS file! TAGs doesn't match", fileFullPath);
                }

                HeaderSettings.LayerHeight = Math.Round(HeaderSettings.LayerHeight, 2);
                HeaderSettings.Volume = Math.Round(HeaderSettings.Volume, 2);

                int previewSize = (int) (HeaderSettings.PreviewResolutionX * HeaderSettings.PreviewResolutionY * 2);
                byte[] previewData = new byte[previewSize];


                uint currentOffset = (uint) Helpers.Serializer.SizeOf(HeaderSettings);
                currentOffset += inputFile.ReadBytes(previewData);
                Thumbnails[0] = PreviewDecode(previewData);

                LayerSettings = Helpers.Deserialize<LayerHeader>(inputFile);
                currentOffset += (uint)Helpers.Serializer.SizeOf(LayerSettings);

                Debug.WriteLine(HeaderSettings);
                Debug.WriteLine(LayerSettings);
  

                LayerData[] layerData = new LayerData[LayerSettings.LayerCount];
                progress.Reset(OperationProgress.StatusGatherLayers, LayerSettings.LayerCount);

                for (int layerIndex = 0; layerIndex < LayerSettings.LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    layerData[layerIndex] = Helpers.Deserialize<LayerData>(inputFile);
                    layerData[layerIndex].EncodedRle = new byte[layerData[layerIndex].RleDataSize];
                    currentOffset += inputFile.ReadBytes(layerData[layerIndex].EncodedRle);
                    Debug.WriteLine($"Layer {layerIndex} -> {layerData[layerIndex]}");
                }

                LayerManager = new LayerManager(LayerSettings.LayerCount, this);
                progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

                Parallel.For(0, LayerCount, 
                    //new ParallelOptions{MaxDegreeOfParallelism = 1},
                    layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;

                    using (var image = layerData[layerIndex].Decode())
                    {
                        this[layerIndex] = new Layer((uint) layerIndex, image, LayerManager);
                        lock (progress.Mutex)
                        {
                            progress++;
                        }
                    }
                });

                LayerManager.RebuildLayersProperties();
                

                FileFullPath = fileFullPath;

            }

            progress.Token.ThrowIfCancellationRequested();
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

        public override bool Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
            return false;
        }
        #endregion
    }
}
