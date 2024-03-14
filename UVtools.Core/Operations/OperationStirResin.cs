/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;


#pragma warning disable CS0661 // Type defines operator == or operator != but does not override Object.GetHashCode()
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
public class OperationStirResin : Operation, IEquatable<OperationStirResin>
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

    #region Members
    private StirMethod _method = StirMethod.StirOnly;
    private ushort _stirs = 10;
    private bool _outputDummyPixel = true;
    private decimal _exposureTime;
    private decimal _liftHeight = 45;
    private decimal _liftSpeed;
    private decimal _retractSpeed;
    private decimal _waitTimeBeforeStir;
    private decimal _waitTimeAfterLift;
    
    #endregion

    #region Overrides
    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override bool CanROI => false;
    public override bool CanMask => false;

    public override bool CanCancel => false;

    public override string IconClass => "fa-solid fa-blender";
    public override string Title => "Stir resin";
    public override string Description =>
        "Stir the resin in the VAT by moving the build plate up and down multiple times.\n" +
        "This will modify the file and insert new layers at beginning of a print sequence to stir the resin multiple times using the build plate to perform multiple lifts and retracts in the bottom of the VAT.\n" +
        "Note: Keep in mind this method will add print time, but also, manual stir with a spatula or in the bottle yields better stirring.";

    public override string ConfirmationText =>
        $"modify the file to stir the resin in the VAT using the method {_method} at print start?";

    public override string ProgressTitle =>
        $"Modifying the file to stir the resin in the VAT using the method {_method}";

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

        if (_stirs <= 0)
        {
            sb.AppendLine("The number of stirs must be greater than 0.");
        }

        if (!SlicerFile.CanUseSameLayerPositionZ)
        {
            var lastStirPosition = Layer.RoundHeight(_stirs * Layer.HeightPrecisionIncrementFloat);
            if (lastStirPosition >= (float)_liftHeight)
            {
                var maxStirs = (int)(_liftHeight / Layer.HeightPrecisionIncrement - 1);
                sb.AppendLine($"Your printer and/or file format does not support layers in same position, as so, each stir will increment in height by {Layer.HeightPrecisionIncrementFloat}mm.");
                sb.AppendLine($"> Your current settings ({_stirs} stirs x {Layer.HeightPrecisionIncrementFloat}mm = {lastStirPosition}mm) is equal or greater than the defined lift height of {_liftHeight}mm.");
                sb.AppendLine($"> As the lift height should be above resin level, all consequent stirs are useless.");
                sb.AppendLine($"> Please lower your stir number up to {maxStirs} stirs to optimize the process and to be below the lift height.");
            }
        }

        if (_method == StirMethod.StirAndPrint)
        {
            if (!SlicerFile.CanUseSameLayerPositionZ)
            {
                sb.AppendLine($"Your printer and/or file format is not compatible with the method {_method}. Please select another method.");
            }
        }

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[{_method} {_stirs}x] [Wait: {_waitTimeBeforeStir}s/{_waitTimeAfterLift}s] [Lift: {_liftHeight}mm @ {_liftSpeed}mm/min] [Retract: {_retractSpeed}mm/min]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Constructor

    public OperationStirResin() { }

    public OperationStirResin(FileFormat slicerFile) : base(slicerFile) { }

    public override void InitWithSlicerFile()
    {
        if (_exposureTime == 0) _exposureTime = SlicerFile.SupportGCode ? 0 : 0.01m;
        if (_liftSpeed == 0) _liftSpeed = (decimal)SlicerFile.MaximumSpeed;
        if (_retractSpeed == 0) _retractSpeed = _liftSpeed;
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the method to use to stir the resin
    /// </summary>
    public StirMethod Method
    {
        get => _method;
        set => RaiseAndSetIfChanged(ref _method, value);
    }

    /// <summary>
    /// Gets or sets the number of stirs to perform
    /// </summary>
    public ushort Stirs
    {
        get => _stirs;
        set
        {
            if(!RaiseAndSetIfChanged(ref _stirs, value)) return;
            RaisePropertyChanged(nameof(StirSeconds));
            RaisePropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// True to output a dummy pixel on bounding rectangle position to avoid empty layer and blank image, otherwise set to false
    /// </summary>
    public bool OutputDummyPixel
    {
        get => _outputDummyPixel;
        set => RaiseAndSetIfChanged(ref _outputDummyPixel, value);
    }

    /// <summary>
    /// Gets or sets the exposure time in seconds (Keep this to very low value)
    /// </summary>
    public decimal ExposureTime
    {
        get => _exposureTime;
        set
        {
            if (!RaiseAndSetIfChanged(ref _exposureTime, Math.Round(Math.Max(0, value), 3))) return;
            RaisePropertyChanged(nameof(StirSeconds));
            RaisePropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets or sets the lift height in mm between stirs
    /// </summary>
    public decimal LiftHeight
    {
        get => _liftHeight;
        set
        {
            if (!RaiseAndSetIfChanged(ref _liftHeight, Math.Round(Math.Max(0, value), 2))) return;
            RaisePropertyChanged(nameof(StirSeconds));
            RaisePropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets or sets the lift speed in mm/min
    /// </summary>
    public decimal LiftSpeed
    {
        get => _liftSpeed;
        set
        {
            if (!RaiseAndSetIfChanged(ref _liftSpeed, Math.Round(Math.Max(0, value), 2))) return;
            RaisePropertyChanged(nameof(StirSeconds));
            RaisePropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets or sets the retract speed in mm/min
    /// </summary>
    public decimal RetractSpeed
    {
        get => _retractSpeed;
        set
        {
            if (!RaiseAndSetIfChanged(ref _retractSpeed, Math.Round(Math.Max(0, value), 2))) return;
            RaisePropertyChanged(nameof(StirSeconds));
            RaisePropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets or sets the wait time in seconds before stirring
    /// </summary>
    public decimal WaitTimeBeforeStir
    {
        get => _waitTimeBeforeStir;
        set
        {
            if (!RaiseAndSetIfChanged(ref _waitTimeBeforeStir, Math.Round(Math.Max(0, value), 2))) return;
            RaisePropertyChanged(nameof(StirSeconds));
            RaisePropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets or sets the wait time in seconds after lift / before retract
    /// </summary>
    public decimal WaitTimeAfterLift
    {
        get => _waitTimeAfterLift;
        set
        {
            if (!RaiseAndSetIfChanged(ref _waitTimeAfterLift, Math.Round(Math.Max(0, value), 2))) return;
            RaisePropertyChanged(nameof(StirSeconds));
            RaisePropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets the total time in seconds to stir the resin
    /// </summary>
    public decimal StirSeconds => OperationCalculator.LightOffDelayC.CalculateSeconds(_liftHeight, _liftSpeed, _retractSpeed, _exposureTime + _waitTimeBeforeStir + _waitTimeAfterLift) * _stirs;

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
        return _method == other._method && _stirs == other._stirs && _outputDummyPixel == other._outputDummyPixel && _exposureTime == other._exposureTime && _liftHeight == other._liftHeight && _liftSpeed == other._liftSpeed && _retractSpeed == other._retractSpeed && _waitTimeBeforeStir == other._waitTimeBeforeStir && _waitTimeAfterLift == other._waitTimeAfterLift;
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
            using var mat = _outputDummyPixel ? SlicerFile.CreateMatWithDummyPixelFromLayer(0) : SlicerFile.CreateMat();
            var layer = new Layer(mat, SlicerFile)
            {
                PositionZ = Layer.HeightPrecisionIncrementFloat,
                LiftHeightTotal = (float)_liftHeight,
                LiftSpeed = (float)_liftSpeed,
                RetractSpeed = (float)_retractSpeed,
                WaitTimeAfterCure = 0,
                WaitTimeAfterLift = (float)_waitTimeAfterLift,
                LightPWM = SlicerFile.SupportGCode ? byte.MinValue : (byte)1,
                ExposureTime = (float)_exposureTime,
            };

            layer.SetWaitTimeBeforeCureOrLightOffDelay((float)_waitTimeBeforeStir);

            var stirLayers = layer.Clone(_stirs);

            switch (_method)
            {
                case StirMethod.StirOnly:
                {
                    SlicerFile.LayerHeight = Layer.HeightPrecisionIncrementFloat;
                    SlicerFile.BottomLayerCount = 1;

                    SlicerFile.BottomLiftHeightTotal = (float)_liftHeight;
                    SlicerFile.LiftHeightTotal = (float)_liftHeight;

                    SlicerFile.BottomLiftSpeed = (float)_liftSpeed;
                    SlicerFile.LiftSpeed = (float)_liftSpeed;

                    SlicerFile.BottomRetractSpeed = (float)_retractSpeed;
                    SlicerFile.RetractSpeed = (float)_retractSpeed;

                    SlicerFile.SetBottomWaitTimeBeforeCureOrLightOffDelay((float)_waitTimeBeforeStir);
                    SlicerFile.SetNormalWaitTimeBeforeCureOrLightOffDelay((float)_waitTimeBeforeStir);
                    
                    SlicerFile.BottomWaitTimeAfterCure = 0;
                    SlicerFile.WaitTimeAfterCure = 0;

                    SlicerFile.BottomWaitTimeAfterLift = (float)_waitTimeAfterLift;
                    SlicerFile.WaitTimeAfterLift = (float)_waitTimeAfterLift;

                    SlicerFile.LightPWM = SlicerFile.BottomLightPWM = SlicerFile.SupportGCode ? byte.MinValue : (byte)1;
                    SlicerFile.ExposureTime = (float)_exposureTime;

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
                        using var thumbnail = EmguExtensions.InitMat(new Size(400, 200), 3);
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
                        CvInvoke.PutText(thumbnail, $"{Stirs}x", new Point(xSpacing, ySpacing * 4), fontFace, fontScale*2, EmguExtensions.WhiteColor, fontThickness);

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
                    throw new ArgumentOutOfRangeException(nameof(Method), _method, null);
            }
        });



        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}