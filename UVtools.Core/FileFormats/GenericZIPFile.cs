/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.FileFormats;

#region Sub Classes

[XmlRoot(ElementName = "Manifest")]
public class GenericZipManifest
{
    public string CreatedBy { get; set; } = About.SoftwareWithVersion;
        
    public string UpdatedBy { get; set; } = About.SoftwareWithVersion;

    public string CreatedDate { get; set; } = DateTime.UtcNow.ToString("u");

    public string LastModifiedDate { get; set; } = DateTime.UtcNow.ToString("u");

    public float LayerHeight { get; set; }

    public ushort ResolutionX { get; set; }

    public ushort ResolutionY { get; set; }

    public float DisplayWidth { get; set; }

    public float DisplayHeight { get; set; }

    public float MachineZ { get; set; }

    public void Update()
    {
        UpdatedBy = About.SoftwareWithVersion;
        LastModifiedDate = DateTime.UtcNow.ToString("u");
    }
}
#endregion

public sealed class GenericZIPFile : FileFormat
{
    #region Constants
    private const string ManifestFileName = "manifest.uvtools";
    #endregion

    #region Properties
    public GenericZipManifest ManifestFile { get; set; } = new ();

    public override FileFormatType FileType => FileFormatType.Archive;

    public override FileExtension[] FileExtensions { get; } =
    [
        new(typeof(GenericZIPFile), "zip", "Generic Zip / Phrozen Zip")
    ];

    public override uint ResolutionX
    {
        get => ManifestFile.ResolutionX;
        set => base.ResolutionX = ManifestFile.ResolutionX = (ushort) value;
    }

    public override uint ResolutionY
    {
        get => ManifestFile.ResolutionY;
        set => base.ResolutionY = ManifestFile.ResolutionY = (ushort)value;
    }

    public override float DisplayWidth
    {
        get => ManifestFile.DisplayWidth;
        set => base.DisplayWidth = ManifestFile.DisplayWidth = RoundDisplaySize(value);
    }

    public override float DisplayHeight
    {
        get => ManifestFile.DisplayHeight;
        set => base.DisplayHeight = ManifestFile.DisplayHeight = RoundDisplaySize(value);
    }

    public override float MachineZ
    {
        get => ManifestFile.MachineZ > 0 ? ManifestFile.MachineZ : base.MachineZ;
        set => base.MachineZ = ManifestFile.MachineZ = value;
    }

    public override float LayerHeight
    {
        get => ManifestFile.LayerHeight;
        set => base.LayerHeight = ManifestFile.LayerHeight = Layer.RoundHeight(value);
    }

    /*public override uint LayerCount
    {
        get => base.LayerCount;
        set => base.LayerCount = ManifestFile.Slices.LayerCount = base.LayerCount;
    }*/

    public override Size[] ThumbnailsOriginalSize { get; } =
    [
        new(854, 480),
        new(472, 240)
    ];


    public override object[] Configs =>
    [
        ManifestFile
    ];

    #endregion

    #region Constructor
    public GenericZIPFile()
    { }
    #endregion

    #region Methods

    public override bool CanProcess(string? fileFullPath)
    {
        if(!base.CanProcess(fileFullPath)) return false;

        try
        {
            using var zip = ZipFile.Open(fileFullPath!, ZipArchiveMode.Read);
            
            foreach (var entry in zip.Entries)
            {
                if (entry.Name == ManifestFileName) return true;
                if (entry.Name.EndsWith(".gcode")) return false;
                if (entry.Name.EndsWith(".xml")) return false;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return false;
        }
            

        return false;
    }

    protected override void OnBeforeEncode(bool isPartialEncode)
    {
        ManifestFile.Update();
    }

    protected override void EncodeInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Create);

        EncodeThumbnailsInZip(outputFile, progress, ChituboxZipFile.ThumbnailsEntryNames);
        EncodeLayersInZip(outputFile, IndexStartNumber.One, progress);

        using var streamManifest = outputFile.CreateEntryStream(ManifestFileName);
        XmlExtensions.Serialize(ManifestFile, streamManifest, XmlExtensions.SettingsIndent, true);
    }

    protected override void DecodeInternally(OperationProgress progress)
    {
        using var inputFile = ZipFile.Open(FileFullPath!, ZipArchiveMode.Read);
        var entry = inputFile.GetEntry(ManifestFileName);
        if (entry is not null)
        {
            try
            {
                using var stream = entry.Open();
                ManifestFile = XmlExtensions.DeserializeFromStream<GenericZipManifest>(stream);
            }
            catch (Exception)
            {
                // Not required
                //Clear();
                //throw new FileLoadException($"Unable to deserialize '{entry.Name}'\n{e}", FileFullPath);
            }
        }

        uint layerCount = 0;
        foreach (var zipEntry in inputFile.Entries)
        {
            if (!zipEntry.Name.EndsWith(".png")) continue;
            var filename = Path.GetFileNameWithoutExtension(zipEntry.Name);
            if (!filename.All(char.IsDigit)) continue;
            if (!uint.TryParse(filename, out var layerIndex)) continue;
            layerCount = Math.Max(layerCount, layerIndex);
        }

        if (layerCount == 0)
        {
            Clear();
            throw new FileLoadException("Unable to detect layer images in the file", FileFullPath);
        }

        DecodeThumbnailsFromZip(inputFile, progress, ChituboxZipFile.ThumbnailsEntryNames);

        Init(layerCount, DecodeType == FileDecodeType.Partial);
        DecodeLayersFromZip(inputFile, IndexStartNumber.One, progress);
    }

    protected override void PartialSaveInternally(OperationProgress progress)
    {
        using var outputFile = ZipFile.Open(TemporaryOutputFileFullPath, ZipArchiveMode.Update);

        using var stream = outputFile.GetOrCreateStream(ManifestFileName);
        stream.SetLength(0);
        XmlExtensions.Serialize(ManifestFile, stream, XmlExtensions.SettingsIndent, true);
    }
    #endregion
}