/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample
{
    /// <summary>
    /// Performs a black inset around objects
    /// </summary>
    public class ScriptChangeLayerPropertiesSample : ScriptGlobals
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
            Progress.Reset("Changing layers", Operation.LayerRangeCount); // Sets the progress name and number of items to process

            Random random = new();

            for (uint layerIndex = Operation.LayerIndexStart; layerIndex < Operation.LayerIndexEnd; layerIndex++)
            {
                Progress.Token.ThrowIfCancellationRequested(); // Abort operation, user requested cancellation
                var layer = SlicerFile[layerIndex]; // Unpack and expose layer variable for easier use

                layer.LiftHeight = random.Next(3, 10);     // Random value from 3 to 10
                layer.LiftSpeed = random.Next(50, 200);    // Random value from 50 to 200
                layer.RetractSpeed = random.Next(50, 200); // Random value from 50 to 200

                Progress++; // Increment progress bar by 1
            }

            // return true if not cancelled by user
            return !Progress.Token.IsCancellationRequested;
        }
    }
}
