/*
 *                               The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so.
 */

using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace UVtools.AvaloniaControls;

public partial class AdvancedImageBox
{
    #region Composite layers and compare split

    private readonly ImageBoxCompositeLayerCollection _compositeLayers;

    /// <summary>
    /// The reductions of the image drawn by the main image, see <see cref="ReducedImageCache"/>.
    /// </summary>
    private readonly ReducedImageCache _reducedCache = new();

    /// <summary>
    /// Gets the extra images drawn over or under the main image with the same transform, in the order of the
    /// collection.
    /// </summary>
    /// <remarks>
    /// <para>Use them for an onion skin (the previous and next layers of a stack, tinted), a comparison between two
    /// images, or any image that has to follow the main one. They are drawn by the GPU or the Skia raster pipeline,
    /// nothing is combined on the CPU, and a <see cref="GrayscaleImage"/> layer keeps being 8-bit. See
    /// <see cref="ImageBoxCompositeLayer"/> for what each type of image supports.</para>
    /// <para>The layers follow the zoom, the scroll, the <see cref="ImageRotation"/>, the flips, the
    /// <see cref="PixelAspectRatio"/> and the size mode of the control, and are sampled like the main image, see
    /// <see cref="BitmapSamplingMode"/>. They are drawn after the main image, over it, unless their
    /// <see cref="ImageBoxCompositeLayer.Placement"/> is <see cref="ImageBoxCompositePlacement.Below"/>, and before the
    /// tracker image, the pixel grid and the <see cref="Overlays"/>. The default is an empty collection, which draws
    /// exactly what the control drew before.</para>
    /// <para>The layers are not part of the minimap, nor of the selected bitmap.</para>
    /// </remarks>
    public ImageBoxCompositeLayerCollection CompositeLayers => _compositeLayers;

    public static readonly StyledProperty<double?> CompareSplitPositionProperty =
        AvaloniaProperty.Register<AdvancedImageBox, double?>(
            nameof(CompareSplitPosition),
            null,
            coerce: CoerceCompareSplitPosition
        );

    private static double? CoerceCompareSplitPosition(AvaloniaObject sender, double? value)
    {
        return value is null or double.NaN ? null : Math.Clamp(value.Value, 0, 1);
    }

    /// <summary>
    /// Gets or sets the position of the compare split, from 0 to 1 as a fraction of the width of the displayed image,
    /// or <see langword="null"/> (default) when there is no split.
    /// </summary>
    /// <remarks>
    /// <para>The split is a vertical line over the image, in the displayed image, that follows its zoom, scroll,
    /// rotation and flips. The <see cref="CompositeLayers"/> with an <see cref="ImageBoxCompositeLayer.CompareSide"/>
    /// are drawn on their side of it only: the main image on the left and an opaque layer with
    /// <see cref="ImageBoxCompareSide.Right"/> on the right is a before/after comparison. Layers with no side ignore
    /// the split, and the layers with a side are not drawn while it is <see langword="null"/>.</para>
    /// <para>The user can drag the line with the left mouse button or a finger. The drag doesn't pan, select or reach
    /// the pointer events of the control, and it changes this property.</para>
    /// <para>It is a fraction of the displayed image, so when the image is rotated by 90 degrees it is a fraction of
    /// the source height. The split is only available while the image is displayed in
    /// <see cref="SizeModes.Normal"/> or <see cref="SizeModes.Fit"/> mode.</para>
    /// </remarks>
    public double? CompareSplitPosition
    {
        get => GetValue(CompareSplitPositionProperty);
        set => SetValue(CompareSplitPositionProperty, value);
    }

    /// <summary>
    /// Gets how the main image maps to the control.
    /// </summary>
    /// <param name="orientation">The display orientation.</param>
    /// <param name="imageSize">The size of the source image.</param>
    /// <param name="imageViewPort">The visible part of the displayed image in the control.</param>
    /// <param name="origin">The position in the control of the top-left corner of the displayed image.</param>
    /// <param name="scaleX">Control units per pixel of the displayed image, horizontally.</param>
    /// <param name="scaleY">Control units per pixel of the displayed image, vertically.</param>
    /// <returns><see langword="false"/> when there is no image or nothing to map.</returns>
    private bool TryGetDisplayMapping(
        out ViewOrientation orientation,
        out Size imageSize,
        out Rect imageViewPort,
        out Point origin,
        out double scaleX,
        out double scaleY
    )
    {
        orientation = _orientation;
        imageSize = default;
        origin = default;
        scaleX = scaleY = 1;
        imageViewPort = default;

        var image = DisplayImage;
        if (image is null)
            return false;

        imageSize = image.Size;
        imageViewPort = GetImageViewPort();
        return TryGetDisplayMapping(
            orientation,
            imageSize,
            imageViewPort,
            out origin,
            out scaleX,
            out scaleY
        );
    }

    private bool TryGetDisplayMapping(
        ViewOrientation orientation,
        Size imageSize,
        Rect imageViewPort,
        out Point origin,
        out double scaleX,
        out double scaleY
    )
    {
        if (SizeMode == SizeModes.Normal)
        {
            // The same mapping as GetOffsetPoint()
            origin = GetNormalImageBoundsInViewport().Position;
            scaleX = ZoomFactorX;
            scaleY = ZoomFactorY;
            return true;
        }

        // The image is stretched to the viewport rectangle, see DrawOrientedImage()
        var viewSize = orientation.GetViewSize(imageSize);
        origin = imageViewPort.Position;
        scaleX = 1;
        scaleY = 1;
        if (
            imageViewPort.Width <= 0
            || imageViewPort.Height <= 0
            || viewSize.Width <= 0
            || viewSize.Height <= 0
        )
        {
            return false;
        }

        scaleX = imageViewPort.Width / viewSize.Width;
        scaleY = imageViewPort.Height / viewSize.Height;
        return true;
    }

