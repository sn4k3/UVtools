/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Text;
using UVtools.Core.Extensions;
using Layer = UVtools.Core.Layers.Layer;

namespace UVtools.Core.Suggestions;

public sealed class SuggestionLayerHeight : Suggestion
{
    #region Members

    private decimal _minimumLayerHeight = 0.03m;
    private decimal _maximumLayerHeight = 0.10m;
    private byte _maximumLayerHeightDecimalPlates = 2;

    #endregion

    #region Properties

    public override bool IsApplied
    {
        get
        {
            if (SlicerFile is null) return false;
            if ((decimal)SlicerFile.LayerHeight < _minimumLayerHeight || (decimal)SlicerFile.LayerHeight > _maximumLayerHeight) return false;
            if (SlicerFile.LayerHeight.DecimalDigits() > _maximumLayerHeightDecimalPlates) return false;

            foreach (var layer in SlicerFile)
            {
                if ((decimal)layer.LayerHeight < _minimumLayerHeight || (decimal)layer.LayerHeight > _maximumLayerHeight) return false;
                if (layer.LayerHeight.DecimalDigits() > _maximumLayerHeightDecimalPlates) return false;
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
        : $"{GlobalNotAppliedMessage} is out of the recommended {_minimumLayerHeight}mm » {_maximumLayerHeight}mm, up to {_maximumLayerHeightDecimalPlates} decimal digit(s)";

    public override string ToolTip => $"The recommended layer height is between {_minimumLayerHeight}mm and {_maximumLayerHeight}mm up to {_maximumLayerHeightDecimalPlates} digit(s) precision.\n" +
                                      $"Explanation: {Description}";

    public override string? ConfirmationMessage => $"{Title}: Re-slice the model with proper layer height";

    public decimal MinimumLayerHeight
    {
        get => _minimumLayerHeight;
        set => RaiseAndSetIfChanged(ref _minimumLayerHeight, Layer.RoundHeight(value.Clamp(Layer.MinimumHeight, Layer.MaximumHeight)));
    }

    public decimal MaximumLayerHeight
    {
        get => _maximumLayerHeight;
        set => RaiseAndSetIfChanged(ref _maximumLayerHeight, Layer.RoundHeight(value.Clamp(Layer.MinimumHeight, Layer.MaximumHeight)));
    }

    public byte MaximumLayerHeightDecimalPlates
    {
        get => _maximumLayerHeightDecimalPlates;
        set => RaiseAndSetIfChanged(ref _maximumLayerHeightDecimalPlates, value.Clamp(2, 4));
    }

    #endregion

    #region Override

    public override string? Validate()
    {
        var sb = new StringBuilder();

        if (_maximumLayerHeightDecimalPlates is < 2 or > 10)
        {
            sb.AppendLine("Layer height digits must be between 2 and 10");
        }

        if (_minimumLayerHeight <= 0)
        {
            sb.AppendLine("Minimum layer height must be higher than 0mm");
        }

        if (_minimumLayerHeight > _maximumLayerHeight)
        {
            sb.AppendLine("Minimum layer height can't be higher than maximum layer height");
        }

        return sb.ToString();
    }

    #endregion

    #region Methods

    #endregion
}