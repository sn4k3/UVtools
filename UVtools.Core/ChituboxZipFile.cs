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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using UVtools.Core.Extensions;

namespace UVtools.Core
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
            [DisplayName("normalLayerLiftSpeed")] public float LayerLiftSpeed { get; set; } = 60; // 60 mm/m
            [DisplayName("normalLayerLiftHeight")] public float LayerLiftHeight { get; set; } = 5; // 5 mm
            [DisplayName("zSlowUpDistance")] public float ZSlowUpDistance { get; set; }
            [DisplayName("bottomLayCount")] public ushort BottomLayCount { get; set; } = 4;
            [DisplayName("bottomLayerCount")] public ushort BottomLayerCount { get; set; } = 4;
            [DisplayName("mirror")] public byte Mirror { get; set; } // 0/1
            [DisplayName("totalLayer")] public uint LayerCount { get; set; }
            [DisplayName("bottomLayerLiftHeight")] public float BottomLayerLiftHeight { get; set; } = 5;
            [DisplayName("bottomLayerLiftSpeed")] public float BottomLayerLiftSpeed { get; set; } = 60;
            [DisplayName("bottomLightOffTime")] public float BottomLightOffTime { get; set; }
            [DisplayName("lightOffTime")] public float LayerLightOffTime { get; set; }
            [DisplayName("bottomPWMLight")] public byte BottomLightPWM { get; set; } = 255;
            [DisplayName("PWMLight")] public byte LayerLightPWM { get; set; } = 255;
            [DisplayName("antiAliasLevel")] public byte AntiAliasing { get; set; } = 1;
        }

        #endregion

        #region Properties
        public Header HeaderSettings { get; } = new Header();

        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("zip", "Chitubox Zip Files")
        };

        public override Type[] ConvertToFormats { get; } = null;

        public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
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

        public override byte ThumbnailsCount { get; } = 2;

        public override Size[] ThumbnailsOriginalSize { get; } = {new Size(954, 850), new Size(168, 150)};

        public override uint ResolutionX => HeaderSettings.ResolutionX;

        public override uint ResolutionY => HeaderSettings.ResolutionY;
        public override byte AntiAliasing => HeaderSettings.AntiAliasing;

        public override float LayerHeight => HeaderSettings.LayerHeight;

        public override ushort InitialLayerCount => HeaderSettings.BottomLayerCount;

        public override float InitialExposureTime => HeaderSettings.BottomLayerExposureTime;

        public override float LayerExposureTime => HeaderSettings.LayerExposureTime;

        public override float LiftHeight => HeaderSettings.LayerLiftHeight;

        public override float LiftSpeed => HeaderSettings.LayerLiftSpeed;

        public override float RetractSpeed => HeaderSettings.RetractSpeed;

        public override float PrintTime => HeaderSettings.EstimatedPrintTime;

        public override float UsedMaterial => HeaderSettings.Weight;

        public override float MaterialCost => HeaderSettings.Price;

        public override string MaterialName => HeaderSettings.Resin;

        public override string MachineName => HeaderSettings.MachineType;

        public override object[] Configs => new object[] { HeaderSettings };
        #endregion

        #region Methods

        public override void Clear()
        {
            base.Clear();
            GCode = null;
        }

        public override void Encode(string fileFullPath)
        {
            base.Encode(fileFullPath);
            using (ZipArchive outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Update))
            {
                for(uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    Layer layer = this[layerIndex];
                    outputFile.PutFileContent($"{layerIndex+1}.png", layer.CompressedBytes);
                }

                if (Thumbnails.Length > 0 && !ReferenceEquals(Thumbnails[0], null))
                {
                    using (Stream stream = outputFile.CreateEntry("preview.png").Open())
                    {
                        var vec = new VectorOfByte();
                        CvInvoke.Imencode(".png", Thumbnails[0], vec);
                        stream.WriteBytes(vec.ToArray());
                        stream.Close();
                    }
                }

                if (Thumbnails.Length > 1 && !ReferenceEquals(Thumbnails[1], null))
                {
                    using (Stream stream = outputFile.CreateEntry("preview_cropping.png").Open())
                    {
                        var vec = new VectorOfByte();
                        CvInvoke.Imencode(".png", Thumbnails[1], vec);
                        stream.WriteBytes(vec.ToArray());
                        stream.Close();
                    }
                }

                UpdateGCode();
                outputFile.PutFileContent("run.gcode", GCode.ToString());
            }
        }

        public override void Decode(string fileFullPath)
        {
            base.Decode(fileFullPath);

            FileFullPath = fileFullPath;
            using (var inputFile = ZipFile.Open(FileFullPath, ZipArchiveMode.Read))
            {
                var entry = inputFile.GetEntry("run.gcode");
                if (ReferenceEquals(entry, null))
                {
                    Clear();
                    throw new FileLoadException("run.gcode not found", fileFullPath);
                }

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


                LayerManager = new LayerManager(HeaderSettings.LayerCount);

                for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    entry = inputFile.GetEntry($"{layerIndex+1}.png");
                    if (ReferenceEquals(entry, null))
                    {
                        Clear();
                        throw new FileLoadException($"Layer {layerIndex+1} not found", fileFullPath);
                    }

                    LayerManager[layerIndex] = new Layer(layerIndex, entry.Open(), entry.Name);
                }

                entry = inputFile.GetEntry("preview.png");
                if (!ReferenceEquals(entry, null))
                {
                    CvInvoke.Imdecode(entry.Open().ToArray(), ImreadModes.AnyColor, Thumbnails[0]);
                }

                entry = inputFile.GetEntry("preview_cropping.png");
                if (!ReferenceEquals(entry, null))
                {
                    CvInvoke.Imdecode(entry.Open().ToArray(), ImreadModes.AnyColor, Thumbnails[CreatedThumbnailsCount]);
                }
            }

            var rect = LayerManager.BoundingRectangle;
        }

        public override object GetValueFromPrintParameterModifier(PrintParameterModifier modifier)
        {
            var baseValue = base.GetValueFromPrintParameterModifier(modifier);
            if (!ReferenceEquals(baseValue, null)) return baseValue;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerOffTime)) return HeaderSettings.BottomLightOffTime;
            if (ReferenceEquals(modifier, PrintParameterModifier.LayerOffTime)) return HeaderSettings.LayerLightOffTime;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight)) return HeaderSettings.BottomLayerLiftHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed)) return HeaderSettings.BottomLayerLiftSpeed;
            /*if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight)) return PrintParametersSettings.LiftHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed)) return PrintParametersSettings.LiftingSpeed;
            if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed)) return PrintParametersSettings.RetractSpeed;*/

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightPWM)) return HeaderSettings.BottomLightPWM;
            if (ReferenceEquals(modifier, PrintParameterModifier.LightPWM)) return HeaderSettings.LayerLightPWM;



            return null;
        }

        public override bool SetValueFromPrintParameterModifier(PrintParameterModifier modifier, string value)
        {
            if (ReferenceEquals(modifier, PrintParameterModifier.InitialLayerCount))
            {
                HeaderSettings.BottomLayerCount =
                    HeaderSettings.BottomLayCount = value.Convert<ushort>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.InitialExposureSeconds))
            {
                HeaderSettings.BottomLayerExposureTime = value.Convert<float>();
                UpdateGCode();
                return true;
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.ExposureSeconds))
            {
                HeaderSettings.LayerExposureTime = value.Convert<float>();
                UpdateGCode();
                return true;
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerOffTime))
            {
                HeaderSettings.BottomLightOffTime = value.Convert<float>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LayerOffTime))
            {
                HeaderSettings.LayerLightOffTime = value.Convert<float>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight))
            {
                HeaderSettings.BottomLayerLiftHeight = value.Convert<float>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed))
            {
                HeaderSettings.LayerLiftSpeed = value.Convert<float>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight))
            {
                HeaderSettings.LayerLiftHeight = value.Convert<float>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed))
            {
                HeaderSettings.LayerLiftSpeed = value.Convert<float>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed))
            {
                HeaderSettings.RetractSpeed = value.Convert<float>();
                UpdateGCode();
                return true;
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightPWM))
            {
                HeaderSettings.BottomLightPWM = value.Convert<byte>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LightPWM))
            {
                HeaderSettings.LayerLightPWM = value.Convert<byte>();
                UpdateGCode();
                return true;
            }

            return false;
        }

        public override void SaveAs(string filePath = null)
        {
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

                outputFile.PutFileContent("run.gcode", GCode.ToString());

                foreach (var layer in this)
                {
                    if (!layer.IsModified) continue;
                    outputFile.PutFileContent(layer.Filename, layer.CompressedBytes);
                    layer.IsModified = false;
                }
            }

            //Decode(FileFullPath);
        }

        public override bool Convert(Type to, string fileFullPath)
        {
            throw new NotImplementedException();
        }

        private void UpdateGCode()
        {
            string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
            GCode = new StringBuilder();
            GCode.AppendLine($"; {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.Now}"); 

            foreach (var propertyInfo in HeaderSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                if (ReferenceEquals(displayNameAttribute, null)) continue;
                GCode.AppendLine($";{displayNameAttribute.DisplayName}:{propertyInfo.GetValue(HeaderSettings)}");
            }

            GCode.AppendLine();
            GCode.AppendFormat(GCodeStart, Environment.NewLine);

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                GCode.AppendLine($";LAYER_START:{layerIndex}");
                GCode.AppendLine($";currPos:{GetHeightFromLayer(layerIndex, false)}");
                GCode.AppendLine($"M6054 \"{layerIndex+1}.png\";show Image");
                GCode.AppendLine($"G0 Z{(layerIndex < InitialLayerCount ? HeaderSettings.BottomLayerLiftHeight : HeaderSettings.LayerLiftHeight) +GetHeightFromLayer(layerIndex, false)} F{(layerIndex < InitialLayerCount ? HeaderSettings.BottomLayerLiftSpeed : HeaderSettings.LayerLiftSpeed)};Z Lift");
                GCode.AppendLine($"G0 Z{GetHeightFromLayer(layerIndex)} F{HeaderSettings.RetractSpeed};Layer position");
                GCode.AppendLine($"G4 P{(layerIndex < InitialLayerCount ? HeaderSettings.BottomLightOffTime : HeaderSettings.LayerLightOffTime)*1000};Before cure delay");
                GCode.AppendLine($"M106 S{(layerIndex < InitialLayerCount ? HeaderSettings.BottomLightPWM : HeaderSettings.LayerLightPWM)};light on");
                GCode.AppendLine($"G4 P{(layerIndex < InitialLayerCount ? HeaderSettings.BottomLayerExposureTime : HeaderSettings.LayerExposureTime) * 1000};Cure time");
                GCode.AppendLine("M106 S0;light off");
                GCode.AppendLine(";LAYER_END");
                GCode.AppendLine();
            }

            GCode.AppendFormat(GCodeEnd, Environment.NewLine, HeaderSettings.MachineZ);
        }
        #endregion
    }
}
