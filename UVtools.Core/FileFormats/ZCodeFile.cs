/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.OpenSsl;
using UVtools.Core.Extensions;
using UVtools.Core.GCode;
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
            public float MachineZ { get; set; } = 220;

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
                public uint BottomLightOffDelay { get; set; }

                [XmlAttribute("cooldown")]
                public uint LightOffDelay { get; set; }

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
            public float LayerHeight { get; set; } = FileFormat.DefaultLayerHeight;

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


        public const string Secret1 = "eHtZQkIuNhIfOk8/PjoDFyAqTyc2DHtZQkJBeRgfPS05PToXFzAuIS4UPiccBAYrSiJmNi4+KTUUFycsLjhLIjETKlgtFBAXNQQqLUUXODJcADhKFCZfIBY2NSYmRF01PgcBJhYiOi1KYwYEDBs4KzAgRQw/LxA5GgEAGDQkGzdHCV0mMSUGFj8dFj4iGhUCXCYgBBhHJwUaKjMFIhdYCUUiHzAuPi0xFD02Bg0vPS8HWz8sCjIBPgwXLSA9MA45Ah8OPSYUMCtXHhAPHxAOHg0WIhBfOgU5HyQfQwomXColZCJdKhZBbRAeJCwpGhhlQCsXOUoFDCAVNj9AJxUnBy4FNhRvBR9WRyEiDwIMGT0mDTogXQ0dOBw5RxwXGUQZBzIiWzo6MTIlXjwhJT0lNyY+KARhP0NbIx01Z25BJhVbRCMhRSI3KUNjGjwkJC4xFzdHLgYeQgQYNgcBDyIcMS0JIFgnGT1MAwkDFiI5AgQAOgovBTZEKUM2AhgeGCxVOBghOTctOgoSBQcsJj03PCUMGgsjBwN9DVwsXz8THhkjXAc6YzoMFx0fCwEcLCIoATIjMD42CB06BB8cLiQuBy8PETUtWEUQAhsrPBwWDDoGIj4FBwYBAgUIIjQ4IV9XDi5cHQ8xJh1mXnh7WUIqIjd1BiYmOS0nEHY/KjZBXnh7WQ==";
        public const string Secret2 = "eHtZQkIuNhIfOk8/OTEZHzdPJCkqeHtZQkJmPhMhAys+NTkeOS4mBxoQGxclKi0uIhQSJxguGyAUHDYuIAspLTJCKkA9ODM8BwI9DjgxGBk6DTlFAiwyLj8JGWMOODpeXwFsDjAYASYgYic5KV4GJCFlTQY+DSdnLEJXFSEwZyYAFjoHNzEuQFhdJEM5NRFcGh8wFCExLi49TmhcWUJCQV4QGDBPPzkxGR83TyQpKnh7WUJC";
        public static readonly string GCodeRSAPrivateKey = CryptExtensions.XORCipherString(CryptExtensions.Base64DecodeString(Secret1), About.Software);
        public static readonly string GCodeRSAPublicKey = CryptExtensions.XORCipherString(CryptExtensions.Base64DecodeString(Secret2), About.Software);

        #endregion

        #region Sub Classes

        #endregion

        #region Properties
        public ZCodePrint ManifestFile { get; set; } = new ();

        public override FileFormatType FileType => FileFormatType.Archive;

        public override FileExtension[] FileExtensions { get; } = {
            new(typeof(ZCodeFile), "zcode", "UnizMaker ZCode")
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } = {
            PrintParameterModifier.BottomLayerCount,

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
            PrintParameterModifier.WaitTimeBeforeCure,
            PrintParameterModifier.ExposureTime,
            PrintParameterModifier.WaitTimeAfterCure,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.LiftHeight2,
            PrintParameterModifier.LiftSpeed2,
            PrintParameterModifier.WaitTimeAfterLift,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.RetractHeight2,
            PrintParameterModifier.RetractSpeed2,
            PrintParameterModifier.LightPWM,
        };

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

        public override float MachineZ
        {
            get => ManifestFile.Device.MachineZ > 0 ? ManifestFile.Device.MachineZ : base.MachineZ;
            set
            {
                ManifestFile.Device.MachineZ = value;
                RaisePropertyChanged();
            }
        }

        public override Enumerations.FlipDirection DisplayMirror => Enumerations.FlipDirection.Vertically;

        public override byte AntiAliasing
        {
            get => ManifestFile.Profile.Slice.AntiAliasing;
            set => base.AntiAliasing = ManifestFile.Profile.Slice.AntiAliasing = value.Clamp(1, 16);
        }

        public override float LayerHeight
        {
            get => ManifestFile.Job.LayerHeight > 0 ? ManifestFile.Job.LayerHeight : ManifestFile.Profile.Slice.LayerHeight;
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

        public override float BottomLightOffDelay
        {
            get => BottomWaitTimeBeforeCure;
            set => BottomWaitTimeBeforeCure = value;
        }

        public override float LightOffDelay
        {
            get => WaitTimeBeforeCure;
            set => WaitTimeBeforeCure = value;
        }

        public override float BottomWaitTimeBeforeCure
        {
            get => TimeExtensions.MillisecondsToSeconds(ManifestFile.Profile.Slice.BottomLightOffDelay);
            set
            {
                ManifestFile.Profile.Slice.BottomLightOffDelay = TimeExtensions.SecondsToMillisecondsUint(value);
                base.BottomWaitTimeBeforeCure = base.BottomLightOffDelay = value;
            }
        }

        public override float WaitTimeBeforeCure
        {
            get => TimeExtensions.MillisecondsToSeconds(ManifestFile.Profile.Slice.LightOffDelay);
            set
            {
                ManifestFile.Profile.Slice.LightOffDelay = TimeExtensions.SecondsToMillisecondsUint(value);
                base.WaitTimeBeforeCure = base.LightOffDelay = value;
            }
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

        #region Constructor
        public ZCodeFile()
        {
            GCode = new GCodeBuilder
            {
                UseTailComma = true,
                UseComments = false,
                GCodePositioningType = GCodeBuilder.GCodePositioningTypes.Absolute,
                GCodeSpeedUnit = GCodeBuilder.GCodeSpeedUnits.CentimetersPerMinute,
                GCodeTimeUnit = GCodeBuilder.GCodeTimeUnits.Milliseconds,
                GCodeShowImageType = GCodeBuilder.GCodeShowImageTypes.FilenamePng1Started,
                LayerMoveCommand = GCodeBuilder.GCodeMoveCommands.G0,
                EndGCodeMoveCommand = GCodeBuilder.GCodeMoveCommands.G1,
                MaxLEDPower = MaxLEDPower,
                CommandClearImage = {Enabled = false},
            };
        }
        #endregion

        #region Methods

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            using ZipArchive outputFile = ZipFile.Open(fileFullPath, ZipArchiveMode.Create);
            if (Thumbnails.Length > 0 && Thumbnails[0] is not null)
            {
                using var thumbnailsStream = outputFile.CreateEntry(PreviewFilename).Open();
                thumbnailsStream.WriteBytes(Thumbnails[0].GetPngByes());
                thumbnailsStream.Close();
            }

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                progress.Token.ThrowIfCancellationRequested();
                var layer = this[layerIndex];
                outputFile.PutFileContent($"{layerIndex + 1}.png", layer.CompressedBytes, ZipArchiveMode.Create);
                progress++;
            }

            XmlSerializer serializer = new(ManifestFile.GetType());
            XmlSerializerNamespaces ns = new();
            ns.Add("", "");
            var entry = outputFile.CreateEntry(ManifestFilename);
            using (var stream = entry.Open())
            {
                serializer.Serialize(stream, ManifestFile, ns);
            }

            outputFile.PutFileContent(GCodeFilename, EncryptGCode(progress), ZipArchiveMode.Create);
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var inputFile = ZipFile.Open(FileFullPath, ZipArchiveMode.Read);
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
                    decodedBytes = decodedBytes.Skip(2).SkipWhile(b => b is 255 or 0).ToArray();
                    GCode.AppendLine(Encoding.UTF8.GetString(decodedBytes));
                        
                    progress++;
                }

                tr.Close();
            }

            LayerManager.Init(ManifestFile.Job.LayerCount);
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

            var entriesToRemove = outputFile.Entries.Where(zipEntry => zipEntry.Name.EndsWith(".gcode") || zipEntry.Name.EndsWith(".xml")).ToArray();
            foreach (var zipEntry in entriesToRemove)
            {
                zipEntry.Delete();
            }

            XmlSerializer serializer = new(ManifestFile.GetType());
            XmlSerializerNamespaces ns = new();
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

            using StringReader sr = new(GCodeStr);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                line = line.Trim();
                if (line == string.Empty || line[0] == ';') continue; // No empty lines nor comment start lines
                progress += (uint)line.Length;

                var data = Encoding.UTF8.GetBytes(line);
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
                //Debug.WriteLine(string.Join(", ", padDataArray));

                var encodedBytes = encryptEngine.ProcessBlock(padDataArray, 0, padDataArray.Length);
                sb.AppendLine(System.Convert.ToBase64String(encodedBytes));
            }

            return sb.ToString();
        }
        #endregion
    }
}
