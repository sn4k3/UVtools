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

    /// <summary>
    /// Gets or sets to flip the drawn artwork along the vertical axis.
    /// <para>Gerber and Excellon place the origin at the bottom left with Y growing upwards, while image rows grow
    /// downwards, so the renderer produces a vertically mirrored image of the artwork. Enabling this puts it back the
    /// way the file describes it, matching what a gerber viewer or the CAD tool shows.</para>
    /// <para>Applied once to the finished plate, so every file lands on the same axis and the layers stay aligned.</para>
    /// </summary>
    [ObservableProperty] public partial bool FlipY { get; set; } = true;

    /// <summary>
    /// Gets or sets to center the artwork on the plate before drawing it.
    /// <para>Files plotted far from the origin, such as a KiCad export using absolute page coordinates,
    /// would otherwise fall outside the plate and render blank.</para>
    /// <para><see cref="OffsetX"/> and <see cref="OffsetY"/> still apply on top of the centering as a manual nudge.</para>
    /// </summary>
    [ObservableProperty] public partial bool AutoCenter { get; set; } = true;

    /// <summary>
    /// Gets or sets to repeat the artwork as many times as it fits to fill the plate.
    /// Only whole copies are placed, a copy that would be clipped by the plate edge is skipped.
    /// </summary>
    [ObservableProperty] public partial bool FillPlate { get; set; }

    /// <summary>
    /// Gets or sets the horizontal gap in millimeters left between each copy when <see cref="FillPlate"/> is enabled
    /// </summary>
    public decimal FillSpacingX
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    } = 5;

    /// <summary>
    /// Gets or sets the vertical gap in millimeters left between each copy when <see cref="FillPlate"/> is enabled
    /// </summary>
    public decimal FillSpacingY
    {
        get;
        set => SetProperty(ref field, Math.Round(Math.Max(0, value), 2));
    } = 5;

    #endregion

    #region Equality

    protected bool Equals(OperationPCBExposure other)
    {
        return Files.Equals(other.Files) && MergeFiles == other.MergeFiles && LayerHeight == other.LayerHeight &&
               ExposureTime == other.ExposureTime && SizeMidpointRounding == other.SizeMidpointRounding &&
               OffsetX == other.OffsetX && OffsetY == other.OffsetY && Mirror == other.Mirror &&
               InvertColor == other.InvertColor && EnableAntiAliasing == other.EnableAntiAliasing &&
               FlipY == other.FlipY && AutoCenter == other.AutoCenter && FillPlate == other.FillPlate &&
               FillSpacingX == other.FillSpacingX && FillSpacingY == other.FillSpacingY;
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
        if (!File.Exists(zipFile) || !zipFile.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) return;
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
        if (filePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
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
            AddFile(file, handleZipFiles);
        }
    }

    public void Sort()
    {
        Files.Sort();
    }

    /// <summary>
    /// Parses every file in <see cref="Files"/> without rendering it, to find the area the artwork occupies.
    /// </summary>
    /// <returns>
    /// The union of each file bounding rectangle in millimeters, as the files declare the coordinates and
    /// therefore possibly negative, or null when none of them plots anything.
    /// </returns>
    public RectangleF? GetBoundsMillimeters()
    {
        // The draw calls are issued against a 1x1 Mat and clipped away by OpenCV: only the parsing costs anything here.
        // Measuring cannot be done from the rendered plate because whatever falls outside it is already lost.
        using var measureMat = new Mat(1, 1, DepthType.Cv8U, 1);
        RectangleF? result = null;

        foreach (var file in Files)
        {
            if (!file.Exists) continue;

            var bounds = ExcellonDrillFormat.Extensions.AsValueEnumerable().Any(file.IsExtension)
                ? ExcellonDrillFormat.ParseAndDraw(file, measureMat, SlicerFile.Ppmm, SizeMidpointRounding).BoundsMm
                : GerberFormat.ParseAndDraw(file, measureMat, SlicerFile.Ppmm, SizeMidpointRounding).BoundsMm;

            if (bounds is null) continue;
            result = result is null ? bounds : RectangleF.Union(result.Value, bounds.Value);
        }

        return result;
    }

    /// <summary>
    /// Gets the offset in millimeters that places the given area at the center of the plate.
    /// </summary>
    /// <param name="boundsMm">Area to center, in millimeters</param>
    public SizeF GetCenterOffsetMillimeters(RectangleF boundsMm)
    {
        var plateWidthMm = SlicerFile.ResolutionX / SlicerFile.Ppmm.Width;
        var plateHeightMm = SlicerFile.ResolutionY / SlicerFile.Ppmm.Height;

        return new SizeF(
            plateWidthMm / 2f - (boundsMm.Left + boundsMm.Width / 2f),
            plateHeightMm / 2f - (boundsMm.Top + boundsMm.Height / 2f));
    }

    /// <summary>
    /// Gets the offset in millimeters to draw with: the manual <see cref="OffsetX"/> and <see cref="OffsetY"/>,
    /// plus the centering correction when <see cref="AutoCenter"/> is enabled.
    /// </summary>
    /// <remarks>
    /// Every file must be drawn with the same offset, otherwise the layers of a multi file job no longer line up.
    /// Compute it once and pass it to <see cref="DrawMat"/> / <see cref="GetMat"/> rather than letting each call re-measure.
    /// </remarks>
    public SizeF GetDrawOffsetMillimeters()
    {
        var offset = new SizeF((float)OffsetX, (float)OffsetY);
        if (!AutoCenter) return offset;

        if (GetBoundsMillimeters() is not { } bounds) return offset;

        var center = GetCenterOffsetMillimeters(bounds);
        return new SizeF(offset.Width + center.Width, offset.Height + center.Height);
    }

    /// <summary>
    /// Repeats whatever is already drawn on <paramref name="mat"/> to fill the rest of the plate, leaving
    /// <see cref="FillSpacingX"/> and <see cref="FillSpacingY"/> millimeters of gap between each copy.
    /// </summary>
    /// <param name="mat">Plate sized <see cref="Mat"/> holding the drawn artwork</param>
    /// <param name="source">
    /// The grid cell to replicate, in pixels. Defaults to the bounds of whatever is drawn on <paramref name="mat"/>.
    /// <para>When several layers are tiled they must all be given the same cell, otherwise each one is measured on its
    /// own content, the cells differ in size and the copies of, say, a copper layer and a drill layer drift apart.</para>
    /// </param>
    /// <returns>Number of copies added, the original is not counted</returns>
    /// <remarks>
    /// <para>The cell is taken from drawn pixels rather than computed from the file coordinates, so it stays correct
    /// regardless of the offset and inversion already applied. It must however be measured in the same orientation as
    /// <paramref name="mat"/>, so tile before mirroring, or measure from an equally mirrored plate.</para>
    /// <para>The resulting grid is centered on the plate, which repositions the artwork even when
    /// <see cref="AutoCenter"/> is disabled. Nothing is moved when a single copy is all that fits.</para>
    /// </remarks>
    public int FillPlateWithCopies(Mat mat, Rectangle? source = null)
    {
        var cell = source ?? CvInvoke.BoundingRectangle(mat);
        if (cell.Width <= 0 || cell.Height <= 0) return 0;

        var gapX = (int)Math.Round((double)FillSpacingX * SlicerFile.Ppmm.Width);
        var gapY = (int)Math.Round((double)FillSpacingY * SlicerFile.Ppmm.Height);

        // n copies span n * size + (n - 1) * gap, so the largest n that still fits is (plate + gap) / (size + gap)
        var columns = (mat.Width + gapX) / (cell.Width + gapX);
        var rows = (mat.Height + gapY) / (cell.Height + gapY);
        if (columns <= 1 && rows <= 1) return 0;

        using var copy = mat.Roi(cell).Clone();

        // Center the grid rather than growing outwards from wherever the original sits, otherwise the
        // margins left over on the anchored side go to waste and fewer copies fit than the plate allows
        var startX = (mat.Width - (columns * cell.Width + (columns - 1) * gapX)) / 2;
        var startY = (mat.Height - (rows * cell.Height + (rows - 1) * gapY)) / 2;

        // The original is not on the grid, so lay the whole thing out from scratch
        mat.SetTo(EmguCvExtensions.BlackColor);

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                using var destination = mat.Roi(new Rectangle(
                    startX + column * (cell.Width + gapX),
                    startY + row * (cell.Height + gapY),
                    cell.Width, cell.Height));
                copy.CopyTo(destination);
            }
        }

        return rows * columns - 1;
    }

    /// <summary>
    /// Composes every file onto one plate to measure the area a single copy occupies, ie: the grid cell that
    /// <see cref="FillPlateWithCopies"/> replicates.
    /// </summary>
    /// <param name="drawOffsetMm">Offset to draw with, defaults to <see cref="GetDrawOffsetMillimeters"/></param>
    /// <param name="canMirror">Mirror the composed plate, to match a target that was drawn mirrored</param>
    /// <returns>The cell rectangle in pixels, or null when nothing is drawn</returns>
    public Rectangle? GetFillSourceRectangle(SizeF? drawOffsetMm = null, bool canMirror = false)
    {
        var offset = drawOffsetMm ?? GetDrawOffsetMillimeters();
        using var mat = SlicerFile.CreateMat();

        // Drawn unmirrored on purpose: DrawMat flips the whole Mat, so mirroring once per file would
        // undo itself on every second one. Apply the flips a single time afterwards instead.
        foreach (var file in Files) DrawMat(file, mat, false, offset);
        if (FlipY) FlipMatVertically(mat);
        if (canMirror && Mirror) MirrorMat(mat);

        var rectangle = CvInvoke.BoundingRectangle(mat);
        return rectangle is { Width: > 0, Height: > 0 } ? rectangle : null;
    }

    /// <summary>
    /// Flips <paramref name="mat"/> along the vertical axis to undo the top/bottom mirroring the renderer applies.
    /// </summary>
    /// <remarks>
    /// Must run once per plate, never per file: this flips the whole <see cref="Mat"/>, so calling it while composing
    /// several files into one would undo itself on every second file.
    /// </remarks>
    private static void FlipMatVertically(Mat mat) => CvInvoke.Flip(mat, mat, FlipType.Vertical);

    /// <summary>
    /// Flips <paramref name="mat"/> along the printer display mirror axis, defaulting to horizontally.
    /// </summary>
    private void MirrorMat(Mat mat)
    {
        var flip = SlicerFile.DisplayMirror;
        if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
        CvInvoke.Flip(mat, mat, (FlipType)flip);
    }

    public Mat GetMat(PCBExposureFile file, bool canMirror = true, SizeF? drawOffsetMm = null, Rectangle? fillSource = null)
    {
        var mat = SlicerFile.CreateMat();
        DrawMat(file, mat, canMirror, drawOffsetMm);
        if (FlipY) FlipMatVertically(mat);

        if (FillPlate)
        {
            // Measured across every file, not just this one: a drill layer covers a smaller area than the
            // copper it belongs to, and sizing each layer grid on its own content pulls the copies out of line
            FillPlateWithCopies(mat, fillSource ?? GetFillSourceRectangle(drawOffsetMm, canMirror));
        }

        return mat;
    }

    public void DrawMat(PCBExposureFile file, Mat mat, bool canMirror = true, SizeF? drawOffsetMm = null)
    {
        if (!file.Exists) return;

        var offset = drawOffsetMm ?? GetDrawOffsetMillimeters();

        if (ExcellonDrillFormat.Extensions.AsValueEnumerable().Any(file.IsExtension))
        {
            ExcellonDrillFormat.ParseAndDraw(file, mat, SlicerFile.Ppmm, SizeMidpointRounding,
                offset, EnableAntiAliasing);
        }
        else
        {
            GerberFormat.ParseAndDraw(file, mat, SlicerFile.Ppmm, SizeMidpointRounding,
                offset, EnableAntiAliasing);
        }

        // Nothing was rendered onto the build area, the operations below would run on an empty Mat and throw
        if (!CvInvoke.HasNonZero(mat)) return;

        //var boundingRectangle = CvInvoke.BoundingRectangle(mat);
        //var cropped = mat.Roi(new Size(boundingRectangle.Right, boundingRectangle.Bottom));
        using var cropped = mat.RoiFromBoundingRectangle(out _);

        if (InvertColor) CvInvoke.BitwiseNot(cropped, cropped);
        if (Mirror && canMirror) MirrorMat(mat);
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

        // Measured once and shared by every file, per file centering would misalign the layers against each other
        var drawOffset = GetDrawOffsetMillimeters();

        // Compose every file first: the per layer Mats below need the grid cell measured across all of them
        for (var i = 0; i < orderFiles.Length; i++)
        {
            progress.PauseOrCancelIfRequested();
            DrawMat(orderFiles[i], mergeMat, false, drawOffset);
            progress++;
        }

        // Once the whole plate is composed, so it lands on the same axis for every file
        if (FlipY) FlipMatVertically(mergeMat);

        if (progress.Token.IsCancellationRequested) return false;

        // Nothing to expose. Reported rather than returned quietly, since the cause is actionable and the
        // thumbnail below would otherwise throw an opaque OpenCV "!_src.empty()" on the empty Mat
        if (!CvInvoke.HasNonZero(mergeMat))
        {
            throw new InvalidOperationException(
                "The generated image is empty, nothing was rendered onto the build area.\n" +
                "This usually means the artwork does not fit the plate, or that it sits outside it because " +
                "auto-center is disabled and the file uses coordinates far from the origin.\n" +
                "Enable auto-center, or set the Offset X/Y to bring the artwork onto the plate, and try again.");
        }

        // The composed plate is the grid cell for every layer. Taken before mirroring and before the plate is
        // tiled, so each layer replicates the same area no matter how much of it that layer actually covers.
        Rectangle? fillSource = FillPlate ? CvInvoke.BoundingRectangle(mergeMat) : null;

        if (!MergeFiles)
        {
            foreach (var file in orderFiles)
            {
                progress.PauseOrCancelIfRequested();

                // Drawn unmirrored so the cell above still applies, then mirrored once the plate is laid out
                using var mat = GetMat(file, false, drawOffset, fillSource);
                if (!CvInvoke.HasNonZero(mat)) continue;

                if (Mirror) MirrorMat(mat);
                layers.Add(new Layer(mat, SlicerFile));
            }
        }

        if (fillSource is not null) FillPlateWithCopies(mergeMat, fillSource);

        if (MergeFiles)
        {
            if (Mirror) MirrorMat(mergeMat);
            layers.Add(new Layer(mergeMat, SlicerFile));
        }

        if (progress.Token.IsCancellationRequested || layers.Count == 0) return false;
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
