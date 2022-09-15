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
using System.Web;
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
using UVtools.WPF.Structures;
using UVtools.WPF.Windows;
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

    public static bool IsDebug
    {
        get
        {
#if DEBUG
            return true;
#else
            return false;
#endif
        }
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

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            UserSettings.Load();
            UserSettings.SetVersion();

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

            /*if (!CvInvoke.Init())
            {
                Console.WriteLine("UVtools can not init OpenCV library\n" +
                                  "Please build or install this dependencies in order to run UVtools\n" +
                                  "Check manual or page at 'Requirements' section for help");
            }*/

            if (UserSettings.Instance.General.Theme != ApplicationTheme.FluentLight)
            {
                ApplyTheme();
            }

            if (Program.IsCrashReport)
            {
                //Program.Args = new[] {"--crash-report", "Debug", "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum." };
                if (Program.Args.Length < 3) return;
                if (string.IsNullOrWhiteSpace(Program.Args[1])) return;
                if (string.IsNullOrWhiteSpace(Program.Args[2])) return;
                var category = Program.Args[1];
                var message = $"{Program.Args[2]}\nCategory: {category}";
                if (Program.Args.Length >= 4 && !string.IsNullOrWhiteSpace(Program.Args[3]))
                {
                    message += $"\nFile: {Program.Args[3]}";
                }

                var bugReportMessageMk = $"# Report\n```\n{message}\n```";

                try
                {
                    var append = $"\n\n# System\n{AboutWindow.GetEssentialInformationStatic()}";
                    message += append;
                    bugReportMessageMk += append;
                }
                catch
                {
                    // ignored
                }

                var append2 = $"\n\nMachine date time: {DateTime.Now}\n    UTC date time: {DateTime.UtcNow}";
                message += append2;
                bugReportMessageMk += $"{append2}\n\n# Additional information and Workflow\nComplete with additional information and the workflow that caused this crash with the file as well.";

                try
                {
                    Current?.Clipboard?.SetTextAsync(bugReportMessageMk);
                }
                catch
                {
                    // ignored
                }
                

                using var reader = new StringReader(message);
                desktop.MainWindow = new MessageWindow($"{About.SoftwareWithVersion} - Crash report", 
                    "fa-regular fa-frown", 
                    $"{About.Software} crashed due an unexpected {category.ToLowerInvariant()} error.\nYou can report this error if you find necessary.\nFind more details below:\n", 
                    message,
                    new[]
                    {
                        MessageWindow.CreateLinkButtonAction("Report", "fa-solid fa-bug", $"https://github.com/sn4k3/UVtools/issues/new?assignees=sn4k3&labels=&template=bug_report.md&title={HttpUtility.UrlEncode($"[Crash] {reader.ReadLine()}")}&body={HttpUtility.UrlEncode("<!--\n# Instructions:\n1. Click on this box;\n2. Select all it text (Ctrl + A);\n3. Paste the report content (Ctrl + V);\n4. Review the content;\n5. Submit the issue.\n!-->")}", () => Current?.Clipboard?.SetTextAsync(bugReportMessageMk)),
                        MessageWindow.CreateLinkButtonAction("Help", "fa-solid fa-question", "https://github.com/sn4k3/UVtools/discussions/categories/q-a", () => Current?.Clipboard?.SetTextAsync(bugReportMessageMk)),
                        MessageWindow.CreateButtonAction("Restart", "fa-solid fa-redo-alt", () => SystemAware.StartThisApplication()),
                        MessageWindow.CreateCloseButton("fa-solid fa-sign-out-alt")
                    });
            }
            else
            {
                try
                {
                    if (!CvInvoke.Init())
                    {
                        desktop.MainWindow = new MessageWindow($"{About.SoftwareWithVersion} is unable to run",
                            "fa-regular fa-frown",
                            $"{About.SoftwareWithVersionArch} [{SystemAware.OperatingSystemName}]\nUnable to run due one or more missing dependencies.\nTriggered by: libcvextern  (OpenCV)",
                            "Your system doesn't have the required dependencies in order to run.\n" +
                            "Those dependencies are required at libcvextern/OpenCV library.\n" +
                            "UVtools is built on top of the OpenCV and therefore cannot run.\n\n" +
                            "Please install or build the dependencies in order to run the software.\n" +
                            "Check the manual page at 'Requirements' section for help.",
                            new[]
                            {
                                MessageWindow.CreateLinkButton("Open manual", "fa-brands fa-edge", "https://github.com/sn4k3/UVtools#requirements"),
                                MessageWindow.CreateLinkButton("Ask for help", "fa-solid fa-question", "https://github.com/sn4k3/UVtools/discussions/categories/q-a"),
                                MessageWindow.CreateCloseButton("fa-solid fa-sign-out-alt")
                            });
                    }
                }
                catch (Exception e)
                {
                    desktop.MainWindow = new MessageWindow($"{About.SoftwareWithVersion} is unable to run",
                        "fa-regular fa-frown",
                        $"{About.SoftwareWithVersionArch} [{SystemAware.OperatingSystemName}]\nUnable to run due one or more missing dependencies.\nTriggered by: libcvextern  (OpenCV)",
                        "Your system doesn't have the required dependencies in order to run.\n" +
                        "Those dependencies are required at libcvextern/OpenCV library.\n" +
                        "UVtools is built on top of the OpenCV and therefore cannot run.\n\n" +
                        "Please install or build the dependencies in order to run the software.\n" +
                        "Check the manual page at 'Requirements' section for help.",
                        new[]
                        {
                            MessageWindow.CreateLinkButton("Open manual", "fa-brands fa-edge", "https://github.com/sn4k3/UVtools#requirements"),
                            MessageWindow.CreateLinkButton("Ask for help", "fa-solid fa-question", "https://github.com/sn4k3/UVtools/discussions/categories/q-a"),
                            MessageWindow.CreateCloseButton("fa-solid fa-sign-out-alt")
                        });
                    Console.WriteLine(e.ToString());
                }

                if (desktop.MainWindow is null)
                {
                    if (Design.IsDesignMode)
                    {
                        SlicerFile = new ChituboxFile
                        {
                            LayerHeight = 0.05f,
                            Resolution = new(1440, 2560),
                            Display = new(68.04f, 120.96f),
                            DisplayMirror = FlipDirection.Horizontally,
                            MachineZ = 155,
                            BottomLayerCount = 3,
                            MachineName = "Epax X1"
                        };
                    }

                    MaterialManager.Load();
                    OperationProfiles.Load();
                    SuggestionManager.Load();

                    MainWindow = new MainWindow();
                    desktop.MainWindow = MainWindow;
                }
            }
            
            
            //desktop.Exit += (sender, e) => ThemeSelector.SaveSelectedTheme(Path.Combine(UserSettings.SettingsFolder, "selected.theme"));
        }

        base.OnFrameworkInitializationCompleted();
    }

#region Utilities
    public static string ApplicationPath => AppContext.BaseDirectory;
    public static string AppExecutable => Environment.ProcessPath!;
    public static string AppExecutableQuoted => $"\"{AppExecutable}\"";
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