/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Drawing;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;


public partial class OperationCalculator : Operation
{
    #region Overrides

    public override bool CanRunInPartialMode => true;

    public override string IconClass => "Calculator";

    public override string Title => "Calculator";
    public override string Description => null!;

    public override string ConfirmationText => null!;

    public override string ProgressTitle => null!;

    public override string ProgressAction => null!;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override bool CanROI => false;

    public override bool CanHaveProfiles => false;
    #endregion

    #region Properties

    public MillimetersToPixels CalcMillimetersToPixels { get; set; } = null!;
    public LightOffDelayC CalcLightOffDelay { get; set; } = null!;
    public OptimalModelTilt CalcOptimalModelTilt { get; set; } = null!;
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
        CalcOptimalModelTilt = new OptimalModelTilt(SlicerFile.Resolution, SlicerFile.Display, (decimal)SlicerFile.LayerHeight);
    }

    #endregion

    public abstract partial class Calculation : ObservableObject
    {
        public abstract string Description { get; }
        public abstract string Formula { get; }
    }

    public sealed partial class MillimetersToPixels : Calculation
    {
        public override string Description => "Converts from Millimeters to Pixels";
        public override string Formula => "Pixels = Resolution / Display * Millimeters";

        public MillimetersToPixels(Size resolution, SizeF display, decimal millimeters = 1)
        {
            ResolutionX = (uint) resolution.Width;
            ResolutionY = (uint) resolution.Height;
            DisplayWidth = (decimal) display.Width;
            DisplayHeight = (decimal) display.Height;
            Millimeters = millimeters;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PixelsPerMillimeterX), nameof(PixelsX))]
        public partial uint ResolutionX { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PixelsPerMillimeterY), nameof(PixelsY))]
        public partial uint ResolutionY { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PixelsPerMillimeterX), nameof(PixelsX))]
        public partial decimal DisplayWidth { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PixelsPerMillimeterY), nameof(PixelsY))]
        public partial decimal DisplayHeight { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PixelsX), nameof(PixelsY))]
        public partial decimal Millimeters { get; set; } = 1;

        public decimal PixelsPerMillimeterX => DisplayWidth > 0 ? Math.Round(ResolutionX / DisplayWidth, 2) : 0;
        public decimal PixelsPerMillimeterY => DisplayHeight > 0 ? Math.Round(ResolutionY / DisplayHeight, 2) : 0;

        public decimal PixelsX => Math.Round(PixelsPerMillimeterX * Millimeters, 2);
        public decimal PixelsY => Math.Round(PixelsPerMillimeterY * Millimeters, 2);


    }

    public sealed partial class LightOffDelayC : Calculation
    {
        public override string Description =>
            "Calculates the required light-off delay (Moving time from the build plate + additional time for resin to stabilize) given the lifting height, speed and retract to wait x seconds before cure a new layer.\n" +
            "Light-off delay is crucial for gaining higher-resolution and sharper prints.\n" +
            "When the build plate retracts, it is important to allow enough time for the resin to stabilize before the UV lights turn on. This would ideally be around 2-3s.";

        public override string Formula => "Light-off delay = Lifting height / (Lifting speed / 60) + Lifting height / (Retract speed / 60) + Desired wait seconds";

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LightOffDelay))]
        public partial decimal LiftHeight { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BottomLightOffDelay))]
        public partial decimal BottomLiftHeight { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LightOffDelay))]
        public partial decimal LiftSpeed { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BottomLightOffDelay))]
        public partial decimal BottomLiftSpeed { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LightOffDelay))]
        public partial decimal RetractSpeed { get; set; }

        partial void OnRetractSpeedChanged(decimal value) => BottomRetractSpeed = value;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BottomLightOffDelay))]
        public partial decimal BottomRetractSpeed { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(LightOffDelay))]
        public partial decimal WaitTime { get; set; } = 2.5m;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(BottomLightOffDelay))]
        public partial decimal BottomWaitTime { get; set; } = 3m;

        public decimal LightOffDelay => CalculateSeconds(LiftHeight, LiftSpeed, RetractSpeed, WaitTime);

        public decimal BottomLightOffDelay => CalculateSeconds(BottomLiftHeight, BottomLiftSpeed, BottomRetractSpeed, BottomWaitTime);

        public LightOffDelayC()
        {
        }

        public LightOffDelayC(decimal liftHeight, decimal bottomLiftHeight, decimal liftSpeed, decimal bottomLiftSpeed, decimal retractSpeed, decimal bottomRetractSpeed, decimal waitTime = 2.5m, decimal bottomWaitTime = 3m)
        {
            LiftHeight = liftHeight;
            BottomLiftHeight = bottomLiftHeight;
            LiftSpeed = liftSpeed;
            BottomLiftSpeed = bottomLiftSpeed;
            RetractSpeed = retractSpeed;
            BottomRetractSpeed = bottomRetractSpeed;
            WaitTime = waitTime;
            BottomWaitTime = bottomWaitTime;
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

            return MathF.Round(time, 2);

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
            if (liftHeight > 0 && liftSpeed > 0) time += MathF.Round(liftHeight / (liftSpeed / 60f) + extraWaitTime, 2);
            if (liftHeight2 > 0 && liftSpeed2 > 0) time += MathF.Round(liftHeight2 / (liftSpeed2 / 60f) + extraWaitTime, 2);
            return time;
        }

        public static uint CalculateMillisecondsLiftOnly(float liftHeight, float liftSpeed, float liftHeight2 = 0, float liftSpeed2 = 0, float extraWaitTime = 0) =>
            (uint)(CalculateSecondsLiftOnly(liftHeight, liftSpeed, liftHeight2, liftSpeed2, extraWaitTime) * 1000);

        public static float CalculateSecondsLiftOnly(Layer layer, float extraWaitTime = 0) =>
            CalculateSecondsLiftOnly(layer.LiftHeight, layer.LiftSpeed, layer.LiftHeight2, layer.LiftSpeed2, extraWaitTime);

        public static uint CalculateMillisecondsLiftOnly(Layer layer, float extraWaitTime = 0) =>
            (uint)(CalculateSecondsLiftOnly(layer.LiftHeight, layer.LiftSpeed, layer.LiftHeight2, layer.LiftSpeed2, extraWaitTime) * 1000);

    }

    public sealed partial class OptimalModelTilt : Calculation
    {
        public override string Description => "Calculates the optimal model tilt angle for printing and to minimize the visual layer effect.";
        public override string Formula => "Angleº = arctan(Layer height / XYResolution) * (180 / PI)";

        public OptimalModelTilt(Size resolution, SizeF display, decimal layerHeight = 0.05m)
        {
            ResolutionX = (uint)resolution.Width;
            ResolutionY = (uint)resolution.Height;
            DisplayWidth = (decimal)display.Width;
            DisplayHeight = (decimal)display.Height;
            LayerHeight = layerHeight;
        }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(XYResolution), nameof(XYResolutionUm), nameof(TiltAngleDegrees))]
        public partial uint ResolutionX { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(XYResolution), nameof(XYResolutionUm), nameof(TiltAngleDegrees))]
        public partial uint ResolutionY { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(XYResolution), nameof(XYResolutionUm), nameof(TiltAngleDegrees))]
        public partial decimal DisplayWidth { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(XYResolution), nameof(XYResolutionUm), nameof(TiltAngleDegrees))]
        public partial decimal DisplayHeight { get; set; }

        public decimal LayerHeight
        {
            get;
            set
            {
                if (!SetProperty(ref field, Layer.RoundHeight(value))) return;
                OnPropertyChanged(nameof(XYResolution));
                OnPropertyChanged(nameof(XYResolutionUm));
                OnPropertyChanged(nameof(TiltAngleDegrees));
            }
        } = 0.05m;

        public decimal XYResolution => DisplayWidth > 0 || DisplayHeight > 0 ?
            Math.Max(
                DisplayWidth / ResolutionX,
                DisplayHeight / ResolutionY
            )
            : 0;

        public decimal XYResolutionUm => Math.Round(XYResolution * 1000, 2);

        public decimal TiltAngleDegrees =>
            XYResolution > 0 ? (decimal) Math.Round(Math.Atan((double) (LayerHeight / XYResolution)) * (180 / Math.PI), 3) : 0;

    }
}
