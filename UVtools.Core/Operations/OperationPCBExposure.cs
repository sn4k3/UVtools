/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Text;
using Emgu.CV;
using Emgu.CV.CvEnum;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Gerber;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;

[Serializable]
public class OperationPCBExposure : Operation
{
    #region Enum

    public enum LithophaneBaseType : byte
    {
        None,
        Square,
        Model
    }

    #endregion

    #region Members
    private string? _filePath;
    
    private decimal _layerHeight;
    private decimal _exposureTime;
    private bool _mirror;
    private bool _invertColor;
    private bool _enableAntiAliasing;

    #endregion

    #region Overrides

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "fas fa-microchip";
    public override string Title => "PCB exposure";
    public override string Description =>
        "Converts a gerber file to a pixel perfect image given your printer LCD/resolution to exposure the copper traces.\n" +
        "Note: The current opened file will be overwritten with this gerber image, use a dummy or a not needed file.";

    public override string ConfirmationText =>
        "generate the PCB traces?";

    public override string ProgressTitle =>
        "Generating PCB traces";

    public override string ProgressAction => "Tracing";

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();
        if (string.IsNullOrWhiteSpace(_filePath))
        {
            sb.AppendLine("The input file is empty");
        }
        else if(!File.Exists(_filePath))
        {
            sb.AppendLine("The input file does not exists");
        }
        
        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"{(FileExists ? $"{Path.GetFileName(_filePath)} [Exposure: {_exposureTime}s] [Invert: {_invertColor}]" : $"[Exposure: {_exposureTime}s] [Invert: {_invertColor}]")}";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }
    #endregion

    #region Constructor

    public OperationPCBExposure() { }

    public OperationPCBExposure(FileFormat slicerFile) : base(slicerFile)
    {
        if (_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if (_exposureTime <= 0) _exposureTime = (decimal)SlicerFile.BottomExposureTime;
        _mirror = SlicerFile.DisplayMirror != FlipDirection.None;
    }

    #endregion

    #region Properties
    public string? FilePath
    {
        get => _filePath;
        set => RaiseAndSetIfChanged(ref _filePath, value);
    }
    public bool FileExists => !string.IsNullOrWhiteSpace(_filePath) && File.Exists(_filePath);

    public decimal LayerHeight
    {
        get => _layerHeight;
        set => RaiseAndSetIfChanged(ref _layerHeight, Layer.RoundHeight(value));
    }

    public decimal ExposureTime
    {
        get => _exposureTime;
        set => RaiseAndSetIfChanged(ref _exposureTime, Math.Round(value, 2));
    }
    
    public bool Mirror
    {
        get => _mirror;
        set => RaiseAndSetIfChanged(ref _mirror, value);
    }

    public bool InvertColor
    {
        get => _invertColor;
        set => RaiseAndSetIfChanged(ref _invertColor, value);
    }

    public bool EnableAntiAliasing
    {
        get => _enableAntiAliasing;
        set => RaiseAndSetIfChanged(ref _enableAntiAliasing, value);
    }

    #endregion

    #region Equality

    protected bool Equals(OperationPCBExposure other)
    {
        return _filePath == other._filePath && _layerHeight == other._layerHeight && _exposureTime == other._exposureTime && _mirror == other._mirror && _invertColor == other._invertColor && _enableAntiAliasing == other._enableAntiAliasing;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((OperationPCBExposure) obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_filePath, _layerHeight, _exposureTime, _mirror, _invertColor, _enableAntiAliasing);
    }

    #endregion

    #region Methods

    public Mat GetMat()
    {
        var mat = SlicerFile.CreateMat();
        if (!FileExists) return mat;
        GerberDocument.ParseAndDraw(_filePath!, mat, _enableAntiAliasing);

        //var boundingRectangle = CvInvoke.BoundingRectangle(mat);
        //var cropped = mat.Roi(new Size(boundingRectangle.Right, boundingRectangle.Bottom));
        var cropped = mat.CropByBounds();

        if (_invertColor) CvInvoke.BitwiseNot(cropped, cropped);
        if (_mirror)
        {
            var flip = SlicerFile.DisplayMirror;
            if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;
            CvInvoke.Flip(cropped, cropped, (FlipType)flip);
        }

        return mat;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        using var mat = GetMat();
        var layer = new Layer(mat, SlicerFile);
        layer.SetNoDelays();

        SlicerFile.SuppressRebuildPropertiesWork(() =>
        {
            SlicerFile.LayerHeight = (float) _layerHeight;
            SlicerFile.BottomLayerCount = 1;
            SlicerFile.BottomExposureTime = (float) _exposureTime;
            SlicerFile.ExposureTime = (float)_exposureTime;
            SlicerFile.LiftHeightTotal = 0;
            SlicerFile.SetNoDelays();

            SlicerFile.Layers = new[] { layer };
        }, true);


        using var croppedMat = mat.CropByBounds(20);
        using var bgrMat = new Mat();
        CvInvoke.CvtColor(croppedMat, bgrMat, ColorConversion.Gray2Bgr);
        SlicerFile.SetThumbnails(bgrMat);

        return !progress.Token.IsCancellationRequested;
    }


    #endregion
}