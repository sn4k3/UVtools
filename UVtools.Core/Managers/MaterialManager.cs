/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;
using ZLinq;

namespace UVtools.Core.Managers;

public partial class MaterialManager : ObservableObject, IList<Material>
{
    #region Settings

    public static string FilePath = Path.Combine(CoreSettings.DefaultSettingsFolderAndEnsureCreation, "materials.xml");
    #endregion

    #region Singleton

    private static Lazy<MaterialManager> _instanceHolder =
        new(() => []);

    /// <summary>
    /// Instance of <see cref="MaterialManager"/> (singleton)
    /// </summary>
    public static MaterialManager Instance => _instanceHolder.Value;

    //public static List<Operation> Operations => _instance.Operations;
    #endregion

    #region Members

    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BottlesInStock), nameof(OwnedBottles), nameof(ConsumedVolume),
        nameof(ConsumedVolumeLiters), nameof(VolumeInStock), nameof(VolumeInStockLiters),
        nameof(TotalCost), nameof(PrintTime), nameof(PrintTimeSpan), nameof(Count))]
    public partial RangeObservableCollection<Material> Materials { get; set; } = [];

    /// <summary>
    /// Gets the total number of bottles in stock
    /// </summary>
    public int BottlesInStock => this.AsValueEnumerable().Sum(material => material.BottlesInStock);

    /// <summary>
    /// Gets the total number of bottles ever owned
    /// </summary>
    public int OwnedBottles => this.AsValueEnumerable().Sum(material => material.OwnedBottles);

    /// <summary>
    /// Gets the total of consumed volume in milliliters
    /// </summary>
    public decimal ConsumedVolume => this.AsValueEnumerable().Sum(material => material.ConsumedVolume);

    /// <summary>
    /// Gets the total of consumed volume in liters
    /// </summary>
    public decimal ConsumedVolumeLiters => this.AsValueEnumerable().Sum(material => material.ConsumedVolumeLiters);

    /// <summary>
    /// Gets the total volume in stock in milliliters
    /// </summary>
    public decimal VolumeInStock => this.AsValueEnumerable().Sum(material => material.VolumeInStock);

    /// <summary>
    /// Gets the total volume in stock in liters
    /// </summary>
    public decimal VolumeInStockLiters => VolumeInStock / 1000;

    /// <summary>
    /// Gets the total costs
    /// </summary>
    public decimal TotalCost => this.AsValueEnumerable().Sum(material => material.TotalCost);

    /// <summary>
    /// Gets the total print time in hours
    /// </summary>
    public double PrintTime => this.AsValueEnumerable().Sum(material => material.PrintTime);

    /// <summary>
    /// Gets the total print time
    /// </summary>
    public TimeSpan PrintTimeSpan => TimeSpan.FromHours(PrintTime);

    #endregion

    #region Constructor
    private MaterialManager()
    {
    }
    #endregion

    #region Methods
    public Material this[int index]
    {
        get => Materials[index];
        set => Materials[index] = value;
    }

    public Material this[uint index]
    {
        get => Materials[(int) index];
        set => Materials[(int) index] = value;
    }

    public Material? this[Material material]
    {
        get
        {
            var index = IndexOf(material);
            return index < 0 ? this[index] : null;
        }
        set
        {
            var index = IndexOf(material);
            if(index >= 0) this[index] = value!;
        }
    }

    public IEnumerator<Material> GetEnumerator() => Materials.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(Material item)
    {
        Materials.Add(item);
        RaisePropertiesChanged();
    }

    public void Add(Material item, bool save)
    {
        Add(item);
        if (save) Save();
    }

    public void Clear()
    {
        Materials.Clear();
        RaisePropertiesChanged();
    }

    public void Clear(bool save)
    {
        Clear();
        if(save) Save();
    }


    public bool Contains(Material item) => Materials.Contains(item);

    public void CopyTo(Material[] array, int arrayIndex)
    {
        Materials.CopyTo(array, arrayIndex);
        RaisePropertiesChanged();
    }

    public bool Remove(Material item)
    {
        if (Materials.Remove(item))
        {
            RaisePropertiesChanged();
            return true;
        }

        return false;
    }

    public void Remove(Material item, bool save)
    {
        Remove(item);
        if (save) Save();
    }

    public void RemoveRange(IEnumerable<Material> collection)
    {
        Materials.RemoveRange(collection);
    }

    public int Count => Materials.Count;
    public bool IsReadOnly => false;
    public int IndexOf(Material item) => Materials.IndexOf(item);

    public void Insert(int index, Material item)
    {
        Materials.Insert(index, item);
        RaisePropertiesChanged();
    }

    public void Insert(int index, Material item, bool save)
    {
        Insert(index, item);
        if (save) Save();
    }

    public void RemoveAt(int index)
    {
        Materials.RemoveAt(index);
        RaisePropertiesChanged();
    }

    public void RemoveAt(int index, bool save)
    {
        RemoveAt(index);
        if (save) Save();
    }

    public void RaisePropertiesChanged()
    {
        OnPropertyChanged(nameof(BottlesInStock));
        OnPropertyChanged(nameof(OwnedBottles));
        OnPropertyChanged(nameof(ConsumedVolume));
        OnPropertyChanged(nameof(ConsumedVolumeLiters));
        OnPropertyChanged(nameof(VolumeInStock));
        OnPropertyChanged(nameof(VolumeInStockLiters));
        OnPropertyChanged(nameof(TotalCost));
        OnPropertyChanged(nameof(PrintTime));
        OnPropertyChanged(nameof(PrintTimeSpan));
        OnPropertyChanged(nameof(Count));
    }

    /// <summary>
    /// Load settings from file
    /// </summary>
    public static void Load()
    {
        if (string.IsNullOrWhiteSpace(FilePath) || !File.Exists(FilePath))
        {
            return;
        }

        try
        {
            var instance = XmlExtensions.DeserializeFromFile<MaterialManager>(FilePath);
            _instanceHolder = new Lazy<MaterialManager>(() => instance);
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

    public void SortByName()
    {
        Materials.Sort((material, material1) => string.Compare(material.Name, material1.Name, StringComparison.Ordinal));
    }

    #endregion
}
