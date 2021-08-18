using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using MoreLinq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using static UVtools.Core.FileFormats.ChituboxFile;

namespace UVtools.Core.FileFormats
{
    public class CTBEncryptedFile : FileFormat
    {

        #region Constants 
        public const uint MAGIC_CBT_ENCRYPTED = 0x12FD0107;
        public const ushort REPEATRGB15MASK = 0x20;

        public const byte RLE8EncodingLimit = 0x7d; // 125;
        public const ushort RLE16EncodingLimit = 0xFFF;

        private const string CTB_DISCLAIMER = "Layout and record format for the ctb and cbddlp file types are the copyrighted programs or codes of CBD Technology (China) Inc..The Customer or User shall not in any manner reproduce, distribute, modify, decompile, disassemble, decrypt, extract, reverse engineer, lease, assign, or sublicense the said programs or codes.";
        private const ushort CTB_DISCLAIMER_SIZE = 320;

        public static readonly byte[] Thing1 = new byte[0x20];

        public static readonly byte[] Thing2 = new byte[0x10];
        #endregion

        #region Sub Classes

        public class FileHeader
        {
            [FieldOrder(0)] public uint Magic;
            [FieldOrder(1)] public uint SettingsSize;
            [FieldOrder(2)] public uint SettingsOffset;
            [FieldOrder(3)] public uint Unknown1; // set to 0
            [FieldOrder(4)] public uint Unknown2; // set to 4
            [FieldOrder(5)] public uint SignatureSize;
            [FieldOrder(6)] public uint SignatureOffset;
            [FieldOrder(7)] public uint Unknown3; //set to 0
            [FieldOrder(8)] public ushort Unknown4 = 1; // set to 1
            [FieldOrder(9)] public ushort Unknown5 = 1; // seet to 1
            [FieldOrder(10)] public uint Unknown6; // set to 0
            [FieldOrder(11)] public uint Unknown7 = 0x2A; // probably 0x2A
            [FieldOrder(12)] public uint Unknown8; // probably 0
        }

        public class SlicerSettings
        {
            private string _machineName;

            [FieldOrder(0)] public ulong checksumValue;
            [FieldOrder(1)] public uint LayerTableOffset;
            [FieldOrder(2)] public float SizeX;
            [FieldOrder(3)] public float SizeY;
            [FieldOrder(4)] public float SizeZ;
            [FieldOrder(5)] public uint unknown1;
            [FieldOrder(6)] public uint unknown2;
            [FieldOrder(7)] public float TotalHeightMilimeter { get; set; }
            [FieldOrder(8)] public float LayerHeight { get; set; }
            [FieldOrder(9)] public float ExposureTime { get; set; }
            [FieldOrder(10)] public float BottomExposureTime { get; set; }
            [FieldOrder(11)] public float LightOffDelay { get; set; }
            [FieldOrder(12)] public uint BottomLayerCount { get; set; }
            [FieldOrder(13)] public uint ResolutionX { get; set; }
            [FieldOrder(14)] public uint ResolutionY { get; set; }
            [FieldOrder(15)] public uint LayerCount;
            [FieldOrder(16)] public uint LargePreviewOffset;
            [FieldOrder(17)] public uint SmallPreviewOffset;
            [FieldOrder(18)] public uint PrintTime { get; set; }
            [FieldOrder(19)] public uint unknown5 = 1;
            [FieldOrder(20)] public float BottomLiftHeight { get; set; }
            [FieldOrder(21)] public float BottomLiftSpeed { get; set; }
            [FieldOrder(22)] public float LiftHeight { get; set; }
            [FieldOrder(23)] public float LiftSpeed { get; set; }
            [FieldOrder(24)] public float RetractSpeed { get; set; }
            [FieldOrder(25)] public float MaterialMilliliters { get; set; }
            [FieldOrder(26)] public float MaterialGrams { get; set; }
            [FieldOrder(27)] public float MaterialCost { get; set; }
            [FieldOrder(28)] public float BottomLightOffDelay { get; set; }
            [FieldOrder(29)] public uint unknown9 = 1;
            [FieldOrder(30)] public ushort LightPWM { get; set; }
            [FieldOrder(31)] public ushort BottomLightPWM { get; set; }
            [FieldOrder(32)] public uint LayerXorKey;
            [FieldOrder(33)] public float BottomLiftHeight2 { get; set; }
            [FieldOrder(34)] public float BottomLiftSpeed2 { get; set; }
            [FieldOrder(35)] public float LiftHeight2 { get; set; }
            [FieldOrder(36)] public float LiftSpeed2 { get; set; }
            [FieldOrder(37)] public float RetractHeight2 { get; set; }
            [FieldOrder(38)] public float RetractSpeed2 { get; set; }
            [FieldOrder(39)] public float RestTimeAfterLift { get; set; }
            [FieldOrder(40)] public uint MachineNameOffset;
            [FieldOrder(41)] public uint MachineNameSize;
            [FieldOrder(42)] public uint unknown12 = 0xF;
            [FieldOrder(43)] public uint unknown13;
            [FieldOrder(44)] public uint unknown14 = 0x8;
            [FieldOrder(45)] public float RestTimeAfterRetract { get; set; }
            [FieldOrder(46)] public float RestTimeAfterLift2 { get; set; }
            [FieldOrder(47)] public uint unknown15;
            [FieldOrder(48)] public float BottomRetractSpeed { get; set; }
            [FieldOrder(49)] public float BottomRetractSpeed2 { get; set; }
            [FieldOrder(50)] public uint unknown16 = 4;
            [FieldOrder(51)] public float unknown17;
            [FieldOrder(52)] public uint unknown18 = 4;
            [FieldOrder(53)] public float unknown19;
            [FieldOrder(54)] public float RestTimeAfterRetract2 { get; set; }
            [FieldOrder(55)] public float RestTimeAfterLift3 { get; set; }
            [FieldOrder(56)] public float RestTimeBeforeLift { get; set; }
            [FieldOrder(57)] public float BottomRetractHeight2 { get; set; }
            [FieldOrder(58)] public float unknown23;
            [FieldOrder(59)] public uint unknown24;
            [FieldOrder(60)] public uint unknown25 = 4;
            [FieldOrder(61)] public uint LastLayerIndex;
            [FieldOrder(62), FieldCount(4)] public uint[] Padding;
            [FieldOrder(63)] public uint DisclaimerOffset;
            [FieldOrder(64)] public uint DisclaimerSize;
            [FieldOrder(65), FieldCount(4)] public uint[] Padding2;

