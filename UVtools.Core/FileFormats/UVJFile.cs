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
            public Properties Properties { get; set; } = new Properties();
            public List<LayerData> Layers { get; set; } = new List<LayerData>();

            public override string ToString()
            {
                return $"{nameof(Properties)}: {Properties}, {nameof(Layers)}: {Layers.Count}";
            }
        }

        #endregion

        #region Properties
        public Settings JsonSettings { get; set; } = new Settings();

        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("uvj", "UVJ Files")
        };

        public override Type[] ConvertToFormats { get; } = null;

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

        public override byte ThumbnailsCount { get; } = 2;

        public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } = {new System.Drawing.Size(400, 400), new System.Drawing.Size(800, 480) };

        public override uint ResolutionX
        {
            get => JsonSettings.Properties.Size.X;
            set => JsonSettings.Properties.Size.X = (ushort) value;
        }

        public override uint ResolutionY
        {
            get => JsonSettings.Properties.Size.Y;
            set => JsonSettings.Properties.Size.Y = (ushort) value;
        }

        public override float DisplayWidth
        {
            get => JsonSettings.Properties.Size.Millimeter.X;
            set => JsonSettings.Properties.Size.Millimeter.X = value;
        }
        public override float DisplayHeight
        {
            get => JsonSettings.Properties.Size.Millimeter.Y;
            set => JsonSettings.Properties.Size.Millimeter.Y = value;
        }

        public override byte AntiAliasing => JsonSettings.Properties.AntiAliasLevel;

        public override float LayerHeight
        {
            get => JsonSettings.Properties.Size.LayerHeight;
            set => JsonSettings.Properties.Size.LayerHeight = value;
        }

        public override uint LayerCount
        {
            set
            {
                JsonSettings.Properties.Size.Layers = LayerCount;
                JsonSettings.Layers.Clear();
            }
        }

        public override ushort BottomLayerCount
        {
            get => JsonSettings.Properties.Bottom.Count;
            set => JsonSettings.Properties.Bottom.Count = value;
        }

        public override float BottomExposureTime
        {
            get => JsonSettings.Properties.Bottom.LightOnTime;
            set => JsonSettings.Properties.Bottom.LightOnTime = value;
        }

        public override float ExposureTime
        {
            get => JsonSettings.Properties.Exposure.LightOnTime;
            set => JsonSettings.Properties.Exposure.LightOnTime = value;
        }

        public override float BottomLayerOffTime
        {
            get => JsonSettings.Properties.Bottom.LightOffTime;
            set => JsonSettings.Properties.Bottom.LightOffTime = value;
        }

        public override float LayerOffTime
        {
            get => JsonSettings.Properties.Exposure.LightOffTime;
            set => JsonSettings.Properties.Exposure.LightOffTime = value;
        }

        public override float BottomLiftHeight
        {
            get => JsonSettings.Properties.Bottom.LiftHeight;
            set => JsonSettings.Properties.Bottom.LiftHeight = value;
        }

        public override float LiftHeight
        {
            get => JsonSettings.Properties.Exposure.LiftHeight;
            set => JsonSettings.Properties.Exposure.LiftHeight = value;
        }

        public override float BottomLiftSpeed
        {
            get => JsonSettings.Properties.Bottom.LiftSpeed;
            set => JsonSettings.Properties.Bottom.LiftSpeed = value;
        }

        public override float LiftSpeed
        {
            get => JsonSettings.Properties.Exposure.LiftSpeed;
            set => JsonSettings.Properties.Exposure.LiftSpeed = value;
        }

        public override float RetractSpeed
        {
            get => JsonSettings.Properties.Exposure.RetractSpeed;
            set => JsonSettings.Properties.Exposure.RetractSpeed = value;
        }

        public override byte BottomLightPWM
        {
            get => JsonSettings.Properties.Bottom.LightPWM;
            set => JsonSettings.Properties.Bottom.LightPWM = value;
        }

        public override byte LightPWM
        {
            get => JsonSettings.Properties.Exposure.LightPWM;
            set => JsonSettings.Properties.Exposure.LightPWM = value;
        }

        public override float PrintTime => 0;

        public override float UsedMaterial => 0;

        public override float MaterialCost => 0;

        public override string MaterialName => null;

        public override string MachineName => null;

        public override object[] Configs => new[] {(object) JsonSettings.Properties.Size, JsonSettings.Properties.Size.Millimeter, JsonSettings.Properties.Bottom, JsonSettings.Properties.Exposure};
        #endregion

        #region Methods

        public override void Clear()
        {
            base.Clear();
            JsonSettings.Layers = new List<LayerData>();
        }

        public override void Encode(string fileFullPath, OperationProgress progress = null)
        {
            base.Encode(fileFullPath, progress);

            if (JsonSettings.Layers.Count == 0)
            {
                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    JsonSettings.Layers.Add(new LayerData
                    {
                        Z = this[layerIndex].PositionZ,
                        Exposure = new Exposure
                        {
                            LiftHeight = GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.LiftHeight, JsonSettings.Properties.Exposure.LiftHeight),
                            LiftSpeed = GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.LiftSpeed, JsonSettings.Properties.Exposure.LiftSpeed),
                            LightOnTime = GetInitialLayerValueOrNormal(layerIndex, BottomExposureTime, ExposureTime),
                            LightOffTime = GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.LightOffTime, JsonSettings.Properties.Exposure.LightOffTime),
                            LightPWM = GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.LightPWM, JsonSettings.Properties.Exposure.LightPWM),
                            RetractSpeed = GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.RetractSpeed, JsonSettings.Properties.Exposure.RetractSpeed),
                        }
                    });
                }
            }

            using (ZipArchive outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create))
            {
                outputFile.PutFileContent(FileConfigName, JsonConvert.SerializeObject(JsonSettings), ZipArchiveMode.Create);

                if (CreatedThumbnailsCount > 0)
                {
                    using (Stream stream = outputFile.CreateEntry(FilePreviewTinyName).Open())
                    {
                        using (var vec = new VectorOfByte())
                        {
                            CvInvoke.Imencode(".png", Thumbnails[0], vec);
                            stream.WriteBytes(vec.ToArray());
                            stream.Close();
                        }
                    }
                }

                if (CreatedThumbnailsCount > 1)
                {
                    using (Stream stream = outputFile.CreateEntry(FilePreviewHugeName).Open())
                    {
                        using (var vec = new VectorOfByte())
                        {
                            CvInvoke.Imencode(".png", Thumbnails[1], vec);
                            stream.WriteBytes(vec.ToArray());
                            stream.Close();
                        }
                    }
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
                var entry = inputFile.GetEntry(FileConfigName);
                if (ReferenceEquals(entry, null))
                {
                    Clear();
                    throw new FileLoadException($"{FileConfigName} not found", fileFullPath);
                }

                JsonSettings = Helpers.JsonDeserializeObject<Settings>(entry.Open());

                LayerManager = new LayerManager(JsonSettings.Properties.Size.Layers, this);

                entry = inputFile.GetEntry(FilePreviewTinyName);
                if (!ReferenceEquals(entry, null))
                {
                    using (Stream stream = entry.Open())
                    {
                        CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, Thumbnails[0]);
                        stream.Close();
                    }
                }

                entry = inputFile.GetEntry(FilePreviewHugeName);
                if (!ReferenceEquals(entry, null))
                {
                    using (Stream stream = entry.Open())
                    {
                        CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, Thumbnails[1]);
                        stream.Close();
                    }
                }

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    entry = inputFile.GetEntry($"{FolderImageName}/{layerIndex:D8}.png");
                    if (ReferenceEquals(entry, null)) continue;

                    LayerManager[layerIndex] = new Layer(layerIndex, entry.Open(), entry.Name)
                    {
                        PositionZ = JsonSettings.Layers.Count >= layerIndex ? JsonSettings.Layers[(int) layerIndex].Z : GetHeightFromLayer(layerIndex),
                        ExposureTime = JsonSettings.Layers.Count >= layerIndex ? JsonSettings.Layers[(int)layerIndex].Exposure.LightOnTime : GetInitialLayerValueOrNormal(layerIndex, BottomExposureTime, ExposureTime)
                    };
                }
                
                progress.ProcessedItems++;
            }

            LayerManager.GetBoundingRectangle(progress);
        }

        public override byte SetValuesFromPrintParametersModifiers()
        {
            var count = base.SetValuesFromPrintParametersModifiers();
            if (count == 0) return 0;

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                // Bottom : others
                if (JsonSettings.Layers.Count <= layerIndex) break;

                JsonSettings.Layers[(int)layerIndex].Exposure.LiftHeight =
                    GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.LiftHeight, JsonSettings.Properties.Exposure.LiftHeight);
                JsonSettings.Layers[(int)layerIndex].Exposure.LightPWM =
                    GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.LightPWM, JsonSettings.Properties.Exposure.LightPWM);
                JsonSettings.Layers[(int)layerIndex].Exposure.LiftSpeed =
                    GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.LiftSpeed, JsonSettings.Properties.Exposure.LiftSpeed);
                JsonSettings.Layers[(int)layerIndex].Exposure.LightOnTime =
                    GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.LightOnTime, JsonSettings.Properties.Exposure.LightOnTime);
                JsonSettings.Layers[(int)layerIndex].Exposure.RetractSpeed =
                    GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.RetractSpeed, JsonSettings.Properties.Exposure.RetractSpeed);
                JsonSettings.Layers[(int)layerIndex].Exposure.LightOffTime =
                    GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.LightOffTime, JsonSettings.Properties.Exposure.LightOffTime);
                JsonSettings.Layers[(int)layerIndex].Exposure.RetractHeight =
                    GetInitialLayerValueOrNormal(layerIndex, JsonSettings.Properties.Bottom.RetractHeight, JsonSettings.Properties.Exposure.RetractHeight);
            }

            return count;
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
                outputFile.PutFileContent(FileConfigName, JsonConvert.SerializeObject(JsonSettings), ZipArchiveMode.Update);
            }

            //Decode(FileFullPath, progress);
        }

        public override bool Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
            throw new NotImplementedException();
        }
        
        #endregion
    }
}