    /// <summary>
    /// Draws the composite layers of a placement, with the same mapping as the main image.
    /// </summary>
    private void DrawCompositeLayers(
        DrawingContext context,
        ImageBoxCompositePlacement placement,
        ViewOrientation orientation,
        Size mainSize,
        Rect viewImageRegion,
        Rect imageViewPort
    )
    {
        var layers = _compositeLayers;
        var split = CompareSplitPosition;
        var viewport = Viewport;

        for (var i = 0; i < layers.Count; i++)
        {
            var layer = layers[i];
            if (!layer.IsVisible || layer.Placement != placement || layer.Opacity <= 0)
                continue;

            var image = layer.DisplayImage;
            if (image is null)
                continue;

            if (image is GrayscaleImage { IsDisposed: true })
                continue;

            var side = layer.CompareSide;
            Rect? clip = null;
            if (side != ImageBoxCompareSide.None)
            {
                if (split is null || !TryGetCompareLine(out var lineX))
                    continue;
                lineX = Math.Clamp(lineX, 0, viewport.Width);
                clip =
                    side == ImageBoxCompareSide.Right
                        ? new Rect(lineX, 0, viewport.Width - lineX, viewport.Height)
                        : new Rect(0, 0, lineX, viewport.Height);
                if (clip.Value.Width <= 0)
                    continue;
            }

            var style = new LayerStyle(
                layer.Tint,
                layer.Opacity,
                layer.Blend,
                layer.Alpha == ImageBoxCompositeAlpha.Luminance
            );

            if (clip is { } rect)
            {
                using (context.PushClip(rect))
                {
                    DrawImageLayer(
                        context,
                        image,
                        mainSize,
                        orientation,
                        viewImageRegion,
                        imageViewPort,
                        layer.ReducedCache,
                        layer.Version,
                        in style
                    );
                }
            }
            else
            {
                DrawImageLayer(
                    context,
                    image,
                    mainSize,
                    orientation,
                    viewImageRegion,
                    imageViewPort,
                    layer.ReducedCache,
                    layer.Version,
                    in style
                );
            }
        }
    }

    /// <summary>
    /// Draws an image, which has the size of the main image or another one that covers it, with the mapping of the
    /// main image.
    /// </summary>
    private void DrawImageLayer(
        DrawingContext context,
        IImage image,
        Size mainSize,
        ViewOrientation orientation,
        Rect viewImageRegion,
        Rect imageViewPort,
        ReducedImageCache cache,
        int imageVersion,
        in LayerStyle style
    )
    {
        if (
            !TryGetDisplayMapping(
                orientation,
                mainSize,
                imageViewPort,
                out var origin,
                out var scaleX,
                out var scaleY
            )
        )
        {
            return;
        }

        DrawImageMapped(
            context,
            image,
            mainSize,
            orientation,
            origin,
            scaleX,
            scaleY,
            viewImageRegion,
            imageViewPort,
            cache,
            imageVersion,
            in style,
            false
        );
    }

    /// <summary>
    /// Releases the reductions of the composite layers, they are created again when needed.
    /// </summary>
    private void ReleaseCompositeCaches()
    {
        for (var i = 0; i < _compositeLayers.Count; i++)
        {
            _compositeLayers[i].ReleaseCache();
        }
    }

    /// <summary>
    /// Disposes the reductions of the main image and of the minimap.
    /// </summary>
    private void ClearReducedBitmaps()
    {
        _reducedCache.Clear();
        _minimapCache.Clear();
    }

    /// <summary>
    /// Applies the opacity and the blend mode of a style to the bitmaps drawn while it is alive. The tint and the
    /// alpha from luminance are not available to bitmaps.
    /// </summary>
    private readonly struct BitmapStyleScope : IDisposable
    {
        private readonly DrawingContext.PushedState _opacityState;
        private readonly DrawingContext.PushedState _blendState;
        private readonly bool _hasOpacity;
        private readonly bool _hasBlend;

        public BitmapStyleScope(DrawingContext context, in LayerStyle style)
        {
            _opacityState = default;
            _blendState = default;
            _hasOpacity = false;
            _hasBlend = false;
            if (!style.HasStyle)
                return;

            if (style.Opacity < 1)
            {
                _opacityState = context.PushOpacity(style.Opacity);
                _hasOpacity = true;
            }

            var blending = style.Blend switch
            {
                ImageBoxCompositeBlend.Plus => BitmapBlendingMode.Plus,
                ImageBoxCompositeBlend.Screen => BitmapBlendingMode.Screen,
                ImageBoxCompositeBlend.Multiply => BitmapBlendingMode.Multiply,
                ImageBoxCompositeBlend.Lighten => BitmapBlendingMode.Lighten,
                ImageBoxCompositeBlend.Darken => BitmapBlendingMode.Darken,
                _ => BitmapBlendingMode.Unspecified,
            };

            if (blending != BitmapBlendingMode.Unspecified)
            {
                _blendState = context.PushRenderOptions(
                    new RenderOptions { BitmapBlendingMode = blending }
                );
                _hasBlend = true;
            }
        }

        public void Dispose()
        {
            if (_hasBlend)
                _blendState.Dispose();
            if (_hasOpacity)
                _opacityState.Dispose();
        }
    }

    #endregion
}
