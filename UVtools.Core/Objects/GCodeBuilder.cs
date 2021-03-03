/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Text;

namespace UVtools.Core.Objects
{
    public class GCodeBuilder : BindableBase
    {
        #region Enums

        public enum GCodeTimeUnits : byte
        {
            Milliseconds,
            Seconds
        }

        public enum GCodeSpeedUnits : byte
        {
            MillimetersPerSecond,
            MillimetersPerMinute,
            CentimetersPerMinute,
        }
        #endregion

        #region Members
        private readonly StringBuilder _gcode = new();

        private bool _appendComma = true;
        private bool _appendComment = true;
        private bool _useAbsoluteCommands = true;
        private GCodeTimeUnits _gCodeTimeUnit = GCodeTimeUnits.Milliseconds;
        private GCodeSpeedUnits _gCodeSpeedUnit = GCodeSpeedUnits.MillimetersPerMinute;

        #endregion

        #region Properties

        public bool UseAbsoluteCommands
        {
            get => _useAbsoluteCommands;
            set => RaiseAndSetIfChanged(ref _useAbsoluteCommands, value);
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

        public bool AppendComma
        {
            get => _appendComma;
            set => RaiseAndSetIfChanged(ref _appendComma, value);
        }

        public bool AppendComment
        {
            get => _appendComment;
            set => RaiseAndSetIfChanged(ref _appendComment, value);
        }
        #endregion

        #region StringBuilder
        public void AppendLine() => _gcode.AppendLine();
        public void AppendLine(string line) => _gcode.AppendLine(line);
        public void AppendFormat(string format, params object?[] args) => _gcode.AppendFormat(format, args);
        public void Clear() => _gcode.Clear();
        public override string ToString() => _gcode.ToString();
        #endregion

        #region Methods

        public string FormatGCodeLine(string line, string comment)
        {
            if (_appendComma || _appendComment)
                line += ';';
            if (_appendComment)
                line += comment;

            return line;
        }

        public void AppendUnitsMmG21()
        {
            AppendLine(FormatGCodeLine("G21", "Set units to be mm"));
        }

        public void AppendPositioningType()
        {
            if (_useAbsoluteCommands)
            {
                AppendLine(FormatGCodeLine("G90", "Absolute positioning"));
            }
            else
            {
                AppendLine(FormatGCodeLine("G91", "Partial positioning"));
            }
        }

        public void AppendEnableMotors()
        {
            AppendLine(FormatGCodeLine("M17", "Enable motors"));
        }

        public void AppendDisableMotors()
        {
            AppendLine(FormatGCodeLine("M18", "Disable motors"));
        }

        public void AppendTurnMotors(bool enable)
        {
            if (enable)
                AppendEnableMotors();
            else 
                AppendDisableMotors();
        }

        public void AppendHomeZG28()
        {
            AppendLine(FormatGCodeLine("G28 Z0", "Home Z"));
        }


        public void AppendMoveG0(float z, float feedRate)
        {
            AppendLine(FormatGCodeLine($"G0 Z{z} F{feedRate}", "Z Move"));
        }

        public void AppendLiftMoveG0(float upZ, float upFeedRate, float downZ, float downFeedRate)
        {
            AppendLine(FormatGCodeLine($"G0 Z{upZ} F{upFeedRate}", "Z Lift"));
            AppendLine(FormatGCodeLine($"G0 Z{downZ} F{downFeedRate}", "Retract to layer height"));
        }

        public void AppendWaitG4(float time)
        {
            AppendLine(FormatGCodeLine($"G4 P{time}", "Delay"));
        }

        public void AppendTurnLightM106(ushort value)
        {
            AppendLine(FormatGCodeLine($"M106 S{value}", "Turn LED"));
        }

        public void AppendLightOffM106() => AppendTurnLightM106(0);
        public void AppendLightFullM106() => AppendTurnLightM106(255);

        public void AppendExposure(ushort value, float time)
        {
            if (time <= 0) return;

            AppendTurnLightM106(value);
            AppendWaitG4(time);
            AppendLightOffM106();
        }

        public void AppendShowImageM6054(string name)
        {
            AppendLine(FormatGCodeLine($"M6054 \"{name}\"","Show image"));
        }

        public void AppendLineComment(string comment)
        {
            AppendLine($";{comment}");
        }
    }
    #endregion
}
