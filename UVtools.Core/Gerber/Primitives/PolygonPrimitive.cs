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
/// A polygon primitive is a regular polygon defined by the number of vertices n, the center point and the diameter of the circumscribed circle.
/// </summary>
public class PolygonPrimitive : Primitive
{
    #region Constants
    public const byte Code = 5;
    #endregion

    #region Properties
    public override string Name => "Polygon";

    /// <summary>
    /// Exposure off/on (0/1)
    /// 1
    /// </summary>
    public string ExposureExpression { get; set; } = "1";
    public byte Exposure { get; set; } = 1;

    /// <summary>
    /// Diameter ≥ 0
    /// 2
    /// </summary>
    public string VerticesCountExpression { get; set; } = "0";
    public byte VerticesCount { get; set; }

    /// <summary>
    /// Center X coordinate.
    /// 3
    /// </summary>
    public string CenterXExpression { get; set; } = "0";
    public float CenterX { get; set; }

    /// <summary>
    /// Center Y coordinate.
    /// 4
    /// </summary>
    public string CenterYExpression { get; set; } = "0";
    public float CenterY { get; set; }

    /// <summary>
    /// Diameter ≥ 0
    /// 5
    /// </summary>
    public string DiameterExpression { get; set; } = "0";
    public float Diameter { get; set; }

    /// <summary>
    /// Rotation angle, in degrees counterclockwise, a decimal.
    /// The primitive is rotated around the origin of the macro definition, i.e. the (0, 0) point of macro coordinates.
    /// 6
    /// </summary>
    public string RotationExpression { get; set; } = "0";
    public float Rotation { get; set; } = 0;
    #endregion

    protected PolygonPrimitive() { }

    public PolygonPrimitive(string exposureExpression, string verticesCountExpression, string centerXExpression = "0", string centerYExpression = "0", string diameterExpression = "0", string rotationExpression = "0")
    {
        ExposureExpression = exposureExpression;
        VerticesCountExpression = verticesCountExpression;
        CenterXExpression = centerXExpression;
        CenterYExpression = centerYExpression;
        DiameterExpression = diameterExpression;
        RotationExpression = rotationExpression;
    }

    public override void DrawFlashD3(Mat mat, SizeF xyPpmm, Point at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        if (!IsParsed) return;
        if (Diameter <= 0) return;

        if (Exposure == 0) color = EmguExtensions.BlackColor;
        else if (color.V0 == 0) color = EmguExtensions.WhiteColor;

        var position = new Point(
            (int) (at.X + CenterX * xyPpmm.Width),
            (int) (at.Y + CenterY * xyPpmm.Height)
        );

        mat.DrawPolygon(VerticesCount, (int)(Diameter * xyPpmm.Max() / 2), position, color, 0, -1, lineType);
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

        if (byte.TryParse(DiameterExpression, out var vertices)) VerticesCount = vertices;
        else
        {
            csharpExp = Regex.Replace(DiameterExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (byte.TryParse(result, out var vertices1)) VerticesCount = vertices1;
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

        if (float.TryParse(DiameterExpression, out num)) Diameter = num;
        else
        {
            csharpExp = Regex.Replace(DiameterExpression, @"\$(\d+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            result = exp.Compute(csharpExp, null).ToString()!;
            if (float.TryParse(result, out num)) Diameter = num;
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