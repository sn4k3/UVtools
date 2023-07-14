using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using UVtools.Core.Operations;

namespace UVtools.UI.Controls.Tools;

public partial class ToolPixelDimmingControl : ToolControl
{
    public OperationPixelDimming Operation => BaseOperation as OperationPixelDimming;

       
    public ToolPixelDimmingControl()
    {
        BaseOperation = new OperationPixelDimming(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
            
        Operation.GeneratePixelDimming("Chessboard");
    }

    public async void LoadNormalPatternFromImage() => await LoadPatternFromImage(false);
    public async void LoadAlternatePatternFromImage() => await LoadPatternFromImage(true);

    public async Task<bool> LoadPatternFromImage(bool isAlternatePattern = false)
    {
        var files = await App.MainWindow.OpenFilePickerAsync(AvaloniaStatic.ImagesFileFilter);
        if (files.Count == 0) return false;
        Operation.LoadPatternFromImage(files[0].TryGetLocalPath()!, isAlternatePattern);
        return true;
    }
}