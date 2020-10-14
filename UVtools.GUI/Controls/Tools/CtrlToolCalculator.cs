/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Globalization;
using UVtools.Core;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.GUI.Controls.Tools
{
    public partial class CtrlToolCalculator : CtrlToolWindowContent
    {
        public OperationCalculator Operation { get; }
        
        public CtrlToolCalculator()
        {
            InitializeComponent();
            Operation = new OperationCalculator
            {
                CalcMillimetersToPixels = new OperationCalculator.MillimetersToPixels(Program.SlicerFile.Resolution, Program.SlicerFile.Display),
                CalcLightOffDelay = new OperationCalculator.LightOffDelayC(
                    (decimal) Program.SlicerFile.LiftHeight, (decimal) Program.SlicerFile.BottomLiftHeight,
                    (decimal) Program.SlicerFile.LiftSpeed, (decimal) Program.SlicerFile.BottomLiftSpeed, 
                    (decimal) Program.SlicerFile.RetractSpeed, (decimal)Program.SlicerFile.RetractSpeed)
            };
            SetOperation(Operation);

            tpMillimetersToPixels.Tag = Operation.CalcMillimetersToPixels;
            tpLightOffDelay.Tag = Operation.CalcMillimetersToPixels;

            lbMMtoPixelsDescription.Text = Operation.CalcMillimetersToPixels.Description;
            lbMMtoPixelsDescription.Text += $"\n\nFormula: {Operation.CalcMillimetersToPixels.Formula}";
            lbMMtoPixelsDescription.MaximumSize = new Size(Width - 20, 0);

            lbLightOffDelayDescription.Text = Operation.CalcLightOffDelay.Description;
            lbLightOffDelayDescription.Text += $"\n\nFormula: {Operation.CalcLightOffDelay.Formula}";
            lbLightOffDelayDescription.MaximumSize = new Size(Width - 20, 0);


            nmMMtoPXResolutionX.Value = Program.SlicerFile.ResolutionX;
            nmMMtoPXResolutionY.Value = Program.SlicerFile.ResolutionY;
            nmMMtoPXDisplayWidth.Value = (decimal) Program.SlicerFile.DisplayWidth;
            nmMMtoPXDisplayHeight.Value = (decimal) Program.SlicerFile.DisplayHeight;
            nmMMtoPXInputMillimeters.Value = Operation.CalcMillimetersToPixels.Millimeters;
            Operation.CalcMillimetersToPixels.PropertyChanged += (sender, e) =>
            {
                if(e.PropertyName == nameof(Operation.CalcMillimetersToPixels.PixelsX) 
                   || e.PropertyName == nameof(Operation.CalcMillimetersToPixels.PixelsY))
                    CalculateMillimetersToPixels();
            };

            nmLightOffDelayLiftHeight.Value = (decimal) Program.SlicerFile.LiftHeight;
            nmLightOffDelayBottomLiftHeight.Value = (decimal) Program.SlicerFile.BottomLiftHeight;
            nmLightOffDelayLiftSpeed.Value = (decimal) Program.SlicerFile.LiftSpeed;
            nmLightOffDelayBottomLiftSpeed.Value = (decimal) Program.SlicerFile.BottomLiftSpeed;
            nmLightOffDelayBottomRetract.Value = nmLightOffDelayRetract.Value = (decimal) Program.SlicerFile.RetractSpeed;
            nmLightOffDelayWaitTime.Value = Operation.CalcLightOffDelay.WaitTime;
            nmLightOffDelayBottomWaitTime.Value = Operation.CalcLightOffDelay.BottomWaitTime;

            lbLightOffDelayCurrentValue.Text = $"Current value: {Program.SlicerFile.LayerOffTime}";
            lbLightOffDelayCurrentBottomValue.Text = $"Current value: {Program.SlicerFile.BottomLayerOffTime}";
            Operation.CalcLightOffDelay.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(Operation.CalcLightOffDelay.LightOffDelay)
                    || e.PropertyName == nameof(Operation.CalcLightOffDelay.BottomLightOffDelay))
                    CalculateLightOffDelay();
            };

            CalculateMillimetersToPixels();
            CalculateLightOffDelay();

        }

        private void CalculateMillimetersToPixels()
        {
            tbMMtoPXResultPixelsPerMillimeterX.Text = Operation.CalcMillimetersToPixels.PixelsPerMillimeterX.ToString(CultureInfo.InvariantCulture);
            tbMMtoPXResultPixelsPerMillimeterY.Text = Operation.CalcMillimetersToPixels.PixelsPerMillimeterY.ToString(CultureInfo.InvariantCulture);
            tbMMtoPXResultPixelsX.Text = Operation.CalcMillimetersToPixels.PixelsX.ToString(CultureInfo.InvariantCulture);
            tbMMtoPXResultPixelsY.Text = Operation.CalcMillimetersToPixels.PixelsY.ToString(CultureInfo.InvariantCulture);
        }

        private void CalculateLightOffDelay()
        {
            tbLightOffDelay.Text = Operation.CalcLightOffDelay.LightOffDelay.ToString(CultureInfo.InvariantCulture);
            tbLightOffDelayBottom.Text = Operation.CalcLightOffDelay.BottomLightOffDelay.ToString(CultureInfo.InvariantCulture);
        }

        private void EventValueChanged(object sender, EventArgs e)
        {
            // Millimeters to pixels
            if (ReferenceEquals(sender, nmMMtoPXResolutionX))
            {
                Operation.CalcMillimetersToPixels.ResolutionX = (uint) nmMMtoPXResolutionX.Value;
                return;
            }
            if (ReferenceEquals(sender, nmMMtoPXResolutionY))
            {
                Operation.CalcMillimetersToPixels.ResolutionY = (uint)nmMMtoPXResolutionY.Value;
                return;
            }

            if (ReferenceEquals(sender, nmMMtoPXDisplayWidth))
            {
                Operation.CalcMillimetersToPixels.DisplayWidth = nmMMtoPXDisplayWidth.Value;
                return;
            }

            if (ReferenceEquals(sender, nmMMtoPXDisplayHeight))
            {
                Operation.CalcMillimetersToPixels.DisplayHeight = nmMMtoPXDisplayHeight.Value;
                return;
            }

            if (ReferenceEquals(sender, nmMMtoPXInputMillimeters))
            {
                Operation.CalcMillimetersToPixels.Millimeters = nmMMtoPXInputMillimeters.Value;
                return;
            }

            // Light-Off Delay
            if (ReferenceEquals(sender, nmLightOffDelayLiftHeight))
            {
                Operation.CalcLightOffDelay.LiftHeight = nmLightOffDelayLiftHeight.Value;
                return;
            }

            if (ReferenceEquals(sender, nmLightOffDelayBottomLiftHeight))
            {
                Operation.CalcLightOffDelay.BottomLiftHeight = nmLightOffDelayBottomLiftHeight.Value;
                return;
            }

            if (ReferenceEquals(sender, nmLightOffDelayLiftSpeed))
            {
                Operation.CalcLightOffDelay.LiftSpeed = nmLightOffDelayLiftSpeed.Value;
                return;
            }

            if (ReferenceEquals(sender, nmLightOffDelayBottomLiftSpeed))
            {
                Operation.CalcLightOffDelay.BottomLiftSpeed = nmLightOffDelayBottomLiftSpeed.Value;
                return;
            }

            if (ReferenceEquals(sender, nmLightOffDelayRetract))
            {
                nmLightOffDelayBottomRetract.Value = Operation.CalcLightOffDelay.RetractSpeed = nmLightOffDelayRetract.Value;
                return;
            }

            if (ReferenceEquals(sender, nmLightOffDelayBottomRetract))
            {
                Operation.CalcLightOffDelay.BottomRetractSpeed = nmLightOffDelayBottomRetract.Value;
                return;
            }

            if (ReferenceEquals(sender, nmLightOffDelayWaitTime))
            {
                Operation.CalcLightOffDelay.WaitTime = nmLightOffDelayWaitTime.Value;
                return;
            }

            if (ReferenceEquals(sender, nmLightOffDelayBottomWaitTime))
            {
                Operation.CalcLightOffDelay.BottomWaitTime = nmLightOffDelayBottomWaitTime.Value;
                return;
            }
        }

        private void EventClick(object sender, EventArgs e)
        {
            if (ReferenceEquals(sender, btnLightOffDelaySetParameter))
            {
                Program.SlicerFile.LiftHeight = (float) Operation.CalcLightOffDelay.LiftHeight;
                Program.SlicerFile.LiftSpeed = (float) Operation.CalcLightOffDelay.LiftSpeed;
                Program.SlicerFile.RetractSpeed = (float) Operation.CalcLightOffDelay.RetractSpeed;
                Program.SlicerFile.LayerOffTime = (float) Operation.CalcLightOffDelay.LightOffDelay;
                Program.FrmMain.CanSave = true;
                lbLightOffDelayCurrentValue.Text = $"Current value: {Program.SlicerFile.LayerOffTime}";
                return;
            }

            if (ReferenceEquals(sender, btnLightOffDelaySetBottomParameter))
            {
                Program.SlicerFile.BottomLiftHeight = (float)Operation.CalcLightOffDelay.BottomLiftHeight;
                Program.SlicerFile.BottomLiftSpeed = (float)Operation.CalcLightOffDelay.BottomLiftSpeed;
                Program.SlicerFile.BottomLayerOffTime = (float)Operation.CalcLightOffDelay.BottomLightOffDelay;
                Program.FrmMain.CanSave = true;
                lbLightOffDelayCurrentBottomValue.Text = $"Current value: {Program.SlicerFile.BottomLayerOffTime}";
                return;
            }
        }
    }
}