            [Ignore]
            public string MachineName
            {
                get => _machineName;
                set
                {
                    _machineName = value;
                    MachineNameSize = string.IsNullOrEmpty(_machineName) ? 0 : (uint)_machineName.Length;
                }
            }

        }

        public class LayerTable
        {
            [Ignore]
            public uint LayerCount;

            [FieldOrder(0), FieldCount(nameof(LayerCount))] public LayerPointer[] Pointers;
        }

        public class LayerPointer
        {
            [FieldOrder(0)] public uint LayerOffset;
            [FieldOrder(1)] public uint Unknown1; // 0
            [FieldOrder(2)] public uint Unknown2; // always 0x58
            [FieldOrder(3)] public uint Unknown3; // 0
        }

        public class LayerData
        {
            [FieldOrder(0)] public uint LayerHeaderSize;
            [FieldOrder(1)] public float PositionZ;
            [FieldOrder(2)] public float ExposureTime;
            [FieldOrder(3)] public float LightOffDelay;
            [FieldOrder(4)] public uint LayerDataOffset;
            [FieldOrder(5)] public uint unknown2;
            [FieldOrder(6)] public uint LayerDataLength;
            [FieldOrder(7)] public uint unknown3;
            [FieldOrder(8)] public uint EncryptedDataOffset;
            [FieldOrder(9)] public uint EncryptedDataLength;
            [FieldOrder(10)] public float LiftHeight;
            [FieldOrder(11)] public float LiftSpeed;
            [FieldOrder(12)] public float LiftHeight2;
            [FieldOrder(13)] public float LiftSpeed2;
            [FieldOrder(14)] public float RetractSpeed;
            [FieldOrder(15)] public float RetractDistance;
            [FieldOrder(16)] public float RetractSpeed2;
            [FieldOrder(17)] public float RestTimeBeforeLift;
            [FieldOrder(18)] public float RestTimeAfterLift;
            [FieldOrder(19)] public float RestTimeAfterRetract;
            [FieldOrder(20)] public float LightPWM;
            [FieldOrder(21)] public uint unknown6;

            [Ignore] public int LayerIndex;
            [Ignore] public CTBEncryptedFile Parent { get; set; }

            [FieldLength(nameof(LayerDataLength)), FieldOrder(22)] public byte[] RLEData;

            public LayerData()
            {
            }

            public LayerData(Layer layer)
            {
                LayerHeaderSize = 0x58;
                PositionZ = layer.PositionZ;
                ExposureTime = layer.ExposureTime;
                LightOffDelay = layer.LightOffDelay;
                LiftHeight = layer.LiftHeight;
                LiftSpeed = layer.LiftSpeed;
                LiftHeight2 = layer.LiftHeight2;
                LiftSpeed2 = layer.LiftSpeed2;
                RetractSpeed = layer.RetractSpeed;
                RetractDistance = layer.RetractHeight;
                RetractSpeed2 = layer.RetractSpeed2;
                RestTimeBeforeLift = layer.WaitTimeAfterCure;
                RestTimeAfterLift = layer.WaitTimeAfterLift;
                RestTimeAfterRetract = layer.WaitTimeBeforeCure;
                LightPWM = layer.LightPWM;
            }
            public Mat DecodeLayerImage()
            {
                var mat = EmguExtensions.InitMat(Parent.Resolution);
                //var span = mat.GetBytePointer();

                if (Parent.Settings.LayerXorKey > 0)
                {
                    KeyRing kr = new(Parent.Settings.LayerXorKey, (uint)LayerIndex);
                    RLEData = kr.Read(RLEData);
                }

                int pixel = 0;
                for (var n = 0; n < RLEData.Length; n++)
                {
                    byte code = RLEData[n];
                    int stride = 1;

                    if ((code & 0x80) == 0x80) // It's a run
                    {
                        code &= 0x7f; // Get the run length
                        n++;

                        var slen = RLEData[n];

                        if ((slen & 0x80) == 0)
                        {
                            stride = slen;
                        }
                        else if ((slen & 0xc0) == 0x80)
                        {
                            stride = ((slen & 0x3f) << 8) + RLEData[n + 1];
                            n++;
                        }
                        else if ((slen & 0xe0) == 0xc0)
                        {
                            stride = ((slen & 0x1f) << 16) + (RLEData[n + 1] << 8) + RLEData[n + 2];
                            n += 2;
                        }
                        else if ((slen & 0xf0) == 0xe0)
                        {
                            stride = ((slen & 0xf) << 24) + (RLEData[n + 1] << 16) + (RLEData[n + 2] << 8) + RLEData[n + 3];
                            n += 3;
                        }
                        else
                        {
                            mat.Dispose();
                            throw new FileLoadException("Corrupted RLE data");
                        }
                    }

                    // Bit extend from 7-bit to 8-bit greymap
                    if (code != 0)
                    {
                        code = (byte)((code << 1) | 1);
                    }

                    mat.FillSpan(ref pixel, stride, code);

                    //if (stride <= 0) continue; // Nothing to do

                    /*if (code == 0) // Ignore blacks, spare cycles
                    {
                        pixel += stride;
                        continue;
                    }*/

                    /*for (; stride > 0; stride--)
                    {
                        span[pixel] = code;
                        pixel++;
                    }*/
                }

                return mat;
            }

