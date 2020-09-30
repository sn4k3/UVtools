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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
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
            [DisplayName("Blanking Layer Time")] public uint BlankingLayerTime { get; set; } = 1000;
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
            [DisplayName("Bottom Layer Light PWM")] public byte BottomLightPWM { get; set; } = 255;
            [DisplayName("Layer Light PWM")] public byte LightPWM { get; set; } = 255;
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

        public enum PrinterType : byte
        {
            Unknown,
            Elfin,
            BeneMono
        }

        public PrinterType Printer { get; set; } = PrinterType.Unknown;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("cws", "NovaMaker CWS Files"),
            //new FileExtension("cws", "NovaMaker Bene Mono CWS Files", "Bene")
        };

        public override Type[] ConvertToFormats { get; } =
        {
            typeof(UVJFile)
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
            PrintParameterModifier.ExposureSeconds,


            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.LiftSpeed,

            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,
        };

        public override byte ThumbnailsCount { get; } = 0;

        public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } = null;

        public override uint ResolutionX
        {
            get => SliceSettings.Xres;
            set => OutputSettings.XResolution = SliceSettings.Xres = (ushort) value;
        }

        public override uint ResolutionY
        {
            get => SliceSettings.Yres;
            set => OutputSettings.YResolution = SliceSettings.Yres = (ushort) value;
        }

        public override byte AntiAliasing => (byte) OutputSettings.AntiAliasingValue;

        public override float LayerHeight
        {
            get => SliceSettings.Thickness;
            set => OutputSettings.LayerThickness = SliceSettings.Thickness = value; 
        }

        public override uint LayerCount
        {
            set
            {
                OutputSettings.LayersNum = LayerCount;
                SliceSettings.LayersNum = LayerCount;
                RebuildGCode();
            }
        }

        public override ushort BottomLayerCount
        {
            get => SliceSettings.HeadLayersNum;
            set => SliceSettings.HeadLayersNum = value;
        }

        public override float BottomExposureTime
        {
            get => (float) Math.Round(SliceSettings.HeadLayersExpoMs / 1000f, 2);
            set => SliceSettings.HeadLayersExpoMs = (uint) (value * 1000f);
        }

        public override float ExposureTime
        {
            get => (float) Math.Round(SliceSettings.LayersExpoMs / 1000f, 2);
            set => SliceSettings.LayersExpoMs = (uint) (value * 1000f);
        }
        
        public override float LiftHeight
        {
            get => SliceSettings.LiftDistance;
            set => SliceSettings.LiftDistance = value;
        }

        public override float LiftSpeed
        {
            get => SliceSettings.LiftDownSpeed;
            set => SliceSettings.LiftDownSpeed = value;
        }

        public override float RetractSpeed
        {
            get => OutputSettings.ZLiftRetractRate;
            set => OutputSettings.ZLiftRetractRate = value;
        }

        public override byte BottomLightPWM
        {
            get => OutputSettings.BottomLightPWM;
            set => OutputSettings.BottomLightPWM = value;
        }

        public override byte LightPWM
        {
            get => OutputSettings.LightPWM;
            set => OutputSettings.LightPWM = value;
        }


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
            if (Printer == PrinterType.Unknown) Printer = PrinterType.Elfin;
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


                //for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                Parallel.For(0, LayerCount, 
                    new ParallelOptions{MaxDegreeOfParallelism = Printer == PrinterType.Elfin ? 1 : Environment.ProcessorCount},
                    layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;

                    Layer layer = this[layerIndex];
                    var layerImagePath =
                        $"{Path.GetFileNameWithoutExtension(fileFullPath)}{layerIndex.ToString().PadLeft(LayerCount.ToString().Length, '0')}.png";

                    if (Printer == PrinterType.Elfin)
                    {
                        outputFile.PutFileContent(layerImagePath, layer.CompressedBytes, ZipArchiveMode.Create);
                    }
                    else
                    {
                        using (var mat = layer.LayerMat)
                        using (var matEncode = new Mat(mat.Height, mat.Step / 3, DepthType.Cv8U, 3))
                        {
                            var span = mat.GetPixelSpan<byte>();
                            var spanEncode = matEncode.GetPixelSpan<byte>();
                            for (int i = 0; i < span.Length; i++)
                            {
                                spanEncode[i] = span[i];
                            }

                            using (VectorOfByte vec = new VectorOfByte())
                            {
                                CvInvoke.Imencode(".png", matEncode, vec);
                                lock (progress.Mutex)
                                {
                                    outputFile.PutFileContent(layerImagePath, vec.ToArray(), ZipArchiveMode.Create);
                                }
                            }
                        }

                        lock (progress.Mutex)
                        {
                            progress++;
                        }

                    }
                });

                RebuildGCode();
                outputFile.PutFileContent($"{Path.GetFileNameWithoutExtension(fileFullPath)}.gcode", GCode.ToString(), ZipArchiveMode.Create);
            }

            AfterEncode();
        }

        public override void Decode(string fileFullPath, OperationProgress progress = null)
        {
            base.Decode(fileFullPath, progress);
            if(progress is null) progress = new OperationProgress(OperationProgress.StatusGatherLayers, LayerCount);

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
                if (entry is null)
                {
                    entry = inputFile.GetEntry("slice.gcode");
                    if (entry is null)
                    {
                        Clear();
                        throw new FileLoadException($"{Path.GetFileNameWithoutExtension(fileFullPath)}.gcode nor slice.gcode was found",
                            fileFullPath);
                    }
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


                LayerManager = new LayerManager(OutputSettings.LayersNum, this);

                progress.ItemCount = OutputSettings.LayersNum;

                var gcode = GCode.ToString();
                //float currentHeight = 0;


                int layerSize = OutputSettings.LayersNum.ToString().Length;

                inputFile.Entries.AsParallel().ForAllInApproximateOrder(zipArchiveEntry =>
                    //foreach (var zipArchiveEntry in inputFile.Entries)
                {
                    if (!zipArchiveEntry.Name.EndsWith(".png") || progress.Token.IsCancellationRequested) return;

                    // - .png - 4 numbers
                    string layerStr =
                        zipArchiveEntry.Name.Substring(zipArchiveEntry.Name.Length - 4 - layerSize, layerSize);
                    uint layerIndex = uint.Parse(layerStr);

                    var startStr = $"{GCodeKeywordSlice} {layerIndex}";
                    var stripGcode =
                        gcode.Substring(gcode.IndexOf(startStr, StringComparison.InvariantCultureIgnoreCase) +
                                        startStr.Length);
                    stripGcode = stripGcode
                        .Substring(0, stripGcode.IndexOf(GCodeKeywordDelay, stripGcode.IndexOf(GCodeKeywordSlice)))
                        .Trim(' ', '\n', '\r', '\t');
                    //var startCurrPos = stripGcode.Remove(0, ";currPos:".Length);


                    //var currPos = Regex.Match(stripGcode, "G1 Z([+-]?([0-9]*[.])?[0-9]+)", RegexOptions.IgnoreCase);
                    var exposureTime = Regex.Match(stripGcode, ";<Delay> (\\d+)", RegexOptions.IgnoreCase);
                    /*var pwm = Regex.Match(stripGcode, "M106 S(\\d+)", RegexOptions.IgnoreCase);
                    if (layerIndex < InitialLayerCount)
                    {
                        OutputSettings.BottomLayerLightPWM = byte.Parse(pwm.Groups[1].Value);
                    }
                    else
                    {
                        OutputSettings.LayerLightPWM = byte.Parse(pwm.Groups[1].Value);
                    }*/

                    /*if (currPos.Success)
                    {
                        var nextMatch = currPos.NextMatch();
                        if (nextMatch.Success)
                        {
                            currentHeight =
                                (float) Math.Round(
                                    currentHeight + float.Parse(currPos.Groups[1].Value) +
                                    float.Parse(currPos.NextMatch().Groups[1].Value), 2);
                        }
                        else
                        {
                            currentHeight = (float) Math.Round(currentHeight + float.Parse(currPos.Groups[1].Value), 2);
                        }
                    }*/

                    byte[] buffer;

                    lock (progress)
                    {
                        using (var stream = zipArchiveEntry.Open())
                        {
                            buffer = stream.ToArray();
                            if (Printer == PrinterType.Unknown)
                            {
                                using (Mat mat = new Mat())
                                {
                                    CvInvoke.Imdecode(buffer, ImreadModes.AnyColor, mat);
                                    Printer = mat.NumberOfChannels == 1 ? PrinterType.Elfin : PrinterType.BeneMono;
                                }
                            }

                            stream.Close();
                        }
                    }


                    if (Printer == PrinterType.Elfin)
                    {
                        LayerManager[layerIndex] =
                            new Layer(layerIndex, buffer, zipArchiveEntry.Name)
                            {
                                PositionZ = GetHeightFromLayer(layerIndex),
                                ExposureTime = float.Parse(exposureTime.Groups[1].Value) / 1000f
                            };
                    }
                    else
                    {
                        using (Mat mat = new Mat())
                        {
                            CvInvoke.Imdecode(buffer, ImreadModes.Color, mat);
                            using (Mat matDecode = new Mat(mat.Height, mat.Step, DepthType.Cv8U, 1))
                            {
                                var span = mat.GetPixelSpan<byte>();
                                var spanDecode = matDecode.GetPixelSpan<byte>();
                                for (int i = 0; i < span.Length; i++)
                                {
                                    spanDecode[i] = span[i];
                                }

                                LayerManager[layerIndex] =
                                    new Layer(layerIndex, matDecode, zipArchiveEntry.Name)
                                    {
                                        PositionZ = GetHeightFromLayer(layerIndex),
                                        ExposureTime = float.Parse(exposureTime.Groups[1].Value) / 1000f
                                    };
                            }
                        }

                    }

                    progress++;
                });
            }

            LayerManager.GetBoundingRectangle(progress);
        }

        public override void RebuildGCode()
        {
            string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
            GCode = new StringBuilder();
            GCode.AppendLine($"; {About.Website} {About.Software} {Assembly.GetExecutingAssembly().GetName().Version} {arch} {DateTime.Now}");
            GCode.AppendLine(";(****Build and Slicing Parameters ****)");

            foreach (var propertyInfo in OutputSettings.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                if (ReferenceEquals(displayNameAttribute, null)) continue;
                if (propertyInfo.Name.Equals(nameof(OutputSettings.LayersNum)))
                {
                    GCode.AppendLine($";{displayNameAttribute.DisplayName.PadRight(24)} = {propertyInfo.GetValue(OutputSettings)}");
                }
                else
                {
                    GCode.AppendLine($";({displayNameAttribute.DisplayName.PadRight(24)} = {propertyInfo.GetValue(OutputSettings)})");
                }

            }
            GCode.AppendLine();
            GCode.AppendFormat(GCodeStart, Environment.NewLine);

            float lastZPosition = 0;

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                Layer layer = this[layerIndex];
                GCode.AppendLine($"{GCodeKeywordSlice} {layerIndex}");
                GCode.AppendLine($"M106 S{GetInitialLayerValueOrNormal(layerIndex, OutputSettings.BottomLightPWM, OutputSettings.LightPWM)}");
                GCode.AppendLine($"{GCodeKeywordDelay} {layer.ExposureTime * 1000}");
                GCode.AppendLine("M106 S0");
                GCode.AppendLine(GCodeKeywordSliceBlank);

                if (lastZPosition != layer.PositionZ)
                {
                    if (LiftHeight > 0)
                    {
                        GCode.AppendLine($"G1 Z{LiftHeight} F{LiftSpeed}");
                        GCode.AppendLine($"G1 Z-{Math.Round(LiftHeight - layer.PositionZ + lastZPosition, 2)} F{RetractSpeed}");
                    }
                    else
                    {
                        GCode.AppendLine($"G1 Z{Math.Round(layer.PositionZ - lastZPosition, 2)} F{LiftSpeed}");
                    }
                }
                // delay = max(extra['wait'], 500) + int(((int(lift)/(extra['lift_feed']/60)) + (int(lift)/(extra['lift_retract']/60)))*1000)
                uint extraDelay = (uint)(((LiftHeight / (LiftSpeed / 60f)) + (LiftHeight / (RetractSpeed / 60f))) * 1000);
                if (layerIndex < BottomLayerCount)
                {
                    extraDelay = (uint)Math.Max(extraDelay + 10000, layer.ExposureTime * 1000);
                }
                else
                {
                    extraDelay += Math.Max(OutputSettings.BlankingLayerTime, 500);
                }

                GCode.AppendLine($"{GCodeKeywordDelay} {extraDelay}");
                GCode.AppendLine();

                lastZPosition = layer.PositionZ;
            }

            GCode.AppendFormat(GCodeEnd, Environment.NewLine, SliceSettings.LiftWhenFinished);
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

                /*foreach (var layer in this)
                {
                    if (!layer.IsModified) continue;
                    outputFile.PutFileContent(layer.Filename, layer.CompressedBytes, ZipArchiveMode.Update);
                    layer.IsModified = false;
                }*/
            }

            //Decode(FileFullPath, progress);
        }

        public override bool Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
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
                                    X = OutputSettings.PlatformXSize,
                                    Y = OutputSettings.PlatformYSize,
                                },
                                LayerHeight = LayerHeight,
                                Layers = LayerCount
                            },
                            Bottom = new UVJFile.Bottom
                            {
                                LiftHeight = SliceSettings.LiftDistance,
                                LiftSpeed = SliceSettings.LiftUpSpeed,
                                LightOnTime = BottomExposureTime,
                                //LightOffTime = SliceSettings.BottomLightOffDelay,
                                LightPWM = OutputSettings.BottomLightPWM,
                                RetractSpeed = OutputSettings.ZBottomLiftFeedRate,
                                Count = BottomLayerCount
                                //RetractHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.JsonSettings.Properties.Bottom.RetractHeight),
                            },
                            Exposure = new UVJFile.Exposure
                            {
                                LiftHeight = SliceSettings.LiftDistance,
                                LiftSpeed = SliceSettings.LiftUpSpeed,
                                LightOnTime = BottomExposureTime,
                                //LightOffTime = SliceSettings.BottomLightOffDelay,
                                LightPWM = OutputSettings.BottomLightPWM,
                                RetractSpeed = SliceSettings.LiftDownSpeed,
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
