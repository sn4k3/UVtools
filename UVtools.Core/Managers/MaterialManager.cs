/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;

namespace UVtools.Core.Managers;

public class MaterialManager : BindableBase, IList<Material>
{
    #region Settings

    public static string FilePath = Path.Combine(CoreSettings.DefaultSettingsFolderAndEnsureCreation, "materials.xml");
    #endregion

    #region Singleton

    private static Lazy<MaterialManager> _instanceHolder =
        new(() => new MaterialManager());

    /// <summary>
    /// Instance of <see cref="MaterialManager"/> (singleton)
    /// </summary>
    public static MaterialManager Instance => _instanceHolder.Value;

    //public static List<Operation> Operations => _instance.Operations;
    #endregion

    #region Members

    private RangeObservableCollection<Material> _materials = new();

    #endregion

    #region Properties

    public RangeObservableCollection<Material> Materials
    {
        get => _materials;
        set => RaiseAndSetIfChanged(ref _materials, value);
    }

    /// <summary>
    /// Gets the total number of bottles in stock
    /// </summary>
    public int BottlesInStock => this.Sum(material => material.BottlesInStock);

    /// <summary>
    /// Gets the total number of bottles ever owned
    /// </summary>
    public int OwnedBottles => this.Sum(material => material.OwnedBottles);

    /// <summary>
    /// Gets the total of consumed volume in milliliters
    /// </summary>
    public decimal ConsumedVolume => this.Sum(material => material.ConsumedVolume);

    /// <summary>
    /// Gets the total of consumed volume in liters
    /// </summary>
    public decimal ConsumedVolumeLiters => this.Sum(material => material.ConsumedVolumeLiters);

    /// <summary>
    /// Gets the total volume in stock in milliliters
    /// </summary>
    public decimal VolumeInStock => this.Sum(material => material.VolumeInStock);

    /// <summary>
    /// Gets the total volume in stock in liters
    /// </summary>
    public decimal VolumeInStockLiters => VolumeInStock / 1000;

    /// <summary>
    /// Gets the total costs
    /// </summary>
    public decimal TotalCost => this.Sum(material => material.TotalCost);

    /// <summary>
    /// Gets the total print time in hours
    /// </summary>
    public double PrintTime => this.Sum(material => material.PrintTime);

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
        get => _materials[index];
        set => _materials[index] = value;
    }

    public Material this[uint index]
    {
        get => _materials[(int) index];
        set => _materials[(int) index] = value;
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

    public IEnumerator<Material> GetEnumerator() => _materials.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(Material item)
    {
        _materials.Add(item);
        RaisePropertiesChanged();
    }

    public void Add(Material item, bool save)
    {
        Add(item);
        if (save) Save();
    }

    public void Clear()
    {
        _materials.Clear();
        RaisePropertiesChanged();
    }

    public void Clear(bool save)
    {
        Clear();
        if(save) Save();
    }


    public bool Contains(Material item) => _materials.Contains(item);

    public void CopyTo(Material[] array, int arrayIndex)
    {
        _materials.CopyTo(array, arrayIndex);
        RaisePropertiesChanged();
    }

    public bool Remove(Material item)
    {
        if (_materials.Remove(item))
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
        _materials.RemoveRange(collection);
    }

    public int Count => _materials.Count;
    public bool IsReadOnly => false;
    public int IndexOf(Material item) => _materials.IndexOf(item);

    public void Insert(int index, Material item)
    {
        _materials.Insert(index, item);
        RaisePropertiesChanged();
    }

    public void Insert(int index, Material item, bool save)
    {
        Insert(index, item);
        if (save) Save();
    }

    public void RemoveAt(int index)
    {
        _materials.RemoveAt(index);
        RaisePropertiesChanged();
    }

    public void RemoveAt(int index, bool save)
    {
        RemoveAt(index);
        if (save) Save();
    }

    public void RaisePropertiesChanged()
    {
        RaisePropertyChanged(nameof(BottlesInStock));
        RaisePropertyChanged(nameof(OwnedBottles));
        RaisePropertyChanged(nameof(ConsumedVolume));
        RaisePropertyChanged(nameof(ConsumedVolumeLiters));
        RaisePropertyChanged(nameof(VolumeInStock));
        RaisePropertyChanged(nameof(VolumeInStockLiters));
        RaisePropertyChanged(nameof(TotalCost));
        RaisePropertyChanged(nameof(PrintTime));
        RaisePropertyChanged(nameof(PrintTimeSpan));
        RaisePropertyChanged(nameof(Count));
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
        _materials.Sort((material, material1) => string.Compare(material.Name, material1.Name, StringComparison.Ordinal));
    }

    #endregion
}