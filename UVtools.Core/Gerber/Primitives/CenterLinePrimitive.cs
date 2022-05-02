/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

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
public class CenterLinePrimitive : Primitive
{
    #region Constants
    public const byte Code = 21;
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
    /// Width ≥ 0
    /// 2
    /// </summary>
    public string WidthExpression { get; set; } = "0";
    public float Width { get; set; }

    /// <summary>
    /// Height ≥ 0
    /// 3
    /// </summary>
    public string HeightExpression { get; set; } = "0";
    public float Height { get; set; }

    /// <summary>
    /// Center point X coordinate
    /// 4
    /// </summary>
    public string CenterXExpression { get; set; } = "0";

    public float CenterX { get; set; }

    /// <summary>
    /// Center point Y coordinate
    /// 5
    /// </summary>
    public string CenterYExpression { get; set; } = "0";

    public float CenterY { get; set; }

    /// <summary>
    /// Rotation angle, in degrees counterclockwise, a decimal.
    /// The primitive is rotated around the origin of the macro definition, i.e. the (0, 0) point of macro coordinates.
    /// 6
    /// </summary>
    public string RotationExpression { get; set; } = "0";
    public float Rotation { get; set; } = 0;
    #endregion

    protected CenterLinePrimitive() { }

    public CenterLinePrimitive(string exposureExpression, string widthExpression = "0", string heightExpression = "0", string centerXExpression = "0", string centerYExpression = "0", string rotationExpression = "0")
    {
        ExposureExpression = exposureExpression;
        WidthExpression = widthExpression;
        HeightExpression = heightExpression;
        CenterXExpression = centerXExpression;
        CenterYExpression = centerYExpression;
        RotationExpression = rotationExpression;
    }


    public override void DrawFlashD3(Mat mat, SizeF xyPpmm, Point at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        if (!IsParsed) return;
        if (Width <= 0 || Height <= 0) return;

        if (Exposure == 0) color = EmguExtensions.BlackColor;
        else if(color.V0 == 0) color = EmguExtensions.WhiteColor;

        var halfWidth = Width / 2;
        var pt1 = new Point(at.X + (int)((CenterX - halfWidth) * xyPpmm.Width), at.Y + (int)(CenterY * xyPpmm.Height));
        var pt2 = new Point(at.X + (int)((CenterX + halfWidth) * xyPpmm.Width), at.Y + (int)(CenterY * xyPpmm.Height));
        CvInvoke.Line(mat, pt1, pt2, color, (int)(Height * xyPpmm.Height), lineType);
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

        if (float.TryParse(WidthExpression, out num)) Width = num;
        else
        {
            csharpExp = Regex.Replace(WidthExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out var val)) Width = val;
        }

        if (float.TryParse(HeightExpression, out num)) Height = num;
        else
        {
            csharpExp = Regex.Replace(HeightExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out var val)) Height = val;
        }

        if (float.TryParse(CenterXExpression, out num)) CenterX = num;
        else
        {
            csharpExp = Regex.Replace(CenterXExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out num)) CenterX = num;
        }

        if (float.TryParse(CenterYExpression, out num)) CenterY = num;
        else
        {
            csharpExp = Regex.Replace(CenterYExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out num)) CenterY = num;
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