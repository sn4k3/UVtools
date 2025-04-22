/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Converters;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.Core.GCode;

public class GCodeBuilder : BindableBase
{
    #region Events
    
    public sealed class  LayerChangeEventArgs : CancelEventArgs
    {
        public uint LayerIndex { get; set; }
        
        public LayerChangeEventArgs(uint layerIndex)
        {
            LayerIndex = layerIndex;
        }
    }
    public delegate void LayerChangeEventHandler(object? sender, LayerChangeEventArgs e);

    public event EventHandler? BeforeRebuildGCode;
    protected virtual void OnBeforeRebuildGCode()
    {
        BeforeRebuildGCode?.Invoke(this, EventArgs.Empty);
    }

    public event CancelEventHandler? BeforeWriteStartGCode;
    protected virtual bool OnBeforeWriteStartGCode()
    {
        var e = new CancelEventArgs();
        BeforeWriteStartGCode?.Invoke(this, e);
        return e.Cancel;
    }

    public event CancelEventHandler? AfterWriteStartGCode;
    protected virtual bool OnAfterWriteStartGCode()
    {
        var e = new CancelEventArgs();
        AfterWriteStartGCode?.Invoke(this, e);
        return e.Cancel;
    }


    public event LayerChangeEventHandler? BeforeWriteLayerGCode;
    protected virtual bool OnBeforeWriteLayerGCode(uint layerIndex)
    {
        var e = new LayerChangeEventArgs(layerIndex);
        BeforeWriteLayerGCode?.Invoke(this, e);
        return e.Cancel;
    }

    public event LayerChangeEventHandler? AfterWriteLayerGCode;
    protected virtual bool OnAfterWriteLayerGCode(uint layerIndex)
    {
        var e = new LayerChangeEventArgs(layerIndex);
        AfterWriteLayerGCode?.Invoke(this, new LayerChangeEventArgs(layerIndex));
        return e.Cancel;
    }

    public event CancelEventHandler? BeforeWriteEndGCode;
    protected virtual bool OnBeforeWriteEndGcode()
    {
        var e = new CancelEventArgs();
        BeforeWriteEndGCode?.Invoke(this, e);
        return e.Cancel;
    }

    public event CancelEventHandler? AfterWriteEndGCode;
    protected virtual bool OnAfterWriteEndGcode()
    {
        var e = new CancelEventArgs();
        AfterWriteEndGCode?.Invoke(this, e);
        return e.Cancel;
    }

    public event EventHandler? AfterRebuildGCode;
    protected virtual void OnAfterRebuildGCode()
    {
        AfterRebuildGCode?.Invoke(this, EventArgs.Empty);
    }
    #endregion

    #region Commands

    public GCodeCommand CommandUnitsMillimetersG21 { get; } = new("G21", null, "Set units to be mm");
    public GCodeCommand CommandPositioningAbsoluteG90 { get; } = new("G90", null, "Absolute positioning");
    public GCodeCommand CommandPositioningPartialG91 { get; } = new("G91", null, "Partial positioning");

    public GCodeCommand CommandMotorsOnM17 { get; } = new("M17", null, "Enable motors");
    public GCodeCommand CommandMotorsOffM18 { get; } = new("M18", null, "Disable motors");

    public GCodeCommand CommandHomeG28 { get; } = new("G28", "Z0", "Home Z");

    public GCodeCommand CommandMoveG0 { get; } = new("G0", "Z{0} F{1}", "Move Z");
    public GCodeCommand CommandMoveG1 { get; } = new("G1", "Z{0} F{1}", "Move Z");
    public GCodeCommand CommandSetAccelerationM204 { get; } = new("M204", "S{0}", "Set acceleration", false);
    
    public GCodeCommand CommandSyncMovements { get; } = new("G4", "P0", "Sync movements", false);

    public GCodeCommand CommandWaitSyncDelay { get; } = new("G4", "P0{0}", "Sync movement", false);
    public GCodeCommand CommandWaitG4 { get; } = new("G4", "P{0}", "Delay");
    
    public GCodeCommand CommandShowImageM6054 = new("M6054", "\"{0}\"", "Show image");
    public GCodeCommand CommandClearImage = new(";<Slice> Blank"); // Clear image
    public GCodeCommand CommandTurnLedM106 { get; } = new("M106", "S{0}", "Turn LED");
    public GCodeCommand CommandTurnLedOff { get; } = new("M106", "S0", "Turn LED off");
    public GCodeCommand CommandSetPrintProgressM73 { get; } = new("M73", "P{0} R{1}", "Set print progress", false);

    public GCodeCommand KlipperCommandTurnLedM1400 { get; } = new("M1400", "S{0} P{1}", "Turn LED", false);

    public GCodeCommand CommandPauseM25 { get; } = new("M25", null, "Pause");
    public GCodeCommand CommandChangeResinM600 { get; } = new("M600", null, "Resin change");

    /// <summary>
    /// Sets Klipper GCode standard
    /// </summary>
    public void SetKlipperStandard()
    {
        _gCodeClearImagePosition = GCodeClearImagePositions.End;

        CommandMotorsOnM17.Enabled = false;
        CommandHomeG28.Arguments = "Z";
        CommandSyncMovements.Command = "M400";
        CommandSyncMovements.Arguments = null;
        CommandWaitSyncDelay.Command = "M400";
        CommandWaitSyncDelay.Arguments = null;
        CommandTurnLedM106.Enabled = false;
        CommandClearImage.Command = "M1450";
        CommandShowImageM6054.Command = "M1451";
        CommandShowImageM6054.Arguments = $"F{CommandShowImageM6054.Arguments}";
        CommandSetPrintProgressM73.Enabled = true;

        CommandTurnLedOff.Command = "M1401";
        CommandTurnLedOff.Arguments = null;
        KlipperCommandTurnLedM1400.Enabled = true;

        CommandSetAccelerationM204.Enabled = true;
        CommandPauseM25.Enabled = true;
        CommandChangeResinM600.Enabled = true;

        CommandPauseM25.CommandAlias = ["PAUSE"];

        EncodeThumbnails = true;
    }
    #endregion

    #region Enums

    public enum GCodePositioningTypes : byte
    {
        Absolute,
        Relative
    }

    public enum GCodeTimeUnits : byte
    {
        /// <summary>
        /// ms
        /// </summary>
        Milliseconds,
        /// <summary>
        /// s
        /// </summary>
        Seconds
    }

    public enum GCodeSpeedUnits : byte
    {
        /// <summary>
        /// mm/s
        /// </summary>
        MillimetersPerSecond,
        /// <summary>
        /// mm/m
        /// </summary>
        MillimetersPerMinute,
        /// <summary>
        /// cm/m
        /// </summary>
        CentimetersPerMinute,
    }

    public enum GCodeMoveCommands : byte
    {
        G0, // Fast
        G1  // Interpolated
    }

    public enum GCodeShowImageTypes : byte
    {
        FilenamePng0Started,
        FilenamePng1Started,
        LayerIndex0Started,
        LayerIndex1Started,
    }

