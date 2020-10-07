using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.WPF.Controls;
using UVtools.WPF.Structures;

namespace UVtools.WPF.Windows
{
    public class PrusaSlicerManager : WindowEx
    {
        public PEProfileFolder[] Profiles { get;}

        public PrusaSlicerManager()
        {
            InitializeComponent();
            Profiles = new[]
            {
                new PEProfileFolder(PEProfileFolder.FolderType.Print),
                new PEProfileFolder(PEProfileFolder.FolderType.Printer),
            };
            
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void RefreshProfiles()
        {
            foreach (var profile in Profiles)
            {
                profile.Reset();
            }
        }
    }
}
