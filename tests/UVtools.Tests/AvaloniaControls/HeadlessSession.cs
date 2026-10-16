/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using UVtools.AvaloniaControls;

namespace UVtools.Tests.AvaloniaControls;

/// <summary>
/// The Avalonia application of the headless tests, with the styles of the controls.
/// </summary>
public sealed class HeadlessTestApp : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        Styles.Add(new FluentTheme());
        Styles.Add(new StyleInclude(new Uri("avares://UVtools.Tests/"))
        {
            Source = new Uri("avares://UVtools.AvaloniaControls/Controls.axaml")
        });
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<HeadlessTestApp>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>
/// A captured frame of a window, as BGRA bytes.
/// </summary>
public readonly record struct HeadlessFrame(int Width, int Height, byte[] Bgra)
{
    public (byte B, byte G, byte R, byte A) At(int x, int y)
    {
        var i = (y * Width + x) * 4;
        return (Bgra[i], Bgra[i + 1], Bgra[i + 2], Bgra[i + 3]);
    }
}

/// <summary>
/// Runs code on the Avalonia headless platform (Skia rendering), one session for all the tests.
/// </summary>
public static class Headless
{
    private static readonly Lazy<HeadlessUnitTestSession> Session =
        new(() => HeadlessUnitTestSession.StartNew(typeof(HeadlessTestApp)), LazyThreadSafetyMode.ExecutionAndPublication);

    public static void Run(Action action)
    {
        Session.Value.Dispatch(action, CancellationToken.None).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Runs the pending jobs and renders a frame.
    /// </summary>
    public static void Pump()
    {
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    public static HeadlessFrame Grab(Window window)
    {
        Pump();
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame rendered");
        using var buffer = frame.Lock();
        var width = buffer.Size.Width;
        var height = buffer.Size.Height;
        var row = new byte[width * 4];
        var data = new byte[width * height * 4];
        var isBgra = buffer.Format == PixelFormat.Bgra8888;
        for (var y = 0; y < height; y++)
        {
            Marshal.Copy(buffer.Address + y * buffer.RowBytes, row, 0, row.Length);
            for (var x = 0; x < width; x++)
            {
                var o = x * 4;
                var d = (y * width + x) * 4;
                data[d] = isBgra ? row[o] : row[o + 2];
                data[d + 1] = row[o + 1];
                data[d + 2] = isBgra ? row[o + 2] : row[o];
                data[d + 3] = row[o + 3];
            }
        }

        return new HeadlessFrame(width, height, data);
    }

    /// <summary>
    /// Creates an opaque BGRA bitmap from a function that gives the color of each pixel.
    /// </summary>
    public static WriteableBitmap CreateBitmap(int width, int height, Func<int, int, (byte R, byte G, byte B)> pixel)
    {
        var bitmap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);
        WriteBitmap(bitmap, pixel);
        return bitmap;
    }

    public static void WriteBitmap(WriteableBitmap bitmap, Func<int, int, (byte R, byte G, byte B)> pixel)
    {
        using var buffer = bitmap.Lock();
        var width = buffer.Size.Width;
        var row = new byte[width * 4];
        for (var y = 0; y < buffer.Size.Height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var (r, g, b) = pixel(x, y);
                row[x * 4] = b;
                row[x * 4 + 1] = g;
                row[x * 4 + 2] = r;
                row[x * 4 + 3] = 255;
            }

            Marshal.Copy(row, 0, buffer.Address + y * buffer.RowBytes, row.Length);
        }
    }

    /// <summary>
    /// Shows an image box with the image in a window without the extras that change the frame (grid, centering).
    /// </summary>
    public static (Window Window, AdvancedImageBox Box) CreateBox(Action<AdvancedImageBox> setup, int width = 500,
        int height = 400)
    {
        var box = new AdvancedImageBox
        {
            ShowGrid = false,
            ConstrainZoomOutToFitLevel = false,
            AutoCenter = true
        };
        setup(box);
        var window = new Window { Width = width, Height = height, Content = box, SizeToContent = SizeToContent.Manual };
        window.Show();
        Pump();
        return (window, box);
    }
}