    public enum GCodeShowImagePositions : byte
    {
        /// <summary>
        /// Show image at start of each layer block commands
        /// </summary>
        FirstLine,

        /// <summary>
        /// Show image just before exposing / turning LED ON
        /// </summary>
        WhenRequired
    }

    [Flags]
    public enum GCodeClearImagePositions : byte
    {
        None = 0,
        Start = 1,
        Layer = 2,
        End = 4
    }

    #endregion

    #region Members
    private readonly StringBuilder _gcode = new();

       
    private GCodePositioningTypes _gCodePositioningType = GCodePositioningTypes.Absolute;
    private GCodeTimeUnits _gCodeTimeUnit = GCodeTimeUnits.Milliseconds;
    private GCodeSpeedUnits _gCodeSpeedUnit = GCodeSpeedUnits.MillimetersPerMinute;
    private GCodeShowImageTypes _gCodeShowImageType = GCodeShowImageTypes.FilenamePng1Started;
    private bool _useTailComma = true;
    private bool _useComments = true;
    private ushort _maxLedPower = byte.MaxValue;
    private bool _encodeThumbnails;
    private uint _lineCount;
    private GCodeMoveCommands _layerMoveCommand;
    private GCodeMoveCommands _endGCodeMoveCommand;
    private GCodeShowImagePositions _gCodeShowImagePosition = GCodeShowImagePositions.FirstLine;
    private GCodeClearImagePositions _gCodeClearImagePosition = GCodeClearImagePositions.Start |
                                             GCodeClearImagePositions.Layer |
                                             GCodeClearImagePositions.End;

    private float _lastAcceleration = 0;

    #endregion

    #region Properties

    public GCodePositioningTypes GCodePositioningType
    {
        get => _gCodePositioningType;
        set => RaiseAndSetIfChanged(ref _gCodePositioningType, value);
    }

    public GCodeTimeUnits GCodeTimeUnit
    {
        get => _gCodeTimeUnit;
        set => RaiseAndSetIfChanged(ref _gCodeTimeUnit, value);
    }

    public GCodeSpeedUnits GCodeSpeedUnit
    {
        get => _gCodeSpeedUnit;
        set => RaiseAndSetIfChanged(ref _gCodeSpeedUnit, value);
    }

    public GCodeShowImageTypes GCodeShowImageType
    {
        get => _gCodeShowImageType;
        set => RaiseAndSetIfChanged(ref _gCodeShowImageType, value);
    }

    public GCodeShowImagePositions GCodeShowImagePosition
    {
        get => _gCodeShowImagePosition;
        set => RaiseAndSetIfChanged(ref _gCodeShowImagePosition, value);
    }

    public GCodeClearImagePositions GCodeClearImagePosition
    {
        get => _gCodeClearImagePosition;
        set => RaiseAndSetIfChanged(ref _gCodeClearImagePosition, value);
    }

    public GCodeMoveCommands LayerMoveCommand
    {
        get => _layerMoveCommand;
        set => RaiseAndSetIfChanged(ref _layerMoveCommand, value);
    }

    public GCodeMoveCommands EndGCodeMoveCommand
    {
        get => _endGCodeMoveCommand;
        set => RaiseAndSetIfChanged(ref _endGCodeMoveCommand, value);
    }

    public bool UseTailComma
    {
        get => _useTailComma;
        set => RaiseAndSetIfChanged(ref _useTailComma, value);
    }

    public bool UseComments
    {
        get => _useComments;
        set => RaiseAndSetIfChanged(ref _useComments, value);
    }

    public ushort MaxLEDPower
    {
        get => _maxLedPower;
        set => RaiseAndSetIfChanged(ref _maxLedPower, value);
    }

    public bool EncodeThumbnails
    {
        get => _encodeThumbnails;
        set => RaiseAndSetIfChanged(ref _encodeThumbnails, value);
    }

    public string BeginStartGCodeComments { get; set; } = ";START_GCODE_BEGIN";
    public string EndStartGCodeComments { get; set; } = ";END_GCODE_BEGIN";

    public string BeginLayerComments { get; set; } = $";LAYER_START:{{0}}{Environment.NewLine};PositionZ:{{1}}mm";

    public string EndLayerComments { get; set; } = ";LAYER_END";

    public string BeginEndGCodeComments { get; set; } = ";START_GCODE_END";
    public string EndEndGCodeComments { get; set; } = $";END_GCODE_END{Environment.NewLine};<Completed>";

    public uint LineCount
    {
        get => _lineCount;
        set
        {
            if(!RaiseAndSetIfChanged(ref _lineCount, value)) return;
            //RaisePropertyChanged(nameof(IsEmpty));
        }
    }

    public bool IsEmpty => _lineCount <= 0 || _gcode.Length <= 0;
    public int Length => _gcode.Length;

    #endregion

    #region StringBuilder

    public void Append(string text)
    {
        _gcode.Append(text);
    }

    public void Append(StringBuilder sb)
    {
        _gcode.Append(sb);
    }

    public void AppendLine()
    {
        _gcode.AppendLine();
        //LineCount++;
    }

    public void AppendLine(string line)
    {
        if (line is null) return;
        _gcode.AppendLine(line);
        LineCount++;
    }

    public void AppendLine(string line, params object[] args)
    {
        if (line is null) return;
        _gcode.AppendLine(string.Format(line, args));
        LineCount++;
    }

    public void AppendLine(GCodeCommand command)
    {
        if (!command.Enabled) return;
        AppendLine(command.ToString(_useComments, _useTailComma));
        LineCount++;
    }

    public void AppendLineOverrideComment(GCodeCommand command, string? comment, params object[] args)
    {
        if (!command.Enabled) return;
        AppendLine(command.ToStringOverrideComment(_useComments, _useTailComma, comment, args));
        LineCount++;
    }

    public void AppendLine(GCodeCommand command, params object[] args)
    {
        if (!command.Enabled) return;
        AppendLine(command.ToString(_useComments, _useTailComma, args));
        LineCount++;
    }

    public void AppendFormat(string format, params object[] args)
    {
        _gcode.AppendFormat(format, args);
        LineCount += (uint)format.Count(c => c == '\n');
    }

    public void AppendLineIfCanComment(string line)
    {
        if (string.IsNullOrWhiteSpace(line) || !_useComments) return;
        AppendLine(line);
    }

    public void AppendLineIfCanComment(string line, params object[] args)
    {
        if (string.IsNullOrWhiteSpace(line) || !_useComments) return;
        AppendLine(line, args);
    }

    public void AppendComment(string comment)
    {
        if (string.IsNullOrWhiteSpace(comment) || !_useComments) return;
        AppendLine($";{comment}");
    }

    public void Clear()
    {
        _gcode.Clear();
        LineCount = 0;
        _lastAcceleration = 0;
    }

    public override string ToString() => _gcode.ToString();
    #endregion

    #region Methods

    public string FormatGCodeLine(string line, string? comment = null)
    {
        if (line[0] == ';') return line;
        if (_useComments && !string.IsNullOrWhiteSpace(comment))
        {
            line += $";{comment}";
        }
        else if (_useTailComma)
        {
            line += ';';
        }

        return line;
    }

