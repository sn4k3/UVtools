using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Projektanker.Icons.Avalonia;
using Projektanker.Icons.Avalonia.FontAwesome;
using Projektanker.Icons.Avalonia.MaterialDesign;
using UVtools.Core.SystemOS;

namespace UVtools.WPF;

#nullable enable

public static class Program
{
    public static string[] Args = Array.Empty<string>();
    public static bool IsCrashReport;

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

        if (Args.Length > 2 && Args[0] == "--crash-report")
        {
            IsCrashReport = true;
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
        AppDomain.CurrentDomain.UnhandledException += (sender, e) => HandleUnhandledException("Non-UI", (Exception)e.ExceptionObject);
        TaskScheduler.UnobservedTaskException += (sender, e) => HandleUnhandledException("Task", e.Exception);
        //AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnFirstChanceException;
        
        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception e)
        {
            HandleUnhandledException("Application", e);
        }
        

        // Closing
    }

    private static void HandleUnhandledException(string category, Exception ex)
    {
        ErrorLog.AppendLine($"Fatal {category} Error", ex.ToString());

        if (!IsCrashReport)
        {
            try
            {
                SystemAware.StartThisApplication($"--crash-report \"{category}\" \"{ex}\"");
                //var errorMsg = $"An application error occurred. Please contact the administrator with the following information:\n\n{ex}";
                //await App.MainWindow.MessageBoxError(errorMsg, "Fatal Non-UI Error");
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                Console.WriteLine(exception);
            }
        }

        Environment.Exit(-1);
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