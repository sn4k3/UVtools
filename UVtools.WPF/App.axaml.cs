/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using Emgu.CV;
using UVtools.Core;
using UVtools.Core.FileFormats;
using UVtools.Core.Managers;
using UVtools.Core.SystemOS;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace UVtools.WPF;

#nullable enable

public class App : Application
{
    public enum ApplicationTheme
    {
        [Description("Fluent light")]
        FluentLight,
        [Description("Fluent dark")]
        FluentDark,

        [Description("Default light")]
        DefaultLight,
        [Description("Default dark")]
        DefaultDark
    }
    //public static ThemeSelector ThemeSelector { get; set; }
    public static MainWindow MainWindow = null!;
    public static FileFormat? SlicerFile = null;

    public static AppVersionChecker VersionChecker { get; } = new();

    public static StyleInclude DataGridFluent => new(CreateAssemblyUri("/Styles"))
    {
        Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Fluent.xaml")
    };

    public static StyleInclude DataGridDefault => new(CreateAssemblyUri("/Styles"))
    {
        Source = new Uri("avares://Avalonia.Controls.DataGrid/Themes/Default.xaml")
    };

    public static readonly StyleInclude AppStyleLight = new(CreateAssemblyUri("/Assets/Styles"))
    {
        Source = CreateAssemblyUri("/Assets/Styles/StylesLight.xaml")
    };

    public static readonly StyleInclude AppStyleDark = new(CreateAssemblyUri("/Assets/Styles"))
    {
        Source = CreateAssemblyUri("/Assets/Styles/StylesDark.xaml")
    };

    public static FluentTheme Fluent = new(CreateAssemblyUri("/Styles"));
        
    public static Styles DefaultLight = new()
    {
        new StyleInclude(new Uri($"resm:Styles?assembly={AssemblyName}"))
        {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml")
        },
        new StyleInclude(new Uri($"resm:Styles?assembly={AssemblyName}"))
        {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
        },
        new StyleInclude(new Uri($"resm:Styles?assembly={AssemblyName}"))
        {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseLight.xaml")
        },
        new StyleInclude(new Uri($"resm:Styles?assembly={AssemblyName}"))
        {
            Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseLight.xaml")
        },
        new StyleInclude(new Uri($"resm:Styles?assembly={AssemblyName}"))
        {
            Source = new Uri("avares://Avalonia.Themes.Default/DefaultTheme.xaml")
        }
    };

    public static Styles DefaultDark = new()
    {
        new StyleInclude(new Uri($"resm:Styles?assembly={AssemblyName}"))
        {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/AccentColors.xaml")
        },
        new StyleInclude(new Uri($"resm:Styles?assembly={AssemblyName}"))
        {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/Base.xaml")
        },
        new StyleInclude(new Uri($"resm:Styles?assembly={AssemblyName}"))
        {
            Source = new Uri("avares://Avalonia.Themes.Fluent/Accents/BaseDark.xaml")
        },
        new StyleInclude(new Uri($"resm:Styles?assembly={AssemblyName}"))
        {
            Source = new Uri("avares://Avalonia.Themes.Default/Accents/BaseDark.xaml")
        },
        new StyleInclude(new Uri($"resm:Styles?assembly={AssemblyName}"))
        {
            Source = new Uri("avares://Avalonia.Themes.Default/DefaultTheme.xaml")
        }
    };

    public static void ApplyTheme()
    {
        switch (UserSettings.Instance.General.Theme)
        {
            case ApplicationTheme.FluentLight:
            {
                if (Fluent.Mode != FluentThemeMode.Light)
                {
                    Fluent.Mode = FluentThemeMode.Light;
                }
                    
                Current!.Styles[0] = Fluent;
                Current.Styles[1] = DataGridFluent;
                Current.Styles[2] = AppStyleLight;
                break;
            }
            case ApplicationTheme.FluentDark:
            {
                if (Fluent.Mode != FluentThemeMode.Dark)
                {
                    Fluent.Mode = FluentThemeMode.Dark;
                }

                Current!.Styles[0] = Fluent;
                Current.Styles[1] = DataGridFluent;
                Current.Styles[2] = AppStyleDark;
                break;
            }
            case ApplicationTheme.DefaultLight:
                Current!.Styles[0] = DefaultLight;
                Current.Styles[1] = DataGridDefault;
                Current.Styles[2] = AppStyleLight;
                break;
            case ApplicationTheme.DefaultDark:
                Current!.Styles[0] = DefaultDark;
                Current.Styles[1] = DataGridDefault;
                Current.Styles[2] = AppStyleDark;
                break;
        }
    }

