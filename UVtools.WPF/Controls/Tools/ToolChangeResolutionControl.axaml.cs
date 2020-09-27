using System.Timers;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolChangeResolutionControl : ToolControl
    {
        private OperationChangeResolution.Resolution _selectedPresetItem;
        public OperationChangeResolution Operation { get; }

        public OperationChangeResolution.Resolution SelectedPresetItem
        {
            get => _selectedPresetItem;
            set
            {
                RaiseAndSetIfChanged(ref _selectedPresetItem, value);
                if (_selectedPresetItem is null || _selectedPresetItem.IsEmpty) return;
                Operation.NewResolutionX = _selectedPresetItem.ResolutionX;
                Operation.NewResolutionY = _selectedPresetItem.ResolutionY;
                Timer timer = new Timer(50);
                timer.Elapsed += (sender, args) =>
                {
                    SelectedPresetItem = null;
                    timer.Dispose();
                };
                timer.Start();
            }
        }

        public ToolChangeResolutionControl()
        {
            InitializeComponent();
            BaseOperation = Operation = new OperationChangeResolution(App.SlicerFile.Resolution, App.SlicerFile.LayerManager.BoundingRectangle);
            DataContext = this;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
