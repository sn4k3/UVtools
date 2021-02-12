using System;
using System.Diagnostics;
using Avalonia;
using UVtools.WPF.Extensions;

namespace UVtools.WPF
{
    public static class Program
    {
        public static string[] Args;

        public static Stopwatch ProgramStartupTime;
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        [STAThread]
        public static void Main(string[] args)
        {
            ProgramStartupTime = Stopwatch.StartNew();
            Args = args;

            // Add the event handler for handling non-UI thread exceptions to the event.
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;

            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        private static async void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                Exception ex = (Exception)e.ExceptionObject;
                string errorMsg = "An application error occurred. Please contact the administrator with the following information:\n\n" +
                                  $"{ex}";

                await App.MainWindow.MessageBoxError(errorMsg, "Fatal Non-UI Error");
            }
            catch (Exception)
            {
            }
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new Win32PlatformOptions { AllowEglInitialization = true})
                .With(new X11PlatformOptions { UseGpu = true, UseEGL = true })
                .With(new MacOSPlatformOptions { ShowInDock = true })
                .With(new AvaloniaNativePlatformOptions { UseGpu = true })
                .UseSkia()
                .LogToTrace();
    }
}
