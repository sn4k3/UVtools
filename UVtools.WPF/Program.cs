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