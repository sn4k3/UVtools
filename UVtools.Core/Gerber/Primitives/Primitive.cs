﻿/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using System.Drawing;

namespace UVtools.Core.Gerber.Primitives;

public abstract class Primitive
{
    #region Properties
    public abstract string Name { get; }

    public bool IsParsed { get; protected set; } = false;

    public GerberFormat Document { get; init; }

    #endregion

    //protected Primitive() { }

    protected Primitive(GerberFormat document)
    {
        Document = document;
    }

    public abstract void DrawFlashD3(Mat mat, PointF at, LineType lineType = LineType.EightConnected);

    public abstract void ParseExpressions(params string[] args);

    public virtual Primitive Clone() => (Primitive)MemberwiseClone();
}