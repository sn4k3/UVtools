/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Dialogs;
using UVtools.Core.FileFormats;
using UVtools.Core.Managers;
using UVtools.Core.Suggestions;
using UVtools.UI.Extensions;
using UVtools.UI.Windows;

namespace UVtools.UI;

public partial class MainWindow
{
    #region Properties
    public Suggestion[] Suggestions
    {
        get => SuggestionManager.Instance.Suggestions;
        set => SuggestionManager.Instance.Suggestions = value;
    }

    public RangeObservableCollection<Suggestion> SuggestionsAvailable { get; } = [];
    public RangeObservableCollection<Suggestion> SuggestionsApplied { get; } = [];

    #endregion

    #region Methods
    public void InitSuggestions()
    {
        
    }

    public void PopulateSuggestions(bool tryToAutoApply = true)
    {
        if (SlicerFile is ImageFile) return;

        var suggestionsAvailable = new List<Suggestion>();
        var suggestionsApplied = new List<Suggestion>();
        byte autoApplied = 0;
        foreach (var suggestion in Suggestions)
        {
            suggestion.SlicerFile = SlicerFile!;
            if(!suggestion.Enabled || !suggestion.IsAvailable) continue;
            if (tryToAutoApply && suggestion.ExecuteIfAutoApply())
            {
                autoApplied++;
            }

            if(suggestion.IsApplied) suggestionsApplied.Add(suggestion);
            else suggestionsAvailable.Add(suggestion);
        }

        SuggestionsAvailable.ReplaceCollection(suggestionsAvailable);
        SuggestionsApplied.ReplaceCollection(suggestionsApplied);

        if (autoApplied > 0)
        {
            CanSave = true;
            ResetDataContext();
            ForceUpdateActualLayer();
        }
    }

    public async Task ApplySuggestionsClicked()
    {
        if (!IsFileLoaded || SuggestionsAvailableListBox!.SelectedItems!.Count == 0) return;
        var suggestions = SuggestionsAvailableListBox.SelectedItems.Cast<Suggestion>().Where(suggestion => !suggestion.IsInformativeOnly).ToArray();
        if (suggestions.Length == 0) return;
        var sb = new StringBuilder($"Are you sure you want to apply the following {suggestions.Length} suggestions?:\n\n");

        foreach (var suggestion in suggestions)
        {
            sb.AppendLine(suggestion.ConfirmationMessage);
        }
        if (await this.MessageBoxQuestion(sb.ToString(), "Apply suggestions?") != MessageButtonResult.Yes) return;

        IsGUIEnabled = false;
        ShowProgressWindow($"Applying {suggestions.Length} suggestions", false);

        bool moved = false;

        var executed = await Task.Run(() =>
        {
            uint executed = 0;

            try
            {
                foreach (var suggestion in suggestions)
                {
                    if (suggestion.Execute(Progress))
                    {
                        executed++;
                        if (suggestion is SuggestionModelPosition) moved = true;
                    }
                }
            }
            catch (OperationCanceledException)
            { }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(async () => await this.MessageBoxError(ex.ToString(), "Error while applying a suggestion"));
            }

            return executed;
        });

        IsGUIEnabled = true;

        if (executed > 0)
        {
            // Re-detect issues when move object
            if (moved && SlicerFile!.IssueManager.HaveIssues)
            {
                await OnClickDetectIssues();
            }

            CanSave = true;
            ResetDataContext();
            ForceUpdateActualLayer();
        }

        PopulateSuggestions(false);
    }

    public async Task ApplySuggestionClicked(object suggestionObject)
    {
        if (!IsFileLoaded || suggestionObject is not Suggestion suggestion || suggestion.IsInformativeOnly) return;

        if (await this.MessageBoxQuestion($"Are you sure you want to apply the following suggestion?:\n\n{suggestion.ConfirmationMessage}", "Apply the suggestion?") != MessageButtonResult.Yes) return;


        IsGUIEnabled = false;
        ShowProgressWindow(suggestion.Title, false);

        var result = await Task.Run(() =>
        {
            try
            {
                return suggestion.Execute(Progress);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Dispatcher.UIThread.InvokeAsync(async () => await this.MessageBoxError(ex.ToString(), $"{suggestion.Title} Error"));
            }

            return false;
        });

        IsGUIEnabled = true;

        if (result)
        {
            // Re-detect issues when move object
            if (suggestion is SuggestionModelPosition && SlicerFile!.IssueManager.HaveIssues)
            {
                await OnClickDetectIssues();
            }

            CanSave = true;
            ResetDataContext();
            ForceUpdateActualLayer();
        }

        PopulateSuggestions(false);
    }

    public async Task ConfigureSuggestionsClicked()
    {
        if (!IsFileLoaded || Suggestions.Length == 0) return;
        var window = new SuggestionSettingsWindow();
        await window.ShowDialog(this);
        PopulateSuggestions(false);
    }

    public async Task ConfigureSuggestionClicked(object suggestion)
    {
        if (!IsFileLoaded || Suggestions.Length == 0) return;
        var window = new SuggestionSettingsWindow((Suggestion)suggestion);
        await window.ShowDialog(this);
        PopulateSuggestions(false);
    }

    #endregion
}