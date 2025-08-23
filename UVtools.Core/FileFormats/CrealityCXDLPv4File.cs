/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using ZLinq;

namespace UVtools.Core.FileFormats;

public sealed class CrealityCXDLPv4File : FileFormat
{

    #region Constants
    private const byte MAGIC_SIZE = 9; // CXSW3DV2
    private const string MAGIC_VALUE = "CXSW3DV2";
    private const string HEADER_VALUE_GENERIC = "CXSW3D";

    public const byte DEFAULT_VERSION = 4;

    #endregion

    #region Sub Classes
    #region Header
    public class Header
    {
        /// <summary>
        /// Gets the size of the magic
        /// </summary>
        [FieldOrder(0)]
        [FieldEndianness(Endianness.Big)]
        public uint MagicSize { get; set; } = MAGIC_SIZE;

        /// <summary>
        /// Gets the magic name
        /// </summary>
        [FieldOrder(1)]
        [FieldLength(MAGIC_SIZE)]
        [SerializeAs(SerializedType.TerminatedString)]
        public string Magic { get; set; } = MAGIC_VALUE;

        [FieldOrder(2)]
        [FieldEndianness(Endianness.Big)]
        public ushort Version { get; set; } = DEFAULT_VERSION;


        /// <summary>
        /// Gets the printer model
        /// </summary>
        [FieldOrder(3)]
        public NullTerminatedUintStringBigEndian PrinterModel { get; set; } = string.Empty;

        /// <summary>
        /// Gets the printer resolution along X axis, in pixels. This information is critical to correctly decoding layer images.
        /// </summary>
        [FieldOrder(4)] public ushort ResolutionX { get; set; }

        /// <summary>
        /// Gets the printer resolution along Y axis, in pixels. This information is critical to correctly decoding layer images.
        /// </summary>
        [FieldOrder(5)] public ushort ResolutionY { get; set; }

        /// <summary>
        /// Gets dimensions of the printer’s X output volume, in millimeters.
        /// </summary>
        [FieldOrder(6)]  public float BedSizeX { get; set; }

        /// <summary>
        /// Gets dimensions of the printer’s Y output volume, in millimeters.
        /// </summary>
        [FieldOrder(7)]  public float BedSizeY { get; set; }

        /// <summary>
        /// Gets dimensions of the printer’s Z output volume, in millimeters.
        /// </summary>
        [FieldOrder(8)]  public float BedSizeZ { get; set; }

        /// <summary>
        /// Gets the height of the model described by this file, in millimeters.
        /// </summary>
        [FieldOrder(9)]  public float PrintHeight { get; set; }

        /// <summary>
        /// Gets the layer height setting used at slicing, in millimeters. Actual height used by the machine is in the layer table.
        /// </summary>
        [FieldOrder(10)]  public float LayerHeight  { get; set; }

        /// <summary>
        /// Gets number of layers configured as "bottom." Note that this field appears in both the file header and ExtConfig..
        /// </summary>
        [FieldOrder(11)] public uint BottomLayersCount { get; set; } = DefaultBottomLayerCount;

        /// <summary>
        /// Gets the file offsets of ImageHeader records describing the smaller preview images.
        /// </summary>
        [FieldOrder(12)] public uint PreviewSmallOffsetAddress { get; set; }

        /// <summary>
        /// Gets the file offset of a table of LayerHeader records giving parameters for each printed layer.
        /// </summary>
        [FieldOrder(13)] public uint LayersDefinitionOffsetAddress { get; set; }

        /// <summary>
        /// Gets the number of records in the layer table for the first level set. In ctb files, that’s equivalent to the total number of records, but records may be multiplied in antialiased cbddlp files.
        /// </summary>
        [FieldOrder(14)] public uint LayerCount { get; set; }

        /// <summary>
        /// Gets the file offsets of ImageHeader records describing the larger preview images.
        /// </summary>
        [FieldOrder(15)] public uint PreviewLargeOffsetAddress { get; set; }

        /// <summary>
        /// Gets the estimated duration of print, in seconds.
        /// </summary>
        [FieldOrder(16)] public uint PrintTime { get; set; }

        /// <summary>
        /// Gets the records whether this file was generated assuming normal (0) or mirrored (1) image projection. LCD printers are "mirrored" for this purpose.
        /// </summary>
        [FieldOrder(17)] public uint ProjectorType { get; set; }

        /// <summary>
        /// Gets the print parameters table offset
        /// </summary>
        [FieldOrder(18)] public uint PrintParametersOffsetAddress { get; set; }

        /// <summary>
        /// Gets the print parameters table size in bytes.
        /// </summary>
        [FieldOrder(19)] public uint PrintParametersSize { get; set; }

        [FieldOrder(20)] public uint AntiAliasLevel { get; set; } = 1;

        /// <summary>
        /// Gets the PWM duty cycle for the UV illumination source on normal levels, respectively.
        /// This appears to be an 8-bit quantity where 0xFF is fully on and 0x00 is fully off.
        /// </summary>
        [FieldOrder(21)] public ushort LightPWM { get; set; } = DefaultLightPWM;

        /// <summary>
        /// Gets the PWM duty cycle for the UV illumination source on bottom levels, respectively.
        /// This appears to be an 8-bit quantity where 0xFF is fully on and 0x00 is fully off.
        /// </summary>
        [FieldOrder(22)] public ushort BottomLightPWM { get; set; } = DefaultBottomLightPWM;

        /// <summary>
        /// Gets the key used to encrypt layer data, or 0 if encryption is not used.
        /// </summary>
        [FieldOrder(23)] public uint EncryptionKey { get; set; }

