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
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Emgu.CV;
using Emgu.CV.CvEnum;
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

    #region Sub Classes

    public sealed partial class PCBExposureFile : GenericFileRepresentation
    {
        public PCBExposureFile()
        {
        }

        public PCBExposureFile(string filePath, bool invertPolarity = false) : base(filePath)
        {
            InvertPolarity = invertPolarity;
            IsBoardOutline = IsLikelyBoardOutline(filePath);
        }

        /// <summary>
        /// Gets or sets to invert the polarity when drawing
        /// </summary>
        [ObservableProperty]
        public partial bool InvertPolarity { get; set; }

        /// <summary>
        /// Gets or sets whether this file is a reference outline/profile used to calculate the physical board bounds.
        /// Reference files are not included in the generated exposure layers.
        /// </summary>
        [ObservableProperty]
        public partial bool IsBoardOutline { get; set; }

        /// <summary>
        /// Gets or sets the scale to apply to each shape drawing size.
        /// Positions and vectors aren't affected by this.
        /// </summary>
        public double SizeScale
        {
            get;
            set => SetProperty(ref field, Math.Max(0.001, Math.Round(value, 4)));
        } = 1;

        private static bool IsLikelyBoardOutline(string filePath)
        {
            if (Path.GetExtension(filePath).Equals(".gko", StringComparison.OrdinalIgnoreCase)) return true;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            return fileName.EndsWith("Edge_Cuts", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith("Edge-Cuts", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith("Edge.Cuts", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith("Outline", StringComparison.OrdinalIgnoreCase) ||
                   fileName.EndsWith("Profile", StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// The area that <see cref="InvertColor"/> covers.
    /// </summary>
    public enum InvertAreaType : byte
    {
        [Description("Inside the board outline only")]
        BoardOutline,

        [Description("Whole plate, including the background")]
        Plate
    }

    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "Chip";
    public override string Title => "PCB exposure";

    public override string Description =>
        "Converts a gerber file to a pixel perfect image given your printer LCD/resolution to exposure the copper traces.\n" +
        "Note: When using a placement anchor, provide a board outline/profile file to preserve the board's physical margins.\n" +
        "The current opened file will be overwritten with this gerber image, use a dummy or a not needed file.";

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
            var hasArtworkFile = false;
            var hasOutline = false;
            foreach (var file in Files)
            {
                if (!file.Exists) sb.AppendLine($"The file {file} does not exists");
                if (file.IsBoardOutline) hasOutline = true;
                else hasArtworkFile = true;
            }

            if (!hasArtworkFile)
                sb.AppendLine("Select at least one artwork file in addition to the board outline/profile");
            else if (!hasOutline && Anchor is not (Anchor.None or Anchor.MiddleCenter))
                sb.AppendLine($"""
                               The anchor {Anchor} requires at least one board outline/profile file to preserve the physical margins, please add the board outline/profile to ensure correct margins from sides.
                               The MiddleCenter or original from board requires no outline/profile file.
                               """);
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

    /// <summary>
    /// Gets or sets the area <see cref="InvertColor"/> covers.
    /// <para><see cref="InvertAreaType.BoardOutline"/> confines it to the board, ie the outline files when any
    /// are selected and the drawn artwork otherwise, leaving the surrounding plate dark.</para>
    /// /// <para><see cref="InvertAreaType.Plate"/> lights the whole build area, so everything outside the artwork
    /// is exposed as well.</para>
    /// </summary>
    [ObservableProperty]
    public partial InvertAreaType InvertArea { get; set; }

    [ObservableProperty] public partial bool EnableAntiAliasing { get; set; }

    /// <summary>
    /// Gets or sets to flip the drawn artwork along the vertical axis.
    /// <para>Gerber and Excellon place the origin at the bottom left with Y growing upwards, while image rows grow
    /// downwards, so the renderer produces a vertically mirrored image of the artwork. Enabling this puts it back the
    /// way the file describes it, matching what a gerber viewer or the CAD tool shows.</para>
    /// <para>Applied once to the finished plate, so every file lands on the same axis and the layers stay aligned.</para>
    /// </summary>
    [ObservableProperty]
    public partial bool FlipVertically { get; set; } = true;

    /// <summary>
    /// Gets or sets where to place the artwork on the plate before drawing it.
    /// <para>Files plotted far from the origin, such as a KiCad export using absolute page coordinates,
    /// would otherwise fall outside the plate and render blank.</para>
    /// <para><see cref="Anchor.None"/> preserves the coordinates declared by the board files.</para>
    /// <para><see cref="OffsetX"/> and <see cref="OffsetY"/> still apply on top of the placement as a manual nudge.</para>
    /// </summary>
    [ObservableProperty]
    public partial Anchor Anchor { get; set; } = Anchor.TopLeft;

    /// <summary>
    /// Gets or sets to repeat the artwork as many times as it fits to fill the plate.
    /// Only whole copies are placed, a copy that would be clipped by the plate edge is skipped.
    /// </summary>
    [ObservableProperty]
    public partial bool FillPlate { get; set; }

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
               InvertColor == other.InvertColor && InvertArea == other.InvertArea &&
               EnableAntiAliasing == other.EnableAntiAliasing &&
               FlipVertically == other.FlipVertically && Anchor == other.Anchor && FillPlate == other.FillPlate &&
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

    public void SetAnchor(byte value)
    {
        Anchor = (Anchor)value;
    }

    public void SetAnchor(object value)
    {
        Anchor = (Anchor)Convert.ToByte(value);
    }

    private static bool IsDrillFile(PCBExposureFile file)
    {
        return ExcellonDrillFormat.Extensions.AsValueEnumerable().Any(file.IsExtension);
    }

    private bool HasBoardOutline => Files.AsValueEnumerable().Any(file => file.IsBoardOutline);

    /// <summary>
    /// Parses the outline/profile files, or every artwork file when no outline is selected, to find the physical
    /// board area.
    /// </summary>
    /// <returns>
    /// The union of each reference file bounding rectangle in millimeters, as the files declare the coordinates and
    /// therefore possibly negative, or null when none of them plots anything.
    /// </returns>
    public RectangleF? GetBoundsMillimeters()
    {
        // The draw calls are issued against a 1x1 Mat and clipped away by OpenCV: only the parsing costs anything here.
        // Measuring cannot be done from the rendered plate because whatever falls outside it is already lost.
        using var measureMat = new Mat(1, 1, DepthType.Cv8U, 1);
        RectangleF? result = null;
        var useBoardOutline = HasBoardOutline;

        foreach (var file in Files)
        {
            if (!file.Exists || (useBoardOutline && !file.IsBoardOutline)) continue;

            var bounds = IsDrillFile(file)
                ? ExcellonDrillFormat.ParseAndDraw(file, measureMat, SlicerFile.Ppmm, SizeMidpointRounding).BoundsMm
                : GerberFormat.ParseAndDraw(file, measureMat, SlicerFile.Ppmm, SizeMidpointRounding).BoundsMm;

            if (bounds is null) continue;
            result = result is null ? bounds : RectangleF.Union(result.Value, bounds.Value);
        }

        return result;
    }

    /// <summary>
    /// Gets the offset in millimeters that places the given area at <see cref="Anchor"/>.
    /// </summary>
    /// <param name="boundsMm">Area to place, in millimeters</param>
    public SizeF GetAnchorOffsetMillimeters(RectangleF boundsMm)
    {
        return GetAnchorOffsetMillimeters(boundsMm, Anchor);
    }

    private SizeF GetAnchorOffsetMillimeters(RectangleF boundsMm, Anchor anchor)
    {
        var plateWidthMm = SlicerFile.ResolutionX / SlicerFile.Ppmm.Width;
        var plateHeightMm = SlicerFile.ResolutionY / SlicerFile.Ppmm.Height;

        var x = anchor switch
        {
            Anchor.TopLeft or Anchor.MiddleLeft or Anchor.BottomLeft => -boundsMm.Left,
            Anchor.TopCenter or Anchor.MiddleCenter or Anchor.BottomCenter =>
                plateWidthMm / 2f - (boundsMm.Left + boundsMm.Width / 2f),
            Anchor.TopRight or Anchor.MiddleRight or Anchor.BottomRight => plateWidthMm - boundsMm.Right,
            _ => 0
        };

        var y = anchor switch
        {
            Anchor.TopLeft or Anchor.TopCenter or Anchor.TopRight =>
                FlipVertically ? plateHeightMm - boundsMm.Bottom : -boundsMm.Top,
            Anchor.MiddleLeft or Anchor.MiddleCenter or Anchor.MiddleRight =>
                plateHeightMm / 2f - (boundsMm.Top + boundsMm.Height / 2f),
            Anchor.BottomLeft or Anchor.BottomCenter or Anchor.BottomRight =>
                FlipVertically ? -boundsMm.Top : plateHeightMm - boundsMm.Bottom,
            _ => 0
        };

        return new SizeF(x, y);
    }

    /// <summary>
    /// Renders the board-bounds reference at the center of the plate and returns the bounds of the pixels it draws.
    /// Gerber coordinate bounds follow path and flash centers, so they do not include aperture radii.
    /// </summary>
    private Rectangle GetRenderedBounds(SizeF centerOffsetMm)
    {
        using var mat = SlicerFile.CreateMat();
        DrawBoundsReference(mat, centerOffsetMm);
        if (FlipVertically) FlipMatVertically(mat);
        return CvInvoke.BoundingRectangle(mat);
    }

    private void DrawBoundsReference(Mat mat, SizeF offsetMm)
    {
        var useBoardOutline = HasBoardOutline;

        // Match execution order so subtractive drill files affect the measured result in the same way.
        foreach (var file in Files)
        {
            if (IsDrillFile(file) || (useBoardOutline && !file.IsBoardOutline)) continue;
            DrawMat(file, mat, false, offsetMm);
        }

        foreach (var file in Files)
        {
            if (!IsDrillFile(file) || (useBoardOutline && !file.IsBoardOutline)) continue;
            DrawMat(file, mat, false, offsetMm);
        }
    }

    /// <summary>
    /// Gets the offset in millimeters to draw with: the manual <see cref="OffsetX"/> and <see cref="OffsetY"/>,
    /// plus the placement correction selected by <see cref="Anchor"/>.
    /// </summary>
    /// <remarks>
    /// Every file must be drawn with the same offset, otherwise the layers of a multi file job no longer line up.
    /// Compute it once and pass it to <see cref="DrawMat"/> / <see cref="GetMat"/> rather than letting each call re-measure.
    /// </remarks>
    public SizeF GetDrawOffsetMillimeters()
    {
        var offset = new SizeF((float)OffsetX, (float)OffsetY);
        if (Anchor == Anchor.None) return offset;

        if (GetBoundsMillimeters() is not { } bounds) return offset;

        // Centering the declared coordinates keeps a fitting drawing fully visible while its real pixel bounds are
        // measured. Those bounds include flashes, line thickness, arcs, macros and anti-aliasing.
        var centerOffset = GetAnchorOffsetMillimeters(bounds, Anchor.MiddleCenter);
        var renderedBounds = GetRenderedBounds(centerOffset);
        if (renderedBounds.Width <= 0 || renderedBounds.Height <= 0)
        {
            var anchorOffset = GetAnchorOffsetMillimeters(bounds);
            return new SizeF(offset.Width + anchorOffset.Width, offset.Height + anchorOffset.Height);
        }

        var availableX = SlicerFile.ResolutionX - renderedBounds.Width;
        var availableY = SlicerFile.ResolutionY - renderedBounds.Height;
        var targetX = Anchor switch
        {
            Anchor.TopLeft or Anchor.MiddleLeft or Anchor.BottomLeft => 0,
            Anchor.TopCenter or Anchor.MiddleCenter or Anchor.BottomCenter => availableX / 2,
            Anchor.TopRight or Anchor.MiddleRight or Anchor.BottomRight => availableX,
            _ => renderedBounds.X
        };
        var targetY = Anchor switch
        {
            Anchor.TopLeft or Anchor.TopCenter or Anchor.TopRight => 0,
            Anchor.MiddleLeft or Anchor.MiddleCenter or Anchor.MiddleRight => availableY / 2,
            Anchor.BottomLeft or Anchor.BottomCenter or Anchor.BottomRight => availableY,
            _ => renderedBounds.Y
        };

        var correctionX = (targetX - renderedBounds.X) / SlicerFile.Ppmm.Width;
        var correctionY = (targetY - renderedBounds.Y) / SlicerFile.Ppmm.Height;
        if (FlipVertically) correctionY = -correctionY;

        return new SizeF(
            offset.Width + centerOffset.Width + correctionX,
            offset.Height + centerOffset.Height + correctionY);
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
    /// <see cref="Anchor"/> is <see cref="UVtools.Core.Anchor.None"/>. Nothing is moved when a single copy is all that
    /// fits.</para>
    /// </remarks>
    public int FillPlateWithCopies(Mat mat, Rectangle? source = null)
    {
        var cell = source ?? CvInvoke.BoundingRectangle(mat);
        var grid = GetFillGrid(mat.Size, cell);
        if (grid.Length == 0) return 0;

        using var copy = mat.Roi(cell).Clone();

        // The original is not on the grid, so lay the whole thing out from scratch
        mat.SetTo(EmguCvExtensions.BlackColor);

        foreach (var target in grid)
        {
            using var destination = mat.Roi(target);
            copy.CopyTo(destination);
        }

        return grid.Length - 1;
    }

    /// <summary>
    /// Lays out the grid of copies that fits the plate for a cell of the given size.
    /// </summary>
    /// <param name="plate">Plate size in pixels</param>
    /// <param name="cell">Area a single copy occupies</param>
    /// <returns>Where each copy lands, empty when no more than one fits and the artwork stays put</returns>
    /// <remarks>
    /// Shared by the tiling and by anything that needs to know where the copies ended up, since the grid is
    /// centered on the plate and the original cell position is not one of them.
    /// </remarks>
    private Rectangle[] GetFillGrid(Size plate, Rectangle cell)
    {
        if (cell.Width <= 0 || cell.Height <= 0) return [];

        var gapX = (int)Math.Round((double)FillSpacingX * SlicerFile.Ppmm.Width);
        var gapY = (int)Math.Round((double)FillSpacingY * SlicerFile.Ppmm.Height);

        // n copies span n * size + (n - 1) * gap, so the largest n that still fits is (plate + gap) / (size + gap)
        var columns = (plate.Width + gapX) / (cell.Width + gapX);
        var rows = (plate.Height + gapY) / (cell.Height + gapY);
        if (columns <= 1 && rows <= 1) return [];

        // Center the grid rather than growing outwards from wherever the original sits, otherwise the
        // margins left over on the anchored side go to waste and fewer copies fit than the plate allows
        var startX = (plate.Width - (columns * cell.Width + (columns - 1) * gapX)) / 2;
        var startY = (plate.Height - (rows * cell.Height + (rows - 1) * gapY)) / 2;

        var grid = new Rectangle[rows * columns];
        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                grid[row * columns + column] = new Rectangle(
                    startX + column * (cell.Width + gapX),
                    startY + row * (cell.Height + gapY),
                    cell.Width, cell.Height);
            }
        }

        return grid;
    }

    /// <summary>
    /// Draws the outline/profile, or all artwork when no outline is selected, to measure the physical board area
    /// that <see cref="FillPlateWithCopies"/> replicates.
    /// </summary>
    /// <param name="drawOffsetMm">Offset to draw with, defaults to <see cref="GetDrawOffsetMillimeters"/></param>
    /// <param name="canMirror">Mirror the composed plate, to match a target that was drawn mirrored</param>
    /// <returns>The cell rectangle in pixels, or null when nothing is drawn</returns>
    public Rectangle? GetFillSourceRectangle(SizeF? drawOffsetMm = null, bool canMirror = false)
    {
        var offset = drawOffsetMm ?? GetDrawOffsetMillimeters();
        using var mat = SlicerFile.CreateMat();

        // Drawn unmirrored on purpose. Apply the plate transforms a single time afterwards.
        DrawBoundsReference(mat, offset);
        if (FlipVertically) FlipMatVertically(mat);
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
    private static void FlipMatVertically(Mat mat)
    {
        CvInvoke.Flip(mat, mat, FlipType.Vertical);
    }

    /// <summary>
    /// Flips <paramref name="mat"/> along the printer display mirror axis, defaulting to horizontally.
    /// </summary>
    private void MirrorMat(Mat mat)
    {
        var flip = SlicerFile.DisplayMirror;
        if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
        CvInvoke.Flip(mat, mat, (FlipType)flip);
    }

    public Mat GetMat(PCBExposureFile file, bool canMirror = true, SizeF? drawOffsetMm = null,
        Rectangle? fillSource = null)
    {
        return GetMat(file, out _, canMirror, drawOffsetMm, fillSource);
    }

    /// <summary>
    /// Draws a single file onto a plate.
    /// </summary>
    /// <param name="file">File to draw</param>
    /// <param name="contentBounds">
    /// Area the artwork occupies, measured before <see cref="InvertColor"/> is applied.
    /// <para>Inverting lights the background, so the drawn pixels of the finished plate are no longer the
    /// artwork. Anything that needs to know where the content is, such as cropping a preview or a thumbnail,
    /// must use this rather than measure the returned <see cref="Mat"/>.</para>
    /// </param>
    /// <param name="canMirror">Apply the display mirror</param>
    /// <param name="drawOffsetMm">Offset to draw with, defaults to <see cref="GetDrawOffsetMillimeters"/></param>
    /// <param name="fillSource">Grid cell to tile, defaults to one measured across every file</param>
    public Mat GetMat(PCBExposureFile file, out Rectangle contentBounds, bool canMirror = true,
        SizeF? drawOffsetMm = null, Rectangle? fillSource = null)
    {
        // Resolve the offset before allocating the final plate because anchor measurement uses a temporary plate.
        var offset = drawOffsetMm ?? GetDrawOffsetMillimeters();
        var mat = SlicerFile.CreateMat();
        DrawMat(file, mat, canMirror, offset);
        if (FlipVertically) FlipMatVertically(mat);

        // Measured across every file, not just this one: a drill layer covers a smaller area than the
        // copper it belongs to, and sizing each layer grid on its own content pulls the copies out of line.
        // Resolved once here, since both the tiling and the board sized inversion need it.
        var boardCell = FillPlate || (InvertColor && InvertArea == InvertAreaType.BoardOutline)
            ? fillSource ?? GetFillSourceRectangle(offset, canMirror)
            : null;

        if (FillPlate) FillPlateWithCopies(mat, boardCell);

        // Measured before inverting: afterwards the lit pixels are the background, not the artwork
        contentBounds = CvInvoke.BoundingRectangle(mat);

        // Last, so everything above still sees the drawn area rather than a lit plate
        if (InvertColor) InvertColors(mat, GetInvertAreas(mat.Size, boardCell));

        return mat;
    }

    public void DrawMat(PCBExposureFile file, Mat mat, bool canMirror = true, SizeF? drawOffsetMm = null)
    {
        if (!file.Exists) return;

        var offset = drawOffsetMm ?? GetDrawOffsetMillimeters();

        if (IsDrillFile(file))
        {
            ExcellonDrillFormat.ParseAndDraw(file, mat, SlicerFile.Ppmm, SizeMidpointRounding,
                offset, EnableAntiAliasing);
        }
        else
        {
            GerberFormat.ParseAndDraw(file, mat, SlicerFile.Ppmm, SizeMidpointRounding,
                offset, EnableAntiAliasing);
        }

        if (Mirror && canMirror) MirrorMat(mat);
    }

    /// <summary>
    /// Inverts the plate so the artwork is dark against a lit background.
    /// </summary>
    /// <param name="mat">Plate to invert</param>
    /// <param name="area">Region to light, or null for the whole plate</param>
    /// <remarks>
    /// <para>Must run once per plate, never per file. Composing several files applies it once each, and every
    /// pass flips the area again: an even number of files cancels out, and whatever was drawn after the first
    /// pass is the only thing left inverted. That reads as "only the drill holes inverted", since drill files
    /// are drawn last.</para>
    /// <para>Runs after the plate is tiled, not before: inverting first lights the area, so the grid cell
    /// measured from it would span everything and no copy would fit.</para>
    /// </remarks>
    private static void InvertColors(Mat mat, Rectangle[]? areas = null)
    {
        // Nothing was drawn, so there is nothing to invert. Lighting the plate here would turn an empty
        // result into a full power exposure of the entire screen.
        if (!CvInvoke.HasNonZero(mat)) return;

        if (areas is null || areas.Length == 0)
        {
            CvInvoke.BitwiseNot(mat, mat);
            return;
        }

        foreach (var area in areas)
        {
            if (area is not { Width: > 0, Height: > 0 }) continue;
            using var roi = mat.Roi(area);
            CvInvoke.BitwiseNot(roi, roi);
        }
    }

    /// <summary>
    /// Gets the regions <see cref="InvertColor"/> should light, in plate pixels.
    /// </summary>
    /// <param name="plate">Plate size in pixels</param>
    /// <param name="boardCell">Area a single board occupies, as returned by <see cref="GetFillSourceRectangle"/></param>
    /// <returns>One region per board, or null to light the whole plate</returns>
    /// <remarks>
    /// With the plate filled the board sits in every grid cell rather than where it was first drawn, so each
    /// copy gets its own region. Using the pre-tiling rectangle would light a patch of plate that no longer
    /// holds a board.
    /// </remarks>
    private Rectangle[]? GetInvertAreas(Size plate, Rectangle? boardCell)
    {
        if (InvertArea != InvertAreaType.BoardOutline || boardCell is not { } cell) return null;
        if (!FillPlate) return [cell];

        var grid = GetFillGrid(plate, cell);
        return grid.Length > 0 ? grid : [cell];
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        if (Files.Count == 0) return false;
        var layers = new List<Layer>();

        //var orderFiles = Files.OrderBy(file => file.IsExtension(".drl") || file.IsExtension(".xln")).ToArray();
        var orderFiles = Files.AsValueEnumerable()
            .Where(file => !file.IsBoardOutline)
            .OrderBy(IsDrillFile).ToArray();
        progress.ItemCount = (uint)orderFiles.Length;

        // Measured once and shared by every file, per file centering would misalign the layers against each other
        var drawOffset = GetDrawOffsetMillimeters();
        using var mergeMat = SlicerFile.CreateMat();

        // Compose every file first: the per layer Mats below need the grid cell measured across all of them
        for (var i = 0; i < orderFiles.Length; i++)
        {
            progress.PauseOrCancelIfRequested();
            DrawMat(orderFiles[i], mergeMat, false, drawOffset);
            progress++;
        }

        // Once the whole plate is composed, so every file is treated the same way
        if (FlipVertically) FlipMatVertically(mergeMat);

        if (progress.Token.IsCancellationRequested) return false;

        // Nothing to expose. Reported rather than returned quietly, since the cause is actionable and the
        // thumbnail below would otherwise throw an opaque OpenCV "!_src.empty()" on the empty Mat
        if (!CvInvoke.HasNonZero(mergeMat))
        {
            throw new InvalidOperationException(
                "The generated image is empty, nothing was rendered onto the build area.\n" +
                "This usually means the artwork does not fit the plate, or that it sits outside it because " +
                "placement is set to None and the file uses coordinates far from the origin.\n" +
                "Select a placement anchor, or set the Offset X/Y to bring the artwork onto the plate, and try again.");
        }

        // The composed plate is the grid cell for every layer. Taken before mirroring and before the plate is
        // tiled, so each layer replicates the same area no matter how much of it that layer actually covers.
        var fillSource = FillPlate
            ? HasBoardOutline
                ? GetFillSourceRectangle(drawOffset)
                : CvInvoke.BoundingRectangle(mergeMat)
            : null;

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

        // Mirrored whether or not the files were merged, so the composed plate keeps the same orientation as
        // the layers above and can stand in for them when measuring below
        if (Mirror) MirrorMat(mergeMat);

        // Where the artwork sits, with every geometric step applied and nothing inverted yet. Inverting lights
        // the background, so from here on the drawn pixels are no longer the content and must not be measured.
        var contentBounds = CvInvoke.BoundingRectangle(mergeMat);

        // Last of all: inverting earlier would light the area, and every measurement above would then be
        // reading the background instead of the artwork. The grid is centered on the plate, so mirroring
        // maps it onto itself and the cells are the same either way.
        if (InvertColor)
        {
            var boardCell = InvertArea == InvertAreaType.BoardOutline
                ? fillSource ?? GetFillSourceRectangle(drawOffset, true)
                : null;
            InvertColors(mergeMat, GetInvertAreas(mergeMat.Size, boardCell));
        }

        if (MergeFiles) layers.Add(new Layer(mergeMat, SlicerFile));

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
            // Measured from contentBounds rather than SlicerFile.BoundingRectangle: the layers may be
            // inverted by now, and their lit pixels would then be the background rather than the artwork
            using var op = new OperationMove(SlicerFile, Anchor.TopLeft)
            {
                MarginLeft = contentBounds.X,
                MarginTop = contentBounds.Y
            };

            var flip = SlicerFile.DisplayMirror;
            if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
            switch (flip)
            {
                case FlipDirection.Horizontally:
                    op.MarginLeft = (int)SlicerFile.ResolutionX - contentBounds.Right;
                    break;
                case FlipDirection.Vertically:
                    op.MarginTop = (int)SlicerFile.ResolutionY - contentBounds.Bottom;
                    break;
                case FlipDirection.Both:
                    op.MarginLeft = (int)SlicerFile.ResolutionX - contentBounds.Right;
                    op.MarginTop = (int)SlicerFile.ResolutionY - contentBounds.Bottom;
                    break;
            }

            op.Execute(progress);
        }

        // Cropped to the artwork, not to whatever the plate happens to have lit
        var thumbnailBounds = Rectangle.Inflate(contentBounds, 20, 20);
        thumbnailBounds.Intersect(new Rectangle(0, 0, mergeMat.Width, mergeMat.Height));

        using var croppedMat = mergeMat.Roi(thumbnailBounds);
        using var bgrMat = new Mat();
        CvInvoke.CvtColor(croppedMat, bgrMat, ColorConversion.Gray2Bgr);
        SlicerFile.SetThumbnails(bgrMat);

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}