/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Operations;


public sealed class OperationCalibrateBloomingEffect : Operation
{
    #region Members
    private decimal _layerHeight;
    private ushort _bottomLayers = 3;
    private decimal _bottomExposure;
    private decimal _normalExposure;
    private ushort _leftRightMargin = 200;
    private ushort _topBottomMargin = 200;
    private decimal _waitTimeBeforeCureStart;
    private decimal _waitTimeBeforeCureIncrement = 1;
    private byte _objectCount = 15;
    private ushort _objectDiameter = 400;
    private decimal _objectHeight = 5;
    private ushort _objectMargin = 20;
    private bool _mirrorOutput;

    #endregion

    #region Overrides

    public override bool CanROI => false;

    public override bool CanCancel => false;

    public override LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.None;
    public override string IconClass => "fa-solid fa-sun";
    public override string Title => "Blooming effect";
    public override string Description =>
        "Generates test models with various strategies and increments to measure the blooming effect.\n" +
        "You must repeat this test when change any of the following: printer, LEDs, resin and exposure times.\n" +
        "Note: The current opened file will be overwritten with this test, use a dummy or a not needed file.";

    public override string ConfirmationText =>
        $"generate the bloom effect test?";

    public override string ProgressTitle =>
        $"Generating the bloom effect test";

    public override string ProgressAction => "Generated";

    public override string? ValidateSpawn()
    {
        if (!SlicerFile.CanUseLayerPositionZ)
        {
            return $"{NotSupportedMessage}\nReason: Can't use more than one layer per same position.";
        }

        if (SlicerFile is {CanUseLayerWaitTimeBeforeCure: false, CanUseLayerLightOffDelay: false})
        {
            return $"{NotSupportedMessage}\nReason: No per layer wait time before cure capabilities.";
        }


        return null;
    }

    public override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        if (SlicerFile.ResolutionX - _leftRightMargin * 2 <= 0)
            sb.AppendLine("The top/bottom margin is too big, it overlaps the screen resolution.");
            
        if (SlicerFile.ResolutionY - _topBottomMargin * 2 <= 0)
            sb.AppendLine("The top/bottom margin is too big, it overlaps the screen resolution.");

        if (_leftRightMargin + _objectDiameter > SlicerFile.ResolutionX - _leftRightMargin)
            sb.AppendLine("The top/bottom margin or object diameter is too big, it overlaps the screen resolution.");

        if (_topBottomMargin + _objectDiameter > SlicerFile.ResolutionY - _topBottomMargin)
            sb.AppendLine("The top/bottom margin or object diameter is too big, it overlaps the screen resolution.");

