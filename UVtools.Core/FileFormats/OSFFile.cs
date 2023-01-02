/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using BinarySerialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Emgu.CV;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

public sealed class OSFFile : FileFormat
{
    #region Constants
    public const ushort DEFAULT_VERSION = 4;
    #endregion
    #region Sub Classes

    #region Header

    public class OSFHeader
    {
        [FieldOrder(0)]
        [FieldEndianness(Endianness.Big)]
        public uint HeaderLength { get; set; } = 350001;

        [FieldOrder(1)]
        [FieldEndianness(Endianness.Big)]
        public ushort Version { get; set; } = DEFAULT_VERSION;

        [FieldOrder(2)]
        [FieldEndianness(Endianness.Big)]
        public byte ImageLog { get; set; } = 2;

        
        public override string ToString()
        {
            return $"{nameof(HeaderLength)}: {HeaderLength}, {nameof(Version)}: {Version}, {nameof(ImageLog)}: {ImageLog}";
        }
    }

    public sealed class OSFSettings
    {
        [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public ushort ResolutionX { get; set; }
        [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public ushort ResolutionY { get; set; }
        [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public ushort PixelUmMagnified100Times { get; set; }
        
        /// <summary>
        /// (0x00 not mirrored, 0x01 X-axis mirroring, 0x02 Y-axis mirroring, 0x03 XY-axis mirroring)
        /// </summary>
        [FieldOrder(3)] [FieldEndianness(Endianness.Big)] public byte Mirror { get; set; }
        [FieldOrder(4)] [FieldEndianness(Endianness.Big)] public byte BottomLightPWM { get; set; } = DefaultBottomLightPWM;
        [FieldOrder(5)] [FieldEndianness(Endianness.Big)] public byte LightPWM { get; set; } = DefaultLightPWM;
        [FieldOrder(6)] [FieldEndianness(Endianness.Big)] public bool AntiAliasEnabled { get; set; }
        [FieldOrder(7)] [FieldEndianness(Endianness.Big)] public bool DistortionEnabled { get; set; }
        [FieldOrder(8)] [FieldEndianness(Endianness.Big)] public bool DelayedExposureActivationEnabled { get; set; }
        [FieldOrder(9)] [FieldEndianness(Endianness.Big)] public uint LayerCount { get; set; }
        [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort NumberParameterSets { get; set; } = 1;
        [FieldOrder(11)] [FieldEndianness(Endianness.Big)] public uint LastLayerIndex { set; get; }
        [FieldOrder(12)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian LayerHeightUmMagnified100Times { set; get; } = new();
        [FieldOrder(13)] [FieldEndianness(Endianness.Big)] public byte BottomLayerCount { set; get; }
        [FieldOrder(14)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian ExposureTimeMagnified100Times { set; get; } = new((uint) (DefaultExposureTime*1000));
        [FieldOrder(15)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian BottomExposureTimeMagnified100Times { set; get; } = new((uint)(DefaultBottomExposureTime * 1000));
        [FieldOrder(16)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian SupportDelayTimeMagnified100Times { set; get; } = new(50);
        [FieldOrder(17)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian BottomSupportDelayTimeMagnified100Times { set; get; } = new(50);
        [FieldOrder(18)] [FieldEndianness(Endianness.Big)] public byte TransitionLayerCount { set; get; }
        
        /// <summary>
        /// （0x00 linear transition）
        /// </summary>
        [FieldOrder(19)] [FieldEndianness(Endianness.Big)] public byte TransitionType { set; get; }
        
        [FieldOrder(20)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian TransitionLayerIntervalTimeDifferenceMagnified100Times { set; get; } = new();
        [FieldOrder(21)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian WaitTimeAfterCureMagnified100Times { set; get; } = new();
        [FieldOrder(22)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian WaitTimeAfterLiftMagnified100Times { set; get; } = new();
        [FieldOrder(23)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian WaitTimeBeforeCureMagnified100Times { set; get; } = new();
        [FieldOrder(24)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian BottomLiftHeightSlowMagnified1000Times { set; get; } = new();
        [FieldOrder(25)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian BottomLiftHeightTotalMagnified1000Times { set; get; } = new((uint)(DefaultBottomLiftHeight * 1000));
        [FieldOrder(26)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian LiftHeightSlowMagnified1000Times { set; get; } = new();
        [FieldOrder(27)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian LiftHeightTotalMagnified1000Times { set; get; } = new((uint)(DefaultLiftHeight * 1000));
        [FieldOrder(28)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian BottomRetractHeightSlowMagnified1000Times { set; get; } = new();
        [FieldOrder(29)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian BottomRetractHeightTotalMagnified1000Times { set; get; } = new((uint)(DefaultBottomLiftHeight * 1000));
        [FieldOrder(30)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian RetractHeightSlowMagnified1000Times { set; get; } = new();
        [FieldOrder(31)] [FieldEndianness(Endianness.Big)] public UInt24BigEndian RetractHeightTotalMagnified1000Times { set; get; } = new((uint)(DefaultLiftHeight * 1000));

        /// <summary>
        /// (0x00: S-shaped acceleration, 0x01: T-shaped acceleration, Default Value: S-shaped acceleration, currently only supports S-shaped acceleration)
        /// </summary>
        [FieldOrder(32)] [FieldEndianness(Endianness.Big)] public byte AccelerationType { set; get; }

        [FieldOrder(33)] [FieldEndianness(Endianness.Big)] public ushort BottomLiftSpeedStart { set; get; } = 80;
        [FieldOrder(34)] [FieldEndianness(Endianness.Big)] public ushort BottomLiftSpeedSlow { set; get; } = (ushort)DefaultBottomLiftSpeed;
        [FieldOrder(35)] [FieldEndianness(Endianness.Big)] public ushort BottomLiftSpeedFast { set; get; } = (ushort)DefaultBottomLiftSpeed2;
        [FieldOrder(36)] [FieldEndianness(Endianness.Big)] public byte BottomLiftAccelerationChange { set; get; } = 5;

        [FieldOrder(37)] [FieldEndianness(Endianness.Big)] public ushort LiftSpeedStart { set; get; } = 80;
        [FieldOrder(38)] [FieldEndianness(Endianness.Big)] public ushort LiftSpeedSlow { set; get; } = (ushort)DefaultLiftSpeed;
        [FieldOrder(39)] [FieldEndianness(Endianness.Big)] public ushort LiftSpeedFast { set; get; } = (ushort)DefaultLiftSpeed2;
        [FieldOrder(40)] [FieldEndianness(Endianness.Big)] public byte LiftAccelerationChange { set; get; } = 5;

        [FieldOrder(41)] [FieldEndianness(Endianness.Big)] public ushort BottomRetractSpeedStart { set; get; } = 80;
        [FieldOrder(42)] [FieldEndianness(Endianness.Big)] public ushort BottomRetractSpeedSlow { set; get; } = (ushort)DefaultBottomRetractSpeed2;
        [FieldOrder(43)] [FieldEndianness(Endianness.Big)] public ushort BottomRetractSpeedFast { set; get; } = (ushort)DefaultBottomRetractSpeed;
        [FieldOrder(44)] [FieldEndianness(Endianness.Big)] public byte BottomRetractAccelerationChange { set; get; } = 5;

        [FieldOrder(45)] [FieldEndianness(Endianness.Big)] public ushort RetractSpeedStart { set; get; } = 80;
        [FieldOrder(46)] [FieldEndianness(Endianness.Big)] public ushort RetractSpeedSlow { set; get; } = (ushort)DefaultRetractSpeed2;
        [FieldOrder(47)] [FieldEndianness(Endianness.Big)] public ushort RetractSpeedFast { set; get; } = (ushort)DefaultRetractSpeed;
        [FieldOrder(48)] [FieldEndianness(Endianness.Big)] public byte RetractAccelerationChange { set; get; } = 5;

        [FieldOrder(49)][FieldEndianness(Endianness.Big)] public ushort BottomLiftSpeedEnd { set; get; } = 7;
        [FieldOrder(50)][FieldEndianness(Endianness.Big)] public byte BottomLiftDecelerationChange { set; get; } = 5;

        [FieldOrder(51)][FieldEndianness(Endianness.Big)] public ushort LiftSpeedEnd { set; get; } = 7;
        [FieldOrder(52)][FieldEndianness(Endianness.Big)] public byte LiftDecelerationChange { set; get; } = 5;

        [FieldOrder(53)][FieldEndianness(Endianness.Big)] public ushort BottomRetractSpeedEnd { set; get; } = 7;
        [FieldOrder(54)][FieldEndianness(Endianness.Big)] public byte BottomRetractDecelerationChange { set; get; } = 5;

        [FieldOrder(55)][FieldEndianness(Endianness.Big)] public ushort RetractSpeedEnd { set; get; } = 7;
        [FieldOrder(56)][FieldEndianness(Endianness.Big)] public byte RetractDecelerationChange { set; get; } = 5;

        [FieldOrder(57)][FieldEndianness(Endianness.Big)] public ushort BottomWaitTimeAfterCureMagnified100Times { set; get; }
        [FieldOrder(58)][FieldEndianness(Endianness.Big)] public ushort BottomWaitTimeAfterLiftMagnified100Times { set; get; }
        [FieldOrder(59)][FieldEndianness(Endianness.Big)] public ushort BottomWaitTimeBeforeCureMagnified100Times { set; get; }

        [FieldOrder(60)] [FieldEndianness(Endianness.Big)] public ushort Reserved { set; get; }
        [FieldOrder(61)] [FieldEndianness(Endianness.Big)] public byte ProtocolType { set; get; } // 0

        public override string ToString()
        {
            return $"{nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(PixelUmMagnified100Times)}: {PixelUmMagnified100Times}, {nameof(Mirror)}: {Mirror}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(LightPWM)}: {LightPWM}, {nameof(AntiAliasEnabled)}: {AntiAliasEnabled}, {nameof(DistortionEnabled)}: {DistortionEnabled}, {nameof(DelayedExposureActivationEnabled)}: {DelayedExposureActivationEnabled}, {nameof(LayerCount)}: {LayerCount}, {nameof(NumberParameterSets)}: {NumberParameterSets}, {nameof(LastLayerIndex)}: {LastLayerIndex}, {nameof(LayerHeightUmMagnified100Times)}: {LayerHeightUmMagnified100Times}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(ExposureTimeMagnified100Times)}: {ExposureTimeMagnified100Times}, {nameof(BottomExposureTimeMagnified100Times)}: {BottomExposureTimeMagnified100Times}, {nameof(SupportDelayTimeMagnified100Times)}: {SupportDelayTimeMagnified100Times}, {nameof(BottomSupportDelayTimeMagnified100Times)}: {BottomSupportDelayTimeMagnified100Times}, {nameof(TransitionLayerCount)}: {TransitionLayerCount}, {nameof(TransitionType)}: {TransitionType}, {nameof(TransitionLayerIntervalTimeDifferenceMagnified100Times)}: {TransitionLayerIntervalTimeDifferenceMagnified100Times}, {nameof(WaitTimeAfterCureMagnified100Times)}: {WaitTimeAfterCureMagnified100Times}, {nameof(WaitTimeAfterLiftMagnified100Times)}: {WaitTimeAfterLiftMagnified100Times}, {nameof(WaitTimeBeforeCureMagnified100Times)}: {WaitTimeBeforeCureMagnified100Times}, {nameof(BottomLiftHeightSlowMagnified1000Times)}: {BottomLiftHeightSlowMagnified1000Times}, {nameof(BottomLiftHeightTotalMagnified1000Times)}: {BottomLiftHeightTotalMagnified1000Times}, {nameof(LiftHeightSlowMagnified1000Times)}: {LiftHeightSlowMagnified1000Times}, {nameof(LiftHeightTotalMagnified1000Times)}: {LiftHeightTotalMagnified1000Times}, {nameof(BottomRetractHeightSlowMagnified1000Times)}: {BottomRetractHeightSlowMagnified1000Times}, {nameof(BottomRetractHeightTotalMagnified1000Times)}: {BottomRetractHeightTotalMagnified1000Times}, {nameof(RetractHeightSlowMagnified1000Times)}: {RetractHeightSlowMagnified1000Times}, {nameof(RetractHeightTotalMagnified1000Times)}: {RetractHeightTotalMagnified1000Times}, {nameof(AccelerationType)}: {AccelerationType}, {nameof(BottomLiftSpeedStart)}: {BottomLiftSpeedStart}, {nameof(BottomLiftSpeedSlow)}: {BottomLiftSpeedSlow}, {nameof(BottomLiftSpeedFast)}: {BottomLiftSpeedFast}, {nameof(BottomLiftAccelerationChange)}: {BottomLiftAccelerationChange}, {nameof(LiftSpeedStart)}: {LiftSpeedStart}, {nameof(LiftSpeedSlow)}: {LiftSpeedSlow}, {nameof(LiftSpeedFast)}: {LiftSpeedFast}, {nameof(LiftAccelerationChange)}: {LiftAccelerationChange}, {nameof(BottomRetractSpeedStart)}: {BottomRetractSpeedStart}, {nameof(BottomRetractSpeedSlow)}: {BottomRetractSpeedSlow}, {nameof(BottomRetractSpeedFast)}: {BottomRetractSpeedFast}, {nameof(BottomRetractAccelerationChange)}: {BottomRetractAccelerationChange}, {nameof(RetractSpeedStart)}: {RetractSpeedStart}, {nameof(RetractSpeedSlow)}: {RetractSpeedSlow}, {nameof(RetractSpeedFast)}: {RetractSpeedFast}, {nameof(RetractAccelerationChange)}: {RetractAccelerationChange}, {nameof(BottomLiftSpeedEnd)}: {BottomLiftSpeedEnd}, {nameof(BottomLiftDecelerationChange)}: {BottomLiftDecelerationChange}, {nameof(LiftSpeedEnd)}: {LiftSpeedEnd}, {nameof(LiftDecelerationChange)}: {LiftDecelerationChange}, {nameof(BottomRetractSpeedEnd)}: {BottomRetractSpeedEnd}, {nameof(BottomRetractDecelerationChange)}: {BottomRetractDecelerationChange}, {nameof(RetractSpeedEnd)}: {RetractSpeedEnd}, {nameof(RetractDecelerationChange)}: {RetractDecelerationChange}, {nameof(BottomWaitTimeAfterCureMagnified100Times)}: {BottomWaitTimeAfterCureMagnified100Times}, {nameof(BottomWaitTimeAfterLiftMagnified100Times)}: {BottomWaitTimeAfterLiftMagnified100Times}, {nameof(BottomWaitTimeBeforeCureMagnified100Times)}: {BottomWaitTimeBeforeCureMagnified100Times}, {nameof(Reserved)}: {Reserved}, {nameof(ProtocolType)}: {ProtocolType}";
        }
    }

    #endregion

    #region LayerDef

    public class OSFLayerDef
    {
        /// <summary>
        /// OD OA begins, indicating that the model + support is included; the beginning of 0D 0B, indicating that the layer only has support data
        /// </summary>
        [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public ushort Mark { get; set; } = 0x0D_0A;

        [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public uint NumberOfPixels { get; set; }
        [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public ushort StartY { get; set; }
        [Ignore] public byte[] EncodedRle { get; set; } = Array.Empty<byte>();

        public OSFLayerDef() { }

        public override string ToString()
        {
            return $"{nameof(Mark)}: {Mark}, {nameof(NumberOfPixels)}: {NumberOfPixels}, {nameof(StartY)}: {StartY}";
        }

        internal unsafe void EncodeImage(Mat mat)
        {
            List<byte> rawData = new();
            byte color = byte.MaxValue >> 1;
            uint stride = 0;
            var span = mat.GetBytePointer();
            var imageLength = mat.GetLength();

            void AddRep()
            {
                switch (stride)
                {
                    case 0:
                        return;
                    case 1:
                        color &= 0xfe;
                        break;
                    case > 1:
                        color |= 0x01;
                        break;
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


            for (int pixel = StartY * mat.GetRealStep(); pixel < imageLength; pixel++)
            {
                var grey = span[pixel];

                if (grey == color)
                {
                    stride++;
                }
                else
                {
                    AddRep();
                    color = grey;
                    stride = 1;
                }
            }

            EncodedRle = rawData.ToArray();
        }

        internal Mat DecodeImage(OSFFile parent)
        {
            var mat = parent.CreateMat();
            if (NumberOfPixels == 0) return mat;

            int pixel = (int)(StartY * parent.ResolutionX);
            for (var n = 0; n < EncodedRle.Length; n++)
            {
                byte code = EncodedRle[n];
                int stride = 1;

                if ((code & 0x01) == 0x01) // It's a run
                {
                    code &= 0xfe; // Get the grey value
                    var slen = EncodedRle[++n];

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
                    code = (byte)(code | 1);
                }

                mat.FillSpan(ref pixel, stride, code);
            }

            return mat;
        }
    }
    #endregion

    #endregion

    #region Properties

    public OSFHeader Header { get; protected internal set; } = new();
    public OSFSettings Settings { get; protected internal set; } = new();
    public override FileFormatType FileType => FileFormatType.Binary;

    public override FileExtension[] FileExtensions { get; } = {
        new (typeof(OSFFile), "osf", "Vlare Open File Format (OSF)"),
    };

    public override PrintParameterModifier[]? PrintParameterModifiers { get; } =
    {
        PrintParameterModifier.BottomLayerCount,
        PrintParameterModifier.TransitionLayerCount,

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
        PrintParameterModifier.LightPWM,
    };

    public override Size[]? ThumbnailsOriginalSize { get; } =
    {
        new(148, 80),
        new(300, 140),
        new(208, 116),
        new(404, 240),
    };

    public override uint[] AvailableVersions { get; } = { 4 };

    public override uint DefaultVersion => DEFAULT_VERSION;

    public override uint Version
    {
        get => Header.Version;
        set
        {
            base.Version = value;
            Header.Version = (ushort) base.Version;
        }
    }

    public override uint ResolutionX
    {
        get => Settings.ResolutionX;
        set => base.ResolutionX = Settings.ResolutionX = (ushort) value;
    }

    public override uint ResolutionY
    {
        get => Settings.ResolutionY;
        set => base.ResolutionY = Settings.ResolutionY = (ushort)value;
    }

    public override FlipDirection DisplayMirror
    {
        get => Settings.Mirror switch
            {
                1 => FlipDirection.Horizontally,
                2 => FlipDirection.Vertically,
                3 => FlipDirection.Both,
                _ => FlipDirection.None
            };
        set
        {
            Settings.Mirror = value switch
            {
                FlipDirection.None => 0,
                FlipDirection.Horizontally => 1,
                FlipDirection.Vertically => 2,
                FlipDirection.Both => 3,
                _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
            };
        }
    }      

    public override float LayerHeight
    {
        get => Layer.RoundHeight(Settings.LayerHeightUmMagnified100Times.Value / 1000_00f);
        set
        {
            Settings.LayerHeightUmMagnified100Times.Value = (ushort)(value * 1000_00f);
            RaisePropertyChanged();
        }
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set
        {
            base.LayerCount = Settings.LayerCount = base.LayerCount;
            Settings.LastLayerIndex = Settings.LayerCount - 1;
        }
    }

    public override ushort BottomLayerCount
    {
        get => Settings.BottomLayerCount;
        set
        {
            Settings.BottomLayerCount = (byte)value;
            base.BottomLayerCount = value;
        }
    }

    public override TransitionLayerTypes TransitionLayerType => TransitionLayerTypes.Software;

    public override ushort TransitionLayerCount
    {
        get => Settings.TransitionLayerCount;
        set => base.TransitionLayerCount = Settings.TransitionLayerCount = (byte)Math.Min(byte.MaxValue, Math.Min(value, MaximumPossibleTransitionLayerCount));
    }

    public override float BottomLightOffDelay => BottomWaitTimeBeforeCure;

    public override float LightOffDelay => WaitTimeBeforeCure;

    public override float BottomWaitTimeBeforeCure
    {
        get => (float)Math.Round(Settings.BottomWaitTimeBeforeCureMagnified100Times / 100f, 2);
        set
        {
            Settings.BottomWaitTimeBeforeCureMagnified100Times = (ushort) (value * 100);
            base.BottomWaitTimeBeforeCure = (float)Math.Round(value, 2);
        }
    }

    public override float WaitTimeBeforeCure
    {
        get => (float)Math.Round(Settings.WaitTimeBeforeCureMagnified100Times.Value / 100f, 2);
        set
        {
            Settings.WaitTimeBeforeCureMagnified100Times.Value = (uint)(value * 100);
            base.WaitTimeBeforeCure = (float)Math.Round(value, 2);
        }
    }

    public override float BottomExposureTime
    {
        get => (float)Math.Round(Settings.BottomExposureTimeMagnified100Times.Value / 100f, 2);
        set
        {
            Settings.BottomExposureTimeMagnified100Times.Value = (uint)(value * 100);
            base.BottomExposureTime = (float)Math.Round(value, 2);
        }
    }

    public override float ExposureTime
    {
        get => (float)Math.Round(Settings.ExposureTimeMagnified100Times.Value / 100f, 2);
        set
        {
            Settings.ExposureTimeMagnified100Times.Value = (uint)(value * 100);
            base.ExposureTime = (float)Math.Round(value, 2);
        }
    }

    public override float BottomWaitTimeAfterCure
    {
        get => (float)Math.Round(Settings.BottomWaitTimeAfterCureMagnified100Times / 100f, 2);
        set
        {
            Settings.BottomWaitTimeAfterCureMagnified100Times = (ushort)(value * 100);
            base.BottomWaitTimeAfterCure = (float)Math.Round(value, 2);
        }
    }

    public override float WaitTimeAfterCure
    {
        get => (float)Math.Round(Settings.WaitTimeAfterCureMagnified100Times.Value / 100f, 2);
        set
        {
            Settings.WaitTimeAfterCureMagnified100Times.Value = (uint)(value * 100);
            base.WaitTimeAfterCure = (float)Math.Round(value, 2);
        }
    }

    public override float BottomLiftHeight
    {
        get => (float)Math.Round(Settings.BottomLiftHeightSlowMagnified1000Times.Value / 1000f, 2);
        set
        {
            value = (float)Math.Round(value, 2);
            Settings.BottomLiftHeightTotalMagnified1000Times.Value -= Settings.BottomLiftHeightSlowMagnified1000Times.Value;
            Settings.BottomLiftHeightSlowMagnified1000Times.Value = (uint)(value * 1000);
            Settings.BottomLiftHeightTotalMagnified1000Times.Value += Settings.BottomLiftHeightSlowMagnified1000Times.Value;
            base.BottomLiftHeight = value;
        }
    }

    public override float LiftHeight
    {
        get => (float)Math.Round(Settings.LiftHeightSlowMagnified1000Times.Value / 1000f, 2);
        set
        {
            value = (float)Math.Round(value, 2);
            Settings.LiftHeightTotalMagnified1000Times.Value -= Settings.LiftHeightSlowMagnified1000Times.Value;
            Settings.LiftHeightSlowMagnified1000Times.Value = (uint)(value * 1000);
            Settings.LiftHeightTotalMagnified1000Times.Value += Settings.LiftHeightSlowMagnified1000Times.Value;
            base.LiftHeight = value;
        }
    }

    public override float BottomLiftSpeed
    {
        get => Settings.BottomLiftSpeedSlow;
        set => base.BottomLiftSpeed = Settings.BottomLiftSpeedSlow = (ushort)value;
    }

    public override float LiftSpeed
    {
        get => Settings.LiftSpeedSlow;
        set => base.LiftSpeed = Settings.LiftSpeedSlow = (ushort)value;
    }

    public override float BottomLiftHeight2
    {
        get => (float)Math.Max(0, Math.Round((Settings.BottomLiftHeightTotalMagnified1000Times - Settings.BottomLiftHeightSlowMagnified1000Times) / 1000f, 2));
        set
        {
            value = (float)Math.Round(value, 2);
            Settings.BottomLiftHeightTotalMagnified1000Times.Value = Settings.BottomLiftHeightSlowMagnified1000Times.Value + (uint)(value * 1000);
            base.BottomLiftHeight2 = value;
        }
    }

    public override float BottomLiftSpeed2
    {
        get =>Settings.BottomLiftSpeedFast;
        set => base.BottomLiftSpeed2 = Settings.BottomLiftSpeedFast = (ushort) value;
    }

    public override float LiftHeight2
    {
        get => (float)Math.Max(0, Math.Round((Settings.LiftHeightTotalMagnified1000Times - Settings.LiftHeightSlowMagnified1000Times) / 1000f, 2));
        set
        {
            value = (float)Math.Round(value, 2);
            Settings.LiftHeightTotalMagnified1000Times.Value = Settings.LiftHeightSlowMagnified1000Times.Value + (uint)(value * 1000);
            base.LiftHeight2 = value;
        }
    }

    public override float LiftSpeed2
    {
        get => Settings.LiftSpeedFast;
        set => base.LiftSpeed2 = Settings.LiftSpeedFast = (ushort)value;
    }

    public override float BottomWaitTimeAfterLift {
        get => (float)Math.Round(Settings.BottomWaitTimeAfterLiftMagnified100Times / 100f, 2); 
        set
        {
            Settings.BottomWaitTimeAfterLiftMagnified100Times = (ushort)(value * 100);
            base.BottomWaitTimeAfterLift = (float)Math.Round(value, 2);
        }
    }

    public override float WaitTimeAfterLift
    {
        get => (float)Math.Round(Settings.WaitTimeAfterLiftMagnified100Times.Value / 100f, 2);
        set
        {
            Settings.WaitTimeAfterLiftMagnified100Times.Value = (uint)(value * 100);
            base.WaitTimeAfterLift = (float)Math.Round(value, 2);
        }
    }

    public override float BottomRetractSpeed
    {
        get => Settings.BottomRetractSpeedFast;
        set => base.BottomRetractSpeed = Settings.BottomRetractSpeedFast = (ushort)value;
    }

    public override float RetractSpeed
    {
        get => Settings.RetractSpeedFast;
        set => base.RetractSpeed = Settings.RetractSpeedFast = (ushort)value;
    }

    public override float BottomRetractHeight2
    {
        get => (float)Math.Round(Settings.BottomRetractHeightSlowMagnified1000Times.Value / 1000f, 2);
        set
        {
            value = Math.Clamp((float)Math.Round(value, 2), 0, BottomRetractHeightTotal);
            Settings.BottomRetractHeightSlowMagnified1000Times.Value = (uint)(value * 1000);
            Settings.BottomRetractHeightTotalMagnified1000Times.Value = (uint)(BottomRetractHeightTotal * 1000);
            base.BottomRetractHeight2 = value;
        }
    }

    public override float BottomRetractSpeed2
    {
        get => Settings.BottomRetractSpeedSlow;
        set => base.BottomRetractSpeed2 = Settings.BottomRetractSpeedSlow = (ushort)value;
    }

    public override float RetractHeight2
    {
        get => (float)Math.Round(Settings.RetractHeightSlowMagnified1000Times.Value / 1000f, 2);
        set
        {
            value = Math.Clamp((float)Math.Round(value, 2), 0, RetractHeightTotal);
            Settings.RetractHeightSlowMagnified1000Times.Value = (uint)(value * 1000);
            Settings.RetractHeightTotalMagnified1000Times.Value = (uint)(RetractHeightTotal * 1000);
            base.RetractHeight2 = value;
        }
    }

    public override float RetractSpeed2
    {
        get => Settings.RetractSpeedSlow;
        set => base.RetractSpeed2 = Settings.RetractSpeedSlow = (ushort)value;
    }

    public override byte BottomLightPWM
    {
        get => Settings.BottomLightPWM;
        set => base.BottomLightPWM = Settings.BottomLightPWM = value;
    }

    public override byte LightPWM
    {
        get => Settings.LightPWM;
        set => base.LightPWM = Settings.LightPWM = value;
    }


    public override object[] Configs => new object[] { Settings };

    #endregion

    #region Constructors
    public OSFFile() { }
    #endregion

    #region Methods
    
    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Create, FileAccess.Write);

        Settings.PixelUmMagnified100Times = (ushort)(PixelSizeMicronsMax * 100);

        Header.HeaderLength = (uint) (Helpers.Serializer.SizeOf(Header) + Helpers.Serializer.SizeOf(Settings) + 3 * 4);

        var previews = new[]
        {
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            Array.Empty<byte>(),
            Array.Empty<byte>()
        };

        for (var i = 0; i < previews.Length; i++)
        {
            if (Thumbnails[i] is null) continue;
            previews[i] = EncodeImage(DATATYPE_RGB565, Thumbnails[i]!);
            Header.HeaderLength += (uint)previews[i].Length;
        }

        outputFile.WriteSerialize(Header);

        for (var i = 0; i < previews.Length; i++)
        {
            outputFile.WriteSerialize(new UInt24BigEndian((uint) previews[i].Length));
            outputFile.WriteBytes(previews[i]);
        }

        outputFile.WriteSerialize(Settings);


        progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
        var layerDef = new OSFLayerDef[LayerCount];

        foreach (var batch in BatchLayersIndexes())
        {
            Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
            {
                var layer = this[layerIndex];
                
                using (var mat = layer.LayerMat)
                {
                    layerDef[layerIndex] = new OSFLayerDef
                    {
                        NumberOfPixels = layer.NonZeroPixelCount,
                        StartY = (ushort)layer.BoundingRectangle.Y,
                    };
                    layerDef[layerIndex].EncodeImage(mat);
                }
                progress.LockAndIncrement();
            });

            foreach (var layerIndex in batch)
            {
                progress.ThrowIfCancellationRequested();
                outputFile.WriteSerialize(layerDef[layerIndex]);
                outputFile.WriteBytes(layerDef[layerIndex].EncodedRle);
                layerDef[layerIndex].EncodedRle = null!; // Free this
            }
        }


        Debug.WriteLine("Encode Results:");
        Debug.WriteLine(Header);
        Debug.WriteLine(Settings);
        Debug.WriteLine("-End-");
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = new FileStream(FileFullPath!, FileMode.Open, FileAccess.Read);
        Header = Helpers.Deserialize<OSFHeader>(inputFile);

        Debug.WriteLine(Header);

        for (byte i = 0; i < ThumbnailsOriginalSize!.Length; i++)
        {
            var previewSize = Helpers.Deserialize<UInt24BigEndian>(inputFile);
            var previewData = inputFile.ReadBytes(previewSize.Value);
            Thumbnails[i] = DecodeImage(DATATYPE_RGB565, previewData, ThumbnailsOriginalSize[i]);
        }


        Settings = Helpers.Deserialize<OSFSettings>(inputFile);
        Debug.WriteLine(Settings);

        Display = new SizeF(
            ResolutionX * (Settings.PixelUmMagnified100Times / 100_000f),
            ResolutionY * (Settings.PixelUmMagnified100Times / 100_000f)
        );

        Init(Settings.LayerCount, DecodeType == FileDecodeType.Partial);
        

        if (DecodeType == FileDecodeType.Full)
        {
            inputFile.Seek(Header.HeaderLength, SeekOrigin.Begin);
            var layerDef = new OSFLayerDef[LayerCount];
            var rle = new List<byte>();
            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
            
            foreach (var batch in BatchLayersIndexes())
            {
                foreach (var layerIndex in batch)
                {
                    progress.ThrowIfCancellationRequested();
                    
                    //Debug.WriteLine($"{layerIndex}: {inputFile.Position}");
                    layerDef[layerIndex] = Helpers.Deserialize<OSFLayerDef>(inputFile);
                    if (layerDef[layerIndex].NumberOfPixels == 0) continue;

                    int buffer;
                    int slen;
                    while ((buffer = inputFile.ReadByte()) > -1)
                    {
                        if ((buffer & 0x01) == 0x01) // It's a run
                        {
                            slen = inputFile.ReadByte();
                            if (buffer == 0x0D && slen is 0x0A or 0x0B)
                            {
                                inputFile.Seek(-2, SeekOrigin.Current);
                                break;
                            }
                            
                            rle.Add((byte)buffer);
                            rle.Add((byte)slen);

                            if ((slen & 0x80) == 0)
                            {
                            }
                            else if ((slen & 0xc0) == 0x80)
                            {
                                rle.Add((byte)inputFile.ReadByte());
                            }
                            else if ((slen & 0xe0) == 0xc0)
                            {
                                rle.AddRange(inputFile.ReadBytes(2));
                            }
                            else if ((slen & 0xf0) == 0xe0)
                            {
                                rle.AddRange(inputFile.ReadBytes(3));
                            }
                            else
                            {
                                throw new FileLoadException("Corrupted RLE data");
                            }
                        }
                        else
                        {
                            rle.Add((byte)buffer);
                        }
                    }

                    layerDef[layerIndex].EncodedRle = rle.ToArray();
                    rle.Clear();
                }

                Parallel.ForEach(batch, CoreSettings.GetParallelOptions(progress), layerIndex =>
                {
                    using var mat = layerDef[layerIndex].DecodeImage(this);
                    _layers[layerIndex] = new Layer((uint)layerIndex, mat, this);
                    layerDef[layerIndex].EncodedRle = null!;

                    progress.LockAndIncrement();
                });
            }
        }

        RebuildLayersProperties();
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = new FileStream(TemporaryOutputFileFullPath, FileMode.Open, FileAccess.Write);
        outputFile.Seek(Header.HeaderLength - Helpers.Serializer.SizeOf(Settings), SeekOrigin.Begin);
        outputFile.WriteSerialize(Settings);
    }
        
    #endregion
}