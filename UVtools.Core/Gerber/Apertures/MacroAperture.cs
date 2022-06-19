/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace UVtools.Core.Gerber.Apertures;

public class MacroAperture : Aperture
{
    #region Properties
    public Macro Macro { get; set; } = null!;

    #endregion

    #region Constructor
    public MacroAperture() : base("Macro") { }

    public MacroAperture(int index, Macro macro) : base(index, "Macro")
    {
        Macro = macro;
    }
    #endregion

    public override void DrawFlashD3(Mat mat, SizeF xyPpmm, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        foreach (var macro in Macro)
        {
            macro.DrawFlashD3(mat, xyPpmm, at, color, lineType);
        }
    }
}