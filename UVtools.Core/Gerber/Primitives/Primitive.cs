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

namespace UVtools.Core.Gerber.Primitives;

public abstract class Primitive
{
    #region Properties
    public abstract string Name { get; }

    public bool IsParsed { get; protected set; } = false;

    #endregion

    protected Primitive() { }

    public abstract void DrawFlashD3(Mat mat, SizeF xyPpmm, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected);

    public abstract void ParseExpressions(params string[] args);
}