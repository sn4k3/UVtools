/*
 *                               The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so.
 */

using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace UVtools.AvaloniaControls;

public partial class AdvancedImageBox
{
    #region Decorations

    /// <summary>
    /// Determines in which corner of the viewport an element of the <see cref="AdvancedImageBox"/>, like the minimap
    /// and the scale bar, is drawn.
    /// </summary>
    public enum Corners
    {
        /// <summary>The top-left corner.</summary>
        [Description("↖ Top Left")]
        TopLeft,

        /// <summary>The top-right corner.</summary>
        [Description("↗ Top Right")]
        TopRight,

        /// <summary>The bottom-left corner.</summary>
        [Description("↙ Bottom Left")]
        BottomLeft,

        /// <summary>The bottom-right corner.</summary>
        [Description("↘ Bottom Right")]
        BottomRight,
    }

    private enum ChromeDrags : byte
    {
        None,
        Minimap,

        [Description("Compare Split")]
        CompareSplit,
    }

    /// <summary>The distance in control units from the edges of the viewport to the minimap and the scale bar.</summary>
    private const double DecorationMargin = 8;

    /// <summary>The distance in control units, from the compare split line, where it can be grabbed.</summary>
    private const double CompareSplitGrabDistance = 8;

    private ChromeDrags _chromeDrag;
    private ImmutablePen? _compareSplitPen;
    private ImmutablePen? _compareSplitOutlinePen;

    /// <summary>
    /// Gets if something drawn over the image needs the pointer position to be drawn again when it changes.
    /// </summary>
    private bool NeedsPointerRender => ShowRulers;

    /// <summary>
    /// Draws what is over the image and the selection, which doesn't modify the image: the compare split, the
    /// minimap, the scale bar and the rulers. It is called with the clip of the viewport.
    /// </summary>
    private void RenderDecorations(DrawingContext context)
    {
        var showSplit = CompareSplitPosition.HasValue;
        var showMinimap = ShowMinimap;
        var showScaleBar = ShowScaleBar;
        var showRulers = ShowRulers;
        if (!showSplit && !showMinimap && !showScaleBar && !showRulers)
            return;

        if (
            !TryGetDisplayMapping(
                out var orientation,
                out var imageSize,
                out var imageViewPort,
                out var origin,
                out var scaleX,
                out var scaleY
            )
        )
        {
            return;
        }

        if (showSplit)
            RenderCompareSplit(context, orientation, imageSize, imageViewPort, origin, scaleX);
        if (showMinimap)
            RenderMinimap(context, orientation, imageSize);
        if (showScaleBar)
            RenderScaleBar(context, orientation);
        if (showRulers)
            RenderRulers(context, orientation, imageSize, origin, scaleX, scaleY);
    }

    /// <summary>
    /// Gets the inset from the top-left of the viewport which is covered by the rulers.
    /// </summary>
    private Vector GetRulerInsets()
    {
        return ShowRulers ? new Vector(RulerSize * 2, RulerSize) : default;
    }

    /// <summary>
    /// Converts a rectangle in the coordinates of the viewport to the coordinates of the control.
    /// </summary>
    private Rect ViewportToControl(Rect rect)
    {
        var viewPort = ViewPort;
        if (viewPort is null)
            return rect;
        var topLeft = viewPort.TranslatePoint(rect.TopLeft, this);
        return topLeft.HasValue ? new Rect(topLeft.Value, rect.Size) : rect;
    }

    /// <summary>
    /// Gets the position of the corner of a box of the given size inside the viewport, over the margin and the rulers.
    /// </summary>
    private Point GetCornerPosition(Corners corner, Size size)
    {
        var viewport = Viewport;
        var insets = GetRulerInsets();
        var left = DecorationMargin + insets.X;
        var top = DecorationMargin + insets.Y;
        var right = viewport.Width - DecorationMargin - size.Width;
        var bottom = viewport.Height - DecorationMargin - size.Height;

        return corner switch
        {
            Corners.TopLeft => new Point(left, top),
            Corners.TopRight => new Point(right, top),
            Corners.BottomLeft => new Point(left, bottom),
            _ => new Point(right, bottom),
        };
    }

    #endregion

    #region Compare split

