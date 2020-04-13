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
using System.IO;
using BinarySerialization;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    public class CbddlpFile : FileFormat
    {
        #region Constants
        private const uint SPI_FILE_MAGIC_BASE = 0x12FD0019;
        private const int SPECIAL_BIT = 1 << 1;
        private const int SPECIAL_BIT_MASK = ~SPECIAL_BIT;
        private const ushort REPEATRGB15MASK = (1 << 5);

        private const byte RLE8EncodingLimit = 125;
        private const ushort RLE16EncodingLimit = 0x1000;
        #endregion

        #region Sub Classes
        #region Header
        public class Header
        {

            [FieldOrder(0)]  public uint Magic     { get; set; }      // 00
            [FieldOrder(1)]  public uint Version   { get; set; }    // 04
            [FieldOrder(2)]  public float BedSizeX { get; set; }  // 08
            [FieldOrder(3)]  public float BedSizeY { get; set; }  // 12
            [FieldOrder(4)]  public float BedSizeZ { get; set; }  // 20
            [FieldOrder(5)]  public uint Unknown1  { get; set; }
            [FieldOrder(6)]  public uint Unknown2  { get; set; }
            [FieldOrder(7)]  public uint Unknown3  { get; set; }
            [FieldOrder(8)]  public float LayerHeightMilimeter  { get; set; }  // 20
            [FieldOrder(9)]  public float LayerExposureSeconds  { get; set; }  // 24: Layer exposure (in seconds)
            [FieldOrder(10)] public float BottomExposureSeconds { get; set; } // 28: Bottom layers exporsure (in seconds)
            [FieldOrder(11)] public float LayerOffTime     { get; set; }          // 2c: Layer off time (in seconds)
            [FieldOrder(12)] public uint BottomLayersCount { get; set; }      // 30: Number of bottom layers
            [FieldOrder(13)] public uint ResolutionX       { get; set; }            // 34:
            [FieldOrder(14)] public uint ResolutionY       { get; set; }            // 38:
            [FieldOrder(15)] public uint PreviewOneOffsetAddress { get; set; }// 3c: Offset of the high-res preview
            [FieldOrder(16)] public uint LayersDefinitionOffsetAddress { get; set; }// 40: Offset of the layer definitions
            [FieldOrder(17)] public uint LayerCount { get; set; }             // 44:
            [FieldOrder(18)] public uint PreviewTwoOffsetAddress { get; set; }// 48: Offset of the low-rew preview
            [FieldOrder(19)] public uint PrintTime { get; set; }              // 4c: In seconds
            [FieldOrder(20)] public uint ProjectorType { get; set; }          // 0 = CAST, 1 = LCD_X_MIRROR
            [FieldOrder(21)] public uint PrintParametersOffsetAddress { get; set; }   // 54:
            [FieldOrder(22)] public uint PrintParametersSize { get; set; }    // 58:
            [FieldOrder(23)] public uint AntiAliasLevel   { get; set; }         // 5c:
            [FieldOrder(24)] public ushort LightPWM       { get; set; }             // 60:
            [FieldOrder(25)] public ushort BottomLightPWM { get; set; }       // 62:
            [FieldOrder(26)] public uint Padding1 { get; set; }
            [FieldOrder(27)] public uint Padding2 { get; set; }
            [FieldOrder(28)] public uint Padding3 { get; set; }

            public override string ToString()
            {
                return $"{nameof(Magic)}: {Magic}, {nameof(Version)}: {Version}, {nameof(BedSizeX)}: {BedSizeX}, {nameof(BedSizeY)}: {BedSizeY}, {nameof(BedSizeZ)}: {BedSizeZ}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(LayerHeightMilimeter)}: {LayerHeightMilimeter}, {nameof(LayerExposureSeconds)}: {LayerExposureSeconds}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(LayerOffTime)}: {LayerOffTime}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(PreviewOneOffsetAddress)}: {PreviewOneOffsetAddress}, {nameof(LayersDefinitionOffsetAddress)}: {LayersDefinitionOffsetAddress}, {nameof(LayerCount)}: {LayerCount}, {nameof(PreviewTwoOffsetAddress)}: {PreviewTwoOffsetAddress}, {nameof(PrintTime)}: {PrintTime}, {nameof(ProjectorType)}: {ProjectorType}, {nameof(PrintParametersOffsetAddress)}: {PrintParametersOffsetAddress}, {nameof(PrintParametersSize)}: {PrintParametersSize}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(LightPWM)}: {LightPWM}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(Padding3)}: {Padding3}";
            }
        }
        #endregion

        #region PrintParameters
        public class PrintParameters
        {
            [FieldOrder(0)]  public float BottomLiftHeight    { get; set; }
            [FieldOrder(1)]  public float BottomLiftSpeed     { get; set; }
            [FieldOrder(2)]  public float LiftHeight          { get; set; }
            [FieldOrder(3)]  public float LiftingSpeed        { get; set; }
            [FieldOrder(4)]  public float RetractSpeed        { get; set; }
            [FieldOrder(5)]  public float VolumeMl            { get; set; }
            [FieldOrder(6)]  public float WeightG             { get; set; }
            [FieldOrder(7)]  public float CostDollars         { get; set; }
            [FieldOrder(8)]  public float BottomLightOffDelay { get; set; }
            [FieldOrder(9)]  public float LightOffDelay       { get; set; }
            [FieldOrder(10)] public uint BottomLayerCount     { get; set; }
            [FieldOrder(11)] public uint P1                   { get; set; }
            [FieldOrder(12)] public uint P2                   { get; set; }
            [FieldOrder(13)] public uint P3                   { get; set; }
            [FieldOrder(14)] public uint P4                   { get; set; }

            public override string ToString()
            {
                return $"{nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftingSpeed)}: {LiftingSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(VolumeMl)}: {VolumeMl}, {nameof(WeightG)}: {WeightG}, {nameof(CostDollars)}: {CostDollars}, {nameof(BottomLightOffDelay)}: {BottomLightOffDelay}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(P1)}: {P1}, {nameof(P2)}: {P2}, {nameof(P3)}: {P3}, {nameof(P4)}: {P4}";
            }
        }
        #endregion

        #region MachineInfo

        public class MachineInfo
        {
            [FieldOrder(0)] public uint d1                 { get; set; }
            [FieldOrder(1)] public uint d2                 { get; set; }
            [FieldOrder(2)] public uint d3                 { get; set; }
            [FieldOrder(3)] public uint d4                 { get; set; }
            [FieldOrder(4)] public uint d5                 { get; set; }
            [FieldOrder(5)] public uint d6                 { get; set; }
            [FieldOrder(6)] public uint d7                 { get; set; }
            [FieldOrder(7)] public uint MachineNameAddress { get; set; }
            [FieldOrder(8)] public uint MachineNameSize    { get; set; }
            [FieldOrder(9)] public uint d8                 { get; set; }
            [FieldOrder(10)] public uint d9                { get; set; }
            [FieldOrder(11)] public uint d10               { get; set; }
            [FieldOrder(12)] public uint d11               { get; set; }
            [FieldOrder(13)] public uint d12               { get; set; }
            [FieldOrder(14)] public uint d13               { get; set; }
            [FieldOrder(15)] public uint d14               { get; set; }
            [FieldOrder(16)] public uint d15               { get; set; }
            [FieldOrder(17)] public uint d16               { get; set; }
            [FieldOrder(18)] public uint d17               { get; set; }
            
            [FieldOrder(19)] [FieldLength(nameof(MachineNameSize))]
            public string MachineName               { get; set; }

            public override string ToString()
            {
                return $"{nameof(d1)}: {d1}, {nameof(d2)}: {d2}, {nameof(d3)}: {d3}, {nameof(d4)}: {d4}, {nameof(d5)}: {d5}, {nameof(d6)}: {d6}, {nameof(d7)}: {d7}, {nameof(MachineNameAddress)}: {MachineNameAddress}, {nameof(MachineNameSize)}: {MachineNameSize}, {nameof(d8)}: {d8}, {nameof(d9)}: {d9}, {nameof(d10)}: {d10}, {nameof(d11)}: {d11}, {nameof(d12)}: {d12}, {nameof(d13)}: {d13}, {nameof(d14)}: {d14}, {nameof(d15)}: {d15}, {nameof(d16)}: {d16}, {nameof(d17)}: {d17}, {nameof(MachineName)}: {MachineName}";
            }
        }

        #endregion

        #region Preview
        public class Preview
        {
            [FieldOrder(0)] public uint ResolutionX { get; set; }
            [FieldOrder(1)] public uint ResolutionY { get; set; }
            [FieldOrder(2)] public uint ImageOffset { get; set; }
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
        public class Layer
        {
            [FieldOrder(0)] public float LayerPositionZ      { get; set; }
            [FieldOrder(1)] public float LayerExposure       { get; set; }
            [FieldOrder(2)] public float LayerOffTimeSeconds { get; set; }
            [FieldOrder(3)] public uint DataAddress          { get; set; }
            [FieldOrder(4)] public uint DataSize             { get; set; }
            [FieldOrder(5)] public uint Unknown1             { get; set; }
            [FieldOrder(6)] public uint Unknown2             { get; set; }
            [FieldOrder(7)] public uint Unknown3             { get; set; }
            [FieldOrder(8)] public uint Unknown4             { get; set; }

            public override string ToString()
            {
                return $"{nameof(LayerPositionZ)}: {LayerPositionZ}, {nameof(LayerExposure)}: {LayerExposure}, {nameof(LayerOffTimeSeconds)}: {LayerOffTimeSeconds}, {nameof(DataAddress)}: {DataAddress}, {nameof(DataSize)}: {DataSize}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(Unknown4)}: {Unknown4}";
            }
        }
        #endregion

        #endregion

        #region Properties

        public FileStream InputFile { get; private set; }
        public FileStream OutputFile { get; private set; }
        public Header HeaderSettings { get; protected internal set; }
        public PrintParameters PrintParametersSettings { get; protected internal set; }

        public MachineInfo MachineInfoSettings { get; protected internal set; }

        public Preview[] Previews { get; protected internal set; }

        public Layer[,] Layers { get; private set; }

        private uint CurrentOffset { get; set; }
        private uint LayerDataCurrentOffset { get; set; }

        #endregion

        #region Constructors
        public CbddlpFile()
        {
            Previews = new Preview[ThumbnailsCount];
        }
        #endregion

        #region Overrides

        public override string FileFullPath { get; set; }

        public override FileExtension[] ValidFiles { get; } = {
            new FileExtension("cbddlp", "Chitubox DLP Files"), 
            new FileExtension("photon", "Photon Files"), 
        };

        public override uint ResolutionX => HeaderSettings.ResolutionX;

        public override uint ResolutionY => HeaderSettings.ResolutionY;


        public override byte ThumbnailsCount { get; } = 2;
        public override Image<Rgba32>[] Thumbnails { get; set; }
        public override uint LayerCount => HeaderSettings.LayerCount;
        public override float InitialExposureTime => HeaderSettings.BottomExposureSeconds;
        public override float LayerExposureTime => HeaderSettings.LayerExposureSeconds;
        public override float PrintTime => HeaderSettings.PrintTime;
        public override float UsedMaterial => PrintParametersSettings.VolumeMl;

        public override float MaterialCost => PrintParametersSettings.CostDollars;

        public override string MaterialName => "Unknown";
        public override string MachineName => MachineInfoSettings.MachineName;
        public override float LayerHeight => HeaderSettings.LayerHeightMilimeter;

        public override object[] Configs => new[] { (object)HeaderSettings, PrintParametersSettings, MachineInfoSettings };

        public override void BeginEncode(string fileFullPath)
        {
            CurrentOffset = (uint)Helpers.Serializer.SizeOf(HeaderSettings);
            Layers = new Layer[HeaderSettings.LayerCount, HeaderSettings.AntiAliasLevel];
            OutputFile = new FileStream(fileFullPath, FileMode.Create, FileAccess.Write);

            HeaderSettings.Magic = SPI_FILE_MAGIC_BASE;
            //HeaderSettings.PreviewOneOffsetAddress = CurrentOffset;
            HeaderSettings.PrintParametersSize = (uint)Helpers.Serializer.SizeOf(PrintParametersSettings);

            //CurrentOffset = Helpers.SerializeWriteFileStream(OutputFile, HeaderSettings);

            OutputFile.Seek((int)CurrentOffset, SeekOrigin.Begin);

            /*for (int i = 0; i < ThumbnailsCount; i++)
            {
                if (i == 1)
                {
                    HeaderSettings.PreviewTwoOffsetAddress = CurrentOffset;
                }
                var image = Thumbnails[i];
                Preview preview = new Preview {ResolutionX = (uint)image.Width, ResolutionY = (uint)image.Height};
                List<byte> rawData = new List<byte>();

                Rgba32 color;
                byte nrOfColor = 0;
                Rgba32? prevColor = null;

                for (int y = 0; y < image.Height; y++)
                {
                    Span<Rgba32> pixelRowSpan = image.GetPixelRowSpan(y);
                    for (int x = 0; x < image.Width; x++)
                    {
                        color = pixelRowSpan[x];
                        if (prevColor == null) prevColor = color;
                        bool isLastPixel = x == (image.Width - 1) && y == (image.Height - 1);
                        if (color == prevColor && nrOfColor < 0x0FFF && !isLastPixel)
                        {
                            nrOfColor++;
                        }
                        else
                        {

                            byte R = prevColor.Value.R;
                            byte G = prevColor.Value.G;
                            byte B = prevColor.Value.B;
                            byte X = nrOfColor > 1 ? (byte)1 : (byte)0;

 
                            // build 2 or 4 bytes (depending on X
                            // The color (R,G,B) of a pixel spans 2 bytes (little endian) and
                            // each color component is 5 bits: RRRRR GGG GG X BBBBB
                            R = (byte)Math.Round(R / 255f * 31f);
                            G = (byte)Math.Round(G / 255f * 31f);
                            B = (byte)Math.Round(B / 255f * 31f);

                            byte encValue0 = (byte)(R << 3 | G >> 2);
                            byte encValue1 = (byte) (((G & 0b00000011) << 6) | X << 5 | B);
                            rawData.Add(encValue0);
                            rawData.Add(encValue1);
                            if (X == 1)
                            {
                                nrOfColor--;
                                // write one less than nr of pixels
                                byte encValue2 = (byte)(nrOfColor >> 8);
                                byte encValue3 = (byte)(nrOfColor & 0b000000011111111);
                                // seems like nr bytes pixels have 0011 as start
                                encValue2 = (byte)(encValue2 | 0b00110000);
                                rawData.Add(encValue2);
                                rawData.Add(encValue3);
                            }

                            prevColor = color;
                            nrOfColor = 1;
                        }
                    }
                }


                preview.ImageLength = (uint)rawData.Count;
                CurrentOffset += (uint)Helpers.Serializer.SizeOf(preview);
                preview.ImageOffset = CurrentOffset;

                Previews[i] = preview;

                Helpers.SerializeWriteFileStream(OutputFile, preview);
                CurrentOffset += Helpers.SerializeWriteFileStream(OutputFile, rawData.ToArray());
            }*/


            if (HeaderSettings.Version == 2)
            {
                HeaderSettings.PrintParametersOffsetAddress = CurrentOffset;

                CurrentOffset += Helpers.SerializeWriteFileStream(OutputFile, PrintParametersSettings);

                MachineInfoSettings.MachineNameAddress = (uint)(CurrentOffset + Helpers.Serializer.SizeOf(MachineInfoSettings) -
                                                                MachineInfoSettings.MachineNameSize);


                CurrentOffset += Helpers.SerializeWriteFileStream(OutputFile, MachineInfoSettings);
            }

            HeaderSettings.LayersDefinitionOffsetAddress = CurrentOffset;
            LayerDataCurrentOffset = CurrentOffset + (uint)Helpers.Serializer.SizeOf(new Layer()) * HeaderSettings.LayerCount * HeaderSettings.AntiAliasLevel;
        }


        public override void InsertLayerImageEncode(Image<Gray8> image, uint layerIndex)
        {
            Layer layer = new Layer();
            List<byte> rawData = new List<byte>();
            
            byte color = 0;
            byte black = 0;
            byte white = 1;

            byte nrOfColor = 0;
            byte prevColor = byte.MaxValue;

            for (int y = 0; y < image.Height; y++)
            {
                Span<Gray8> pixelRowSpan = image.GetPixelRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    color = pixelRowSpan[x].PackedValue < 128 ? black : white;
                    if (prevColor == byte.MaxValue) prevColor = color;
                    bool isLastPixel = x == (image.Width - 1) && y == (image.Height - 1);

                    if (color == prevColor && nrOfColor < 0x7D && !isLastPixel)
                    {
                        nrOfColor++;
                    }
                    else
                    {
                        byte encValue =
                            (byte) ((prevColor << 7) |
                                    nrOfColor); // push color (B/W) to highest bit and repetitions to lowest 7 bits.
                        rawData.Add(encValue);
                        prevColor = color;
                        nrOfColor = 1;
                    }
                }
            }


            //layer.DataAddress = CurrentOffset + (uint)Helpers.Serializer.SizeOf(layer);
            layer.DataAddress = LayerDataCurrentOffset;
            layer.DataSize = (uint)rawData.Count;
            layer.LayerPositionZ = layerIndex * HeaderSettings.LayerHeightMilimeter;
            layer.LayerOffTimeSeconds = layerIndex < HeaderSettings.BottomLayersCount ? PrintParametersSettings.BottomLightOffDelay : PrintParametersSettings.LightOffDelay;
            layer.LayerExposure = layerIndex < HeaderSettings.BottomLayersCount ? HeaderSettings.BottomExposureSeconds : HeaderSettings.LayerExposureSeconds;
            Layers[layerIndex, 0] = layer;

            CurrentOffset += Helpers.SerializeWriteFileStream(OutputFile, layer);

            OutputFile.Seek(LayerDataCurrentOffset, SeekOrigin.Begin);
            LayerDataCurrentOffset += Helpers.WriteFileStream(OutputFile, rawData.ToArray());
            OutputFile.Seek(CurrentOffset, SeekOrigin.Begin);
        }

        public override void EndEncode()
        {

            OutputFile.Seek(0, SeekOrigin.Begin);
            Helpers.SerializeWriteFileStream(OutputFile, HeaderSettings);

            OutputFile.Close();
            OutputFile.Dispose();

            Debug.WriteLine("Encode Results:");
            Debug.WriteLine(HeaderSettings);
            Debug.WriteLine(Previews[0]);
            Debug.WriteLine(Previews[1]);
            Debug.WriteLine(PrintParametersSettings);
            Debug.WriteLine(MachineInfoSettings);
            Debug.WriteLine("-End-");
        }

        public override void Decode(string fileFullPath)
        {
            base.Decode(fileFullPath);
            
            InputFile = new FileStream(fileFullPath, FileMode.Open, FileAccess.Read);

            //HeaderSettings = Helpers.ByteToType<CbddlpFile.Header>(InputFile);
            //HeaderSettings = Helpers.Serializer.Deserialize<Header>(InputFile.ReadBytes(Helpers.Serializer.SizeOf(typeof(Header))));
            HeaderSettings = Helpers.Deserialize<Header>(InputFile);
            if (HeaderSettings.Magic != SPI_FILE_MAGIC_BASE)
            {
                throw new FileLoadException("Not a valid CBDDLP file!", fileFullPath);
            }

            if (HeaderSettings.Version == 1 || HeaderSettings.AntiAliasLevel == 0)
            {
                HeaderSettings.AntiAliasLevel = 1;
            }

            FileFullPath = fileFullPath;



            Debug.Write("Header -> ");
            Debug.WriteLine(HeaderSettings);

            for (byte i = 0; i < ThumbnailsCount; i++)
            {
                uint offsetAddress = i == 0 ? HeaderSettings.PreviewOneOffsetAddress : HeaderSettings.PreviewTwoOffsetAddress;
                if(offsetAddress == 0) continue;

                InputFile.Seek(offsetAddress, SeekOrigin.Begin);
                Previews[i] = Helpers.Deserialize<Preview>(InputFile);

                Debug.Write($"Preview {i} -> ");
                Debug.WriteLine(Previews[i]);

                Thumbnails[i] = new Image<Rgba32>((int)Previews[i].ResolutionX, (int)Previews[i].ResolutionY);

                InputFile.Seek(Previews[i].ImageOffset, SeekOrigin.Begin);
                byte[] rawImageData = new byte[Previews[i].ImageLength];
                InputFile.Read(rawImageData, 0, (int)Previews[i].ImageLength);
                int x = 0;
                int y = 0;
                for (int n = 0; n < Previews[i].ImageLength; n++)
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
                        Thumbnails[i][x, y] = new Rgba32(red, green, blue, 255);
                        x++;

                        if (x == Previews[i].ResolutionX)
                        {
                            x = 0;
                            y++;
                        }
                    }
                }
            }

            if (HeaderSettings.Version == 2 && HeaderSettings.PrintParametersOffsetAddress > 0)
            {
                InputFile.Seek(HeaderSettings.PrintParametersOffsetAddress, SeekOrigin.Begin);
                PrintParametersSettings = Helpers.Deserialize<PrintParameters>(InputFile);
                Debug.Write("Print Parameters -> ");
                Debug.WriteLine(PrintParametersSettings);
                
                MachineInfoSettings = Helpers.Deserialize<MachineInfo>(InputFile);
                Debug.Write("Machine Info -> ");
                Debug.WriteLine(MachineInfoSettings);

                /*InputFile.BaseStream.Seek(MachineInfoSettings.MachineNameAddress, SeekOrigin.Begin);
                byte[] bytes = InputFile.ReadBytes((int)MachineInfoSettings.MachineNameSize);
                MachineName = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.WriteLine($"{nameof(MachineName)}: {MachineName}");*/
            }

            Layers = new Layer[HeaderSettings.LayerCount, HeaderSettings.AntiAliasLevel];

            uint layerOffset = HeaderSettings.LayersDefinitionOffsetAddress;

            Image<Gray8> image = new Image<Gray8>((int)HeaderSettings.BedSizeX, (int)HeaderSettings.BedSizeY);

            
            for (uint aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
            {
                Debug.WriteLine($"-Image GROUP {aaIndex}-");
                for (uint layerIndex = 0; layerIndex < HeaderSettings.LayerCount; layerIndex++)
                {
                    InputFile.Seek(layerOffset, SeekOrigin.Begin);
                    Layer layer = Helpers.Deserialize<Layer>(InputFile);
                    Layers[layerIndex, aaIndex] = layer;
                    //Layers.Add(layer);

                    layerOffset += (uint)Helpers.Serializer.SizeOf(layer);
                    Debug.Write($"LAYER {layerIndex} -> ");
                    Debug.WriteLine(layer);
                }
            }
        }

        public override void Extract(string path, bool emptyFirst = true)
        {
            throw new NotImplementedException();
        }

        public override Image<Gray8> GetLayerImage(uint layerIndex)
        {
            if (layerIndex >= HeaderSettings.LayerCount)
            {
                throw new IndexOutOfRangeException($"Layer {layerIndex} doesn't exists, out of bounds.");
            }

            Image<Gray8> image = new Image<Gray8>((int)HeaderSettings.ResolutionX, (int)HeaderSettings.ResolutionY);
            
            for (uint aaIndex = 0; aaIndex < HeaderSettings.AntiAliasLevel; aaIndex++)
            {
                Layer layer = Layers[layerIndex, aaIndex];
                InputFile.Seek(layer.DataAddress, SeekOrigin.Begin);
                byte[] rawImageData = new byte[(int)layer.DataSize];
                InputFile.Read(rawImageData, 0, (int)layer.DataSize);
                uint x = 0;
                uint y = 0;

                foreach (byte rle in rawImageData)
                {
                    // From each byte retrieve color (highest bit) and number of pixels of that color (lowest 7 bits)
                    uint length = (uint)(rle & 0x7F);    // turn highest bit of
                    bool color = (rle & 0x80) == 0x80;   // only read 1st bit

                    uint x2 = (uint)Math.Min(x + length, HeaderSettings.ResolutionX);

                    if (color)
                    {
                        for (uint i = x; i < x2; i++)
                        {
                            image[(int) i, (int) y] = Helpers.Gray8White;
                        }
                    }

                    x += length;

                    if (x >= HeaderSettings.ResolutionX)
                    {
                        length = x - HeaderSettings.ResolutionX;
                        x = 0;
                        y++;
                        x2 = x + length;

                        if (color)
                        {
                            for (uint i = x; i < x2; i++)
                            {
                                image[(int) i, (int) y] = new Gray8(255);
                            }
                        }

                        x += length;
                    }
                }
            }

            return image;
        }

        public override float GetHeightFromLayer(uint layerNum)
        {
            return HeaderSettings.LayerCount;
        }

        public override void Clear()
        {
            base.Clear();

            for (byte i = 0; i < ThumbnailsCount; i++)
            {
                Previews[i] = new Preview();
            }

            Layers = null;

            if (!ReferenceEquals(InputFile, null))
            {
                InputFile.Close();
                InputFile.Dispose();
            }

            if (!ReferenceEquals(OutputFile, null))
            {
                OutputFile.Close();
                OutputFile.Dispose();
            }
        }

        public override bool Convert(Type to, string fileFullPath)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Methods

        #endregion
    }
}
