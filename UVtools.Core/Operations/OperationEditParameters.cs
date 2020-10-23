/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Linq;
using System.Text;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    public class OperationEditParameters : Operation
    {
        public override Enumerations.LayerRangeSelection LayerRangeSelection => Enumerations.LayerRangeSelection.None;

        public override bool CanROI { get; set; } = false;

        public override string Title => "Edit print parameters";

        public override string Description =>
            "Edits the available print parameters.";

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
                return $"commit print parameter changes?\n{sb}";
            }
        }

        public override string ProgressTitle => "Change print parameters";

        public override string ProgressAction => "Changing print parameters";

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

        public FileFormat.PrintParameterModifier[] Modifiers { get; set; }

        public OperationEditParameters()
        {
        }

        public OperationEditParameters(FileFormat.PrintParameterModifier[] modifiers)
        {
            Modifiers = modifiers;
        }
    }
}
