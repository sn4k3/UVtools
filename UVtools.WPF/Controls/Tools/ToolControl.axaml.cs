using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools;

public class ToolControl : UserControlEx
{
    private Operation _baseOperation;

    public Operation BaseOperation
    {
        get => _baseOperation;
        set
        {
            bool wasNullBefore = _baseOperation is null;
            _baseOperation = value;
            _baseOperation.SlicerFile = SlicerFile;
            RaisePropertyChanged();

            if (!wasNullBefore)
            {
                Callback(ToolWindow.Callbacks.Loaded);
            }

            if (DataContext is null) return;
            ResetDataContext();
        }
    }

    public ToolWindow ParentWindow { get; set; } = null;

    public bool CanRun { get; set; } = true;

    public ToolControl()
    {
        InitializeComponent();
    }

    public ToolControl(Operation operation)
    {
        BaseOperation = operation;
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public bool ValidateSpawn()
    {
        if (Design.IsDesignMode) return true;
        if(_baseOperation is null)
        {
            App.MainWindow.MessageBoxInfo("The operation does not contain a valid configuration.\n" +
                                          "Please contact the support/developer.", BaseOperation.NotSupportedTitle).ConfigureAwait(false);
            CanRun = false;
            return false;
        }
        if (!_baseOperation.ValidateSpawn(out var message))
        {
            App.MainWindow.MessageBoxInfo(message, BaseOperation.NotSupportedTitle).ConfigureAwait(false);
            CanRun = false;
            return false;
        }

        return true;
    }

    public virtual void Callback(ToolWindow.Callbacks callback) { }

    public virtual bool UpdateOperation() => true;

    /*public virtual void SetOperation(Operation operation)
    {
        BaseOperation = operation;
        ResetDataContext();
    }*/

    /// <summary>
    /// Validates if is safe to continue with operation
    /// </summary>
    /// <returns></returns>
    public virtual async Task<bool> ValidateForm()
    {
        if (BaseOperation is null) return true;
        if (!UpdateOperation()) return false;
        return await ValidateFormFromString(BaseOperation.Validate());
    }

    /// <summary>
    /// Validates if is safe to continue with operation, if not shows a message box with the error
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public async Task<bool> ValidateFormFromString(string text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        await ParentWindow.MessageBoxError(text);
        return false;
    }

    /// <summary>
    /// Validates if is safe to continue with operation, if not shows a message box with the error
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public async Task<bool> ValidateFormFromString(ValueDescription text) => await ValidateFormFromString(text?.ToString());
}