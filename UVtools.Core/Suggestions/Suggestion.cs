/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using UVtools.Core.Extensions;
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

    public string Id => GetType().Name.Remove(0, "Suggestion".Length);

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
        if (!Enabled || !IsAvailable || IsApplied || IsInformativeOnly || !SlicerFile.HaveLayers) return false;

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

    /// <summary>
    /// Serialize class to XML file
    /// </summary>
    /// <param name="path"></param>
    /// <param name="indent"></param>
    public void Serialize(string path, bool indent = false)
    {
        if (indent) XmlExtensions.SerializeToFile(this, path, XmlExtensions.SettingsIndent);
        else XmlExtensions.SerializeToFile(this, path);
    }

    public Suggestion Clone()
    {
        return (MemberwiseClone() as Suggestion)!;
    }
    #endregion

    #region Static Methods

    /// <summary>
    /// Create an instance from a class name or file path
    /// </summary>
    /// <param name="classNamePath">Classname or path to a file</param>
    /// <param name="enableXmlProfileFile">If true, it will attempt to deserialize the suggestion from a file profile.</param>
    /// <param name="slicerFile"></param>
    /// <returns></returns>
    public static Suggestion? CreateInstance(string classNamePath, bool enableXmlProfileFile = false, FileFormat? slicerFile = null)
    {
        if (string.IsNullOrWhiteSpace(classNamePath)) return null;
        if (enableXmlProfileFile)
        {
            var suggestionFile = Deserialize(classNamePath, slicerFile);
            if (suggestionFile is not null) return suggestionFile;
        }

        var baseName = "Suggestion";
        if (classNamePath.StartsWith(baseName)) classNamePath = classNamePath.Remove(0, baseName.Length);
        if (classNamePath == string.Empty) return null;

        var baseType = typeof(Suggestion).FullName;
        if (string.IsNullOrWhiteSpace(baseType)) return null;
        var classname = baseType + classNamePath + ", UVtools.Core";
        var type = Type.GetType(classname);

        var suggestion = type?.CreateInstance() as Suggestion;
        if (suggestion is not null && slicerFile is not null)
        {
            suggestion.SlicerFile = slicerFile;
        }

        return suggestion;
    }

    /// <summary>
    /// Deserialize <see cref="Suggestion"/> from a XML file
    /// </summary>
    /// <param name="path">XML file path</param>
    /// <param name="slicerFile"></param>
    /// <returns></returns>
    public static Suggestion? Deserialize(string path, FileFormat? slicerFile = null)
    {
        if (!File.Exists(path)) return null;

        var fileText = File.ReadAllText(path);
        var match = Regex.Match(fileText, @"(?:<\/\s*Suggestion)([a-zA-Z0-9_]+)(?:\s*>)");
        if (!match.Success) return null;
        if (match.Groups.Count < 1) return null;
        var suggestionName = match.Groups[1].Value;
        var baseType = typeof(Suggestion).FullName;
        if (string.IsNullOrWhiteSpace(baseType)) return null;
        var classname = baseType + suggestionName + ", UVtools.Core";
        var type = Type.GetType(classname);
        if (type is null) return null;

        return Deserialize(path, type, slicerFile);
    }

    /// <summary>
    /// Deserialize <see cref="Suggestion"/> from a XML file
    /// </summary>
    /// <param name="path">XML file path</param>
    /// <param name="type"></param>
    /// <param name="slicerFile"></param>
    /// <returns></returns>
    public static Suggestion? Deserialize(string path, Type type, FileFormat? slicerFile = null)
    {
        var serializer = new XmlSerializer(type);
        using var stream = File.OpenRead(path);
        var suggestion = serializer.Deserialize(stream) as Suggestion;
        if (suggestion is not null && slicerFile is not null)
        {
            suggestion.SlicerFile = slicerFile;
        }
        return suggestion;
    }

    /// <summary>
    /// Deserialize <see cref="Suggestion"/> from a XML file
    /// </summary>
    /// <param name="path">XML file path</param>
    /// <param name="suggestion"></param>
    /// <param name="slicerFile"></param>
    /// <returns></returns>
    public static Suggestion? Deserialize(string path, Suggestion suggestion, FileFormat? slicerFile = null) => Deserialize(path, suggestion.GetType(), slicerFile);

    #endregion
}