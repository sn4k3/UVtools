using Avalonia;
using Avalonia.Markup.Xaml;
using Emgu.CV;
using System;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Media;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.SystemOS;
using UVtools.WPF.Controls;
using UVtools.WPF.Extensions;

namespace UVtools.WPF.Windows;

public class AboutWindow : WindowEx
{
    public static string OpenCVBuildInformation
    {
        get
        {
            try
            {
                return CvInvoke.BuildInformation;
            }
            catch
            {
                // ignored
            }

            return "Error: Unable to load the library.";
        }
    }

    public static string LoadedAssemblies 
    {
        get
        {
            var sb = new StringBuilder();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            var assembliesLengthPad = assemblies.Length.DigitCount();
            for (var i = 0; i < assemblies.Length; i++)
            {
                var assembly = assemblies[i].GetName();
                sb.AppendLine($"{(i + 1).ToString().PadLeft(assembliesLengthPad, '0')}: {assembly.Name}, Version={assembly.Version}");
            }

            return sb.ToString();
        }
    }

    public static string OSDescription => $"{RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}";

    public static string RuntimeDescription => RuntimeInformation.RuntimeIdentifier;

    public static string FrameworkDescription => RuntimeInformation.FrameworkDescription;
    public static string AvaloniaUIDescription => typeof(AvaloniaObject).Assembly.GetName().Version!.ToString(3);

    public static string OpenCVVersion
    {
        get
        {
            try
            {
                return typeof(Mat).Assembly.GetName().Version!.ToString(3);
            }
            catch 
            {
                // ignored
            }

            return "???";
        }
    }

    public static string GraphicCardName => SystemAware.GetGraphicCardName();

    public static string ProcessorName => SystemAware.GetProcessorName();

    public static int ProcessorCount => Environment.ProcessorCount;

    public static string MemoryRAMDescription
    {
        get
        {
            var memory = SystemAware.GetMemoryStatus();
            if (memory.ullTotalPhys == 0)
            {
                return "Unknown";
            }

            var factor = Math.Pow(1024, 3);
            return $"{(memory.ullTotalPhys-memory.ullAvailPhys) / factor:F2} / {memory.ullTotalPhys / factor:F2} GB";
        }
    }

    public int ScreenCount => Screens.ScreenCount;
    //public string ScreenResolution => $"{Screens.Primary.Bounds.Width} x {Screens.Primary.Bounds.Height} @ {Screens.Primary.PixelDensity*100}%";
    //public string WorkingArea => $"{Screens.Primary.WorkingArea.Width} x {Screens.Primary.WorkingArea.Height}";
    //public string RealWorkingArea => $"{App.MaxWindowSize.Width} x {App.MaxWindowSize.Height}";

    public string ScreensDescription
    {
        get
        {
            var result = new StringBuilder();
            for (var i = 0; i < Screens.All.Count; i++)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                var onScreen = Screens.ScreenFromVisual(App.MainWindow is not null ? App.MainWindow : this);
                var screen = Screens.All[i];
                result.AppendLine($"{i+1}: {screen.Bounds.Width} x {screen.Bounds.Height} @ {Math.Round(screen.PixelDensity * 100, 2)}%" + 
                                  (screen.Primary ? " (Primary)" : null) +
                                  (onScreen == screen ? " (On this)" : null)
                );
                result.AppendLine($"    WA: {screen.WorkingArea.Width} x {screen.WorkingArea.Height}    UA: {Math.Round(screen.WorkingArea.Width / screen.PixelDensity)} x {Math.Round(screen.WorkingArea.Height / screen.PixelDensity)}");
            }
            return result.ToString().TrimEnd();
        }
    }

    public AboutWindow()
    {
        InitializeComponent();
        DataContext = this;
        Title = $"About {About.SoftwareWithVersion}";
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async void TermsOfUseClicked()
    {
        var messageWindow = new MessageWindow(About.TermsOfUseTitle, "fa-solid fa-handshake",
            About.TermsOfUseHeader,
            About.TermsOfUse);

        await messageWindow.ShowDialog(this);
    }

    public static string GetEssentialInformationStatic()
    {
        var gpu = GraphicCardName;
        var message = new StringBuilder();
        message.AppendLine($"{About.SoftwareWithVersionArch}");
        message.AppendLine($"Operative system: {OSDescription}");
        if (string.IsNullOrWhiteSpace(gpu)) message.AppendLine($"Graphic card: {gpu}");
        message.AppendLine($"Processor: {ProcessorName}");
        message.AppendLine($"Processor cores: {ProcessorCount}");
        message.AppendLine($"Memory RAM: {MemoryRAMDescription}");
        message.AppendLine($"Runtime: {RuntimeDescription}");
        message.AppendLine($"Framework: {FrameworkDescription}");
        message.AppendLine($"AvaloniaUI: {AvaloniaUIDescription}");
        message.AppendLine($"OpenCV: {OpenCVVersion}");
        message.AppendLine();
        message.AppendLine($"Path:       {App.ApplicationPath}");
        message.AppendLine($"Executable: {App.AppExecutable}");
        if (App.SlicerFile is not null)
        {
            message.AppendLine($"Loaded file: {App.SlicerFile.Filename} [Version: {App.SlicerFile.Version}] [Class: {App.SlicerFile.GetType().Name}]");
        }

        return message.ToString();
    }

    private string GetEssentialInformation()
    {
        var gpu = GraphicCardName;
        var message = new StringBuilder();
        message.AppendLine($"{About.SoftwareWithVersionArch}");
        message.AppendLine($"Operative system: {OSDescription}");
        if(string.IsNullOrWhiteSpace(gpu)) message.AppendLine($"Graphic card: {gpu}");
        message.AppendLine($"Processor: {ProcessorName}");
        message.AppendLine($"Processor cores: {ProcessorCount}");
        message.AppendLine($"Memory RAM: {MemoryRAMDescription}");
        message.AppendLine($"Runtime: {RuntimeDescription}");
        message.AppendLine($"Framework: {FrameworkDescription}");
        message.AppendLine($"AvaloniaUI: {AvaloniaUIDescription}");
        message.AppendLine($"OpenCV: {OpenCVVersion}");
        message.AppendLine();
        message.AppendLine("Sreens, resolution, working area, usable area:");
        message.AppendLine(ScreensDescription);
        message.AppendLine();
        message.AppendLine($"Path:       {App.ApplicationPath}");
        message.AppendLine($"Executable: {App.AppExecutable}");
        if (SlicerFile is not null)
        {
            message.AppendLine($"Loaded file: {SlicerFile.Filename} [Version: {SlicerFile.Version}] [Class: {SlicerFile.GetType().Name}]");
        }

        return message.ToString();
    }

    public void CopyEssentialInformation()
    {
        Application.Current?.Clipboard?.SetTextAsync(GetEssentialInformation());
    }
        

    public void CopyOpenCVInformationToClipboard()
    {
        Application.Current?.Clipboard?.SetTextAsync(CvInvoke.BuildInformation);
    }

    public void CopyLoadedAssembliesToClipboard()
    {
        Application.Current?.Clipboard?.SetTextAsync(LoadedAssemblies);
    }

    public async void CopyInformationToClipboard()
    {
        var message = new StringBuilder();
        message.Append(GetEssentialInformation());
        message.AppendLine(CvInvoke.BuildInformation);
        message.AppendLine("Loaded Assemblies:");
        message.AppendLine(LoadedAssemblies);
        await Application.Current?.Clipboard?.SetTextAsync(message.ToString())!;
    }
}