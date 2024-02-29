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
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;


public class OperationStirResin : Operation, IEquatable<OperationStirResin>
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

    public override string? ValidateSpawn()
    {
        if (SlicerFile.Count >= 2 && SlicerFile[0].PositionZ == SlicerFile[1].PositionZ)
        {
            return $"This file contains layers with same positions in it's beginning.\n" +
                   $"If you ran this tool before, you can't run again, to do so, remove all stirring layers first.";
        }

        return base.ValidateSpawn();
    }

    public override string? ValidateInternally()
    {
        if (_stirs <= 0)
        {
            return "The number of stirs must be greater than 0.";
        }

        if (_method == StirMethod.StirAndPrint)
        {
            if (!SlicerFile.CanUseSameLayerPositionZ)
            {
                return $"Your printer and/or file format is not compatible with the method {_method}.\n" +
                       $"Please select another method.";
            }
        }

        return null;
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
    /// Gets or sets the lift height in mm between stirs
    /// </summary>
    public decimal LiftHeight
    {
        get => _liftHeight;
        set
        {
            if (!RaiseAndSetIfChanged(ref _liftHeight, value)) return;
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
            if (!RaiseAndSetIfChanged(ref _liftSpeed, Math.Round(value, 2))) return;
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
            if (!RaiseAndSetIfChanged(ref _retractSpeed, Math.Round(value, 2))) return;
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
            if (!RaiseAndSetIfChanged(ref _waitTimeBeforeStir, Math.Round(value, 2))) return;
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
            if (!RaiseAndSetIfChanged(ref _waitTimeAfterLift, Math.Round(value, 2))) return;
            RaisePropertyChanged(nameof(StirSeconds));
            RaisePropertyChanged(nameof(StirTimeString));
        }
    }

    /// <summary>
    /// Gets the total time in seconds to stir the resin
    /// </summary>
    public decimal StirSeconds => OperationCalculator.LightOffDelayC.CalculateSeconds(_liftHeight, _liftSpeed, _retractSpeed, _waitTimeBeforeStir + _waitTimeAfterLift) * _stirs;

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
        return _method == other._method && _stirs == other._stirs && _liftHeight == other._liftHeight && _liftSpeed == other._liftSpeed && _retractSpeed == other._retractSpeed && _waitTimeBeforeStir == other._waitTimeBeforeStir && _waitTimeAfterLift == other._waitTimeAfterLift;
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
            using var mat = SlicerFile.CreateMatWithDummyPixelFromLayer(0);
            var layer = new Layer(mat, SlicerFile)
            {
                PositionZ = Layer.HeightPrecisionIncrementFloat,
                LiftHeightTotal = (float)_liftHeight,
                LiftSpeed = (float)_liftSpeed,
                RetractSpeed = (float)_retractSpeed,
                WaitTimeAfterCure = 0,
                WaitTimeAfterLift = (float)_waitTimeAfterLift,
                LightPWM = SlicerFile.SupportGCode ? byte.MinValue : (byte)1,
                ExposureTime = SlicerFile.BottomExposureTime = SlicerFile.SupportGCode ? 0 : 0.01f,
            };

            layer.SetWaitTimeBeforeCureOrLightOffDelay((float)_waitTimeBeforeStir);

            var layers = layer.Clone(_stirs);

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
                    SlicerFile.ExposureTime = SlicerFile.BottomExposureTime = SlicerFile.SupportGCode ? 0 : 0.01f;

                    SlicerFile.Layers = layers;


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
                    SlicerFile.InsertRange(0, layers);
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