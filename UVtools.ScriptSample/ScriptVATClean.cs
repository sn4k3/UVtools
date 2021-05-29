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
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.Scripting;

namespace UVtools.ScriptSample
{
    /// <summary>
    /// Change layer properties to random values
    /// </summary>
    public class ScriptVATClean : ScriptGlobals
    {
        private ScriptNumericalInput<ushort> InputInset = new()
        {
            Label = "Resolution inset",
            ToolTip = "Inset image resolution by this value to create a black border",
            Unit = "px",
            Minimum = 0,
            Maximum = ushort.MaxValue,
            Increment = 1
        };

        private ScriptNumericalInput<float> InputExposureTime = new()
        {
            Label = "Exposure time",
            ToolTip = "Time to exposure the layer",
            Unit = "s",
            Minimum = 0,
            Maximum = 50,
            DecimalPlates = 2,
            Increment = 1
        };

        /// <summary>
        /// Set configurations here, this function trigger just after load a script
        /// </summary>
        public void ScriptInit()
        {
            Script.Name = "Create a file to clean VAT exposing 1 layer";
            Script.Description = "Print this file to clean your VAT by exposing 1 layer and peel it off.\n" +
                                 "1) Load a file for your printer that you previous printed into UVtools\n" +
                                 "2) Configure and run this script\n" +
                                 "3) Go to File -> Save As, and give it a new name\n" +
                                 "4) Remove head/plate\n" +
                                 "5) Place a plastic spatula in the VAT at an angle with the handle laying on the top of the VAT frame\n" +
                                 "6) Print the created file\n" +
                                 "7) When print finish slowly peel the layer with the spatula";
            Script.Author = "Tiago Conceição";
            Script.Version = new Version(0, 1);


            InputInset.Maximum = (ushort) (Math.Max(SlicerFile.ResolutionX, SlicerFile.ResolutionY) / 2 - 2);
            InputExposureTime.Value = SlicerFile.ExposureTime * 2;

            Script.UserInputs.Add(InputInset);
            Script.UserInputs.Add(InputExposureTime);
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
            Progress.Reset("Generating layers", 1); // Sets the progress name and number of items to process

            var layer = SlicerFile[0];
            layer.PositionZ = SlicerFile.MachineZ;          // Send head to top if possible

            using var mat = EmguExtensions.InitMat(SlicerFile.Resolution);
            CvInvoke.Rectangle(mat, new Rectangle(
                new Point(InputInset.Value, InputInset.Value),
                new Size((int) (SlicerFile.ResolutionX - InputInset.Value*2)-1, (int) (SlicerFile.ResolutionY - InputInset.Value*2)-1)
                ), EmguExtensions.WhiteColor, -1, LineType.FourConnected);
            layer.LayerMat = mat;

            SlicerFile.SuppressRebuildPropertiesWork(() =>
            {
                SlicerFile.BottomLayerCount = 1;
                
                SlicerFile.LayerManager.Layers = new[] { layer };
            });

            SlicerFile.BottomExposureTime =
            SlicerFile.ExposureTime = InputExposureTime.Value;

            SlicerFile.BottomLiftSpeed =
            SlicerFile.LiftSpeed =
            SlicerFile.RetractSpeed = 200;

            SlicerFile.BottomLiftHeight =
            SlicerFile.LiftHeight = 1;

            Progress++;

            SlicerFile.SetThumbnails(GetThumbnail());
            
            // return true if not cancelled by user
            return !Progress.Token.IsCancellationRequested;
        }

        public Mat GetThumbnail()
        {
            Mat thumbnail = EmguExtensions.InitMat(new Size(400, 200), 3);
            var fontFace = FontFace.HersheyDuplex;
            var fontScale = 1;
            var fontThickness = 2;
            const byte xSpacing = 45;
            const byte ySpacing = 45;
            CvInvoke.PutText(thumbnail, "UVtools", new Point(140, 35), fontFace, fontScale, new MCvScalar(255, 27, 245), fontThickness + 1);
            CvInvoke.Line(thumbnail, new Point(xSpacing, 0), new Point(xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.Line(thumbnail, new Point(xSpacing, ySpacing + 5), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.Line(thumbnail, new Point(thumbnail.Width - xSpacing, 0), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
            CvInvoke.PutText(thumbnail, "VAT Clean Utility", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
            CvInvoke.PutText(thumbnail, $"Exposure time: {SlicerFile.ExposureTime}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);
            CvInvoke.PutText(thumbnail, $"Use the spatula in!", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);

            return thumbnail;
        }
    }
}
