/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationRepairLayers : Operation
    {
        private bool _repairIslands = true;
        private bool _repairResinTraps = true;
        private bool _removeEmptyLayers = true;
        private byte _removeIslandsBelowEqualPixelCount = 5;
        private ushort _removeIslandsRecursiveIterations = 4;
        private uint _gapClosingIterations = 1;
        private uint _noiseRemovalIterations = 0;
        public override bool CanROI => false;
        public override string Title => "Repair layers and issues";
        public override string Description => null;

        public override string ConfirmationText => "attempt  this repair?";

        public override string ProgressTitle =>
            $"Reparing layers {LayerIndexStart} through {LayerIndexEnd}";

        public override string ProgressAction => "Repaired layers";

        public override bool CanHaveProfiles => false;

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (!RepairIslands && !RemoveEmptyLayers && !RepairResinTraps)
            {
                sb.AppendLine("You must select at least one repair operation.");
            }

            return new StringTag(sb.ToString());
        }

        public bool RepairIslands
        {
            get => _repairIslands;
            set => RaiseAndSetIfChanged(ref _repairIslands, value);
        }

        public bool RepairResinTraps
        {
            get => _repairResinTraps;
            set => RaiseAndSetIfChanged(ref _repairResinTraps, value);
        }

        public bool RemoveEmptyLayers
        {
            get => _removeEmptyLayers;
            set => RaiseAndSetIfChanged(ref _removeEmptyLayers, value);
        }

        public byte RemoveIslandsBelowEqualPixelCount
        {
            get => _removeIslandsBelowEqualPixelCount;
            set => RaiseAndSetIfChanged(ref _removeIslandsBelowEqualPixelCount, value);
        }

        public ushort RemoveIslandsRecursiveIterations
        {
            get => _removeIslandsRecursiveIterations;
            set => RaiseAndSetIfChanged(ref _removeIslandsRecursiveIterations, value);
        }

        public uint GapClosingIterations
        {
            get => _gapClosingIterations;
            set => RaiseAndSetIfChanged(ref _gapClosingIterations, value);
        }

        public uint NoiseRemovalIterations
        {
            get => _noiseRemovalIterations;
            set => RaiseAndSetIfChanged(ref _noiseRemovalIterations, value);
        }

        [XmlIgnore]
        public IslandDetectionConfiguration IslandDetectionConfig { get; set; }

        [XmlIgnore]
        public List<LayerIssue> Issues { get; set; }
    }
}