            public unsafe byte[] EncodeLayerImage(Mat image, uint layerIndex)
            {
                List<byte> rawData = new();
                byte color = byte.MaxValue >> 1;
                uint stride = 0;
                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

                void AddRep()
                {
                    if (stride == 0)
                    {
                        return;
                    }

                    if (stride > 1)
                    {
                        color |= 0x80;
                    }
                    rawData.Add(color);

                    if (stride <= 1)
                    {
                        // no run needed
                        return;
                    }

                    if (stride <= 0x7f)
                    {
                        rawData.Add((byte)stride);
                        return;
                    }

                    if (stride <= 0x3fff)
                    {
                        rawData.Add((byte)((stride >> 8) | 0x80));
                        rawData.Add((byte)stride);
                        return;
                    }

                    if (stride <= 0x1fffff)
                    {
                        rawData.Add((byte)((stride >> 16) | 0xc0));
                        rawData.Add((byte)(stride >> 8));
                        rawData.Add((byte)stride);
                        return;
                    }

                    if (stride <= 0xfffffff)
                    {
                        rawData.Add((byte)((stride >> 24) | 0xe0));
                        rawData.Add((byte)(stride >> 16));
                        rawData.Add((byte)(stride >> 8));
                        rawData.Add((byte)stride);
                    }
                }


                for (int pixel = 0; pixel < imageLength; pixel++)
                {
                    var grey7 = (byte)(span[pixel] >> 1);

                    if (grey7 == color)
                    {
                        stride++;
                    }
                    else
                    {
                        AddRep();
                        color = grey7;
                        stride = 1;
                    }
                }

                AddRep();

                KeyRing kr = new(Parent.Settings.LayerXorKey, layerIndex);
                RLEData = kr.Read(rawData.ToArray());

                LayerDataLength = (uint)RLEData.Length;

                return RLEData;
            }

        }

        #region Preview
        /// <summary>
        /// The files contain two preview images.
        /// These are shown on the printer display when choosing which file to print, sparing the poor printer from needing to render a 3D image from scratch.
        /// </summary>
        public class Preview
        {
            /// <summary>
            /// Gets the X dimension of the preview image, in pixels. 
            /// </summary>
            [FieldOrder(0)] public uint ResolutionX { get; set; }

            /// <summary>
            /// Gets the Y dimension of the preview image, in pixels. 
            /// </summary>
            [FieldOrder(1)] public uint ResolutionY { get; set; }

            /// <summary>
            /// Gets the image offset of the encoded data blob.
            /// </summary>
            [FieldOrder(2)] public uint ImageOffset { get; set; }

            /// <summary>
            /// Gets the image length in bytes.
            /// </summary>
            [FieldOrder(3)] public uint ImageLength { get; set; }


            public unsafe Mat Decode(byte[] rawImageData)
            {
                var image = new Mat(new Size((int)ResolutionX, (int)ResolutionY), DepthType.Cv8U, 3);
                var span = image.GetBytePointer();

                int pixel = 0;
                for (int n = 0; n < rawImageData.Length; n++)
                {
                    uint dot = (uint)(rawImageData[n] & 0xFF | ((rawImageData[++n] & 0xFF) << 8));
                    byte red = (byte)(((dot >> 11) & 0x1F) << 3);
                    byte green = (byte)(((dot >> 6) & 0x1F) << 3);
                    byte blue = (byte)((dot & 0x1F) << 3);
                    int repeat = 1;
                    if ((dot & 0x0020) == 0x0020)
                    {
                        repeat += rawImageData[++n] & 0xFF | ((rawImageData[++n] & 0x0F) << 8);
                    }

                    for (int j = 0; j < repeat; j++)
                    {
                        span[pixel++] = blue;
                        span[pixel++] = green;
                        span[pixel++] = red;
                    }
                }

                return image;
            }

            public override string ToString()
            {
                return $"{nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(ImageOffset)}: {ImageOffset}, {nameof(ImageLength)}: {ImageLength}";
            }

