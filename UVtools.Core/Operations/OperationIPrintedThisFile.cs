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
        private decimal _volume;
        private float _printTime;
        private Material _materialItem;

        #endregion

        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override bool CanROI => false;
        public override bool CanHaveProfiles => false;
        public override string ButtonOkText => "Consume";

        public override string Title => "I printed this file";
        public override string Description => "Select a material and consume resin from stock and print time.";

        public override string ConfirmationText =>
            $"consume {_volume}ml and {PrintTimeHours}h on:\n{_materialItem} ?";

        public override string ProgressTitle =>
            $"Consuming";

        public override string ProgressAction => "Consumed";

        public override StringTag Validate(params object[] parameters)
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

            return new StringTag(sb.ToString());
        }

        public override string ToString()
        {
            var result = $"{_volume}ml {PrintTimeHours}h";
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
            set => RaiseAndSetIfChanged(ref _volume, value);
        }

        public float PrintTime
        {
            get => _printTime;
            set
            {
                if(!RaiseAndSetIfChanged(ref _printTime, value)) return;
                RaisePropertyChanged(nameof(PrintTimeHours));
            }
        }

        public float PrintTimeHours => _printTime / 60 / 60;

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

            MaterialItem.Consume(_volume, _printTime);

            MaterialManager.Save();
            return !progress.Token.IsCancellationRequested;
        }

        #endregion

        #region Equality

        #endregion
    }
}
