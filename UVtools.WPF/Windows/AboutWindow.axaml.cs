using Avalonia.Markup.Xaml;
using UVtools.Core;
using UVtools.WPF.Controls;

namespace UVtools.WPF.Windows
{
    public class AboutWindow : WindowEx
    {
        public string Software => About.Software;
        public string Version => $"Version: {AppSettings.Version}";
        public string Copyright => AppSettings.AssemblyCopyright;
        public string Company => AppSettings.AssemblyCompany;
        public string Description => AppSettings.AssemblyDescription;


        public AboutWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
