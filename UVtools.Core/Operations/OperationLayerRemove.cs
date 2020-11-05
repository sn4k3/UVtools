/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationLayerRemove : Operation
    {
        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.Current;
        public override bool CanROI => false;
        public override bool PassActualLayerIndex => true;

        public override string Title => "Remove layers";

        public override string Description =>
            "Remove Layers in a given range.";

        public override string ConfirmationText =>
            $"remove layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Removing layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Removed layers";

        public override bool CanCancel => false;

        public override bool CanHaveProfiles => false;

        #endregion

        #region Properties


        #endregion
    }
}
