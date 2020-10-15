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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class SL1File : FileFormat
    {
        #region Constants

        public const string Keyword_AntiAliasing        = "AntiAliasing";
        public const string Keyword_BottomLightOffDelay = "BottomLightOffDelay";
        public const string Keyword_LayerOffTime        = "LayerOffTime";
        public const string Keyword_LightOffDelay       = "LightOffDelay";
        public const string Keyword_BottomLiftHeight    = "BottomLiftHeight";
        public const string Keyword_BottomLiftSpeed     = "BottomLiftSpeed";
        public const string Keyword_LiftHeight          = "LiftHeight";
        public const string Keyword_LiftSpeed           = "LiftSpeed";
        public const string Keyword_RetractSpeed        = "RetractSpeed";
        public const string Keyword_BottomLightPWM      = "BottomLightPWM";
        public const string Keyword_LightPWM            = "LightPWM";
        #endregion

        #region Sub Classes 

        #region Printer
        public class Printer
        {
            #region Printer
            public string InheritsCummulative { get; set; }
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
            public float MaxPrintHeight { get; set; }
            #endregion

            #region Display
            public float DisplayWidth { get; set; }
            public float DisplayHeight { get; set; }
            public uint DisplayPixelsX { get; set; }
            public uint DisplayPixelsY { get; set; }
            public string DisplayOrientation { get; set; }
            public bool DisplayMirrorX { get; set; }
            public bool DisplayMirrorY { get; set; }
            #endregion

            #region Tilt
            public float FastTiltTime { get; set; }
            public float SlowTiltTime { get; set; }
            public float AreaFill { get; set; }
            #endregion

            #region Corrections
            public string RelativeCorrection { get; set; }
            public float AbsoluteCorrection { get; set; }
            public float ElefantFootCompensation { get; set; }
            public float ElefantFootMinWidth { get; set; }
            public float GammaCorrection { get; set; }

            #endregion

            #region Exposure

            public float MinExposureTime { get; set; }
            public float MaxExposureTime { get; set; }
            public float MinInitialExposureTime { get; set; }
            public float MaxInitialExposureTime { get; set; }

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
            public float BottleVolume { get; set; }
            public float BottleWeight { get; set; }
            public float MaterialDensity { get; set; }
            public string MaterialNotes { get; set; }

            #endregion

            #region Layers

            public float InitialLayerHeight { get; set; }
            #endregion

            #region Exposure

            public float ExposureTime { get; set; }
            public float InitialExposureTime { get; set; }
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
            public ushort FadedLayers { get; set; }
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
            public float ExpTime { get; set; }
            public float ExpTimeFirst { get; set; }
            public string FileCreationTimestamp { get; set; }
            public float LayerHeight { get; set; }
            public string MaterialName { get; set; }
            public ushort NumFade { get; set; }
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
        public Printer PrinterSettings { get; private set; }

        public Material MaterialSettings { get; private set; }

        public Print PrintSettings { get; private set; }

        public OutputConfig OutputConfigSettings { get; private set; }

        public Statistics Statistics { get; } = new Statistics();


        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("sl1", "PrusaSlicer SL1 Files")
        };

        public override Type[] ConvertToFormats { get; } =
        {
            typeof(ChituboxFile),
            typeof(ChituboxZipFile),
            typeof(PWSFile),
            typeof(PHZFile),
            typeof(ZCodexFile),
            typeof(CWSFile),
            typeof(LGSFile),
            typeof(UVJFile),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
            PrintParameterModifier.ExposureSeconds,
        };

        public override byte ThumbnailsCount { get; } = 2;

        public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } = { new System.Drawing.Size(400, 400), new System.Drawing.Size(800, 480) };
        //public override Image<Rgba32>[] Thumbnails { get; set; }

        public override uint ResolutionX
        {
            get => PrinterSettings.DisplayPixelsX;
            set
            {
                PrinterSettings.DisplayPixelsX = value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => PrinterSettings.DisplayPixelsY;
            set
            {
                PrinterSettings.DisplayPixelsY = value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayWidth
        {
            get => PrinterSettings.DisplayWidth;
            set
            {
                PrinterSettings.DisplayWidth = value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayHeight
        {
            get => PrinterSettings.DisplayHeight;
            set
            {
                PrinterSettings.DisplayHeight = value;
                RaisePropertyChanged();
            }
        }

        public override byte AntiAliasing => (byte) (PrinterSettings.GammaCorrection > 0 ? LookupCustomValue(Keyword_AntiAliasing, 4) : 1);

        public override float LayerHeight
        {
            get => OutputConfigSettings.LayerHeight;
            set
            {
                OutputConfigSettings.LayerHeight = value;
                RaisePropertyChanged();
            }
        }

        public override uint LayerCount
        {
            set
            {
                OutputConfigSettings.NumSlow = 0;
                OutputConfigSettings.NumFast = (ushort) LayerCount;
                RaisePropertyChanged();
            }
        }

        public override ushort BottomLayerCount
        {
            get => OutputConfigSettings.NumFade;
            set
            {
                OutputConfigSettings.NumFade = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomExposureTime
        {
            get => OutputConfigSettings.ExpTimeFirst;
            set
            {
                OutputConfigSettings.ExpTimeFirst = value;
                RaisePropertyChanged();
            }
        }

        public override float ExposureTime
        {
            get => OutputConfigSettings.ExpTime;
            set
            {
                OutputConfigSettings.ExpTime = value;
                RaisePropertyChanged();
            }
        }

        public override float PrintTime => OutputConfigSettings.PrintTime;

        public override float UsedMaterial => OutputConfigSettings.UsedMaterial;

        public override float MaterialCost => (float) Math.Round(OutputConfigSettings.UsedMaterial * MaterialSettings.BottleCost / MaterialSettings.BottleVolume, 2);

        public override string MaterialName => OutputConfigSettings.MaterialName;

        public override string MachineName => PrinterSettings.PrinterSettingsId;

        public override object[] Configs => new object[] { PrinterSettings, MaterialSettings, PrintSettings, OutputConfigSettings };
        #endregion

        #region Overrides
        public override string ToString()
        {
            return $"{nameof(FileFullPath)}: {FileFullPath}, {nameof(MaterialSettings)}: {MaterialSettings}, {nameof(PrintSettings)}: {PrintSettings}, {nameof(OutputConfigSettings)}: {OutputConfigSettings}, {nameof(Statistics)}: {Statistics}, {nameof(LayerCount)}: {LayerCount}, {nameof(TotalHeight)}: {TotalHeight}";
        }

        #endregion

        #region Contructors
        public SL1File() { }
        #endregion

        #region Static Methods
        public static string IniKeyToMemberName(string keyName)
        {
            string memberName = string.Empty;
            string[] objs = keyName.Split('_');
            return objs.Aggregate(memberName, (current, obj) => current + obj.FirstCharToUpper());
        }

        public static string MemberNameToIniKey(string memberName)
        {
            string iniKey = char.ToLowerInvariant(memberName[0]).ToString();
            for (var i = 1; i < memberName.Length; i++)
            {
                iniKey += char.IsUpper(memberName[i])
                    ? $"_{char.ToLowerInvariant(memberName[i])}"
                    : memberName[i].ToString();
            }


            if (iniKey.EndsWith("_"))
                iniKey.Remove(iniKey.Length - 1);

            return iniKey;
        }

        
        #endregion

        #region Methods
        public override void Clear()
        {
            base.Clear();
            Statistics.Clear();
        }

        public override void Encode(string fileFullPath, OperationProgress progress = null)
        {
            base.Encode(fileFullPath, progress);

            using (ZipArchive outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create))
            {
                var entry = outputFile.CreateEntry("config.ini");
                using (TextWriter tw = new StreamWriter(entry.Open()))
                {
                    var properties = OutputConfigSettings.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var property in properties)
                    {
                        var name = char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
                        tw.WriteLine($"{name} = {property.GetValue(OutputConfigSettings)}");
                    }

                    tw.Close();
                }

                entry = outputFile.CreateEntry("prusaslicer.ini");
                using (TextWriter tw = new StreamWriter(entry.Open()))
                {
                    foreach (var config in Configs)
                    {
                        if (ReferenceEquals(config, OutputConfigSettings))
                            continue;

                        var properties = config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                        foreach (var property in properties)
                        {
                            tw.WriteLine($"{MemberNameToIniKey(property.Name)} = {property.GetValue(config)}");
                        }
                    }

                    tw.Close();
                }

                foreach (var thumbnail in Thumbnails)
                {
                    if (ReferenceEquals(thumbnail, null)) continue;
                    using (var stream = outputFile.CreateEntry($"thumbnail/thumbnail{thumbnail.Width}x{thumbnail.Height}.png").Open())
                    {
                        var vec = new VectorOfByte();
                        CvInvoke.Imencode(".png", thumbnail, vec);
                        stream.WriteBytes(vec.ToArray());
                        stream.Close();
                    }
                }

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    Layer layer = this[layerIndex];
                    var layerImagePath = $"{Path.GetFileNameWithoutExtension(fileFullPath)}{layerIndex:D5}.png";
                    layer.Filename = layerImagePath;
                    outputFile.PutFileContent(layerImagePath, layer.CompressedBytes, ZipArchiveMode.Create);
                    progress++;
                }
            }

            AfterEncode();
        }

        
        public override void Decode(string fileFullPath, OperationProgress progress = null)
        {
            base.Decode(fileFullPath, progress);

            if (progress is null) progress = new OperationProgress();
            progress.ItemName = OperationProgress.StatusGatherLayers;

            FileFullPath = fileFullPath;

            PrinterSettings = new Printer();
            MaterialSettings = new Material();
            PrintSettings = new Print();
            OutputConfigSettings = new OutputConfig();

            Statistics.ExecutionTime.Restart();

            using (var inputFile = ZipFile.OpenRead(FileFullPath))
            {

                foreach (ZipArchiveEntry entity in inputFile.Entries)
                {
                    if (!entity.Name.EndsWith(".ini")) continue;
                    using (StreamReader streamReader = new StreamReader(entity.Open()))
                    {
                        string line = null;
                        while ((line = streamReader.ReadLine()) != null)
                        {
                            string[] keyValue = line.Split(new[] {'='}, 2);
                            if (keyValue.Length < 2) continue;
                            keyValue[0] = keyValue[0].Trim();
                            keyValue[1] = keyValue[1].Trim();

                            var fieldName = IniKeyToMemberName(keyValue[0]);
                            bool foundMember = false;

                            foreach (var obj in Configs)
                            {
                                var attribute = obj.GetType().GetProperty(fieldName);
                                if (ReferenceEquals(attribute, null)) continue;
                                Helpers.SetPropertyValue(attribute, obj, keyValue[1]);
                                Statistics.ImplementedKeys.Add(keyValue[0]);
                                foundMember = true;
                            }

                            if (!foundMember)
                            {
                                Statistics.MissingKeys.Add(keyValue[0]);
                            }
                        }
                    }
                }

                LayerManager = new LayerManager((uint) (OutputConfigSettings.NumSlow + OutputConfigSettings.NumFast), this);

                progress.ItemCount = LayerCount;

                foreach (ZipArchiveEntry entity in inputFile.Entries)
                {
                    if (!entity.Name.EndsWith(".png")) continue;
                    if (entity.Name.StartsWith("thumbnail"))
                    {
                        using (Stream stream = entity.Open())
                        {
                            Mat image = new Mat();
                            CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, image);
                            byte thumbnailIndex =
                                (byte) (image.Width == ThumbnailsOriginalSize[(int) FileThumbnailSize.Small].Width &&
                                        image.Height == ThumbnailsOriginalSize[(int) FileThumbnailSize.Small].Height
                                    ? FileThumbnailSize.Small
                                    : FileThumbnailSize.Large);
                            Thumbnails[thumbnailIndex] = image;
                            stream.Close();
                        }

                        //thumbnailIndex++;

                        continue;
                    }

                    // - .png - 5 numbers
                    string layerStr = entity.Name.Substring(entity.Name.Length - 4 - 5, 5);
                    uint iLayer = uint.Parse(layerStr);
                    LayerManager[iLayer] = new Layer(iLayer, entity.Open(), entity.Name)
                    {
                        PositionZ = GetHeightFromLayer(iLayer),
                        ExposureTime = GetInitialLayerValueOrNormal(iLayer, BottomExposureTime, ExposureTime)
                    };
                    progress.ProcessedItems++;
                }
            }

            LayerManager.GetBoundingRectangle(progress);

            Statistics.ExecutionTime.Stop();

            Debug.WriteLine(Statistics);
        }

        /*public override Image<L8> GetLayerImage(uint layerIndex)
        {
            //Stopwatch sw = Stopwatch.StartNew();
            var image = Image.Load<L8>(DecompressLayer(Layers[layerIndex]));
            //Debug.WriteLine(sw.ElapsedMilliseconds);

            return layerIndex >= LayerCount ? null : image;
            //return layerIndex >= LayerCount ? null : Image.Load<L8>(LayerEntries[(int)layerIndex].Open());
            //return layerIndex >= LayerCount ? null : Image.Load<L8>(DecompressLayer(Layers[layerIndex]));
        }*/

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

                //InputFile.CreateEntry("Modified");
                using (TextWriter tw = new StreamWriter(outputFile.PutFileContent("config.ini", string.Empty, ZipArchiveMode.Update).Open()))
                {
                    var properties = OutputConfigSettings.GetType()
                        .GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var property in properties)
                    {
                        var name = char.ToLowerInvariant(property.Name[0]) + property.Name.Substring(1);
                        tw.WriteLine($"{name} = {property.GetValue(OutputConfigSettings)}");
                    }

                    tw.Close();
                }

                using (TextWriter tw = new StreamWriter(outputFile.PutFileContent("prusaslicer.ini", string.Empty, ZipArchiveMode.Update).Open()))
                {
                    foreach (var config in Configs)
                    {
                        if (ReferenceEquals(config, OutputConfigSettings))
                            continue;

                        var properties = config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                        foreach (var property in properties)
                        {
                            tw.WriteLine($"{MemberNameToIniKey(property.Name)} = {property.GetValue(config)}");
                        }
                    }

                    tw.Close();
                }
            }

            //Decode(FileFullPath, progress);

        }

        public override bool Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
            if (!IsValid) return false;

            if (to == typeof(ChituboxFile))
            {
                ChituboxFile defaultFormat = (ChituboxFile)FindByType(typeof(ChituboxFile));
                ChituboxFile file = new ChituboxFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        Version = 2,
                        BedSizeX = PrinterSettings.DisplayWidth,
                        BedSizeY = PrinterSettings.DisplayHeight,
                        BedSizeZ = PrinterSettings.MaxPrintHeight,
                        OverallHeightMilimeter = TotalHeight,
                        BottomExposureSeconds = BottomExposureTime,
                        BottomLayersCount = BottomLayerCount,
                        BottomLightPWM = LookupCustomValue<ushort>(Keyword_BottomLightPWM, defaultFormat.HeaderSettings.BottomLightPWM),
                        LayerCount = LayerCount,
                        LayerExposureSeconds = ExposureTime,
                        LayerHeightMilimeter = LayerHeight,
                        LayerOffTime = LookupCustomValue<float>(Keyword_LayerOffTime, defaultFormat.HeaderSettings.LayerOffTime),
                        LightPWM = LookupCustomValue<ushort>(Keyword_LightPWM, defaultFormat.HeaderSettings.LightPWM),
                        PrintTime = (uint) OutputConfigSettings.PrintTime,
                        ProjectorType = PrinterSettings.DisplayMirrorX || PrinterSettings.DisplayMirrorY ? 1u : 0u,
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        AntiAliasLevel = ValidateAntiAliasingLevel()
                    },
                    PrintParametersSettings =
                    {
                        BottomLayerCount = PrintSettings.FadedLayers,
                        BottomLiftHeight = LookupCustomValue<float>(Keyword_BottomLiftHeight,
                            defaultFormat.PrintParametersSettings.BottomLiftHeight),
                        BottomLiftSpeed = LookupCustomValue<float>(Keyword_BottomLiftSpeed,
                            defaultFormat.PrintParametersSettings.BottomLiftSpeed),
                        BottomLightOffDelay = LookupCustomValue<float>(Keyword_BottomLightOffDelay,
                            defaultFormat.PrintParametersSettings.BottomLightOffDelay),
                        CostDollars = MaterialCost,
                        LiftHeight = LookupCustomValue<float>(Keyword_LiftHeight,
                            defaultFormat.PrintParametersSettings.LiftHeight),
                        LiftSpeed = LookupCustomValue<float>(Keyword_LiftSpeed,
                            defaultFormat.PrintParametersSettings.LiftSpeed),
                        LightOffDelay = LookupCustomValue<float>(Keyword_LightOffDelay,
                            defaultFormat.PrintParametersSettings.LightOffDelay),
                        RetractSpeed = LookupCustomValue<float>(Keyword_RetractSpeed,
                            defaultFormat.PrintParametersSettings.RetractSpeed),
                        VolumeMl = UsedMaterial,
                        WeightG = (float) Math.Round(
                            OutputConfigSettings.UsedMaterial * MaterialSettings.MaterialDensity, 2)
                    },
                    SlicerInfoSettings = {MachineName = MachineName, MachineNameSize = (uint) MachineName.Length}
                };


                if (LookupCustomValue<bool>("FLIP_XY", false, true))
                {
                    file.HeaderSettings.ResolutionX = PrinterSettings.DisplayPixelsY;
                    file.HeaderSettings.ResolutionY = PrinterSettings.DisplayPixelsX;

                    file.HeaderSettings.BedSizeX = PrinterSettings.DisplayHeight;
                    file.HeaderSettings.BedSizeY = PrinterSettings.DisplayWidth;
                }

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);
                
                return true;
            }

            if (to == typeof(ChituboxZipFile))
            {
                ChituboxZipFile defaultFormat = (ChituboxZipFile)FindByType(typeof(ChituboxZipFile));
                ChituboxZipFile file = new ChituboxZipFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        Filename = Path.GetFileName(FileFullPath),

                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        MachineX = PrinterSettings.DisplayWidth,
                        MachineY = PrinterSettings.DisplayHeight,
                        MachineZ = PrinterSettings.MaxPrintHeight,
                        MachineType = MachineName,
                        ProjectType = PrinterSettings.DisplayMirrorX || PrinterSettings.DisplayMirrorY ? "LCD_mirror" : "Normal",

                        Resin = MaterialName,
                        Price = MaterialCost,
                        Weight = (float) Math.Round(UsedMaterial * MaterialSettings.MaterialDensity, 2),
                        Volume = UsedMaterial,
                        Mirror = (byte) (PrinterSettings.DisplayMirrorX || PrinterSettings.DisplayMirrorY ? 1 : 0),


                        BottomLiftHeight = LookupCustomValue<float>(Keyword_BottomLiftHeight, defaultFormat.HeaderSettings.BottomLiftHeight),
                        LiftHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.HeaderSettings.LiftHeight),
                        BottomLiftSpeed = LookupCustomValue<float>(Keyword_BottomLiftSpeed, defaultFormat.HeaderSettings.BottomLiftSpeed),
                        LiftSpeed = LookupCustomValue<float>(Keyword_LiftSpeed, defaultFormat.HeaderSettings.LiftSpeed),
                        RetractSpeed = LookupCustomValue<float>(Keyword_RetractSpeed, defaultFormat.HeaderSettings.RetractSpeed),
                        BottomLayCount = BottomLayerCount,
                        BottomLayerCount = BottomLayerCount,
                        BottomLightOffTime = LookupCustomValue<float>(Keyword_BottomLightOffDelay, defaultFormat.HeaderSettings.BottomLightOffTime),
                        LightOffTime = LookupCustomValue<float>(Keyword_LightOffDelay, defaultFormat.HeaderSettings.LightOffTime),
                        BottomLayExposureTime = BottomExposureTime,
                        BottomLayerExposureTime = BottomExposureTime,
                        LayerExposureTime = ExposureTime,
                        LayerHeight = LayerHeight,
                        LayerCount = LayerCount,
                        AntiAliasing = ValidateAntiAliasingLevel(),
                        BottomLightPWM = LookupCustomValue<byte>(Keyword_BottomLightPWM, defaultFormat.HeaderSettings.BottomLightPWM),
                        LightPWM = LookupCustomValue<byte>(Keyword_LightPWM, defaultFormat.HeaderSettings.LightPWM),
                        
                        EstimatedPrintTime = PrintTime
                    },
                };


                if (LookupCustomValue<bool>("FLIP_XY", false, true))
                {
                    file.HeaderSettings.ResolutionX = PrinterSettings.DisplayPixelsY;
                    file.HeaderSettings.ResolutionY = PrinterSettings.DisplayPixelsX;
                }

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(PWSFile))
            {
                PWSFile defaultFormat = (PWSFile)FindByType(typeof(PWSFile));
                PWSFile file = new PWSFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        LayerHeight = LayerHeight,
                        LayerExposureTime = ExposureTime,
                        LiftHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.HeaderSettings.LiftHeight),
                        LiftSpeed = LookupCustomValue<float>(Keyword_LiftSpeed, defaultFormat.HeaderSettings.LiftSpeed) / 60,
                        RetractSpeed = LookupCustomValue<float>(Keyword_RetractSpeed, defaultFormat.HeaderSettings.RetractSpeed) / 60,
                        LayerOffTime = LookupCustomValue<float>(Keyword_LayerOffTime, defaultFormat.HeaderSettings.LayerOffTime),
                        BottomLayersCount = BottomLayerCount,
                        BottomExposureSeconds = BottomExposureTime,
                        Price = MaterialCost,
                        Volume = UsedMaterial,
                        Weight = (float) Math.Round(OutputConfigSettings.UsedMaterial * MaterialSettings.MaterialDensity, 2),
                        AntiAliasing = ValidateAntiAliasingLevel()
                    }
                };


                if (LookupCustomValue<bool>("FLIP_XY", false, true))
                {
                    file.HeaderSettings.ResolutionX = PrinterSettings.DisplayPixelsY;
                    file.HeaderSettings.ResolutionY = PrinterSettings.DisplayPixelsX;
                }

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(PHZFile))
            {
                PHZFile defaultFormat = (PHZFile)FindByType(typeof(PHZFile));
                PHZFile file = new PHZFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        Version = 2,
                        BedSizeX = PrinterSettings.DisplayWidth,
                        BedSizeY = PrinterSettings.DisplayHeight,
                        BedSizeZ = PrinterSettings.MaxPrintHeight,
                        OverallHeightMilimeter = TotalHeight,
                        BottomExposureSeconds = MaterialSettings.InitialExposureTime,
                        BottomLayersCount = PrintSettings.FadedLayers,
                        BottomLightPWM = LookupCustomValue<ushort>(Keyword_BottomLightPWM, defaultFormat.HeaderSettings.BottomLightPWM),
                        LayerCount = LayerCount,
                        LayerExposureSeconds = MaterialSettings.ExposureTime,
                        LayerHeightMilimeter = PrintSettings.LayerHeight,
                        LayerOffTime = LookupCustomValue<float>(Keyword_LayerOffTime, defaultFormat.HeaderSettings.LayerOffTime),
                        LightPWM = LookupCustomValue<ushort>(Keyword_LightPWM, defaultFormat.HeaderSettings.LightPWM),
                        PrintTime = (uint) OutputConfigSettings.PrintTime,
                        ProjectorType = PrinterSettings.DisplayMirrorX || PrinterSettings.DisplayMirrorY ? 1u : 0u,
                        ResolutionX = PrinterSettings.DisplayPixelsX,
                        ResolutionY = PrinterSettings.DisplayPixelsY,
                        BottomLayerCount = PrintSettings.FadedLayers,
                        BottomLiftHeight = LookupCustomValue<float>(Keyword_BottomLiftHeight, defaultFormat.HeaderSettings.BottomLiftHeight),
                        BottomLiftSpeed = LookupCustomValue<float>(Keyword_BottomLiftSpeed, defaultFormat.HeaderSettings.BottomLiftSpeed),
                        BottomLightOffDelay = LookupCustomValue<float>(Keyword_BottomLightOffDelay, defaultFormat.HeaderSettings.BottomLightOffDelay),
                        CostDollars = MaterialCost,
                        LiftHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.HeaderSettings.LiftHeight),
                        LiftSpeed = LookupCustomValue<float>(Keyword_LiftSpeed, defaultFormat.HeaderSettings.LiftSpeed),
                        RetractSpeed = LookupCustomValue<float>(Keyword_RetractSpeed, defaultFormat.HeaderSettings.RetractSpeed),
                        VolumeMl = OutputConfigSettings.UsedMaterial,
                        WeightG = (float)Math.Round(OutputConfigSettings.UsedMaterial * MaterialSettings.MaterialDensity, 2),
                        MachineName = MachineName,
                        MachineNameSize = (uint)MachineName.Length,
                        AntiAliasLevelInfo = ValidateAntiAliasingLevel()
                    }
                };

                if (LookupCustomValue<bool>("FLIP_XY", false, true))
                {
                    file.HeaderSettings.ResolutionX = ResolutionY;
                    file.HeaderSettings.ResolutionY = ResolutionX;

                    file.HeaderSettings.BedSizeX = PrinterSettings.DisplayHeight;
                    file.HeaderSettings.BedSizeY = PrinterSettings.DisplayWidth;
                }

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(ZCodexFile))
            {
                ZCodexFile defaultFormat = (ZCodexFile)FindByType(typeof(ZCodexFile));
                TimeSpan ts = new TimeSpan(0, 0, (int)PrintTime);
                ZCodexFile file = new ZCodexFile
                {
                    ResinMetadataSettings = new ZCodexFile.ResinMetadata
                    {
                        MaterialId = 2,
                        Material = MaterialName,
                        AdditionalSupportLayerTime = 0,
                        BottomLayersNumber = BottomLayerCount,
                        BottomLayersTime = (uint)(BottomExposureTime*1000),
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
                        AntiAliasing = (byte) (ValidateAntiAliasingLevel() > 1 ? 1 : 0),
                        CrossSupportEnabled = 1,
                        ExposureOffTime = LookupCustomValue<uint>(Keyword_LayerOffTime, defaultFormat.UserSettings.ExposureOffTime) * 1000,
                        HollowEnabled = PrintSettings.HollowingEnable ? (byte)1 : (byte)0,
                        HollowThickness = PrintSettings.HollowingMinThickness,
                        InfillDensity = 0,
                        IsAdvanced = 0,
                        MaterialType = MaterialName,
                        MaterialVolume = UsedMaterial,
                        MaxLayer = LayerCount-1,
                        ModelLiftEnabled = PrintSettings.SupportObjectElevation > 0 ? (byte)1 : (byte)0,
                        ModelLiftHeight = PrintSettings.SupportObjectElevation,
                        RaftEnabled = PrintSettings.SupportBaseHeight > 0 ? (byte)1 : (byte)0,
                        RaftHeight = PrintSettings.SupportBaseHeight,
                        RaftOffset = 0,
                        SupportAdditionalExposureEnabled = 0,
                        SupportAdditionalExposureTime = 0,
                        XCorrection = PrinterSettings.AbsoluteCorrection,
                        YCorrection = PrinterSettings.AbsoluteCorrection,
                        ZLiftDistance = (float)Math.Round(LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.UserSettings.ZLiftDistance), 2),
                        ZLiftFeedRate = (float)Math.Round(LookupCustomValue<float>(Keyword_LiftSpeed, defaultFormat.UserSettings.ZLiftFeedRate), 2),
                        ZLiftRetractRate = (float)Math.Round(LookupCustomValue<float>(Keyword_RetractSpeed, defaultFormat.UserSettings.ZLiftRetractRate), 2),
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
                CWSFile file = new CWSFile {LayerManager = LayerManager};

                file.SliceSettings.Xppm = file.OutputSettings.PixPermmX = (float) Math.Round(LookupCustomValue<float>("Xppm", defaultFormat.SliceSettings.Xppm), 3);
                file.SliceSettings.Yppm = file.OutputSettings.PixPermmY = (float) Math.Round(LookupCustomValue<float>("Yppm", defaultFormat.SliceSettings.Xppm), 3);
                file.SliceSettings.Xres = file.OutputSettings.XResolution = (ushort)ResolutionX;
                file.SliceSettings.Yres = file.OutputSettings.YResolution = (ushort)ResolutionY;
                file.OutputSettings.PlatformXSize = PrinterSettings.DisplayWidth;
                file.OutputSettings.PlatformYSize = PrinterSettings.DisplayHeight;
                file.OutputSettings.PlatformZSize = PrinterSettings.MaxPrintHeight;
                file.SliceSettings.Thickness = file.OutputSettings.LayerThickness = LayerHeight;
                file.SliceSettings.LayersNum = file.OutputSettings.LayersNum = LayerCount;
                file.SliceSettings.HeadLayersNum = file.OutputSettings.NumberBottomLayers = BottomLayerCount;
                file.SliceSettings.LayersExpoMs = file.OutputSettings.LayerTime = (uint) ExposureTime * 1000;
                file.SliceSettings.HeadLayersExpoMs = file.OutputSettings.BottomLayersTime = (uint) BottomExposureTime * 1000;
                file.SliceSettings.WaitBeforeExpoMs = LookupCustomValue<uint>("WaitBeforeExpoMs", defaultFormat.SliceSettings.WaitBeforeExpoMs);
                file.SliceSettings.LiftDistance = file.OutputSettings.LiftDistance = (float) Math.Round(LookupCustomValue<float>(Keyword_LiftHeight, file.SliceSettings.LiftDistance), 2);
                file.SliceSettings.LiftUpSpeed = file.OutputSettings.ZLiftFeedRate = file.OutputSettings.ZBottomLiftFeedRate = (float) Math.Round(LookupCustomValue<float>(Keyword_LiftSpeed, file.SliceSettings.LiftUpSpeed), 2);
                file.SliceSettings.LiftDownSpeed = file.OutputSettings.ZLiftRetractRate = (float) Math.Round(LookupCustomValue<float>(Keyword_RetractSpeed, file.SliceSettings.LiftDownSpeed), 2);
                file.SliceSettings.LiftWhenFinished = LookupCustomValue<byte>("LiftWhenFinished", defaultFormat.SliceSettings.LiftWhenFinished);

                file.OutputSettings.BlankingLayerTime = LookupCustomValue<uint>("BlankingLayerTime", defaultFormat.OutputSettings.BlankingLayerTime);
                //file.OutputSettings.RenderOutlines = false;
                //file.OutputSettings.OutlineWidthInset = 0;
                //file.OutputSettings.OutlineWidthOutset = 0;
                file.OutputSettings.RenderOutlines = false;
                //file.OutputSettings.TiltValue = 0;
                //file.OutputSettings.UseMainliftGCodeTab = false;
                //file.OutputSettings.AntiAliasing = 0;
                //file.OutputSettings.AntiAliasingValue = 0;
                file.OutputSettings.FlipX = PrinterSettings.DisplayMirrorX;
                file.OutputSettings.FlipY = PrinterSettings.DisplayMirrorY;
                file.OutputSettings.AntiAliasingValue = ValidateAntiAliasingLevel();
                file.OutputSettings.AntiAliasing = file.OutputSettings.AntiAliasingValue > 1;

                if (LookupCustomValue<bool>("FLIP_XY", false, true))
                {
                    file.SliceSettings.Xres = file.OutputSettings.XResolution = (ushort) ResolutionY;
                    file.SliceSettings.Yres = file.OutputSettings.YResolution = (ushort) ResolutionX;

                    file.OutputSettings.PlatformXSize = PrinterSettings.DisplayHeight;
                    file.OutputSettings.PlatformYSize = PrinterSettings.DisplayWidth;
                }

                file.Printer = LookupCustomValue<bool>("NOVAMAKER_GRAY2RGB_ENCODE", false, true) ||
                               MachineName.Contains("Bene4 Mono") ||
                               FileFullPath.Contains("bene4_mono") 
                    ? CWSFile.PrinterType.BeneMono : CWSFile.PrinterType.Elfin;
                
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(LGSFile))
            {
                LGSFile defaultFormat = (LGSFile)FindByType(typeof(LGSFile));
                LGSFile file = new LGSFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        LayerHeight = LayerHeight,
                        ExposureTimeMs = ExposureTime * 1000,
                        LiftHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.HeaderSettings.LiftHeight),
                        LiftSpeed = LookupCustomValue<float>(Keyword_LiftSpeed, defaultFormat.HeaderSettings.LiftSpeed),
                        LiftSpeed_ = LookupCustomValue<float>(Keyword_LiftSpeed, defaultFormat.HeaderSettings.LiftSpeed),
                        LightOffDelayMs = LookupCustomValue<float>(Keyword_LayerOffTime, defaultFormat.HeaderSettings.LightOffDelayMs) * 1000,
                        BottomHeight = BottomLayerCount * LayerHeight,
                        BottomExposureTimeMs = BottomExposureTime * 1000,
                        LayerCount = LayerCount,
                        BottomLiftSpeed = LookupCustomValue<float>(Keyword_BottomLiftSpeed, defaultFormat.HeaderSettings.BottomLiftSpeed),
                        BottomLiftSpeed_ = LookupCustomValue<float>(Keyword_BottomLiftSpeed, defaultFormat.HeaderSettings.BottomLiftSpeed_),
                        BottomLiftHeight = LookupCustomValue<float>(Keyword_BottomLiftHeight, defaultFormat.HeaderSettings.BottomLiftHeight),
                        BottomLightOffDelayMs = LookupCustomValue<float>(Keyword_BottomLightOffDelay, defaultFormat.HeaderSettings.BottomLightOffDelayMs) * 1000,
                        PixelPerMmX = (float) Math.Round(ResolutionY / PrinterSettings.DisplayWidth, 3),
                        PixelPerMmY = (float) Math.Round(ResolutionX / PrinterSettings.DisplayHeight, 3),
                    }
                };


                if (LookupCustomValue<bool>("FLIP_XY", false, true))
                {
                    file.HeaderSettings.ResolutionX = PrinterSettings.DisplayPixelsY;
                    file.HeaderSettings.ResolutionY = PrinterSettings.DisplayPixelsX;
                }

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(UVJFile))
            {
                UVJFile defaultFormat = (UVJFile)FindByType(typeof(UVJFile));
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
                                    X = PrinterSettings.DisplayWidth,
                                    Y = PrinterSettings.DisplayHeight,
                                },
                                LayerHeight = LayerHeight,
                                Layers = LayerCount
                            },
                            Bottom = new UVJFile.Bottom
                            {
                                LiftHeight = LookupCustomValue<float>(Keyword_BottomLiftHeight, defaultFormat.JsonSettings.Properties.Bottom.LiftHeight),
                                LiftSpeed = LookupCustomValue<float>(Keyword_BottomLiftSpeed, defaultFormat.JsonSettings.Properties.Bottom.LiftSpeed),
                                LightOnTime = BottomExposureTime,
                                LightOffTime = LookupCustomValue<float>(Keyword_BottomLightOffDelay, defaultFormat.JsonSettings.Properties.Bottom.LightOffTime),
                                LightPWM = LookupCustomValue<byte>(Keyword_BottomLightPWM, defaultFormat.JsonSettings.Properties.Bottom.LightPWM),
                                RetractSpeed = LookupCustomValue<float>(Keyword_RetractSpeed, defaultFormat.JsonSettings.Properties.Bottom.RetractSpeed),
                                Count = BottomLayerCount
                                //RetractHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.JsonSettings.Properties.Bottom.RetractHeight),
                            },
                            Exposure = new UVJFile.Exposure
                            {
                                LiftHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.JsonSettings.Properties.Exposure.LiftHeight),
                                LiftSpeed = LookupCustomValue<float>(Keyword_LiftSpeed, defaultFormat.JsonSettings.Properties.Exposure.LiftSpeed),
                                LightOnTime = ExposureTime,
                                LightOffTime = LookupCustomValue<float>(Keyword_LightOffDelay, defaultFormat.JsonSettings.Properties.Exposure.LightOffTime),
                                LightPWM = LookupCustomValue<byte>(Keyword_LightPWM, defaultFormat.JsonSettings.Properties.Exposure.LightPWM),
                                RetractSpeed = LookupCustomValue<float>(Keyword_RetractSpeed, defaultFormat.JsonSettings.Properties.Exposure.RetractSpeed),
                            },
                            AntiAliasLevel = ValidateAntiAliasingLevel()
                        }
                    }
                };

                if (LookupCustomValue<bool>("FLIP_XY", false, true))
                {
                    file.JsonSettings.Properties.Size.X = (ushort) ResolutionY;
                    file.JsonSettings.Properties.Size.Y = (ushort) ResolutionX;
                }

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            return false;
        }

        public T LookupCustomValue<T>(string name, T defaultValue, bool existsOnly = false)
        {
            if (string.IsNullOrEmpty(PrinterSettings.PrinterNotes)) return defaultValue;
            string result = string.Empty;
            if(!existsOnly)
                name += '_';

            var lines = PrinterSettings.PrinterNotes.Split(new[] { "\\r\\n", "\\r", "\\n" },
                StringSplitOptions.RemoveEmptyEntries);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (!line.StartsWith(name)) continue;
                if (existsOnly || line == name) return "true".Convert<T>();
                var value = line.Remove(0, name.Length);
                for (int x = 0; x < value.Length; x++)
                {
                    char c = value[x];
                    if (!char.IsLetterOrDigit(c) && c != '.')
                    {
                        break;
                    }

                    result += c;
                }
            }

            return string.IsNullOrWhiteSpace(result) ? defaultValue : result.Convert<T>();
        }

        #endregion
    }
}
