/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UVtools.Core.Objects;

/// <summary>
/// Represents a material to feed in the printer
/// </summary>
public partial class Material : ObservableObject, ICloneable
{
    #region Members
    private decimal _bottleRemainingVolume = 1000;

    #endregion

    #region Properties
    [ObservableProperty]
    public partial string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the bottle volume in milliliters
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BottleWeight), nameof(ConsumedBottles), nameof(OwnedBottles), nameof(TotalCost), nameof(VolumeInStock))]
    public partial uint BottleVolume { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the bottle weight in grams
    /// </summary>
    public decimal BottleWeight => BottleVolume * Density;

    /// <summary>
    /// Gets or sets the material density in g/ml
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BottleWeight))]
    public partial decimal Density { get; set; } = 1;

    /// <summary>
    /// Gets or sets the bottle cost
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalCost))]
    public partial decimal BottleCost { get; set; } = 30;

    public decimal TotalCost => OwnedBottles * BottleCost;

    /// <summary>
    /// Gets or sets the number of bottles in stock
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OwnedBottles), nameof(TotalCost), nameof(VolumeInStock))]
    public partial int BottlesInStock { get; set; } = 1;

    /// <summary>
    /// Gets or sets the current bottle remaining material in milliliters
    /// </summary>
    public decimal BottleRemainingVolume
    {
        get => Math.Round(_bottleRemainingVolume, 2);
        set
        {
            if(!SetProperty(ref _bottleRemainingVolume, value)) return;
            OnPropertyChanged(nameof(VolumeInStock));
        }
    }

    /// <summary>
    /// Gets the total available volume in stock in milliliters
    /// </summary>
    public decimal VolumeInStock => BottlesInStock * BottleVolume - (BottleVolume - _bottleRemainingVolume);

    /// <summary>
    /// Gets the number of consumed bottles 
    /// </summary>
    public uint ConsumedBottles => (uint)(ConsumedVolume / BottleVolume);

    /// <summary>
    /// Gets the total number of owned bottles
    /// </summary>
    public int OwnedBottles => BottlesInStock + (int)ConsumedBottles;

    /// <summary>
    /// Gets or sets the total number of consumed volume in milliliters
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ConsumedVolumeLiters), nameof(ConsumedBottles), nameof(OwnedBottles), nameof(TotalCost))]
    public partial decimal ConsumedVolume { get; set; }

    /// <summary>
    /// Gets total number of consumed volume in liters
    /// </summary>
    public decimal ConsumedVolumeLiters => ConsumedVolume / 1000;

    /// <summary>
    /// Gets or sets the total print time using with material in hours
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PrintTimeSpan))]
    public partial double PrintTime { get; set; }

    public TimeSpan PrintTimeSpan => TimeSpan.FromHours(PrintTime);

    #endregion

    #region Constructors
    public Material() { }

    public Material(string name, uint bottleVolume = 1000, decimal density = 1, decimal bottleCost = 30, int bottlesInStock = 1)
    {
        Name = name;
        BottleVolume = bottleVolume;
        Density = density;
        BottleCost = bottleCost;
        BottlesInStock = bottlesInStock;
        _bottleRemainingVolume = bottleVolume;
    }
    #endregion

    #region Overrides

    protected bool Equals(Material other)
    {
        return Name == other.Name;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((Material) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Name);
    }

    public override string ToString()
    {
        return $"{Name} ({_bottleRemainingVolume}/{VolumeInStock}ml)";
    }

    public object Clone()
    {
        return MemberwiseClone();
    }

    public Material CloneMaterial()
    {
        return (Material)Clone();
    }

    #endregion

    #region Methods
    /// <summary>
    /// Gets the cost for a given volume
    /// </summary>
    /// <param name="volume">Volume in ml</param>
    /// <returns></returns>
    public decimal GetVolumeCost(decimal volume) => BottleVolume > 0 ? volume * BottleCost / BottleVolume : 0;

    /// <summary>
    /// Gets the grams for a given volume
    /// </summary>
    /// <param name="volume">Volume in ml</param>
    /// <returns></returns>
    public decimal GetVolumeGrams(decimal volume) => volume * Density;

    /// <summary>
    /// Consume material from current bottle and manage stock
    /// </summary>
    /// <param name="volume">Volume to consume in milliliters</param>
    /// <param name="printSeconds">Time in seconds it took to print</param>
    /// <returns>True if still have bottles in stock, otherwise false</returns>
    public bool Consume(decimal volume, double printSeconds = 0)
    {
        if (volume <= 0 || BottleVolume == 0) return true; // Safe check
        int consumedBottles = (int)(volume / BottleVolume);
        decimal remainder = volume % BottleVolume;

        if (remainder > 0)
        {
            decimal remainingVolume = _bottleRemainingVolume - remainder;
            if (remainingVolume < 0)
            {
                consumedBottles++;
                remainingVolume += BottleVolume;
            }

            BottleRemainingVolume = remainingVolume;
        }

        BottlesInStock -= consumedBottles;
        ConsumedVolume += volume;

        AddPrintTimeSeconds(printSeconds);

        return BottlesInStock > 0;
    }

    /// <summary>
    /// Add print time with this material
    /// </summary>
    /// <param name="seconds">Seconds to add</param>
    public void AddPrintTimeSeconds(double seconds)
    {
        if (seconds <= 0) return;
        PrintTime += seconds / 60 / 60;
    }
    #endregion
}
