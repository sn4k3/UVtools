/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.Scripting;
using System.IO;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;

namespace UVtools.ScriptSample
{
    /// <summary>
    /// Change layer properties to random values
    /// </summary>
    public class ScriptCustomGCode : ScriptGlobals
    {
        /// <summary>
        /// Set configurations here, this function trigger just after load a script
        /// </summary>
        public void ScriptInit()
        {
            Script.Name = "Custo gcode generator";
            Script.Description = "Generates custom gcode and saves the file";
            Script.Author = "Tiago Conceição";
            Script.Version = new Version(0, 1);
        }

        /// <summary>
        /// Validate user inputs here, this function trigger when user click on execute
        /// </summary>
        /// <returns>A error message, empty or null if validation passes.</returns>
        public string ScriptValidate()
        {
            return SlicerFile.SupportsGCode ? null : "GCode is not supported on this file";
        }

        /// <summary>
        /// Execute the script, this function trigger when when user click on execute and validation passes
        /// </summary>
        /// <returns>True if executes successfully to the end, otherwise false.</returns>
        public bool ScriptExecute()
        {
            var gcode = SlicerFile.GCode;
            gcode.Clear();

            float pos = 1;
            float layerHeight = 0.025f;
            float liftHeight = 4.5f;
            float feedrate = gcode.ConvertFromMillimetersPerMinute(150);
            float lightoff = gcode.ConvertFromSeconds(1f);

            gcode.AppendStartGCode();
            //gcode.AppendShowImageM6054(gcode.GetShowImageString(0));
            //gcode.AppendWaitG4(gcode.ConvertFromSeconds(2));
            //gcode.AppendTurnLightM106(255);
            gcode.AppendWaitG4(gcode.ConvertFromSeconds(1));
            //gcode.AppendTurnLightM106(0);
            gcode.AppendLiftMoveG0(20, feedrate, pos, feedrate);
            gcode.AppendWaitG4(gcode.ConvertFromSeconds(5));

            // 0.025 test
            /*gcode.AppendComment("0.025 layer height simulated print test");
            for (int i = 0; i < 50; i++)
            {
                pos = Layer.RoundHeight(pos + layerHeight);
                var liftPos = Layer.RoundHeight(pos + liftHeight);
                gcode.AppendLiftMoveG0(liftPos, feedrate, pos, feedrate, lightoff);
            }*/

            // 0.01 test
            gcode.AppendComment("0.01 layer height simulated print test");
            pos = 1;
            layerHeight = 0.01f;


            gcode.AppendMoveG0(pos, feedrate);
            gcode.AppendWaitG4(gcode.ConvertFromSeconds(5));
            for (int i = 0; i < 50; i++)
            {
                pos = Layer.RoundHeight(pos + layerHeight);
                var liftPos = Layer.RoundHeight(pos + liftHeight);
                gcode.AppendLiftMoveG0(liftPos, feedrate, pos, feedrate, lightoff);
            }

            // 0.001 test
            /*gcode.AppendComment("0.001 layer height simulated print test");
            pos = 1;
            layerHeight = 0.001f;
            liftHeight = 1;

            gcode.AppendMoveG0(pos, feedrate);
            gcode.AppendWaitG4(gcode.ConvertFromSeconds(5));
            for (int i = 0; i < 50; i++)
            {
                pos = Layer.RoundHeight(pos + layerHeight);
                var liftPos = Layer.RoundHeight(pos + liftHeight);
                gcode.AppendLiftMoveG0(liftPos, feedrate, pos, feedrate, lightoff);
            }*/


            /*// 0.05 backlash test
            gcode.AppendComment("0.05 backlash test");
            pos = 1;
            layerHeight = 0.02f;

            gcode.AppendMoveG0(pos, feedrate);
            //gcode.AppendWaitG4(gcode.ConvertFromSeconds(5));
            for (int i = 0; i < 50; i++)
            {
                var liftPos = Layer.RoundHeight(pos + layerHeight);
                gcode.AppendMoveG0(liftPos, feedrate);
                gcode.AppendWaitG4(lightoff);
                gcode.AppendMoveG0(pos, feedrate);
                gcode.AppendWaitG4(lightoff);
            }
            */

            /*gcode.AppendMoveG0(2, gcode.ConvertFromMillimetersPerMinute(150));
            gcode.AppendWaitG4(lightoff);
            gcode.AppendMoveG0(2.5f, gcode.ConvertFromMillimetersPerMinute(160));
            gcode.AppendWaitG4(lightoff);
            gcode.AppendMoveG0(3f, gcode.ConvertFromMillimetersPerMinute(170));
            gcode.AppendWaitG4(lightoff);
            gcode.AppendMoveG0(3.5f, gcode.ConvertFromMillimetersPerMinute(180));
            gcode.AppendWaitG4(lightoff);
            gcode.AppendMoveG0(4.0f, gcode.ConvertFromMillimetersPerMinute(190));
            gcode.AppendWaitG4(lightoff);
            gcode.AppendMoveG0(4.5f, gcode.ConvertFromMillimetersPerMinute(195));
            gcode.AppendWaitG4(lightoff);
            gcode.AppendMoveG0(5f, gcode.ConvertFromMillimetersPerMinute(199));
            gcode.AppendWaitG4(lightoff);
            gcode.AppendMoveG0(5.5f, gcode.ConvertFromMillimetersPerMinute(205));
            gcode.AppendWaitG4(lightoff);*/

            gcode.AppendMoveG0(100, feedrate);
            //gcode.AppendTurnMotors(false);



            SlicerFile.SuppressRebuildGCode = true;
            SlicerFile.Save(Progress);
            SlicerFile.SuppressRebuildGCode = false;

            // return true if not cancelled by user
            return !Progress.Token.IsCancellationRequested;
        }
    }
}