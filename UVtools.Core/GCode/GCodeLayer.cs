/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Operations;

namespace UVtools.Core.GCode;

public class GCodeLayer
{
    private float? _positionZ;
    private float? _waitTimeBeforeCure;
    private float? _exposureTime;
    private float? _waitTimeAfterCure;
    private float? _liftHeight;
    private float? _liftSpeed;
    private float? _liftHeight2;
    private float? _liftSpeed2;
    private float? _waitTimeAfterLift;
    private float? _retractSpeed;
    private float? _retractHeight2;
    private float? _retractSpeed2;

    public enum GCodeLastParsedLine : byte
    {
        LayerIndex,
    }

    public bool IsValid => LayerIndex.HasValue;

    public FileFormat SlicerFile { get; }
    public List<(float Pos, float Speed)> Movements = new();
    public uint? LayerIndex { get; set; }

    public float? PositionZ
    {
        get => _positionZ;
        set => _positionZ = value;
    }

    public float PreviousPositionZ { get; set; }

    public float? WaitTimeBeforeCure
    {
        get => _waitTimeBeforeCure;
        set => _waitTimeBeforeCure = value is null ? null : (float)Math.Round(value.Value, 2);
    }

    public float? ExposureTime
    {
        get => _exposureTime;
        set => _exposureTime = value is null ? null : (float)Math.Round(value.Value, 2);
    }

    public float? WaitTimeAfterCure
    {
        get => _waitTimeAfterCure;
        set => _waitTimeAfterCure = value is null ? null : (float)Math.Round(value.Value, 2);
    }

    public float? LiftHeight
    {
        get => _liftHeight;
        set => _liftHeight = value is null ? null : Layer.RoundHeight(value.Value);
    }

    public float? LiftSpeed
    {
        get => _liftSpeed;
        set => _liftSpeed = value is null ? null : (float)Math.Round(value.Value, 2);
    }

    public float LiftHeightTotal => Layer.RoundHeight((LiftHeight ?? 0) + (LiftHeight2 ?? 0));

    public float? LiftHeight2
    {
        get => _liftHeight2;
        set => _liftHeight2 = value is null ? null : Layer.RoundHeight(value.Value);
    }

    public float? LiftSpeed2
    {
        get => _liftSpeed2;
        set => _liftSpeed2 = value is null ? null : (float)Math.Round(value.Value, 2);
    }

    public float? WaitTimeAfterLift
    {
        get => _waitTimeAfterLift;
        set => _waitTimeAfterLift = value is null ? null : (float)Math.Round(value.Value, 2);
    }

    public float? RetractSpeed
    {
        get => _retractSpeed;
        set => _retractSpeed = value is null ? null : (float)Math.Round(value.Value, 2);
    }

    public float? RetractHeight2
    {
        get => _retractHeight2;
        set => _retractHeight2 = value is null ? null : Layer.RoundHeight(value.Value);
    }

    public float? RetractSpeed2
    {
        get => _retractSpeed2;
        set => _retractSpeed2 = value is null ? null : (float)Math.Round(value.Value, 2);
    }

    public byte? LightPWM { get; set; }

    public bool IsExposing => LightPWM.HasValue && !IsAfterLightOff;
    public bool IsExposed => LightPWM.HasValue && IsAfterLightOff;

    public byte LightOffCount { get; set; }
    public bool IsAfterLightOff => LightOffCount > 0;

    public GCodeLayer(FileFormat slicerFile)
    {
        SlicerFile = slicerFile;
    }

    public void Init()
    {
        PreviousPositionZ = PositionZ ?? 0;

        Movements.Clear();
        LayerIndex = null;
        PositionZ = null;
        WaitTimeBeforeCure = null;
        ExposureTime = null;
        WaitTimeAfterCure = null;
        LiftHeight = null;
        LiftSpeed = null;
        LiftHeight2 = null;
        LiftSpeed2 = null;
        WaitTimeAfterLift = null;
        RetractSpeed = null;
        RetractHeight2 = null;
        RetractSpeed2 = null;
        LightPWM = null;
        LightOffCount = 0;
    }

