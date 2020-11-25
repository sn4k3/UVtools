using System;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia;
using Avalonia.Markup.Xaml;
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
        public string Description => App.AssemblyDescription;

        public string OSDescription => $"{RuntimeInformation.OSDescription} {RuntimeInformation.OSArchitecture}";
        public string FrameworkDescription => RuntimeInformation.FrameworkDescription;
        public int ProcessorCount => Environment.ProcessorCount;
        public int ScreenCount => Screens.ScreenCount;
        public string ScreenResolution => $"{Screens.Primary.Bounds.Width} x {Screens.Primary.Bounds.Height} @ {Screens.Primary.PixelDensity*100}%";
        public string WorkingArea => $"{Screens.Primary.WorkingArea.Width} x {Screens.Primary.WorkingArea.Height}";
        public string RealWorkingArea => $"{Math.Round(Screens.Primary.WorkingArea.Width / Screens.Primary.PixelDensity)} x {Math.Round(Screens.Primary.WorkingArea.Height / Screens.Primary.PixelDensity)}";

        public string ScreensDescription
        {
            get
            {
                var result = new StringBuilder();
                for (int i = 0; i < Screens.All.Count; i++)
                {
                    var screen = Screens.All[i];
                    result.AppendLine($"{i+1}: {screen.Bounds.Width} x {screen.Bounds.Height} @ {screen.PixelDensity * 100}%");
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
    }
}
