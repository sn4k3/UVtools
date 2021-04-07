/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.GCode
{
    public class GCodeBuilder : BindableBase
    {
        #region Commands

        public GCodeCommand CommandUnitsMillimetersG21 { get; } = new("G21", null, "Set units to be mm");
        public GCodeCommand CommandPositioningAbsoluteG90 { get; } = new("G90", null, "Absolute positioning");
        public GCodeCommand CommandPositioningPartialG91 { get; } = new("G91", null, "Partial positioning");

        public GCodeCommand CommandTurnMotorsOnM17 { get; } = new("M17", null, "Enable motors");
        public GCodeCommand CommandTurnMotorsOffM18 { get; } = new("M18", null, "Disable motors");

        public GCodeCommand CommandHomeG28 { get; } = new("G28", "Z0", "Home Z");

        public GCodeCommand CommandMoveG0 { get; } = new("G0", "Z{0} F{1}", "Move Z");
        public GCodeCommand CommandMoveG1 { get; } = new("G1", "Z{0} F{1}", "Move Z");

        public GCodeCommand CommandWaitG4 { get; } = new("G4", "P{0}", "Delay");
        public GCodeCommand CommandShowImageM6054 = new("M6054", "\"{0}\"", "Show image");
        public GCodeCommand CommandClearImage = new(";<Slice> Blank"); // Clear image
        public GCodeCommand CommandTurnLEDM106 { get; } = new("M106", "S{0}", "Turn LED");
        #endregion

        #region Enums

        public enum GCodePositioningTypes : byte
        {
            Absolute,
            Partial
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

        public enum GCodeShowImageTypes : byte
        {
            FilenameZeroPNG,
            FilenameNonZeroPNG,
            LayerIndexZero,
            LayerIndexNonZero,
        }
        #endregion

        #region Members
        private readonly StringBuilder _gcode = new();

        private bool _useTailComma = true;
        private bool _useComments = true;
        private GCodePositioningTypes _gCodePositioningType = GCodePositioningTypes.Absolute;
        private GCodeTimeUnits _gCodeTimeUnit = GCodeTimeUnits.Milliseconds;
        private GCodeSpeedUnits _gCodeSpeedUnit = GCodeSpeedUnits.MillimetersPerMinute;
        private GCodeShowImageTypes _gCodeShowImageType = GCodeShowImageTypes.FilenameNonZeroPNG;
        private ushort _maxLedPower = byte.MaxValue;
        private uint _lineCount;

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

        public string BeginStartGCodeComments { get; set; } = ";START_GCODE_BEGIN";
        public string EndStartGCodeComments { get; set; } = ";END_GCODE_BEGIN";

        public string BeginLayerComments { get; set; } = ";LAYER_START:{0}" + Environment.NewLine +
                                                         ";PositionZ:{1}mm";

        public string EndLayerComments { get; set; } = ";LAYER_END";

        public string BeginEndGCodeComments { get; set; } = ";START_GCODE_END";
        public string EndEndGCodeComments { get; set; } = ";END_GCODE_END" + Environment.NewLine +
                                                          ";<Completed>";

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

        public void AppendLineOverrideComment(GCodeCommand command, string comment, params object[] args)
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
        }

        public override string ToString() => _gcode.ToString();
        #endregion

        #region Methods

        public string FormatGCodeLine(string line, string comment = null)
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
            string arch = Environment.Is64BitOperatingSystem ? "64-bits" : "32-bits";
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            AppendComment($"Generated by {About.Software} v{version.ToString(3)} {arch} @ {DateTime.Now}");
        }

        public void AppendStartGCode()
        {
            AppendLineIfCanComment(BeginStartGCodeComments);
            AppendUnitsMmG21();
            AppendPositioningType();
            AppendLightOffM106();
            AppendEnableMotors();
            AppendClearImage();
            AppendHomeZG28();
            AppendLineIfCanComment(EndStartGCodeComments);
            AppendLine();
        }

        public void AppendEndGCode(float raiseZ = 0, float feedRate = 0)
        {
            AppendLineIfCanComment(BeginEndGCodeComments);
            AppendLightOffM106();
            if (raiseZ > 0)
            {
                AppendMoveG0(raiseZ, feedRate);
            }

            AppendDisableMotors();
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
                case GCodePositioningTypes.Partial:
                    AppendLine(CommandPositioningPartialG91);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void AppendEnableMotors()
        {
            AppendLine(CommandTurnMotorsOnM17);
        }

        public void AppendDisableMotors()
        {
            AppendLine(CommandTurnMotorsOffM18);
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
            AppendLine(CommandHomeG28);
        }


        public void AppendMoveG0(float z, float feedRate)
        {
            if(z == 0 || feedRate <= 0) return;
            AppendLine(CommandMoveG0, z, feedRate);
        }

        public void AppendLiftMoveG0(float upZ, float upFeedRate, float downZ, float downFeedRate, float stabilizationDelay = 0)
        {
            if (upZ == 0 || upFeedRate <= 0) return;
            AppendLineOverrideComment(CommandMoveG0, "Z Lift", upZ, upFeedRate); // Z Lift
            if (downZ != 0 && downFeedRate > 0)
            {
                AppendLineOverrideComment(CommandMoveG0, "Retract to layer height", downZ, downFeedRate);
            }

            if (stabilizationDelay > 0)
            {
                AppendWaitG4(stabilizationDelay);
            }
        }

        public void AppendMoveG1(float z, float feedRate)
        {
            if (z == 0 || feedRate <= 0) return;
            AppendLine(CommandMoveG1, z, feedRate);
        }

        public void AppendLiftMoveG1(float upZ, float upFeedRate, float downZ, float downFeedRate, float stabilizationDelay = 0)
        {
            if (upZ == 0 || upFeedRate <= 0) return;
            AppendLineOverrideComment(CommandMoveG1, "Lift Z", upZ, upFeedRate); // Z Lift
            if (downZ != 0 && downFeedRate > 0)
            {
                AppendLineOverrideComment(CommandMoveG1, "Retract to layer height", downZ, downFeedRate);
            }

            if (stabilizationDelay > 0)
            {
                AppendWaitG4(stabilizationDelay);
            }
        }

        public void AppendWaitG4(float time, string comment = null)
        {
            if (time < 0) return;
            AppendLineOverrideComment(CommandWaitG4, comment, time);
        }

        public void AppendTurnLightM106(ushort value)
        {
            AppendLineOverrideComment(CommandTurnLEDM106, "Turn LED " + (value == 0 ? "OFF" : "ON"), value);
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

            AppendTurnLightM106(pwmValue);
            AppendWaitG4(time, "Cure time/delay");
            AppendLightOffM106();
            AppendClearImage();
        }

        public void AppendShowImageM6054(string filename)
        {
            AppendLine(CommandShowImageM6054, filename);
        }

        public void AppendShowImageM6054(uint layerIndex)
        {
            AppendLine(CommandShowImageM6054, layerIndex);
        }

        /*public void AppendLayer(Layer layer)
        {

        }*/

        public string GetShowImageString(uint layerIndex) => _gCodeShowImageType switch
        {
            GCodeShowImageTypes.FilenameZeroPNG => $"{layerIndex}.png",
            GCodeShowImageTypes.FilenameNonZeroPNG => $"{layerIndex + 1}.png",
            GCodeShowImageTypes.LayerIndexZero => $"{layerIndex}",
            GCodeShowImageTypes.LayerIndexNonZero => $"{layerIndex + 1}",
            _ => throw new ArgumentOutOfRangeException()
        };

        public void RebuildGCode(FileFormat slicerFile, StringBuilder header) => RebuildGCode(slicerFile, header?.ToString());
        public void RebuildGCode(FileFormat slicerFile, string header = null)
        {
            Clear();
            AppendUVtools();

            if (slicerFile.LayerCount == 0) return;

            if (!string.IsNullOrWhiteSpace(header))
            {
                Append(header);
                AppendLine();
            }

            AppendStartGCode();

            float lastZPosition = 0;

            // Defaults for: Absolute, mm/m and s
            for (uint layerIndex = 0; layerIndex < slicerFile.LayerCount; layerIndex++)
            {
                var layer = slicerFile[layerIndex];
                float exposureTime = layer.ExposureTime;
                float liftHeight = layer.LiftHeight;
                float liftZPos = Layer.RoundHeight(liftHeight + layer.PositionZ);
                float liftZPosAbs = liftZPos;
                float liftSpeed = layer.LiftSpeed;
                float retractPos = layer.PositionZ;
                float retractSpeed = layer.RetractSpeed;
                float lightOffDelay = layer.LightOffDelay;
                ushort pwmValue = layer.LightPWM;
                if (_maxLedPower != byte.MaxValue)
                {
                    pwmValue = (ushort)(_maxLedPower * pwmValue / byte.MaxValue);
                }

                switch (GCodePositioningType)
                {
                    case GCodePositioningTypes.Partial:
                        liftZPos = liftHeight;
                        retractPos = Layer.RoundHeight(layer.PositionZ - lastZPosition - liftHeight);
                        break;
                }

                switch (GCodeTimeUnit)
                {
                    case GCodeTimeUnits.Milliseconds:
                        exposureTime *= 1000;
                        lightOffDelay *= 1000;
                        break;
                }

                switch (GCodeSpeedUnit)
                {
                    case GCodeSpeedUnits.MillimetersPerSecond:
                        liftSpeed = (float)Math.Round(liftSpeed / 60, 2);
                        retractSpeed = (float)Math.Round(retractSpeed / 60, 2);
                        break;
                    case GCodeSpeedUnits.CentimetersPerMinute:
                        liftSpeed = (float)Math.Round(liftSpeed / 10, 2);
                        retractSpeed = (float)Math.Round(retractSpeed / 10);
                        break;
                }

                AppendLineIfCanComment(BeginLayerComments, layerIndex, layer.PositionZ);

                if (layer.CanExpose)
                {
                    AppendShowImageM6054(GetShowImageString(layerIndex));
                }

                if (liftHeight > 0 && liftZPosAbs > layer.PositionZ)
                {
                    AppendLiftMoveG0(liftZPos, liftSpeed, retractPos, retractSpeed);
                }
                else if (lastZPosition < layer.PositionZ) // Ensure Z is on correct position
                {
                    switch (GCodePositioningType)
                    {
                        case GCodePositioningTypes.Absolute:
                            AppendMoveG0(layer.PositionZ, liftSpeed);
                            break;
                        case GCodePositioningTypes.Partial:
                            AppendMoveG0(Layer.RoundHeight(layer.PositionZ - lastZPosition), liftSpeed);
                            break;
                    }

                }

                AppendWaitG4(lightOffDelay, "Stabilization delay");
                AppendExposure(exposureTime, pwmValue);

                AppendLineIfCanComment(EndLayerComments, layerIndex, layer.PositionZ);
                AppendLine();

                lastZPosition = layer.PositionZ;
            }

            float finalRaiseZPosition = slicerFile.MaxPrintHeight;
            switch (GCodePositioningType)
            {

                case GCodePositioningTypes.Partial:
                    finalRaiseZPosition = Layer.RoundHeight(finalRaiseZPosition - lastZPosition);
                    break;
            }

            AppendEndGCode(finalRaiseZPosition, slicerFile.LiftSpeed);
        }

        public void RebuildGCode(FileFormat slicerFile, object[] configs, string separator = ":")
        {
            StringBuilder sb = null;
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
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (line.StartsWith(CommandPositioningAbsoluteG90.Command)) return GCodePositioningTypes.Absolute;
                if (line.StartsWith(CommandPositioningPartialG91.Command)) return GCodePositioningTypes.Partial;
            }

            return _gCodePositioningType;
        }

        public void ParseLayersFromGCode(FileFormat slicerFile, bool rebuildGlobalTable = true) =>
            ParseLayersFromGCode(slicerFile, null, rebuildGlobalTable);

        public void ParseLayersFromGCode(FileFormat slicerFile, string gcode, bool rebuildGlobalTable = true)
        {
            if (slicerFile.LayerCount == 0) return;
            
            if (string.IsNullOrWhiteSpace(gcode))
            {
                gcode = _gcode.ToString();
                if (string.IsNullOrWhiteSpace(gcode)) return;
            }

            var positionType = ParsePositioningTypeFromGCode(gcode);


            float positionZ = 0;
            for (uint layerIndex = 0; layerIndex < slicerFile.LayerCount; layerIndex++)
            {
                var layer = slicerFile[layerIndex];
                if(layer is null) continue;
                var startStr = CommandShowImageM6054.ToStringWithoutComments(GetShowImageString(layerIndex));
                var endStr = CommandShowImageM6054.ToStringWithoutComments(GetShowImageString(layerIndex+1));
                gcode = gcode.Substring(gcode.IndexOf(startStr, StringComparison.InvariantCultureIgnoreCase) + startStr.Length + 1);
                var endStrIndex = gcode.IndexOf(endStr, StringComparison.Ordinal);
                var stripGcode = endStrIndex > 0 ? gcode.Substring(0, endStrIndex) : gcode;/*.Trim(' ', '\n', '\r', '\t');*/

                float liftHeight = 0;// this allow read back no lifts slicerFile.GetInitialLayerValueOrNormal(layerIndex, slicerFile.BottomLiftHeight, slicerFile.LiftHeight);
                float liftSpeed = slicerFile.GetInitialLayerValueOrNormal(layerIndex, slicerFile.BottomLiftSpeed, slicerFile.LiftSpeed);
                float retractSpeed = slicerFile.RetractSpeed;
                float lightOffDelay = 0;
                byte pwm = slicerFile.GetInitialLayerValueOrNormal(layerIndex, slicerFile.BottomLightPWM, slicerFile.LightPWM);
                float exposureTime = slicerFile.GetInitialLayerValueOrNormal(layerIndex, slicerFile.BottomExposureTime, slicerFile.ExposureTime);
                var moveG0Regex = Regex.Match(stripGcode, CommandMoveG0.ToStringWithoutComments(@"([+-]?([0-9]*[.])?[0-9]+)", @"(\d+)"), RegexOptions.IgnoreCase);
                var moveG1Regex = Regex.Match(stripGcode, CommandMoveG1.ToStringWithoutComments(@"([+-]?([0-9]*[.])?[0-9]+)", @"(\d+)"), RegexOptions.IgnoreCase);
                var waitG4Regex = Regex.Match(stripGcode, CommandWaitG4.ToStringWithoutComments(@"(\d+)"), RegexOptions.IgnoreCase);
                var pwmM106Regex = Regex.Match(stripGcode, CommandTurnLEDM106.ToStringWithoutComments(@"(\d+)"), RegexOptions.IgnoreCase);
                var moveRegex = moveG0Regex.Success ? moveG0Regex : moveG1Regex;

                if (moveRegex.Success)
                {
                    float liftPosTemp = float.Parse(moveRegex.Groups[1].Value, CultureInfo.InvariantCulture);
                    float liftSpeedTemp = float.Parse(moveRegex.Groups[3].Value, CultureInfo.InvariantCulture);

                    switch (GCodeSpeedUnit)
                    {
                        case GCodeSpeedUnits.MillimetersPerSecond:
                            liftSpeedTemp = (float) Math.Round(liftSpeedTemp / 60, 2);
                            break;
                        case GCodeSpeedUnits.MillimetersPerMinute:
                            break;
                        case GCodeSpeedUnits.CentimetersPerMinute:
                            liftSpeedTemp *= 10;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    moveRegex = moveRegex.NextMatch();
                    if (moveRegex.Success)
                    {
                        float retractPos = float.Parse(moveRegex.Groups[1].Value, CultureInfo.InvariantCulture);
                        retractSpeed = float.Parse(moveRegex.Groups[3].Value, CultureInfo.InvariantCulture);
                        liftSpeed = liftSpeedTemp;

                        switch (positionType)
                        {
                            case GCodePositioningTypes.Absolute:
                                liftHeight = Layer.RoundHeight(liftPosTemp - retractPos);
                                positionZ = retractPos;
                                break;
                            case GCodePositioningTypes.Partial:
                                liftHeight = liftPosTemp;
                                positionZ = Layer.RoundHeight(positionZ + liftPosTemp + retractPos);
                                break;
                        }

                        switch (GCodeSpeedUnit)
                        {
                            case GCodeSpeedUnits.MillimetersPerSecond:
                                retractSpeed = (float)Math.Round(retractSpeed / 60, 2);
                                break;
                            case GCodeSpeedUnits.MillimetersPerMinute:
                                break;
                            case GCodeSpeedUnits.CentimetersPerMinute:
                                retractSpeed *= 10;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }
                    }
                    else
                    {
                        switch (positionType)
                        {
                            case GCodePositioningTypes.Absolute:
                                positionZ = liftPosTemp;
                                break;
                            case GCodePositioningTypes.Partial:
                                positionZ = Layer.RoundHeight(positionZ + liftPosTemp);
                                break;
                        }
                        
                    }
                }

                if (pwmM106Regex.Success)
                {
                    if (_maxLedPower == byte.MaxValue)
                    {
                        pwm = byte.Parse(pwmM106Regex.Groups[1].Value);
                    }
                    else
                    {
                        ushort pwmValue = ushort.Parse(pwmM106Regex.Groups[1].Value);
                        pwm = (byte)(pwmValue * byte.MaxValue / _maxLedPower);
                    }
                }

                if (waitG4Regex.Success)
                {
                    lightOffDelay = float.Parse(waitG4Regex.Groups[1].Value, CultureInfo.InvariantCulture);
                    switch (GCodeTimeUnit)
                    {
                        case GCodeTimeUnits.Milliseconds:
                            lightOffDelay = TimeExtensions.MillisecondsToSeconds(lightOffDelay);
                            break;
                    }
                    
                    waitG4Regex = waitG4Regex.NextMatch();
                    if (waitG4Regex.Success)
                    {
                        exposureTime = float.Parse(waitG4Regex.Groups[1].Value, CultureInfo.InvariantCulture);
                        switch (GCodeTimeUnit)
                        {
                            case GCodeTimeUnits.Milliseconds:
                                exposureTime = TimeExtensions.MillisecondsToSeconds(exposureTime);
                                break;
                        }
                    }
                    else // Only one match, meaning light off delay is not present
                    {
                        lightOffDelay = slicerFile.GetInitialLayerValueOrNormal(layerIndex, slicerFile.BottomLightOffDelay, slicerFile.LightOffDelay);
                    }
                }

                layer.PositionZ = positionZ;
                layer.ExposureTime = exposureTime;
                layer.LiftHeight = liftHeight;
                layer.LiftSpeed = liftSpeed;
                layer.RetractSpeed = retractSpeed;
                layer.LightOffDelay = lightOffDelay;
                layer.LightPWM = pwm;
            }

            if (rebuildGlobalTable)
            {
                slicerFile.SuppressRebuildPropertiesWork(() =>
                {
                    var bottomLayer = slicerFile[0];
                    if (bottomLayer is not null)
                    {
                        if (bottomLayer.ExposureTime > 0) slicerFile.BottomExposureTime = bottomLayer.ExposureTime;
                        if (bottomLayer.LiftHeight > 0) slicerFile.BottomLiftHeight = bottomLayer.LiftHeight;
                        if (bottomLayer.LiftSpeed > 0) slicerFile.BottomLiftSpeed = bottomLayer.LiftSpeed;
                        if (bottomLayer.RetractSpeed > 0) slicerFile.RetractSpeed = bottomLayer.RetractSpeed;
                        if (bottomLayer.LightOffDelay > 0) slicerFile.BottomLightOffDelay = bottomLayer.LightOffDelay;
                        if (bottomLayer.LightPWM > 0) slicerFile.BottomLightPWM = bottomLayer.LightPWM;
                    }

                    var normalLayer = slicerFile[slicerFile.LastLayerIndex];
                    if (normalLayer is not null)
                    {
                        if (normalLayer.ExposureTime > 0) slicerFile.ExposureTime = normalLayer.ExposureTime;
                        if (normalLayer.LiftHeight > 0) slicerFile.LiftHeight = normalLayer.LiftHeight;
                        if (normalLayer.LiftSpeed > 0) slicerFile.LiftSpeed = normalLayer.LiftSpeed;
                        if (normalLayer.RetractSpeed > 0) slicerFile.RetractSpeed = normalLayer.RetractSpeed;
                        if (normalLayer.LightOffDelay > 0) slicerFile.LightOffDelay = normalLayer.LightOffDelay;
                        if (normalLayer.LightPWM > 0) slicerFile.LightPWM = normalLayer.LightPWM;
                    }
                });
            }
        }

        public StringReader GetStringReader()
        {
            return new(_gcode.ToString());
        }
    }
    #endregion
}
