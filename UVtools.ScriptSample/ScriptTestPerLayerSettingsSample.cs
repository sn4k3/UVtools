/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.Operations;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample
{
    /// <summary>
    /// Change layer properties to random values
    /// </summary>
    public class ScriptTestPerLayerSettingsSample : ScriptGlobals
    {
        private ScriptCheckBoxInput InputDoNotUseLift = new()
        {
            Label = "Do not perform the lift sequence for same height layers",
            ToolTip = "Not all printers are compatible with this even if they can maintain same Z position, some will require a obligatory lift/retract",
            Value = true
        };

        /// <summary>
        /// Set configurations here, this function trigger just after load a script
        /// </summary>
        public void ScriptInit()
        {
            Script.Name = "Test per layer settings capability with a print";
            Script.Description = "Print this file to check if your printer is able to have per layer independent settings.\n" +
                                 "1) Load a file that you previous printed into UVtools\n" +
                                 "2) Run this script\n" +
                                 "3) Go to File -> Save As, and give it a new name\n" +
                                 "4) Remove printer VAT and head/plate\n" +
                                 "5) Print the created file and observe the printer LCD and movements:\n" +
                                 "- First layer must show the whole face and do the normal lift sequence and raise to the next layer.\n" +
                                 "- Then all the renaming layers should print one object per layer at same height, if not, then your printer is not able.\n" +
                                 "Note: Look at printer screen to confirm the layer position, exposure time is set to 5s.\n" +
                                 "If you find yours compatible and not on the official list, please report to us.\n" +
                                 "https://github.com/sn4k3/UVtools/wiki/Printer-compability-with-per-layer-settings-and-advanced-tools";
            Script.Author = "Tiago Conceição";
            Script.Version = new Version(0, 1);
            Script.UserInputs.Add(InputDoNotUseLift);
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
            const byte layerCount = 5;
            const ushort eyeDiameter = 300;
            const ushort noseHeight = eyeDiameter;
            const ushort noseThickness = 100;
            const ushort mouthHeight = 300;
            const ushort faceSpacing = 150;
            const LineType lineType = LineType.AntiAlias;
            Progress.Reset("Generating layers", layerCount); // Sets the progress name and number of items to process

            // Layer 0 = Whole face
            // Layer 1 = Left eye
            // Layer 2 = Nose
            // Layer 3 = Right eye
            // Layer 4 = Mouth
            // Exercise for you: Do eyebrows
            var mats = EmguExtensions.Allocate(layerCount, SlicerFile.Resolution); // Allocate x images with file resolution

            int x, y;
            int xCenter = (int) (SlicerFile.ResolutionX / 2);
            //int yCenter = (int) (SlicerFile.ResolutionY / 2);

            // Do the left eye
            x = xCenter - noseThickness/2 - faceSpacing - eyeDiameter/2;
            y = faceSpacing;
            CvInvoke.Circle(mats[0], new Point(x, y), eyeDiameter/2, EmguExtensions.WhiteByte, -1, lineType);
            CvInvoke.Circle(mats[1], new Point(x, y), eyeDiameter/2, EmguExtensions.WhiteByte, -1, lineType);
            Progress++;

            // Do the right eye, the mirror of left...
            x = (int)(SlicerFile.ResolutionX - x);
            CvInvoke.Circle(mats[0], new Point(x, y), eyeDiameter / 2, EmguExtensions.WhiteByte, -1, lineType);
            CvInvoke.Circle(mats[3], new Point(x, y), eyeDiameter / 2, EmguExtensions.WhiteByte, -1, lineType);
            Progress++;

            // Do the noose
            x = xCenter - noseThickness / 2;
            CvInvoke.Rectangle(mats[0], new Rectangle(x, y, noseThickness, noseHeight), EmguExtensions.WhiteByte, -1, lineType);
            CvInvoke.Rectangle(mats[2], new Rectangle(x, y, noseThickness, noseHeight), EmguExtensions.WhiteByte, -1, lineType);
            Progress++;

            // Do the mouth
            x = xCenter;
            y += noseHeight + faceSpacing;
            CvInvoke.Ellipse(mats[0], new Point(x, y), new Size(eyeDiameter+faceSpacing+noseThickness/2, mouthHeight), 0, 0, 180, EmguExtensions.WhiteByte, -1, lineType);
            CvInvoke.Ellipse(mats[4], new Point(x, y), new Size(eyeDiameter+faceSpacing+noseThickness/2, mouthHeight), 0, 0, 180, EmguExtensions.WhiteByte, -1, lineType);

            SlicerFile.LayerManager.AllocateAndSetFromMat(mats); // Replace layers and rebuild properties

            SlicerFile.BottomLayerCount = 1;    // Set one bottom layer, the whole face
            SlicerFile.BottomExposureTime = 5;  // Set exposure to be fixed at 5s
            SlicerFile.ExposureTime = 5;        // Set exposure to be fixed at 5s

            // Set layers 2-4 all same z height as layer 1
            for (int layerIndex = 2; layerIndex < mats.Length; layerIndex++)
            {
                SlicerFile[layerIndex].PositionZ = SlicerFile[1].PositionZ;
            }

            if (InputDoNotUseLift.Value)
            {
                SlicerFile.LayerManager.SetNoLiftForSamePositionedLayers();
            }
            Progress++;

            // Move me to the middle
            new OperationMove(SlicerFile).Execute(Progress);

            // Generate a cool waves pattern, just because i can :)
            var pdOp = new OperationPixelDimming(SlicerFile) {WallThickness = 25};
            pdOp.GenerateInfill("Waves");
            pdOp.Execute(Progress);

            // return true if not cancelled by user
            return !Progress.Token.IsCancellationRequested;
        }
    }
}
