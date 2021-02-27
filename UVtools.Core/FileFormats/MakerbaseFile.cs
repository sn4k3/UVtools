/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

// https://github.com/cbiffle/catibo/blob/master/doc/cbddlp-ctb.adoc

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using BinarySerialization;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats
{
    public class MakerbaseFile : FileFormat
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
            public const string TagValue = "MKSDLP";
            //[FieldOrder(0)]  public uint Offset1     { get; set; }

            /// <summary>
            /// Gets the file tag = MKSDLP
            /// </summary>
            //[SerializeAs(SerializedType.TerminatedString)]
            [FieldOrder(0)] [FieldOffset(4)] [FieldLength(6)] public string Tag { get; set; } = TagValue;

            // 290 * 290 * 2 + 116 * 116 * 2 + 4 + 1
            [FieldOrder(1)] [FieldLength(195116)] public byte[] PreviewData { get; set; }

            [FieldOrder(2)] public ushort MaxSize { get; set; }
            [FieldOrder(3)] public ushort ResolutionX { get; set; }
            [FieldOrder(4)] public ushort ResolutionY { get; set; }

            
        }
        #endregion

        #endregion

        #region Properties

        public Header HeaderSettings { get; protected internal set; } = new Header();
        public override FileFormatType FileType => FileFormatType.Binary;

        public override FileExtension[] FileExtensions { get; } = {
            new ("mdlp", "Makerbase MDLP Files"),
            new ("gr1", "Workshop GR1 Files"),
        };

        public override PrintParameterModifier[] PrintParameterModifiers { get; } =
        {
            PrintParameterModifier.BottomLayerCount,
            PrintParameterModifier.BottomExposureSeconds,
            PrintParameterModifier.ExposureSeconds,

            PrintParameterModifier.BottomLightOffDelay,
            PrintParameterModifier.LightOffDelay,
            PrintParameterModifier.BottomLiftHeight,
            PrintParameterModifier.BottomLiftSpeed,
            PrintParameterModifier.LiftHeight,
            PrintParameterModifier.LiftSpeed,
            PrintParameterModifier.RetractSpeed,

            PrintParameterModifier.BottomLightPWM,
            PrintParameterModifier.LightPWM,
        };

        public override byte ThumbnailsCount { get; } = 0;

        public override Size[] ThumbnailsOriginalSize { get; } = {new (290, 290), new (116, 116) };

        public override uint ResolutionX
        {
            get => 0;
            set
            {
            }
        }

        public override uint ResolutionY
        {
            get => 0;
            set { }
        }

        public override float DisplayWidth { get; set; }
        public override float DisplayHeight { get; set; }
        public override bool MirrorDisplay { get; set; }

        public override byte AntiAliasing
        {
            get => 1;
            set { }
        }

        public override float LayerHeight
        {
            get => 0;
            set { }
        }

        public override uint LayerCount
        {
            set
            {
                
                /*HeaderSettings.LayerCount = LayerCount;
                HeaderSettings.OverallHeightMilimeter = TotalHeight;*/
            }
        }

        public override ushort BottomLayerCount => 0;

        public override float BottomExposureTime => 0;

        public override float ExposureTime => 0;
        public override float LiftHeight => 0;
        public override float LiftSpeed => 0;
        public override float RetractSpeed => 0;

        public override float PrintTime => 0;

        public override float MaterialMilliliters => 0;

        public override float MaterialCost => 0;

        public override string MaterialName => "Unknown";
        public override string MachineName => null;
        
        public override object[] Configs => new[] { (object)HeaderSettings };

        #endregion

        #region Constructors

        #endregion

        #region Methods
        protected override void EncodeInternally(string fileFullPath, OperationProgress progress)
        {
            uint currentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
            using (var outputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write))
            {

                outputFile.Seek((int) currentOffset, SeekOrigin.Begin);


                
            }

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
            if (HeaderSettings.Tag != Header.TagValue)
            {
                throw new FileLoadException("Not a valid Makerbase file!", fileFullPath);
            }

            /*var rng = 290 * 290 * 2 + 116 * 116 * 2 + 4;
            byte[] buffer = new byte[rng];
            inputFile.Read(buffer, 0, rng);*/
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

            /*using (var outputFile = new FileStream(FileFullPath, FileMode.Open, FileAccess.Write))
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
            }*/

            //Decode(FileFullPath, progress);
        }

        #endregion
    }
}