    public void AppendUVtools()
    {
        AppendComment($" Generated by {About.Software} v{About.VersionString} {RuntimeInformation.RuntimeIdentifier} {About.SystemBits} on {DateTime.UtcNow} UTC");
    }

    public void AppendStartGCode(FileFormat slicerFile)
    {
        AppendLineIfCanComment(BeginStartGCodeComments);

        if (!OnBeforeWriteStartGCode())
        {
            AppendUnitsMmG21();
            AppendPositioningType();
            AppendLightOffM106();
            if ((_gCodeClearImagePosition & GCodeClearImagePositions.Start) != 0) AppendClearImage();
            AppendMotorsOn();
            AppendHomeZG28();
            AppendSyncMovements();
        }

        OnAfterWriteStartGCode();
        AppendLineIfCanComment(EndStartGCodeComments);
        AppendLine();
    }

    public void AppendEndGCode(FileFormat slicerFile)
    {
        AppendLineIfCanComment(BeginEndGCodeComments);

        if (!OnBeforeWriteEndGcode())
        {
            AppendPrintProgressM73(100, 0);
            AppendLightOffM106();
            if ((_gCodeClearImagePosition & GCodeClearImagePositions.End) != 0) AppendClearImage();

            var lastLayer = slicerFile.LastLayer;

            if (lastLayer is not null && lastLayer.PositionZ < slicerFile.MachineZ)
            {
                float lastLiftHeight = Math.Max(4, lastLayer.LiftHeight);
                float lastPosition = Math.Min(Layer.RoundHeight(lastLayer.PositionZ + lastLiftHeight),
                    slicerFile.MachineZ);

                var lift = new List<(float z, float feedrate, float acceleration)>();
                switch (_gCodePositioningType)
                {
                    case GCodePositioningTypes.Absolute:
                        lift.Add((lastPosition, ConvertFromMillimetersPerMinute(slicerFile.LiftSpeed), slicerFile.LiftAcceleration));
                        break;
                    case GCodePositioningTypes.Relative:
                        lift.Add((lastLiftHeight, ConvertFromMillimetersPerMinute(slicerFile.LiftSpeed), slicerFile.LiftAcceleration));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                AppendLiftMoveGx(lift, [], 0, 0, lastLayer);

                if (lastPosition < slicerFile.MachineZ)
                {
                    float finalRaiseZPositionRelative = Layer.RoundHeight(slicerFile.MachineZ - lastPosition);
                    var finalRaiseZPosition = _gCodePositioningType switch
                    {
                        GCodePositioningTypes.Relative => finalRaiseZPositionRelative,
                        _ => slicerFile.MachineZ
                    };


                    if (finalRaiseZPositionRelative > 0)
                    {
                        if (_endGCodeMoveCommand == GCodeMoveCommands.G0)
                            AppendMoveG0(finalRaiseZPosition, ConvertFromMillimetersPerMinute(slicerFile.MaximumSpeed));
                        else
                            AppendMoveG1(finalRaiseZPosition, ConvertFromMillimetersPerMinute(slicerFile.MaximumSpeed));

                        if (CommandWaitSyncDelay.Enabled)
                        {
                            var seconds = OperationCalculator.LightOffDelayC.CalculateSecondsLiftOnly(
                                finalRaiseZPositionRelative + lastLiftHeight, slicerFile.MaximumSpeed, 0.75f);
                            var time = ConvertFromSeconds(seconds);
                            if (seconds > 0) AppendWaitSyncDelay(time);
                        }

                        AppendSyncMovements();
                    }
                }
            }

            AppendMotorsOff();
        }

        OnAfterWriteEndGcode();
        AppendLineIfCanComment(EndEndGCodeComments);
    }

    public void AppendEndGCode(float raiseZ = 0, float feedRate = 0, float absRaiseZ = 0, float rawSpeed = 0)
    {
        AppendLineIfCanComment(BeginEndGCodeComments);
        OnBeforeWriteEndGcode();
        AppendLightOffM106();
        if (raiseZ > 0)
        {
            if (_endGCodeMoveCommand == GCodeMoveCommands.G0)
                AppendMoveG0(raiseZ, feedRate);
            else
                AppendMoveG1(raiseZ, feedRate);

            if (CommandWaitSyncDelay.Enabled)
            {
                var seconds = OperationCalculator.LightOffDelayC.CalculateSecondsLiftOnly(absRaiseZ, rawSpeed, 0.75f);
                var time = ConvertFromSeconds(seconds);
                AppendWaitSyncDelay(time);
            }
        }

        AppendSyncMovements();
        AppendMotorsOff();
        OnAfterWriteEndGcode();
        AppendLineIfCanComment(EndEndGCodeComments);
    }

    public void AppendUnitsMmG21()
    {
        AppendLine(CommandUnitsMillimetersG21);
    }

    public void AppendPositioningType()
    {
        switch (GCodePositioningType)
        {
            case GCodePositioningTypes.Absolute:
                AppendLine(CommandPositioningAbsoluteG90);
                break;
            case GCodePositioningTypes.Relative:
                AppendLine(CommandPositioningPartialG91);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void AppendMotorsOn()
    {
        AppendLine(CommandMotorsOnM17);
    }

    public void AppendMotorsOff()
    {
        AppendLine(CommandMotorsOffM18);
    }

    public void AppendTurnMotors(bool enable)
    {
        if (enable)
            AppendMotorsOn();
        else 
            AppendMotorsOff();
    }

    public void AppendHomeZG28()
    {
        AppendLine(CommandHomeG28);
    }

    public void AppendSyncMovements()
    {
        AppendLine(CommandSyncMovements);
    }

    public void AppendLayerPause(Layer layer)
    {
        if (layer.Pause) AppendLine(CommandPauseM25);
    }

    public void AppendLayerChangeResinM600(Layer layer)
    {
        if (layer.ChangeResin) AppendLine(CommandChangeResinM600);
    }

    public void AppendMoveGx(float z, float feedRate)
    {
        if(_layerMoveCommand == GCodeMoveCommands.G0)
            AppendMoveG0(z, feedRate);
        else
            AppendMoveG1(z, feedRate);
    }

    public void AppendLiftMoveGx(List<(float z, float feedrate, float acceleration)> lifts, 
        List<(float z, float feedrate, float acceleration)> retracts, float waitAfterLift = 0, float waitAfterRetract = 0, Layer? layer = null)
    {
        if (_layerMoveCommand == GCodeMoveCommands.G0)
            AppendLiftMoveG0(lifts, retracts, waitAfterLift, waitAfterRetract, layer);
        else
            AppendLiftMoveG1(lifts, retracts, waitAfterLift, waitAfterRetract, layer);
    }


    public void AppendMoveG0(float z, float feedRate)
    {
        if(z == 0 || feedRate <= 0) return;
        AppendLine(CommandMoveG0, z, feedRate);
    }

    public void AppendLiftMoveG0(List<(float z, float feedrate, float acceleration)> lifts, 
        List<(float z, float feedrate, float acceleration)> retracts, float waitAfterLift = 0, float waitAfterRetract = 0, Layer? layer = null)
    {
        if (lifts.Count == 0 || lifts.All(tuple => tuple.z <= 0)) return;

        for (var i = 0; i < lifts.Count; i++)
        {
            if (lifts[i].z <= 0) continue;
            if (CommandSetAccelerationM204.Enabled && lifts[i].acceleration > 0 && lifts[i].acceleration != _lastAcceleration)
            {
                AppendLine(CommandSetAccelerationM204, lifts[i].acceleration);
                _lastAcceleration = lifts[i].acceleration;
            }
            AppendLineOverrideComment(CommandMoveG0, $"Z Lift ({i+1})", lifts[i].z, lifts[i].feedrate); // Z Lift
        }

        if (CommandWaitSyncDelay.Enabled && layer is not null)
        {
            var seconds = OperationCalculator.LightOffDelayC.CalculateSecondsLiftOnly(layer, 0.75f);
            var time = ConvertFromSeconds(seconds);
            AppendWaitSyncDelay(time);
        }

        AppendSyncMovements();

        if (waitAfterLift > 0)
        {
            AppendWaitG4(waitAfterLift, "Wait after lift");
        }

        if (retracts.Count > 0 && retracts.All(tuple => tuple.z != 0))
        {
            for (var i = 0; i < retracts.Count; i++)
            {
                if (retracts[i].z == 0) continue;
                if (CommandSetAccelerationM204.Enabled && retracts[i].acceleration > 0 && retracts[i].acceleration != _lastAcceleration)
                {
                    AppendLine(CommandSetAccelerationM204, retracts[i].acceleration);
                    _lastAcceleration = retracts[i].acceleration;
                }
                AppendLineOverrideComment(CommandMoveG0, $"Retract ({i+1})", retracts[i].z, retracts[i].feedrate);
            }

            if (CommandWaitSyncDelay.Enabled && layer is not null)
            {
                var seconds = OperationCalculator.LightOffDelayC.CalculateSecondsLiftOnly(layer.RetractHeight, layer.RetractSpeed, layer.RetractHeight2, layer.RetractSpeed2, 0.75f);
                var time = ConvertFromSeconds(seconds);
                AppendWaitSyncDelay(time);
            }

            AppendSyncMovements();

            if (waitAfterRetract > 0)
            {
                AppendWaitG4(waitAfterRetract, "Wait after retract");
            }
        }
    }

    public void AppendLiftMoveG0(float upZ, float upFeedrate, float upAcceleration, float downZ, float downFeedrate, float downAcceleration,
        float waitAfterLift = 0, float waitAfterRetract = 0, Layer? layer = null)
        => AppendLiftMoveG0(
            new List<(float, float, float)> { new(upZ, upFeedrate, upAcceleration) }, 
            new List<(float, float, float)> { new(downZ, downFeedrate, downAcceleration) }, 
            waitAfterLift, waitAfterRetract, layer);

    public void AppendMoveG1(float z, float feedRate)
    {
        if (z == 0 || feedRate <= 0) return;
        AppendLine(CommandMoveG1, z, feedRate);
    }

    public void AppendLiftMoveG1(List<(float z, float feedrate, float acceleration)> lifts, 
        List<(float z, float feedrate, float acceleration)> retracts, float waitAfterLift = 0, float waitAfterRetract = 0, Layer? layer = null)
    {
        if (lifts.Count == 0 || lifts.All(tuple => tuple.z <= 0)) return;

        for (var i = 0; i < lifts.Count; i++)
        {
            if (lifts[i].z <= 0) continue;
            if (CommandSetAccelerationM204.Enabled && lifts[i].acceleration > 0 && lifts[i].acceleration != _lastAcceleration)
            {
                AppendLine(CommandSetAccelerationM204, lifts[i].acceleration);
                _lastAcceleration = lifts[i].acceleration;
            }
            AppendLineOverrideComment(CommandMoveG1, $"Z Lift ({i+1})", lifts[i].z, lifts[i].feedrate); // Z Lift
        }

        if (CommandWaitSyncDelay.Enabled && layer is not null)
        {
            var seconds = OperationCalculator.LightOffDelayC.CalculateSecondsLiftOnly(layer, 0.75f);
            var time = ConvertFromSeconds(seconds);
            AppendWaitSyncDelay(time);
        }

        AppendSyncMovements();

        if (waitAfterLift > 0)
        {
            AppendWaitG4(waitAfterLift, "Wait after lift");
        }

        if (retracts.Count > 0 && retracts.All(tuple => tuple.z != 0))
        {
            for (var i = 0; i < retracts.Count; i++)
            {
                if (retracts[i].z == 0) continue;
                if (CommandSetAccelerationM204.Enabled && retracts[i].acceleration > 0 && retracts[i].acceleration != _lastAcceleration)
                {
                    AppendLine(CommandSetAccelerationM204, retracts[i].acceleration);
                    _lastAcceleration = retracts[i].acceleration;
                }
                AppendLineOverrideComment(CommandMoveG1, $"Retract ({i+1})", retracts[i].z, retracts[i].feedrate);
            }

            if (CommandWaitSyncDelay.Enabled && layer is not null)
            {
                var seconds = OperationCalculator.LightOffDelayC.CalculateSecondsLiftOnly(layer.RetractHeight, layer.RetractSpeed, layer.RetractHeight2, layer.RetractSpeed2, 0.75f);
                var time = ConvertFromSeconds(seconds);
                AppendWaitSyncDelay(time);
            }

            AppendSyncMovements();

            if (waitAfterRetract > 0)
            {
                AppendWaitG4(waitAfterRetract, "Wait after retract");
            }
        }
    }

    public void AppendLiftMoveG1(float upZ, float upFeedrate, float upAcceleration, float downZ, float downFeedrate, float downAcceleration,
        float waitAfterLift = 0, float waitAfterRetract = 0, Layer? layer = null)
        => AppendLiftMoveG1(
            new List<(float, float, float)> { new(upZ, upFeedrate, upAcceleration) },
            new List<(float, float, float)> { new(downZ, downFeedrate, downAcceleration) },
            waitAfterLift, waitAfterRetract, layer);

    public void AppendWaitSyncDelay(float time, string? comment = null)
    {
        if (time < 0) return;
        AppendLineOverrideComment(CommandWaitSyncDelay, comment, time);
    }

    public void AppendWaitSyncDelay(string timeStr, string? comment = null)
    {
        if (!float.TryParse(timeStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var time)) return;
        if (time < 0) return;
        AppendLineOverrideComment(CommandWaitSyncDelay, comment, timeStr);
    }

    public void AppendWaitG4(float time, string? comment = null)
    {
        if (time < 0) return;
        AppendLineOverrideComment(CommandWaitG4, comment, time);
    }

    public void AppendWaitG4(string timeStr, string? comment = null)
    {
        if (!float.TryParse(timeStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var time)) return;
        if (time < 0) return;
        AppendLineOverrideComment(CommandWaitG4, comment, timeStr);
    }

    public void AppendTurnLightM106(ushort value, float exposureTime = 0)
    {
        if (KlipperCommandTurnLedM1400.Enabled)
        {
            if (value == 0)
            {
                AppendLine($"{KlipperCommandTurnLedM1400.Command} S0; Turn LED OFF");
            }
            else
            {
                AppendLineOverrideComment(KlipperCommandTurnLedM1400, $"Exposure for {Math.Round(exposureTime / 1000, 2, MidpointRounding.AwayFromZero)}s", value, exposureTime);
            }
        }
        else
        {
            AppendLineOverrideComment(CommandTurnLedM106, "Turn LED " + (value == 0 ? "OFF" : "ON"), value);
        }
    }

    public void AppendLightOffM106() => AppendTurnLightM106(0);
    public void AppendLightFullM106() => AppendTurnLightM106(_maxLedPower);

    public void AppendClearImage()
    {
        AppendLine(CommandClearImage);
    }

    public void AppendExposure(float time, ushort pwmValue = 255)
    {
        if (pwmValue <= 0 || time <= 0) return;

        AppendTurnLightM106(pwmValue, time);
        if (!KlipperCommandTurnLedM1400.Enabled)
        {
            AppendWaitG4(time, "Cure time/delay");
            AppendLightOffM106();
        }
        if ((_gCodeClearImagePosition & GCodeClearImagePositions.Layer) != 0) AppendClearImage();
    }

    public void AppendPrintProgressM73(float percent, float minutes)
    {
        AppendLine(CommandSetPrintProgressM73, percent, minutes);
    }

    public void AppendShowImageM6054(string filename)
    {
        AppendLine(CommandShowImageM6054, filename);
    }

    public void AppendShowImageM6054(uint layerIndex)
    {
        AppendLine(CommandShowImageM6054, layerIndex);
    }

    public string GetShowImageString(uint layerIndex) => _gCodeShowImageType switch
    {
        GCodeShowImageTypes.FilenamePng0Started => $"{layerIndex}.png",
        GCodeShowImageTypes.FilenamePng1Started => $"{layerIndex + 1}.png",
        GCodeShowImageTypes.LayerIndex0Started => $"{layerIndex}",
        GCodeShowImageTypes.LayerIndex1Started => $"{layerIndex + 1}",
        _ => throw new InvalidExpressionException($"Unhandled image type for {_gCodeShowImageType}")
    };

    public string GetShowImageString(string value) => _gCodeShowImageType switch
    {
        GCodeShowImageTypes.FilenamePng0Started => $"{value}.png",
        GCodeShowImageTypes.FilenamePng1Started => $"{value}.png",
        GCodeShowImageTypes.LayerIndex0Started => $"{value}",
        GCodeShowImageTypes.LayerIndex1Started => $"{value}",
        _ => throw new InvalidExpressionException($"Unhandled image type for {_gCodeShowImageType}")
    };

    public void RebuildGCode(FileFormat slicerFile, StringBuilder? header) => RebuildGCode(slicerFile, header?.ToString());
    public void RebuildGCode(FileFormat slicerFile, string? header = null)
    {
        Clear();

        OnBeforeRebuildGCode();
        
        AppendUVtools();

        if (_encodeThumbnails && slicerFile.ThumbnailsCount > 0)
        {
            AppendLine();
            
            for (int i = 0; i < slicerFile.ThumbnailsCount; i++)
            {
                var thumbnail = slicerFile.Thumbnails[i];
                if (thumbnail.IsEmpty) continue;
                var pngBytes = thumbnail.GetPngByes();
                var base64Thumbnail = Convert.ToBase64String(pngBytes);
                var chunks = base64Thumbnail.Chunk(78);

                AppendLine();
                AppendLine(";");
                AppendLine($"; thumbnail begin {thumbnail.Width}x{thumbnail.Height} {base64Thumbnail.Length}");
                foreach (var chunk in chunks)
                {
                    AppendLine($"; {new string(chunk)}");
                }
                AppendLine("; thumbnail end");
                AppendLine(";");
            }

            
            AppendLine();
            AppendLine();
        }

        if (!slicerFile.HaveLayers) return;

        if (!string.IsNullOrWhiteSpace(header))
        {
            Append(header);
            AppendLine();
        }

        AppendStartGCode(slicerFile);

        float lastZPosition = 0;

        // Defaults for: Absolute, mm/min and s
        for (uint layerIndex = 0; layerIndex < slicerFile.LayerCount; layerIndex++)
        {
            var layer = slicerFile[layerIndex];

            float waitBeforeCure = ConvertFromSeconds(layer.WaitTimeBeforeCure);
            float exposureTime = ConvertFromSeconds(layer.ExposureTime);
            float waitAfterCure = ConvertFromSeconds(layer.WaitTimeAfterCure);
            float liftHeight = layer.LiftHeight;
            //float liftZPos = Layer.RoundHeight(liftHeight + layer.PositionZ);
            //float liftZPosAbs = liftZPos;
            float liftSpeed = ConvertFromMillimetersPerMinute(layer.LiftSpeed);
            float liftAcceleration = layer.LiftAcceleration;
            float liftHeight2 = layer.LiftHeight2;
            float liftSpeed2 = ConvertFromMillimetersPerMinute(layer.LiftSpeed2);
            float liftAcceleration2 = layer.LiftAcceleration2;
            float waitAfterLift = ConvertFromSeconds(layer.WaitTimeAfterLift);
            float retractHeight = layer.RetractHeight;
            float retractSpeed = ConvertFromMillimetersPerMinute(layer.RetractSpeed);
            float retractAcceleration = layer.RetractAcceleration;
            float retractHeight2 = layer.RetractHeight2;
            float retractSpeed2 = ConvertFromMillimetersPerMinute(layer.RetractSpeed2);
            float retractAcceleration2 = layer.RetractAcceleration2;
            float liftHeightTotal = layer.LiftHeightTotal;
            ushort pwmValue = layer.LightPWM;
            if (_maxLedPower != byte.MaxValue)
            {
                pwmValue = (ushort)(_maxLedPower * pwmValue / byte.MaxValue);
            }

            var lifts = new List<(float z, float feedrate, float acceleration)>();
            var retracts = new List<(float z, float feedrate, float acceleration)>();

            switch (GCodePositioningType)
            {
                case GCodePositioningTypes.Absolute:
                    var absLiftPos = Layer.RoundHeight(liftHeight + layer.PositionZ);
                    var absLiftPos2 = Layer.RoundHeight(absLiftPos + liftHeight2);
                    var absRetractPos = Layer.RoundHeight(absLiftPos2 - retractHeight);
                    if (liftHeight > 0) lifts.Add((absLiftPos, liftSpeed, liftAcceleration));
                    if (liftHeight2 > 0) lifts.Add((absLiftPos2, liftSpeed2, liftAcceleration2));
                    if (retractHeight > 0 && absRetractPos >= layer.PositionZ) retracts.Add((absRetractPos, retractSpeed, retractAcceleration));
                    if (retractHeight2 > 0 && absRetractPos > layer.PositionZ) retracts.Add((layer.PositionZ, retractSpeed2, retractAcceleration2));
                    break;
                case GCodePositioningTypes.Relative:
                    var partialLiftPos = Layer.RoundHeight(layer.PositionZ - lastZPosition + liftHeight);
                    if (liftHeight > 0)
                    {
                        lifts.Add((partialLiftPos, liftSpeed, liftAcceleration));
                        if (liftHeight2 > 0) lifts.Add((liftHeight2, liftSpeed2, liftAcceleration2));
                    }
                    else
                    {
                        if (liftHeight2 > 0) lifts.Add((Layer.RoundHeight(partialLiftPos + liftHeight2), liftSpeed2, liftAcceleration2));
                    }

                    if (Layer.RoundHeight(retractHeight + retractHeight2) != liftHeightTotal) // Fail-safe
                    {
                        retracts.Add((-liftHeightTotal, retractSpeed, retractAcceleration));
                    }
                    else
                    {
                        if (retractHeight > 0) retracts.Add((-retractHeight, retractSpeed, retractAcceleration2));
                        if (retractHeight2 > 0) retracts.Add((-retractHeight2, retractSpeed2, retractAcceleration2));
                    }

                    break;
            }

            AppendLineIfCanComment(BeginLayerComments, layerIndex, layer.PositionZ);
            if (!OnBeforeWriteLayerGCode(layerIndex))
            {
                AppendPrintProgressM73(MathF.Round(layerIndex * 100.0f / slicerFile.LayerCount, 2, MidpointRounding.AwayFromZero), 
                    MathF.Round(slicerFile.Skip((int)layerIndex).Sum(l => l.PrintTime) / 60, 2, MidpointRounding.AwayFromZero));

                AppendLayerPause(layer);
                AppendLayerChangeResinM600(layer);

                //if (layer.CanExpose)
                //{ Dont check this for compability
                if (_gCodeShowImagePosition == GCodeShowImagePositions.FirstLine)
                {
                    AppendShowImageM6054(GetShowImageString(layerIndex));
                }
                //}

                if (liftHeightTotal > 0 && Layer.RoundHeight(liftHeightTotal + layer.PositionZ) > layer.PositionZ)
                {
                    AppendLiftMoveGx(lifts, retracts, waitAfterLift, 0, layer);
                }
                else if (lastZPosition != layer.PositionZ) // Ensure Z is on correct position
                {
                    switch (GCodePositioningType)
                    {
                        case GCodePositioningTypes.Absolute:
                            AppendMoveGx(layer.PositionZ, lastZPosition < layer.PositionZ ? liftSpeed : retractSpeed);
                            break;
                        case GCodePositioningTypes.Relative:
                            AppendMoveGx(Layer.RoundHeight(layer.PositionZ - lastZPosition),
                                lastZPosition < layer.PositionZ ? liftSpeed : retractSpeed);
                            break;
                    }

                }

                AppendWaitG4(waitBeforeCure, "Wait before cure"); // Safer to parse if present
                if (_gCodeShowImagePosition == GCodeShowImagePositions.WhenRequired && layer.ShouldExpose)
                {
                    AppendShowImageM6054(GetShowImageString(layerIndex));
                }

                AppendExposure(exposureTime, pwmValue);
                if (waitAfterCure > 0) AppendWaitG4(waitAfterCure, "Wait after cure");
            }

            OnAfterWriteLayerGCode(layerIndex);
            AppendLineIfCanComment(EndLayerComments, layerIndex, layer.PositionZ);
            AppendLine();

            lastZPosition = layer.PositionZ;
        }

        AppendEndGCode(slicerFile);
        OnAfterRebuildGCode();
    }
    
    public void RebuildGCode(FileFormat slicerFile, object[]? configs, string separator = ":")
    {
        StringBuilder? sb = null;
        if (configs is not null)
        {
            sb = new StringBuilder();
            foreach (var config in configs)
            {
                foreach (var propertyInfo in config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    var displayNameAttribute = propertyInfo.GetCustomAttributes(false).OfType<DisplayNameAttribute>().FirstOrDefault();
                    string name;
                    if (displayNameAttribute is null)
                    {
                        name = propertyInfo.Name;
                        if(name == "Item") continue;
                    }
                    else
                    {
                        name = displayNameAttribute.DisplayName;
                    }
                    sb.AppendLine($";{name}{separator}{propertyInfo.GetValue(config)}");
                }
            }
        }

        RebuildGCode(slicerFile, sb);
    }

    public GCodePositioningTypes ParsePositioningTypeFromGCode(string gcode)
    {
        using var reader = new StringReader(gcode);
        while (reader.ReadLine() is { } line)
        {
            if (line.StartsWith(CommandPositioningAbsoluteG90.Command)) return GCodePositioningTypes.Absolute;
            if (line.StartsWith(CommandPositioningPartialG91.Command)) return GCodePositioningTypes.Relative;
        }

        return _gCodePositioningType;
    }

    public void ParseLayersFromGCode(FileFormat slicerFile, bool rebuildGlobalTable = true) 
        => ParseLayersFromGCode(slicerFile, null, rebuildGlobalTable);

    public void ParseLayersFromGCode(FileFormat slicerFile, string? gcode, bool rebuildGlobalTable = true)
    {
        if (!slicerFile.HaveLayers) return;
            
        if (string.IsNullOrWhiteSpace(gcode))
        {
            gcode = _gcode.ToString();
            if (string.IsNullOrWhiteSpace(gcode)) return;
        }

        var positionType = GCodePositioningTypes.Absolute;

        // test
        /*gcode =
            "G90\r\n" +
            "M6054 \"1.png\";\r\n" +
            "G4 P0;\r\n" +
            "M106 S300;\r\n" +
            "G4 P36000;\r\n" +
            "M106 S0;\r\n" +
            "G4 P5000;\r\n" +
            "\r\n"
            ;*/

        var layerBlock = new GCodeLayer(slicerFile);
        _lastAcceleration = 0;

        //bool parseLayerIndexFromComments = false;

        const string thumbnailBegin = "; thumbnail begin ";
        const string thumbnailEnd = "; thumbnail end";

        using var reader = new StringReader(gcode);
        while (reader.ReadLine()?.Trim() is { } line)
        {
            if (string.IsNullOrEmpty(line)) continue;

            if (line.StartsWith(thumbnailBegin))
            {
                var thumbnailInfo = line[thumbnailBegin.Length..].Split([' ', 'x'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (thumbnailInfo.Length < 3) continue;
                if (!uint.TryParse(thumbnailInfo[0], out var width)) continue;
                if (!uint.TryParse(thumbnailInfo[1], out var height)) continue;
                if (!uint.TryParse(thumbnailInfo[2], out var length)) continue;
                var base64Thumbnail = new StringBuilder();
                while (reader.ReadLine()?.Trim() is { } subLine)
                {
                    if (line[0] != ';') break;
                    if (subLine.StartsWith(thumbnailEnd))
                    {
                        if (base64Thumbnail.Length != length) break;
                        var pngBytes = Convert.FromBase64String(base64Thumbnail.ToString());

                        var mat = new Mat();
                        CvInvoke.Imdecode(pngBytes, ImreadModes.Color, mat);
                        if (mat.IsEmpty) break;
                        slicerFile.Thumbnails.Add(mat);

                        break;
                    }
                    base64Thumbnail.Append(subLine.Trim(';', ' '));
                }
            }
                
            // Search for and switch position type when needed
            if (line.StartsWith(CommandPositioningAbsoluteG90.Command))
            {
                positionType = GCodePositioningTypes.Absolute;
                continue;
            }

            if (line.StartsWith(CommandPositioningPartialG91.Command))
            {
                positionType = GCodePositioningTypes.Relative;
                continue;
            }

            Match match;

            if (line[0] == ';')
            {
                // Can be dangerous!
                match = Regex.Match(line, @"^;\W*(layer\W*|LAYER_START:\s*)([0-9]+)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                if (match is {Success: true, Groups.Count: > 2} && uint.TryParse(match.Groups[2].Value, out var layerIndex))
                {
                    if (layerIndex > slicerFile.LayerCount)
                    {
                        throw new FileLoadException(
                            $"GCode parser detected the layer {layerIndex}, but the file was sliced to {slicerFile.LayerCount} layers.",
                            slicerFile.FileFullPath);
                    }
                    layerBlock.SetLayer(true);
                    layerBlock.LayerIndex = layerIndex;
                    //parseLayerIndexFromComments = true;
                    continue;
                }
            }

            // Display image
            if (line.StartsWith(CommandShowImageM6054.Command))
            {
                match = Regex.Match(line, CommandShowImageM6054.ToStringWithoutComments(GetShowImageString(@"([0-9]+)")), RegexOptions.IgnoreCase);
                if (match is {Success: true, Groups.Count: >= 2}) // Begin new layer
                {
                    var layerIndex = uint.Parse(match.Groups[1].Value);
                    if (_gCodeShowImageType is GCodeShowImageTypes.FilenamePng1Started or GCodeShowImageTypes.LayerIndex1Started) layerIndex--;
                    if (layerIndex > slicerFile.LayerCount)
                    {
                        throw new FileLoadException(
                            $"GCode parser detected the layer {layerIndex}, but the file was sliced to {slicerFile.LayerCount} layers.",
                            slicerFile.FileFullPath);
                    }

                    // Propagate values before switch to the new layer
                    if (/*_gCodeShowImagePosition == GCodeShowImagePositions.FirstLine && */layerBlock.LayerIndex != layerIndex)
                    {
                        layerBlock.SetLayer(true);
                        layerBlock.LayerIndex = layerIndex;
                    }
                }
                continue;
            }

            if (!layerBlock.IsValid) continue; // No layer yet found to work on, skip

            // Check pauses
            if (CommandPauseM25.Enabled)
            {
                if (line.StartsWith(CommandPauseM25.Command))
                {
                    layerBlock.Pause = true;
                    continue;
                }

                bool found = false;
                foreach (var alias in CommandPauseM25.CommandAlias)
                {
                    if (line.StartsWith(alias))
                    {
                        layerBlock.Pause = true;
                        found = true;
                        break;
                    }
                }
                if (found) continue;
            }
            if (CommandChangeResinM600.Enabled && line.StartsWith(CommandChangeResinM600.Command))
            {
                layerBlock.ChangeResin = true;
                continue;
            }

            // Check acceleration
            if (line.StartsWith(CommandSetAccelerationM204.Command))
            {
                match = Regex.Match(line, CommandSetAccelerationM204.ToStringWithoutComments(@"(([0-9]*[.])?[0-9]+)"), RegexOptions.IgnoreCase);
                if (match is { Success: true, Groups.Count: >= 2 })
                {
                    _lastAcceleration = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
                }
                continue;
            }

            // Check moves
            if (line.StartsWith(CommandMoveG0.Command) || line.StartsWith(CommandMoveG1.Command))
            {
                match = Regex.Match(line, $"{CommandMoveG0.ToStringWithoutComments(@"([+-]?([0-9]*[.])?[0-9]+)", @"(([0-9]*[.])?[0-9]+)")}|{CommandMoveG1.ToStringWithoutComments(@"([+-]?([0-9]*[.])?[0-9]+)", @"(([0-9]*[.])?[0-9]+)")}", RegexOptions.IgnoreCase);
                
                if (match is {Success: true, Groups.Count: >= 9} && !layerBlock.RetractSpeed2.HasValue && layerBlock.LightOffCount < 2)
                {
                    var startIndex = match.Groups[1].Value.Length > 0 ? 0 : 4;
                    float pos = float.Parse(match.Groups[startIndex+1].Value, CultureInfo.InvariantCulture);
                    float speed = ConvertToMillimetersPerMinute(float.Parse(match.Groups[startIndex+3].Value, CultureInfo.InvariantCulture));


                    layerBlock.Movements.Add(new(pos, speed, _lastAcceleration));
                    layerBlock.AssignMovements(positionType);

                    /*if (!layerBlock.PositionZ.HasValue) // Lift or pos, set here for now
                    {
                        switch (positionType)
                        {
                            case GCodePositioningTypes.Absolute:
                                layerBlock.PositionZ = pos;
                                break;
                            case GCodePositioningTypes.Partial:
                                layerBlock.PositionZ = Layer.RoundHeight(positionZ + pos);
                                break;
                        }

                        layerBlock.LiftSpeed = speed;
                        continue;
                    }


                    // ~90% sure its retract here, still is possible to bug this with 2 lifts
                    layerBlock.RetractSpeed = speed;

                    switch (positionType)
                    {
                        case GCodePositioningTypes.Absolute:
                            layerBlock.LiftHeight = Layer.RoundHeight(layerBlock.PositionZ.Value - pos);
                            layerBlock.PositionZ = positionZ = pos;
                            break;
                        case GCodePositioningTypes.Partial:
                            layerBlock.LiftHeight = layerBlock.PositionZ - positionZ;
                            layerBlock.PositionZ = positionZ = Layer.RoundHeight(layerBlock.PositionZ.Value + pos);
                            break;
                    }*/
                }
                continue;
            }

            // Check for waits 
            if (line.StartsWith(CommandWaitG4.Command))
            {
                match = Regex.Match(line, CommandWaitG4.ToStringWithoutComments(@"(([0-9]*[.])?[0-9]+)"), RegexOptions.IgnoreCase);
                if (match is {Success: true, Groups.Count: >= 2})
                {
                    if (/*CommandWaitSyncDelay.Enabled && */match.Groups[1].Value.StartsWith('0'))
                    {
                        layerBlock.WaitSyncDelayDetected = true;
                        continue; // Sync movement delay, skip
                    }
                    var waitTime = float.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);

                    if (layerBlock.PositionZ.HasValue &&
                        //layerBlock.LiftHeight.HasValue &&
                        !layerBlock.RetractSpeed.HasValue) // Must be wait time after lift, if not, don't blame me!
                    {
                        layerBlock.WaitTimeAfterLift ??= 0;
                        layerBlock.WaitTimeAfterLift += ConvertToSeconds(waitTime);
                        continue;
                    }

                    if (!layerBlock.LightPWM.HasValue) // Before cure
                    {
                        layerBlock.WaitTimeBeforeCure ??= 0;
                        layerBlock.WaitTimeBeforeCure += ConvertToSeconds(waitTime);
                        continue;
                    }

                    if (layerBlock.IsExposing) // Must be exposure time, if not, don't blame me!
                    {
                        layerBlock.ExposureTime ??= 0;
                        layerBlock.ExposureTime += ConvertToSeconds(waitTime);
                        continue;
                    }

                    if (layerBlock.IsAfterLightOff)
                    {
                        if (!layerBlock.WaitTimeBeforeCure.HasValue) // Novamaker fix, delay on last line, broke logic but safer
                        {
                            layerBlock.WaitTimeBeforeCure ??= 0;
                            layerBlock.WaitTimeBeforeCure += ConvertToSeconds(waitTime);
                        }
                        else
                        {
                            layerBlock.WaitTimeAfterCure ??= 0;
                            layerBlock.WaitTimeAfterCure += ConvertToSeconds(waitTime);
                        }

                        continue;
                    }
                }
                continue;
            }

            // Check LightPWM Off
            if (CommandTurnLedOff.Enabled)
            {
                if (line.StartsWith(CommandTurnLedOff.Command) && 
                    (string.IsNullOrWhiteSpace(CommandTurnLedOff.Arguments) || line.Contains(CommandTurnLedOff.Arguments)))
                {
                    if (layerBlock.LightPWM.HasValue)
                    {
                        layerBlock.LightOffCount++;
                    }

                    continue;
                }
            }

            // Check LightPWM
            if (KlipperCommandTurnLedM1400.Enabled)
            {
                if (line.StartsWith(KlipperCommandTurnLedM1400.Command))
                {
                    // Remove from ; to end of line
                    var commaIndex = line.IndexOf(';');
                    if (commaIndex > 0)
                    {
                        line = line[..commaIndex].TrimEnd();
                    }

                    byte pwm = byte.MaxValue;
                    uint waitTimeMs = 0;

                    var lineSplit = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 1; i < lineSplit.Length; i++)
                    {
                        if (lineSplit[i].StartsWith("S"))
                        {
                            if(!float.TryParse(lineSplit[i][1..], CultureInfo.InvariantCulture, out var pwmValue)) continue;
                            pwm = _maxLedPower == byte.MaxValue
                                ? (byte)pwmValue
                                : (byte)(pwmValue * byte.MaxValue / _maxLedPower);
                        }
                        else if (lineSplit[i].StartsWith("P"))
                        {
                            uint.TryParse(lineSplit[i][1..], out waitTimeMs);
                        }
                    }

                    if (pwm == 0 && layerBlock.LightPWM.HasValue)
                    {
                        layerBlock.LightOffCount++;
                    }
                    else if (!layerBlock.IsAfterLightOff)
                    {
                        layerBlock.LightPWM = pwm;
                    }

                    if (pwm > 0 && waitTimeMs > 0)
                    {
                        // Exposure time
                        layerBlock.ExposureTime ??= 0;
                        layerBlock.ExposureTime += ConvertToSeconds(waitTimeMs);
                        layerBlock.LightOffCount++;
                    }

                    continue;
                }
            }
            else if (CommandTurnLedM106.Enabled && line.StartsWith(CommandTurnLedM106.Command))
            {
                match = Regex.Match(line, CommandTurnLedM106.ToStringWithoutComments(@"([0-9]+)"), RegexOptions.IgnoreCase);
                if (match is {Success: true, Groups.Count: >= 2})
                {
                    byte pwm;
                    if (_maxLedPower == byte.MaxValue)
                    {
                        pwm = byte.Parse(match.Groups[1].Value);
                    }
                    else
                    {
                        ushort pwmValue = ushort.Parse(match.Groups[1].Value);
                        pwm = (byte) (pwmValue * byte.MaxValue / _maxLedPower);
                    }

                    if (pwm == 0 && layerBlock.LightPWM.HasValue)
                    {
                        layerBlock.LightOffCount++;
                    }
                    else if (!layerBlock.IsAfterLightOff)
                    {
                        layerBlock.LightPWM = pwm;
                    }
                }
                continue;
            }
        }

        // Propagate values of left over layer
        layerBlock.SetLayer();

        if (rebuildGlobalTable)
        {
            slicerFile.UpdateGlobalPropertiesFromLayers();
        }
    }

    public StringReader GetStringReader()
    {
        return new(_gcode.ToString());
    }

    /// <summary>
    /// Converts seconds to current gcode norm
    /// </summary>
    /// <param name="seconds"></param>
    /// <returns></returns>
    public float ConvertFromSeconds(float seconds)
    {
        return _gCodeTimeUnit switch
        {
            GCodeTimeUnits.Seconds => seconds,
            GCodeTimeUnits.Milliseconds => TimeConverter.SecondsToMilliseconds(seconds),
            _ => throw new InvalidExpressionException($"Unhandled time unit for {_gCodeTimeUnit}")
        };
    }

    /// <summary>
    /// Converts speed in mm/min to current gcode norm
    /// </summary>
    /// <param name="mmMin">Millimeters per minute</param>
    /// <returns></returns>
    public float ConvertFromMillimetersPerMinute(float mmMin)
    {
        return _gCodeSpeedUnit switch
        {
            GCodeSpeedUnits.MillimetersPerMinute => mmMin,
            GCodeSpeedUnits.MillimetersPerSecond => MathF.Round(mmMin / 60, 2),
            GCodeSpeedUnits.CentimetersPerMinute => MathF.Round(mmMin / 10, 2),
            _ => throw new InvalidExpressionException($"Unhandled speed unit for {_gCodeSpeedUnit}")
        };
    }

    /// <summary>
    /// Converts time from current gcode norm in <see cref="GCodeTimeUnit"/> to s
    /// </summary>
    /// <param name="time">Time in <see cref="GCodeTimeUnit"/></param>
    /// <returns></returns>
    public float ConvertToSeconds(float time)
    {
        return _gCodeTimeUnit switch
        {
            GCodeTimeUnits.Seconds => time,
            GCodeTimeUnits.Milliseconds => TimeConverter.MillisecondsToSeconds(time),
            _ => throw new InvalidExpressionException($"Unhandled time unit for {_gCodeTimeUnit}")
        };
    }

    /// <summary>
    /// Converts speed from current gcode norm in <see cref="GCodeSpeedUnit"/> to mm/min
    /// </summary>
    /// <param name="speed">Speed in <see cref="GCodeSpeedUnit"/></param>
    /// <returns></returns>
    public float ConvertToMillimetersPerMinute(float speed)
    {
        return _gCodeSpeedUnit switch
        {
            GCodeSpeedUnits.MillimetersPerMinute => speed,
            GCodeSpeedUnits.MillimetersPerSecond => MathF.Round(speed * 60, 2),
            GCodeSpeedUnits.CentimetersPerMinute => MathF.Round(speed * 10, 2),
            _ => throw new InvalidExpressionException($"Unhandled speed unit for {_gCodeSpeedUnit}")
        };
    }
}
#endregion