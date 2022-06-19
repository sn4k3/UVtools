/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Data;
using System.Drawing;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using UVtools.Core.Extensions;

namespace UVtools.Core.Gerber.Primitives;

/// <summary>
/// A vector line is a rectangle defined by its line width, start and end points. The line ends are rectangular.
/// </summary>
public class VectorLinePrimitive : Primitive
{
    #region Constants
    public const byte Code = 20;
    #endregion

    #region Properties
    public override string Name => "VectorLine";

    /// <summary>
    /// Exposure off/on (0/1)
    /// 1
    /// </summary>
    public string ExposureExpression { get; set; } = "1";
    public byte Exposure { get; set; } = 1;

    /// <summary>
    /// Width of the line ≥ 0
    /// 2
    /// </summary>
    public string LineWidthExpression { get; set; } = "0";
    public float LineWidth { get; set; }

    /// <summary>
    /// Start point X coordinate
    /// 3
    /// </summary>
    public string StartXExpression { get; set; } = "0";

    public float StartX { get; set; }

    /// <summary>
    /// Start point Y coordinate
    /// 4
    /// </summary>
    public string StartYExpression { get; set; } = "0";

    public float StartY { get; set; }

    /// <summary>
    /// End point X coordinate
    /// 5
    /// </summary>
    public string EndXExpression { get; set; } = "0";

    public float EndX { get; set; }

    /// <summary>
    /// Start point Y coordinate
    /// 6
    /// </summary>
    public string EndYExpression { get; set; } = "0";

    public float EndY { get; set; }

    /// <summary>
    /// Rotation angle, in degrees counterclockwise, a decimal.
    /// The primitive is rotated around the origin of the macro definition, i.e. the (0, 0) point of macro coordinates.
    /// 7
    /// </summary>
    public string RotationExpression { get; set; } = "0";
    public float Rotation { get; set; } = 0;
    #endregion

    protected VectorLinePrimitive() { }

    public VectorLinePrimitive(string exposureExpression, string lineWidthExpression, string startXExpression, string startYExpression, string endXExpression, string endYExpression, string rotationExpression = "0")
    {
        ExposureExpression = exposureExpression;
        LineWidthExpression = lineWidthExpression;
        StartXExpression = startXExpression;
        StartYExpression = startYExpression;
        EndXExpression = endXExpression;
        EndYExpression = endYExpression;
        RotationExpression = rotationExpression;
    }

    public override void DrawFlashD3(Mat mat, SizeF xyPpmm, PointF at, MCvScalar color,
        LineType lineType = LineType.EightConnected)
    {
        if (!IsParsed) return;
        if (LineWidth <= 0) return;

        if (Exposure == 0) color = EmguExtensions.BlackColor;
        else if (color.V0 == 0) color = EmguExtensions.WhiteColor;

        var pt1 = GerberDocument.PositionMmToPx(at.X + StartX, at.Y + StartY, xyPpmm);
        var pt2 = GerberDocument.PositionMmToPx(at.X + EndX, at.Y + EndY, xyPpmm);
        CvInvoke.Line(mat, pt1, pt2, color, GerberDocument.SizeMmToPx(LineWidth, xyPpmm.Height), lineType);
        //CvInvoke.Rectangle(mat, rectangle, color, -1, lineType);
    }

    public override void ParseExpressions(params string[] args)
    {
        string csharpExp, result;
        float num;
        var exp = new DataTable();

        if (byte.TryParse(ExposureExpression, out var exposure)) Exposure = exposure;
        else
        {
            csharpExp = string.Format(Regex.Replace(ExposureExpression, @"\$(\d+)", "{$1}"), args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (byte.TryParse(result, out var val)) Exposure = val;
        }

        if (float.TryParse(LineWidthExpression, out num)) LineWidth = num;
        else
        {
            csharpExp = Regex.Replace(LineWidthExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out var val)) LineWidth = val;
        }

        if (float.TryParse(StartXExpression, out num)) StartX = num;
        else
        {
            csharpExp = Regex.Replace(StartXExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out num)) StartX = num;
        }

        if (float.TryParse(EndXExpression, out num)) EndX = num;
        else
        {
            csharpExp = Regex.Replace(EndXExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out num)) EndX = num;
        }

        if (float.TryParse(StartYExpression, out num)) StartY = num;
        else
        {
            csharpExp = Regex.Replace(StartYExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out num)) StartY = num;
        }

        if (float.TryParse(EndYExpression, out num)) EndY = num;
        else
        {
            csharpExp = Regex.Replace(EndYExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out num)) EndY = num;
        }


        if (float.TryParse(RotationExpression, out num)) Rotation = (short)num;
        else
        {
            csharpExp = Regex.Replace(RotationExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out num)) Rotation = num;
        }

        IsParsed = true;
    }
}