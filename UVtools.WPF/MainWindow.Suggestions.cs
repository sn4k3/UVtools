/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using MessageBox.Avalonia.Enums;
using UVtools.Core.Suggestions;
using UVtools.WPF.Extensions;
using UVtools.WPF.Structures;
using UVtools.WPF.Windows;

namespace UVtools.WPF;

public partial class MainWindow
{
    #region Members

    private ListBox _suggestionsAvailableListBox;

    #endregion

    #region Properties
    public Suggestion[] Suggestions
    {
        get => SuggestionManager.Instance.Suggestions;
        set => SuggestionManager.Instance.Suggestions = value;
    }

    public RangeObservableCollection<Suggestion> SuggestionsAvailable { get; } = new();
    public RangeObservableCollection<Suggestion> SuggestionsApplied { get; } = new();

    #endregion

    #region Methods
    public void InitSuggestions()
    {
        _suggestionsAvailableListBox = this.FindControl<ListBox>("SuggestionsAvailableListBox");
    }

    public void PopulateSuggestions(bool tryToAutoApply = true)
    {
        var suggestionsAvailable = new List<Suggestion>();
        var suggestionsApplied = new List<Suggestion>();
        foreach (var suggestion in Suggestions)
        {
            suggestion.SlicerFile = SlicerFile;
            if(!suggestion.Enabled || !suggestion.IsAvailable) continue;
            if (tryToAutoApply)
            {
                if (suggestion.ExecuteIfAutoApply())
                {
                    CanSave = true;
                }
            }

            if(suggestion.IsApplied) suggestionsApplied.Add(suggestion);
            else suggestionsAvailable.Add(suggestion);
        }

        SuggestionsAvailable.ReplaceCollection(suggestionsAvailable);
        SuggestionsApplied.ReplaceCollection(suggestionsApplied);
    }

    public async void ApplySuggestionsClicked()
    {
        if (!IsFileLoaded || _suggestionsAvailableListBox.SelectedItems.Count == 0) return;
        var suggestions = _suggestionsAvailableListBox.SelectedItems.Cast<Suggestion>().Where(suggestion => !suggestion.IsInformativeOnly).ToArray();
        if (suggestions.Length == 0) return;
        var sb = new StringBuilder($"Are you sure you want to apply the following {suggestions.Length} suggestions?:\n\n");

        foreach (var suggestion in suggestions)
        {
            sb.AppendLine(suggestion.ConfirmationMessage);
        }
        if (await this.MessageBoxQuestion(sb.ToString(), "Apply suggestions?") != ButtonResult.Yes) return;

        uint executed = 0;
        foreach (var suggestion in suggestions)
        {
            if (suggestion.Execute())
            {
                executed++;
            }
        }

        if (executed > 0)
        {
            CanSave = true;
            ResetDataContext();
            ForceUpdateActualLayer();
        }

        PopulateSuggestions(false);
    }

    public async void ApplySuggestionClicked(Suggestion suggestion)
    {
        if (!IsFileLoaded || suggestion is null || suggestion.IsInformativeOnly) return;

        if (await this.MessageBoxQuestion($"Are you sure you want to apply the following suggestion?:\n\n{suggestion.ConfirmationMessage}", "Apply the suggestion?") != ButtonResult.Yes) return;

        if (suggestion.Execute())
        {
            CanSave = true;
            ResetDataContext();
            ForceUpdateActualLayer();
        }
        PopulateSuggestions(false);
    }

    public async void ConfigureSuggestionsClicked()
    {
        if (!IsFileLoaded || Suggestions.Length == 0) return;
        var window = new SuggestionSettingsWindow();
        await window.ShowDialog(this);
        PopulateSuggestions(false);
    }

    public async void ConfigureSuggestionClicked(Suggestion suggestion)
    {
        if (!IsFileLoaded || Suggestions.Length == 0) return;
        var window = new SuggestionSettingsWindow(suggestion);
        await window.ShowDialog(this);
        PopulateSuggestions(false);
    }

    #endregion
}