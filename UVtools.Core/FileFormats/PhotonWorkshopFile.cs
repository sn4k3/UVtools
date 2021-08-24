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
using System.Threading.Tasks;
using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class PhotonWorkshopFile : FileFormat
    {
        #region Constants
        public const byte VERSION_1 = 1;
        public const ushort VERSION_515 = 515;

        public const byte MarkSize = 12;
        public const byte RLE1EncodingLimit = 0x7d; // 125;
        public const ushort RLE4EncodingLimit = 0xfff; // 4095;

        // CRC-16-ANSI (aka CRC-16-IMB) Polynomial: x^16 + x^15 + x^2 + 1
        public static readonly int[] CRC16Table = {
            0x0000, 0xc0c1, 0xc181, 0x0140, 0xc301, 0x03c0, 0x0280, 0xc241,
            0xc601, 0x06c0, 0x0780, 0xc741, 0x0500, 0xc5c1, 0xc481, 0x0440,
            0xcc01, 0x0cc0, 0x0d80, 0xcd41, 0x0f00, 0xcfc1, 0xce81, 0x0e40,
            0x0a00, 0xcac1, 0xcb81, 0x0b40, 0xc901, 0x09c0, 0x0880, 0xc841,
            0xd801, 0x18c0, 0x1980, 0xd941, 0x1b00, 0xdbc1, 0xda81, 0x1a40,
            0x1e00, 0xdec1, 0xdf81, 0x1f40, 0xdd01, 0x1dc0, 0x1c80, 0xdc41,
            0x1400, 0xd4c1, 0xd581, 0x1540, 0xd701, 0x17c0, 0x1680, 0xd641,
            0xd201, 0x12c0, 0x1380, 0xd341, 0x1100, 0xd1c1, 0xd081, 0x1040,
            0xf001, 0x30c0, 0x3180, 0xf141, 0x3300, 0xf3c1, 0xf281, 0x3240,
            0x3600, 0xf6c1, 0xf781, 0x3740, 0xf501, 0x35c0, 0x3480, 0xf441,
            0x3c00, 0xfcc1, 0xfd81, 0x3d40, 0xff01, 0x3fc0, 0x3e80, 0xfe41,
            0xfa01, 0x3ac0, 0x3b80, 0xfb41, 0x3900, 0xf9c1, 0xf881, 0x3840,
            0x2800, 0xe8c1, 0xe981, 0x2940, 0xeb01, 0x2bc0, 0x2a80, 0xea41,
            0xee01, 0x2ec0, 0x2f80, 0xef41, 0x2d00, 0xedc1, 0xec81, 0x2c40,
            0xe401, 0x24c0, 0x2580, 0xe541, 0x2700, 0xe7c1, 0xe681, 0x2640,
            0x2200, 0xe2c1, 0xe381, 0x2340, 0xe101, 0x21c0, 0x2080, 0xe041,
            0xa001, 0x60c0, 0x6180, 0xa141, 0x6300, 0xa3c1, 0xa281, 0x6240,
            0x6600, 0xa6c1, 0xa781, 0x6740, 0xa501, 0x65c0, 0x6480, 0xa441,
            0x6c00, 0xacc1, 0xad81, 0x6d40, 0xaf01, 0x6fc0, 0x6e80, 0xae41,
            0xaa01, 0x6ac0, 0x6b80, 0xab41, 0x6900, 0xa9c1, 0xa881, 0x6840,
            0x7800, 0xb8c1, 0xb981, 0x7940, 0xbb01, 0x7bc0, 0x7a80, 0xba41,
            0xbe01, 0x7ec0, 0x7f80, 0xbf41, 0x7d00, 0xbdc1, 0xbc81, 0x7c40,
            0xb401, 0x74c0, 0x7580, 0xb541, 0x7700, 0xb7c1, 0xb681, 0x7640,
            0x7200, 0xb2c1, 0xb381, 0x7340, 0xb101, 0x71c0, 0x7080, 0xb041,
            0x5000, 0x90c1, 0x9181, 0x5140, 0x9301, 0x53c0, 0x5280, 0x9241,
            0x9601, 0x56c0, 0x5780, 0x9741, 0x5500, 0x95c1, 0x9481, 0x5440,
            0x9c01, 0x5cc0, 0x5d80, 0x9d41, 0x5f00, 0x9fc1, 0x9e81, 0x5e40,
            0x5a00, 0x9ac1, 0x9b81, 0x5b40, 0x9901, 0x59c0, 0x5880, 0x9841,
            0x8801, 0x48c0, 0x4980, 0x8941, 0x4b00, 0x8bc1, 0x8a81, 0x4a40,
            0x4e00, 0x8ec1, 0x8f81, 0x4f40, 0x8d01, 0x4dc0, 0x4c80, 0x8c41,
            0x4400, 0x84c1, 0x8581, 0x4540, 0x8701, 0x47c0, 0x4680, 0x8641,
            0x8201, 0x42c0, 0x4380, 0x8341, 0x4100, 0x81c1, 0x8081, 0x4040,
        };

        #endregion

        #region Enums
        public enum LayerRleFormat
        {
            PWS,
            PW0
        }
        
        public enum AnyCubicMachine : byte
        {
            AnyCubicPhotonS,
            AnyCubicPhotonZero,
            AnyCubicPhotonX,
            AnyCubicPhotonUltra,
            AnyCubicPhotonMono,
            AnyCubicPhotonMonoSE,
            AnyCubicPhotonMonoX,
            AnyCubicPhotonMonoSQ,
        }
        #endregion

        #region Sub Classes

        #region FileMark
        public class FileMark
        {
            public const string SectionMarkFile = "ANYCUBIC";

            /// <summary>
            /// Gets the file mark placeholder
            /// Fixed to "ANYCUBIC"
            /// 00
            /// </summary>
            [FieldOrder(0)]
            [FieldLength(MarkSize)]
            [SerializeAs(SerializedType.TerminatedString)]
            public string Mark { get; set; } = SectionMarkFile;

            /// <summary>
            /// Gets the file format version
            /// 0C
            /// </summary>
            [FieldOrder(1)] public uint Version { get; set; } = VERSION_1;

            /// <summary>
            /// Gets the area num
            /// 10
            /// </summary>
            [FieldOrder(2)] public uint AreaNum { get; set; } = 4;

            /// <summary>
            /// Gets the header start address
            /// 14
            /// </summary>
            [FieldOrder(3)]  public uint HeaderAddress { get; set; }

            /// <summary>
            /// 18
            /// </summary>
            [FieldOrder(4)]  public uint Padding1 { get; set; }

            /// <summary>
            /// Gets the preview start offset
            /// 1C
            /// </summary>
            [FieldOrder(5)]  public uint PreviewAddress { get; set; }

            /// <summary>
            /// 20, Spoted on version 515 only
            /// </summary>
            [FieldOrder(6)]  public uint PreviewEndAddress  { get; set; } 

            /// <summary>
            /// Gets the layer definition start address
            /// 24
            /// </summary>
            [FieldOrder(7)]  public uint LayerDefinitionAddress { get; set; }

            /// <summary>
            /// 28
            /// </summary>
            [FieldOrder(8)]  public uint Padding2  { get; set; }

            /// <summary>
            /// Gets layer image start address
            /// 2C
            /// </summary>
            [FieldOrder(9)]  public uint LayerImageAddress { get; set; }

            public override string ToString()
            {
                return $"{nameof(Mark)}: {Mark}, {nameof(Version)}: {Version}, {nameof(AreaNum)}: {AreaNum}, {nameof(HeaderAddress)}: {HeaderAddress}, {nameof(Padding1)}: {Padding1}, {nameof(PreviewAddress)}: {PreviewAddress}, {nameof(PreviewEndAddress)}: {PreviewEndAddress}, {nameof(LayerDefinitionAddress)}: {LayerDefinitionAddress}, {nameof(Padding2)}: {Padding2}, {nameof(LayerImageAddress)}: {LayerImageAddress}";
            }
        }
        #endregion

        #region Section

        public class SectionHeader
        {
            /// <summary>
            /// Gets the section mark placeholder
            /// </summary>
            [FieldOrder(0)]
            [FieldLength(MarkSize)]
            [SerializeAs(SerializedType.TerminatedString)]
            public string Mark { get; set; }

            /// <summary>
            /// Gets the length of this section
            /// </summary>
            [FieldOrder(1)] public uint Length { get; set; }

            public SectionHeader() { }

            public SectionHeader(string mark, object obj) : this(mark)
            {
                //Debug.WriteLine(Helpers.Serializer.SizeOf(obj));
                Length = (uint)Helpers.Serializer.SizeOf(obj);
            }

            public SectionHeader(string mark, uint length = 0)
            {
                Mark = mark;
                Length = length;
            }


            public void Validate(string mark, object obj = null)
            {
                Validate(mark, 0, obj);
            }

            public void Validate(string mark, int length, object obj = null)
            {
                if (!Mark.Equals(mark))
                {
                    throw new FileLoadException($"'{Mark}' section expected, but got '{mark}'");
                }

                if (obj is not null)
                {
                    length += (int)Helpers.Serializer.SizeOf(obj);
                }

                if (length > 0 && Length != length)
                {
                    throw new FileLoadException($"{Mark} section bytes: expected {Length}, got {length}, difference: {(int)Length - length}");
                }
            }

            public override string ToString() => $"[{nameof(Mark)}: {Mark}, {nameof(Length)}: {Length}]";
        }

        #endregion

        #region Header
        public class Header
        {
            public const string SectionMark = "HEADER";

            /// <summary>
            /// 30
            /// </summary>
            [FieldOrder(0)] public SectionHeader Section { get; set; }

            /// <summary>
            /// 40
            /// </summary>
            [FieldOrder(1)] public float PixelSizeUm { get; set; } = 47.25f;

            /// <summary>
            /// Layer height in mm
            /// 44
            /// </summary>
            [FieldOrder(2)] public float LayerHeight { get; set; }

            /// <summary>
            /// 48
            /// </summary>
            [FieldOrder(3)] public float LayerExposureTime { get; set; }

            /// <summary>
            /// 4C
            /// </summary>
            [FieldOrder(4)] public float LightOffDelay { get; set; } = 1;

            /// <summary>
            /// 50
            /// </summary>
            [FieldOrder(5)] public float BottomExposureSeconds { get; set; } 

            /// <summary>
            /// 54
            /// </summary>
            [FieldOrder(6)] public float BottomLayersCount { get; set; }

            /// <summary>
            /// 58
            /// </summary>
            [FieldOrder(7)] public float LiftHeight { get; set; } = 6;
            /// <summary>
            /// Gets the lift speed in mm/s
            /// 5C
            /// </summary>
            [FieldOrder(8)] public float LiftSpeed { get; set; } = 3; // mm/s

            /// <summary>
            /// Gets the retract speed in mm/s
            /// 60
            /// </summary>
            [FieldOrder(9)] public float RetractSpeed { get; set; } = 3; // mm/s

            /// <summary>
            /// 64
            /// </summary>
            [FieldOrder(10)] public float VolumeMl { get; set; }

            /// <summary>
            /// 68
            /// </summary>
            [FieldOrder(11)] public uint AntiAliasing { get; set; } = 1;

            /// <summary>
            /// 6C
            /// </summary>
            [FieldOrder(12)] public uint ResolutionX { get; set; }

            /// <summary>
            /// 70
            /// </summary>
            [FieldOrder(13)] public uint ResolutionY { get; set; }

            /// <summary>
            /// 74
            /// </summary>
            [FieldOrder(14)] public float WeightG { get; set; }

            /// <summary>
            /// 78
            /// </summary>
            [FieldOrder(15)] public float Price { get; set; }

            /// <summary>
            /// 24 00 00 00 $ or ¥ C2 A5 00 or € = E2 82 AC 00
            /// 7C
            /// </summary>
            [FieldOrder(16)] public uint PriceCurrencyDec { get; set; } = 0x24;
            [Ignore] public char PriceCurrencySymbol
            {
                get
                {
                    switch (PriceCurrencyDec)
                    {
                        case 0x24:
                            return '$';
                        case 0xA5C2:
                            return '¥';
                        case 0x000020AC:
                            return '€';
                        default:
                            return '$';
                    }
                }
            }

            /// <summary>
            /// 80
            /// </summary>
            [FieldOrder(17)] public uint PerLayerOverride { get; set; } // bool

            /// <summary>
            /// 84
            /// </summary>
            [FieldOrder(18)] public uint PrintTime { get; set; }

            /// <summary>
            /// 88
            /// </summary>
            [FieldOrder(19)] public uint Padding1 { get; set; }

            /// <summary>
            /// 8C
            /// </summary>
            [FieldOrder(20)] public uint Padding2 { get; set; }

            public Header()
            {
                Section = new SectionHeader(SectionMark, this);
            }

            public override string ToString() => $"{nameof(Section)}: {Section}, {nameof(PixelSizeUm)}: {PixelSizeUm}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(LayerExposureTime)}: {LayerExposureTime}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(VolumeMl)}: {VolumeMl}, {nameof(AntiAliasing)}: {AntiAliasing}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(WeightG)}: {WeightG}, {nameof(Price)}: {Price}, {nameof(PriceCurrencyDec)}: {PriceCurrencyDec}, {nameof(PerLayerOverride)}: {PerLayerOverride}, {nameof(PrintTime)}: {PrintTime}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}";

            public void Validate()
            {
                Section.Validate(SectionMark, (int)-Helpers.Serializer.SizeOf(Section), this);
            }
        }

        #endregion

        #region Preview

        /// <summary>
        /// The files contain two preview images.
        /// These are shown on the printer display when choosing which file to print, sparing the poor printer from needing to render a 3D image from scratch.
        /// </summary>
        public class Preview
        {
            public const string SectionMark = "PREVIEW";

            /// <summary>
            /// 90
            /// </summary>
            [FieldOrder(0)] public SectionHeader Section { get; set; }

            /// <summary>
            /// Gets the image width, in pixels.
            /// A0
            /// </summary>
            [FieldOrder(1)] public uint ResolutionX { get; set; } = 224;

            /// <summary>
            /// Gets the resolution of the image, in dpi.
            /// A4
            /// </summary>
            [FieldOrder(2)] public uint DpiResolution { get; set; } = 42;

            /// <summary>
            /// Gets the image height, in pixels.
            /// A8
            /// </summary>
            [FieldOrder(3)] public uint ResolutionY { get; set; } = 168;

            /*[FieldOrder(4)] public uint Unknown1 { get; set; }
            [FieldOrder(5)] public uint Unknown2 { get; set; }
            [FieldOrder(6)] public uint Unknown3 { get; set; }
            [FieldOrder(7)] public uint Unknown4 { get; set; }*/

            [Ignore] public uint DataSize => ResolutionX * ResolutionY * 2;

            // little-endian 16bit colors, RGB 565 encoded.
            //[FieldOrder(4)] [FieldLength(nameof(Section)+"."+nameof(SectionHeader.Length))]
            [Ignore] public byte[] Data { get; set; }

            public Preview()
            {
                Section = new SectionHeader(SectionMark, this);
            }

            public Preview(uint resolutionX, uint resolutionY, uint dpiResolution = 42) : this()
            {
                ResolutionX = resolutionX;
                ResolutionY = resolutionY;
                DpiResolution = dpiResolution;
                Data = new byte[DataSize];
                Section.Length += (uint)Data.Length;
            }

            /*public unsafe Mat Decode(bool consumeData = true)
            {
                Mat image = new(new Size((int)ResolutionX, (int)ResolutionY), DepthType.Cv8U, 3);
                var span = image.GetBytePointer();

                int pixel = 0;
                for (uint i = 0; i < Data.Length; i += 2)
                {
                    ushort color16 = BitExtensions.ToUShortLittleEndian(Data[i], Data[i+1]);
                    byte r = (byte)((color16 >> 11) & 0x1f);
                    byte g = (byte)((color16 >> 5) & 0x3f);
                    byte b = (byte)((color16 >> 0) & 0x1f);

                    span[pixel++] = (byte) ((b << 3) | (b & 0x7));
                    span[pixel++] = (byte) ((g << 2) | (g & 0x3));
                    span[pixel++] = (byte) ((r << 3) | (r & 0x7));
                }

                if (consumeData)
                    Data = null;

                return image;
            }

            public static unsafe Preview Encode(Mat image)
            {
                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

                Preview preview = new((uint) image.Width, (uint) image.Height);

                int i = 0;
                for (int pixel = 0; pixel < imageLength; pixel += image.NumberOfChannels)
                {
                    // BGR
                    byte b = (byte)(span[pixel] >> 3);
                    byte g = (byte)(span[pixel+1] >> 2);
                    byte r = (byte)(span[pixel+2] >> 3);
                    

                    ushort color = (ushort) ((r << 11) | (g << 5) | (b << 0));

                    preview.Data[i++] = (byte) color;
                    preview.Data[i++] = (byte) (color >> 8);
                }

                return preview;
            }*/
            
            public override string ToString()
            {
                return $"{nameof(Section)}: {Section}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(DpiResolution)}: {DpiResolution}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(Data)}: {Data?.Length ?? 0}";
            }

            public void Validate(int size)
            {
                Section.Validate(SectionMark, (int) (size - Helpers.Serializer.SizeOf(Section)), this);
            }
        }

        #endregion

        #region Layer

        public class LayerDef
        {
            public const byte ClassSize = 32;
            /// <summary>
            /// Gets the layer image offset to encoded layer data, and its length in bytes.
            /// </summary>
            [FieldOrder(0)] public uint DataAddress { get; set; }

            /// <summary>
            /// Gets the layer image length in bytes.
            /// </summary>
            [FieldOrder(1)] public uint DataLength { get; set; }

            [FieldOrder(2)] public float LiftHeight { get; set; }

            [FieldOrder(3)] public float LiftSpeed { get; set; }

            /// <summary>
            /// Gets the exposure time for this layer, in seconds.
            /// </summary>
            [FieldOrder(4)] public float ExposureTime { get; set; }

            /// <summary>
            /// Gets the layer height for this layer, measured in millimeters.
            /// </summary>
            [FieldOrder(5)] public float LayerHeight { get; set; }

            [FieldOrder(6)] public uint NonZeroPixelCount { get; set; }
            [FieldOrder(7)] public uint Padding1 { get; set; }

            [Ignore] public byte[] EncodedRle { get; set; }
            [Ignore] public PhotonWorkshopFile Parent { get; set; }

            public LayerDef()
            {
            }

            public LayerDef(PhotonWorkshopFile parent, Layer layer)
            {
                Parent = parent;
                SetFrom(layer);
            }

            public void SetFrom(Layer layer)
            {
                LayerHeight = layer.LayerHeight;
                ExposureTime = layer.ExposureTime;
                LiftHeight = layer.LiftHeight;
                LiftSpeed = (float) Math.Round(layer.LiftSpeed / 60, 2);
                NonZeroPixelCount = layer.NonZeroPixelCount;
            }

            public void CopyTo(Layer layer)
            {
                layer.ExposureTime = ExposureTime;
                layer.LiftHeight = LiftHeight;
                layer.LiftSpeed = (float)Math.Round(LiftSpeed * 60, 2);
            }

            public Mat Decode(bool consumeData = true)
            {
                var result = Parent.LayerFormat == LayerRleFormat.PWS ? DecodePWS() : DecodePW0();
                if (consumeData)
                    EncodedRle = null;

                return result;
            }

            public byte[] Encode(Mat image)
            {
                EncodedRle = Parent.LayerFormat == LayerRleFormat.PWS ? EncodePWS(image) : EncodePW0(image);
                return EncodedRle;
            }

            private unsafe Mat DecodePWS()
            {
                var image = EmguExtensions.InitMat(Parent.Resolution);
                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

                int index = 0;
                for (byte bit = 0; bit < Parent.AntiAliasing; bit++)
                {
                    int pixel = 0;
                    for (; index < EncodedRle.Length; index++)
                    {
                        // Lower 7 bits is the repeat count for the bit (0..127)
                        int reps = EncodedRle[index] & 0x7f;

                        // We only need to set the non-zero pixels
                        // High bit is on for white, off for black
                        if ((EncodedRle[index] & 0x80) != 0)
                        {
                            for (int i = 0; i < reps; i++)
                            {
                                span[pixel + i]++;
                            }
                        }

                        pixel += reps;

                        if (pixel == imageLength)
                        {
                            index++;
                            break;
                        }

                        if (pixel > imageLength)
                        {
                            image.Dispose();
                            throw new FileLoadException("Error image ran off the end");
                        }
                    }
                }

                for (int i = 0; i < imageLength; i++)
                {
                    int newC = span[i] * (256 / Parent.AntiAliasing);

                    if (newC > 0)
                    {
                        newC--;
                    }

                    span[i] = (byte)newC;
                }

                return image;
            }

            public unsafe byte[] EncodePWS(Mat image)
            {
                List<byte> rawData = new();
                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

                bool obit;
                int rep;

                void AddRep()
                {
                    if (rep <= 0) return;

                    byte by = (byte)rep;

                    if (obit)
                    {
                        by |= 0x80;
                        //bitsOn += uint(rep)
                    }

                    rawData.Add(by);
                }

                for (byte aalevel = 1; aalevel <= Parent.AntiAliasing; aalevel++)
                {
                    obit = false;
                    rep = 0;

                    //ngrey:= uint16(r | g | b)
                    // thresholds:
                    // aa 1:  127
                    // aa 2:  255 127
                    // aa 4:  255 191 127 63
                    // aa 8:  255 223 191 159 127 95 63 31
                    //byte threshold = (byte)(256 / Parent.AntiAliasing * aalevel - 1);
                    // threshold := byte(int(255 * (level + 1) / (levels + 1))) + 1
                    byte threshold = (byte) (255 * aalevel / (Parent.AntiAliasing + 1) + 1);


                    for (int pixel = 0; pixel < imageLength; pixel++)
                    {
                        var nbit = span[pixel] >= threshold;

                        if (nbit == obit)
                        {
                            rep++;

                            if (rep == RLE1EncodingLimit)
                            {
                                AddRep();
                                rep = 0;
                            }
                        }
                        else
                        {
                            AddRep();
                            obit = nbit;
                            rep = 1;
                        }
                    }

                    // Collect stragglers
                    AddRep();
                }

                DataLength = (uint) rawData.Count;

                return rawData.ToArray();
            }

            private Mat DecodePW0()
            {
                var mat = EmguExtensions.InitMat(Parent.Resolution);
                var imageLength = mat.GetLength();

                int pixelPos = 0;
                for (int i = 0; i < EncodedRle.Length; i++)
                {
                    byte b = EncodedRle[i];
                    int code = b >> 4;
                    int repeat = b & 0xf;
                    byte color;
                    switch (code)
                    {
                        case 0x0:
                            color = 0;
                            i++;
                            //reps = reps * 256 + EncodedRle[i];
                            if (i >= EncodedRle.Length)
                            {
                                repeat = imageLength - pixelPos;
                                break;
                            }

                            repeat = (repeat << 8) + EncodedRle[i];
                            break;
                        case 0xf:
                            color = 255;
                            i++;
                            //reps = reps * 256 + EncodedRle[i];
                            if (i >= EncodedRle.Length)
                            {
                                repeat = imageLength - pixelPos;
                                break;
                            }

                            repeat = (repeat << 8) + EncodedRle[i];
                            break;
                        default:
                            color = (byte) ((code << 4) | code);
                            if (i >= EncodedRle.Length)
                            {
                                repeat = imageLength - pixelPos;
                            }
                            break;
                    }

                    //color &= 0xff;

                    // We only need to set the non-zero pixels
                    mat.FillSpan(ref pixelPos, repeat, color);


                    if (pixelPos == imageLength)
                    {
                        //i++;
                        break;
                    }

                    if (pixelPos > imageLength)
                    {
                        mat.Dispose();
                        throw new FileLoadException($"Error image ran off the end: {pixelPos - repeat}({repeat}) of {imageLength}");
                    }
                }

                if (pixelPos > 0 && pixelPos != imageLength)
                {
                    mat.Dispose();
                    throw new FileLoadException($"Error image ended short: {pixelPos} of {imageLength}");
                }

                return mat;
            }

            public unsafe byte[] EncodePW0(Mat image)
            {
                List<byte> rawData = new();
                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

                int lastColor = -1;
                int reps = 0;

                void PutReps()
                {
                    while (reps > 0)
                    {
                        int done = reps;

                        if (lastColor is 0 or 0xf)
                        {
                            if (done > RLE4EncodingLimit)
                            {
                                done = RLE4EncodingLimit;
                            }
                            //more:= []byte{ 0, 0}
                            //binary.BigEndian.PutUint16(more, uint16(done | (color << 12)))

                            //rle = append(rle, more...)

                            ushort more = (ushort)(done | (lastColor << 12));
                            rawData.Add((byte)(more >> 8));
                            rawData.Add((byte)more);
                        }
                        else
                        {
                            if (done > 0xf)
                            {
                                done = 0xf;
                            }
                            rawData.Add((byte)(done | lastColor << 4));
                        }

                        reps -= done;
                    }
                }

                for (int i = 0; i < imageLength; i++)
                {
                    int color = span[i] >> 4;

                    if (color == lastColor)
                    {
                        reps++;
                    }
                    else
                    {
                        PutReps();
                        lastColor = color;
                        reps = 1;
                    }
                }

                PutReps();

                EncodedRle = rawData.ToArray();
                DataLength = (uint)rawData.Count;

                ushort crc = CRCRle4(EncodedRle);
                rawData.Add((byte)(crc >> 8));
                rawData.Add((byte)crc);

                return EncodedRle;
            }

            public static ushort CRCRle4(byte[] data)
            {
                ushort crc16 = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    crc16 = (ushort) ((crc16 << 8) ^ CRC16Table[((crc16 >> 8) ^ CRC16Table[data[i]]) & 0xff]);
                }

                crc16 = (ushort) ((CRC16Table[crc16 & 0xff] * 0x100) + CRC16Table[(crc16 >> 8) & 0xff]);

                return crc16;
            }

            public ushort CRCEncodedRle()
            {
                return CRCRle4(EncodedRle);
            }

            public override string ToString()
            {
                return $"{nameof(DataAddress)}: {DataAddress}, {nameof(DataLength)}: {DataLength}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(NonZeroPixelCount)}: {NonZeroPixelCount}, {nameof(Padding1)}: {Padding1}, {nameof(EncodedRle)}: {EncodedRle?.Length ?? 0}";
            }
        }

        #endregion

        #region LayerDefinition
        public class LayerDefinition
        {
            public const string SectionMark = "LAYERDEF";

            /// <summary>
            /// 1269C
            /// </summary>
            [FieldOrder(0)] public SectionHeader Section { get; set; }

            [FieldOrder(1)] public uint LayerCount { get; set; }

            [Ignore] public LayerDef[] Layers { get; set; }

            public LayerDefinition()
            {
                Section = new SectionHeader(SectionMark, this);
            }

            public LayerDefinition(uint layerCount) : this()
            {
                LayerCount = layerCount;
                Layers = new LayerDef[layerCount];
                Section.Length += (uint) Helpers.Serializer.SizeOf(new LayerDef()) * LayerCount;
            }

            [Ignore]
            public LayerDef this[uint index]
            {
                get => Layers[index];
                set => Layers[index] = value;
            }

            [Ignore]
            public LayerDef this[int index]
            {
                get => Layers[index];
                set => Layers[index] = value;
            }

            public void Validate()
            {
                Section.Validate(SectionMark, (int) (LayerCount * Helpers.Serializer.SizeOf(new LayerDef()) - Helpers.Serializer.SizeOf(Section)), this);
            }

            public override string ToString() => $"{nameof(Section)}: {Section}, {nameof(LayerCount)}: {LayerCount}";
        }
        #endregion

        #endregion

        #region Properties

        public FileMark FileMarkSettings { get; protected internal set; } = new();

        public Header HeaderSettings { get; protected internal set; } = new();

        public Preview PreviewSettings { get; protected internal set; } = new();

        public LayerDefinition LayersDefinition { get; protected internal set; } = new();

        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {

            new(typeof(PhotonWorkshopFile), "pws", "Photon / Photon S (PWS)"),
            new(typeof(PhotonWorkshopFile), "pw0", "Photon Zero (PW0)"),
            new(typeof(PhotonWorkshopFile), "pwx", "Photon X (PWX)"),
            new(typeof(PhotonWorkshopFile), "dlp", "Photon Ultra (DLP)", false, false),
            new(typeof(PhotonWorkshopFile), "pwmx", "Photon Mono X (PWMX)"),
            new(typeof(PhotonWorkshopFile), "pwms", "Photon Mono SE (PWMS)"),
            new(typeof(PhotonWorkshopFile), "pwmo", "Photon Mono (PWMO)"),
            new(typeof(PhotonWorkshopFile), "pmsq", "Photon Mono SQ (PMSQ)", false, false),
            
            
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,

            PrintParameterModifier.LightOffDelay,

            PrintParameterModifier.BottomExposureTime,
            PrintParameterModifier.ExposureTime,

            //PrintParameterModifier.BottomLightOffDelay,
            //PrintParameterModifier.BottomLiftHeight,
            //PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
        };

        public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
            PrintParameterModifier.ExposureTime,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
        };

        public override Size[] ThumbnailsOriginalSize { get; } = {new(224, 168)};

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
            get
            {
                return PrinterModel switch
                {
                    AnyCubicMachine.AnyCubicPhotonS => 68.04f,
                    AnyCubicMachine.AnyCubicPhotonZero => 55.44f,
                    AnyCubicMachine.AnyCubicPhotonX => 192,
                    AnyCubicMachine.AnyCubicPhotonUltra => 102.40f,
                    AnyCubicMachine.AnyCubicPhotonMono => 82.62f,
                    AnyCubicMachine.AnyCubicPhotonMonoSE => 82.62f,
                    AnyCubicMachine.AnyCubicPhotonMonoX => 192,
                    AnyCubicMachine.AnyCubicPhotonMonoSQ => 120,
                    _ => 0
                };
            }
            set { }
        }
        public override float DisplayHeight
        {
            get
            {
                return PrinterModel switch
                {
                    AnyCubicMachine.AnyCubicPhotonS => 120.96f,
                    AnyCubicMachine.AnyCubicPhotonZero => 98.637f,
                    AnyCubicMachine.AnyCubicPhotonX => 120,
                    AnyCubicMachine.AnyCubicPhotonUltra => 57.60f,
                    AnyCubicMachine.AnyCubicPhotonMono => 130.56f,
                    AnyCubicMachine.AnyCubicPhotonMonoSE => 130.56f,
                    AnyCubicMachine.AnyCubicPhotonMonoX => 120,
                    AnyCubicMachine.AnyCubicPhotonMonoSQ => 128,
                    _ => 0
                };
            }
            set { }
        }

        public override float MachineZ
        {
            get
            {
                return PrinterModel switch
                {
                    AnyCubicMachine.AnyCubicPhotonS => 165,
                    AnyCubicMachine.AnyCubicPhotonZero => 150,
                    AnyCubicMachine.AnyCubicPhotonX => 245,
                    AnyCubicMachine.AnyCubicPhotonUltra => 165,
                    AnyCubicMachine.AnyCubicPhotonMono => 165,
                    AnyCubicMachine.AnyCubicPhotonMonoSE => 160,
                    AnyCubicMachine.AnyCubicPhotonMonoX => 245,
                    AnyCubicMachine.AnyCubicPhotonMonoSQ => 200,
                    _ => 0
                };
            }
            set { }
        }

        public override Enumerations.FlipDirection DisplayMirror
        {
            get => Enumerations.FlipDirection.Horizontally;
            set {}
        }

        public override bool IsAntiAliasingEmulated => true;

        public override byte AntiAliasing
        {
            get => (byte) HeaderSettings.AntiAliasing;
            set
            {
                base.AntiAliasing = (byte)(HeaderSettings.AntiAliasing = value.Clamp(1, 16));
                ValidateAntiAliasingLevel();
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
            set => base.LayerCount = LayersDefinition.LayerCount = base.LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => (ushort) HeaderSettings.BottomLayersCount;
            set => base.BottomLayerCount = (ushort) (HeaderSettings.BottomLayersCount = value);
        }

        public override float BottomLightOffDelay => LightOffDelay;

        public override float LightOffDelay
        {
            get => HeaderSettings.LightOffDelay;
            set => base.LightOffDelay = HeaderSettings.LightOffDelay = (float)Math.Round(value, 2);
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

        public override float BottomExposureTime
        {
            get => HeaderSettings.BottomExposureSeconds;
            set => base.BottomExposureTime = HeaderSettings.BottomExposureSeconds = (float) Math.Round(value, 2);
        }

        public override float ExposureTime
        {
            get => HeaderSettings.LayerExposureTime;
            set => base.ExposureTime = HeaderSettings.LayerExposureTime = (float) Math.Round(value, 2);
        }

        public override float BottomLiftHeight => LiftHeight;

        public override float LiftHeight
        {
            get => HeaderSettings.LiftHeight;
            set => base.LiftHeight = HeaderSettings.LiftHeight = (float) Math.Round(value, 2);
        }

        public override float BottomLiftSpeed => LiftSpeed;

        public override float LiftSpeed
        {
            get => (float)Math.Round(HeaderSettings.LiftSpeed * 60, 2);
            set
            {
                HeaderSettings.LiftSpeed = (float) Math.Round(value / 60, 2);
                base.LiftSpeed = value;
            }
        }

        public override float BottomRetractSpeed => RetractSpeed;

        public override float RetractSpeed
        {
            get => (float)Math.Round(HeaderSettings.RetractSpeed * 60, 2);
            set
            {
                HeaderSettings.RetractSpeed = (float) Math.Round(value / 60, 2);
                base.RetractSpeed = value;
            }
        }

        public override float PrintTime
        {
            get => base.PrintTime;
            set
            {
                base.PrintTime = value;
                HeaderSettings.PrintTime = (uint) base.PrintTime;
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
            set => base.MaterialGrams = HeaderSettings.WeightG = (float) Math.Round(value, 3);
        }

        public override float MaterialCost
        {
            get => (float) Math.Round(HeaderSettings.Price, 3);
            set => base.MaterialCost = HeaderSettings.Price = (float)Math.Round(value, 3);
        }

        public override string MachineName
        {
            get
            {
                return PrinterModel switch
                {
                    AnyCubicMachine.AnyCubicPhotonS => "AnyCubic Photon S",
                    AnyCubicMachine.AnyCubicPhotonZero => "AnyCubic Photon Zero",
                    AnyCubicMachine.AnyCubicPhotonX => "AnyCubic Photon X",
                    AnyCubicMachine.AnyCubicPhotonUltra => "AnyCubic Photon Ultra",
                    AnyCubicMachine.AnyCubicPhotonMono => "AnyCubic Photon Mono",
                    AnyCubicMachine.AnyCubicPhotonMonoSE => "AnyCubic Photon Mono SE",
                    AnyCubicMachine.AnyCubicPhotonMonoX => "AnyCubic Photon Mono X",
                    AnyCubicMachine.AnyCubicPhotonMonoSQ => "AnyCubic Photon Mono SQ",
                    _ => base.MachineName
                };
            }
        }
        
        public override object[] Configs => new object[] { FileMarkSettings, HeaderSettings, PreviewSettings, LayersDefinition };

        public LayerRleFormat LayerFormat =>
            FileEndsWith(".pws")
                ? LayerRleFormat.PWS
                : LayerRleFormat.PW0;

        public AnyCubicMachine PrinterModel
        {
            get
            {
                if (FileEndsWith(".pws"))
                {
                    return AnyCubicMachine.AnyCubicPhotonS;
                }

                if (FileEndsWith(".pw0"))
                {
                    return AnyCubicMachine.AnyCubicPhotonZero;
                }

                if (FileEndsWith(".pwx"))
                {
                    return AnyCubicMachine.AnyCubicPhotonX;
                }

                if (FileEndsWith(".dlp"))
                {
                    return AnyCubicMachine.AnyCubicPhotonUltra;
                }

                if (FileEndsWith(".pwmo"))
                {
                    return AnyCubicMachine.AnyCubicPhotonMono;
                }

                if (FileEndsWith(".pwms"))
                {
                    return AnyCubicMachine.AnyCubicPhotonMonoSE;
                }

                if (FileEndsWith(".pwmx"))
                {
                    return AnyCubicMachine.AnyCubicPhotonMonoX;
                }

                if (FileEndsWith(".pmsq"))
                {
                    return AnyCubicMachine.AnyCubicPhotonMonoSQ;
                }

                return AnyCubicMachine.AnyCubicPhotonS;
            }
        } 
        #endregion

        #region Constructors
        public PhotonWorkshopFile()
        {
        }
        #endregion

        #region Methods
        public override void Clear()
        {
            base.Clear();

            LayersDefinition = null;
        }

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            HeaderSettings.PixelSizeUm = PixelSizeMicronsMax;
                
                /*PrinterModel switch
            {
                AnyCubicMachine.AnyCubicPhotonS => 47.25f,
                AnyCubicMachine.AnyCubicPhotonZero => 115.5f,
                AnyCubicMachine.AnyCubicPhotonX => 75,
                AnyCubicMachine.AnyCubicPhotonUltra => 80,
                AnyCubicMachine.AnyCubicPhotonMono => 51,
                AnyCubicMachine.AnyCubicPhotonMonoSE => 51,
                AnyCubicMachine.AnyCubicPhotonMonoX => 50,
                AnyCubicMachine.AnyCubicPhotonMonoSQ => 50,
                _ => 47.25f
            };*/

            HeaderSettings.PerLayerOverride = (byte)(LayerManager.AllLayersAreUsingGlobalParameters ? 0 : 1);


            FileMarkSettings.HeaderAddress = (uint) Helpers.Serializer.SizeOf(FileMarkSettings);
            using var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
            outputFile.Seek((int)FileMarkSettings.HeaderAddress, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

            if (CreatedThumbnailsCount > 0)
            {
                FileMarkSettings.PreviewAddress = (uint)outputFile.Position;
                //Preview preview = Preview.Encode(Thumbnails[0]);
                var preview = new Preview((uint)Thumbnails[0].Width, (uint)Thumbnails[0].Height)
                {
                    Data = EncodeImage(DATATYPE_RGB565, Thumbnails[0])
                };
                Helpers.SerializeWriteFileStream(outputFile, preview);
                outputFile.WriteBytes(preview.Data);
                FileMarkSettings.PreviewEndAddress = (uint)outputFile.Position;
            }

            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
            FileMarkSettings.LayerDefinitionAddress = (uint)outputFile.Position;
            LayersDefinition = new LayerDefinition(LayerCount);
            Helpers.SerializeWriteFileStream(outputFile, LayersDefinition);
            uint layerDefOffset = (uint)outputFile.Position;
            uint layerRleOffset = FileMarkSettings.LayerImageAddress = (uint)(layerDefOffset + Helpers.Serializer.SizeOf(new LayerDef()) * LayerCount);
            var layersHash = new Dictionary<string, LayerDef>();

            foreach (var batch in BatchLayersIndexes())
            {
                Parallel.ForEach(batch, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    using (var mat = this[layerIndex].LayerMat)
                    {
                        LayersDefinition.Layers[layerIndex] = new LayerDef(this, this[layerIndex]);
                        LayersDefinition.Layers[layerIndex].Encode(mat);
                    }
                    progress.LockAndIncrement();
                });

                foreach (var layerIndex in batch)
                {
                    progress.Token.ThrowIfCancellationRequested();

                    var layerDef = LayersDefinition.Layers[layerIndex];

                    var hash = CryptExtensions.ComputeSHA1Hash(layerDef.EncodedRle);

                    if (layersHash.TryGetValue(hash, out var layerDataHash))
                    {
                        layerDef.DataAddress = layerDataHash.DataAddress;
                        layerDef.DataLength = (uint)layerDataHash.EncodedRle.Length;
                    }
                    else
                    {
                        layersHash.Add(hash, layerDef);

                        layerDef.DataAddress = layerRleOffset;

                        outputFile.Seek(layerRleOffset, SeekOrigin.Begin);
                        layerRleOffset += Helpers.SerializeWriteFileStream(outputFile, layerDef.EncodedRle);
                    }

                    outputFile.Seek(layerDefOffset, SeekOrigin.Begin);
                    layerDefOffset += Helpers.SerializeWriteFileStream(outputFile, layerDef);
                }
            }
            
            // Rewind
            outputFile.Seek(0, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, FileMarkSettings);
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);
            FileMarkSettings = Helpers.Deserialize<FileMark>(inputFile);

            Debug.Write("FileMark -> ");
            Debug.WriteLine(FileMarkSettings);

            if (!FileMarkSettings.Mark.Equals(FileMark.SectionMarkFile))
            {
                throw new FileLoadException(
                    $"Invalid Filemark {FileMarkSettings.Mark}, expected {FileMark.SectionMarkFile}", fileFullPath);
            }

            if (FileMarkSettings.Version is not VERSION_1 and not VERSION_515)
            {
                throw new FileLoadException($"Invalid Version {FileMarkSettings.Version}, expected {VERSION_1} or {VERSION_515}",
                    fileFullPath);
            }

            FileFullPath = fileFullPath;

            inputFile.Seek(FileMarkSettings.HeaderAddress, SeekOrigin.Begin);
            HeaderSettings = Helpers.Deserialize<Header>(inputFile);

            Debug.Write("Header -> ");
            Debug.WriteLine(HeaderSettings);

            HeaderSettings.Validate();

            if (FileMarkSettings.PreviewAddress > 0)
            {
                inputFile.Seek(FileMarkSettings.PreviewAddress, SeekOrigin.Begin);

                PreviewSettings = Helpers.Deserialize<Preview>(inputFile);
                Debug.Write("Preview -> ");
                Debug.WriteLine(PreviewSettings);

                //PreviewSettings.Validate((int) PreviewSettings.DataSize);

                PreviewSettings.Data = new byte[PreviewSettings.DataSize];
                inputFile.ReadBytes(PreviewSettings.Data);

                Thumbnails[0] = DecodeImage(DATATYPE_RGB565, PreviewSettings.Data, PreviewSettings.ResolutionX, PreviewSettings.ResolutionY);
                //Thumbnails[0] = PreviewSettings.Decode();
                PreviewSettings.Data = null;
            }

            inputFile.Seek(FileMarkSettings.LayerDefinitionAddress, SeekOrigin.Begin);

            LayersDefinition = Helpers.Deserialize<LayerDefinition>(inputFile);
            Debug.Write("LayersDefinition -> ");
            Debug.WriteLine(LayersDefinition);

            LayerManager.Init(LayersDefinition.LayerCount);
            LayersDefinition.Layers = new LayerDef[LayerCount];
            
            LayersDefinition.Validate();

            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
            foreach (var batch in BatchLayersIndexes())
            {
                foreach (var layerIndex in batch)
                {
                    progress.Token.ThrowIfCancellationRequested();

                    LayersDefinition[layerIndex] = Helpers.Deserialize<LayerDef>(inputFile);
                    LayersDefinition[layerIndex].Parent = this;
                    Debug.WriteLine($"Layer {layerIndex}: {LayersDefinition[layerIndex]}");


                    inputFile.SeekDoWorkAndRewind(LayersDefinition[layerIndex].DataAddress, () =>
                    {
                        LayersDefinition[layerIndex].EncodedRle = inputFile.ReadBytes(LayersDefinition[layerIndex].DataLength);
                    });
                }

                Parallel.ForEach(batch, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    using (var mat = LayersDefinition[layerIndex].Decode())
                    {
                        var layer = new Layer((uint)layerIndex, mat, this)
                        {
                            PositionZ = Layer.RoundHeight(LayersDefinition[(uint)layerIndex].LayerHeight),
                        };
                        LayersDefinition[layerIndex].CopyTo(layer);
                        this[layerIndex] = layer;
                    }

                    progress.LockAndIncrement();
                });
            }

            // Fix position z height values
            if (LayerCount > 0)
            {
                for (uint layerIndex = 1; layerIndex < LayerCount; layerIndex++)
                {
                    this[layerIndex].PositionZ = Layer.RoundHeight(this[layerIndex-1].PositionZ + this[layerIndex].PositionZ);
                }
            }
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

            HeaderSettings.PerLayerOverride = LayerManager.AllLayersAreUsingGlobalParameters ? 0 : 1u;

            using var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write);
            outputFile.Seek(FileMarkSettings.HeaderAddress, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);


            outputFile.Seek(FileMarkSettings.LayerDefinitionAddress + Helpers.Serializer.SizeOf(LayersDefinition), SeekOrigin.Begin);
            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                LayersDefinition[layerIndex].SetFrom(this[layerIndex]);
                Helpers.SerializeWriteFileStream(outputFile, LayersDefinition[layerIndex]);
            }
        }

        #endregion
    }
}
