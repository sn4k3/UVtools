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
            [FieldOrder(3)] public uint Uint_10 { get; set; } = 10; // 0x10: 10 ?
            [FieldOrder(4)] public uint Uint_14 { get; set; } = 0; // 0x14: 0 ?
            [FieldOrder(5)] public uint Uint_18 { get; set; } = 34; // 0x18: 34 ?
            [FieldOrder(6)] public float PixelPerMmY { get; set; }
            [FieldOrder(7)] public float PixelPerMmX { get; set; }
            [FieldOrder(8)] public float ResolutionY { get; set; }
            [FieldOrder(9)] public float ResolutionX { get; set; }
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

            public byte[] Encode(Mat mat)
            {
                List<byte> rawData = new List<byte>();

                var spanMat = mat.GetPixelSpan<byte>();

                uint span = 0;
                byte lc = 0;

                void addSpan(){
                    for(; span > 0; span >>= 4) {
                        rawData.Add((byte) ((byte)(span & 0xf) | (lc & 0xf0)));
                    }
                }

                for (int i = 0; i < spanMat.Length; i++)
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

            public Mat Decode(bool consumeRle = true)
            {
                Mat mat = new Mat(new Size((int)Parent.HeaderSettings.ResolutionX, (int) Parent.HeaderSettings.ResolutionY), DepthType.Cv8U, 1);
                var matSpan = mat.GetPixelSpan<byte>();

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
                            if (index >= matSpan.Length)
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
                    if (index >= matSpan.Length)
                    {
                        throw new FileLoadException($"'{span}' bytes to many");
                    }

                    matSpan[index++] = last;
                }

                if (index != matSpan.Length)
                {
                    throw new FileLoadException($"Incomplete buffer, expected: {matSpan.Length}, got: {index}");
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
            new FileExtension("lgs", "Longer Orange 10 Files"),
            new FileExtension("lgs30", "Longer Orange 30 Files"),
        };

        public override Type[] ConvertToFormats { get; } =
        {
            typeof(UVJFile),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.InitialLayerCount,
            PrintParameterModifier.InitialExposureSeconds,
            PrintParameterModifier.ExposureSeconds,

            PrintParameterModifier.BottomLayerOffTime,
            PrintParameterModifier.LayerOffTime,
            PrintParameterModifier.BottomLiftHeight,
            PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            //PrintParameterModifier.RetractSpeed,

            //PrintParameterModifier.BottomLightPWM,
            //PrintParameterModifier.LightPWM,
        };

        public override byte ThumbnailsCount { get; } = 1;

        public override Size[] ThumbnailsOriginalSize { get; } = {new Size(120, 150)};

        public override uint ResolutionX
        {
            get => (uint) HeaderSettings.ResolutionX;
            set => HeaderSettings.ResolutionX = value;
        }

        public override uint ResolutionY
        {
            get => (uint)HeaderSettings.ResolutionY;
            set => HeaderSettings.ResolutionX = value;
        }

        public override byte AntiAliasing => 4;

        public override float LayerHeight
        {
            get => HeaderSettings.LayerHeight;
            set => HeaderSettings.LayerHeight = value;
        }

        public override uint LayerCount
        {
            set => HeaderSettings.LayerCount = LayerCount;
        }

        public override ushort InitialLayerCount => (ushort) (HeaderSettings.BottomHeight / LayerHeight);

        public override float InitialExposureTime => (float) Math.Round(HeaderSettings.BottomExposureTimeMs/1000, 2);

        public override float LayerExposureTime => (float) Math.Round(HeaderSettings.ExposureTimeMs/1000, 2);
        public override float LiftHeight => HeaderSettings.LiftHeight;
        public override float LiftSpeed => HeaderSettings.LiftSpeed;
        public override float RetractSpeed => 0;

        public override float PrintTime => 0;

        public override float UsedMaterial => 0;

        public override float MaterialCost => 0;

        public override string MaterialName => "Unknown";
        public override string MachineName => null;
        
        public override object[] Configs => new object[] { HeaderSettings };

        #endregion

        #region Constructors
        public LGSFile()
        {
        }
        #endregion

        #region Methods

        public byte[] PreviewEncode(Mat mat)
        {
            var span = mat.GetPixelSpan<byte>();
            byte[] bytes = new byte[mat.Width*mat.Height*2];

            int index = 0;
            for (int i = 0; i < span.Length; i+=3)
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

            AfterEncode();

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine("-End-");
        }

        public Mat PreviewDecode(byte []data)
        {
            Mat mat = new Mat((int) HeaderSettings.PreviewSizeY, (int)HeaderSettings.PreviewSizeX, DepthType.Cv8U, 3);
            var span = mat.GetPixelSpan<byte>();
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

        public override void Decode(string fileFullPath, OperationProgress progress = null)
        {
            base.Decode(fileFullPath, progress);

            using (var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
            {
                //HeaderSettings = Helpers.ByteToType<CbddlpFile.Header>(InputFile);
                //HeaderSettings = Helpers.Serializer.Deserialize<Header>(InputFile.ReadBytes(Helpers.Serializer.SizeOf(typeof(Header))));
                HeaderSettings = Helpers.Deserialize<Header>(inputFile);
                if (HeaderSettings.Name != Header.NameValue)
                {
                    throw new FileLoadException("Not a valid LGS file!", fileFullPath);
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

                Parallel.For(0, LayerCount, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;

                    using (var image = layerData[layerIndex].Decode())
                    {
                        this[layerIndex] = new Layer((uint) layerIndex, image);

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

        public override object GetValueFromPrintParameterModifier(PrintParameterModifier modifier)
        {
            var baseValue = base.GetValueFromPrintParameterModifier(modifier);
            if (!ReferenceEquals(baseValue, null)) return baseValue;
            /*if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerOffTime)) return PrintParametersSettings.BottomLightOffDelay;
            if (ReferenceEquals(modifier, PrintParameterModifier.LayerOffTime)) return PrintParametersSettings.LightOffDelay;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight)) return PrintParametersSettings.BottomLiftHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed)) return PrintParametersSettings.BottomLiftSpeed;*/
            /*if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight)) return PrintParametersSettings.LiftHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed)) return PrintParametersSettings.LiftingSpeed;
            if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed)) return PrintParametersSettings.RetractSpeed;*/

            /*if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightPWM)) return HeaderSettings.BottomLightPWM;
            if (ReferenceEquals(modifier, PrintParameterModifier.LightPWM)) return HeaderSettings.LightPWM;*/



            return null;
        }

        public override bool SetValueFromPrintParameterModifier(PrintParameterModifier modifier, string value)
        {
            return false;
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

            /*using (var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write))
            {

                outputFile.Seek(0, SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

                if (HeaderSettings.Version >= 2 && HeaderSettings.PrintParametersOffsetAddress > 0)
                {
                    outputFile.Seek(HeaderSettings.PrintParametersOffsetAddress, SeekOrigin.Begin);
                    Helpers.SerializeWriteFileStream(outputFile, PrintParametersSettings);
                    Helpers.SerializeWriteFileStream(outputFile, SlicerInfoSettings);
                }

                uint layerOffset = HeaderSettings.LayersDefinitionOffsetAddress;
                for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
                {
                    for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                    {
                        outputFile.Seek(layerOffset, SeekOrigin.Begin);
                        layerOffset += Helpers.SerializeWriteFileStream(outputFile, LayersDefinitions[aaIndex, layerIndex]);
                    }
                }
            }*/

            //Decode(FileFullPath, progress);
        }

        public override bool Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
            /*if (to == typeof(ChituboxFile))
            {
                if (Path.GetExtension(FileFullPath).Equals(Path.GetExtension(fileFullPath)))
                {
                    return false;
                }
                ChituboxFile file = new ChituboxFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        BedSizeX = HeaderSettings.BedSizeX,
                        BedSizeY = HeaderSettings.BedSizeY,
                        BedSizeZ = HeaderSettings.BedSizeZ,
                        ProjectorType = HeaderSettings.ProjectorType,
                        LayerCount = LayerCount,
                        AntiAliasLevel = ValidateAntiAliasingLevel(),
                        BottomLightPWM = (byte) HeaderSettings.BottomLightPWM,
                        LightPWM = (byte) HeaderSettings.LightPWM,
                        LayerOffTime = HeaderSettings.LayerOffTime,
                        PrintTime = HeaderSettings.PrintTime,
                        BottomExposureSeconds = HeaderSettings.BottomExposureSeconds,
                        BottomLayersCount = HeaderSettings.BottomLayersCount,
                        //EncryptionKey = HeaderSettings.EncryptionKey,
                        LayerExposureSeconds = HeaderSettings.LayerExposureSeconds,
                        LayerHeightMilimeter = HeaderSettings.LayerHeightMilimeter,
                        OverallHeightMilimeter = HeaderSettings.OverallHeightMilimeter,
                    },
                    PrintParametersSettings =
                    {
                        LiftSpeed = PrintParametersSettings.LiftSpeed,
                        LiftHeight = PrintParametersSettings.LiftHeight,
                        BottomLiftSpeed = PrintParametersSettings.BottomLiftSpeed,
                        RetractSpeed = PrintParametersSettings.RetractSpeed,
                        BottomLightOffDelay = PrintParametersSettings.BottomLightOffDelay,
                        LightOffDelay = PrintParametersSettings.BottomLightOffDelay,
                        BottomLayerCount = PrintParametersSettings.BottomLayerCount,
                        VolumeMl = PrintParametersSettings.VolumeMl,
                        BottomLiftHeight = PrintParametersSettings.BottomLiftHeight,
                        CostDollars = PrintParametersSettings.CostDollars,
                        WeightG = PrintParametersSettings.WeightG
                    },
                    SlicerInfoSettings =
                    {
                        AntiAliasLevel = SlicerInfoSettings.AntiAliasLevel,
                        MachineName = SlicerInfoSettings.MachineName,
                        //EncryptionMode = SlicerInfoSettings.EncryptionMode,
                        MachineNameSize = SlicerInfoSettings.MachineNameSize,
                    },
                    Thumbnails = Thumbnails,
                };

                //file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(ChituboxZipFile))
            {
                ChituboxZipFile file = new ChituboxZipFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        Filename = Path.GetFileName(FileFullPath),

                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        MachineX = HeaderSettings.BedSizeX,
                        MachineY = HeaderSettings.BedSizeY,
                        MachineZ = HeaderSettings.BedSizeZ,
                        MachineType = MachineName,
                        ProjectType = HeaderSettings.ProjectorType == 0 ? "Normal" : "LCD_mirror",

                        Resin = MaterialName,
                        Price = MaterialCost,
                        Weight = PrintParametersSettings.WeightG,
                        Volume = UsedMaterial,
                        Mirror = (byte)  (HeaderSettings.ProjectorType == 0 ? 0 : 1),


                        BottomLiftHeight = PrintParametersSettings.BottomLiftHeight,
                        LiftHeight = PrintParametersSettings.LiftHeight,
                        BottomLiftSpeed = PrintParametersSettings.BottomLiftSpeed,
                        LiftSpeed = PrintParametersSettings.LiftSpeed,
                        RetractSpeed = PrintParametersSettings.RetractSpeed,
                        BottomLayCount = InitialLayerCount,
                        BottomLayerCount = InitialLayerCount,
                        BottomLightOffTime = PrintParametersSettings.BottomLightOffDelay,
                        LightOffTime = PrintParametersSettings.LightOffDelay,
                        BottomLayExposureTime = InitialExposureTime,
                        BottomLayerExposureTime = InitialExposureTime,
                        LayerExposureTime = LayerExposureTime,
                        LayerHeight = LayerHeight,
                        LayerCount = LayerCount,
                        AntiAliasing = ValidateAntiAliasingLevel(),
                        BottomLightPWM = (byte) HeaderSettings.BottomLightPWM,
                        LayerLightPWM = (byte) HeaderSettings.LightPWM,

                        EstimatedPrintTime = PrintTime
                    },
                };

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(PWSFile))
            {
                PWSFile file = new PWSFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        LayerHeight = LayerHeight,
                        LayerExposureTime = LayerExposureTime,
                        LiftHeight = LiftHeight,
                        LiftSpeed = LiftSpeed / 60,
                        RetractSpeed = RetractSpeed / 60,
                        LayerOffTime = HeaderSettings.LayerOffTime,
                        BottomLayersCount = InitialLayerCount,
                        BottomExposureSeconds = InitialExposureTime,
                        Price = MaterialCost,
                        Volume = UsedMaterial,
                        Weight = PrintParametersSettings.WeightG,
                        AntiAliasing = ValidateAntiAliasingLevel()
                    }
                };

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(PHZFile))
            {
                PHZFile file = new PHZFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        Version = 2,
                        BedSizeX = HeaderSettings.BedSizeX,
                        BedSizeY = HeaderSettings.BedSizeY,
                        BedSizeZ = HeaderSettings.BedSizeZ,
                        OverallHeightMilimeter = TotalHeight,
                        BottomExposureSeconds = InitialExposureTime,
                        BottomLayersCount = InitialLayerCount,
                        BottomLightPWM = HeaderSettings.BottomLightPWM,
                        LayerCount = LayerCount,
                        LayerExposureSeconds = LayerExposureTime,
                        LayerHeightMilimeter = LayerHeight,
                        LayerOffTime = HeaderSettings.LayerOffTime,
                        LightPWM = HeaderSettings.LightPWM,
                        PrintTime = HeaderSettings.PrintTime,
                        ProjectorType = HeaderSettings.ProjectorType,
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        BottomLayerCount = InitialLayerCount,
                        BottomLiftHeight = PrintParametersSettings.BottomLiftHeight,
                        BottomLiftSpeed = PrintParametersSettings.BottomLiftSpeed,
                        BottomLightOffDelay = PrintParametersSettings.BottomLightOffDelay,
                        CostDollars = MaterialCost,
                        LiftHeight = PrintParametersSettings.LiftHeight,
                        LiftSpeed = PrintParametersSettings.LiftSpeed,
                        RetractSpeed = PrintParametersSettings.RetractSpeed,
                        VolumeMl = UsedMaterial,
                        AntiAliasLevelInfo = ValidateAntiAliasingLevel(),
                        WeightG = PrintParametersSettings.WeightG,
                        MachineName = MachineName,
                        MachineNameSize = (uint)MachineName.Length
                    }
                };

                

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(ZCodexFile))
            {
                TimeSpan ts = new TimeSpan(0, 0, (int)PrintTime);
                ZCodexFile file = new ZCodexFile
                {
                    ResinMetadataSettings = new ZCodexFile.ResinMetadata
                    {
                        MaterialId = 2,
                        Material = MaterialName,
                        AdditionalSupportLayerTime = 0,
                        BottomLayersNumber = InitialLayerCount,
                        BottomLayersTime = (uint)(InitialExposureTime * 1000),
                        LayerTime = (uint)(LayerExposureTime * 1000),
                        DisableSettingsChanges = false,
                        LayerThickness = LayerHeight,
                        PrintTime = (uint)PrintTime,
                        TotalLayersCount = LayerCount,
                        TotalMaterialVolumeUsed = UsedMaterial,
                        TotalMaterialWeightUsed = UsedMaterial,
                    },
                    UserSettings = new ZCodexFile.UserSettingsdata
                    {
                        Printer = MachineName,
                        BottomLayersCount = InitialLayerCount,
                        PrintTime = $"{ts.Hours}h {ts.Minutes}m",
                        LayerExposureTime = (uint)(LayerExposureTime * 1000),
                        BottomLayerExposureTime = (uint)(InitialExposureTime * 1000),
                        MaterialId = 2,
                        LayerThickness = $"{LayerHeight} mm",
                        AntiAliasing = (byte)(ValidateAntiAliasingLevel() > 1 ? 1 : 0),
                        CrossSupportEnabled = 1,
                        ExposureOffTime = (uint) HeaderSettings.LayerOffTime,
                        HollowEnabled = 0,
                        HollowThickness = 0,
                        InfillDensity = 0,
                        IsAdvanced = 0,
                        MaterialType = MaterialName,
                        MaterialVolume = UsedMaterial,
                        MaxLayer = LayerCount - 1,
                        ModelLiftEnabled = 0,
                        ModelLiftHeight = 0,
                        RaftEnabled = 0,
                        RaftHeight = 0,
                        RaftOffset = 0,
                        SupportAdditionalExposureEnabled = 0,
                        SupportAdditionalExposureTime = 0,
                        XCorrection = 0,
                        YCorrection = 0,
                        ZLiftDistance = PrintParametersSettings.LiftHeight,
                        ZLiftFeedRate = PrintParametersSettings.LiftSpeed,
                        ZLiftRetractRate = PrintParametersSettings.RetractSpeed,
                    },
                    ZCodeMetadataSettings = new ZCodexFile.ZCodeMetadata
                    {
                        PrintTime = (uint)PrintTime,
                        PrinterName = MachineName,
                        Materials = new List<ZCodexFile.ZCodeMetadata.MaterialsData>
                        {
                            new ZCodexFile.ZCodeMetadata.MaterialsData
                            {
                                Name = MaterialName,
                                ExtruderType = "MAIN",
                                Id = 0,
                                Usage = 0,
                                Temperature = 0
                            }
                        },
                    },
                    LayerManager = LayerManager
                };

                float usedMaterial = UsedMaterial / LayerCount;
                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    file.ResinMetadataSettings.Layers.Add(new ZCodexFile.ResinMetadata.LayerData
                    {
                        Layer = layerIndex,
                        UsedMaterialVolume = usedMaterial
                    });
                }

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);
                return true;
            }

            if (to == typeof(CWSFile))
            {
                CWSFile defaultFormat = (CWSFile)FindByType(typeof(CWSFile));
                CWSFile file = new CWSFile { LayerManager = LayerManager };

                file.SliceSettings.Xppm = file.OutputSettings.PixPermmX = (float)Math.Round(ResolutionX / HeaderSettings.BedSizeX, 3);
                file.SliceSettings.Yppm = file.OutputSettings.PixPermmY = (float)Math.Round(ResolutionY / HeaderSettings.BedSizeY, 3);
                file.SliceSettings.Xres = file.OutputSettings.XResolution = (ushort)ResolutionX;
                file.SliceSettings.Yres = file.OutputSettings.YResolution = (ushort)ResolutionY;
                file.SliceSettings.Thickness = file.OutputSettings.LayerThickness = LayerHeight;
                file.SliceSettings.LayersNum = file.OutputSettings.LayersNum = LayerCount;
                file.SliceSettings.HeadLayersNum = file.OutputSettings.NumberBottomLayers = InitialLayerCount;
                file.SliceSettings.LayersExpoMs = file.OutputSettings.LayerTime = (uint)LayerExposureTime * 1000;
                file.SliceSettings.HeadLayersExpoMs = file.OutputSettings.BottomLayersTime = (uint)InitialExposureTime * 1000;
                file.SliceSettings.WaitBeforeExpoMs = (uint)(PrintParametersSettings.LightOffDelay * 1000);
                file.SliceSettings.LiftDistance = file.OutputSettings.LiftDistance = LiftHeight;
                file.SliceSettings.LiftUpSpeed = file.OutputSettings.ZLiftFeedRate = LiftSpeed;
                file.SliceSettings.LiftDownSpeed = file.OutputSettings.ZLiftRetractRate = RetractSpeed;
                file.SliceSettings.LiftWhenFinished = defaultFormat.SliceSettings.LiftWhenFinished;

                file.OutputSettings.BlankingLayerTime = (uint) (PrintParametersSettings.LightOffDelay * 1000);
                //file.OutputSettings.RenderOutlines = false;
                //file.OutputSettings.OutlineWidthInset = 0;
                //file.OutputSettings.OutlineWidthOutset = 0;
                file.OutputSettings.RenderOutlines = false;
                //file.OutputSettings.TiltValue = 0;
                //file.OutputSettings.UseMainliftGCodeTab = false;
                //file.OutputSettings.AntiAliasing = 0;
                //file.OutputSettings.AntiAliasingValue = 0;
                file.OutputSettings.FlipX = HeaderSettings.ProjectorType != 0;
                file.OutputSettings.FlipY = file.OutputSettings.FlipX;
                file.OutputSettings.AntiAliasingValue = ValidateAntiAliasingLevel();
                file.OutputSettings.AntiAliasing = file.OutputSettings.AntiAliasingValue > 1;

                file.Encode(fileFullPath, progress);

                return true;
            }

            /*if (to == typeof(UVJFile))
            {
                UVJFile file = new UVJFile
                {
                    LayerManager = LayerManager,
                    JsonSettings = new UVJFile.Settings
                    {
                        Properties = new UVJFile.Properties
                        {
                            Size = new UVJFile.Size
                            {
                                X = (ushort)ResolutionX,
                                Y = (ushort)ResolutionY,
                                Millimeter = new UVJFile.Millimeter
                                {
                                    X = HeaderSettings.BedSizeX,
                                    Y = HeaderSettings.BedSizeY,
                                },
                                LayerHeight = LayerHeight,
                                Layers = LayerCount
                            },
                            Bottom = new UVJFile.Bottom
                            {
                                LiftHeight = PrintParametersSettings.BottomLiftHeight,
                                LiftSpeed = PrintParametersSettings.BottomLiftSpeed,
                                LightOnTime = InitialExposureTime,
                                LightOffTime = PrintParametersSettings.BottomLightOffDelay,
                                LightPWM = (byte) HeaderSettings.BottomLightPWM,
                                RetractSpeed = PrintParametersSettings.RetractSpeed,
                                Count = InitialLayerCount
                                //RetractHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.JsonSettings.Properties.Bottom.RetractHeight),
                            },
                            Exposure = new UVJFile.Exposure
                            {
                                LiftHeight = PrintParametersSettings.LiftHeight,
                                LiftSpeed = PrintParametersSettings.LiftSpeed,
                                LightOnTime = LayerExposureTime,
                                LightOffTime = PrintParametersSettings.LightOffDelay,
                                LightPWM = (byte) HeaderSettings.LightPWM,
                                RetractSpeed = PrintParametersSettings.RetractSpeed,
                            },
                            AntiAliasLevel = ValidateAntiAliasingLevel()
                        }
                    }
                };

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }*/

            return false;
        }
        #endregion
    }
}
