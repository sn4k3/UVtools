/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using UVtools.AvaloniaControls;
using Xunit;
using static UVtools.AvaloniaControls.AdvancedImageBox;

namespace UVtools.Tests.AvaloniaControls;

/// <summary>
/// Headless rendering checks of the <see cref="AdvancedImageBox"/> display features: rotation, flips, pixel aspect
/// ratio, overlays, in place updates, grayscale images, sampling, minimap and scale bar.
/// </summary>
public class AdvancedImageBoxTests
{
    public static IEnumerable<object[]> Orientations()
    {
        foreach (var rotation in Enum.GetValues<ImageRotations>())
        {
            foreach (var flipHorizontal in new[] { false, true })
            {
                foreach (var flipVertical in new[] { false, true })
                {
                    yield return [rotation, flipHorizontal, flipVertical];
                }
            }
        }
    }

    // An asymmetric pattern, so any wrong rotation or flip changes the frame
    private static (byte R, byte G, byte B) Pattern(int x, int y)
    {
        if (x < 4 && y < 10) return (255, 0, 0);
        if (y < 3 && x < 12) return (0, 200, 0);
        return ((byte)(x * 6), (byte)(y * 8), (byte)((x * 7 + y * 13) % 256));
    }

    /// <summary>
    /// Gets the source pixel shown at a pixel of the displayed image: the source is flipped first, then rotated.
    /// </summary>
    private static (int X, int Y) ViewToSource(int viewX, int viewY, int width, int height, ImageRotations rotation,
        bool flipHorizontal, bool flipVertical)
    {
        var (x, y) = rotation switch
        {
            ImageRotations.Rotate90Clockwise => (viewY, height - 1 - viewX),
            ImageRotations.Rotate180 => (width - 1 - viewX, height - 1 - viewY),
            ImageRotations.Rotate90CounterClockwise => (width - 1 - viewY, viewX),
            _ => (viewX, viewY)
        };
        if (flipHorizontal) x = width - 1 - x;
        if (flipVertical) y = height - 1 - y;
        return (x, y);
    }

    private static void SetOrientation(AdvancedImageBox box, ImageRotations rotation, bool flipHorizontal,
        bool flipVertical)
    {
        box.ImageRotation = rotation;
        box.FlipHorizontal = flipHorizontal;
        box.FlipVertical = flipVertical;
    }

    [Theory]
    [MemberData(nameof(Orientations))]
    public void RotationAndFlipsDrawTheSameAsTheTransformedImage(ImageRotations rotation, bool flipHorizontal,
        bool flipVertical)
    {
        Headless.Run(() =>
        {
            const int width = 40;
            const int height = 30;
            var transposed = rotation is ImageRotations.Rotate90Clockwise or ImageRotations.Rotate90CounterClockwise;
            var viewWidth = transposed ? height : width;
            var viewHeight = transposed ? width : height;

            using var source = Headless.CreateBitmap(width, height, Pattern);
            using var transformed = Headless.CreateBitmap(viewWidth, viewHeight, (x, y) =>
            {
                var (sx, sy) = ViewToSource(x, y, width, height, rotation, flipHorizontal, flipVertical);
                return Pattern(sx, sy);
            });

            var (window, box) = Headless.CreateBox(b =>
            {
                b.Image = source;
                SetOrientation(b, rotation, flipHorizontal, flipVertical);
            });
            var (referenceWindow, referenceBox) = Headless.CreateBox(b => b.Image = transformed);
            try
            {
                // An integer zoom avoids the sampling ties of nearest neighbor on the mirrored axes
                box.Zoom = 400;
                referenceBox.Zoom = 400;

                var frame = Headless.Grab(window);
                var reference = Headless.Grab(referenceWindow);
                Assert.Equal(reference.Bgra, frame.Bgra);
            }
            finally
            {
                window.Close();
                referenceWindow.Close();
            }
        });
    }

