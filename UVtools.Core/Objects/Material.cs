/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;

namespace UVtools.Core.Objects
{
    /// <summary>
    /// Represents a material to feed in the printer
    /// </summary>
    public class Material : BindableBase, ICloneable
    {
        #region Members
        private string _name;
        private uint _bottleVolume = 1000;
        private decimal _density = 1;
        private decimal _bottleCost = 30;
        private int _bottlesInStock = 1;
        private decimal _bottleRemainingVolume = 1000;
        private decimal _consumedVolume;
        private double _printTime;

        #endregion

        #region Properties
        public string Name
        {
            get => _name;
            set => RaiseAndSetIfChanged(ref _name, value);
        }

        /// <summary>
        /// Gets or sets the bottle volume in milliliters
        /// </summary>
        public uint BottleVolume
        {
            get => _bottleVolume;
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottleVolume, value)) return;
                RaisePropertyChanged(nameof(BottleWeight));
                RaisePropertyChanged(nameof(ConsumedBottles));
                RaisePropertyChanged(nameof(TotalCost));
                RaisePropertyChanged(nameof(VolumeInStock));
            }
        }

        /// <summary>
        /// Gets or sets the bottle weight in grams
        /// </summary>
        public decimal BottleWeight => _bottleVolume * _density;

        /// <summary>
        /// Gets or sets the material density in g/ml
        /// </summary>
        public decimal Density
        {
            get => _density;
            set
            {
                if(!RaiseAndSetIfChanged(ref _density, value)) return;
                RaisePropertyChanged(nameof(BottleWeight));
            }
        }

        /// <summary>
        /// Gets or sets the bottle cost
        /// </summary>
        public decimal BottleCost
        {
            get => _bottleCost;
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottleCost, value)) return;
                RaisePropertyChanged(nameof(TotalCost));
            }
        }

        public decimal TotalCost => OwnedBottles * _bottleCost;

        /// <summary>
        /// Gets or sets the number of bottles in stock
        /// </summary>
        public int BottlesInStock
        {
            get => _bottlesInStock;
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottlesInStock, value)) return;
                RaisePropertyChanged(nameof(OwnedBottles));
                RaisePropertyChanged(nameof(TotalCost));
                RaisePropertyChanged(nameof(VolumeInStock));
            }
        }

        /// <summary>
        /// Gets or sets the current bottle remaining material in milliliters
        /// </summary>
        public decimal BottleRemainingVolume
        {
            get => Math.Round(_bottleRemainingVolume, 2);
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottleRemainingVolume, value)) return;
                RaisePropertyChanged(nameof(VolumeInStock));
            }
        }

        /// <summary>
        /// Gets the total available volume in stock in milliliters
        /// </summary>
        public decimal VolumeInStock => _bottlesInStock * _bottleVolume - (_bottleVolume - _bottleRemainingVolume);

        /// <summary>
        /// Gets the number of consumed bottles 
        /// </summary>
        public uint ConsumedBottles => (uint)(_consumedVolume / _bottleVolume);

        /// <summary>
        /// Gets the total number of owned bottles
        /// </summary>
        public int OwnedBottles => (int) (_bottlesInStock + ConsumedBottles);

        /// <summary>
        /// Gets or sets the total number of consumed volume in milliliters
        /// </summary>
        public decimal ConsumedVolume
        {
            get => _consumedVolume;
            set
            {
                if(!RaiseAndSetIfChanged(ref _consumedVolume, value)) return;
                RaisePropertyChanged(nameof(ConsumedVolumeLiters));
            }
        }

        /// <summary>
        /// Gets total number of consumed volume in liters
        /// </summary>
        public decimal ConsumedVolumeLiters => ConsumedVolume / 1000;

        /// <summary>
        /// Gets or sets the total print time using with material in hours
        /// </summary>
        public double PrintTime
        {
            get => _printTime;
            set
            {
                if(!RaiseAndSetIfChanged(ref _printTime, value)) return;
                RaisePropertyChanged(nameof(PrintTimeSpan));
            }
        }

        public TimeSpan PrintTimeSpan => TimeSpan.FromHours(_printTime);

        #endregion

        #region Constructors
        public Material() { }

        public Material(string name, uint bottleVolume = 1000, decimal density = 1, decimal bottleCost = 30, int bottlesInStock = 1)
        {
            _name = name;
            _bottleVolume = bottleVolume;
            _density = density;
            _bottleCost = bottleCost;
            _bottlesInStock = bottlesInStock;
            _bottleRemainingVolume = bottleVolume;
        }
        #endregion

        #region Overrides

        protected bool Equals(Material other)
        {
            return _name == other._name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Material) obj);
        }

        public override int GetHashCode()
        {
            return (_name != null ? _name.GetHashCode() : 0);
        }

        public override string ToString()
        {
            return $"{_name} ({_bottleRemainingVolume}/{VolumeInStock}ml)";
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
        public decimal GetVolumeCost(decimal volume) => _bottleVolume > 0 ? volume * _bottleCost / _bottleVolume : 0;

        /// <summary>
        /// Gets the grams for a given volume
        /// </summary>
        /// <param name="volume">Volume in ml</param>
        /// <returns></returns>
        public decimal GetVolumeGrams(decimal volume) => volume * _density;

        /// <summary>
        /// Consume material from current bottle and manage stock
        /// </summary>
        /// <param name="volume">Volume to consume in milliliters</param>
        /// <param name="printSeconds">Time in seconds it took to print</param>
        /// <returns>True if still have bottles in stock, otherwise false</returns>
        public bool Consume(decimal volume, double printSeconds = 0)
        {
            if (volume <= 0 || _bottleVolume == 0) return true; // Safe check
            int consumedBottles = (int)(volume / _bottleVolume);
            decimal remainder = volume % _bottleVolume;

            if (remainder > 0)
            {
                decimal remainingVolume = _bottleRemainingVolume - remainder;
                if (remainingVolume < 0)
                {
                    consumedBottles++;
                    remainingVolume += _bottleVolume;
                }

                BottleRemainingVolume = remainingVolume;
            }

            BottlesInStock -= consumedBottles;
            ConsumedVolume += volume;

            AddPrintTimeSeconds(printSeconds);

            return _bottlesInStock > 0;
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
}
