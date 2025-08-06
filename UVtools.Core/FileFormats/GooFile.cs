using BinarySerialization;
using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using UVtools.Core.Exceptions;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;
using ZLinq;

namespace UVtools.Core.FileFormats;

public sealed class GooFile : FileFormat
{

    #region Constants

    private const string FileVersion = "V3.0";

    private static readonly byte[] FileMagic =
    [
        0x07, 0x00, 0x00, 0x00,
        0x44, 0x4C, 0x50, 0x00
    ];

    private static byte[] Delimiter => [0x0D, 0x0A];

    private static byte LayerMagic => 0x55;

    #endregion

    #region Enums
    public enum DelayModes : byte
    {
        /// <summary>
        /// Time with motor movement
        /// </summary>
        LightOff = 0,

        /// <summary>
        /// Absolute time to wait
        /// </summary>
        WaitTime = 1
    }
    #endregion

    #region Sub Classes

    public class FileHeader
    {
        [FieldEndianness(Endianness.Big)] [FieldOrder(0)] [FieldLength(4)] public string Version { get; set; } = FileVersion;
        [FieldOrder(1)] [FieldCount(8)] public byte[] Magic { get; set; } = FileMagic;
        [FieldOrder(2)] [FieldLength(32)] [SerializeAs(SerializedType.TerminatedString)] public string SoftwareName { get; set; } = About.Software;
        [FieldOrder(3)] [FieldLength(24)] [SerializeAs(SerializedType.TerminatedString)] public string SoftwareVersion { get; set; } = About.VersionString;
        [FieldOrder(4)] [FieldLength(24)] [SerializeAs(SerializedType.TerminatedString)] public string FileCreateTime { get; set; } = DateTime.UtcNow.ToString("yyyy-mm-dd HH:mm:ss");
        [FieldOrder(5)] [FieldLength(32)] [SerializeAs(SerializedType.TerminatedString)] public string MachineName { get; set; } = DefaultMachineName;
        [FieldOrder(6)] [FieldLength(32)] [SerializeAs(SerializedType.TerminatedString)] public string MachineType { get; set; } = "DLP";
        [FieldOrder(7)] [FieldLength(32)] [SerializeAs(SerializedType.TerminatedString)] public string ProfileName { get; set; } = About.Software;
        [FieldEndianness(Endianness.Big)] [FieldOrder(8)] public ushort AntiAliasingLevel { get; set; } = 8;
        [FieldEndianness(Endianness.Big)] [FieldOrder(9)] public ushort GreyLevel { get; set; } = 1;
        [FieldEndianness(Endianness.Big)] [FieldOrder(10)] public ushort BlurLevel { get; set; } = 0;
        [FieldOrder(11)] [FieldCount(116 * 116 * 2)] public byte[] SmallPreview565 { get; set; } = [];
        [FieldOrder(12)] [FieldCount(2)] public byte[] SmallPreviewDelimiter { get; set; } = Delimiter;
        [FieldOrder(13)] [FieldCount(290 * 290 * 2)] public byte[] BigPreview565 { get; set; } = [];
        [FieldOrder(14)] [FieldCount(2)] public byte[] BigPreviewDelimiter { get; set; } = Delimiter;
        [FieldEndianness(Endianness.Big)] [FieldOrder(15)] public uint LayerCount { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(16)] public ushort ResolutionX { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(17)] public ushort ResolutionY { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(18)] public bool MirrorX { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(19)] public bool MirrorY { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(20)] public float DisplayWidth { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(21)] public float DisplayHeight { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(22)] public float MachineZ { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(23)] public float LayerHeight { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(24)] public float ExposureTime { get; set; }
        /// <summary>
        ///  0: Light off delay mode | 1：Wait time mode
        /// </summary>
        [FieldEndianness(Endianness.Big)] [FieldOrder(25)] public DelayModes DelayMode { get; set; } = DelayModes.WaitTime;
        [FieldEndianness(Endianness.Big)] [FieldOrder(26)] public float LightOffDelay { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(27)] public float BottomWaitTimeAfterCure { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(28)] public float BottomWaitTimeAfterLift { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(29)] public float BottomWaitTimeBeforeCure { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(30)] public float WaitTimeAfterCure { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(31)] public float WaitTimeAfterLift { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(32)] public float WaitTimeBeforeCure { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(33)] public float BottomExposureTime { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(34)] public uint BottomLayerCount { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(35)] public float BottomLiftHeight { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(36)] public float BottomLiftSpeed { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(37)] public float LiftHeight { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(38)] public float LiftSpeed { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(39)] public float BottomRetractHeight { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(40)] public float BottomRetractSpeed { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(41)] public float RetractHeight { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(42)] public float RetractSpeed { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(43)] public float BottomLiftHeight2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(44)] public float BottomLiftSpeed2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(45)] public float LiftHeight2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(46)] public float LiftSpeed2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(47)] public float BottomRetractHeight2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(48)] public float BottomRetractSpeed2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(49)] public float RetractHeight2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(50)] public float RetractSpeed2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(51)] public ushort BottomLightPWM { get; set; } = DefaultBottomLightPWM;
        [FieldEndianness(Endianness.Big)] [FieldOrder(52)] public ushort LightPWM { get; set; } = DefaultLightPWM;
        /// <summary>
        /// <para>0: Normal mode</para>
        /// <para>1: Advance mode, printing use the value of "Layer Definition Content"</para>
        /// </summary>
        [FieldEndianness(Endianness.Big)] [FieldOrder(53)] public bool PerLayerSettings { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(54)] public uint PrintTime { get; set; }
        /// <summary>
        /// // The volume of all parts. unit: mm3
        /// </summary>
        [FieldEndianness(Endianness.Big)] [FieldOrder(55)] public float Volume { get; set; }
        /// <summary>
        /// The weight of all parts. unit: g
        /// </summary>
        [FieldEndianness(Endianness.Big)] [FieldOrder(56)] public float MaterialGrams { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(57)] public float MaterialCost { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(58)] [FieldLength(8)] [SerializeAs(SerializedType.TerminatedString)] public string PriceCurrencySymbol { get; set; } = "$";
        [FieldEndianness(Endianness.Big)] [FieldOrder(59)] public uint LayerDefAddress { get; set; } // 195477
        /// <summary>
        /// <para>0：The range of pixel's gray value is from 0x0 ~ 0xf</para>
        /// <para>1：The range of pixel's gray value is from 0x0 ~ 0xff</para>
        /// </summary>
        [FieldEndianness(Endianness.Big)] [FieldOrder(60)] public byte GrayScaleLevel { get; set; } = 1;
        [FieldEndianness(Endianness.Big)] [FieldOrder(61)] public ushort TransitionLayerCount { get; set; }

        public override string ToString()
        {
            return $"{nameof(Version)}: {Version}, {nameof(Magic)}: {Magic}, {nameof(SoftwareName)}: {SoftwareName}, {nameof(SoftwareVersion)}: {SoftwareVersion}, {nameof(FileCreateTime)}: {FileCreateTime}, {nameof(MachineName)}: {MachineName}, {nameof(MachineType)}: {MachineType}, {nameof(ProfileName)}: {ProfileName}, {nameof(AntiAliasingLevel)}: {AntiAliasingLevel}, {nameof(GreyLevel)}: {GreyLevel}, {nameof(BlurLevel)}: {BlurLevel}, {nameof(SmallPreview565)}: {SmallPreview565}, {nameof(SmallPreviewDelimiter)}: {SmallPreviewDelimiter}, {nameof(BigPreview565)}: {BigPreview565}, {nameof(BigPreviewDelimiter)}: {BigPreviewDelimiter}, {nameof(LayerCount)}: {LayerCount}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(MirrorX)}: {MirrorX}, {nameof(MirrorY)}: {MirrorY}, {nameof(DisplayWidth)}: {DisplayWidth}, {nameof(DisplayHeight)}: {DisplayHeight}, {nameof(MachineZ)}: {MachineZ}, {nameof(LayerHeight)}: {LayerHeight}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(DelayMode)}: {DelayMode}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(BottomWaitTimeAfterCure)}: {BottomWaitTimeAfterCure}, {nameof(BottomWaitTimeAfterLift)}: {BottomWaitTimeAfterLift}, {nameof(BottomWaitTimeBeforeCure)}: {BottomWaitTimeBeforeCure}, {nameof(WaitTimeAfterCure)}: {WaitTimeAfterCure}, {nameof(WaitTimeAfterLift)}: {WaitTimeAfterLift}, {nameof(WaitTimeBeforeCure)}: {WaitTimeBeforeCure}, {nameof(BottomExposureTime)}: {BottomExposureTime}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(BottomRetractHeight)}: {BottomRetractHeight}, {nameof(BottomRetractSpeed)}: {BottomRetractSpeed}, {nameof(RetractHeight)}: {RetractHeight}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(BottomLiftHeight2)}: {BottomLiftHeight2}, {nameof(BottomLiftSpeed2)}: {BottomLiftSpeed2}, {nameof(LiftHeight2)}: {LiftHeight2}, {nameof(LiftSpeed2)}: {LiftSpeed2}, {nameof(BottomRetractHeight2)}: {BottomRetractHeight2}, {nameof(BottomRetractSpeed2)}: {BottomRetractSpeed2}, {nameof(RetractHeight2)}: {RetractHeight2}, {nameof(RetractSpeed2)}: {RetractSpeed2}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(LightPWM)}: {LightPWM}, {nameof(PerLayerSettings)}: {PerLayerSettings}, {nameof(PrintTime)}: {PrintTime}, {nameof(Volume)}: {Volume}, {nameof(MaterialGrams)}: {MaterialGrams}, {nameof(MaterialCost)}: {MaterialCost}, {nameof(PriceCurrencySymbol)}: {PriceCurrencySymbol}, {nameof(LayerDefAddress)}: {LayerDefAddress}, {nameof(GrayScaleLevel)}: {GrayScaleLevel}, {nameof(TransitionLayerCount)}: {TransitionLayerCount}";
        }
    }

    public class LayerDef
    {
        /// <summary>
        /// 0: reserve
        /// 1: current layer pause printing
        /// </summary>
        [FieldEndianness(Endianness.Big)] [FieldOrder(0)] public ushort Pause { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(1)] public float PausePositionZ { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(2)] public float PositionZ { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(3)] public float ExposureTime { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(4)] public float LightOffDelay { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(5)] public float WaitTimeAfterCure { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(6)] public float WaitTimeAfterLift { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(7)] public float WaitTimeBeforeCure { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(8)] public float LiftHeight { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(9)] public float LiftSpeed { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(10)] public float LiftHeight2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(11)] public float LiftSpeed2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(12)] public float RetractHeight { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(13)] public float RetractSpeed { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(14)] public float RetractHeight2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(15)] public float RetractSpeed2 { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(16)] public ushort LightPWM { get; set; }
        [FieldEndianness(Endianness.Big)] [FieldOrder(17)] [FieldCount(2)] public byte[] DelimiterData { get; set; } = Delimiter;
        [FieldEndianness(Endianness.Big)] [FieldOrder(18)] public uint DataLength { get; set; }


        [Ignore] public GooFile? Parent { get; set; }

        [Ignore] public byte[] EncodedRle { get; set; } = [];

        // DelimiterRLE

        public LayerDef() { }

        public LayerDef(GooFile parent, Layer layer)
        {
            Parent = parent;
            PausePositionZ = parent.MachineZ;
            SetFrom(layer);
        }

        public void SetFrom(Layer layer)
        {
            PositionZ = layer.PositionZ;
            ExposureTime = layer.ExposureTime;
            LightOffDelay = layer.LightOffDelay;
            LiftHeight = layer.LiftHeight;
            LiftSpeed = layer.LiftSpeed;
            LiftHeight2 = layer.LiftHeight2;
            LiftSpeed2 = layer.LiftSpeed2;
            RetractSpeed = layer.RetractSpeed;
            RetractHeight = layer.RetractHeight;
            RetractHeight2 = layer.RetractHeight2;
            RetractSpeed2 = layer.RetractSpeed2;
            WaitTimeAfterCure = layer.WaitTimeAfterCure;
            WaitTimeAfterLift = layer.WaitTimeAfterLift;
            WaitTimeBeforeCure = layer.WaitTimeBeforeCure;
            LightPWM = layer.LightPWM;
        }

        public void CopyTo(Layer layer)
        {
            layer.PositionZ = PositionZ;
            layer.ExposureTime = ExposureTime;
            layer.LightOffDelay = LightOffDelay;
            layer.LiftHeight = LiftHeight;
            layer.LiftSpeed = LiftSpeed;
            layer.LiftHeight2 = LiftHeight2;
            layer.LiftSpeed2 = LiftSpeed2;
            layer.RetractSpeed = RetractSpeed;
            layer.RetractHeight2 = RetractHeight2;
            layer.RetractSpeed2 = RetractSpeed2;
            layer.WaitTimeAfterCure = WaitTimeAfterCure;
            layer.WaitTimeAfterLift = WaitTimeAfterLift;
            layer.WaitTimeBeforeCure = WaitTimeBeforeCure;
            layer.LightPWM = (byte)LightPWM;
        }


        public Mat DecodeImage(uint layerIndex, bool consumeRle = true)
        {
            var mat = EmguExtensions.InitMat(Parent!.Resolution);

            if (DataLength <= 3) return mat;

            if (EncodedRle[0] != LayerMagic) throw new MessageException($"RLE for layer {layerIndex} is corrupted, should start with {LayerMagic} but got {EncodedRle[0]}");

            int pixel = 0;
            var lastByteIndex = DataLength - 1;
            byte color = 0;
            byte checkSum = 0;

            for (var i = 1; i < lastByteIndex; i++)
            {
                // Calculate checksum
                unchecked
                {
                    checkSum += EncodedRle[i];
                }
            }
            checkSum = (byte)~checkSum;
            if (EncodedRle[^1] != checkSum) throw new MessageException($"Decoded RLE for layer {layerIndex} is corrupted, expected checksum <{EncodedRle[^1]}>, got <{checkSum}>");

            for (var i = 1; i < lastByteIndex; i++)
            {
                /* Byte0[7:6]: The type of chunk
                 * (0x0) 0 0 This chunk contain all 0x0 pixels
                 * (0x1) 0 1 This chunk contain the value of gray between 0x1 to 0xfe. The gray value is after byte0.
                 * (0x2) 1 0 This chunk contain the diff value from the previous pixel
                 * (0x3) 1 1 This chunk contain all 0xff pixels
                */
                byte chunkType = (byte)(EncodedRle[i] >> 6);
                int stride = 0;

                int strideIndex0 = i;
                int strideIndex1 = i + 1;
                int strideIndex2 = i + 2;
                int strideIndex3 = i + 3;

                if (chunkType == 0x0) // 0 0
                {
                    color = byte.MinValue;
                }
                else if (chunkType == 0x1) // 0 1
                {
                    color = EncodedRle[++i];
                    strideIndex1++;
                    strideIndex2++;
                    strideIndex3++;
                }
                else if (chunkType == 0x2) // 1 0
                {
                    /* When byte0[7:6] is [1:0], the meaning of byte0[5:4] follow below definition:
                     * 0 0 byte0[3:0] is the positive diff value. that's mean
                           current value subtract previous value is bigger
                           than 0. The range is from 0 to 15. 0x0 map to 0.
                           0xf map to 15.
                     * 0 1 byte0[3:0] is the positive diff value. And this
                           value's run-length represent by byte1[7:0]
                     * 1 0 byte0[3:0] is the negative diff value.that's mean
                           current value subtract previous value is smaller
                           than 0. The range is from 0 to 15. 0x0 map to 0.
                           0xf map to 15.
                     * 1 1 byte0[3:0] is the negative diff value. And this
                           value's run-length represent by byte1[7:0]
                    */
                    byte diffType = (byte)(EncodedRle[i] >> 4 & 0x3);
                    byte diffValue = (byte)(EncodedRle[i] & 0xf);
                    if (diffType == 0x0)
                    {
                        color += diffValue;
                        stride = 1;
                    }
                    else if (diffType == 0x1)
                    {
                        color += diffValue;
                        stride = EncodedRle[++i];
                    }
                    else if (diffType == 0x2)
                    {
                        color -= diffValue;
                        stride = 1;
                    }
                    else if (diffType == 0x3)
                    {
                        color -= diffValue;
                        stride = EncodedRle[++i];
                    }
                    else
                    {
                        throw new ArgumentOutOfRangeException(nameof(diffType), $"Diff type {diffType:X} is out of range, can only go up to 0x3.");
                    }
                }
                else if (chunkType == 0x3) // 1 1
                {
                    color = byte.MaxValue;
                }
                else
                {
                    throw new ArgumentOutOfRangeException(nameof(chunkType), $"Chunk type {chunkType:X} is out of range, can only go up to 0x3.");
                }

                if (chunkType != 0x2)
                {
                    /* Byte0[5:4]: The length of chunk except when byte0[7:6] is [1 0]
                     * (0x0) 0 0 4-bit run-length use byte0[3:0]
                     * (0x1) 0 1 The run-length consist by byte1[7:0] and byte0[3:0]
                     * (0x2) 1 0 The run-length consist by byte1[7:0], byte2[7:0] and byte0[3:0]
                     * (0x3) 1 1 The run-length consist by byte1[7:0], byte2[7:0], byte3[7:0] and byte0[3:0]
                     */
                    byte chunkLength = (byte) (EncodedRle[strideIndex0] >> 4 & 0x3);
                    switch (chunkLength)
                    {
                        case 0x0:
                            stride = EncodedRle[strideIndex0] & 0xF;
                            break;
                        case 0x1:
                            stride = (EncodedRle[strideIndex1] << 4) + (EncodedRle[strideIndex0] & 0xF);
                            i += 1;
                            break;
                        case 0x2:
                            stride = (EncodedRle[strideIndex1] << 12) + (EncodedRle[strideIndex2] << 4) + (EncodedRle[strideIndex0] & 0xF);
                            i += 2;
                            break;
                        case 0x3:
                            stride = (EncodedRle[strideIndex1] << 20) + (EncodedRle[strideIndex2] << 12) + (EncodedRle[strideIndex3] << 4) + (EncodedRle[strideIndex0] & 0xF);
                            i += 3;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(chunkLength), $"Chunk length {chunkLength:X} is out of range, can only go up to 0x3.");
                    }
                }

                mat.FillSpan(ref pixel, stride, color);
            }

            if (consumeRle) EncodedRle = null!;

            return mat;
        }

        public byte[] EncodeImage(Mat image, uint layerIndex, bool useColorDifferenceCompression = true)
        {
            List<byte> rle = [LayerMagic];
            byte previousColor = 0;
            byte currentColor = 0;
            uint stride = 0;
            byte checkSum = 0;
            var span = image.GetDataByteReadOnlySpan();

            void AddRep()
            {
                if (stride == 0)
                {
                    return;
                }

                int firstByteIndex = rle.Count;
                rle.Add(0);

                // Difference mode
                var colorDifference = (byte) Math.Abs(currentColor - previousColor);
                if (useColorDifferenceCompression && colorDifference <= 0xF && stride <= byte.MaxValue && currentColor is > 0 and < byte.MaxValue)
                {
                    rle[firstByteIndex] = (byte) (0b10 << 6 | (colorDifference & 0xF));
                    if (stride > 1)
                    {
                        rle[firstByteIndex] |= 0x1 << 4;
                        rle.Add((byte)stride);
                    }

                    if (currentColor < previousColor)
                    {
                        rle[firstByteIndex] |= 0x1 << 5;
                    }
                }
                else
                {
                    /*if (currentColor == byte.MinValue)
                    {
                        0 0 This chunk contain all 0x0 pixels
                        firstByte |= 0b00 << 6;
                    }*/
                    if (currentColor == byte.MaxValue)
                    {
                        // 1 1 This chunk contain all 0xff pixels
                        rle[firstByteIndex] |= 0b11 << 6;
                    }
                    else if (currentColor > byte.MinValue)
                    {
                        // 0 1 This chunk contain the value of gray between 0x1 to 0xfe. The gray value is after byte0.
                        rle[firstByteIndex] |= 0b01 << 6;
                        rle.Add(currentColor);
                    }

                    rle[firstByteIndex] |= (byte) (stride & 0xF);
                    if (stride <= 0xF)
                    {
                        //rle[firstByteIndex] |= 0b00 << 4;
                        return;
                    }

                    if (stride <= 0xFFF)
                    {
                        rle[firstByteIndex] |= 0b01 << 4;
                        rle.Add((byte) (stride >> 4));
                        return;
                    }

                    if (stride <= 0xFFFFF)
                    {
                        rle[firstByteIndex] |= 0b10 << 4;
                        rle.Add((byte) (stride >> 12));
                        rle.Add((byte) (stride >> 4));
                        return;
                    }

                    if (stride <= 0xFFFFFFF)
                    {
                        rle[firstByteIndex] |= 0b11 << 4;
                        rle.Add((byte) (stride >> 20));
                        rle.Add((byte) (stride >> 12));
                        rle.Add((byte) (stride >> 4));
                        return;
                    }
                }
            }

            for (int i = 0; i < span.Length; i++)
            {
                if (currentColor == span[i])
                {
                    stride++;
                }
                else
                {
                    AddRep();
                    stride = 1;
                    previousColor = currentColor;
                    currentColor = span[i];
                }
            }

            AddRep();

            // Calculate checksum
            for (int i = 1; i < rle.Count; i++)
            {
                unchecked
                {
                    checkSum += rle[i];
                }
            }

            rle.Add((byte) ~checkSum);

            EncodedRle =  rle.ToArray();
            DataLength = (uint)EncodedRle.Length;

            return EncodedRle;
        }

        public override string ToString()
        {
            return $"{nameof(Pause)}: {Pause}, {nameof(PausePositionZ)}: {PausePositionZ}, {nameof(PositionZ)}: {PositionZ}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(WaitTimeAfterCure)}: {WaitTimeAfterCure}, {nameof(WaitTimeAfterLift)}: {WaitTimeAfterLift}, {nameof(WaitTimeBeforeCure)}: {WaitTimeBeforeCure}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(LiftHeight2)}: {LiftHeight2}, {nameof(LiftSpeed2)}: {LiftSpeed2}, {nameof(RetractHeight)}: {RetractHeight}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(RetractHeight2)}: {RetractHeight2}, {nameof(RetractSpeed2)}: {RetractSpeed2}, {nameof(LightPWM)}: {LightPWM}, {nameof(DelimiterData)}: {DelimiterData}, {nameof(DataLength)}: {DataLength}, {nameof(Parent)}: {Parent}, {nameof(EncodedRle)}: {EncodedRle}";
        }
    }

    public class FileFooter
    {
        [FieldOrder(0)] public byte Padding1 { get; set; }
        [FieldOrder(1)] public byte Padding2 { get; set; }
        [FieldOrder(2)] public byte Padding3 { get; set; }
        [FieldOrder(3)] [FieldCount(8)] public byte[] Magic { get; set; } = FileMagic;

        public override string ToString()
        {
            return $"{nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(Padding3)}: {Padding3}, {nameof(Magic)}: {Magic}";
        }
    }

    #endregion

    #region Properties
    public override FileFormatType FileType => FileFormatType.Binary;

    public override FileExtension[] FileExtensions { get; } =
    [
        new(typeof(GooFile), "goo", "Elegoo GOO"),
        new(typeof(GooFile), "prz", "Phrozen Sonic Mini 8K S (PRZ)")
    ];

    public override Size[] ThumbnailsOriginalSize { get; } =
    [
        new(116, 116),
        new(290, 290)
    ];

    public FileHeader Header { get; private set; } = new();

    public LayerDef[]? LayersDefinition { get; private set; }

    public FileFooter Footer { get; private set; } = new();

    public override PrintParameterModifier[] PrintParameterModifiers
    {
        get
        {
            if (HaveTiltingVat)
            {
                return
                [
                    PrintParameterModifier.BottomLayerCount,
                    PrintParameterModifier.TransitionLayerCount,

                    //PrintParameterModifier.BottomLightOffDelay,
                    PrintParameterModifier.LightOffDelay,

                    PrintParameterModifier.BottomWaitTimeBeforeCure,
                    PrintParameterModifier.WaitTimeBeforeCure,

                    PrintParameterModifier.BottomExposureTime,
                    PrintParameterModifier.ExposureTime,

                    PrintParameterModifier.BottomWaitTimeAfterCure,
                    PrintParameterModifier.WaitTimeAfterCure,

                    PrintParameterModifier.BottomWaitTimeAfterLift,
                    PrintParameterModifier.WaitTimeAfterLift,

                    PrintParameterModifier.BottomLightPWM,
                    PrintParameterModifier.LightPWM
                ];
            }

            return
            [
                PrintParameterModifier.BottomLayerCount,
                PrintParameterModifier.TransitionLayerCount,

                //PrintParameterModifier.BottomLightOffDelay,
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
        }
    }

    public override PrintParameterModifier[] PrintParameterPerLayerModifiers
    {
        get
        {
            if (!IsPerLayerSettingsAllowed) return base.PrintParameterPerLayerModifiers;

            if (HaveTiltingVat)
            {
                return
                [
                    PrintParameterModifier.PositionZ,
                    PrintParameterModifier.LightOffDelay,
                    PrintParameterModifier.WaitTimeBeforeCure,
                    PrintParameterModifier.ExposureTime,
                    PrintParameterModifier.WaitTimeAfterCure,
                    PrintParameterModifier.WaitTimeAfterLift,
                    PrintParameterModifier.LightPWM
                ];
            }

            return
            [
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

    public override bool HaveTiltingVat
    {
        get
        {
            if (MachineName.Contains("Saturn 4 Ultra", StringComparison.OrdinalIgnoreCase)) return true;
            if (MachineName.Contains("Mars 5 Ultra", StringComparison.OrdinalIgnoreCase)) return true;
            return LiftHeight == 0;
        }
    }

    public override uint ResolutionX
    {
        get => Header.ResolutionX;
        set => base.ResolutionX = Header.ResolutionX = (ushort)value;
    }

    public override uint ResolutionY
    {
        get => Header.ResolutionY;
        set => base.ResolutionY = Header.ResolutionY = (ushort)value;
    }

    public override float LayerHeight
    {
        get => Header.LayerHeight;
        set => base.LayerHeight = Header.LayerHeight = value;
    }

    public override float DisplayWidth
    {
        get => Header.DisplayWidth;
        set => base.DisplayWidth = Header.DisplayWidth = RoundDisplaySize(value);
    }

    public override float DisplayHeight
    {
        get => Header.DisplayHeight;
        set => base.DisplayHeight = Header.DisplayHeight = RoundDisplaySize(value);
    }

    public override float MachineZ
    {
        get => Header.MachineZ > 0 ? Header.MachineZ : base.MachineZ;
        set => base.MachineZ = Header.MachineZ = MathF.Round(value, 2);
    }

    public override FlipDirection DisplayMirror
    {
        get
        {
            if (Header is {MirrorX: true, MirrorY: true}) return FlipDirection.Both;
            if (Header.MirrorX) return FlipDirection.Horizontally;
            if (Header.MirrorY) return FlipDirection.Vertically;
            return FlipDirection.None;
        }
        set
        {
            switch (value)
            {
                case FlipDirection.None:
                    Header.MirrorX = false;
                    Header.MirrorY = false;
                    break;
                case FlipDirection.Horizontally:
                    Header.MirrorX = true;
                    Header.MirrorY = false;
                    break;
                case FlipDirection.Vertically:
                    Header.MirrorX = false;
                    Header.MirrorY = true;
                    break;
                case FlipDirection.Both:
                    Header.MirrorX = true;
                    Header.MirrorY = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(value), value, null);
            }
            RaisePropertyChanged();
        }
    }

    public override byte AntiAliasing
    {
        get => (byte)(Header.AntiAliasingLevel);
        set => base.AntiAliasing = (byte)(Header.AntiAliasingLevel = value);
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = Header.LayerCount = base.LayerCount;
    }

    public override ushort BottomLayerCount
    {
        get => (ushort) Header.BottomLayerCount;
        set => base.BottomLayerCount =  (ushort) (Header.BottomLayerCount = value);
    }

    public override TransitionLayerTypes TransitionLayerType => TransitionLayerTypes.Software;

    public override ushort TransitionLayerCount
    {
        get => Header.TransitionLayerCount;
        set => base.TransitionLayerCount = Header.TransitionLayerCount = (ushort) Math.Min(value, MaximumPossibleTransitionLayerCount);
    }

    public override float BottomLightOffDelay => Header.LightOffDelay;

    public override float LightOffDelay
    {
        get => Header.LightOffDelay;
        set
        {
            base.LightOffDelay = Header.LightOffDelay = MathF.Round(value, 2);
            if (value > 0)
            {
                Header.DelayMode = DelayModes.LightOff;
            }
        }
    }

    public override float BottomWaitTimeBeforeCure
    {
        get => base.BottomWaitTimeBeforeCure;
        set
        {
            base.BottomWaitTimeBeforeCure = value;
            Header.DelayMode = DelayModes.WaitTime;
        }
    }


    public override float WaitTimeBeforeCure
    {
        get => Header.WaitTimeBeforeCure;
        set
        {
            base.WaitTimeBeforeCure = Header.WaitTimeBeforeCure = Header.WaitTimeBeforeCure = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
            Header.DelayMode = DelayModes.WaitTime;
        }
    }

    public override float BottomExposureTime
    {
        get => Header.BottomExposureTime;
        set => base.BottomExposureTime = Header.BottomExposureTime = MathF.Round(value, 2);
    }

    public override float BottomWaitTimeAfterCure
    {
        get => base.BottomWaitTimeAfterCure;
        set
        {
            base.BottomWaitTimeAfterCure = value;
            Header.DelayMode = DelayModes.WaitTime;
        }
    }

    public override float WaitTimeAfterCure
    {
        get => Header.WaitTimeAfterCure;
        set
        {
            base.WaitTimeAfterCure = Header.WaitTimeAfterCure = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
            Header.DelayMode = DelayModes.WaitTime;
        }
    }

    public override float ExposureTime
    {
        get => Header.ExposureTime;
        set => base.ExposureTime = Header.ExposureTime = MathF.Round(value, 2);
    }

    public override float BottomLiftHeight
    {
        get => Header.BottomLiftHeight;
        set => base.BottomLiftHeight = Header.BottomLiftHeight = MathF.Round(value, 2);
    }

    public override float BottomLiftSpeed
    {
        get => Header.BottomLiftSpeed;
        set => base.BottomLiftSpeed = Header.BottomLiftSpeed = MathF.Round(value, 2);
    }

    public override float LiftHeight
    {
        get => Header.LiftHeight;
        set => base.LiftHeight = Header.LiftHeight = MathF.Round(value, 2);
    }

    public override float LiftSpeed
    {
        get => Header.LiftSpeed;
        set => base.LiftSpeed = Header.LiftSpeed = MathF.Round(value, 2);
    }

    public override float BottomLiftHeight2
    {
        get => Header.BottomLiftHeight2;
        set => base.BottomLiftHeight2 = Header.BottomLiftHeight2 = MathF.Round(value, 2);
    }

    public override float BottomLiftSpeed2
    {
        get => Header.BottomLiftSpeed2;
        set => base.BottomLiftSpeed2 = Header.BottomLiftSpeed2 = MathF.Round(value, 2);
    }

    public override float LiftHeight2
    {
        get => Header.LiftHeight2;
        set => base.LiftHeight2 = Header.LiftHeight2 = MathF.Round(value, 2);
    }

    public override float LiftSpeed2
    {
        get => Header.LiftSpeed2;
        set => base.LiftSpeed2 = Header.LiftSpeed2 = MathF.Round(value, 2);
    }

    public override float BottomWaitTimeAfterLift
    {
        get => base.BottomWaitTimeAfterLift;
        set
        {
            base.BottomWaitTimeAfterLift = value;
            Header.DelayMode = DelayModes.WaitTime;
        }
    }

    public override float WaitTimeAfterLift
    {
        get => Header.WaitTimeAfterLift;
        set
        {
            base.WaitTimeAfterLift = Header.WaitTimeAfterLift = Header.WaitTimeAfterLift = MathF.Round(value, 2);
            if (value > 0)
            {
                BottomLightOffDelay = 0;
                LightOffDelay = 0;
            }
            Header.DelayMode = DelayModes.WaitTime;
        }
    }

    public override float BottomRetractSpeed
    {
        get => Header.BottomRetractSpeed;
        set => base.BottomRetractSpeed = Header.BottomRetractSpeed = MathF.Round(value, 2);
    }

    public override float RetractSpeed
    {
        get => Header.RetractSpeed;
        set => base.RetractSpeed = Header.RetractSpeed = MathF.Round(value, 2);
    }

    public override float BottomRetractHeight2
    {
        get => Header.BottomRetractHeight2;
        set
        {
            value = Math.Clamp(MathF.Round(value, 2), 0, BottomRetractHeightTotal);
            base.BottomRetractHeight2 = Header.BottomRetractHeight2 = value;
            Header.BottomRetractHeight = BottomRetractHeight;
        }
    }

    public override float BottomRetractSpeed2
    {
        get => Header.BottomRetractSpeed2;
        set => base.BottomRetractSpeed2 = Header.BottomRetractSpeed2 = MathF.Round(value, 2);
    }

    public override float RetractHeight2
    {
        get => Header.RetractHeight2;
        set
        {
            value = Math.Clamp(MathF.Round(value, 2), 0, RetractHeightTotal);
            base.RetractHeight2 = Header.RetractHeight2 = value;
            Header.RetractHeight = RetractHeight;
        }
    }

    public override float RetractSpeed2
    {
        get => Header.RetractSpeed2;
        set => base.RetractSpeed2 = Header.RetractSpeed2 = MathF.Round(value, 2);
    }

    public override byte BottomLightPWM
    {
        get => (byte)Header.BottomLightPWM;
        set => base.BottomLightPWM = (byte)(Header.BottomLightPWM = value);
    }

    public override byte LightPWM
    {
        get => (byte)Header.LightPWM;
        set => base.LightPWM = (byte)(Header.LightPWM = value);
    }

    public override float PrintTime
    {
        get => base.PrintTime;
        set
        {
            base.PrintTime = value;
            Header.PrintTime = (uint)base.PrintTime;
        }
    }

    public override string MachineName
    {
        get => Header.MachineName;
        set => base.MachineName = Header.MachineName = value;
    }

    public override float MaterialGrams
    {
        get => Header.MaterialGrams;
        set => base.MaterialGrams = Header.MaterialGrams = MathF.Round(value, 3);
    }

    public override float MaterialCost
    {
        get => MathF.Round(Header.MaterialCost, 3);
        set => base.MaterialCost = Header.MaterialCost = MathF.Round(value, 3);
    }

    public override object[] Configs => [Header, Footer];

    #endregion

    #region Constructors
    public GooFile()
    {
    }
    #endregion

    #region Methods

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = new FileStream(FileFullPath!, FileMode.Open, FileAccess.Read);
        Header = Helpers.Deserialize<FileHeader>(inputFile);
        Debug.WriteLine($"Header: {Header}");

        var expectedMagic = FileMagic;

        if (!Header.Magic.AsValueEnumerable().SequenceEqual(expectedMagic))
        {
            throw new FileLoadException("Not a valid GOO file! Magic value mismatch", FileFullPath);
        }

        progress.Reset(OperationProgress.StatusDecodePreviews, (uint)ThumbnailCountFileShouldHave);

        Thumbnails.Add(DecodeImage(DATATYPE_RGB565_BE, Header.SmallPreview565, ThumbnailsOriginalSize[0]));
        progress++;
        Thumbnails.Add(DecodeImage(DATATYPE_RGB565_BE, Header.BigPreview565, ThumbnailsOriginalSize[1]));
        progress++;


        progress.Reset(OperationProgress.StatusDecodeLayers, Header.LayerCount);
        Init(Header.LayerCount, DecodeType == FileDecodeType.Partial);
        LayersDefinition = new LayerDef[LayerCount];
        inputFile.Seek(Header.LayerDefAddress, SeekOrigin.Begin);

        foreach (var batch in BatchLayersIndexes())
        {
            foreach (var layerIndex in batch)
            {
                progress.PauseOrCancelIfRequested();

                LayersDefinition[layerIndex] = Helpers.Deserialize<LayerDef>(inputFile);
                LayersDefinition[layerIndex].Parent = this;
                if (DecodeType == FileDecodeType.Full)
                {
                    LayersDefinition[layerIndex].EncodedRle = inputFile.ReadBytes(LayersDefinition[layerIndex].DataLength);
                }
                else
                {
                    inputFile.Seek(LayersDefinition[layerIndex].DataLength, SeekOrigin.Current);
                }

                inputFile.Seek(2, SeekOrigin.Current); // \n
                Debug.WriteLine($"layer[{layerIndex}]: {LayersDefinition[layerIndex]}");
            }

            if (DecodeType == FileDecodeType.Full)
            {
                Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
                {
                    progress.PauseIfRequested();

                    using (var mat = LayersDefinition[layerIndex].DecodeImage((uint)layerIndex))
                    {
                        _layers[layerIndex] = new Layer((uint)layerIndex, mat, this);
                    }

                    progress.LockAndIncrement();
                });
            }
        }

        Footer = Helpers.Deserialize<FileFooter>(inputFile);
        Debug.WriteLine($"Footer: {Footer}");
        if (!Footer.Magic.AsValueEnumerable().SequenceEqual(expectedMagic))
        {
            throw new FileLoadException("Not a valid GOO file! Footer magic value mismatch", FileFullPath);
        }

        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            LayersDefinition[layerIndex].CopyTo(this[layerIndex]);
        }

        // Fixes virtual bottom properties
        SuppressRebuildPropertiesWork(() =>
        {
            var enumerable = this.AsValueEnumerable();
            base.BottomWaitTimeBeforeCure = enumerable.FirstOrDefault(layer => layer is { IsBottomLayer: true, IsDummy: false })?.WaitTimeBeforeCure ?? 0;
            base.BottomWaitTimeAfterCure = enumerable.FirstOrDefault(layer => layer is { IsBottomLayer: true, IsDummy: false })?.WaitTimeAfterCure ?? 0;
            base.BottomWaitTimeAfterLift = enumerable.FirstOrDefault(layer => layer is { IsBottomLayer: true, IsDummy: false })?.WaitTimeAfterLift ?? 0;
        });
    }

    protected override void OnBeforeEncode(bool isPartialEncode)
    {
        Header.PerLayerSettings = SupportPerLayerSettings && UsingPerLayerSettings;
        Header.Volume = Volume;
        Header.MaterialGrams = MaterialMilliliters;

        if (HaveTiltingVat)
        {
            const float lift = 0.05f;
            const float speed = lift;


            BottomLiftHeight = lift;
            BottomLiftSpeed = speed;

            BottomLiftHeight2 = 0;
            BottomLiftSpeed2 = 0;

            BottomRetractHeight2 = 0;
            BottomRetractSpeed2 = 0;

            BottomRetractSpeed = speed;

            LiftHeight = lift;
            LiftSpeed = speed;

            LiftHeight2 = 0;
            LiftSpeed2 = 0;

            RetractHeight2 = 0;
            RetractSpeed2 = 0;

            RetractSpeed = speed;
        }
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Create, FileAccess.Write);

        progress.Reset(OperationProgress.StatusEncodePreviews, 2);

        Mat?[] thumbnails = [GetLargestThumbnail(), GetSmallestThumbnail()];
        Header.BigPreview565 = EncodeImage(DATATYPE_RGB565_BE, thumbnails[0]!);
        progress++;
        Header.SmallPreview565 = EncodeImage(DATATYPE_RGB565_BE, thumbnails[1]!);
        progress++;

        Header.LayerDefAddress = (uint)Helpers.Serializer.SizeOf(Header);
        outputFile.Seek(Header.LayerDefAddress, SeekOrigin.Begin);

        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        var layerData = new LayerDef[LayerCount];

        var delimiter = Delimiter;

        var useColorDifferenceCompression = !FileEndsWith(".prz");

        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                progress.PauseIfRequested();
                using (var mat = this[layerIndex].LayerMat)
                {
                    layerData[layerIndex] = new LayerDef(this, this[layerIndex]);
                    layerData[layerIndex].EncodeImage(mat, (uint) layerIndex, useColorDifferenceCompression);
                }
                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                progress.PauseOrCancelIfRequested();

                outputFile.WriteSerialize(layerData[layerIndex]);
                outputFile.WriteBytes(layerData[layerIndex].EncodedRle);
                outputFile.WriteBytes(delimiter);

                layerData[layerIndex].EncodedRle = null!; // Free
            }
        }

        // Footer
        outputFile.WriteSerialize(Footer);

        // Header
        outputFile.Seek(0, SeekOrigin.Begin);
        outputFile.WriteSerialize(Header);

        Debug.WriteLine("Encode Results:");
        Debug.WriteLine(Header);
        Debug.WriteLine(Footer);
        Debug.WriteLine("-End-");
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Open, FileAccess.Write);
        outputFile.Seek(0, SeekOrigin.Begin);

        outputFile.WriteSerialize(Header);
        outputFile.Seek(Header.LayerDefAddress, SeekOrigin.Begin);
        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            LayersDefinition![layerIndex].SetFrom(this[layerIndex]);
            outputFile.WriteSerialize(LayersDefinition[layerIndex]);
            outputFile.Seek(LayersDefinition[layerIndex].DataLength + 2, SeekOrigin.Current);
        }
    }
    #endregion
}