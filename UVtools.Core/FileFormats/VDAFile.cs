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
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

#region Sub Classes
[Serializable]
[XmlRoot(ElementName = "root")]
public class VDARoot
{
    [Serializable]
    public class VDAFileInfo
    {
        [Serializable]
        public class VDAVersion
        {
            public ushort Major { get; set; } = 1;
            public ushort Minor { get; set; } = 2;

        }

        [Serializable]
        public class VDAWritten
        {
            [Serializable]
            [XmlRoot(ElementName = "By")]
            public class VDABy
            {
                [XmlAttribute]
                public string ApplicationName { get; set; } = About.Software;

                [XmlAttribute]
                public string ApplicationVersion { get; set; } = About.VersionStr;

                public override string ToString()
                {
                    return $"{ApplicationName} v{ApplicationVersion}";
                }

                public void Reset()
                {
                    ApplicationName = About.Software;
                    ApplicationVersion = About.VersionStr;
                }
            }

            public VDABy By { get; set; } = new();

            public string When { get; set; } = DateTime.UtcNow.ToString("u");

            public void Reset()
            {
                When = DateTime.UtcNow.ToString("u");
                By.Reset();
            }
        }


        public VDAVersion Version { get; set; } = new();

        public VDAWritten Written { get; set; } = new();
    }

    [Serializable]
    public class VDASlices
    {
        public ushort Count { get; set; } = 1;

        [XmlElement("thickness")]
        public float LayerHeight { get; set; }

        [XmlElement("startHeight")]
        public float StartHeight { get; set; }

        [XmlElement("endHeight")]
        public float EndHeight { get; set; }

        [XmlElement("layersCount")]
        public uint LayerCount { get; set; }
    }

    [Serializable]
    public class VDAMachines
    {
        public string FileType { get; set; } = "ZIP File";
        public string Resolution { get; set; } = "1920*1080P";
        public string PixelXSize { get; set; } = "50um";
        public string PixelYSize { get; set; } = "50um";

        [XmlElement("Anti-Aliasing")] 
        public byte AntiAliasing { get; set; } = 1;

        public float XLength { get; set; }
        public float YWidth { get; set; }
        public float ZHeight { get; set; }
    }

    public class VDALayer
    {
        [XmlElement("Index")]
        public uint Index { get; set; }

        [XmlElement("zvalue")]
        public float ZPosition { get; set; }

        [XmlElement("filename")]
        public string? Filename { get; set; }

        public VDALayer()
        {
        }

        public VDALayer(uint index, float zPosition, string? filename)
        {
            Index = index;
            ZPosition = zPosition;
            Filename = filename;
        }
    }


    public VDAFileInfo FileInfo { get; set; } = new();
    public VDASlices Slices { get; set; } = new();
    public VDAMachines Machines { get; set; } = new();
    public List<VDALayer> Layers { get; set; } = new();
}
#endregion

public class VDAFile : FileFormat
{
    #region Constants

    #endregion

    #region Properties
    public VDARoot ManifestFile { get; set; } = new ();

    public override FileFormatType FileType => FileFormatType.Archive;

    public override FileExtension[] FileExtensions { get; } = {
        new(typeof(VDAFile), "zip", "Voxeldance Additive Zip")
    };

    public override uint ResolutionX
    {
        get
        {
            var resolution = ManifestFile.Machines.Resolution.Split('*', StringSplitOptions.TrimEntries);
            if (resolution.Length < 2) return 0;
            uint.TryParse(resolution[0], out var xRes);
            return xRes;
        }
        set
        {
            ManifestFile.Machines.Resolution = $"{value}*{ResolutionY}P";
            RaisePropertyChanged();
        }
    }

    public override uint ResolutionY
    {
        get
        {
            var resolution = ManifestFile.Machines.Resolution.Split('*', StringSplitOptions.TrimEntries);
            if (resolution.Length < 2) return 0;
            resolution[1] = resolution[1].TrimEnd('P');
            uint.TryParse(resolution[1], out var yRes);
            return yRes;
        }
        set
        {
            ManifestFile.Machines.Resolution = $"{ResolutionX}*{value}P";
            RaisePropertyChanged();
        }
    }

    public override float DisplayWidth
    {
        get
        {
            if (ManifestFile.Machines.XLength > 0) return ManifestFile.Machines.XLength;

            var umStr= ManifestFile.Machines.PixelXSize.Replace("um", string.Empty, StringComparison.OrdinalIgnoreCase);

            if (ushort.TryParse(umStr, out var um) && um > 0)
            {
                return (float) Math.Round(ResolutionX * um / 1000f, 2);
            }

            return ManifestFile.Machines.XLength;
        }
        set
        {
            ManifestFile.Machines.XLength = (float) Math.Round(value, 2);
            ManifestFile.Machines.PixelXSize = $"{Math.Round(value / ResolutionX * 1000, 2)}um";
            RaisePropertyChanged();
        }
    }

