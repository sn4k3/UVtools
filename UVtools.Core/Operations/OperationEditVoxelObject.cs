/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using EmguExtensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Voxel;

namespace UVtools.Core.Operations;

public enum VoxelObjectEdit
{
    Move,
    Duplicate,
    Delete
}

/// <summary>
/// Edits a connected group of slice pixels. The caller owns the undo snapshot.
/// </summary>
public sealed class OperationEditVoxelObject : Operation
{
    private VoxelObjectSelection? _selection;
    private readonly VoxelObjectEdit _edit;
    private readonly Point _offset;

    public OperationEditVoxelObject(FileFormat file, VoxelObjectSelection selection, VoxelObjectEdit edit, Point offset)
        : base(file)
    {
        _selection = selection;
        _edit = edit;
        _offset = offset;
        LayerIndexStart = selection.Planes[0].FirstLayer;
        LayerIndexEnd = selection.Planes[^1].EndLayer - 1;
    }

    public override bool CanROI => false;
    public override bool CanMask => false;
    public override bool CanHaveProfiles => false;
    public override string Title => $"{_edit} 3D object";
    public override string ProgressAction => "Editing object layers";

    public override string? ValidateInternally()
    {
        if (_selection is not { } selection || !selection.IsCurrentFor(SlicerFile))
            return "The slices changed. Select the object again.";

        if (_edit == VoxelObjectEdit.Delete) return null;
        if (_offset.IsEmpty) return "Enter an X or Y offset of at least one pixel.";

        var bounds = selection.PixelBounds;
        if ((long)bounds.Left + _offset.X < 0 || (long)bounds.Top + _offset.Y < 0 ||
            (long)bounds.Right + _offset.X > SlicerFile.ResolutionX ||
            (long)bounds.Bottom + _offset.Y > SlicerFile.ResolutionY)
        {
            return "The object would extend outside the build plate.";
        }

        return null;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var selection = _selection;
        try
        {
            if (selection is null || !selection.IsCurrentFor(SlicerFile)) return false;

            var move = _edit == VoxelObjectEdit.Move;
            var translate = _edit != VoxelObjectEdit.Delete;
            var planes = selection.Planes;
            var options = CoreSettings.GetParallelOptions(progress);

            if (translate)
            {
                progress.Reset("Checking placement", (uint)planes.Length);
                var collision = false;
                Parallel.ForEach(planes, options, (plane, state) =>
                {
                    progress.PauseOrCancelIfRequested();
                    if (OverlapsOriginal(plane, move))
                    {
                        Volatile.Write(ref collision, true);
                        state.Stop();
                        return;
                    }
                    progress.LockAndIncrement();
                });

                if (collision)
                {
                    AfterCompleteReport = "The destination overlaps existing pixels. Choose a different offset.";
                    return false;
                }
            }

            if (!selection.IsCurrentFor(SlicerFile)) return false;

            progress.Reset(ProgressAction, (uint)planes.Length);
            Parallel.ForEach(planes, options, plane =>
            {
                for (var index = plane.FirstLayer; index < plane.EndLayer; index++)
                {
                    progress.PauseOrCancelIfRequested();
                    var layer = SlicerFile[index];
                    if (layer.IsEmpty) continue;

                    using var image = layer.LayerMat;
                    if (EditLayer(image, plane, move, translate)) layer.LayerMat = image;
                }
                progress.LockAndIncrement();
            });

            SlicerFile.BoundingRectangle = Rectangle.Empty;
            return true;
        }
        finally
        {
            _selection = null;
        }
    }

    private bool OverlapsOriginal(VoxelObjectSelection.Plane plane, bool move)
    {
        var bounds = plane.Bounds;
        bounds.Offset(_offset);
        using var image = VoxelObjectSelection.GetPlaneImage(SlicerFile, bounds, plane.FirstLayer, plane.EndLayer);

        foreach (var run in plane.Runs)
        {
            var target = image.GetReadOnlyRowSpanOfBytes(run.Y - plane.Bounds.Y)
                .Slice(run.Start - plane.Bounds.X, run.Length);
            if (!target.ContainsAnyExcept((byte)0)) continue;
            if (!move) return true;
            for (var x = 0; x < run.Length; x++)
            {
                if (target[x] != 0 && !ContainsPixel(plane.Runs, run.Start + _offset.X + x, run.Y + _offset.Y))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsPixel(ReadOnlySpan<VoxelObjectSelection.PixelRun> runs, int x, int y)
    {
        var left = 0;
        var right = runs.Length - 1;
        while (left <= right)
        {
            var middle = left + (right - left) / 2;
            var run = runs[middle];
            if (y < run.Y || (y == run.Y && x < run.Start)) right = middle - 1;
            else if (y > run.Y || x >= run.Start + run.Length) left = middle + 1;
            else return true;
        }
        return false;
    }

    private bool EditLayer(Mat image, VoxelObjectSelection.Plane plane, bool move, bool translate)
    {
        using var source = translate ? new Mat(image, plane.Bounds) : null;
        // Keep the original exposure values when a move overlaps its own source.
        using var movedPixels = move ? source!.Clone() : null;
        if (move)
        {
            ClearLayer(image, plane.Runs);
            return PasteLayer(image, movedPixels!, plane);
        }
        return translate ? PasteLayer(image, source!, plane) : ClearLayer(image, plane.Runs);
    }

    private static bool ClearLayer(Mat image, VoxelObjectSelection.PixelRun[] runs)
    {
        var changed = false;
        foreach (var run in runs)
        {
            var pixels = image.GetRowSpanOfBytes(run.Y).Slice(run.Start, run.Length);
            if (!pixels.ContainsAnyExcept((byte)0)) continue;
            pixels.Clear();
            changed = true;
        }
        return changed;
    }

    private bool PasteLayer(Mat image, Mat source, VoxelObjectSelection.Plane plane)
    {
        var changed = false;
        foreach (var run in plane.Runs)
        {
            var pixels = source.GetReadOnlyRowSpanOfBytes(run.Y - plane.Bounds.Y)
                .Slice(run.Start - plane.Bounds.X, run.Length);
            var target = image.GetRowSpanOfBytes(run.Y + _offset.Y).Slice(run.Start + _offset.X, run.Length);
            for (var x = 0; x < pixels.Length; x++)
            {
                if (pixels[x] == 0) continue;
                target[x] = pixels[x];
                changed = true;
            }
        }
        return changed;
    }
}
