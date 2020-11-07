using Avalonia.Markup.Xaml;
using UVtools.Core;
using UVtools.WPF.Controls;

namespace UVtools.WPF.Windows
{
    public class AboutWindow : WindowEx
    {
        public string Software => About.Software;
        public string Version => $"Version: {App.VersionStr}";
        public string Copyright => App.AssemblyCopyright;
        public string Company => App.AssemblyCompany;
        public string Description => App.AssemblyDescription;


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
