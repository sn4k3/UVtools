/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

namespace UVtools.Core.Operations
{
    public sealed class OperationSolidify : Operation
    {
        #region Overrides

        public override string Title => "Solidify";

        public override string Description =>
            "Solidifies the selected layers, closing all interior holes.\n\n" +
            "NOTE: All open areas of the layer that are completely surrounded by pixels will be filled. Please ensure that none of the holes in the layer are required before proceeding.";

        public override string ConfirmationText =>
            $"solidify layers {LayerIndexStart} through {LayerIndexEnd}?";

        public override string ProgressTitle =>
            $"Solidifying layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Solidified layers";

        #endregion
    }
}
