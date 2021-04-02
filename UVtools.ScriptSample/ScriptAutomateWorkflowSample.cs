/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using Emgu.CV.CvEnum;
using UVtools.Core.Operations;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample
{
    /// <summary>
    /// A workflow automation sample
    /// </summary>
    public class ScriptAutomateWorkflowSample : ScriptGlobals
    {
        private ScriptCheckBoxInput InputErode = new()
        {
            Label = "Erode base layers",
            Value = true
        };
        private ScriptCheckBoxInput InputReliefRaft = new()
        {
            Label = "Relief raft",
            Value = true
        };
        private ScriptCheckBoxInput InputPixelDimming = new()
        {
            Label = "Pixel dimming base layers",
            Value = true
        };
        /// <summary>
        /// Set configurations here, this function trigger just after load a script
        /// </summary>
        public void ScriptInit()
        {
            Script.Name = "Automate my workflow";
            Script.Description = "A workflow automation sample";
            Script.Author = "Tiago Conceição";
            Script.Version = new Version(0, 1);
            Script.UserInputs.AddRange(new []
            {
                InputErode, 
                InputReliefRaft,
                InputPixelDimming
            });
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
            List<Operation> operations = new();

            // Morph bottom layers
            if (InputErode.Value)
            {
                OperationMorph morph = new(SlicerFile)
                {
                    MorphOperation = MorphOp.Erode,
                    Iterations = 4,
                };
                morph.SelectBottomLayers();
                operations.Add(morph);
            }

            // Raft relief
            if (InputReliefRaft.Value)
            {
                OperationRaftRelief raftRelief = new(SlicerFile)
                {
                    ReliefType = OperationRaftRelief.RaftReliefTypes.Relief,
                };
                operations.Add(raftRelief);
            }

            // Dim and apply checkboard pattern to bottom layers
            if (InputPixelDimming.Value)
            {
                OperationPixelDimming pixelDimming = new(SlicerFile);
                pixelDimming.GeneratePixelDimming("Chessboard");
                pixelDimming.SelectBottomLayers();
                operations.Add(pixelDimming);

                foreach (var operation in operations) // Loop all my created operations to execute them
                {
                    Progress.Token.ThrowIfCancellationRequested(); // Abort operation, user requested cancellation
                    operation.ROI = Operation.ROI; // Copy user selected ROI to my operation
                    operation.MaskPoints = Operation.MaskPoints; // Copy user selected Masks to my operation
                    if (!operation.CanValidate()) continue; // If cant validate don't execute the operation
                    operation.Execute(Progress);
                }
            }

            // return true if not cancelled by user
            return !Progress.Token.IsCancellationRequested;
        }
    }
}
