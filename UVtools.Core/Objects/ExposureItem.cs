using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UVtools.Core.Objects
{
    [Serializable]
    public sealed class ExposureItem : BindableBase, IComparable<ExposureItem>
    {
        private decimal _layerHeight;
        private decimal _bottomExposure;
        private decimal _exposure;


        /// <summary>
        /// Gets or sets the layer height in millimeters
        /// </summary>
        public decimal LayerHeight
        {
            get => _layerHeight;
            set => RaiseAndSetIfChanged(ref _layerHeight, Math.Round(value, 2));
        }


        /// <summary>
        /// Gets or sets the bottom exposure in seconds
        /// </summary>
        public decimal BottomExposure
        {
            get => _bottomExposure;
            set => RaiseAndSetIfChanged(ref _bottomExposure, Math.Round(value, 2));
        }

        /// <summary>
        /// Gets or sets the bottom exposure in seconds
        /// </summary>
        public decimal Exposure
        {
            get => _exposure;
            set => RaiseAndSetIfChanged(ref _exposure, Math.Round(value, 2));
        }

        public bool IsValid => _layerHeight > 0 && _bottomExposure > 0 && _exposure > 0;

        public ExposureItem() { }

        public ExposureItem(decimal layerHeight, decimal bottomExposure = 0, decimal exposure = 0)
        {
            _layerHeight = Math.Round(layerHeight, 2);
            _bottomExposure = Math.Round(bottomExposure, 2);
            _exposure = Math.Round(exposure, 2);
        }

        public override string ToString()
        {
            return $"{nameof(LayerHeight)}: {LayerHeight}mm, {nameof(BottomExposure)}: {BottomExposure}s, {nameof(Exposure)}: {Exposure}s";
        }

        private bool Equals(ExposureItem other)
        {
            return _layerHeight == other._layerHeight && _bottomExposure == other._bottomExposure && _exposure == other._exposure;
        }

        public override bool Equals(object obj)
        {
            return ReferenceEquals(this, obj) || obj is ExposureItem other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_layerHeight, _bottomExposure, _exposure);
        }

        public int CompareTo(ExposureItem other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var layerHeightComparison = _layerHeight.CompareTo(other._layerHeight);
            if (layerHeightComparison != 0) return layerHeightComparison;
            var bottomExposureComparison = _bottomExposure.CompareTo(other._bottomExposure);
            if (bottomExposureComparison != 0) return bottomExposureComparison;
            return _exposure.CompareTo(other._exposure);
        }
    }
}
