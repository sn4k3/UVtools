using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace UVtools.WPF.Windows
{
    public class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
