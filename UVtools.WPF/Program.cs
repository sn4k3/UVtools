using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using Avalonia;

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
            //AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnFirstChanceException;
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        private static void CurrentDomainOnFirstChanceException(object? sender, FirstChanceExceptionEventArgs e)
        {
            ErrorLog.AppendLine("First chance exception", e.Exception.ToString());
        }


        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            ErrorLog.AppendLine("Fatal Non-UI Error", ex.ToString());
            
            /*try
            {
                string errorMsg = "An application error occurred. Please contact the administrator with the following information:\n\n" +
                                  $"{ex}";

                await App.MainWindow.MessageBoxError(errorMsg, "Fatal Non-UI Error");
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }*/
        }

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .With(new Win32PlatformOptions { AllowEglInitialization = true/*, UseWgl = true*/})
                .With(new X11PlatformOptions { UseGpu = true/*, UseEGL = true*/ })
                .With(new MacOSPlatformOptions { ShowInDock = true })
                .With(new AvaloniaNativePlatformOptions { UseGpu = true })
                .UseSkia()
                .LogToTrace();
    }
}