        /// <summary>
        /// Gets the slicer tablet address
        /// </summary>
        [FieldOrder(24)] public uint SlicerAddress { get; set; }

        /// <summary>
        /// Gets the slicer table size in bytes
        /// </summary>
        [FieldOrder(25)] public uint SlicerSize { get; set; }

        public override string ToString()
        {
            return $"{nameof(MagicSize)}: {MagicSize}, {nameof(Magic)}: {Magic}, {nameof(Version)}: {Version}, {nameof(PrinterModel)}: {PrinterModel}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(BedSizeX)}: {BedSizeX}, {nameof(BedSizeY)}: {BedSizeY}, {nameof(BedSizeZ)}: {BedSizeZ}, {nameof(PrintHeight)}: {PrintHeight}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(PreviewSmallOffsetAddress)}: {PreviewSmallOffsetAddress}, {nameof(LayersDefinitionOffsetAddress)}: {LayersDefinitionOffsetAddress}, {nameof(LayerCount)}: {LayerCount}, {nameof(PreviewLargeOffsetAddress)}: {PreviewLargeOffsetAddress}, {nameof(PrintTime)}: {PrintTime}, {nameof(ProjectorType)}: {ProjectorType}, {nameof(PrintParametersOffsetAddress)}: {PrintParametersOffsetAddress}, {nameof(PrintParametersSize)}: {PrintParametersSize}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(LightPWM)}: {LightPWM}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(EncryptionKey)}: {EncryptionKey}, {nameof(SlicerAddress)}: {SlicerAddress}, {nameof(SlicerSize)}: {SlicerSize}";
        }

        public void Validate()
        {
            if (MagicSize - 1 != Magic.Length || !Magic.StartsWith(HEADER_VALUE_GENERIC))
            {
                throw new FileLoadException("Invalid header data for CXDLPv4 file.");
            }
        }
    }
    #endregion

    #region PrintParameters
    public class PrintParameters
    {
        /// <summary>
        /// Gets the distance to lift the build platform away from the vat after bottom layers, in millimeters.
        /// </summary>
        [FieldOrder(0)] public float BottomLiftHeight { get; set; } = DefaultBottomLiftHeight;

        /// <summary>
        /// Gets the speed at which to lift the build platform away from the vat after bottom layers, in millimeters per minute.
        /// </summary>
        [FieldOrder(1)]  public float BottomLiftSpeed     { get; set; } = DefaultBottomLiftSpeed;

        /// <summary>
        /// Gets the distance to lift the build platform away from the vat after normal layers, in millimeters.
        /// </summary>
        [FieldOrder(2)]  public float LiftHeight          { get; set; } = DefaultLayerHeight;

        /// <summary>
        /// Gets the speed at which to lift the build platform away from the vat after normal layers, in millimeters per minute.
        /// </summary>
        [FieldOrder(3)]  public float LiftSpeed        { get; set; } = DefaultLiftSpeed;

        /// <summary>
        /// Gets the speed to use when the build platform re-approaches the vat after lift, in millimeters per minute.
        /// </summary>
        [FieldOrder(4)]  public float RetractSpeed        { get; set; } = DefaultRetractSpeed;

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
        [FieldOrder(8)]  public float BottomLightOffDelay { get; set; }

        /// <summary>
        /// Gets the light off time setting used at slicing, for normal layers, in seconds. Actual time used by the machine is in the layer table. Note that light_off_time_s appears in both the file header and ExtConfig.
        /// </summary>
        [FieldOrder(9)]  public float LightOffDelay       { get; set; }

        /// <summary>
        /// Gets number of layers configured as "bottom." Note that this field appears in both the file header and ExtConfig.
        /// </summary>
        [FieldOrder(10)] public uint BottomLayerCount     { get; set; } = DefaultBottomLayerCount;
        [FieldOrder(11)] public float ExposureTime        { get; set; } = DefaultExposureTime;
        [FieldOrder(12)] public float BottomExposureTime  { get; set; } = DefaultBottomExposureTime;
        [FieldOrder(13)] public uint Padding1             { get; set; }
        [FieldOrder(14)] public uint Padding2             { get; set; }
        [FieldOrder(15)] public uint Padding3             { get; set; }
        [FieldOrder(16)] public uint Padding4             { get; set; }

        public override string ToString()
        {
            return $"{nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(VolumeMl)}: {VolumeMl}, {nameof(WeightG)}: {WeightG}, {nameof(CostDollars)}: {CostDollars}, {nameof(BottomLightOffDelay)}: {BottomLightOffDelay}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(BottomExposureTime)}: {BottomExposureTime}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(Padding3)}: {Padding3}, {nameof(Padding4)}: {Padding4}";
        }
    }
    #endregion

    #region SlicerInfo

    public class SlicerInfo
    {
        [FieldOrder(0)] public float BottomLiftHeight2 { get; set; }
        [FieldOrder(1)] public float BottomLiftSpeed2  { get; set; }
        [FieldOrder(2)] public float LiftHeight2       { get; set; }
        [FieldOrder(3)] public float LiftSpeed2        { get; set; }
        [FieldOrder(4)] public float RetractHeight2    { get; set; }
        [FieldOrder(5)] public float RetractSpeed2     { get; set; }
        [FieldOrder(6)] public float RestTimeAfterLift { get; set; }

        /// <summary>
        /// Enable per layer settings, true or false
        /// </summary>
        [FieldOrder(7)] [SerializeAs(SerializedType.UInt4)] public bool PerLayerSettings { get; set; }

        /// <summary>
        /// Gets the minutes since Jan 1, 1970 UTC
        /// </summary>
        [FieldOrder(8)] public uint ModifiedTimestampMinutes { get; set; } = (uint)DateTimeExtensions.Timestamp.TotalMinutes;

