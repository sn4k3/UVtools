/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Emgu.CV.CvEnum;
using System.Drawing;

namespace UVtools.Core.PixelEditor;

public class PixelSupport : PixelOperation
{
    private byte _tipDiameter = 19;
    private byte _pillarDiameter = 32;
    private byte _baseDiameter = 60;
    public override PixelOperationType OperationType => PixelOperationType.Supports;

    public byte TipDiameter
    {
        get => _tipDiameter;
        set => RaiseAndSetIfChanged(ref _tipDiameter, value);
    }

    public byte PillarDiameter
    {
        get => _pillarDiameter;
        set => RaiseAndSetIfChanged(ref _pillarDiameter, value);
    }

    public byte BaseDiameter
    {
        get => _baseDiameter;
        set => RaiseAndSetIfChanged(ref _baseDiameter, value);
    }

    public PixelSupport(){}

    public PixelSupport(uint layerIndex, Point location, byte tipDiameter, byte pillarDiameter, byte baseDiameter, byte pixelBrightness) : base(layerIndex, location, LineType.AntiAlias, pixelBrightness)
    {
        TipDiameter = tipDiameter;
        PillarDiameter = pillarDiameter;
        BaseDiameter = baseDiameter;
        Size = new Size(TipDiameter, TipDiameter);
    }
}