    public override void Initialize()
    {
        Styles.Insert(0, Fluent);
        Styles.Insert(1, DataGridFluent);
        Styles.Insert(2, AppStyleLight);
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (Design.IsDesignMode)
        {
            SlicerFile = new ChituboxFile
            {
                LayerHeight = 0.05f,
                Resolution = new (1440, 2560),
                Display = new (68.04f, 120.96f),
                DisplayMirror = FlipDirection.Horizontally,
                MachineZ = 155,
                BottomLayerCount = 3,
                MachineName = "Epax X1"
            };
        }
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            UserSettings.Load();
            UserSettings.SetVersion();

            MaterialManager.Load();
            OperationProfiles.Load();
            SuggestionManager.Load();

            /*ThemeSelector = ThemeSelector.Create(Path.Combine(ApplicationPath, "Assets", "Themes"));
            ThemeSelector.LoadSelectedTheme(Path.Combine(UserSettings.SettingsFolder, "selected.theme"));
            if (ThemeSelector.SelectedTheme.Name == "UVtoolsDark" || ThemeSelector.SelectedTheme.Name == "Light")
            {
                foreach (var theme in ThemeSelector.Themes)
                {
                    if (theme.Name != "UVtoolsLight") continue;
                    theme.ApplyTheme();
                    break;
                }
            }*/

            if (!CvInvoke.Init())
            {
                Console.WriteLine("UVtools can not init OpenCV library\n" +
                                  "Please build or install this dependencies in order to run UVtools\n" +
                                  "Check manual or page at 'Requirements' section for help");
            }

            if (UserSettings.Instance.General.Theme != ApplicationTheme.FluentLight)
            {
                ApplyTheme();
            }

            MainWindow = new MainWindow();
            try
            {
                if(!CvInvoke.Init())
                    await MainWindow.MessageBoxError("UVtools can not init OpenCV library\n" +
                                                     "Please build or install this dependencies in order to run UVtools\n" +
                                                     "Check manual or page at 'Requirements' section for help", 
                        "UVtools can not run");
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
            //desktop.Exit += (sender, e) => ThemeSelector.SaveSelectedTheme(Path.Combine(UserSettings.SettingsFolder, "selected.theme"));
        }

        base.OnFrameworkInitializationCompleted();
    }

    #region Utilities
    public static string ApplicationPath => AppContext.BaseDirectory;
    public static readonly string AppExecutable = Environment.ProcessPath!;
    public static readonly string AppExecutableQuoted = $"\"{AppExecutable}\"";
    public static void NewInstance(string filePath)
    {
        try
        {
            if (File.Exists(AppExecutable)) // Direct execute
            {
                SystemAware.StartProcess(AppExecutable, $"\"{filePath}\"");
            }
            else
            {
                SystemAware.StartProcess("dotnet", $"UVtools.dll \"{filePath}\"");
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }
    }

    public static Uri CreateAssemblyUri(string url)
    {
        Uri uri;

        // Allow for assembly overrides
        if (url.StartsWith("avares://"))
        {
            uri = new Uri(url);
        }
        else
        {
            uri = new Uri($"avares://{AssemblyName}{url}");
        }

        return uri;
    }

    public static Stream GetAsset(string url)
    {
        return AvaloniaLocator.Current.GetService<IAssetLoader>()?.Open(CreateAssemblyUri(url))!;
    }

    public static Bitmap GetBitmapFromAsset(string url) => new(GetAsset(url));

        
    public static string? GetPrusaSlicerDirectory(bool isSuperSlicer = false)
    {
        var slicerFolder = isSuperSlicer ? "SuperSlicer" : "PrusaSlicer";
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                slicerFolder);
        }

        if (OperatingSystem.IsLinux())
        {
            var folder1 = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".config",
                slicerFolder);
            if (Directory.Exists(folder1)) return folder1;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                $".{slicerFolder}");
        }

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support",
                slicerFolder);
        }

        return null;
    }

    #endregion

    #region Assembly properties
    public static Assembly WpfAssembly => Assembly.GetExecutingAssembly();

    public static string AssemblyVersion => WpfAssembly.GetName().Version?.ToString()!;

    public static string AssemblyName => Assembly.GetExecutingAssembly().GetName().Name!;

    public static string AssemblyTitle
    {
        get
        {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
            if (attributes.Length > 0)
            {
                var titleAttribute = (AssemblyTitleAttribute)attributes[0];
                if (titleAttribute.Title != string.Empty)
                {
                    return titleAttribute.Title;
                }
            }
            return Path.GetFileNameWithoutExtension(WpfAssembly.Location);
        }
    }

    public static string AssemblyDescription
    {
        get
        {
            var attributes = WpfAssembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            if (attributes.Length == 0)
            {
                return string.Empty;
            }

            var description = ((AssemblyDescriptionAttribute)attributes[0]).Description + $"{Environment.NewLine}{Environment.NewLine}Available File Formats:";

            return FileFormat.AvailableFormats.SelectMany(fileFormat => fileFormat.FileExtensions).Aggregate(description, (current, fileExtension) => current + $"{Environment.NewLine}- {fileExtension.Description} (.{fileExtension.Extension})");
        }
    }

    public static string AssemblyProduct
    {
        get
        {
            var attributes = WpfAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            return attributes.Length == 0 ? string.Empty : ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    public static string AssemblyCopyright
    {
        get
        {
            var attributes = WpfAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            return attributes.Length == 0 ? string.Empty : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }

    public static string AssemblyCompany
    {
        get
        {
            var attributes = WpfAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            return attributes.Length == 0 ? string.Empty : ((AssemblyCompanyAttribute)attributes[0]).Company;
        }
    }
    #endregion
}