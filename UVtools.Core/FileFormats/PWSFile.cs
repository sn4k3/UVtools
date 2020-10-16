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
    public class PWSFile : FileFormat
    {
        #region Constants
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
        #endregion

        #region Sub Classes

        #region FileMark
        public class FileMark
        {
            public const string SectionMarkFile = "ANYCUBIC";

            private string _mark = SectionMarkFile;
            /// <summary>
            /// Gets the file mark placeholder
            /// Fixed to "ANYCUBIC"
            /// </summary>
            [FieldOrder(0)]
            [FieldLength(MarkSize)]
            public string Mark
            {
                get => _mark;
                set => _mark = value.TrimEnd('\0');
            }

            /// <summary>
            /// Gets the file format version
            /// </summary>
            [FieldOrder(1)] public uint Version { get; set; } = 1;

            /// <summary>
            /// Gets the area num
            /// </summary>
            [FieldOrder(2)] public uint AreaNum { get; set; } = 4;

            /// <summary>
            /// Gets the header start address
            /// </summary>
            [FieldOrder(3)]  public uint HeaderAddress { get; set; }

            [FieldOrder(4)]  public uint Offset1 { get; set; }

            /// <summary>
            /// Gets the preview start offset
            /// </summary>
            [FieldOrder(5)]  public uint PreviewAddress { get; set; }

            [FieldOrder(6)]  public uint Offset2  { get; set; }

            /// <summary>
            /// Gets the layer definition start address
            /// </summary>
            [FieldOrder(7)]  public uint LayerDefinitionAddress { get; set; }

            [FieldOrder(8)]  public uint Offset3  { get; set; }

            /// <summary>
            /// Gets layer image start address
            /// </summary>
            [FieldOrder(9)]  public uint LayerImageAddress { get; set; }

            public override string ToString()
            {
                return $"{nameof(Mark)}: {Mark}, {nameof(Version)}: {Version}, {nameof(AreaNum)}: {AreaNum}, {nameof(HeaderAddress)}: {HeaderAddress}, {nameof(Offset1)}: {Offset1}, {nameof(PreviewAddress)}: {PreviewAddress}, {nameof(Offset2)}: {Offset2}, {nameof(LayerDefinitionAddress)}: {LayerDefinitionAddress}, {nameof(Offset3)}: {Offset3}, {nameof(LayerImageAddress)}: {LayerImageAddress}";
            }
        }
        #endregion

        #region Section

        public class Section
        {
            private string _mark;

            /// <summary>
            /// Gets the section mark placeholder
            /// </summary>
            [FieldOrder(0)]
            [FieldLength(MarkSize)]
            public string Mark
            {
                get => _mark;
                set => _mark = value.TrimEnd('\0');
            }

            /// <summary>
            /// Gets the length of this section
            /// </summary>
            [FieldOrder(1)] public uint Length { get; set; }

            public Section() { }

            public Section(string mark, object obj) : this(mark, (uint)Helpers.Serializer.SizeOf(obj)) { }

            public Section(string mark, uint length = 0)
            {
                Mark = mark;
                Length = length;
            }


            public void Validate(string mark, object obj = null)
            {
                Validate(mark, 0u, obj);
            }

            public void Validate(string mark, uint length, object obj = null)
            {
                if (!Mark.Equals(mark))
                {
                    throw new FileLoadException($"'{Mark}' section expected, but got '{mark}'");
                }

                if (!ReferenceEquals(obj, null))
                {
                    length += (uint)Helpers.Serializer.SizeOf(obj);
                }

                if (length > 0 && Length != length)
                {
                    throw new FileLoadException($"{Mark} section bytes: expected {Length}, got {length}, difference: {(int)Length - length}");
                }
            }

            public override string ToString() => $"{{{nameof(Mark)}: {Mark}, {nameof(Length)}: {Length}}}";
        }

        #endregion

        #region Header
        public class Header
        {
            public const string SectionMark = "HEADER";

            [Ignore] public Section Section { get; set; }
            [FieldOrder(0)] public float PixelSize { get; set; }
            [FieldOrder(1)] public float LayerHeight { get; set; }
            [FieldOrder(2)] public float LayerExposureTime { get; set; }
            [FieldOrder(3)] public float LayerOffTime { get; set; } = 1;
            [FieldOrder(4)] public float BottomExposureSeconds { get; set; } 
            [FieldOrder(5)] public float BottomLayersCount { get; set; }
            [FieldOrder(6)] public float LiftHeight { get; set; } = 6;

            /// <summary>
            /// Gets the lift speed in mm/s
            /// </summary>
            [FieldOrder(7)] public float LiftSpeed { get; set; } = 3; // mm/s

            /// <summary>
            /// Gets the retract speed in mm/s
            /// </summary>
            [FieldOrder(8)] public float RetractSpeed { get; set; } = 3; // mm/s
            [FieldOrder(9)] public float Volume { get; set; }
            [FieldOrder(10)] public uint AntiAliasing { get; set; } = 1;
            [FieldOrder(11)] public uint ResolutionX { get; set; }
            [FieldOrder(12)] public uint ResolutionY { get; set; }
            [FieldOrder(13)] public float Weight { get; set; }
            [FieldOrder(14)] public float Price { get; set; }
            [FieldOrder(15)] public uint ResinType { get; set; } // 0x24 ?
            [FieldOrder(16)] public uint PerLayerOverride { get; set; } // bool
            [FieldOrder(17)] public uint Offset1 { get; set; }
            [FieldOrder(18)] public uint Offset2 { get; set; }
            [FieldOrder(19)] public uint Offset3 { get; set; }

            public Header()
            {
                Section = new Section(SectionMark, this);
            }

            public override string ToString() => $"{nameof(Section)}: {Section}, {nameof(PixelSize)}: {PixelSize}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(LayerExposureTime)}: {LayerExposureTime}, {nameof(LayerOffTime)}: {LayerOffTime}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(Volume)}: {Volume}, {nameof(AntiAliasing)}: {AntiAliasing}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(Weight)}: {Weight}, {nameof(Price)}: {Price}, {nameof(ResinType)}: {ResinType}, {nameof(PerLayerOverride)}: {PerLayerOverride}, {nameof(Offset1)}: {Offset1}, {nameof(Offset2)}: {Offset2}, {nameof(Offset3)}: {Offset3}";

            public void Validate()
            {
                Section.Validate(SectionMark, this);
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
            [Ignore] public Section Section { get; set; }

            /// <summary>
            /// Gets the image width, in pixels. 
            /// </summary>
            [FieldOrder(1)] public uint Width { get; set; } = 224;

            /// <summary>
            /// Gets the resolution of the image, in dpi. 
            /// </summary>
            [FieldOrder(2)] public uint Resolution { get; set; } = 42;

            /// <summary>
            /// Gets the image height, in pixels. 
            /// </summary>
            [FieldOrder(3)] public uint Height { get; set; } = 168;

            // little-endian 16bit colors, RGB 565 encoded.
            //[FieldOrder(4)]
            //[FieldLength("Section.Length")]
            [Ignore]
            public byte[] Data { get; set; }

            public Preview()
            {
                Section = new Section(SectionMark, this);
            }

            public unsafe Mat Decode(bool consumeData = true)
            {
                Mat image = new Mat(new Size((int) Width, (int) Height), DepthType.Cv8U, 3);
                var span = image.GetBytePointer();

                int pixel = 0;
                for (uint i = 0; i < Data.Length; i += 2)
                {
                    ushort color16 = (ushort)(Data[i] + (Data[i+1] << 8));
                    var r =(color16 >> 11) & 0x1f;
                    var g = (color16 >> 5) & 0x3f;
                    var b = (color16 >> 0) & 0x1f;

                    /*span[pixel++] = new Rgba32(
                        (byte)((r << 3) | (r & 0x7)), 
                        (byte)((g << 2) | (g & 0x3)),
                        (byte)((b << 3) | (b & 0x7))
                        );*/
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

                Preview preview = new Preview
                {
                    Width = (uint) image.Width,
                    Height = (uint) image.Height,
                    Resolution = 42,
                    Data = new byte[image.Width * image.Height * 2]
                };

                int i = 0;
                for (int pixel = 0; pixel < imageLength; pixel += image.NumberOfChannels)
                {
                    // BGR
                    int b = span[pixel] >> 3;
                    int g = span[pixel+1] >> 2;
                    int r = span[pixel+2] >> 3;
                    

                    ushort color = (ushort) ((r << 11) | (g << 5) | (b << 0));

                    preview.Data[i++] = (byte) color;
                    preview.Data[i++] = (byte) (color >> 8);
                }

                preview.Section.Length += (uint) preview.Data.Length;
                return preview;
            }

            public override string ToString()
            {
                return $"{nameof(Section)}: {Section}, {nameof(Width)}: {Width}, {nameof(Resolution)}: {Resolution}, {nameof(Height)}: {Height}, {nameof(Data)}: {Data}";
            }

            public void Validate(uint size)
            {
                Section.Validate(SectionMark, size, this);
            }
        }

        #endregion

        #region Layer

        public class LayerData
        {
            /// <summary>
            /// Gets the layer image offset to encoded layer data, and its length in bytes.
            /// </summary>
            [FieldOrder(0)]
            public uint DataAddress { get; set; }

            /// <summary>
            /// Gets the layer image length in bytes.
            /// </summary>
            [FieldOrder(1)]
            public uint DataLength { get; set; }

            [FieldOrder(2)] public float LiftHeight { get; set; }

            [FieldOrder(3)] public float LiftSpeed { get; set; }

            /// <summary>
            /// Gets the exposure time for this layer, in seconds.
            /// </summary>
            [FieldOrder(4)]
            public float LayerExposure { get; set; }

            /// <summary>
            /// Gets the build platform Z position for this layer, measured in millimeters.
            /// </summary>
            [FieldOrder(5)]
            public float LayerPositionZ { get; set; }

            [FieldOrder(6)] public float Offset1 { get; set; }
            [FieldOrder(7)] public float Offset2 { get; set; }

            [Ignore] public byte[] EncodedRle { get; set; }
            [Ignore] public PWSFile Parent { get; set; }

            public LayerData()
            {
            }

            public LayerData(PWSFile parent, uint layerIndex)
            {
                Parent = parent;
                LiftHeight = Parent.HeaderSettings.LiftHeight;
                LiftSpeed = Parent.HeaderSettings.LiftSpeed;

                LayerPositionZ = parent[layerIndex].PositionZ;
                LayerExposure = parent[layerIndex].ExposureTime;
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
                List<byte> rawData = new List<byte>();
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

                for (byte aalevel = 0; aalevel < Parent.AntiAliasing; aalevel++)
                {
                    obit = false;
                    rep = 0;

                    //ngrey:= uint16(r | g | b)
                    // thresholds:
                    // aa 1:  127
                    // aa 2:  255 127
                    // aa 4:  255 191 127 63
                    // aa 8:  255 223 191 159 127 95 63 31
                    byte threshold = (byte)(256 / Parent.AntiAliasing * aalevel - 1);


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

            private unsafe Mat DecodePW0()
            {
                var image = EmguExtensions.InitMat(Parent.Resolution);
                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

                uint n = 0;
                for (int index = 0; index < EncodedRle.Length; index++)
                {
                    byte b = EncodedRle[index];
                    int code = (b >> 4);
                    uint reps = (uint) (b & 0xf);
                    byte color;
                    switch (code)
                    {
                        case 0x0:
                            color = 0x00;
                            index++;
                            reps = reps * 256 + EncodedRle[index];
                            break;
                        case 0xf:
                            color = 0xff;
                            index++;
                            reps = reps * 256 + EncodedRle[index];
                            break;
                        default:
                            color = (byte) ((code << 4) | code);
                            break;
                    }

                    color &= 0xff;

                    // We only need to set the non-zero pixels
                    if (color != 0)
                    {
                        for (int i = 0; i < reps; i++)
                        {
                            span[(int) (n + i)] |= color;
                        }
                    }

                    n += reps;


                    if (n == imageLength)
                    {
                        //index++;
                        break;
                    }

                    if (n > imageLength)
                    {
                        image.Dispose();
                        throw new FileLoadException($"Error image ran off the end: {n - reps}({reps}) of {imageLength}");
                    }
                }

                if (n != imageLength)
                {
                    image.Dispose();
                    throw new FileLoadException($"Error image ended short: {n} of {imageLength}");
                }

                return image;
            }

            public unsafe byte[] EncodePW0(Mat image)
            {
                List<byte> rawData = new List<byte>();
                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

                int lastColor = -1;
                int reps = 0;

                void PutReps()
                {
                    while (reps > 0)
                    {
                        int done = reps;

                        if (lastColor == 0 || lastColor == 0xf)
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
        }

        #endregion

        #region LayerDefinition
        public class LayerDefinition
        {
            public const string SectionMark = "LAYERDEF";

            [Ignore] public Section Section { get; set; } = new Section(SectionMark);

            [FieldOrder(0)] public uint LayersCount { get; set; }

            [Ignore] public LayerData[] Layers;

            public LayerDefinition()
            {
                Section = new Section(SectionMark, this);
            }

            public LayerDefinition(uint layersCount) : this()
            {
                LayersCount = layersCount;
                Layers = new LayerData[layersCount];
            }

            [Ignore]
            public LayerData this[uint index]
            {
                get => Layers[index];
                set => Layers[index] = value;
            }

            [Ignore]
            public LayerData this[int index]
            {
                get => Layers[index];
                set => Layers[index] = value;
            }

            public void Validate()
            {
                Section.Validate(SectionMark, (uint)(LayersCount * Helpers.Serializer.SizeOf(new LayerData())), this);
            }

            public override string ToString() => $"{nameof(Section)}: {Section}, {nameof(LayersCount)}: {LayersCount}";
        }
        #endregion

        #endregion

        #region Properties

        public FileMark FileMarkSettings { get; protected internal set; } = new FileMark();

        public Header HeaderSettings { get; protected internal set; } = new Header();

        public Preview PreviewSettings { get; protected internal set; } = new Preview();

        public LayerDefinition LayersDefinition { get; private set; } = new LayerDefinition();

        public Dictionary<string, LayerData> LayersHash { get; } = new Dictionary<string, LayerData>();

        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("pws", "Photon Workshop PWS Files"),
            new FileExtension("pw0", "Photon Workshop PW0 Files"),
            new FileExtension("pwmx", "Photon Workshop PWMX Files")
        };

        public override Type[] ConvertToFormats { get; } =
        {
            //typeof(ChituboxZipFile)
            //typeof(PHZFile),
            typeof(PWSFile),
            typeof(CWSFile),
            typeof(UVJFile),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
            PrintParameterModifier.ExposureSeconds,

            //PrintParameterModifier.BottomLayerOffTime,
            PrintParameterModifier.LayerOffTime,
            //PrintParameterModifier.BottomLiftHeight,
            //PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
        };

        public override byte ThumbnailsCount { get; } = 1;

        public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } = {new System.Drawing.Size(224, 168)};

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
            get => 0;
            set { }
        }
        public override float DisplayHeight
        {
            get => 0;
            set { }
        }

        public override byte AntiAliasing => (byte) HeaderSettings.AntiAliasing;

        public override float LayerHeight
        {
            get => HeaderSettings.LayerHeight;
            set
            {
                HeaderSettings.LayerHeight = value;
                RaisePropertyChanged();
            }
        }

        public override uint LayerCount
        {
            set
            {
                LayersDefinition.LayersCount = LayerCount;
                RaisePropertyChanged();
            }
        }

        public override ushort BottomLayerCount
        {
            get => (ushort)HeaderSettings.BottomLayersCount;
            set
            {
                HeaderSettings.BottomLayersCount = value;
                RaisePropertyChanged();
            }
        }

        public override float BottomExposureTime
        {
            get => HeaderSettings.BottomExposureSeconds;
            set
            {
                HeaderSettings.BottomExposureSeconds = value;
                RaisePropertyChanged();
            }
        }

        public override float ExposureTime
        {
            get => HeaderSettings.LayerExposureTime;
            set
            {
                HeaderSettings.LayerExposureTime = value;
                RaisePropertyChanged();
            }
        }

        public override float LayerOffTime
        {
            get => HeaderSettings.LayerOffTime;
            set
            {
                HeaderSettings.LayerOffTime = value;
                RaisePropertyChanged();
            }
        }

        public override float LiftHeight
        {
            get => HeaderSettings.LiftHeight;
            set
            {
                HeaderSettings.LiftHeight = value;
                RaisePropertyChanged();
            }
        }

        public override float LiftSpeed
        {
            get => (float) Math.Round(HeaderSettings.LiftSpeed * 60, 2);
            set
            {
                HeaderSettings.LiftSpeed = (float) Math.Round(value / 60, 2);
                RaisePropertyChanged();
            }
        }

        public override float RetractSpeed
        {
            get => (float)Math.Round(HeaderSettings.RetractSpeed * 60, 2);
            set
            {
                HeaderSettings.RetractSpeed = (float) Math.Round(value / 60, 2);
                RaisePropertyChanged();
            }
        }

        public override float PrintTime => 0;

        public override float UsedMaterial => (float) Math.Round(HeaderSettings.Volume, 2);

        public override float MaterialCost => (float) Math.Round(HeaderSettings.Price, 2);

        public override string MaterialName => null;
        public override string MachineName => LayerFormat == LayerRleFormat.PWS ? "AnyCubic Photon S" : "AnyCubic Photon Zero";
        
        public override object[] Configs => new object[] { FileMarkSettings, HeaderSettings, PreviewSettings, LayersDefinition };

        public LayerRleFormat LayerFormat => FileFullPath.EndsWith(".pws") || FileFullPath.EndsWith($".pws{TemporaryFileAppend}") ? LayerRleFormat.PWS : LayerRleFormat.PW0;

        #endregion

        #region Constructors
        public PWSFile()
        {
        }
        #endregion

        #region Methods
        public override void Clear()
        {
            base.Clear();

            LayersDefinition = null;
        }

        public override void Encode(string fileFullPath, OperationProgress progress = null)
        {
            base.Encode(fileFullPath, progress);
            LayersHash.Clear();

            LayersDefinition = new LayerDefinition(LayerCount);

            uint currentOffset = FileMarkSettings.HeaderAddress = (uint) Helpers.Serializer.SizeOf(FileMarkSettings);
            using (var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write))
            {
                outputFile.Seek((int) currentOffset, SeekOrigin.Begin);
                currentOffset += Helpers.SerializeWriteFileStream(outputFile, HeaderSettings.Section);
                currentOffset += Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

                if (CreatedThumbnailsCount > 0)
                {
                    FileMarkSettings.PreviewAddress = currentOffset;
                    Preview preview = Preview.Encode(Thumbnails[0]);
                    currentOffset += Helpers.SerializeWriteFileStream(outputFile, preview.Section);
                    currentOffset += Helpers.SerializeWriteFileStream(outputFile, preview);
                    currentOffset += outputFile.WriteBytes(preview.Data);
                }

                FileMarkSettings.LayerDefinitionAddress = currentOffset;

                Parallel.For(0, LayerCount, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    LayerData layer = new LayerData(this, (uint) layerIndex);
                    using (var image = this[layerIndex].LayerMat)
                    {
                        layer.Encode(image);
                        LayersDefinition.Layers[layerIndex] = layer;
                    }
                    lock (progress.Mutex)
                    {
                        progress++;
                    }
                });

                LayersDefinition.Section.Length += (uint)Helpers.Serializer.SizeOf(LayersDefinition[0]) * LayerCount;
                currentOffset += Helpers.SerializeWriteFileStream(outputFile, LayersDefinition.Section);
                uint offsetLayerRle = FileMarkSettings.LayerImageAddress = currentOffset + LayersDefinition.Section.Length;

                currentOffset += Helpers.SerializeWriteFileStream(outputFile, LayersDefinition);

                progress.Reset(OperationProgress.StatusWritingFile, LayerCount);

                foreach (var layer in LayersDefinition.Layers)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    string hash = Helpers.ComputeSHA1Hash(layer.EncodedRle);

                    if (LayersHash.TryGetValue(hash, out var layerDataHash))
                    {
                        layer.DataAddress = layerDataHash.DataAddress;
                        layer.DataLength = (uint)layerDataHash.EncodedRle.Length;
                    }
                    else
                    {
                        LayersHash.Add(hash, layer);

                        layer.DataAddress = offsetLayerRle;

                        outputFile.Seek(offsetLayerRle, SeekOrigin.Begin);
                        offsetLayerRle += Helpers.SerializeWriteFileStream(outputFile, layer.EncodedRle);
                    }

                    outputFile.Seek(currentOffset, SeekOrigin.Begin);
                    currentOffset += Helpers.SerializeWriteFileStream(outputFile, layer);

                    progress++;
                }

                // Rewind
                outputFile.Seek(0, SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, FileMarkSettings);
            }

            AfterEncode();
        }

        public override void Decode(string fileFullPath, OperationProgress progress = null)
        {
            base.Decode(fileFullPath, progress);

            using (var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
            {

                //HeaderSettings = Helpers.ByteToType<CbddlpFile.Header>(InputFile);
                //HeaderSettings = Helpers.Serializer.Deserialize<Header>(InputFile.ReadBytes(Helpers.Serializer.SizeOf(typeof(Header))));
                FileMarkSettings = Helpers.Deserialize<FileMark>(inputFile);

                Debug.Write("FileMark -> ");
                Debug.WriteLine(FileMarkSettings);

                if (!FileMarkSettings.Mark.Equals(FileMark.SectionMarkFile))
                {
                    throw new FileLoadException(
                        $"Invalid Filemark {FileMarkSettings.Mark}, expected {FileMark.SectionMarkFile}", fileFullPath);
                }

                if (FileMarkSettings.Version != 1)
                {
                    throw new FileLoadException($"Invalid Version {FileMarkSettings.Version}, expected 1",
                        fileFullPath);
                }

                FileFullPath = fileFullPath;

                inputFile.Seek(FileMarkSettings.HeaderAddress, SeekOrigin.Begin);
                //Section sectionHeader = Helpers.Deserialize<Section>(inputFile);
                //Debug.Write("SectionHeader -> ");
                //Debug.WriteLine(sectionHeader);

                var section = Helpers.Deserialize<Section>(inputFile);
                HeaderSettings = Helpers.Deserialize<Header>(inputFile);
                HeaderSettings.Section = section;


                Debug.Write("Header -> ");
                Debug.WriteLine(HeaderSettings);

                HeaderSettings.Validate();

                if (FileMarkSettings.PreviewAddress > 0)
                {
                    inputFile.Seek(FileMarkSettings.PreviewAddress, SeekOrigin.Begin);

                    section = Helpers.Deserialize<Section>(inputFile);
                    PreviewSettings = Helpers.Deserialize<Preview>(inputFile);
                    PreviewSettings.Section = section;
                    Debug.Write("Preview -> ");
                    Debug.WriteLine(PreviewSettings);

                    uint datasize = PreviewSettings.Width * PreviewSettings.Height * 2;
                    //PreviewSettings.Validate(datasize);

                    PreviewSettings.Data = new byte[datasize];
                    inputFile.ReadBytes(PreviewSettings.Data);

                    Thumbnails[0] = PreviewSettings.Decode(true);
                }

                inputFile.Seek(FileMarkSettings.LayerDefinitionAddress, SeekOrigin.Begin);

                section = Helpers.Deserialize<Section>(inputFile);
                LayersDefinition = Helpers.Deserialize<LayerDefinition>(inputFile);
                LayersDefinition.Section = section;
                Debug.Write("LayersDefinition -> ");
                Debug.WriteLine(LayersDefinition);

                LayerManager = new LayerManager(LayersDefinition.LayersCount, this);
                LayersDefinition.Layers = new LayerData[LayerCount];


                LayersDefinition.Validate();

                for (int i = 0; i < LayerCount; i++)
                {
                    LayersDefinition[i] = Helpers.Deserialize<LayerData>(inputFile);
                    LayersDefinition[i].Parent = this;
                }

                progress.Reset(OperationProgress.StatusGatherLayers, LayerCount);

                for (int i = 0; i < LayerCount; i++)
                {
                    var layer = LayersDefinition[i];
                    inputFile.Seek(layer.DataAddress, SeekOrigin.Begin);
                    layer.EncodedRle = new byte[layer.DataLength];
                    inputFile.ReadBytes(layer.EncodedRle);

                    progress++;
                    progress.Token.ThrowIfCancellationRequested();
                }

                progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

                Parallel.For(0, LayerCount, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    using (var image = LayersDefinition[(uint) layerIndex].Decode())
                    {
                        this[layerIndex] = new Layer((uint) layerIndex, image)
                        {
                            PositionZ = LayersDefinition[(uint)layerIndex].LayerPositionZ,
                            ExposureTime = LayersDefinition[(uint)layerIndex].LayerExposure
                        };
                    }

                    lock (progress.Mutex)
                    {
                        progress++;
                    }
                });
            }

            progress.Token.ThrowIfCancellationRequested();
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

            using (var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write))
            {
                outputFile.Seek(FileMarkSettings.HeaderAddress+Helpers.Serializer.SizeOf(HeaderSettings.Section), SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);


                outputFile.Seek(FileMarkSettings.LayerDefinitionAddress + Helpers.Serializer.SizeOf(HeaderSettings.Section) + Helpers.Serializer.SizeOf(LayersDefinition), SeekOrigin.Begin);
                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    Helpers.SerializeWriteFileStream(outputFile, LayersDefinition[layerIndex]);
                }
            }

            //Decode(FileFullPath, progress);
        }

        public override bool Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
            if (to == typeof(PWSFile))
            {
                if (Path.GetExtension(FileFullPath).Equals(Path.GetExtension(fileFullPath)))
                {
                    return false;
                }
                PWSFile file = new PWSFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        LayerHeight = LayerHeight,
                        LayerExposureTime = ExposureTime,
                        LiftHeight = LiftHeight,
                        LiftSpeed = LiftSpeed / 60,
                        RetractSpeed = RetractSpeed / 60,
                        LayerOffTime = HeaderSettings.LayerOffTime,
                        BottomLayersCount = BottomLayerCount,
                        BottomExposureSeconds = BottomExposureTime,
                        Price = MaterialCost,
                        Volume = UsedMaterial,
                        Weight = HeaderSettings.Weight,
                        AntiAliasing = ValidateAntiAliasingLevel()
                    }
                };

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }
            /*if (to == typeof(PHZFile))
            {
                PHZFile file = new PHZFile
                {
                    LayerManager = LayerManager
                };


                file.HeaderSettings.Version = 2;
                file.HeaderSettings.BedSizeX = HeaderSettings.BedSizeX;
                file.HeaderSettings.BedSizeY = HeaderSettings.BedSizeY;
                file.HeaderSettings.BedSizeZ = HeaderSettings.BedSizeZ;
                file.HeaderSettings.OverallHeightMilimeter = TotalHeight;
                file.HeaderSettings.BottomExposureSeconds = InitialExposureTime;
                file.HeaderSettings.BottomLayersCount = InitialLayerCount;
                file.HeaderSettings.BottomLightPWM = HeaderSettings.BottomLightPWM;
                file.HeaderSettings.LayerCount = LayerCount;
                file.HeaderSettings.LayerExposureSeconds = LayerExposureTime;
                file.HeaderSettings.LayerHeightMilimeter = LayerHeight;
                file.HeaderSettings.LayerOffTime = HeaderSettings.LayerOffTime;
                file.HeaderSettings.LightPWM = HeaderSettings.LightPWM;
                file.HeaderSettings.PrintTime = HeaderSettings.PrintTime;
                file.HeaderSettings.ProjectorType = HeaderSettings.ProjectorType;
                file.HeaderSettings.ResolutionX = ResolutionX;
                file.HeaderSettings.ResolutionY = ResolutionY;

                file.HeaderSettings.BottomLayerCount = InitialLayerCount;
                file.HeaderSettings.BottomLiftHeight = PrintParametersSettings.BottomLiftHeight;
                file.HeaderSettings.BottomLiftSpeed = PrintParametersSettings.BottomLiftSpeed;
                file.HeaderSettings.BottomLightOffDelay = PrintParametersSettings.BottomLightOffDelay;
                file.HeaderSettings.CostDollars = MaterialCost;
                file.HeaderSettings.LiftHeight = PrintParametersSettings.LiftHeight;
                file.HeaderSettings.LiftingSpeed = PrintParametersSettings.LiftingSpeed;
                file.HeaderSettings.LayerOffTime = HeaderSettings.LayerOffTime;
                file.HeaderSettings.RetractSpeed = PrintParametersSettings.RetractSpeed;
                file.HeaderSettings.VolumeMl = UsedMaterial;
                file.HeaderSettings.WeightG = PrintParametersSettings.WeightG;

                file.HeaderSettings.MachineName = MachineName;
                file.HeaderSettings.MachineNameSize = (uint) MachineName.Length;

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(ZCodexFile))
            {
                TimeSpan ts = new TimeSpan(0, 0, (int)PrintTime);
                ZCodexFile file = new ZCodexFile
                {
                    ResinMetadataSettings = new ZCodexFile.ResinMetadata
                    {
                        MaterialId = 2,
                        Material = MaterialName,
                        AdditionalSupportLayerTime = 0,
                        BottomLayersNumber = InitialLayerCount,
                        BottomLayersTime = (uint)(InitialExposureTime * 1000),
                        LayerTime = (uint)(LayerExposureTime * 1000),
                        DisableSettingsChanges = false,
                        LayerThickness = LayerHeight,
                        PrintTime = (uint)PrintTime,
                        TotalLayersCount = LayerCount,
                        TotalMaterialVolumeUsed = UsedMaterial,
                        TotalMaterialWeightUsed = UsedMaterial,
                    },
                    UserSettings = new ZCodexFile.UserSettingsdata
                    {
                        Printer = MachineName,
                        BottomLayersCount = InitialLayerCount,
                        PrintTime = $"{ts.Hours}h {ts.Minutes}m",
                        LayerExposureTime = (uint)(LayerExposureTime * 1000),
                        BottomLayerExposureTime = (uint)(InitialExposureTime * 1000),
                        MaterialId = 2,
                        LayerThickness = $"{LayerHeight} mm",
                        AntiAliasing = 0,
                        CrossSupportEnabled = 1,
                        ExposureOffTime = (uint) HeaderSettings.LayerOffTime,
                        HollowEnabled = 0,
                        HollowThickness = 0,
                        InfillDensity = 0,
                        IsAdvanced = 0,
                        MaterialType = MaterialName,
                        MaterialVolume = UsedMaterial,
                        MaxLayer = LayerCount - 1,
                        ModelLiftEnabled = 0,
                        ModelLiftHeight = 0,
                        RaftEnabled = 0,
                        RaftHeight = 0,
                        RaftOffset = 0,
                        SupportAdditionalExposureEnabled = 0,
                        SupportAdditionalExposureTime = 0,
                        XCorrection = 0,
                        YCorrection = 0,
                        ZLiftDistance = PrintParametersSettings.LiftHeight,
                        ZLiftFeedRate = PrintParametersSettings.LiftingSpeed,
                        ZLiftRetractRate = PrintParametersSettings.RetractSpeed,
                    },
                    ZCodeMetadataSettings = new ZCodexFile.ZCodeMetadata
                    {
                        PrintTime = (uint)PrintTime,
                        PrinterName = MachineName,
                        Materials = new List<ZCodexFile.ZCodeMetadata.MaterialsData>
                        {
                            new ZCodexFile.ZCodeMetadata.MaterialsData
                            {
                                Name = MaterialName,
                                ExtruderType = "MAIN",
                                Id = 0,
                                Usage = 0,
                                Temperature = 0
                            }
                        },
                    },
                    LayerManager = LayerManager
                };

                float usedMaterial = UsedMaterial / LayerCount;
                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    file.ResinMetadataSettings.Layers.Add(new ZCodexFile.ResinMetadata.LayerData
                    {
                        Layer = layerIndex,
                        UsedMaterialVolume = usedMaterial
                    });
                }

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);
                return true;
            }
            */

            if (to == typeof(CWSFile))
            {
                CWSFile defaultFormat = (CWSFile)FindByType(typeof(CWSFile));
                CWSFile file = new CWSFile { LayerManager = LayerManager };

                //file.SliceSettings.Xppm = file.OutputSettings.PixPermmX = defaultFormat.OutputSettings.
                //file.SliceSettings.Yppm = file.OutputSettings.PixPermmY = (float)Math.Round(ResolutionY / HeaderSettings.BedSizeY, 3);
                file.SliceSettings.Xres = file.OutputSettings.XResolution = (ushort)ResolutionX;
                file.SliceSettings.Yres = file.OutputSettings.YResolution = (ushort)ResolutionY;
                file.SliceSettings.Thickness = file.OutputSettings.LayerThickness = LayerHeight;
                file.SliceSettings.LayersNum = file.OutputSettings.LayersNum = LayerCount;
                file.SliceSettings.HeadLayersNum = file.OutputSettings.NumberBottomLayers = BottomLayerCount;
                file.SliceSettings.LayersExpoMs = file.OutputSettings.LayerTime = (uint)ExposureTime * 1000;
                file.SliceSettings.HeadLayersExpoMs = file.OutputSettings.BottomLayersTime = (uint)BottomExposureTime * 1000;
                file.SliceSettings.WaitBeforeExpoMs = (uint)(HeaderSettings.LayerOffTime * 1000);
                file.SliceSettings.LiftDistance = file.OutputSettings.LiftDistance = LiftHeight;
                file.SliceSettings.LiftUpSpeed = file.OutputSettings.ZLiftFeedRate = LiftSpeed;
                file.SliceSettings.LiftDownSpeed = file.OutputSettings.ZLiftRetractRate = RetractSpeed;
                file.SliceSettings.LiftWhenFinished = defaultFormat.SliceSettings.LiftWhenFinished;

                file.OutputSettings.BlankingLayerTime = (uint)(HeaderSettings.LayerOffTime * 1000);
                //file.OutputSettings.RenderOutlines = false;
                //file.OutputSettings.OutlineWidthInset = 0;
                //file.OutputSettings.OutlineWidthOutset = 0;
                file.OutputSettings.RenderOutlines = false;
                //file.OutputSettings.TiltValue = 0;
                //file.OutputSettings.UseMainliftGCodeTab = false;
                //file.OutputSettings.AntiAliasing = 0;
                //file.OutputSettings.AntiAliasingValue = 0;
                //file.OutputSettings.FlipX = HeaderSettings. != 0;
                //file.OutputSettings.FlipY = file.OutputSettings.FlipX;
                file.OutputSettings.AntiAliasingValue = ValidateAntiAliasingLevel();
                file.OutputSettings.AntiAliasing = file.OutputSettings.AntiAliasingValue > 1;

                file.Printer = MachineName.Contains("Bene4 Mono") ||
                               FileFullPath.Contains("bene4_mono")
                    ? CWSFile.PrinterType.BeneMono : CWSFile.PrinterType.Elfin;

                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(UVJFile))
            {
                UVJFile file = new UVJFile
                {
                    LayerManager = LayerManager,
                    JsonSettings = new UVJFile.Settings
                    {
                        Properties = new UVJFile.Properties
                        {
                            Size = new UVJFile.Size
                            {
                                X = (ushort)ResolutionX,
                                Y = (ushort)ResolutionY,
                                Millimeter = new UVJFile.Millimeter
                                {
                                    //X = HeaderSettings.BedSizeX,
                                    //Y = HeaderSettings.BedSizeY,
                                },
                                LayerHeight = LayerHeight,
                                Layers = LayerCount
                            },
                            Bottom = new UVJFile.Bottom
                            {
                                LiftHeight = HeaderSettings.LiftHeight,
                                LiftSpeed = HeaderSettings.LiftSpeed,
                                LightOnTime = BottomExposureTime,
                                LightOffTime = HeaderSettings.LayerOffTime,
                                //LightPWM = (byte)HeaderSettings.LightPWM,
                                RetractSpeed = HeaderSettings.RetractSpeed,
                                Count = BottomLayerCount
                                //RetractHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.JsonSettings.Properties.Bottom.RetractHeight),
                            },
                            Exposure = new UVJFile.Exposure
                            {
                                LiftHeight = HeaderSettings.LiftHeight,
                                LiftSpeed = HeaderSettings.LiftSpeed,
                                LightOnTime = ExposureTime,
                                LightOffTime = HeaderSettings.LayerOffTime,
                                //LightPWM = (byte)HeaderSettings.LightPWM,
                                RetractSpeed = HeaderSettings.RetractSpeed,
                            },
                            AntiAliasLevel = ValidateAntiAliasingLevel()
                        }
                    }
                };

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            return false;
        }

        #endregion
    }
}
