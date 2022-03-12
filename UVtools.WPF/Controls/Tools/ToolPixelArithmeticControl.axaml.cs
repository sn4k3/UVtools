using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Operations;

namespace UVtools.WPF.Controls.Tools;

public partial class ToolPixelArithmeticControl : ToolControl
{
    public OperationPixelArithmetic Operation => BaseOperation as OperationPixelArithmetic;
    public ToolPixelArithmeticControl()
    {
        BaseOperation = new OperationPixelArithmetic(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();

        Operation.GeneratePattern("Chessboard");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void PresetElephantFootCompensation()
    {
        ParentWindow.SelectBottomLayers();
        Operation.PresetElephantFootCompensation();
    }

    public async void LoadPatternFromImage(bool isAlternatePattern = false)
    {
        var dialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters = Helpers.ImagesFileFilter,
        };
        var files = await dialog.ShowAsync(ParentWindow);
        if (files is null || files.Length == 0) return;
        Operation.LoadPatternFromImage(files[0], isAlternatePattern);
    }
}