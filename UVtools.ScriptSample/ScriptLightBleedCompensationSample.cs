/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Scripting;
using Emgu.CV;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;

namespace UVtools.ScriptSample
{
    /// <summary>
    /// Performs a black inset around objects
    /// </summary>
    public class ScriptLightBleedCompensationSample : ScriptGlobals
    {
        ScriptTextBoxInput BrightnessesInput = new()
        {
            Label = "Brightnesses",
            ToolTip = "Brightness to reduce each subsequent repeated pixels",
            Unit = "1-255",
            Value = "25,20,15,10,5"
        };


        public byte[] Levels
        {
            get
            {
                List<byte> levels = new();
                var split = BrightnessesInput.Value.Split(',', StringSplitOptions.TrimEntries);
                foreach (var str in split)
                {
                    if(!byte.TryParse(str, out var brightness)) continue;
                    if(brightness is byte.MinValue or byte.MaxValue) continue;
                    levels.Add(brightness);
                }

                return levels.ToArray();
            }
        }

        /// <summary>
        /// Set configurations here, this function trigger just after load a script
        /// </summary>
        public void ScriptInit()
        {
            Script.Name = "Light bleed compensation";
            Script.Description = "Dim sequential pixels";
            Script.Author = "Tiago Conceição";
            Script.Version = new Version(0, 1);
            Script.UserInputs.Add(BrightnessesInput);
        }

        /// <summary>
        /// Validate user inputs here, this function trigger when user click on execute
        /// </summary>
        /// <returns>A error message, empty or null if validation passes.</returns>
        public string ScriptValidate()
        {
            StringBuilder sb = new();
            
            if (Levels.Length == 0)
            {
                sb.AppendLine($"No brightness levels are set");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Execute the script, this function trigger when when user click on execute and validation passes
        /// </summary>
        /// <returns>True if executes successfully to the end, otherwise false.</returns>
        public bool ScriptExecute()
        {
            Progress.Reset("Bleed compensation", Operation.LayerRangeCount); // Sets the progress name and number of items to process
            var brightnesses = Levels;

            // Loop user selected layers in parallel, this will put each core of CPU working here on parallel
            Parallel.For(Operation.LayerIndexStart, Operation.LayerIndexEnd+1, layerIndex =>
            {
                if (Progress.Token.IsCancellationRequested) return; // Abort operation, user requested cancellation

                var layer = SlicerFile[layerIndex]; // Unpack and expose layer variable for easier use
                using var mat = layer.LayerMat;     // Gets this layer mat/image
                var original = mat.Clone();     // Keep a original mat copy

                var target = Operation.GetRoiOrDefault(mat); // Get ROI from mat if user selected an region

                for (byte i = 0; i < brightnesses.Length; i++)
                {
                    uint layerIndexNext = (uint) (layerIndex + i + 1);
                    if (layerIndexNext > Operation.LayerIndexEnd) break;
                    using var subtractMat = EmguExtensions.InitMat(target.Size, new MCvScalar(brightnesses[i]));

                    using var nextMat = SlicerFile[layerIndexNext].LayerMat;
                    var nextMatRoi = Operation.GetRoiOrDefault(nextMat);

                    CvInvoke.Subtract(target, subtractMat, target, nextMatRoi);
                }

                // Apply the results only to the selected masked area, if user selected one
                Operation.ApplyMask(original, target);

                // Set current layer image with the modified mat we just manipulated
                layer.LayerMat = mat;

                // Increment progress bar by 1
                Progress.LockAndIncrement();
            });

            // return true if not cancelled by user
            return !Progress.Token.IsCancellationRequested;
        }
    }
}
