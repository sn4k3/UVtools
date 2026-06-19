/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using EmguExtensions;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;


#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public partial class OperationStirResin : Operation, IEquatable<OperationStirResin>
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
#pragma warning restore CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
{
    #region Enums
    public enum StirMethod
    {
        [Description("Stir only: Modify the file to only stir the resin")]
        StirOnly,
        [Description("Stir and print: Stir the resin and then continue to print the present model on the file")]
        StirAndPrint
    }
    #endregion

    #region Overrides
    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override bool CanROI => false;
    public override bool CanMask => false;
    public override bool CanCancel => false;

    public override string IconClass => "Blender";
    public override string Title => "Stir resin";
    public override string Description =>
        "Stir the resin in the VAT by moving the build plate up and down multiple times.\n" +
        "This will modify the file and insert new layers at beginning of a print sequence to stir the resin multiple times using the build plate to perform multiple lifts and retracts in the bottom of the VAT.\n" +
        "Note: Keep in mind this method will add print time, but also, manual stir with a spatula or in the bottle yields better stirring.";

    public override string ConfirmationText =>
        $"modify the file to stir the resin in the VAT using the method {Method} at print start?";

    public override string ProgressTitle =>
        $"Modifying the file to stir the resin in the VAT using the method {Method}";

    public override string ProgressAction => "Processed layers";

    /*public override string? ValidateSpawn()
    {
        if (SlicerFile.Count >= 2 && SlicerFile[0].PositionZ == SlicerFile[1].PositionZ)
        {
            return $"This file contains layers with same positions in it's beginning.\n" +
                   $"If you ran this tool before, you can't run again, to do so, remove all stirring layers first.";
        }

        return base.ValidateSpawn();
    }*/

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (Stirs <= 0)
        {
            sb.AppendLine("The number of stirs must be greater than 0.");
        }

        if (!SlicerFile.CanUseSameLayerPositionZ)
        {
            var lastStirPosition = Layer.RoundHeight(Stirs * Layer.HeightPrecisionIncrementFloat);
            if (lastStirPosition >= (float)LiftHeight)
            {
                var maxStirs = (int)(LiftHeight / Layer.HeightPrecisionIncrement - 1);
                sb.AppendLine($"Your printer and/or file format does not support layers in same position, as so, each stir will increment in height by {Layer.HeightPrecisionIncrementFloat}mm.");
                sb.AppendLine($"> Your current settings ({Stirs} stirs x {Layer.HeightPrecisionIncrementFloat}mm = {lastStirPosition}mm) is equal or greater than the defined lift height of {LiftHeight}mm.");
                sb.AppendLine($"> As the lift height should be above resin level, all consequent stirs are useless.");
                sb.AppendLine($"> Please lower your stir number up to {maxStirs} stirs to optimize the process and to be below the lift height.");
            }
        }

        if (Method == StirMethod.StirAndPrint)
        {
            if (!SlicerFile.CanUseSameLayerPositionZ)
            {
                sb.AppendLine($"Your printer and/or file format is not compatible with the method {Method}. Please select another method.");
            }
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[{Method} {Stirs}x] [Wait: {WaitTimeBeforeStir}s/{WaitTimeAfterLift}s] [Lift: {LiftHeight}mm @ {LiftSpeed}mm/min] [Retract: {RetractSpeed}mm/min]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Constructor

    public OperationStirResin() { }

    public OperationStirResin(FileFormat slicerFile) : base(slicerFile) { }

    public override void InitWithSlicerFile()
    {
        if (ExposureTime == 0) ExposureTime = SlicerFile.SupportGCode ? 0 : 0.01m;
        if (LiftSpeed == 0) LiftSpeed = (decimal)SlicerFile.MaximumSpeed;
        if (RetractSpeed == 0) RetractSpeed = LiftSpeed;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the method to use to stir the resin
    /// </summary>
    [ObservableProperty]
    public partial StirMethod Method { get; set; } = StirMethod.StirOnly;

    /// <summary>
    /// Gets or sets the number of stirs to perform
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(StirSeconds))]
    [NotifyPropertyChangedFor(nameof(StirTimeString))]
    public partial ushort Stirs { get; set; } = 10;

    /// <summary>
    /// True to output a dummy pixel on bounding rectangle position to avoid empty layer and blank image, otherwise set to false
    /// </summary>
    [ObservableProperty]
    public partial bool OutputDummyPixel { get; set; } = true;

    /// <summary>
    /// Gets or sets the exposure time in seconds (Keep this to very low value)
    /// </summary>
    public decimal ExposureTime
    {
        get;
        set
        {
            if (!SetProperty(ref field, Math.Round(Math.Max(0, value), 3))) return;
            OnPropertyChanged(nameof(StirSeconds));
            OnPropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets or sets the lift height in mm between stirs
    /// </summary>
    public decimal LiftHeight
    {
        get;
        set
        {
            if (!SetProperty(ref field, Math.Round(Math.Max(0, value), 2))) return;
            OnPropertyChanged(nameof(StirSeconds));
            OnPropertyChanged(nameof(StirTimeString));
        }
    } = 45;

    /// <summary>
    /// Gets or sets the lift speed in mm/min
    /// </summary>
    public decimal LiftSpeed
    {
        get;
        set
        {
            if (!SetProperty(ref field, Math.Round(Math.Max(0, value), 2))) return;
            OnPropertyChanged(nameof(StirSeconds));
            OnPropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets or sets the retract speed in mm/min
    /// </summary>
    public decimal RetractSpeed
    {
        get;
        set
        {
            if (!SetProperty(ref field, Math.Round(Math.Max(0, value), 2))) return;
            OnPropertyChanged(nameof(StirSeconds));
            OnPropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets or sets the wait time in seconds before stirring
    /// </summary>
    public decimal WaitTimeBeforeStir
    {
        get;
        set
        {
            if (!SetProperty(ref field, Math.Round(Math.Max(0, value), 2))) return;
            OnPropertyChanged(nameof(StirSeconds));
            OnPropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets or sets the wait time in seconds after lift / before retract
    /// </summary>
    public decimal WaitTimeAfterLift
    {
        get;
        set
        {
            if (!SetProperty(ref field, Math.Round(Math.Max(0, value), 2))) return;
            OnPropertyChanged(nameof(StirSeconds));
            OnPropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets the total time in seconds to stir the resin
    /// </summary>
    public decimal StirSeconds => OperationCalculator.LightOffDelayC.CalculateSeconds(LiftHeight, LiftSpeed, RetractSpeed, ExposureTime + WaitTimeBeforeStir + WaitTimeAfterLift) * Stirs;

    /// <summary>
    /// Gets the total time in readable string to stir the resin
    /// </summary>
    public string StirTimeString => TimeSpan.FromSeconds((double)StirSeconds).ToTimeString();

    #endregion

    #region Equality

    public bool Equals(OperationStirResin? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Method == other.Method && Stirs == other.Stirs && OutputDummyPixel == other.OutputDummyPixel &&
               ExposureTime == other.ExposureTime && LiftHeight == other.LiftHeight &&
               LiftSpeed == other.LiftSpeed && RetractSpeed == other.RetractSpeed &&
               WaitTimeBeforeStir == other.WaitTimeBeforeStir && WaitTimeAfterLift == other.WaitTimeAfterLift;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationStirResin)obj);
    }

    public static bool operator ==(OperationStirResin? left, OperationStirResin? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(OperationStirResin? left, OperationStirResin? right)
    {
        return !Equals(left, right);
    }

    #endregion

    #region Methods

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        SlicerFile.SuppressRebuildPropertiesWork(() =>
        {
            using var mat = OutputDummyPixel ? SlicerFile.CreateMatWithDummyPixelFromLayer(0) : SlicerFile.CreateMat();
            var layer = new Layer(mat, SlicerFile)
            {
                PositionZ = Layer.HeightPrecisionIncrementFloat,
                LiftHeightTotal = (float)LiftHeight,
                LiftSpeed = (float)LiftSpeed,
                RetractSpeed = (float)RetractSpeed,
                WaitTimeAfterCure = 0,
                WaitTimeAfterLift = (float)WaitTimeAfterLift,
                LightPWM = SlicerFile.SupportGCode ? byte.MinValue : (byte)1,
                ExposureTime = (float)ExposureTime,
            };

            layer.SetWaitTimeBeforeCureOrLightOffDelay((float)WaitTimeBeforeStir);

            var stirLayers = layer.Clone(Stirs);

            switch (Method)
            {
                case StirMethod.StirOnly:
                {
                    SlicerFile.LayerHeight = Layer.HeightPrecisionIncrementFloat;
                    SlicerFile.BottomLayerCount = 1;

                    SlicerFile.BottomLiftHeightTotal = (float)LiftHeight;
                    SlicerFile.LiftHeightTotal = (float)LiftHeight;

                    SlicerFile.BottomLiftSpeed = (float)LiftSpeed;
                    SlicerFile.LiftSpeed = (float)LiftSpeed;

                    SlicerFile.BottomRetractSpeed = (float)RetractSpeed;
                    SlicerFile.RetractSpeed = (float)RetractSpeed;

                    SlicerFile.SetBottomWaitTimeBeforeCureOrLightOffDelay((float)WaitTimeBeforeStir);
                    SlicerFile.SetNormalWaitTimeBeforeCureOrLightOffDelay((float)WaitTimeBeforeStir);

                    SlicerFile.BottomWaitTimeAfterCure = 0;
                    SlicerFile.WaitTimeAfterCure = 0;

                    SlicerFile.BottomWaitTimeAfterLift = (float)WaitTimeAfterLift;
                    SlicerFile.WaitTimeAfterLift = (float)WaitTimeAfterLift;

                    SlicerFile.LightPWM = SlicerFile.BottomLightPWM = SlicerFile.SupportGCode ? byte.MinValue : (byte)1;
                    SlicerFile.ExposureTime = (float)ExposureTime;

                    if (!SlicerFile.CanUseSameLayerPositionZ)
                    {
                        // Fix positions
                        for (uint i = 1; i < stirLayers.Length; i++)
                        {
                            stirLayers[i].PositionZ = SlicerFile.CalculatePositionZ(i);
                        }
                    }

                    SlicerFile.Layers = stirLayers;

                    if (SlicerFile.ThumbnailsCount > 0)
                    {
                        using var thumbnail = EmguCvExtensions.InitMat(new Size(400, 200), 3);
                        var fontFace = FontFace.HersheyDuplex;
                        var fontScale = 1;
                        var fontThickness = 2;
                        const byte xSpacing = 45;
                        const byte ySpacing = 45;
                        CvInvoke.PutText(thumbnail, "UVtools", new Point(140, 35), fontFace, fontScale, new MCvScalar(255, 27, 245), fontThickness + 1);
                        CvInvoke.Line(thumbnail, new Point(xSpacing, 0), new Point(xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
                        CvInvoke.Line(thumbnail, new Point(xSpacing, ySpacing + 5), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
                        CvInvoke.Line(thumbnail, new Point(thumbnail.Width - xSpacing, 0), new Point(thumbnail.Width - xSpacing, ySpacing + 5), new MCvScalar(255, 27, 245), 3);
                        CvInvoke.PutText(thumbnail, "Stir Resin", new Point(xSpacing, ySpacing * 2 + 20), fontFace, fontScale*2, new MCvScalar(0, 255, 255), fontThickness);
                        CvInvoke.PutText(thumbnail, $"{Stirs}x", new Point(xSpacing, ySpacing * 4), fontFace, fontScale*2, EmguCvExtensions.WhiteColor, fontThickness);

                        SlicerFile.SetThumbnails(thumbnail);
                    }

                    break;
                }
                case StirMethod.StirAndPrint:
                {
                    var layers = SlicerFile.ToList();

                    // Try to remove dummy layers, ie previous stir layers
                    for (int i = 0; i < layers.Count && layers[i].IsDummy; i++)
                    {
                        layers.RemoveAt(i);
                        i--;
                    }

                    layers.InsertRange(0, stirLayers);

                    SlicerFile.Layers = layers.ToArray();
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(Method), Method, null);
            }
        });



        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}