    public override float DisplayHeight
    {
        get
        {
            if (ManifestFile.Machines.YWidth > 0) return ManifestFile.Machines.YWidth;

            var umStr = ManifestFile.Machines.PixelYSize.Replace("um", string.Empty, StringComparison.OrdinalIgnoreCase);

            if (ushort.TryParse(umStr, out var um) && um > 0)
            {
                return (float)Math.Round(ResolutionY * um / 1000f, 2);
            }

            return ManifestFile.Machines.YWidth;
        }
        set
        {
            ManifestFile.Machines.YWidth = (float)Math.Round(value, 2);
            ManifestFile.Machines.PixelYSize = $"{Math.Round(value / ResolutionY * 1000, 2)}um";
            RaisePropertyChanged();
        }
    }

    public override float MachineZ
    {
        get => ManifestFile.Machines.ZHeight > 0 ? ManifestFile.Machines.ZHeight : base.MachineZ;
        set
        {
            ManifestFile.Machines.ZHeight = value;
            RaisePropertyChanged();
        }
    }

    public override byte AntiAliasing
    {
        get => ManifestFile.Machines.AntiAliasing;
        set => base.AntiAliasing = ManifestFile.Machines.AntiAliasing = value.Clamp(1, 16);
    }

    public override float LayerHeight
    {
        get => ManifestFile.Slices.LayerHeight;
        set
        {
            ManifestFile.Slices.LayerHeight = Layer.RoundHeight(value);
            RaisePropertyChanged();
        }
    }

    public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = ManifestFile.Slices.LayerCount = base.LayerCount;
    }

        
    public override object[] Configs => new object[] { 
        ManifestFile.FileInfo.Version,
        ManifestFile.FileInfo.Written, 
        ManifestFile.Machines, 
        ManifestFile.Slices };

    #endregion

    #region Constructor
    public VDAFile()
    { }
    #endregion

    #region Methods

    public override bool CanProcess(string? fileFullPath)
    {
        if(!base.CanProcess(fileFullPath)) return false;

        try
        {
            using var zip = ZipFile.Open(fileFullPath!, ZipArchiveMode.Read);
            if (zip.Entries.Any(entry => entry.Name.EndsWith(".xml"))) return true;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
            

        return false;
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Create);
        var manifestFilename = Filename!.
            Replace($".{FileExtensions[0].Extension}{TemporaryFileAppend}", ".xml").
            Replace($".{FileExtensions[0].Extension}", ".xml");

        EncodeLayersInZip(outputFile, 4, IndexStartNumber.One, progress);
        
        UpdateManifest();

        var entry = outputFile.CreateEntry(manifestFilename);
        using var stream = entry.Open();
        XmlExtensions.Serialize(ManifestFile, stream, XmlExtensions.SettingsIndent, true);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Read);
        var entry = inputFile.Entries.FirstOrDefault(zipEntry => zipEntry.Name.EndsWith(".xml"));
        if (entry is null)
        {
            Clear();
            throw new FileLoadException($".xml manifest not found", FileFullPath);
        }

        try
        {
            using var stream = entry.Open();
            ManifestFile = XmlExtensions.DeserializeFromStream<VDARoot>(stream);
        }
        catch (Exception e)
        {
            Clear();
            throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
        }


        Init(ManifestFile.Slices.LayerCount, DecodeType == FileDecodeType.Partial);
        DecodeLayersFromZip(inputFile, IndexStartNumber.One, progress);
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Update);
        bool deleted;

        do
        {
            deleted = false;
            foreach (var zipEntry in outputFile.Entries)
            {
                if (!zipEntry.Name.EndsWith(".xml")) continue;
                zipEntry.Delete();
                deleted = true;
                break;
            }
        } while (deleted);

        var manifestFilename = Path.GetFileName(FileFullPath)!.
            Replace($".{FileExtensions[0].Extension}{TemporaryFileAppend}", ".xml").
            Replace($".{FileExtensions[0].Extension}", ".xml");

        UpdateManifest();

        var entry = outputFile.CreateEntry(manifestFilename);
        using var stream = entry.Open();
        XmlExtensions.Serialize(ManifestFile, stream, XmlExtensions.SettingsIndent, true);
    }

    public void UpdateManifest()
    {
        ManifestFile.FileInfo.Written.Reset();
        ManifestFile.Slices.StartHeight = FirstLayer?.PositionZ ?? 0;
        ManifestFile.Slices.EndHeight = LastLayer?.PositionZ ?? 0;
        ManifestFile.Layers.Clear();
        for (uint layerIndex = 0; layerIndex < LayerCount; layerIndex++)
        {
            var layer = this[layerIndex];
            ManifestFile.Layers.Add(new VDARoot.VDALayer(layerIndex, layer.PositionZ, layer.FormatFileName(4, IndexStartNumber.One)));
        }
    }
    #endregion
}