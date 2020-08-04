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
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class MakerbaseFile : FileFormat
    {
        #region Constants
        private const uint MAGIC_CBDDLP = 0x12FD0019;
        private const uint MAGIC_CBT = 0x12FD0086;
        private const ushort REPEATRGB15MASK = 0x20;

        private const byte RLE8EncodingLimit = 0x7d; // 125;
        private const ushort RLE16EncodingLimit = 0xFFF;
        #endregion

        #region Sub Classes

        #region Header
        public class Header
        {
            public const string TagValue = "MKSDLP";
            //[FieldOrder(0)]  public uint Offset1     { get; set; }

            /// <summary>
            /// Gets the file tag = MKSDLP
            /// </summary>
            [FieldOrder(0)] [FieldOffset(4)] [FieldLength(6)] public string Tag { get; set; } = TagValue;
            [FieldOrder(1)] [FieldOffset(1)] public ushort MaxSize { get; set; }
            [FieldOrder(2)] public ushort ResolutionX { get; set; }
            [FieldOrder(3)] public ushort ResolutionY { get; set; }

            
        }
        #endregion

        #endregion

        #region Properties

        public Header HeaderSettings { get; protected internal set; } = new Header();
        private int temp = 0;
        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("mdlp", "Makerbase MDLP Files"),
            new FileExtension("gr1", "GR1 Workshop GR1 Files"),
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
            PrintParameterModifier.RetractSpeed,

            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,
        };

        public override byte ThumbnailsCount { get; } = 0;

        public override Size[] ThumbnailsOriginalSize { get; } = {new Size(400, 300), new Size(200, 125)};

        public override uint ResolutionX
        {
            get => 0;
            set => temp = 0;
        }

        public override uint ResolutionY
        {
            get => 0;
            set => temp = 0;
        }

        public override byte AntiAliasing => 1;

        public override float LayerHeight
        {
            get => 0;
            set => temp = 0;
        }

        public override uint LayerCount
        {
            set
            {
                temp = 0;
                /*HeaderSettings.LayerCount = LayerCount;
                HeaderSettings.OverallHeightMilimeter = TotalHeight;*/
            }
        }

        public override ushort InitialLayerCount => 0;

        public override float InitialExposureTime => 0;

        public override float LayerExposureTime => 0;
        public override float LiftHeight => 0;
        public override float LiftSpeed => 0;
        public override float RetractSpeed => 0;

        public override float PrintTime => 0;

        public override float UsedMaterial => 0;

        public override float MaterialCost => 0;

        public override string MaterialName => "Unknown";
        public override string MachineName => null;
        
        public override object[] Configs => new[] { (object)HeaderSettings };

        #endregion

        #region Constructors
        public MakerbaseFile()
        {
        }
        #endregion

        #region Methods
        public override void Encode(string fileFullPath, OperationProgress progress = null)
        {
            base.Encode(fileFullPath, progress);
            
            uint currentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
            using (var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write))
            {

                outputFile.Seek((int) currentOffset, SeekOrigin.Begin);


                
            }

            AfterEncode();

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine("-End-");
        }

        

        public override void Decode(string fileFullPath, OperationProgress progress = null)
        {
            base.Decode(fileFullPath, progress);

            using (var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
            {
                //HeaderSettings = Helpers.ByteToType<CbddlpFile.Header>(InputFile);
                //HeaderSettings = Helpers.Serializer.Deserialize<Header>(InputFile.ReadBytes(Helpers.Serializer.SizeOf(typeof(Header))));
                HeaderSettings = Helpers.Deserialize<Header>(inputFile);
                if (HeaderSettings.Tag != Header.TagValue)
                {
                    throw new FileLoadException("Not a valid Makerfile file!", fileFullPath);
                }


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
