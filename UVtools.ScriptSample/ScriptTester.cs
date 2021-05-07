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
using UVtools.Core.Extensions;

namespace UVtools.ScriptSample
{
    /// <summary>
    /// Change layer properties to random values
    /// </summary>
    public class ScriptChangeLayesrPropertiesSample : ScriptGlobals
    {
        /// <summary>
        /// Set configurations here, this function trigger just after load a script
        /// </summary>
        public void ScriptInit()
        {
            Script.Name = "Change layer properties";
            Script.Description = "Change layer properties to random values :D";
            Script.Author = "Tiago Conceição";
            Script.Version = new Version(0, 1);
        }

        /// <summary>
        /// Validate user inputs here, this function trigger when user click on execute
        /// </summary>
        /// <returns>A error message, empty or null if validation passes.</returns>
        public string ScriptValidate()
        {
            return null;
        }

        /// <summary>
        /// Execute the script, this function trigger when when user click on execute and validation passes
        /// </summary>
        /// <returns>True if executes successfully to the end, otherwise false.</returns>
        public bool ScriptExecute()
        {
            string path = @"c:\temp\UVToolAreaExp_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";

            Progress.Reset("Changing layers", Operation.LayerRangeCount); // Sets the progress name and number of items to process

            using var sw = File.CreateText(path);
            for (uint layerIndex = Operation.LayerIndexStart; layerIndex <= Operation.LayerIndexEnd; layerIndex++)
            {
                Progress.Token.ThrowIfCancellationRequested(); // Abort operation, user requested cancellation
                var layer = SlicerFile[layerIndex]; // Unpack and expose layer variable for easier use

                sw.WriteLine($@"{layerIndex}\{layer.NonZeroPixelCount}\{layer.BoundingRectangleMillimeters.Area()}");
                //sw.WriteLine(SlicerFile.GetName);
             
                Progress++; // Increment progress bar by 1
            }
            sw.Close();

            // return true if not cancelled by user
            return !Progress.Token.IsCancellationRequested;
        }
    }
}