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
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class ChituboxZipFile : FileFormat
    {
        #region Constants

        public const string GCodeFilename = "run.gcode";
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
            [DisplayName("normalExposureTime")] public float ExposureTime { get; set; } = 7; // 35s
            [DisplayName("bottomLayExposureTime")] public float BottomLayExposureTime { get; set; } = 35; // 35s
            [DisplayName("bottomLayerExposureTime")] public float BottomExposureTime { get; set; } = 35; // 35s
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
            [DisplayName("bottomLightOffTime")] public float BottomLightOffDelay { get; set; }
            [DisplayName("lightOffTime")] public float LightOffDelay { get; set; }
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

            PrintParameterModifier.BottomLiftHeight,
            PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.BottomLightOffDelay,
            PrintParameterModifier.LightOffDelay,

            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,
        };

        public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
            PrintParameterModifier.ExposureSeconds,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.LightOffDelay,
            PrintParameterModifier.LightPWM,
        };

        public override Size[] ThumbnailsOriginalSize { get; } =
        {
            new(954, 850), 
            new(168, 150)
        };

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

        public override float MachineZ
        {
            get => HeaderSettings.MachineZ > 0 ? HeaderSettings.MachineZ : base.MachineZ;
            set => base.MachineZ = HeaderSettings.MachineZ = (float)Math.Round(value, 2);
        }

        public override bool MirrorDisplay
        {
            get => HeaderSettings.Mirror > 0;
            set
            {
                HeaderSettings.ProjectType = value ? "LCD_mirror" : "Normal";
                HeaderSettings.Mirror = (byte)(value ? 1 : 0);
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
                HeaderSettings.LayerHeight = Layer.RoundHeight(value);
                RaisePropertyChanged();
            }
        }

        public override uint LayerCount
        {
            get => base.LayerCount;
            set => base.LayerCount = HeaderSettings.LayerCount = base.LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => HeaderSettings.BottomLayerCount;
            set => base.BottomLayerCount = HeaderSettings.BottomLayerCount = HeaderSettings.BottomLayCount = value;
        }

        public override float BottomExposureTime
        {
            get => HeaderSettings.BottomExposureTime;
            set => base.BottomExposureTime = HeaderSettings.BottomExposureTime = HeaderSettings.BottomLayExposureTime = (float)Math.Round(value, 2);
        }

        public override float ExposureTime
        {
            get => HeaderSettings.ExposureTime;
            set => base.ExposureTime = HeaderSettings.ExposureTime = (float)Math.Round(value, 2);
        }

        public override float BottomLiftHeight
        {
            get => HeaderSettings.BottomLiftHeight;
            set => base.BottomLiftHeight = HeaderSettings.BottomLiftHeight = (float)Math.Round(value, 2);
        }

        public override float LiftHeight
        {
            get => HeaderSettings.LiftHeight;
            set => base.LiftHeight = HeaderSettings.LiftHeight = (float)Math.Round(value, 2);
        }

        public override float BottomLiftSpeed
        {
            get => HeaderSettings.BottomLiftSpeed;
            set => base.BottomLiftSpeed = HeaderSettings.BottomLiftSpeed = (float)Math.Round(value, 2);
        }

        public override float LiftSpeed
        {
            get => HeaderSettings.LiftSpeed;
            set => base.LiftSpeed = HeaderSettings.LiftSpeed = (float)Math.Round(value, 2);
        }

        public override float RetractSpeed
        {
            get => HeaderSettings.RetractSpeed;
            set => base.RetractSpeed = HeaderSettings.RetractSpeed = (float)Math.Round(value, 2);
        }

        public override float BottomLightOffDelay
        {
            get => HeaderSettings.BottomLightOffDelay;
            set => base.BottomLightOffDelay = HeaderSettings.BottomLightOffDelay = (float)Math.Round(value, 2);
        }

        public override float LightOffDelay
        {
            get => HeaderSettings.LightOffDelay;
            set => base.LightOffDelay = HeaderSettings.LightOffDelay = (float)Math.Round(value, 2);
        }

        public override byte BottomLightPWM
        {
            get => HeaderSettings.BottomLightPWM;
            set => base.BottomLightPWM = HeaderSettings.BottomLightPWM = value;
        }

        public override byte LightPWM
        {
            get => HeaderSettings.LightPWM;
            set => base.LightPWM = HeaderSettings.LightPWM = value;
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
            set => base.MachineName = HeaderSettings.MachineType = value;
        }

        public override object[] Configs => new object[] { HeaderSettings };

        public override bool SupportsGCode => base.SupportsGCode && !IsPHZZip;

        public bool IsPHZZip;
        #endregion

        public ChituboxZipFile()
        {
            GCode = new GCodeBuilder
            {
                UseComments = true,
                GCodePositioningType = GCodeBuilder.GCodePositioningTypes.Absolute,
                GCodeSpeedUnit = GCodeBuilder.GCodeSpeedUnits.MillimetersPerMinute,
                GCodeTimeUnit = GCodeBuilder.GCodeTimeUnits.Milliseconds,
                GCodeShowImageType = GCodeBuilder.GCodeShowImageTypes.FilenameNonZeroPNG
            };
        }

        #region Methods

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            using (ZipArchive outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create))
            {
                if (Thumbnails.Length > 0 && !ReferenceEquals(Thumbnails[0], null))
                {
                    using (Stream stream = outputFile.CreateEntry("preview.png").Open())
                    {
                        stream.WriteBytes(Thumbnails[0].GetPngByes());
                        stream.Close();
                    }
                }

                if (Thumbnails.Length > 1 && !ReferenceEquals(Thumbnails[1], null))
                {
                    using (Stream stream = outputFile.CreateEntry("preview_cropping.png").Open())
                    {
                        using (var vec = new VectorOfByte())
                        {
                            stream.WriteBytes(Thumbnails[1].GetPngByes());
                            stream.Close();
                        }
                    }
                }

                if (!IsPHZZip)
                {
                    RebuildGCode();
                    outputFile.PutFileContent(GCodeFilename, GCodeStr, ZipArchiveMode.Create);
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
                var entry = inputFile.GetEntry(GCodeFilename);
                if (entry is not null)
                {
                    //Clear();
                    //throw new FileLoadException("run.gcode not found", fileFullPath);
                    using (TextReader tr = new StreamReader(entry.Open()))
                    {
                        string line;
                        GCode.Clear();
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
                                if (displayNameAttribute is null) continue;
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

                LayerManager.Init(HeaderSettings.LayerCount);

                progress.ItemCount = LayerCount;

                var gcode = GCodeStr;
                float lastPostZ = LayerHeight;

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    if (progress.Token.IsCancellationRequested) break;
                    entry = inputFile.GetEntry($"{layerIndex+1}.png");
                    if (entry is null)
                    {
                        Clear();
                        throw new FileLoadException($"Layer {layerIndex+1} not found", fileFullPath);
                    }

                    using var stream = entry.Open();
                    this[layerIndex] = new Layer(layerIndex, stream, LayerManager);

                    progress++;
                }

                if (IsPHZZip) // PHZ file
                {
                    LayerManager.RebuildLayersProperties();
                }
                else
                {
                    GCode.ParseLayersFromGCode(this);
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
                if (entry is not null)
                {
                    Thumbnails[0] = new Mat();
                    CvInvoke.Imdecode(entry.Open().ToArray(), ImreadModes.AnyColor, Thumbnails[0]);
                }

                entry = inputFile.GetEntry("preview_cropping.png");
                if (entry is not null)
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
            GCode.RebuildGCode(this, new object[]{ HeaderSettings });
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
                    outputFile.PutFileContent(GCodeFilename, GCodeStr, ZipArchiveMode.Update);
                }
            }

            //Decode(FileFullPath, progress);
        }
        #endregion
    }
}
