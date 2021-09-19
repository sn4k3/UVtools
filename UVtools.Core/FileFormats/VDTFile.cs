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
using Newtonsoft.Json;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
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
            [JsonProperty("application_name")] public string ApplicationName { get; set; } = About.Software;

            [JsonProperty("application_version")] public string ApplicationVersion { get; set; } = About.VersionStr;
            //2021-04-09 17:48:46
            [JsonProperty("create_datetime")] public string CreateDateTime { get; set; } = DateTime.UtcNow.ToString("u");

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
            [JsonProperty("bottom_light_off_delay")] public float BottomLightOffDelay { get; set; }
            [JsonProperty("light_off_delay")] public float LightOffDelay { get; set; }
            [JsonProperty("bottom_wait_time_before_cure")] public float BottomWaitTimeBeforeCure { get; set; }
            [JsonProperty("wait_time_before_cure")] public float WaitTimeBeforeCure { get; set; }
            [JsonProperty("bottom_exposure_time")] public float BottomExposureTime { get; set; } = DefaultBottomExposureTime;
            [JsonProperty("exposure_time")] public float ExposureTime { get; set; } = DefaultExposureTime;
            [JsonProperty("bottom_wait_time_after_cure")] public float BottomWaitTimeAfterCure { get; set; }
            [JsonProperty("wait_time_after_cure")] public float WaitTimeAfterCure { get; set; }
            [JsonProperty("bottom_lift_distance")] public float BottomLiftHeight { get; set; } = DefaultBottomLiftHeight;
            [JsonProperty("bottom_lift_speed")] public float BottomLiftSpeed { get; set; } = DefaultBottomLiftSpeed;
            [JsonProperty("lift_distance")] public float LiftHeight { get; set; } = DefaultLiftHeight;
            [JsonProperty("lift_speed")] public float LiftSpeed { get; set; } = DefaultLiftSpeed;
            [JsonProperty("bottom_lift_distance2")] public float BottomLiftHeight2 { get; set; } = DefaultBottomLiftHeight2;
            [JsonProperty("bottom_lift_speed2")] public float BottomLiftSpeed2 { get; set; } = DefaultBottomLiftSpeed2;
            [JsonProperty("lift_distance2")] public float LiftHeight2 { get; set; } = DefaultLiftHeight2;
            [JsonProperty("lift_speed2")] public float LiftSpeed2 { get; set; } = DefaultLiftSpeed2;
            [JsonProperty("bottom_wait_time_after_lift")] public float BottomWaitTimeAfterLift { get; set; }
            [JsonProperty("wait_time_after_lift")] public float WaitTimeAfterLift { get; set; }
            [JsonProperty("bottom_retract_speed")] public float BottomRetractSpeed { get; set; } = DefaultBottomRetractSpeed;
            [JsonProperty("retract_speed")] public float RetractSpeed { get; set; } = DefaultRetractSpeed;
            [JsonProperty("bottom_retract_distance2")] public float BottomRetractHeight2 { get; set; } = DefaultBottomRetractHeight2;
            [JsonProperty("bottom_retract_speed2")] public float BottomRetractSpeed2 { get; set; } = DefaultBottomRetractSpeed2;
            [JsonProperty("retract_distance2")] public float RetractHeight2 { get; set; } = DefaultRetractHeight2;
            [JsonProperty("retract_speed2")] public float RetractSpeed2 { get; set; } = DefaultRetractSpeed2;
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
            [JsonProperty("light_off_delay")] public float LightOffDelay { get; set; }
            [JsonProperty("wait_time_before_cure")] public float WaitTimeBeforeCure { get; set; }
            [JsonProperty("exposure_time")] public float ExposureTime { get; set; } = DefaultExposureTime;
            [JsonProperty("wait_time_after_cure")] public float WaitTimeAfterCure { get; set; }
            [JsonProperty("lift_distance")] public float LiftHeight { get; set; } = DefaultLiftHeight;
            [JsonProperty("lift_speed")] public float LiftSpeed { get; set; } = DefaultLiftSpeed;
            [JsonProperty("lift_distance2")] public float LiftHeight2 { get; set; } = DefaultLiftHeight2;
            [JsonProperty("lift_speed2")] public float LiftSpeed2 { get; set; } = DefaultLiftSpeed2;
            [JsonProperty("wait_time_after_lift")] public float WaitTimeAfterLift { get; set; }
            [JsonProperty("retract_speed")] public float RetractSpeed { get; set; } = DefaultRetractSpeed;
            [JsonProperty("retract_distance2")] public float RetractHeight2 { get; set; } = DefaultRetractHeight2;
            [JsonProperty("retract_speed2")] public float RetractSpeed2 { get; set; } = DefaultRetractSpeed2;
            [JsonProperty("light_pwm")] public byte LightPWM { get; set; } = DefaultLightPWM;
        }

        #endregion

        #region Properties
        public VDTManifest ManifestFile { get; set; } = new();

        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new(typeof(VDTFile), "vdt", "Voxeldance Tango VDT")
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
            PrintParameterModifier.BottomLayerCount,

            PrintParameterModifier.BottomLightOffDelay,
            PrintParameterModifier.LightOffDelay,

            PrintParameterModifier.BottomWaitTimeBeforeCure,
            PrintParameterModifier.WaitTimeBeforeCure,

            PrintParameterModifier.BottomExposureTime,
            PrintParameterModifier.ExposureTime,

            PrintParameterModifier.BottomWaitTimeAfterCure,
            PrintParameterModifier.WaitTimeAfterCure,

            PrintParameterModifier.BottomLiftHeight,
            PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,

            PrintParameterModifier.BottomLiftHeight2,
            PrintParameterModifier.BottomLiftSpeed2,
            PrintParameterModifier.LiftHeight2,
            PrintParameterModifier.LiftSpeed2,

            PrintParameterModifier.BottomWaitTimeAfterLift,
            PrintParameterModifier.WaitTimeAfterLift,

            PrintParameterModifier.BottomRetractSpeed,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.BottomRetractHeight2,
            PrintParameterModifier.BottomRetractSpeed2,
            PrintParameterModifier.RetractHeight2,
            PrintParameterModifier.RetractSpeed2,

            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,
        };

        public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
            
            PrintParameterModifier.LightOffDelay,
            PrintParameterModifier.WaitTimeBeforeCure,
            PrintParameterModifier.ExposureTime,
            PrintParameterModifier.BottomWaitTimeAfterCure,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.LiftHeight2,
            PrintParameterModifier.LiftSpeed2,
            PrintParameterModifier.WaitTimeAfterLift,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.RetractHeight2,
            PrintParameterModifier.RetractSpeed2,
            
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

        public override Enumerations.FlipDirection DisplayMirror
        {
            get
            {
                if (ManifestFile.Machine.XMirror && ManifestFile.Machine.YMirror) return Enumerations.FlipDirection.Both;
                if (ManifestFile.Machine.XMirror) return Enumerations.FlipDirection.Horizontally;
                if (ManifestFile.Machine.YMirror) return Enumerations.FlipDirection.Vertically;
                return Enumerations.FlipDirection.None;
            }
            set
            {
                ManifestFile.Machine.XMirror = value is Enumerations.FlipDirection.Horizontally or Enumerations.FlipDirection.Both;
                ManifestFile.Machine.YMirror = value is Enumerations.FlipDirection.Vertically or Enumerations.FlipDirection.Both;
                RaisePropertyChanged();
            }
        }

        public override byte AntiAliasing
        {
            get => ManifestFile.AdvancedParameters.AntialasingLevel;
            set => base.AntiAliasing = ManifestFile.AdvancedParameters.AntialasingLevel = value.Clamp(1, 16);
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

        public override float BottomWaitTimeBeforeCure
        {
            get => ManifestFile.Print.BottomWaitTimeBeforeCure;
            set => base.BottomWaitTimeBeforeCure = ManifestFile.Print.BottomWaitTimeBeforeCure = (float)Math.Round(value, 2);
        }
        public override float WaitTimeBeforeCure
        {
            get => ManifestFile.Print.WaitTimeBeforeCure;
            set => base.WaitTimeBeforeCure = ManifestFile.Print.WaitTimeBeforeCure = (float)Math.Round(value, 2);
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

        public override float BottomWaitTimeAfterCure
        {
            get => ManifestFile.Print.BottomWaitTimeAfterCure;
            set => base.BottomWaitTimeAfterCure = ManifestFile.Print.BottomWaitTimeAfterCure = (float)Math.Round(value, 2);
        }
        public override float WaitTimeAfterCure
        {
            get => ManifestFile.Print.WaitTimeAfterCure;
            set => base.WaitTimeAfterCure = ManifestFile.Print.WaitTimeAfterCure = (float)Math.Round(value, 2);
        }

        public override float BottomLiftHeight
        {
            get => ManifestFile.Print.BottomLiftHeight;
            set => base.BottomLiftHeight = ManifestFile.Print.BottomLiftHeight = (float)Math.Round(value, 2);
        }

        public override float BottomLiftSpeed
        {
            get => ManifestFile.Print.BottomLiftSpeed;
            set => base.BottomLiftSpeed = ManifestFile.Print.BottomLiftSpeed = (float)Math.Round(value, 2);
        }

        public override float LiftHeight
        {
            get => ManifestFile.Print.LiftHeight;
            set => base.LiftHeight = ManifestFile.Print.LiftHeight = (float)Math.Round(value, 2);
        }
        
        public override float LiftSpeed
        {
            get => ManifestFile.Print.LiftSpeed;
            set => base.LiftSpeed = ManifestFile.Print.LiftSpeed = (float)Math.Round(value, 2);
        }

        public override float BottomLiftHeight2
        {
            get => ManifestFile.Print.BottomLiftHeight2;
            set => base.BottomLiftHeight2 = ManifestFile.Print.BottomLiftHeight2 = (float)Math.Round(value, 2);
        }

        public override float BottomLiftSpeed2
        {
            get => ManifestFile.Print.BottomLiftSpeed2;
            set => base.BottomLiftSpeed2 = ManifestFile.Print.BottomLiftSpeed2 = (float)Math.Round(value, 2);
        }

        public override float LiftHeight2
        {
            get => ManifestFile.Print.LiftHeight2;
            set => base.LiftHeight2 = ManifestFile.Print.LiftHeight2 = (float)Math.Round(value, 2);
        }
        
        public override float LiftSpeed2
        {
            get => ManifestFile.Print.LiftSpeed2;
            set => base.LiftSpeed2 = ManifestFile.Print.LiftSpeed2 = (float)Math.Round(value, 2);
        }

        public override float BottomWaitTimeAfterLift
        {
            get => ManifestFile.Print.BottomWaitTimeAfterLift;
            set => base.BottomWaitTimeAfterLift = ManifestFile.Print.BottomWaitTimeAfterLift = (float)Math.Round(value, 2);
        }
        public override float WaitTimeAfterLift
        {
            get => ManifestFile.Print.WaitTimeAfterLift;
            set => base.WaitTimeAfterLift = ManifestFile.Print.WaitTimeAfterLift = (float)Math.Round(value, 2);
        }

        public override float BottomRetractSpeed
        {
            get => ManifestFile.Print.BottomRetractSpeed;
            set => base.BottomRetractSpeed = ManifestFile.Print.BottomRetractSpeed = (float)Math.Round(value, 2);
        }

        public override float RetractSpeed
        {
            get => ManifestFile.Print.RetractSpeed;
            set => base.RetractSpeed = ManifestFile.Print.RetractSpeed = (float)Math.Round(value, 2);
        }

        public override float BottomRetractHeight2
        {
            get => ManifestFile.Print.BottomRetractHeight2;
            set
            {
                value = Math.Clamp((float)Math.Round(value, 2), 0, BottomRetractHeightTotal);
                base.BottomRetractHeight2 = ManifestFile.Print.BottomRetractHeight2 = value;
            }
        }
        
        public override float BottomRetractSpeed2
        {
            get => ManifestFile.Print.BottomRetractSpeed2;
            set => base.BottomRetractSpeed2 = ManifestFile.Print.BottomRetractSpeed2 = (float)Math.Round(value, 2);
        }

        public override float RetractHeight2
        {
            get => ManifestFile.Print.RetractHeight2;
            set
            {
                value = Math.Clamp((float)Math.Round(value, 2), 0, RetractHeightTotal);
                base.RetractHeight2 = ManifestFile.Print.RetractHeight2 = value;
            }
        }
        
        public override float RetractSpeed2
        {
            get => ManifestFile.Print.RetractSpeed2;
            set => base.RetractSpeed2 = ManifestFile.Print.RetractSpeed2 = (float)Math.Round(value, 2);
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
            ManifestFile.CreateDateTime = DateTime.UtcNow.ToString("u");
            ManifestFile.Layers = new VDTLayer[LayerCount];
            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                ManifestFile.Layers[layerIndex] = new VDTLayer
                {
                    PositionZ = layer.PositionZ,
                    LightOffDelay = layer.LightOffDelay,
                    WaitTimeBeforeCure = layer.WaitTimeBeforeCure,
                    ExposureTime = layer.ExposureTime,
                    WaitTimeAfterCure = layer.WaitTimeAfterCure,
                    LiftHeight = layer.LiftHeight,
                    LiftSpeed = layer.LiftSpeed,
                    LiftHeight2 = layer.LiftHeight2,
                    LiftSpeed2 = layer.LiftSpeed2,
                    WaitTimeAfterLift = layer.WaitTimeAfterLift,
                    RetractSpeed = layer.RetractSpeed,
                    RetractHeight2 = layer.RetractHeight2,
                    RetractSpeed2 = layer.RetractSpeed2,
                    LightPWM = layer.LightPWM
                };
            }
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
                    stream.WriteBytes(Thumbnails[i].GetPngByes());
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
                        LightOffDelay = manifestLayer.LightOffDelay,
                        WaitTimeBeforeCure = manifestLayer.WaitTimeBeforeCure,
                        ExposureTime = manifestLayer.ExposureTime,
                        WaitTimeAfterCure = manifestLayer.WaitTimeAfterCure,
                        LiftHeight = manifestLayer.LiftHeight,
                        LiftSpeed = manifestLayer.LiftSpeed,
                        LiftHeight2 = manifestLayer.LiftHeight2,
                        LiftSpeed2 = manifestLayer.LiftSpeed2,
                        WaitTimeAfterLift = manifestLayer.WaitTimeAfterLift,
                        RetractSpeed = manifestLayer.RetractSpeed,
                        RetractHeight2 = manifestLayer.RetractHeight2,
                        RetractSpeed2 = manifestLayer.RetractSpeed2,
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
