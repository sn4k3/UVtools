# UVtools AvaloniaUI controls

[![Nuget](https://img.shields.io/nuget/v/UVtools.AvaloniaControls?style=flat-square)](https://www.nuget.org/packages/UVtools.AvaloniaControls)

Targets `net8.0`, `net9.0` and `net10.0`.

## Usage

In your `app.axaml` add:

```xml
<Application.Styles>
    <StyleInclude Source="avares://UVtools.AvaloniaControls/Controls.axaml"/>
</Application.Styles>
```

And the namespace where the controls are used:

```xml
xmlns:uvtava="clr-namespace:UVtools.AvaloniaControls;assembly=UVtools.AvaloniaControls"
```

## AdvancedImageBox

An image viewer to pan, zoom, select and inspect images of any size, built to display huge images
(for example 16K resin printer layers) without processing them.

- Port of: https://github.com/cyotek/Cyotek.Windows.Forms.ImageBox
- Demo: https://youtu.be/bIr6P4dDlHc

```xml
<uvtava:AdvancedImageBox Name="ImageBox"
                         BitmapSamplingMode="Auto"
                         ShowScaleBar="True"
                         ShowMinimap="True" />
```

Every feature below is opt-in: with the default property values the control draws exactly as before.

### Core features

- Size modes: `Normal` (zoom), `Fit` and `Stretch`
- Pan and zoom with the mouse, the keyboard and zoom levels, `ZoomToFit()`, `ZoomToRegion()`, `CenterAt()`
- Region selection (`SelectionRegion`, `SelectionMode`) and `GetSelectedBitmap()`
- Pixel grid when zoomed in, checkerboard background grid
- Tracker (cursor) images that follow the pointer and the zoom
- The coordinates of the public API (`PointToImage`, `GetOffsetPoint`, `GetOffsetRectangle`, `CenterAt`,
  `ZoomToRegion`, `SelectionRegion`, `GetSourceImageRegion`, overlays...) are always in pixels of the source image,
  whatever the rotation, the flips and the pixel aspect ratio

### Non-square pixels

`PixelAspectRatio` is the physical height of one source pixel divided by its width (default `1`). The image is
stretched when drawing, it is never resampled. `ZoomFactorX` and `ZoomFactorY` are the zoom factors of each axis.

### Rotation and mirroring

`ImageRotation` (`None`, `Rotate90Clockwise`, `Rotate180`, `Rotate90CounterClockwise`), `FlipHorizontal` and
`FlipVertical` display the image rotated and mirrored as a drawing transform, the image is not processed. The image
is flipped first and then rotated. Changing them keeps the zoom and the same pixel at the center of the view.
`GetSelectedBitmap(true)` returns the selection as it is displayed.

### Vector overlays

`Overlays` draws vector items over the image with coordinates in source pixels. They follow the zoom, the scroll,
the rotation, the flips and the aspect ratio, while the stroke thickness, the marker size and the font size are in
screen pixels, and the text is always upright.

- `ImageBoxLineOverlay`, `ImageBoxRectangleOverlay`, `ImageBoxEllipseOverlay`, `ImageBoxPolylineOverlay` (polygon
  when closed), `ImageBoxMarkerOverlay` (`Plus`, `Cross`, `Circle`, `Square`) and `ImageBoxTextOverlay`
- `Stroke`, `StrokeThickness`, `Fill`, `Opacity`, `IsVisible`, changing them redraws the control
- `ReplaceAll()` and `AddRange()` redraw once, items outside of the viewport are skipped, `ShowOverlays` hides all

```csharp
ImageBox.Overlays.ReplaceAll([
    new ImageBoxRectangleOverlay(new Rect(100, 100, 400, 250)) { Stroke = Brushes.Lime, StrokeThickness = 2 },
    new ImageBoxMarkerOverlay(new Point(300, 225), ImageBoxMarkerStyle.Cross) { Stroke = Brushes.Red },
    new ImageBoxTextOverlay(new Point(100, 90), "Part A", ImageBoxTextAnchor.BottomLeft) { FontSize = 14 }
]);
```

### Updating the pixels in place

After writing the pixels of the same `WriteableBitmap` that is displayed, call `InvalidateImage()` to redraw it
without touching the zoom, the scroll position or the centering. Reusing one bitmap avoids allocating a new one for
every frame of a huge image.

### Smooth zoom out

`BitmapSamplingMode`: `Nearest` (default, sharp pixels at any zoom), `Auto` (sharp pixels when zoomed in, averaged
when zoomed out) and `Smooth`. A zoomed out image is drawn from cached power-of-two reductions, built once per image
content, so panning and zooming cost the same as nearest sampling and huge images don't alias or shimmer.

### Grayscale images

`GrayscaleImage` displays an 8-bit image without expanding it to 32-bit: a quarter of the memory and of the copy.
It supports an optional 256 entry color map applied when drawing, and every feature of the control works with it.

```csharp
var image = new GrayscaleImage(width, height);
image.CopyFrom(matDataPointer, matStep);          // Or: using var pixels = image.Lock(); write pixels.Span
image.SetColorMap(palette);                       // Optional, 256 colors
ImageBox.GrayscaleImage = image;                  // Displayed instead of Image while set
```

It is drawn with Skia, without copies, and falls back to a 32-bit copy for other render backends. The control only
references `Avalonia.Skia` to build, so it is not a new dependency of the package.

### Composite layers (onion skin, comparisons)

`CompositeLayers` draws more images over or under the main one with the same transform and sampling:
`ImageBoxCompositeLayer` with a `GrayscaleImage` or a `Bitmap`, `Tint`, `Opacity`, `Placement` (`Above`, `Below`),
`Blend` (`Normal`, `Plus`, `Screen`, `Multiply`, `Lighten`, `Darken`) and `Alpha` (`Luminance` uses the pixel values
as the alpha with the tint as the color, which is what an onion skin needs).

`CompareSplitPosition` (0 to 1, `null` to disable) draws a vertical split line that the user drags, and the layers
with a `CompareSide` of `Left` or `Right` are only drawn on their side of it: a before/after comparison slider.

### Rulers and scale bar

- `ShowRulers`: rulers along the top and left edges with round steps that adapt to the zoom and a mark that follows
  the pointer, drawn over the viewport without changing it
- `ShowScaleBar` with `ScaleBarCorner`: a bar with a round physical length
- `PixelWidth` and `PixelHeight` are the physical size of one source pixel, in `UnitName` (default `mm`). With `0`
  the rulers and the scale bar show pixels
- `RulerSize`, `RulerBackground` and `RulerForeground`

### Minimap

`ShowMinimap` draws a thumbnail of the whole image, with the visible part marked, while the image is zoomed in.
Click or drag on it to move the view, it doesn't start a pan or a selection. `MinimapCorner`, `MinimapSize`,
`MinimapOpacity` and `MinimapViewportColor` customize it.

`MinimapBounds` and `ScaleBarBounds` give their bounds in the control, empty when they are not shown, to place other
elements over the control without covering them.

### Touch and touchpad

`EnableTouchGestures` (default on): pinch to zoom around the fingers with a two finger pan, and the magnify gesture of
touchpads, respecting the zoom limits. The mouse is not affected.

## ExtendedNumericUpDown
  Extended the NumericUpDown with more features
  - Features:
    - Initial value with a reset button
    - Value unit label

## IndexSelector
  Allow to choose an index from a collection count and display the selected number
  - Features:
    - SelectedIndex
    - SelectedNumber
    - Count

## GroupBox
Similar to `GroupBox` of WinForms, it contain an `Header` and `Content`

## ScrollGroupBox
Same as `GroupBox` but content is inside a `ScrollViewer`
