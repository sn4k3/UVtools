/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform;
using Emgu.CV;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Input.Platform;
using Material.Icons;
using SukiUI.MessageBox;
using Updatum;
using UVtools.Core;
using UVtools.Core.FileFormats;
using UVtools.Core.Managers;
using UVtools.Core.SystemOS;
using UVtools.UI.Extensions;
using UVtools.UI.Structures;
using UVtools.UI.Windows;
using ZLinq;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace UVtools.UI;

public partial class App : Application
{
    public enum ApplicationTheme
    {
        [Description("Fluent system")]
        FluentSystem,

        [Description("Fluent light")]
        FluentLight,
        [Description("Fluent dark")]
        FluentDark,

        /*[Description("Simple system")]
        SimpleSystem,
        [Description("Simple light")]
        SimpleLight,
        [Description("Simple dark")]
        SimpleDark*/
    }

    public static MainWindow MainWindow = null!;
    public static FileFormat? SlicerFile;

    internal static readonly UpdatumManager AppUpdater = new(EntryApplication.AssemblyRepositoryUrl)
    {
        InstallUpdateWindowsInstallerArguments = "/qb",
        InstallUpdateSingleFileExecutableName = About.Software,
        InstallUpdateCodesignMacOSApp = true,
        DownloadProgressUpdateFrequencySeconds = 0.1 // 10 FPS
    };


