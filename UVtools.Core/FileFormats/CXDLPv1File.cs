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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinarySerialization;
using Emgu.CV;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class CXDLPv1File : FileFormat
    {
        #region Constants
        private const byte HEADER_SIZE = 9; // CXSW3DV2
        private const string HEADER_VALUE = "CXSW3DV2";
        #endregion

        #region Sub Classes
        #region Header
        public sealed class Header
        {
            /// <summary>
            /// Gets the size of the header
            /// </summary>
            [FieldOrder(0)]
            [FieldEndianness(Endianness.Big)]
            public uint HeaderSize { get; set; } = HEADER_SIZE;

            /// <summary>
            /// Gets the header name
            /// </summary>
            [FieldOrder(1)]
            [FieldLength(HEADER_SIZE)]
            [SerializeAs(SerializedType.TerminatedString)]
            public string HeaderValue { get; set; } = HEADER_VALUE;

            /// <summary>
            /// Gets the number of records in the layer table
            /// </summary>
            [FieldOrder(2)]
            [FieldEndianness(Endianness.Big)]
            public ushort LayerCount { get; set; }

            /// <summary>
            /// Gets the printer resolution along X axis, in pixels. This information is critical to correctly decoding layer images.
            /// </summary>
            [FieldOrder(3)]
            [FieldEndianness(Endianness.Big)]
            public ushort ResolutionX { get; set; }

            /// <summary>
            /// Gets the printer resolution along Y axis, in pixels. This information is critical to correctly decoding layer images.
            /// </summary>
            [FieldOrder(4)]
            [FieldEndianness(Endianness.Big)]
            public ushort ResolutionY { get; set; }

            public void Validate()
            {
                if (HeaderSize != HEADER_SIZE || HeaderValue != HEADER_VALUE)
                {
                    throw new FileLoadException("Not a valid CXDLP file!");
                }
            }

            public override string ToString()
            {
                return $"{nameof(HeaderSize)}: {HeaderSize}, {nameof(HeaderValue)}: {HeaderValue}, {nameof(LayerCount)}: {LayerCount}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}";
            }
        }

        #endregion

        #region SlicerInfo
        // Address: 363407
        public sealed class SlicerInfo
        {
            [FieldOrder(0)]
            [FieldEndianness(Endianness.Big)]
            public uint DisplayWidthDataSize { get; set; } = 20;

            [FieldOrder(1)]
            [FieldLength(nameof(DisplayWidthDataSize))]
            public byte[] DisplayWidthBytes { get; set; }

            [FieldOrder(2)]
            [FieldEndianness(Endianness.Big)]
            public uint DisplayHeightDataSize { get; set; } = 20;

            [FieldOrder(3)]
            [FieldLength(nameof(DisplayHeightDataSize))]
            public byte[] DisplayHeightBytes { get; set; }

            [FieldOrder(4)]
            [FieldEndianness(Endianness.Big)]
            public uint LayerHeightDataSize { get; set; } = 16;

            [FieldOrder(5)]
            [FieldLength(nameof(LayerHeightDataSize))]
            public byte[] LayerHeightBytes { get; set; }

            [FieldOrder(6)]
            [FieldEndianness(Endianness.Big)]
            public ushort WaitTimeBeforeCure { get; set; }

            [FieldOrder(7)]
            [FieldEndianness(Endianness.Big)]
            public ushort ExposureTime { get; set; }

            [FieldOrder(8)]
            [FieldEndianness(Endianness.Big)]
            public ushort BottomExposureTime { get; set; }

            [FieldOrder(9)]
            [FieldEndianness(Endianness.Big)]
            public ushort BottomLayersCount { get; set; }

            [FieldOrder(10)]
            [FieldEndianness(Endianness.Big)]
            public ushort BottomLiftHeight { get; set; }

            [FieldOrder(11)]
            [FieldEndianness(Endianness.Big)]
            public ushort BottomLiftSpeed { get; set; }

            [FieldOrder(12)]
            [FieldEndianness(Endianness.Big)]
            public ushort LiftHeight { get; set; }

            [FieldOrder(13)]
            [FieldEndianness(Endianness.Big)]
            public ushort LiftSpeed { get; set; }

            [FieldOrder(14)]
            [FieldEndianness(Endianness.Big)]
            public ushort RetractSpeed { get; set; }

            [FieldOrder(15)]
            [FieldEndianness(Endianness.Big)]
            public ushort BottomLightPWM { get; set; } = 255;

            [FieldOrder(16)]
            [FieldEndianness(Endianness.Big)]
            public ushort LightPWM { get; set; } = 255;

            public override string ToString()
            {
                return $"{nameof(DisplayWidthDataSize)}: {DisplayWidthDataSize}, {nameof(DisplayWidthBytes)}: {DisplayWidthBytes}, {nameof(DisplayHeightDataSize)}: {DisplayHeightDataSize}, {nameof(DisplayHeightBytes)}: {DisplayHeightBytes}, {nameof(LayerHeightDataSize)}: {LayerHeightDataSize}, {nameof(LayerHeightBytes)}: {LayerHeightBytes}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(WaitTimeBeforeCure)}: {WaitTimeBeforeCure}, {nameof(BottomExposureTime)}: {BottomExposureTime}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(LightPWM)}: {LightPWM}";
            }
        }
        #endregion

        #region Layer Def

        public sealed class PreLayer
        {
            [FieldOrder(0)]
            [FieldEndianness(Endianness.Big)]
            public uint Unknown { get; set; }

            public PreLayer()
            {
            }

            public PreLayer(uint unknown)
            {
                Unknown = unknown;
            }
        }

        public sealed class LayerDef
        {
            [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public uint Unknown { get; set; }
            [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public uint LineCount { get; set; }
            [FieldOrder(2)] [FieldCount(nameof(LineCount))] public LayerLine[] Lines { get; set; }
            [FieldOrder(3)] public PageBreak PageBreak { get; set; } = new();

            public static byte[] GetHeaderBytes(uint unknown, uint lineCount)
            {
                var bytes = new byte[8];
                BitExtensions.ToBytesBigEndian(unknown, bytes);
                BitExtensions.ToBytesBigEndian(lineCount, bytes, 4);
                return bytes;
            }

            public LayerDef() { }

            public LayerDef(uint unknown, uint lineCount, LayerLine[] lines)
            {
                Unknown = unknown;
                LineCount = lineCount;
                Lines = lines;
            }
        }

        public sealed class LayerLine
        {
            public const byte CoordinateCount = 5;
            [FieldOrder(0)] [FieldCount(CoordinateCount)] public byte[] Coordinates { get; set; } = new byte[CoordinateCount];
            //[FieldOrder(0)] [FieldEndianness(Endianness.Big)] [FieldBitLength(13)] public ushort StartY { get; set; }
            //[FieldOrder(1)] [FieldEndianness(Endianness.Big)] [FieldBitLength(13)] public ushort EndY { get; set; }
            //[FieldOrder(2)] [FieldEndianness(Endianness.Big)] [FieldBitLength(14)] public ushort StartX { get; set; }
            [FieldOrder(1)] public byte Gray { get; set; }

            [Ignore] public ushort StartY => (ushort)((((Coordinates[0] << 8) + Coordinates[1]) >> 3) & 0x1FFF); // 13 bits

            [Ignore] public ushort EndY => (ushort)((((Coordinates[1] << 16) + (Coordinates[2] << 8) + Coordinates[3]) >> 6) & 0x1FFF); // 13 bits

            [Ignore] public ushort StartX => (ushort)(((Coordinates[3] << 8) + Coordinates[4]) & 0x3FFF); // 14 bits
            [Ignore] public ushort Length => (ushort)(EndY - StartY);

            public static byte[] GetBytes(ushort startY, ushort endY, ushort startX, byte gray)
            {
                var bytes = new byte[CoordinateCount + 1];
                bytes[0] = (byte)((startY >> 5) & 0xFF);
                bytes[1] = (byte)(((startY << 3) + (endY >> 10)) & 0xFF);
                bytes[2] = (byte)((endY >> 2) & 0xFF);
                bytes[3] = (byte)(((endY << 6) + (startX >> 8)) & 0xFF);
                bytes[4] = (byte)startX;
                bytes[5] = gray;
                return bytes;
            }

            public LayerLine() { }

            public LayerLine(ushort startY, ushort endY, ushort startX, byte gray)
            {
                Coordinates[0] = (byte)((startY >> 5) & 0xFF);
                Coordinates[1] = (byte)(((startY << 3) + (endY >> 10)) & 0xFF);
                Coordinates[2] = (byte)((endY >> 2) & 0xFF);
                Coordinates[3] = (byte)(((endY << 6) + (startX >> 8)) & 0xFF);
                Coordinates[4] = (byte)startX;
                /*StartY = startY;
                EndY = endY;
                StartX = startX;*/
                Gray = gray;
            }

            public override string ToString()
            {
                return $"{nameof(Gray)}: {Gray}, {nameof(StartY)}: {StartY}, {nameof(EndY)}: {EndY}, {nameof(StartX)}: {StartX}, {nameof(Length)}: {Length}";
            }
        }

        public sealed class PageBreak
        {
            public static byte[] Bytes => new byte[] { 0x0D, 0x0A };

            [FieldOrder(0)] public byte Line { get; set; } = 0x0D;
            [FieldOrder(1)] public byte Break { get; set; } = 0x0A;
        }

        #endregion

        #region Footer
        public sealed class Footer
        {
            /// <summary>
            /// Gets the size of the header
            /// </summary>
            [FieldOrder(0)]
            [FieldEndianness(Endianness.Big)]
            public uint FooterSize { get; set; } = HEADER_SIZE;

            /// <summary>
            /// Gets the header name
            /// </summary>
            [FieldOrder(1)]
            [FieldLength(HEADER_SIZE)]
            [SerializeAs(SerializedType.TerminatedString)]
            public string FooterValue { get; set; } = HEADER_VALUE;

            [FieldOrder(2)]
            [FieldEndianness(Endianness.Big)]
            public uint Unknown { get; set; } = 7;

            public void Validate()
            {
                if (FooterSize != HEADER_SIZE || FooterValue != HEADER_VALUE)
                {
                    throw new FileLoadException("Not a valid CXDLP file!");
                }
            }
        }
        #endregion

        #endregion

        #region Properties

        public Header HeaderSettings { get; protected internal set; } = new();
        public SlicerInfo SlicerInfoSettings { get; protected internal set; } = new();
        public Footer FooterSettings { get; protected internal set; } = new();

        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new(typeof(CXDLPv1File), "v1.cxdlp", "Creality CXDLP v1"),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,

            PrintParameterModifier.WaitTimeBeforeCure,

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

        public override Size[] ThumbnailsOriginalSize { get; } =
        {
            new(116, 116),
            new(290, 290),
            new(290, 290)
        };

        public override uint ResolutionX
        {
            get => HeaderSettings.ResolutionX;
            set
            {
                HeaderSettings.ResolutionX = (ushort)value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => HeaderSettings.ResolutionY;
            set
            {
                HeaderSettings.ResolutionY = (ushort)value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayWidth
        {
            get => float.Parse(Encoding.ASCII.GetString(SlicerInfoSettings.DisplayWidthBytes.Where(b => b != 0).ToArray()));
            set
            {
                string str = Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);
                SlicerInfoSettings.DisplayWidthDataSize = (uint)(str.Length * 2);
                var data = new byte[SlicerInfoSettings.DisplayWidthDataSize];
                for (var i = 0; i < str.Length; i++)
                {
                    data[i * 2 + 1] = System.Convert.ToByte(str[i]);
                }

                SlicerInfoSettings.DisplayWidthBytes = data;
                RaisePropertyChanged();
            }
        }

        public override float DisplayHeight
        {
            get => float.Parse(Encoding.ASCII.GetString(SlicerInfoSettings.DisplayHeightBytes.Where(b => b != 0).ToArray()));
            set
            {
                string str = Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);
                SlicerInfoSettings.DisplayHeightDataSize = (uint)(str.Length * 2);
                var data = new byte[SlicerInfoSettings.DisplayHeightDataSize];
                for (var i = 0; i < str.Length; i++)
                {
                    data[i * 2 + 1] = System.Convert.ToByte(str[i]);
                }

                SlicerInfoSettings.DisplayHeightBytes = data;
                RaisePropertyChanged();
            }
        }

        public override float LayerHeight
        {
            get => float.Parse(Encoding.ASCII.GetString(SlicerInfoSettings.LayerHeightBytes.Where(b => b != 0).ToArray()));
            set
            {
                string str = Layer.RoundHeight(value).ToString(CultureInfo.InvariantCulture);
                SlicerInfoSettings.LayerHeightDataSize = (uint)(str.Length * 2);
                var data = new byte[SlicerInfoSettings.LayerHeightDataSize];
                for (var i = 0; i < str.Length; i++)
                {
                    data[i * 2 + 1] = System.Convert.ToByte(str[i]);
                }

                SlicerInfoSettings.LayerHeightBytes = data;
                RaisePropertyChanged();
            }
        }

        public override uint LayerCount
        {
            get => base.LayerCount;
            set => base.LayerCount = HeaderSettings.LayerCount = (ushort)base.LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => SlicerInfoSettings.BottomLayersCount;
            set => base.BottomLayerCount = SlicerInfoSettings.BottomLayersCount = value;
        }

        public override float BottomWaitTimeBeforeCure => WaitTimeBeforeCure;

        public override float WaitTimeBeforeCure
        {
            get => SlicerInfoSettings.WaitTimeBeforeCure;
            set => base.WaitTimeBeforeCure = SlicerInfoSettings.WaitTimeBeforeCure = (ushort)value;
        }

        public override float BottomExposureTime
        {
            get => SlicerInfoSettings.BottomExposureTime;
            set => base.BottomExposureTime = SlicerInfoSettings.BottomExposureTime = (ushort)value;
        }

        public override float ExposureTime
        {
            get => SlicerInfoSettings.ExposureTime;
            set => base.ExposureTime = SlicerInfoSettings.ExposureTime = (ushort)value;
        }

        public override float BottomLiftHeight
        {
            get => SlicerInfoSettings.BottomLiftHeight;
            set => base.BottomLiftHeight = SlicerInfoSettings.BottomLiftHeight = (ushort)value;
        }

        public override float LiftHeight
        {
            get => SlicerInfoSettings.LiftHeight;
            set => base.LiftHeight = SlicerInfoSettings.LiftHeight = (ushort)value;
        }

        public override float BottomLiftSpeed
        {
            get => SlicerInfoSettings.BottomLiftSpeed;
            set => base.BottomLiftSpeed = SlicerInfoSettings.BottomLiftSpeed = (ushort)value;
        }

        public override float LiftSpeed
        {
            get => SlicerInfoSettings.LiftSpeed;
            set => base.LiftSpeed = SlicerInfoSettings.LiftSpeed = (ushort)value;
        }

        public override float BottomRetractSpeed => RetractSpeed;

        public override float RetractSpeed
        {
            get => SlicerInfoSettings.RetractSpeed;
            set => base.RetractSpeed = SlicerInfoSettings.RetractSpeed = (ushort)value;
        }

        public override byte BottomLightPWM
        {
            get => (byte)SlicerInfoSettings.BottomLightPWM;
            set => base.BottomLightPWM = (byte)(SlicerInfoSettings.BottomLightPWM = value);
        }

        public override byte LightPWM
        {
            get => (byte)SlicerInfoSettings.LightPWM;
            set => base.LightPWM = (byte)(SlicerInfoSettings.LightPWM = value);
        }

        public override object[] Configs => new object[] { HeaderSettings, SlicerInfoSettings, FooterSettings };

        #endregion

        #region Constructors
        #endregion

        #region Methods

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);

            if (ResolutionX == 2560 && ResolutionY == 1620)
            {
                MachineName = "CL-60";
            }
            else if (ResolutionX == 3840 && ResolutionY == 2400)
            {
                MachineName = "CL-89";
            }

            var pageBreak = PageBreak.Bytes;

            Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

            var previews = new byte[ThumbnailsOriginalSize.Length][];

            // Previews
            Parallel.For(0, previews.Length, previewIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                var encodeLength = ThumbnailsOriginalSize[previewIndex].Area() * 2;
                if (Thumbnails[previewIndex] is null)
                {
                    previews[previewIndex] = new byte[encodeLength];
                    return;
                }

                previews[previewIndex] = EncodeImage(DATATYPE_RGB565_BE, Thumbnails[previewIndex]);

                if (encodeLength != previews[previewIndex].Length)
                {
                    throw new FileLoadException($"Preview encode incomplete encode, expected: {previews[previewIndex].Length}, encoded: {encodeLength}");
                }
            });

            for (int i = 0; i < ThumbnailsOriginalSize.Length; i++)
            {
                Helpers.SerializeWriteFileStream(outputFile, previews[i]);
                outputFile.WriteBytes(pageBreak);
                previews[i] = null;
            }
            Helpers.SerializeWriteFileStream(outputFile, SlicerInfoSettings);

            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
            

            for (int layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                outputFile.WriteBytes(BitExtensions.ToBytesBigEndian(this[layerIndex].NonZeroPixelCount));
            }
            outputFile.WriteBytes(pageBreak);

            var layerBytes = new List<byte>[LayerCount];
            foreach (var batch in BatchLayersIndexes())
            {
                progress.Token.ThrowIfCancellationRequested();

                Parallel.ForEach(batch, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    var layer = this[layerIndex];
                    using (var mat = layer.LayerMat)
                    {
                        var span = mat.GetDataByteSpan();

                        layerBytes[layerIndex] = new();

                        uint lineCount = 0;

                        for (int x = layer.BoundingRectangle.X; x < layer.BoundingRectangle.Right; x++)
                        {
                            int y = layer.BoundingRectangle.Y;
                            int startY = -1;
                            byte lastColor = 0;
                            for (; y < layer.BoundingRectangle.Bottom; y++)
                            {
                                int pos = mat.GetPixelPos(x, y);
                                byte color = span[pos];

                                if (lastColor == color && color != 0) continue;

                                if (startY >= 0)
                                {
                                    layerBytes[layerIndex].AddRange(LayerLine.GetBytes((ushort)startY, (ushort)(y - 1),
                                        (ushort)x, lastColor));
                                    lineCount++;
                                }

                                startY = color == 0 ? -1 : y;

                                lastColor = color;
                            }

                            if (startY >= 0)
                            {
                                layerBytes[layerIndex].AddRange(LayerLine.GetBytes((ushort)startY, (ushort)(y - 1),
                                    (ushort)x, lastColor));
                                lineCount++;
                            }
                        }

                        layerBytes[layerIndex].InsertRange(0, LayerDef.GetHeaderBytes(layer.NonZeroPixelCount, lineCount));
                        layerBytes[layerIndex].AddRange(pageBreak);
                    }

                    progress.LockAndIncrement();
                });

                progress.Token.ThrowIfCancellationRequested();

                foreach (var layerIndex in batch)
                {
                    outputFile.WriteBytes(layerBytes[layerIndex].ToArray());
                    layerBytes[layerIndex] = null;
                }
            }


            Helpers.SerializeWriteFileStream(outputFile, FooterSettings);

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine(SlicerInfoSettings);
            Debug.WriteLine("-End-");
        }

        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);
            HeaderSettings = Helpers.Deserialize<Header>(inputFile);
            HeaderSettings.Validate();

            Debug.WriteLine(HeaderSettings);

            byte[][] previews = new byte[ThumbnailsOriginalSize.Length][];
            for (int i = 0; i < ThumbnailsOriginalSize.Length; i++)
            {
                previews[i] = new byte[ThumbnailsOriginalSize[i].Area() * 2];
                inputFile.ReadBytes(previews[i]);
                inputFile.Seek(2, SeekOrigin.Current);
            }

            Parallel.For(0, previews.Length, previewIndex =>
            {
                Thumbnails[previewIndex] = DecodeImage(DATATYPE_RGB565_BE, previews[previewIndex], ThumbnailsOriginalSize[previewIndex]);
                previews[previewIndex] = null;
            });


            SlicerInfoSettings = Helpers.Deserialize<SlicerInfo>(inputFile);
            Debug.WriteLine(SlicerInfoSettings);

            LayerManager.Init(HeaderSettings.LayerCount);
            inputFile.Seek(LayerCount * 4 + 2, SeekOrigin.Current); // Skip pre layers


            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

            var range = Enumerable.Range(0, (int)LayerCount);

            var linesBytes = new byte[LayerCount][];
            foreach (var batch in BatchLayersIndexes())
            {
                progress.Token.ThrowIfCancellationRequested();

                foreach (var layerIndex in batch)
                {
                    inputFile.Seek(4, SeekOrigin.Current);
                    var lineCount = BitExtensions.ToUIntBigEndian(inputFile.ReadBytes(4));

                    linesBytes[layerIndex] = new byte[lineCount * 6];
                    inputFile.ReadBytes(linesBytes[layerIndex]);
                    inputFile.Seek(2, SeekOrigin.Current);

                    progress.Token.ThrowIfCancellationRequested();
                }

                Parallel.ForEach(batch, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    using (var mat = EmguExtensions.InitMat(Resolution))
                    {
                        for (int i = 0; i < linesBytes[layerIndex].Length; i++)
                        {
                            LayerLine line = new()
                            {
                                Coordinates =
                                {
                                    [0] = linesBytes[layerIndex][i++],
                                    [1] = linesBytes[layerIndex][i++],
                                    [2] = linesBytes[layerIndex][i++],
                                    [3] = linesBytes[layerIndex][i++],
                                    [4] = linesBytes[layerIndex][i++]
                                },
                                Gray = linesBytes[layerIndex][i]
                            };

                            CvInvoke.Line(mat, new Point(line.StartX, line.StartY), new Point(line.StartX, line.EndY),
                                new MCvScalar(line.Gray));
                        }

                        linesBytes[layerIndex] = null;

                        this[layerIndex] = new Layer((uint)layerIndex, mat, this);
                    }

                    progress.LockAndIncrement();
                });
            }

            progress.Token.ThrowIfCancellationRequested();

            FooterSettings = Helpers.Deserialize<Footer>(inputFile);
            FooterSettings.Validate();
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

            var offset = Helpers.Serializer.SizeOf(HeaderSettings);
            foreach (var size in ThumbnailsOriginalSize)
            {
                offset += size.Area() * 2 + 2; // + page break
            }

            using var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write);
            outputFile.Seek(offset, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, SlicerInfoSettings);
        }

        #endregion
    }
}
