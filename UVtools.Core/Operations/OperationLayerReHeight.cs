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
    public sealed class OperationLayerReHeight : Operation
    {
        #region Overrides

        public override string Title => "Layer re-height";
        public override string Description =>
            "Changes layer height.\n" +
            "Going lower doesn't give you better XYZ accuracy but will reduce Z lines, layers will be cloned and repeated over Z for the effect.\n" +
            "Going higher will reduce detail, layers will sum times the modifier for the effect.\n" +
            "Note: Reslice with the new layer height is always preferable";
        public override string ConfirmationText =>
            $"re-height layers to {Item.LayerHeight}mm?";

        public override string ProgressTitle =>
            $"Re-height layers to {Item.LayerHeight}mm";

        public override string ProgressAction => "Re-height-ed layers";

        public override StringTag Validate(params object[] parameters)
        {
            var sb = new StringBuilder();

            if (Item is null)
            {
                sb.AppendLine("No valid configurations to proceed");
            }


            return new StringTag(sb.ToString());
        }

        #endregion

        public OperationLayerReHeightItem Item { get; set; }


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
