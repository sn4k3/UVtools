/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class ChituboxZipFile : FileFormat
    {
        #region Constants

        public const string GCodeStart = ";START_GCODE_BEGIN{0}" +
                                         "G21 ;Set units to be mm{0}" +
                                         "G90 ;Absolute Positioning{0}" +
                                         "M17 ;Enable motors{0}" +
                                         "G28 Z0 ;Home Z{0}" +
                                         //"G91 ;Relative Positioning{0}" +
                                         "M106 S0 ;Light off{0}" +
                                         ";START_GCODE_END{0}{0}";

        public const string GCodeEnd = ";END_GCODE_BEGIN{0}" +
                                      "M106 S0 ;Light off{0}" +
                                      "G1 Z{1} F25 ;Raize Z{0}" +
                                      "M18 ;Disable Motors{0}" +
                                      ";END_GCODE_END{0}";

        #endregion

        #region Sub Classes

        public class Header
        {
            // ;(****Build and Slicing Parameters****)
            [DisplayName("fileName")] public string Filename { get; set; } = string.Empty;
            [DisplayName("machineType")] public string MachineType { get; set; } = "Default";
            [DisplayName("estimatedPrintTime")] public float EstimatedPrintTime { get; set; }
            [DisplayName("volume")] public float Volume { get; set; }
            [DisplayName("resin")] public string Resin { get; set; } = "Normal";
            [DisplayName("weight")] public float Weight { get; set; }
            [DisplayName("price")] public float Price { get; set; }
            [DisplayName("layerHeight")] public float LayerHeight { get; set; }
            [DisplayName("resolutionX")] public uint ResolutionX { get; set; }
            [DisplayName("resolutionY")] public uint ResolutionY { get; set; }
            [DisplayName("machineX")] public float MachineX { get; set; }
            [DisplayName("machineY")] public float MachineY { get; set; }
            [DisplayName("machineZ")] public float MachineZ { get; set; }
            [DisplayName("projectType")] public string ProjectType { get; set; } = "Normal";
            [DisplayName("normalExposureTime")] public float LayerExposureTime { get; set; } = 7; // 35s
            [DisplayName("bottomLayExposureTime")] public float BottomLayExposureTime { get; set; } = 35; // 35s
            [DisplayName("bottomLayerExposureTime")] public float BottomLayerExposureTime { get; set; } = 35; // 35s
            [DisplayName("normalDropSpeed")] public float RetractSpeed { get; set; } = 150; // 150 mm/m
            [DisplayName("normalLayerLiftSpeed")] public float LiftSpeed { get; set; } = 60; // 60 mm/m
            [DisplayName("normalLayerLiftHeight")] public float LiftHeight { get; set; } = 5; // 5 mm
            [DisplayName("zSlowUpDistance")] public float ZSlowUpDistance { get; set; }
            [DisplayName("bottomLayCount")] public ushort BottomLayCount { get; set; } = 4;
            [DisplayName("bottomLayerCount")] public ushort BottomLayerCount { get; set; } = 4;
            [DisplayName("mirror")] public byte Mirror { get; set; } // 0/1
            [DisplayName("totalLayer")] public uint LayerCount { get; set; }
            [DisplayName("bottomLayerLiftHeight")] public float BottomLiftHeight { get; set; } = 5;
            [DisplayName("bottomLayerLiftSpeed")] public float BottomLiftSpeed { get; set; } = 60;
            [DisplayName("bottomLightOffTime")] public float BottomLightOffTime { get; set; }
            [DisplayName("lightOffTime")] public float LightOffTime { get; set; }
            [DisplayName("bottomPWMLight")] public byte BottomLightPWM { get; set; } = 255;
            [DisplayName("PWMLight")] public byte LightPWM { get; set; } = 255;
            [DisplayName("antiAliasLevel")] public byte AntiAliasing { get; set; } = 1;
        }

        #endregion

        #region Properties
        public Header HeaderSettings { get; } = new Header();

        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("zip", "Chitubox Zip")
        };

        public override Type[] ConvertToFormats { get; } = {
            typeof(ChituboxFile),
            typeof(PHZFile),
            typeof(PhotonWorkshopFile),
            typeof(CWSFile),
            typeof(ZCodexFile),
            typeof(UVJFile)
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

        public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
            PrintParameterModifier.ExposureSeconds,
            PrintParameterModifier.LayerOffTime,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.LightPWM,
        };

        public override byte ThumbnailsCount { get; } = 2;

        public override Size[] ThumbnailsOriginalSize { get; } = {new Size(954, 850), new Size(168, 150)};

        public override uint ResolutionX
        {
            get => HeaderSettings.ResolutionX;
            set
            {
                HeaderSettings.ResolutionX = value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => HeaderSettings.ResolutionY;
            set
            {
                HeaderSettings.ResolutionY = value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayWidth
        {
            get => HeaderSettings.MachineX;
            set
            {
                HeaderSettings.MachineX = value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayHeight
        {
            get => HeaderSettings.MachineY;
            set
            {
                HeaderSettings.MachineX = value;
                RaisePropertyChanged();
            }
        }

        public override byte AntiAliasing => HeaderSettings.AntiAliasing;

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
            get => HeaderSettings.BottomLayerCount;
            set
            {
                HeaderSettings.BottomLayerCount = HeaderSettings.BottomLayCount = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomExposureTime
        {
            get => HeaderSettings.BottomLayerExposureTime;
            set
            {
                HeaderSettings.BottomLayerExposureTime = HeaderSettings.BottomLayExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public override float ExposureTime
        {
            get => HeaderSettings.LayerExposureTime;
            set
            {
                HeaderSettings.LayerExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomLayerOffTime
        {
            get => HeaderSettings.BottomLightOffTime;
            set
            {
                HeaderSettings.BottomLightOffTime = value;
                RaisePropertyChanged();
            }
        }

        public override float LayerOffTime
        {
            get => HeaderSettings.LightOffTime;
            set
            {
                HeaderSettings.LightOffTime = value;
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
                HeaderSettings.BottomLiftSpeed = value;
                RaisePropertyChanged();
            }
        }

        public override float LiftSpeed
        {
            get => HeaderSettings.LiftSpeed;
            set
            {
                HeaderSettings.LiftSpeed = value;
                RaisePropertyChanged();
            }
        }

        public override float RetractSpeed
        {
            get => HeaderSettings.RetractSpeed;
            set
            {
                HeaderSettings.RetractSpeed = value;
                RaisePropertyChanged();
            }
        }

        public override byte BottomLightPWM
        {
            get => HeaderSettings.BottomLightPWM;
            set
            {
                HeaderSettings.BottomLightPWM = value;
                RaisePropertyChanged();
            }
        }

        public override byte LightPWM
        {
            get => HeaderSettings.LightPWM;
            set
            {
                HeaderSettings.LightPWM = value;
                RaisePropertyChanged();
            }
        }

        public override float PrintTime
        {
            get => HeaderSettings.EstimatedPrintTime;
            set
            {
                HeaderSettings.EstimatedPrintTime = value;
                RaisePropertyChanged();
            }
        }

        public override float UsedMaterial
        {
            get => HeaderSettings.Weight;
            set
            {
                HeaderSettings.Weight = value;
                RaisePropertyChanged();
            }
        }

        public override float MaterialCost
        {
            get => HeaderSettings.Price;
            set
            {
                HeaderSettings.Price = value;
                RaisePropertyChanged();
            }
        }

        public override string MaterialName
        {
            get => HeaderSettings.Resin;
            set
            {
                HeaderSettings.Resin = value;
                RaisePropertyChanged();
            }
        }

        public override string MachineName
        {
            get => HeaderSettings.MachineType;
            set
            {
                HeaderSettings.MachineType = value;
                RequireFullEncode = true;
                RaisePropertyChanged();
            }
        }

        public override object[] Configs => new object[] { HeaderSettings };

        public bool IsPHZZip = false;
        #endregion

        #region Methods

        public override void Encode(string fileFullPath, OperationProgress progress = null)
        {
            base.Encode(fileFullPath, progress);
            using (ZipArchive outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create))
            {
                if (Thumbnails.Length > 0 && !ReferenceEquals(Thumbnails[0], null))
                {
                    using (Stream stream = outputFile.CreateEntry("preview.png").Open())
                    {
                        using (var vec = new VectorOfByte())
                        {
                            CvInvoke.Imencode(".png", Thumbnails[0], vec);
                            stream.WriteBytes(vec.ToArray());
                            stream.Close();
                        }
                    }
                }

                if (Thumbnails.Length > 1 && !ReferenceEquals(Thumbnails[1], null))
                {
                    using (Stream stream = outputFile.CreateEntry("preview_cropping.png").Open())
                    {
                        using (var vec = new VectorOfByte())
                        {
                            CvInvoke.Imencode(".png", Thumbnails[1], vec);
                            stream.WriteBytes(vec.ToArray());
                            stream.Close();
                        }
                    }
                }

                if (!IsPHZZip)
                {
                    RebuildGCode();
                    outputFile.PutFileContent("run.gcode", GCode.ToString(), ZipArchiveMode.Create);
                }

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    Layer layer = this[layerIndex];
                    outputFile.PutFileContent($"{layerIndex + 1}.png", layer.CompressedBytes,
                        ZipArchiveMode.Create);
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
                var entry = inputFile.GetEntry("run.gcode");
                if (!ReferenceEquals(entry, null))
                {
                    //Clear();
                    //throw new FileLoadException("run.gcode not found", fileFullPath);
                    using (TextReader tr = new StreamReader(entry.Open()))
                    {
                        string line;
                        GCode = new StringBuilder();
                        while ((line = tr.ReadLine()) != null)
                        {
                            GCode.AppendLine(line);
                            if (string.IsNullOrEmpty(line)) continue;

                            if (line[0] != ';')
                            {
                                continue;
                            }

                            var splitLine = line.Split(':');
                            if (splitLine.Length < 2) continue;

                            foreach (var propertyInfo in HeaderSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                            {
                                var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                                if (ReferenceEquals(displayNameAttribute, null)) continue;
                                if (!splitLine[0].Trim(' ', ';').Equals(displayNameAttribute.DisplayName)) continue;
                                Helpers.SetPropertyValue(propertyInfo, HeaderSettings, splitLine[1].Trim());
                            }
                        }
                        tr.Close();
                    }
                }
                else
                {
                    IsPHZZip = true;
                }

                if (HeaderSettings.LayerCount == 0)
                {
                    foreach (var zipEntry in inputFile.Entries)
                    {
                        if(!zipEntry.Name.EndsWith(".png")) continue;
                        var filename = Path.GetFileNameWithoutExtension(zipEntry.Name);
                        if (!filename.All(char.IsDigit)) continue;
                        if (!uint.TryParse(filename, out var layerIndex)) continue;
                        HeaderSettings.LayerCount = Math.Max(HeaderSettings.LayerCount, layerIndex);
                    }
                }

                LayerManager = new LayerManager(HeaderSettings.LayerCount, this);

                progress.ItemCount = LayerCount;

                var gcode = GCode?.ToString();
                float lastPostZ = LayerHeight;

                for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    if (progress.Token.IsCancellationRequested) break;
                    entry = inputFile.GetEntry($"{layerIndex+1}.png");
                    if (ReferenceEquals(entry, null))
                    {
                        Clear();
                        throw new FileLoadException($"Layer {layerIndex+1} not found", fileFullPath);
                    }

                    if (IsPHZZip) // PHZ file
                    {
                        this[layerIndex] = new Layer(layerIndex, entry.Open(), LayerManager);
                        progress++;
                        continue;;
                    }


                    var startStr = $";LAYER_START:{layerIndex}";
                    var stripGcode = gcode.Substring(gcode.IndexOf(startStr, StringComparison.InvariantCultureIgnoreCase) + startStr.Length);
                    stripGcode = stripGcode.Substring(0, stripGcode.IndexOf(";LAYER_END")).Trim(' ', '\n', '\r', '\t');
                    //var startCurrPos = stripGcode.Remove(0, ";currPos:".Length);

                    float posZ = lastPostZ;
                    float liftHeight = GetInitialLayerValueOrNormal(layerIndex, BottomLiftHeight, LiftHeight);
                    float liftSpeed = GetInitialLayerValueOrNormal(layerIndex, BottomLiftSpeed, LiftSpeed);
                    float retractSpeed = RetractSpeed;
                    float lightOffDelay = 0;
                    byte pwm = GetInitialLayerValueOrNormal(layerIndex, BottomLightPWM, LightPWM); ;
                    float exposureTime = GetInitialLayerValueOrNormal(layerIndex, BottomExposureTime, ExposureTime);

                    //var currPosRegex = Regex.Match(stripGcode, @";currPos:([+-]?([0-9]*[.])?[0-9]+)", RegexOptions.IgnoreCase);
                    var moveG0Regex = Regex.Match(stripGcode, @"G0 Z([+-]?([0-9]*[.])?[0-9]+) F(\d+)", RegexOptions.IgnoreCase);
                    var waitG4Regex = Regex.Match(stripGcode, @"G4 P(\d+)", RegexOptions.IgnoreCase);
                    var pwmM106Regex = Regex.Match(stripGcode, @"M106 S(\d+)", RegexOptions.IgnoreCase);


                    /*if (currPosRegex.Success)
                    {
                        var posZRegex = currPosRegex.Groups[1].Value;
                        posZ = float.Parse(posZRegex, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        posZ = GetHeightFromLayer(layerIndex);
                    }*/

                    if (moveG0Regex.Success)
                    {
                        float liftHeightTemp = float.Parse(moveG0Regex.Groups[1].Value, CultureInfo.InvariantCulture);
                        float liftSpeedTemp = float.Parse(moveG0Regex.Groups[3].Value, CultureInfo.InvariantCulture);
                        moveG0Regex = moveG0Regex.NextMatch();
                        if (moveG0Regex.Success)
                        {
                            float retractHeight = float.Parse(moveG0Regex.Groups[1].Value, CultureInfo.InvariantCulture);
                            retractSpeed = float.Parse(moveG0Regex.Groups[3].Value, CultureInfo.InvariantCulture);
                            liftHeight = (float) Math.Round(liftHeightTemp - retractHeight, 2);
                            liftSpeed = liftSpeedTemp;
                            lastPostZ = posZ = retractHeight;
                        }
                        else
                        {
                            lastPostZ = posZ = liftHeightTemp;
                        }
                    }

                    if (pwmM106Regex.Success)
                    {
                        pwm = byte.Parse(pwmM106Regex.Groups[1].Value);
                    }
                    if (layerIndex == 0)
                    {
                        HeaderSettings.BottomLightPWM = pwm;
                    }
                    /*else if(layerIndex)
                    {
                        HeaderSettings.LightPWM = byte.Parse(pwmM106Regex.Groups[1].Value);
                    }*/

                    if (waitG4Regex.Success)
                    {
                        lightOffDelay = (float) Math.Round(float.Parse(waitG4Regex.Groups[1].Value, CultureInfo.InvariantCulture) / 1000f, 2);
                        waitG4Regex = waitG4Regex.NextMatch();
                        if (waitG4Regex.Success)
                        {
                            exposureTime = (float) Math.Round(float.Parse(waitG4Regex.Groups[1].Value, CultureInfo.InvariantCulture) / 1000f, 2);
                        }
                        else // Only one match, meaning light off delay is not present
                        {
                            lightOffDelay = GetInitialLayerValueOrNormal(layerIndex, BottomLayerOffTime, LayerOffTime);
                        }
                    }

                    this[layerIndex] = new Layer(layerIndex, entry.Open(), LayerManager)
                    {
                        PositionZ = posZ,
                        ExposureTime = exposureTime,
                        LiftHeight = liftHeight,
                        LiftSpeed = liftSpeed,
                        RetractSpeed = retractSpeed,
                        LayerOffTime = lightOffDelay,
                        LightPWM = pwm,
                    };
                    progress++;
                }

                if (GCode is null) // PHZ file
                {
                    LayerManager.RebuildLayersProperties();
                }

                if (HeaderSettings.LayerCount > 0 && ResolutionX == 0)
                {
                    using (var mat = this[0].LayerMat)
                    {
                        HeaderSettings.ResolutionX = (uint)mat.Width;
                        HeaderSettings.ResolutionY = (uint)mat.Height;
                    }
                }

                entry = inputFile.GetEntry("preview.png");
                if (!ReferenceEquals(entry, null))
                {
                    Thumbnails[0] = new Mat();
                    CvInvoke.Imdecode(entry.Open().ToArray(), ImreadModes.AnyColor, Thumbnails[0]);
                }

                entry = inputFile.GetEntry("preview_cropping.png");
                if (!ReferenceEquals(entry, null))
                {
                    var count = CreatedThumbnailsCount;
                    Thumbnails[count] = new Mat();
                    CvInvoke.Imdecode(entry.Open().ToArray(), ImreadModes.AnyColor, Thumbnails[count]);
                }
            }

            LayerManager.GetBoundingRectangle(progress);
        }

        public override void RebuildGCode()
        {
            if (IsPHZZip) return;
            string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
            GCode = new StringBuilder();
            GCode.AppendLine($"; {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.Now}");

            foreach (var propertyInfo in HeaderSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                if (displayNameAttribute is null) continue;
                GCode.AppendLine($";{displayNameAttribute.DisplayName}:{propertyInfo.GetValue(HeaderSettings)}");
            }

            GCode.AppendLine();
            GCode.AppendFormat(GCodeStart, Environment.NewLine);

            float lastZPosition = 0;

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                var exposureTime = layer.ExposureTime * 1000;
                var liftHeight = layer.LiftHeight;
                var liftZHeight = Math.Round(liftHeight + layer.PositionZ, 2);
                var liftSpeed = layer.LiftSpeed;
                var retractSpeed = layer.RetractSpeed;
                var lightOffDelay = layer.LayerOffTime * 1000;
                var pwmValue = layer.LightPWM;

                GCode.AppendLine($";LAYER_START:{layerIndex}");
                GCode.AppendLine($";currPos:{layer.PositionZ}");
                if (layer.ExposureTime > 0 && pwmValue > 0)
                {
                    GCode.AppendLine($"M6054 \"{layerIndex + 1}.png\";show Image");
                }

                // Absolute gcode
                if (liftHeight > 0 && liftZHeight > layer.PositionZ)
                {
                    GCode.AppendLine($"G0 Z{liftZHeight} F{liftSpeed};Z Lift");
                    GCode.AppendLine($"G0 Z{layer.PositionZ} F{retractSpeed};Layer position");
                }
                else if (lastZPosition < layer.PositionZ)
                    GCode.AppendLine($"G0 Z{layer.PositionZ} F{retractSpeed};Layer position");

                GCode.AppendLine($"G4 P{lightOffDelay};Stabilization delay");

                if (layer.ExposureTime > 0 && pwmValue > 0)
                {
                    GCode.AppendLine($"M106 S{pwmValue};light on");
                    GCode.AppendLine($"G4 P{exposureTime};Cure time");
                    GCode.AppendLine("M106 S0;light off");
                }

                GCode.AppendLine(";LAYER_END");
                GCode.AppendLine();

                lastZPosition = layer.PositionZ;
            }

            GCode.AppendFormat(GCodeEnd, Environment.NewLine, HeaderSettings.MachineZ);
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
                foreach (var zipentry in outputFile.Entries)
                {
                    if (zipentry.Name.EndsWith(".gcode"))
                    {
                        zipentry.Delete();
                        break;
                    }
                }

                if (!IsPHZZip)
                {
                    outputFile.PutFileContent("run.gcode", GCode.ToString(), ZipArchiveMode.Update);
                }
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
                            BedSizeX = HeaderSettings.MachineX,
                            BedSizeY = HeaderSettings.MachineY,
                            BedSizeZ = HeaderSettings.MachineZ,
                            OverallHeightMilimeter = TotalHeight,
                            BottomExposureSeconds = BottomExposureTime,
                            BottomLayersCount = BottomLayerCount,
                            BottomLightPWM = HeaderSettings.BottomLightPWM,
                            LayerCount = LayerCount,
                            LayerExposureSeconds = ExposureTime,
                            LayerHeightMilimeter = LayerHeight,
                            LayerOffTime = HeaderSettings.LightOffTime,
                            LightPWM = HeaderSettings.LightPWM,
                            PrintTime = (uint) HeaderSettings.EstimatedPrintTime,
                            ProjectorType = HeaderSettings.Mirror,
                            ResolutionX = ResolutionX,
                            ResolutionY = ResolutionY,
                            AntiAliasLevel = ValidateAntiAliasingLevel()
                        },
                    PrintParametersSettings =
                    {
                        BottomLayerCount = BottomLayerCount,
                        BottomLiftHeight = HeaderSettings.BottomLiftHeight,
                        BottomLiftSpeed = HeaderSettings.BottomLiftSpeed,
                        BottomLightOffDelay = HeaderSettings.BottomLightOffTime,
                        CostDollars = MaterialCost,
                        LiftHeight = HeaderSettings.LiftHeight,
                        LiftSpeed = HeaderSettings.LiftSpeed,
                        LightOffDelay = HeaderSettings.LightOffTime,
                        RetractSpeed = HeaderSettings.RetractSpeed,
                        VolumeMl = UsedMaterial,
                        WeightG = HeaderSettings.Weight
                    },
                    SlicerInfoSettings = { MachineName = MachineName, MachineNameSize = (uint)MachineName.Length }
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
                        BedSizeX = HeaderSettings.MachineX,
                        BedSizeY = HeaderSettings.MachineY,
                        BedSizeZ = HeaderSettings.MachineZ,
                        OverallHeightMilimeter = TotalHeight,
                        BottomExposureSeconds = BottomExposureTime,
                        BottomLayersCount = BottomLayerCount,
                        BottomLightPWM = HeaderSettings.BottomLightPWM,
                        LayerCount = LayerCount,
                        LayerExposureSeconds = ExposureTime,
                        LayerHeightMilimeter = LayerHeight,
                        LayerOffTime = HeaderSettings.LightOffTime,
                        LightPWM = HeaderSettings.LightPWM,
                        PrintTime = (uint) HeaderSettings.EstimatedPrintTime,
                        ProjectorType = HeaderSettings.Mirror,
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        BottomLayerCount = BottomLayerCount,
                        BottomLiftHeight = HeaderSettings.BottomLiftHeight,
                        BottomLiftSpeed = HeaderSettings.BottomLiftSpeed,
                        BottomLightOffDelay = HeaderSettings.BottomLightOffTime,
                        CostDollars = MaterialCost,
                        LiftHeight = HeaderSettings.LiftHeight,
                        LiftSpeed = HeaderSettings.LiftSpeed,
                        RetractSpeed = HeaderSettings.RetractSpeed,
                        VolumeMl = UsedMaterial,
                        AntiAliasLevelInfo = ValidateAntiAliasingLevel(),
                        WeightG = HeaderSettings.Weight,
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
                        LayerOffTime = HeaderSettings.LightOffTime,
                        BottomLayersCount = BottomLayerCount,
                        BottomExposureSeconds = BottomExposureTime,
                        Price = MaterialCost,
                        Volume = UsedMaterial,
                        Weight = HeaderSettings.Weight,
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
                        ExposureOffTime = (uint)HeaderSettings.LightOffTime,
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
                        ZLiftDistance = HeaderSettings.LiftHeight,
                        ZLiftFeedRate = HeaderSettings.LiftSpeed,
                        ZLiftRetractRate = HeaderSettings.RetractSpeed,
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

                file.SliceSettings.Xppm = file.OutputSettings.PixPermmX = (float)Math.Round(ResolutionX / HeaderSettings.MachineX, 3);
                file.SliceSettings.Yppm = file.OutputSettings.PixPermmY = (float)Math.Round(ResolutionY / HeaderSettings.MachineY, 3);
                file.SliceSettings.Xres = file.OutputSettings.XResolution = (ushort)ResolutionX;
                file.SliceSettings.Yres = file.OutputSettings.YResolution = (ushort)ResolutionY;
                file.SliceSettings.Thickness = file.OutputSettings.LayerThickness = LayerHeight;
                file.SliceSettings.LayersNum = file.OutputSettings.LayersNum = LayerCount;
                file.SliceSettings.HeadLayersNum = file.OutputSettings.NumberBottomLayers = BottomLayerCount;
                file.SliceSettings.LayersExpoMs = file.OutputSettings.LayerTime = (uint)ExposureTime * 1000;
                file.SliceSettings.HeadLayersExpoMs = file.OutputSettings.BottomLayersTime = (uint)BottomExposureTime * 1000;
                file.SliceSettings.WaitBeforeExpoMs = (uint)(HeaderSettings.LightOffTime * 1000);
                file.SliceSettings.LiftDistance = file.OutputSettings.LiftDistance = LiftHeight;
                file.SliceSettings.LiftUpSpeed = file.OutputSettings.ZLiftFeedRate = LiftSpeed;
                file.SliceSettings.LiftDownSpeed = file.OutputSettings.ZLiftRetractRate = RetractSpeed;
                file.SliceSettings.LiftWhenFinished = defaultFormat.SliceSettings.LiftWhenFinished;

                file.OutputSettings.BlankingLayerTime = (uint)(HeaderSettings.LightOffTime * 1000);
                //file.OutputSettings.RenderOutlines = false;
                //file.OutputSettings.OutlineWidthInset = 0;
                //file.OutputSettings.OutlineWidthOutset = 0;
                file.OutputSettings.RenderOutlines = false;
                //file.OutputSettings.TiltValue = 0;
                //file.OutputSettings.UseMainliftGCodeTab = false;
                //file.OutputSettings.AntiAliasing = 0;
                //file.OutputSettings.AntiAliasingValue = 0;
                file.OutputSettings.FlipX = HeaderSettings.Mirror != 0;
                file.OutputSettings.FlipY = file.OutputSettings.FlipX;
                file.OutputSettings.AntiAliasingValue = ValidateAntiAliasingLevel();
                file.OutputSettings.AntiAliasing = file.OutputSettings.AntiAliasingValue > 1;

                file.Printer = MachineName.Contains("Bene4 Mono") ||
                               FileFullPath.Contains("bene4_mono")
                    ? CWSFile.PrinterType.BeneMono : CWSFile.PrinterType.Elfin;

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
                                    X = HeaderSettings.MachineX,
                                    Y = HeaderSettings.MachineY,
                                },
                                LayerHeight = LayerHeight,
                                Layers = LayerCount
                            },
                            Bottom = new UVJFile.Bottom
                            {
                                LiftHeight = HeaderSettings.BottomLiftHeight,
                                LiftSpeed = HeaderSettings.BottomLiftSpeed,
                                LightOnTime = BottomExposureTime,
                                LightOffTime = HeaderSettings.BottomLightOffTime,
                                LightPWM = HeaderSettings.BottomLightPWM,
                                RetractSpeed = HeaderSettings.RetractSpeed,
                                Count = BottomLayerCount
                                //RetractHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.JsonSettings.Properties.Bottom.RetractHeight),
                            },
                            Exposure = new UVJFile.Exposure
                            {
                                LiftHeight = HeaderSettings.LiftHeight,
                                LiftSpeed = HeaderSettings.LiftSpeed,
                                LightOnTime = ExposureTime,
                                LightOffTime = HeaderSettings.LightOffTime,
                                LightPWM = HeaderSettings.LightPWM,
                                RetractSpeed = HeaderSettings.RetractSpeed,
                            },
                            AntiAliasLevel = ValidateAntiAliasingLevel()
                        }
                    }
                };

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            return false;
        }

        #endregion
    }
}
