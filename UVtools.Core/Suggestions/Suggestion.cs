/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.Core.Suggestions;

public abstract class Suggestion : BindableBase
{
    #region Enums
    public enum SuggestionApplyWhen : byte
    {
        [Description("Outside limits: Applies when the recommended value does not meet the current limit criteria")]
        OutsideLimits,

        [Description("Different: Applies when the recommended value is different from the current")]
        Different,
    }
    #endregion

    #region Members

    protected FileFormat _slicerFile = null!;
    protected bool _enabled = true;
    protected bool _autoApply;
    protected SuggestionApplyWhen _applyWhen = SuggestionApplyWhen.OutsideLimits;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the <see cref="FileFormat"/>
    /// </summary>
    [XmlIgnore]
    public FileFormat SlicerFile
    {
        get => _slicerFile;
        set
        {
            if (ReferenceEquals(_slicerFile, value)) return;
            _slicerFile = value;
            RaisePropertyChanged();
            RaisePropertyChanged(nameof(IsAvailable));
            RaisePropertyChanged(nameof(IsApplied));
        }
    }

    /// <summary>
    /// Gets or sets if this suggestion is enabled
    /// </summary>
    public bool Enabled
    {
        get => _enabled;
        set => RaiseAndSetIfChanged(ref _enabled, value);
    }

    /// <summary>
    /// Gets or sets if this suggestion can be auto applied once file load
    /// </summary>
    public bool AutoApply
    {
        get => _autoApply;
        set => RaiseAndSetIfChanged(ref _autoApply, value);
    }

    /// <summary>
    /// Gets or sets when to apply the suggestion
    /// </summary>
    public SuggestionApplyWhen ApplyWhen
    {
        get => _applyWhen;
        set => RaiseAndSetIfChanged(ref _applyWhen, value);
    }

    /// <summary>
    /// Gets if this suggestion is informative only and contain no actions to execute
    /// </summary>
    public virtual bool IsInformativeOnly => false;

    /// <summary>
    /// Gets if this suggestion is available given the <see cref="SlicerFile"/>
    /// </summary>
    public virtual bool IsAvailable => true;

    /// <summary>
    /// Gets if this suggestion is already applied given the <see cref="SlicerFile"/>
    /// </summary>
    public abstract bool IsApplied { get; }

    /// <summary>
    /// Gets the title for this suggestion
    /// </summary>
    public abstract string Title { get; }

    public abstract string Description { get; }

    /// <summary>
    /// Gets the message for this suggestion
    /// </summary>
    public abstract string Message { get; } 

    /// <summary>
    /// Gets the tooltip message
    /// </summary>
    public virtual string ToolTip => Description;

    public virtual string? InformationUrl => null;

    /// <summary>
    /// Gets the confirmation message before apply the suggestion
    /// </summary>
    public virtual string? ConfirmationMessage => null;
        

    public string GlobalAppliedMessage => $"✓ {Title}";
    public string GlobalNotAppliedMessage => $"⚠ {Title}";

    #endregion

    #region Methods

    public void RefreshNotifyAll()
    {
        RaisePropertyChanged(nameof(IsAvailable));
        RaisePropertyChanged(nameof(IsApplied));
        RefreshNotifyMessage();
    }

    public void RefreshNotifyMessage()
    {
        RaisePropertyChanged(nameof(Message));
        RaisePropertyChanged(nameof(ToolTip));
    }

    /// <summary>
    /// Validates the settings and return a string message if got any error
    /// </summary>
    /// <returns></returns>
    public virtual string? Validate() => null;

    /// <summary>
    /// Executes and applies the suggestion
    /// </summary>
    /// <param name="progress"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual bool ExecuteInternally(OperationProgress progress)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Executes and applies the suggestion
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool Execute(OperationProgress? progress = null)
    {
        if (_slicerFile is null) throw new InvalidOperationException($"The suggestion '{Title}' can't execute due the lacking of a file parent.");
        if (!Enabled || !IsAvailable || IsApplied || IsInformativeOnly || SlicerFile.LayerCount == 0) return false;

        progress ??= new OperationProgress();
        progress.Title = $"Applying suggestion: {Title}";

        var result = ExecuteInternally(progress);

        RaisePropertyChanged(nameof(IsApplied));
        RefreshNotifyMessage();

        return result;
    }

    /// <summary>
    /// Executes only if this suggestion is marked with <see cref="AutoApply"/> as true
    /// </summary>
    /// <returns></returns>
    public bool ExecuteIfAutoApply()
    {
        return _autoApply && Execute();
    }

    public Suggestion Clone()
    {
        return (MemberwiseClone() as Suggestion)!;
    }
    #endregion


}