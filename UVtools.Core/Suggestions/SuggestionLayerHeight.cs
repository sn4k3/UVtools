/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Text;
using UVtools.Core.Extensions;
using Layer = UVtools.Core.Layers.Layer;

namespace UVtools.Core.Suggestions;

public sealed partial class SuggestionLayerHeight : Suggestion
{
    #region Members

    #endregion

    #region Properties

    public override bool IsApplied
    {
        get
        {
            if (SlicerFile is null) return false;
            if ((decimal)SlicerFile.LayerHeight < MinimumLayerHeight || (decimal)SlicerFile.LayerHeight > MaximumLayerHeight) return false;
            if (SlicerFile.LayerHeight.DecimalDigits() > MaximumLayerHeightDecimalPlates) return false;

            foreach (var layer in SlicerFile)
            {
                if ((decimal)layer.LayerHeight < MinimumLayerHeight || (decimal)layer.LayerHeight > MaximumLayerHeight) return false;
                if (layer.LayerHeight.DecimalDigits() > MaximumLayerHeightDecimalPlates) return false;
            }

            return true;
        }
    }

    public override bool IsInformativeOnly => true;

    public override string Title => "Layer height";

    public override string Description => "Using the right layer height is important to get successful prints:\n" +
                                          "Thin layers may cause problems on adhesion, lamination, will print much slower and have no real visual benefits.\n" +
                                          "Thick layers may not fully cure no matter the exposure time you use, causing lamination and other hazards. Read your resin datasheet to know the limits.\n" +
                                          "Using layer height with too many decimal digits may produce a wrong positioning due stepper step loss and/or Z axis quality.\n" +
                                          "Solution: Re-slice the model with proper layer height.";

    public override string Message => IsApplied 
        ? $"{GlobalAppliedMessage}: {SlicerFile.LayerHeight}mm" 
        : $"{GlobalNotAppliedMessage} is out of the recommended {MinimumLayerHeight}mm » {MaximumLayerHeight}mm, up to {MaximumLayerHeightDecimalPlates} decimal digit(s)";

    public override string ToolTip => $"The recommended layer height is between [{MinimumLayerHeight}mm to {MaximumLayerHeight}mm] up to {MaximumLayerHeightDecimalPlates} digit(s) precision.\n" +
                                      $"Explanation: {Description}";

    public override string? ConfirmationMessage => $"{Title}: Re-slice the model with proper layer height";

    public decimal MinimumLayerHeight
    {
        get;
        set => SetProperty(ref field, Layer.RoundHeight(Math.Clamp(value, Layer.MinimumHeight, Layer.MaximumHeight)));
    } = 0.03m;

    public decimal MaximumLayerHeight
    {
        get;
        set => SetProperty(ref field, Layer.RoundHeight(Math.Clamp(value, Layer.MinimumHeight, Layer.MaximumHeight)));
    } = 0.10m;

    public byte MaximumLayerHeightDecimalPlates
    {
        get;
        set => SetProperty(ref field, Math.Clamp(value, (byte)2, (byte)4));
    } = 2;

    #endregion

    #region Override

    public override string? Validate()
    {
        var sb = new StringBuilder();

        if (MaximumLayerHeightDecimalPlates is < 2 or > 10)
        {
            sb.AppendLine("Layer height digits must be between 2 and 10");
        }

        if (MinimumLayerHeight <= 0)
        {
            sb.AppendLine("Minimum layer height must be higher than 0mm");
        }

        if (MaximumLayerHeight <= 0)
        {
            sb.AppendLine("Maximum layer height must be higher than 0mm");
        }

        if (MinimumLayerHeight > MaximumLayerHeight)
        {
            sb.AppendLine("Minimum layer height can't be higher than maximum layer height");
        }

        return sb.ToString();
    }

    #endregion

    #region Methods

    #endregion
}
