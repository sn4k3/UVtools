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
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    public class SL1File : FileFormat, IEquatable<SL1File>
    {
        #region Sub Classes 

        #region Printer
        public class Printer
        {
            #region Printer
            public string PrinterSettingsId { get; set; }
            public string PrinterTechnology { get; set; }
            public string PrinterModel { get; set; }
            public string PrinterVariant { get; set; }
            public string PrinterVendor { get; set; }
            public string DefaultSlaMaterialProfile { get; set; }
            public string DefaultSlaPrintProfile { get; set; }
            public string PrinterNotes { get; set; }
            public string Thumbnails { get; set; }
            #endregion

            #region Size and Coordinates
            public string BedCustomModel { get; set; }
            public string BedCustomTexture { get; set; }
            public string BedShape { get; set; }
            public ushort MaxPrintHeight { get; set; }
            #endregion

            #region Display
            public float DisplayWidth { get; set; }
            public float DisplayHeight { get; set; }
            public ushort DisplayPixelsX { get; set; }
            public ushort DisplayPixelsY { get; set; }
            public string DisplayOrientation { get; set; }
            public bool DisplayMirrorX { get; set; }
            public bool DisplayMirrorY { get; set; }
            #endregion

            #region Tilt
            public byte FastTiltTime { get; set; }
            public byte SlowTiltTime { get; set; }
            public byte AreaFill { get; set; }
            #endregion

            #region Corrections
            public string RelativeCorrection { get; set; }
            public byte AbsoluteCorrection { get; set; }
            public float ElefantFootCompensation { get; set; }
            public float ElefantFootMinWidth { get; set; }
            public byte GammaCorrection { get; set; }

            #endregion

            #region Exposure

            public byte MinExposureTime { get; set; }
            public byte MaxExposureTime { get; set; }
            public byte MinInitialExposureTime { get; set; }
            public ushort MaxInitialExposureTime { get; set; }

            #endregion

            #region Overrides
            public override string ToString()
            {
                return $"{nameof(PrinterSettingsId)}: {PrinterSettingsId}, {nameof(PrinterTechnology)}: {PrinterTechnology}, {nameof(PrinterModel)}: {PrinterModel}, {nameof(PrinterVariant)}: {PrinterVariant}, {nameof(PrinterVendor)}: {PrinterVendor}, {nameof(DefaultSlaMaterialProfile)}: {DefaultSlaMaterialProfile}, {nameof(DefaultSlaPrintProfile)}: {DefaultSlaPrintProfile}, {nameof(PrinterNotes)}: {PrinterNotes}, {nameof(Thumbnails)}: {Thumbnails}, {nameof(BedCustomModel)}: {BedCustomModel}, {nameof(BedCustomTexture)}: {BedCustomTexture}, {nameof(BedShape)}: {BedShape}, {nameof(MaxPrintHeight)}: {MaxPrintHeight}, {nameof(DisplayWidth)}: {DisplayWidth}, {nameof(DisplayHeight)}: {DisplayHeight}, {nameof(DisplayPixelsX)}: {DisplayPixelsX}, {nameof(DisplayPixelsY)}: {DisplayPixelsY}, {nameof(DisplayOrientation)}: {DisplayOrientation}, {nameof(DisplayMirrorX)}: {DisplayMirrorX}, {nameof(DisplayMirrorY)}: {DisplayMirrorY}, {nameof(FastTiltTime)}: {FastTiltTime}, {nameof(SlowTiltTime)}: {SlowTiltTime}, {nameof(AreaFill)}: {AreaFill}, {nameof(RelativeCorrection)}: {RelativeCorrection}, {nameof(AbsoluteCorrection)}: {AbsoluteCorrection}, {nameof(ElefantFootCompensation)}: {ElefantFootCompensation}, {nameof(ElefantFootMinWidth)}: {ElefantFootMinWidth}, {nameof(GammaCorrection)}: {GammaCorrection}, {nameof(MinExposureTime)}: {MinExposureTime}, {nameof(MaxExposureTime)}: {MaxExposureTime}, {nameof(MinInitialExposureTime)}: {MinInitialExposureTime}, {nameof(MaxInitialExposureTime)}: {MaxInitialExposureTime}";
            }
            #endregion
        }
        #endregion

        #region Material
        public class Material
        {
            #region Material
            public string MaterialVendor { get; set; }
            public string MaterialType { get; set; }
            public string SlaMaterialSettingsId { get; set; }
            public float BottleCost { get; set; }
            public ushort BottleVolume { get; set; }
            public float BottleWeight { get; set; }
            public float MaterialDensity { get; set; }
            public string MaterialNotes { get; set; }

            #endregion

            #region Layers

            public float InitialLayerHeight { get; set; }
            #endregion

            #region Exposure

            public byte ExposureTime { get; set; }
            public ushort InitialExposureTime { get; set; }
            #endregion

            #region Corrections
            public string MaterialCorrection { get; set; }

            #endregion

            #region Dependencies

            public string CompatiblePrintersConditionCummulative { get; set; }
            public string CompatiblePrintsConditionCummulative { get; set; }

            #endregion

            #region Overrides
            public override string ToString()
            {
                return $"{nameof(MaterialVendor)}: {MaterialVendor}, {nameof(MaterialType)}: {MaterialType}, {nameof(SlaMaterialSettingsId)}: {SlaMaterialSettingsId}, {nameof(BottleCost)}: {BottleCost}, {nameof(BottleVolume)}: {BottleVolume}, {nameof(BottleWeight)}: {BottleWeight}, {nameof(MaterialDensity)}: {MaterialDensity}, {nameof(MaterialNotes)}: {MaterialNotes}, {nameof(InitialLayerHeight)}: {InitialLayerHeight}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(InitialExposureTime)}: {InitialExposureTime}, {nameof(MaterialCorrection)}: {MaterialCorrection}, {nameof(CompatiblePrintersConditionCummulative)}: {CompatiblePrintersConditionCummulative}, {nameof(CompatiblePrintsConditionCummulative)}: {CompatiblePrintsConditionCummulative}";
            }
            #endregion
        }
        #endregion

        #region Print

        public class Print
        {
            #region Print
            public string SlaPrintSettingsId { get; set; }
            #endregion

            #region Layers

            public float LayerHeight { get; set; }
            public byte FadedLayers { get; set; }
            #endregion

            #region Supports
            public bool SupportsEnable { get; set; }


            public float SupportHeadFrontDiameter { get; set; }
            public float SupportHeadPenetration { get; set; }
            public float SupportHeadWidth { get; set; }

            public byte SupportPillarWideningFactor { set; get; }
            public float SupportPillarDiameter { get; set; }
            public float SupportMaxBridgesOnPillar { get; set; }
            public string SupportPillarConnectionMode { get; set; }
            public bool SupportBuildplateOnly { get; set; }
            public float SupportBaseDiameter { get; set; }
            public float SupportBaseHeight { get; set; }
            public float SupportBaseSafetyDistance { get; set; }
            public bool PadAroundObject { get; set; }
            public float SupportObjectElevation { get; set; }


            public ushort SupportCriticalAngle { get; set; }
            public float SupportMaxBridgeLength { get; set; }
            public float SupportMaxPillarLinkDistance { get; set; }


            public byte SupportPointsDensityRelative { get; set; }
            public float SupportPointsMinimalDistance { get; set; }

            #endregion

            #region Pad

            public bool PadEnable { set; get; }
            public float PadWallThickness { set; get; }
            public float PadWallHeight { set; get; }
            public float PadBrimSize { set; get; }
            public float PadMaxMergeDistance { set; get; }
            public float PadWallSlope { set; get; }
            //public float PadAroundObject { set; get; }
            public bool PadAroundObjectEverywhere { set; get; }
            public float PadObjectGap { set; get; }
            public float PadObjectConnectorStride { set; get; }
            public float PadObjectConnectorWidth { set; get; }
            public float PadObjectConnectorPenetration { set; get; }
            #endregion

            #region Hollowing
            public bool HollowingEnable { set; get; }
            public float HollowingMinThickness { set; get; }
            public float HollowingQuality { set; get; }
            public float HollowingClosingDistance { set; get; }
            #endregion

            #region Advanced
            public float SliceClosingRadius { set; get; }
            #endregion

            #region Output
            public string OutputFilenameFormat { set; get; }
            #endregion

            #region Dependencies
            public string CompatiblePrintsCondition { set; get; }
            #endregion

            #region Overrides
            public override string ToString()
            {
                return $"{nameof(SlaPrintSettingsId)}: {SlaPrintSettingsId}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(FadedLayers)}: {FadedLayers}, {nameof(SupportsEnable)}: {SupportsEnable}, {nameof(SupportHeadFrontDiameter)}: {SupportHeadFrontDiameter}, {nameof(SupportHeadPenetration)}: {SupportHeadPenetration}, {nameof(SupportHeadWidth)}: {SupportHeadWidth}, {nameof(SupportPillarWideningFactor)}: {SupportPillarWideningFactor}, {nameof(SupportPillarDiameter)}: {SupportPillarDiameter}, {nameof(SupportMaxBridgesOnPillar)}: {SupportMaxBridgesOnPillar}, {nameof(SupportPillarConnectionMode)}: {SupportPillarConnectionMode}, {nameof(SupportBuildplateOnly)}: {SupportBuildplateOnly}, {nameof(SupportBaseDiameter)}: {SupportBaseDiameter}, {nameof(SupportBaseHeight)}: {SupportBaseHeight}, {nameof(SupportBaseSafetyDistance)}: {SupportBaseSafetyDistance}, {nameof(PadAroundObject)}: {PadAroundObject}, {nameof(SupportObjectElevation)}: {SupportObjectElevation}, {nameof(SupportCriticalAngle)}: {SupportCriticalAngle}, {nameof(SupportMaxBridgeLength)}: {SupportMaxBridgeLength}, {nameof(SupportMaxPillarLinkDistance)}: {SupportMaxPillarLinkDistance}, {nameof(SupportPointsDensityRelative)}: {SupportPointsDensityRelative}, {nameof(SupportPointsMinimalDistance)}: {SupportPointsMinimalDistance}, {nameof(PadEnable)}: {PadEnable}, {nameof(PadWallThickness)}: {PadWallThickness}, {nameof(PadWallHeight)}: {PadWallHeight}, {nameof(PadBrimSize)}: {PadBrimSize}, {nameof(PadMaxMergeDistance)}: {PadMaxMergeDistance}, {nameof(PadWallSlope)}: {PadWallSlope}, {nameof(PadAroundObjectEverywhere)}: {PadAroundObjectEverywhere}, {nameof(PadObjectGap)}: {PadObjectGap}, {nameof(PadObjectConnectorStride)}: {PadObjectConnectorStride}, {nameof(PadObjectConnectorWidth)}: {PadObjectConnectorWidth}, {nameof(PadObjectConnectorPenetration)}: {PadObjectConnectorPenetration}, {nameof(HollowingEnable)}: {HollowingEnable}, {nameof(HollowingMinThickness)}: {HollowingMinThickness}, {nameof(HollowingQuality)}: {HollowingQuality}, {nameof(HollowingClosingDistance)}: {HollowingClosingDistance}, {nameof(SliceClosingRadius)}: {SliceClosingRadius}, {nameof(OutputFilenameFormat)}: {OutputFilenameFormat}, {nameof(CompatiblePrintsCondition)}: {CompatiblePrintsCondition}";
            }
            #endregion
        }

        #endregion

        #region OutputConfig

        public class OutputConfig
        {
            public string Action { get; set; }
            public string JobDir { get; set; }
            public byte ExpTime { get; set; }
            public ushort ExpTimeFirst { get; set; }
            public string FileCreationTimestamp { get; set; }
            public float LayerHeight { get; set; }
            public string MaterialName { get; set; }
            public byte NumFade { get; set; }
            public ushort NumFast { get; set; }
            public byte NumSlow { get; set; }
            public string PrintProfile { get; set; }
            public float PrintTime { get; set; }
            public string PrinterModel { get; set; }
            public string PrinterProfile { get; set; }
            public string PrinterVariant { get; set; }
            public string PrusaSlicerVersion { get; set; }
            public float UsedMaterial { get; set; }

            public override string ToString()
            {
                return $"{nameof(Action)}: {Action}, {nameof(JobDir)}: {JobDir}, {nameof(ExpTime)}: {ExpTime}, {nameof(ExpTimeFirst)}: {ExpTimeFirst}, {nameof(FileCreationTimestamp)}: {FileCreationTimestamp}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(MaterialName)}: {MaterialName}, {nameof(NumFade)}: {NumFade}, {nameof(NumFast)}: {NumFast}, {nameof(NumSlow)}: {NumSlow}, {nameof(PrintProfile)}: {PrintProfile}, {nameof(PrintTime)}: {PrintTime}, {nameof(PrinterModel)}: {PrinterModel}, {nameof(PrinterProfile)}: {PrinterProfile}, {nameof(PrinterVariant)}: {PrinterVariant}, {nameof(PrusaSlicerVersion)}: {PrusaSlicerVersion}, {nameof(UsedMaterial)}: {UsedMaterial}";
            }
        }

        #endregion

        #endregion

        #region Properties
        public ZipArchive Archive { get; private set; }

        public Printer PrinterSettings { get; private set; }

        public Material MaterialSettings { get; private set; }

        public Print PrintSettings { get; private set; }
        public OutputConfig OutputConfigSettings { get; private set; }

        public Statistics Statistics { get; } = new Statistics();

        public override byte ThumbnailsCount { get; } = 2;

        public override Image<Rgba32>[] Thumbnails { get; protected internal set; }

        //public List<ZipArchiveEntry> Thumbnails { get; } = new List<ZipArchiveEntry>(2);
        public List<ZipArchiveEntry> LayerImages { get; } = new List<ZipArchiveEntry>();

        public uint GetLayerCount => (uint)LayerImages.Count;

        public double TotalHeight => Math.Round(MaterialSettings.InitialLayerHeight + (LayerImages.Count - 1) * OutputConfigSettings.LayerHeight, 2);

        #endregion

        #region Overrides

        public override string FileFullPath { get; protected set; }

        public override FileExtension[] ValidFiles { get; } = {
            new FileExtension("sl1", "Prusa SL1 Files")
        };
        public override bool Equals(object obj)
        {
            return Equals(obj as SL1File);
        }

        public bool Equals(SL1File other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return FileFullPath == other.FileFullPath;
        }

        public override int GetHashCode()
        {
            return (FileFullPath != null ? FileFullPath.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return $"{nameof(FileFullPath)}: {FileFullPath}, {nameof(MaterialSettings)}: {MaterialSettings}, {nameof(PrintSettings)}: {PrintSettings}, {nameof(OutputConfigSettings)}: {OutputConfigSettings}, {nameof(Statistics)}: {Statistics}, {nameof(GetLayerCount)}: {GetLayerCount}, {nameof(TotalHeight)}: {TotalHeight}";
        }

        #endregion

        #region Contructors
        public SL1File() { }
        public SL1File(string fileFullPath) 
        {
            Decode(fileFullPath);
        }
        #endregion

        #region Methods

        

        private List<Image<Gray8>> images = new List<Image<Gray8>>();

        public override void BeginEncode(string fileFullPath)
        {
            throw new NotImplementedException();
        }

        public override void InsertLayerImageEncode(Image<Gray8> image, uint layerIndex)
        {
            throw new NotImplementedException();
        }

        public override void EndEncode()
        {
            throw new NotImplementedException();
        }

        public override void Decode(string fileFullPath)
        {
            DecodeInternal(fileFullPath);

            FileFullPath = fileFullPath;

            PrinterSettings = new Printer();
            MaterialSettings = new Material();
            PrintSettings = new Print();
            OutputConfigSettings = new OutputConfig();

            Statistics.ExecutionTime.Restart();

            var parseTypes = new[]
            {
                new KeyValuePair<Type, object>(typeof(Printer), PrinterSettings),
                new KeyValuePair<Type, object>(typeof(Material), MaterialSettings),
                new KeyValuePair<Type, object>(typeof(Print), PrintSettings),
                new KeyValuePair<Type, object>(typeof(OutputConfig), OutputConfigSettings),
            };

            Archive = ZipFile.OpenRead(FileFullPath);
            byte thumbnailIndex = 0;
            foreach (ZipArchiveEntry entity in Archive.Entries)
            {
                if (entity.Name.EndsWith(".ini"))
                {
                    using (StreamReader streamReader = new StreamReader(entity.Open()))
                    {
                        string line = null;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            string[] keyValue = line.Split(new []{'='}, 2);
                            if (keyValue.Length < 1) continue;
                            keyValue[0] = keyValue[0].Trim();
                            keyValue[1] = keyValue[1].Trim();
                            
                            var fieldName = IniKeyToMemberName(keyValue[0]);
                            bool foundMember = false;

                            foreach (var kv in parseTypes)
                            {
                                var attribute = kv.Key.GetProperty(fieldName);
                                if (attribute == null) continue;
                                SetValue(attribute, kv.Value, keyValue[1]);
                                Statistics.ImplementedKeys.Add(keyValue[0]);
                                foundMember = true;
                            }

                            if (!foundMember)
                            {
                                Statistics.MissingKeys.Add(keyValue[0]);
                            }
                        }
                    }

                    continue;
                }

                if (entity.Name.EndsWith(".png"))
                {
                    if (entity.Name.StartsWith("thumbnail"))
                    {
                        using (Stream stream = entity.Open())
                        {
                            Thumbnails[thumbnailIndex] = Image.Load<Rgba32>(stream);
                            stream.Close();
                        }

                        thumbnailIndex++;

                        continue;
                    }

                    LayerImages.Add(entity);
                }
            }

            Statistics.ExecutionTime.Stop();

            Debug.WriteLine(Statistics);
        }

        public override Image<Gray8> GetLayerImage(uint layerIndex)
        {
            return Image.Load<Gray8>(LayerImages[(int)layerIndex].Open());
        }

        public override float GetHeightFromLayer(uint layerNum)
        {
            return (float)Math.Round(MaterialSettings.InitialLayerHeight + (layerNum - 1) * OutputConfigSettings.LayerHeight, 2);
        }

        public override void Clear()
        {
            ClearInternal();
            Archive?.Dispose();
            LayerImages.Clear();
            Statistics.Clear();
        }

        public override bool Convert(Type to, string fileFullPath)
        {
            if (!IsValid()) return false;

            if (to == typeof(CbddlpFile))
            {
                CbddlpFile file = new CbddlpFile
                {
                    HeaderSettings = new CbddlpFile.Header
                    {
                        Version = 2,
                        AntiAliasLevel = LookupCustomValue<uint>("AntiAliasLevel", 1),
                        BedSizeX = PrinterSettings.DisplayWidth,
                        BedSizeY = PrinterSettings.DisplayHeight,
                        BedSizeZ = PrinterSettings.MaxPrintHeight,
                        BottomExposureSeconds = MaterialSettings.InitialExposureTime,
                        BottomLayersCount = PrintSettings.FadedLayers,
                        BottomLightPWM = LookupCustomValue<ushort>("BottomLightPWM", 255), // TODO
                        LayerCount = GetLayerCount,
                        LayerExposureSeconds = MaterialSettings.ExposureTime,
                        LayerHeightMilimeter = PrintSettings.LayerHeight,
                        LayerOffTime = LookupCustomValue<float>("LayerOffTime", 0), // TODO
                        LightPWM = LookupCustomValue<ushort>("LightPWM", 255), // TODO
                        PrintTime = (uint) OutputConfigSettings.PrintTime,
                        ProjectorType = PrinterSettings.DisplayMirrorX ? 1u : 0u,
                        ResolutionX = PrinterSettings.DisplayPixelsX,
                        ResolutionY = PrinterSettings.DisplayPixelsY,
                    },
                    PrintParametersSettings = new CbddlpFile.PrintParameters
                    {
                        BottomLayerCount = PrintSettings.FadedLayers,
                        BottomLiftHeight = LookupCustomValue<float>("BottomLiftHeight", 5), // TODO
                        BottomLiftSpeed = LookupCustomValue<float>("BottomLiftSpeed", 60), // TODO
                        BottomLightOffDelay = LookupCustomValue<float>("BottomLightOffDelay", 0), // TODO
                        CostDollars = OutputConfigSettings.UsedMaterial * MaterialSettings.BottleCost /
                                      MaterialSettings.BottleVolume,
                        LiftHeight = LookupCustomValue<float>("LiftHeight", 5), // TODO, // TODO
                        LiftingSpeed = LookupCustomValue<float>("LiftingSpeed", 60), // TODO
                        LightOffDelay = LookupCustomValue<float>("LightOffDelay", 0), // TODO
                        RetractSpeed = LookupCustomValue<float>("RetractSpeed", 150), // TODO
                        VolumeMl = OutputConfigSettings.UsedMaterial,
                        WeightG = OutputConfigSettings.UsedMaterial * MaterialSettings.MaterialDensity
                    },
                    Thumbnails = Thumbnails,
                    MachineInfoSettings = new CbddlpFile.MachineInfo()
                    {
                        MachineName = PrinterSettings.PrinterSettingsId,
                        MachineNameSize = (uint)PrinterSettings.PrinterSettingsId.Length

                    },
                };

                if (LookupCustomValue<bool>("FLIP_XY", false, true))
                {
                    file.HeaderSettings.ResolutionX = PrinterSettings.DisplayPixelsY;
                    file.HeaderSettings.ResolutionY = PrinterSettings.DisplayPixelsX;
                }


                file.BeginEncode(fileFullPath);

                for (uint layerIndex = 0; layerIndex < GetLayerCount; layerIndex++)
                {
                    file.InsertLayerImageEncode(GetLayerImage(layerIndex), layerIndex);
                }

                file.EndEncode();
            }

            return false;
        }

        public T LookupCustomValue<T>(string name, T defaultValue, bool existsOnly = false)
        {
            string result = string.Empty;
            if(!existsOnly)
                name += '_';

            int index = PrinterSettings.PrinterNotes.IndexOf(name, StringComparison.Ordinal);
            
            
            int startIndex = index + name.Length;
            
            if (index < 0 || PrinterSettings.PrinterNotes.Length < startIndex) return defaultValue;
            if (existsOnly) return "true".Convert<T>();
            for (int i = startIndex; i < PrinterSettings.PrinterNotes.Length; i++)
            {
                char c = PrinterSettings.PrinterNotes[i];
                if (!Char.IsLetterOrDigit(c) && c != '.')
                {
                    break;
                }
                
                result += PrinterSettings.PrinterNotes[i];
            }

            return result.Convert<T>();
        }

        #endregion

        #region Static Methods
        public static string IniKeyToMemberName(string keyName)
        {
            string memberName = string.Empty;
            string[] objs = keyName.Split('_');
            return objs.Aggregate(memberName, (current, obj) => current + obj.FirstCharToUpper());
        }

        public static bool SetValue(PropertyInfo attribute, object obj, string value)
        {
            var name = attribute.PropertyType.Name.ToLower();
            switch (name)
            {
                case "string":
                    attribute.SetValue(obj, value.Convert<string>());
                    return true;
                case "boolean":
                    attribute.SetValue(obj, !value.Equals(0));
                    return true;
                case "byte":
                    attribute.SetValue(obj, value.Convert<byte>());
                    return true;
                case "uint16":
                    attribute.SetValue(obj, value.Convert<ushort>());
                    return true;
                case "single":
                    attribute.SetValue(obj, float.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
                    return true;
                case "double":
                    attribute.SetValue(obj, double.Parse(value, CultureInfo.InvariantCulture.NumberFormat));
                    return true;
                default:
                    throw new Exception($"Data type '{name}' not recognized, contact developer.");
            }
        }
        #endregion
    }
}
