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

        public const string Keyword_FileFormat = "FILEFORMAT";

        public const string Keyword_BottomLightOffDelay = "BottomLightOffDelay";
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
            public string HostType { get; set; }
            public string PhysicalPrinterSettingsId { get; set; }
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
            public string SupportSmallPillarDiameterPercent { get; set; }
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
                return $"{nameof(SlaPrintSettingsId)}: {SlaPrintSettingsId}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(FadedLayers)}: {FadedLayers}, {nameof(SupportsEnable)}: {SupportsEnable}, {nameof(SupportHeadFrontDiameter)}: {SupportHeadFrontDiameter}, {nameof(SupportHeadPenetration)}: {SupportHeadPenetration}, {nameof(SupportHeadWidth)}: {SupportHeadWidth}, {nameof(SupportPillarWideningFactor)}: {SupportPillarWideningFactor}, {nameof(SupportPillarDiameter)}: {SupportPillarDiameter}, {nameof(SupportSmallPillarDiameterPercent)}: {SupportSmallPillarDiameterPercent}, {nameof(SupportMaxBridgesOnPillar)}: {SupportMaxBridgesOnPillar}, {nameof(SupportPillarConnectionMode)}: {SupportPillarConnectionMode}, {nameof(SupportBuildplateOnly)}: {SupportBuildplateOnly}, {nameof(SupportBaseDiameter)}: {SupportBaseDiameter}, {nameof(SupportBaseHeight)}: {SupportBaseHeight}, {nameof(SupportBaseSafetyDistance)}: {SupportBaseSafetyDistance}, {nameof(PadAroundObject)}: {PadAroundObject}, {nameof(SupportObjectElevation)}: {SupportObjectElevation}, {nameof(SupportCriticalAngle)}: {SupportCriticalAngle}, {nameof(SupportMaxBridgeLength)}: {SupportMaxBridgeLength}, {nameof(SupportMaxPillarLinkDistance)}: {SupportMaxPillarLinkDistance}, {nameof(SupportPointsDensityRelative)}: {SupportPointsDensityRelative}, {nameof(SupportPointsMinimalDistance)}: {SupportPointsMinimalDistance}, {nameof(PadEnable)}: {PadEnable}, {nameof(PadWallThickness)}: {PadWallThickness}, {nameof(PadWallHeight)}: {PadWallHeight}, {nameof(PadBrimSize)}: {PadBrimSize}, {nameof(PadMaxMergeDistance)}: {PadMaxMergeDistance}, {nameof(PadWallSlope)}: {PadWallSlope}, {nameof(PadAroundObjectEverywhere)}: {PadAroundObjectEverywhere}, {nameof(PadObjectGap)}: {PadObjectGap}, {nameof(PadObjectConnectorStride)}: {PadObjectConnectorStride}, {nameof(PadObjectConnectorWidth)}: {PadObjectConnectorWidth}, {nameof(PadObjectConnectorPenetration)}: {PadObjectConnectorPenetration}, {nameof(HollowingEnable)}: {HollowingEnable}, {nameof(HollowingMinThickness)}: {HollowingMinThickness}, {nameof(HollowingQuality)}: {HollowingQuality}, {nameof(HollowingClosingDistance)}: {HollowingClosingDistance}, {nameof(SliceClosingRadius)}: {SliceClosingRadius}, {nameof(OutputFilenameFormat)}: {OutputFilenameFormat}, {nameof(CompatiblePrintsCondition)}: {CompatiblePrintsCondition}";
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
            public ushort NumSlow { get; set; }
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
        public Printer PrinterSettings { get; private set; } = new();

        public Material MaterialSettings { get; private set; } = new();

        public Print PrintSettings { get; private set; } = new();

        public OutputConfig OutputConfigSettings { get; private set; } = new();

        public Statistics Statistics { get; } = new ();


        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new("sl1", "PrusaSlicer SL1")
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
                PrinterSettings.DisplayWidth = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float DisplayHeight
        {
            get => PrinterSettings.DisplayHeight;
            set
            {
                PrinterSettings.DisplayHeight = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float MaxPrintHeight
        {
            get => PrinterSettings.MaxPrintHeight;
            set
            {
                PrinterSettings.MaxPrintHeight = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override bool MirrorDisplay
        {
            get => PrinterSettings.DisplayMirrorX || PrinterSettings.DisplayMirrorY;  
            set
            {
                PrinterSettings.DisplayMirrorX = value;
                PrinterSettings.DisplayMirrorY = false;
                RaisePropertyChanged();
            }
        }   

        public override byte AntiAliasing
        {
            get => (byte)PrinterSettings.GammaCorrection > 0 ? 4 : 1;
            set => PrinterSettings.GammaCorrection = value > 0 ? 1 : 0;
        }

        public override float LayerHeight
        {
            get => OutputConfigSettings.LayerHeight;
            set
            {
                OutputConfigSettings.LayerHeight = (float)Math.Round(value, 2);
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
                OutputConfigSettings.ExpTimeFirst = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float ExposureTime
        {
            get => OutputConfigSettings.ExpTime;
            set
            {
                OutputConfigSettings.ExpTime = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float PrintTime
        {
            get => OutputConfigSettings.PrintTime;
            set => base.PrintTime = OutputConfigSettings.PrintTime = (float)Math.Round(value, 2);
        }

        public override float MaterialMilliliters
        {
            get => OutputConfigSettings.UsedMaterial;
            set
            {
                OutputConfigSettings.UsedMaterial = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float MaterialGrams
        {
            get => (float) Math.Round(OutputConfigSettings.UsedMaterial * MaterialSettings.MaterialDensity, 2);
            set { }
        }

        public override float MaterialCost => MaterialSettings.BottleVolume > 0 ? (float) Math.Round(OutputConfigSettings.UsedMaterial * MaterialSettings.BottleCost / MaterialSettings.BottleVolume, 2) : 0;

        public override string MaterialName
        {
            get => OutputConfigSettings.MaterialName;
            set
            {
                OutputConfigSettings.MaterialName = value;
                RaisePropertyChanged();
            }
        }

        public override string MachineName
        {
            get => PrinterSettings.PrinterSettingsId;
            set
            {
                PrinterSettings.PrinterSettingsId = value;
                RaisePropertyChanged();
            }
        }

        public override object[] Configs => new object[] { PrinterSettings, MaterialSettings, PrintSettings, OutputConfigSettings };
        #endregion

        #region Overrides
        public override string ToString()
        {
            return $"{nameof(FileFullPath)}: {FileFullPath}, {nameof(MaterialSettings)}: {MaterialSettings}, {nameof(PrintSettings)}: {PrintSettings}, {nameof(OutputConfigSettings)}: {OutputConfigSettings}, {nameof(Statistics)}: {Statistics}, {nameof(LayerCount)}: {LayerCount}, {nameof(PrintHeight)}: {PrintHeight}";
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
                    //layer.Filename = layerImagePath;
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
                                //Debug.WriteLine(attribute.Name);
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

                BottomLiftHeight = LookupCustomValue(Keyword_BottomLiftHeight, DefaultBottomLiftHeight);
                BottomLiftSpeed = LookupCustomValue(Keyword_BottomLiftSpeed, DefaultBottomLiftSpeed);
                LiftHeight = LookupCustomValue(Keyword_LiftHeight, DefaultLiftHeight);
                LiftSpeed = LookupCustomValue(Keyword_LiftSpeed, DefaultLiftSpeed);
                RetractSpeed = LookupCustomValue(Keyword_RetractSpeed, DefaultRetractSpeed);
                BottomLightOffDelay = LookupCustomValue(Keyword_BottomLightOffDelay, DefaultBottomLightOffDelay);
                LightOffDelay = LookupCustomValue(Keyword_LightOffDelay, DefaultLightOffDelay);
                BottomLightPWM = LookupCustomValue(Keyword_BottomLightPWM, DefaultLightPWM);
                LightPWM = LookupCustomValue(Keyword_LightPWM, DefaultBottomLightPWM);

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
                    this[iLayer] = new Layer(iLayer, entity.Open(), LayerManager);
                    progress.ProcessedItems++;
                }
            }

            LayerManager.GetBoundingRectangle(progress);

            Statistics.ExecutionTime.Stop();

            Debug.WriteLine(Statistics);
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
                    if (typeof(T) == typeof(string))
                    {
                        if (char.IsWhiteSpace(c)) break;
                    }
                    else
                    {
                        if (!char.IsLetterOrDigit(c) && c != '.')
                        {
                            break;
                        }
                    }
                    

                    result += c;
                }
            }

            return string.IsNullOrWhiteSpace(result) ? defaultValue : result.Convert<T>();
        }

        #endregion
    }
}
