/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
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
    public class VDTFile : FileFormat
    {
        #region Constants

        private const string FileManifestName = "manifest.json";
        private static string[] FilePreviewNames = {
            "Preview_Top.png",
            "Preview_Top_48.png",
            "Preview_Right.png",
            "Preview_Right_48.png",
            "Preview_Left.png",
            "Preview_Left_48.png",
            "Preview_Front.png",
            "Preview_Front_48.png",
            "Preview_FLT.png",
            "Preview_FLT_48.png",
        };
        #endregion

        #region Sub Classes

        public sealed class VDTManifest
        {
            [JsonProperty("application_name")] public string ApplicationName { get; set; } = "Voxeldance Tango";

            [JsonProperty("application_version")] public string ApplicationVersion { get; set; } = "2.1.15.14";
            //2021-04-09 17:48:46
            [JsonProperty("create_datetime")] public string CreateDateTime { get; set; } = DateTime.Now.ToString("u");

            [JsonProperty("file_version")] public byte FileVersion { get; set; } = 1;

            [JsonProperty("project_name")] public string ProjectName { get; set; } = "UVtools";

            [JsonProperty("machine")] public VDTMachine Machine { get; set; } = new();
            [JsonProperty("advanced_parameters")] public VDTAdvancedParameters AdvancedParameters { get; set; } = new();
            [JsonProperty("resin")] public VDTResin Resin { get; set; } = new();
            [JsonProperty("print")] public VDTPrint Print { get; set; } = new();
            [JsonProperty("print_statistics")] public VDTPrintStatistics PrintStatistics { get; set; } = new();
            [JsonProperty("layers")] public VDTLayer[] Layers { get; set; }
        }

        public sealed class VDTAdvancedParameters
        {
            [JsonProperty("antialasing_level")] public byte AntialasingLevel { get; set; } = 1;
            [JsonProperty("grey_level")] public byte GreyLevel { get; set; }
            [JsonProperty("image_blur_level")] public byte ImageBlurLevel { get; set; }

            [JsonProperty("bottom_light_pwm")] public byte BottomLightPWM { get; set; } = DefaultBottomLightPWM;
            [JsonProperty("light_pwm")] public byte LightPWM { get; set; } = DefaultLightPWM;

            [JsonProperty("beam_compensation")] public float BeamCompensation { get; set; }
        }

        public sealed class VDTMachine
        {
            [JsonProperty("name")] public string Name { get; set; } = "Unknown";
            [JsonProperty("type")] public string Type { get; set; } = "Default";
            [JsonProperty("lcd_width")] public float DisplayWidth { get; set; }
            [JsonProperty("lcd_height")] public float DisplayHeight { get; set; }
            [JsonProperty("z_height")] public float ZHeight { get; set; }
            [JsonProperty("resolution_x")] public ushort ResolutionX { get; set; }
            [JsonProperty("resolution_y")] public ushort ResolutionY { get; set; }
            [JsonProperty("x_mirror")] public bool XMirror { get; set; }
            [JsonProperty("y_mirror")] public bool YMirror { get; set; }
            [JsonProperty("notes")] public string Notes { get; set; }
            [JsonProperty("uvtools_convert_to")] public string UVToolsConvertTo { get; set; }
        }

        public sealed class VDTResin
        {
            [JsonProperty("name")] public string Name { get; set; }
            [JsonProperty("cost")] public float Cost { get; set; }
            [JsonProperty("density")] public float Density { get; set; } = 1;
            [JsonProperty("notes")] public string Notes { get; set; }
        }

        public sealed class VDTPrint
        {
            [JsonProperty("layer_thickness")] public float LayerHeight { get; set; }
            [JsonProperty("bottom_layers")] public ushort BottomLayers { get; set; } = DefaultBottomLayerCount;
            [JsonProperty("bottom_exposure_time")] public float BottomExposureTime { get; set; } = DefaultBottomExposureTime;
            [JsonProperty("exposure_time")] public float ExposureTime { get; set; } = DefaultExposureTime;
            [JsonProperty("bottom_lift_distance")] public float BottomLiftHeight { get; set; } = DefaultBottomLiftHeight;
            [JsonProperty("lift_distance")] public float LiftHeight { get; set; } = DefaultLiftHeight;
            [JsonProperty("bottom_lift_speed")] public float BottomLiftSpeed { get; set; } = DefaultBottomLiftSpeed;
            [JsonProperty("lift_speed")] public float LiftSpeed { get; set; } = DefaultLiftSpeed;
            [JsonProperty("bottom_retract_speed")] public float BottomRetractSpeed { get; set; } = DefaultRetractSpeed;
            [JsonProperty("retract_speed")] public float RetractSpeed { get; set; } = DefaultRetractSpeed;
            [JsonProperty("bottom_light_off_delay")] public float BottomLightOffDelay { get; set; } = DefaultBottomLightOffDelay;
            [JsonProperty("light_off_delay")] public float LightOffDelay { get; set; } = DefaultLightOffDelay;
        }

        public sealed class VDTPrintStatistics
        {
            [JsonProperty("price")] public float Price { get; set; }
            [JsonProperty("price_currency")] public string PriceCurrency { get; set; } = "€";
            [JsonProperty("estimated_time")] public uint EstimatedTime { get; set; }
            [JsonProperty("volume")] public float Volume { get; set; }
            [JsonProperty("weight")] public float Weight { get; set; }
        }

        public sealed class VDTLayer
        {
            [JsonProperty("height")] public float PositionZ { get; set; }
            [JsonProperty("exposure_time")] public float ExposureTime { get; set; } = DefaultExposureTime;
            [JsonProperty("lift_distance")] public float LiftHeight { get; set; } = DefaultLiftHeight;
            [JsonProperty("lift_speed")] public float LiftSpeed { get; set; } = DefaultLiftSpeed;
            [JsonProperty("retract_speed")] public float RetractSpeed { get; set; } = DefaultRetractSpeed;
            [JsonProperty("light_off_delay")] public float LightOffDelay { get; set; } = DefaultLightOffDelay;
            [JsonProperty("light_pwm")] public byte LightPWM { get; set; } = DefaultLightPWM;
        }

        #endregion

        #region Properties
        public VDTManifest ManifestFile { get; set; } = new();

        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new("vdt", "Voxeldance Tango VDT")
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
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.LightOffDelay,
            
            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,
        };

        public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } =
        {
            new(200, 200),
            new(48, 48),
            new(200, 200),
            new(48, 48),
            new(200, 200),
            new(48, 48),
            new(200, 200),
            new(48, 48),
            new(200, 200),
            new(48, 48),
        };

        public override uint ResolutionX
        {
            get => ManifestFile.Machine.ResolutionX;
            set
            {
                ManifestFile.Machine.ResolutionX = (ushort) value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => ManifestFile.Machine.ResolutionY;
            set
            {
                ManifestFile.Machine.ResolutionY = (ushort) value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayWidth
        {
            get => ManifestFile.Machine.DisplayWidth;
            set
            {
                ManifestFile.Machine.DisplayWidth = (float) Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float DisplayHeight
        {
            get => ManifestFile.Machine.DisplayHeight;
            set
            {
                ManifestFile.Machine.DisplayHeight = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float MachineZ
        {
            get => ManifestFile.Machine.ZHeight > 0 ? ManifestFile.Machine.ZHeight : base.MachineZ;
            set
            {
                ManifestFile.Machine.ZHeight = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override bool MirrorDisplay => ManifestFile.Machine.XMirror || ManifestFile.Machine.YMirror;

        public override byte AntiAliasing
        {
            get => ManifestFile.AdvancedParameters.AntialasingLevel;
            set
            {
                ManifestFile.AdvancedParameters.AntialasingLevel = value.Clamp(1, 16);
                RaisePropertyChanged();
            }

        }

        public override float LayerHeight
        {
            get => ManifestFile.Print.LayerHeight;
            set
            {
                ManifestFile.Print.LayerHeight = Layer.RoundHeight(value);
                RaisePropertyChanged();
            }
        }

       /* public override uint LayerCount
        {
            get => base.LayerCount;
            set => base.LayerCount = base.LayerCount;
        }*/

        public override ushort BottomLayerCount
        {
            get => ManifestFile.Print.BottomLayers;
            set => base.BottomLayerCount = ManifestFile.Print.BottomLayers = value;
        }

        public override float BottomExposureTime
        {
            get => ManifestFile.Print.BottomExposureTime;
            set => base.BottomExposureTime = ManifestFile.Print.BottomExposureTime = (float)Math.Round(value, 2);
        }

        public override float ExposureTime
        {
            get => ManifestFile.Print.ExposureTime;
            set => base.ExposureTime = ManifestFile.Print.ExposureTime = (float)Math.Round(value, 2);
        }

        public override float BottomLightOffDelay
        {
            get => ManifestFile.Print.BottomLightOffDelay;
            set => base.BottomLightOffDelay = ManifestFile.Print.BottomLightOffDelay = (float)Math.Round(value, 2);
        }

        public override float LightOffDelay
        {
            get => ManifestFile.Print.LightOffDelay;
            set => base.LightOffDelay = ManifestFile.Print.LightOffDelay = (float)Math.Round(value, 2);
        }

        public override float BottomLiftHeight
        {
            get => ManifestFile.Print.BottomLiftHeight;
            set => base.BottomLiftHeight = ManifestFile.Print.BottomLiftHeight = (float)Math.Round(value, 2);
        }

        public override float LiftHeight
        {
            get => ManifestFile.Print.LiftHeight;
            set => base.LiftHeight = ManifestFile.Print.LiftHeight = (float)Math.Round(value, 2);
        }

        public override float BottomLiftSpeed
        {
            get => ManifestFile.Print.BottomLiftSpeed;
            set => base.BottomLiftSpeed = ManifestFile.Print.BottomLiftSpeed = (float)Math.Round(value, 2);
        }

        public override float LiftSpeed
        {
            get => ManifestFile.Print.LiftSpeed;
            set => base.LiftSpeed = ManifestFile.Print.LiftSpeed = (float)Math.Round(value, 2);
        }

        public override float RetractSpeed
        {
            get => ManifestFile.Print.RetractSpeed;
            set => base.RetractSpeed = ManifestFile.Print.RetractSpeed = ManifestFile.Print.BottomRetractSpeed = (float)Math.Round(value, 2);
        }

        public override byte BottomLightPWM
        {
            get => ManifestFile.AdvancedParameters.BottomLightPWM;
            set => base.BottomLightPWM = ManifestFile.AdvancedParameters.BottomLightPWM = value;
        }

        public override byte LightPWM
        {
            get => ManifestFile.AdvancedParameters.LightPWM;
            set => base.LightPWM = ManifestFile.AdvancedParameters.LightPWM = value;
        }

        public override float PrintTime
        {
            get => base.PrintTime;
            set
            {
                base.PrintTime = value;
                ManifestFile.PrintStatistics.EstimatedTime = (uint)base.PrintTime;
            }
        }

        public override float MaterialMilliliters
        {
            get => base.MaterialMilliliters;
            set
            {
                base.MaterialMilliliters = value;
                ManifestFile.PrintStatistics.Volume = base.MaterialMilliliters;
            }
        }

       public override float MaterialGrams
        {
            get => (float)Math.Round(ManifestFile.PrintStatistics.Weight, 3);
            set => base.MaterialGrams = ManifestFile.PrintStatistics.Weight = (float)Math.Round(value, 3);
        }

       public override string MaterialName
       {
           get => ManifestFile.Resin.Name;
           set => base.MaterialName = ManifestFile.Resin.Name = value;
       }

        public override float MaterialCost
        {
            get => (float)Math.Round(ManifestFile.Resin.Cost, 3);
            set => base.MaterialCost = ManifestFile.Resin.Cost = (float)Math.Round(value, 3);
        }

        public override string MachineName
        {
            get => ManifestFile.Machine.Name;
            set => base.MachineName = ManifestFile.Machine.Name = value;
        }

        public override object[] Configs => new[] {(object)ManifestFile, ManifestFile.Machine, ManifestFile.AdvancedParameters, ManifestFile.Resin, ManifestFile.Print, ManifestFile.PrintStatistics};
        #endregion

        #region Methods

        public void RebuildVDTLayers()
        {
            ManifestFile.CreateDateTime = DateTime.Now.ToString("u");
            var layers = new VDTLayer[LayerCount];
            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                layers[layerIndex] = new VDTLayer
                {
                    PositionZ = layer.PositionZ,
                    ExposureTime = layer.ExposureTime,
                    LiftHeight = layer.LiftHeight,
                    LiftSpeed = layer.LiftSpeed,
                    RetractSpeed = layer.RetractSpeed,
                    LightOffDelay = layer.LightOffDelay,
                    LightPWM = layer.LightPWM
                };
            }

            ManifestFile.Layers = layers;
        }

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            // Redo layer data
            RebuildVDTLayers();

            using var outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create);
            outputFile.PutFileContent(FileManifestName, JsonConvert.SerializeObject(ManifestFile, Formatting.Indented), ZipArchiveMode.Create);

            if (CreatedThumbnailsCount > 0)
            {
                for (int i = 0; i < FilePreviewNames.Length; i++)
                {
                    if(Thumbnails.Length <= i) break;
                    if(Thumbnails[i] is null) continue;

                    using var stream = outputFile.CreateEntry(FilePreviewNames[i]).Open();
                    using var vec = new VectorOfByte();
                    CvInvoke.Imencode(".png", Thumbnails[i], vec);
                    stream.WriteBytes(vec.ToArray());
                    stream.Close();
                }
            }

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                progress.Token.ThrowIfCancellationRequested();

                var layer = this[layerIndex];
                var layerImagePath = $"{layerIndex}.png";
                outputFile.PutFileContent(layerImagePath, layer.CompressedBytes, ZipArchiveMode.Create);
                progress++;
            }
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using (var inputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Read))
            {
                var entry = inputFile.GetEntry(FileManifestName);
                if (entry is null)
                {
                    Clear();
                    throw new FileLoadException($"{FileManifestName} not found", fileFullPath);
                }

                ManifestFile = Helpers.JsonDeserializeObject<VDTManifest>(entry.Open());
                
                LayerManager.Init((uint) ManifestFile.Layers.Length);

                for (int i = 0; i < FilePreviewNames.Length; i++)
                {
                    if (Thumbnails.Length <= i) break;

                    entry = inputFile.GetEntry(FilePreviewNames[i]);
                    if (entry is null) continue;
                    using var stream = entry.Open();
                    Mat image = new();
                    CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, image);
                    Thumbnails[i] = image;
                    stream.Close();
                }

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    var manifestLayer = ManifestFile.Layers[layerIndex];
                    entry = inputFile.GetEntry($"{layerIndex}.png");
                    if (entry is null) continue;
                    using var stream = entry.Open();
                    this[layerIndex] = new Layer(layerIndex, stream, LayerManager)
                    {
                        PositionZ = manifestLayer.PositionZ,
                        ExposureTime = manifestLayer.ExposureTime,
                        LiftHeight = manifestLayer.LiftHeight,
                        LiftSpeed = manifestLayer.LiftSpeed,
                        RetractSpeed = manifestLayer.RetractSpeed,
                        LightOffDelay = manifestLayer.LightOffDelay,
                        LightPWM = manifestLayer.LightPWM,
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

            RebuildVDTLayers();
            using var outputFile = ZipFile.Open(FileFullPath, ZipArchiveMode.Update);
            outputFile.PutFileContent(FileManifestName, JsonConvert.SerializeObject(ManifestFile, Formatting.Indented), ZipArchiveMode.Update);

            //Decode(FileFullPath, progress);
        }

        public T LookupCustomValue<T>(string name, T defaultValue, bool existsOnly = false)
        {
            //if (string.IsNullOrEmpty(PrinterSettings.PrinterNotes)) return defaultValue;
            var result = string.Empty;
            if (!existsOnly) name += '_';

            foreach (var notes in new[] { ManifestFile.Machine.Notes })
            {
                if (string.IsNullOrWhiteSpace(notes)) continue;

                var lines = notes.Split(new[] { "\\r\\n", "\\r", "\\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

                foreach (var line in lines)
                {
                    if (!line.StartsWith(name)) continue;
                    if (existsOnly || line == name) return "true".Convert<T>();
                    var value = line.Remove(0, name.Length);
                    foreach (var c in value)
                    {
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

                if (!string.IsNullOrEmpty(result)) break; // Found a candidate
            }

            return string.IsNullOrWhiteSpace(result) ? defaultValue : result.Convert<T>();
        }
        #endregion
    }
}
