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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinarySerialization;
using PrusaSL1Reader.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    public class PHZFile : FileFormat
    {
        #region Constants
        private const uint MAGIC_PHZ = 0x9FDA83AE;
        private const int SPECIAL_BIT = 1 << 1;
        private const int SPECIAL_BIT_MASK = ~SPECIAL_BIT;
        private const ushort REPEATRGB15MASK = 0x20;

        private const byte RLE8EncodingLimit = 125;
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
            [FieldOrder(31)] public float LiftingSpeed { get; set; } = 300;

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
            /// Not totally understood. 0 for cbddlp files, 0xF for ctb files.
            /// </summary>
            [FieldOrder(45)] public uint EncryptionMode { get; set; } = 28;

            /// <summary>
            /// Gets a number that increments with time or number of models sliced, or both. Zeroing it in output seems to have no effect. Possibly a user tracking bug.
            /// </summary>
            [FieldOrder(46)] public uint MysteriousId { get; set; }

            /// <summary>
            /// Gets a number that increments with time or number of models sliced, or both. Zeroing it in output seems to have no effect. Possibly a user tracking bug.
            /// </summary>
            [FieldOrder(47)] public uint AntiAliasLevel2 { get; set; }

            [FieldOrder(48)] public uint SoftwareVersion { get; set; } = 0x01060300;

            [FieldOrder(49)] public uint Padding11 { get; set; }
            [FieldOrder(50)] public uint Padding12 { get; set; }
            [FieldOrder(51)] public uint Padding13 { get; set; }
            [FieldOrder(52)] public uint Padding14 { get; set; }
            [FieldOrder(53)] public uint Padding15 { get; set; }
            [FieldOrder(54)] public uint Padding16{ get; set; }

            public override string ToString()
            {
                return $"{nameof(Magic)}: {Magic}, {nameof(Version)}: {Version}, {nameof(LayerHeightMilimeter)}: {LayerHeightMilimeter}, {nameof(LayerExposureSeconds)}: {LayerExposureSeconds}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(PreviewLargeOffsetAddress)}: {PreviewLargeOffsetAddress}, {nameof(LayersDefinitionOffsetAddress)}: {LayersDefinitionOffsetAddress}, {nameof(LayerCount)}: {LayerCount}, {nameof(PreviewSmallOffsetAddress)}: {PreviewSmallOffsetAddress}, {nameof(PrintTime)}: {PrintTime}, {nameof(ProjectorType)}: {ProjectorType}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(LightPWM)}: {LightPWM}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(OverallHeightMilimeter)}: {OverallHeightMilimeter}, {nameof(BedSizeX)}: {BedSizeX}, {nameof(BedSizeY)}: {BedSizeY}, {nameof(BedSizeZ)}: {BedSizeZ}, {nameof(EncryptionKey)}: {EncryptionKey}, {nameof(BottomLightOffDelay)}: {BottomLightOffDelay}, {nameof(LayerOffTime)}: {LayerOffTime}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(Padding3)}: {Padding3}, {nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftingSpeed)}: {LiftingSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(VolumeMl)}: {VolumeMl}, {nameof(WeightG)}: {WeightG}, {nameof(CostDollars)}: {CostDollars}, {nameof(Padding4)}: {Padding4}, {nameof(MachineNameAddress)}: {MachineNameAddress}, {nameof(MachineNameSize)}: {MachineNameSize}, {nameof(MachineName)}: {MachineName}, {nameof(Padding5)}: {Padding5}, {nameof(Padding6)}: {Padding6}, {nameof(Padding7)}: {Padding7}, {nameof(Padding8)}: {Padding8}, {nameof(Padding9)}: {Padding9}, {nameof(Padding10)}: {Padding10}, {nameof(EncryptionMode)}: {EncryptionMode}, {nameof(MysteriousId)}: {MysteriousId}, {nameof(AntiAliasLevel2)}: {AntiAliasLevel2}, {nameof(SoftwareVersion)}: {SoftwareVersion}, {nameof(Padding11)}: {Padding11}, {nameof(Padding12)}: {Padding12}, {nameof(Padding13)}: {Padding13}, {nameof(Padding14)}: {Padding14}, {nameof(Padding15)}: {Padding15}, {nameof(Padding16)}: {Padding16}";
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

            public override string ToString()
            {
                return $"{nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(ImageOffset)}: {ImageOffset}, {nameof(ImageLength)}: {ImageLength}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(Unknown4)}: {Unknown4}";
            }
        }

        #endregion

        #region Layer
        public class Layer
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

            public override string ToString()
            {
                return $"{nameof(LayerPositionZ)}: {LayerPositionZ}, {nameof(LayerExposure)}: {LayerExposure}, {nameof(LayerOffTimeSeconds)}: {LayerOffTimeSeconds}, {nameof(DataAddress)}: {DataAddress}, {nameof(DataSize)}: {DataSize}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(Unknown4)}: {Unknown4}";
            }
        }
        #endregion

        #region KeyRing

        public class KeyRing
        {
            public ulong Init { get; }
            public ulong Key { get; private set; }
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

        public Layer[,] LayersDefinitions { get; private set; }

        public Dictionary<string, Layer> LayersHash { get; } = new Dictionary<string, Layer>();

        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("phz", "PHZ Files"),
        };

        public override Type[] ConvertToFormats { get; } =
        {
            typeof(ChituboxFile),
            typeof(ZCodexFile),
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

        public override float LayerHeight => HeaderSettings.LayerHeightMilimeter;

        public override uint LayerCount => HeaderSettings.LayerCount;

        public override ushort InitialLayerCount => (ushort)HeaderSettings.BottomLayersCount;

        public override float InitialExposureTime => HeaderSettings.BottomExposureSeconds;

        public override float LayerExposureTime => HeaderSettings.LayerExposureSeconds;
        public override float LiftHeight => HeaderSettings.LiftHeight;
        public override float LiftSpeed => HeaderSettings.LiftingSpeed;
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

        public override void Encode(string fileFullPath)
        {
            base.Encode(fileFullPath);
            LayersHash.Clear();

            /*if (HeaderSettings.EncryptionKey == 0)
            {
                Random rnd = new Random();
                HeaderSettings.EncryptionKey = (uint)rnd.Next(short.MaxValue, int.MaxValue);
            }*/


            uint currentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
            LayersDefinitions = new Layer[HeaderSettings.LayerCount, HeaderSettings.AntiAliasLevel];
            using (var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write))
            {

                outputFile.Seek((int) currentOffset, SeekOrigin.Begin);

                List<byte> rawData = new List<byte>();
                ushort color15 = 0;
                uint rep = 0;

                void rleRGB15()
                {
                    switch (rep)
                    {
                        case 0:
                            return;
                        case 1:
                            rawData.Add((byte) (color15 & ~REPEATRGB15MASK));
                            rawData.Add((byte) ((color15 & ~REPEATRGB15MASK) >> 8));
                            break;
                        case 2:
                            for (int i = 0; i < 2; i++)
                            {
                                rawData.Add((byte) (color15 & ~REPEATRGB15MASK));
                                rawData.Add((byte) ((color15 & ~REPEATRGB15MASK) >> 8));
                            }

                            break;
                        default:
                            rawData.Add((byte) (color15 | REPEATRGB15MASK));
                            rawData.Add((byte) ((color15 | REPEATRGB15MASK) >> 8));
                            rawData.Add((byte) ((rep - 1) | 0x3000));
                            rawData.Add((byte) (((rep - 1) | 0x3000) >> 8));
                            break;
                    }
                }

                for (byte i = 0; i < ThumbnailsCount; i++)
                {
                    var image = Thumbnails[i];

                    color15 = 0;
                    rep = 0;
                    rawData.Clear();


                    for (int y = 0; y < image.Height; y++)
                    {
                        Span<Rgba32> pixelRowSpan = image.GetPixelRowSpan(y);
                        for (int x = 0; x < image.Width; x++)
                        {
                            var ncolor15 =
                                (pixelRowSpan[x].B >> 3)
                                | ((pixelRowSpan[x].G >> 2) << 5)
                                | ((pixelRowSpan[x].R >> 3) << 11);

                            if (ncolor15 == color15)
                            {
                                rep++;
                                if (rep == RLE16EncodingLimit)
                                {
                                    rleRGB15();
                                    rep = 0;
                                }
                            }
                            else
                            {
                                rleRGB15();
                                color15 = (ushort) ncolor15;
                                rep = 1;
                            }
                        }
                    }

                    rleRGB15();

                    if (rawData.Count == 0) continue;

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
                        ImageLength = (uint) rawData.Count,
                    };

                    currentOffset += (uint) Helpers.Serializer.SizeOf(preview);
                    preview.ImageOffset = currentOffset;

                    Helpers.SerializeWriteFileStream(outputFile, preview);

                    currentOffset += (uint) rawData.Count;
                    outputFile.Write(rawData.ToArray(), 0, rawData.Count);
                }



                HeaderSettings.MachineNameSize = string.IsNullOrWhiteSpace(HeaderSettings.MachineName) ? 0 : (uint)HeaderSettings.MachineName.Length;
                if (HeaderSettings.MachineNameSize > 0)
                {
                    HeaderSettings.MachineNameAddress = currentOffset;
                    var machineBytes = Encoding.ASCII.GetBytes(HeaderSettings.MachineName);
                    outputFile.Write(machineBytes, 0, machineBytes.Length);
                    currentOffset += (uint)machineBytes.Length;
                }

                HeaderSettings.LayersDefinitionOffsetAddress = currentOffset;
                uint layerDataCurrentOffset = currentOffset + (uint) Helpers.Serializer.SizeOf(new Layer()) * HeaderSettings.LayerCount * HeaderSettings.AntiAliasLevel;
                for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                {
                    Layer layer = new Layer();
                    Layer layerHash = null;
                    var image = this[layerIndex].Image;
                    rawData = EncodePhzImage(image, layerIndex);

                    var byteArr = rawData.ToArray();

                    if (HeaderSettings.EncryptionKey == 0)
                    {
                        string hash = Helpers.ComputeSHA1Hash(byteArr);
                        if (!LayersHash.TryGetValue(hash, out layerHash))
                        {
                            LayersHash.Add(hash, layer);
                        }
                    }

                    //layer.DataAddress = CurrentOffset + (uint)Helpers.Serializer.SizeOf(layer);
                    layer.DataAddress = layerHash?.DataAddress ?? layerDataCurrentOffset;
                    layer.DataSize = layerHash?.DataSize ?? (uint)byteArr.Length;
                    layer.LayerPositionZ = layerIndex * HeaderSettings.LayerHeightMilimeter;
                    layer.LayerOffTimeSeconds = layerIndex < HeaderSettings.BottomLayersCount ? HeaderSettings.BottomLightOffDelay : HeaderSettings.LayerOffTime;
                    layer.LayerExposure = layerIndex < HeaderSettings.BottomLayersCount ? HeaderSettings.BottomExposureSeconds : HeaderSettings.LayerExposureSeconds;
                    LayersDefinitions[layerIndex, 0] = layer;

                    currentOffset += Helpers.SerializeWriteFileStream(outputFile, layer);

                    if (!ReferenceEquals(layerHash, null)) continue;

                    outputFile.Seek(layerDataCurrentOffset, SeekOrigin.Begin);
                    layerDataCurrentOffset += Helpers.WriteFileStream(outputFile, byteArr);
                    outputFile.Seek(currentOffset, SeekOrigin.Begin);
                }

                outputFile.Seek(0, SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

                outputFile.Close();
                outputFile.Dispose();

                Debug.WriteLine("Encode Results:");
                Debug.WriteLine(HeaderSettings);
                Debug.WriteLine(Previews[0]);
                Debug.WriteLine(Previews[1]);
                Debug.WriteLine("-End-");
            }
        }

        private List<byte> EncodePhzImage(Image<L8> image, uint layerIndex)
        {
            List<byte> rawData = new List<byte>();
            //byte color = byte.MaxValue >> 1;
            byte color = byte.MaxValue;
            uint stride = 0;

            void AddRep()
            {
                rawData.Add((byte)(color|0x80));
                stride--;
                int done = 0;
                while(done < stride)
                {
                    int todo = 0x7d;

                    if (stride - done < todo) {
                        todo = (int)(stride - done);
                    }

                    rawData.Add((byte)(todo));

                    done += todo;
                }
            }

            int halfWidth = image.Width / 2;
            
            for (int y = 0; y < image.Height; y++)
            {
                var pixelRowSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    var grey7 = (byte)((pixelRowSpan[x].PackedValue >> 1) & 0x7f);

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


            if (HeaderSettings.EncryptionKey > 0)
            {
                List<byte> encodedData = new List<byte>();
                KeyRing kr = new KeyRing(HeaderSettings.EncryptionKey, layerIndex);
                return kr.Read(rawData);
            }

            return rawData;
        }

        public override void Decode(string fileFullPath)
        {
            base.Decode(fileFullPath);

            var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);

            //HeaderSettings = Helpers.ByteToType<CbddlpFile.Header>(InputFile);
            //HeaderSettings = Helpers.Serializer.Deserialize<Header>(InputFile.ReadBytes(Helpers.Serializer.SizeOf(typeof(Header))));
            HeaderSettings = Helpers.Deserialize<Header>(inputFile);
            if (HeaderSettings.Magic != MAGIC_PHZ)
            {
                throw new FileLoadException("Not a valid PHZ file!", fileFullPath);
            }

            HeaderSettings.AntiAliasLevel = 1;

            FileFullPath = fileFullPath;



            Debug.Write("Header -> ");
            Debug.WriteLine(HeaderSettings);

            for (byte i = 0; i < ThumbnailsCount; i++)
            {
                uint offsetAddress = i == 0 ? HeaderSettings.PreviewSmallOffsetAddress : HeaderSettings.PreviewLargeOffsetAddress;
                if (offsetAddress == 0) continue;

                inputFile.Seek(offsetAddress, SeekOrigin.Begin);
                Previews[i] = Helpers.Deserialize<Preview>(inputFile);

                Debug.Write($"Preview {i} -> ");
                Debug.WriteLine(Previews[i]);

                Thumbnails[i] = new Image<Rgba32>((int)Previews[i].ResolutionX, (int)Previews[i].ResolutionY);

                inputFile.Seek(Previews[i].ImageOffset, SeekOrigin.Begin);
                byte[] rawImageData = new byte[Previews[i].ImageLength];
                inputFile.Read(rawImageData, 0, (int)Previews[i].ImageLength);
                int x = 0;
                int y = 0;
                for (int n = 0; n < Previews[i].ImageLength; n++)
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
                        Thumbnails[i][x, y] = new Rgba32(red, green, blue, 255);
                        x++;

                        if (x == Previews[i].ResolutionX)
                        {
                            x = 0;
                            y++;
                        }
                    }
                }
            }

            if (HeaderSettings.MachineNameAddress > 0 && HeaderSettings.MachineNameSize > 0)
            {
                inputFile.Seek(HeaderSettings.MachineNameAddress, SeekOrigin.Begin);
                byte[] buffer = new byte[HeaderSettings.MachineNameSize];
                inputFile.Read(buffer, 0, (int) HeaderSettings.MachineNameSize);
                HeaderSettings.MachineName = Encoding.ASCII.GetString(buffer);
            }


            LayersDefinitions = new Layer[HeaderSettings.LayerCount, HeaderSettings.AntiAliasLevel];

            uint layerOffset = HeaderSettings.LayersDefinitionOffsetAddress;


            for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
            {
                Debug.WriteLine($"-Image GROUP {aaIndex}-");
                for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    inputFile.Seek(layerOffset, SeekOrigin.Begin);
                    Layer layer = Helpers.Deserialize<Layer>(inputFile);
                    LayersDefinitions[layerIndex, aaIndex] = layer;

                    layerOffset += (uint)Helpers.Serializer.SizeOf(layer);
                    Debug.Write($"LAYER {layerIndex} -> ");
                    Debug.WriteLine(layer);

                    layer.EncodedRle = new byte[layer.DataSize];
                    inputFile.Seek(layer.DataAddress, SeekOrigin.Begin);
                    inputFile.Read(layer.EncodedRle, 0, (int)layer.DataSize);
                }
            }

            LayerManager = new LayerManager(LayerCount);

            Parallel.For(0, LayerCount, layerIndex => {
                    var image = DecodePhzImage((uint) layerIndex);
                    this[layerIndex] = new LayerManager.Layer((uint) layerIndex, image);
            });
        }

       private Image<L8> DecodePhzImage(uint layerIndex)
        {
            Image<L8> image = new Image<L8>((int)HeaderSettings.ResolutionX, (int)HeaderSettings.ResolutionY);
            var rawImageData = LayersDefinitions[layerIndex, 0].EncodedRle;

            if (HeaderSettings.EncryptionKey > 0)
            {
                KeyRing kr = new KeyRing(HeaderSettings.EncryptionKey, layerIndex);
                rawImageData = kr.Read(rawImageData);
            }

            int limit = image.Width * image.Height;
            int index = 0;
            byte lastColor = 0;

            image.TryGetSinglePixelSpan(out var span);

            foreach (var code in rawImageData)
            {
                if ((code & 0x80) == 0x80)
                {
                    //lastColor = (byte) (code << 1);
                    // // Convert from 7bpp to 8bpp (extending the last bit)
                    lastColor = (byte) (((code & 0x7f) << 1) | (code & 1));
                    if (index < limit)
                    {
                        span[index].PackedValue = lastColor;
                    }
                    else
                    {
                        Debug.WriteLine("Corrupted RLE data.");
                    }

                    index++;
                }
                else
                {
                    for (uint i = 0; i < code; i++)
                    {
                        if (index < limit)
                        {
                            span[index].PackedValue = lastColor;
                        }
                        else
                        {
                            Debug.WriteLine("Corrupted RLE data.");
                        }
                        index++;
                    }
                }
            }

            return image;
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
                for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
                {
                    for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                    {
                        // Bottom : others
                        LayersDefinitions[layerIndex, aaIndex].LayerExposure = layerIndex < HeaderSettings.BottomLayersCount ? HeaderSettings.BottomExposureSeconds : HeaderSettings.LayerExposureSeconds;
                        LayersDefinitions[layerIndex, aaIndex].LayerOffTimeSeconds = layerIndex < HeaderSettings.BottomLayersCount ? HeaderSettings.BottomLightOffDelay : HeaderSettings.LayerOffTime;
                    }
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
                HeaderSettings.LiftingSpeed = value.Convert<float>();
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

        public override void SaveAs(string filePath = null)
        {
            if (LayerManager.IsModified)
            {
                if (!string.IsNullOrEmpty(filePath))
                {
                    FileFullPath = filePath;
                }
                Encode(FileFullPath);
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
                for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
                {
                    for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                    {
                        outputFile.Seek(layerOffset, SeekOrigin.Begin);
                        Helpers.SerializeWriteFileStream(outputFile, LayersDefinitions[layerIndex, aaIndex]);
                        layerOffset += (uint)Helpers.Serializer.SizeOf(LayersDefinitions[layerIndex, aaIndex]);
                    }
                }
                outputFile.Close();
            }

            //Decode(FileFullPath);
        }

        public override bool Convert(Type to, string fileFullPath)
        {
            if (to == typeof(ChituboxFile))
            {
                ChituboxFile file = new ChituboxFile
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

                file.PrintParametersSettings.BottomLayerCount = InitialLayerCount;
                file.PrintParametersSettings.BottomLiftHeight = HeaderSettings.BottomLiftHeight;
                file.PrintParametersSettings.BottomLiftSpeed = HeaderSettings.BottomLiftSpeed;
                file.PrintParametersSettings.BottomLightOffDelay = HeaderSettings.BottomLightOffDelay;
                file.PrintParametersSettings.CostDollars = MaterialCost;
                file.PrintParametersSettings.LiftHeight = HeaderSettings.LiftHeight;
                file.PrintParametersSettings.LiftingSpeed = HeaderSettings.LiftingSpeed;
                file.PrintParametersSettings.LightOffDelay = HeaderSettings.LayerOffTime;
                file.PrintParametersSettings.RetractSpeed = HeaderSettings.RetractSpeed;
                file.PrintParametersSettings.VolumeMl = UsedMaterial;
                file.PrintParametersSettings.WeightG = HeaderSettings.WeightG;

                file.SlicerInfoSettings.MachineName = MachineName;
                file.SlicerInfoSettings.MachineNameSize = (uint)MachineName.Length;

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath);

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
                        ZLiftFeedRate = HeaderSettings.LiftingSpeed,
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
                file.Encode(fileFullPath);
                return true;
            }

            return false;
        }
        #endregion
    }
}
