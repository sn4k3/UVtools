/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ThemeManager;
using UVtools.Core.FileFormats;

namespace UVtools.WPF
{
    public class App : Application
    {
        public static IThemeSelector? Selector { get; set; }

        public static FileFormat SlicerFile = null;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                UserSettings.Load();

                Selector = ThemeSelector.Create("Assets/Themes");
                Selector.LoadSelectedTheme("Assets/selected.theme");
                desktop.MainWindow = new MainWindow
                {
                    //DataContext = Selector
                };
                desktop.Exit += (sender, e) 
                    => Selector.SaveSelectedTheme("Assets/selected.theme");
            }

            base.OnFrameworkInitializationCompleted();
        }

        #region Utilities


        #endregion
    }
}
