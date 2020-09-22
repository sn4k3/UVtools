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
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ThemeManager;
using UVtools.Core.FileFormats;

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

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
                UserSettings.Load();
                UserSettings.SetVersion();

                ThemeSelector = Avalonia.ThemeManager.ThemeSelector.Create("Assets/Themes");
                ThemeSelector.LoadSelectedTheme("Assets/selected.theme");
                desktop.MainWindow = MainWindow = new MainWindow
                {
                    //DataContext = Selector
                };
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
                var info = new ProcessStartInfo("UVtools.exe", $"\"{filePath}\"");
                Process.Start(info)?.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
            
        }

        #endregion
    }
}
