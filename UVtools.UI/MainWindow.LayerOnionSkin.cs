/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Threading.Tasks;
using Avalonia.Media;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EmguExtensions;
using UVtools.AvaloniaControls;
using UVtools.Core;
using UVtools.Core.Layers;

namespace UVtools.UI;

/// <summary>
/// The GPU onion skin of the layer preview. The previous and next layers are drawn by the layer image box as tinted
/// composite layers over the current layer, which keeps being displayed as the 8-bit image it is, instead of the CPU
/// mixing the layers into a 32-bit image pixel by pixel.
/// </summary>
/// <remarks>
/// It is a simplified version of the CPU onion skin (the default): the ghosts of the layers use the same colors and
/// priority (-1 over -2 over -3 over +1 over +2 over +3), but the current layer is always white, there is no yellow
/// for the pixels that don't have a layer below, and no magenta for the pixels that have layers below and above. Those
/// need to know the pixels of more than one layer at once, which only the CPU path does.
/// </remarks>
public partial class MainWindow
{
    /// <summary>The ghost layers: -1, -2, -3, +1, +2 and +3.</summary>
    private const int OnionSkinSlots = 6;

    // The colors of the CPU onion skin, by slot
    private static readonly Color[] OnionSkinColors =
    [
        Color.FromRgb(240, 50, 50), // -1
        Color.FromRgb(215, 80, 20), // -2
        Color.FromRgb(160, 25, 25), // -3
        Color.FromRgb(30, 215, 255), // +1
        Color.FromRgb(25, 120, 215), // +2
        Color.FromRgb(25, 50, 160) // +3
    ];

    // The images of the ghost layers by slot, they are written again for each layer shown while their size doesn't change
    private readonly GrayscaleImage?[] _onionSkinImages = new GrayscaleImage?[OnionSkinSlots];

    // The layers of the layer image box, in drawing order: the lowest priority first and the current layer last
    private ImageBoxCompositeLayer[]? _onionSkinLayers;

    /// <summary>What the composite layers of the layer image box are drawing.</summary>
    private enum GpuDifferenceKind : byte
    {
        None,
        OnionSkin,
        Difference
    }

    private GpuDifferenceKind _gpuDifferenceKind;

    // The slots of the GPU difference images: the pixels only on the previous layer, only on the next and on both
    private const int DifferencePreviousSlot = 0;
    private const int DifferenceNextSlot = 1;
    private const int DifferenceBothSlot = 2;

    // The area of the GPU difference images which has pixels, the rest is black
    private System.Drawing.Rectangle _gpuDifferenceDirtyRect;

    /// <summary>
    /// Gets or sets if the onion skin of the layer preview is drawn by the GPU, faster but with simplified colors, see
    /// <see cref="UseGpuOnionSkin"/>.
    /// </summary>
    public bool IsLayerDiffOnionSkinGpu
    {
        get => Settings.LayerPreview.OnionSkinGpu;
        set
        {
            if (Settings.LayerPreview.OnionSkinGpu == value) return;
            Settings.LayerPreview.OnionSkinGpu = value;
            RaisePropertyChanged();
            if (IsFileLoaded && _showLayerImageDifference)
            {
                ShowLayer();
            }
        }
    }

    /// <summary>
    /// Gets if the difference is shown as the onion skin and it is set to be drawn by the GPU.
    /// </summary>
    private bool UseGpuOnionSkin()
    {
        return _showLayerImageDifference
               && Settings.LayerPreview.OnionSkinGpu
               && Settings.LayerPreview.DifferenceMode ==
               UserSettings.LayerPreviewUserSettings.LayerDifferenceMode.OnionSkin;
    }

    /// <summary>
    /// Gets if the difference is shown as the difference or the similarity with the previous and next layers, and it
    /// is set to be drawn by the GPU.
    /// </summary>
    private bool UseGpuLayerDifference()
    {
        return _showLayerImageDifference
               && Settings.LayerPreview.OnionSkinGpu
               && Settings.LayerPreview.DifferenceMode is
                   UserSettings.LayerPreviewUserSettings.LayerDifferenceMode.Difference
                   or UserSettings.LayerPreviewUserSettings.LayerDifferenceMode.Similarity;
    }

