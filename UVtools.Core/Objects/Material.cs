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
    public class Material : BindableBase
    {
        #region Members
        private string _name;
        private decimal _bottleVolume = 1000;
        private decimal _density = 1;
        private decimal _bottleCost = 30;
        private int _bottlesInStock = 1;
        private decimal _bottleRemainingVolume = 1000;
        private decimal _totalConsumedVolume;
        private double _totalPrintTime;

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
        public decimal BottleVolume
        {
            get => _bottleVolume;
            set
            {
                if(!RaiseAndSetIfChanged(ref _bottleVolume, value)) return;
                RaisePropertyChanged(nameof(BottleWeight));
                RaisePropertyChanged(nameof(TotalConsumedBottles));
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
            set => RaiseAndSetIfChanged(ref _bottleCost, value);
        }

        /// <summary>
        /// Gets or sets the number of bottles in stock
        /// </summary>
        public int BottlesInStock
        {
            get => _bottlesInStock;
            set => RaiseAndSetIfChanged(ref _bottlesInStock, value);
        }

        /// <summary>
        /// Gets or sets the current bottle remaining material in milliliters
        /// </summary>
        public decimal BottleRemainingVolume
        {
            get => _bottleRemainingVolume;
            set => RaiseAndSetIfChanged(ref _bottleRemainingVolume, value);
        }

        /// <summary>
        /// Gets the number of consumed bottles 
        /// </summary>
        public uint TotalConsumedBottles => (uint) Math.Floor(_totalConsumedVolume / _bottleVolume);

        /// <summary>
        /// Gets or sets the total number of consumed volume in milliliters
        /// </summary>
        public decimal TotalConsumedVolume
        {
            get => _totalConsumedVolume;
            set => RaiseAndSetIfChanged(ref _totalConsumedVolume, value);
        }

        /// <summary>
        /// Gets or sets the total print time using with material in hours
        /// </summary>
        public double TotalPrintTime
        {
            get => _totalPrintTime;
            set => RaiseAndSetIfChanged(ref _totalPrintTime, value);
        }

        public TimeSpan TotalPrintTimeSpan => TimeSpan.FromHours(_totalPrintTime);

        #endregion

        #region Constructors
        public Material() { }

        public Material(string name, decimal bottleVolume = 1000, decimal density = 1, decimal bottleCost = 30, int bottlesInStock = 1)
        {
            _name = name;
            _bottleVolume = bottleVolume;
            _density = density;
            _bottleCost = bottleCost;
            _bottlesInStock = bottlesInStock;
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
            return $"{nameof(Name)}: {Name}, {nameof(BottleVolume)}: {BottleVolume}ml, {nameof(BottleWeight)}: {BottleWeight}g, {nameof(Density)}: {Density}g/ml, {nameof(BottleCost)}: {BottleCost}, {nameof(BottlesInStock)}: {BottlesInStock}, {nameof(BottleRemainingVolume)}: {BottleRemainingVolume}ml, {nameof(TotalConsumedBottles)}: {TotalConsumedBottles}, {nameof(TotalConsumedVolume)}: {TotalConsumedVolume}ml, {nameof(TotalPrintTime)}: {TotalPrintTime:F4}h";
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
            int consumedBottles = (int) Math.Floor(volume / _bottleVolume);
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

            AddPrintTime(printSeconds);

            return _bottlesInStock > 0;
        }

        /// <summary>
        /// Add print time with this material
        /// </summary>
        /// <param name="seconds">Seconds to add</param>
        public void AddPrintTime(double seconds)
        {
            if (seconds <= 0) return;
            TotalPrintTime += seconds / 60 / 60;
        }
        #endregion
    }
}
