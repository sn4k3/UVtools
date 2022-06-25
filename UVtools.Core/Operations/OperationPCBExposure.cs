/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Gerber;
using UVtools.Core.Layers;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations;

[Serializable]
public class OperationPCBExposure : Operation
{
    #region

    public static string[] ValidExtensions => new[]
    {
        "gbr", // Gerber
        "gko", // Board outline layer
        "gtl", // Top layer
        "gto", // Top silkscreen layer
        "gts", // Top solder mask layer
        "gbl", // Bottom layer
        "gbo", // Bottom silkscreen layer
        "gbs", // Bottom solder mask layer
        "gml", // Mechanical layer
    };
    #endregion

    #region Members
    private RangeObservableCollection<ValueDescription> _files = new();

    private bool _mergeFiles;
    private decimal _layerHeight;
    private decimal _exposureTime;
    private bool _mirror;
    private bool _invertColor;
    private bool _enableAntiAliasing;

    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "fa-solid fa-microchip";
    public override string Title => "PCB exposure";
    public override string Description =>
        "Converts a gerber file to a pixel perfect image given your printer LCD/resolution to exposure the copper traces.\n" +
        "Note: The current opened file will be overwritten with this gerber image, use a dummy or a not needed file.";

    public override string ConfirmationText =>
        "generate the PCB traces?";

    public override string ProgressTitle =>
        "Generating PCB traces";

    public override string ProgressAction => "Tracing";

    public override string? ValidateSpawn()
    {
        if(SlicerFile.DisplayWidth <= 0 || SlicerFile.DisplayHeight <= 0)
        {
            return $"{NotSupportedMessage}\nReason: No display size information is available to calculate the correct pixel pitch, and so, it's unable to produce a pixel perfect image.";
        }

        return null;
    }

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();
        if (_files.Count == 0)
        {
            sb.AppendLine("Select at least one gerber file");
        }
        else 
        {
            foreach (var valueDescription in _files)
            {
                if(!File.Exists(valueDescription.ValueAsString)) sb.AppendLine($"The file {valueDescription} does not exists");
            }
        }
        
        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"{string.Join(" / ", _files)} [Exposure: {_exposureTime}s] [Mirror: {_mirror}] [Invert: {_invertColor}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationPCBExposure() { }