    /// <summary>
    /// Gets the position in the control of the compare split line.
    /// </summary>
    private bool TryGetCompareLine(out double lineX)
    {
        lineX = 0;
        var split = CompareSplitPosition;
        if (split is null)
            return false;
        if (
            !TryGetDisplayMapping(
                out var orientation,
                out var imageSize,
                out _,
                out var origin,
                out var scaleX,
                out _
            )
        )
        {
            return false;
        }

        lineX = origin.X + split.Value * orientation.GetViewSize(imageSize).Width * scaleX;
        return true;
    }

    private void RenderCompareSplit(
        DrawingContext context,
        ViewOrientation orientation,
        Size imageSize,
        Rect imageViewPort,
        Point origin,
        double scaleX
    )
    {
        var split = CompareSplitPosition;
        if (split is null || imageViewPort.Width <= 0 || imageViewPort.Height <= 0)
            return;

        var lineX = origin.X + split.Value * orientation.GetViewSize(imageSize).Width * scaleX;
        var viewport = Viewport;
        if (lineX < -2 || lineX > viewport.Width + 2)
            return;

        var pen = _compareSplitPen ??= new ImmutablePen(Brushes.White.ToImmutable(), 2);
        var outline = _compareSplitOutlinePen ??= new ImmutablePen(
            new ImmutableSolidColorBrush(Color.FromArgb(160, 0, 0, 0)),
            4
        );
        var top = imageViewPort.Top;
        var bottom = imageViewPort.Bottom;
        context.DrawLine(outline, new Point(lineX, top), new Point(lineX, bottom));
        context.DrawLine(pen, new Point(lineX, top), new Point(lineX, bottom));

        // The grip, in the middle of the visible part of the line
        var middle = (top + bottom) / 2;
        var grip = new Rect(lineX - 5, middle - 14, 10, 28);
        context.DrawRectangle(Brushes.White, outline, new RoundedRect(grip, 4));
    }

    private bool HitCompareSplit(Point location)
    {
        if (!TryGetCompareLine(out var lineX))
            return false;
        var imageViewPort = GetImageViewPort();
        return imageViewPort.Width > 0
            && imageViewPort.Height > 0
            && location.Y >= imageViewPort.Top
            && location.Y <= imageViewPort.Bottom
            && Math.Abs(location.X - lineX) <= CompareSplitGrabDistance;
    }

    private void SetCompareSplitFromPoint(Point location)
    {
        if (
            !TryGetDisplayMapping(
                out var orientation,
                out var imageSize,
                out _,
                out var origin,
                out var scaleX,
                out _
            )
        )
        {
            return;
        }

        var width = orientation.GetViewSize(imageSize).Width * scaleX;
        if (width <= 0)
            return;
        CompareSplitPosition = (location.X - origin.X) / width;
    }

    #endregion

    #region Pointer interaction of the decorations

    /// <summary>
    /// Starts dragging the minimap or the compare split when the pointer was pressed over them, which then takes the
    /// pointer instead of the pan and the selection.
    /// </summary>
    private bool TryBeginChromeDrag(PointerPressedEventArgs e, PointerPoint pointer, Point location)
    {
        if (!pointer.Properties.IsLeftButtonPressed || !IsImageLoaded)
            return false;

        ChromeDrags drag;
        if (CompareSplitPosition.HasValue && HitCompareSplit(location))
        {
            drag = ChromeDrags.CompareSplit;
        }
        else if (
            TryGetMinimapLayout(out var minimapRect, out _)
            && minimapRect.Inflate(2).Contains(location)
        )
        {
            drag = ChromeDrags.Minimap;
        }
        else
        {
            return false;
        }

        _chromeDrag = drag;
        _activePointer = e.Pointer;
        e.Pointer.Capture(ViewPort);
        e.Handled = true;
        UpdateChromeDrag(location);
        return true;
    }

    private void UpdateChromeDrag(Point location)
    {
        switch (_chromeDrag)
        {
            case ChromeDrags.Minimap:
                CenterAtMinimapPoint(location);
                break;
            case ChromeDrags.CompareSplit:
                SetCompareSplitFromPoint(location);
                break;
        }
    }

    /// <summary>
    /// Ends a drag of the decorations.
    /// </summary>
    /// <returns><see langword="true"/> when there was one.</returns>
    private bool EndChromeDrag(PointerReleasedEventArgs? e)
    {
        if (_chromeDrag == ChromeDrags.None)
            return false;
        _chromeDrag = ChromeDrags.None;
        if (e is not null)
        {
            e.Pointer.Capture(null);
            e.Handled = true;
        }

        return true;
    }

    #endregion
}
