/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public class OperationCalculator : Operation
    {
        #region Overrides
        public override string Title => "Calculator";
        public override string Description => null;

        public override string ConfirmationText => null;

        public override string ProgressTitle => null;

        public override string ProgressAction => null;

        public override Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.None;
        public override bool CanROI => false;

        public override bool CanHaveProfiles => false;
        #endregion

        #region Properties
        public MillimetersToPixels CalcMillimetersToPixels { get; set; }
        public LightOffDelayC CalcLightOffDelay { get; set; }
        public OptimalModelTilt CalcOptimalModelTilt { get; set; }
        #endregion

        #region Constructor

        public OperationCalculator() { }

        public OperationCalculator(FileFormat slicerFile) : base(slicerFile)
        { }

        public override void InitWithSlicerFile()
        {
            base.InitWithSlicerFile();
            CalcMillimetersToPixels = new MillimetersToPixels(SlicerFile.Resolution, SlicerFile.Display);
            CalcLightOffDelay = new LightOffDelayC(
                (decimal)SlicerFile.LiftHeight, (decimal)SlicerFile.BottomLiftHeight,
                (decimal)SlicerFile.LiftSpeed, (decimal)SlicerFile.BottomLiftSpeed,
                (decimal)SlicerFile.RetractSpeed, (decimal)SlicerFile.RetractSpeed);
            CalcOptimalModelTilt = new OptimalModelTilt(SlicerFile.Resolution, SlicerFile.Display,
                (decimal)SlicerFile.LayerHeight);
        }

        #endregion

        public abstract class Calculation : BindableBase
        {
            public abstract string Description { get; }
            public abstract string Formula { get; }
        }

        public sealed class MillimetersToPixels : Calculation
        {
            private uint _resolutionX;
            private uint _resolutionY;
            private decimal _displayWidth;
            private decimal _displayHeight;
            private decimal _millimeters = 1;

            public override string Description => "Converts from Millimeters to Pixels";
            public override string Formula => "Pixels = Resolution / Display * Millimeters";

            public MillimetersToPixels(Size resolution, SizeF display, decimal millimeters = 1)
            {
                _resolutionX = (uint) resolution.Width;
                _resolutionY = (uint) resolution.Height;

                _displayWidth = (decimal) display.Width;
                _displayHeight = (decimal) display.Height;

                _millimeters = millimeters;
            }

            public uint ResolutionX
            {
                get => _resolutionX;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _resolutionX, value)) return;
                    RaisePropertyChanged(nameof(PixelsPerMillimeterX));
                    RaisePropertyChanged(nameof(PixelsX));
                }
            }

            public uint ResolutionY
            {
                get => _resolutionY;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _resolutionY, value)) return;
                    RaisePropertyChanged(nameof(PixelsPerMillimeterY));
                    RaisePropertyChanged(nameof(PixelsY));
                }
            }

            public decimal DisplayWidth
            {
                get => _displayWidth;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _displayWidth, value)) return;
                    RaisePropertyChanged(nameof(PixelsPerMillimeterX));
                    RaisePropertyChanged(nameof(PixelsX));
                }
            }

            public decimal DisplayHeight
            {
                get => _displayHeight;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _displayHeight, value)) return;
                    RaisePropertyChanged(nameof(PixelsPerMillimeterY));
                    RaisePropertyChanged(nameof(PixelsY));
                }
            }

            public decimal Millimeters
            {
                get => _millimeters;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _millimeters, value)) return;
                    RaisePropertyChanged(nameof(PixelsX));
                    RaisePropertyChanged(nameof(PixelsY));
                }
            }

            public decimal PixelsPerMillimeterX => DisplayWidth > 0 ? Math.Round(ResolutionX / DisplayWidth, 2) : 0;
            public decimal PixelsPerMillimeterY => DisplayHeight > 0 ? Math.Round(ResolutionY / DisplayHeight, 2) : 0;

            public decimal PixelsX => Math.Round(PixelsPerMillimeterX * Millimeters, 2);
            public decimal PixelsY => Math.Round(PixelsPerMillimeterY * Millimeters, 2);

            
        }

        public sealed class LightOffDelayC : Calculation
        {
            private decimal _liftHeight;
            private decimal _bottomLiftHeight;
            private decimal _liftSpeed;
            private decimal _bottomLiftSpeed;
            private decimal _retractSpeed;
            private decimal _bottomRetractSpeed;
            private decimal _waitTime = 2.5m;
            private decimal _bottomWaitTime = 3m;

            public override string Description =>
                "Calculates the required light-off delay (Moving time from the build plate + additional time for resin to stabilize) given the lifting height, speed and retract to wait x seconds before cure a new layer.\n" +
                "Light-off delay is crucial for gaining higher-resolution and sharper prints.\n" +
                "When the build plate retracts, it is important to allow enough time for the resin to stabilize before the UV lights turn on. This would ideally be around 2-3s.";

            public override string Formula => "Light-off delay = Lifting height / (Lifting speed / 60) + Lifting height / (Retract speed / 60) + Desired wait seconds";

            public decimal LiftHeight
            {
                get => _liftHeight;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _liftHeight, value)) return;
                    RaisePropertyChanged(nameof(LightOffDelay));
                }
            }

            public decimal BottomLiftHeight
            {
                get => _bottomLiftHeight;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _bottomLiftHeight, value)) return;
                    RaisePropertyChanged(nameof(BottomLightOffDelay));
                }
            }

            public decimal LiftSpeed
            {
                get => _liftSpeed;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _liftSpeed, value)) return;
                    RaisePropertyChanged(nameof(LightOffDelay));
                }
            }

            public decimal BottomLiftSpeed
            {
                get => _bottomLiftSpeed;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _bottomLiftSpeed, value)) return;
                    RaisePropertyChanged(nameof(BottomLightOffDelay));
                }
            }

            public decimal RetractSpeed
            {
                get => _retractSpeed;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _retractSpeed, value)) return;
                    RaisePropertyChanged(nameof(LightOffDelay));

                    BottomRetractSpeed = _retractSpeed;
                }
            }

            public decimal BottomRetractSpeed
            {
                get => _bottomRetractSpeed;
                set
                {
                    if (!RaiseAndSetIfChanged(ref _bottomRetractSpeed, value)) return;
                    RaisePropertyChanged(nameof(BottomLightOffDelay));
                }
            }

            public decimal WaitTime
            {
                get => _waitTime;
                set
                {
                    if(!RaiseAndSetIfChanged(ref _waitTime, value)) return;
                    RaisePropertyChanged(nameof(LightOffDelay));
                }
            }

            public decimal BottomWaitTime
            {
                get => _bottomWaitTime;
                set
                {
                    if (!RaiseAndSetIfChanged(ref _bottomWaitTime, value)) return;
                    RaisePropertyChanged(nameof(BottomLightOffDelay));
                }
            }

            public decimal LightOffDelay => CalculateSeconds(_liftHeight, _liftSpeed, _retractSpeed, _waitTime);

            public decimal BottomLightOffDelay => CalculateSeconds(_bottomLiftHeight, _bottomLiftSpeed, _bottomRetractSpeed, _bottomWaitTime);

            public LightOffDelayC()
            {
            }

            public LightOffDelayC(decimal liftHeight, decimal bottomLiftHeight, decimal liftSpeed, decimal bottomLiftSpeed, decimal retractSpeed, decimal bottomRetractSpeed, decimal waitTime = 2.5m, decimal bottomWaitTime = 3m)
            {
                _liftHeight = liftHeight;
                _bottomLiftHeight = bottomLiftHeight;
                _liftSpeed = liftSpeed;
                _bottomLiftSpeed = bottomLiftSpeed;
                _retractSpeed = retractSpeed;
                _bottomRetractSpeed = bottomRetractSpeed;
                _waitTime = waitTime;
                _bottomWaitTime = bottomWaitTime;
            }

            public static decimal CalculateSeconds(decimal liftHeight, decimal liftSpeed, decimal retractSpeed, decimal extraWaitTime = 0)
            {
                var time = extraWaitTime;
                if (liftSpeed > 0)
                {
                    time += liftHeight / (liftSpeed / 60m);
                }
                if (retractSpeed > 0)
                {
                    time += liftHeight / (retractSpeed / 60m);
                }
                
                return Math.Round(time, 2);
            }

            public static float CalculateSeconds(float liftHeight, float liftSpeed, float retractSpeed, float extraWaitTime = 0,
                float liftHeight2 = 0, float liftSpeed2 = 0, float retractHeight2 = 0, float retractSpeed2 = 0)
            {
                var time = extraWaitTime;
                if (liftHeight > 0 && liftSpeed > 0)
                    time += liftHeight / (liftSpeed / 60f);

                if (liftHeight2 > 0 && liftSpeed2 > 0)
                    time += liftHeight2 / (liftSpeed2 / 60f);

                if (retractHeight2 > 0 && retractSpeed2 > 0)
                    time += retractHeight2 / (retractSpeed2 / 60f);

                var remainingRetractHeight = liftHeight + liftHeight2 - retractHeight2;

                if (remainingRetractHeight > 0 && retractSpeed > 0)
                {
                    time += remainingRetractHeight / (retractSpeed / 60f);
                }

                return (float)Math.Round(time, 2);

            }

            public static float CalculateSeconds(Layer layer, float extraTime)
                => CalculateSeconds(layer.LiftHeight, layer.LiftSpeed, layer.RetractSpeed, extraTime, 
                    layer.LiftHeight2, layer.LiftSpeed2, layer.RetractHeight2, layer.RetractSpeed2);

            public static uint CalculateMilliseconds(Layer layer, float extraTime)
                => (uint)(CalculateSeconds(layer.LiftHeight, layer.LiftSpeed, layer.RetractSpeed, extraTime,
                    layer.LiftHeight2, layer.LiftSpeed2, layer.RetractHeight2, layer.RetractSpeed2) * 1000);

            public static uint CalculateMilliseconds(float liftHeight, float liftSpeed, float retract, float extraWaitTime = 0) =>
                (uint)(CalculateSeconds(liftHeight, liftSpeed, retract, extraWaitTime) * 1000);


            public static float CalculateSecondsLiftOnly(float liftHeight, float liftSpeed, float liftHeight2 = 0, float liftSpeed2 = 0, float extraWaitTime = 0)
            {
                var time = extraWaitTime;
                if (liftHeight > 0 && liftSpeed > 0) time += (float)Math.Round(liftHeight / (liftSpeed / 60f) + extraWaitTime, 2);
                if (liftHeight2 > 0 && liftSpeed2 > 0) time += (float)Math.Round(liftHeight2 / (liftSpeed2 / 60f) + extraWaitTime, 2);
                return time;
            }

            public static uint CalculateMillisecondsLiftOnly(float liftHeight, float liftSpeed, float liftHeight2 = 0, float liftSpeed2 = 0, float extraWaitTime = 0) =>
                (uint)(CalculateSecondsLiftOnly(liftHeight, liftSpeed, liftHeight2, liftSpeed2, extraWaitTime) * 1000);

            public static float CalculateSecondsLiftOnly(Layer layer, float extraWaitTime = 0) =>
                CalculateSecondsLiftOnly(layer.LiftHeight, layer.LiftSpeed, layer.LiftHeight2, layer.LiftSpeed2, extraWaitTime);

            public static uint CalculateMillisecondsLiftOnly(Layer layer, float extraWaitTime = 0) =>
                (uint)(CalculateSecondsLiftOnly(layer.LiftHeight, layer.LiftSpeed, layer.LiftHeight2, layer.LiftSpeed2, extraWaitTime) * 1000);

        }

        public sealed class OptimalModelTilt : Calculation
        {
            private uint _resolutionX;
            private uint _resolutionY;
            private decimal _displayWidth;
            private decimal _displayHeight;
            private decimal _layerHeight = 0.05m;

            public override string Description => "Calculates the optimal model tilt angle for printing and to minimize the visual layer effect.";
            public override string Formula => "Angleº = tanh(Layer height / XYResolution) * (180 / PI)";

            public OptimalModelTilt(Size resolution, SizeF display, decimal layerHeight = 0.05m)
            {
                _resolutionX = (uint)resolution.Width;
                _resolutionY = (uint)resolution.Height;

                _displayWidth = (decimal)display.Width;
                _displayHeight = (decimal)display.Height;

                _layerHeight = layerHeight;
            }

            public uint ResolutionX
            {
                get => _resolutionX;
                set
                {
                    if (!RaiseAndSetIfChanged(ref _resolutionX, value)) return;
                    RaisePropertyChanged(nameof(XYResolution));
                    RaisePropertyChanged(nameof(XYResolutionUm));
                    RaisePropertyChanged(nameof(TiltAngleDegrees));
                }
            }

            public uint ResolutionY
            {
                get => _resolutionY;
                set
                {
                    if (!RaiseAndSetIfChanged(ref _resolutionY, value)) return;
                    RaisePropertyChanged(nameof(XYResolution));
                    RaisePropertyChanged(nameof(XYResolutionUm));
                    RaisePropertyChanged(nameof(TiltAngleDegrees));
                }
            }

            public decimal DisplayWidth
            {
                get => _displayWidth;
                set
                {
                    if (!RaiseAndSetIfChanged(ref _displayWidth, value)) return;
                    RaisePropertyChanged(nameof(XYResolution));
                    RaisePropertyChanged(nameof(XYResolutionUm));
                    RaisePropertyChanged(nameof(TiltAngleDegrees));
                }
            }

            public decimal DisplayHeight
            {
                get => _displayHeight;
                set
                {
                    if (!RaiseAndSetIfChanged(ref _displayHeight, value)) return;
                    RaisePropertyChanged(nameof(XYResolution));
                    RaisePropertyChanged(nameof(XYResolutionUm));
                    RaisePropertyChanged(nameof(TiltAngleDegrees));
                }
            }

            public decimal LayerHeight
            {
                get => _layerHeight;
                set
                {
                    if (!RaiseAndSetIfChanged(ref _layerHeight, Layer.RoundHeight(value))) return;
                    RaisePropertyChanged(nameof(XYResolution));
                    RaisePropertyChanged(nameof(XYResolutionUm));
                    RaisePropertyChanged(nameof(TiltAngleDegrees));
                }
            }

            public decimal XYResolution => DisplayWidth > 0 || DisplayHeight > 0 ?
                Math.Max(
                    DisplayWidth / ResolutionX,
                    DisplayHeight / ResolutionY
                )
                : 0;

            public decimal XYResolutionUm => Math.Round(XYResolution * 1000, 2);

            public decimal TiltAngleDegrees =>
                XYResolution > 0 ? (decimal) Math.Round(Math.Tanh((double) (_layerHeight / XYResolution)) * (180 / Math.PI), 3) : 0;

        }
    }
}
