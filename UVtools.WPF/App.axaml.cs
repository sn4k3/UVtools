/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.ThemeManager;
using Emgu.CV;
using UVtools.Core.FileFormats;
using UVtools.WPF.Extensions;

namespace UVtools.WPF
{
    public class App : Application
    {
        public static IThemeSelector? ThemeSelector { get; set; }
        public static MainWindow MainWindow;
        public static FileFormat SlicerFile = null;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override async void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

                UserSettings.Load();
                UserSettings.SetVersion();

                ThemeSelector = Avalonia.ThemeManager.ThemeSelector.Create("Assets/Themes");
                ThemeSelector.LoadSelectedTheme("Assets/selected.theme");
                MainWindow = new MainWindow();

                try
                {
                    CvInvoke.CheckLibraryLoaded();
                }
                catch (Exception e)
                {
                    await MainWindow.MessageBoxError("UVtools can not run due lack of dependencies from cvextern/OpenCV\n" +
                                                     "Please build or install this dependencies in order to run UVtools\n" +
                                                     "Check manual or page at 'Requirements' section for help\n\n" +
                                                     "Additional information:\n" +
                                                     $"{e}", "UVtools can not run");
                    return;
                }
                

                desktop.MainWindow = MainWindow;
                desktop.Exit += (sender, e) 
                    => ThemeSelector.SaveSelectedTheme("Assets/selected.theme");
            }

            base.OnFrameworkInitializationCompleted();
        }

        #region Utilities
        public static void NewInstance(string filePath)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    var info = new ProcessStartInfo("UVtools.exe", $"\"{filePath}\"")
                    {
                        UseShellExecute = true
                    };
                    Process.Start(info).Dispose();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("dotnet", $"UVtools.dll \"{filePath}\"").Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public static void OpenBrowser(string url)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }).Dispose();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url).Dispose();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url).Dispose();
                }
                else
                {
                    // throw 
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            
        }

        public static void StartProcess(string name)
        {
            try
            {
                using (Process.Start(new ProcessStartInfo(name)
                {
                    UseShellExecute = true
                })) { }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            
        }

        public static Stream GetAsset(string url)
        {
            Uri uri;

            // Allow for assembly overrides
            if (url.StartsWith("avares://"))
            {
                uri = new Uri(url);
            }
            else
            {
                var assemblyName = Assembly.GetEntryAssembly().GetName().Name;
                uri = new Uri($"avares://{assemblyName}{url}");
            }

            var res = AvaloniaLocator.Current.GetService<IAssetLoader>().Open(uri);
            return res;
        }

        public static Bitmap GetBitmapFromAsset(string url) => new Bitmap(GetAsset(url));

        #endregion
    }
}
