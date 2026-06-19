/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Text;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Managers;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations;


#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationIPrintedThisFile : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Members
    private decimal _volume;
    private float _printTime;

    #endregion

    #region Overrides

    public override bool CanRunInPartialMode => true;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;

    public override bool CanROI => false;
    public override bool CanHaveProfiles => false;
    public override string ButtonOkText => "Consume";
    public override string IconClass => "Flask";
    public override string Title => "I printed this file";
    public override string Description => "Select a material and consume resin from stock and print time.";

    public override string ConfirmationText =>
        $"consume {FinalVolume}ml and {FinalPrintTimeString:F4}h on:\n{MaterialItem} ?";

    public override string ProgressTitle =>
        $"Consuming";

    public override string ProgressAction => "Consumed";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (MaterialItem is null)
        {
            sb.AppendLine("You must select an material.");
        }
        if (_volume <= 0)
        {
            sb.AppendLine("Volume must be higher than 0ml.");
        }
        if (_printTime <= 0)
        {
            sb.AppendLine("Print time must be higher than 0s.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"{FinalVolume}ml {FinalPrintTimeString:F4}h on {MaterialItem?.Name}";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Properties

    [ObservableProperty]
    public partial Material? MaterialItem { get; set; }

    public decimal Volume
    {
        get => _volume;
        set
        {
            if(!SetProperty(ref _volume, Math.Max(0, value))) return;
            OnPropertyChanged(nameof(FinalVolume));
        }
    }

    public decimal FinalVolume => _volume * Multiplier;

    public float PrintTime
    {
        get => _printTime;
        set
        {
            if(!SetProperty(ref _printTime, Math.Max(0, value))) return;
            OnPropertyChanged(nameof(PrintTimeString));
            OnPropertyChanged(nameof(FinalPrintTime));
            OnPropertyChanged(nameof(FinalPrintTimeString));
        }
    }

    public string PrintTimeString => TimeSpan.FromSeconds(_printTime).ToTimeString();
    public float FinalPrintTime => _printTime * (float)Multiplier;
    public string FinalPrintTimeString => TimeSpan.FromSeconds(FinalPrintTime).ToTimeString();

    /// <summary>
    /// Number of times this file has been printed
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FinalVolume))]
    [NotifyPropertyChangedFor(nameof(FinalPrintTime))]
    public partial decimal Multiplier { get; set; } = 1;

    public MaterialManager Manager => MaterialManager.Instance;

    #endregion

    #region Constructor

    public OperationIPrintedThisFile() { }

    public OperationIPrintedThisFile(FileFormat slicerFile) : base(slicerFile) { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        _volume = (decimal) SlicerFile.MaterialMilliliters;
        _printTime = SlicerFile.PrintTime;
        MaterialManager.Load();
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        if (MaterialItem is null) return !progress.Token.IsCancellationRequested;

        MaterialItem.Consume(FinalVolume, FinalPrintTime);

        MaterialManager.Save();
        return !progress.Token.IsCancellationRequested;
    }

    #endregion

    #region Equality

    protected bool Equals(OperationIPrintedThisFile other)
    {
        return _volume == other._volume && _printTime.Equals(other._printTime) && Equals(MaterialItem, other.MaterialItem) && Multiplier == other.Multiplier;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationIPrintedThisFile) obj);
    }


    #endregion
}