/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Linq;
using System.Text;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationEditParameters : Operation
    {
        #region Members
        private bool _perLayerOverride;
        #endregion

        #region Overrides
        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override bool CanROI => false;

        public override string Title => "Edit print parameters";

        public override string Description =>
            "Edits the available print parameters.\n" +
            "Note: Set global parameters will override all per layer settings when they are available.";

        public override string ConfirmationText
        {
            get
            {
                var sb = new StringBuilder();
                foreach (var modifier in Modifiers)
                {
                    if(!modifier.HasChanged) continue;
                    sb.AppendLine($"{modifier.Name}: {modifier.OldValue}{modifier.ValueUnit} » {modifier.NewValue}{modifier.ValueUnit}");
                }
                var text = "commit print parameter changes";
                if (_perLayerOverride)
                {
                    if (LayerRangeCount == 1)
                    {
                        text += $" to layer {LayerIndexStart}";
                    }
                    else
                    {
                        text += $" from layer {LayerIndexStart} to {LayerIndexEnd}";
                    }
                }

                return $"{text}?\n{sb}";
            }
        }

        public override string ProgressTitle => "Change print parameters";

        public override string ProgressAction => "Changing print parameters";

        public override bool CanHaveProfiles => false;

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();
            var changed = Modifiers.Any(modifier => modifier.HasChanged);

            if (!changed)
            {
                sb.AppendLine("Nothing changed\nDo some changes or cancel the operation.");
            }


            return new StringTag(sb.ToString());
        }
        #endregion

        #region Propertiers

        public FileFormat.PrintParameterModifier[] Modifiers { get; set; }

        /// <summary>
        /// Gets or sets if parameters are global or per layer inside a layer range
        /// </summary>
        public bool PerLayerOverride
        {
            get => _perLayerOverride;
            set => RaiseAndSetIfChanged(ref _perLayerOverride, value);
        }
        #endregion

        public OperationEditParameters()
        {
        }

        public OperationEditParameters(FileFormat.PrintParameterModifier[] modifiers)
        {
            Modifiers = modifiers;
        }

        #region Methods

        public override bool Execute(FileFormat slicerFile, OperationProgress progress = null)
        {
            slicerFile.EditPrintParameters(this);
            return true;
        }

        #endregion
    }
}
