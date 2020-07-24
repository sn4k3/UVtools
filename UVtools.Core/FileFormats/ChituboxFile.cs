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
using System.Threading.Tasks;
using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class ChituboxFile : FileFormat
    {
        #region Constants
        private const uint MAGIC_CBDDLP = 0x12FD0019;
        private const uint MAGIC_CBT = 0x12FD0086;
        private const ushort REPEATRGB15MASK = 0x20;

        private const byte RLE8EncodingLimit = 0x7d; // 125;
        private const ushort RLE16EncodingLimit = 0xFFF;
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
            [FieldOrder(1)] public uint Version { get; set; } = 2;

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
            [FieldOrder(7)]  public float OverallHeightMilimeter { get; set; }

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
            [FieldOrder(11)] public float LayerOffTime     { get; set; } = 1;

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
                return $"{nameof(Magic)}: {Magic}, {nameof(Version)}: {Version}, {nameof(BedSizeX)}: {BedSizeX}, {nameof(BedSizeY)}: {BedSizeY}, {nameof(BedSizeZ)}: {BedSizeZ}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(OverallHeightMilimeter)}: {OverallHeightMilimeter}, {nameof(LayerHeightMilimeter)}: {LayerHeightMilimeter}, {nameof(LayerExposureSeconds)}: {LayerExposureSeconds}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(LayerOffTime)}: {LayerOffTime}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(PreviewLargeOffsetAddress)}: {PreviewLargeOffsetAddress}, {nameof(LayersDefinitionOffsetAddress)}: {LayersDefinitionOffsetAddress}, {nameof(LayerCount)}: {LayerCount}, {nameof(PreviewSmallOffsetAddress)}: {PreviewSmallOffsetAddress}, {nameof(PrintTime)}: {PrintTime}, {nameof(ProjectorType)}: {ProjectorType}, {nameof(PrintParametersOffsetAddress)}: {PrintParametersOffsetAddress}, {nameof(PrintParametersSize)}: {PrintParametersSize}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(LightPWM)}: {LightPWM}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(EncryptionKey)}: {EncryptionKey}, {nameof(SlicerOffset)}: {SlicerOffset}, {nameof(SlicerSize)}: {SlicerSize}";
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
            [FieldOrder(0)] public uint Padding1           { get; set; }
            [FieldOrder(1)] public uint Padding2           { get; set; }
            [FieldOrder(2)] public uint Padding3           { get; set; }
            [FieldOrder(3)] public uint Padding4           { get; set; }
            [FieldOrder(4)] public uint Padding5           { get; set; }
            [FieldOrder(5)] public uint Padding6           { get; set; }
            [FieldOrder(6)] public uint Padding7           { get; set; }

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
            /// Not totally understood. 0 for cbddlp files, 0xF for ctb files.
            /// </summary>
            [FieldOrder(9)] public uint EncryptionMode     { get; set; } = 8;

            /// <summary>
            /// Gets a number that increments with time or number of models sliced, or both. Zeroing it in output seems to have no effect. Possibly a user tracking bug.
            /// </summary>
            [FieldOrder(10)] public uint MysteriousId      { get; set; }

            /// <summary>
            /// Gets the user-selected antialiasing level. For cbddlp files this will match the level_set_count. For ctb files, this number is essentially arbitrary.
            /// </summary>
            [FieldOrder(11)] public uint AntiAliasLevel { get; set; } = 1;

            /// <summary>
            /// Gets a version of software that generated this file, encoded with major, minor, and patch release in bytes starting from the MSB down.
            /// (No provision is made to name the software being used, so this assumes that only one software package can generate the files.
            /// Probably best to hardcode it at 0x01060300.)
            /// </summary>
            [FieldOrder(12)] public uint SoftwareVersion { get; set; } = 0x01060300;
            [FieldOrder(13)] public uint Unknown1          { get; set; }
            [FieldOrder(14)] public uint Padding8          { get; set; }
            [FieldOrder(15)] public uint Padding9          { get; set; }
            [FieldOrder(16)] public uint Padding10         { get; set; }
            [FieldOrder(17)] public uint Padding11         { get; set; }
            [FieldOrder(18)] public uint Padding12         { get; set; }

            /// <summary>
            /// Gets the machine name. string is not nul-terminated.
            /// The character encoding is currently unknown — all observed files in the wild use 7-bit ASCII characters only.
            /// Note that the machine type here is set in the software profile, and is not the name the user assigned to the machine.
            /// </summary>
            [FieldOrder(19)] [FieldLength(nameof(MachineNameSize))]
            public string MachineName               { get; set; }

            public override string ToString()
            {
                return $"{nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(Padding3)}: {Padding3}, {nameof(Padding4)}: {Padding4}, {nameof(Padding5)}: {Padding5}, {nameof(Padding6)}: {Padding6}, {nameof(Padding7)}: {Padding7}, {nameof(MachineNameAddress)}: {MachineNameAddress}, {nameof(MachineNameSize)}: {MachineNameSize}, {nameof(EncryptionMode)}: {EncryptionMode}, {nameof(MysteriousId)}: {MysteriousId}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(SoftwareVersion)}: {SoftwareVersion}, {nameof(Unknown1)}: {Unknown1}, {nameof(Padding8)}: {Padding8}, {nameof(Padding9)}: {Padding9}, {nameof(Padding10)}: {Padding10}, {nameof(Padding11)}: {Padding11}, {nameof(Padding12)}: {Padding12}, {nameof(MachineName)}: {MachineName}";
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
                var image = new Mat(new Size((int) ResolutionX, (int) ResolutionY), DepthType.Cv8U, 3);
                var span = image.GetPixelSpan<byte>();
                //var image = EmguExtensions.CreateMat(out var bytes, new Size((int) ResolutionX, (int) ResolutionY), 3);
                //var image = new MatBytes(new Size((int)ResolutionX, (int)ResolutionY), 3);
                //var bytes = image.Bytes;

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

            public byte[] Encode(Mat image)
            {
                List<byte> rawData = new List<byte>();
                ushort color15 = 0;
                uint rep = 0;

                var span = image.GetPixelSpan<byte>();

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
                while (pixel < span.Length)
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
            [Ignore] public ChituboxFile Parent { get; set; }

            public LayerData()
            {
            }

            public LayerData(ChituboxFile parent, uint layerIndex)
            {
                Parent = parent;
                LayerPositionZ = parent[layerIndex].PositionZ;
                LayerExposure = parent[layerIndex].ExposureTime;

                LayerOffTimeSeconds = parent.GetInitialLayerValueOrNormal(layerIndex, 
                    parent.PrintParametersSettings.BottomLightOffDelay, 
                    parent.PrintParametersSettings.LightOffDelay);

                /*LayerExposure = layerIndex < parent.HeaderSettings.BottomLayersCount
                    ? parent.HeaderSettings.BottomExposureSeconds
                    : parent.HeaderSettings.LayerExposureSeconds;*/
            }

            public Mat Decode(uint layerIndex, bool consumeData = true)
            {
                var image = Parent.IsCbtFile ? DecodeCbtImage(layerIndex) : DecodeCbddlpImage(Parent, layerIndex);

                if (consumeData)
                    EncodedRle = null;

                return image;
            }

            public static Mat DecodeCbddlpImage(ChituboxFile parent, uint layerIndex)
            {
                var image = new Mat(new Size((int)parent.HeaderSettings.ResolutionX, (int)parent.HeaderSettings.ResolutionY), DepthType.Cv8U, 1);
                var span = image.GetPixelSpan<byte>();

                for (byte bit = 0; bit < parent.AntiAliasing; bit++)
                {
                    var layer = parent.LayersDefinitions[bit, layerIndex];

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

                        if (n == span.Length)
                        {
                            break;
                        }

                        if (n > span.Length)
                        {
                            image.Dispose();
                            throw new FileLoadException("Error image ran off the end");
                        }
                    }
                }

                for (int i = 0; i < span.Length; i++)
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

            private Mat DecodeCbtImage(uint layerIndex)
            {
                //Mat image = new Mat(new Size((int)Parent.HeaderSettings.ResolutionX, (int)Parent.HeaderSettings.ResolutionY), DepthType.Cv8U, 1);
                //var bytes = image.GetBytes();
                //var image = EmguExtensions.CreateMat(out var bytes, new Size((int)Parent.HeaderSettings.ResolutionX, (int)Parent.HeaderSettings.ResolutionY));
                var image = new Mat(new Size((int)Parent.HeaderSettings.ResolutionX, (int)Parent.HeaderSettings.ResolutionY), DepthType.Cv8U, 1);
                var span = image.GetPixelSpan<byte>();

                if (Parent.HeaderSettings.EncryptionKey > 0)
                {
                    KeyRing kr = new KeyRing(Parent.HeaderSettings.EncryptionKey, layerIndex);
                    EncodedRle = kr.Read(EncodedRle);
                }

                int pixel = 0;
                for (var n = 0; n < EncodedRle.Length; n++)
                {
                    byte code = EncodedRle[n];
                    uint stride = 1;

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
                            stride = (uint)(((slen & 0x3f) << 8) + EncodedRle[n + 1]);
                            n++;
                        }
                        else if ((slen & 0xe0) == 0xc0)
                        {
                            stride = (uint)(((slen & 0x1f) << 16) + (EncodedRle[n + 1] << 8) + EncodedRle[n + 2]);
                            n += 2;
                        }
                        else if ((slen & 0xf0) == 0xe0)
                        {
                            stride = (uint)(((slen & 0xf) << 24) + (EncodedRle[n + 1] << 16) + (EncodedRle[n + 2] << 8) + EncodedRle[n + 3]);

                            n += 3;
                        }
                        else
                        {
                            image.Dispose();
                            throw new FileLoadException("Corrupted RLE data");
                        }
                    }

                    // Bit extend from 7-bit to 8-bit greymap
                    if (code != 0)
                    {
                        code = (byte)((code << 1) | 1);
                    }

                    if (stride == 0) continue; // Nothing to do

                    if (code == 0) // Ignore blacks, spare cycles
                    {
                        pixel += (int)stride;
                        continue;
                    }

                    while (stride-- > 0)
                    {
                        span[pixel] = code;
                        pixel++;
                    }
                }

                return image;
            }

            public byte[] Encode(Mat image, byte aaIndex, uint layerIndex)
            {
                return Parent.IsCbtFile ? EncodeCbtImage(image, layerIndex) : EncodeCbddlpImage(image, aaIndex);
            }

            public byte[] EncodeCbddlpImage(Mat image, byte bit)
            {
                List<byte> rawData = new List<byte>();
                var span = image.GetPixelSpan<byte>();

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

                for (int pixel = 0; pixel < span.Length; pixel++)
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

            private byte[] EncodeCbtImage(Mat image, uint layerIndex)
            {
                List<byte> rawData = new List<byte>();
                byte color = byte.MaxValue >> 1;
                uint stride = 0;
                var span = image.GetPixelSpan<byte>();

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


                for (int pixel = 0; pixel < span.Length; pixel++)
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
                    KeyRing kr = new KeyRing(Parent.HeaderSettings.EncryptionKey, layerIndex);
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
                List<byte> data = new List<byte>(input.Count);
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

        public Header HeaderSettings { get; protected internal set; } = new Header();
        public PrintParameters PrintParametersSettings { get; protected internal set; } = new PrintParameters();

        public SlicerInfo SlicerInfoSettings { get; protected internal set; } = new SlicerInfo();

        public Preview[] Previews { get; protected internal set; }

        public LayerData[,] LayersDefinitions { get; private set; }

        public Dictionary<string, LayerData> LayersHash { get; } = new Dictionary<string, LayerData>();

        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new FileExtension("cbddlp", "Chitubox DLP Files"),
            new FileExtension("ctb", "Chitubox CTB Files"),
            new FileExtension("photon", "Chitubox Photon Files"),
        };

        public override Type[] ConvertToFormats { get; } =
        {
            typeof(ChituboxFile),
            typeof(ChituboxZipFile),
            typeof(PWSFile),
            typeof(PHZFile),
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

        public override Size[] ThumbnailsOriginalSize { get; } = {new Size(400, 300), new Size(200, 125)};

        public override uint ResolutionX => HeaderSettings.ResolutionX;

        public override uint ResolutionY => HeaderSettings.ResolutionY;
        public override byte AntiAliasing => (byte) (IsCbtFile ? SlicerInfoSettings.AntiAliasLevel : HeaderSettings.AntiAliasLevel);

        public override float LayerHeight
        {
            get => HeaderSettings.LayerHeightMilimeter;
            set => HeaderSettings.LayerHeightMilimeter = value;
        }

        public override uint LayerCount
        {
            set
            {
                HeaderSettings.LayerCount = LayerCount;
                HeaderSettings.OverallHeightMilimeter = TotalHeight;
            }
        }

        public override ushort InitialLayerCount => (ushort)HeaderSettings.BottomLayersCount;

        public override float InitialExposureTime => HeaderSettings.BottomExposureSeconds;

        public override float LayerExposureTime => HeaderSettings.LayerExposureSeconds;
        public override float LiftHeight => PrintParametersSettings.LiftHeight;
        public override float LiftSpeed => PrintParametersSettings.LiftSpeed;
        public override float RetractSpeed => PrintParametersSettings.RetractSpeed;

        public override float PrintTime => HeaderSettings.PrintTime;

        public override float UsedMaterial => (float) Math.Round(PrintParametersSettings.VolumeMl, 2);

        public override float MaterialCost => (float) Math.Round(PrintParametersSettings.CostDollars, 2);

        public override string MaterialName => "Unknown";
        public override string MachineName => SlicerInfoSettings.MachineName;
        
        public override object[] Configs => new[] { (object)HeaderSettings, PrintParametersSettings, SlicerInfoSettings };

        public bool IsCbddlpFile => HeaderSettings.Magic == MAGIC_CBDDLP;
        public bool IsCbtFile => HeaderSettings.Magic == MAGIC_CBT;
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

            LayersDefinitions = null;
        }

        public override void Encode(string fileFullPath, OperationProgress progress = null)
        {
            base.Encode(fileFullPath, progress);
            LayersHash.Clear();

            HeaderSettings.Magic = fileFullPath.EndsWith(".ctb") ? MAGIC_CBT : MAGIC_CBDDLP;
            HeaderSettings.PrintParametersSize = (uint)Helpers.Serializer.SizeOf(PrintParametersSettings);

            
            if (IsCbtFile)
            {
                SlicerInfoSettings.AntiAliasLevel = HeaderSettings.AntiAliasLevel;
                HeaderSettings.AntiAliasLevel = 1;
                PrintParametersSettings.Padding4 = 0x1234;
                //SlicerInfoSettings.EncryptionMode = 0xf;
                SlicerInfoSettings.EncryptionMode = 7;
                SlicerInfoSettings.MysteriousId = 0x12345678;
                SlicerInfoSettings.Unknown1 = 0x200;

                if (HeaderSettings.EncryptionKey == 0)
                {
                    Random rnd = new Random();
                    HeaderSettings.EncryptionKey = (uint)rnd.Next(byte.MaxValue, int.MaxValue);
                }
            }
            else
            {
                HeaderSettings.EncryptionKey = 0;
            }

            uint currentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
            LayersDefinitions = new LayerData[HeaderSettings.AntiAliasLevel, HeaderSettings.LayerCount];
            using (var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write))
            {

                outputFile.Seek((int) currentOffset, SeekOrigin.Begin);


                for (byte i = 0; i < ThumbnailsCount; i++)
                {
                    var image = Thumbnails[i];

                    Preview preview = new Preview
                    {
                        ResolutionX = (uint)image.Width,
                        ResolutionY = (uint)image.Height,
                    };

                    var previewBytes = preview.Encode(image);

                    if (previewBytes.Length == 0) continue;

                    if (i == (byte) FileThumbnailSize.Small)
                    {
                        HeaderSettings.PreviewSmallOffsetAddress = currentOffset;
                    }
                    else
                    {
                        HeaderSettings.PreviewLargeOffsetAddress = currentOffset;
                    }


                    currentOffset += (uint) Helpers.Serializer.SizeOf(preview);
                    preview.ImageOffset = currentOffset;

                    Helpers.SerializeWriteFileStream(outputFile, preview);
                    currentOffset += outputFile.WriteBytes(previewBytes);
                }


                if (HeaderSettings.Version >= 2)
                {
                    HeaderSettings.PrintParametersOffsetAddress = currentOffset;

                    currentOffset += Helpers.SerializeWriteFileStream(outputFile, PrintParametersSettings);

                    HeaderSettings.SlicerOffset = currentOffset;
                    HeaderSettings.SlicerSize = (uint) Helpers.Serializer.SizeOf(SlicerInfoSettings) - SlicerInfoSettings.MachineNameSize;

                    SlicerInfoSettings.MachineNameAddress = currentOffset + HeaderSettings.SlicerSize;


                    currentOffset += Helpers.SerializeWriteFileStream(outputFile, SlicerInfoSettings);
                }

                HeaderSettings.LayersDefinitionOffsetAddress = currentOffset;
                uint layerDataCurrentOffset = currentOffset + (uint)Helpers.Serializer.SizeOf(new LayerData()) * HeaderSettings.LayerCount * HeaderSettings.AntiAliasLevel;

                progress.ItemCount *= 2 * HeaderSettings.AntiAliasLevel;

                for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
                {
                    progress.Token.ThrowIfCancellationRequested();
                    Parallel.For(0, LayerCount, /*new ParallelOptions{MaxDegreeOfParallelism = 1},*/ layerIndex =>
                    {
                        if (progress.Token.IsCancellationRequested) return;
                        LayerData layerData = new LayerData(this, (uint) layerIndex);
                        using (var image = this[layerIndex].LayerMat)
                        {
                            layerData.Encode(image, aaIndex, (uint) layerIndex);
                            LayersDefinitions[aaIndex, layerIndex] = layerData;
                        }

                        lock (progress.Mutex)
                        {
                            progress++;
                        }
                    });

                    for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
                    {
                        progress.Token.ThrowIfCancellationRequested();
                        var layerData = LayersDefinitions[aaIndex, layerIndex];
                        LayerData layerDataHash = null;

                        if (!IsCbtFile /*&& HeaderSettings.EncryptionKey == 0*/)
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

                        outputFile.Seek(currentOffset, SeekOrigin.Begin);
                        currentOffset += Helpers.SerializeWriteFileStream(outputFile, layerData);

                        progress++;
                    }
                }

                outputFile.Seek(0, SeekOrigin.Begin);
                Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

                AfterEncode();

                Debug.WriteLine("Encode Results:");
                Debug.WriteLine(HeaderSettings);
                Debug.WriteLine(Previews[0]);
                Debug.WriteLine(Previews[1]);
                Debug.WriteLine(PrintParametersSettings);
                Debug.WriteLine(SlicerInfoSettings);
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
                if (HeaderSettings.Magic != MAGIC_CBDDLP && HeaderSettings.Magic != MAGIC_CBT)
                {
                    throw new FileLoadException("Not a valid CBDDLP nor CTB nor Photon file!", fileFullPath);
                }

                if (HeaderSettings.Version == 1 || HeaderSettings.AntiAliasLevel == 0)
                {
                    HeaderSettings.AntiAliasLevel = 1;
                }

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

                LayersDefinitions = new LayerData[HeaderSettings.AntiAliasLevel, HeaderSettings.LayerCount];

                uint layerOffset = HeaderSettings.LayersDefinitionOffsetAddress;

                progress.Reset(OperationProgress.StatusGatherLayers,
                    HeaderSettings.AntiAliasLevel * HeaderSettings.LayerCount);

                for (byte aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
                {
                    Debug.WriteLine($"-Image GROUP {aaIndex}-");
                    for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                    {
                        inputFile.Seek(layerOffset, SeekOrigin.Begin);
                        LayerData layerData = Helpers.Deserialize<LayerData>(inputFile);
                        layerData.Parent = this;
                        LayersDefinitions[aaIndex, layerIndex] = layerData;

                        layerOffset += (uint) Helpers.Serializer.SizeOf(layerData);
                        //Debug.Write($"LAYER {layerIndex} -> ");
                        //Debug.WriteLine(layerData);

                        layerData.EncodedRle = new byte[layerData.DataSize];
                        inputFile.Seek(layerData.DataAddress, SeekOrigin.Begin);
                        inputFile.Read(layerData.EncodedRle, 0, (int) layerData.DataSize);
                        progress++;
                        progress.Token.ThrowIfCancellationRequested();
                    }
                }

                LayerManager = new LayerManager(HeaderSettings.LayerCount, this);

                progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

                Parallel.For(0, LayerCount, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested)
                    {
                        return;
                    }

                    using (var image = LayersDefinitions[0, layerIndex].Decode((uint) layerIndex))
                    {
                        this[layerIndex] = new Layer((uint) layerIndex, image)
                        {
                            PositionZ = LayersDefinitions[0, layerIndex].LayerPositionZ,
                            ExposureTime = LayersDefinitions[0, layerIndex].LayerExposure
                        };

                        lock (progress.Mutex)
                        {
                            progress++;
                        }
                    }
                });
            }

            progress.Token.ThrowIfCancellationRequested();
        }

        public override object GetValueFromPrintParameterModifier(PrintParameterModifier modifier)
        {
            var baseValue = base.GetValueFromPrintParameterModifier(modifier);
            if (!ReferenceEquals(baseValue, null)) return baseValue;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLayerOffTime)) return PrintParametersSettings.BottomLightOffDelay;
            if (ReferenceEquals(modifier, PrintParameterModifier.LayerOffTime)) return PrintParametersSettings.LightOffDelay;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight)) return PrintParametersSettings.BottomLiftHeight;
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed)) return PrintParametersSettings.BottomLiftSpeed;
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
                        this[layerIndex].ExposureTime =
                            LayersDefinitions[aaIndex, layerIndex].LayerExposure = GetInitialLayerValueOrNormal(layerIndex, HeaderSettings.BottomExposureSeconds, HeaderSettings.LayerExposureSeconds);

                        LayersDefinitions[aaIndex, layerIndex].LayerOffTimeSeconds = GetInitialLayerValueOrNormal(layerIndex, PrintParametersSettings.BottomLightOffDelay, HeaderSettings.LayerOffTime);
                    }
                }
            }

            if (ReferenceEquals(modifier, PrintParameterModifier.InitialLayerCount))
            {
                HeaderSettings.BottomLayersCount =
                PrintParametersSettings.BottomLayerCount = value.Convert<uint>();
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
                PrintParametersSettings.BottomLightOffDelay = value.Convert<float>();
                UpdateLayers();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LayerOffTime))
            {
                HeaderSettings.LayerOffTime =
                PrintParametersSettings.LightOffDelay = value.Convert<float>();
                UpdateLayers();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftHeight))
            {
                PrintParametersSettings.BottomLiftHeight = value.Convert<float>();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.BottomLiftSpeed))
            {
                PrintParametersSettings.BottomLiftSpeed = value.Convert<float>();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftHeight))
            {
                PrintParametersSettings.LiftHeight = value.Convert<float>();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.LiftSpeed))
            {
                PrintParametersSettings.LiftSpeed = value.Convert<float>();
                return true;
            }
            if (ReferenceEquals(modifier, PrintParameterModifier.RetractSpeed))
            {
                PrintParametersSettings.RetractSpeed = value.Convert<float>();
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
                        outputFile.Seek(layerOffset, SeekOrigin.Begin);
                        layerOffset += Helpers.SerializeWriteFileStream(outputFile, LayersDefinitions[aaIndex, layerIndex]);
                    }
                }
            }

            //Decode(FileFullPath, progress);
        }

        public override bool Convert(Type to, string fileFullPath, OperationProgress progress = null)
        {
            if (to == typeof(ChituboxFile))
            {
                if (Path.GetExtension(FileFullPath).Equals(Path.GetExtension(fileFullPath)))
                {
                    return false;
                }
                ChituboxFile file = new ChituboxFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
                    {
                        ResolutionX = ResolutionX,
                        ResolutionY = ResolutionY,
                        BedSizeX = HeaderSettings.BedSizeX,
                        BedSizeY = HeaderSettings.BedSizeY,
                        BedSizeZ = HeaderSettings.BedSizeZ,
                        ProjectorType = HeaderSettings.ProjectorType,
                        LayerCount = LayerCount,
                        AntiAliasLevel = ValidateAntiAliasingLevel(),
                        BottomLightPWM = (byte) HeaderSettings.BottomLightPWM,
                        LightPWM = (byte) HeaderSettings.LightPWM,
                        LayerOffTime = HeaderSettings.LayerOffTime,
                        PrintTime = HeaderSettings.PrintTime,
                        BottomExposureSeconds = HeaderSettings.BottomExposureSeconds,
                        BottomLayersCount = HeaderSettings.BottomLayersCount,
                        //EncryptionKey = HeaderSettings.EncryptionKey,
                        LayerExposureSeconds = HeaderSettings.LayerExposureSeconds,
                        LayerHeightMilimeter = HeaderSettings.LayerHeightMilimeter,
                        OverallHeightMilimeter = HeaderSettings.OverallHeightMilimeter,
                    },
                    PrintParametersSettings =
                    {
                        LiftSpeed = PrintParametersSettings.LiftSpeed,
                        LiftHeight = PrintParametersSettings.LiftHeight,
                        BottomLiftSpeed = PrintParametersSettings.BottomLiftSpeed,
                        RetractSpeed = PrintParametersSettings.RetractSpeed,
                        BottomLightOffDelay = PrintParametersSettings.BottomLightOffDelay,
                        LightOffDelay = PrintParametersSettings.BottomLightOffDelay,
                        BottomLayerCount = PrintParametersSettings.BottomLayerCount,
                        VolumeMl = PrintParametersSettings.VolumeMl,
                        BottomLiftHeight = PrintParametersSettings.BottomLiftHeight,
                        CostDollars = PrintParametersSettings.CostDollars,
                        WeightG = PrintParametersSettings.WeightG
                    },
                    SlicerInfoSettings =
                    {
                        AntiAliasLevel = SlicerInfoSettings.AntiAliasLevel,
                        MachineName = SlicerInfoSettings.MachineName,
                        //EncryptionMode = SlicerInfoSettings.EncryptionMode,
                        MachineNameSize = SlicerInfoSettings.MachineNameSize,
                    },
                    Thumbnails = Thumbnails,
                };

                //file.SetThumbnails(Thumbnails);
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
                        Weight = PrintParametersSettings.WeightG,
                        Volume = UsedMaterial,
                        Mirror = (byte)  (HeaderSettings.ProjectorType == 0 ? 0 : 1),


                        BottomLiftHeight = PrintParametersSettings.BottomLiftHeight,
                        LiftHeight = PrintParametersSettings.LiftHeight,
                        BottomLiftSpeed = PrintParametersSettings.BottomLiftSpeed,
                        LiftSpeed = PrintParametersSettings.LiftSpeed,
                        RetractSpeed = PrintParametersSettings.RetractSpeed,
                        BottomLayCount = InitialLayerCount,
                        BottomLayerCount = InitialLayerCount,
                        BottomLightOffTime = PrintParametersSettings.BottomLightOffDelay,
                        LightOffTime = PrintParametersSettings.LightOffDelay,
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
                        Weight = PrintParametersSettings.WeightG,
                        AntiAliasing = ValidateAntiAliasingLevel()
                    }
                };

                file.SetThumbnails(Thumbnails);
                file.Encode(fileFullPath, progress);

                return true;
            }

            if (to == typeof(PHZFile))
            {
                PHZFile file = new PHZFile
                {
                    LayerManager = LayerManager,
                    HeaderSettings =
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
                        BottomLayerCount = InitialLayerCount,
                        BottomLiftHeight = PrintParametersSettings.BottomLiftHeight,
                        BottomLiftSpeed = PrintParametersSettings.BottomLiftSpeed,
                        BottomLightOffDelay = PrintParametersSettings.BottomLightOffDelay,
                        CostDollars = MaterialCost,
                        LiftHeight = PrintParametersSettings.LiftHeight,
                        LiftSpeed = PrintParametersSettings.LiftSpeed,
                        RetractSpeed = PrintParametersSettings.RetractSpeed,
                        VolumeMl = UsedMaterial,
                        AntiAliasLevelInfo = ValidateAntiAliasingLevel(),
                        WeightG = PrintParametersSettings.WeightG,
                        MachineName = MachineName,
                        MachineNameSize = (uint)MachineName.Length
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
                        AntiAliasing = (byte)(ValidateAntiAliasingLevel() > 1 ? 1 : 0),
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
                        ZLiftFeedRate = PrintParametersSettings.LiftSpeed,
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
                file.SliceSettings.WaitBeforeExpoMs = (uint)(PrintParametersSettings.LightOffDelay * 1000);
                file.SliceSettings.LiftDistance = file.OutputSettings.LiftDistance = LiftHeight;
                file.SliceSettings.LiftUpSpeed = file.OutputSettings.ZLiftFeedRate = LiftSpeed;
                file.SliceSettings.LiftDownSpeed = file.OutputSettings.ZLiftRetractRate = RetractSpeed;
                file.SliceSettings.LiftWhenFinished = defaultFormat.SliceSettings.LiftWhenFinished;

                file.OutputSettings.BlankingLayerTime = (uint) (PrintParametersSettings.LightOffDelay * 1000);
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
                                LiftHeight = PrintParametersSettings.BottomLiftHeight,
                                LiftSpeed = PrintParametersSettings.BottomLiftSpeed,
                                LightOnTime = InitialExposureTime,
                                LightOffTime = PrintParametersSettings.BottomLightOffDelay,
                                LightPWM = (byte) HeaderSettings.BottomLightPWM,
                                RetractSpeed = PrintParametersSettings.RetractSpeed,
                                Count = InitialLayerCount
                                //RetractHeight = LookupCustomValue<float>(Keyword_LiftHeight, defaultFormat.JsonSettings.Properties.Bottom.RetractHeight),
                            },
                            Exposure = new UVJFile.Exposure
                            {
                                LiftHeight = PrintParametersSettings.LiftHeight,
                                LiftSpeed = PrintParametersSettings.LiftSpeed,
                                LightOnTime = LayerExposureTime,
                                LightOffTime = PrintParametersSettings.LightOffDelay,
                                LightPWM = (byte) HeaderSettings.LightPWM,
                                RetractSpeed = PrintParametersSettings.RetractSpeed,
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
