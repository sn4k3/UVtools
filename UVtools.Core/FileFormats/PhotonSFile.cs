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
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class PhotonSFile : FileFormat
    {
        #region Constants
        public const byte RLEEncodingLimit = 128;

        public const ushort RESOLUTION_X = 1440;
        public const ushort RESOLUTION_Y = 2560;

        public const float DISPLAY_WIDTH = 68.04f;
        public const float DISPLAY_HEIGHT = 120.96f;
        public const float MACHINE_Z = 165f;

        #endregion

        #region Members

        private uint _resolutionX = RESOLUTION_X;
        private uint _resolutionY = RESOLUTION_Y;

        #endregion

        #region Sub Classes

        #region Header

        public class Header
        {
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

        public class LayerDef
        {
            [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public uint Unknown1 { get; set; } = 44944;
            [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public uint Unknown2 { get; set; } = 0;
            [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public uint Unknown3 { get; set; } = 0;
            [FieldOrder(3)] [FieldEndianness(Endianness.Big)] public uint ResolutionX { get; set; } = RESOLUTION_X;
            [FieldOrder(4)] [FieldEndianness(Endianness.Big)] public uint ResolutionY { get; set; } = RESOLUTION_Y;
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

            public LayerDef()
            {
            }

            public LayerDef(Mat mat)
            {
                ResolutionX = (uint)mat.Width;
                ResolutionY = (uint)mat.Height;
            }

            public override string ToString()
            {
                return $"{nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(DataSize)}: {DataSize}, {nameof(RleDataSize)}: {RleDataSize}, {nameof(Unknown5)}: {Unknown5}, {nameof(EncodedRle)}: {EncodedRle?.Length}";
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

            public Mat Decode(bool consumeRle = true)
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

                // Some slicers will not fill the pixel RLE to the end when the remaining pixels are trailing black,
                // this was triggering error on read because data checksum was incomplete, ignoring checksum now (#344)
                /*if (pixelPos != imageLength && pixelPos-1 != imageLength)
                {
                    mat.Dispose();
                    throw new FileLoadException($"Error image ran shortly or off the end, expecting {imageLength} pixels, got {pixelPos} pixels.");
                }*/

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
            get => _resolutionX;
            set
            {
                if(!RaiseAndSetIfChanged(ref _resolutionX, value)) return;
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

        public override Enumerations.FlipDirection DisplayMirror
        {
            get => Enumerations.FlipDirection.Horizontally;
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
            get => MACHINE_Z;
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
        
        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            //throw new NotSupportedException("PhotonS is read-only format, please use pws instead!");
            using var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
            outputFile.WriteSerialize(HeaderSettings);
            outputFile.WriteBytes(EncodeImage(DATATYPE_BGR565, Thumbnails[0]));
            outputFile.WriteSerialize(LayerSettings);

            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
            var layerData = new LayerDef[LayerCount];

            foreach (var batch in BatchLayersIndexes())
            {
                Parallel.ForEach(batch, CoreSettings.ParallelOptions, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    using (var mat = this[layerIndex].LayerMat)
                    {
                        layerData[layerIndex] = new LayerDef(mat);
                        layerData[layerIndex].Encode(mat);
                    }
                    progress.LockAndIncrement();
                });

                foreach (var layerIndex in batch)
                {
                    progress.Token.ThrowIfCancellationRequested();

                    outputFile.WriteSerialize(layerData[layerIndex]);
                    outputFile.WriteBytes(layerData[layerIndex].EncodedRle);

                    layerData[layerIndex].EncodedRle = null; // Free
                }
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

            inputFile.ReadBytes(previewData);
            Thumbnails[0] = DecodeImage(DATATYPE_BGR565, previewData, HeaderSettings.PreviewResolutionX, HeaderSettings.PreviewResolutionY);

            LayerSettings = Helpers.Deserialize<LayerHeader>(inputFile);
            
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine(LayerSettings);
  

            LayerManager.Init(LayerSettings.LayerCount);
            var layersDefinitions = new LayerDef[LayerSettings.LayerCount];

            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
            foreach (var batch in BatchLayersIndexes())
            {
                foreach (var layerIndex in batch)
                {
                    progress.Token.ThrowIfCancellationRequested();

                    var layerDef = Helpers.Deserialize<LayerDef>(inputFile);
                    layersDefinitions[layerIndex] = layerDef;

                    layerDef.EncodedRle = inputFile.ReadBytes(layerDef.RleDataSize);

                    Debug.Write($"LAYER {layerIndex} -> ");
                    Debug.WriteLine(layerDef);

                    if (layerIndex == 1)
                    {
                        // Auto fix resolution if needed
                        ResolutionX = layersDefinitions[layerIndex].ResolutionX;
                        ResolutionY = layersDefinitions[layerIndex].ResolutionY;
                    }
                }

                Parallel.ForEach(batch, CoreSettings.ParallelOptions, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    using (var mat = layersDefinitions[layerIndex].Decode())
                    {
                        this[layerIndex] = new Layer((uint)layerIndex, mat, this);
                    }

                    progress.LockAndIncrement();
                });
            }

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
