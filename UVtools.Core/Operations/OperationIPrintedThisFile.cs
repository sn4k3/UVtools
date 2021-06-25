/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;
using UVtools.Core.FileFormats;
using UVtools.Core.Managers;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationIPrintedThisFile : Operation
    {
        #region Members
        private Material _materialItem;
        private decimal _volume;
        private float _printTime;
        private decimal _multiplier = 1;

        #endregion

        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override bool CanROI => false;
        public override bool CanHaveProfiles => false;
        public override string ButtonOkText => "Consume";

        public override string Title => "I printed this file";
        public override string Description => "Select a material and consume resin from stock and print time.";

        public override string ConfirmationText =>
            $"consume {FinalVolume}ml and {FinalPrintTimeHours:F4}h on:\n{_materialItem} ?";

        public override string ProgressTitle =>
            $"Consuming";

        public override string ProgressAction => "Consumed";

        public override string ValidateInternally()
        {
            var sb = new StringBuilder();

            if (_materialItem is null)
            {
                sb.AppendLine("You must select an material.");
            }
            if (_volume <= 0)
            {
                sb.AppendLine("Volume must be higher than 0ml.");
            }
            if (_printTime <= 0)
            {
                sb.AppendLine("Print time must be higher than 0s.");
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            var result = $"{FinalVolume}ml {FinalPrintTimeHours:F4}h on {_materialItem.Name}";
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }
        #endregion

        #region Properties

        public Material MaterialItem
        {
            get => _materialItem;
            set => RaiseAndSetIfChanged(ref _materialItem, value);
        }

        public decimal Volume
        {
            get => _volume;
            set
            {
                if(!RaiseAndSetIfChanged(ref _volume, value)) return;
                RaisePropertyChanged(nameof(FinalVolume));
            }
        }

        public decimal FinalVolume => _volume * _multiplier;

        public float PrintTime
        {
            get => _printTime;
            set
            {
                if(!RaiseAndSetIfChanged(ref _printTime, value)) return;
                RaisePropertyChanged(nameof(PrintTimeHours));
                RaisePropertyChanged(nameof(FinalPrintTime));
                RaisePropertyChanged(nameof(FinalPrintTimeHours));
            }
        }

        public float PrintTimeHours => _printTime / 60 / 60;
        public float FinalPrintTime => _printTime * (float)_multiplier;
        public float FinalPrintTimeHours => FinalPrintTime / 60 / 60;

        /// <summary>
        /// Number of times this file has been printed
        /// </summary>
        public decimal Multiplier
        {
            get => _multiplier;
            set
            {
                if (!RaiseAndSetIfChanged(ref _multiplier, value)) return;
                RaisePropertyChanged(nameof(FinalVolume));
                RaisePropertyChanged(nameof(FinalPrintTime));
            }
        }

        public MaterialManager Manager => MaterialManager.Instance;

        #endregion

        #region Constructor

        public OperationIPrintedThisFile() { }

        public OperationIPrintedThisFile(FileFormat slicerFile) : base(slicerFile) { }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();
            _volume = (decimal) SlicerFile.MaterialMilliliters;
            _printTime = SlicerFile.PrintTime;
            MaterialManager.Load();
        }

        #endregion

        #region Methods

        protected override bool ExecuteInternally(OperationProgress progress)
        {
            if (MaterialItem is null) return !progress.Token.IsCancellationRequested;

            MaterialItem.Consume(FinalVolume, FinalPrintTime);

            MaterialManager.Save();
            return !progress.Token.IsCancellationRequested;
        }

        #endregion

        #region Equality

        protected bool Equals(OperationIPrintedThisFile other)
        {
            return _volume == other._volume && _printTime.Equals(other._printTime) && Equals(_materialItem, other._materialItem) && _multiplier == other._multiplier;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((OperationIPrintedThisFile) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_volume, _printTime, _materialItem, _multiplier);
        }

        #endregion
    }
}