        [Ignore] public string ModifiedDate => DateTimeExtensions.GetDateTimeFromTimestampMinutes(ModifiedTimestampMinutes).ToString("dd/MM/yyyy HH:mm");

        /// <summary>
        /// Gets the user-selected antialiasing level.
        /// </summary>
        [FieldOrder(9)] public uint AntiAliasLevel { get; set; } = 1;

        /// <summary>
        /// Gets a version of software that generated this file, encoded with major, minor, and patch release in bytes starting from the MSB down.
        /// (No provision is made to name the software being used, so this assumes that only one software package can generate the files.
        /// Probably best to hardcode it at 0x01060300.)
        /// </summary>17170480
        [FieldOrder(10)] public uint SoftwareVersion { get; set; } = 0;
        [FieldOrder(11)] public float RestTimeAfterRetract { get; set; }
        [FieldOrder(12)] public float RestTimeBeforeLift   { get; set; }
        [FieldOrder(13)] public float ExposureTime { get; set; } = DefaultExposureTime;
        [FieldOrder(14)] public float BottomExposureTime { get; set; } = DefaultBottomExposureTime;
        [FieldOrder(15)] public float RestTimeAfterLift2 { get; set; } = DefaultBottomExposureTime;
        [FieldOrder(16)] public uint TransitionLayerCount { get; set; }
        [FieldOrder(17)] public uint Padding1         { get; set; }
        [FieldOrder(18)] public uint Padding2         { get; set; }


