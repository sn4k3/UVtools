/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using UVtools.Core.FileFormats;

namespace UVtools.Core.Operations
{
    public class OperationEditParameters : Operation
    {
        public override string Title => "Edit Parameters";
        public override string Description =>
            "Edits the available print parameters.";

        public override string ConfirmationText => "commit print parameter changes?";

        public override string ProgressTitle => null;

        public override string ProgressAction => null;

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
