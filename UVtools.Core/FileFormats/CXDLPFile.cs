﻿/*
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
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using MoreLinq;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class CXDLPFile : FileFormat
    {
        #region Constants
        private const byte HEADER_SIZE = 9; // CXSW3DV2
        private const string HEADER_VALUE = "CXSW3DV2";
        #endregion

        #region Sub Classes
        #region Header
        public sealed class Header
        {
            private string _printerModel = "CL-89";

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

            [FieldOrder(2)]
            [FieldEndianness(Endianness.Big)]
            public ushort Unknown { get; set; } = 2;

            /// <summary>
            /// Gets the size of the printer model
            /// </summary>
            [FieldOrder(3)]
            [FieldEndianness(Endianness.Big)]
            public uint PrinterModelSize { get; set; } = 6;

            /// <summary>
            /// Gets the printer model
            /// </summary>
            /*[FieldOrder(4)]
            [FieldLength(nameof(PrinterModelSize), BindingMode = BindingMode.OneWay)]
            [SerializeAs(SerializedType.TerminatedString)]
            public string PrinterModel
            {
                get => _printerModel;
                set
                {
                    _printerModel = value;
                    PrinterModelSize = string.IsNullOrEmpty(value) ? 0 : (uint)value.Length+1;
                }
            }*/

            [FieldOrder(4)]
            [FieldLength(nameof(PrinterModelSize))]
            public byte[] PrinterModelArray { get; set; } = { 0x43, 0x4C, 0x2D, 0x38, 0x39, 0x0 }; // CL-89

            [Ignore]
            public string PrinterModel
            {
                get => Encoding.ASCII.GetString(PrinterModelArray).TrimEnd(char.MinValue);
                set
                {
                    PrinterModelArray = Encoding.ASCII.GetBytes(value + char.MinValue);
                    PrinterModelSize = (uint) PrinterModelArray.Length;
                }
            }

            /// <summary>
            /// Gets the number of records in the layer table
            /// </summary>
            [FieldOrder(5)] 
            [FieldEndianness(Endianness.Big)] 
            public ushort LayerCount { get; set; }

            /// <summary>
            /// Gets the printer resolution along X axis, in pixels. This information is critical to correctly decoding layer images.
            /// </summary>
            [FieldOrder(6)]
            [FieldEndianness(Endianness.Big)] 
            public ushort ResolutionX { get; set; }

            /// <summary>
            /// Gets the printer resolution along Y axis, in pixels. This information is critical to correctly decoding layer images.
            /// </summary>
            [FieldOrder(7)]
            [FieldEndianness(Endianness.Big)] 
            public ushort ResolutionY { get; set; }
            
            [FieldOrder(8)]
            [FieldLength(64)]
            public byte[] Offset { get; set; } = new byte[64];

            public void Validate()
            {
                if (HeaderSize != HEADER_SIZE || HeaderValue != HEADER_VALUE)
                {
                    throw new FileLoadException("Not a valid CXDLP file!");
                }
            }

            public override string ToString()
            {
                return $"{nameof(HeaderSize)}: {HeaderSize}, {nameof(HeaderValue)}: {HeaderValue}, {nameof(Unknown)}: {Unknown}, {nameof(PrinterModelSize)}: {PrinterModelSize}, {nameof(PrinterModelArray)}: {PrinterModelArray}, {nameof(PrinterModel)}: {PrinterModel}, {nameof(LayerCount)}: {LayerCount}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(Offset)}: {Offset}";
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
            public ushort ExposureTime { get; set; }

            [FieldOrder(7)]
            [FieldEndianness(Endianness.Big)]
            public ushort LightOffDelay { get; set; }

            [FieldOrder(8)]
            [FieldEndianness(Endianness.Big)]
            public ushort BottomExposureTime { get; set; }

            [FieldOrder(9)]
            [FieldEndianness(Endianness.Big)]
            public ushort BottomLayers { get; set; }

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
                return $"{nameof(DisplayWidthDataSize)}: {DisplayWidthDataSize}, {nameof(DisplayWidthBytes)}: {DisplayWidthBytes}, {nameof(DisplayHeightDataSize)}: {DisplayHeightDataSize}, {nameof(DisplayHeightBytes)}: {DisplayHeightBytes}, {nameof(LayerHeightDataSize)}: {LayerHeightDataSize}, {nameof(LayerHeightBytes)}: {LayerHeightBytes}, {nameof(ExposureTime)}: {ExposureTime}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(BottomExposureTime)}: {BottomExposureTime}, {nameof(BottomLayers)}: {BottomLayers}, {nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftSpeed)}: {LiftSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(LightPWM)}: {LightPWM}";
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

            [Ignore] public ushort StartY => (ushort) ((((Coordinates[0] << 8) + Coordinates[1]) >> 3) & 0x1FFF); // 13 bits

            [Ignore] public ushort EndY => (ushort)((((Coordinates[1] << 16) + (Coordinates[2] << 8) + Coordinates[3]) >> 6) & 0x1FFF); // 13 bits

            [Ignore] public ushort StartX => (ushort)(((Coordinates[3] << 8) + Coordinates[4]) & 0x3FFF); // 14 bits
            [Ignore] public ushort Length => (ushort) (EndY - StartY);

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
                Coordinates[0] = (byte) ((startY >> 5) & 0xFF);
                Coordinates[1] = (byte) (((startY << 3) + (endY >> 10)) & 0xFF);
                Coordinates[2] = (byte) ((endY >> 2) & 0xFF);
                Coordinates[3] = (byte)(((endY << 6) + (startX >> 8)) & 0xFF);
                Coordinates[4] = (byte) startX;
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
            public static byte[] Bytes => new byte[] {0x0D, 0x0A};

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
            new("cxdlp", "Creality CXDLP"),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
            PrintParameterModifier.ExposureSeconds,

            PrintParameterModifier.BottomLiftHeight,
            PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,
            PrintParameterModifier.LightOffDelay,

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
                HeaderSettings.ResolutionX = (ushort) value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => HeaderSettings.ResolutionY;
            set
            {
                HeaderSettings.ResolutionY = (ushort) value;
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

        public override byte AntiAliasing
        {
            get => 8;
            set {}
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
            set => base.LayerCount = HeaderSettings.LayerCount = (ushort) base.LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => SlicerInfoSettings.BottomLayers;
            set => base.BottomLayerCount = SlicerInfoSettings.BottomLayers = value;
        }

        public override float BottomExposureTime
        {
            get => SlicerInfoSettings.BottomExposureTime;
            set => base.BottomExposureTime = SlicerInfoSettings.BottomExposureTime = (ushort) value;
        }

        public override float ExposureTime
        {
            get => SlicerInfoSettings.ExposureTime;
            set => base.ExposureTime = SlicerInfoSettings.ExposureTime = (ushort) value;
        }

        public override float BottomLiftHeight
        {
            get => SlicerInfoSettings.BottomLiftHeight;
            set => base.BottomLiftHeight = SlicerInfoSettings.BottomLiftHeight = (ushort) value;
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

        public override float RetractSpeed
        {
            get => SlicerInfoSettings.RetractSpeed;
            set => base.RetractSpeed = SlicerInfoSettings.RetractSpeed = (ushort)value;
        }

        public override float BottomLightOffDelay => SlicerInfoSettings.LightOffDelay;

        public override float LightOffDelay
        {
            get => SlicerInfoSettings.LightOffDelay;
            set => base.LightOffDelay = SlicerInfoSettings.LightOffDelay = (ushort)value;
        }

        public override byte BottomLightPWM
        {
            get => (byte) SlicerInfoSettings.BottomLightPWM;
            set => base.BottomLightPWM = (byte) (SlicerInfoSettings.BottomLightPWM = value);
        }

        public override byte LightPWM
        {
            get => (byte)SlicerInfoSettings.LightPWM;
            set => base.LightPWM = (byte) (SlicerInfoSettings.LightPWM = value);
        }

        public override string MachineName
        {
            get => HeaderSettings.PrinterModel;
            set => base.MachineName = HeaderSettings.PrinterModel = value;
        }

        public override object[] Configs => new object[] { HeaderSettings, SlicerInfoSettings, FooterSettings };

        #endregion

        #region Constructors
        #endregion

        #region Methods

        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);

            if (ResolutionX == 1620 && ResolutionY == 2560)
            {
                MachineName = "CL-60";
            }
            else if (ResolutionX == 3840 && ResolutionY == 2400)
            {
                MachineName = "CL-89";
            }
            else if (!MachineName.StartsWith("CL-"))
            {
                throw new Exception("Unable to detect printer model from resolution, check if resolution is well defined on slicer for your printer model.");
            }

            var pageBreak = PageBreak.Bytes;

            Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

            byte[][] previews = new byte[ThumbnailsOriginalSize.Length][];
            for (int i = 0; i < ThumbnailsOriginalSize.Length; i++)
            {
                previews[i] = new byte[ThumbnailsOriginalSize[i].Area() * 2];
            }
            // Previews
            Parallel.For(0, previews.Length, previewIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                if (Thumbnails[previewIndex] is null) return;
                var span = Thumbnails[previewIndex].GetDataByteSpan();
                int index = 0;
                for (int i = 0; i < span.Length; i += 3)
                {
                    byte b = span[i];
                    byte g = span[i + 1];
                    byte r = span[i + 2];

                    ushort rgb15 = (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | ((b >> 3) << 0));

                    previews[previewIndex][index++] = (byte)(rgb15 >> 8);
                    previews[previewIndex][index++] = (byte)(rgb15 & 0xff);
                }

                if (index != previews[previewIndex].Length)
                {
                    throw new FileLoadException($"Preview encode incomplete encode, expected: {previews[previewIndex].Length}, encoded: {index}");
                }
            });

            for (int i = 0; i < ThumbnailsOriginalSize.Length; i++)
            {
                Helpers.SerializeWriteFileStream(outputFile, previews[i]);
                outputFile.WriteBytes(pageBreak);
                //Helpers.SerializeWriteFileStream(outputFile, pageBreak);
            }
            Helpers.SerializeWriteFileStream(outputFile, SlicerInfoSettings);

            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
            //var preLayers = new PreLayer[LayerCount];
            //var layerDefs = new LayerDef[LayerCount];
            //var layersStreams = new MemoryStream[LayerCount];
            

            for (int layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                //var layer = this[layerIndex];
                outputFile.WriteBytes(BitExtensions.ToBytesBigEndian(this[layerIndex].NonZeroPixelCount));
                //preLayers[layerIndex] = new(layer.NonZeroPixelCount);
            }
            //Helpers.SerializeWriteFileStream(outputFile, preLayers);
            //Helpers.SerializeWriteFileStream(outputFile, pageBreak);
            outputFile.WriteBytes(pageBreak);

            var range = Enumerable.Range(0, (int) LayerCount);

            var layerBytes = new List<byte>[LayerCount];
            foreach (var batch in range.Batch(Environment.ProcessorCount * 10))
            {
                progress.Token.ThrowIfCancellationRequested();

                Parallel.ForEach(batch, layerIndex =>
                {
                    if (progress.Token.IsCancellationRequested) return;
                    var layer = this[layerIndex];
                    using var mat = layer.LayerMat;
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
                                layerBytes[layerIndex].AddRange(LayerLine.GetBytes((ushort)startY, (ushort)(y - 1), (ushort)x, lastColor));
                                lineCount++;
                            }

                            startY = color == 0 ? -1 : y;

                            lastColor = color;
                        }

                        if (startY >= 0)
                        {
                            layerBytes[layerIndex].AddRange(LayerLine.GetBytes((ushort)startY, (ushort)(y - 1), (ushort)x, lastColor));
                            lineCount++;
                        }
                    }

                    layerBytes[layerIndex].InsertRange(0, LayerDef.GetHeaderBytes(layer.NonZeroPixelCount, lineCount));
                    layerBytes[layerIndex].AddRange(pageBreak);

                    progress.LockAndIncrement();
                });

                progress.Token.ThrowIfCancellationRequested();

                foreach (var layerIndex in batch)
                {
                    outputFile.WriteBytes(layerBytes[layerIndex].ToArray());
                    layerBytes[layerIndex] = null;
                }
            }


            /*Parallel.For(0, LayerCount, 
                //new ParallelOptions{MaxDegreeOfParallelism = 1}, 
                layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                //List<LayerLine> layerLines = new();
                var layer = this[layerIndex];
                using var mat = layer.LayerMat;
                var span = mat.GetDataByteSpan();

                layerBytes[layerIndex] = new();
                
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
                            layerBytes[layerIndex].AddRange(LayerLine.GetBytes((ushort)startY, (ushort)(y - 1), (ushort)x, lastColor));
                            //layerLines.Add(new LayerLine((ushort)startY, (ushort)(y - 1), (ushort)x, lastColor));
                            //Debug.WriteLine(layerLines[^1]);
                        }

                        startY = color == 0 ? -1 : y;

                        lastColor = color;
                    }

                    if (startY >= 0)
                    {
                        layerBytes[layerIndex].AddRange(LayerLine.GetBytes((ushort)startY, (ushort)(y - 1), (ushort)x, lastColor));
                        //layerLines.Add(new LayerLine((ushort)startY, (ushort)(y - 1), (ushort)x, lastColor));
                        //Debug.WriteLine(layerLines[^1]);
                    }
                }

                //layerDefs[layerIndex] = new LayerDef(layer.NonZeroPixelCount, (uint)layerLines.Count, layerLines.ToArray());
                //var layerDef = new LayerDef(layer.NonZeroPixelCount, (uint)layerLines.Count, layerLines.ToArray());
                //layersStreams[layerIndex] = new MemoryStream();
                //Helpers.Serializer.Serialize(layersStreams[layerIndex], layerDef);

                //layerBytes[layerIndex].InsertRange(0, LayerDef.GetHeaderBytes(layer.NonZeroPixelCount, (uint) layerBytes[layerIndex].Count));
                //layerBytes[layerIndex].AddRange(PageBreak.Bytes);

                progress.LockAndIncrement();
            });

            progress.Reset(OperationProgress.StatusWritingFile, LayerCount);
            for (int layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                progress.Token.ThrowIfCancellationRequested();
                //Helpers.SerializeWriteFileStream(outputFile, layerDefs[layerIndex]);
                //outputFile.WriteStream(layersStreams[layerIndex]);
                //layersStreams[layerIndex].Dispose();
                outputFile.WriteBytes(LayerDef.GetHeaderBytes(this[layerIndex].NonZeroPixelCount, (uint)layerBytes[layerIndex].Count));
                outputFile.WriteBytes(layerBytes[layerIndex].ToArray());
                outputFile.WriteBytes(pageBreak);
                progress++;
            }*/

            Helpers.SerializeWriteFileStream(outputFile, FooterSettings);

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
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
                var mat = new Mat(ThumbnailsOriginalSize[previewIndex], DepthType.Cv8U, 3);
                var span = mat.GetDataByteSpan();

                int spanIndex = 0;
                for (int i = 0; i < previews[previewIndex].Length; i += 2)
                {
                    ushort rgb15 = (ushort)((ushort)(previews[previewIndex][i + 0] << 8) | previews[previewIndex][i + 1]);
                    byte r = (byte)((rgb15 >> 11) << 3);
                    byte g = (byte)((rgb15 >> 5) << 2);
                    byte b = (byte)((rgb15 >> 0) << 3);

                    span[spanIndex++] = b;
                    span[spanIndex++] = g;
                    span[spanIndex++] = r;
                }

                Thumbnails[previewIndex] = mat;
            });


            SlicerInfoSettings = Helpers.Deserialize<SlicerInfo>(inputFile);
            Debug.WriteLine(SlicerInfoSettings);

            LayerManager.Init(HeaderSettings.LayerCount);
            inputFile.Seek(LayerCount * 4 + 2, SeekOrigin.Current); // Skip pre layers
            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
            /*var preLayers = new PreLayer[LayerCount];
            for (int layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                progress.Token.ThrowIfCancellationRequested();
                preLayers[layerIndex] = Helpers.Deserialize<PreLayer>(inputFile);
                progress++;
            }*/

            //inputFile.Seek(2, SeekOrigin.Current);
            var range = Enumerable.Range(0, (int)LayerCount);

            var linesBytes = new byte[LayerCount][];
            foreach (var batch in range.Batch(Environment.ProcessorCount * 10))
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
                    using var mat = EmguExtensions.InitMat(Resolution);

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

                        CvInvoke.Line(mat, new Point(line.StartX, line.StartY), new Point(line.StartX, line.EndY), new MCvScalar(line.Gray));
                    }

                    linesBytes[layerIndex] = null;

                    this[layerIndex] = new Layer((uint)layerIndex, mat, this);

                    progress.LockAndIncrement();
                });
            }

            progress.Token.ThrowIfCancellationRequested();

            /*var layerDefs = new LayerDef[LayerCount];
            for (int layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                progress.Token.ThrowIfCancellationRequested();
                layerDefs[layerIndex] = Helpers.Deserialize<LayerDef>(inputFile);
                progress++;
            }

            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);
            Parallel.For(0, LayerCount, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                using var mat = EmguExtensions.InitMat(Resolution);
                foreach (var line in layerDefs[layerIndex].Lines)
                {
                    CvInvoke.Line(mat, new Point(line.StartX, line.StartY), new Point(line.StartX, line.EndY), new MCvScalar(line.Gray));
                }

                this[layerIndex] = new Layer((uint)layerIndex, mat, this);
                progress.LockAndIncrement();
            });*/

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