        public override string ToString()
        {
            return $"{nameof(BottomLiftHeight2)}: {BottomLiftHeight2}, {nameof(BottomLiftSpeed2)}: {BottomLiftSpeed2}, {nameof(LiftHeight2)}: {LiftHeight2}, {nameof(LiftSpeed2)}: {LiftSpeed2}, {nameof(RetractHeight2)}: {RetractHeight2}, {nameof(RetractSpeed2)}: {RetractSpeed2}, {nameof(RestTimeAfterLift)}: {RestTimeAfterLift}, {nameof(PerLayerSettings)}: {PerLayerSettings}, {nameof(ModifiedTimestampMinutes)}: {ModifiedTimestampMinutes}, {nameof(ModifiedDate)}: {ModifiedDate}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(SoftwareVersion)}: {SoftwareVersion}, {nameof(RestTimeAfterRetract)}: {RestTimeAfterRetract}, {nameof(RestTimeBeforeLift)}: {RestTimeBeforeLift}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(BottomExposureTime)}: {BottomExposureTime}, {nameof(RestTimeAfterLift2)}: {RestTimeAfterLift2}, {nameof(TransitionLayerCount)}: {TransitionLayerCount}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}";
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
    public class LayerDef
    {
        /// <summary>
        /// Gets the build platform Z position for this layer, measured in millimeters.
        /// </summary>
        [FieldOrder(0)] public float PositionZ       { get; set; }

        /// <summary>
        /// Gets the exposure time for this layer, in seconds.
        /// </summary>
        [FieldOrder(1)] public float ExposureTime    { get; set; }

        /// <summary>
        /// Gets how long to keep the light off after exposing this layer, in seconds.
        /// </summary>
        [FieldOrder(2)] public float LightOffSeconds { get; set; }

        /// <summary>
        /// Gets the layer image offset to encoded layer data, and its length in bytes.
        /// </summary>
        [FieldOrder(3)] public uint DataAddress      { get; set; }

        /// <summary>
        /// Gets the layer image length in bytes.
        /// </summary>
        [FieldOrder(4)] public uint DataSize         { get; set; }
        [FieldOrder(5)] public uint DataType         { get; set; }
        [FieldOrder(6)] public uint CentroidDistance { get; set; }
        [FieldOrder(7)] public uint LargestArea        { get; set; }
        [FieldOrder(8)] public uint Unknown1         { get; set; }
        [FieldOrder(9)] public uint Unknown2         { get; set; }


        public LayerDef() { }

        public LayerDef(Layer layer)
        {
            SetFrom(layer);
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

        public override string ToString()
        {
            return $"{nameof(PositionZ)}: {PositionZ}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(LightOffSeconds)}: {LightOffSeconds}, {nameof(DataAddress)}: {DataAddress}, {nameof(DataSize)}: {DataSize}, {nameof(DataType)}: {DataType}, {nameof(CentroidDistance)}: {CentroidDistance}, {nameof(LargestArea)}: {LargestArea}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}";
        }
    }

    public class LayerDefEx
    {
        [FieldOrder(1)] public float LiftHeight { get; set; }
        [FieldOrder(2)] public float LiftSpeed { get; set; }
        [FieldOrder(3)] public float LiftHeight2 { get; set; }
        [FieldOrder(4)] public float LiftSpeed2 { get; set; }
        [FieldOrder(5)] public float RetractSpeed { get; set; }
        [FieldOrder(6)] public float RetractHeight2 { get; set; }
        [FieldOrder(7)] public float RetractSpeed2 { get; set; }
        [FieldOrder(8)] public float RestTimeBeforeLift { get; set; }
        [FieldOrder(9)] public float RestTimeAfterLift { get; set; }
        [FieldOrder(10)] public float RestTimeAfterRetract { get; set; } // 28672 v3?
        [FieldOrder(11)] public float LightPWM { get; set; } = DefaultLightPWM;

        [Ignore] public byte[]? EncodedRle { get; set; }

        public LayerDefEx() { }

        public LayerDefEx(Layer layer)
        {
            SetFrom(layer);
        }

        public void SetFrom(Layer layer)
        {
            LiftHeight = layer.LiftHeight;
            LiftSpeed = layer.LiftSpeed;
            RetractSpeed = layer.RetractSpeed;
            LightPWM = layer.LightPWM;

            LiftHeight += layer.LiftHeight2;
            LiftHeight2 = layer.LiftHeight2;
            LiftSpeed2 = layer.LiftSpeed2;

            RetractHeight2 = layer.RetractHeight2;
            RetractSpeed2 = layer.RetractSpeed2;

            RestTimeAfterRetract = layer.WaitTimeBeforeCure;
            RestTimeBeforeLift = layer.WaitTimeAfterCure;
            RestTimeAfterLift = layer.WaitTimeAfterLift;
        }

        public void CopyTo(Layer layer)
        {
            layer.LiftHeight = LiftHeight;
            layer.LiftSpeed = LiftSpeed;
            layer.RetractSpeed = RetractSpeed;
            layer.LightPWM = (byte)LightPWM;

            layer.LiftHeight -= LiftHeight2;
            layer.LiftHeight2 = LiftHeight2;
            layer.LiftSpeed2 = LiftSpeed2;

            layer.RetractHeight2 = RetractHeight2;
            layer.RetractSpeed2 = RetractSpeed2;

            layer.WaitTimeBeforeCure = RestTimeAfterRetract;
            layer.WaitTimeAfterCure = RestTimeBeforeLift;
            layer.WaitTimeAfterLift = RestTimeAfterLift;
        }

        public Mat Decode(CrealityCXDLPv4File parent, LayerDef layerDef, uint layerIndex)
        {
            if (layerDef.DataType > 0)
            {
                throw new NotImplementedException($"Layer {layerIndex} have a data type of {layerDef.DataType} which is not implemented, please provide this file to developer.");
            }

            var mat = parent.CreateMat();

            if (parent.HeaderSettings.EncryptionKey > 0)
            {
                LayerRleCryptBuffer(parent.HeaderSettings.EncryptionKey, layerIndex, EncodedRle!);
            }

            int pixel = 0;
            for (var n = 0; n < EncodedRle!.Length; n++)
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

        public unsafe byte[] Encode(CrealityCXDLPv4File parent, Mat image, uint layerIndex)
        {
            List<byte> rawData = [];
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

            EncodedRle = parent.HeaderSettings.EncryptionKey > 0
                ? LayerRleCrypt(parent.HeaderSettings.EncryptionKey, layerIndex, rawData)
                : rawData.ToArray();

            return EncodedRle;
        }

        public override string ToString()
        {
            return $"{nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(LiftHeight2)}: {LiftHeight2}, {nameof(LiftSpeed2)}: {LiftSpeed2}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(RetractHeight2)}: {RetractHeight2}, {nameof(RetractSpeed2)}: {RetractSpeed2}, {nameof(RestTimeBeforeLift)}: {RestTimeBeforeLift}, {nameof(RestTimeAfterLift)}: {RestTimeAfterLift}, {nameof(RestTimeAfterRetract)}: {RestTimeAfterRetract}, {nameof(LightPWM)}: {LightPWM}";
        }
    }

    #endregion

    #endregion

    #region Properties

    public Header HeaderSettings { get; private set; } = new();
    public PrintParameters PrintParametersSettings { get; private set; } = new();

    public SlicerInfo SlicerInfoSettings { get; private set; } = new();

    public Preview[] Previews { get; }

    public LayerDef[] LayerDefinitions { get; private set; } = [];
    public LayerDefEx[] LayerDefinitionsEx { get; private set; } = [];

    public override FileFormatType FileType => FileFormatType.Binary;

    public override string ConvertMenuGroup => "Creality CXDLP";

    public override FileExtension[] FileExtensions { get; } =
    [
        new(typeof(CrealityCXDLPv4File), "cxdlpv4", "Creality CXDLPv4")
    ];

    public override PrintParameterModifier[] PrintParameterModifiers =>
    [

        PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.TransitionLayerCount,

            PrintParameterModifier.BottomLightOffDelay,
            PrintParameterModifier.LightOffDelay,

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
            PrintParameterModifier.LightPWM
    ];


    public override PrintParameterModifier[] PrintParameterPerLayerModifiers
    {
        get
        {
            if (!IsPerLayerSettingsAllowed) return base.PrintParameterPerLayerModifiers;

            return [
                PrintParameterModifier.PositionZ,
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
            ];
        }
    }



    public override Size[] ThumbnailsOriginalSize { get; } =
    [
        new(120, 120),
        new(300, 300)
    ];


    public override uint DefaultVersion => DEFAULT_VERSION;

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
        set => base.ResolutionX = HeaderSettings.ResolutionX = (ushort)value;
    }

    public override uint ResolutionY
    {
        get => HeaderSettings.ResolutionY;
        set => base.ResolutionY = HeaderSettings.ResolutionY = (ushort)value;
    }

    public override float DisplayWidth
    {
        get => HeaderSettings.BedSizeX;
        set => base.DisplayWidth = HeaderSettings.BedSizeX = RoundDisplaySize(value);
    }

    public override float DisplayHeight
    {
        get => HeaderSettings.BedSizeY;
        set => base.DisplayHeight = HeaderSettings.BedSizeY = RoundDisplaySize(value);
    }

    public override float MachineZ
    {
        get => HeaderSettings.BedSizeZ > 0 ? HeaderSettings.BedSizeZ : base.MachineZ;
        set => base.MachineZ = HeaderSettings.BedSizeZ = MathF.Round(value, 2);
    }

    public override FlipDirection DisplayMirror
    {
        get => HeaderSettings.ProjectorType == 0 ? FlipDirection.None : FlipDirection.Horizontally;
        set
        {
            HeaderSettings.ProjectorType = value == FlipDirection.None ? 0u : 1;
            RaisePropertyChanged();
        }
    }

    public override byte AntiAliasing
    {
        get => (byte) HeaderSettings.AntiAliasLevel;
        set => base.AntiAliasing = (byte)(SlicerInfoSettings.AntiAliasLevel = Math.Clamp(value, 1u, 16u));
    }

    public override float LayerHeight
    {
        get => HeaderSettings.LayerHeight;
        set => base.LayerHeight = HeaderSettings.LayerHeight = Layer.RoundHeight(value);
    }

    public override float PrintHeight
    {
        get => base.PrintHeight;
        set => base.PrintHeight = HeaderSettings.PrintHeight = base.PrintHeight;
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = HeaderSettings.LayerCount = base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => (ushort) HeaderSettings.BottomLayersCount;
        set => base.BottomLayerCount = (ushort) (HeaderSettings.BottomLayersCount = PrintParametersSettings.BottomLayerCount = value);
    }

    public override ushort TransitionLayerCount
    {
        get => (ushort)SlicerInfoSettings.TransitionLayerCount;
        set => base.TransitionLayerCount = (ushort)(SlicerInfoSettings.TransitionLayerCount = Math.Min(value, MaximumPossibleTransitionLayerCount));
    }

    public override float BottomLightOffDelay
    {
        get => PrintParametersSettings.BottomLightOffDelay;
        set
        {
            base.BottomLightOffDelay = PrintParametersSettings.BottomLightOffDelay = MathF.Round(value, 2);
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
        get => PrintParametersSettings.LightOffDelay;
        set
        {
            base.LightOffDelay = PrintParametersSettings.LightOffDelay = PrintParametersSettings.LightOffDelay = MathF.Round(value, 2);
            if (value > 0)
            {
                WaitTimeBeforeCure = 0;
                WaitTimeAfterCure = 0;
                WaitTimeAfterLift = 0;
            }
        }
    }

    public override float WaitTimeBeforeCure
    {
        get => SlicerInfoSettings.RestTimeAfterRetract;
        set
        {
            base.WaitTimeBeforeCure = SlicerInfoSettings.RestTimeAfterRetract = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
        }
    }

    public override float BottomExposureTime
    {
        get => PrintParametersSettings.BottomExposureTime;
        set => base.BottomExposureTime = PrintParametersSettings.BottomExposureTime = SlicerInfoSettings.BottomExposureTime = MathF.Round(value, 2);
    }

    public override float WaitTimeAfterCure
    {
        get => SlicerInfoSettings.RestTimeBeforeLift;
        set
        {
            base.WaitTimeAfterCure = SlicerInfoSettings.RestTimeBeforeLift = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
        }
    }

    public override float ExposureTime
    {
        get => PrintParametersSettings.ExposureTime;
        set => base.ExposureTime = PrintParametersSettings.ExposureTime = SlicerInfoSettings.ExposureTime = MathF.Round(value, 2);
    }

    public override float BottomLiftHeight
    {
        get => MathF.Round(Math.Max(0, PrintParametersSettings.BottomLiftHeight - SlicerInfoSettings.BottomLiftHeight2), 2);
        set
        {
            value = MathF.Round(value, 2);
            PrintParametersSettings.BottomLiftHeight = MathF.Round(value + SlicerInfoSettings.BottomLiftHeight2, 2);
            base.BottomLiftHeight = value;
        }
    }

    public override float BottomLiftSpeed
    {
        get => PrintParametersSettings.BottomLiftSpeed;
        set => base.BottomLiftSpeed = PrintParametersSettings.BottomLiftSpeed = MathF.Round(value, 2);
    }

    public override float LiftHeight
    {
        get => Math.Max(0, PrintParametersSettings.LiftHeight - SlicerInfoSettings.LiftHeight2);
        set
        {
            value = MathF.Round(value, 2);
            PrintParametersSettings.LiftHeight = MathF.Round(value + SlicerInfoSettings.LiftHeight2, 2);
            base.LiftHeight = value;
        }
    }

    public override float LiftSpeed
    {
        get => PrintParametersSettings.LiftSpeed;
        set => base.LiftSpeed = PrintParametersSettings.LiftSpeed = MathF.Round(value, 2);
    }

    public override float BottomLiftHeight2
    {
        get => SlicerInfoSettings.BottomLiftHeight2;
        set
        {
            var bottomLiftHeight = BottomLiftHeight;
            SlicerInfoSettings.BottomLiftHeight2 = MathF.Round(value, 2);
            BottomLiftHeight = bottomLiftHeight;
            base.BottomLiftHeight2 = SlicerInfoSettings.BottomLiftHeight2;
        }
    }

    public override float BottomLiftSpeed2
    {
        get => SlicerInfoSettings.BottomLiftSpeed2;
        set => base.BottomLiftSpeed2 = SlicerInfoSettings.BottomLiftSpeed2 = MathF.Round(value, 2);
    }

    public override float LiftHeight2
    {
        get => SlicerInfoSettings.LiftHeight2;
        set
        {
            var liftHeight = LiftHeight;
            SlicerInfoSettings.LiftHeight2 = MathF.Round(value, 2);
            LiftHeight = liftHeight;
            base.LiftHeight2 = SlicerInfoSettings.LiftHeight2;
        }
    }

    public override float LiftSpeed2
    {
        get => SlicerInfoSettings.LiftSpeed2;
        set => base.LiftSpeed2 = SlicerInfoSettings.LiftSpeed2 = MathF.Round(value, 2);
    }

    public override float WaitTimeAfterLift
    {
        get => SlicerInfoSettings.RestTimeAfterLift;
        set
        {
            base.WaitTimeAfterLift = SlicerInfoSettings.RestTimeAfterLift = SlicerInfoSettings.RestTimeAfterLift2 = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
        }
    }

    public override float RetractSpeed
    {
        get => PrintParametersSettings.RetractSpeed;
        set => base.RetractSpeed = PrintParametersSettings.RetractSpeed = MathF.Round(value, 2);
    }

    public override float RetractHeight2
    {
        get => SlicerInfoSettings.RetractHeight2;
        set
        {
            value = Math.Clamp(MathF.Round(value, 2), 0, RetractHeightTotal);
            base.RetractHeight2 = SlicerInfoSettings.RetractHeight2 = value;
        }
    }

    public override float RetractSpeed2
    {
        get => SlicerInfoSettings.RetractSpeed2;
        set => base.RetractSpeed2 = SlicerInfoSettings.RetractSpeed2 = MathF.Round(value, 2);
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
        set => base.MaterialGrams = PrintParametersSettings.WeightG = MathF.Round(value, 3);
    }

    public override float MaterialCost
    {
        get => MathF.Round(PrintParametersSettings.CostDollars, 3);
        set => base.MaterialCost = PrintParametersSettings.CostDollars = MathF.Round(value, 3);
    }

    public override string MachineName
    {
        get => HeaderSettings.PrinterModel.ValueNotNull;
        set
        {
            if (!string.IsNullOrWhiteSpace(value) && !value.StartsWith("CL-") && !value.StartsWith("CT-"))
            {
                // Parse from machine name, if coming from PrusaSlicer this will help
                var match = Regex.Match(value, @"(CL|CT)-?[0-9]+[a-zA-Z]?");
                if (match is { Success: true, Groups.Count: > 1 })
                {
                    value = match.Value;
                }
            }
            base.MachineName = HeaderSettings.PrinterModel.Value = value;
        }
    }

    public override object[] Configs => [HeaderSettings, PrintParametersSettings, SlicerInfoSettings];

    #endregion

    #region Constructors
    public CrealityCXDLPv4File()
    {
        Previews = new Preview[ThumbnailCountFileShouldHave];
    }
    #endregion

    #region Methods
    public override void Clear()
    {
        base.Clear();

        for (byte i = 0; i < ThumbnailCountFileShouldHave; i++)
        {
            Previews[i] = new Preview();
        }

        LayerDefinitions = null!;
        LayerDefinitionsEx = null!;
    }

    protected override void OnBeforeEncode(bool isPartialEncode)
    {
        SlicerInfoSettings.PerLayerSettings = SupportPerLayerSettings && UsingPerLayerSettings;
        SlicerInfoSettings.ModifiedTimestampMinutes = (uint)DateTimeExtensions.TimestampMinutes;
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        HeaderSettings.PrintParametersSize = (uint)Helpers.Serializer.SizeOf(PrintParametersSettings);
        HeaderSettings.EncryptionKey = 0; // Force disable encryption

        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Create, FileAccess.ReadWrite);
        outputFile.Seek(Helpers.Serializer.SizeOf(HeaderSettings), SeekOrigin.Begin);

        Mat?[] thumbnails = [GetSmallestThumbnail(), GetLargestThumbnail()];
        for (byte i = 0; i < thumbnails.Length; i++)
        {
            var image = thumbnails[i];
            if(image is null) continue;
            var previewBytes = EncodeChituImageRGB15Rle(image);
            if (previewBytes.Length == 0) continue;

            Preview preview = new()
            {
                ResolutionX = (uint)image.Width,
                ResolutionY = (uint)image.Height,
                ImageLength = (uint)previewBytes.Length,
            };


            if (i == 0)
            {
                HeaderSettings.PreviewSmallOffsetAddress = (uint)outputFile.Position;
            }
            else
            {
                HeaderSettings.PreviewLargeOffsetAddress = (uint)outputFile.Position;
            }


            preview.ImageOffset = (uint)(outputFile.Position + Helpers.Serializer.SizeOf(preview));

            outputFile.WriteSerialize(preview);
            outputFile.WriteBytes(previewBytes);
        }



        HeaderSettings.PrintParametersOffsetAddress = (uint)outputFile.Position;
        outputFile.WriteSerialize(PrintParametersSettings);

        HeaderSettings.SlicerAddress = (uint) outputFile.Position;
        HeaderSettings.SlicerSize = (uint) Helpers.Serializer.SizeOf(SlicerInfoSettings);
        outputFile.WriteSerialize(SlicerInfoSettings);

        HeaderSettings.LayersDefinitionOffsetAddress = (uint)outputFile.Position;
        long layerDefSize = Helpers.Serializer.SizeOf(new LayerDef());
        long layerDefExSize = Helpers.Serializer.SizeOf(new LayerDefEx());

        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        LayerDefinitions = new LayerDef[LayerCount];
        LayerDefinitionsEx = new LayerDefEx[LayerCount];

        long dataPosition = outputFile.Position + layerDefSize * LayerCount;
        outputFile.Seek(dataPosition, SeekOrigin.Begin);
        var pixelArea = PixelArea;

        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                var layer = this[layerIndex];
                using (var mat = layer.LayerMat)
                {
                    LayerDefinitions[layerIndex] = new LayerDef(layer);
                    LayerDefinitionsEx[layerIndex] = new LayerDefEx(layer);
                    LayerDefinitionsEx[layerIndex].Encode(this, mat, (uint)layerIndex);

                    LayerDefinitions[layerIndex].DataSize = (uint)(LayerDefinitionsEx[layerIndex].EncodedRle!.Length + layerDefExSize);

                    using var matRoi = mat.Roi(layer.BoundingRectangle);
                    using var contours = matRoi.FindContours(RetrType.External);
                    LayerDefinitions[layerIndex].LargestArea = (uint)(EmguContours.GetLargestContourArea(contours) * pixelArea * 1000);
                }

                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                LayerDefinitions[layerIndex].DataAddress = (uint)outputFile.Position;

                outputFile.WriteSerialize(LayerDefinitionsEx[layerIndex]);
                outputFile.WriteBytes(LayerDefinitionsEx[layerIndex].EncodedRle!);

                LayerDefinitionsEx[layerIndex].EncodedRle = null; // Free this
            }
        }

        progress.Reset("Calculating checksum");
        outputFile.Seek(HeaderSettings.LayersDefinitionOffsetAddress, SeekOrigin.Begin);
        for (int layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            outputFile.WriteSerialize(LayerDefinitions[layerIndex]);
        }

        outputFile.Seek(0, SeekOrigin.Begin);
        outputFile.WriteSerialize(HeaderSettings);

        uint checkSum = CalculateCheckSum(outputFile, false);

        outputFile.WriteUIntBigEndian(checkSum);

        Debug.WriteLine("Encode Results:");
        Debug.WriteLine(HeaderSettings);
        Debug.WriteLine(Previews[0]);
        Debug.WriteLine(Previews[1]);
        Debug.WriteLine(PrintParametersSettings);
        Debug.WriteLine(SlicerInfoSettings);
        Debug.WriteLine($"CheckSum: {checkSum}");
        Debug.WriteLine("-End-");
    }


    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = new FileStream(FileFullPath!, FileMode.Open, FileAccess.Read);
        HeaderSettings = Helpers.Deserialize<Header>(inputFile);
        HeaderSettings.Validate();

