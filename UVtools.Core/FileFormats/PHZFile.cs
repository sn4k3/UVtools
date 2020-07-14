/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

// https://github.com/cbiffle/catibo/blob/master/doc/cbddlp-ctb.adoc

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class PHZFile : FileFormat
    {
        #region Constants
        private const uint MAGIC_PHZ = 0x9FDA83AE;
        private const ushort REPEATRGB15MASK = 0x20;

        private const ushort RLE16EncodingLimit = 0x1000;
        #endregion

        #region Sub Classes
        #region Header
        public class Header
        {

            /// <summary>
            /// Gets a magic number identifying the file type.
            /// 0x12fd_0019 for cbddlp
            /// 0x12fd_0086 for ctb
            /// 0x9FDA83AE for phz
            /// </summary>
            [FieldOrder(0)] public uint Magic { get; set; } = MAGIC_PHZ;

            /// <summary>
            /// Gets the software version
            /// </summary>
            [FieldOrder(1)] public uint Version { get; set; } = 2;

            /// <summary>
            /// Gets the layer height setting used at slicing, in millimeters. Actual height used by the machine is in the layer table.
            /// </summary>
            [FieldOrder(2)] public float LayerHeightMilimeter { get; set; }

            /// <summary>
            /// Gets the exposure time setting used at slicing, in seconds, for normal (non-bottom) layers, respectively. Actual time used by the machine is in the layer table.
            /// </summary>
            [FieldOrder(3)] public float LayerExposureSeconds { get; set; }

            /// <summary>
            /// Gets the exposure time setting used at slicing, in seconds, for bottom layers. Actual time used by the machine is in the layer table.
            /// </summary>
            [FieldOrder(4)] public float BottomExposureSeconds { get; set; }

            /// <summary>
            /// Gets number of layers configured as "bottom." Note that this field appears in both the file header and ExtConfig..
            /// </summary>
            [FieldOrder(5)] public uint BottomLayersCount { get; set; } = 10;

            /// <summary>
            /// Gets the printer resolution along X axis, in pixels. This information is critical to correctly decoding layer images.
            /// </summary>
            [FieldOrder(6)] public uint ResolutionX { get; set; }

            /// <summary>
            /// Gets the printer resolution along Y axis, in pixels. This information is critical to correctly decoding layer images.
            /// </summary>
            [FieldOrder(7)] public uint ResolutionY { get; set; }

            /// <summary>
            /// Gets the file offsets of ImageHeader records describing the larger preview images.
            /// </summary>
            [FieldOrder(8)] public uint PreviewLargeOffsetAddress { get; set; }

            /// <summary>
            /// Gets the file offset of a table of LayerHeader records giving parameters for each printed layer.
            /// </summary>
            [FieldOrder(9)] public uint LayersDefinitionOffsetAddress { get; set; }

            /// <summary>
            /// Gets the number of records in the layer table for the first level set. In ctb files, that’s equivalent to the total number of records, but records may be multiplied in antialiased cbddlp files.
            /// </summary>
            [FieldOrder(10)] public uint LayerCount { get; set; }

            /// <summary>
            /// Gets the file offsets of ImageHeader records describing the smaller preview images.
            /// </summary>
            [FieldOrder(11)] public uint PreviewSmallOffsetAddress { get; set; }

            /// <summary>
            /// Gets the estimated duration of print, in seconds.
            /// </summary>
            [FieldOrder(12)] public uint PrintTime { get; set; }

            /// <summary>
            /// Gets the records whether this file was generated assuming normal (0) or mirrored (1) image projection. LCD printers are "mirrored" for this purpose.
            /// </summary>
            [FieldOrder(13)] public uint ProjectorType { get; set; }

            /// <summary>
            /// Gets the number of times each layer image is repeated in the file.
            /// This is used to implement antialiasing in cbddlp files. When greater than 1,
            /// the layer table will actually contain layer_table_count * level_set_count entries.
            /// See the section on antialiasing for details.
            /// </summary>
            [FieldOrder(14)] public uint AntiAliasLevel { get; set; } = 1;

            /// <summary>
            /// Gets the PWM duty cycle for the UV illumination source on normal levels, respectively.
            /// This appears to be an 8-bit quantity where 0xFF is fully on and 0x00 is fully off.
            /// </summary>
            [FieldOrder(15)] public ushort LightPWM { get; set; } = 255;

            /// <summary>
            /// Gets the PWM duty cycle for the UV illumination source on bottom levels, respectively.
            /// This appears to be an 8-bit quantity where 0xFF is fully on and 0x00 is fully off.
            /// </summary>
            [FieldOrder(16)] public ushort BottomLightPWM { get; set; } = 255;

            [FieldOrder(17)] public uint Padding1 { get; set; }
            [FieldOrder(18)] public uint Padding2 { get; set; }

            /// <summary>
            /// Gets the height of the model described by this file, in millimeters.
            /// </summary>
            [FieldOrder(19)] public float OverallHeightMilimeter { get; set; }

            /// <summary>
            /// Gets dimensions of the printer’s X output volume, in millimeters.
            /// </summary>
            [FieldOrder(20)]  public float BedSizeX { get; set; }

            /// <summary>
            /// Gets dimensions of the printer’s Y output volume, in millimeters.
            /// </summary>
            [FieldOrder(21)]  public float BedSizeY { get; set; }

            /// <summary>
            /// Gets dimensions of the printer’s Z output volume, in millimeters.
            /// </summary>
            [FieldOrder(22)]  public float BedSizeZ { get; set; }

            /// <summary>
            /// Gets the key used to encrypt layer data, or 0 if encryption is not used.
            /// </summary>
            [FieldOrder(23)] public uint EncryptionKey { get; set; }

            /// <summary>
            /// Gets the light off time setting used at slicing, for bottom layers, in seconds. Actual time used by the machine is in the layer table. Note that light_off_time_s appears in both the file header and ExtConfig.
            /// </summary>
            [FieldOrder(24)] public float BottomLightOffDelay { get; set; } = 1;

            /// <summary>
            /// Gets the light off time setting used at slicing, for normal layers, in seconds. Actual time used by the machine is in the layer table. Note that light_off_time_s appears in both the file header and ExtConfig.
            /// </summary>
            [FieldOrder(25)] public float LayerOffTime     { get; set; } = 1;

            /// <summary>
            /// Gets number of layers configured as "bottom." Note that this field appears in both the file header and ExtConfig.
            /// </summary>
            [FieldOrder(26)] public uint BottomLayerCount { get; set; } = 10;

            [FieldOrder(27)] public uint Padding3 { get; set; }

            /// <summary>
            /// Gets the distance to lift the build platform away from the vat after bottom layers, in millimeters.
            /// </summary>
            [FieldOrder(28)] public float BottomLiftHeight { get; set; } = 5;

            /// <summary>
            /// Gets the speed at which to lift the build platform away from the vat after bottom layers, in millimeters per minute.
            /// </summary>
            [FieldOrder(29)] public float BottomLiftSpeed { get; set; } = 300;

            /// <summary>
            /// Gets the distance to lift the build platform away from the vat after normal layers, in millimeters.
            /// </summary>
            [FieldOrder(30)] public float LiftHeight { get; set; } = 5;

            /// <summary>
            /// Gets the speed at which to lift the build platform away from the vat after normal layers, in millimeters per minute.
            /// </summary>
            [FieldOrder(31)] public float LiftSpeed { get; set; } = 300;

            /// <summary>
            /// Gets the speed to use when the build platform re-approaches the vat after lift, in millimeters per minute.
            /// </summary>
            [FieldOrder(32)] public float RetractSpeed { get; set; } = 300;

            /// <summary>
            /// Gets the estimated required resin, measured in milliliters. The volume number is derived from the model.
            /// </summary>
            [FieldOrder(33)] public float VolumeMl { get; set; }

            /// <summary>
            /// Gets the estimated grams, derived from volume using configured factors for density.
            /// </summary>
            [FieldOrder(34)] public float WeightG { get; set; }

            /// <summary>
            /// Gets the estimated cost based on currency unit the user had configured. Derived from volume using configured factors for density and cost.
            /// </summary>
            [FieldOrder(35)] public float CostDollars { get; set; }

            [FieldOrder(36)] public uint Padding4 { get; set; }

            /// <summary>
            /// Gets the machine name offset to a string naming the machine type, and its length in bytes.
            /// </summary>
            [FieldOrder(37)] public uint MachineNameAddress { get; set; }

            /// <summary>
            /// Gets the machine size in bytes
            /// </summary>
            [FieldOrder(38)] public uint MachineNameSize { get; set; }

            /// <summary>
            /// Gets the machine name. string is not nul-terminated.
            /// The character encoding is currently unknown — all observed files in the wild use 7-bit ASCII characters only.
            /// Note that the machine type here is set in the software profile, and is not the name the user assigned to the machine.
            /// </summary>
            [Ignore] public string MachineName { get; set; }

            [FieldOrder(39)] public uint Padding5 { get; set; }
            [FieldOrder(40)] public uint Padding6 { get; set; }
            [FieldOrder(41)] public uint Padding7 { get; set; }
            [FieldOrder(42)] public uint Padding8 { get; set; }
            [FieldOrder(43)] public uint Padding9 { get; set; }
            [FieldOrder(44)] public uint Padding10 { get; set; }

            /// <summary>
            /// Gets the parameter used to control encryption.
            /// Not totally understood. 0 for cbddlp files, 0xF for ctb files, 0x1c for phz
            /// </summary>
            [FieldOrder(45)] public uint EncryptionMode { get; set; } = 28;

            /// <summary>
            /// Gets a number that increments with time or number of models sliced, or both. Zeroing it in output seems to have no effect. Possibly a user tracking bug.
            /// </summary>
            [FieldOrder(46)] public uint MysteriousId { get; set; }

            /// <summary>
            /// Gets a number that increments with time or number of models sliced, or both. Zeroing it in output seems to have no effect. Possibly a user tracking bug.
            /// </summary>
            [FieldOrder(47)] public uint AntiAliasLevelInfo { get; set; }

            [FieldOrder(48)] public uint SoftwareVersion { get; set; } = 0x01060300;

            [FieldOrder(49)] public uint Padding11 { get; set; }
            [FieldOrder(50)] public uint Padding12 { get; set; }
            [FieldOrder(51)] public uint Padding13 { get; set; }
            [FieldOrder(52)] public uint Padding14 { get; set; }
            [FieldOrder(53)] public uint Padding15 { get; set; }
            [FieldOrder(54)] public uint Padding16{ get; set; }

            public override string ToString()
            {
                return $"{nameof(Magic)}: {Magic}, {nameof(Version)}: {Version}, {nameof(LayerHeightMilimeter)}: {LayerHeightMilimeter}, {nameof(LayerExposureSeconds)}: {LayerExposureSeconds}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(PreviewLargeOffsetAddress)}: {PreviewLargeOffsetAddress}, {nameof(LayersDefinitionOffsetAddress)}: {LayersDefinitionOffsetAddress}, {nameof(LayerCount)}: {LayerCount}, {nameof(PreviewSmallOffsetAddress)}: {PreviewSmallOffsetAddress}, {nameof(PrintTime)}: {PrintTime}, {nameof(ProjectorType)}: {ProjectorType}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(LightPWM)}: {LightPWM}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(OverallHeightMilimeter)}: {OverallHeightMilimeter}, {nameof(BedSizeX)}: {BedSizeX}, {nameof(BedSizeY)}: {BedSizeY}, {nameof(BedSizeZ)}: {BedSizeZ}, {nameof(EncryptionKey)}: {EncryptionKey}, {nameof(BottomLightOffDelay)}: {BottomLightOffDelay}, {nameof(LayerOffTime)}: {LayerOffTime}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(Padding3)}: {Padding3}, {nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(VolumeMl)}: {VolumeMl}, {nameof(WeightG)}: {WeightG}, {nameof(CostDollars)}: {CostDollars}, {nameof(Padding4)}: {Padding4}, {nameof(MachineNameAddress)}: {MachineNameAddress}, {nameof(MachineNameSize)}: {MachineNameSize}, {nameof(MachineName)}: {MachineName}, {nameof(Padding5)}: {Padding5}, {nameof(Padding6)}: {Padding6}, {nameof(Padding7)}: {Padding7}, {nameof(Padding8)}: {Padding8}, {nameof(Padding9)}: {Padding9}, {nameof(Padding10)}: {Padding10}, {nameof(EncryptionMode)}: {EncryptionMode}, {nameof(MysteriousId)}: {MysteriousId}, {nameof(AntiAliasLevelInfo)}: {AntiAliasLevelInfo}, {nameof(SoftwareVersion)}: {SoftwareVersion}, {nameof(Padding11)}: {Padding11}, {nameof(Padding12)}: {Padding12}, {nameof(Padding13)}: {Padding13}, {nameof(Padding14)}: {Padding14}, {nameof(Padding15)}: {Padding15}, {nameof(Padding16)}: {Padding16}";
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

            [FieldOrder(4)] public uint Unknown1    { get; set; }
            [FieldOrder(5)] public uint Unknown2    { get; set; }
            [FieldOrder(6)] public uint Unknown3    { get; set; }
            [FieldOrder(7)] public uint Unknown4    { get; set; }

            public Mat Decode(byte[] rawImageData)
            {
                var image = new Mat(new Size((int)ResolutionX, (int)ResolutionY), DepthType.Cv8U, 3);
                var span = image.GetPixelSpan<byte>();
                

                int pixel = 0;
                for (uint n = 0; n < ImageLength; n++)
                {
                    uint dot = (uint)(rawImageData[n] & 0xFF | ((rawImageData[++n] & 0xFF) << 8));
                    //uint color = ((dot & 0xF800) << 8) | ((dot & 0x07C0) << 5) | ((dot & 0x001F) << 3);
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
                        //span[pixel] = new Rgba32(red, green, blue, byte.MaxValue);
                    }
                }

                return image;
            }

            public static byte[] Encode(Mat image)
            {
                List<byte> rawData = new List<byte>();
                var span = image.GetPixelSpan<byte>();
                ushort color15 = 0;
                uint rep = 0;

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

                for (int pixel = 0; pixel < span.Length; pixel += image.NumberOfChannels)
                {
                    var ncolor15 =
                        (span[pixel] >> 3)
                        | ((span[pixel+1] >> 2) << 5)
                        | ((span[pixel+2] >> 3) << 11);

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
                        color15 = (ushort) ncolor15;
                        rep = 1;
                    }
                }

                RleRGB15();

                return rawData.ToArray();
            }

            public override string ToString()
            {
                return $"{nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(ImageOffset)}: {ImageOffset}, {nameof(ImageLength)}: {ImageLength}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(Unknown4)}: {Unknown4}";
            }
        }

        #endregion

        #region Layer
        public class LayerData
        {
            /// <summary>
            /// Gets the build platform Z position for this layer, measured in millimeters.
            /// </summary>
            [FieldOrder(0)] public float LayerPositionZ      { get; set; }

            /// <summary>
            /// Gets the exposure time for this layer, in seconds.
            /// </summary>
            [FieldOrder(1)] public float LayerExposure       { get; set; }

            /// <summary>
            /// Gets how long to keep the light off after exposing this layer, in seconds.
            /// </summary>
            [FieldOrder(2)] public float LayerOffTimeSeconds { get; set; }

            /// <summary>
            /// Gets the layer image offset to encoded layer data, and its length in bytes.
            /// </summary>
            [FieldOrder(3)] public uint DataAddress          { get; set; }

            /// <summary>
            /// Gets the layer image length in bytes.
            /// </summary>
            [FieldOrder(4)] public uint DataSize             { get; set; }
            [FieldOrder(5)] public uint Unknown1             { get; set; }
            [FieldOrder(6)] public uint Unknown2             { get; set; }
            [FieldOrder(7)] public uint Unknown3             { get; set; }
            [FieldOrder(8)] public uint Unknown4             { get; set; }

            [Ignore] public byte[] EncodedRle { get; set; }

            [Ignore] public PHZFile Parent { get; set; }

            public LayerData()
            {
            }

            public LayerData(PHZFile parent, uint layerIndex)
            {
                Parent = parent;
                LayerPositionZ = parent[layerIndex].PositionZ;
                LayerExposure = parent[layerIndex].ExposureTime;

                LayerOffTimeSeconds = parent.GetInitialLayerValueOrNormal(layerIndex,
                    parent.HeaderSettings.BottomLightOffDelay,
                    parent.HeaderSettings.LayerOffTime);
            }

            public Mat Decode(uint layerIndex, bool consumeData = true)
            {
                var image = new Mat(new Size((int)Parent.ResolutionX, (int)Parent.ResolutionY), DepthType.Cv8U, 1);
                var span = image.GetPixelSpan<byte>();

                if (Parent.HeaderSettings.EncryptionKey > 0)
                {
                    KeyRing kr = new KeyRing(Parent.HeaderSettings.EncryptionKey, layerIndex);
                    EncodedRle = kr.Read(EncodedRle);
                }

                int limit = image.Width * image.Height;
                int index = 0;
                byte lastColor = 0;

                foreach (var code in EncodedRle)
                {
                    if ((code & 0x80) == 0x80)
                    {
                        //lastColor = (byte) (code << 1);
                        // // Convert from 7bpp to 8bpp (extending the last bit)
                        lastColor = (byte)(((code & 0x7f) << 1) | (code & 1));
                        if (lastColor >= 0xfc)
                        {
                            // Make 'white' actually white
                            lastColor = 0xff;

                        }

                        if (index < limit)
                        {
                            span[index] = lastColor;
                        }
                        else
                        {
                            image.Dispose();
                            throw new FileLoadException("Corrupted RLE data.");
                        }

                        index++;
                    }
                    else
                    {
                        for (uint i = 0; i < code; i++)
                        {
                            if (index < limit)
                            {
                                span[index] = lastColor;
                            }
                            else
                            {
                                image.Dispose();
                                throw new FileLoadException("Corrupted RLE data.");
                            }
                            index++;
                        }
                    }
                }

                if (consumeData)
                    EncodedRle = null;

                return image;
            }

            public void Encode(Mat image, uint layerIndex)
            {
                List<byte> rawData = new List<byte>();
                
                //byte color = byte.MaxValue >> 1;
                byte color = byte.MaxValue;
                uint stride = 0;

                void AddRep()
                {
                    rawData.Add((byte)(color | 0x80));
                    stride--;
                    int done = 0;
                    while (done < stride)
                    {
                        int todo = 0x7d;

                        if (stride - done < todo)
                        {
                            todo = (int)(stride - done);
                        }

                        rawData.Add((byte)(todo));

                        done += todo;
                    }
                }

                int halfWidth = image.Width / 2;

                //int pixel = 0;
                for (int y = 0; y < image.Height; y++)
                {
                    var span = image.GetPixelRowSpan<byte>(y);
                    for (int x = 0; x < span.Length; x++)
                    {
                        
                        var grey7 = (byte)((span[x] >> 1) & 0x7f);
                        if (grey7 > 0x7c)
                        {
                            grey7 = 0x7c;
                        }

                        if (color == byte.MaxValue)
                        {
                            color = grey7;
                            stride = 1;
                        }
                        else if (grey7 != color || x == halfWidth)
                        {
                            AddRep();
                            color = grey7;
                            stride = 1;
                        }
                        else
                        {
                            stride++;
                        }
                    }

                    AddRep();
                    color = byte.MaxValue;
                }


                if (Parent.HeaderSettings.EncryptionKey > 0)
                {
                    KeyRing kr = new KeyRing(Parent.HeaderSettings.EncryptionKey, layerIndex);
                    EncodedRle = kr.Read(rawData).ToArray();
                }
                else
                {
                    EncodedRle = rawData.ToArray();
                }

                DataSize = (uint) EncodedRle.Length;
            }

            public override string ToString()
            {
                return $"{nameof(LayerPositionZ)}: {LayerPositionZ}, {nameof(LayerExposure)}: {LayerExposure}, {nameof(LayerOffTimeSeconds)}: {LayerOffTimeSeconds}, {nameof(DataAddress)}: {DataAddress}, {nameof(DataSize)}: {DataSize}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(Unknown4)}: {Unknown4}";
            }
        }
        #endregion

        #region KeyRing

        public class KeyRing
        {
            public uint Init { get; }
            public uint Key { get; private set; }
            public uint Index { get; private set; }

            public KeyRing(uint seed, uint layerIndex)
            {
                seed %= 0x4324;
                Init = seed * 0x34a32231;
                Key = (layerIndex ^ 0x3fad2212) * seed * 0x4910913d;
            }

            public byte Next()
            {
                byte k = (byte)(Key >> (int)(8 * Index));
                Index++;

                if ((Index & 3) == 0)
                {
                    Key += Init;
                    Index = 0;
                }

                return k;
            }

            public List<byte> Read(List<byte> input)
            {
                List<byte> data = new List<byte>(input.Count);
                data.AddRange(input.Select(t => (byte)(t ^ Next())));

                return data;
            }

            public byte[] Read(byte[] input)
            {
                byte[] data = new byte[input.Length];
                for (int i = 0; i < input.Length; i++)
                {
                    data[i] = (byte) (input[i] ^ Next());
                }
                return data;
            }
        }

        #endregion

        #endregion

        #region Properties

        public Header HeaderSettings { get; protected internal set; } = new Header();

        public Preview[] Previews { get; protected internal set; }

        public LayerData[] LayersDefinitions { get; private set; }

        public Dictionary<string, LayerData> LayersHash { get; } = new Dictionary<string, LayerData>();

        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("phz", "Chitubox PHZ Files"),
        };

        public override Type[] ConvertToFormats { get; } =
        {
            typeof(ChituboxFile),
            typeof(ChituboxZipFile),
            typeof(PWSFile),
            typeof(ZCodexFile),
            typeof(CWSFile),
            typeof(UVJFile),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.InitialLayerCount,
            PrintParameterModifier.InitialExposureSeconds,
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

        public override System.Drawing.Size[] ThumbnailsOriginalSize { get; } = {new System.Drawing.Size(400, 300), new System.Drawing.Size(200, 125)};

        public override uint ResolutionX => HeaderSettings.ResolutionX;

        public override uint ResolutionY => HeaderSettings.ResolutionY;
        public override byte AntiAliasing => (byte) HeaderSettings.AntiAliasLevelInfo;

        public override float LayerHeight => HeaderSettings.LayerHeightMilimeter;

        public override ushort InitialLayerCount => (ushort)HeaderSettings.BottomLayersCount;

        public override float InitialExposureTime => HeaderSettings.BottomExposureSeconds;

        public override float LayerExposureTime => HeaderSettings.LayerExposureSeconds;
        public override float LiftHeight => HeaderSettings.LiftHeight;
        public override float LiftSpeed => HeaderSettings.LiftSpeed;
        public override float RetractSpeed => HeaderSettings.RetractSpeed;

        public override float PrintTime => HeaderSettings.PrintTime;

        public override float UsedMaterial => (float) Math.Round(HeaderSettings.VolumeMl, 2);

        public override float MaterialCost => (float) Math.Round(HeaderSettings.CostDollars, 2);

        public override string MaterialName => "Unknown";
        public override string MachineName => HeaderSettings.MachineName;
        
        public override object[] Configs => new object[] { HeaderSettings };

        #endregion

        #region Constructors
        public PHZFile()
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

            LayersDefinitions = null;
        }

        public override void Encode(string fileFullPath, OperationProgress progress = null)
        {
            base.Encode(fileFullPath, progress);
            LayersHash.Clear();

            /*if (HeaderSettings.EncryptionKey == 0)
            {
                Random rnd = new Random();
                HeaderSettings.EncryptionKey = (uint)rnd.Next(short.MaxValue, int.MaxValue);
            }*/


            uint currentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
            LayersDefinitions = new LayerData[HeaderSettings.LayerCount];
            using (var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write))
            {
                outputFile.Seek((int) currentOffset, SeekOrigin.Begin);

                for (byte i = 0; i < ThumbnailsCount; i++)
                {
                    var image = Thumbnails[i];

                    var bytes = Preview.Encode(image);

                    if (bytes.Length == 0) continue;

                    if (i == (byte) FileThumbnailSize.Small)
                    {
                        HeaderSettings.PreviewSmallOffsetAddress = currentOffset;
                    }
                    else
                    {
                        HeaderSettings.PreviewLargeOffsetAddress = currentOffset;
                    }



                    Preview preview = new Preview
                    {
                        ResolutionX = (uint) image.Width,
                        ResolutionY = (uint) image.Height,
                        ImageLength = (uint)bytes.Length,
                    };

                    currentOffset += (uint) Helpers.Serializer.SizeOf(preview);
                    preview.ImageOffset = currentOffset;

                    Helpers.SerializeWriteFileStream(outputFile, preview);

                    currentOffset += (uint)bytes.Length;
                    outputFile.WriteBytes(bytes);
                }

                if (HeaderSettings.MachineNameSize > 0)
                {
                    HeaderSettings.MachineNameAddress = currentOffset;
                    var machineBytes = Encoding.ASCII.GetBytes(HeaderSettings.MachineName);
                    outputFile.Write(machineBytes, 0, machineBytes.Length);
                    currentOffset += (uint)machineBytes.Length;
                }

                Parallel.For(0, LayerCount, /*new ParallelOptions{MaxDegreeOfParallelism = 1},*/ layerIndex =>
                {
                    if(progress.Token.IsCancellationRequested) return;
                    LayerData layer = new LayerData(this, (uint) layerIndex);
                    using (var image = this[layerIndex].LayerMat)
                    {
                        layer.Encode(image, (uint) layerIndex);
                        LayersDefinitions[layerIndex] = layer;
                    }

                    lock (progress.Mutex)
                    {
                        progress++;
                    }
                });

                progress.Reset(OperationProgress.StatusWritingFile, LayerCount);

                HeaderSettings.LayersDefinitionOffsetAddress = currentOffset;
                uint layerDataCurrentOffset = currentOffset + (uint) Helpers.Serializer.SizeOf(LayersDefinitions[0]) * LayerCount;
                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    LayerData layerData = LayersDefinitions[layerIndex];
                    LayerData layerDataHash = null;

                    if (HeaderSettings.EncryptionKey == 0)
                    {
                        string hash = Helpers.ComputeSHA1Hash(layerData.EncodedRle);
                        if (LayersHash.TryGetValue(hash, out layerDataHash))
                        {
                            layerData.DataAddress = layerDataHash.DataAddress;
                            layerData.DataSize = layerDataHash.DataSize;
                        }
                        else
                        {
                            LayersHash.Add(hash, layerData);
                        }
                    }

                    if (ReferenceEquals(layerDataHash, null))
                    {
                        layerData.DataAddress = layerDataCurrentOffset;

                        outputFile.Seek(layerDataCurrentOffset, SeekOrigin.Begin);
                        layerDataCurrentOffset += outputFile.WriteBytes(layerData.EncodedRle);
                    }


                    LayersDefinitions[layerIndex] = layerData;

                    outputFile.Seek(currentOffset, SeekOrigin.Begin);
                    currentOffset += Helpers.SerializeWriteFileStream(outputFile, layerData);
                    progress++;
                }

                outputFile.Seek(0, SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

                Debug.WriteLine("Encode Results:");
                Debug.WriteLine(HeaderSettings);
                Debug.WriteLine(Previews[0]);
                Debug.WriteLine(Previews[1]);
                Debug.WriteLine("-End-");
            }
        }

        public override void Decode(string fileFullPath, OperationProgress progress = null)
        {
            base.Decode(fileFullPath, progress);

            using (var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read))
            {

                //HeaderSettings = Helpers.ByteToType<CbddlpFile.Header>(InputFile);
                //HeaderSettings = Helpers.Serializer.Deserialize<Header>(InputFile.ReadBytes(Helpers.Serializer.SizeOf(typeof(Header))));
                HeaderSettings = Helpers.Deserialize<Header>(inputFile);
                if (HeaderSettings.Magic != MAGIC_PHZ)
                {
                    throw new FileLoadException("Not a valid PHZ file!", fileFullPath);
                }

                HeaderSettings.AntiAliasLevel = 1;

                FileFullPath = fileFullPath;


                progress.Reset(OperationProgress.StatusDecodeThumbnails, ThumbnailsCount);
                Debug.Write("Header -> ");
                Debug.WriteLine(HeaderSettings);

                for (byte i = 0; i < ThumbnailsCount; i++)
                {
                    uint offsetAddress = i == 0
                        ? HeaderSettings.PreviewSmallOffsetAddress
                        : HeaderSettings.PreviewLargeOffsetAddress;
                    if (offsetAddress == 0) continue;

                    inputFile.Seek(offsetAddress, SeekOrigin.Begin);
                    Previews[i] = Helpers.Deserialize<Preview>(inputFile);

                    Debug.Write($"Preview {i} -> ");
                    Debug.WriteLine(Previews[i]);

                    inputFile.Seek(Previews[i].ImageOffset, SeekOrigin.Begin);
                    byte[] rawImageData = new byte[Previews[i].ImageLength];
                    inputFile.Read(rawImageData, 0, (int) Previews[i].ImageLength);

                    Thumbnails[i] = Previews[i].Decode(rawImageData);
                    progress++;
                }

                if (HeaderSettings.MachineNameAddress > 0 && HeaderSettings.MachineNameSize > 0)
                {
                    inputFile.Seek(HeaderSettings.MachineNameAddress, SeekOrigin.Begin);
                    byte[] buffer = new byte[HeaderSettings.MachineNameSize];
                    inputFile.Read(buffer, 0, (int) HeaderSettings.MachineNameSize);
                    HeaderSettings.MachineName = Encoding.ASCII.GetString(buffer);
                }


                LayersDefinitions = new LayerData[HeaderSettings.LayerCount];

                uint layerOffset = HeaderSettings.LayersDefinitionOffsetAddress;

                progress.Reset(OperationProgress.StatusGatherLayers, HeaderSettings.LayerCount);

                for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    inputFile.Seek(layerOffset, SeekOrigin.Begin);
                    LayerData layerData = Helpers.Deserialize<LayerData>(inputFile);
                    layerData.Parent = this;
                    LayersDefinitions[layerIndex] = layerData;

                    layerOffset += (uint) Helpers.Serializer.SizeOf(layerData);
                    Debug.Write($"LAYER {layerIndex} -> ");
                    Debug.WriteLine(layerData);

                    layerData.EncodedRle = new byte[layerData.DataSize];
                    inputFile.Seek(layerData.DataAddress, SeekOrigin.Begin);
                    inputFile.Read(layerData.EncodedRle, 0, (int) layerData.DataSize);

                    progress++;
                    progress.Token.ThrowIfCancellationRequested();
                }

                LayerManager = new LayerManager(HeaderSettings.LayerCount);

                progress.Reset(OperationProgress.StatusDecodeLayers, HeaderSettings.LayerCount);

                Parallel.For(0, LayerCount, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    using (var image = LayersDefinitions[layerIndex].Decode((uint) layerIndex, true))
                    {
                        this[layerIndex] = new Layer((uint) layerIndex, image)
                        {
                            PositionZ = LayersDefinitions[layerIndex].LayerPositionZ,
                            ExposureTime = LayersDefinitions[layerIndex].LayerExposure
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

       public override object GetValueFromPrintParameterModifier(PrintParameterModifier modifier)
        {
            var baseValue = base.GetValueFromPrintParameterModifier(modifier);
            if (!ReferenceEquals(baseValue, null)) return baseValue;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerOffTime)) return HeaderSettings.BottomLightOffDelay;
            if (ReferenceEquals(modifier, PrintParameterModifier.LayerOffTime)) return HeaderSettings.LayerOffTime;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight)) return HeaderSettings.BottomLiftHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed)) return HeaderSettings.BottomLiftSpeed;
            /*if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight)) return PrintParametersSettings.LiftHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed)) return PrintParametersSettings.LiftingSpeed;
            if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed)) return PrintParametersSettings.RetractSpeed;*/

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightPWM)) return HeaderSettings.BottomLightPWM;
            if (ReferenceEquals(modifier, PrintParameterModifier.LightPWM)) return HeaderSettings.LightPWM;



            return null;
        }

        public override bool SetValueFromPrintParameterModifier(PrintParameterModifier modifier, string value)
        {
            void UpdateLayers()
            {
                for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    // Bottom : others
                    this[layerIndex].ExposureTime =
                    LayersDefinitions[layerIndex].LayerExposure = GetInitialLayerValueOrNormal(layerIndex, HeaderSettings.BottomExposureSeconds, HeaderSettings.LayerExposureSeconds);

                    LayersDefinitions[layerIndex].LayerOffTimeSeconds = GetInitialLayerValueOrNormal(layerIndex, HeaderSettings.BottomLightOffDelay, HeaderSettings.LayerOffTime);
                }
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.InitialLayerCount))
            {
                HeaderSettings.BottomLayersCount =
                    HeaderSettings.BottomLayerCount = value.Convert<uint>();
                UpdateLayers();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.InitialExposureSeconds))
            {
                HeaderSettings.BottomExposureSeconds = value.Convert<float>();
                UpdateLayers();
                return true;
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.ExposureSeconds))
            {
                HeaderSettings.LayerExposureSeconds = value.Convert<float>();
                UpdateLayers();
                return true;
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerOffTime))
            {
                HeaderSettings.BottomLightOffDelay = value.Convert<float>();
                UpdateLayers();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LayerOffTime))
            {
                HeaderSettings.LayerOffTime =
                    HeaderSettings.LayerOffTime = value.Convert<float>();
                UpdateLayers();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight))
            {
                HeaderSettings.BottomLiftHeight = value.Convert<float>();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed))
            {
                HeaderSettings.BottomLiftSpeed = value.Convert<float>();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight))
            {
                HeaderSettings.LiftHeight = value.Convert<float>();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed))
            {
                HeaderSettings.LiftSpeed = value.Convert<float>();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed))
            {
                HeaderSettings.RetractSpeed = value.Convert<float>();
                return true;
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLightPWM))
            {
                HeaderSettings.BottomLightPWM = value.Convert<ushort>();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LightPWM))
            {
                HeaderSettings.LightPWM = value.Convert<ushort>();
                return true;
            }

            return false;
        }

        public override void SaveAs(string filePath = null, OperationProgress progress = null)
        {
            if (LayerManager.IsModified)
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
                outputFile.Seek(0, SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

                /*if (HeaderSettings.MachineNameAddress > 0 && HeaderSettings.MachineNameSize > 0)
                {
                    outputFile.Seek(HeaderSettings.MachineNameAddress, SeekOrigin.Begin);
                    byte[] buffer = new byte[HeaderSettings.MachineNameSize];
                    outputFile.Write(Encoding.ASCII.GetBytes(HeaderSettings.MachineName), 0, (int)HeaderSettings.MachineNameSize);
                }*/

                uint layerOffset = HeaderSettings.LayersDefinitionOffsetAddress;
                for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    outputFile.Seek(layerOffset, SeekOrigin.Begin);
                    Helpers.SerializeWriteFileStream(outputFile, LayersDefinitions[layerIndex]);
                    layerOffset += (uint)Helpers.Serializer.SizeOf(LayersDefinitions[layerIndex]);
                }
            }

            //Decode(FileFullPath, progress);
        }

        public override bool Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
            if (to == typeof(ChituboxFile))
            {
                ChituboxFile file = new ChituboxFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings
                        =
                        {
                            Version = 2,
                            BedSizeX = HeaderSettings.BedSizeX,
                            BedSizeY = HeaderSettings.BedSizeY,
                            BedSizeZ = HeaderSettings.BedSizeZ,
                            OverallHeightMilimeter = TotalHeight,
                            BottomExposureSeconds = InitialExposureTime,
                            BottomLayersCount = InitialLayerCount,
                            BottomLightPWM = HeaderSettings.BottomLightPWM,
                            LayerCount = LayerCount,
                            LayerExposureSeconds = LayerExposureTime,
                            LayerHeightMilimeter = LayerHeight,
                            LayerOffTime = HeaderSettings.LayerOffTime,
                            LightPWM = HeaderSettings.LightPWM,
                            PrintTime = HeaderSettings.PrintTime,
                            ProjectorType = HeaderSettings.ProjectorType,
                            ResolutionX = ResolutionX,
                            ResolutionY = ResolutionY,
                            AntiAliasLevel = ValidateAntiAliasingLevel()
                        },
                    PrintParametersSettings =
                    {
                        BottomLayerCount = InitialLayerCount,
                        BottomLiftHeight = HeaderSettings.BottomLiftHeight,
                        BottomLiftSpeed = HeaderSettings.BottomLiftSpeed,
                        BottomLightOffDelay = HeaderSettings.BottomLightOffDelay,
                        CostDollars = MaterialCost,
                        LiftHeight = HeaderSettings.LiftHeight,
                        LiftSpeed = HeaderSettings.LiftSpeed,
                        LightOffDelay = HeaderSettings.LayerOffTime,
                        RetractSpeed = HeaderSettings.RetractSpeed,
                        VolumeMl = UsedMaterial,
                        WeightG = HeaderSettings.WeightG
                    },
                    SlicerInfoSettings = {MachineName = MachineName, MachineNameSize = (uint) MachineName.Length}
                };





                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(ChituboxZipFile))
            {
                ChituboxZipFile file = new ChituboxZipFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        Filename = Path.GetFileName(FileFullPath),

                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        MachineX = HeaderSettings.BedSizeX,
                        MachineY = HeaderSettings.BedSizeY,
                        MachineZ = HeaderSettings.BedSizeZ,
                        MachineType = MachineName,
                        ProjectType = HeaderSettings.ProjectorType == 0 ? "Normal" : "LCD_mirror",

                        Resin = MaterialName,
                        Price = MaterialCost,
                        Weight = HeaderSettings.WeightG,
                        Volume = UsedMaterial,
                        Mirror = (byte)  (HeaderSettings.ProjectorType == 0 ? 0 : 1),


                        BottomLiftHeight = HeaderSettings.BottomLiftHeight,
                        LiftHeight = HeaderSettings.LiftHeight,
                        BottomLiftSpeed = HeaderSettings.BottomLiftSpeed,
                        LiftSpeed = HeaderSettings.LiftSpeed,
                        RetractSpeed = HeaderSettings.RetractSpeed,
                        BottomLayCount = InitialLayerCount,
                        BottomLayerCount = InitialLayerCount,
                        BottomLightOffTime = HeaderSettings.BottomLightOffDelay,
                        LightOffTime = HeaderSettings.LayerOffTime,
                        BottomLayExposureTime = InitialExposureTime,
                        BottomLayerExposureTime = InitialExposureTime,
                        LayerExposureTime = LayerExposureTime,
                        LayerHeight = LayerHeight,
                        LayerCount = LayerCount,
                        AntiAliasing = ValidateAntiAliasingLevel(),
                        BottomLightPWM = (byte) HeaderSettings.BottomLightPWM,
                        LayerLightPWM = (byte) HeaderSettings.LightPWM,

                        EstimatedPrintTime = PrintTime
                    },
                };

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(PWSFile))
            {
                PWSFile file = new PWSFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        LayerHeight = LayerHeight,
                        LayerExposureTime = LayerExposureTime,
                        LiftHeight = LiftHeight,
                        LiftSpeed = LiftSpeed / 60,
                        RetractSpeed = RetractSpeed / 60,
                        LayerOffTime = HeaderSettings.LayerOffTime,
                        BottomLayersCount = InitialLayerCount,
                        BottomExposureSeconds = InitialExposureTime,
                        Price = MaterialCost,
                        Volume = UsedMaterial,
                        Weight = HeaderSettings.WeightG,
                        AntiAliasing = ValidateAntiAliasingLevel()
                    }
                };

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
                        AntiAliasing = (byte) (AntiAliasing > 1 ? 1 : 0),
                        CrossSupportEnabled = 1,
                        ExposureOffTime = (uint)HeaderSettings.LayerOffTime,
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
                        ZLiftDistance = HeaderSettings.LiftHeight,
                        ZLiftFeedRate = HeaderSettings.LiftSpeed,
                        ZLiftRetractRate = HeaderSettings.RetractSpeed,
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

            if (to == typeof(CWSFile))
            {
                CWSFile defaultFormat = (CWSFile)FindByType(typeof(CWSFile));
                CWSFile file = new CWSFile { LayerManager = LayerManager };

                file.SliceSettings.Xppm = file.OutputSettings.PixPermmX = (float)Math.Round(ResolutionX / HeaderSettings.BedSizeX, 3);
                file.SliceSettings.Yppm = file.OutputSettings.PixPermmY = (float)Math.Round(ResolutionY / HeaderSettings.BedSizeY, 3);
                file.SliceSettings.Xres = file.OutputSettings.XResolution = (ushort)ResolutionX;
                file.SliceSettings.Yres = file.OutputSettings.YResolution = (ushort)ResolutionY;
                file.SliceSettings.Thickness = file.OutputSettings.LayerThickness = LayerHeight;
                file.SliceSettings.LayersNum = file.OutputSettings.LayersNum = LayerCount;
                file.SliceSettings.HeadLayersNum = file.OutputSettings.NumberBottomLayers = InitialLayerCount;
                file.SliceSettings.LayersExpoMs = file.OutputSettings.LayerTime = (uint)LayerExposureTime * 1000;
                file.SliceSettings.HeadLayersExpoMs = file.OutputSettings.BottomLayersTime = (uint)InitialExposureTime * 1000;
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
                file.OutputSettings.FlipX = HeaderSettings.ProjectorType != 0;
                file.OutputSettings.FlipY = file.OutputSettings.FlipX;
                file.OutputSettings.AntiAliasingValue = ValidateAntiAliasingLevel();
                file.OutputSettings.AntiAliasing = file.OutputSettings.AntiAliasingValue > 1;

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
                                    X = HeaderSettings.BedSizeX,
                                    Y = HeaderSettings.BedSizeY,
                                },
                                LayerHeight = LayerHeight,
                                Layers = LayerCount
                            },
                            Bottom = new UVJFile.Bottom
                            {
                                LiftHeight = HeaderSettings.BottomLiftHeight,
                                LiftSpeed = HeaderSettings.BottomLiftSpeed,
                                LightOnTime = InitialExposureTime,
                                LightOffTime = HeaderSettings.BottomLightOffDelay,
                                LightPWM = (byte)HeaderSettings.BottomLightPWM,
                                RetractSpeed = HeaderSettings.RetractSpeed,
                                Count = InitialLayerCount
                                //RetractHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.JsonSettings.Properties.Bottom.RetractHeight),
                            },
                            Exposure = new UVJFile.Exposure
                            {
                                LiftHeight = HeaderSettings.LiftHeight,
                                LiftSpeed = HeaderSettings.LiftSpeed,
                                LightOnTime = LayerExposureTime,
                                LightOffTime = HeaderSettings.LayerOffTime,
                                LightPWM = (byte)HeaderSettings.LightPWM,
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
