/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.Suggestions;

public sealed class SuggestionBottomLayerCount : Suggestion
{
    #region Members

    private decimal _targetBottomHeight = 0.25m;
    private decimal _minimumBottomHeight = 0.07m;
    private decimal _maximumBottomHeight = 0.4m;
    private byte _minimumBottomLayerCount = 3;
    private byte _maximumBottomLayerCount = 7;

    #endregion

    #region Properties

    public override bool IsAvailable => SlicerFile?.CanUseBottomLayerCount ?? false;

    public override bool IsApplied
    {
        get
        {
            if (SlicerFile is null) return false;
            var bottomHeight = (decimal)SlicerFile.BottomLayersHeight;

            return _applyWhen switch
            {
                SuggestionApplyWhen.OutsideLimits => bottomHeight >= _minimumBottomHeight &&
                                                     bottomHeight <= _maximumBottomHeight &&
                                                     SlicerFile.BottomLayerCount >= _minimumBottomLayerCount &&
                                                     SlicerFile.BottomLayerCount <= _maximumBottomLayerCount,
                SuggestionApplyWhen.Different => bottomHeight == TargetBottomHeight,
                    
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public override string Title => "Bottom layers";

    public override string Description => "Bottom layers should be kept to a minimum, usually from 2 to 3, it function is to provide a good adhesion to the first layer on the build plate, using a high count have disadvantages.";

    public override string Message => IsApplied 
        ? $"{GlobalAppliedMessage}: {SlicerFile.BottomLayerCount} / {SlicerFile.BottomLayersHeight}mm" 
        : $"{GlobalNotAppliedMessage} ({SlicerFile.BottomLayerCount}) is out of the recommended {BottomLayerCountValue} layers";

    public override string ToolTip => $"The recommended total height for the bottom layers must be between [{_minimumBottomHeight}mm to {_maximumBottomHeight}mm] constrained from [{_minimumBottomLayerCount} to {_maximumBottomLayerCount}] layers.\n" +
                                      $"Explanation: {Description}";

    public override string? InformationUrl => "https://ameralabs.com/blog/default-3d-printing-raft-settings";

    public override string? ConfirmationMessage => $"{Title}: {SlicerFile.BottomLayerCount} » {BottomLayerCountValue}";

    public decimal TargetBottomHeight
    {
        get => _targetBottomHeight;
        set => RaiseAndSetIfChanged(ref _targetBottomHeight, Layer.RoundHeight(Math.Max(0, value)));
    }

    public decimal MinimumBottomHeight
    {
        get => _minimumBottomHeight;
        set => RaiseAndSetIfChanged(ref _minimumBottomHeight, Layer.RoundHeight(Math.Max(0, value)));
    }

    public decimal MaximumBottomHeight
    {
        get => _maximumBottomHeight;
        set => RaiseAndSetIfChanged(ref _maximumBottomHeight, Layer.RoundHeight(Math.Max(0, value)));
    }

    public byte MinimumBottomLayerCount
    {
        get => _minimumBottomLayerCount;
        set => RaiseAndSetIfChanged(ref _minimumBottomLayerCount, value);
    }

    public byte MaximumBottomLayerCount
    {
        get => _maximumBottomLayerCount;
        set => RaiseAndSetIfChanged(ref _maximumBottomLayerCount, value);
    }

    public ushort BottomLayerCountValue => Math.Clamp((ushort)Math.Ceiling((float)_targetBottomHeight / SlicerFile.LayerHeight), _minimumBottomLayerCount, _maximumBottomLayerCount);

    #endregion

    #region Override

    public override string? Validate()
    {
        var sb = new StringBuilder();

        if (_targetBottomHeight < 0)
        {
            sb.AppendLine("Bottom height must be a positive value");
        }

        if (_minimumBottomHeight < 0)
        {
            sb.AppendLine("Minimum limit (mm) must be a positive value");
        }

        if (_maximumBottomHeight < 0)
        {
            sb.AppendLine("maximum limit (mm) must be a positive value");
        }

        if (_minimumBottomHeight > _maximumBottomHeight)
        {
            sb.AppendLine("Minimum limit (mm) can't be higher than maximum limit (mm)");
        }

        if (_minimumBottomLayerCount > _maximumBottomLayerCount)
        {
            sb.AppendLine("Minimum limit (layers) can't be higher than maximum limit (layers)");
        }

        return sb.ToString();
    }

    #endregion

    #region Constructor
    public SuggestionBottomLayerCount()
    {
        _applyWhen = SuggestionApplyWhen.OutsideLimits;
    }
    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        SlicerFile.BottomLayerCount = BottomLayerCountValue;
        return true;
    }

    #endregion
}