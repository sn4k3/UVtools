using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.ExceptionServices;
using Avalonia;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Projektanker.Icons.Avalonia.MaterialDesign;
using UVtools.WPF.Extensions;

namespace UVtools.WPF;

#nullable enable

public static class Program
{
    public static string[] Args = null!;

    public static Stopwatch ProgramStartupTime = null!;
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        ProgramStartupTime = Stopwatch.StartNew();
        Args = args;
        try
        {
            if (ConsoleArguments.ParseArgs(args)) return;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            return;
        }

        //var mat = EmguExtensions.InitMat(new System.Drawing.Size(2000, 1080));
        /*const byte z = 1;
        int pixel = 1000;

        for (int y = 0; y < mat.Height; y+=200)
        for (int x = 0; x < mat.Width; x+=1)
        {
            float x1 = x / (float)mat.Width;
            float y1 = y / (float)mat.Height;

            //var result = Math.Sin(x1) * Math.Cos(y1) + Math.Sin(y1) * Math.Cos(1) + Math.Sin(1) * Math.Cos(x1);
            //mat.SetByte((int) result* mat.Width, 255);

            //CvInvoke.Circle(mat, new Point(x, (int)pixelY + y), 1, EmguExtensions.WhiteColor, -1, LineType.AntiAlias);
        }*/


        /*var sineHeight = 100;
        var sineWidth = 100;
        byte radius = 10;

        for (int y1 = 0; y1 < mat.Height; y1 += sineHeight)
        for (int x = 0; x < mat.Width; x++)
        {
            int y2 = (int)(Math.Sin((double)x / sineWidth) * sineHeight / 2.0 + sineHeight / 2.0 + radius);

            CvInvoke.Circle(mat, new Point(x, y1+y2), radius, EmguExtensions.WhiteColor, -1, LineType.AntiAlias);          
        }

        CvInvoke.Imshow("gyroid", mat);
        CvInvoke.WaitKey();
        return;*/

        /*Slicer slicer = new(Size.Empty, SizeF.Empty, "D:\\Cube100x100x100.stl");
        var slices = slicer.SliceModel(0.05f);
        
        foreach (var slice in slices)
        {
            using var mat = EmguExtensions.InitMat(new Size(1000, 1000));
            var contour = slice.Value.ToContour();
            using var vec = new VectorOfPoint(contour);
            CvInvoke.FillPoly(mat, vec, EmguExtensions.WhiteColor, LineType.AntiAlias);
            mat.Save(@$"D:\SLICE\{slice.Key}.png");
        }*/

        // PrusaSlicer to Machine.cs
        //var machines = Machine.GetMachinesFromPrusaSlicer();
        //var machinesText = Machine.GenerateMachinePresetsFromPrusaSlicer();

        // Add the event handler for handling non-UI thread exceptions to the event.
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        //AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnFirstChanceException;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static void CurrentDomainOnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
    {
        ErrorLog.AppendLine("First chance exception", e.Exception.ToString());
    }


    private static async void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var ex = (Exception)e.ExceptionObject;
        ErrorLog.AppendLine("Fatal Non-UI Error", ex.ToString());

        try
        {
            var errorMsg = $"An application error occurred. Please contact the administrator with the following information:\n\n{ex}";
            await App.MainWindow.MessageBoxError(errorMsg, "Fatal Non-UI Error");
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .With(new Win32PlatformOptions { AllowEglInitialization = false/*, UseWgl = true*/})
            .With(new X11PlatformOptions { UseGpu = true/*, UseEGL = true*/ })
            .With(new MacOSPlatformOptions { ShowInDock = true })
            .With(new AvaloniaNativePlatformOptions { UseGpu = true })
            .WithIcons(container => container
                .Register<FontAwesomeIconProvider>()
                .Register<MaterialDesignIconProvider>())
            //.UseSkia()
            .LogToTrace();
}