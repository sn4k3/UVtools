using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.ThemeManager;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;
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
                Selector = ThemeSelector.Create("Themes");
                Selector.LoadSelectedTheme("UVtools.theme");
                desktop.MainWindow = new MainWindow
                {
                    //DataContext = Selector
                };
                desktop.Exit += (sender, e) 
                    => Selector.SaveSelectedTheme("UVtools.theme");

                Debug.WriteLine(Selector.Themes[1].Name);
            }

            base.OnFrameworkInitializationCompleted();
        }

        #region Utilities


        #endregion
    }
}
