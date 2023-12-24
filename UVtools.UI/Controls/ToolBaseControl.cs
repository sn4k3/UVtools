using System.Threading.Tasks;
using UVtools.UI.Extensions;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls;

public class ToolBaseControl : UserControlEx
{
    public ToolWindow? ParentWindow { get; set; }

    private string? _lastValidationMessage;

    public bool CanRun { get; set; } = true;

    public virtual bool ValidateSpawn() => true;
    protected virtual string? ValidateInternally() => null;

    public string? LastValidationMessage
    {
        get => _lastValidationMessage;
        private set
        {
            if(!RaiseAndSetIfChanged(ref _lastValidationMessage, value)) return;
            RaisePropertyChanged(nameof(IsLastValidationSuccess));
        }
    }

    public bool IsLastValidationSuccess => string.IsNullOrWhiteSpace(_lastValidationMessage);

    protected virtual void OnBeforeValidate(){}

    public string? Validate()
    {
        OnBeforeValidate();
        LastValidationMessage = ValidateInternally();
        OnAfterValidate();
        return _lastValidationMessage;
    }

    protected virtual void OnAfterValidate() { }

    /// <summary>
    /// Validates if is safe to continue with operation
    /// </summary>
    /// <returns></returns>
    public virtual async Task<bool> ValidateForm()
    {
        return await ValidateFormFromString(Validate());
    }

    /// <summary>
    /// Validates if is safe to continue with operation, if not shows a message box with the error
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public async Task<bool> ValidateFormFromString(string? text)
    {
        if (string.IsNullOrEmpty(text)) return true;
        await ParentWindow!.MessageBoxError(text);
        return false;
    }

    /// <summary>
    /// Called before process, ie click on Ok button
    /// </summary>
    /// <returns>True if can continue processing the operation, otherwise false</returns>
    public virtual Task<bool> OnBeforeProcess() => Task.FromResult(true);

    /// <summary>
    /// Called after process, ie click on Ok button
    /// </summary>
    /// <returns></returns>
    public virtual Task<bool> OnAfterProcess() => Task.FromResult(true);
}