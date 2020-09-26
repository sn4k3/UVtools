/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;
using System.Text;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    public class OperationRepairLayers : Operation
    {
        public override bool CanROI { get; set; } = false;
        public override string Title => "Repair layers and issues";
        public override string Description => null;

        public override string ConfirmationText => "attempt  this repair?";

        public override string ProgressTitle =>
            $"Reparing layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Repaired layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (!RepairIslands && !RemoveEmptyLayers && !RepairResinTraps)
            {
                sb.AppendLine("You must select at least one repair operation.");
            }

            return new StringTag(sb.ToString());
        }

        public bool RepairIslands { get; set; } = true;
        public bool RepairResinTraps { get; set; } = true;
        public bool RemoveEmptyLayers { get; set; } = true;
        public byte RemoveIslandsBelowEqualPixelCount { get; set; } = 5;
        public uint GapClosingIterations { get; set; } = 1;
        public uint NoiseRemovalIterations { get; set; } = 0;

        public List<LayerIssue> Issues { get; set; }

    }
}