    public OperationPCBExposure(FileFormat slicerFile) : base(slicerFile)
    {
        if (_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if (_exposureTime <= 0) _exposureTime = (decimal)SlicerFile.BottomExposureTime;
        //_mirror = SlicerFile.DisplayMirror != FlipDirection.None;
    }

    #endregion

    #region Properties
    public RangeObservableCollection<ValueDescription> Files
    {
        get => _files;
        set => RaiseAndSetIfChanged(ref _files, value);
    }

    public uint FileCount => (uint)_files.Count;

    public bool MergeFiles
    {
        get => _mergeFiles;
        set => RaiseAndSetIfChanged(ref _mergeFiles, value);
    }

    /*public string? FilePath
    {
        get => _filePath;
        set => RaiseAndSetIfChanged(ref _filePath, value);
    }
    public bool FileExists => !string.IsNullOrWhiteSpace(_filePath) && File.Exists(_filePath);*/

    public decimal LayerHeight
    {
        get => _layerHeight;
        set => RaiseAndSetIfChanged(ref _layerHeight, Layer.RoundHeight(value));
    }

    public decimal ExposureTime
    {
        get => _exposureTime;
        set => RaiseAndSetIfChanged(ref _exposureTime, Math.Round(value, 2));
    }
    
    public bool Mirror
    {
        get => _mirror;
        set => RaiseAndSetIfChanged(ref _mirror, value);
    }

    public bool InvertColor
    {
        get => _invertColor;
        set => RaiseAndSetIfChanged(ref _invertColor, value);
    }

    public bool EnableAntiAliasing
    {
        get => _enableAntiAliasing;
        set => RaiseAndSetIfChanged(ref _enableAntiAliasing, value);
    }

    #endregion

    #region Equality

    protected bool Equals(OperationPCBExposure other)
    {
        return _files.Equals(other._files) && _mergeFiles == other._mergeFiles && _layerHeight == other._layerHeight && _exposureTime == other._exposureTime && _mirror == other._mirror && _invertColor == other._invertColor && _enableAntiAliasing == other._enableAntiAliasing;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationPCBExposure) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_files, _mergeFiles, _layerHeight, _exposureTime, _mirror, _invertColor, _enableAntiAliasing);
    }

    #endregion

    #region Methods

    public void AddFilesFromZip(string zipFile)
    {
        if (!File.Exists(zipFile)) return;
        using var zip = ZipFile.Open(zipFile, ZipArchiveMode.Read);

        var tmpPath = PathExtensions.GetTemporaryDirectory($"{About.Software}.");
        foreach (var entry in zip.Entries)
        {
            if(!ValidExtensions.Any(extension => entry.Name.EndsWith($".{extension}", StringComparison.InvariantCultureIgnoreCase))) continue;

            var filePath = entry.ImprovedExtractToFile(tmpPath, false);
            if (!string.IsNullOrEmpty(filePath))
            {
                AddFile(filePath);
            }
        }

    }

    public void AddFile(string file)
    {
        if (!File.Exists(file)) return;
        var vd = new ValueDescription(file, Path.GetFileNameWithoutExtension(file));
        if (_files.Contains(vd)) return;
        _files.Add(vd);
    }

    public void AddFiles(string[] files)
    {
        foreach (var file in files)
        {
            AddFile(file);
        }
    }

    public void Sort()
    {
        _files.Sort((file1, file2) => string.Compare(Path.GetFileNameWithoutExtension(file1.ValueAsString), Path.GetFileNameWithoutExtension(file2.ValueAsString), StringComparison.Ordinal));
    }

    public Mat GetMat(string file)
    {
        var mat = SlicerFile.CreateMat();
        if (!File.Exists(file)) return mat;
        GerberDocument.ParseAndDraw(file, mat, SlicerFile.Ppmm, _enableAntiAliasing);

        //var boundingRectangle = CvInvoke.BoundingRectangle(mat);
        //var cropped = mat.Roi(new Size(boundingRectangle.Right, boundingRectangle.Bottom));
        var cropped = mat.CropByBounds();

        if (_invertColor) CvInvoke.BitwiseNot(cropped, cropped);
        if (_mirror)
        {
            var flip = SlicerFile.DisplayMirror;
            if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
            CvInvoke.Flip(mat, mat, (FlipType)flip);
        }

        return mat;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var layers = new List<Layer>();
        Mat? mergeMat = null;
        progress.ItemCount = FileCount;
        foreach (var file in _files)
        {
            using var mat = GetMat(file.ValueAsString);

            if (mergeMat is null)
            {
                mergeMat = mat.Clone();
            }
            else
            {
                CvInvoke.Max(mergeMat, mat, mergeMat);
            }

            if (_mergeFiles) continue;
            layers.Add(new Layer(mat, SlicerFile));

            progress++;
        }

        if (_mergeFiles && mergeMat is not null)
        {
            layers.Add(new Layer(mergeMat, SlicerFile));
        }

        SlicerFile.SuppressRebuildPropertiesWork(() =>
        {
            SlicerFile.LayerHeight = (float) _layerHeight;
            SlicerFile.BottomLayerCount = 1;
            SlicerFile.BottomExposureTime = (float) _exposureTime;
            SlicerFile.ExposureTime = (float)_exposureTime;
            SlicerFile.LiftHeightTotal = 0;
            SlicerFile.SetNoDelays();

            SlicerFile.Layers = layers.ToArray();
        }, true);

        if (_mirror) // Reposition layers
        {
            using var op = new OperationMove(SlicerFile, Anchor.TopLeft)
            {
                MarginLeft = SlicerFile.BoundingRectangle.X,
                MarginTop = SlicerFile.BoundingRectangle.Y,
            };

            var flip = SlicerFile.DisplayMirror;
            if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
            switch (flip)
            {
                case FlipDirection.Horizontally:
                    op.MarginLeft = (int)SlicerFile.ResolutionX - SlicerFile.BoundingRectangle.Right;
                    break;
                case FlipDirection.Vertically:
                    op.MarginTop = (int)SlicerFile.ResolutionY - SlicerFile.BoundingRectangle.Bottom;
                    break;
                case FlipDirection.Both:
                    op.MarginLeft = (int)SlicerFile.ResolutionX - SlicerFile.BoundingRectangle.Right;
                    op.MarginTop = (int)SlicerFile.ResolutionY - SlicerFile.BoundingRectangle.Bottom;
                    break;
            }

            op.Execute(progress);
        }


        if (mergeMat is not null)
        {
            using var croppedMat = mergeMat.CropByBounds(20);
            /*if (_mirror)
            {
                var flip = SlicerFile.DisplayMirror;
                if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
                CvInvoke.Flip(croppedMat, croppedMat, (FlipType)flip);
            }*/
            using var bgrMat = new Mat();
            CvInvoke.CvtColor(croppedMat, bgrMat, ColorConversion.Gray2Bgr);
            SlicerFile.SetThumbnails(bgrMat);
            mergeMat.Dispose();
        }

        return !progress.Token.IsCancellationRequested;
    }


    #endregion

}