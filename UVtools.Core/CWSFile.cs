/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;

namespace UVtools.Core
{
    public class CWSFile : FileFormat
    {
        #region Constants

        public const string GCodeStart = "G28 ; Auto Home{0}" +
                                          "G21 ;Set units to be mm{0}" +
                                          "G91 ;Relative Positioning{0}" +
                                          "M17 ;Enable motors{0}" +
                                          "<Slice> Blank{0}" +
                                          "M106 S0{0}{0}";

        public const string GCodeEnd = "M106 S0{0}" +
                                       "G1 Z{1}{0}" +
                                       "{0}M18 ;Disable Motors{0}" +
                                       ";<Completed>{0}";

        public const string GCodeKeywordSlice = ";<Slice>";
        public const string GCodeKeywordSliceBlank = ";<Slice> Blank";
        public const string GCodeKeywordDelay = ";<Delay>";
        #endregion

        #region Sub Classes

        public class Output
        {
            // ;(****Build and Slicing Parameters****)
            [DisplayName("Pix per mm X")] public float PixPermmX { get; set; } = 19.324f;
            [DisplayName("Pix per mm Y")] public float PixPermmY { get; set; } = 19.324f;
            [DisplayName("X Resolution")] public ushort XResolution { get; set; }
            [DisplayName("Y Resolution")] public ushort YResolution { get; set; }
            [DisplayName("Layer Thickness")] public float LayerThickness { get; set; }
            [DisplayName("Layer Time")] public uint LayerTime { get; set; } = 5500;
            [DisplayName("Render Outlines")] public bool RenderOutlines { get; set; } = false;
            [DisplayName("Outline Width Inset")] public ushort OutlineWidthInset { get; set; } = 2;
            [DisplayName("Outline Width Outset")] public ushort OutlineWidthOutset { get; set; } = 0;
            [DisplayName("Bottom Layers Time")] public uint BottomLayersTime { get; set; } = 35000;
            [DisplayName("Number of Bottom Layers")] public ushort NumberBottomLayers { get; set; } = 3;
            [DisplayName("Blanking Layer Time")] public uint BlankingLayerTime { get; set; }
            [DisplayName("BuildDirection")] public string BuildDirection { get; set; } = "Bottom_Up";
            [DisplayName("Lift Distance")] public float LiftDistance { get; set; } = 4;
            [DisplayName("Slide/Tilt Value")] public byte TiltValue { get; set; }
            [DisplayName("Use Mainlift GCode Tab")] public bool UseMainliftGCodeTab { get; set; }
            [DisplayName("Anti Aliasing")] public bool AntiAliasing { get; set; } = true;
            [DisplayName("Anti Aliasing Value")] public float AntiAliasingValue { get; set; } = 2;
            [DisplayName("Z Lift Feed Rate")] public float ZLiftFeedRate { get; set; } = 120;
            [DisplayName("Z Bottom Lift Feed Rate")] public float ZBottomLiftFeedRate { get; set; } = 120;
            [DisplayName("Z Lift Retract Rate")] public float ZLiftRetractRate { get; set; } = 120;
            [DisplayName("Flip X")] public bool FlipX { get; set; }
            [DisplayName("Flip Y")] public bool FlipY { get; set; }
            [DisplayName("Number of Slices")] public uint LayersNum { get; set; }

            // ;(****Machine Configuration ******)  
            [DisplayName("Platform X Size")] public float PlatformXSize { get; set; }
            [DisplayName("Platform Y Size")] public float PlatformYSize { get; set; }
            [DisplayName("Platform Z Size")] public float PlatformZSize { get; set; }
            [DisplayName("Max X Feedrate")] public ushort MaxXFeedrate { get; set; } = 200;
            [DisplayName("Max Y Feedrate")] public ushort MaxYFeedrate { get; set; } = 200;
            [DisplayName("Max Z Feedrate")] public ushort MaxZFeedrate { get; set; } = 200;
            [DisplayName("Machine Type")] public string MachineType { get; set; } = "UV_LCD";

            // ;(****UVtools Configuration ******)  
            [DisplayName("Bottom Layer Light PWM")] public byte BottomLayerLightPWM { get; set; } = 255;
            [DisplayName("Layer Light PWM")] public byte LayerLightPWM { get; set; } = 255;
        }