        Debug.Write("Header -> ");
        Debug.WriteLine(HeaderSettings);

        var position = inputFile.Position;

        progress.Reset("Validating checksum");
        var expectedCheckSum = CalculateCheckSum(inputFile, false, -4);
        uint checkSum = inputFile.ReadUIntBigEndian();

        if (expectedCheckSum != checkSum)
        {
            throw new FileLoadException($"Checksum fails, expecting: {expectedCheckSum} but got: {checkSum}.\n" +
                                        $"Try to reslice the file.", FileFullPath);
        }

        inputFile.Seek(position, SeekOrigin.Begin);

        progress.Reset(OperationProgress.StatusDecodePreviews, (uint)ThumbnailCountFileShouldHave);
        var thumbnailOffsets = new[] { HeaderSettings.PreviewSmallOffsetAddress, HeaderSettings.PreviewLargeOffsetAddress };
        for (int i = 0; i < thumbnailOffsets.Length; i++)
        {
            if (thumbnailOffsets[i] == 0) continue;

            inputFile.Seek(thumbnailOffsets[i], SeekOrigin.Begin);
            Previews[i] = Helpers.Deserialize<Preview>(inputFile);

            Debug.Write($"Preview {i} -> ");
            Debug.WriteLine(Previews[i]);

            inputFile.Seek(Previews[i].ImageOffset, SeekOrigin.Begin);
            var rawImageData = GC.AllocateUninitializedArray<byte>((int)Previews[i].ImageLength);
            inputFile.ReadExactly(rawImageData.AsSpan());

            Thumbnails.Add(DecodeChituImageRGB15Rle(rawImageData, Previews[i].ResolutionX, Previews[i].ResolutionY));
            progress++;
        }

