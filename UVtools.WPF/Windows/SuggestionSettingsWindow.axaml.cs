using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Linq;
using System.Threading.Tasks;
using UVtools.Core.Dialogs;
using UVtools.Core.Extensions;
using UVtools.Core.Managers;
using UVtools.Core.Suggestions;
using UVtools.WPF.Controls;
using UVtools.WPF.Controls.Suggestions;
using UVtools.WPF.Extensions;
using Helpers = UVtools.WPF.Controls.Helpers;

namespace UVtools.WPF.Windows;

public partial class SuggestionSettingsWindow : WindowEx
{
    private readonly ContentControl _activeSuggestionContentPanel;

    private Suggestion _activeSuggestion;
    private Suggestion _selectedSuggestion;
    private bool _pendingChanges;

    public Suggestion[] Suggestions => App.MainWindow.Suggestions;

    public Suggestion SelectedSuggestion
    {
        get => _selectedSuggestion;
        set
        {
            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                bool recoverSuggestion = false;
                if (_pendingChanges && _activeSuggestion is not null)
                {
                    switch (await this.MessageBoxQuestion(
                                $"You have pending changes on '{_activeSuggestion.Title}' and you are about to discard them.\n" +
                                "Do you want to discard the changes?\n\n" +
                                "Yes: Discard changes\n" +
                                "No: Save changes\n" +
                                "Cancel: Continue editing",
                                "Pending changes", MessageButtons.YesNoCancel))
                    {
                        case MessageButtonResult.Yes:
                            break;
                        case MessageButtonResult.No:
                            if (!await SaveSuggestion(false))
                            {
                                recoverSuggestion = true;
                                break;
                            }
                            SuggestionManager.SetSuggestion(_activeSuggestion.Clone(), true);
                            break;
                        case MessageButtonResult.Cancel:
                        case MessageButtonResult.Abort:
                        case MessageButtonResult.None:
                            recoverSuggestion = true;
                            break;
                    }
                }


                var oldSuggestion = _selectedSuggestion;
                if (!RaiseAndSetIfChanged(ref _selectedSuggestion, value)) return;
                if (recoverSuggestion)
                {
                    RaiseAndSetIfChanged(ref _selectedSuggestion, oldSuggestion);
                    return;
                }

                ActiveSuggestion = value is null ? null : SuggestionManager.GetSuggestion(_selectedSuggestion.GetType())?.Clone();
            });

        }
    }

    public Suggestion ActiveSuggestion
    {
        get => _activeSuggestion;
        set
        {
            if(!RaiseAndSetIfChanged(ref _activeSuggestion, value) || value is null) return;
            PendingChanges = false;
            _activeSuggestion.PropertyChanged += (sender, e) => PendingChanges = true;

            var type = typeof(SuggestionControl);
            var classname = $"{type.Namespace}.{_activeSuggestion.GetType().Name}Control";
            var controlType = Type.GetType(classname);
            SuggestionControl control;

            if (controlType is null)
            {
                control = new SuggestionControl(_activeSuggestion);
            }
            else
            {
                control = controlType.CreateInstance<SuggestionControl>(_activeSuggestion);
                if (control is null) return;
            }

            _activeSuggestionContentPanel.Content = null;
            _activeSuggestionContentPanel.Content = control;
        }
    }

    public bool PendingChanges
    {
        get => _pendingChanges;
        set => RaiseAndSetIfChanged(ref _pendingChanges, value);
    }

    public SuggestionSettingsWindow() : this(null) { }

    public SuggestionSettingsWindow(Suggestion highlightSuggestion)
    {
        if (Design.IsDesignMode)
        {
            _activeSuggestion = new SuggestionBottomLayerCount();
        }

        DataContext = this;

        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        _activeSuggestionContentPanel = this.FindControl<ContentControl>("ActiveSuggestionContentPanel");

        if (highlightSuggestion is not null)
        {
            SelectedSuggestion = Suggestions.FirstOrDefault(suggestion => suggestion.GetType() == highlightSuggestion.GetType());
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }


    public async void ResetDefaults()
    {
        if (await this.MessageBoxQuestion(
                "Are you sure you want to reset all suggestions to the default settings?",
                "Reset all settings?") != MessageButtonResult.Yes) return;

        SuggestionManager.Reset();

        foreach (var suggestionMngr in SuggestionManager.Instance.Suggestions)
        {
            suggestionMngr.SlicerFile = SlicerFile;
        }

        var suggestion = (Suggestion)_activeSuggestion?.GetType().CreateInstance();
        if (suggestion == null) return;
        suggestion.SlicerFile = SlicerFile;
        ActiveSuggestion = suggestion;
    }


    public async Task<bool> SaveSuggestion(bool promptBeforeSave = true)
    {
        if (_activeSuggestion is null) return false;

        var result = _activeSuggestion.Validate();
        if (!string.IsNullOrWhiteSpace(result))
        {
            if (await this.MessageBoxError(result,
                    $"{_activeSuggestion.Title} - Error") != MessageButtonResult.Yes) return false;
        }

        if (promptBeforeSave && await this.MessageBoxQuestion(
                "Are you sure you want to save and overwrite previous settings?",
                "Save suggestion changes?") != MessageButtonResult.Yes) return false;

        SuggestionManager.SetSuggestion(_activeSuggestion.Clone(), true);
        PendingChanges = false;
        return true;
    }

    public async void SaveSuggestionClicked()
    {
        await SaveSuggestion();
    }

    public async void DiscardSuggestionClicked()
    {
        if (_activeSuggestion is null) return;

        if (await this.MessageBoxQuestion(
                "Are you sure you want to discard all changes and return to the last saved state?",
                "Discard suggestion changes?") != MessageButtonResult.Yes) return;

        ActiveSuggestion = SuggestionManager.GetSuggestion(_activeSuggestion.GetType())?.Clone();
    }

    public async void ResetSuggestionClicked()
    {
        if (_activeSuggestion is null) return;

        if (await this.MessageBoxQuestion(
                "Are you sure you want to reset to the default settings?",
                "Reset suggestion settings?") != MessageButtonResult.Yes) return;

        var suggestion = (Suggestion)_activeSuggestion.GetType().CreateInstance();
        if (suggestion == null) return;
        suggestion.SlicerFile = SlicerFile;
        ActiveSuggestion = suggestion;
        SuggestionManager.SetSuggestion(suggestion.Clone(), true);
    }

    public async void ImportSettingsClicked()
    {
        var dialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Filters = Helpers.SuggestionSettingFileFilter
        };

        var files = await dialog.ShowAsync(this);

        if (files is null || files.Length == 0) return;

        try
        {
            var suggestion = Suggestion.Deserialize(files[0], SlicerFile);
            if (suggestion is null)
            {
                await this.MessageBoxError("Unable to import settings, file may be malformed.", "Error while trying to import the settings");
                return;
            }
            
            if (_activeSuggestion.GetType() != suggestion.GetType())
            {
                await this.MessageBoxError($"Unable to import '{suggestion.GetType().Name}' to '{_activeSuggestion.GetType().Name}', the type does not match the active suggestion.\n" +
                                           $"Please select the correct profile for the active '{_activeSuggestion.GetType().Name}' suggestion.", "Error while trying to import the settings");
                return;
            }

            ReflectionExtensions.CopyPropertiesTo(suggestion, _activeSuggestion, nameof(_activeSuggestion.SlicerFile), nameof(_activeSuggestion.Enabled), nameof(_activeSuggestion.AutoApply));
        }
        catch (Exception e)
        {
            await this.MessageBoxError(e.ToString(), "Error while trying to import the settings");
        }
    }

    public async void ExportSettingsClicked()
    {
        var dialog = new SaveFileDialog
        {
            Filters = Helpers.SuggestionSettingFileFilter,
            InitialFileName = _activeSuggestion.Id
        };

        var file = await dialog.ShowAsync(this);

        if (string.IsNullOrWhiteSpace(file)) return;

        try
        {
            _activeSuggestion.Serialize(file, true);
        }
        catch (Exception e)
        {
            await this.MessageBoxError(e.ToString(), "Error while trying to export the settings");
        }
    }
}