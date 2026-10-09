/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Xml.Serialization;
using UVtools.Core.FileFormats;
using ZLinq;

namespace UVtools.Core.Operations;


public partial class OperationEditParameters : Operation
{
    #region Members


    #endregion

    #region Overrides

    public override bool CanRunInPartialMode => true;
    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;

    public override bool CanROI => false;
    public override string IconClass => "TagEdit";
    public override string Title => "Edit print parameters";

    public override string Description =>
        "Edits the available print parameters.\n" +
        "Note: Set global parameters will override all per layer settings when they are available.";

    public override string ConfirmationText
    {
        get
        {
            var sb = new StringBuilder();
            foreach (var modifier in Modifiers)
            {
                if(!modifier.HasChanged) continue;
                var storedValue = GetStoredValue(modifier);
                if (storedValue == modifier.OldValue) continue;
                sb.AppendLine($"{modifier.Name}: {FormatValue(modifier, modifier.OldValue)}{modifier.ValueUnit} » {FormatValue(modifier, storedValue)}{modifier.ValueUnit}");
            }
            var text = "commit the following print parameter changes";
            if (PerLayerOverride)
            {
                if (LayerRangeCount == 1)
                {
                    text += $" to layer {LayerIndexStart}";
                }
                else
                {
                    text += $" from layer {LayerIndexStart} to {LayerIndexEnd}";
                }
            }

            return $"{text}?\n\n{sb}";
        }
    }

    public override string ProgressTitle => "Change print parameters";

    public override string ProgressAction => "Changing print parameters";

    public override bool CanHaveProfiles => false;

    public override string? ValidateSpawn()
    {
        if (Modifiers.Length == 0)
        {
            return "No available properties to edit on this file format.";
        }

        return null;
    }

    public override string? ValidateInternally()
    {
        if (Modifiers.Length == 0)
        {
            return "Modifiers does not exists, can't validate.";
        }


        CoerceChangedValues();

        var sb = new StringBuilder();
        var changed = Modifiers.AsValueEnumerable().Any(modifier => modifier.HasChanged);

        if (!changed)
        {
            sb.AppendLine("Nothing changed\nDo some changes or cancel the operation.");
        }

        if (Modifiers.AsValueEnumerable().Contains(FileFormat.PrintParameterModifier.PositionZ)
            && FileFormat.PrintParameterModifier.PositionZ.HasChanged
            && SkipNumberOfLayer > 0
            && LayerRangeCount > 1
            && SetNumberOfLayer + SkipNumberOfLayer < LayerRangeCount)
        {
            sb.AppendLine("Can not change the PositionZ in layers with an active alternating pattern.");
        }


        return sb.ToString();
    }
    #endregion

    #region Propertiers

    [XmlIgnore]
    public FileFormat.PrintParameterModifier[] Modifiers { get; set; } = [];

    [ObservableProperty]
    public partial bool PropagateModificationsToLayers { get; set; } = true;

    /// <summary>
    /// Gets or sets if parameters are global or per layer inside a layer range
    /// </summary>
    [ObservableProperty]
    public partial bool PerLayerOverride { get; set; }

    /// <summary>
    /// Gets or sets the number of sequential layers to set the parameters
    /// </summary>
    [ObservableProperty]
    public partial uint SetNumberOfLayer { get; set; } = 1;

    /// <summary>
    /// Gets or sets the number of sequential layers to skip after set a layer
    /// </summary>
    [ObservableProperty]
    public partial uint SkipNumberOfLayer { get; set; }

    #endregion

    #region Constructor

    public OperationEditParameters() { }

    public OperationEditParameters(FileFormat slicerFile) : base(slicerFile) { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        SlicerFile.RefreshPrintParametersModifiersValues();
        Modifiers = SlicerFile.PrintParameterModifiers;
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the value that the file format stores for a modifier
    /// </summary>
    private decimal GetStoredValue(FileFormat.PrintParameterModifier modifier)
    {
        return SlicerFile.CoercePrintParameterModifierValue(modifier, modifier.NewValue);
    }

    /// <summary>
    /// Formats a value with the decimal plates that the file format stores
    /// </summary>
    private string FormatValue(FileFormat.PrintParameterModifier modifier, decimal value)
    {
        var decimalPlates = SlicerFile.GetPrintParameterModifierConstraints(modifier).DecimalPlates;
        return Math.Round(value, decimalPlates, MidpointRounding.AwayFromZero).ToString();
    }

    /// <summary>
    /// Replaces the new value of the changed modifiers by the value that the file format stores, so a value that is
    /// not storable is not reported or applied as a change
    /// </summary>
    private void CoerceChangedValues()
    {
        foreach (var modifier in Modifiers)
        {
            if (!modifier.HasChanged) continue;
            modifier.NewValue = GetStoredValue(modifier);
        }
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        CoerceChangedValues();

        if (PerLayerOverride)
        {
            uint setLayers = 0;
            for (uint layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
            {
                progress.PauseOrCancelIfRequested();
                SlicerFile[layerIndex].SetValuesFromPrintParametersModifiers(Modifiers);
                progress++;
                if (SkipNumberOfLayer == 0) continue;
                setLayers++;
                if (setLayers >= SetNumberOfLayer)
                {
                    setLayers = 0;
                    layerIndex += SkipNumberOfLayer;
                    progress.ProcessedItems = Math.Min(progress.ProcessedItems + SkipNumberOfLayer, progress.ItemCount); // Skipped layers also count as processed
                }
            }

            foreach (var modifier in Modifiers)
            {
                modifier.OldValue = modifier.NewValue;
            }
            SlicerFile.RebuildGCode();
        }
        else
        {
            var suppressRebuild = !PropagateModificationsToLayers;
            if (suppressRebuild)
            {
                SlicerFile.SuppressRebuildProperties = true;
            }
            try
            {
                SlicerFile.SetValuesFromPrintParametersModifiers();
            }
            finally
            {
                if (suppressRebuild)
                {
                    SlicerFile.SuppressRebuildProperties = false;
                }
            }
            if (suppressRebuild)
            {
                SlicerFile.RebuildGCode();
            }
        }

        SlicerFile.RefreshPrintParametersModifiersValues();

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}
