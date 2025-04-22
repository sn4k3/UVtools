/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.PixelEditor;

namespace UVtools.UI.Structures;


public class PixelEditorProfiles //: IList<Operation>
{
    #region Properties
    /// <summary>
    /// Default filepath for store <see cref="PixelEditorProfiles"/>
    /// </summary>
    private static string FilePath => Path.Combine(UserSettings.SettingsFolder, "pixeleditor_profiles.xml");

    [XmlElement(typeof(PixelDrawing))]
    [XmlElement(typeof(PixelText))]
    [XmlElement(typeof(PixelFill))]
    [XmlElement(typeof(PixelDrainHole))]
    [XmlElement(typeof(PixelSupport))]
    public List<PixelOperation> Profiles { get; internal set; } = [];

    [XmlIgnore]
    public static List<PixelOperation> ProfileList
    {
        get => Instance.Profiles;
        internal set => Instance.Profiles = value;
    }

    #endregion

    #region Singleton

    private static Lazy<PixelEditorProfiles> _instanceHolder = new(() => new PixelEditorProfiles());

    /// <summary>
    /// Instance (singleton)
    /// </summary>
    public static PixelEditorProfiles Instance => _instanceHolder.Value;
    #endregion

    #region Constructor

    private PixelEditorProfiles()
    { }

    #endregion

    #region Indexers

    public PixelOperation this[uint index]
    {
        get => Profiles[(int) index];
        set => Profiles[(int) index] = value;
    }

    public PixelOperation this[int index]
    {
        get => Profiles[index];
        set => Profiles[index] = value;
    }

    #endregion

    #region Enumerable
        
    public IEnumerator<PixelOperation> GetEnumerator()
    {
        return Profiles.GetEnumerator();
    }

    /*IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }*/
    #endregion

    #region List Implementation
    public void Add(PixelOperation item)
    {
        Profiles.Add(item);
    }

    public int IndexOf(PixelOperation item)
    {
        return Profiles.IndexOf(item);
    }

    public void Insert(int index, PixelOperation item)
    {
        Profiles.Insert(index, item);
    }

    public bool Remove(PixelOperation item)
    {
        return Profiles.Remove(item);
    }

    public void RemoveAt(int index)
    {
        Profiles.RemoveAt(index);
    }

    public void Clear()
    {
        Profiles.Clear();
    }

    public bool Contains(PixelOperation item)
    {
        return Profiles.Contains(item);
    }

    public void CopyTo(PixelOperation[] array, int arrayIndex)
    {
        Profiles.CopyTo(array, arrayIndex);
    }

    public int Count => Profiles.Count;
    public bool IsReadOnly => false;
    #endregion

    #region Static Methods
    /// <summary>
    /// Clear all profiles
    /// </summary>
    /// <param name="save">True to save settings on file, otherwise false</param>
    public static void ClearProfiles(bool save = true)
    {
        Instance.Clear();
        if (save) Save();
    }

    /// <summary>
    /// Clear all profiles given a type
    /// </summary>
    /// <param name="type">Type profiles to clear</param>
    /// <param name="save">True to save settings on file, otherwise false</param>
    public static void ClearProfiles(Type type, bool save = true)
    {
        ProfileList.RemoveAll(operation => operation.GetType() == type);
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
            var instance = XmlExtensions.DeserializeFromFile<PixelEditorProfiles>(FilePath);
            _instanceHolder = new Lazy<PixelEditorProfiles>(() => instance);
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.ToString());
            ClearProfiles();
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

    public static PixelOperation? FindByName(PixelOperation baseProfile, string? profileName)
    {
        return ProfileList.Find(operation =>
            operation.GetType() == baseProfile.GetType() &&
            (
                !string.IsNullOrWhiteSpace(profileName) && operation.ProfileName == profileName
                || operation.Equals(baseProfile)
            ));
    }


    /// <summary>
    /// Adds a profile
    /// </summary>
    /// <param name="profile"></param>
    /// <param name="save"></param>
    public static void AddProfile(PixelOperation profile, bool save = true)
    {
        ProfileList.Insert(0, profile);
        if(save) Save();
    }

    /// <summary>
    /// Removes a profile
    /// </summary>
    /// <param name="profile"></param>
    /// <param name="save"></param>
    public static void RemoveProfile(PixelOperation profile, bool save = true)
    {
        Instance.Remove(profile);
        if (save) Save();
    }

    /// <summary>
    /// Get all profiles within a type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static List<PixelOperation> GetProfiles(Type type) 
        => ProfileList.Where(operation => operation.GetType() == type).ToList();

    /// <summary>
    /// Get all profiles within a type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> GetProfiles<T>()
    {
        var result = new List<T>();
        foreach (var profile in Instance)
        {
            if (profile is T profileCast)
            {
                result.Add(profileCast);
            }
        }

        return result;
    }

    #endregion
}