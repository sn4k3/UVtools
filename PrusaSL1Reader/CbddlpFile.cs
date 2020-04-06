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
using System.Runtime.InteropServices;

namespace PrusaSL1Reader
{
    public class CbddlpFile : FileFormat
    {
        const uint SPI_FILE_MAGIC_BASE = 0x12FD0000;
        const int SPECIAL_BIT = 1 << 1;
        const int SPECIAL_BIT_MASK = ~SPECIAL_BIT;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Header
        {
            public uint Magic;
            public uint Version;
            public float BedSizeX;
            public float BedSizeY;
            public float BedSizeZ;
            public uint Unknown1;
            public uint Unknown2;
            public uint Unknown3;
            public float LayerHeightMilimeter;
            public float LayerExposureSeconds;
            public float BottomExposureSeconds;
            public float LayerOffTime;
            public uint BottomLayersCount;
            public uint ResolutionX;
            public uint ResolutionY;
            public uint PreviewOneOffsetAddress;
            public uint LayersDefinitionOffsetAddress;
            public uint LayerCount;
            public uint PreviewTwoOffsetAddress;
            public uint PrintTime;
            public uint ProjectorType;
            public uint PrintParametersOffsetAddress;
            public uint PrintParametersSize;
            public uint AntiAliasLevel;
            public ushort LightPWM;
            public ushort BottomLightPWM;
            public uint Padding1;
            public uint Padding2;
            public uint Padding3;

            public override string ToString()
            {
                return $"{nameof(Magic)}: {Magic}, {nameof(Version)}: {Version}, {nameof(BedSizeX)}: {BedSizeX}, {nameof(BedSizeY)}: {BedSizeY}, {nameof(BedSizeZ)}: {BedSizeZ}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(LayerHeightMilimeter)}: {LayerHeightMilimeter}, {nameof(LayerExposureSeconds)}: {LayerExposureSeconds}, {nameof(BottomExposureSeconds)}: {BottomExposureSeconds}, {nameof(LayerOffTime)}: {LayerOffTime}, {nameof(BottomLayersCount)}: {BottomLayersCount}, {nameof(ResolutionX)}: {ResolutionX}, {nameof(ResolutionY)}: {ResolutionY}, {nameof(PreviewOneOffsetAddress)}: {PreviewOneOffsetAddress}, {nameof(LayersDefinitionOffsetAddress)}: {LayersDefinitionOffsetAddress}, {nameof(LayerCount)}: {LayerCount}, {nameof(PreviewTwoOffsetAddress)}: {PreviewTwoOffsetAddress}, {nameof(PrintTime)}: {PrintTime}, {nameof(ProjectorType)}: {ProjectorType}, {nameof(PrintParametersOffsetAddress)}: {PrintParametersOffsetAddress}, {nameof(PrintParametersSize)}: {PrintParametersSize}, {nameof(AntiAliasLevel)}: {AntiAliasLevel}, {nameof(LightPWM)}: {LightPWM}, {nameof(BottomLightPWM)}: {BottomLightPWM}, {nameof(Padding1)}: {Padding1}, {nameof(Padding2)}: {Padding2}, {nameof(Padding3)}: {Padding3}";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PrintParameters
        {
            public float BottomLiftHeight;
            public float BottomLiftSpeed;
            public float LiftHeight;
            public float LiftingSpeed;
            public float RetractSpeed;
            public float VolumeMl;
            public float WeightG;
            public float CostDollars;
            public float BottomLightOffDelay;
            public float LightOffDelay;
            public uint BottomLayerCount;
            public uint P1;
            public uint P2;
            public uint P3;
            public uint P4;

            public override string ToString()
            {
                return $"{nameof(BottomLiftHeight)}: {BottomLiftHeight}, {nameof(BottomLiftSpeed)}: {BottomLiftSpeed}, {nameof(LiftHeight)}: {LiftHeight}, {nameof(LiftingSpeed)}: {LiftingSpeed}, {nameof(RetractSpeed)}: {RetractSpeed}, {nameof(VolumeMl)}: {VolumeMl}, {nameof(WeightG)}: {WeightG}, {nameof(CostDollars)}: {CostDollars}, {nameof(BottomLightOffDelay)}: {BottomLightOffDelay}, {nameof(LightOffDelay)}: {LightOffDelay}, {nameof(BottomLayerCount)}: {BottomLayerCount}, {nameof(P1)}: {P1}, {nameof(P2)}: {P2}, {nameof(P3)}: {P3}, {nameof(P4)}: {P4}";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Layer
        {
            public float LayerPositionZ;
            public float LayerExposure;
            public float LayerOffTimeSeconds;
            public uint DataAddress;
            public uint DataSize;
            public uint Unknown1;
            public uint Unknown2;
            public uint Unknown3;
            public uint Unknown4;

            public override string ToString()
            {
                return $"{nameof(LayerPositionZ)}: {LayerPositionZ}, {nameof(LayerExposure)}: {LayerExposure}, {nameof(LayerOffTimeSeconds)}: {LayerOffTimeSeconds}, {nameof(DataAddress)}: {DataAddress}, {nameof(DataSize)}: {DataSize}, {nameof(Unknown1)}: {Unknown1}, {nameof(Unknown2)}: {Unknown2}, {nameof(Unknown3)}: {Unknown3}, {nameof(Unknown4)}: {Unknown4}";
            }
        }

        public Header HeaderSettings { get; private set; }
        public PrintParameters PrintParametersSettings { get; private set; }

        public List<Layer> Layers { get; } = new List<Layer>();


        public override string FileExtension { get; } = "cbddlp";
        public override string FileExtensionName { get; } = "Chitubox DLP Files";
        public override void Load(string fileFullPath)
        {
            FileValidation(fileFullPath);
            FileFullPath = fileFullPath;

            Layers.Clear();

            BinaryReader binReader = new BinaryReader(File.Open(FileFullPath, FileMode.Open));
            
            HeaderSettings = Helpers.ByteToType<CbddlpFile.Header>(binReader);
            if (HeaderSettings.Magic != (SPI_FILE_MAGIC_BASE | 0x19))
            {
                throw new FileLoadException("Not a valid CBDDLP file!", fileFullPath);
            }

            Debug.WriteLine(HeaderSettings);

            if (HeaderSettings.Version == 2)
            {
                binReader.BaseStream.Seek(HeaderSettings.PrintParametersOffsetAddress, SeekOrigin.Begin);
                PrintParametersSettings = Helpers.ByteToType<CbddlpFile.PrintParameters>(binReader);
                Debug.WriteLine(PrintParametersSettings);
            }

            uint aaLevel = HeaderSettings.Version == 1 ? 1 : HeaderSettings.AntiAliasLevel;

            uint layerOffset = HeaderSettings.LayersDefinitionOffsetAddress;

            for (int image = 1; image <= HeaderSettings.AntiAliasLevel; image++)
            {
                if (HeaderSettings.AntiAliasLevel > 1) Debug.WriteLine("Image GROUP " + image + "----");
                for (int i = 0; i < HeaderSettings.LayerCount; i++)
                {
                    binReader.BaseStream.Seek(layerOffset, SeekOrigin.Begin);
                    Layer layer = Helpers.ByteToType<CbddlpFile.Layer>(binReader);
                    Layers.Add(layer);

                    layerOffset += (uint)Marshal.SizeOf((object)layer);
                    Debug.WriteLine("LAYER " + i);
                    Debug.WriteLine(layer);
                }
            }

            binReader.Close();
            binReader.Dispose();




        }

        public override float GetHeightFromLayer(uint layerNum)
        {
            throw new NotImplementedException();
        }
    }
}
