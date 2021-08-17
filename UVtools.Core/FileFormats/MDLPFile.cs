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
using Emgu.CV.CvEnum;
using MoreLinq;
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

            public static byte[] GetBytes(ushort StartY, ushort EndY, ushort StartX)
            {
                var bytes = new byte[6];
                BitExtensions.ToBytesBigEndian(StartY, bytes);
                BitExtensions.ToBytesBigEndian(EndY, bytes, 2);
                BitExtensions.ToBytesBigEndian(StartX, bytes, 4);
                return bytes;
            }


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
            public static byte[] Bytes => new byte[] {0x0D, 0x0A};
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
            new (typeof(MDLPFile), "mdlp", "Makerbase MDLP v1"),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.LightOffDelay,
            PrintParameterModifier.BottomExposureTime,
            PrintParameterModifier.ExposureTime,

            //PrintParameterModifier.BottomLightOffDelay,
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

        public override bool DisplayMirror { get; set; }
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

        public override float BottomLightOffDelay => LightOffDelay;

        public override float LightOffDelay
        {
            get => SlicerInfoSettings.LightOffDelay;
            set => base.LightOffDelay = SlicerInfoSettings.LightOffDelay = (ushort)value;
        }

        public override float BottomWaitTimeBeforeCure
        {
            get => base.BottomWaitTimeBeforeCure;
            set
            {
                SetBottomLightOffDelay(value);
                base.BottomWaitTimeBeforeCure = value;
            }
        }

        public override float WaitTimeBeforeCure
        {
            get => base.WaitTimeBeforeCure;
            set
            {
                SetNormalLightOffDelay(value);
                base.WaitTimeBeforeCure = value;
            }
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

        public override object[] Configs => new[] { (object)HeaderSettings, SlicerInfoSettings };

        #endregion

        #region Constructors

        #endregion

        #region Methods
        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            using var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);
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
            var range = Enumerable.Range(0, (int)LayerCount);

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
                            for (; y < layer.BoundingRectangle.Bottom; y++)
                            {
                                int pos = mat.GetPixelPos(x, y);
                                if (span[pos] < 128) // Black pixel
                                {
                                    if (startY == -1) continue; // Keep ignoring
                                    layerBytes[layerIndex]
                                        .AddRange(LayerLine.GetBytes((ushort)startY, (ushort)(y - 1), (ushort)x));
                                    startY = -1;
                                    lineCount++;
                                }
                                else
                                {
                                    if (startY >= 0) continue; // Keep sum
                                    startY = y;
                                }
                            }

                            if (startY >= 0)
                            {
                                layerBytes[layerIndex].AddRange(LayerLine.GetBytes((ushort)startY, (ushort)(y - 1), (ushort)x));
                                lineCount++;
                            }
                        }

                        layerBytes[layerIndex].InsertRange(0, BitExtensions.ToBytesBigEndian(lineCount));
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
                Thumbnails[previewIndex] = DecodeImage(DATATYPE_RGB565_BE, previews[previewIndex], ThumbnailsOriginalSize[previewIndex]);
                previews[previewIndex] = null;
            });


            SlicerInfoSettings = Helpers.Deserialize<SlicerInfo>(inputFile);

            LayerManager.Init(SlicerInfoSettings.LayerCount);


            progress.Reset(OperationProgress.StatusDecodeLayers, LayerCount);

            var range = Enumerable.Range(0, (int)LayerCount);

            var linesBytes = new byte[LayerCount][];
            foreach (var batch in BatchLayersIndexes())
            {
                progress.Token.ThrowIfCancellationRequested();

                foreach (var layerIndex in batch)
                {
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
                            var startY = BitExtensions.ToUShortBigEndian(linesBytes[layerIndex][i++], linesBytes[layerIndex][i++]);
                            var endY = BitExtensions.ToUShortBigEndian(linesBytes[layerIndex][i++], linesBytes[layerIndex][i++]);
                            var startX = BitExtensions.ToUShortBigEndian(linesBytes[layerIndex][i++], linesBytes[layerIndex][i]);

                            CvInvoke.Line(mat, new Point(startX, startY), new Point(startX, endY), EmguExtensions.WhiteColor);
                        }

                        linesBytes[layerIndex] = null;

                        this[layerIndex] = new Layer((uint)layerIndex, mat, this);
                    }

                    progress.LockAndIncrement();
                });
            }

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