        public class Slice
        {
            [DisplayName("xppm")] public float Xppm { get; set; } = 19.324f;
            [DisplayName("yppm")] public float Yppm { get; set; } = 19.324f;
            [DisplayName("xres")] public ushort Xres { get; set; }
            [DisplayName("yres")] public ushort Yres { get; set; }
            [DisplayName("thickness")] public float Thickness { get; set; }
            [DisplayName("layers_num")] public uint LayersNum { get; set; }
            [DisplayName("head_layers_num")] public ushort HeadLayersNum { get; set; } = 3;
            [DisplayName("layers_expo_ms")] public uint LayersExpoMs { get; set; } = 5500;
            [DisplayName("head_layers_expo_ms")] public uint HeadLayersExpoMs { get; set; } = 35000;
            [DisplayName("wait_before_expo_ms")] public uint WaitBeforeExpoMs { get; set; } = 2000;
            [DisplayName("lift_distance")] public float LiftDistance { get; set; } = 4;
            [DisplayName("lift_up_speed")] public float LiftUpSpeed { get; set; } = 120;
            [DisplayName("lift_down_speed")] public float LiftDownSpeed { get; set; } = 120;
            [DisplayName("lift_when_finished")] public byte LiftWhenFinished { get; set; } = 80;
        }

        #endregion

        #region Properties
        public Slice SliceSettings { get; } = new Slice();
        public Output OutputSettings { get; } = new Output();


        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("cws", "NovaMaker CWS Files")
        };

        public override Type[] ConvertToFormats { get; } = null;

