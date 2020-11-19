/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Newtonsoft.Json;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class UVJFile : FileFormat
    {
        #region Constants

        private const string FileConfigName = "config.json";
        private const string FolderImageName = "slice";
        private const string FolderPreviewName = "preview";
        private const string FilePreviewHugeName = "preview/huge.png";
        private const string FilePreviewTinyName = "preview/tiny.png";
        #endregion

        #region Sub Classes

        public class Millimeter
        {
            public float X { get; set; }
            public float Y { get; set; }
        }

        public class Size
        {
            public ushort X { get; set; }
            public ushort Y { get; set; }

            public Millimeter Millimeter { get; set; } = new Millimeter();

            public uint Layers { get; set; }
            public float LayerHeight { get; set; }

            public override string ToString()
            {
                return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Millimeter)}: {Millimeter}, {nameof(Layers)}: {Layers}, {nameof(LayerHeight)}: {LayerHeight}";
            }
        }

        public class Exposure
        {
            public float LightOnTime { get; set; }
            public float LightOffTime { get; set; }
            public byte LightPWM { get; set; } = 255;
            public float LiftHeight { get; set; } = 5;
            public float LiftSpeed { get; set; } = 100;
            public float RetractHeight { get; set; }
            public float RetractSpeed { get; set; } = 100;

            public override string ToString()
            {
                return $"{nameof(LightOnTime)}: {LightOnTime}, {nameof(LightOffTime)}: {LightOffTime}, {nameof(LightPWM)}: {LightPWM}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractHeight)}: {RetractHeight}, {nameof(RetractSpeed)}: {RetractSpeed}";
            }
        }

        public class Bottom
        {
            public float LightOnTime { get; set; }
            public float LightOffTime { get; set; }
            public byte LightPWM { get; set; } = 255;
            public float LiftHeight { get; set; } = 5;
            public float LiftSpeed { get; set; } = 100;
            public float RetractHeight { get; set; }
            public float RetractSpeed { get; set; } = 100;
            public ushort Count { get; set; }

            public override string ToString()
            {
                return $"{nameof(LightOnTime)}: {LightOnTime}, {nameof(LightOffTime)}: {LightOffTime}, {nameof(LightPWM)}: {LightPWM}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractHeight)}: {RetractHeight}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(Count)}: {Count}";
            }
        }

        public class LayerData
        {
            public float Z { get; set; }
            public Exposure Exposure { get; set; }

            public override string ToString()
            {
                return $"{nameof(Z)}: {Z}, {nameof(Exposure)}: {Exposure}";
            }
        }

        public class Properties
        {
            public Size Size { get; set; } = new Size();
            public Exposure Exposure { get; set; } = new Exposure();
            public Bottom Bottom { get; set; } = new Bottom();
            public byte AntiAliasLevel { get; set; } = 1;

            public override string ToString()
            {
                return $"{nameof(Size)}: {Size}, {nameof(Exposure)}: {Exposure}, {nameof(Bottom)}: {Bottom}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}";
            }
        }

        public class Settings
        {
            public Properties Properties { get; set; } = new Properties();
            public List<LayerData> Layers { get; set; } = new List<LayerData>();

            public override string ToString()
            {
                return $"{nameof(Properties)}: {Properties}, {nameof(Layers)}: {Layers.Count}";
            }
        }

        #endregion

        #region Properties
        public Settings JsonSettings { get; set; } = new Settings();

        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("uvj", "UVJ")
        };

        public override Type[] ConvertToFormats { get; } =
        {
            typeof(ChituboxFile),
            typeof(ChituboxZipFile),
            typeof(PHZFile),
            typeof(PhotonWorkshopFile),
            typeof(ZCodexFile),
            typeof(CWSFile),
            //typeof(LGSFile)
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
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

        public override byte ThumbnailsCount { get; } = 2;

        public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } = {new System.Drawing.Size(400, 400), new System.Drawing.Size(800, 480) };

        public override uint ResolutionX
        {
            get => JsonSettings.Properties.Size.X;
            set
            {
                JsonSettings.Properties.Size.X = (ushort) value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => JsonSettings.Properties.Size.Y;
            set
            {
                JsonSettings.Properties.Size.Y = (ushort) value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayWidth
        {
            get => JsonSettings.Properties.Size.Millimeter.X;
            set
            {
                JsonSettings.Properties.Size.Millimeter.X = value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayHeight
        {
            get => JsonSettings.Properties.Size.Millimeter.Y;
            set
            {
                JsonSettings.Properties.Size.Millimeter.Y = value;
                RaisePropertyChanged();
            }
        }

        public override byte AntiAliasing => JsonSettings.Properties.AntiAliasLevel;

        public override bool SupportPerLayerSettings => true;

        public override float LayerHeight
        {
            get => JsonSettings.Properties.Size.LayerHeight;
            set
            {
                JsonSettings.Properties.Size.LayerHeight = value;
                RaisePropertyChanged();
            }
        }

        public override uint LayerCount
        {
            set
            {
                JsonSettings.Properties.Size.Layers = LayerCount;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(NormalLayerCount));
            }
        }

        public override ushort BottomLayerCount
        {
            get => JsonSettings.Properties.Bottom.Count;
            set
            {
                JsonSettings.Properties.Bottom.Count = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomExposureTime
        {
            get => JsonSettings.Properties.Bottom.LightOnTime;
            set
            {
                JsonSettings.Properties.Bottom.LightOnTime = value;
                RaisePropertyChanged();
            }
        }

        public override float ExposureTime
        {
            get => JsonSettings.Properties.Exposure.LightOnTime;
            set
            {
                JsonSettings.Properties.Exposure.LightOnTime = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomLayerOffTime
        {
            get => JsonSettings.Properties.Bottom.LightOffTime;
            set
            {
                JsonSettings.Properties.Bottom.LightOffTime = value;
                RaisePropertyChanged();
            }
        }

        public override float LayerOffTime
        {
            get => JsonSettings.Properties.Exposure.LightOffTime;
            set
            {
                JsonSettings.Properties.Exposure.LightOffTime = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomLiftHeight
        {
            get => JsonSettings.Properties.Bottom.LiftHeight;
            set
            {
                JsonSettings.Properties.Bottom.LiftHeight = value;
                RaisePropertyChanged();
            }
        }

        public override float LiftHeight
        {
            get => JsonSettings.Properties.Exposure.LiftHeight;
            set
            {
                JsonSettings.Properties.Exposure.LiftHeight = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomLiftSpeed
        {
            get => JsonSettings.Properties.Bottom.LiftSpeed;
            set
            {
                JsonSettings.Properties.Bottom.LiftSpeed = value;
                RaisePropertyChanged();
            }
        }

        public override float LiftSpeed
        {
            get => JsonSettings.Properties.Exposure.LiftSpeed;
            set
            {
                JsonSettings.Properties.Exposure.LiftSpeed = value;
                RaisePropertyChanged();
            }
        }

        public override float RetractSpeed
        {
            get => JsonSettings.Properties.Exposure.RetractSpeed;
            set
            {
                JsonSettings.Properties.Exposure.RetractSpeed = value;
                RaisePropertyChanged();
            }
        }

        public override byte BottomLightPWM
        {
            get => JsonSettings.Properties.Bottom.LightPWM;
            set
            {
                JsonSettings.Properties.Bottom.LightPWM = value;
                RaisePropertyChanged();
            }
        }

        public override byte LightPWM
        {
            get => JsonSettings.Properties.Exposure.LightPWM;
            set
            {
                JsonSettings.Properties.Exposure.LightPWM = value;
                RaisePropertyChanged();
            }
        }

        public override object[] Configs => new[] {(object) JsonSettings.Properties.Size, JsonSettings.Properties.Size.Millimeter, JsonSettings.Properties.Bottom, JsonSettings.Properties.Exposure};
        #endregion

        #region Methods

        public override void Clear()
        {
            base.Clear();
            JsonSettings.Layers = new List<LayerData>();
        }

        public override void Encode(string fileFullPath, OperationProgress progress = null)
        {
            base.Encode(fileFullPath, progress);

            // Redo layer data
            JsonSettings.Layers.Clear();
            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                JsonSettings.Layers.Add(new LayerData
                {
                    Z = layer.PositionZ,
                    Exposure = new Exposure
                    {
                        LiftHeight = layer.LiftHeight,
                        LiftSpeed = layer.LiftSpeed,
                        RetractHeight = layer.LiftHeight+1,
                        RetractSpeed = layer.RetractSpeed,
                        LightOffTime = layer.LayerOffTime,
                        LightOnTime = layer.ExposureTime,
                        LightPWM = layer.LightPWM
                    }
                });
            }

            using (ZipArchive outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create))
            {
                outputFile.PutFileContent(FileConfigName, JsonConvert.SerializeObject(JsonSettings), ZipArchiveMode.Create);

                if (CreatedThumbnailsCount > 0)
                {
                    using (Stream stream = outputFile.CreateEntry(FilePreviewTinyName).Open())
                    {
                        using (var vec = new VectorOfByte())
                        {
                            CvInvoke.Imencode(".png", Thumbnails[0], vec);
                            stream.WriteBytes(vec.ToArray());
                            stream.Close();
                        }
                    }
                }

                if (CreatedThumbnailsCount > 1)
                {
                    using (Stream stream = outputFile.CreateEntry(FilePreviewHugeName).Open())
                    {
                        using (var vec = new VectorOfByte())
                        {
                            CvInvoke.Imencode(".png", Thumbnails[1], vec);
                            stream.WriteBytes(vec.ToArray());
                            stream.Close();
                        }
                    }
                }

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();

                    Layer layer = this[layerIndex];

                    var layerimagePath = $"{FolderImageName}/{layerIndex:D8}.png";
                    outputFile.PutFileContent(layerimagePath, layer.CompressedBytes, ZipArchiveMode.Create);
                    
                    progress++;
                }
            }
            AfterEncode();
        }

        public override void Decode(string fileFullPath, OperationProgress progress = null)
        {
            base.Decode(fileFullPath, progress);
            if(progress is null) progress = new OperationProgress();
            progress.Reset(OperationProgress.StatusGatherLayers, LayerCount);

            FileFullPath = fileFullPath;
            using (var inputFile = ZipFile.Open(FileFullPath, ZipArchiveMode.Read))
            {
                var entry = inputFile.GetEntry(FileConfigName);
                if (ReferenceEquals(entry, null))
                {
                    Clear();
                    throw new FileLoadException($"{FileConfigName} not found", fileFullPath);
                }

                JsonSettings = Helpers.JsonDeserializeObject<Settings>(entry.Open());
                
                LayerManager = new LayerManager(JsonSettings.Properties.Size.Layers, this);

                entry = inputFile.GetEntry(FilePreviewTinyName);
                if (!ReferenceEquals(entry, null))
                {
                    using (Stream stream = entry.Open())
                    {
                        Mat image = new Mat();
                        CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, image);
                        Thumbnails[0] = image;
                        stream.Close();
                    }
                }

                entry = inputFile.GetEntry(FilePreviewHugeName);
                if (!ReferenceEquals(entry, null))
                {
                    using (Stream stream = entry.Open())
                    {
                        Mat image = new Mat();
                        CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, image);
                        Thumbnails[1] = image;
                        stream.Close();
                    }
                }

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    entry = inputFile.GetEntry($"{FolderImageName}/{layerIndex:D8}.png");
                    if (ReferenceEquals(entry, null)) continue;

                    this[layerIndex] = new Layer(layerIndex, entry.Open(), LayerManager)
                    {
                        PositionZ = JsonSettings.Layers.Count >= layerIndex ? JsonSettings.Layers[(int) layerIndex].Z : GetHeightFromLayer(layerIndex),
                        LiftHeight = JsonSettings.Layers.Count >= layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LiftHeight : GetInitialLayerValueOrNormal(layerIndex, BottomLiftHeight, LiftHeight),
                        LiftSpeed = JsonSettings.Layers.Count >= layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LiftSpeed : GetInitialLayerValueOrNormal(layerIndex, BottomLiftSpeed, LiftSpeed),
                        RetractSpeed = JsonSettings.Layers.Count >= layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.RetractSpeed : RetractSpeed,
                        LayerOffTime = JsonSettings.Layers.Count >= layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LightOffTime : GetInitialLayerValueOrNormal(layerIndex, BottomLayerOffTime, LayerOffTime),
                        ExposureTime = JsonSettings.Layers.Count >= layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LightOnTime : GetInitialLayerValueOrNormal(layerIndex, BottomExposureTime, ExposureTime),
                        LightPWM = JsonSettings.Layers.Count >= layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LightPWM : GetInitialLayerValueOrNormal(layerIndex, BottomLightPWM, LightPWM),
                    };
                }
                
                progress.ProcessedItems++;
            }

            LayerManager.GetBoundingRectangle(progress);
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

            using (var outputFile = ZipFile.Open(FileFullPath, ZipArchiveMode.Update))
            {
                outputFile.PutFileContent(FileConfigName, JsonConvert.SerializeObject(JsonSettings), ZipArchiveMode.Update);
            }

            //Decode(FileFullPath, progress);
        }

        public override bool Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
            if (to == typeof(ChituboxFile))
            {
                ChituboxFile file = new ChituboxFile
                {

                    LayerManager = LayerManager,
                    HeaderSettings
                        =
                        {
                            BedSizeX = DisplayWidth,
                            BedSizeY = DisplayHeight,
                            BedSizeZ = TotalHeight,
                            OverallHeightMilimeter = TotalHeight,
                            BottomExposureSeconds = BottomExposureTime,
                            BottomLayersCount = BottomLayerCount,
                            BottomLightPWM = BottomLightPWM,
                            LayerCount = LayerCount,
                            LayerExposureSeconds = ExposureTime,
                            LayerHeightMilimeter = LayerHeight,
                            LayerOffTime = LayerOffTime,
                            LightPWM = LightPWM,
                            PrintTime = (uint) PrintTimeOrComputed,
                            ProjectorType = 0,
                            ResolutionX = ResolutionX,
                            ResolutionY = ResolutionY,
                            AntiAliasLevel = ValidateAntiAliasingLevel()
                        },
                    PrintParametersSettings =
                    {
                        BottomLayerCount = BottomLayerCount,
                        BottomLiftHeight = BottomLiftHeight,
                        BottomLiftSpeed = BottomLiftSpeed,
                        BottomLightOffDelay = BottomLayerOffTime,
                        CostDollars = MaterialCost,
                        LiftHeight = LiftHeight,
                        LiftSpeed = LiftSpeed,
                        LightOffDelay = LayerOffTime,
                        RetractSpeed = RetractSpeed,
                        VolumeMl = UsedMaterial,
                        WeightG = 0
                    },
                    SlicerInfoSettings = { MachineName = MachineName, MachineNameSize = (uint)MachineName.Length }
                };


                file.SetThumbnails(Thumbnails);
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
                        MachineX = DisplayWidth,
                        MachineY = DisplayHeight,
                        MachineZ = TotalHeight,
                        MachineType = MachineName,
                        ProjectType = "Normal",

                        Resin = MaterialName,
                        Price = MaterialCost,
                        Weight = 0,
                        Volume = UsedMaterial,
                        Mirror = 0,


                        BottomLiftHeight = BottomLiftHeight,
                        LiftHeight = LiftHeight,
                        BottomLiftSpeed = BottomLiftSpeed,
                        LiftSpeed = LiftSpeed,
                        RetractSpeed = RetractSpeed,
                        BottomLayCount = BottomLayerCount,
                        BottomLayerCount = BottomLayerCount,
                        BottomLightOffTime = BottomLayerOffTime,
                        LightOffTime = LayerOffTime,
                        BottomLayExposureTime = BottomExposureTime,
                        BottomLayerExposureTime = BottomExposureTime,
                        LayerExposureTime = ExposureTime,
                        LayerHeight = LayerHeight,
                        LayerCount = LayerCount,
                        AntiAliasing = ValidateAntiAliasingLevel(),
                        BottomLightPWM = BottomLightPWM,
                        LightPWM = LightPWM,

                        EstimatedPrintTime = PrintTime
                    },
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
                        BedSizeX = DisplayWidth,
                        BedSizeY = DisplayHeight,
                        BedSizeZ = TotalHeight,
                        OverallHeightMilimeter = TotalHeight,
                        BottomExposureSeconds = BottomExposureTime,
                        BottomLayersCount = BottomLayerCount,
                        BottomLightPWM = BottomLightPWM,
                        LayerCount = LayerCount,
                        LayerExposureSeconds = ExposureTime,
                        LayerHeightMilimeter = LayerHeight,
                        LayerOffTime = LayerOffTime,
                        LightPWM = LightPWM,
                        PrintTime = (uint) PrintTimeOrComputed,
                        ProjectorType = 0,
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        BottomLayerCount = BottomLayerCount,
                        BottomLiftHeight = BottomLiftHeight,
                        BottomLiftSpeed = BottomLiftSpeed,
                        BottomLightOffDelay = BottomLayerOffTime,
                        CostDollars = MaterialCost,
                        LiftHeight = LiftHeight,
                        LiftSpeed = LiftSpeed,
                        RetractSpeed = RetractSpeed,
                        VolumeMl = UsedMaterial,
                        AntiAliasLevelInfo = ValidateAntiAliasingLevel(),
                        WeightG = 0,
                        MachineName = MachineName,
                        MachineNameSize = (uint)MachineName.Length
                    }
                };

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(PhotonWorkshopFile))
            {
                PhotonWorkshopFile file = new PhotonWorkshopFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        LayerHeight = LayerHeight,
                        LayerExposureTime = ExposureTime,
                        LiftHeight = LiftHeight,
                        LiftSpeed = LiftSpeed / 60,
                        RetractSpeed = RetractSpeed / 60,
                        LayerOffTime = LayerOffTime,
                        BottomLayersCount = BottomLayerCount,
                        BottomExposureSeconds = BottomExposureTime,
                        Price = MaterialCost,
                        Volume = UsedMaterial,
                        Weight = 0,
                        AntiAliasing = ValidateAntiAliasingLevel()
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
                        BottomLayersNumber = BottomLayerCount,
                        BottomLayersTime = (uint)(BottomExposureTime * 1000),
                        LayerTime = (uint)(ExposureTime * 1000),
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
                        BottomLayersCount = BottomLayerCount,
                        PrintTime = $"{ts.Hours}h {ts.Minutes}m",
                        LayerExposureTime = (uint)(ExposureTime * 1000),
                        BottomLayerExposureTime = (uint)(BottomExposureTime * 1000),
                        MaterialId = 2,
                        LayerThickness = $"{LayerHeight} mm",
                        AntiAliasing = (byte)(AntiAliasing > 1 ? 1 : 0),
                        CrossSupportEnabled = 1,
                        ExposureOffTime = (uint)LayerOffTime,
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
                        ZLiftDistance = LiftHeight,
                        ZLiftFeedRate = LiftSpeed,
                        ZLiftRetractRate = RetractSpeed,
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

                file.SliceSettings.Xppm = file.OutputSettings.PixPermmX = (float)Math.Round(ResolutionX / DisplayWidth, 3);
                file.SliceSettings.Yppm = file.OutputSettings.PixPermmY = (float)Math.Round(ResolutionY / DisplayHeight, 3);
                file.ResolutionX = (ushort)ResolutionX;
                file.ResolutionY = (ushort)ResolutionY;
                file.SliceSettings.Thickness = file.OutputSettings.LayerThickness = LayerHeight;
                file.SliceSettings.LayersNum = file.OutputSettings.LayersNum = LayerCount;
                file.SliceSettings.HeadLayersNum = file.OutputSettings.NumberBottomLayers = BottomLayerCount;
                file.SliceSettings.LayersExpoMs = file.OutputSettings.LayerTime = (uint)ExposureTime * 1000;
                file.SliceSettings.HeadLayersExpoMs = file.OutputSettings.BottomLayersTime = (uint)BottomExposureTime * 1000;
                file.SliceSettings.WaitBeforeExpoMs = (uint)(LayerOffTime * 1000);
                file.SliceSettings.LiftDistance = file.OutputSettings.LiftDistance = LiftHeight;
                file.SliceSettings.LiftUpSpeed = file.OutputSettings.ZLiftFeedRate = LiftSpeed;
                file.SliceSettings.LiftDownSpeed = file.OutputSettings.ZLiftRetractRate = RetractSpeed;
                file.SliceSettings.LiftWhenFinished = defaultFormat.SliceSettings.LiftWhenFinished;

                file.OutputSettings.BlankingLayerTime = (uint)(LayerOffTime * 1000);
                //file.OutputSettings.RenderOutlines = false;
                //file.OutputSettings.OutlineWidthInset = 0;
                //file.OutputSettings.OutlineWidthOutset = 0;
                file.OutputSettings.RenderOutlines = false;
                //file.OutputSettings.TiltValue = 0;
                //file.OutputSettings.UseMainliftGCodeTab = false;
                //file.OutputSettings.AntiAliasing = 0;
                //file.OutputSettings.AntiAliasingValue = 0;
                file.OutputSettings.FlipX = false;
                file.OutputSettings.FlipY = file.OutputSettings.FlipX;
                file.OutputSettings.AntiAliasingValue = ValidateAntiAliasingLevel();
                file.OutputSettings.AntiAliasing = file.OutputSettings.AntiAliasingValue > 1;

                /*file.Printer = MachineName.Contains("Bene4 Mono") ||
                               FileFullPath.Contains("bene4_mono")
                    ? CWSFile.PrinterType.BeneMono : CWSFile.PrinterType.Elfin;*/

                file.Encode(fileFullPath, progress);

                return true;
            }

            return false;
        }
        
        #endregion
    }
}