            public unsafe byte[] Encode(Mat image)
            {
                List<byte> rawData = new();
                ushort color15 = 0;
                uint rep = 0;

                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

                void RleRGB15()
                {
                    switch (rep)
                    {
                        case 0:
                            return;
                        case 1:
                            rawData.Add((byte)(color15 & ~REPEATRGB15MASK));
                            rawData.Add((byte)((color15 & ~REPEATRGB15MASK) >> 8));
                            break;
                        case 2:
                            for (int i = 0; i < 2; i++)
                            {
                                rawData.Add((byte)(color15 & ~REPEATRGB15MASK));
                                rawData.Add((byte)((color15 & ~REPEATRGB15MASK) >> 8));
                            }

                            break;
                        default:
                            rawData.Add((byte)(color15 | REPEATRGB15MASK));
                            rawData.Add((byte)((color15 | REPEATRGB15MASK) >> 8));
                            rawData.Add((byte)((rep - 1) | 0x3000));
                            rawData.Add((byte)(((rep - 1) | 0x3000) >> 8));
                            break;
                    }
                }

                int pixel = 0;
                while (pixel < imageLength)
                {
                    var ncolor15 =
                        // bgr
                        (span[pixel++] >> 3) | ((span[pixel++] >> 2) << 5) | ((span[pixel++] >> 3) << 11);

                    if (ncolor15 == color15)
                    {
                        rep++;
                        if (rep == RLE16EncodingLimit)
                        {
                            RleRGB15();
                            rep = 0;
                        }
                    }
                    else
                    {
                        RleRGB15();
                        color15 = (ushort)ncolor15;
                        rep = 1;
                    }
                }

                RleRGB15();

                ImageLength = (uint)rawData.Count;

                return rawData.ToArray();
            }
        }
        #endregion

        #endregion

        #region Properties
        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new(typeof(CTBEncryptedFile), "ctb", $"Chitubox Encrypted", false, false),
            new(typeof(CTBEncryptedFile),"encrypted.ctb", "Chitubox Encrypted", false, false),
        };

        public override string MachineName
        {
            get => Settings.MachineName;
            set
            {
                Settings.MachineName = value;
                Settings.MachineNameSize = (uint)value.Length;
            }
        }

