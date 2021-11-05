using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Markup.Xaml;
using Emgu.CV;
using UVtools.Core;
using UVtools.WPF.Controls;

namespace UVtools.WPF.Windows
{
    public class AboutWindow : WindowEx
    {
        public string Software => About.Software;
        public string Version => $"Version: {App.VersionStr} {RuntimeInformation.ProcessArchitecture}";
        public string Copyright => App.AssemblyCopyright;
        public string Company => App.AssemblyCompany;
        public string License => About.License;
        public string Description => App.AssemblyDescription;

        public string OSDescription => $"{RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}";

        public string RuntimeDescription => RuntimeInformation.RuntimeIdentifier;

        public string FrameworkDescription => RuntimeInformation.FrameworkDescription;
        public string AvaloniaUIDescription => typeof(AvaloniaObject).Assembly.GetName().Version.ToString(3);

        public string OpenCVDescription
        {
            get
            {
                var match = Regex.Match(CvInvoke.BuildInformation, @"(?:Version control:\s*)(\S*)");
                if (!match.Success) return "Not found!";
                return match.Groups[1].Value;
            }
        }
        public int ProcessorCount => Environment.ProcessorCount;
        public int ScreenCount => Screens.ScreenCount;
        //public string ScreenResolution => $"{Screens.Primary.Bounds.Width} x {Screens.Primary.Bounds.Height} @ {Screens.Primary.PixelDensity*100}%";
        //public string WorkingArea => $"{Screens.Primary.WorkingArea.Width} x {Screens.Primary.WorkingArea.Height}";
        //public string RealWorkingArea => $"{App.MaxWindowSize.Width} x {App.MaxWindowSize.Height}";

        public string ScreensDescription
        {
            get
            {
                var result = new StringBuilder();
                for (int i = 0; i < Screens.All.Count; i++)
                {
                    var onScreen = Screens.ScreenFromVisual(App.MainWindow);
                    var screen = Screens.All[i];
                    result.AppendLine($"{i+1}: {screen.Bounds.Width} x {screen.Bounds.Height} @ {screen.PixelDensity * 100}%" + 
                        (screen.Primary ? " (Primary)" : string.Empty) +
                        (onScreen == screen ? " (On this)" : string.Empty)                        
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
            Environment.OSVersion.Version.ToString();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void CopyOpenCVInformationToClipboard()
        {
            Application.Current.Clipboard.SetTextAsync(CvInvoke.BuildInformation);
        }

        public void CopyLoadedAssembliesToClipboard()
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            var sb = new StringBuilder();
            foreach (var assembly in loadedAssemblies)
            {
                sb.AppendLine(assembly.FullName);
            }
            Application.Current.Clipboard.SetTextAsync(sb.ToString());
        }

        public void OpenLicense() => App.OpenBrowser(About.LicenseUrl);
    }
}
