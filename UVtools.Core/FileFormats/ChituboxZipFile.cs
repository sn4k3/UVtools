/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
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
            [DisplayName("volume")] public float VolumeMl { get; set; }
            [DisplayName("resin")] public string Resin { get; set; } = "Normal";
            [DisplayName("weight")] public float WeightG { get; set; }
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
            new("zip", "Chitubox Zip")
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
            PrintParameterModifier.ExposureSeconds,

            PrintParameterModifier.BottomLightOffDelay,
            PrintParameterModifier.LightOffDelay,
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
            PrintParameterModifier.LightOffDelay,
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
                HeaderSettings.MachineY = value;
                RaisePropertyChanged();
            }
        }

        public override float MaxPrintHeight
        {
            get => HeaderSettings.MachineZ > 0 ? HeaderSettings.MachineZ : base.MaxPrintHeight;
            set
            {
                HeaderSettings.MachineZ = value;
                RaisePropertyChanged();
            }
        }

        public override bool MirrorDisplay
        {
            get => HeaderSettings.Mirror > 0;
            set
            {
                HeaderSettings.ProjectType = value ? "LCD_mirror" : "Normal";
                HeaderSettings.Mirror = value ? 1 : 0;
                RaisePropertyChanged();
            }
        }

        public override byte AntiAliasing
        {
            get => HeaderSettings.AntiAliasing;
            set
            {
                HeaderSettings.AntiAliasing = value.Clamp(1, 16);
                RaisePropertyChanged();
            }
        }

        public override float LayerHeight
        {
            get => HeaderSettings.LayerHeight;
            set
            {
                HeaderSettings.LayerHeight = (float)Math.Round(value, 2);
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
                HeaderSettings.BottomLayerExposureTime = HeaderSettings.BottomLayExposureTime = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float ExposureTime
        {
            get => HeaderSettings.LayerExposureTime;
            set
            {
                HeaderSettings.LayerExposureTime = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float BottomLightOffDelay
        {
            get => HeaderSettings.BottomLightOffTime;
            set
            {
                HeaderSettings.BottomLightOffTime = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float LightOffDelay
        {
            get => HeaderSettings.LightOffTime;
            set
            {
                HeaderSettings.LightOffTime = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float BottomLiftHeight
        {
            get => HeaderSettings.BottomLiftHeight;
            set
            {
                HeaderSettings.BottomLiftHeight = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float LiftHeight
        {
            get => HeaderSettings.LiftHeight;
            set
            {
                HeaderSettings.LiftHeight = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float BottomLiftSpeed
        {
            get => HeaderSettings.BottomLiftSpeed;
            set
            {
                HeaderSettings.BottomLiftSpeed = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float LiftSpeed
        {
            get => HeaderSettings.LiftSpeed;
            set
            {
                HeaderSettings.LiftSpeed = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float RetractSpeed
        {
            get => HeaderSettings.RetractSpeed;
            set
            {
                HeaderSettings.RetractSpeed = (float)Math.Round(value, 2);
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
            get => base.PrintTime;
            set
            {
                base.PrintTime = value;
                HeaderSettings.EstimatedPrintTime = base.PrintTime;
            }
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

        public override float MaterialGrams
        {
            get => (float) Math.Round(HeaderSettings.WeightG, 3);
            set
            {
                HeaderSettings.WeightG = (float)Math.Round(value, 3);
                RaisePropertyChanged();
            }
        }

        public override float MaterialCost
        {
            get => (float) Math.Round(HeaderSettings.Price, 3);
            set
            {
                HeaderSettings.Price = (float)Math.Round(value, 3);
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

        public bool IsPHZZip;
        #endregion

        #region Methods

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
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
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using (var inputFile = ZipFile.Open(FileFullPath, ZipArchiveMode.Read))
            {
                var entry = inputFile.GetEntry("run.gcode");
                if (entry is not null)
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
                    gcode = gcode.Substring(gcode.IndexOf(startStr, StringComparison.InvariantCultureIgnoreCase) + startStr.Length);
                    var stripGcode = gcode.Substring(0, gcode.IndexOf(";LAYER_END")).Trim(' ', '\n', '\r', '\t');
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
                            lightOffDelay = GetInitialLayerValueOrNormal(layerIndex, BottomLightOffDelay, LightOffDelay);
                        }
                    }

                    this[layerIndex] = new Layer(layerIndex, entry.Open(), LayerManager)
                    {
                        PositionZ = posZ,
                        ExposureTime = exposureTime,
                        LiftHeight = liftHeight,
                        LiftSpeed = liftSpeed,
                        RetractSpeed = retractSpeed,
                        LightOffDelay = lightOffDelay,
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
                var lightOffDelay = layer.LightOffDelay * 1000;
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
        #endregion
    }
}
