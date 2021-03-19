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
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
using UVtools.Core.Objects;
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
                public ushort LedPower { get; set; } = ZCodeFile.MaxLEDPower;
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
        public const ushort MaxLEDPower = 300;

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
            PrintParameterModifier.LightOffDelay,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.LightPWM,
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
                ManifestFile.Job.LayerHeight = ManifestFile.Profile.Slice.LayerHeight = Layer.RoundHeight(value);
                RaisePropertyChanged();
            }
        }

        public override uint LayerCount
        {
            get => base.LayerCount;
            set => base.LayerCount = ManifestFile.Job.LayerCount = base.LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => ManifestFile.Profile.Slice.BottomLayerCount;
            set => base.BottomLayerCount = ManifestFile.Profile.Slice.BottomLayerCount = value;
        }

        public override float BottomExposureTime
        {
            get => TimeExtensions.MillisecondsToSeconds(ManifestFile.Profile.Slice.BottomExposureTime);
            set
            {
                ManifestFile.Profile.Slice.BottomExposureTime = TimeExtensions.SecondsToMillisecondsUint(value);
                base.BottomExposureTime = value;
            }
        }

        public override float ExposureTime
        {
            get => TimeExtensions.MillisecondsToSeconds(ManifestFile.Profile.Slice.ExposureTime);
            set
            {
                ManifestFile.Profile.Slice.ExposureTime = TimeExtensions.SecondsToMillisecondsUint(value);
                base.ExposureTime = value;
            }
        }

        public override float BottomLiftHeight
        {
            get => ManifestFile.Profile.Slice.BottomLiftHeight;
            set => base.BottomLiftHeight = ManifestFile.Profile.Slice.BottomLiftHeight = (float)Math.Round(value, 2);
        }

        public override float LiftHeight
        {
            get => ManifestFile.Profile.Slice.LiftHeight;
            set => base.LiftHeight = ManifestFile.Profile.Slice.LiftHeight = (float)Math.Round(value, 2);
        }

        public override float BottomLiftSpeed
        {
            get => ManifestFile.Profile.Slice.BottomLiftSpeed;
            set => base.BottomLiftSpeed = ManifestFile.Profile.Slice.BottomLiftSpeed = (float)Math.Round(value, 2);
        }

        public override float LiftSpeed
        {
            get => ManifestFile.Profile.Slice.LiftSpeed;
            set => base.LiftSpeed = ManifestFile.Profile.Slice.LiftSpeed = (float)Math.Round(value, 2);
        }

        public override float BottomLightOffDelay
        {
            get => TimeExtensions.MillisecondsToSeconds(ManifestFile.Profile.Slice.BottomLightOffDelay);
            set
            {
                ManifestFile.Profile.Slice.BottomLightOffDelay = TimeExtensions.SecondsToMillisecondsUint(value);
                base.BottomLightOffDelay = value;
            }
        }

        public override float LightOffDelay
        {
            get => (float)Math.Round(ManifestFile.Profile.Slice.LightOffDelay / 1000f, 2);
            set
            {
                ManifestFile.Profile.Slice.LightOffDelay = (uint)(value * 1000);
                base.LightOffDelay = value;
            }
        }

        public override byte LightPWM
        {
            get => (byte)(byte.MaxValue * ManifestFile.Profile.Slice.LedPower / MaxLEDPower);
            set
            {
                ManifestFile.Profile.Slice.LedPower = (ushort)(MaxLEDPower * value / byte.MaxValue);
                base.LightPWM = value;
                RaisePropertyChanged(nameof(BottomLightPWM));
            }
        }

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
            set => base.MaterialGrams = ManifestFile.Job.WeightG = (float)Math.Round(value, 3);
        }

        public override float MaterialCost
        {
            get => ManifestFile.Job.Price;
            set => base.MaterialCost = ManifestFile.Job.Price = (float)Math.Round(value, 3);
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
            set => base.MachineName = ManifestFile.Device.MachineModel = value;
        }

        public override object[] Configs => new object[] { ManifestFile.Device, ManifestFile.Job, ManifestFile.Profile.Slice };

        #endregion

        public ZCodeFile()
        {
            GCode.UseTailComma = true;
            GCode.UseComments = false;
            GCode.GCodePositioningType = GCodeBuilder.GCodePositioningTypes.Absolute;
            GCode.GCodeSpeedUnit = GCodeBuilder.GCodeSpeedUnits.CentimetersPerMinute;
            GCode.GCodeTimeUnit = GCodeBuilder.GCodeTimeUnits.Milliseconds;
            GCode.GCodeShowImageType = GCodeBuilder.GCodeShowImageTypes.FilenameNonZeroPNG;
            GCode.MaxLEDPower = MaxLEDPower;
        }

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
                        if (!line.EndsWith("==")) continue;
                        
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

                //var gcode = GCode.ToString();
                //float lastPostZ = LayerHeight;

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

                GCode.ParseLayersFromGCode(this);

                entry = inputFile.GetEntry(PreviewFilename);
                if (entry is not null)
                {
                    Thumbnails[0] = new Mat();
                    CvInvoke.Imdecode(entry.Open().ToArray(), ImreadModes.AnyColor, Thumbnails[0]);
                }
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

            using StringReader sr = new(GCode.ToString());
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                if (line == string.Empty || line[0] == ';') continue; // No empty lines nor comment start lines
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