    [Theory]
    [MemberData(nameof(Orientations))]
    public void CoordinatesRoundTripInSourcePixels(ImageRotations rotation, bool flipHorizontal, bool flipVertical)
    {
        Headless.Run(() =>
        {
            using var source = Headless.CreateBitmap(400, 300, Pattern);
            var (window, box) = Headless.CreateBox(b =>
            {
                b.Image = source;
                SetOrientation(b, rotation, flipHorizontal, flipVertical);
            });
            try
            {
                foreach (var aspect in new[] { 1.0, 1.5 })
                {
                    box.PixelAspectRatio = aspect;
                    foreach (var zoom in new[] { 50, 237, 800 })
                    {
                        box.Zoom = zoom;
                        box.CenterAt(new Point(120, 210));
                        Headless.Pump();

                        foreach (var point in new[] { new Point(0, 0), new Point(120.5, 210.25), new Point(399, 299) })
                        {
                            var screen = box.GetOffsetPoint(point.X, point.Y);
                            var back = box.PointToImage(screen, false);
                            Assert.Equal(point.X, back.X, 6);
                            Assert.Equal(point.Y, back.Y, 6);
                        }
                    }
                }
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void PixelAspectRatioStretchesTheDisplayedImage()
    {
        Headless.Run(() =>
        {
            using var source = Headless.CreateBitmap(100, 80, Pattern);
            var (window, box) = Headless.CreateBox(b => b.Image = source);
            try
            {
                box.Zoom = 200;
                box.PixelAspectRatio = 1.5;
                Headless.Pump();

                var viewPort = box.GetImageViewPort();
                Assert.Equal(200, viewPort.Width, 1);
                Assert.Equal(80 * 2 * 1.5, viewPort.Height, 1);
                Assert.Equal(box.ZoomFactorX * 1.5, box.ZoomFactorY, 6);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Theory]
    [MemberData(nameof(Orientations))]
    public void OverlaysAreDrawnOnTheirSourcePixels(ImageRotations rotation, bool flipHorizontal, bool flipVertical)
    {
        Headless.Run(() =>
        {
            using var source = Headless.CreateBitmap(200, 150, (_, _) => (0, 0, 0));
            var target = new Point(50.5, 40.5);
            var (window, box) = Headless.CreateBox(b =>
            {
                b.Image = source;
                SetOrientation(b, rotation, flipHorizontal, flipVertical);
                b.Overlays.Add(new ImageBoxEllipseOverlay(target, 3) { Fill = Brushes.Red, Stroke = Brushes.Red });
            });
            try
            {
                box.Zoom = 300;
                box.CenterAt(target);
                var frame = Headless.Grab(window);

                var screen = box.TranslatePoint(box.GetOffsetPoint(target.X, target.Y), window)!.Value;
                var (b, g, r, _) = frame.At((int)screen.X, (int)screen.Y);
                Assert.True(r > 200 && g < 50 && b < 50, $"Expected red at {screen}, got R={r} G={g} B={b}");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void InvalidateImageRedrawsWithoutMovingTheView()
    {
        Headless.Run(() =>
        {
            using var source = Headless.CreateBitmap(100, 100, (_, _) => (10, 10, 10));
            var (window, box) = Headless.CreateBox(b => b.Image = source);
            try
            {
                box.Zoom = 800;
                box.CenterAt(new Point(30, 70));
                Headless.Pump();
                var zoom = box.Zoom;
                var offset = box.Offset;

                Headless.WriteBitmap(source, (_, _) => (250, 0, 0));
                box.InvalidateImage();
                var frame = Headless.Grab(window);

                Assert.Equal(zoom, box.Zoom);
                Assert.Equal(offset, box.Offset);
                // The center of the pixel, its corners are under the pixel grid at this zoom
                var center = box.TranslatePoint(box.GetOffsetPoint(30.5, 70.5), window)!.Value;
                var (_, _, r, _) = frame.At((int)center.X, (int)center.Y);
                Assert.True(r > 200, $"The new pixels were not drawn, R={r}");
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ChangingTheRotationKeepsTheSamePixelInTheCenter()
    {
        Headless.Run(() =>
        {
            using var source = Headless.CreateBitmap(100, 100, Pattern);
            var (window, box) = Headless.CreateBox(b => b.Image = source);
            try
            {
                box.Zoom = 800;
                box.CenterAt(new Point(30, 70));
                Headless.Pump();
                var zoom = box.Zoom;

                foreach (var rotation in new[]
                         {
                             ImageRotations.Rotate90Clockwise, ImageRotations.Rotate180,
                             ImageRotations.Rotate90CounterClockwise
                         })
                {
                    box.ImageRotation = rotation;
                    box.FlipHorizontal = !box.FlipHorizontal;
                    Headless.Pump();

                    var region = box.GetSourceImageRegion();
                    Assert.Equal(zoom, box.Zoom);
                    Assert.InRange(region.X + region.Width / 2, 29, 31);
                    Assert.InRange(region.Y + region.Height / 2, 69, 71);
                }
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void GrayscaleImageDrawsTheSameAsTheGrayBitmap()
    {
        Headless.Run(() =>
        {
            const int width = 64;
            const int height = 48;
            var pixels = new byte[width * height];
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                pixels[y * width + x] = (byte)((x * 4 + y * 3) % 256);

            using var gray = new GrayscaleImage(width, height);
            var handle = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            try
            {
                gray.CopyFrom(handle.AddrOfPinnedObject(), width);
            }
            finally
            {
                handle.Free();
            }

            using var bitmap = Headless.CreateBitmap(width, height, (x, y) =>
            {
                var value = pixels[y * width + x];
                return (value, value, value);
            });

            var (window, box) = Headless.CreateBox(b => b.GrayscaleImage = gray);
            var (referenceWindow, referenceBox) = Headless.CreateBox(b => b.Image = bitmap);
            try
            {
                Assert.True(box.IsImageLoaded);
                box.Zoom = 400;
                referenceBox.Zoom = 400;

                var frame = Headless.Grab(window);
                var reference = Headless.Grab(referenceWindow);
                var maxDifference = 0;
                for (var i = 0; i < frame.Bgra.Length; i++)
                {
                    maxDifference = Math.Max(maxDifference, Math.Abs(frame.Bgra[i] - reference.Bgra[i]));
                }

                Assert.True(maxDifference <= 1, $"Max channel difference {maxDifference}");
            }
            finally
            {
                window.Close();
                referenceWindow.Close();
            }
        });
    }

    [Fact]
    public void AutoSamplingAveragesAZoomedOutImage()
    {
        Headless.Run(() =>
        {
            // A one pixel checkerboard aliases to solid black or white with nearest sampling at 25%, it skips 3 of
            // every 4 pixels, while the averaged image is the mid gray of the checkerboard
            using var source = Headless.CreateBitmap(512, 512,
                (x, y) => (x + y) % 2 == 0 ? ((byte)255, (byte)255, (byte)255) : ((byte)0, (byte)0, (byte)0));
            var (window, box) = Headless.CreateBox(b => b.Image = source);
            try
            {
                double CenterMean()
                {
                    box.Zoom = 25;
                    var frame = Headless.Grab(window);
                    var center = box.TranslatePoint(box.GetOffsetPoint(256, 256), window)!.Value;
                    double sum = 0;
                    var count = 0;
                    for (var y = (int)center.Y - 20; y < (int)center.Y + 20; y++)
                    for (var x = (int)center.X - 20; x < (int)center.X + 20; x++)
                    {
                        sum += frame.At(x, y).G;
                        count++;
                    }

                    return sum / count;
                }

                box.BitmapSamplingMode = BitmapSamplingModes.Nearest;
                var nearest = CenterMean();
                box.BitmapSamplingMode = BitmapSamplingModes.Auto;
                var auto = CenterMean();

                Assert.True(nearest is < 30 or > 225, $"Nearest sampling mean {nearest} should be aliased");
                Assert.InRange(auto, 100, 156);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void MinimapIsOnlyShownWhileZoomedIn()
    {
        Headless.Run(() =>
        {
            using var source = Headless.CreateBitmap(400, 300, Pattern);
            var (window, box) = Headless.CreateBox(b =>
            {
                b.Image = source;
                b.ShowMinimap = true;
            });
            try
            {
                box.ZoomToFit();
                Headless.Pump();
                Assert.Equal(default, box.MinimapBounds);

                box.Zoom = 800;
                box.CenterAt(new Point(200, 150));
                Headless.Pump();
                var bounds = box.MinimapBounds;
                Assert.True(bounds.Width > 0 && bounds.Height > 0);
                Assert.True(new Rect(box.Bounds.Size).Contains(bounds), $"{bounds} outside of {box.Bounds}");

                box.ShowMinimap = false;
                Assert.Equal(default, box.MinimapBounds);
            }
            finally
            {
                window.Close();
            }
        });
    }

    [Fact]
    public void ScaleBarIsInItsCorner()
    {
        Headless.Run(() =>
        {
            using var source = Headless.CreateBitmap(400, 300, Pattern);
            var (window, box) = Headless.CreateBox(b =>
            {
                b.Image = source;
                b.ShowScaleBar = true;
                b.PixelWidth = 0.05;
                b.PixelHeight = 0.05;
            });
            try
            {
                box.ScaleBarCorner = Corners.BottomLeft;
                Headless.Pump();
                var bottomLeft = box.ScaleBarBounds;
                Assert.True(bottomLeft.Width > 0 && bottomLeft.Height > 0);

                box.ScaleBarCorner = Corners.TopRight;
                Headless.Pump();
                var topRight = box.ScaleBarBounds;
                Assert.True(topRight.X > bottomLeft.X && topRight.Y < bottomLeft.Y);

                box.ShowScaleBar = false;
                Assert.Equal(default, box.ScaleBarBounds);
            }
            finally
            {
                window.Close();
            }
        });
    }
}
