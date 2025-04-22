using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolPixelArithmeticControl : ToolControl
{
    public OperationPixelArithmetic Operation => (BaseOperation as OperationPixelArithmetic)!;
    public ToolPixelArithmeticControl()
    {
        BaseOperation = new OperationPixelArithmetic(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();

        Operation.GeneratePattern("Chessboard");
    }

    public void PresetElephantFootCompensation()
    {
        ParentWindow!.SelectBottomLayers();
        Operation.PresetElephantFootCompensation();
    }

    public async Task LoadNormalPatternFromImage() => await LoadPatternFromImage(false);
    public async Task LoadAlternatePatternFromImage() => await LoadPatternFromImage(true);
    public async Task<bool> LoadPatternFromImage(bool isAlternatePattern = false)
    {
        var files = await App.MainWindow.OpenFilePickerAsync(AvaloniaStatic.ImagesFileFilter);
        if (files.Count == 0) return false;
        Operation.LoadPatternFromImage(files[0].TryGetLocalPath()!, isAlternatePattern);
        return true;
    }
}