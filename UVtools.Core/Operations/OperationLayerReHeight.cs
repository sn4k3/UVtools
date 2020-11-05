/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public sealed class OperationLayerReHeight : Operation
    {
        private OperationLayerReHeightItem _item;

        #region Overrides

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;
        public override bool CanROI => false;
        public override string Title => "Adjust layer height";
        public override string Description =>
            "Adjust the layer height of the model\n\n" +
            "Adjusting to values lower than current height will reduce layer lines, adjusting to values higher" +
            " than current height will reduce model detail.\n\n" +
            "Note: Using dedicated slicer software to re-slice will usually yeild better results.";
        public override string ConfirmationText =>
            $"adjust layer height to {Item.LayerHeight}mm?";

        public override string ProgressTitle =>
            $"Adjusting layer height to {Item.LayerHeight}mm";

        public override string ProgressAction => "Height adjusted layers";

        public override bool CanCancel => false;

        public override bool CanHaveProfiles => false;

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (Item is null)
            {
                sb.AppendLine("No valid configurations, unable to proceed.");
            }


            return new StringTag(sb.ToString());
        }

        #endregion

        public OperationLayerReHeightItem Item
        {
            get => _item;
            set => RaiseAndSetIfChanged(ref _item, value);
        }


        public static OperationLayerReHeightItem[] GetItems(uint layerCount, decimal layerHeight)
        {
            List<OperationLayerReHeightItem> list = new List<OperationLayerReHeightItem>();
            for (byte i = 2; i < 255; i++) // Lower
            {
                if (layerHeight / i < 0.01m) break;
                var countStr = (layerCount / (decimal)i).ToString(CultureInfo.InvariantCulture);
                if (countStr.IndexOf(".", StringComparison.Ordinal) >= 0) continue; // Cant multiply layers
                countStr = (layerHeight / i).ToString(CultureInfo.InvariantCulture);
                int decimalCount = countStr.Substring(countStr.IndexOf(".", StringComparison.Ordinal)).Length - 1;
                if (decimalCount > 2) continue; // Cant multiply height

                var item = new OperationLayerReHeightItem(false, i, layerHeight / i, layerCount * i);
                list.Add(item);
            }

            for (byte i = 2; i < 255; i++) // Higher
            {
                if (layerHeight * i > 0.2m) break;
                var countStr = (layerCount / (decimal)i).ToString(CultureInfo.InvariantCulture);
                if (countStr.IndexOf(".", StringComparison.Ordinal) >= 0) continue; // Cant multiply layers


                countStr = (layerHeight * i).ToString(CultureInfo.InvariantCulture);
                int decimalCount = countStr.Substring(countStr.IndexOf(".", StringComparison.Ordinal)).Length - 1;
                if (decimalCount > 2) continue; // Cant multiply height

                var item = new OperationLayerReHeightItem(true, i, layerHeight * i, layerCount / i);
                list.Add(item);
            }

            return list.ToArray();
        }


        public class OperationLayerReHeightItem
        {
            public bool IsMultiply { get; }
            public bool IsDivision => !IsMultiply;
            public byte Modifier { get; }
            public decimal LayerHeight { get; }
            public uint LayerCount { get; }

            public OperationLayerReHeightItem(bool isMultiply, byte modifier, decimal layerHeight, uint layerCount)
            {
                IsMultiply = isMultiply;
                Modifier = modifier;
                LayerHeight = layerHeight;
                LayerCount = layerCount;
            }

            public override string ToString()
            {
                return (IsMultiply ? 'x' : '÷') + $" {Modifier} → {LayerCount} layers at {LayerHeight}mm";
            }
        }
    }
}