        if (HeaderSettings.PrintParametersOffsetAddress > 0)
        {
            inputFile.Seek(HeaderSettings.PrintParametersOffsetAddress, SeekOrigin.Begin);
            PrintParametersSettings = Helpers.Deserialize<PrintParameters>(inputFile);
            Debug.Write("Print Parameters -> ");
            Debug.WriteLine(PrintParametersSettings);
        }

        if (HeaderSettings.SlicerAddress > 0)
        {
            inputFile.Seek(HeaderSettings.SlicerAddress, SeekOrigin.Begin);
            SlicerInfoSettings = Helpers.Deserialize<SlicerInfo>(inputFile);
            Debug.Write("Slicer Info -> ");
            Debug.WriteLine(SlicerInfoSettings);
        }

        Init(HeaderSettings.LayerCount, DecodeType == FileDecodeType.Partial);
        LayerDefinitions = new LayerDef[LayerCount];
        LayerDefinitionsEx = new LayerDefEx[LayerCount];

        progress.Reset(OperationProgress.StatusGatherLayers, LayerCount);
        inputFile.Seek(HeaderSettings.LayersDefinitionOffsetAddress, SeekOrigin.Begin);

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            progress.PauseOrCancelIfRequested();
            LayerDefinitions[layerIndex] = Helpers.Deserialize<LayerDef>(inputFile);

