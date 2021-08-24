/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
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
    public class UVJFile : FileFormat
    {
        #region Constants

        private const string FileConfigName = "config.json";
        private const string FolderImageName = "slice";
        private const string FolderPreviewName = "preview";
        private const string FilePreviewHugeName = "preview/huge.png";
        private const string FilePreviewTinyName = "preview/tiny.png";
        #endregion

        #region Sub Classes

        public class Millimeter
        {
            public float X { get; set; }
            public float Y { get; set; }
        }

        public class Size
        {
            public ushort X { get; set; }
            public ushort Y { get; set; }

            public Millimeter Millimeter { get; set; } = new Millimeter();

            public uint Layers { get; set; }
            public float LayerHeight { get; set; }

            public override string ToString()
            {
                return $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Millimeter)}: {Millimeter}, {nameof(Layers)}: {Layers}, {nameof(LayerHeight)}: {LayerHeight}";
            }
        }

        public class Exposure
        {
            public float LightOnTime { get; set; }
            public float LightOffTime { get; set; }
            public byte LightPWM { get; set; } = 255;
            public float LiftHeight { get; set; } = 5;
            public float LiftSpeed { get; set; } = 100;
            public float RetractHeight { get; set; }
            public float RetractSpeed { get; set; } = 100;

            public override string ToString()
            {
                return $"{nameof(LightOnTime)}: {LightOnTime}, {nameof(LightOffTime)}: {LightOffTime}, {nameof(LightPWM)}: {LightPWM}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractHeight)}: {RetractHeight}, {nameof(RetractSpeed)}: {RetractSpeed}";
            }
        }

        public class Bottom
        {
            public float LightOnTime { get; set; }
            public float LightOffTime { get; set; }
            public byte LightPWM { get; set; } = 255;
            public float LiftHeight { get; set; } = 5;
            public float LiftSpeed { get; set; } = 100;
            public float RetractHeight { get; set; }
            public float RetractSpeed { get; set; } = 100;
            public ushort Count { get; set; }

            public override string ToString()
            {
                return $"{nameof(LightOnTime)}: {LightOnTime}, {nameof(LightOffTime)}: {LightOffTime}, {nameof(LightPWM)}: {LightPWM}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractHeight)}: {RetractHeight}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(Count)}: {Count}";
            }
        }

        public class LayerData
        {
            public float Z { get; set; }
            public Exposure Exposure { get; set; }

            public override string ToString()
            {
                return $"{nameof(Z)}: {Z}, {nameof(Exposure)}: {Exposure}";
            }
        }

        public class Properties
        {
            public Size Size { get; set; } = new Size();
            public Exposure Exposure { get; set; } = new Exposure();
            public Bottom Bottom { get; set; } = new Bottom();
            public byte AntiAliasLevel { get; set; } = 1;

            public override string ToString()
            {
                return $"{nameof(Size)}: {Size}, {nameof(Exposure)}: {Exposure}, {nameof(Bottom)}: {Bottom}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}";
            }
        }

        public class Settings
        {
            public Properties Properties { get; set; } = new();
            public List<LayerData> Layers { get; set; } = new();

            public override string ToString()
            {
                return $"{nameof(Properties)}: {Properties}, {nameof(Layers)}: {Layers.Count}";
            }
        }

        #endregion

        #region Properties
        public Settings JsonSettings { get; set; } = new();

        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new(typeof(UVJFile), "uvj", "UVJ")
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
            PrintParameterModifier.BottomLayerCount,

            PrintParameterModifier.BottomLightOffDelay,
            PrintParameterModifier.LightOffDelay,

            PrintParameterModifier.BottomExposureTime,
            PrintParameterModifier.ExposureTime,

            PrintParameterModifier.BottomLiftHeight,
            PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.BottomRetractSpeed,
            PrintParameterModifier.RetractSpeed,

            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,
        };

        public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
            PrintParameterModifier.LightOffDelay,
            PrintParameterModifier.ExposureTime,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
            
            
            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,
        };

        public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } =
        {
            new(400, 400),
            new(800, 480)
        };

        public override uint ResolutionX
        {
            get => JsonSettings.Properties.Size.X;
            set
            {
                JsonSettings.Properties.Size.X = (ushort) value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => JsonSettings.Properties.Size.Y;
            set
            {
                JsonSettings.Properties.Size.Y = (ushort) value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayWidth
        {
            get => JsonSettings.Properties.Size.Millimeter.X;
            set
            {
                JsonSettings.Properties.Size.Millimeter.X = (float) Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float DisplayHeight
        {
            get => JsonSettings.Properties.Size.Millimeter.Y;
            set
            {
                JsonSettings.Properties.Size.Millimeter.Y = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override Enumerations.FlipDirection DisplayMirror { get; set; }

        public override byte AntiAliasing
        {
            get => JsonSettings.Properties.AntiAliasLevel;
            set => base.AntiAliasing = JsonSettings.Properties.AntiAliasLevel = value.Clamp(1, 16);
        }

        public override float LayerHeight
        {
            get => JsonSettings.Properties.Size.LayerHeight;
            set
            {
                JsonSettings.Properties.Size.LayerHeight = Layer.RoundHeight(value);
                RaisePropertyChanged();
            }
        }

        public override uint LayerCount
        {
            get => base.LayerCount;
            set => base.LayerCount = JsonSettings.Properties.Size.Layers = base.LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => JsonSettings.Properties.Bottom.Count;
            set => base.BottomLayerCount = JsonSettings.Properties.Bottom.Count = value;
        }

        public override float LightOffDelay
        {
            get => JsonSettings.Properties.Exposure.LightOffTime;
            set => base.LightOffDelay = JsonSettings.Properties.Exposure.LightOffTime = (float)Math.Round(value, 2);
        }

        public override float BottomWaitTimeBeforeCure
        {
            get => base.BottomWaitTimeBeforeCure;
            set
            {
                SetBottomLightOffDelay(value);
                base.BottomWaitTimeBeforeCure = value;
            }
        }

        public override float WaitTimeBeforeCure
        {
            get => base.WaitTimeBeforeCure;
            set
            {
                SetNormalLightOffDelay(value);
                base.WaitTimeBeforeCure = value;
            }
        }

        public override float BottomLiftHeight
        {
            get => JsonSettings.Properties.Bottom.LiftHeight;
            set => base.BottomLiftHeight = JsonSettings.Properties.Bottom.LiftHeight = (float)Math.Round(value, 2);
        }

        public override float BottomExposureTime
        {
            get => JsonSettings.Properties.Bottom.LightOnTime;
            set => base.BottomExposureTime = JsonSettings.Properties.Bottom.LightOnTime = (float)Math.Round(value, 2);
        }

        public override float ExposureTime
        {
            get => JsonSettings.Properties.Exposure.LightOnTime;
            set => base.ExposureTime = JsonSettings.Properties.Exposure.LightOnTime = (float)Math.Round(value, 2);
        }

        public override float BottomLightOffDelay
        {
            get => JsonSettings.Properties.Bottom.LightOffTime;
            set => base.BottomLightOffDelay = JsonSettings.Properties.Bottom.LightOffTime = (float)Math.Round(value, 2);
        }

        public override float LiftHeight
        {
            get => JsonSettings.Properties.Exposure.LiftHeight;
            set => base.LiftHeight = JsonSettings.Properties.Exposure.LiftHeight = (float)Math.Round(value, 2);
        }

        public override float BottomLiftSpeed
        {
            get => JsonSettings.Properties.Bottom.LiftSpeed;
            set => base.BottomLiftSpeed = JsonSettings.Properties.Bottom.LiftSpeed = (float)Math.Round(value, 2);
        }

        public override float LiftSpeed
        {
            get => JsonSettings.Properties.Exposure.LiftSpeed;
            set => base.LiftSpeed = JsonSettings.Properties.Exposure.LiftSpeed = (float)Math.Round(value, 2);
        }

        public override float BottomRetractSpeed
        {
            get => JsonSettings.Properties.Bottom.RetractSpeed;
            set => base.BottomRetractSpeed = JsonSettings.Properties.Bottom.RetractSpeed = (float)Math.Round(value, 2);
        }

        public override float RetractSpeed
        {
            get => JsonSettings.Properties.Exposure.RetractSpeed;
            set => base.RetractSpeed = JsonSettings.Properties.Exposure.RetractSpeed = (float)Math.Round(value, 2);
        }

        public override byte BottomLightPWM
        {
            get => JsonSettings.Properties.Bottom.LightPWM;
            set => base.BottomLightPWM = JsonSettings.Properties.Bottom.LightPWM = value;
        }

        public override byte LightPWM
        {
            get => JsonSettings.Properties.Exposure.LightPWM;
            set => base.LightPWM = JsonSettings.Properties.Exposure.LightPWM = value;
        }

        public override object[] Configs => new[] {(object) JsonSettings.Properties.Size, JsonSettings.Properties.Size.Millimeter, JsonSettings.Properties.Bottom, JsonSettings.Properties.Exposure};
        #endregion

        #region Methods

        public override void Clear()
        {
            base.Clear();
            JsonSettings.Layers = new List<LayerData>();
        }

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            // Redo layer data
            JsonSettings.Layers.Clear();
            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                JsonSettings.Layers.Add(new LayerData
                {
                    Z = layer.PositionZ,
                    Exposure = new Exposure
                    {
                        LiftHeight = layer.LiftHeight,
                        LiftSpeed = layer.LiftSpeed,
                        RetractHeight = layer.LiftHeight,
                        RetractSpeed = layer.RetractSpeed,
                        LightOffTime = layer.LightOffDelay,
                        LightOnTime = layer.ExposureTime,
                        LightPWM = layer.LightPWM
                    }
                });
            }

            using var outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create);
            outputFile.PutFileContent(FileConfigName, JsonConvert.SerializeObject(JsonSettings, Formatting.Indented), ZipArchiveMode.Create);

            if (CreatedThumbnailsCount > 0)
            {
                using var stream = outputFile.CreateEntry(FilePreviewTinyName).Open();
                stream.WriteBytes(Thumbnails[0].GetPngByes());
                stream.Close();
            }

            if (CreatedThumbnailsCount > 1)
            {
                using var stream = outputFile.CreateEntry(FilePreviewHugeName).Open();
                stream.WriteBytes(Thumbnails[1].GetPngByes());
                stream.Close();
            }

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                progress.Token.ThrowIfCancellationRequested();

                Layer layer = this[layerIndex];

                var layerimagePath = $"{FolderImageName}/{layerIndex:D8}.png";
                outputFile.PutFileContent(layerimagePath, layer.CompressedBytes, ZipArchiveMode.Create);
                    
                progress++;
            }
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using (var inputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Read))
            {
                var entry = inputFile.GetEntry(FileConfigName);
                if (entry is null)
                {
                    Clear();
                    throw new FileLoadException($"{FileConfigName} not found", fileFullPath);
                }

                JsonSettings = Helpers.JsonDeserializeObject<Settings>(entry.Open());
                
                LayerManager.Init(JsonSettings.Properties.Size.Layers);

                entry = inputFile.GetEntry(FilePreviewTinyName);
                if (entry is not null)
                {
                    using var stream = entry.Open();
                    Mat image = new();
                    CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, image);
                    Thumbnails[0] = image;
                    stream.Close();
                }

                entry = inputFile.GetEntry(FilePreviewHugeName);
                if (entry is not null)
                {
                    using var stream = entry.Open();
                    Mat image = new();
                    CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, image);
                    Thumbnails[1] = image;
                    stream.Close();
                }

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    entry = inputFile.GetEntry($"{FolderImageName}/{layerIndex:D8}.png");
                    if (entry is null) continue;

                    using var stream = entry.Open();
                    this[layerIndex] = new Layer(layerIndex, stream, LayerManager)
                    {
                        PositionZ = JsonSettings.Layers?.Count > layerIndex ? JsonSettings.Layers[(int) layerIndex].Z : GetHeightFromLayer(layerIndex),
                        LiftHeight = JsonSettings.Layers?.Count > layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LiftHeight : GetInitialLayerValueOrNormal(layerIndex, BottomLiftHeight, LiftHeight),
                        LiftSpeed = JsonSettings.Layers?.Count > layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LiftSpeed : GetInitialLayerValueOrNormal(layerIndex, BottomLiftSpeed, LiftSpeed),
                        RetractSpeed = JsonSettings.Layers?.Count > layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.RetractSpeed : RetractSpeed,
                        LightOffDelay = JsonSettings.Layers?.Count > layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LightOffTime : GetInitialLayerValueOrNormal(layerIndex, BottomLightOffDelay, LightOffDelay),
                        ExposureTime = JsonSettings.Layers?.Count > layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LightOnTime : GetInitialLayerValueOrNormal(layerIndex, BottomExposureTime, ExposureTime),
                        LightPWM = JsonSettings.Layers?.Count > layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LightPWM : GetInitialLayerValueOrNormal(layerIndex, BottomLightPWM, LightPWM),
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

            using var outputFile = ZipFile.Open(FileFullPath, ZipArchiveMode.Update);
            outputFile.PutFileContent(FileConfigName, JsonConvert.SerializeObject(JsonSettings, Formatting.Indented), ZipArchiveMode.Update);

            //Decode(FileFullPath, progress);
        }
        #endregion
    }
}
