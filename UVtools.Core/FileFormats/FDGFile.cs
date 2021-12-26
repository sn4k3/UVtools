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
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class FDGFile : FileFormat
    {
        #region Constants
        private const uint MAGIC = 0xBD3C7AC8; // 3174857416
        private const ushort REPEATRGB15MASK = 0x20;

        private const ushort RLE16EncodingLimit = 0x1000;
        #endregion

        #region Sub Classes
        #region Header
        public class Header
        {
            private string _machineName = DefaultMachineName;

            /// <summary>
            /// Gets a magic number identifying the file type.
            /// 0xBD3C7AC8 for fdg
            /// </summary>
            [FieldOrder(0)] public uint Magic { get; set; } = MAGIC;

            /// <summary>
            /// Gets the software version
            /// </summary>
            [FieldOrder(1)] public uint Version { get; set; } = 2;

            /// <summary>
            /// Gets the number of records in the layer table
            /// </summary>
            [FieldOrder(2)] public uint LayerCount { get; set; }

            /// <summary>
            /// Gets number of layers configured as "bottom." Note that this field appears in both the file header and ExtConfig..
            /// </summary>
            [FieldOrder(3)] public uint BottomLayersCount { get; set; } = 10;

            /// <summary>
            /// Gets the records whether this file was generated assuming normal (0) or mirrored (1) image projection. LCD printers are "mirrored" for this purpose.
            /// </summary>
            [FieldOrder(4)] public uint ProjectorType { get; set; }

            [FieldOrder(5)] public uint BottomLayersCount2 { get; set; } = 10; // ???

            /// <summary>
            /// Gets the printer resolution along X axis, in pixels. This information is critical to correctly decoding layer images.
            /// </summary>
            [FieldOrder(6)] public uint ResolutionX { get; set; }

            /// <summary>
            /// Gets the printer resolution along Y axis, in pixels. This information is critical to correctly decoding layer images.
            /// </summary>
            [FieldOrder(7)] public uint ResolutionY { get; set; }

            /// <summary>
            /// Gets the layer height setting used at slicing, in millimeters. Actual height used by the machine is in the layer table.
            /// </summary>
            [FieldOrder(8)] public float LayerHeightMilimeter { get; set; }

            /// <summary>
            /// Gets the exposure time setting used at slicing, in seconds, for normal (non-bottom) layers, respectively. Actual time used by the machine is in the layer table.
            /// </summary>
            [FieldOrder(9)] public float LayerExposureSeconds { get; set; }

            /// <summary>
            /// Gets the exposure time setting used at slicing, in seconds, for bottom layers. Actual time used by the machine is in the layer table.
            /// </summary>
            [FieldOrder(10)] public float BottomExposureSeconds { get; set; }

            /// <summary>
            /// Gets the file offsets of ImageHeader records describing the larger preview images.
            /// </summary>
            [FieldOrder(11)] public uint PreviewLargeOffsetAddress { get; set; }

            /// <summary>
            /// Gets the file offsets of ImageHeader records describing the smaller preview images.
            /// </summary>
            [FieldOrder(12)] public uint PreviewSmallOffsetAddress { get; set; }

            /// <summary>
            /// Gets the file offset of a table of LayerHeader records giving parameters for each printed layer.
            /// </summary>
            [FieldOrder(13)] public uint LayersDefinitionOffsetAddress { get; set; }

            /// <summary>
            /// Gets the estimated duration of print, in seconds.
            /// </summary>
            [FieldOrder(14)] public uint PrintTime { get; set; }

            /// <summary>
            /// ?
            /// </summary>
            [FieldOrder(15)] public uint AntiAliasLevel { get; set; } = 1;

            /// <summary>
            /// Gets the PWM duty cycle for the UV illumination source on normal levels, respectively.
            /// This appears to be an 8-bit quantity where 0xFF is fully on and 0x00 is fully off.
            /// </summary>
            [FieldOrder(16)] public ushort LightPWM { get; set; } = 255;

            /// <summary>
            /// Gets the PWM duty cycle for the UV illumination source on bottom levels, respectively.
            /// This appears to be an 8-bit quantity where 0xFF is fully on and 0x00 is fully off.
            /// </summary>
            [FieldOrder(17)] public ushort BottomLightPWM { get; set; } = 255;

            [FieldOrder(18)] public uint Padding1 { get; set; }
            [FieldOrder(19)] public uint Padding2 { get; set; }

            /// <summary>
            /// Gets the height of the model described by this file, in millimeters.
            /// </summary>
            [FieldOrder(20)] public float OverallHeightMilimeter { get; set; }

            /// <summary>
            /// Gets dimensions of the printer’s X output volume, in millimeters.
            /// </summary>
            [FieldOrder(21)]  public float BedSizeX { get; set; }

            /// <summary>
            /// Gets dimensions of the printer’s Y output volume, in millimeters.
            /// </summary>
            [FieldOrder(22)]  public float BedSizeY { get; set; }

            /// <summary>
            /// Gets dimensions of the printer’s Z output volume, in millimeters.
            /// </summary>
            [FieldOrder(23)]  public float BedSizeZ { get; set; }

            /// <summary>
            /// Gets the key used to encrypt layer data, or 0 if encryption is not used.
            /// </summary>
            [FieldOrder(24)] public uint EncryptionKey { get; set; }

            [FieldOrder(25)] public uint AntiAliasLevelInfo { get; set; }
            [FieldOrder(26)] public uint EncryptionMode { get; set; } = 0x4c;

            /// <summary>
            /// Gets the estimated required resin, measured in milliliters. The volume number is derived from the model.
            /// </summary>
            [FieldOrder(27)] public float VolumeMl { get; set; }

            /// <summary>
            /// Gets the estimated grams, derived from volume using configured factors for density.
            /// </summary>
            [FieldOrder(28)] public float WeightG { get; set; }

            /// <summary>
            /// Gets the estimated cost based on currency unit the user had configured. Derived from volume using configured factors for density and cost.
            /// </summary>
            [FieldOrder(29)] public float CostDollars { get; set; }

            /// <summary>
            /// Gets the machine name offset to a string naming the machine type, and its length in bytes.
            /// </summary>
            [FieldOrder(30)] public uint MachineNameAddress { get; set; }

            /// <summary>
            /// Gets the machine size in bytes
            /// </summary>
            [FieldOrder(31)] public uint MachineNameSize { get; set; } = (uint)(string.IsNullOrEmpty(DefaultMachineName) ? 0 : DefaultMachineName.Length);

            /// <summary>
            /// Gets the machine name. string is not nul-terminated.
            /// The character encoding is currently unknown — all observed files in the wild use 7-bit ASCII characters only.
            /// Note that the machine type here is set in the software profile, and is not the name the user assigned to the machine.
            /// </summary>
            [Ignore]
            public string MachineName
            {
                get => _machineName;
                set
                {
                    if (string.IsNullOrEmpty(value)) value = DefaultMachineName;
                    _machineName = value;
                    MachineNameSize = string.IsNullOrEmpty(_machineName) ? 0 : (uint)_machineName.Length;
                }
            }

            /// <summary>
            /// Gets the light off time setting used at slicing, for bottom layers, in seconds. Actual time used by the machine is in the layer table. Note that light_off_time_s appears in both the file header and ExtConfig.
            /// </summary>
            [FieldOrder(32)] public float BottomLightOffDelay { get; set; } = 1;

            /// <summary>
            /// Gets the light off time setting used at slicing, for normal layers, in seconds. Actual time used by the machine is in the layer table. Note that light_off_time_s appears in both the file header and ExtConfig.
            /// </summary>
            [FieldOrder(33)] public float LightOffDelay     { get; set; } = 1;

            [FieldOrder(34)] public uint Padding4 { get; set; }

            /// <summary>
            /// Gets the distance to lift the build platform away from the vat after bottom layers, in millimeters.
            /// </summary>
            [FieldOrder(35)] public float BottomLiftHeight { get; set; } = 5;

            /// <summary>
            /// Gets the speed at which to lift the build platform away from the vat after bottom layers, in millimeters per minute.
            /// </summary>
            [FieldOrder(36)] public float BottomLiftSpeed { get; set; } = 300;

            /// <summary>
            /// Gets the distance to lift the build platform away from the vat after normal layers, in millimeters.
            /// </summary>
            [FieldOrder(37)] public float LiftHeight { get; set; } = 5;

            /// <summary>
            /// Gets the speed at which to lift the build platform away from the vat after normal layers, in millimeters per minute.
            /// </summary>
            [FieldOrder(38)] public float LiftSpeed { get; set; } = 300;

            /// <summary>
            /// Gets the speed to use when the build platform re-approaches the vat after lift, in millimeters per minute.
            /// </summary>
            [FieldOrder(39)] public float RetractSpeed { get; set; } = 300;

            [FieldOrder(40)] public uint Padding5 { get; set; }
            [FieldOrder(41)] public uint Padding6 { get; set; }
            [FieldOrder(42)] public uint Padding7 { get; set; }
            [FieldOrder(43)] public uint Padding8 { get; set; }
            [FieldOrder(44)] public uint Padding9 { get; set; }
            [FieldOrder(45)] public uint Padding10 { get; set; }
            [FieldOrder(46)] public uint Padding11 { get; set; }

            /// <summary>
            /// Gets the minutes since Jan 1, 1970 UTC
            /// </summary>
            [FieldOrder(47)] public uint Timestamp { get; set; }

            [FieldOrder(48)] public uint SoftwareVersion { get; set; } = 0x01060300;

            [FieldOrder(49)] public uint Padding12 { get; set; }
            [FieldOrder(50)] public uint Padding13 { get; set; }
            [FieldOrder(51)] public uint Padding14 { get; set; }
            [FieldOrder(52)] public uint Padding15 { get; set; }
            [FieldOrder(53)] public uint Padding16 { get; set; }
            [FieldOrder(54)] public uint Padding17 { get; set; }

            public override string ToString()
            {
                return $"{nameof(Magic)}: {Magic}, {nameof(Version)}: {Version}, {nameof(LayerCount)}: {LayerCount}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(ProjectorType)}: {ProjectorType}, {nameof(BottomLayersCount2)}: {BottomLayersCount2}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(LayerHeightMilimeter)}: {LayerHeightMilimeter}, {nameof(LayerExposureSeconds)}: {LayerExposureSeconds}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(PreviewLargeOffsetAddress)}: {PreviewLargeOffsetAddress}, {nameof(PreviewSmallOffsetAddress)}: {PreviewSmallOffsetAddress}, {nameof(LayersDefinitionOffsetAddress)}: {LayersDefinitionOffsetAddress}, {nameof(PrintTime)}: {PrintTime}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(LightPWM)}: {LightPWM}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(OverallHeightMilimeter)}: {OverallHeightMilimeter}, {nameof(BedSizeX)}: {BedSizeX}, {nameof(BedSizeY)}: {BedSizeY}, {nameof(BedSizeZ)}: {BedSizeZ}, {nameof(EncryptionKey)}: {EncryptionKey}, {nameof(AntiAliasLevelInfo)}: {AntiAliasLevelInfo}, {nameof(EncryptionMode)}: {EncryptionMode}, {nameof(VolumeMl)}: {VolumeMl}, {nameof(WeightG)}: {WeightG}, {nameof(CostDollars)}: {CostDollars}, {nameof(MachineNameAddress)}: {MachineNameAddress}, {nameof(MachineNameSize)}: {MachineNameSize}, {nameof(MachineName)}: {MachineName}, {nameof(BottomLightOffDelay)}: {BottomLightOffDelay}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(Padding4)}: {Padding4}, {nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(Padding5)}: {Padding5}, {nameof(Padding6)}: {Padding6}, {nameof(Padding7)}: {Padding7}, {nameof(Padding8)}: {Padding8}, {nameof(Padding9)}: {Padding9}, {nameof(Padding10)}: {Padding10}, {nameof(Padding11)}: {Padding11}, {nameof(Timestamp)}: {Timestamp}, {nameof(SoftwareVersion)}: {SoftwareVersion}, {nameof(Padding12)}: {Padding12}, {nameof(Padding13)}: {Padding13}, {nameof(Padding14)}: {Padding14}, {nameof(Padding15)}: {Padding15}, {nameof(Padding16)}: {Padding16}, {nameof(Padding17)}: {Padding17}";
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

            public unsafe Mat Decode(byte[] rawImageData)
            {
                var image = new Mat(new Size((int)ResolutionX, (int)ResolutionY), DepthType.Cv8U, 3);
                var span = image.GetBytePointer();

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

            public static unsafe byte[] Encode(Mat image)
            {
                List<byte> rawData = new();
                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

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

                for (int pixel = 0; pixel < imageLength; pixel += image.NumberOfChannels)
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
        public class LayerDef
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
            [FieldOrder(2)] public float LightOffDelay { get; set; }

            /// <summary>
            /// Gets the layer image offset to encoded layer data, and its length in bytes.
            /// </summary>
            [FieldOrder(3)] public uint DataAddress          { get; set; }

            /// <summary>
            /// Gets the layer image length in bytes.
            /// </summary>
            [FieldOrder(4)] public uint DataSize             { get; set; }
            [FieldOrder(5)] public uint Unknown1             { get; set; }
            [FieldOrder(6)] public uint Unknown2             { get; set; } = 84;
            [FieldOrder(7)] public uint Unknown3             { get; set; }
            [FieldOrder(8)] public uint Unknown4             { get; set; }

            [Ignore] public byte[] EncodedRle { get; set; }

            [Ignore] public FDGFile Parent { get; set; }

            public LayerDef()
            {
            }

            public LayerDef(FDGFile parent, Layer layer)
            {
                Parent = parent;
                SetFrom(layer);
            }

            public void SetFrom(Layer layer)
            {
                LayerPositionZ = layer.PositionZ;
                LayerExposure = layer.ExposureTime;
                LightOffDelay = layer.LightOffDelay;
            }

            public void CopyTo(Layer layer)
            {
                layer.PositionZ = LayerPositionZ;
                layer.ExposureTime = LayerExposure;
                layer.LightOffDelay = LightOffDelay;
            }

            public unsafe Mat Decode(uint layerIndex, bool consumeData = true)
            {
                var image = EmguExtensions.InitMat(Parent.Resolution);
                var span = image.GetBytePointer();

                if (Parent.HeaderSettings.EncryptionKey > 0)
                {
                    LayerRleCryptBuffer(Parent.HeaderSettings.EncryptionKey, layerIndex, EncodedRle);
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

            public void Encode(Mat mat, uint layerIndex)
            {
                List<byte> rawData = new();
                
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

                int halfWidth = mat.Width / 2;

                //int pixel = 0;
                for (int y = 0; y < mat.Height; y++)
                {
                    var span = mat.GetRowSpan<byte>(y);
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
                    EncodedRle = LayerRleCrypt(Parent.HeaderSettings.EncryptionKey, layerIndex, rawData);
                }
                else
                {
                    EncodedRle = rawData.ToArray();
                }

                DataSize = (uint) EncodedRle.Length;
            }

            public override string ToString()
            {
                return $"{nameof(LayerPositionZ)}: {LayerPositionZ}, {nameof(LayerExposure)}: {LayerExposure}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(DataAddress)}: {DataAddress}, {nameof(DataSize)}: {DataSize}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(Unknown4)}: {Unknown4}";
            }
        }
        #endregion

        #endregion

        #region Properties

        public Header HeaderSettings { get; protected internal set; } = new Header();

        public Preview[] Previews { get; protected internal set; }

        public LayerDef[] LayersDefinitions { get; private set; }

        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new(typeof(FDGFile), "fdg", "Voxelab FDG"),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureTime,
            PrintParameterModifier.ExposureTime,

            PrintParameterModifier.BottomLightOffDelay,
            PrintParameterModifier.LightOffDelay,

            PrintParameterModifier.BottomLiftHeight,
            PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,

            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,
        };

        public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
            PrintParameterModifier.LightOffDelay,
            PrintParameterModifier.ExposureTime,
        };

        public override Size[] ThumbnailsOriginalSize { get; } =
        {
            new(400, 300),
            new(200, 125)
        };

        public override uint[] AvailableVersions { get; } = { 2 };

        public override uint DefaultVersion => 2;

        public override uint Version
        {
            get => HeaderSettings.Version;
            set
            {
                base.Version = value;
                HeaderSettings.Version = (ushort)base.Version;
            }
        }

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
            get => HeaderSettings.BedSizeX;
            set
            {
                HeaderSettings.BedSizeX = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }


        public override float DisplayHeight
        {
            get => HeaderSettings.BedSizeY;
            set
            {
                HeaderSettings.BedSizeY = (float)Math.Round(value, 2);
                RaisePropertyChanged();
            }
        }

        public override float MachineZ
        {
            get => HeaderSettings.BedSizeZ > 0 ? HeaderSettings.BedSizeZ : base.MachineZ;
            set => base.MachineZ = HeaderSettings.BedSizeZ = (float)Math.Round(value, 2);
        }

        public override Enumerations.FlipDirection DisplayMirror
        {
            get => HeaderSettings.ProjectorType == 0 ? Enumerations.FlipDirection.None : Enumerations.FlipDirection.Horizontally;
            set
            {
                HeaderSettings.ProjectorType = value == Enumerations.FlipDirection.None ? 0u : 1;
                RaisePropertyChanged();
            }
        }

        public override byte AntiAliasing
        {
            get => (byte) HeaderSettings.AntiAliasLevelInfo;
            set => base.AntiAliasing = (byte)(HeaderSettings.AntiAliasLevelInfo = value.Clamp(1, 16));
        }

        public override float LayerHeight
        {
            get => HeaderSettings.LayerHeightMilimeter;
            set
            {
                HeaderSettings.LayerHeightMilimeter = Layer.RoundHeight(value);
                RaisePropertyChanged();
            }
        }

        public override float PrintHeight
        {
            get => HeaderSettings.OverallHeightMilimeter;
            set => base.PrintHeight = HeaderSettings.OverallHeightMilimeter = base.PrintHeight;
        }

        public override uint LayerCount
        {
            get => base.LayerCount;
            set => base.LayerCount = HeaderSettings.LayerCount = base.LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => (ushort) HeaderSettings.BottomLayersCount;
            set => base.BottomLayerCount = (ushort) (HeaderSettings.BottomLayersCount2 = HeaderSettings.BottomLayersCount = value);
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
            set => base.BottomExposureTime = HeaderSettings.BottomExposureSeconds = value;
        }

        public override float ExposureTime
        {
            get => HeaderSettings.LayerExposureSeconds;
            set => base.ExposureTime = HeaderSettings.LayerExposureSeconds = (float)Math.Round(value, 2);
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

        public override float BottomRetractSpeed => RetractSpeed;

        public override float RetractSpeed
        {
            get => HeaderSettings.RetractSpeed;
            set => base.RetractSpeed = HeaderSettings.RetractSpeed = (float)Math.Round(value, 2);
        }

        public override byte BottomLightPWM
        {
            get => (byte) HeaderSettings.BottomLightPWM;
            set => base.BottomLightPWM = (byte) (HeaderSettings.BottomLightPWM = value);
        }

        public override byte LightPWM
        {
            get => (byte) HeaderSettings.BottomLightPWM;
            set => base.LightPWM = (byte) (HeaderSettings.BottomLightPWM = value);
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
            get => (float)Math.Round(HeaderSettings.WeightG, 3);
            set => base.MaterialGrams = HeaderSettings.WeightG = (float)Math.Round(value, 3);
        }

        public override float MaterialCost
        {
            get => (float) Math.Round(HeaderSettings.CostDollars, 3);
            set => base.MaterialCost = HeaderSettings.CostDollars = (float)Math.Round(value, 3);
        }

        public override string MachineName
        {
            get => HeaderSettings.MachineName;
            set => base.MachineName = HeaderSettings.MachineName = value;
        }

        public override object[] Configs => new object[] { HeaderSettings };

        #endregion

        #region Constructors
        public FDGFile()
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

        protected override void EncodeInternally(OperationProgress progress)
        {
            /*if (HeaderSettings.EncryptionKey == 0)
            {
                Random rnd = new Random();
                HeaderSettings.EncryptionKey = (uint)rnd.Next(short.MaxValue, int.MaxValue);
            }*/

            using var outputFile = new FileStream(FileFullPath, FileMode.Create, FileAccess.Write);
            outputFile.Seek(Helpers.Serializer.SizeOf(HeaderSettings), SeekOrigin.Begin);

            for (byte i = 0; i < ThumbnailsCount; i++)
            {
                var image = Thumbnails[i];

                var bytes = Preview.Encode(image);

                if (bytes.Length == 0) continue;

                if (i == (byte) FileThumbnailSize.Small)
                {
                    HeaderSettings.PreviewSmallOffsetAddress = (uint)outputFile.Position;
                }
                else
                {
                    HeaderSettings.PreviewLargeOffsetAddress = (uint)outputFile.Position;
                }
                    
                Preview preview = new()
                {
                    ResolutionX = (uint) image.Width,
                    ResolutionY = (uint) image.Height,
                    ImageLength = (uint)bytes.Length,
                };

                preview.ImageOffset = (uint)(outputFile.Position + Helpers.Serializer.SizeOf(preview));

                Helpers.SerializeWriteFileStream(outputFile, preview);

                outputFile.WriteBytes(bytes);
            }

            if (HeaderSettings.MachineNameSize > 0)
            {
                HeaderSettings.MachineNameAddress = (uint)outputFile.Position;
                var machineBytes = Encoding.ASCII.GetBytes(HeaderSettings.MachineName);
                outputFile.Write(machineBytes, 0, machineBytes.Length);
            }

            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
            var layersHash = new Dictionary<string, LayerDef>();
            LayersDefinitions = new LayerDef[HeaderSettings.LayerCount];
            HeaderSettings.LayersDefinitionOffsetAddress = (uint)outputFile.Position;
            uint layerDefCurrentOffset = HeaderSettings.LayersDefinitionOffsetAddress;
            uint layerDataCurrentOffset = HeaderSettings.LayersDefinitionOffsetAddress + (uint)Helpers.Serializer.SizeOf(new LayerDef()) * LayerCount;

            foreach (var batch in BatchLayersIndexes())
            {
                Parallel.ForEach(batch, CoreSettings.ParallelOptions, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    using (var mat = this[layerIndex].LayerMat)
                    {
                        LayersDefinitions[layerIndex] = new LayerDef(this, this[layerIndex]);
                        LayersDefinitions[layerIndex].Encode(mat, (uint)layerIndex);
                    }
                    progress.LockAndIncrement();
                });

                foreach (var layerIndex in batch)
                {
                    progress.Token.ThrowIfCancellationRequested();

                    var layerDef = LayersDefinitions[layerIndex];
                    LayerDef layerDefHash = null;

                    if (HeaderSettings.EncryptionKey == 0)
                    {
                        string hash = CryptExtensions.ComputeSHA1Hash(layerDef.EncodedRle);
                        if (layersHash.TryGetValue(hash, out layerDefHash))
                        {
                            layerDef.DataAddress = layerDefHash.DataAddress;
                            layerDef.DataSize = layerDefHash.DataSize;
                        }
                        else
                        {
                            layersHash.Add(hash, layerDef);
                        }
                    }

                    if (layerDefHash is null)
                    {
                        layerDef.DataAddress = layerDataCurrentOffset;

                        outputFile.Seek(layerDataCurrentOffset, SeekOrigin.Begin);
                        layerDataCurrentOffset += outputFile.WriteBytes(layerDef.EncodedRle);
                    }


                    outputFile.Seek(layerDefCurrentOffset, SeekOrigin.Begin);
                    layerDefCurrentOffset += Helpers.SerializeWriteFileStream(outputFile, layerDef);

                    layerDef.EncodedRle = null; // Free
                }
            }


            outputFile.Seek(0, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine(Previews[0]);
            Debug.WriteLine(Previews[1]);
            Debug.WriteLine("-End-");
        }

        protected override void DecodeInternally(OperationProgress progress)
        {
            using var inputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Read);
            //HeaderSettings = Helpers.ByteToType<CbddlpFile.Header>(InputFile);
            //HeaderSettings = Helpers.Serializer.Deserialize<Header>(InputFile.ReadBytes(Helpers.Serializer.SizeOf(typeof(Header))));
            HeaderSettings = Helpers.Deserialize<Header>(inputFile);
            if (HeaderSettings.Magic != MAGIC)
            {
                throw new FileLoadException("Not a valid FDG file!", FileFullPath);
            }

            HeaderSettings.AntiAliasLevel = 1;

            progress.Reset(OperationProgress.StatusDecodePreviews, ThumbnailsCount);
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
                var buffer = new byte[HeaderSettings.MachineNameSize];
                inputFile.Read(buffer, 0, (int) HeaderSettings.MachineNameSize);
                HeaderSettings.MachineName = Encoding.ASCII.GetString(buffer);
            }

            LayerManager.Init(HeaderSettings.LayerCount, DecodeType == FileDecodeType.Partial);
            LayersDefinitions = new LayerDef[HeaderSettings.LayerCount];
            
            progress.Reset(OperationProgress.StatusDecodeLayers, HeaderSettings.LayerCount);
            foreach (var batch in BatchLayersIndexes())
            {
                foreach (var layerIndex in batch)
                {
                    progress.Token.ThrowIfCancellationRequested();

                    var layerDef = Helpers.Deserialize<LayerDef>(inputFile);
                    layerDef.Parent = this;
                    LayersDefinitions[layerIndex] = layerDef;
                    
                    Debug.Write($"LAYER {layerIndex} -> ");
                    Debug.WriteLine(layerDef);

                    if (DecodeType == FileDecodeType.Full)
                    {
                        inputFile.SeekDoWorkAndRewind(layerDef.DataAddress,
                            () => { layerDef.EncodedRle = inputFile.ReadBytes(layerDef.DataSize); });
                    }
                }

                if (DecodeType == FileDecodeType.Full)
                {
                    Parallel.ForEach(batch, CoreSettings.ParallelOptions, layerIndex =>
                    {
                        if (progress.Token.IsCancellationRequested) return;
                        if (DecodeType == FileDecodeType.Full)
                        {
                            using var mat = LayersDefinitions[layerIndex].Decode((uint)layerIndex);
                            this[layerIndex] = new Layer((uint)layerIndex, mat, this);
                        }

                        progress.LockAndIncrement();
                    });
                }
            }

            for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                LayersDefinitions[layerIndex].CopyTo(this[layerIndex]);
            }
        }

        protected override void PartialSaveInternally(OperationProgress progress)
        {
            using var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write);
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
                LayersDefinitions[layerIndex].SetFrom(this[layerIndex]);
                outputFile.Seek(layerOffset, SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, LayersDefinitions[layerIndex]);
                layerOffset += (uint)Helpers.Serializer.SizeOf(LayersDefinitions[layerIndex]);
            }
        }

        #endregion

        #region Static Methods
        public static byte[] LayerRleCrypt(uint seed, uint layerIndex, IEnumerable<byte> input)
        {
            var result = input.ToArray();
            LayerRleCryptBuffer(seed, layerIndex, result);
            return result;
        }

        public static void LayerRleCryptBuffer(uint seed, uint layerIndex, byte[] input)
        {
            if (seed == 0) return;

            var init = (seed - 0x1dcb76c3) ^ 0x257e2431;
            var key = init * 0x82391efd * (layerIndex ^ 0x110bdacd);

            int index = 0;
            for (int i = 0; i < input.Length; i++)
            {
                var k = (byte)(key >> 8 * index);

                index++;

                if ((index & 3) == 0)
                {
                    key += init;
                    index = 0;
                }

                input[i] = (byte)(input[i] ^ k);
            }
        }
        #endregion
    }
}
