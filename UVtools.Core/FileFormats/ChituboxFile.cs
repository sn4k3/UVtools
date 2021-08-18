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
using MoreLinq;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class ChituboxFile : FileFormat
    {

        #region Constants
        public const byte USED_VERSION = 3; // 318570521

        public const uint MAGIC_CBDDLP = 0x12FD0019; // 318570521
        public const uint MAGIC_CTB = 0x12FD0086; // 318570630
        public const uint MAGIC_CTBv4 = 0x12FD0106; // 318570758
        public const uint MAGIC_CTB_ENCRYPTED = 0x12FD0107; // 318570759
        public const ushort REPEATRGB15MASK = 0x20;

        public const byte RLE8EncodingLimit = 0x7d; // 125;
        public const ushort RLE16EncodingLimit = 0xFFF;

        public const uint ENCRYPTYION_MODE_CBDDLP = 0x8;  // 0 or 8
        public const uint ENCRYPTYION_MODE_CTBv2 = 0xF; // 15 for ctb v2 files
        public const uint ENCRYPTYION_MODE_CTBv3 = 536870927; // 536870927 for ctb v3 files (This allow per layer settings, while 15 don't)
        public const uint ENCRYPTYION_MODE_CTBv4 = 1073741839; // 1073741839 for ctb v4 files (This allow per layer settings, while 15 don't)

        private const string CTBv4_DISCLAIMER = "Layout and record format for the ctb and cbddlp file types are the copyrighted programs or codes of CBD Technology (China) Inc..The Customer or User shall not in any manner reproduce, distribute, modify, decompile, disassemble, decrypt, extract, reverse engineer, lease, assign, or sublicense the said programs or codes.";
        private const ushort CTBv4_DISCLAIMER_SIZE = 320;
        private const ushort CTBv4_RESERVED_SIZE = 384;

        #endregion

        #region Sub Classes
        #region Header
        public class Header
        {

            /// <summary>
            /// Gets a magic number identifying the file type.
            /// 0x12fd_0019 for cbddlp
            /// 0x12fd_0086 for ctb
            /// </summary>
            [FieldOrder(0)]  public uint Magic     { get; set; }

            /// <summary>
            /// Gets the software version
            /// </summary>
            [FieldOrder(1)] public uint Version { get; set; } = USED_VERSION;

            /// <summary>
            /// Gets dimensions of the printer’s X output volume, in millimeters.
            /// </summary>
            [FieldOrder(2)]  public float BedSizeX { get; set; }

            /// <summary>
            /// Gets dimensions of the printer’s Y output volume, in millimeters.
            /// </summary>
            [FieldOrder(3)]  public float BedSizeY { get; set; }

            /// <summary>
            /// Gets dimensions of the printer’s Z output volume, in millimeters.
            /// </summary>
            [FieldOrder(4)]  public float BedSizeZ { get; set; }

            [FieldOrder(5)]  public uint Unknown1  { get; set; }
            [FieldOrder(6)]  public uint Unknown2  { get; set; }

            /// <summary>
            /// Gets the height of the model described by this file, in millimeters.
            /// </summary>
            [FieldOrder(7)]  public float TotalHeightMilimeter { get; set; }

            /// <summary>
            /// Gets the layer height setting used at slicing, in millimeters. Actual height used by the machine is in the layer table.
            /// </summary>
            [FieldOrder(8)]  public float LayerHeightMilimeter  { get; set; }

            /// <summary>
            /// Gets the exposure time setting used at slicing, in seconds, for normal (non-bottom) layers, respectively. Actual time used by the machine is in the layer table.
            /// </summary>
            [FieldOrder(9)]  public float LayerExposureSeconds  { get; set; }

            /// <summary>
            /// Gets the exposure time setting used at slicing, in seconds, for bottom layers. Actual time used by the machine is in the layer table.
            /// </summary>
            [FieldOrder(10)] public float BottomExposureSeconds { get; set; }

            /// <summary>
            /// Gets the light off time setting used at slicing, for normal layers, in seconds. Actual time used by the machine is in the layer table. Note that light_off_time_s appears in both the file header and ExtConfig.
            /// </summary>
            [FieldOrder(11)] public float LightOffDelay     { get; set; } = 1;

            /// <summary>
            /// Gets number of layers configured as "bottom." Note that this field appears in both the file header and ExtConfig..
            /// </summary>
            [FieldOrder(12)] public uint BottomLayersCount { get; set; } = 10;

            /// <summary>
            /// Gets the printer resolution along X axis, in pixels. This information is critical to correctly decoding layer images.
            /// </summary>
            [FieldOrder(13)] public uint ResolutionX       { get; set; }

            /// <summary>
            /// Gets the printer resolution along Y axis, in pixels. This information is critical to correctly decoding layer images.
            /// </summary>
            [FieldOrder(14)] public uint ResolutionY       { get; set; }

            /// <summary>
            /// Gets the file offsets of ImageHeader records describing the larger preview images.
            /// </summary>
            [FieldOrder(15)] public uint PreviewLargeOffsetAddress { get; set; }

            /// <summary>
            /// Gets the file offset of a table of LayerHeader records giving parameters for each printed layer.
            /// </summary>
            [FieldOrder(16)] public uint LayersDefinitionOffsetAddress { get; set; }

            /// <summary>
            /// Gets the number of records in the layer table for the first level set. In ctb files, that’s equivalent to the total number of records, but records may be multiplied in antialiased cbddlp files.
            /// </summary>
            [FieldOrder(17)] public uint LayerCount { get; set; }

            /// <summary>
            /// Gets the file offsets of ImageHeader records describing the smaller preview images.
            /// </summary>
            [FieldOrder(18)] public uint PreviewSmallOffsetAddress { get; set; }

            /// <summary>
            /// Gets the estimated duration of print, in seconds.
            /// </summary>
            [FieldOrder(19)] public uint PrintTime { get; set; }

            /// <summary>
            /// Gets the records whether this file was generated assuming normal (0) or mirrored (1) image projection. LCD printers are "mirrored" for this purpose.
            /// </summary>
            [FieldOrder(20)] public uint ProjectorType { get; set; }

            /// <summary>
            /// Gets the print parameters table offset
            /// </summary>
            [FieldOrder(21)] public uint PrintParametersOffsetAddress { get; set; }

            /// <summary>
            /// Gets the print parameters table size in bytes.
            /// </summary>
            [FieldOrder(22)] public uint PrintParametersSize { get; set; }

            /// <summary>
            /// Gets the number of times each layer image is repeated in the file.
            /// This is used to implement antialiasing in cbddlp files. When greater than 1,
            /// the layer table will actually contain layer_table_count * level_set_count entries.
            /// See the section on antialiasing for details.
            /// 1 for ctb
            /// </summary>
            [FieldOrder(23)] public uint AntiAliasLevel { get; set; } = 1;

            /// <summary>
            /// Gets the PWM duty cycle for the UV illumination source on normal levels, respectively.
            /// This appears to be an 8-bit quantity where 0xFF is fully on and 0x00 is fully off.
            /// </summary>
            [FieldOrder(24)] public ushort LightPWM { get; set; } = 255;

            /// <summary>
            /// Gets the PWM duty cycle for the UV illumination source on bottom levels, respectively.
            /// This appears to be an 8-bit quantity where 0xFF is fully on and 0x00 is fully off.
            /// </summary>
            [FieldOrder(25)] public ushort BottomLightPWM { get; set; } = 255;

            /// <summary>
            /// Gets the key used to encrypt layer data, or 0 if encryption is not used.
            /// </summary>
            [FieldOrder(26)] public uint EncryptionKey { get; set; }

            /// <summary>
            /// Gets the slicer tablet offset 
            /// </summary>
            [FieldOrder(27)] public uint SlicerOffset { get; set; }

            /// <summary>
            /// Gets the slicer table size in bytes
            /// </summary>
            [FieldOrder(28)] public uint SlicerSize { get; set; }

            public override string ToString()
            {
                return $"{nameof(Magic)}: {Magic}, {nameof(Version)}: {Version}, {nameof(BedSizeX)}: {BedSizeX}, {nameof(BedSizeY)}: {BedSizeY}, {nameof(BedSizeZ)}: {BedSizeZ}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(TotalHeightMilimeter)}: {TotalHeightMilimeter}, {nameof(LayerHeightMilimeter)}: {LayerHeightMilimeter}, {nameof(LayerExposureSeconds)}: {LayerExposureSeconds}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(PreviewLargeOffsetAddress)}: {PreviewLargeOffsetAddress}, {nameof(LayersDefinitionOffsetAddress)}: {LayersDefinitionOffsetAddress}, {nameof(LayerCount)}: {LayerCount}, {nameof(PreviewSmallOffsetAddress)}: {PreviewSmallOffsetAddress}, {nameof(PrintTime)}: {PrintTime}, {nameof(ProjectorType)}: {ProjectorType}, {nameof(PrintParametersOffsetAddress)}: {PrintParametersOffsetAddress}, {nameof(PrintParametersSize)}: {PrintParametersSize}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(LightPWM)}: {LightPWM}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(EncryptionKey)}: {EncryptionKey}, {nameof(SlicerOffset)}: {SlicerOffset}, {nameof(SlicerSize)}: {SlicerSize}";
            }
        }
        #endregion

        #region PrintParameters
        public class PrintParameters
        {
            /// <summary>
            /// Gets the distance to lift the build platform away from the vat after bottom layers, in millimeters.
            /// </summary>
            [FieldOrder(0)] public float BottomLiftHeight { get; set; } = 5;

            /// <summary>
            /// Gets the speed at which to lift the build platform away from the vat after bottom layers, in millimeters per minute.
            /// </summary>
            [FieldOrder(1)]  public float BottomLiftSpeed     { get; set; } = 300;

            /// <summary>
            /// Gets the distance to lift the build platform away from the vat after normal layers, in millimeters.
            /// </summary>
            [FieldOrder(2)]  public float LiftHeight          { get; set; } = 5;

            /// <summary>
            /// Gets the speed at which to lift the build platform away from the vat after normal layers, in millimeters per minute.
            /// </summary>
            [FieldOrder(3)]  public float LiftSpeed        { get; set; } = 300;

            /// <summary>
            /// Gets the speed to use when the build platform re-approaches the vat after lift, in millimeters per minute.
            /// </summary>
            [FieldOrder(4)]  public float RetractSpeed        { get; set; } = 300;

            /// <summary>
            /// Gets the estimated required resin, measured in milliliters. The volume number is derived from the model.
            /// </summary>
            [FieldOrder(5)]  public float VolumeMl            { get; set; }

            /// <summary>
            /// Gets the estimated grams, derived from volume using configured factors for density.
            /// </summary>
            [FieldOrder(6)]  public float WeightG             { get; set; }

            /// <summary>
            /// Gets the estimated cost based on currency unit the user had configured. Derived from volume using configured factors for density and cost.
            /// </summary>
            [FieldOrder(7)]  public float CostDollars         { get; set; }

            /// <summary>
            /// Gets the light off time setting used at slicing, for bottom layers, in seconds. Actual time used by the machine is in the layer table. Note that light_off_time_s appears in both the file header and ExtConfig.
            /// </summary>
            [FieldOrder(8)]  public float BottomLightOffDelay { get; set; } = 1;

            /// <summary>
            /// Gets the light off time setting used at slicing, for normal layers, in seconds. Actual time used by the machine is in the layer table. Note that light_off_time_s appears in both the file header and ExtConfig.
            /// </summary>
            [FieldOrder(9)]  public float LightOffDelay       { get; set; } = 1;

            /// <summary>
            /// Gets number of layers configured as "bottom." Note that this field appears in both the file header and ExtConfig.
            /// </summary>
            [FieldOrder(10)] public uint BottomLayerCount     { get; set; } = 10;
            [FieldOrder(11)] public uint Padding1             { get; set; }
            [FieldOrder(12)] public uint Padding2             { get; set; }
            [FieldOrder(13)] public uint Padding3             { get; set; }
            [FieldOrder(14)] public uint Padding4             { get; set; }

            public override string ToString()
            {
                return $"{nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(VolumeMl)}: {VolumeMl}, {nameof(WeightG)}: {WeightG}, {nameof(CostDollars)}: {CostDollars}, {nameof(BottomLightOffDelay)}: {BottomLightOffDelay}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(Padding3)}: {Padding3}, {nameof(Padding4)}: {Padding4}";
            }
        }
        #endregion

        #region SlicerInfo

        public class SlicerInfo
        {
            private string _machineName;
            [FieldOrder(0)] public float BottomLiftHeight2 { get; set; }
            [FieldOrder(1)] public float BottomLiftSpeed2    { get; set; }
            [FieldOrder(2)] public float LiftHeight2         { get; set; }
            [FieldOrder(3)] public float LiftSpeed2          { get; set; }
            [FieldOrder(4)] public float RetractHeight2      { get; set; }
            [FieldOrder(5)] public float RetractSpeed2       { get; set; }
            [FieldOrder(6)] public float RestTimeAfterLift    { get; set; }

            /// <summary>
            /// Gets the machine name offset to a string naming the machine type, and its length in bytes.
            /// </summary>
            [FieldOrder(7)] public uint MachineNameAddress { get; set; }

            /// <summary>
            /// Gets the machine size in bytes
            /// </summary>
            [FieldOrder(8)] public uint MachineNameSize    { get; set; }

            /// <summary>
            /// Gets the parameter used to control encryption.
            /// Not totally understood. 0/8 for cbddlp files, 0xF (15) for ctb files, 0x2000000F (536870927) for v3 ctb and 1073741839 for v4 ctb files to allow per layer parameters
            /// </summary>
            [FieldOrder(9)] public uint EncryptionMode     { get; set; } = ENCRYPTYION_MODE_CTBv3;

            /// <summary>
            /// Gets a number that increments with time or number of models sliced, or both. Zeroing it in output seems to have no effect. Possibly a user tracking bug.
            /// </summary>
            [FieldOrder(10)] public uint MysteriousId      { get; set; } = 305419896; // v3 = 305419896 | v4 = 27087675

            /// <summary>
            /// Gets the user-selected antialiasing level. For cbddlp files this will match the level_set_count. For ctb files, this number is essentially arbitrary.
            /// </summary>
            [FieldOrder(11)] public uint AntiAliasLevel { get; set; } = 1;

            /// <summary>
            /// Gets a version of software that generated this file, encoded with major, minor, and patch release in bytes starting from the MSB down.
            /// (No provision is made to name the software being used, so this assumes that only one software package can generate the files.
            /// Probably best to hardcode it at 0x01060300.)
            /// </summary>
            [FieldOrder(12)] public uint SoftwareVersion { get; set; } = 0x01060300; // ctb v3 = 17171200 | ctb v4 = 16777216
            [FieldOrder(13)] public float RestTimeAfterRetract { get; set; }
            [FieldOrder(14)] public float RestTimeAfterLift2   { get; set; }
            [FieldOrder(15)] public uint TransitionLayerCount { get; set; } // CTB not all printers
            [FieldOrder(16)] public uint PrintParametersV4Address { get; set; } // V4 Only
            [FieldOrder(17)] public uint Padding2         { get; set; }
            [FieldOrder(18)] public uint Padding3         { get; set; }

            /// <summary>
            /// Gets the machine name. string is not nul-terminated.
            /// The character encoding is currently unknown — all observed files in the wild use 7-bit ASCII characters only.
            /// Note that the machine type here is set in the software profile, and is not the name the user assigned to the machine.
            /// </summary>
            [FieldOrder(19)]
            [FieldLength(nameof(MachineNameSize))]
            public string MachineName
            {
                get => _machineName;
                set
                {
                    _machineName = value;
                    MachineNameSize = string.IsNullOrEmpty(_machineName) ? 0 : (uint)_machineName.Length;
                }
                
            }

            public override string ToString()
            {
                return $"{nameof(BottomLiftHeight2)}: {BottomLiftHeight2}, {nameof(BottomLiftSpeed2)}: {BottomLiftSpeed2}, {nameof(LiftHeight2)}: {LiftHeight2}, {nameof(LiftSpeed2)}: {LiftSpeed2}, {nameof(RetractHeight2)}: {RetractHeight2}, {nameof(RetractSpeed2)}: {RetractSpeed2}, {nameof(RestTimeAfterLift)}: {RestTimeAfterLift}, {nameof(MachineNameAddress)}: {MachineNameAddress}, {nameof(MachineNameSize)}: {MachineNameSize}, {nameof(EncryptionMode)}: {EncryptionMode}, {nameof(MysteriousId)}: {MysteriousId}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(SoftwareVersion)}: {SoftwareVersion}, {nameof(RestTimeAfterRetract)}: {RestTimeAfterRetract}, {nameof(RestTimeAfterLift2)}: {RestTimeAfterLift2}, {nameof(TransitionLayerCount)}: {TransitionLayerCount}, {nameof(PrintParametersV4Address)}: {PrintParametersV4Address}, {nameof(Padding2)}: {Padding2}, {nameof(Padding3)}: {Padding3}, {nameof(MachineName)}: {MachineName}";
            }
        }

        #endregion

        #region PrintParametersV4
        public sealed class PrintParametersV4
        {
            /*[FieldOrder(0)]
            [FieldLength(nameof(DisclaimerLength))] 
            public string Disclaimer { get; set; } = CTBv4_DISCLAIMER; // 320 bytes
            */

            [FieldOrder(1)]
            public float BottomRetractSpeed { get; set; }

            [FieldOrder(2)]
            public float BottomRetractSpeed2 { get; set; }

            [FieldOrder(3)]
            public uint Padding1 { get; set; }

            [FieldOrder(4)]
            public float Four1 { get; set; } = 4; // 4?

            [FieldOrder(5)]
            public uint Padding2 { get; set; }

            [FieldOrder(6)]
            public float Four2 { get; set; } = 4; // ?

            [FieldOrder(7)]
            public float RestTimeAfterRetract { get; set; }

            [FieldOrder(8)]
            public float RestTimeAfterLift { get; set; }

            [FieldOrder(9)]
            public float RestTimeBeforeLift { get; set; }

            [FieldOrder(10)]
            public float BottomRetractHeight2 { get; set; }

            [FieldOrder(11)]
            public float Unknown1 { get; set; } // 2955.996 or uint:1161347054 but changes

            [FieldOrder(12)]
            public uint Unknown2 { get; set; } // 73470 but changes

            [FieldOrder(13)]
            public uint Unknown3 { get; set; } = 5; // 5?

            [FieldOrder(14)]
            public uint LastLayerIndex { get; set; }

            [FieldOrder(15)]
            public uint Padding3 { get; set; }

            [FieldOrder(16)]
            public uint Padding4 { get; set; }

            [FieldOrder(17)]
            public uint Padding5 { get; set; }

            [FieldOrder(18)]
            public uint Padding6 { get; set; }

            [FieldOrder(19)]
            public uint DisclaimerAddress { get; set; }

            [FieldOrder(20)] public uint DisclaimerLength { get; set; } = CTBv4_DISCLAIMER_SIZE;

            [FieldOrder(21)]
            [FieldLength(CTBv4_RESERVED_SIZE)]
            public byte[] Reserved { get; set; } = new byte[CTBv4_RESERVED_SIZE]; // 384 bytes

            public override string ToString()
            {
                return $"{nameof(BottomRetractSpeed)}: {BottomRetractSpeed}, {nameof(BottomRetractSpeed2)}: {BottomRetractSpeed2}, {nameof(Padding1)}: {Padding1}, {nameof(Four1)}: {Four1}, {nameof(Padding2)}: {Padding2}, {nameof(Four2)}: {Four2}, {nameof(RestTimeAfterRetract)}: {RestTimeAfterRetract}, {nameof(RestTimeAfterLift)}: {RestTimeAfterLift}, {nameof(RestTimeBeforeLift)}: {RestTimeBeforeLift}, {nameof(BottomRetractHeight2)}: {BottomRetractHeight2}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(LastLayerIndex)}: {LastLayerIndex}, {nameof(Padding3)}: {Padding3}, {nameof(Padding4)}: {Padding4}, {nameof(Padding5)}: {Padding5}, {nameof(Padding6)}: {Padding6}, {nameof(DisclaimerAddress)}: {DisclaimerAddress}, {nameof(DisclaimerLength)}: {DisclaimerLength}, {nameof(Reserved)}: {Reserved}";
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
                var image = new Mat(new Size((int) ResolutionX, (int) ResolutionY), DepthType.Cv8U, 3);
                var span = image.GetBytePointer();

                /*var previewSize = ResolutionX * ResolutionY  * 2;
                if (previewSize != rawImageData.Length)
                {
                    throw new FileLoadException($"Thumbnail out of size, expecting {previewSize} bytes, got {rawImageData.Length}");
                    return null;
                }*/

                int pixel = 0;
                for (int n = 0; n < rawImageData.Length; n++)
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
                        //span[pixel++] = new Rgba32(red, green, blue);
                    }
                }

                return image;
            }

            public override string ToString()
            {
                return $"{nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(ImageOffset)}: {ImageOffset}, {nameof(ImageLength)}: {ImageLength}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(Unknown4)}: {Unknown4}";
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
                        color15 = (ushort) ncolor15;
                        rep = 1;
                    }
                }

                RleRGB15();

                ImageLength = (uint) rawData.Count;

                return rawData.ToArray();
            }
        }

        #endregion

        #region Layer
        public class LayerDef
        {
            /// <summary>
            /// Gets the build platform Z position for this layer, measured in millimeters.
            /// </summary>
            [FieldOrder(0)] public float PositionZ      { get; set; }

            /// <summary>
            /// Gets the exposure time for this layer, in seconds.
            /// </summary>
            [FieldOrder(1)] public float ExposureTime       { get; set; }

            /// <summary>
            /// Gets how long to keep the light off after exposing this layer, in seconds.
            /// </summary>
            [FieldOrder(2)] public float LightOffSeconds { get; set; }

            /// <summary>
            /// Gets the layer image offset to encoded layer data, and its length in bytes.
            /// </summary>
            [FieldOrder(3)] public uint DataAddress          { get; set; }

            /// <summary>
            /// Gets the layer image length in bytes.
            /// </summary>
            [FieldOrder(4)] public uint DataSize             { get; set; }
            [FieldOrder(5)] public uint Unknown1             { get; set; }
            [FieldOrder(6)] public uint Unknown2             { get; set; }// = 84; // Spoted on Mars 2 Pro
            [FieldOrder(7)] public uint Unknown3             { get; set; }
            [FieldOrder(8)] public uint Unknown4             { get; set; }


            [Ignore] public byte[] EncodedRle { get; set; }
            [Ignore] public ChituboxFile Parent { get; set; }

            public LayerDef()
            {
            }

            public LayerDef(ChituboxFile parent, Layer layer)
            {
                Parent = parent;
                SetFrom(layer);

                if (parent.HeaderSettings.Version >= 3 && Unknown2 == 0)
                {
                    Unknown2 = 84;
                }
            }

            public void SetFrom(Layer layer)
            {
                PositionZ = layer.PositionZ;
                ExposureTime = layer.ExposureTime;
                LightOffSeconds = layer.LightOffDelay;
            }

            public void CopyTo(Layer layer)
            {
                layer.PositionZ = PositionZ;
                layer.ExposureTime = ExposureTime;
                layer.LightOffDelay = LightOffSeconds;
            }


            public Mat Decode(uint layerIndex, bool consumeData = true)
            {
                var image = Parent.IsCtbFile ? DecodeCtbImage(layerIndex) : DecodeCbddlpImage(Parent, layerIndex);

                if (consumeData)
                {
                    for (byte aaIndex = 0; aaIndex < Parent.HeaderSettings.AntiAliasLevel; aaIndex++)
                    {
                        Parent.LayerDefinitions[aaIndex, layerIndex].EncodedRle = null;
                    }
                }

                return image;
            }

            public static unsafe Mat DecodeCbddlpImage(ChituboxFile parent, uint layerIndex)
            {
                var image = EmguExtensions.InitMat(parent.Resolution);
                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

                for (byte bit = 0; bit < parent.AntiAliasing; bit++)
                {
                    var layer = parent.LayerDefinitions[bit, layerIndex];

                    int n = 0;
                    for (int index = 0; index < layer.DataSize; index++)
                    {
                        // Lower 7 bits is the repeat count for the bit (0..127)
                        int reps = layer.EncodedRle[index] & 0x7f;

                        // We only need to set the non-zero pixels
                        // High bit is on for white, off for black
                        if ((layer.EncodedRle[index] & 0x80) != 0)
                        {
                            for (int i = 0; i < reps; i++)
                            {
                                span[n + i]++;
                            }
                        }

                        n += reps;

                        if (n == imageLength)
                        {
                            break;
                        }

                        if (n > imageLength)
                        {
                            image.Dispose();
                            throw new FileLoadException("Error image ran off the end");
                        }
                    }
                }

                for (int i = 0; i < imageLength; i++)
                {
                    int newC = span[i] * (256 / parent.AntiAliasing);

                    if (newC > 0)
                    {
                        newC--;
                    }

                    span[i] = (byte) newC;
                }

                return image;
            }

            private Mat DecodeCtbImage(uint layerIndex)
            {
                var mat = EmguExtensions.InitMat(Parent.Resolution);
                //var span = mat.GetBytePointer();

                if (Parent.HeaderSettings.EncryptionKey > 0)
                {
                    KeyRing kr = new(Parent.HeaderSettings.EncryptionKey, layerIndex);
                    EncodedRle = kr.Read(EncodedRle);
                }

                int pixel = 0;
                for (var n = 0; n < EncodedRle.Length; n++)
                {
                    byte code = EncodedRle[n];
                    int stride = 1;

                    if ((code & 0x80) == 0x80) // It's a run
                    {
                        code &= 0x7f; // Get the run length
                        n++;

                        var slen = EncodedRle[n];

                        if ((slen & 0x80) == 0)
                        {
                            stride = slen;
                        }
                        else if ((slen & 0xc0) == 0x80)
                        {
                            stride = ((slen & 0x3f) << 8) + EncodedRle[n + 1];
                            n++;
                        }
                        else if ((slen & 0xe0) == 0xc0)
                        {
                            stride = ((slen & 0x1f) << 16) + (EncodedRle[n + 1] << 8) + EncodedRle[n + 2];
                            n += 2;
                        }
                        else if ((slen & 0xf0) == 0xe0)
                        {
                            stride = ((slen & 0xf) << 24) + (EncodedRle[n + 1] << 16) + (EncodedRle[n + 2] << 8) + EncodedRle[n + 3];
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

            public byte[] Encode(Mat image, byte aaIndex, uint layerIndex)
            {
                return Parent.IsCtbFile ? EncodeCtbImage(image, layerIndex) : EncodeCbddlpImage(image, aaIndex);
            }

            public unsafe byte[] EncodeCbddlpImage(Mat image, byte bit)
            {
                List<byte> rawData = new();
                var span = image.GetBytePointer();
                var imageLength = image.GetLength();

                bool obit = false;
                int rep = 0;

                //ngrey:= uint16(r | g | b)
                // thresholds:
                // aa 1:  127
                // aa 2:  255 127
                // aa 4:  255 191 127 63
                // aa 8:  255 223 191 159 127 95 63 31
                byte threshold = (byte)(256 / Parent.AntiAliasing * bit - 1);

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

                for (int pixel = 0; pixel < imageLength; pixel++)
                {
                    var nbit = span[pixel] >= threshold;

                    if (nbit == obit)
                    {
                        rep++;

                        if (rep == RLE8EncodingLimit)
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

                EncodedRle = rawData.ToArray();
                DataSize = (uint) EncodedRle.Length;

                return EncodedRle;
            }

            private unsafe byte[] EncodeCtbImage(Mat image, uint layerIndex)
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
                    var grey7 = (byte) (span[pixel] >> 1);

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

                if (Parent.HeaderSettings.EncryptionKey > 0)
                {
                    KeyRing kr = new(Parent.HeaderSettings.EncryptionKey, layerIndex);
                    EncodedRle = kr.Read(rawData.ToArray());
                }
                else
                {
                    EncodedRle = rawData.ToArray();
                }

                DataSize = (uint)EncodedRle.Length;

                return EncodedRle;
            }

            public override string ToString()
            {
                return $"{nameof(PositionZ)}: {PositionZ}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(LightOffSeconds)}: {LightOffSeconds}, {nameof(DataAddress)}: {DataAddress}, {nameof(DataSize)}: {DataSize}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(Unknown4)}: {Unknown4}";
            }
        }

        public class LayerDefEx
        {
            /// <summary>
            /// Gets a copy of layer data definition
            /// </summary>
            [FieldOrder(0)] public LayerDef LayerDef { get; set; } = new();

            /// <summary>
            /// Gets the total size of ctbImageInfo and Image data
            /// </summary>
            [FieldOrder(1)] public uint TotalSize { get; set; }
            [FieldOrder(2)] public float LiftHeight { get; set; }
            [FieldOrder(3)] public float LiftSpeed { get; set; }
            [FieldOrder(4)] public float LiftHeight2 { get; set; }
            [FieldOrder(5)] public float LiftSpeed2 { get; set; }
            [FieldOrder(6)] public float RetractSpeed { get; set; }
            [FieldOrder(7)] public float RetractHeight2 { get; set; }
            [FieldOrder(8)] public float RetractSpeed2 { get; set; }
            [FieldOrder(9)] public float RestTimeBeforeLift { get; set; }
            [FieldOrder(10)] public float RestTimeAfterLift { get; set; }
            [FieldOrder(11)] public float RestTimeAfterRetract { get; set; } // 28672 v3?
            [FieldOrder(12)] public float LightPWM { get; set; }

            public LayerDefEx() { }

            public LayerDefEx(LayerDef layerDef, Layer layer)
            {
                LayerDef = layerDef;
                if (layer is not null)
                {
                    LiftHeight = layer.LiftHeight;
                    LiftSpeed = layer.LiftSpeed;
                    RetractSpeed = layer.RetractSpeed;
                    LightPWM = layer.LightPWM;

                    if (layerDef.Parent is not null && layerDef.Parent.HeaderSettings.Version >= 4)
                    {
                        LiftHeight2 = layer.LiftHeight2;
                        LiftSpeed2 = layer.LiftSpeed2;

                        RetractHeight2 = layer.RetractHeight2;
                        RetractSpeed2 = layer.RetractSpeed2;

                        RestTimeAfterRetract = layer.WaitTimeBeforeCure;
                        RestTimeBeforeLift = layer.WaitTimeAfterCure;
                        RestTimeAfterLift = layer.WaitTimeAfterLift;
                    }
                }

                if (layerDef.DataSize > 0)
                {
                    TotalSize = (uint) (Helpers.Serializer.SizeOf(this) + layerDef.DataSize);
                }
            }

            public void CopyTo(Layer layer)
            {
                LayerDef.CopyTo(layer);

                layer.LiftHeight = LiftHeight;
                layer.LiftSpeed = LiftSpeed;
                layer.RetractSpeed = RetractSpeed;
                layer.LightPWM = (byte)LightPWM;

                if (LayerDef.Parent is not null && LayerDef.Parent.HeaderSettings.Version >= 4)
                {
                    layer.LiftHeight2 = LiftHeight2;
                    layer.LiftSpeed2 = LiftSpeed2;

                    layer.RetractHeight2 = RetractHeight2;
                    layer.RetractSpeed2 = RetractSpeed2;

                    layer.WaitTimeBeforeCure = RestTimeAfterRetract;
                    layer.WaitTimeAfterCure = RestTimeBeforeLift;
                    layer.WaitTimeAfterLift = RestTimeAfterLift;
                }
            }

            public override string ToString()
            {
                return $"{nameof(LayerDef)}: {LayerDef}, {nameof(TotalSize)}: {TotalSize}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(LiftHeight2)}: {LiftHeight2}, {nameof(LiftSpeed2)}: {LiftSpeed2}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(RetractHeight2)}: {RetractHeight2}, {nameof(RetractSpeed2)}: {RetractSpeed2}, {nameof(RestTimeBeforeLift)}: {RestTimeBeforeLift}, {nameof(RestTimeAfterLift)}: {RestTimeAfterLift}, {nameof(RestTimeAfterRetract)}: {RestTimeAfterRetract}, {nameof(LightPWM)}: {LightPWM}";
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
                Init = seed * 0x2d83cdac + 0xd8a83423;
                Key = (layerIndex * 0x1e1530cd + 0xec3d47cd) * Init;
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
                List<byte> data = new(input.Count);
                data.AddRange(input.Select(t => (byte) (t ^ Next())));

                return data;
            }

            public byte[] Read(byte[] input)
            {
                byte[] data = new byte[input.Length];
                for (int i = 0; i < input.Length; i++)
                {
                    data[i] = (byte)(input[i]^Next());
                }
                return data;
            }
        }

        #endregion

        #endregion

        #region Properties

        public Header HeaderSettings { get; protected internal set; } = new();
        public PrintParameters PrintParametersSettings { get; protected internal set; } = new();

        public SlicerInfo SlicerInfoSettings { get; protected internal set; } = new();
        public PrintParametersV4 PrintParametersV4Settings { get; protected internal set; } = new();

        public Preview[] Previews { get; protected internal set; }

        public LayerDef[,] LayerDefinitions { get; private set; }

        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new(typeof(ChituboxFile), "ctb", $"Chitubox CTBv{USED_VERSION}"),
            //new(typeof(ChituboxFile), "v2.ctb", "Chitubox CTBv2"),
            //new(typeof(ChituboxFile), "v3.ctb", "Chitubox CTBv3"),
            new(typeof(ChituboxFile), "v4.ctb", "Chitubox CTBv4", false),
            //new(typeof(ChituboxFile), "encrypted.ctb", "Chitubox encrypted CTB"),
            new(typeof(ChituboxFile), "cbddlp", "Chitubox CBDDLP"),
            new(typeof(ChituboxFile), "photon", "Chitubox Photon"),
        };

        public override PrintParameterModifier[] PrintParameterModifiers
        {
            get
            {
                if (HeaderSettings.Version >= 4)
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
                        PrintParameterModifier.LightPWM,
                    };
                }

                return new[]
                {

                    PrintParameterModifier.BottomLayerCount,

                    PrintParameterModifier.BottomLightOffDelay,
                    PrintParameterModifier.LightOffDelay,
                    PrintParameterModifier.BottomExposureTime,
                    PrintParameterModifier.ExposureTime,

                    PrintParameterModifier.BottomLiftHeight,
                    PrintParameterModifier.BottomLiftSpeed,
                    PrintParameterModifier.LiftHeight,
                    PrintParameterModifier.LiftSpeed,
                    PrintParameterModifier.RetractSpeed,


                    PrintParameterModifier.BottomLightPWM,
                    PrintParameterModifier.LightPWM,
                };
            }
        }



        public override PrintParameterModifier[] PrintParameterPerLayerModifiers {
            get
            {
                if (!IsCtbFile) return null; // Only ctb files
                if (HeaderSettings.Version == 3)
                {
                    return new[]
                    {
                        PrintParameterModifier.LightOffDelay,
                        PrintParameterModifier.ExposureTime,
                        PrintParameterModifier.LiftHeight,
                        PrintParameterModifier.LiftSpeed,
                        PrintParameterModifier.RetractSpeed,
                        PrintParameterModifier.LightPWM,
                    };
                }
                if (HeaderSettings.Version >= 4)
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
                        PrintParameterModifier.LightPWM,
                    };
                }

                /* Disable for v2 beside the fields on format they are not used
                 if (HeaderSettings.Version <= 2)
                {
                    return new[]
                    {
                        PrintParameterModifier.ExposureSeconds,
                        PrintParameterModifier.LightOffDelay,
                    };
                }*/

                return null;
            } 
        }



        public override Size[] ThumbnailsOriginalSize { get; } =
        {
            new(400, 300),
            new(200, 125)
        };

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
                HeaderSettings.BedSizeX = (float) Math.Round(value, 2);
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

        public override bool DisplayMirror
        {
            get => HeaderSettings.ProjectorType > 0;
            set
            {
                HeaderSettings.ProjectorType = value ? 1u : 0;
                RaisePropertyChanged();
            }
        }

        public override bool IsAntiAliasingEmulated => IsCbddlpFile;

        public override byte AntiAliasing
        {
            get => (byte) (IsCtbFile ? SlicerInfoSettings.AntiAliasLevel : HeaderSettings.AntiAliasLevel);
            set
            {
                if (IsCtbFile)
                {
                    base.AntiAliasing = (byte)(SlicerInfoSettings.AntiAliasLevel = value.Clamp(1, 16));
                }
                else if(IsCbddlpFile)
                {
                    base.AntiAliasing = (byte)(SlicerInfoSettings.AntiAliasLevel = HeaderSettings.AntiAliasLevel = value.Clamp(1, 16));
                    ValidateAntiAliasingLevel();
                }
            }
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
            get => base.PrintHeight;
            set => base.PrintHeight = HeaderSettings.TotalHeightMilimeter = base.PrintHeight;
        }

        public override uint LayerCount
        {
            get => base.LayerCount;
            set
            {
                base.LayerCount = HeaderSettings.LayerCount = base.LayerCount;
                PrintParametersV4Settings.LastLayerIndex = LastLayerIndex;
            }
        }

        public override ushort BottomLayerCount
        {
            get => (ushort) HeaderSettings.BottomLayersCount;
            set => base.BottomLayerCount = (ushort) (HeaderSettings.BottomLayersCount = value);
        }

        public override float BottomLightOffDelay
        {
            get => PrintParametersSettings.BottomLightOffDelay;
            set
            {
                base.BottomLightOffDelay = PrintParametersSettings.BottomLightOffDelay = (float) Math.Round(value, 2);
                if (HeaderSettings.Version >= 4 && value > 0)
                {
                    WaitTimeBeforeCure = 0;
                    WaitTimeAfterCure = 0;
                    WaitTimeAfterLift = 0;
                }
            }
        }

        public override float LightOffDelay
        {
            get => PrintParametersSettings.LightOffDelay;
            set
            {
                base.LightOffDelay = HeaderSettings.LightOffDelay = PrintParametersSettings.LightOffDelay = (float) Math.Round(value, 2);
                if (HeaderSettings.Version >= 4 && value > 0)
                {
                    WaitTimeBeforeCure = 0;
                    WaitTimeAfterCure = 0;
                    WaitTimeAfterLift = 0;
                }
            }
        }

        public override float BottomWaitTimeBeforeCure
        {
            get => WaitTimeBeforeCure;
            set
            {
                if (HeaderSettings.Version < 4)
                {
                    if (value > 0)
                    {
                        SetBottomLightOffDelay(value);
                    }
                }
            }
        }

        public override float WaitTimeBeforeCure
        {
            get => HeaderSettings.Version >= 4 ? PrintParametersV4Settings.RestTimeAfterRetract : 0;
            set
            {
                if (HeaderSettings.Version < 4)
                {
                    if (value > 0)
                    {
                        SetNormalLightOffDelay(value);
                    }

                    return;
                }
                base.WaitTimeBeforeCure = SlicerInfoSettings.RestTimeAfterRetract = PrintParametersV4Settings.RestTimeAfterRetract = (float)Math.Round(value, 2);
                if (value > 0)
                {
                    BottomLightOffDelay = 0;
                    LightOffDelay = 0;
                }
            }
        }

        public override float BottomExposureTime
        {
            get => HeaderSettings.BottomExposureSeconds;
            set => base.BottomExposureTime = HeaderSettings.BottomExposureSeconds = (float) Math.Round(value, 2);
        }

        public override float BottomWaitTimeAfterCure => WaitTimeAfterCure;
        public override float WaitTimeAfterCure
        {
            get => HeaderSettings.Version >= 4 ? PrintParametersV4Settings.RestTimeBeforeLift : 0;
            set
            {
                if (HeaderSettings.Version < 4) return;
                base.WaitTimeAfterCure = PrintParametersV4Settings.RestTimeBeforeLift = (float) Math.Round(value, 2);
                if (value > 0)
                {
                    BottomLightOffDelay = 0;
                    LightOffDelay = 0;
                }
            }
        }

        public override float ExposureTime
        {
            get => HeaderSettings.LayerExposureSeconds;
            set => base.ExposureTime = HeaderSettings.LayerExposureSeconds = (float)Math.Round(value, 2);
        }

        public override float BottomLiftHeight
        {
            get
            {
                if (HeaderSettings.Version <= 3) return PrintParametersSettings.BottomLiftHeight;
                return Math.Max(0, PrintParametersSettings.BottomLiftHeight - SlicerInfoSettings.BottomLiftHeight2);
            }
            set
            {
                value = (float)Math.Round(value, 2);
                if (HeaderSettings.Version <= 3) PrintParametersSettings.BottomLiftHeight = value;
                if (HeaderSettings.Version >= 4) PrintParametersSettings.BottomLiftHeight = (float)Math.Round(value + SlicerInfoSettings.BottomLiftHeight2, 2);
                base.BottomLiftHeight = value;
            }
        }

        public override float BottomLiftSpeed
        {
            get => PrintParametersSettings.BottomLiftSpeed;
            set => base.BottomLiftSpeed = PrintParametersSettings.BottomLiftSpeed = (float)Math.Round(value, 2);
        }

        public override float LiftHeight
        {
            get
            {
                if(HeaderSettings.Version <= 3) return PrintParametersSettings.LiftHeight;
                return Math.Max(0, PrintParametersSettings.LiftHeight - SlicerInfoSettings.LiftHeight2);
            }
            set
            {
                value = (float)Math.Round(value, 2);
                if (HeaderSettings.Version <= 3) PrintParametersSettings.LiftHeight = value;
                if (HeaderSettings.Version >= 4) PrintParametersSettings.LiftHeight = (float)Math.Round(value + SlicerInfoSettings.LiftHeight2, 2);
                base.LiftHeight = value;
            }
        }

        public override float LiftSpeed
        {
            get => PrintParametersSettings.LiftSpeed;
            set => base.LiftSpeed = PrintParametersSettings.LiftSpeed = (float)Math.Round(value, 2);
        }

        public override float BottomLiftHeight2
        {
            get => HeaderSettings.Version >= 4 ? SlicerInfoSettings.BottomLiftHeight2 : 0;
            set
            {
                if (HeaderSettings.Version < 4) return;
                var bottomLiftHeight = BottomLiftHeight;
                SlicerInfoSettings.BottomLiftHeight2 = (float)Math.Round(value, 2);
                BottomLiftHeight = bottomLiftHeight;
                base.BottomLiftHeight2 = SlicerInfoSettings.BottomLiftHeight2;
            }
        }

        public override float BottomLiftSpeed2
        {
            get => HeaderSettings.Version >= 4 ? SlicerInfoSettings.BottomLiftSpeed2 : 0;
            set
            {
                if (HeaderSettings.Version < 4) return;
                base.BottomLiftSpeed2 = SlicerInfoSettings.BottomLiftSpeed2 = (float)Math.Round(value, 2);
            }
        }

        public override float LiftHeight2
        {
            get => HeaderSettings.Version >= 4 ? SlicerInfoSettings.LiftHeight2 : 0;
            set
            {
                if (HeaderSettings.Version < 4) return;
                var liftHeight = LiftHeight;
                SlicerInfoSettings.LiftHeight2 = (float)Math.Round(value, 2);
                LiftHeight = liftHeight;
                base.LiftHeight2 = SlicerInfoSettings.BottomLiftHeight2;
            }
        }

        public override float LiftSpeed2
        {
            get => HeaderSettings.Version >= 4 ? SlicerInfoSettings.LiftSpeed2 : 0;
            set
            {
                if (HeaderSettings.Version < 4) return;
                base.LiftSpeed2 = SlicerInfoSettings.LiftSpeed2 = (float)Math.Round(value, 2);
            }
        }

        public override float BottomWaitTimeAfterLift => WaitTimeAfterLift;
        public override float WaitTimeAfterLift
        {
            get => HeaderSettings.Version >= 4 ? PrintParametersV4Settings.RestTimeAfterLift : 0;
            set
            {
                if (HeaderSettings.Version < 4) return;
                base.WaitTimeAfterLift = SlicerInfoSettings.RestTimeAfterLift = SlicerInfoSettings.RestTimeAfterLift2 = PrintParametersV4Settings.RestTimeAfterLift = (float)Math.Round(value, 2);
                if (value > 0)
                {
                    BottomLightOffDelay = 0;
                    LightOffDelay = 0;
                }
            }
        }

        public override float BottomRetractSpeed
        {
            get => HeaderSettings.Version >= 4 ? PrintParametersV4Settings.BottomRetractSpeed : RetractSpeed;
            set
            {
                if (HeaderSettings.Version < 4) return;
                base.BottomRetractSpeed = PrintParametersV4Settings.BottomRetractSpeed = (float)Math.Round(value, 2);
            }
        }

        public override float RetractSpeed
        {
            get => PrintParametersSettings.RetractSpeed;
            set => base.RetractSpeed = PrintParametersSettings.RetractSpeed = (float)Math.Round(value, 2);
        }

        public override float BottomRetractHeight2
        {
            get => HeaderSettings.Version >= 4 ? PrintParametersV4Settings.BottomRetractHeight2 : 0;
            set
            {
                if (HeaderSettings.Version < 4) return;
                value = Math.Clamp((float)Math.Round(value, 2), 0, BottomRetractHeightTotal);
                base.BottomRetractHeight2 = PrintParametersV4Settings.BottomRetractHeight2 = value;
            }
        }

        public override float BottomRetractSpeed2
        {
            get => HeaderSettings.Version >= 4 ? PrintParametersV4Settings.BottomRetractSpeed2 : 0;
            set
            {
                if (HeaderSettings.Version < 4) return;
                base.BottomRetractSpeed2 = PrintParametersV4Settings.BottomRetractSpeed2 = (float)Math.Round(value, 2);
            }
        }

        public override float RetractHeight2
        {
            get => HeaderSettings.Version >= 4 ? SlicerInfoSettings.RetractHeight2 : 0;
            set
            {
                if (HeaderSettings.Version < 4) return;
                value = Math.Clamp((float)Math.Round(value, 2), 0, RetractHeightTotal);
                base.RetractHeight2 = SlicerInfoSettings.RetractHeight2 = value;
            } 
        }

        public override float RetractSpeed2
        {
            get => HeaderSettings.Version >= 4 ? SlicerInfoSettings.RetractSpeed2 : 0;
            set => base.RetractSpeed2 = SlicerInfoSettings.RetractSpeed2 = (float)Math.Round(value, 2);
        }

        public override byte BottomLightPWM
        {
            get => (byte)HeaderSettings.BottomLightPWM;
            set => base.BottomLightPWM = (byte) (HeaderSettings.BottomLightPWM = value);
        }

        public override byte LightPWM
        {
            get => (byte)HeaderSettings.LightPWM;
            set => base.LightPWM = (byte) (HeaderSettings.LightPWM = value);
        }

        public override float PrintTime
        {
            get => base.PrintTime;
            set
            {
                base.PrintTime = value;
                HeaderSettings.PrintTime = (uint)base.PrintTime;
            }
        }

        public override float MaterialMilliliters
        {
            get => base.MaterialMilliliters;
            set
            {
                base.MaterialMilliliters = value;
                PrintParametersSettings.VolumeMl = base.MaterialMilliliters;
            }
        }

        public override float MaterialGrams
        {
            get => PrintParametersSettings.WeightG;
            set => base.MaterialGrams = PrintParametersSettings.WeightG = (float)Math.Round(value, 3);
        }

        public override float MaterialCost
        {
            get => (float) Math.Round(PrintParametersSettings.CostDollars, 3);
            set => base.MaterialCost = PrintParametersSettings.CostDollars = (float) Math.Round(value, 3);
        }

        public override string MachineName
        {
            get => SlicerInfoSettings.MachineName;
            set
            {
                base.MachineName = SlicerInfoSettings.MachineName = value;
                SlicerInfoSettings.MachineNameSize = (uint) SlicerInfoSettings.MachineName.Length;
            }
        }

        public override object[] Configs
        {
            get
            {
                if (HeaderSettings.Version <= 1)
                    return new object[] { HeaderSettings };

                if (HeaderSettings.Version <= 3)
                    return new object[] {HeaderSettings, PrintParametersSettings, SlicerInfoSettings};
                
                return new object[] { HeaderSettings, PrintParametersSettings, SlicerInfoSettings, PrintParametersV4Settings };
            }
        }

        public bool IsCbddlpFile => HeaderSettings.Magic == MAGIC_CBDDLP;
        public bool IsCtbFile => HeaderSettings.Magic is MAGIC_CTB or MAGIC_CTBv4;

        public bool CanHash => IsCbddlpFile && HeaderSettings.Version <= 2;
        #endregion

        #region Constructors
        public ChituboxFile()
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

            LayerDefinitions = null;
        }

        public override bool CanProcess(string fileFullPath)
        {
            if (!base.CanProcess(fileFullPath)) return false;

            try
            {
                using var fs = new BinaryReader(new FileStream(fileFullPath, FileMode.Open, FileAccess.Read));
                var magic = fs.ReadUInt32();
                return magic is MAGIC_CBDDLP or MAGIC_CTB or MAGIC_CTBv4;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }

            return false;
        }

        public void SanitizeProperties()
        {
            if (IsCtbFile)
            {
                if (SlicerInfoSettings.AntiAliasLevel <= 1)
                {
                    SlicerInfoSettings.AntiAliasLevel = HeaderSettings.AntiAliasLevel;
                }

                HeaderSettings.AntiAliasLevel = 1;

                if (HeaderSettings.Version <= 2)
                {
                    SlicerInfoSettings.EncryptionMode = ENCRYPTYION_MODE_CTBv2;
                    PrintParametersSettings.Padding4 = 0x1234; // 4660

                    if (SlicerInfoSettings.MysteriousId == 0)
                        SlicerInfoSettings.MysteriousId = 305419896;
                }
                else if (HeaderSettings.Version == 3)
                {
                    SlicerInfoSettings.EncryptionMode = ENCRYPTYION_MODE_CTBv3;

                    if (SlicerInfoSettings.MysteriousId == 0)
                        SlicerInfoSettings.MysteriousId = 305419896;
                }
                else if (HeaderSettings.Version >= 4)
                {
                    SlicerInfoSettings.EncryptionMode = ENCRYPTYION_MODE_CTBv4;

                    if (SlicerInfoSettings.MysteriousId == 0)
                        SlicerInfoSettings.MysteriousId = 27087820;
                }
            }
        }

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            if (FileEndsWith(".ctb"))
            {
                if (HeaderSettings.Magic is not MAGIC_CTB and not MAGIC_CTBv4)
                {
                    HeaderSettings.Magic = MAGIC_CTB;
                }

                if (FileEndsWith(".v2.ctb"))
                {
                    HeaderSettings.Magic = MAGIC_CTB;
                    HeaderSettings.Version = 2;
                }
                else if (FileEndsWith(".v3.ctb"))
                {
                    HeaderSettings.Magic = MAGIC_CTB;
                    HeaderSettings.Version = 3;
                }
                else if (FileEndsWith(".v4.ctb"))
                {
                    HeaderSettings.Magic = MAGIC_CTBv4;
                    HeaderSettings.Version = 4;
                }
            }
            else if (FileEndsWith(".cbddlp"))
            {
                HeaderSettings.Magic = MAGIC_CBDDLP;
            }
            HeaderSettings.PrintParametersSize = (uint)Helpers.Serializer.SizeOf(PrintParametersSettings);

            
            SanitizeProperties();
            if (IsCtbFile)
            {
                if (HeaderSettings.EncryptionKey == 0)
                {
                    var rnd = new Random();
                    HeaderSettings.EncryptionKey = (uint)rnd.Next(byte.MaxValue, int.MaxValue);
                }
            }
            else
            {
                //HeaderSettings.Version = 2;
                HeaderSettings.EncryptionKey = 0; // Force disable encryption
                SlicerInfoSettings.EncryptionMode = ENCRYPTYION_MODE_CBDDLP;
            }

            //uint currentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
            LayerDefinitions = new LayerDef[HeaderSettings.AntiAliasLevel, HeaderSettings.LayerCount];
            using var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
            outputFile.Seek(Helpers.Serializer.SizeOf(HeaderSettings), SeekOrigin.Begin);

            Mat[] thumbnails = {GetThumbnail(true), GetThumbnail(false)};
            for (byte i = 0; i < thumbnails.Length; i++)
            {
                var image = thumbnails[i];
                if(image is null) continue;

                Preview preview = new()
                {
                    ResolutionX = (uint)image.Width,
                    ResolutionY = (uint)image.Height,
                };

                var previewBytes = preview.Encode(image);

                if (previewBytes.Length == 0) continue;

                if (i == 0)
                {
                    HeaderSettings.PreviewLargeOffsetAddress = (uint)outputFile.Position;
                }
                else
                {
                    HeaderSettings.PreviewSmallOffsetAddress = (uint)outputFile.Position;
                }


                preview.ImageOffset = (uint)(outputFile.Position + Helpers.Serializer.SizeOf(preview));

                Helpers.SerializeWriteFileStream(outputFile, preview);
                outputFile.WriteBytes(previewBytes);
            }


            if (HeaderSettings.Version >= 2)
            {
                HeaderSettings.PrintParametersOffsetAddress = (uint)outputFile.Position;

                Helpers.SerializeWriteFileStream(outputFile, PrintParametersSettings);

                HeaderSettings.SlicerOffset = (uint)outputFile.Position;
                HeaderSettings.SlicerSize = (uint) Helpers.Serializer.SizeOf(SlicerInfoSettings) - SlicerInfoSettings.MachineNameSize;

                SlicerInfoSettings.MachineNameAddress = HeaderSettings.SlicerOffset + HeaderSettings.SlicerSize;

                if (HeaderSettings.Version >= 4)
                {
                    SlicerInfoSettings.PrintParametersV4Address = (uint)(HeaderSettings.SlicerOffset + 
                                                                         Helpers.Serializer.SizeOf(SlicerInfoSettings) + 
                                                                         CTBv4_DISCLAIMER_SIZE);
                }


                Helpers.SerializeWriteFileStream(outputFile, SlicerInfoSettings);

                if (HeaderSettings.Version >= 4)
                {
                    PrintParametersV4Settings.DisclaimerAddress = (uint)outputFile.Position;
                    PrintParametersV4Settings.DisclaimerLength = (uint)CTBv4_DISCLAIMER.Length;
                    outputFile.WriteBytes(Encoding.UTF8.GetBytes(CTBv4_DISCLAIMER));
                    Helpers.SerializeWriteFileStream(outputFile, PrintParametersV4Settings);
                }
            }

            HeaderSettings.LayersDefinitionOffsetAddress = (uint)outputFile.Position;
            uint layerDefSize = (uint)Helpers.Serializer.SizeOf(new LayerDef());
            //uint layerDefCurrentOffset = HeaderSettings.LayersDefinitionOffsetAddress;
            uint layerDataCurrentOffset = HeaderSettings.LayersDefinitionOffsetAddress + layerDefSize * HeaderSettings.LayerCount * HeaderSettings.AntiAliasLevel;

            var layersHash = new Dictionary<string, LayerDef>();
            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);

            foreach (var batch in BatchLayersIndexes())
            {
                Parallel.ForEach(batch, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    using (var mat = this[layerIndex].LayerMat)
                    {
                        for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
                        {
                            var layerDef = new LayerDef(this, this[layerIndex]);
                            layerDef.Encode(mat, aaIndex, (uint)layerIndex);
                            LayerDefinitions[aaIndex, layerIndex] = layerDef;
                        }
                    }
                    progress.LockAndIncrement();
                });

                foreach (var layerIndex in batch)
                {
                    if (layerIndex == 0) layerDefSize = (uint)Helpers.Serializer.SizeOf(LayerDefinitions[0, layerIndex]);
                    for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
                    {
                        progress.Token.ThrowIfCancellationRequested();

                        var layerDef = LayerDefinitions[aaIndex, layerIndex];
                        LayerDef layerDefHash = null;

                        if (CanHash)
                        {
                            var hash = Helpers.ComputeSHA1Hash(layerDef.EncodedRle);
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

                            if (HeaderSettings.Version >= 3)
                            {
                                var layerDataEx = new LayerDefEx(layerDef, this[layerIndex]);
                                layerDataCurrentOffset += (uint)Helpers.Serializer.SizeOf(layerDataEx);
                                layerDef.DataAddress = layerDataCurrentOffset;
                                Helpers.SerializeWriteFileStream(outputFile, layerDataEx);
                            }

                            layerDataCurrentOffset += outputFile.WriteBytes(layerDef.EncodedRle);
                        }

                        outputFile.Seek(HeaderSettings.LayersDefinitionOffsetAddress +
                                        aaIndex * HeaderSettings.LayerCount * layerDefSize +
                                        layerDefSize * layerIndex
                            , SeekOrigin.Begin);
                        Helpers.SerializeWriteFileStream(outputFile, layerDef);

                        layerDef.EncodedRle = null; // Free this
                    }
                }
            }
            

            outputFile.Seek(0, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine(Previews[0]);
            Debug.WriteLine(Previews[1]);
            Debug.WriteLine(PrintParametersSettings);
            Debug.WriteLine(SlicerInfoSettings);
            Debug.WriteLine(PrintParametersV4Settings);
            Debug.WriteLine("-End-");
        }


        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);
            //HeaderSettings = Helpers.ByteToType<CbddlpFile.Header>(InputFile);
            //HeaderSettings = Helpers.Serializer.Deserialize<Header>(InputFile.ReadBytes(Helpers.Serializer.SizeOf(typeof(Header))));
            HeaderSettings = Helpers.Deserialize<Header>(inputFile);

            if (HeaderSettings.Magic is not MAGIC_CBDDLP and not MAGIC_CTB and not MAGIC_CTBv4)
            {
                throw new FileLoadException($"Not a valid PHOTON nor CBDDLP nor CTB file! Magic Value: {HeaderSettings.Magic}", fileFullPath);
            }

            if (HeaderSettings.Version == 1 || HeaderSettings.AntiAliasLevel == 0)
            {
                HeaderSettings.AntiAliasLevel = 1;
            }

            FileFullPath = fileFullPath;

            progress.Reset(OperationProgress.StatusDecodePreviews, ThumbnailsCount);

            Debug.Write("Header -> ");
            Debug.WriteLine(HeaderSettings);

            for (byte i = 0; i < ThumbnailsCount; i++)
            {
                uint offsetAddress = i == 0
                    ? HeaderSettings.PreviewLargeOffsetAddress
                    : HeaderSettings.PreviewSmallOffsetAddress;
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

            if (HeaderSettings.PrintParametersOffsetAddress > 0)
            {
                inputFile.Seek(HeaderSettings.PrintParametersOffsetAddress, SeekOrigin.Begin);
                PrintParametersSettings = Helpers.Deserialize<PrintParameters>(inputFile);
                Debug.Write("Print Parameters -> ");
                Debug.WriteLine(PrintParametersSettings);


            }

            if (HeaderSettings.SlicerOffset > 0)
            {
                inputFile.Seek(HeaderSettings.SlicerOffset, SeekOrigin.Begin);
                SlicerInfoSettings = Helpers.Deserialize<SlicerInfo>(inputFile);
                Debug.Write("Slicer Info -> ");
                Debug.WriteLine(SlicerInfoSettings);
            }

            /*InputFile.BaseStream.Seek(MachineInfoSettings.MachineNameAddress, SeekOrigin.Begin);
                byte[] bytes = InputFile.ReadBytes((int)MachineInfoSettings.MachineNameSize);
                MachineName = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.WriteLine($"{nameof(MachineName)}: {MachineName}");*/
            //}

            if (HeaderSettings.Version >= 4)
            {
                if (SlicerInfoSettings.PrintParametersV4Address == 0)
                {
                    throw new FileLoadException(
                        $"Malformed file, PrintParametersV4Address is missing",
                        fileFullPath);
                }

                inputFile.Seek(SlicerInfoSettings.PrintParametersV4Address, SeekOrigin.Begin);
                PrintParametersV4Settings = Helpers.Deserialize<PrintParametersV4>(inputFile);
                Debug.Write("Print Parameters V4 -> ");
                Debug.WriteLine(PrintParametersV4Settings);

                if (PrintParametersV4Settings.Four1 != 4 && PrintParametersV4Settings.Four2 != 4)
                {
                    throw new FileLoadException(
                        $"Malformed file, PrintParametersV4 found invalid validation values, expected (4, 4) " +
                        $"but got ({PrintParametersV4Settings.Four1}, {PrintParametersV4Settings.Four2})",
                        fileFullPath);
                }
            }

            LayerDefinitions = new LayerDef[HeaderSettings.AntiAliasLevel, HeaderSettings.LayerCount];
            var layerDefinitionsEx = HeaderSettings.Version >= 3 ? new LayerDefEx[HeaderSettings.LayerCount] : null;

            uint layerOffset = HeaderSettings.LayersDefinitionOffsetAddress;

            progress.Reset(OperationProgress.StatusGatherLayers,
                HeaderSettings.AntiAliasLevel * HeaderSettings.LayerCount);

            for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
            {
                Debug.WriteLine($"-Image GROUP {aaIndex}-");
                for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    inputFile.Seek(layerOffset, SeekOrigin.Begin);
                    var layerDef = Helpers.Deserialize<LayerDef>(inputFile);
                    layerDef.Parent = this;
                    LayerDefinitions[aaIndex, layerIndex] = layerDef;
                    LayerDefinitions[aaIndex, layerIndex].Parent = this;

                    layerOffset += (uint) Helpers.Serializer.SizeOf(layerDef);
                    Debug.Write($"LAYER {layerIndex} -> ");
                    Debug.WriteLine(layerDef);

                    //layerDef.EncodedRle = new byte[layerDef.DataSize];
                        
                    if (HeaderSettings.Version >= 3)
                    {
                        inputFile.SeekDoWorkAndRewind(layerDef.DataAddress - 84, () =>
                        {
                            layerDefinitionsEx[layerIndex] = Helpers.Deserialize<LayerDefEx>(inputFile);
                            layerDefinitionsEx[layerIndex].LayerDef.Parent = this;
                            Debug.Write($"LAYER {layerIndex} -> ");
                            Debug.WriteLine(layerDefinitionsEx[layerIndex]);
                        });
                    }

                    progress++;
                }
            }

            LayerManager.Init(HeaderSettings.LayerCount);

            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

            foreach (var batch in BatchLayersIndexes())
            {
                foreach (var layerIndex in batch)
                {
                    for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
                    {
                        progress.Token.ThrowIfCancellationRequested();

                        inputFile.Seek(LayerDefinitions[aaIndex, layerIndex].DataAddress, SeekOrigin.Begin);
                        LayerDefinitions[aaIndex, layerIndex].EncodedRle = inputFile.ReadBytes(LayerDefinitions[aaIndex, layerIndex].DataSize);
                    }
                }

                Parallel.ForEach(batch, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    using (var mat = LayerDefinitions[0, layerIndex].Decode((uint)layerIndex))
                    {
                        var layer = new Layer((uint)layerIndex, mat, this);
                        if (layerDefinitionsEx is not null) // CTBv4
                        {
                            layerDefinitionsEx[layerIndex].CopyTo(layer);
                        }
                        else // others
                        {
                            LayerDefinitions[0, layerIndex].CopyTo(layer);
                        }

                        this[layerIndex] = layer;
                    }

                    progress.LockAndIncrement();
                });
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

            SanitizeProperties();
            using var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write);
            outputFile.Seek(0, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

            if (HeaderSettings.Version >= 2 && HeaderSettings.PrintParametersOffsetAddress > 0)
            {
                outputFile.Seek(HeaderSettings.PrintParametersOffsetAddress, SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, PrintParametersSettings);
                Helpers.SerializeWriteFileStream(outputFile, SlicerInfoSettings);
            }

            uint layerOffset = HeaderSettings.LayersDefinitionOffsetAddress;
            for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
            {
                for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    var layer = this[layerIndex];
                    LayerDefinitions[aaIndex, layerIndex].SetFrom(layer);

                    outputFile.Seek(layerOffset, SeekOrigin.Begin);
                    layerOffset += Helpers.SerializeWriteFileStream(outputFile, LayerDefinitions[aaIndex, layerIndex]);
                }
            }

            if (HeaderSettings.Version >= 3)
            {
                for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    outputFile.Seek(LayerDefinitions[0, layerIndex].DataAddress - 84, SeekOrigin.Begin);
                    Helpers.SerializeWriteFileStream(outputFile, new LayerDefEx(LayerDefinitions[0, layerIndex], this[layerIndex]));
                }
            }

            if (HeaderSettings.Version >= 4)
            {
                outputFile.Seek(SlicerInfoSettings.PrintParametersV4Address, SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, PrintParametersV4Settings);
            }
        }

        #endregion
    }
}
