/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.Suggestions;

namespace UVtools.WPF.Structures;

[Serializable]
public class SuggestionManager
{
    #region Properties
    /// <summary>
    /// Default filepath for store
    /// </summary>
    public static string FilePath => Path.Combine(CoreSettings.DefaultSettingsFolderAndEnsureCreation, "suggestions.xml");

    public SuggestionBottomLayerCount BottomLayerCount { get; set; } = new();
    public SuggestionTransitionLayerCount TransitionLayerCount { get; set; } = new();
    public SuggestionWaitTimeBeforeCure WaitTimeBeforeCure { get; set; } = new();
    public SuggestionWaitTimeAfterCure WaitTimeAfterCure { get; set; } = new();
    public SuggestionLayerHeight LayerHeight { get; set; } = new();
    public SuggestionModelPosition ModelPosition { get; set; } = new();

    /// <summary>
    /// Gets all suggestions
    /// </summary>
    [XmlIgnore]
    public Suggestion[] Suggestions
    {
        get
        {
            return new Suggestion[]
            {
                BottomLayerCount,
                TransitionLayerCount,
                WaitTimeBeforeCure,
                WaitTimeAfterCure,
                LayerHeight,
                ModelPosition,
            };
        }
        set
        {
            foreach (var suggestion in value)
            {
                Set(suggestion);
            }

            Save();
        }
    }

    public void Set(Suggestion suggestion)
    {
        switch (suggestion)
        {
            case SuggestionBottomLayerCount bottomLayerCount:
                BottomLayerCount = bottomLayerCount;
                break;
            case SuggestionTransitionLayerCount transitionLayerCount:
                TransitionLayerCount = transitionLayerCount;
                break;
            case SuggestionWaitTimeBeforeCure waitTimeBeforeCure:
                WaitTimeBeforeCure = waitTimeBeforeCure;
                break;
            case SuggestionWaitTimeAfterCure waitTimeAfterCure:
                WaitTimeAfterCure = waitTimeAfterCure;
                break;
            case SuggestionLayerHeight layerHeight:
                LayerHeight = layerHeight;
                break;
            case SuggestionModelPosition modelPosition:
                ModelPosition = modelPosition;
                break;
            default: throw new ArgumentOutOfRangeException(nameof(suggestion));
        }
    }

    public Suggestion? Get(Type type)
    {
        return Suggestions.FirstOrDefault(suggestion => suggestion.GetType() == type);
    }

    #endregion

    #region Singleton

    private static Lazy<SuggestionManager> _instanceHolder =
        new(() => new SuggestionManager());

    /// <summary>
    /// Instance (singleton)
    /// </summary>
    public static SuggestionManager Instance => _instanceHolder.Value;

    //public static List<Operation> Operations => _instance.Operations;
    #endregion

    #region Constructor

    private SuggestionManager()
    { }

    #endregion

    #region Static Methods

    public static void SetSuggestion(Suggestion suggestion, bool save = false)
    {
        Instance.Set(suggestion);
        if(save) Save();
    }

    public static Suggestion? GetSuggestion(Type type)
    {
        return Instance.Get(type);
    }

    /// <summary>
    /// Reset to defaults
    /// </summary>
    /// <param name="save">True to save settings on file, otherwise false</param>
    public static void Reset(bool save = true)
    {
        _instanceHolder = new Lazy<SuggestionManager>(() => new SuggestionManager());
        if (save) Save();
    }


    /// <summary>
    /// Load settings from file
    /// </summary>
    public static void Load()
    {
        if (!File.Exists(FilePath))
        {
            return;
        }

        try
        {
            var instance = XmlExtensions.DeserializeFromFile<SuggestionManager>(FilePath);
            _instanceHolder = new Lazy<SuggestionManager>(() => instance);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.ToString());
        }
    }

    /// <summary>
    /// Save settings to file
    /// </summary>
    public static void Save()
    {
        try
        {
            XmlExtensions.SerializeToFile(Instance, FilePath, XmlExtensions.SettingsIndent);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.ToString());
        }
    }

    #endregion
}