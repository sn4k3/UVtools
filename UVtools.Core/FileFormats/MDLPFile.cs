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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BinarySerialization;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class MDLPFile : FileFormat
    {
        #region Constants

        private const uint SlicerInfoAddress = 4 + 7 + 290 * 290 * 2 + 116 * 116 * 2 + 4;
        #endregion

        #region Sub Classes

        #region Header

        public sealed class Header
        {
            public const byte HEADER_SIZE = 7;
            public const string HEADER_VALUE = "MKSDLP";

            [FieldOrder(0)]
            [FieldEndianness(Endianness.Big)]
            public uint HeaderSize { get; set; } = HEADER_SIZE;

            /// <summary>
            /// Gets the file tag = MKSDLP
            /// </summary>
            [FieldOrder(1)]
            [FieldLength(HEADER_SIZE)]
            [SerializeAs(SerializedType.TerminatedString)]
            public string HeaderValue { get; set; } = HEADER_VALUE;

            public void Validate()
            {
                if (HeaderSize != HEADER_SIZE || HeaderValue != HEADER_VALUE)
                {
                    throw new FileLoadException("Not a valid Makerbase mdlp file!");
                }
            }
        }

        public sealed class SlicerInfo
        {
            // 290 * 290 * 2 + 116 * 116 * 2 + 4
            //[FieldOrder(0)] [FieldLength(195116)] public byte[] PreviewData { get; set; }

            [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public ushort LayerCount { get; set; }
            [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public ushort ResolutionX { get; set; }
            [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public ushort ResolutionY { get; set; }
            [FieldOrder(3)] [FieldEndianness(Endianness.Big)] public uint DisplayWidthDataSize { get; set; } = 6;
            [FieldOrder(4)] [FieldLength(nameof(DisplayWidthDataSize))] public byte[] DisplayWidthBytes { get; set; }
            [FieldOrder(5)] [FieldEndianness(Endianness.Big)] public uint DisplayHeightDataSize { get; set; } = 6;
            [FieldOrder(6)] [FieldLength(nameof(DisplayHeightDataSize))] public byte[] DisplayHeightBytes { get; set; }

            [FieldOrder(7)] [FieldEndianness(Endianness.Big)] public uint LayerHeightDataSize { get; set; } = 6;
            [FieldOrder(8)] [FieldLength(nameof(LayerHeightDataSize))] public byte[] LayerHeightBytes { get; set; }
            [FieldOrder(9)] [FieldEndianness(Endianness.Big)] public ushort ExposureTime { get; set; }
            [FieldOrder(10)] [FieldEndianness(Endianness.Big)] public ushort LightOffDelay { get; set; }
            [FieldOrder(11)] [FieldEndianness(Endianness.Big)] public ushort BottomExposureTime { get; set; }
            [FieldOrder(12)] [FieldEndianness(Endianness.Big)] public ushort BottomLayers { get; set; }
           /* [FieldOrder(13)] [FieldEndianness(Endianness.Big)] public ushort BottomLiftHeight { get; set; }
            [FieldOrder(14)] [FieldEndianness(Endianness.Big)] public ushort BottomLiftSpeed { get; set; }
            [FieldOrder(15)] [FieldEndianness(Endianness.Big)] public ushort LiftHeight { get; set; }
            [FieldOrder(16)] [FieldEndianness(Endianness.Big)] public ushort LiftSpeed { get; set; }
            [FieldOrder(17)] [FieldEndianness(Endianness.Big)] public ushort RetractSpeed { get; set; }
            [FieldOrder(18)] [FieldEndianness(Endianness.Big)] public ushort BottomLightPWM { get; set; }
            [FieldOrder(19)] [FieldEndianness(Endianness.Big)] public ushort LightPWM { get; set; }*/
        }
        #endregion

        #region LayerDef

        public sealed class LayerDef
        {
            [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public uint LineCount { get; set; }
            [FieldOrder(1)] [FieldCount(nameof(LineCount))] public LayerLine[] Lines { get; set; }
            [FieldOrder(2)] public PageBreak PageBreak { get; set; } = new();


            public LayerDef(uint lineCount, LayerLine[] lines)
            {
                LineCount = lineCount;
                Lines = lines;
            }
        }

        public sealed class LayerLine
        {
            [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public ushort StartY { get; set; }
            [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public ushort EndY { get; set; }
            [FieldOrder(2)] [FieldEndianness(Endianness.Big)] public ushort StartX { get; set; }


            public LayerLine()
            { }

            public LayerLine(ushort startY, ushort endY, ushort startX)
            {
                StartY = startY;
                EndY = endY;
                StartX = startX;
            }
        }

        public sealed class PageBreak
        {
            [FieldOrder(0)] [FieldEndianness(Endianness.Big)] public byte Line { get; set; } = 0x0D;
            [FieldOrder(1)] [FieldEndianness(Endianness.Big)] public byte Break { get; set; } = 0x0A;
        }

        #endregion
        #endregion

        #region Properties

        public Header HeaderSettings { get; protected internal set; } = new();
        public SlicerInfo SlicerInfoSettings { get; protected internal set; } = new();
        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new ("mdlp", "Makerbase MDLP v1"),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
            PrintParameterModifier.ExposureSeconds,

            //PrintParameterModifier.BottomLightOffDelay,
            PrintParameterModifier.LightOffDelay,
            /*PrintParameterModifier.BottomLiftHeight,
            PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,

            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,*/
        };

        public override Size[] ThumbnailsOriginalSize { get; } =
        {
            new (116, 116),
            new (290, 290)
        };

        public override uint ResolutionX
        {
            get => SlicerInfoSettings.ResolutionX;
            set
            {
                SlicerInfoSettings.ResolutionX = (ushort) value;
                RaisePropertyChanged();
            }
        }

        public override uint ResolutionY
        {
            get => SlicerInfoSettings.ResolutionY;
            set
            {
                SlicerInfoSettings.ResolutionY = (ushort) value;
                RaisePropertyChanged();
            }
        }

        public override float DisplayWidth
        {
            get => float.Parse(Encoding.ASCII.GetString(SlicerInfoSettings.DisplayWidthBytes.Where(b => b != 0).ToArray()));
            set
            {
                string str = Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);
                SlicerInfoSettings.DisplayWidthDataSize = (uint) (str.Length * 2);
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

        public override bool MirrorDisplay { get; set; }

        public override byte AntiAliasing
        {
            get => 1;
            set { }
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
            set => base.LayerCount = SlicerInfoSettings.LayerCount = (ushort)base.LayerCount;
        }

        public override ushort BottomLayerCount
        {
            get => SlicerInfoSettings.BottomLayers;
            set => base.BottomLayerCount = SlicerInfoSettings.BottomLayers = value;
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

        public override float BottomLightOffDelay
        {
            get => LightOffDelay;
            set { }
        }

        public override float LightOffDelay
        {
            get => SlicerInfoSettings.LightOffDelay;
            set => base.LightOffDelay = SlicerInfoSettings.LightOffDelay = (ushort)value;
        }

        public override object[] Configs => new[] { (object)HeaderSettings, SlicerInfoSettings };

        #endregion

        #region Constructors

        #endregion

        #region Methods
        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
            var pageBreak = new PageBreak();

            Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

            byte[][] previews = new byte[2][];
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
                Helpers.SerializeWriteFileStream(outputFile, pageBreak);
            }
            Helpers.SerializeWriteFileStream(outputFile, SlicerInfoSettings);
            
            progress.Reset(OperationProgress.StatusEncodeLayers, LayerCount);
            var layerDefs = new LayerDef[LayerCount];
            Parallel.For(0, LayerCount, layerIndex =>
            {
                if (progress.Token.IsCancellationRequested) return;
                List<LayerLine> layerLines = new();
                var layer = this[layerIndex];
                using var mat = layer.LayerMat;
                var span = mat.GetDataByteSpan();

                for (int x = layer.BoundingRectangle.X; x < layer.BoundingRectangle.Right; x++)
                {
                    int y = layer.BoundingRectangle.Y;
                    int startY = -1;
                    for (; y < layer.BoundingRectangle.Bottom; y++)
                    {
                        int pos = mat.GetPixelPos(x, y);
                        if (span[pos] < 128) // Black pixel
                        {
                            if(startY == -1) continue; // Keep ignoring
                            layerLines.Add(new LayerLine((ushort) startY, (ushort) (y-1), (ushort) x));
                            startY = -1;
                        }
                        else  // White pixel
                        {
                            if (startY >= 0) continue; // Keep sum
                            startY = y;
                        }
                    }

                    if (startY >= 0)
                    {
                        layerLines.Add(new LayerLine((ushort)startY, (ushort)(y - 1), (ushort) x));
                    }
                }

                layerDefs[layerIndex] = new LayerDef((uint) layerLines.Count, layerLines.ToArray());

                progress.LockAndIncrement();
            });

            progress.Reset(OperationProgress.StatusWritingFile, LayerCount);
            for (int layerIndex = 0; layerIndex < LayerCount; layerIndex++)
            {
                progress.Token.ThrowIfCancellationRequested();
                Helpers.SerializeWriteFileStream(outputFile, layerDefs[layerIndex]);
                progress++;
            }

            Helpers.SerializeWriteFileStream(outputFile, HeaderSettings);

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine("-End-");
        }



        protected override void DecodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var inputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);
            //HeaderSettings = Helpers.ByteToType<CbddlpFile.Header>(InputFile);
            //HeaderSettings = Helpers.Serializer.Deserialize<Header>(InputFile.ReadBytes(Helpers.Serializer.SizeOf(typeof(Header))));
            HeaderSettings = Helpers.Deserialize<Header>(inputFile);
            HeaderSettings.Validate();

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
                    ushort rgb15 = BitExtensions.ToUShortBigEndian(previews[previewIndex][i], previews[previewIndex][i + 1]);
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

            LayerManager.Init(SlicerInfoSettings.LayerCount);
            progress.ItemCount = LayerCount;
            LayerDef[] layerDefs = new LayerDef[LayerCount];
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
                    CvInvoke.Line(mat, new Point(line.StartX, line.StartY), new Point(line.StartX, line.EndY), EmguExtensions.WhiteColor);
                }

                this[layerIndex] = new Layer((uint) layerIndex, mat, this);
                progress.LockAndIncrement();
            });

            HeaderSettings = Helpers.Deserialize<Header>(inputFile);
            HeaderSettings.Validate();
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

            using var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write);
            outputFile.Seek(SlicerInfoAddress, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(outputFile, SlicerInfoSettings);
        }

        #endregion
    }
}
