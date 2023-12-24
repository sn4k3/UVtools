using Avalonia.Controls;
using System.Threading.Tasks;
using UVtools.Core.Operations;
using UVtools.UI.Extensions;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools;

public class ToolControl : ToolBaseControl
{
    private Operation? _baseOperation;

    public Operation? BaseOperation
    {
        get => _baseOperation;
        set
        {
            bool wasNullBefore = _baseOperation is null;

            if (!wasNullBefore)
            {
                _baseOperation!.ClearPropertyChangedListeners();
                Callback(ToolWindow.Callbacks.BeforeLoadProfile);
            }

            _baseOperation = value;
            if (_baseOperation is not null) _baseOperation.SlicerFile = SlicerFile!;
            RaisePropertyChanged();

            if (!wasNullBefore)
            {
                Callback(ToolWindow.Callbacks.AfterLoadProfile);
            }

            if (DataContext is null) return;
            ResetDataContext();
        }
    }

    public ToolControl()
    {
    }

    public ToolControl(Operation operation)
    {
        BaseOperation = operation;
        if (!ValidateSpawn()) return;
    }

    public override bool ValidateSpawn()
    {
        if (Design.IsDesignMode) return true;
        if (_baseOperation is null)
        {
            App.MainWindow.MessageBoxInfo("The operation does not contain a valid configuration.\n" +
                                          "Please contact the support/developer.", _baseOperation?.NotSupportedTitle).ConfigureAwait(true);
            CanRun = false;
            return false;
        }

        if (!_baseOperation.ValidateSpawn(out var message))
        {
            App.MainWindow.MessageBoxInfo(message!, _baseOperation!.NotSupportedTitle).ConfigureAwait(true);
            CanRun = false;
            return false;
        }

        return true;
    }


    public virtual void Callback(ToolWindow.Callbacks callback) { }

    public virtual bool UpdateOperation() => true;

    protected override string? ValidateInternally()
    {
        return BaseOperation?.Validate();
    }
    
    public override async Task<bool> ValidateForm()
    {
        if (BaseOperation is null) return true;
        if (!UpdateOperation()) return false;
        return await base.ValidateForm();
    }
}