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
using System.Drawing;

namespace UVtools.Core.Gerber.Apertures;

public class MacroAperture : Aperture
{
    #region Properties
    public Macro Macro { get; set; } = null!;

    #endregion

    #region Constructor
    public MacroAperture(GerberFormat document) : base(document, "Macro") { }

    public MacroAperture(GerberFormat document, int index, Macro macro) : base(document, index, "Macro")
    {
        Macro = macro;
    }
    #endregion

    public override void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        foreach (var primitive in Macro)
        {
            //if(primitive.Name == "Comment") continue;
            primitive.DrawFlashD3(mat, at, lineType);
        }
    }
}