            Debug.Write($"LAYER {layerIndex} -> ");
            Debug.WriteLine(LayerDefinitions[layerIndex]);

            progress++;
        }

        progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
        uint layerDefExSize = (uint)Helpers.Serializer.SizeOf(new LayerDefEx());

        foreach (var batch in BatchLayersIndexes())
        {
            foreach (var layerIndex in batch)
            {
                progress.PauseOrCancelIfRequested();

                inputFile.Seek(LayerDefinitions[layerIndex].DataAddress, SeekOrigin.Begin);
                LayerDefinitionsEx[layerIndex] = Helpers.Deserialize<LayerDefEx>(inputFile);
                if (DecodeType == FileDecodeType.Full) LayerDefinitionsEx[layerIndex].EncodedRle = inputFile.ReadBytes(LayerDefinitions[layerIndex].DataSize - layerDefExSize);
                Debug.Write($"LAYER {layerIndex} -> ");
                Debug.WriteLine(LayerDefinitionsEx[layerIndex]);
            }

            if (DecodeType == FileDecodeType.Full)
            {
                Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
                {
                    progress.PauseIfRequested();
                    using (var mat = LayerDefinitionsEx[layerIndex].Decode(this, LayerDefinitions[layerIndex], (uint)layerIndex))
                    {
                        _layers[layerIndex] = new Layer((uint)layerIndex, mat, this);
                    }

                    progress.LockAndIncrement();
                });
            }
        }

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            LayerDefinitions[layerIndex].CopyTo(this[layerIndex]);
            LayerDefinitionsEx[layerIndex].CopyTo(this[layerIndex]);
        }

        // Fixes virtual bottom properties
        SuppressRebuildPropertiesWork(() =>
        {
            var enumerable = this.AsValueEnumerable();
            BottomWaitTimeBeforeCure = enumerable.FirstOrDefault(layer => layer is { IsBottomLayer: true, IsDummy: false })?.WaitTimeBeforeCure ?? 0;
            BottomWaitTimeAfterCure = enumerable.FirstOrDefault(layer => layer is { IsBottomLayer: true, IsDummy: false })?.WaitTimeAfterCure ?? 0;
            BottomWaitTimeAfterLift = enumerable.FirstOrDefault(layer => layer is { IsBottomLayer: true, IsDummy: false })?.WaitTimeAfterLift ?? 0;
            BottomRetractSpeed = enumerable.FirstOrDefault(layer => layer is { IsBottomLayer: true, IsDummy: false })?.RetractSpeed ?? 0;
            BottomRetractHeight2 = enumerable.FirstOrDefault(layer => layer is { IsBottomLayer: true, IsDummy: false })?.RetractHeight2 ?? 0;
            BottomRetractSpeed2 = enumerable.FirstOrDefault(layer => layer is { IsBottomLayer: true, IsDummy: false })?.RetractSpeed2 ?? 0;
        });
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Open, FileAccess.ReadWrite);
        outputFile.Seek(0, SeekOrigin.Begin);
        outputFile.WriteSerialize(HeaderSettings);

        if (HeaderSettings.PrintParametersOffsetAddress > 0)
        {
            outputFile.Seek(HeaderSettings.PrintParametersOffsetAddress, SeekOrigin.Begin);
            outputFile.WriteSerialize(PrintParametersSettings);
        }

        if (HeaderSettings.SlicerAddress > 0)
        {
            outputFile.Seek(HeaderSettings.SlicerAddress, SeekOrigin.Begin);
            outputFile.WriteSerialize(SlicerInfoSettings);
        }

        progress.Reset(OperationProgress.StatusEncodeLayers);
        outputFile.Seek(HeaderSettings.LayersDefinitionOffsetAddress, SeekOrigin.Begin);
        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            LayerDefinitions[layerIndex].SetFrom(this[layerIndex]);
            outputFile.WriteSerialize(LayerDefinitions[layerIndex]);
            if (layerIndex % 2 == 0) progress++;
        }

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            LayerDefinitionsEx[layerIndex].SetFrom(this[layerIndex]);
            outputFile.Seek(LayerDefinitions[layerIndex].DataAddress, SeekOrigin.Begin);
            outputFile.WriteSerialize(LayerDefinitionsEx[layerIndex]);
            if (layerIndex % 2 == 0) progress++;
        }

        progress.Reset("Calculating checksum");
        uint checkSum = CalculateCheckSum(outputFile, false, -4);
        outputFile.WriteUIntBigEndian(checkSum);
    }

    private uint CalculateCheckSum(FileStream fs, bool restorePosition = true, int offsetSize = 0)
    {
        uint checkSum = 0;
        var position = fs.Position;
        var dataSize = fs.Length + offsetSize;
        const int bufferSize = 50 * 1024 * 1024;

        fs.Seek(0, SeekOrigin.Begin);

        // https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.Hashing/src/System/IO/Hashing/Crc32.Table.cs
        var table = new uint[256];

        for (uint i = 0; i < 256; i++)
        {
            uint val = i;

            for (int j = 0; j < 8; j++)
            {
                if ((val & 0b0000_0001) == 0)
                {
                    val >>= 1;
                }
                else
                {
                    val = (val >> 1) ^ 0xEDB88320u;
                }
            }
            table[i] = val;
        }

        for (
            int chunkSize = (int)Math.Min(bufferSize, dataSize - fs.Position);
            chunkSize > 0;
            chunkSize = (int)Math.Min(chunkSize, dataSize - fs.Position))
        {
            var bytes = fs.ReadBytes(chunkSize);
            for (int i = 0; i < bytes.Length; i++)
            {
                // https://github.com/dotnet/runtime/blob/main/src/libraries/System.IO.Hashing/src/System/IO/Hashing/Crc32.cs
                byte idx = (byte)checkSum;
                idx ^= bytes[i];
                checkSum = table[idx] ^ (checkSum >> 8);
            }
        }

        if (restorePosition) fs.Seek(position, SeekOrigin.Begin);

        return checkSum;
    }

    #endregion

    #region Static Methods
    public static byte[] LayerRleCrypt(uint seed, uint layerIndex, IEnumerable<byte> input)
    {
        var result = input.AsValueEnumerable().ToArray();
        LayerRleCryptBuffer(seed, layerIndex, result);
        return result;
    }

    public static void LayerRleCryptBuffer(uint seed, uint layerIndex, byte[] input)
    {
        if (seed == 0) return;
        var init = seed * 0x2d83cdac + 0xd8a83423;
        var key = (layerIndex * 0x1e1530cd + 0xec3d47cd) * init;

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