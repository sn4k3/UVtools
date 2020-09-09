/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Text;

namespace UVtools.Core.Operations
{
    public sealed class OperationLayerRemove : Operation
    {
        #region Overrides

        public override bool PassActualLayerIndex => true;

        public override string Title => "Remove layer(s)";

        public override string Description =>
            "Remove Layer(s) in a given range.";

        public override string ConfirmationText =>
            $"remove layers from {LayerIndexStart} to {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Removing layers from {LayerIndexStart} to {LayerIndexEnd}";

        public override string ProgressAction => "Removed layers";

        #endregion

        #region Properties


        #endregion
    }
}