    public void AssignMovements(GCodeBuilder.GCodePositioningTypes positionType)
    {
        if (Movements.Count == 0) return;
        float currentZ = PreviousPositionZ;

        PositionZ = null;
        LiftHeight = null;
        LiftSpeed = null;
        LiftHeight2 = null;
        LiftSpeed2 = null;
        RetractSpeed = null;
        RetractHeight2 = null;
        RetractSpeed2 = null;

        var previousLayerEmpty = LayerIndex > 0 && SlicerFile[LayerIndex.Value - 1].NonZeroPixelCount <= 1;

        for (int i = 0; i < Movements.Count; i++)
        {
            var (pos, speed) = Movements[i];
            float partialPositionZ;
            switch (positionType)
            {
                case GCodeBuilder.GCodePositioningTypes.Absolute:
                    partialPositionZ = Layer.RoundHeight(pos - currentZ);
                    currentZ = pos;
                    break;
                case GCodeBuilder.GCodePositioningTypes.Partial:
                    partialPositionZ = pos;
                    currentZ = Layer.RoundHeight(currentZ + pos);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(positionType));
            }

            // Fail-safe check
                
            if (currentZ < PreviousPositionZ && !previousLayerEmpty)
            {
                throw new NotSupportedException(
                    $"GCode parsing error: Attempting to crash the print on the LCD due a lower position ({currentZ}mm) than the previous layer ({PreviousPositionZ}mm).\n" +
                    "Do not attempt to print this file!");
            }

            // Last movement on the list, must be position Z
            if (i == Movements.Count - 1)
            {
                PositionZ = currentZ;
                if (LiftHeight.HasValue && partialPositionZ < 0) 
                {
                    RetractSpeed = speed; // A lift exists, and its descending, set to retract speed of this move
                }
                break;
            }

            if (partialPositionZ == 0) continue;
            var height = Math.Abs(partialPositionZ);

            if (currentZ < PreviousPositionZ) // Check for inverse lifts
            {
                LiftHeight ??= 0;
                LiftHeight = Math.Min(LiftHeight.Value, -currentZ);
                LiftSpeed = speed;
                continue;
            }
                
            if (partialPositionZ > 0) // Is a lift
            {
                // Lift 1
                if (!LiftHeight.HasValue)
                {
                    LiftHeight = height;
                    LiftSpeed = speed;
                    continue;
                }

                // Lift 2
                LiftHeight2 ??= 0;
                LiftHeight2 += height;
                LiftSpeed2 = speed;

                continue;
            }

            if(!LiftHeight.HasValue) continue; // Fail-safe: Retract without a lift? Skip

            // Is a extra retract (2)
            RetractHeight2 ??= 0;
            RetractHeight2 += height;
            RetractSpeed2 = speed;
        }

        if (Movements.Count == 1) // Only 1 move, this is the PositionZ only
        {
            LiftSpeed = Movements[0].Speed;
            return;
        }

        // Sanitize
        if (PositionZ.HasValue && LiftHeight.HasValue)
        {
            if (LiftHeight < 0) // Inverse lift
            {
                LiftHeight = Layer.RoundHeight(Math.Abs(PositionZ.Value + LiftHeight.Value));
            }
            else if (!IsExposed) // Lift before exposure order, need to remove layer height as offset
            {
                var liftHeight = Layer.RoundHeight(LiftHeight.Value - (PositionZ.Value - PreviousPositionZ));
                if (liftHeight <= 0) return; // Something not right or not the correct moment, skip
                LiftHeight = liftHeight;
            }
        }

        if (PositionZ.HasValue && LiftHeight.HasValue && !IsExposed) 
        {
                
        }

        if (RetractHeight2.HasValue) // Need to fix the purpose of this value
        {
            RetractHeight2 = Layer.RoundHeight(LiftHeightTotal - RetractHeight2.Value);
            (RetractSpeed, RetractSpeed2) = (RetractSpeed2, RetractSpeed);
        }

        if (LiftHeight.HasValue && RetractHeight2.HasValue) // Sanitize RetractHeight2 value
        {
            RetractHeight2 = Math.Clamp(RetractHeight2.Value, 0, LiftHeightTotal);
        }
    }

    /// <summary>
    /// Set gathered data to the layer
    /// </summary>
    public void SetLayer(bool reinit = false)
    {
        if (!IsValid) return;
        uint layerIndex = LayerIndex!.Value;
        var layer = SlicerFile[layerIndex];
            
        PositionZ ??= PreviousPositionZ;
        layer.PositionZ = PositionZ.Value;
        layer.WaitTimeBeforeCure = WaitTimeBeforeCure ?? 0;
        layer.ExposureTime = ExposureTime ?? 0;
        layer.WaitTimeAfterCure = WaitTimeAfterCure ?? 0;
        layer.LiftHeight = LiftHeight ?? 0;
        layer.LiftSpeed = LiftSpeed ?? SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomLiftSpeed, SlicerFile.LiftSpeed);
        layer.LiftHeight2 = LiftHeight2 ?? 0;
        layer.LiftSpeed2 = LiftSpeed2 ?? SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomLiftSpeed2, SlicerFile.LiftSpeed2);
        layer.WaitTimeAfterLift = WaitTimeAfterLift ?? 0;
        layer.RetractSpeed = RetractSpeed ?? SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomRetractSpeed, SlicerFile.RetractSpeed);
        layer.RetractHeight2 = RetractHeight2 ?? 0;
        layer.RetractSpeed2 = RetractSpeed2 ?? SlicerFile.GetBottomOrNormalValue(layer, SlicerFile.BottomRetractSpeed2, SlicerFile.RetractSpeed2);
        layer.LightPWM = LightPWM ?? 0;//SlicerFile.GetInitialLayerValueOrNormal(layerIndex, SlicerFile.BottomLightPWM, SlicerFile.LightPWM);

        if (SlicerFile.GCode!.SyncMovementsWithDelay) // Dirty fix of the value
        {
            var syncTime = OperationCalculator.LightOffDelayC.CalculateSeconds(layer, 1.5f);
            if (syncTime < layer.WaitTimeBeforeCure)
            {
                layer.WaitTimeBeforeCure = (float) Math.Round(layer.WaitTimeBeforeCure - syncTime, 2);
            }
        }

        if(reinit) Init();
    }
}