    /// <summary>
    /// Sets the composite layers of the layer image box to the previous and next layers of the current one, or removes
    /// them when the GPU onion skin or difference doesn't apply to what the box displays.
    /// </summary>
    private void UpdateOnionSkinLayers()
    {
        var main = LayerImageBox.GrayscaleImage;
        var kind = UseGpuOnionSkin()
            ? GpuDifferenceKind.OnionSkin
            : UseGpuLayerDifference()
                ? GpuDifferenceKind.Difference
                : GpuDifferenceKind.None;
        if (main is null || SlicerFile is null || kind == GpuDifferenceKind.None
            || _showLayerOutlineEdgeDetection || _showLayerOutlineDistanceDetection || _showLayerOutlineSkeletonize)
        {
            ClearOnionSkinLayers();
            return;
        }

        if (_gpuDifferenceKind != kind) ClearOnionSkinLayers();
        _gpuDifferenceKind = kind;

        if (kind == GpuDifferenceKind.Difference)
        {
            UpdateGpuDifferenceLayers(main);
            return;
        }

        // The same layers as the CPU onion skin
        var onionLayers = Settings.LayerPreview.OnionSkinLayers;
        var prevCount = onionLayers switch
        {
            4 => 2,
            5 => 2,
            6 => 3,
            7 => 3,
            _ => 1
        };
        var nextCount = onionLayers switch
        {
            5 => 2,
            6 => 2,
            7 => 3,
            _ => 1
        };

        var slicerFile = SlicerFile;
        var width = main.Width;
        var height = main.Height;
        var layers = new Layer?[OnionSkinSlots];
        for (var slot = 0; slot < OnionSkinSlots; slot++)
        {
            var isPrevious = slot < 3;
            var distance = isPrevious ? slot + 1 : slot - 2;
            if (distance > (isPrevious ? prevCount : nextCount)) continue;

            if (isPrevious)
            {
                if (_actualLayer < distance) continue;
                layers[slot] = slicerFile[_actualLayer - (uint)distance];
            }
            else
            {
                if (_actualLayer + distance > slicerFile.LastLayerIndex) continue;
                layers[slot] = slicerFile[_actualLayer + (uint)distance];
            }
        }

        var visible = new bool[OnionSkinSlots];
        var replaced = new GrayscaleImage?[OnionSkinSlots];
        Parallel.For(0, OnionSkinSlots, CoreSettings.ParallelOptions, slot =>
        {
            var layer = layers[slot];
            if (layer is null || layer.IsEmpty) return;

            using var mat = layer.LayerMat;
            if (mat is null || mat.IsEmpty || mat.Width != width || mat.Height != height) return;

            var image = _onionSkinImages[slot];
            if (image is null || image.Width != width || image.Height != height)
            {
                replaced[slot] = image;
                image = new GrayscaleImage(width, height);
                _onionSkinImages[slot] = image;
            }

            image.CopyFrom(mat.DataPointer, mat.Step);
            visible[slot] = true;
        });

        var onionSkinLayers = _onionSkinLayers;
        if (onionSkinLayers is null)
        {
            // Drawing order: +3, +2, +1, -3, -2, -1, then the current layer over all of them
            ImageBoxCompositeLayer[] created =
            [
                CreateOnionSkinLayer(5), CreateOnionSkinLayer(4), CreateOnionSkinLayer(3), CreateOnionSkinLayer(2),
                CreateOnionSkinLayer(1), CreateOnionSkinLayer(0),
                new ImageBoxCompositeLayer { Blend = ImageBoxCompositeBlend.Lighten }
            ];
            LayerImageBox.CompositeLayers.ReplaceAll(created);
            _onionSkinLayers = onionSkinLayers = created;
        }

        for (var i = 0; i < OnionSkinSlots; i++)
        {
            var slot = OnionSkinSlots - 1 - i;
            var layer = onionSkinLayers[i];
            layer.GrayscaleImage = _onionSkinImages[slot];
            layer.IsVisible = visible[slot];
        }

        // The current layer is lightened over the ghosts so they only show where it has no pixels
        onionSkinLayers[OnionSkinSlots].GrayscaleImage = main;

        // The images that were replaced are not displayed anymore
        foreach (var image in replaced)
        {
            image?.Dispose();
        }
    }

