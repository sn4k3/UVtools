/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */


namespace UVtools.Core.Operations
{
    public class OperationCalibrateExternalTests : Operation
    {
        #region Members
        #endregion

        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;
        public override bool CanROI => false;
        public override bool CanHaveProfiles => false;
        public override string ButtonOkText => null;
        public override string Title => "External tests";
        public override string Description =>
            "A set of useful external tests to run within your slicer.\nClick on a button to open website and instructions.";

        public override string ConfirmationText => null;

        public override string ProgressTitle => null;

        public override string ProgressAction => null;
        
        #endregion

        #region Properties

        #endregion

        #region Equality
        
        #endregion

        #region Methods

        #endregion
    }
}
