/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    [Serializable]
    [XmlRoot(ElementName = "Print")]
    public class ZCodePrint
    {
        [Serializable]
        [XmlRoot(ElementName = "Device")]
        public class ZcodePrintDevice
        {
            [XmlAttribute("z")]
            public ushort MachineZ { get; set; } = 220;

            [XmlAttribute("height")]
            public uint ResolutionY { get; set; } = 2400;

            [XmlAttribute("width")]
            public uint ResolutionX { get; set; } = 3840;

            [XmlAttribute("type")]
            public string MachineModel { get; set; } = "IBEE";

            [XmlAttribute("pixel_size")]
            public float PixelSize { get; set; } = 50;

        }

        [Serializable]
        [XmlRoot(ElementName = "Profile")]
        public class ZcodePrintProfile
        {
            [XmlAttribute("name")]
            public string Name { get; set; } = "UVtools";

            [Serializable]
            [XmlRoot(ElementName = "Slice")]
            public class ZcodePrintProfileSlice
            {
                [XmlAttribute("bottom_layers")]
                public ushort BottomLayerCount { get; set; } = FileFormat.DefaultBottomLayerCount;

                [XmlAttribute("exposure_bottom")]
                public uint BottomExposureTime { get; set; } = (uint) (FileFormat.DefaultBottomExposureTime * 1000);

                [XmlAttribute("exposure")]
                public uint ExposureTime { get; set; } = (uint) (FileFormat.DefaultExposureTime * 1000);

                [XmlAttribute("height_bottom")]
                public float BottomLiftHeight { get; set; } = FileFormat.DefaultBottomLiftHeight;

                [XmlAttribute("speed_bottom")]
                public float BottomLiftSpeed { get; set; } = FileFormat.DefaultBottomLiftSpeed;

                [XmlAttribute("height")]
                public float LiftHeight { get; set; } = FileFormat.DefaultLiftHeight;

                [XmlAttribute("speed")]
                public float LiftSpeed { get; set; } = FileFormat.DefaultLiftSpeed;

                [XmlAttribute("cooldown_bottom")]
                public uint BottomLightOffDelay { get; set; } = (uint) (FileFormat.DefaultBottomLightOffDelay * 1000);

                [XmlAttribute("cooldown")]
                public uint LightOffDelay { get; set; } = (uint) (FileFormat.DefaultLightOffDelay * 1000);

                [XmlAttribute("thickness")]
                public float LayerHeight { get; set; } = FileFormat.DefaultLayerHeight;

                [XmlAttribute("anti_aliasing_level")]
                public byte AntiAliasing { get; set; }

                [XmlAttribute("anti_aliasing_grey_level")]
                public byte AntiAliasingGrey { get; set; }

                [XmlAttribute("led_power")]
                public ushort LedPower { get; set; } = 300;
            }

            public ZcodePrintProfileSlice Slice { get; set; } = new();
        }

        [Serializable]
        [XmlRoot(ElementName = "Job")]
        public class ZcodePrintJob
        {
            [XmlElement("name")]
            public string StlName { get; set; }

            [XmlElement("previewImage")]
            public string PreviewImage { get; set; } = ZCodeFile.PreviewFilename;

            [XmlElement("layers")]
            public uint LayerCount { get; set; }

            [XmlElement("time")]
            public uint PrintTime { get; set; }

            [XmlElement("volumn")]
            public float VolumeMl { get; set; }

            [XmlElement("thickness")]
            public float LayerHeight { get; set; }

            [XmlElement("price")]
            public float Price { get; set; }

            [XmlElement("weight")]
            public float WeightG { get; set; }
        }

        public ZcodePrintDevice Device = new();

        public ZcodePrintProfile Profile = new();

        public ZcodePrintJob Job = new();

    }
    public class ZCodeFile : FileFormat
    {
        #region Constants

        public const string GCodeFilename = "lcd.gcode";
        public const string ManifestFilename = "task.xml";
        public const string PreviewFilename = "preview.png";

        public const string GCodeStart = "G21;{0}" +        // Set units to be mm
                                         "G90;{0}" +        // Absolute Positioning
                                         "M106 S0;{0}" +    // Light Off
                                         "G28 Z0;{0}";      // Home

        public const string GCodeEnd = "M106 S0{0}" +               // Light Off
                                       "G1 Z{1} F10;{0}" +          // Raize Z
                                       "M18;{0}";                   // Disable Motors

        public const string GCodeRSAPrivateKey = 
            "-----BEGIN PRIVATE KEY-----\n" +
            "MIIBVQIBADANBgkqhkiG9w0BAQEFAASCAT8wggE7AgEAAkEA6BnF3oT9Ap+OyZFs" +
            "p02ZRtTpbMUA96PpctTXev1cPCclLuowXWNa3f2JBpPbPrzMwLam3JSQN3HjvYfS" +
            "Vx7e6wIDAQABAkBibCNzQ/PCfAThxxBLNeXMmpbNsBDD8rcZIdaqaewF+UjUlqI7" +
            "eI0Yp2V2Ez28FjKCEiM34DxU9PZTzYS3rCShAiEA9qp9+RwYvcvQUXlT2bqKIo3s" +
            "xu7LQFM4VIddQ1SMVhsCIQDw4i74LqF285Iz77vw1MXE06LHKKBBBa3Air1QNBhn" +
            "cQIgYfO4TLk8lfoewovkoVyzSB+F/EWNjwC9KMwMXBVyGSsCIQDisxudOtV+y3C3" +
            "LFHmL3kI6lxxrsxTJXMGmAvfJYgqIQIhAKBtzYeZB46EToDSpeYlrMQitSWvjgNG" +
            "mw+8aB/HYEIr\n" +
            "-----END PRIVATE KEY-----";

        public const string GCodeRSAPublicKey =
            "-----BEGIN PUBLIC KEY-----\n" +
            "MFwwDQYJKoZIhvcNAQEBBQADSwAwSAJBAOgZxd6E/QKfjsmRbKdNmUbU6WzFAPej" +
            "6XLU13r9XDwnJS7qMF1jWt39iQaT2z68zMC2ptyUkDdx472H0lce3usCAwEAAQ==\n" +
            "-----END PUBLIC KEY-----";

        #endregion

        #region Sub Classes

        #endregion

        #region Properties
        public ZCodePrint ManifestFile { get; set; } = new ();

        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new("zcode", "UnizMaker ZCode files")
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

            //PrintParameterModifier.BottomLightPWM,
            //PrintParameterModifier.LightPWM,
        };

        public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
            PrintParameterModifier.ExposureSeconds,
            PrintParameterModifier.LightOffDelay,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
            //PrintParameterModifier.LightPWM,
        };

        public override byte ThumbnailsCount { get; } = 1;

        public override Size[] ThumbnailsOriginalSize { get; } = {new(640, 480)};

        public override uint ResolutionX
        {
            get => ManifestFile.Device.ResolutionX;
            set
            {
                ManifestFile.Device.ResolutionX = value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => ManifestFile.Device.ResolutionY;
            set
            {
                ManifestFile.Device.ResolutionY = value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayWidth
        {
            get => (float) Math.Round(ManifestFile.Device.ResolutionX * ManifestFile.Device.PixelSize / 1000, 2);
            set => RaisePropertyChanged();
        }

        public override float DisplayHeight
        {
            get => (float)Math.Round(ManifestFile.Device.ResolutionY * ManifestFile.Device.PixelSize / 1000, 2);
            set => RaisePropertyChanged();
        }

        public override float MaxPrintHeight
        {
            get => ManifestFile.Device.MachineZ > 0 ? ManifestFile.Device.MachineZ : base.MaxPrintHeight;
            set
            {
                ManifestFile.Device.MachineZ = (ushort) value;
                RaisePropertyChanged();
            }
        }

        public override bool MirrorDisplay => true;

        public override byte AntiAliasing
        {
            get => ManifestFile.Profile.Slice.AntiAliasing;
            set
            {
                ManifestFile.Profile.Slice.AntiAliasing = value.Clamp(1, 16);
                RaisePropertyChanged();
            }
        }

        public override float LayerHeight
        {
            get => ManifestFile.Job.LayerHeight;
            set
            {
                ManifestFile.Job.LayerHeight = ManifestFile.Profile.Slice.LayerHeight = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override uint LayerCount
        {
            set
            {
                ManifestFile.Job.LayerCount = LayerCount;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(NormalLayerCount));
            }
        }

        public override ushort BottomLayerCount
        {
            get => ManifestFile.Profile.Slice.BottomLayerCount;
            set
            {
                ManifestFile.Profile.Slice.BottomLayerCount = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomExposureTime
        {
            get => (float)Math.Round(ManifestFile.Profile.Slice.BottomExposureTime / 1000f, 2);
            set
            {
                ManifestFile.Profile.Slice.BottomExposureTime = (uint) (value * 1000);
                RaisePropertyChanged();
            }
        }

        public override float ExposureTime
        {
            get => (float)Math.Round(ManifestFile.Profile.Slice.ExposureTime / 1000f, 2);
            set
            {
                ManifestFile.Profile.Slice.ExposureTime = (uint)(value * 1000);
                RaisePropertyChanged();
            }
        }

        public override float BottomLightOffDelay
        {
            get => (float)Math.Round(ManifestFile.Profile.Slice.BottomLightOffDelay / 1000f, 2);
            set
            {
                ManifestFile.Profile.Slice.BottomLightOffDelay = (uint)(value * 1000);
                RaisePropertyChanged();
            }
        }

        public override float LightOffDelay
        {
            get => (float)Math.Round(ManifestFile.Profile.Slice.LightOffDelay / 1000f, 2);
            set
            {
                ManifestFile.Profile.Slice.LightOffDelay = (uint)(value * 1000);
                RaisePropertyChanged();
            }
        }

        public override float BottomLiftHeight
        {
            get => ManifestFile.Profile.Slice.BottomLiftHeight;
            set
            {
                ManifestFile.Profile.Slice.BottomLiftHeight = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float LiftHeight
        {
            get => ManifestFile.Profile.Slice.LiftHeight;
            set
            {
                ManifestFile.Profile.Slice.LiftHeight = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float BottomLiftSpeed
        {
            get => ManifestFile.Profile.Slice.BottomLiftSpeed;
            set
            {
                ManifestFile.Profile.Slice.BottomLiftSpeed = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float LiftSpeed
        {
            get => ManifestFile.Profile.Slice.LiftSpeed;
            set
            {
                ManifestFile.Profile.Slice.LiftSpeed = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        /*public override float RetractSpeed
        {
            get => HeaderSettings.RetractSpeed;
            set
            {
                HeaderSettings.RetractSpeed = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }*/

        public override float PrintTime
        {
            get => base.PrintTime;
            set
            {
                base.PrintTime = value;
                ManifestFile.Job.PrintTime = (uint) base.PrintTime;
            }
        }

        public override float MaterialMilliliters
        {
            get => base.MaterialMilliliters;
            set
            {
                base.MaterialMilliliters = value;
                ManifestFile.Job.VolumeMl = base.MaterialMilliliters;
            }
        }

        public override float MaterialGrams
        {
            get => ManifestFile.Job.WeightG;
            set
            {
                ManifestFile.Job.WeightG = (float)Math.Round(value, 3);
                RaisePropertyChanged();
            }
        }

        public override float MaterialCost
        {
            get => ManifestFile.Job.Price;
            set
            {
                ManifestFile.Job.Price = (float)Math.Round(value, 3);
                RaisePropertyChanged();
            }
        }

        /*public override string MaterialName
        {
            get => HeaderSettings.Resin;
            set
            {
                HeaderSettings.Resin = value;
                RaisePropertyChanged();
            }
        }*/

        public override string MachineName
        {
            get => ManifestFile.Device.MachineModel;
            set
            {
                ManifestFile.Device.MachineModel = value;
                RequireFullEncode = true;
                RaisePropertyChanged();
            }
        }

        public override object[] Configs => new object[] { ManifestFile.Device, ManifestFile.Job, ManifestFile.Profile.Slice };

        #endregion

        #region Methods

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            using (ZipArchive outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create))
            {
                if (Thumbnails.Length > 0 && Thumbnails[0] is not null)
                {
                    using var thumbnailsStream = outputFile.CreateEntry(PreviewFilename).Open();
                    using var vec = new VectorOfByte();
                    CvInvoke.Imencode(".png", Thumbnails[0], vec);
                    thumbnailsStream.WriteBytes(vec.ToArray());
                    thumbnailsStream.Close();
                }

                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    var layer = this[layerIndex];
                    outputFile.PutFileContent($"{layerIndex + 1}.png", layer.CompressedBytes, ZipArchiveMode.Create);
                    progress++;
                }

                XmlSerializer serializer = new XmlSerializer(ManifestFile.GetType());
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                ns.Add("", "");
                var entry = outputFile.CreateEntry(ManifestFilename);
                using (var stream = entry.Open())
                {
                    serializer.Serialize(stream, ManifestFile, ns);
                }

                outputFile.PutFileContent(GCodeFilename, EncryptGCode(progress), ZipArchiveMode.Create);
            }
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using (var inputFile = ZipFile.Open(FileFullPath, ZipArchiveMode.Read))
            {
                var entry = inputFile.GetEntry(ManifestFilename);
                if (entry is null)
                {
                    Clear();
                    throw new FileLoadException($"{ManifestFilename} not found", fileFullPath);
                }

                try
                {
                    var serializer = new XmlSerializer(ManifestFile.GetType());
                    using var stream = entry.Open();
                    ManifestFile = (ZCodePrint)serializer.Deserialize(stream);
                }
                catch (Exception e)
                {
                    Clear();
                    throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", fileFullPath);
                }

                entry = inputFile.GetEntry(GCodeFilename);
                if (entry is null)
                {
                    Clear();
                    throw new FileLoadException($"{GCodeFilename} not found", fileFullPath);
                }

                GCode = new();

                var encryptEngine = new RsaEngine();
                using var txtreader = new StringReader(GCodeRSAPublicKey);
                var keyParameter = (AsymmetricKeyParameter)new PemReader(txtreader).ReadObject();
                encryptEngine.Init(true, keyParameter);

                

                using (TextReader tr = new StreamReader(entry.Open()))
                {
                    string line;
                    progress.Reset("Decrypting GCode", (uint) (entry.Length / 88));
                    while ((line = tr.ReadLine()) != null)
                    {
                        if (string.IsNullOrEmpty(line)) continue;
                        if (!line.EndsWith("=="))
                        {
                            continue;
                        }
                        
                        byte[] data = System.Convert.FromBase64String(line);
                        var decodedBytes = encryptEngine.ProcessBlock(data, 0, data.Length);
                        decodedBytes = decodedBytes.Skip(2).SkipWhile(b => b == 255 || b == 0).ToArray();
                        GCode.AppendLine(Encoding.UTF8.GetString(decodedBytes));
                        
                        progress++;
                    }

                    tr.Close();
                }

                LayerManager = new LayerManager(ManifestFile.Job.LayerCount, this);
                progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

                var gcode = GCode.ToString();
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

                    var startStr = $"M6054 \"{layerIndex+1}.png\"";
                    gcode = gcode.Substring(gcode.IndexOf(startStr, StringComparison.InvariantCultureIgnoreCase) + startStr.Length + 1);
                    var stripGcode = gcode.Substring(0, gcode.IndexOf("M106 S0")).Trim(' ', '\n', '\r', '\t');
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

                    if (moveG0Regex.Success)
                    {
                        float liftHeightTemp = float.Parse(moveG0Regex.Groups[1].Value, CultureInfo.InvariantCulture);
                        float liftSpeedTemp = float.Parse(moveG0Regex.Groups[3].Value, CultureInfo.InvariantCulture);
                        moveG0Regex = moveG0Regex.NextMatch();
                        if (moveG0Regex.Success)
                        {
                            float retractHeight = float.Parse(moveG0Regex.Groups[1].Value, CultureInfo.InvariantCulture);
                            retractSpeed = float.Parse(moveG0Regex.Groups[3].Value, CultureInfo.InvariantCulture) * 10;
                            liftHeight = (float) Math.Round(liftHeightTemp - retractHeight, 2);
                            liftSpeed = liftSpeedTemp * 10;
                            lastPostZ = posZ = retractHeight;
                        }
                        else
                        {
                            lastPostZ = posZ = liftHeightTemp;
                        }
                    }

                    if (pwmM106Regex.Success)
                    {
                        //pwm = byte.Parse(pwmM106Regex.Groups[1].Value);
                    }

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

               
                entry = inputFile.GetEntry(PreviewFilename);
                if (entry is not null)
                {
                    Thumbnails[0] = new Mat();
                    CvInvoke.Imdecode(entry.Open().ToArray(), ImreadModes.AnyColor, Thumbnails[0]);
                }
            }

            LayerManager.GetBoundingRectangle(progress);
        }

        public override void RebuildGCode()
        {
            GCode = new();

            GCode.AppendFormat(GCodeStart, Environment.NewLine);

            float lastZPosition = 0;

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                var layer = this[layerIndex];
                var exposureTime = layer.ExposureTime * 1000;
                var liftHeight = layer.LiftHeight;
                var liftZHeight = Math.Round(liftHeight + layer.PositionZ, 2);
                var liftSpeed = Math.Round(layer.LiftSpeed / 10, 2);
                var retractSpeed = Math.Round(layer.RetractSpeed / 10, 2);
                var lightOffDelay = layer.LightOffDelay * 1000;
                var pwmValue = layer.LightPWM;

                if (layer.ExposureTime > 0 && pwmValue > 0)
                {
                    GCode.AppendLine($"M6054 \"{layerIndex + 1}.png\";");
                }

                // Absolute gcode
                if (liftHeight > 0 && liftZHeight > layer.PositionZ)
                {
                    GCode.AppendLine($"G0 Z{liftZHeight} F{liftSpeed};");
                    GCode.AppendLine($"G0 Z{layer.PositionZ} F{retractSpeed};");
                }
                else if (lastZPosition < layer.PositionZ)
                {
                    GCode.AppendLine($"G0 Z{layer.PositionZ} F{retractSpeed};");
                }

                GCode.AppendLine($"G4 P{lightOffDelay};");

                if (layer.ExposureTime > 0 && pwmValue > 0)
                {
                    GCode.AppendLine($"M106 S300;");
                    GCode.AppendLine($"G4 P{exposureTime};");
                    GCode.AppendLine("M106 S0;");
                }

                lastZPosition = layer.PositionZ;
            }

            GCode.AppendFormat(GCodeEnd, Environment.NewLine, MaxPrintHeight);
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
            bool deleted;

            do
            {
                deleted = false;
                foreach (var zipentry in outputFile.Entries)
                {
                    if (!zipentry.Name.EndsWith(".gcode") && !zipentry.Name.EndsWith(".xml")) continue;
                    zipentry.Delete();
                    deleted = true;
                    break;
                }
            } while (deleted);

            XmlSerializer serializer = new XmlSerializer(ManifestFile.GetType());
            XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
            ns.Add("", "");
            var entry = outputFile.CreateEntry(ManifestFilename);
            using var stream = entry.Open();
            serializer.Serialize(stream, ManifestFile, ns);

            outputFile.PutFileContent(GCodeFilename, EncryptGCode(progress), ZipArchiveMode.Update);

            //Decode(FileFullPath, progress);
        }

        private string EncryptGCode(OperationProgress progress)
        {
            RebuildGCode();
            progress.Reset("Encrypting GCode", (uint) GCode.Length);
            StringBuilder sb = new();

            var encryptEngine = new RsaEngine();
            using var txtreader = new StringReader(GCodeRSAPrivateKey);
            var keyParameter = (AsymmetricKeyParameter)new PemReader(txtreader).ReadObject();
            encryptEngine.Init(true, keyParameter);

            using StringReader sr = new StringReader(GCode.ToString());
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                progress += (uint)line.Length;

                byte[] data = Encoding.UTF8.GetBytes(line);
                List<byte> padData = new(64) {0, 1, 0};
                padData.AddRange(data);

                if (padData.Count > 64)
                {
                    throw new ArgumentOutOfRangeException($"Too long gcode line to encrypt, got: {padData.Count} bytes while expecting less than 64 bytes");
                }

                while (padData.Count < 64)
                {
                    padData.Insert(2, 255);
                }

                var padDataArray = padData.ToArray();

                var encodedBytes = encryptEngine.ProcessBlock(padDataArray, 0, padDataArray.Length);
                sb.AppendLine(System.Convert.ToBase64String(encodedBytes));
            }

            return sb.ToString();
        }
        #endregion
    }
}
