/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
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

    protected VectorLinePrimitive(GerberFormat document) : base(document) { }

    public VectorLinePrimitive(GerberFormat document, string exposureExpression, string lineWidthExpression, string startXExpression, string startYExpression, string endXExpression, string endYExpression, string rotationExpression = "0") : base(document)
    {
        ExposureExpression = exposureExpression;
        LineWidthExpression = lineWidthExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase); ;
        StartXExpression = startXExpression;
        StartYExpression = startYExpression;
        EndXExpression = endXExpression;
        EndYExpression = endYExpression;
        RotationExpression = rotationExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase); ;
    }

    public override void DrawFlashD3(Mat mat, PointF at, LineType lineType = LineType.EightConnected)
    {
        if (!IsParsed) return;
        if (LineWidth <= 0) return;

        if (Rotation != 0)
        {
            throw new NotImplementedException($"{Name} primitive with code {Code} have a rotation value of {Rotation} which is not implemented. Open a issue regarding this problem and provide a sample file to be able to implement rotation correctly on this primitive.");
        }

        var pt1 = Document.PositionMmToPx(at.X + StartX, at.Y + StartY);
        var pt2 = Document.PositionMmToPx(at.X + EndX, at.Y + EndY);
        CvInvoke.Line(mat, pt1, pt2, Document.GetPolarityColor(Exposure), EmguExtensions.CorrectThickness(Document.SizeMmToPxOverride(LineWidth, Document.XYppmm.Height)), lineType);
        //CvInvoke.Rectangle(mat, rectangle, color, -1, lineType);
    }

    public override void ParseExpressions(params string[] args)
    {
        string csharpExp;
        float num;
        var exp = new DataTable();

        if (byte.TryParse(ExposureExpression, out var exposure)) Exposure = exposure;
        else
        {
            csharpExp = string.Format(Regex.Replace(ExposureExpression, @"\$([0-9]+)", "{$1}"), args);
            var temp = exp.Compute(csharpExp, null);
            if (temp is not DBNull) Exposure = Convert.ToByte(temp);
        }

        if (float.TryParse(LineWidthExpression, NumberStyles.Float, CultureInfo.InvariantCulture, out num)) LineWidth = num;
        else
        {
            csharpExp = Regex.Replace(LineWidthExpression, @"\$([0-9]+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            var temp = exp.Compute(csharpExp, null);
            if (temp is not DBNull) LineWidth = Convert.ToSingle(temp);
        }
        LineWidth = Document.GetMillimeters(LineWidth);

        if (float.TryParse(StartXExpression, NumberStyles.Float, CultureInfo.InvariantCulture, out num)) StartX = num;
        else
        {
            csharpExp = Regex.Replace(StartXExpression, @"\$([0-9]+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            var temp = exp.Compute(csharpExp, null);
            if (temp is not DBNull) StartX = Convert.ToSingle(temp);
        }
        StartX = Document.GetMillimeters(StartX);

        if (float.TryParse(EndXExpression, NumberStyles.Float, CultureInfo.InvariantCulture, out num)) EndX = num;
        else
        {
            csharpExp = Regex.Replace(EndXExpression, @"\$([0-9]+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            var temp = exp.Compute(csharpExp, null);
            if (temp is not DBNull) EndX = Convert.ToSingle(temp);
        }
        EndX = Document.GetMillimeters(EndX);

        if (float.TryParse(StartYExpression, NumberStyles.Float, CultureInfo.InvariantCulture, out num)) StartY = num;
        else
        {
            csharpExp = Regex.Replace(StartYExpression, @"\$([0-9]+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            var temp = exp.Compute(csharpExp, null);
            if (temp is not DBNull) StartY = Convert.ToSingle(temp);
        }
        StartY = Document.GetMillimeters(StartY);

        if (float.TryParse(EndYExpression, NumberStyles.Float, CultureInfo.InvariantCulture, out num)) EndY = num;
        else
        {
            csharpExp = Regex.Replace(EndYExpression, @"\$([0-9]+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            var temp = exp.Compute(csharpExp, null);
            if (temp is not DBNull) EndY = Convert.ToSingle(temp);
        }
        EndY = Document.GetMillimeters(EndY);

        if (float.TryParse(RotationExpression, NumberStyles.Float, CultureInfo.InvariantCulture, out num)) Rotation = (short)num;
        else
        {
            csharpExp = Regex.Replace(RotationExpression, @"\$([0-9]+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            var temp = exp.Compute(csharpExp, null);
            if (temp is not DBNull) Rotation = Convert.ToSingle(temp);
        }

        IsParsed = true;
    }
}