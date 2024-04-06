/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using BinarySerialization;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UVtools.Core.Converters;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public sealed class PhotonWorkshopFile : FileFormat
{
    #region Constants
    public const byte VERSION_1 = 1;       // 0x1
    public const ushort VERSION_515 = 515; // 0x203
    public const ushort VERSION_516 = 516; // 0x204
    public const ushort VERSION_517 = 517; // 0x205
    public const ushort VERSION_518 = 518; // 0x206

    public const byte MarkSize = 12;
    public const byte RLE1EncodingLimit = 0x7d; // 125;
    public const ushort RLE4EncodingLimit = 0xfff; // 4095;

    // CRC-16-ANSI (aka CRC-16-IBM) Polynomial: x^16 + x^15 + x^2 + 1
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
    public enum PhotonRleFormat
    {
        PWS,
        PW0
    }
        
    public enum AnyCubicMachine : byte
    {
        PhotonS,
        PhotonZero,
        PhotonX,
        PhotonUltra,
        PhotonD2,
        PhotonMono,
        PhotonMono2,
        PhotonMonoSE,
        PhotonMono4K,
        PhotonMonoX,
        PhotonMonoX2,
        PhotonMonoX6KM3Plus,
        PhotonMonoX6Ks,
        PhotonMonoSQ,
        PhotonM3,
        PhotonM3Max,
        PhotonM3Premium,
        PhotonMonoM5,
        PhotonMonoM5s,
        PhotonMonoM5sPro,
        Custom,
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
        /// </summary>
        [FieldOrder(0)]
        [FieldLength(MarkSize)]
        [SerializeAs(SerializedType.TerminatedString)]
        public string Mark { get; set; } = SectionMarkFile;

        /// <summary>
        /// Gets the file format version
        /// </summary>
        [FieldOrder(1)] public uint Version { get; set; } = VERSION_1;

        /// <summary>
        /// Gets the area num
        /// 4 for v1, 5 for v515, 8 for v516?
        /// </summary>
        [FieldOrder(2)] public uint NumberOfTables { get; set; }

        /// <summary>
        /// Gets the header start address
        /// </summary>
        [FieldOrder(3)]  public uint HeaderAddress { get; set; }

        /// <summary>
        /// </summary>
        [FieldOrder(4)]  public uint SoftwareAddress { get; set; }

        /// <summary>
        /// Gets the preview start offset
        /// </summary>
        [FieldOrder(5)]  public uint PreviewAddress { get; set; }

        /// <summary>
        /// Spotted on version 515 only
        /// </summary>
        [FieldOrder(6)]  public uint LayerImageColorTableAddress  { get; set; } 

        /// <summary>
        /// Gets the layer definition start address
        /// </summary>
        [FieldOrder(7)]  public uint LayerDefinitionAddress { get; set; }

        /// <summary>
        /// Spotted on version 516 only
        /// </summary>
        [FieldOrder(8)]  public uint ExtraAddress  { get; set; }

        /// <summary>
        /// Spotted on version 516 only
        /// </summary>
        [SerializeWhen(nameof(Version), VERSION_516, ComparisonOperator.GreaterThanOrEqual)]
        [FieldOrder(9)] public uint MachineAddress { get; set; }

        /// <summary>
        /// Gets layer image start address
        /// </summary>
        [FieldOrder(10)]  public uint LayerImageAddress { get; set; }

        /// <summary>
        /// Spotted on version 517 only
        /// </summary>
        [SerializeWhen(nameof(Version), VERSION_517, ComparisonOperator.GreaterThanOrEqual)]
        [FieldOrder(11)] public uint ModelAddress { get; set; }

        /// <summary>
        /// Spotted on version 518 only
        /// </summary>
        [SerializeWhen(nameof(Version), VERSION_518, ComparisonOperator.GreaterThanOrEqual)]
        [FieldOrder(12)] public uint SubLayerDefinitionAddress { get; set; }

        /// <summary>
        /// Spotted on version 518 only
        /// </summary>
        [SerializeWhen(nameof(Version), VERSION_518, ComparisonOperator.GreaterThanOrEqual)]
        [FieldOrder(13)] public uint Preview2Address { get; set; }

        public override string ToString()
        {
            return $"{nameof(Mark)}: {Mark}, {nameof(Version)}: {Version}, {nameof(NumberOfTables)}: {NumberOfTables}, {nameof(HeaderAddress)}: {HeaderAddress}, {nameof(SoftwareAddress)}: {SoftwareAddress}, {nameof(PreviewAddress)}: {PreviewAddress}, {nameof(LayerImageColorTableAddress)}: {LayerImageColorTableAddress}, {nameof(LayerDefinitionAddress)}: {LayerDefinitionAddress}, {nameof(ExtraAddress)}: {ExtraAddress}, {nameof(MachineAddress)}: {MachineAddress}, {nameof(LayerImageAddress)}: {LayerImageAddress}, {nameof(ModelAddress)}: {ModelAddress}, {nameof(SubLayerDefinitionAddress)}: {SubLayerDefinitionAddress}, {nameof(Preview2Address)}: {Preview2Address}";
        }
    }
    #endregion

    #region Section

    public abstract class AnycubicNamedTable
    {
        public const int TableBaseLength = MarkSize + 4;

        /// <summary>
        /// Gets or sets whatever to include the base table length into <see cref="TableLength"/>
        /// </summary>
        protected virtual bool IncludeBaseTableLength => false;

        /// <summary>
        /// Name of this table
        /// </summary>
        protected abstract string DefaultTableName { get; }

        /*/// <summary>
        /// Table length offset
        /// </summary>
        protected virtual int TableLengthOffset => 0;*/

        /// <summary>
        /// Gets the section name placeholder
        /// </summary>
        [FieldOrder(0)]
        [FieldLength(MarkSize)]
        [SerializeAs(SerializedType.TerminatedString)]
        public string TableName { get; set; }

        /// <summary>
        /// Gets the length of this section
        /// </summary>
        [FieldOrder(1)] public uint TableLength { get; set; }

        protected AnycubicNamedTable()
        {
            TableName = DefaultTableName;
            TableLength = GetFinalTableLength();
        }

        public override string ToString()
        {
            return $"{nameof(TableName)}: {TableName}, {nameof(TableLength)}: {TableLength}";
        }

        /// <summary>
        /// Gets the absolute size of the table including base and derived
        /// </summary>
        public uint GetWholeTableLength() => (uint)Helpers.Serializer.SizeOf(this);

        /// <summary>
        /// Gets the absolute size of the derived table
        /// </summary>
        /// <returns></returns>
        public uint GetDerivedTableLength() => GetWholeTableLength() - TableBaseLength;

        /// <summary>
        /// Gets the absolute size to set in this table
        /// </summary>
        /// <returns></returns>
        public uint GetFinalTableLength() => IncludeBaseTableLength ? GetWholeTableLength() : GetDerivedTableLength();

        /// <summary>
        /// Validates this table
        /// </summary>
        /// <param name="validateName">True to validate the name, otherwise false</param>
        /// <param name="lengthOffset">Offset to length</param>
        /// <exception cref="FileLoadException"></exception>
        public void Validate(bool validateName, int lengthOffset = 0) => ValidateExpecting((int)GetFinalTableLength() + lengthOffset, validateName);
       
        public void Validate(sbyte lengthOffset) => Validate(true, lengthOffset);
        public void Validate(byte lengthOffset) => Validate(true, lengthOffset);
        public void Validate(ushort lengthOffset) => Validate(true, lengthOffset);
        public void Validate(short lengthOffset) => Validate(true, lengthOffset);
        public void Validate(int lengthOffset = 0) => Validate(true, lengthOffset);
        public void Validate(uint lengthOffset) => Validate(true, (int)lengthOffset);

        /// <summary>
        /// Validates this table
        /// </summary>
        /// <param name="length">Length to expect</param>
        /// <param name="validateName">True to validate the name, otherwise false</param>
        /// <exception cref="FileLoadException"></exception>
        public void ValidateExpecting(int length, bool validateName = true)
        {
            if (validateName && !DefaultTableName.Equals(TableName))
            {
                throw new FileLoadException($"'{DefaultTableName}' section expected, but got '{TableName}'");
            }

            if (TableLength != length)
            {
                throw new FileLoadException($"{DefaultTableName} section bytes: expected {length}, got {TableLength}, difference: {length - TableLength}");
            }
        }
    }

    #endregion

    #region Header
    public sealed class Header : AnycubicNamedTable
    {
        protected override string DefaultTableName => "HEADER";

        //[FieldOrder(0)] public SectionHeader Section { get; set; }

        [FieldOrder(1)] public float PixelSizeUm { get; set; } = 47.25f;

        /// <summary>
        /// Layer height in mm
        /// </summary>
        [FieldOrder(2)] public float LayerHeight { get; set; }

        [FieldOrder(3)] public float ExposureTime { get; set; }

        [FieldOrder(4)] public float WaitTimeBeforeCure { get; set; } = 1;

        [FieldOrder(5)] public float BottomExposureTime { get; set; } 

        [FieldOrder(6)] public float BottomLayersCount { get; set; }

        [FieldOrder(7)] public float LiftHeight { get; set; } = DefaultLiftHeight;
        
        /// <summary>
        /// Gets the lift speed in mm/s
        /// </summary>
        [FieldOrder(8)] public float LiftSpeed { get; set; } = SpeedConverter.Convert(DefaultLiftSpeed, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond); // mm/s

        /// <summary>
        /// Gets the retract speed in mm/s
        /// </summary>
        [FieldOrder(9)] public float RetractSpeed { get; set; } = SpeedConverter.Convert(DefaultRetractSpeed, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond); // mm/s

        [FieldOrder(10)] public float VolumeMl { get; set; }

        [FieldOrder(11)] public uint AntiAliasing { get; set; } = 1;

        [FieldOrder(12)] public uint ResolutionX { get; set; }

        [FieldOrder(13)] public uint ResolutionY { get; set; }

        [FieldOrder(14)] public float WeightG { get; set; }

        [FieldOrder(15)] public float Price { get; set; }

        /// <summary>
        /// 24 00 00 00 $ or ¥ C2 A5 00 00 or € = E2 82 AC 00
        /// </summary>
        [FieldOrder(16)] [FieldLength(4)] public char PriceCurrencySymbol { get; set; } = '$';

        /// <summary>
        /// 80
        /// </summary>
        [FieldOrder(17)] [SerializeAs(SerializedType.UInt4)] public bool PerLayerOverride { get; set; } // bool

        [FieldOrder(18)] public uint PrintTime { get; set; }

        /// <summary>
        /// spotted on 516
        /// </summary>
        [FieldOrder(19)] public uint TransitionLayerCount { get; set; }

        /// <summary>
        /// spotted on 516
        /// </summary>
        [FieldOrder(20)] public uint TransitionLayerType { get; set; }

        //[SerializeUntil((char)'P')] [FieldOrder(21)] public List<byte> Offset { get; set; } = new();

        /// <summary>
        /// 0 = Basic mode | 1 = Advanced mode which allows TSMC
        /// v516
        /// </summary>
        [FieldOrder(21)] [SerializeWhen(nameof(TableLength), 84, ComparisonOperator.GreaterThanOrEqual)] public uint AdvancedMode { get; set; }

        /// <summary>
        /// v517
        /// </summary>
        [FieldOrder(22)] [SerializeWhen(nameof(TableLength), 86, ComparisonOperator.GreaterThanOrEqual)] public ushort Grey { get; set; }
        /// <summary>
        /// v517
        /// </summary>
        [FieldOrder(23)] [SerializeWhen(nameof(TableLength), 88, ComparisonOperator.GreaterThanOrEqual)] public ushort BlurLevel { get; set; }
        /// <summary>
        /// v517
        /// </summary>
        [FieldOrder(24)] [SerializeWhen(nameof(TableLength), 92, ComparisonOperator.GreaterThanOrEqual)] public uint ResinType { get; set; }
        /// <summary>
        /// When true, normal exposure time will be auto set, use false for traditional way
        /// v518
        /// </summary>
        [FieldOrder(25)] [SerializeWhen(nameof(TableLength), 96, ComparisonOperator.GreaterThanOrEqual)] [SerializeAs(SerializedType.UInt4)] public bool IntelligentMode { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(PixelSizeUm)}: {PixelSizeUm}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(WaitTimeBeforeCure)}: {WaitTimeBeforeCure}, {nameof(BottomExposureTime)}: {BottomExposureTime}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(VolumeMl)}: {VolumeMl}, {nameof(AntiAliasing)}: {AntiAliasing}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(WeightG)}: {WeightG}, {nameof(Price)}: {Price}, {nameof(PriceCurrencySymbol)}: {PriceCurrencySymbol}, {nameof(PerLayerOverride)}: {PerLayerOverride}, {nameof(PrintTime)}: {PrintTime}, {nameof(TransitionLayerCount)}: {TransitionLayerCount}, {nameof(TransitionLayerType)}: {TransitionLayerType}, {nameof(AdvancedMode)}: {AdvancedMode}, {nameof(Grey)}: {Grey}, {nameof(BlurLevel)}: {BlurLevel}, {nameof(ResinType)}: {ResinType}, {nameof(IntelligentMode)}: {IntelligentMode}";
        }
    }

    #endregion

    #region Preview

    /// <summary>
    /// The files contain two preview images.
    /// These are shown on the printer display when choosing which file to print, sparing the poor printer from needing to render a 3D image from scratch.
    /// </summary>
    public class Preview : AnycubicNamedTable
    {
        protected override string DefaultTableName => "PREVIEW";
        protected override bool IncludeBaseTableLength => true;

        /// <summary>
        /// Gets the image width, in pixels.
        /// </summary>
        [FieldOrder(0)] public uint ResolutionX { get; set; } = 224;

        /// <summary>
        /// Gets the operation mark 'x'
        /// </summary>
        [FieldOrder(1)] [FieldLength(4)] [SerializeAs(SerializedType.TerminatedString)] public string Mark { get; set; } = "x";

        /// <summary>
        /// Gets the image height, in pixels.
        /// </summary>
        [FieldOrder(2)] public uint ResolutionY { get; set; } = 168;

        [Ignore] public uint DataSize => ResolutionX * ResolutionY * 2;

        [Ignore] public byte[] Data { get; set; } = null!;

        public Preview()
        {
        }

        public Preview(uint resolutionX, uint resolutionY) : this()
        {
            ResolutionX = resolutionX;
            ResolutionY = resolutionY;
            TableLength += DataSize;
        }

        public Preview(Mat mat) : this((uint)mat.Width, (uint)mat.Height)
        {
            Data = EncodeImage(DATATYPE_RGB565, mat);
        }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(Mark)}: {Mark}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(DataSize)}: {DataSize}";
        }

        public void ResetData() => Data = null!;
    }

    public sealed class Preview2 : AnycubicNamedTable
    {
        protected override string DefaultTableName => "PREVIEW2";
        protected override bool IncludeBaseTableLength => true;

        /// <summary>
        /// Gets the image width, in pixels.
        /// </summary>
        [FieldOrder(0)] public uint ResolutionX { get; set; } = 320;

        /// <summary>
        /// Gets the operation mark 'x'
        /// </summary>
        [FieldOrder(1)] public ushort BackgroundColor1 { get; set; } = 4293;
        [FieldOrder(2)] public ushort BackgroundColor2 { get; set; }

        /// <summary>
        /// Gets the image height, in pixels.
        /// </summary>
        [FieldOrder(3)] public uint ResolutionY { get; set; } = 190;

        [Ignore] public uint DataSize => ResolutionX * ResolutionY * 2;

        [Ignore] public byte[] Data { get; set; } = null!;

        public Preview2()
        {
        }

        public Preview2(uint resolutionX, uint resolutionY)
        {
            ResolutionX = resolutionX;
            ResolutionY = resolutionY;
            TableLength += DataSize;
        }

        public Preview2(Mat mat) : this((uint)mat.Width, (uint)mat.Height)
        {
            Data = EncodeImage(DATATYPE_RGB565, mat);
        }

        public void ResetData()
        {
            Data = null!;
        }
    }

    #endregion

    #region LayerColorTable

    public sealed class LayerImageColorTable
    {
        [FieldOrder(0)] public uint UseFullGreyscale { get; set; }
        [FieldOrder(1)] public uint GreyMaxCount { get; set; } = 16;
        [FieldOrder(2)] [FieldCount(nameof(GreyMaxCount))] public byte[] Grey { get; set; } = {
            // AA16: 255, 239, 223, 207, 191, 175, 159, 143, 127, 111, 95, 79, 63, 47, 31, 15
            15, 31, 47,    // 1,2,3
            63, 79, 95,    // 4,5,6
            111, 127, 143, // 7,8,9
            159, 175, 191, // 10,11,12
            207, 223, 239, // 13,14,15
            byte.MaxValue  // 16
        };

        [FieldOrder(3)] public uint Unknown { get; set; }

        public override string ToString()
        {
            return $"{nameof(UseFullGreyscale)}: {UseFullGreyscale}, {nameof(GreyMaxCount)}: {GreyMaxCount}, {nameof(Grey)}: {Grey}, {nameof(Unknown)}: {Unknown}";
        }
    }
    #endregion

    #region Extra
    public class Extra : AnycubicNamedTable
    {
        protected override string DefaultTableName => "EXTRA";

        [FieldOrder(0)] public uint BottomLiftCount { get; set; } = 2;
        [FieldOrder(1)] public float BottomLiftHeight1 { get; set; }
        [FieldOrder(2)] public float BottomLiftSpeed1 { get; set; } = SpeedConverter.Convert(DefaultBottomLiftSpeed, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond);
        [FieldOrder(3)] public float BottomRetractSpeed2 { get; set; } = SpeedConverter.Convert(DefaultBottomRetractSpeed, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond);
        [FieldOrder(4)] public float BottomLiftHeight2 { get; set; }
        [FieldOrder(5)] public float BottomLiftSpeed2 { get; set; } = SpeedConverter.Convert(DefaultBottomLiftSpeed2, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond);
        [FieldOrder(6)] public float BottomRetractSpeed1 { get; set; } = SpeedConverter.Convert(DefaultBottomRetractSpeed2, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond);
        [FieldOrder(7)] public uint NormalLiftCount { get; set; } = 2;
        [FieldOrder(8)] public float LiftHeight1 { get; set; }
        [FieldOrder(9)] public float LiftSpeed1 { get; set; } = SpeedConverter.Convert(DefaultLiftSpeed, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond);
        [FieldOrder(10)] public float RetractSpeed2 { get; set; } = SpeedConverter.Convert(DefaultRetractSpeed, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond);
        [FieldOrder(11)] public float LiftHeight2 { get; set; }
        [FieldOrder(12)] public float LiftSpeed2 { get; set; } = SpeedConverter.Convert(DefaultLiftSpeed2, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond);
        [FieldOrder(13)] public float RetractSpeed1 { get; set; } = SpeedConverter.Convert(DefaultRetractSpeed2, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond);

        public Extra()
        {
            TableLength = 24; // Don't know why...
        }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(BottomLiftCount)}: {BottomLiftCount}, {nameof(BottomLiftHeight1)}: {BottomLiftHeight1}, {nameof(BottomLiftSpeed1)}: {BottomLiftSpeed1}, {nameof(BottomRetractSpeed1)}: {BottomRetractSpeed1}, {nameof(BottomLiftHeight2)}: {BottomLiftHeight2}, {nameof(BottomLiftSpeed2)}: {BottomLiftSpeed2}, {nameof(BottomRetractSpeed2)}: {BottomRetractSpeed2}, {nameof(NormalLiftCount)}: {NormalLiftCount}, {nameof(LiftHeight1)}: {LiftHeight1}, {nameof(LiftSpeed1)}: {LiftSpeed1}, {nameof(RetractSpeed1)}: {RetractSpeed1}, {nameof(LiftHeight2)}: {LiftHeight2}, {nameof(LiftSpeed2)}: {LiftSpeed2}, {nameof(RetractSpeed2)}: {RetractSpeed2}";
        }
    }
    #endregion

    #region Machine
    public class Machine : AnycubicNamedTable
    {
        protected override string DefaultTableName => "MACHINE";

        protected override bool IncludeBaseTableLength => true;

        [FieldOrder(0)][FieldLength(96)][SerializeAs(SerializedType.TerminatedString)] public string MachineName { get; set; } = null!;
        [FieldOrder(1)][FieldLength(16)][SerializeAs(SerializedType.TerminatedString)] public string LayerImageFormat { get; set; } = "pw0img";
        [FieldOrder(2)] public uint MaxAntialiasingLevel { get; set; } = 16;
        [FieldOrder(3)] public uint PropertyFields { get; set; } = 1;
        [FieldOrder(4)] public float DisplayWidth { get; set; }
        [FieldOrder(5)] public float DisplayHeight { get; set; }
        [FieldOrder(6)] public float MachineZ { get; set; }
        [FieldOrder(7)] public uint MaxFileVersion { get; set; } = VERSION_518;
        [FieldOrder(8)] public uint MachineBackground { get; set; } = 6506241;
        [FieldOrder(9)] [SerializeWhen(nameof(TableLength), 160, ComparisonOperator.GreaterThanOrEqual)] public float PixelWidthUm { get; set; }
        [FieldOrder(10)] [SerializeWhen(nameof(TableLength), 164, ComparisonOperator.GreaterThanOrEqual)] public float PixelHeightUm { get; set; }
        [FieldOrder(11)] [SerializeWhen(nameof(TableLength), 168, ComparisonOperator.GreaterThanOrEqual)] public uint Padding1 { get; set; }
        [FieldOrder(12)] [SerializeWhen(nameof(TableLength), 172, ComparisonOperator.GreaterThanOrEqual)] public uint Padding2 { get; set; }
        [FieldOrder(13)] [SerializeWhen(nameof(TableLength), 176, ComparisonOperator.GreaterThanOrEqual)] public uint Padding3 { get; set; }
        [FieldOrder(14)] [SerializeWhen(nameof(TableLength), 180, ComparisonOperator.GreaterThanOrEqual)] public uint Padding4 { get; set; }
        [FieldOrder(15)] [SerializeWhen(nameof(TableLength), 184, ComparisonOperator.GreaterThanOrEqual)] public uint Padding5 { get; set; }
        [FieldOrder(16)] [SerializeWhen(nameof(TableLength), 188, ComparisonOperator.GreaterThanOrEqual)] public uint Padding6 { get; set; }
        [FieldOrder(17)] [SerializeWhen(nameof(TableLength), 192, ComparisonOperator.GreaterThanOrEqual)] public uint Padding7 { get; set; }
        [FieldOrder(18)] [SerializeWhen(nameof(TableLength), 196, ComparisonOperator.GreaterThanOrEqual)] public uint Padding8 { get; set; }
        [FieldOrder(19)] [SerializeWhen(nameof(TableLength), 200, ComparisonOperator.GreaterThanOrEqual)] public uint DisplayCount { get; set; } = 1;
        [FieldOrder(20)] [SerializeWhen(nameof(TableLength), 204, ComparisonOperator.GreaterThanOrEqual)] public uint Padding9 { get; set; }
        [FieldOrder(21)] [SerializeWhen(nameof(TableLength), 206, ComparisonOperator.GreaterThanOrEqual)] public ushort ResolutionX { get; set; }
        [FieldOrder(22)] [SerializeWhen(nameof(TableLength), 208, ComparisonOperator.GreaterThanOrEqual)] public ushort ResolutionY { get; set; }
        [FieldOrder(23)] [SerializeWhen(nameof(TableLength), 212, ComparisonOperator.GreaterThanOrEqual)] public uint Padding10 { get; set; }
        [FieldOrder(24)] [SerializeWhen(nameof(TableLength), 216, ComparisonOperator.GreaterThanOrEqual)] public uint Padding11 { get; set; }
        [FieldOrder(25)] [SerializeWhen(nameof(TableLength), 220, ComparisonOperator.GreaterThanOrEqual)] public uint Padding12 { get; set; }
        [FieldOrder(26)] [SerializeWhen(nameof(TableLength), 224, ComparisonOperator.GreaterThanOrEqual)] public uint Padding13 { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(MachineName)}: {MachineName}, {nameof(LayerImageFormat)}: {LayerImageFormat}, {nameof(MaxAntialiasingLevel)}: {MaxAntialiasingLevel}, {nameof(PropertyFields)}: {PropertyFields}, {nameof(DisplayWidth)}: {DisplayWidth}, {nameof(DisplayHeight)}: {DisplayHeight}, {nameof(MachineZ)}: {MachineZ}, {nameof(MaxFileVersion)}: {MaxFileVersion}, {nameof(MachineBackground)}: {MachineBackground}, {nameof(PixelWidthUm)}: {PixelWidthUm}, {nameof(PixelHeightUm)}: {PixelHeightUm}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(Padding3)}: {Padding3}, {nameof(Padding4)}: {Padding4}, {nameof(Padding5)}: {Padding5}, {nameof(Padding6)}: {Padding6}, {nameof(Padding7)}: {Padding7}, {nameof(Padding8)}: {Padding8}, {nameof(DisplayCount)}: {DisplayCount}, {nameof(Padding9)}: {Padding9}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(Padding10)}: {Padding10}, {nameof(Padding11)}: {Padding11}, {nameof(Padding12)}: {Padding12}, {nameof(Padding13)}: {Padding13}";
        }
    }
    #endregion

    #region Software
    public sealed class Software
    {
        public const uint TableSize = 164;
        [FieldOrder(0)][FieldLength(32)][SerializeAs(SerializedType.TerminatedString)] public string SoftwareName { get; set; } = About.Software;
        [FieldOrder(1)] public uint TableLength { get; set; } = TableSize;
        [FieldOrder(2)][FieldLength(32)][SerializeAs(SerializedType.TerminatedString)] public string Version { get; set; } = About.VersionString;
        [FieldOrder(3)][FieldLength(64)][SerializeAs(SerializedType.TerminatedString)] public string OperativeSystem { get; set; } = RuntimeInformation.RuntimeIdentifier;
        [FieldOrder(4)][FieldLength(32)][SerializeAs(SerializedType.TerminatedString)] public string OpenGLVersion { get; set; } = "3.3-CoreProfile";

        public override string ToString()
        {
            return $"{nameof(SoftwareName)}: {SoftwareName}, {nameof(TableLength)}: {TableLength}, {nameof(Version)}: {Version}, {nameof(OperativeSystem)}: {OperativeSystem}, {nameof(OpenGLVersion)}: {OpenGLVersion}";
        }
    }
    #endregion

    #region Model
    public sealed class Model : AnycubicNamedTable
    {
        protected override string DefaultTableName => "MODEL";

        protected override bool IncludeBaseTableLength => true;

        [FieldOrder(0)] public float MinX { get; set; }
        [FieldOrder(1)] public float MinY { get; set; }
        [FieldOrder(2)] public float MinZ { get; set; }
        [FieldOrder(3)] public float MaxX { get; set; }
        [FieldOrder(4)] public float MaxY { get; set; }
        [FieldOrder(5)] public float MaxZ { get; set; }
        [FieldOrder(6)] public uint SupportsEnabled { get; set; }
        [FieldOrder(7)] public float SupportsDensity { get; set; }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(MinX)}: {MinX}, {nameof(MinY)}: {MinY}, {nameof(MinZ)}: {MinZ}, {nameof(MaxX)}: {MaxX}, {nameof(MaxY)}: {MaxY}, {nameof(MaxZ)}: {MaxZ}, {nameof(SupportsEnabled)}: {SupportsEnabled}, {nameof(SupportsDensity)}: {SupportsDensity}";
        }

        public void Parse(FileFormat slicerFile)
        {
            var rect = slicerFile.BoundingRectangleMillimeters;
            rect.Offset(slicerFile.DisplayWidth / -2f, slicerFile.DisplayHeight / -2f);
            MinX = (float)Math.Round(rect.X, 4);
            MinY = (float)Math.Round(rect.Y, 4);
            MaxX = (float)Math.Round(rect.Right, 4);
            MaxY = (float)Math.Round(rect.Bottom, 4);

            MinZ = 0;
            MaxZ = slicerFile.PrintHeight;
        }
    }
    #endregion

    #region Layer

    public sealed class LayerDef
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

        [Ignore] public byte[] EncodedRle { get; set; } = null!;

        public LayerDef()
        {
        }

        public LayerDef(Layer layer)
        {
            SetFrom(layer);
        }

        public void SetFrom(Layer layer)
        {
            LayerHeight = layer.RelativePositionZ;
            ExposureTime = layer.ExposureTime;
            LiftHeight = layer.LiftHeight;
            LiftSpeed = SpeedConverter.Convert(layer.LiftSpeed, CoreSpeedUnit, SpeedUnit.MillimetersPerSecond);
            NonZeroPixelCount = layer.NonZeroPixelCount;
        }

        public void CopyTo(Layer layer)
        {
            // Don't forget to compute LayerHeight outside here
            layer.ExposureTime = ExposureTime;
            layer.LiftHeight = LiftHeight;
            layer.LiftSpeed = SpeedConverter.Convert(LiftSpeed, SpeedUnit.MillimetersPerSecond, CoreSpeedUnit);
        }

        public Mat Decode(PhotonWorkshopFile slicerFile, bool consumeData = true)
        {
            var result = slicerFile.RleFormat == PhotonRleFormat.PWS ? DecodePWS(slicerFile) : DecodePW0(slicerFile);
            if (consumeData)
                EncodedRle = null!;

            return result;
        }

        public byte[] Encode(PhotonWorkshopFile slicerFile, Mat image)
        {
            EncodedRle = slicerFile.RleFormat == PhotonRleFormat.PWS ? EncodePWS(slicerFile, image) : EncodePW0(image);
            return EncodedRle;
        }

        private unsafe Mat DecodePWS(PhotonWorkshopFile slicerFile)
        {
            var image = EmguExtensions.InitMat(slicerFile.Resolution);
            var span = image.GetBytePointer();
            var imageLength = image.GetLength();

            int index = 0;
            for (byte bit = 0; bit < slicerFile.AntiAliasing; bit++)
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
                        throw new FileLoadException("Image ran off the end.");
                    }
                }
            }

            for (int i = 0; i < imageLength; i++)
            {
                int newC = span[i] * (256 / slicerFile.AntiAliasing);

                if (newC > 0)
                {
                    newC--;
                }

                span[i] = (byte)newC;
            }
            
            return image;
        }

        public unsafe byte[] EncodePWS(PhotonWorkshopFile slicerFile, Mat image)
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

            for (byte aalevel = 1; aalevel <= slicerFile.AntiAliasing; aalevel++)
            {
                obit = false;
                rep = 0;

                //ngrey:= uint16(r | g | b)
                // thresholds:
                // aa 1:  127
                // aa 2:  255 127
                // aa 4:  255 191 127 63
                // aa 8:  255 223 191 159 127 95 63 31
                //byte threshold = (byte)(256 / slicerFile.AntiAliasing * aalevel - 1);
                // threshold := byte(int(255 * (level + 1) / (levels + 1))) + 1
                byte threshold = (byte) (255 * aalevel / (slicerFile.AntiAliasing + 1) + 1);


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

        private Mat DecodePW0(PhotonWorkshopFile slicerFile)
        {
            var mat = slicerFile.CreateMat();
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

                if (pixelPos + repeat > imageLength)
                {
                    mat.Dispose();
                    throw new FileLoadException($"Image ran off the end: {pixelPos} + {repeat} = {pixelPos + repeat}, expecting: {imageLength}");
                }

                // We only need to set the non-zero pixels
                mat.FillSpan(ref pixelPos, repeat, color);


                if (pixelPos == imageLength)
                {
                    //i++;
                    break;
                }
            }

            if (pixelPos > 0 && pixelPos != imageLength)
            {
                mat.Dispose();
                throw new FileLoadException($"Image ended short: {pixelPos}, expecting: {imageLength}");
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

    public sealed class SubLayerDef
    {
        /// <summary>
        /// Gets the layer image offset to encoded layer data, and its length in bytes.
        /// </summary>
        [FieldOrder(0)] public uint DataAddress { get; set; }

        /// <summary>
        /// Gets the layer image length in bytes.
        /// </summary>
        [FieldOrder(1)] public uint DataLength { get; set; }

        [FieldOrder(2)] public uint NonZeroPixelCount { get; set; }

        [FieldOrder(3)] public float Padding1 { get; set; }
        [FieldOrder(4)] public float Padding2 { get; set; }
        [FieldOrder(5)] public float Padding3 { get; set; }
        [FieldOrder(6)] public float Padding4 { get; set; }
        [FieldOrder(7)] public float Padding5 { get; set; }
        [FieldOrder(8)] public float Padding6 { get; set; }
        [FieldOrder(9)] public float Padding7 { get; set; }
        [FieldOrder(10)] public float Padding8 { get; set; }

        public override string ToString()
        {
            return $"{nameof(DataAddress)}: {DataAddress}, {nameof(DataLength)}: {DataLength}, {nameof(NonZeroPixelCount)}: {NonZeroPixelCount}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(Padding3)}: {Padding3}, {nameof(Padding4)}: {Padding4}, {nameof(Padding5)}: {Padding5}, {nameof(Padding6)}: {Padding6}, {nameof(Padding7)}: {Padding7}, {nameof(Padding8)}: {Padding8}";
        }

        public void SetFrom(LayerDef layerDef)
        {
            DataAddress = layerDef.DataAddress;
            DataLength = layerDef.DataLength;
            NonZeroPixelCount = layerDef.NonZeroPixelCount;
        }
    }

    #endregion

    #region LayerDefinition
    public sealed class LayerDefinition : AnycubicNamedTable
    {
        protected override string DefaultTableName => "LAYERDEF";

        [FieldOrder(0)] public uint LayerCount { get; set; }

        [FieldOrder(1)] [FieldCount(nameof(LayerCount))] public LayerDef[] Layers { get; set; } = Array.Empty<LayerDef>();

        public LayerDefinition()
        { }

        public LayerDefinition(uint layerCount) : this()
        {
            LayerCount = layerCount;
            Layers = new LayerDef[layerCount];
            TableLength += (uint) Helpers.Serializer.SizeOf(new LayerDef()) * LayerCount;
        }

        public LayerDefinition(FileFormat slicerFile) : this(slicerFile.LayerCount)
        { }

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

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(LayerCount)}: {LayerCount}";
        }
    }

    public sealed class SubLayerDefinition : AnycubicNamedTable
    {
        protected override string DefaultTableName => "SUBIMGS";

        protected override bool IncludeBaseTableLength => true;

        [FieldOrder(0)] public uint LayerCount { get; set; }
        [FieldOrder(1)] public uint Index { get; set; } = 1;

        [FieldOrder(2)] [FieldCount(nameof(LayerCount))] public SubLayerDef[] Layers { get; set; } = Array.Empty<SubLayerDef>();

        public SubLayerDefinition()
        { }

        public SubLayerDefinition(uint layerCount) : this()
        {
            LayerCount = layerCount;
            Layers = new SubLayerDef[layerCount];
            TableLength += (uint)Helpers.Serializer.SizeOf(new SubLayerDef()) * LayerCount;
        }

        public SubLayerDefinition(FileFormat slicerFile) : this(slicerFile.LayerCount)
        { }

        [Ignore]
        public SubLayerDef this[uint index]
        {
            get => Layers[index];
            set => Layers[index] = value;
        }

        [Ignore]
        public SubLayerDef this[int index]
        {
            get => Layers[index];
            set => Layers[index] = value;
        }

        public override string ToString()
        {
            return $"{base.ToString()}, {nameof(LayerCount)}: {LayerCount}, {nameof(Index)}: {Index}";
        }
    }
    #endregion


    #endregion

    #region Properties

    public FileMark FileMarkSettings { get; private set; } = new();

    public Header HeaderSettings { get; private set; } = new();
    public Preview PreviewSettings { get; private set; } = new();
    public Preview2 Preview2Settings { get; private set; } = new();
    public LayerImageColorTable LayerImageColorSettings { get; private set; } = new();

    public LayerDefinition LayersDefinition { get; private set; } = new();
    public SubLayerDefinition SubLayersDefinition { get; private set; } = new();
    public Extra ExtraSettings { get; private set; } = new();
    public Machine MachineSettings { get; private set; } = new();
    public Software SoftwareSettings { get; private set; } = new();
    public Model ModelSettings { get; private set; } = new();

    public override FileFormatType FileType => FileFormatType.Binary;

    public override PrinterManufacturingProcess ManufacturingProcess =>
        PrinterModel switch
        {
            AnyCubicMachine.PhotonUltra => PrinterManufacturingProcess.DLP,
            AnyCubicMachine.PhotonD2 => PrinterManufacturingProcess.DLP,
            _ => PrinterManufacturingProcess.mSLA
        };

    public override string ConvertMenuGroup => "Anycubic Photon Workshop";

    public override FileExtension[] FileExtensions { get; } = {

        new(typeof(PhotonWorkshopFile), "pws", "Photon / Photon S (PWS)"),
        new(typeof(PhotonWorkshopFile), "pw0", "Photon Zero (PW0)"),
        new(typeof(PhotonWorkshopFile), "pwx", "Photon X (PWX)"),
        new(typeof(PhotonWorkshopFile), "dlp", "Photon Ultra (DLP)"),
        new(typeof(PhotonWorkshopFile), "dl2p", "Photon Photon D2 (DL2P)"),
        new(typeof(PhotonWorkshopFile), "pwmx", "Photon Mono X (PWMX)"),
        new(typeof(PhotonWorkshopFile), "pmx2", "Photon Mono X2 (PMX2)"),
        new(typeof(PhotonWorkshopFile), "pwmb", "Photon Mono X 6K / Photon M3 Plus (PWMB)"),
        new(typeof(PhotonWorkshopFile), "px6s", "Photon Mono X 6Ks (PX6S)"),
        new(typeof(PhotonWorkshopFile), "pwmo", "Photon Mono (PWMO)"),
        new(typeof(PhotonWorkshopFile), "pm3n", "Photon Mono 2 (PM3N)"),
        new(typeof(PhotonWorkshopFile), "pwms", "Photon Mono SE (PWMS)"),
        new(typeof(PhotonWorkshopFile), "pwma", "Photon Mono 4K (PWMA)"),
        new(typeof(PhotonWorkshopFile), "pmsq", "Photon Mono SQ (PMSQ)"),
        new(typeof(PhotonWorkshopFile), "pm3", "Photon M3 (PM3)"),
        new(typeof(PhotonWorkshopFile), "pm3m", "Photon M3 Max (PM3M)"),
        new(typeof(PhotonWorkshopFile), "pm3r", "Photon M3 Premium (PM3R)"),
        new(typeof(PhotonWorkshopFile), "pm5", "Photon Mono M5 (PM5)"),
        new(typeof(PhotonWorkshopFile), "pm5s", "Photon Mono M5s (PM5s)"),
        new(typeof(PhotonWorkshopFile), "m5sp", "Photon Mono M5s Pro (M5sp)"),
        new(typeof(PhotonWorkshopFile), "pwc", "Anycubic Custom Machine (PWC)"),
        //new(typeof(PhotonWorkshopFile), "pwmb", "Photon M3 Plus (PWMB)"),
    };
    
    public override SpeedUnit FormatSpeedUnit => SpeedUnit.MillimetersPerSecond;

    public override PrintParameterModifier[] PrintParameterModifiers
    {
        get
        {
            if (FileMarkSettings.Version >= VERSION_516)
            {
                return new[]
                {
                    PrintParameterModifier.BottomLayerCount,
                    PrintParameterModifier.TransitionLayerCount,

                    PrintParameterModifier.WaitTimeBeforeCure,

                    PrintParameterModifier.BottomExposureTime,
                    PrintParameterModifier.ExposureTime,

                    PrintParameterModifier.BottomLiftHeight,
                    PrintParameterModifier.BottomLiftSpeed,
                    PrintParameterModifier.LiftHeight,
                    PrintParameterModifier.LiftSpeed,
                    PrintParameterModifier.BottomLiftHeight2,
                    PrintParameterModifier.BottomLiftSpeed2,
                    PrintParameterModifier.LiftHeight2,
                    PrintParameterModifier.LiftSpeed2,

                    PrintParameterModifier.BottomRetractSpeed,
                    PrintParameterModifier.RetractSpeed,
                    //PrintParameterModifier.BottomRetractHeight2,
                    PrintParameterModifier.BottomRetractSpeed2,
                    //PrintParameterModifier.RetractHeight2,
                    PrintParameterModifier.RetractSpeed2,
                };
            }

            return new[]
            {
                PrintParameterModifier.BottomLayerCount,
                PrintParameterModifier.TransitionLayerCount,

                PrintParameterModifier.WaitTimeBeforeCure,

                PrintParameterModifier.BottomExposureTime,
                PrintParameterModifier.ExposureTime,

                PrintParameterModifier.BottomLiftHeight,
                PrintParameterModifier.BottomLiftSpeed,
                PrintParameterModifier.LiftHeight,
                PrintParameterModifier.LiftSpeed,

                PrintParameterModifier.RetractSpeed,
            };
        }
    }

    public override PrintParameterModifier[] PrintParameterPerLayerModifiers { get; } = {
        PrintParameterModifier.PositionZ,
        PrintParameterModifier.ExposureTime,
        PrintParameterModifier.LiftHeight,
        PrintParameterModifier.LiftSpeed,
    };

    public override Size[] ThumbnailsOriginalSize => new Size[] { new(224, 168), new(330, 190) };
    /*public override Size[]? ThumbnailsOriginalSize =>
        Version switch
        {
            >= VERSION_518 => new Size[] { new(224, 168), new(330, 190) },
            _ => new Size[] { new(224, 168) }
        };*/

    public override uint[] AvailableVersions { get; } = { VERSION_1, VERSION_515, VERSION_516, VERSION_517, VERSION_518 };

    public override uint[] GetAvailableVersionsForExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return AvailableVersions;
        if (extension[0] == '.') extension = extension.Remove(0, 1).ToLower();

        switch (extension)
        {
            case "pws":
            case "pw0":
            case "pwx":
                return new uint[] {VERSION_1};
            case "pwmx":
            case "pwmo":
            case "pwms":
            case "pmsq":
            case "dlp":
                return new uint[] { VERSION_1, VERSION_515, VERSION_516 };
            case "pwma":
            case "pm3":
            case "pm3m":
                return new uint[] { VERSION_515, VERSION_516 };
            case "pwmb":
            case "dl2p":
            case "pmx2":
            case "pm3r":
                return new uint[] { VERSION_515, VERSION_516, VERSION_517 };
            case "pm3n":
            case "pm5":
            case "px6s":
                return new uint[] { VERSION_517 };
            case "pm5s":
            case "m5sp":
                return new uint[] { VERSION_518 };
            default:
                return AvailableVersions;
        }
    }

    public override uint DefaultVersion => VERSION_1;

    public override uint Version
    {
        get => FileMarkSettings.Version;
        set
        {
            base.Version = value;
            FileMarkSettings.Version = base.Version;
        }
    }

    public override uint ResolutionX
    {
        get => HeaderSettings.ResolutionX;
        set
        {
            MachineSettings.ResolutionX = (ushort)value;
            base.ResolutionX = HeaderSettings.ResolutionX = value;
        }
    }

    public override uint ResolutionY
    {
        get => HeaderSettings.ResolutionY;
        set
        {
            MachineSettings.ResolutionY = (ushort)value;
            base.ResolutionY = HeaderSettings.ResolutionY = value;
        }
    }

    public override float DisplayWidth
    {
        get
        {
            if (MachineSettings.DisplayWidth > 0) return MachineSettings.DisplayWidth;
            return PrinterModel switch
            {
                AnyCubicMachine.PhotonS => 68.04f,
                AnyCubicMachine.PhotonZero => 55.44f,
                AnyCubicMachine.PhotonX => 192,
                AnyCubicMachine.PhotonUltra => 102.40f,
                AnyCubicMachine.PhotonD2 => 130.56f,
                AnyCubicMachine.PhotonMono => 82.62f,
                AnyCubicMachine.PhotonMono2 => 143.36f,
                AnyCubicMachine.PhotonMonoSE => 82.62f,
                AnyCubicMachine.PhotonMono4K => 134.40f,
                AnyCubicMachine.PhotonMonoX => 192,
                AnyCubicMachine.PhotonMonoX2 => 196.61f,
                AnyCubicMachine.PhotonMonoX6KM3Plus => 198.15f,
                AnyCubicMachine.PhotonMonoX6Ks => 195.84f,
                AnyCubicMachine.PhotonMonoSQ => 120,
                AnyCubicMachine.PhotonM3 => 163.84f,
                AnyCubicMachine.PhotonM3Max => 298.08f,
                AnyCubicMachine.PhotonM3Premium => 218.88f,
                AnyCubicMachine.PhotonMonoM5 => 218.88f,
                AnyCubicMachine.PhotonMonoM5s => 218.88f,
                AnyCubicMachine.PhotonMonoM5sPro => 223.6416f,
                _ => 0
            };
        }
        set => base.DisplayWidth = MachineSettings.DisplayWidth = RoundDisplaySize(value);
    }
    public override float DisplayHeight
    {
        get
        {
            if (MachineSettings.DisplayHeight > 0) return MachineSettings.DisplayHeight;
            return PrinterModel switch
            {
                AnyCubicMachine.PhotonS => 120.96f,
                AnyCubicMachine.PhotonZero => 98.637f,
                AnyCubicMachine.PhotonX => 120,
                AnyCubicMachine.PhotonUltra => 57.60f,
                AnyCubicMachine.PhotonD2 => 73.44f,
                AnyCubicMachine.PhotonMono => 130.56f,
                AnyCubicMachine.PhotonMono2 => 89.60f,
                AnyCubicMachine.PhotonMonoSE => 130.56f,
                AnyCubicMachine.PhotonMono4K => 84,
                AnyCubicMachine.PhotonMonoX => 120,
                AnyCubicMachine.PhotonMonoX2 => 122.88f,
                AnyCubicMachine.PhotonMonoX6KM3Plus => 123.84f,
                AnyCubicMachine.PhotonMonoX6Ks => 122.40f,
                AnyCubicMachine.PhotonMonoSQ => 128,
                AnyCubicMachine.PhotonM3 => 102.40f,
                AnyCubicMachine.PhotonM3Max => 165.60f,
                AnyCubicMachine.PhotonM3Premium => 123.12f,
                AnyCubicMachine.PhotonMonoM5 => 122.88f,
                AnyCubicMachine.PhotonMonoM5s => 122.88f,
                AnyCubicMachine.PhotonMonoM5sPro => 126.976f,
                _ => 0
            };
        }
        set => base.DisplayHeight = MachineSettings.DisplayHeight = RoundDisplaySize(value);
    }

    public override float MachineZ
    {
        get
        {
            if (MachineSettings.MachineZ > 0) return MachineSettings.MachineZ;
            return PrinterModel switch
            {
                AnyCubicMachine.PhotonS => 165,
                AnyCubicMachine.PhotonZero => 150,
                AnyCubicMachine.PhotonX => 245,
                AnyCubicMachine.PhotonUltra => 165,
                AnyCubicMachine.PhotonD2 => 165,
                AnyCubicMachine.PhotonMono => 165,
                AnyCubicMachine.PhotonMono2 => 165,
                AnyCubicMachine.PhotonMonoSE => 160,
                AnyCubicMachine.PhotonMono4K => 165,
                AnyCubicMachine.PhotonMonoX => 245,
                AnyCubicMachine.PhotonMonoX2 => 200,
                AnyCubicMachine.PhotonMonoX6KM3Plus => 245,
                AnyCubicMachine.PhotonMonoX6Ks => 200,
                AnyCubicMachine.PhotonMonoSQ => 200,
                AnyCubicMachine.PhotonM3 => 180,
                AnyCubicMachine.PhotonM3Max => 300,
                AnyCubicMachine.PhotonM3Premium => 250,
                AnyCubicMachine.PhotonMonoM5 => 200,
                AnyCubicMachine.PhotonMonoM5s => 200,
                AnyCubicMachine.PhotonMonoM5sPro => 200,
                _ => 0
            };
        }
        set => base.MachineZ = MachineSettings.MachineZ = (float)Math.Round(value, 2);
    }

    public override FlipDirection DisplayMirror
    {
        get => FlipDirection.Horizontally;
        set {}
    }

    public override bool IsAntiAliasingEmulated => true;

    public override byte AntiAliasing
    {
        get => (byte) HeaderSettings.AntiAliasing;
        set
        {
            base.AntiAliasing = (byte)(HeaderSettings.AntiAliasing = Math.Clamp(value, (byte)1, (byte)16));
            ValidateAntiAliasingLevel();
        }
    }

    public override float LayerHeight
    {
        get => HeaderSettings.LayerHeight;
        set => base.LayerHeight = HeaderSettings.LayerHeight = Layer.RoundHeight(value);
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

    public override ushort TransitionLayerCount
    {
        get => (ushort)HeaderSettings.TransitionLayerCount;
        set => base.TransitionLayerCount = (ushort)(HeaderSettings.TransitionLayerCount = Math.Min(value, MaximumPossibleTransitionLayerCount));
    }

    public override float BottomLightOffDelay => BottomWaitTimeBeforeCure;

    public override float LightOffDelay => WaitTimeBeforeCure;

    public override float BottomWaitTimeBeforeCure => WaitTimeBeforeCure;

    public override float WaitTimeBeforeCure
    {
        get => HeaderSettings.WaitTimeBeforeCure;
        set => base.WaitTimeBeforeCure = HeaderSettings.WaitTimeBeforeCure = (float)Math.Round(value, 2);
    }

    public override float BottomExposureTime
    {
        get => HeaderSettings.BottomExposureTime;
        set => base.BottomExposureTime = HeaderSettings.BottomExposureTime = (float) Math.Round(value, 2);
    }

    public override float ExposureTime
    {
        get => HeaderSettings.ExposureTime;
        set
        {
            HeaderSettings.IntelligentMode = false;
            base.ExposureTime = HeaderSettings.ExposureTime = (float)Math.Round(value, 2);
        }
    }

    public override float BottomLiftHeight
    {
        get => FileMarkSettings.Version >= VERSION_516 ? ExtraSettings.BottomLiftHeight1 : base.BottomLiftHeight;
        set
        {
            value = (float)Math.Round(value, 2);
            ExtraSettings.BottomLiftHeight1 = value;
            if (FileMarkSettings.Version >= VERSION_516)
            {
                base.BottomLiftHeight = (float)Math.Round(value + ExtraSettings.BottomLiftHeight2, 2);
                foreach (var layer in this) // Fix layer value
                {
                    if (!layer.IsBottomLayer) continue;
                    layer.LiftHeight = base.BottomLiftHeight;
                }
            }
            else base.BottomLiftHeight = value;
        }
    }

    public override float BottomLiftSpeed
    {
        get => FileMarkSettings.Version >= VERSION_516 
            ? SpeedConverter.Convert(ExtraSettings.BottomLiftSpeed1, FormatSpeedUnit, CoreSpeedUnit) 
            : base.BottomLiftSpeed;
        set
        {
            value = (float)Math.Round(value, 2);
            ExtraSettings.BottomLiftSpeed1 = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            base.BottomLiftSpeed = value;
        }
    }

    public override float LiftHeight
    {
        get => FileMarkSettings.Version >= VERSION_516 ? ExtraSettings.LiftHeight1 : HeaderSettings.LiftHeight;
        set
        {
            value = (float)Math.Round(value, 2);
            ExtraSettings.LiftHeight1 = value;
            if (FileMarkSettings.Version >= VERSION_516)
            {
                base.LiftHeight = HeaderSettings.LiftHeight = (float)Math.Round(value + ExtraSettings.LiftHeight2, 2);
                foreach (var layer in this) // Fix layer value
                {
                    if(!layer.IsNormalLayer) continue;
                    layer.LiftHeight = base.LiftHeight;
                }
            }
            else base.LiftHeight = HeaderSettings.LiftHeight = value;
        }
    }

    public override float LiftSpeed
    {
        get => SpeedConverter.Convert(FileMarkSettings.Version >= VERSION_516 ? ExtraSettings.LiftSpeed1 : HeaderSettings.LiftSpeed, FormatSpeedUnit, CoreSpeedUnit);
        set
        {
            value = (float)Math.Round(value, 2);
            HeaderSettings.LiftSpeed = ExtraSettings.LiftSpeed1 = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            base.LiftSpeed = value;
        }
    }

    public override float BottomLiftHeight2
    {
        get => FileMarkSettings.Version >= VERSION_516 ? ExtraSettings.BottomLiftHeight2 : 0;
        set
        {
            if (FileMarkSettings.Version < VERSION_516) return;
            var bottomLiftHeight = BottomLiftHeight;
            ExtraSettings.BottomLiftHeight2 = (float)Math.Round(value, 2);
            BottomLiftHeight = bottomLiftHeight;
            base.BottomLiftHeight2 = ExtraSettings.BottomLiftHeight2;
            HeaderSettings.AdvancedMode = System.Convert.ToUInt32(IsUsingTSMC);
        }
    }

    public override float BottomLiftSpeed2
    {
        get => FileMarkSettings.Version >= VERSION_516 ? SpeedConverter.Convert(ExtraSettings.BottomLiftSpeed2, FormatSpeedUnit, CoreSpeedUnit) : 0;
        set
        {
            if (FileMarkSettings.Version < VERSION_516) return;
            value = (float)Math.Round(value, 2);
            ExtraSettings.BottomLiftSpeed2 = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            base.BottomLiftSpeed2 = value;
        }
    }

    public override float LiftHeight2
    {
        get => FileMarkSettings.Version >= VERSION_516 ? ExtraSettings.LiftHeight2 : 0;
        set
        {
            if (FileMarkSettings.Version < VERSION_516) return;
            var liftHeight = LiftHeight;
            ExtraSettings.LiftHeight2 = (float)Math.Round(value, 2);
            LiftHeight = liftHeight;
            base.BottomLiftHeight2 = ExtraSettings.LiftHeight2;
            HeaderSettings.AdvancedMode = System.Convert.ToUInt32(IsUsingTSMC);
        }
    }

    public override float LiftSpeed2
    {
        get => FileMarkSettings.Version >= VERSION_516 ? SpeedConverter.Convert(ExtraSettings.LiftSpeed2, FormatSpeedUnit, CoreSpeedUnit) : 0;
        set
        {
            if (FileMarkSettings.Version < VERSION_516) return;
            value = (float)Math.Round(value, 2);
            ExtraSettings.LiftSpeed2 = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            base.LiftSpeed2 = value;
        }
    }

    public override float BottomRetractSpeed
    {
        get => FileMarkSettings.Version >= VERSION_516 ? SpeedConverter.Convert(ExtraSettings.BottomRetractSpeed1, FormatSpeedUnit, CoreSpeedUnit) : RetractSpeed;
        set
        {
            value = (float)Math.Round(value, 2);

            if (FileMarkSettings.Version < VERSION_516) return;
            ExtraSettings.BottomRetractSpeed1 = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
        }
    }

    public override float RetractSpeed
    {
        get => SpeedConverter.Convert(FileMarkSettings.Version >= VERSION_516 ? ExtraSettings.RetractSpeed1 : HeaderSettings.RetractSpeed, FormatSpeedUnit, CoreSpeedUnit);
        set
        {
            value = (float)Math.Round(value, 2);
            ExtraSettings.RetractSpeed1 = HeaderSettings.RetractSpeed = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            base.RetractSpeed = value;
        }
    }

    public override float BottomRetractSpeed2
    {
        get => FileMarkSettings.Version >= VERSION_516 ? SpeedConverter.Convert(ExtraSettings.BottomRetractSpeed2, FormatSpeedUnit, CoreSpeedUnit) : 0;
        set
        {
            if (FileMarkSettings.Version < VERSION_516) return;
            value = (float)Math.Round(value, 2);
            ExtraSettings.BottomRetractSpeed2 = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            base.BottomRetractSpeed2 = value;
        }
    }

    public override float RetractSpeed2
    {
        get => FileMarkSettings.Version >= VERSION_516 ? SpeedConverter.Convert(ExtraSettings.RetractSpeed2, FormatSpeedUnit, CoreSpeedUnit) : 0;
        set
        {
            if (FileMarkSettings.Version < VERSION_516) return;
            value = (float)Math.Round(value, 2);
            ExtraSettings.RetractSpeed2 = SpeedConverter.Convert(value, CoreSpeedUnit, FormatSpeedUnit);
            base.RetractSpeed2 = value;
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
            if (!string.IsNullOrWhiteSpace(MachineSettings.MachineName)) return MachineSettings.MachineName;
            return PrinterModel switch
            {
                AnyCubicMachine.PhotonS       => "Photon S",
                AnyCubicMachine.PhotonZero    => "Photon Zero",
                AnyCubicMachine.PhotonX       => "Photon X",
                AnyCubicMachine.PhotonUltra   => "Photon Ultra",
                AnyCubicMachine.PhotonD2      => "Photon D2",
                AnyCubicMachine.PhotonMono    => "Photon Mono",
                AnyCubicMachine.PhotonMono2   => "Photon Mono 2",
                AnyCubicMachine.PhotonMonoSE  => "Photon Mono SE",
                AnyCubicMachine.PhotonMono4K  => "Photon Mono 4K",
                AnyCubicMachine.PhotonMonoX   => "Photon Mono X",
                AnyCubicMachine.PhotonMonoX2   => "Photon Mono X2",
                AnyCubicMachine.PhotonMonoX6KM3Plus => "Photon Mono X 6K / M3 Plus",
                AnyCubicMachine.PhotonMonoX6Ks => "Photon Mono X 6Ks",
                AnyCubicMachine.PhotonMonoSQ  => "Photon Mono SQ",
                AnyCubicMachine.PhotonM3  => "Photon M3",
                AnyCubicMachine.PhotonM3Max  => "Photon M3 Max",
                AnyCubicMachine.PhotonM3Premium => "Photon M3 Premium",
                AnyCubicMachine.PhotonMonoM5 => "Photon Mono M5",
                AnyCubicMachine.PhotonMonoM5s => "Photon Mono M5s",
                AnyCubicMachine.PhotonMonoM5sPro => "Photon Mono M5s Pro",
                AnyCubicMachine.Custom  => "Custom",
                _ => base.MachineName
            };
        }
        set => base.MachineName = MachineSettings.MachineName = value;
    }
        
    public override object[] Configs
    {
        get
        {
            switch (Version)
            {
                case VERSION_1:
                    return new object[] { FileMarkSettings, HeaderSettings, PreviewSettings, LayersDefinition };
                case VERSION_515:
                    return new object[]
                    {
                        FileMarkSettings, HeaderSettings, PreviewSettings, LayerImageColorSettings, LayersDefinition
                    };
                case VERSION_516:
                    return new object[]
                    {
                        FileMarkSettings, HeaderSettings, PreviewSettings, LayerImageColorSettings, LayersDefinition,
                        ExtraSettings, MachineSettings 
                    };
                case VERSION_517:
                    return new object[]
                    {
                        FileMarkSettings, HeaderSettings, PreviewSettings, LayerImageColorSettings, LayersDefinition,
                        ExtraSettings, MachineSettings, SoftwareSettings, ModelSettings
                    };
                default:
                    return new object[]
                    {
                        FileMarkSettings, HeaderSettings, PreviewSettings, LayerImageColorSettings, LayersDefinition,
                        ExtraSettings, MachineSettings, SoftwareSettings, ModelSettings, SubLayersDefinition, Preview2Settings
                    };
            }
        }
    }

    public override bool CanUseSameLayerPositionZ => false;

    public PhotonRleFormat RleFormat =>
        FileEndsWith(".pws")
            ? PhotonRleFormat.PWS
            : PhotonRleFormat.PW0;

    public AnyCubicMachine PrinterModel
    {
        get
        {
            if (FileEndsWith(".pws"))
            {
                return AnyCubicMachine.PhotonS;
            }

            if (FileEndsWith(".pw0"))
            {
                return AnyCubicMachine.PhotonZero;
            }

            if (FileEndsWith(".pwx"))
            {
                return AnyCubicMachine.PhotonX;
            }

            if (FileEndsWith(".dlp"))
            {
                return AnyCubicMachine.PhotonUltra;
            }

            if (FileEndsWith(".dl2p"))
            {
                return AnyCubicMachine.PhotonD2;
            }

            if (FileEndsWith(".pwmo"))
            {
                return AnyCubicMachine.PhotonMono;
            }

            if (FileEndsWith(".pm3n"))
            {
                return AnyCubicMachine.PhotonMono2;
            }

            if (FileEndsWith(".pwms"))
            {
                return AnyCubicMachine.PhotonMonoSE;
            }

            if (FileEndsWith(".pwma"))
            {
                return AnyCubicMachine.PhotonMono4K;
            }

            if (FileEndsWith(".pwmx"))
            {
                return AnyCubicMachine.PhotonMonoX;
            }

            if (FileEndsWith(".pmx2"))
            {
                return AnyCubicMachine.PhotonMonoX2;
            }

            if (FileEndsWith(".pwmb"))
            {
                return AnyCubicMachine.PhotonMonoX6KM3Plus;
            }

            if (FileEndsWith(".px6s"))
            {
                return AnyCubicMachine.PhotonMonoX6Ks;
            }

            if (FileEndsWith(".pmsq"))
            {
                return AnyCubicMachine.PhotonMonoSQ;
            }

            if (FileEndsWith(".pm3"))
            {
                return AnyCubicMachine.PhotonM3;
            }

            if (FileEndsWith(".pm3m"))
            {
                return AnyCubicMachine.PhotonM3Max;
            }

            if (FileEndsWith(".pm3r"))
            {
                return AnyCubicMachine.PhotonM3Premium;
            }

            if (FileEndsWith(".pm5"))
            {
                return AnyCubicMachine.PhotonMonoM5;
            }

            if (FileEndsWith(".pm5s"))
            {
                return AnyCubicMachine.PhotonMonoM5s;
            }

            if (FileEndsWith(".m5sp"))
            {
                return AnyCubicMachine.PhotonMonoM5sPro;
            }

            if (FileEndsWith(".pwc"))
            {
                return AnyCubicMachine.Custom;
            }

            return AnyCubicMachine.PhotonS;
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
        LayersDefinition = null!;
    }

    protected override void OnBeforeEncode(bool isPartialEncode)
    {
        HeaderSettings.PerLayerOverride = UsingPerLayerSettings;
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        FileMarkSettings.NumberOfTables = Version switch
        {
            VERSION_1 => 4,
            VERSION_515 => 5,
            VERSION_516 => 8,
            VERSION_517 => 9,
            VERSION_518 => 11,
            _ => 4
        };

        HeaderSettings.TableLength = Version switch
        {
            >= VERSION_518 => 96,
            >= VERSION_517 => 92,
            >= VERSION_516 => 84,
            _ => 80
        };

        MachineSettings.TableLength = Version switch
        {
            >= VERSION_518 => 224,
            _ => 156
        };

        MachineSettings.PropertyFields = Version switch
        {
            >= VERSION_518 => 15,
            >= VERSION_517 => 7,
            _ => 1
        };

        HeaderSettings.PixelSizeUm = PixelSizeMicronsMax;
        MachineSettings.PixelWidthUm = PixelWidthMicrons;
        MachineSettings.PixelHeightUm = PixelHeightMicrons;
        MachineSettings.LayerImageFormat = $"{RleFormat.ToString().ToLower()}Img";
        MachineSettings.MaxFileVersion = GetAvailableVersionsForExtension(FileExtension)[^1];

        FileMarkSettings.HeaderAddress = (uint) Helpers.Serializer.SizeOf(FileMarkSettings);
        FileMarkSettings.PreviewAddress = 0;
        FileMarkSettings.Preview2Address = 0;

        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Create, FileAccess.Write);

        outputFile.Seek((int)FileMarkSettings.HeaderAddress, SeekOrigin.Begin);
        outputFile.WriteSerialize(HeaderSettings);

        if (ThumbnailsCount >= 1)
        {
            FileMarkSettings.PreviewAddress = (uint)outputFile.Position;
            var preview = new Preview(Thumbnails[0]);
            outputFile.WriteSerialize(preview);
            outputFile.WriteBytes(preview.Data);
            preview.ResetData();
        }

        if (FileMarkSettings.Version >= VERSION_515)
        {
            FileMarkSettings.LayerImageColorTableAddress = (uint)outputFile.Position;
            outputFile.WriteSerialize(LayerImageColorSettings);
        }

        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);

        LayersDefinition = new LayerDefinition(LayerCount);
        SubLayersDefinition = new SubLayerDefinition(LayerCount);
        FileMarkSettings.LayerDefinitionAddress = (uint)outputFile.Position;
        FileMarkSettings.LayerImageAddress = FileMarkSettings.LayerDefinitionAddress + LayersDefinition.TableLength + AnycubicNamedTable.TableBaseLength;

        outputFile.Seek(FileMarkSettings.LayerImageAddress, SeekOrigin.Begin);
        
        if (Version >= VERSION_516)
        {
            FileMarkSettings.ExtraAddress = FileMarkSettings.LayerImageAddress;
            outputFile.WriteSerialize(ExtraSettings);

            FileMarkSettings.MachineAddress = (uint)outputFile.Position;
            outputFile.WriteSerialize(MachineSettings);

            if (Version >= VERSION_517)
            {
                FileMarkSettings.SoftwareAddress = (uint)outputFile.Position;
                SoftwareSettings = new Software();
                outputFile.WriteSerialize(SoftwareSettings);

                FileMarkSettings.ModelAddress = (uint)outputFile.Position;
                ModelSettings.Parse(this);
                outputFile.WriteSerialize(ModelSettings);

                if (Version >= VERSION_518)
                {
                    FileMarkSettings.SubLayerDefinitionAddress = (uint)outputFile.Position;
                    outputFile.Seek(SubLayersDefinition.TableLength, SeekOrigin.Current);

                    if (ThumbnailsCount >= 2)
                    {
                        FileMarkSettings.Preview2Address = (uint)outputFile.Position;
                        var preview = new Preview2(Thumbnails[1]);
                        outputFile.WriteSerialize(preview);
                        outputFile.WriteBytes(preview.Data);
                        preview.ResetData();
                    }
                }
            }

            FileMarkSettings.LayerImageAddress = (uint)outputFile.Position;
        }
        else if (FileMarkSettings.Version == VERSION_515)
        {
            FileMarkSettings.ExtraAddress = FileMarkSettings.LayerImageAddress;
        }

        //var layersHash = new Dictionary<string, LayerDef>();

        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                using (var mat = this[layerIndex].LayerMat)
                {
                    LayersDefinition[layerIndex] = new LayerDef(this[layerIndex]);
                    LayersDefinition[layerIndex].Encode(this, mat);
                    SubLayersDefinition[layerIndex] = new SubLayerDef();
                }
                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                progress.PauseOrCancelIfRequested();

                var layerDef = LayersDefinition.Layers[layerIndex];

                /*var hash = CryptExtensions.ComputeSHA1Hash(layerDef.EncodedRle);

                if (layersHash.TryGetValue(hash, out var layerDataHash))
                {
                    layerDef.DataAddress = layerDataHash.DataAddress;
                    layerDef.DataLength = layerDataHash.DataLength;
                }
                else
                {*/
                    layerDef.DataAddress = (uint)outputFile.Position;
                    SubLayersDefinition[layerIndex].SetFrom(layerDef);


                    //layersHash.Add(hash, layerDef);
                    
                    outputFile.WriteBytes(layerDef.EncodedRle);
                    layerDef.EncodedRle = null!;
                //}
            }
        }

        // Layers definition
        outputFile.Seek(FileMarkSettings.LayerDefinitionAddress, SeekOrigin.Begin);
        outputFile.WriteSerialize(LayersDefinition);

        if (Version >= VERSION_518)
        {
            // SubLayers definition
            outputFile.Seek(FileMarkSettings.SubLayerDefinitionAddress, SeekOrigin.Begin);
            outputFile.WriteSerialize(SubLayersDefinition);
        }

        // Header
        outputFile.Seek(0, SeekOrigin.Begin);
        outputFile.WriteSerialize(FileMarkSettings);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = new FileStream(FileFullPath!, FileMode.Open, FileAccess.Read);
        FileMarkSettings = Helpers.Deserialize<FileMark>(inputFile);

        Debug.Write("FileMark -> ");
        Debug.WriteLine(FileMarkSettings);

        if (!FileMarkSettings.Mark.Equals(FileMark.SectionMarkFile))
        {
            throw new FileLoadException($"Invalid Filemark {FileMarkSettings.Mark}, expected {FileMark.SectionMarkFile}", FileFullPath);
        }

        if (!AvailableVersions.Contains(FileMarkSettings.Version))
        {
            throw new FileLoadException($"Invalid Version {FileMarkSettings.Version}, expecting {string.Join(" or ", AvailableVersions)}", FileFullPath);
        }

        inputFile.Seek(FileMarkSettings.HeaderAddress, SeekOrigin.Begin);
        HeaderSettings = Helpers.Deserialize<Header>(inputFile);
        Debug.Write("Header -> ");
        Debug.WriteLine(HeaderSettings);

        HeaderSettings.Validate();

        if (FileMarkSettings.PreviewAddress > 0)
        {
            inputFile.Seek(FileMarkSettings.PreviewAddress, SeekOrigin.Begin);

            PreviewSettings = Helpers.Deserialize<Preview>(inputFile);
            PreviewSettings.Data = inputFile.ReadBytes(PreviewSettings.DataSize);
            Debug.Write("Preview 1 -> ");
            Debug.WriteLine(PreviewSettings);

            Thumbnails.Add(DecodeImage(DATATYPE_RGB565, PreviewSettings.Data, PreviewSettings.ResolutionX, PreviewSettings.ResolutionY));
            PreviewSettings.ResetData();
        }

        if (Version >= VERSION_515)
        {
            if (FileMarkSettings.LayerImageColorTableAddress > 0)
            {
                inputFile.Seek(FileMarkSettings.LayerImageColorTableAddress, SeekOrigin.Begin);
                LayerImageColorSettings = Helpers.Deserialize<LayerImageColorTable>(inputFile);
                Debug.Write("LayerImageColorTable -> ");
                Debug.WriteLine(LayerImageColorSettings);
            }

            if (Version >= VERSION_516)
            {
                if (FileMarkSettings.ExtraAddress > 0)
                {
                    inputFile.Seek(FileMarkSettings.ExtraAddress, SeekOrigin.Begin);
                    ExtraSettings = Helpers.Deserialize<Extra>(inputFile);
                    Debug.Write("Extra -> ");
                    Debug.WriteLine(ExtraSettings);
                    //ExtraSettings.ValidateExpecting(24);
                }

                if (FileMarkSettings.MachineAddress > 0)
                {
                    inputFile.Seek(FileMarkSettings.MachineAddress, SeekOrigin.Begin);
                    MachineSettings = Helpers.Deserialize<Machine>(inputFile);
                    MachineSettings.Validate();
                    Debug.Write("Machine -> ");
                    Debug.WriteLine(MachineSettings);
                    MachineSettings.Validate();
                }

                if (Version >= VERSION_517)
                {
                    if (FileMarkSettings.SoftwareAddress > 0)
                    {
                        inputFile.Seek(FileMarkSettings.SoftwareAddress, SeekOrigin.Begin);
                        SoftwareSettings = Helpers.Deserialize<Software>(inputFile);
                        Debug.Write("Software -> ");
                        Debug.WriteLine(SoftwareSettings);
                    }

                    if (FileMarkSettings.ModelAddress > 0)
                    {
                        inputFile.Seek(FileMarkSettings.ModelAddress, SeekOrigin.Begin);
                        ModelSettings = Helpers.Deserialize<Model>(inputFile);
                        Debug.Write("Model -> ");
                        Debug.WriteLine(ModelSettings);
                        //ModelSettings.Validate();
                    }

                    if (Version >= VERSION_518)
                    {
                        if (FileMarkSettings.SubLayerDefinitionAddress > 0)
                        {
                            inputFile.Seek(FileMarkSettings.SubLayerDefinitionAddress, SeekOrigin.Begin);
                            SubLayersDefinition = Helpers.Deserialize<SubLayerDefinition>(inputFile);
                            Debug.Write("SubLayers -> ");
                            Debug.WriteLine(SubLayersDefinition);
                            SubLayersDefinition.Validate();
                        }

                        if (FileMarkSettings.Preview2Address > 0)
                        {
                            inputFile.Seek(FileMarkSettings.Preview2Address, SeekOrigin.Begin);

                            Preview2Settings = Helpers.Deserialize<Preview2>(inputFile);
                            Preview2Settings.Data = inputFile.ReadBytes(Preview2Settings.DataSize);
                            Debug.Write("Preview 2 -> ");
                            Debug.WriteLine(Preview2Settings);
                            Preview2Settings.Validate(Preview2Settings.Data.Length);

                            Thumbnails.Add(DecodeImage(DATATYPE_RGB565, Preview2Settings.Data, Preview2Settings.ResolutionX, Preview2Settings.ResolutionY));
                            Preview2Settings.ResetData();
                        }
                    }
                }
            }
        }

        inputFile.Seek(FileMarkSettings.LayerDefinitionAddress, SeekOrigin.Begin);

        LayersDefinition = Helpers.Deserialize<LayerDefinition>(inputFile);
        Debug.Write("LayersDefinition -> ");
        Debug.WriteLine(LayersDefinition);
        LayersDefinition.Validate();
        
        Init(LayersDefinition.LayerCount, DecodeType == FileDecodeType.Partial);
            
        progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
        foreach (var batch in BatchLayersIndexes())
        {
            foreach (var layerIndex in batch)
            {
                progress.PauseOrCancelIfRequested();

                Debug.WriteLine($"Layer {layerIndex}: {LayersDefinition[layerIndex]}");

                if (DecodeType == FileDecodeType.Full)
                {
                    inputFile.Seek(LayersDefinition[layerIndex].DataAddress, SeekOrigin.Begin);
                    LayersDefinition[layerIndex].EncodedRle = inputFile.ReadBytes(LayersDefinition[layerIndex].DataLength);
                }
            }

            if (DecodeType == FileDecodeType.Full)
            {
                Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
                {
                    progress.PauseIfRequested();

                    using (var mat = LayersDefinition[layerIndex].Decode(this))
                    {
                        this[layerIndex] = new Layer((uint)layerIndex, mat, this)
                        {
                            PositionZ = LayersDefinition.Layers
                                .Where((_, i) => i <= layerIndex)
                                .Sum(def => def.LayerHeight),
                        };
                    }

                    progress.LockAndIncrement();
                });
            }
        }

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            LayersDefinition[layerIndex].CopyTo(this[layerIndex]);
        }

        if (FileMarkSettings.Version < VERSION_516)
        {
            // Fix the base.Bottoms
            SuppressRebuildPropertiesWork(() =>
            {
                BottomLiftHeight = FirstLayer?.LiftHeight ?? LiftHeight;
                BottomLiftSpeed = FirstLayer?.LiftSpeed ?? LiftSpeed;
            });
        }
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Open, FileAccess.Write);
        outputFile.Seek(FileMarkSettings.HeaderAddress, SeekOrigin.Begin);
        outputFile.WriteSerialize(HeaderSettings);

        if (Version >= VERSION_516)
        {
            if (FileMarkSettings.ExtraAddress > 0)
            {
                outputFile.Seek(FileMarkSettings.ExtraAddress, SeekOrigin.Begin);
                outputFile.WriteSerialize(ExtraSettings);
            }

            if (FileMarkSettings.MachineAddress > 0)
            {
                outputFile.Seek(FileMarkSettings.MachineAddress, SeekOrigin.Begin);
                outputFile.WriteSerialize(MachineSettings);
            }

            if (Version >= VERSION_517)
            {
                if (FileMarkSettings.SoftwareAddress > 0)
                {
                    outputFile.Seek(FileMarkSettings.SoftwareAddress, SeekOrigin.Begin);
                    SoftwareSettings = new Software();
                    outputFile.WriteSerialize(SoftwareSettings);
                }

                if (FileMarkSettings.ModelAddress > 0)
                {
                    outputFile.Seek(FileMarkSettings.ModelAddress, SeekOrigin.Begin);
                    ModelSettings.Parse(this);
                    outputFile.WriteSerialize(ModelSettings);
                }
            }
        }

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            LayersDefinition[layerIndex].SetFrom(this[layerIndex]);
        }

        outputFile.Seek(FileMarkSettings.LayerDefinitionAddress, SeekOrigin.Begin);
        outputFile.WriteSerialize(LayersDefinition);
    }

    #endregion
}