        public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
            PrintParameterModifier.InitialLayerCount,
            PrintParameterModifier.InitialExposureSeconds,
            PrintParameterModifier.ExposureSeconds,


            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.LiftSpeed,

            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,
        };

        public override byte ThumbnailsCount { get; } = 0;

        public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } = null;

        public override uint ResolutionX => SliceSettings.Xres;

        public override uint ResolutionY => SliceSettings.Yres;
        public override byte AntiAliasing => (byte) OutputSettings.AntiAliasingValue;

        public override float LayerHeight => SliceSettings.Thickness;

        public override ushort InitialLayerCount => SliceSettings.HeadLayersNum;

        public override float InitialExposureTime => SliceSettings.HeadLayersExpoMs / 1000f;

        public override float LayerExposureTime => SliceSettings.LayersExpoMs / 1000f;

        public override float LiftHeight => SliceSettings.LiftDistance;

        public override float LiftSpeed => SliceSettings.LiftDownSpeed;

        public override float RetractSpeed => OutputSettings.ZLiftRetractRate;

        public override float PrintTime => 0;

        public override float UsedMaterial => 0;

        public override float MaterialCost => 0;

        public override string MaterialName => string.Empty;

        public override string MachineName => "Unknown";

        public override object[] Configs => new object[] { SliceSettings, OutputSettings};
        #endregion

        #region Methods

        public override void Clear()
        {
            base.Clear();
            GCode = null;
        }

        public override void Encode(string fileFullPath, OperationProgress progress = null)
        {
            base.Encode(fileFullPath, progress);
            using (ZipArchive outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create))
            {
                string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
                var entry = outputFile.CreateEntry("slice.conf");
                var stream = entry.Open();

                using (TextWriter tw = new StreamWriter(stream))
                {
                    
                    tw.WriteLine($"# {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.Now}");
                    tw.WriteLine("# conf version 1.0");
                    tw.WriteLine("");

                    foreach (var propertyInfo in SliceSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                        if (ReferenceEquals(displayNameAttribute, null)) continue;
                        tw.WriteLine($"{displayNameAttribute.DisplayName.PadRight(24)}= {propertyInfo.GetValue(SliceSettings)}");
                    }
                }
                

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    Layer layer = this[layerIndex];
                    var layerImagePath = $"{Path.GetFileNameWithoutExtension(fileFullPath)}{layer.Index.ToString().PadLeft(LayerCount.ToString().Length, '0')}.png";
                    outputFile.PutFileContent(layerImagePath, layer.CompressedBytes, ZipArchiveMode.Create);
                    progress++;
                }

                UpdateGCode();
                outputFile.PutFileContent($"{Path.GetFileNameWithoutExtension(fileFullPath)}.gcode", GCode.ToString(), ZipArchiveMode.Create);
            }
        }

        public override void Decode(string fileFullPath, OperationProgress progress = null)
        {
            base.Decode(fileFullPath, progress);

            FileFullPath = fileFullPath;
            using (var inputFile = ZipFile.Open(FileFullPath, ZipArchiveMode.Read))
            {
                var entry = inputFile.GetEntry("slice.conf");
                if (ReferenceEquals(entry, null))
                {
                    Clear();
                    throw new FileLoadException("slice.conf not found", fileFullPath);
                }

                

                using (TextReader tr = new StreamReader(entry.Open()))
                {
                    string line;
                    while ((line = tr.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        if(line[0] == '#') continue;

                        var splitLine = line.Split('=');
                        if(splitLine.Length < 2) continue;

                        foreach (var propertyInfo in SliceSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                            if(ReferenceEquals(displayNameAttribute, null)) continue;
                            if(!splitLine[0].Trim().Equals(displayNameAttribute.DisplayName)) continue;
                            Helpers.SetPropertyValue(propertyInfo, SliceSettings, splitLine[1].Trim());
                        }
                    }
                    tr.Close();
                }

                entry = inputFile.GetEntry($"{Path.GetFileNameWithoutExtension(fileFullPath)}.gcode");
                if (ReferenceEquals(entry, null))
                {
                    Clear();
                    throw new FileLoadException($"{Path.GetFileNameWithoutExtension(fileFullPath)}.gcode not found", fileFullPath);
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

                        var splitLine = line.Split('=');
                        if (splitLine.Length < 2) continue;

                        foreach (var propertyInfo in OutputSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        {
                            var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                            if (ReferenceEquals(displayNameAttribute, null)) continue;
                            if (!splitLine[0].Trim(' ', ';', '(').Equals(displayNameAttribute.DisplayName)) continue;
                            Helpers.SetPropertyValue(propertyInfo, OutputSettings, splitLine[1].Trim(' ', ')', 'm', 'n', 's', '/'));
                            //Debug.WriteLine(splitLine[1].Trim(' ', ')', 'm', 'n', '/'));
                        }
                    }
                    tr.Close();
                }


                LayerManager = new LayerManager(OutputSettings.LayersNum);

                foreach (var zipArchiveEntry in inputFile.Entries)
                {
                    if (!zipArchiveEntry.Name.EndsWith(".png")) continue;

                    // - .png - 4 numbers
                    int layerSize = OutputSettings.LayersNum.ToString().Length;
                    string layerStr = zipArchiveEntry.Name.Substring(zipArchiveEntry.Name.Length - 4 - layerSize, layerSize);
                    uint iLayer = uint.Parse(layerStr);
                    LayerManager[iLayer] = new Layer(iLayer, zipArchiveEntry.Open(), zipArchiveEntry.Name);
                }
            }

            LayerManager.GetBoundingRectangle(progress);
        }

        public override object GetValueFromPrintParameterModifier(PrintParameterModifier modifier)
        {
            var baseValue = base.GetValueFromPrintParameterModifier(modifier);
            if (!ReferenceEquals(baseValue, null)) return baseValue;

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightPWM)) return OutputSettings.BottomLayerLightPWM;
            if (ReferenceEquals(modifier, PrintParameterModifier.LightPWM)) return OutputSettings.LayerLightPWM;

            return null;
        }

        public override bool SetValueFromPrintParameterModifier(PrintParameterModifier modifier, string value)
        {
            if (ReferenceEquals(modifier, PrintParameterModifier.InitialLayerCount))
            {
                SliceSettings.HeadLayersNum =
                OutputSettings.NumberBottomLayers = value.Convert<ushort>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.InitialExposureSeconds))
            {
                SliceSettings.HeadLayersExpoMs = 
                OutputSettings.BottomLayersTime = value.Convert<uint>()*1000;
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.ExposureSeconds))
            {
                SliceSettings.LayersExpoMs =
                OutputSettings.LayerTime = value.Convert<uint>() * 1000;
                UpdateGCode();
                return true;
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight))
            {
                SliceSettings.LiftDistance =
                OutputSettings.LiftDistance = value.Convert<byte>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed))
            {
                SliceSettings.LiftUpSpeed = 
                OutputSettings.ZLiftFeedRate = value.Convert<float>();
                UpdateGCode();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed))
            {
                SliceSettings.LiftDownSpeed =
                OutputSettings.ZLiftRetractRate =
                OutputSettings.ZBottomLiftFeedRate = value.Convert<float>();
                UpdateGCode();
                return true;
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightPWM))
            {
                OutputSettings.BottomLayerLightPWM = value.Convert<byte>();
                UpdateGCode();
                return true;
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.LightPWM))
            {
                OutputSettings.BottomLayerLightPWM = value.Convert<byte>();
                UpdateGCode();
                return true;
            }

            return false;
        }

        public override void SaveAs(string filePath = null, OperationProgress progress = null)
        {
            if (!string.IsNullOrEmpty(filePath))
            {
                File.Copy(FileFullPath, filePath, true);
                FileFullPath = filePath;
            }

            using (var outputFile = ZipFile.Open(FileFullPath, ZipArchiveMode.Update))
            {
                string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
                var entry = outputFile.GetPutFile("slice.conf");
                var stream = entry.Open();
                stream.SetLength(0);

                using (TextWriter tw = new StreamWriter(stream))
                {

                    tw.WriteLine($"# {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.Now}");
                    tw.WriteLine("# conf version 1.0");
                    tw.WriteLine("");

                    foreach (var propertyInfo in SliceSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                        if (ReferenceEquals(displayNameAttribute, null)) continue;
                        tw.WriteLine($"{displayNameAttribute.DisplayName.PadRight(24)}= {propertyInfo.GetValue(SliceSettings)}");
                    }
                }


                foreach (var zipentry in outputFile.Entries)
                {
                    if (zipentry.Name.EndsWith(".gcode"))
                    {
                        zipentry.Delete();
                        break;
                    }
                }
                outputFile.PutFileContent($"{Path.GetFileNameWithoutExtension(FileFullPath)}.gcode", GCode.ToString(), ZipArchiveMode.Update);

                foreach (var layer in this)
                {
                    if (!layer.IsModified) continue;
                    outputFile.PutFileContent(layer.Filename, layer.CompressedBytes, ZipArchiveMode.Update);
                    layer.IsModified = false;
                }
            }

            //Decode(FileFullPath, progress);
        }

        public override bool Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
            throw new NotImplementedException();
        }

        private void UpdateGCode()
        {
            string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
            GCode = new StringBuilder();
            GCode.AppendLine($"; {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.Now}"); 
            GCode.AppendLine("(****Build and Slicing Parameters * ***)");

            foreach (var propertyInfo in OutputSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                if (ReferenceEquals(displayNameAttribute, null)) continue;
                GCode.AppendLine($";({displayNameAttribute.DisplayName.PadRight(24)} = {propertyInfo.GetValue(OutputSettings)})");
            }
            GCode.AppendLine();
            GCode.AppendFormat(GCodeStart, Environment.NewLine);

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                //Layer layer = this[layerIndex];
                GCode.AppendLine($"{GCodeKeywordSlice} {layerIndex}");
                GCode.AppendLine($"M106 S{GetInitialLayerValueOrNormal(layerIndex, OutputSettings.BottomLayerLightPWM, OutputSettings.LayerLightPWM)}");
                GCode.AppendLine($"{GCodeKeywordDelay} {GetInitialLayerValueOrNormal(layerIndex, SliceSettings.HeadLayersExpoMs, SliceSettings.LayersExpoMs)}");
                GCode.AppendLine("M106 S0");
                GCode.AppendLine(GCodeKeywordSliceBlank);
                GCode.AppendLine($"G1 Z{LiftHeight} F{LiftSpeed}");
                GCode.AppendLine($"G1 Z-{LiftHeight - LayerHeight} F{RetractSpeed}");
                GCode.AppendLine($"{GCodeKeywordDelay} {GetInitialLayerValueOrNormal(layerIndex, SliceSettings.HeadLayersExpoMs, SliceSettings.LayersExpoMs)}");
            }

            GCode.AppendFormat(GCodeEnd, Environment.NewLine, SliceSettings.LiftWhenFinished);

            /*GCode = Regex.Replace(GCode, @"Z[+]?([0-9]*\.[0-9]+|[0-9]+) F[+]?([0-9]*\.[0-9]+|[0-9]+)",
                $"Z{SliceSettings.LiftDistance} F{SliceSettings.LiftUpSpeed}");

            GCode = Regex.Replace(GCode, @"Z-[-]?([0-9]*\.[0-9]+|[0-9]+) F[+]?([0-9]*\.[0-9]+|[0-9]+)",
                $"Z-{SliceSettings.LiftDistance - LayerHeight} F{SliceSettings.LiftDownSpeed}");*/

        }
        #endregion
    }
}
