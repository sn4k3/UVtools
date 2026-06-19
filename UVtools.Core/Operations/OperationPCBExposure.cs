/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using EmguExtensions;
using UVtools.Core.Excellon;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Gerber;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using ZLinq;

namespace UVtools.Core.Operations;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationPCBExposure : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Sub Classes

    public sealed partial class PCBExposureFile : GenericFileRepresentation
    {
        /// <summary>
        /// Gets or sets to invert the polarity when drawing
        /// </summary>
        [ObservableProperty]
        public partial bool InvertPolarity { get; set; }

        /// <summary>
        /// Gets or sets the scale to apply to each shape drawing size.
        /// Positions and vectors aren't affected by this.
        /// </summary>
        public double SizeScale
        {
            get;
            set => SetProperty(ref field, Math.Max(0.001, Math.Round(value, 4)));
        } = 1;

        public PCBExposureFile()
        {
        }

        public PCBExposureFile(string filePath, bool invertPolarity = false) : base(filePath)
        {
            InvertPolarity = invertPolarity;
        }
    }

    #endregion

    #region Static

    public static string[] ValidExtensions =>
    [
        "gbr", // Gerber
        "gko", // Board outline layer
        "gtl", // Top layer
        "gto", // Top silkscreen layer
        "gts", // Top solder mask layer
        "gbl", // Bottom layer
        "gbo", // Bottom silkscreen layer
        "gbs", // Bottom solder mask layer
        "gml", // Mechanical layer
        "drl", // Drill holes
        "xln" // Eagle drill holes
    ];

    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "Chip";
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
        if (SlicerFile.DisplayWidth <= 0 || SlicerFile.DisplayHeight <= 0)
        {
            return
                $"{NotSupportedMessage}\nReason: No display size information is available to calculate the correct pixel pitch, and so, it's unable to produce a pixel perfect image.";
        }

        return null;
    }

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();
        if (Files.Count == 0)
        {
            sb.AppendLine("Select at least one gerber file");
        }
        else
        {
            foreach (var file in Files)
            {
                if (!file.Exists) sb.AppendLine($"The file {file} does not exists");
            }
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result =
            $"{string.Join(" / ", Files)} [Exposure: {ExposureTime}s] [Rounding: {SizeMidpointRounding}] [Mirror: {Mirror}] [Invert: {InvertColor}]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    public int Count => Files.Count;

    public PCBExposureFile this[int index] => Files[index];

    public override Operation Clone()
    {
        var clone = (OperationPCBExposure)base.Clone();
        clone.Files = Files.CloneByXmlSerialization();
        return clone;
    }

    #endregion

    #region Constructor

    public OperationPCBExposure()
    {
    }

    public OperationPCBExposure(FileFormat slicerFile) : base(slicerFile)
    {
        if (LayerHeight <= 0) LayerHeight = (decimal)SlicerFile.LayerHeight;
        if (ExposureTime <= 0) ExposureTime = (decimal)SlicerFile.BottomExposureTime;
        //Mirror = SlicerFile.DisplayMirror != FlipDirection.None;
    }

    #endregion

    #region Properties

    [ObservableProperty] public partial RangeObservableCollection<PCBExposureFile> Files { get; set; } = [];

    /// <summary>
    /// Gets or sets the gerber files to use.
    /// This property is a redirect to the <see cref="Files"/> collection, but with string array for easier binding and serialization.
    /// </summary>
    public string[] FileArray
    {
        get => Files.Select(file => file.FilePath).ToArray();
        set
        {
            Files.Clear();
            Files.ReplaceRange(value.Select(file => new PCBExposureFile(file)));
        }
    }

    public uint FileCount => (uint)Files.Count;

    [ObservableProperty] public partial bool MergeFiles { get; set; }

    public decimal LayerHeight
    {
        get;
        set => SetProperty(ref field, Layer.RoundHeight(value));
    }

    public decimal ExposureTime
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    }

    [ObservableProperty]
    public partial MidpointRoundingType SizeMidpointRounding { get; set; } = MidpointRoundingType.AwayFromZero;

    [ObservableProperty] public partial decimal OffsetX { get; set; }

    [ObservableProperty] public partial decimal OffsetY { get; set; }

    [ObservableProperty] public partial bool Mirror { get; set; }

    [ObservableProperty] public partial bool InvertColor { get; set; }

    [ObservableProperty] public partial bool EnableAntiAliasing { get; set; }

    #endregion

    #region Equality

    protected bool Equals(OperationPCBExposure other)
    {
        return Files.Equals(other.Files) && MergeFiles == other.MergeFiles && LayerHeight == other.LayerHeight &&
               ExposureTime == other.ExposureTime && SizeMidpointRounding == other.SizeMidpointRounding &&
               OffsetX == other.OffsetX && OffsetY == other.OffsetY && Mirror == other.Mirror &&
               InvertColor == other.InvertColor && EnableAntiAliasing == other.EnableAntiAliasing;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OperationPCBExposure)obj);
    }

    #endregion

    #region Methods

    public void AddFilesFromZip(string zipFile)
    {
        if (!File.Exists(zipFile) || !zipFile.EndsWith(".zip")) return;
        using var zip = ZipFile.Open(zipFile, ZipArchiveMode.Read);

        var tmpPath = PathExtensions.GetTemporaryDirectory($"{About.Software}.");
        foreach (var entry in zip.Entries)
        {
            if (!ValidExtensions.AsValueEnumerable().Any(extension =>
                    entry.Name.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))) continue;

            var filePath = entry.ImprovedExtractToFile(tmpPath, false);
            if (!string.IsNullOrEmpty(filePath))
            {
                AddFile(filePath);
            }
        }
    }

    public void AddFile(string filePath, bool handleZipFiles = true)
    {
        if (!File.Exists(filePath)) return;
        if (filePath.EndsWith(".zip"))
        {
            if (handleZipFiles) AddFilesFromZip(filePath);
            return;
        }

        if (!ValidExtensions.AsValueEnumerable()
                .Any(extension => filePath.EndsWith($".{extension}", StringComparison.OrdinalIgnoreCase))) return;
        var file = new PCBExposureFile(filePath);
        if (Files.Contains(file)) return;
        Files.Add(file);
    }

    public void AddFiles(string[] files, bool handleZipFiles = true)
    {
        foreach (var file in files)
        {
            AddFile(file);
        }
    }

    public void Sort()
    {
        Files.Sort();
    }

    public Mat GetMat(PCBExposureFile file, bool canMirror = true)
    {
        var mat = SlicerFile.CreateMat();
        DrawMat(file, mat, canMirror);
        return mat;
    }

    public void DrawMat(PCBExposureFile file, Mat mat, bool canMirror = true)
    {
        if (!file.Exists) return;


        if (ExcellonDrillFormat.Extensions.AsValueEnumerable().Any(file.IsExtension))
        {
            ExcellonDrillFormat.ParseAndDraw(file, mat, SlicerFile.Ppmm, SizeMidpointRounding,
                new SizeF((float)OffsetX, (float)OffsetY), EnableAntiAliasing);
        }
        else
        {
            GerberFormat.ParseAndDraw(file, mat, SlicerFile.Ppmm, SizeMidpointRounding,
                new SizeF((float)OffsetX, (float)OffsetY), EnableAntiAliasing);
        }

        //var boundingRectangle = CvInvoke.BoundingRectangle(mat);
        //var cropped = mat.Roi(new Size(boundingRectangle.Right, boundingRectangle.Bottom));
        using var cropped = mat.RoiFromBoundingRectangle(out _);

        if (InvertColor) CvInvoke.BitwiseNot(cropped, cropped);
        if (Mirror && canMirror)
        {
            var flip = SlicerFile.DisplayMirror;
            if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
            CvInvoke.Flip(mat, mat, (FlipType)flip);
        }

        return;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        if (Files.Count == 0) return false;
        var layers = new List<Layer>();
        using var mergeMat = SlicerFile.CreateMat();
        progress.ItemCount = FileCount;

        //var orderFiles = Files.OrderBy(file => file.IsExtension(".drl") || file.IsExtension(".xln")).ToArray();
        var orderFiles = Files.AsValueEnumerable()
            .OrderBy(file => ExcellonDrillFormat.Extensions.AsValueEnumerable().Any(file.IsExtension)).ToArray();

        for (var i = 0; i < orderFiles.Length; i++)
        {
            DrawMat(orderFiles[i], mergeMat, false);
            if (!MergeFiles)
            {
                using var mat = GetMat(orderFiles[i]);
                if (CvInvoke.HasNonZero(mat))
                {
                    layers.Add(new Layer(mat, SlicerFile));
                }
            }

            progress++;
        }

        if (MergeFiles)
        {
            if (CvInvoke.HasNonZero(mergeMat))
            {
                if (Mirror)
                {
                    var flip = SlicerFile.DisplayMirror;
                    if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
                    CvInvoke.Flip(mergeMat, mergeMat, (FlipType)flip);
                }

                layers.Add(new Layer(mergeMat, SlicerFile));
            }
        }

        SlicerFile.SuppressRebuildPropertiesWork(() =>
        {
            SlicerFile.LayerHeight = (float)LayerHeight;
            SlicerFile.TransitionLayerCount = 0;
            SlicerFile.BottomLayerCount = 1;
            SlicerFile.BottomExposureTime = (float)ExposureTime;
            SlicerFile.ExposureTime = (float)ExposureTime;

            if (SlicerFile.SupportGCode)
            {
                SlicerFile.BottomLiftHeightTotal = 0;
                SlicerFile.LiftHeightTotal = 0;
            }
            else
            {
                SlicerFile.BottomLiftHeightTotal = 0.1f;
                SlicerFile.LiftHeightTotal = 0.1f;
            }

            /*SlicerFile.BottomLiftSpeed = 300;
            SlicerFile.BottomLiftSpeed2 = 300;
            SlicerFile.LiftSpeed = 300;
            SlicerFile.LiftSpeed2 = 300;*/
            SlicerFile.SetNoDelays();

            SlicerFile.Layers = layers.ToArray();
        }, true);

        if (Mirror) // Reposition layers
        {
            using var op = new OperationMove(SlicerFile, Anchor.TopLeft)
            {
                MarginLeft = SlicerFile.BoundingRectangle.X,
                MarginTop = SlicerFile.BoundingRectangle.Y
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

        using var croppedMat = mergeMat.RoiFromBoundingRectangle(out _, 20);
        using var bgrMat = new Mat();
        CvInvoke.CvtColor(croppedMat, bgrMat, ColorConversion.Gray2Bgr);
        SlicerFile.SetThumbnails(bgrMat);

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}