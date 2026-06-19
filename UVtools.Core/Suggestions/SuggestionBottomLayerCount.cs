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
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.Suggestions;

public sealed partial class SuggestionBottomLayerCount : Suggestion
{
    #region Members

    #endregion

    #region Properties

    public override bool IsAvailable => SlicerFile?.CanUseBottomLayerCount ?? false;

    public override bool IsApplied
    {
        get
        {
            if (SlicerFile is null) return false;
            var bottomHeight = (decimal)SlicerFile.BottomLayersHeight;

            return ApplyWhen switch
            {
                SuggestionApplyWhen.OutsideLimits => bottomHeight >= Math.Min((decimal)SlicerFile.PrintHeight, MinimumBottomHeight) &&
                                                      bottomHeight <= MaximumBottomHeight &&
                                                      SlicerFile.BottomLayerCount >= Math.Min(SlicerFile.LayerCount, MinimumBottomLayerCount) &&
                                                      SlicerFile.BottomLayerCount <= MaximumBottomLayerCount,
                SuggestionApplyWhen.Different => bottomHeight == Math.Min((decimal)SlicerFile.PrintHeight, TargetBottomHeight),
                    
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    public override string Title => "Bottom layers";

    public override string Description => "Bottom layers should be kept to a minimum, usually from 2 to 3, it function is to provide a good adhesion to the first layer on the build plate, using a high count have disadvantages.";

    public override string Message => IsApplied 
        ? $"{GlobalAppliedMessage}: {SlicerFile.BottomLayerCount} / {SlicerFile.BottomLayersHeight}mm" 
        : $"{GlobalNotAppliedMessage} ({SlicerFile.BottomLayerCount}) is out of the recommended {BottomLayerCountValue} layers";

    public override string ToolTip => $"The recommended total height for the bottom layers must be between [{MinimumBottomHeight}mm to {MaximumBottomHeight}mm] constrained from [{MinimumBottomLayerCount} to {MaximumBottomLayerCount}] layers.\n" +
                                      $"Explanation: {Description}";

    public override string? InformationUrl => "https://ameralabs.com/blog/default-3d-printing-raft-settings";

    public override string? ConfirmationMessage => $"{Title}: {SlicerFile.BottomLayerCount} » {BottomLayerCountValue}";

    public decimal TargetBottomHeight
    {
        get;
        set => SetProperty(ref field, Layer.RoundHeight(Math.Max(0, value)));
    } = 0.25m;

    public decimal MinimumBottomHeight
    {
        get;
        set => SetProperty(ref field, Layer.RoundHeight(Math.Max(0, value)));
    } = 0.07m;

    public decimal MaximumBottomHeight
    {
        get;
        set => SetProperty(ref field, Layer.RoundHeight(Math.Max(0, value)));
    } = 0.4m;

    [ObservableProperty]
    public partial byte MinimumBottomLayerCount { get; set; } = 3;

    [ObservableProperty]
    public partial byte MaximumBottomLayerCount { get; set; } = 7;

    public ushort BottomLayerCountValue => Math.Clamp((ushort)Math.Ceiling((float)TargetBottomHeight / SlicerFile.LayerHeight), MinimumBottomLayerCount, MaximumBottomLayerCount);

    #endregion

    #region Override

    public override string? Validate()
    {
        var sb = new StringBuilder();

        if (TargetBottomHeight < 0)
        {
            sb.AppendLine("Bottom height must be a positive value");
        }

        if (MinimumBottomHeight < 0)
        {
            sb.AppendLine("Minimum limit (mm) must be a positive value");
        }

        if (MaximumBottomHeight < 0)
        {
            sb.AppendLine("maximum limit (mm) must be a positive value");
        }

        if (MinimumBottomHeight > MaximumBottomHeight)
        {
            sb.AppendLine("Minimum limit (mm) can't be higher than maximum limit (mm)");
        }

        if (MinimumBottomLayerCount > MaximumBottomLayerCount)
        {
            sb.AppendLine("Minimum limit (layers) can't be higher than maximum limit (layers)");
        }

        return sb.ToString();
    }

    #endregion

    #region Constructor
    public SuggestionBottomLayerCount()
    {
        ApplyWhen = SuggestionApplyWhen.OutsideLimits;
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
