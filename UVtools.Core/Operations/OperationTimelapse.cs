/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationTimelapse : Operation
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
{
    #region Enums

    public enum TimelapseRaiseMode
    {
        [Description("Lift height: Use the lift sequence to raise to the set postion (Faster)")]
        LiftHeight,

        [Description("Virtual layer: Print a blank layer to simulate and raise to the set position (Slower)")]
        VirtualLayer
    }

    #endregion

    #region Members

    private decimal _raiseEachNthHeight = 1;

    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.Normal;
    public override string IconClass => "Camera";
    public override string Title => "Timelapse";

    public override string Description =>
        "Raise the build platform to a set position every odd-even height to be able to take a photo and create a time-lapse video of the print.\n" +
        "You will require external hardware to take the photos, and create the time-lapse video by your own.\n" +
        "NOTE: Only use this tool once. It will delay the total print time significantly.";

    public override string ConfirmationText =>
        $"raise the platform at every odd-even {_raiseEachNthHeight}mm to Z={RaisePositionZ}mm and generate {NumberOfLifts} additional lifts?";

    public override string ProgressTitle => "Raising layers";

    public override string ProgressAction => "Raised layer";

    public override string? ValidateSpawn()
    {
        if (SlicerFile is { CanUseLayerPositionZ: false, CanUseLayerLiftHeight: false })
        {
            return NotSupportedMessage;
        }

        return null;
    }

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (!ValidateSpawn(out var message))
        {
            sb.AppendLine(message);
        }

        if ((RaiseMode == TimelapseRaiseMode.LiftHeight && !SlicerFile.CanUseLayerLiftHeight) ||
            (RaiseMode == TimelapseRaiseMode.VirtualLayer && !SlicerFile.CanUseLayerPositionZ))
        {
            sb.AppendLine(
                $"The raise method {RaiseMode} is not compatible with this printer / file format, please choose other method.");
        }

        if (NumberOfLifts == 0)
        {
            sb.AppendLine("The selected settings does not generate any lift for the timelapse.");
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Mode: {RaiseMode}] [Z={RaisePositionZ}mm] [Raise each: {_raiseEachNthHeight}mm]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LayerRangeCount))
        {
            OnPropertyChanged(nameof(NumberOfLifts));
        }

        base.OnPropertyChanged(e);
    }

    #endregion

    #region Properties

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLiftHeightMode))]
    [NotifyPropertyChangedFor(nameof(IsVirtualLayerMode))]
    [NotifyPropertyChangedFor(nameof(NumberOfLifts))]
    public partial TimelapseRaiseMode RaiseMode { get; set; } = TimelapseRaiseMode.LiftHeight;

    public bool IsLiftHeightMode => RaiseMode is TimelapseRaiseMode.LiftHeight;
    public bool IsVirtualLayerMode => RaiseMode is TimelapseRaiseMode.VirtualLayer;

    /// <summary>
    /// Sets or gets the Z position to raise to
    /// </summary>
    public decimal RaisePositionZ
    {
        get;
        set => SetProperty(ref field, Layer.RoundHeight(Math.Clamp(value, 10, 10000)));
    }

    /// <summary>
    /// True to output a dummy pixel on bounding rectangle position to avoid empty layer and blank image, otherwise set to false
    /// </summary>
    [ObservableProperty]
    public partial bool OutputDummyPixel { get; set; } = true;

    /// <summary>
    /// Gets or sets the alternating height in millimeters to raise when, it will raise only at each defined millimeters and skip the same next millimeters
    /// </summary>
    public decimal RaiseEachNthHeight
    {
        get => _raiseEachNthHeight;
        set
        {
            if (!SetProperty(ref _raiseEachNthHeight, Math.Max(0, Layer.RoundHeight(value)))) return;
            OnPropertyChanged(nameof(RaiseEachNthLayers));
            OnPropertyChanged(nameof(NumberOfLifts));
        }
    }

    /// <summary>
    /// Gets or sets the alternating layer count to raise when, it will raise only at each defined layers and skip the same next layers
    /// </summary>
    public ushort RaiseEachNthLayers
    {
        get
        {
            if (_raiseEachNthHeight == 0) return 1;
            return (ushort)Math.Max(1, _raiseEachNthHeight / (decimal)SlicerFile.LayerHeight);
        }
        set => RaiseEachNthHeight = (decimal)SlicerFile.LayerHeight * value;
    }

    /// <summary>
    /// Gets the total number of additional lifts
    /// </summary>
    public uint NumberOfLifts
    {
        get
        {
            if (_raiseEachNthHeight <= 0) return LayerRangeCount;
            var height = SlicerFile[LayerIndexEnd].PositionZ - SlicerFile[LayerIndexStart].PositionZ;
            if (height <= 0) return EnsureLastLayer && RaiseMode == TimelapseRaiseMode.VirtualLayer ? 1u : 0;
            /*return (uint)(Math.Min(SlicerFile.PrintHeight, (float)RaisePositionZ) /
                          Math.Max(SlicerFile.LayerHeight, (float)_raiseEachNthHeight))
                   + (EnsureLastLayer && RaiseMode == TimelapseRaiseMode.VirtualLayer ? 1u : 0);*/

            return (uint)(height / Math.Max(SlicerFile.LayerHeight, (float)_raiseEachNthHeight))
                   + (EnsureLastLayer && RaiseMode == TimelapseRaiseMode.VirtualLayer ? 1u : 0);
        }
    }

    public decimal WaitTimeAfterLift
    {
        get;
        set => SetProperty(ref field, Math.Max(0, value));
    } = 1;

    public decimal ExposureTime
    {
        get;
        set => SetProperty(ref field, Math.Max(0, value));
    } = 1;

    /// <summary>
    /// Gets or sets if last layer must be ensured in the sequence,
    /// If true, it will generate an obligatory additional layer to cover the last layer
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NumberOfLifts))]
    public partial bool EnsureLastLayer { get; set; } = true;

    [ObservableProperty] public partial bool UseCustomLift { get; set; }

    public bool CanUseCustomLift => UseCustomLift && SlicerFile.CanUseLayerLiftHeight;

    public decimal SlowLiftHeight
    {
        get;
        set => SetProperty(ref field, Math.Max(0, value));
    } = 3;

    public decimal LiftSpeed
    {
        get;
        set => SetProperty(ref field, Math.Max(0, value));
    }

    public decimal LiftSpeed2
    {
        get;
        set => SetProperty(ref field, Math.Max(0, value));
    }

    public decimal SlowRetractHeight
    {
        get;
        set => SetProperty(ref field, Math.Max(0, value));
    } = 3;

    public decimal RetractSpeed
    {
        get;
        set => SetProperty(ref field, Math.Max(0, value));
    }

    public decimal RetractSpeed2
    {
        get;
        set => SetProperty(ref field, Math.Max(0, value));
    }

    #endregion

    #region Constructor

    public OperationTimelapse()
    {
    }

    public OperationTimelapse(FileFormat slicerFile) : base(slicerFile)
    {
        if (RaisePositionZ <= 0) RaisePositionZ = (decimal)SlicerFile.MachineZ;
        if (ExposureTime <= 0) ExposureTime = SlicerFile.SupportGCode ? 0 : 0.05M;

        if (LiftSpeed <= 0) LiftSpeed = (decimal)SlicerFile.LiftSpeed;
        if (LiftSpeed2 <= 0) LiftSpeed2 = (decimal)SlicerFile.MaximumSpeed;

        if (SlicerFile.CanUseLayerRetractSpeed2) // TSMC
        {
            if (RetractSpeed <= 0) RetractSpeed = (decimal)SlicerFile.MaximumSpeed;
            if (RetractSpeed2 <= 0) RetractSpeed2 = (decimal)SlicerFile.RetractSpeed2;
        }
        else
        {
            if (RetractSpeed <= 0) RetractSpeed = (decimal)SlicerFile.RetractSpeed;
            if (RetractSpeed2 <= 0) RetractSpeed2 = (decimal)SlicerFile.MaximumSpeed;
        }
    }

    #endregion

    #region Equality

    protected bool Equals(OperationTimelapse other)
    {
        return RaisePositionZ == other.RaisePositionZ && OutputDummyPixel == other.OutputDummyPixel &&
               _raiseEachNthHeight == other._raiseEachNthHeight && RaiseMode == other.RaiseMode &&
               WaitTimeAfterLift == other.WaitTimeAfterLift && ExposureTime == other.ExposureTime &&
               EnsureLastLayer == other.EnsureLastLayer && UseCustomLift == other.UseCustomLift &&
               SlowLiftHeight == other.SlowLiftHeight && LiftSpeed == other.LiftSpeed &&
               LiftSpeed2 == other.LiftSpeed2 && SlowRetractHeight == other.SlowRetractHeight &&
               RetractSpeed == other.RetractSpeed && RetractSpeed2 == other.RetractSpeed2;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((OperationTimelapse)obj);
    }

    #endregion

    #region Methods

    public void OptimizeRaisePositionZ()
    {
        RaisePositionZ = (decimal)Math.Min(SlicerFile.MachineZ, SlicerFile.PrintHeight + 5);
    }

    public void MaxRaisePositionZ()
    {
        RaisePositionZ = (decimal)SlicerFile.MachineZ;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        var virtualLayers = new List<uint>();
        var checkpointHeight = SlicerFile[0].PositionZ;

        for (var layerIndex = LayerIndexStart; layerIndex <= LayerIndexEnd; layerIndex++)
        {
            progress++;
            var layer = SlicerFile[layerIndex];
            if ((decimal)layer.PositionZ >= RaisePositionZ) break; // pass the target height, do not continue
            if (RaiseMode == TimelapseRaiseMode.VirtualLayer && EnsureLastLayer && layerIndex == LayerIndexEnd)
            {
                virtualLayers.Add(layerIndex + 1);
                break;
            }

            if (_raiseEachNthHeight > 0 &&
                (decimal)Layer.RoundHeight(layer.PositionZ - checkpointHeight) < _raiseEachNthHeight) continue;
            checkpointHeight = layer.PositionZ;

            switch (RaiseMode)
            {
                case TimelapseRaiseMode.LiftHeight:
                    if (CanUseCustomLift)
                    {
                        layer.LiftSpeed = (float)LiftSpeed;
                        layer.RetractSpeed = (float)RetractSpeed;

                        if (SlicerFile.CanUseLayerLiftHeight2)
                        {
                            layer.LiftHeight = (float)SlowLiftHeight;
                        }

                        if (SlicerFile.CanUseLayerLiftSpeed2)
                        {
                            layer.LiftSpeed2 = (float)LiftSpeed2;
                        }

                        if (SlicerFile.CanUseLayerRetractHeight2)
                        {
                            layer.RetractHeight2 = (float)SlowRetractHeight;
                        }

                        if (SlicerFile.CanUseLayerRetractSpeed2)
                        {
                            layer.RetractSpeed2 = (float)RetractSpeed2;
                        }
                    }

                    if (SlicerFile.CanUseLayerLiftHeight2 &&
                        (layer.LiftHeight2 > 0 || (CanUseCustomLift && SlowLiftHeight > 0))) // TSMC
                    {
                        layer.LiftHeight2 = Math.Max(0, (float)RaisePositionZ - layer.PositionZ - layer.LiftHeight);
                    }
                    else
                    {
                        layer.LiftHeightTotal =
                            Math.Max(layer.LiftHeightTotal, (float)RaisePositionZ - layer.PositionZ);
                    }

                    if (SlicerFile.CanUseLayerWaitTimeAfterLift && WaitTimeAfterLift > 0)
                    {
                        layer.WaitTimeAfterLift = (float)WaitTimeAfterLift;
                    }

                    break;
                case TimelapseRaiseMode.VirtualLayer:
                    virtualLayers.Add(layerIndex);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(RaiseMode));
            }
        }

        if (virtualLayers.Count <= 0 || RaiseMode != TimelapseRaiseMode.VirtualLayer)
            return !progress.Token.IsCancellationRequested;

        var minLiftSpeed = CanUseCustomLift ? (float)Math.Min(LiftSpeed, LiftSpeed2) : SlicerFile.MinimumNormalSpeed;
        var minRetractSpeed = CanUseCustomLift
            ? (float)Math.Min(RetractSpeed, RetractSpeed2)
            : SlicerFile.MinimumNormalSpeed;
        var maxLiftSpeed = CanUseCustomLift ? (float)Math.Max(LiftSpeed, LiftSpeed2) : SlicerFile.MaximumSpeed;
        var maxRetractSpeed =
            CanUseCustomLift ? (float)Math.Max(RetractSpeed, RetractSpeed2) : SlicerFile.MaximumSpeed;

        using var mat = OutputDummyPixel
            ? SlicerFile.CreateMatWithDummyPixel()
            : SlicerFile.CreateMat();

        var virtualSlowLiftLayer = new Layer(SlicerFile.LayerCount, mat, SlicerFile)
        {
            PositionZ = (float)RaisePositionZ,
            LightPWM = SlicerFile.SupportGCode ? byte.MinValue : (byte)1,
            ExposureTime = SlicerFile.SupportGCode ? 0 : 0.01f,
            LiftHeightTotal = SlicerFile.SupportGCode ? 0 : 0.1f,
            LiftSpeed = minLiftSpeed,
            LiftSpeed2 = maxLiftSpeed,
            RetractSpeed = maxRetractSpeed,
            RetractSpeed2 = maxRetractSpeed,
            RetractHeight2 = 0
        };

        virtualSlowLiftLayer.SetNoDelays();

        var virtualPhotoLayer = virtualSlowLiftLayer.Clone();
        virtualPhotoLayer.ExposureTime = (float)ExposureTime;
        virtualPhotoLayer.LiftSpeed = maxLiftSpeed;

        virtualSlowLiftLayer.LightPWM = 0; // Disable light power if possible

        var slowLiftHeight = CanUseCustomLift ? (float)SlowLiftHeight : SlicerFile.LiftHeight;

        var layers = SlicerFile.ToList();
        uint insertedLayers = 0;
        foreach (var insertIndex in virtualLayers)
        {
            if (insertIndex < LayerIndexEnd)
            {
                // Replace lift with retract
                if (SlicerFile.CanUseLayerRetractHeight2 && SlicerFile[insertIndex].RetractHeight2 > 0)
                {
                    SlicerFile[insertIndex].LiftHeightTotal = SlicerFile[insertIndex].RetractHeight2;
                }

                SlicerFile[insertIndex].RetractHeight2 = 0;
                SlicerFile[insertIndex].LiftSpeed = maxRetractSpeed;

                if (CanUseCustomLift)
                {
                    SlicerFile[insertIndex].LiftHeight = (float)SlowRetractHeight;
                    SlicerFile[insertIndex].RetractSpeed = minRetractSpeed;

                    if (SlicerFile.CanUseLayerLiftSpeed2)
                    {
                        SlicerFile[insertIndex].LiftSpeed2 = maxLiftSpeed;
                    }

                    if (SlicerFile.CanUseLayerRetractSpeed2)
                    {
                        SlicerFile[insertIndex].RetractSpeed2 = maxRetractSpeed;
                    }
                }
            }

            var insertLayerIndex = (int)(insertIndex + insertedLayers);
            layers.Insert(insertLayerIndex, virtualPhotoLayer.Clone());

            if (slowLiftHeight > 0 && insertIndex > 0)
            {
                virtualSlowLiftLayer.PositionZ = SlicerFile[insertIndex - 1].PositionZ + slowLiftHeight;
                if (virtualSlowLiftLayer.PositionZ >= virtualPhotoLayer.PositionZ)
                {
                    // Slow lift layer must be lower than photo layer, break this insertion from now on
                    slowLiftHeight = 0;
                    virtualPhotoLayer.LiftSpeed = minLiftSpeed;
                    continue;
                }

                layers.Insert(insertLayerIndex, virtualSlowLiftLayer.Clone());
                insertedLayers++;
            }

            insertedLayers++;
        }

        SlicerFile.SuppressRebuildPropertiesWork(() => SlicerFile.Layers = layers.ToArray());

        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}