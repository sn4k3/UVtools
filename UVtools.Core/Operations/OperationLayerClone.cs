/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationLayerClone : Operation
    {
        private uint _clones = 1;

        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.Current;
        public override bool CanROI => false;
        public override bool PassActualLayerIndex => true;

        public override string Title => "Clone layers";
        public override string Description =>
            "Clone layers.\n\n" +
            "Useful to increase the height of the model or add additional structure by duplicating layers. For example, can be used to increase the raft height for added stability.";
        public override string ConfirmationText =>
            $"clone layers {LayerIndexStart} through {LayerIndexEnd}, {Clones} time{(Clones != 1 ? "s" : "")}?";

        public override string ProgressTitle =>
            $"Cloning layers {LayerIndexStart} through {LayerIndexEnd}, {Clones} time{(Clones != 1 ? "s" : "")}";

        public override string ProgressAction => "Cloned layers";

        public override bool CanCancel => false;

        public override bool CanHaveProfiles => false;

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();
            if (Clones <= 0)
            {
                sb.AppendLine("Clones must be a positive number");
            }

            return new StringTag(sb.ToString());
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the number of clones
        /// </summary>
        public uint Clones
        {
            get => _clones;
            set => RaiseAndSetIfChanged(ref _clones, value);
        }

        #endregion

        public override string ToString()
        {
            var result = $"[Clones: {Clones}]" + LayerRangeString;
            if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
            return result;
        }
    }
}
