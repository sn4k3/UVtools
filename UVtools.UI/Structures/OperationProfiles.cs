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
using System.Xml.Serialization;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using ZLinq;

namespace UVtools.UI.Structures;


public class OperationProfiles //: IList<Operation>
{
    #region Properties
    /// <summary>
    /// Default filepath for store <see cref="OperationProfiles"/>
    /// </summary>
    private static string FilePath => Path.Combine(UserSettings.SettingsFolder, "operation_profiles.xml");

    [XmlElement(typeof(OperationRepairLayers))]

    [XmlElement(typeof(OperationResize))]
    [XmlElement(typeof(OperationFlip))]
    [XmlElement(typeof(OperationRotate))]
    [XmlElement(typeof(OperationSolidify))]
    [XmlElement(typeof(OperationMorph))]
    [XmlElement(typeof(OperationRaftRelief))]
    [XmlElement(typeof(OperationRedrawModel))]
    [XmlElement(typeof(OperationThreshold))]
    [XmlElement(typeof(OperationLayerArithmetic))]
    [XmlElement(typeof(OperationPixelArithmetic))]
    [XmlElement(typeof(OperationPixelDimming))]
    [XmlElement(typeof(OperationLightBleedCompensation))]
    [XmlElement(typeof(OperationInfill))]
    [XmlElement(typeof(OperationBlur))]
    [XmlElement(typeof(OperationFadeExposureTime))]
    [XmlElement(typeof(OperationDoubleExposure))]
    [XmlElement(typeof(OperationPhasedExposure))]
    [XmlElement(typeof(OperationDynamicLayerHeight))]
    [XmlElement(typeof(OperationDynamicLifts))]
    [XmlElement(typeof(OperationRaiseOnPrintFinish))]
    [XmlElement(typeof(OperationChangeResolution))]
    [XmlElement(typeof(OperationTimelapse))]
    [XmlElement(typeof(OperationLithophane))]
    [XmlElement(typeof(OperationPCBExposure))]
    [XmlElement(typeof(OperationStirResin))]
    [XmlElement(typeof(OperationScripting))]

    [XmlElement(typeof(OperationLayerExportGif))]
    [XmlElement(typeof(OperationLayerExportHtml))]

    [XmlElement(typeof(OperationCalibrateExposureFinder))]
    [XmlElement(typeof(OperationCalibrateElephantFoot))]
    [XmlElement(typeof(OperationCalibrateXYZAccuracy))]
    [XmlElement(typeof(OperationCalibrateLiftHeight))]
    [XmlElement(typeof(OperationCalibrateBloomingEffect))]
    [XmlElement(typeof(OperationCalibrateTolerance))]
    [XmlElement(typeof(OperationCalibrateGrayscale))]
    [XmlElement(typeof(OperationCalibrateStressTower))]
    public List<Operation> Operations { get; internal set; } = [];

    [XmlIgnore]
    public static List<Operation> Profiles
    {
        get => Instance.Operations;
        internal set => Instance.Operations = value;
    }

    #endregion

    #region Singleton

    private static Lazy<OperationProfiles> _instanceHolder = new(() => new OperationProfiles());

    /// <summary>
    /// Instance of <see cref="UserSettings"/> (singleton)
    /// </summary>
    public static OperationProfiles Instance => _instanceHolder.Value;

    //public static List<Operation> Operations => _instance.Operations;
    #endregion

    #region Constructor

    private OperationProfiles()
    { }

    #endregion

    #region Indexers

    public Operation this[uint index]
    {
        get => Operations[(int) index];
        set => Operations[(int) index] = value;
    }

    public Operation this[int index]
    {
        get => Operations[index];
        set => Operations[index] = value;
    }

    #endregion

    #region Enumerable

    public IEnumerator<Operation> GetEnumerator()
    {
        return Operations.GetEnumerator();
    }

    /*IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }*/
    #endregion

    #region List Implementation
    public void Add(Operation item)
    {
        Operations.Add(item);
    }

    public int IndexOf(Operation item)
    {
        return Operations.IndexOf(item);
    }

    public void Insert(int index, Operation item)
    {
        Operations.Insert(index, item);
    }

    public bool Remove(Operation item)
    {
        return Operations.Remove(item);
    }

    public void RemoveAt(int index)
    {
        Operations.RemoveAt(index);
    }

    public void Clear()
    {
        Operations.Clear();
    }

    public bool Contains(Operation item)
    {
        return Operations.Contains(item);
    }

    public void CopyTo(Operation[] array, int arrayIndex)
    {
        Operations.CopyTo(array, arrayIndex);
    }

    public int Count => Operations.Count;
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
        Profiles.RemoveAll(operation => operation.GetType() == type);
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
            var instance = XmlExtensions.DeserializeFromFile<OperationProfiles>(FilePath);
            _instanceHolder = new Lazy<OperationProfiles>(() => instance);
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

    public static Operation? FindByName(Operation baseOperation, string? profileName)
    {
        return Profiles.Find(operation =>
            operation.GetType() == baseOperation.GetType() &&
            (
                !string.IsNullOrWhiteSpace(profileName) && operation.ProfileName == profileName
                || operation.Equals(baseOperation)
            ));
    }


    /// <summary>
    /// Adds a profile
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="save"></param>
    public static void AddProfile(Operation operation, bool save = true)
    {
        Profiles.Insert(0, operation);
        if(save) Save();
    }

    /// <summary>
    /// Removes a profile
    /// </summary>
    /// <param name="operation"></param>
    /// <param name="save"></param>
    public static void RemoveProfile(Operation operation, bool save = true)
    {
        Instance.Remove(operation);
        if (save) Save();
    }

    /// <summary>
    /// Get all profiles within a type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static List<Operation> GetOperations(Type type)
        => Profiles.AsValueEnumerable().Where(operation => operation.GetType() == type).ToList();

    /// <summary>
    /// Get all profiles within a type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static List<T> GetOperations<T>()
    {
        var result = new List<T>();
        foreach (var operation in Instance)
        {
            if (operation is T operationCast)
            {
                result.Add(operationCast);
            }
        }

        return result;
    }

    #endregion
}