        public override uint ResolutionX
        {
            get => Settings.ResolutionX;
            set
            {
                Settings.ResolutionX = value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => Settings.ResolutionY;
            set
            {
                Settings.ResolutionY = value;
                RaisePropertyChanged();
            }
        }

        public override float LayerHeight
        {
            get => Settings.LayerHeight;
            set
            {
                Settings.LayerHeight = value;
                RaisePropertyChanged();
            }
        }

        public byte[] Hash = new byte[0x20];

        public override byte AntiAliasing { get => 8; set { } }

        public override Size[] ThumbnailsOriginalSize { get; } =
        {
            new(400, 300),
            new(200, 125)
        };

        public Preview[] Previews { get; protected internal set; }

        public FileHeader Header { get; protected internal set; } = new();

        public SlicerSettings Settings { get; protected internal set; } = new();

        /**********************************/
        public override PrintParameterModifier[] PrintParameterModifiers
        {
            get
            {
                return new[]
                {
                    PrintParameterModifier.BottomLayerCount,

                    PrintParameterModifier.BottomLightOffDelay,
                    PrintParameterModifier.LightOffDelay,

                    //PrintParameterModifier.BottomWaitTimeBeforeCure,
                    PrintParameterModifier.WaitTimeBeforeCure,

                    PrintParameterModifier.BottomExposureTime,
                    PrintParameterModifier.ExposureTime,

                    //PrintParameterModifier.BottomWaitTimeAfterCure,
                    PrintParameterModifier.WaitTimeAfterCure,

                    PrintParameterModifier.BottomLiftHeight,
                    PrintParameterModifier.BottomLiftSpeed,
                    PrintParameterModifier.LiftHeight,
                    PrintParameterModifier.LiftSpeed,
                    PrintParameterModifier.BottomLiftHeight2,
                    PrintParameterModifier.BottomLiftSpeed2,
                    PrintParameterModifier.LiftHeight2,
                    PrintParameterModifier.LiftSpeed2,

                    //PrintParameterModifier.BottomWaitTimeAfterLift,
                    PrintParameterModifier.WaitTimeAfterLift,

                    PrintParameterModifier.BottomRetractSpeed,
                    PrintParameterModifier.RetractSpeed,
                    PrintParameterModifier.BottomRetractHeight2,
                    PrintParameterModifier.BottomRetractSpeed2,
                    PrintParameterModifier.RetractHeight2,
                    PrintParameterModifier.RetractSpeed2,

                    PrintParameterModifier.BottomLightPWM,
                    PrintParameterModifier.LightPWM
                };
            }
        }



        public override PrintParameterModifier[] PrintParameterPerLayerModifiers
        {
            get
            {
                return new[]
                {
                    PrintParameterModifier.LightOffDelay,
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
                    PrintParameterModifier.LightPWM
                };
            }
        }


        public override float DisplayWidth
        {
            get => Settings.SizeX;
            set
            {
                Settings.SizeX = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float DisplayHeight
        {
            get => Settings.SizeY;
            set
            {
                Settings.SizeY = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float MachineZ
        {
            get => Settings.SizeZ > 0 ? Settings.SizeZ : base.MachineZ;
            set => base.MachineZ = Settings.SizeZ = (float)Math.Round(value, 2);
        }

        /* TODO: Find ProjectorType in file */
        /* public override bool DisplayMirror
        {
            get => HeaderSettings.ProjectorType > 0;
            set
            {
                HeaderSettings.ProjectorType = value ? 1u : 0;
                RaisePropertyChanged();
            }
        }*/

        public override bool IsAntiAliasingEmulated => false;

        /* TODO: Find AntiAliasLevel in file */
        /*
        public override byte AntiAliasing
        {
            get => (byte)(Settings.AntiAliasLevel);
            set
            {
                Settings.AntiAliasLevel = value;
                RaisePropertyChanged();
            }
        }*/

        public override float PrintHeight
        {
            get => base.PrintHeight;
            set => base.PrintHeight = Settings.TotalHeightMilimeter = base.PrintHeight;
        }

        public override uint LayerCount
        {
            get => base.LayerCount;
            set => base.LayerCount = Settings.LayerCount = base.LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => (ushort)Settings.BottomLayerCount;
            set => base.BottomLayerCount = (ushort)(Settings.BottomLayerCount = value);
        }

        public override float BottomLightOffDelay
        {
            get => Settings.BottomLightOffDelay;
            set
            {
                base.BottomLightOffDelay = Settings.BottomLightOffDelay = (float)Math.Round(value, 2);
                if (value > 0)
                {
                    WaitTimeBeforeCure = 0;
                    WaitTimeAfterCure = 0;
                    WaitTimeAfterLift = 0;
                }
            }
        }

        public override float LightOffDelay
        {
            get => Settings.LightOffDelay;
            set
            {
                base.LightOffDelay = Settings.LightOffDelay = (float)Math.Round(value, 2);
                WaitTimeBeforeCure = 0;
                WaitTimeAfterCure = 0;
                WaitTimeAfterLift = 0;
            }
        }

        public override float BottomWaitTimeBeforeCure
        {
            get => WaitTimeBeforeCure;
            set
            {
                if (value > 0)
                {
                    SetBottomLightOffDelay(value);
                }
            }
        }

        public override float WaitTimeBeforeCure
        {
            get => Settings.RestTimeAfterRetract;
            set
            {
                base.WaitTimeBeforeCure = Settings.RestTimeAfterRetract = (float)Math.Round(value, 2);
                if (value > 0)
                {
                    BottomLightOffDelay = 0;
                    LightOffDelay = 0;
                }
            }
        }

        public override float BottomExposureTime
        {
            get => Settings.BottomExposureTime;
            set => base.BottomExposureTime = Settings.BottomExposureTime = (float)Math.Round(value, 2);
        }

        public override float BottomWaitTimeAfterCure => WaitTimeAfterCure;
        public override float WaitTimeAfterCure
        {
            get => Settings.RestTimeBeforeLift;
            set
            {
                base.WaitTimeAfterCure = Settings.RestTimeBeforeLift = (float)Math.Round(value, 2);
                if (value > 0)
                {
                    BottomLightOffDelay = 0;
                    LightOffDelay = 0;
                }
            }
        }

        public override float ExposureTime
        {
            get => Settings.ExposureTime;
            set => base.ExposureTime = Settings.ExposureTime = (float)Math.Round(value, 2);
        }

        public override float BottomLiftHeight
        {
            get => Math.Max(0,Settings.BottomLiftHeight - Settings.BottomLiftHeight2);
            set
            {
                value = (float)Math.Round(value, 2);
                Settings.BottomLiftHeight = (float)Math.Round(value + Settings.BottomLiftHeight2, 2);
                base.BottomLiftHeight = value;
            }
        }

        public override float LiftHeight
        {
            get => Math.Max(0,Settings.LiftHeight - Settings.LiftHeight2);
            set
            {
                value = (float)Math.Round(value, 2);
                Settings.LiftHeight = (float)Math.Round(value + Settings.LiftHeight2, 2);
                base.LiftHeight = value;
            }
        }

        public override float BottomLiftSpeed
        {
            get => Settings.BottomLiftSpeed;
            set => base.BottomLiftSpeed = Settings.BottomLiftSpeed = (float)Math.Round(value, 2);
        }

        public override float LiftSpeed
        {
            get => Settings.LiftSpeed;
            set => base.LiftSpeed = Settings.LiftSpeed = (float)Math.Round(value, 2);
        }

        public override float BottomLiftHeight2
        {
            get => Settings.BottomLiftHeight2;
            set
            {
                var bottomLiftHeight = BottomLiftHeight;
                Settings.BottomLiftHeight2 = (float)Math.Round(value, 2);
                BottomLiftHeight = bottomLiftHeight;
                base.BottomLiftHeight2 = Settings.BottomLiftHeight2; 
            }
        }

        public override float LiftHeight2
        {
            get => Settings.LiftHeight2;
            set
            {
                var liftHeight = LiftHeight;
                Settings.LiftHeight2 = (float)Math.Round(value, 2);
                LiftHeight = liftHeight;
                base.LiftHeight2 = Settings.LiftHeight2;
            }
        }

        public override float BottomLiftSpeed2
        {
            get => Settings.BottomLiftSpeed2;
            set
            {
                base.BottomLiftSpeed2 = Settings.BottomLiftSpeed2 = (float)Math.Round(value, 2);
            }
        }

        public override float LiftSpeed2
        {
            get => Settings.LiftSpeed2;
            set
            {
                base.LiftSpeed2 = Settings.LiftSpeed2 = (float)Math.Round(value, 2);
            }
        }

        public override float BottomWaitTimeAfterLift => WaitTimeAfterLift;
        public override float WaitTimeAfterLift
        {
            get => Settings.RestTimeAfterLift;
            set
            {
                base.WaitTimeAfterLift = Settings.RestTimeAfterLift = Settings.RestTimeAfterLift2 = (float)Math.Round(value, 2);
                if (value > 0)
                {
                    BottomLightOffDelay = 0;
                    LightOffDelay = 0;
                }
            }
        }

        public override float BottomRetractSpeed
        {
            get => Settings.BottomRetractSpeed;
            set
            {
                base.BottomRetractSpeed = Settings.BottomRetractSpeed = (float)Math.Round(value, 2);
            }
        }

        public override float RetractSpeed
        {
            get => Settings.RetractSpeed;
            set => base.RetractSpeed = Settings.RetractSpeed = (float)Math.Round(value, 2);
        }

        public override float BottomRetractHeight2
        {
            get => Settings.BottomRetractHeight2;
            set
            {
                value = Math.Clamp((float)Math.Round(value, 2), 0, RetractHeightTotal);
                base.BottomRetractHeight2 = Settings.BottomRetractHeight2 = value;
            }
        }

        public override float RetractHeight2
        {
            get => Settings.RetractHeight2;
            set
            {
                value = Math.Clamp((float)Math.Round(value, 2), 0, RetractHeightTotal);
                base.RetractHeight2 = Settings.RetractHeight2 = value;
            }
        }

        public override float BottomRetractSpeed2
        {
            get => Settings.BottomRetractSpeed2;
            set
            {
                base.BottomRetractSpeed2 = Settings.BottomRetractSpeed2 = (float)Math.Round(value, 2);
            }
        }

        public override float RetractSpeed2
        {
            get => Settings.RetractSpeed2;
            set => base.RetractSpeed2 = Settings.RetractSpeed2 = (float)Math.Round(value, 2);
        }

        public override byte BottomLightPWM
        {
            get => (byte)Settings.BottomLightPWM;
            set => base.BottomLightPWM = (byte)(Settings.BottomLightPWM = value);
        }

        public override byte LightPWM
        {
            get => (byte)Settings.LightPWM;
            set => base.LightPWM = (byte)(Settings.LightPWM = value);
        }

        public override float PrintTime
        {
            get => base.PrintTime;
            set
            {
                base.PrintTime = value;
                Settings.PrintTime = (uint)base.PrintTime;
            }
        }

        public override float MaterialMilliliters
        {
            get => base.MaterialMilliliters;
            set
            {
                base.MaterialMilliliters = value;
                Settings.MaterialMilliliters = base.MaterialMilliliters;
            }
        }

        public override float MaterialGrams
        {
            get => Settings.MaterialGrams;
            set => base.MaterialGrams = Settings.MaterialGrams = (float)Math.Round(value, 3);
        }

        public override float MaterialCost
        {
            get => (float)Math.Round(Settings.MaterialCost, 3);
            set => base.MaterialCost = Settings.MaterialCost = (float)Math.Round(value, 3);
        }

        public override object[] Configs
        {
            get
            {
                return new object[] { Settings };
            }
        }



        #endregion

        #region Constructors
        public CTBEncryptedFile()
        {
            Previews = new Preview[ThumbnailsCount];
        }
        #endregion

        #region Methods

        public override void Clear()
        {
            base.Clear();

            for (byte i = 0; i < ThumbnailsCount; i++)
            {
                Previews[i] = new Preview();
            }
        }

        public override bool CanProcess(string fileFullPath)
        {
            if (!base.CanProcess(fileFullPath)) return false;

            try
            {
                using var fs = new BinaryReader(new FileStream(fileFullPath, FileMode.Open, FileAccess.Read));
                var magic = fs.ReadUInt32();
                return magic is MAGIC_CBT_ENCRYPTED;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return false;
        }


        public override void SaveAs(string filePath = null, OperationProgress progress = null)
        {
            EncodeInternally(filePath, progress);
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);
            Header = Helpers.Deserialize<FileHeader>(inputFile);

            byte[] encryptedBlock = new byte[Header.SettingsSize];
            inputFile.Position = Header.SettingsOffset;
            inputFile.ReadBytes(encryptedBlock, 0);

            byte[] decryptedBlock = Helpers.AesCryptBytes(encryptedBlock, Thing1, CipherMode.CBC, PaddingMode.None, false, Thing2);
            using (MemoryStream ms = new(decryptedBlock))
            {
                Settings = Helpers.Deserialize<SlicerSettings>(ms);
                //LayerCount = Settings.LayerCount;
                BottomLayerCount = (ushort)Settings.BottomLayerCount;
            }
            encryptedBlock = null;
            decryptedBlock = null;

            progress.Reset(OperationProgress.StatusDecodePreviews, ThumbnailsCount);

            for (byte i = 0; i < ThumbnailsCount; i++)
            {
                uint offsetAddress = i == 0
                    ? Settings.LargePreviewOffset
                    : Settings.SmallPreviewOffset;
                if (offsetAddress == 0) continue;

                inputFile.Seek(offsetAddress, SeekOrigin.Begin);
                Previews[i] = Helpers.Deserialize<Preview>(inputFile);

                Debug.Write($"Preview {i} -> ");
                Debug.WriteLine(Previews[i]);

                inputFile.Seek(Previews[i].ImageOffset, SeekOrigin.Begin);
                byte[] rawImageData = new byte[Previews[i].ImageLength];
                inputFile.Read(rawImageData, 0, (int)Previews[i].ImageLength);

                Thumbnails[i] = Previews[i].Decode(rawImageData);
                progress++;
            }

            /* Read the settings and disclaimer */
            inputFile.Position = Settings.MachineNameOffset;
            byte[] machineNameBytes = new byte[Settings.MachineNameSize];
            inputFile.ReadBytes(machineNameBytes);
            Settings.MachineName = UTF8Encoding.UTF8.GetString(machineNameBytes);

            /* TODO: read the disclaimer here? we can really just ignore it though...*/

            /* start gathering up the layers */
            progress.Reset(OperationProgress.StatusGatherLayers, Settings.LayerCount);


            inputFile.Position = Settings.LayerTableOffset;

            LayerPointer[] pointers = new LayerPointer[Settings.LayerCount];
            for (var x = 0; x < Settings.LayerCount; x++)
            {
                pointers[x] = Helpers.Deserialize<LayerPointer>(inputFile);
                progress++;
                progress.Token.ThrowIfCancellationRequested();
            }

            progress.Reset(OperationProgress.StatusDecodeLayers, Settings.LayerCount);
            var range = Enumerable.Range(0, (int)Settings.LayerCount);
            LayerManager.Init(Settings.LayerCount);
            ConcurrentBag<int> buggyLayers = new ConcurrentBag<int>();
            foreach (var batch in MoreEnumerable.Batch(range, Environment.ProcessorCount * 10))
            {
                List<LayerData> parsedLayerData = new();

                foreach (var layerIndex in batch)
                {
                    progress.Token.ThrowIfCancellationRequested();

                    inputFile.Seek(pointers[layerIndex].LayerOffset, SeekOrigin.Begin);
                    LayerData layerInfo = Helpers.Deserialize<LayerData>(inputFile);
                    layerInfo.LayerIndex = layerIndex;
                    layerInfo.Parent = this;
                    parsedLayerData.Add(layerInfo);

                }


                Parallel.ForEach(parsedLayerData, layerData =>
                {
                    if (progress.Token.IsCancellationRequested) return;

                    if (layerData.EncryptedDataLength > 0)
                    {
                        /* decrypte RLE data here */

                        using (MemoryStream ms = new MemoryStream(layerData.RLEData))
                        {
                            ms.Position = layerData.EncryptedDataOffset;
                            byte[] byteBuffer = new byte[layerData.EncryptedDataLength];
                            ms.Read(byteBuffer, 0, (int)layerData.EncryptedDataLength);

                            byteBuffer = Helpers.AesCryptBytes(byteBuffer, Thing1, CipherMode.CBC, PaddingMode.None, false, Thing2);

                            Array.Copy(byteBuffer, 0, layerData.RLEData, layerData.EncryptedDataOffset, layerData.EncryptedDataLength);
                        }

                    }

                    Layer layer = null;

                    /* bug fix for Chitubox when a small layer RLE data is encrypted */
                    if (layerData.EncryptedDataLength > 0 && layerData.RLEData.Length < 0x200 && layerData.RLEData.Length % 0x10 != 0)
                    {
                        buggyLayers.Add(layerData.LayerIndex);

                        /* create a layer with no decoded mat, these will be fixed up *after* the parallel section */
                        layer = new Layer((uint)layerData.LayerIndex, LayerManager)
                        {
                            PositionZ = layerData.PositionZ,
                            ExposureTime = layerData.ExposureTime,
                            LightOffDelay = 0
                        };

                    }
                    else
                    {

                        layer = new Layer((uint)layerData.LayerIndex, layerData.DecodeLayerImage(), LayerManager)
                        {
                            PositionZ = layerData.PositionZ,
                            ExposureTime = layerData.ExposureTime,
                            LightOffDelay = 0
                        };
                    }

                    layer.LiftHeight = layerData.LiftHeight;
                    layer.LiftSpeed = layerData.LiftSpeed;
                    layer.RetractSpeed = layerData.RetractSpeed;
                    layer.LightPWM = (byte)layerData.LightPWM;
                    layer.LiftHeight2 = layerData.LiftHeight2;
                    layer.LiftSpeed2 = layerData.LiftSpeed2;
                    layer.RetractHeight2 = layerData.RetractDistance;
                    layer.RetractSpeed2 = layerData.RetractSpeed2;
                    layer.WaitTimeBeforeCure = layerData.RestTimeAfterRetract;
                    layer.WaitTimeAfterCure = layerData.RestTimeBeforeLift;
                    layer.WaitTimeAfterLift = layerData.RestTimeAfterLift;
                    this[layerData.LayerIndex] = layer;
                    layerData.RLEData = null; /* clean up RLE data */
                    progress.LockAndIncrement();
                });
                parsedLayerData.Clear();

            }

            if (buggyLayers.Count == LayerCount)
            {
                throw new FileLoadException("Unable to load this file due to Chitubox bug affecting every layer. Please increase the portion of the plate in use and reslice from Chitubox");
            }

            var sortedLayerIndexes = buggyLayers.ToList();
            sortedLayerIndexes.Sort();
            int correctedLayerCount = 0;
            foreach(var layerIndex in sortedLayerIndexes)
            {
                int direction = layerIndex == 0 ? 1 : -1;

                /* clone from the next one that has a mat */
                var substituteLayerFound = false;
                var layerIndexForClone = layerIndex + direction;
                while (layerIndexForClone >= 0 && layerIndexForClone < LayerCount && !substituteLayerFound)
                {
                    if (this[layerIndexForClone].CompressedBytes != null)
                    {
                        substituteLayerFound = true;
                        this[layerIndex].LayerMat = this[layerIndexForClone].LayerMat.Clone();
                        this[layerIndex].IsModified = true;
                        correctedLayerCount++;

                        /* TODO: Report to the user that a layer was cloned to work around chitubox crypto bug */
                    } else
                    {
                        layerIndexForClone += direction;
                    }
                }
            }

            if (correctedLayerCount < buggyLayers.Count)
            {
                throw new FileLoadException("Unable to load this file due to Chitubox bug. UVTools was unable to correct some of these layers. Please increase the portion of the plate in use and reslice from Chitubox.");
            }


            inputFile.ReadBytes(Hash);
        }

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
            uint currentOffset = 0;
            Settings.LastLayerIndex = Settings.LayerCount - 1;

            /* Create the file header and fill out what we can. SignatureOffset will have to be populated later
             * this will be the last thing written to file */
            Header.Magic = MAGIC_CBT_ENCRYPTED;
            Header.SettingsSize = (uint)Helpers.Serializer.SizeOf(Settings);
            Header.SettingsOffset = (uint)Helpers.Serializer.SizeOf(Header);
            Header.Unknown2 = 0x4;
            Header.Unknown4 = 1;
            Header.Unknown5 = 1;
            Header.Unknown5 = 0x2A;


            if (Settings.LayerXorKey == 0)
            {
                Settings.LayerXorKey = 0xEFBEADDE;
            }

            currentOffset = Header.SettingsOffset + Header.SettingsSize;
            outputFile.Seek((int)currentOffset, SeekOrigin.Begin);

            progress.Reset(OperationProgress.StatusEncodePreviews, 2);

            Mat[] thumbnails = { GetThumbnail(true), GetThumbnail(false) };
            for (byte i = 0; i < thumbnails.Length; i++)
            {
                var image = thumbnails[i];
                if (image is null) continue;

                Preview preview = new()
                {
                    ResolutionX = (uint)image.Width,
                    ResolutionY = (uint)image.Height,
                };

                var previewBytes = preview.Encode(image);

                if (previewBytes.Length == 0) continue;

                if (i == 0)
                {
                    Settings.LargePreviewOffset = currentOffset;
                }
                else
                {
                    Settings.SmallPreviewOffset = currentOffset;
                }

                currentOffset += (uint)Helpers.Serializer.SizeOf(preview);
                preview.ImageOffset = currentOffset;

                Helpers.SerializeWriteFileStream(outputFile, preview);
                currentOffset += outputFile.WriteBytes(previewBytes);
                progress++;
            }

            Settings.MachineNameOffset = currentOffset;
            Settings.MachineNameSize = (uint)Settings.MachineName.Length;
            byte[] machineNameBytes = Encoding.UTF8.GetBytes(Settings.MachineName);

            currentOffset += outputFile.WriteBytes(machineNameBytes);

            Settings.DisclaimerOffset = currentOffset;
            Settings.DisclaimerSize = CTB_DISCLAIMER_SIZE;
            currentOffset += outputFile.WriteBytes(Encoding.UTF8.GetBytes(CTB_DISCLAIMER));

            Settings.LayerTableOffset = currentOffset;

            /* we'll write this after we write out all of the layers ... */
            LayerTable lt = new LayerTable();
            lt.LayerCount = LayerCount;
            lt.Pointers = new LayerPointer[LayerCount];
            uint layerTableSize = (uint)Helpers.Serializer.SizeOf(new LayerPointer()) * LayerCount;
            currentOffset += layerTableSize;

            outputFile.Seek((int)currentOffset, SeekOrigin.Begin);

            LayerData[] layerDataList = new LayerData[LayerCount];
            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
            Parallel.For(0, LayerCount, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                LayerData layerData = new(this[layerIndex]);
                layerData.Parent = this;
                using (var image = this[layerIndex].LayerMat)
                {
                    layerData.EncodeLayerImage(image, (uint)layerIndex);
                    layerDataList[layerIndex] = layerData;
                }

                progress.LockAndIncrement();
            });
            progress.Reset(OperationProgress.StatusWritingFile, LayerCount);
            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                var currentLayer = layerDataList[layerIndex];
                lt.Pointers[layerIndex] = new LayerPointer() { LayerOffset = currentOffset, Unknown2 = 0x58 };

                currentLayer.LayerDataOffset = currentOffset + 0x58;
                //currentLayer.LiftHeight = currentLayer.LiftHeight + currentLayer.LiftHeight2;
                currentOffset += Helpers.SerializeWriteFileStream(outputFile, currentLayer);
                progress++;
            }

            /* write the final hash */
            byte[] hash = Helpers.ComputeSHA256Hash(BitConverter.GetBytes((ulong)(Settings.checksumValue)));
            byte[] encryptedHash = Helpers.AesCryptBytes(hash, Thing1, CipherMode.CBC, PaddingMode.None, true, Thing2);
            Header.SignatureOffset = currentOffset;
            Header.SignatureSize = (uint)encryptedHash.Length;
            outputFile.WriteBytes(encryptedHash);
            outputFile.Seek(0, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, Header);
            outputFile.Seek(0, SeekOrigin.Begin);
            outputFile.Seek(Header.SettingsOffset, SeekOrigin.Begin);
            var settingsBytes = Helpers.Serialize(Settings).ToArray();
            var encryptedSettings = Helpers.AesCryptBytes(settingsBytes, Thing1, CipherMode.CBC, PaddingMode.None, true, Thing2);
            outputFile.WriteBytes(encryptedSettings);
            outputFile.Seek(Settings.LayerTableOffset, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, lt);
        }
        #endregion


    }
}
