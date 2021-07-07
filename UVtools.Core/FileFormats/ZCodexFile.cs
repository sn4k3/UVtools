/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Stitching;
using Emgu.CV.Util;
using Newtonsoft.Json;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class ZCodexFile : FileFormat
    {
        #region Constants

        private const string GCodeStart = "G28\nG21\nG91\nM17\n";
        private const string GCodeKeywordSlice = "<Slice>";
        private const string GCodeKeywordDelayBlank = "<Delay_blank>";
        private const string GCodeKeywordDelayModel = "<Delay_model>";
        private const string GCodeKeywordDelaySupportPart = "<Delay_support_part>";
        private const string GCodeKeywordDelaySupportFull = "<Delay_support_full>";
        private const string FolderImages = "ResinSlicesData";
        private const string FolderImageName = "Slice";
        #endregion

        #region Sub Classes

        public class ResinMetadata
        {
            public class LayerData
            {
                public uint Layer { get; set; }
                public float UsedMaterialVolume { get; set; }

            }

            public string Guid { get; set; } = "07452AC2-7494-4576-BA60-BFEA8815F917";
            public string Material { get; set; }
            public string MaterialId { get; set; }
            public float LayerThickness { get; set; }
            public uint PrintTime { get; set; }
            public uint LayerTime { get; set; }
            public uint BottomLayersTime { get; set; }
            public uint AdditionalSupportLayerTime { get; set; }
            public ushort BottomLayersNumber { get; set; }
            public uint BlankingLayerTime { get; set; }
            public float TotalMaterialVolumeUsed { get; set; }
            public float TotalMaterialWeightUsed { get; set; }
            public uint TotalLayersCount { get; set; }
            public bool DisableSettingsChanges { get; set; }

            public List<LayerData> Layers { get; set; } = new();
        }

        public class UserSettingsdata
        {
            public uint MaxLayer { get; set; }
            public string PrintTime { get; set; }
            public float MaterialVolume { get; set; }
            public byte IsAdvanced { get; set; }
            public string Printer { get; set; } = "Zortrax Inkspire";
            public string MaterialType { get; set; }
            public uint MaterialId { get; set; }
            public string LayerThickness { get; set; }
            public byte RaftEnabled { get; set; }
            public float RaftHeight { get; set; }
            public float RaftOffset { get; set; }
            public byte ModelLiftEnabled { get; set; }
            public float ModelLiftHeight { get; set; }
            public byte CrossSupportEnabled { get; set; }
            public uint LayerExposureTime { get; set; }
            //public uint LayerThicknessesDisplayTime { get; set; } arr
            public uint ExposureOffTime { get; set; } = 5;
            public uint BottomLayerExposureTime { get; set; }
            public ushort BottomLayersCount { get; set; }
            public byte SupportAdditionalExposureEnabled { get; set; }
            public uint SupportAdditionalExposureTime { get; set; }
            public float ZLiftDistance { get; set; } = 5;
            public float ZLiftRetractRate { get; set; } = 100;
            public float ZLiftFeedRate { get; set; } = 100;
            public byte AntiAliasing { get; set; } = 0;
            public float XCorrection { get; set; }
            public float YCorrection { get; set; }
            public byte HollowEnabled { get; set; }
            public float HollowThickness { get; set; }
            public byte InfillDensity { get; set; }
        }

        public class ZCodeMetadata
        {
            public class MaterialsData
            {
                public string ExtruderType { get; set; }
                public uint Id { get; set; }
                public string Name { get; set; }
                public uint Usage { get; set; }
                public uint Temperature { get; set; }
            }

            public string ZCodexVersion { get; set; } = "2.0.0.0";
            public string SoftwareVersion { get; set; } = "2.12.2.0";
            public string MinFirmwareVersion { get; set; } = "20013";
            public uint PrinterModelEnumId { get; set; } = 40;
            public string PrinterName { get; set; } = "Inkspire";

            public List<MaterialsData> Materials { get; set; } = new()
            {
                new MaterialsData
                {
                    Name = "",
                    ExtruderType = "MAIN",
                    Id = 0,
                    Usage = 0,
                    Temperature = 0
                }
            };
            public byte HeatbedTemperature { get; set; }
            public byte ChamberTemperature { get; set; }
            public uint CommandCount { get; set; }
            public uint PrintTime { get; set; }
            public float NozzleDiameter { get; set; }
            public string PrintBoundingBox { get; set; }
            public string Pauses { get; set; }
            public string MaterialUsages { get; set; }
        }

        public class LayerData
        {
            public int SupportLayerFileIndex { get; set; } = -1;
            public int LayerFileIndex { get; set; } = -1;
            public ZipArchiveEntry SupportLayerEntry { get; set; }
            public ZipArchiveEntry LayerEntry { get; set; }

            public bool HaveSupportLayer => !ReferenceEquals(SupportLayerEntry, null);
        }

        #endregion

        #region Properties
        public ResinMetadata ResinMetadataSettings { get; set; } = new();
        public UserSettingsdata UserSettings { get; set; } = new();
        public ZCodeMetadata ZCodeMetadataSettings { get; set; } = new();

        public List<LayerData> LayersSettings { get; } = new();

        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new("zcodex", "Z-Suite ZCodex")
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
            PrintParameterModifier.ExposureSeconds,


            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.LightOffDelay
        };

        public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.LightPWM,
        };

        public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } = {new(320, 180)};

        public override uint ResolutionX
        {
            get => 1440;
            set { }
        }

        public override uint ResolutionY
        {
            get => 2560;
            set { }
        }

        public override float DisplayWidth
        {
            get => 74.67f;
            set {}
        }
        public override float DisplayHeight
        {
            get => 132.88f;
            set { }
        }

        public override bool MirrorDisplay
        {
            get => true;
            set { }
        }

        public override byte AntiAliasing
        {
            get => UserSettings.AntiAliasing;
            set
            {
                UserSettings.AntiAliasing = value.Clamp(1, 16);
                RaisePropertyChanged();
            }

        }

        public override float LayerHeight
        {
            get => ResinMetadataSettings.LayerThickness;
            set
            {
                ResinMetadataSettings.LayerThickness = Layer.RoundHeight(value);
                UserSettings.LayerThickness = $"{ResinMetadataSettings.LayerThickness} mm";
                RaisePropertyChanged();
            }
        }

        public override uint LayerCount
        {
            get => base.LayerCount;
            set
            {
                base.LayerCount = ResinMetadataSettings.TotalLayersCount = base.LayerCount;
                UserSettings.MaxLayer = LastLayerIndex;
                 
            }
        }

        public override ushort BottomLayerCount
        {
            get => ResinMetadataSettings.BottomLayersNumber;
            set => base.BottomLayerCount = ResinMetadataSettings.BottomLayersNumber = UserSettings.BottomLayersCount = value;
        }

        public override float BottomExposureTime
        {
            get => TimeExtensions.MillisecondsToSeconds(UserSettings.BottomLayerExposureTime);
            set
            {
                ResinMetadataSettings.BottomLayersTime = UserSettings.BottomLayerExposureTime = TimeExtensions.SecondsToMillisecondsUint(value);
                base.BottomExposureTime = value;
            }
        }

        public override float ExposureTime
        {
            get => TimeExtensions.MillisecondsToSeconds(UserSettings.LayerExposureTime);
            set
            {
                ResinMetadataSettings.LayerTime = UserSettings.LayerExposureTime = TimeExtensions.SecondsToMillisecondsUint(value);
                base.ExposureTime = value;
            }
        }

        public override float BottomLiftHeight => LiftHeight;

        public override float LiftHeight
        {
            get => UserSettings.ZLiftDistance;
            set => base.LiftHeight = UserSettings.ZLiftDistance = (float)Math.Round(value, 2);
        }

        public override float BottomLiftSpeed => LiftSpeed;

        public override float LiftSpeed
        {
            get => UserSettings.ZLiftFeedRate;
            set => base.LiftSpeed = UserSettings.ZLiftFeedRate = (float)Math.Round(value, 2);
        }

        public override float RetractSpeed
        {
            get => UserSettings.ZLiftRetractRate;
            set => base.RetractSpeed = UserSettings.ZLiftRetractRate = (float)Math.Round(value, 2);
        }

        public override float BottomLightOffDelay => LightOffDelay;

        public override float LightOffDelay
        {
            get => TimeExtensions.MillisecondsToSeconds(ResinMetadataSettings.BlankingLayerTime);
            set
            {
                UserSettings.ExposureOffTime = ResinMetadataSettings.BlankingLayerTime = TimeExtensions.SecondsToMillisecondsUint(value);
                base.LightOffDelay = value;
            }
        }


        public override float PrintTime
        {
            get => ResinMetadataSettings.PrintTime;
            set
            {
                base.PrintTime = value;
                ResinMetadataSettings.PrintTime = (uint)base.PrintTime;
                TimeSpan ts = new(0, 0, (int)base.PrintTime);
                UserSettings.PrintTime = $"{ts.Hours}h {ts.Minutes}m";
            }
        }

        public override float MaterialMilliliters
        {
            get => base.MaterialMilliliters;
            set
            {
                base.MaterialMilliliters = value;
                ResinMetadataSettings.TotalMaterialVolumeUsed = base.MaterialMilliliters;
            }
        }

        public override float MaterialGrams
        {
            get => (float) Math.Round(ResinMetadataSettings.TotalMaterialWeightUsed, 3);
            set => base.MaterialGrams = ResinMetadataSettings.TotalMaterialWeightUsed = (float) Math.Round(value, 3);
        }

        public override string MaterialName
        {
            get => ResinMetadataSettings.Material;
            set => base.MaterialName = ResinMetadataSettings.Material = value;
        }

        public override string MachineName
        {
            get => ZCodeMetadataSettings.PrinterName;
            set => base.MachineName = ZCodeMetadataSettings.PrinterName = value;
        }

        public override object[] Configs => new[] {(object) ResinMetadataSettings, UserSettings, ZCodeMetadataSettings};
        #endregion

        #region Constructor

        public ZCodexFile()
        {
            GCode = new();
        }

        #endregion

        #region Methods

        public override void Clear()
        {
            base.Clear();
            LayersSettings.Clear();
        }

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress) 
        { 
            float usedMaterial = MaterialMilliliters / LayerCount;
            ResinMetadataSettings.Layers.Clear();
            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                ResinMetadataSettings.Layers.Add(new ResinMetadata.LayerData
                {
                    Layer = layerIndex,
                    UsedMaterialVolume = usedMaterial
                });
            }

            using (ZipArchive outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create))
            {
                outputFile.PutFileContent("ResinMetadata", JsonConvert.SerializeObject(ResinMetadataSettings, Formatting.Indented), ZipArchiveMode.Create);
                outputFile.PutFileContent("UserSettingsData", JsonConvert.SerializeObject(UserSettings, Formatting.Indented), ZipArchiveMode.Create);
                outputFile.PutFileContent("ZCodeMetadata", JsonConvert.SerializeObject(ZCodeMetadataSettings, Formatting.Indented), ZipArchiveMode.Create);

                if (CreatedThumbnailsCount > 0)
                {
                    using (var stream = outputFile.CreateEntry("Preview.png").Open())
                    {
                        stream.WriteBytes(Thumbnails[0].GetPngByes());
                        stream.Close();
                    }
                }

                GCode.Clear();

                float lastZPosition = 0;
                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();

                    Layer layer = this[layerIndex];
                    GCode.AppendLine($"{GCodeKeywordSlice} {layerIndex}");

                    if (lastZPosition != layer.PositionZ)
                    {
                        if (layer.LiftHeight > 0)
                        {
                            GCode.AppendLine($"G1 Z{layer.LiftHeight} F{layer.LiftSpeed}");
                            GCode.AppendLine($"G1 Z-{Layer.RoundHeight(layer.LiftHeight - layer.PositionZ + lastZPosition)} F{layer.RetractSpeed}");
                        }
                        else
                        {
                            GCode.AppendLine($"G1 Z{Layer.RoundHeight(layer.PositionZ- lastZPosition)} F{layer.LiftSpeed}");
                        }
                    }
                    /*else
                    {
                        //GCode.AppendLine($";G1 Z{LiftHeight} F{LiftSpeed}; Already here");
                        //GCode.AppendLine($";G1 Z-{LiftHeight - layer.PositionZ + lastZPosition} F{RetractSpeed}; Already here");
                    }*/

                    //GCode.AppendLine($"G1 Z{LiftHeight} F{LiftSpeed}");
                    //GCode.AppendLine($"G1 Z-{LiftHeight - LayerHeight} F{RetractSpeed}");
                    GCode.AppendLine(GCodeKeywordDelayBlank);
                    GCode.AppendLine("M106 S255");
                    GCode.AppendLine(GCodeKeywordDelayModel);
                    GCode.AppendLine("M106 S0");


                    var layerimagePath = $"{FolderImages}/{FolderImageName}{layerIndex:D5}.png";
                    using (Stream stream = outputFile.CreateEntry(layerimagePath).Open())
                    {
                        //image.Save(stream, Helpers.PngEncoder);
                        var byteArr = this[layerIndex].CompressedBytes;
                        stream.Write(byteArr, 0, byteArr.Length);
                        stream.Close();
                    }

                    lastZPosition = layer.PositionZ;

                    progress++;
                }

                GCode.AppendLine($"G1 Z40.0 F{UserSettings.ZLiftFeedRate}");
                GCode.AppendLine("M18");

                outputFile.PutFileContent("ResinGCodeData", GCode.ToString(), ZipArchiveMode.Create);
            }
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using (var inputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Read))
            {
                var entry = inputFile.GetEntry("ResinMetadata");
                if (entry is null)
                {
                    Clear();
                    throw new FileLoadException("ResinMetadata not found", fileFullPath);
                }

                ResinMetadataSettings = Helpers.JsonDeserializeObject<ResinMetadata>(entry.Open());

                entry = inputFile.GetEntry("UserSettingsData");
                if (entry is null)
                {
                    Clear();
                    throw new FileLoadException("UserSettingsData not found", fileFullPath);
                }

                UserSettings = Helpers.JsonDeserializeObject<UserSettingsdata>(entry.Open());

                entry = inputFile.GetEntry("ZCodeMetadata");
                if (entry is null)
                {
                    Clear();
                    throw new FileLoadException("ZCodeMetadata not found", fileFullPath);
                }

                ZCodeMetadataSettings = Helpers.JsonDeserializeObject<ZCodeMetadata>(entry.Open());

                entry = inputFile.GetEntry("ResinGCodeData");
                if (entry is null)
                {
                    Clear();
                    throw new FileLoadException("ResinGCodeData not found", fileFullPath);
                }

                LayerManager.Init(ResinMetadataSettings.TotalLayersCount);
                GCode.Clear();
                using (TextReader tr = new StreamReader(entry.Open()))
                {
                    string line;
                    int layerIndex = 0;
                    int layerFileIndex = 0;
                    string layerimagePath = null;
                    float currentHeight = 0;
                    while ((line = tr.ReadLine()) is not null)
                    {
                        GCode.AppendLine(line);
                        if (line.StartsWith(GCodeKeywordSlice))
                        {
                            layerFileIndex = int.Parse(line.Substring(GCodeKeywordSlice.Length));
                            layerimagePath = $"{FolderImages}/{FolderImageName}{layerFileIndex:D5}.png";
                            if (LayersSettings.Count - 1 < layerIndex) LayersSettings.Add(new LayerData());
                            continue;
                        }

                        if (line.StartsWith(GCodeKeywordDelaySupportPart))
                        {
                            LayersSettings[layerIndex].SupportLayerFileIndex = layerFileIndex;
                            LayersSettings[layerIndex].SupportLayerEntry = inputFile.GetEntry(layerimagePath);
                            continue;
                        }

                        /*
                         *
<Slice> 0
G1 Z5.0 F100.0
G1 Z-4.9 F100.0
<Delay_blank>
M106 S255
<Delay_support_full>
M106 S0
                         */

                        var gcode = GCodeStr;

                        if (line.StartsWith(GCodeKeywordDelaySupportFull) || line.StartsWith(GCodeKeywordDelayModel))
                        {
                            var startStr = $"{GCodeKeywordSlice} {layerIndex}";
                            var stripGcode = gcode.Substring(gcode.IndexOf(startStr, StringComparison.InvariantCultureIgnoreCase) + startStr.Length).Trim(' ', '\n', '\r', '\t');

                            float liftHeight = 0;
                            float liftSpeed = GetInitialLayerValueOrNormal((uint)layerIndex, BottomLiftSpeed, LiftSpeed);
                            float retractSpeed = RetractSpeed;
                            byte pwm = GetInitialLayerValueOrNormal((uint)layerIndex, BottomLightPWM, LightPWM); ;

                            //var currPos = Regex.Match(stripGcode, "G1 Z([+-]?([0-9]*[.])?[0-9]+)", RegexOptions.IgnoreCase);
                            var moveG1Regex = Regex.Match(stripGcode, @"G1 Z([+-]?([0-9]*[.])?[0-9]+) F(\d+)", RegexOptions.IgnoreCase);
                            var pwmM106Regex = Regex.Match(stripGcode, @"M106 S(\d+)", RegexOptions.IgnoreCase);

                            if (moveG1Regex.Success)
                            {
                                var liftHeightTemp = float.Parse(moveG1Regex.Groups[1].Value, CultureInfo.InvariantCulture);
                                var liftSpeedTemp = float.Parse(moveG1Regex.Groups[3].Value, CultureInfo.InvariantCulture);
                                moveG1Regex = moveG1Regex.NextMatch();
                                if (moveG1Regex.Success)
                                {
                                    liftHeight = liftHeightTemp;
                                    liftSpeed = liftSpeedTemp;
                                    var retractHeight = float.Parse(moveG1Regex.Groups[1].Value, CultureInfo.InvariantCulture);
                                    retractSpeed = float.Parse(moveG1Regex.Groups[3].Value, CultureInfo.InvariantCulture);
                                    currentHeight = Layer.RoundHeight(currentHeight + liftHeightTemp + retractHeight);
                                }
                                else
                                {
                                    currentHeight = Layer.RoundHeight(currentHeight + liftHeightTemp);
                                }
                            }

                            if (pwmM106Regex.Success)
                            {
                                pwm = byte.Parse(pwmM106Regex.Groups[1].Value);
                            }
                           
                            LayersSettings[layerIndex].LayerFileIndex = layerFileIndex;
                            LayersSettings[layerIndex].LayerEntry = inputFile.GetEntry(layerimagePath);
                            this[layerIndex] = new Layer((uint) layerIndex, LayersSettings[layerIndex].LayerEntry.Open(), LayerManager)
                            {
                                PositionZ = currentHeight,
                                LiftHeight = liftHeight,
                                LiftSpeed = liftSpeed,
                                RetractSpeed = retractSpeed,
                                LightPWM = pwm
                            };
                            layerIndex++;

                            progress++;
                        }
                    }

                    tr.Close();
                }

                entry = inputFile.GetEntry("Preview.png");
                if (!ReferenceEquals(entry, null))
                {
                    using Stream stream = entry.Open();
                    CvInvoke.Imdecode(stream.ToArray(), ImreadModes.AnyColor, Thumbnails[0]);
                    stream.Close();
                }
            }

            LayerManager.GetBoundingRectangle(progress);
        }

        public override void RebuildGCode()
        {
            var gcode = GCodeStr;
            gcode = Regex.Replace(gcode, @"Z[+]?([0-9]*\.[0-9]+|[0-9]+) F[+]?([0-9]*\.[0-9]+|[0-9]+)",
                $"Z{UserSettings.ZLiftDistance} F{UserSettings.ZLiftFeedRate}");

            gcode = Regex.Replace(gcode, @"Z-[-]?([0-9]*\.[0-9]+|[0-9]+) F[+]?([0-9]*\.[0-9]+|[0-9]+)",
                $"Z-{UserSettings.ZLiftDistance - LayerHeight} F{UserSettings.ZLiftRetractRate}");

            GCode.Clear();
            GCode.Append(gcode);
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
                outputFile.PutFileContent("ResinMetadata", JsonConvert.SerializeObject(ResinMetadataSettings, Formatting.Indented), ZipArchiveMode.Update);
                outputFile.PutFileContent("UserSettingsData", JsonConvert.SerializeObject(UserSettings, Formatting.Indented), ZipArchiveMode.Update);
                outputFile.PutFileContent("ZCodeMetadata", JsonConvert.SerializeObject(ZCodeMetadataSettings, Formatting.Indented), ZipArchiveMode.Update);
                outputFile.PutFileContent("ResinGCodeData", GCodeStr, ZipArchiveMode.Update);
            }

            //Decode(FileFullPath, progress);
        }

        #endregion
    }
}