    private static ImageBoxCompositeLayer CreateOnionSkinLayer(int slot)
    {
        return new ImageBoxCompositeLayer
        {
            Tint = OnionSkinColors[slot],
            Alpha = ImageBoxCompositeAlpha.Luminance
        };
    }

    /// <summary>
    /// Draws the difference or the similarity with the previous and next layers with the GPU. The pixels of the
    /// previous layer, of the next and of both are split into three images by the CPU with 8-bit masks, which are then
    /// drawn tinted over the current layer. Where the current layer is black (difference) the result is the same as the
    /// CPU path; where it has pixels (similarity) the anti-aliased pixels of the other layers blend with it instead of
    /// replacing it.
    /// </summary>
    private void UpdateGpuDifferenceLayers(GrayscaleImage main)
    {
        var slicerFile = SlicerFile!;
        var current = LayerCache.Image!;
        var width = main.Width;
        var height = main.Height;

        var previousLayer = _actualLayer > 0 ? slicerFile[_actualLayer - 1] : null;
        var nextLayer = _actualLayer < slicerFile.LastLayerIndex ? slicerFile[_actualLayer + 1] : null;
        if (previousLayer is { IsEmpty: true }) previousLayer = null;
        if (nextLayer is { IsEmpty: true }) nextLayer = null;

        var onionSkinLayers = _onionSkinLayers;
        if (onionSkinLayers is null)
        {
            // Drawing order: next, previous, both
            ImageBoxCompositeLayer[] created =
            [
                CreateDifferenceLayer(Settings.LayerPreview.NextLayerDifferenceColor.ToAvalonia()),
                CreateDifferenceLayer(Settings.LayerPreview.PreviousLayerDifferenceColor.ToAvalonia()),
                CreateDifferenceLayer(Settings.LayerPreview.BothLayerDifferenceColor.ToAvalonia())
            ];
            LayerImageBox.CompositeLayers.ReplaceAll(created);
            _onionSkinLayers = onionSkinLayers = created;
            _gpuDifferenceDirtyRect = System.Drawing.Rectangle.Empty;
        }
        else
        {
            // The colors may have been changed in the settings
            onionSkinLayers[0].Tint = Settings.LayerPreview.NextLayerDifferenceColor.ToAvalonia();
            onionSkinLayers[1].Tint = Settings.LayerPreview.PreviousLayerDifferenceColor.ToAvalonia();
            onionSkinLayers[2].Tint = Settings.LayerPreview.BothLayerDifferenceColor.ToAvalonia();
        }

        for (var slot = DifferencePreviousSlot; slot <= DifferenceBothSlot; slot++)
        {
            var image = _onionSkinImages[slot];
            if (image is not null && image.Width == width && image.Height == height) continue;
            image?.Dispose();
            _onionSkinImages[slot] = new GrayscaleImage(width, height);
            _gpuDifferenceDirtyRect = new System.Drawing.Rectangle(0, 0, width, height);
        }

        using var previousMat = previousLayer?.LayerMat;
        using var nextMat = nextLayer?.LayerMat;
        var previous = previousMat is not null && previousMat.Width == width && previousMat.Height == height
            ? previousMat
            : null;
        var next = nextMat is not null && nextMat.Width == width && nextMat.Height == height ? nextMat : null;

        // Only the area of the previous and next layers can be colored
        var rect = System.Drawing.Rectangle.Empty;
        if (previous is not null) rect = previousLayer!.BoundingRectangle;
        if (next is not null)
        {
            rect = rect.IsEmpty
                ? nextLayer!.BoundingRectangle
                : System.Drawing.Rectangle.Union(rect, nextLayer!.BoundingRectangle);
        }

        rect.Intersect(new System.Drawing.Rectangle(0, 0, width, height));
        if (current.Width != width || current.Height != height) rect = System.Drawing.Rectangle.Empty;

        var locks = new GrayscaleImageLock[3];
        var targets = new Mat?[3];
        var locked = 0;
        try
        {
            for (var slot = DifferencePreviousSlot; slot <= DifferenceBothSlot; slot++)
            {
                locks[slot] = _onionSkinImages[slot]!.Lock();
                locked++;
                var target = targets[slot] =
                    new Mat(height, width, DepthType.Cv8U, 1, locks[slot].Address, locks[slot].RowBytes);

                // Clear what the previous layer left
                var dirty = System.Drawing.Rectangle.Union(_gpuDifferenceDirtyRect, rect);
                if (dirty.IsEmpty) continue;
                using var dirtyRoi = new Mat(target, dirty);
                dirtyRoi.SetTo(new MCvScalar(0));
            }

            if (!rect.IsEmpty)
            {
                using var currentRoi = new Mat(current, rect);
                using var area = new Mat();
                CvInvoke.Compare(currentRoi, new ScalarArray(0), area,
                    Settings.LayerPreview.LayerDifferenceHighlightSimilarityInstead ? CmpType.GreaterThan : CmpType.Equal);

                using var previousRoi = previous is null ? null : new Mat(previous, rect);
                using var nextRoi = next is null ? null : new Mat(next, rect);
                using var previousMask = new Mat();
                using var nextMask = new Mat();
                using var bothMask = new Mat();

                if (previousRoi is not null)
                {
                    CvInvoke.Compare(previousRoi, new ScalarArray(0), previousMask, CmpType.GreaterThan);
                    CvInvoke.BitwiseAnd(previousMask, area, previousMask);
                }

                if (nextRoi is not null)
                {
                    CvInvoke.Compare(nextRoi, new ScalarArray(0), nextMask, CmpType.GreaterThan);
                    CvInvoke.BitwiseAnd(nextMask, area, nextMask);
                }

                if (previousRoi is not null && nextRoi is not null)
                {
                    // The pixels on both layers get the both color with the brightest of them
                    CvInvoke.BitwiseAnd(previousMask, nextMask, bothMask);
                    using var brightest = new Mat();
                    CvInvoke.Max(previousRoi, nextRoi, brightest);
                    using var bothTarget = new Mat(targets[DifferenceBothSlot]!, rect);
                    brightest.CopyTo(bothTarget, bothMask);

                    CvInvoke.BitwiseNot(bothMask, bothMask);
                    CvInvoke.BitwiseAnd(previousMask, bothMask, previousMask);
                    CvInvoke.BitwiseAnd(nextMask, bothMask, nextMask);
                }

                if (previousRoi is not null)
                {
                    using var previousTarget = new Mat(targets[DifferencePreviousSlot]!, rect);
                    previousRoi.CopyTo(previousTarget, previousMask);
                }

                if (nextRoi is not null)
                {
                    using var nextTarget = new Mat(targets[DifferenceNextSlot]!, rect);
                    nextRoi.CopyTo(nextTarget, nextMask);
                }
            }

            _gpuDifferenceDirtyRect = rect;
        }
        finally
        {
            for (var slot = 0; slot < locked; slot++)
            {
                targets[slot]?.Dispose();
                locks[slot].Dispose();
            }
        }

        onionSkinLayers[0].GrayscaleImage = _onionSkinImages[DifferenceNextSlot];
        onionSkinLayers[1].GrayscaleImage = _onionSkinImages[DifferencePreviousSlot];
        onionSkinLayers[2].GrayscaleImage = _onionSkinImages[DifferenceBothSlot];
        onionSkinLayers[0].IsVisible = next is not null;
        onionSkinLayers[1].IsVisible = previous is not null;
        onionSkinLayers[2].IsVisible = previous is not null && next is not null;
    }

    private static ImageBoxCompositeLayer CreateDifferenceLayer(Color tint)
    {
        return new ImageBoxCompositeLayer
        {
            Tint = tint,
            Alpha = ImageBoxCompositeAlpha.Luminance
        };
    }

    /// <summary>
    /// Removes the composite layers of the onion skin or difference from the layer image box and frees their images.
    /// </summary>
    private void ClearOnionSkinLayers()
    {
        _gpuDifferenceKind = GpuDifferenceKind.None;
        if (_onionSkinLayers is null) return;

        LayerImageBox.CompositeLayers.Clear();
        _onionSkinLayers = null;
        _gpuDifferenceDirtyRect = System.Drawing.Rectangle.Empty;
        for (var slot = 0; slot < OnionSkinSlots; slot++)
        {
            _onionSkinImages[slot]?.Dispose();
            _onionSkinImages[slot] = null;
        }
    }
}