    public override void Initialize()
    {
        //Styles.Add(ThemeStylesContainer);
        AvaloniaXamlLoader.Load(this);

        SetupTheme();
        UserSettings.Load();
        UserSettings.SetVersion();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        //BindingPlugins.DataValidators.RemoveAt(0);



        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (Program.IsCrashReport)
            {
                //Program.Args = new[] {"--crash-report", "Debug", "Lorem Ipsum is simply dummy text of the printing and typesetting industry. Lorem Ipsum has been the industry's standard dummy text ever since the 1500s, when an unknown printer took a galley of type and scrambled it to make a type specimen book. It has survived not only five centuries, but also the leap into electronic typesetting, remaining essentially unchanged. It was popularised in the 1960s with the release of Letraset sheets containing Lorem Ipsum passages, and more recently with desktop publishing software like Aldus PageMaker including versions of Lorem Ipsum." };
                if (Program.Args.Length < 3) return;
                if (string.IsNullOrWhiteSpace(Program.Args[1])) return;
                if (string.IsNullOrWhiteSpace(Program.Args[2])) return;
                var category = Program.Args[1];
                var bugDescription = $"{Program.Args[2]}\nCategory: {category}";
                var system = string.Empty;
                if (Program.Args.Length >= 4 && !string.IsNullOrWhiteSpace(Program.Args[3]))
                {
                    bugDescription += $"\nFile: {Program.Args[3]}";
                }
                bugDescription += $"\n\nMachine date time: {DateTime.Now}\n       UTC date time: {DateTime.UtcNow}";

                try
                {
                    system = AboutWindow.GetEssentialInformationStatic();
                }
                catch
                {
                    // ignored
                }

                using var reader = new StringReader(bugDescription);
                MessageWindow window = null!;
                window = new MessageWindow($"{About.SoftwareWithVersion} - Crash report",
                    MaterialIconKind.EmojiFrownOutline,
                    $"{About.Software} crashed due an unexpected {category.ToLowerInvariant()} error.\nYou can report this error if you find necessary.\nFind more details below:\n",
                    bugDescription,
                    TextWrapping.Wrap,
                    [
                        MessageWindow.CreateLinkButtonAction("Report", MaterialIconKind.Bug, $"https://github.com/sn4k3/UVtools/issues/new?template=bug_report_form.yml&title={HttpUtility.UrlEncode($"[Crash] {reader.ReadLine()}")}&system={HttpUtility.UrlEncode(system)}&bug_description={HttpUtility.UrlEncode($"```\n{bugDescription}\n```")}", () => window?.Clipboard?.SetTextAsync($"```\n{bugDescription}\n```")),
                        MessageWindow.CreateLinkButtonAction("Help", MaterialIconKind.QuestionMark, "https://github.com/sn4k3/UVtools/discussions/categories/q-a", () => window?.Clipboard?.SetTextAsync($"```\n{bugDescription}\n```")),
                        MessageWindow.CreateButtonAction("Restart", MaterialIconKind.Restart, () => SystemAware.StartThisApplication()),
                        MessageWindow.CreateCloseButton(MaterialIconKind.SignOut)
                    ])
                {
                    AboutButtonIsVisible = true
                };

                try
                {
                    window.Clipboard?.SetTextAsync(bugDescription);
                }
                catch
                {
                    // ignored
                }

                desktop.MainWindow = window;
            }
            else
            {
                try
                {
                    if (!CvInvoke.Init())
                    {
                        desktop.MainWindow = MissingOpenCVDependenciesWindow();
                    }
                }
                catch (Exception e)
                {
                    desktop.MainWindow = MissingOpenCVDependenciesWindow();
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

                    PixelEditorProfiles.Load();
                    SuggestionManager.Load();
                    MaterialManager.Load();
                    OperationProfiles.Load();

                    MainWindow = new MainWindow();
                    desktop.MainWindow = MainWindow;
                    MessageBoxManager.Standard = UiMessageBoxStandard.Instance;
                }
            }


            //desktop.Exit += (sender, e) => ThemeSelector.SaveSelectedTheme(Path.Combine(UserSettings.SettingsFolder, "selected.theme"));
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MessageWindow MissingOpenCVDependenciesWindow()
    {
        var message = "Your system doesn't have the required dependencies in order to run.\n" +
                      "Those dependencies are required at libcvextern/OpenCV library.\n" +
                      "UVtools is built on top of the OpenCV and therefore cannot run.\n\n";

        if (OperatingSystem.IsLinux())
        {
            try
            {
                var result = SystemAware.GetProcessOutput("bash", $"-c \"ldd '{Path.Combine(ApplicationPath, "libcvextern.so")}' | grep not\"");
                if (!string.IsNullOrWhiteSpace(result))
                {
                    message += $"Missing dependencies:\n{result}\n";
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            try
            {
                var result = SystemAware.GetProcessOutput("otool", $"-L '{Path.Combine(ApplicationPath, "libcvextern.dylib")}'");
                if (!string.IsNullOrWhiteSpace(result))
                {
                    message += $"Dependencies:\n{result}\n";
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        message += "Please install or build the dependencies in order to run the software.\n" +
                   "Check the manual page at 'Requirements' section for help.";


        return new MessageWindow($"{About.SoftwareWithVersion} is unable to run",
            MaterialIconKind.EmojiFrownOutline,
            $"{About.SoftwareWithVersionArch} [{SystemAware.OperatingSystemName}]\nUnable to run due one or more missing dependencies.\nTriggered by: libcvextern  (OpenCV)",
            message,
            TextWrapping.Wrap,
            [
                MessageWindow.CreateLinkButton("Open manual", MaterialIconKind.MicrosoftEdge, "https://github.com/sn4k3/UVtools#requirements"),
                MessageWindow.CreateLinkButton("Ask for help", MaterialIconKind.QuestionMark, "https://github.com/sn4k3/UVtools/discussions/categories/q-a"),
                MessageWindow.CreateCloseButton(MaterialIconKind.SignOut)
            ])
        {
            AboutButtonIsVisible = true
        };
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
        var uri =
            // Allow for assembly overrides
            url.StartsWith("avares://")
            ? new Uri(url)
            : new Uri($"avares://{AssemblyName}{url}");

        return uri;
    }

    public static Stream GetAsset(string url)
    {
        return AssetLoader.Open(CreateAssemblyUri(url));
    }

    public static Bitmap GetBitmapFromAsset(string url) => new(GetAsset(url));


    public static string? GetPrusaSlicerDirectory(bool isSuperSlicer = false, bool isAlpha = false)
    {
        var slicerFolder = isSuperSlicer ? "SuperSlicer" : "PrusaSlicer";
        if (isAlpha) slicerFolder += "-alpha";
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                slicerFolder);
        }

        if (OperatingSystem.IsLinux())
        {
            // 2.9.0 flatpak
            var flatpak = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".var",
                "app",
                $"com.prusa3d.{slicerFolder}",
                "config",
                slicerFolder);
            if (Directory.Exists(flatpak)) return flatpak;

            // AppImage
            var folder1 = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), // home/user/.config
                slicerFolder);
            if (Directory.Exists(folder1)) return folder1;
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), // home/user
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

    public static void BeepIfAble()
    {
        if (!UserSettings.Instance.General.NotificationBeep || UserSettings.Instance.General.NotificationBeepCount == 0) return;
        Task.Run(() =>
        {
            int frequency = UserSettings.Instance.General.NotificationBeepFrequency;

            for (int i = 0; i < UserSettings.Instance.General.NotificationBeepCount; i++)
            {
                SystemAware.Beep(frequency, UserSettings.Instance.General.NotificationBeepDuration, true);
                frequency += UserSettings.Instance.General.NotificationBeepRepeatFrequencyOffset;
                Thread.Sleep(UserSettings.Instance.General.NotificationBeepRepeatDelay);
            }

        });
    }

#endregion

    #region Assembly properties
    public static Assembly MyAssembly => Assembly.GetExecutingAssembly();

    public static string AssemblyVersion => MyAssembly.GetName().Version?.ToString()!;

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
            return Path.GetFileNameWithoutExtension(MyAssembly.Location);
        }
    }

    public static string AssemblyDescription
    {
        get
        {
            var attributes = MyAssembly.GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            if (attributes.Length == 0)
            {
                return string.Empty;
            }

            var description = ((AssemblyDescriptionAttribute)attributes[0]).Description + $"{Environment.NewLine}{Environment.NewLine}Available File Formats:";

            return FileFormat.AvailableFormats.AsValueEnumerable().SelectMany(fileFormat => fileFormat.FileExtensions).Aggregate(description, (current, fileExtension) => current + $"{Environment.NewLine}- {fileExtension.Description} (.{fileExtension.Extension})");
        }
    }

    public static string AssemblyProduct
    {
        get
        {
            var attributes = MyAssembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
            return attributes.Length == 0 ? string.Empty : ((AssemblyProductAttribute)attributes[0]).Product;
        }
    }

    public static string AssemblyCopyright
    {
        get
        {
            var attributes = MyAssembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            return attributes.Length == 0 ? string.Empty : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
    }

    public static string AssemblyCompany
    {
        get
        {
            var attributes = MyAssembly.GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
            return attributes.Length == 0 ? string.Empty : ((AssemblyCompanyAttribute)attributes[0]).Company;
        }
    }

    #endregion
}