        return sb.ToString();
    }

    public override string ToString()
    {
        var result = $"[Layer Height: {_layerHeight}] " +
                     $"[Bottom layers: {_bottomLayers}] " +
                     $"[Exposure: {_bottomExposure}/{_normalExposure}s]" +
                     $"[Wait time: {_waitTimeBeforeCureStart}s] [Increment: {_waitTimeBeforeCureIncrement}s] " +
                     $"[Objects: {_objectCount}] [Height: {_objectHeight}mm] [Diameter: {_objectDiameter}px]";
        if (!string.IsNullOrEmpty(ProfileName)) result = $"{ProfileName}: {result}";
        return result;
    }

    #endregion

    #region Properties

    public decimal LayerHeight
    {
        get => _layerHeight;
        set
        {
            if(!RaiseAndSetIfChanged(ref _layerHeight, Layer.RoundHeight(value))) return;
            RaisePropertyChanged(nameof(BottomHeight));
        }
    }

    public ushort BottomLayers
    {
        get => _bottomLayers;
        set => RaiseAndSetIfChanged(ref _bottomLayers, value);
    }

    public ushort Microns => (ushort) (LayerHeight * 1000);

    public decimal BottomHeight => Layer.RoundHeight(_layerHeight * _bottomLayers);

    public decimal BottomExposure
    {
        get => _bottomExposure;
        set => RaiseAndSetIfChanged(ref _bottomExposure, Math.Round(value, 2));
    }

    public decimal NormalExposure
    {
        get => _normalExposure;
        set => RaiseAndSetIfChanged(ref _normalExposure, Math.Round(value, 2));
    }

    public ushort LeftRightMargin
    {
        get => _leftRightMargin;
        set => RaiseAndSetIfChanged(ref _leftRightMargin, value);
    }

    public ushort MaxLeftRightMargin => (ushort)((SlicerFile.ResolutionX - 100) / 2);

    public ushort TopBottomMargin
    {
        get => _topBottomMargin;
        set => RaiseAndSetIfChanged(ref _topBottomMargin, value);
    }

    public ushort MaxTopBottomMargin => (ushort) ((SlicerFile.ResolutionY - 100) / 2);

    public decimal WaitTimeBeforeCureStart
    {
        get => _waitTimeBeforeCureStart;
        set => RaiseAndSetIfChanged(ref _waitTimeBeforeCureStart, Math.Round(Math.Max(0, value), 2));
    }

    public decimal WaitTimeBeforeCureIncrement
    {
        get => _waitTimeBeforeCureIncrement;
        set => RaiseAndSetIfChanged(ref _waitTimeBeforeCureIncrement, Math.Round(Math.Max(0.05m, value), 2));
    }

    public byte ObjectCount
    {
        get => _objectCount;
        set => RaiseAndSetIfChanged(ref _objectCount, Math.Max((byte)1, value));
    }

    public decimal MaximumWaiTimeBeforeCure => Math.Round(_waitTimeBeforeCureStart + _waitTimeBeforeCureIncrement * _objectCount, 2);

    public ushort ObjectDiameter
    {
        get => _objectDiameter;
        set => RaiseAndSetIfChanged(ref _objectDiameter, Math.Max((ushort)20, value));
    }

    public decimal ObjectHeight
    {
        get => _objectHeight;
        set => RaiseAndSetIfChanged(ref _objectHeight, value);
    }

    public ushort ObjectMargin
    {
        get => _objectMargin;
        set => RaiseAndSetIfChanged(ref _objectMargin, value);
    }

    public bool MirrorOutput
    {
        get => _mirrorOutput;
        set => RaiseAndSetIfChanged(ref _mirrorOutput, value);
    }

    #endregion

    #region Constructor

    public OperationCalibrateBloomingEffect() { }

    public OperationCalibrateBloomingEffect(FileFormat slicerFile) : base(slicerFile)
    { }

    public override void InitWithSlicerFile()
    {
        base.InitWithSlicerFile();
        if(_layerHeight <= 0) _layerHeight = (decimal)SlicerFile.LayerHeight;
        if(_bottomExposure <= 0) _bottomExposure = (decimal)SlicerFile.BottomExposureTime;
        if(_normalExposure <= 0) _normalExposure = (decimal)SlicerFile.ExposureTime;
        _mirrorOutput = SlicerFile.DisplayMirror != FlipDirection.None;
    }

    #endregion

    #region Equality


    private bool Equals(OperationCalibrateBloomingEffect other)
    {
        return _layerHeight == other._layerHeight && _bottomLayers == other._bottomLayers && _bottomExposure == other._bottomExposure && _normalExposure == other._normalExposure && _leftRightMargin == other._leftRightMargin && _topBottomMargin == other._topBottomMargin && _waitTimeBeforeCureStart == other._waitTimeBeforeCureStart && _waitTimeBeforeCureIncrement == other._waitTimeBeforeCureIncrement && _objectCount == other._objectCount && _objectDiameter == other._objectDiameter && _objectHeight == other._objectHeight && _objectMargin == other._objectMargin && _mirrorOutput == other._mirrorOutput;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is OperationCalibrateBloomingEffect other && Equals(other);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Gets the layers
    /// </summary>
    /// <returns></returns>
    public Mat GetLayerPreview()
    {
        var mat = SlicerFile.CreateMat();

        uint currentX = _leftRightMargin;
        uint currentY = _topBottomMargin;
        uint maxWidth = SlicerFile.ResolutionX - _leftRightMargin;
        uint maxHeight = SlicerFile.ResolutionY - _topBottomMargin;
        var maxWaitTime = MaximumWaiTimeBeforeCure;

        for (decimal waitTime = _waitTimeBeforeCureStart; waitTime <= maxWaitTime; waitTime += _waitTimeBeforeCureIncrement)
        {
            if (currentX + _objectDiameter > maxWidth)
            {
                currentX = _leftRightMargin;
                currentY += (uint)(_objectDiameter + _objectMargin);
                if (currentY + _objectDiameter > maxHeight) break;
            }

            waitTime = Math.Round(waitTime, 2);

            CvInvoke.Rectangle(mat, new Rectangle((int)currentX, (int)currentY, _objectDiameter, _objectDiameter), EmguExtensions.WhiteColor, -1);
            mat.PutTextExtended($"E: {_bottomExposure}s/{_normalExposure}s\nW: {waitTime}s", new Point((int) currentX+20, (int) currentY + _objectDiameter / 2), FontFace.HersheyDuplex, 2.0, EmguExtensions.BlackColor, 2, 10);
            currentX += (uint)(_objectDiameter + _objectMargin);
        }

        return mat;
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
        CvInvoke.PutText(thumbnail, "Bloom Effect Cal.", new Point(xSpacing, ySpacing * 2), fontFace, fontScale, new MCvScalar(0, 255, 255), fontThickness);
        CvInvoke.PutText(thumbnail, $"{Microns}um @ {BottomExposure}s/{NormalExposure}s", new Point(xSpacing, ySpacing * 3), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);
        CvInvoke.PutText(thumbnail, $"Wait: {_waitTimeBeforeCureStart}s/+{_waitTimeBeforeCureIncrement}s", new Point(xSpacing, ySpacing * 4), fontFace, fontScale, EmguExtensions.WhiteColor, fontThickness);
            
        return thumbnail;
    }

    protected override bool ExecuteInternally(OperationProgress progress)
    {
        progress.ItemCount = 1;
            
        var newLayers = new List<Layer>();

        if (SlicerFile.ThumbnailsCount > 0)
            SlicerFile.SetThumbnails(GetThumbnail());

        var maxWaitTime = MaximumWaiTimeBeforeCure;
        var flip = SlicerFile.DisplayMirror;
        if (flip == FlipDirection.None) flip = FlipDirection.Horizontally;

        SlicerFile.SuppressRebuildPropertiesWork(() =>
        {
            float currentPosition = (float)_layerHeight;
            SlicerFile.LayerHeight = currentPosition;
            SlicerFile.BottomExposureTime = (float)_bottomExposure;
            SlicerFile.ExposureTime = (float)_normalExposure;
            SlicerFile.BottomLayerCount = _bottomLayers;
            SlicerFile.TransitionLayerCount = 0;
            SlicerFile.BottomLightOffDelay = 0;
            SlicerFile.LightOffDelay = 0;
            SlicerFile.SetBottomWaitTimeBeforeCureOrLightOffDelay((float)_waitTimeBeforeCureStart);
            SlicerFile.SetNormalWaitTimeBeforeCureOrLightOffDelay((float)(_waitTimeBeforeCureStart + _waitTimeBeforeCureIncrement * _objectCount));


            uint currentX = _leftRightMargin;
            uint currentY = _topBottomMargin;
            uint maxWidth = SlicerFile.ResolutionX - _leftRightMargin;
            uint maxHeight = SlicerFile.ResolutionY - _topBottomMargin;

            for (decimal waitTime = _waitTimeBeforeCureStart; waitTime <= maxWaitTime; waitTime += _waitTimeBeforeCureIncrement)
            {
                if (currentX + _objectDiameter > maxWidth)
                {
                    currentX = _leftRightMargin;
                    currentY += (uint)(_objectDiameter + _objectMargin);
                    if(currentY + _objectDiameter > maxHeight) break;
                }

                waitTime = Math.Round(waitTime, 2);

                using var mat = SlicerFile.CreateMat();
                CvInvoke.Rectangle(mat, new Rectangle((int)currentX, (int)currentY, _objectDiameter, _objectDiameter), EmguExtensions.WhiteColor, -1);
                if (_mirrorOutput) CvInvoke.Flip(mat, mat, (FlipType)flip);
                var layer = new Layer(mat, SlicerFile)
                {
                    PositionZ = currentPosition
                };
                layer.SetWaitTimeBeforeCureOrLightOffDelay((float) waitTime);
                newLayers.Add(layer);

                currentX += (uint)(_objectDiameter + _objectMargin);
            }

            var objects = newLayers.Count;
            int heightLayers = (int)Math.Ceiling(_objectHeight / (decimal)SlicerFile.LayerHeight);

            for (int h = 0; h < heightLayers; h++)
            {
                currentPosition += (float)_layerHeight;
                for (int i = 0; i < objects; i++)
                {
                    var layer = newLayers[i].Clone();
                    layer.PositionZ = currentPosition;
                    newLayers.Add(layer);
                }
            }

            using var textMat = SlicerFile.CreateMat();
            currentX = _leftRightMargin;
            currentY = _topBottomMargin;
            for (decimal waitTime = _waitTimeBeforeCureStart; waitTime <= maxWaitTime; waitTime += _waitTimeBeforeCureIncrement)
            {
                if (currentX + _objectDiameter > maxWidth)
                {
                    currentX = _leftRightMargin;
                    currentY += (uint)(_objectDiameter + _objectMargin);
                    if (currentY > maxHeight) break;
                }

                waitTime = Math.Round(waitTime, 2);

                textMat.PutTextExtended($"E: {_bottomExposure}s/{_normalExposure}s\nW: {waitTime}s", new Point((int)currentX + 20, (int)currentY + _objectDiameter / 2), 
                    FontFace.HersheyDuplex, 2.0, EmguExtensions.WhiteColor, 2, 10);
                currentX += (uint)(_objectDiameter + _objectMargin);
            }

            if (_mirrorOutput) CvInvoke.Flip(textMat, textMat, (FlipType) flip);

            var textLayerCount = 1 / _layerHeight;
            for (int i = 0; i < textLayerCount; i++)
            {
                currentPosition += (float)_layerHeight;
                var layer = new Layer(textMat, SlicerFile)
                {
                    PositionZ = currentPosition
                };
                newLayers.Add(layer);
            }

            SlicerFile.Layers = newLayers.ToArray();
        });

        // Fix exposure times
        foreach (var layer in SlicerFile)
        {
            if (layer.IsBottomLayerByHeight) continue;
            layer.ExposureTime = (float)_normalExposure;
        }

        progress++;
        
        return !progress.Token.IsCancellationRequested;
    }

    #endregion
}