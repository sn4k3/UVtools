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

namespace UVtools.Core.Gerber.Primitives;

/// <summary>
/// A circle primitive is defined by its center point and diameter.
/// </summary>
public class CirclePrimitive : Primitive
{
    #region Constants
    public const byte Code = 1;
    #endregion

    #region Properties
    public override string Name => "Circle";

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
    public string DiameterExpression { get; set; } = "0";
    public float Diameter { get; set; }

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
    /// Rotation angle, in degrees counterclockwise, a decimal.
    /// The primitive is rotated around the origin of the macro definition, i.e. the (0, 0) point of macro coordinates.
    /// 5
    /// </summary>
    public string RotationExpression { get; set; } = "0";
    public float Rotation { get; set; } = 0;
    #endregion

    protected CirclePrimitive(GerberFormat document) : base(document) { }

    public CirclePrimitive(GerberFormat document, string exposureExpression = "1", string diameterExpression = "0", string centerXExpression = "0", string centerYExpression = "0", string rotationExpression = "0") : base(document)
    {
        ExposureExpression = exposureExpression;
        DiameterExpression = diameterExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase);
        CenterXExpression = centerXExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase);
        CenterYExpression = centerYExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase);
        RotationExpression = rotationExpression.Replace("X", "*", StringComparison.OrdinalIgnoreCase);
    }

    public override void DrawFlashD3(Mat mat, PointF at, LineType lineType = LineType.EightConnected)
    {
        if (!IsParsed) return;
        if (Diameter <= 0) return;

        //CvInvoke.Circle(mat, Document.PositionMmToPx(at.X + CenterX, at.Y + CenterY), Document.SizeMmToPx(Diameter / 2), Document.GetPolarityColor(Exposure), -1, lineType);
        CvInvoke.Ellipse(mat, Document.PositionMmToPx(at.X + CenterX, at.Y + CenterY), Document.SizeMmToPx(Diameter / 2.0, Diameter / 2.0), 0, 0, 360, Document.GetPolarityColor(Exposure), -1, lineType);
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

        if (float.TryParse(DiameterExpression, NumberStyles.Float, CultureInfo.InvariantCulture, out num)) Diameter = num;
        else
        {
            csharpExp = Regex.Replace(DiameterExpression, @"\$([0-9]+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            var temp = exp.Compute(csharpExp, null);
            if (temp is not DBNull) Diameter = Convert.ToSingle(temp);
        }
        Diameter = Document.GetMillimeters(Diameter);

        if (float.TryParse(CenterXExpression, NumberStyles.Float, CultureInfo.InvariantCulture, out num)) CenterX = num;
        else
        {
            csharpExp = Regex.Replace(CenterXExpression, @"\$([0-9]+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            var temp = exp.Compute(csharpExp, null);
            if (temp is not DBNull) CenterX = Convert.ToSingle(temp);
        }
        CenterX = Document.GetMillimeters(CenterX);

        if (float.TryParse(CenterYExpression, NumberStyles.Float, CultureInfo.InvariantCulture, out num)) CenterY = num;
        else
        {
            csharpExp = Regex.Replace(CenterYExpression, @"\$([0-9]+)", "{$1}");
            csharpExp = string.Format(csharpExp, args);
            var temp = exp.Compute(csharpExp, null);
            if (temp is not DBNull) CenterY = Convert.ToSingle(temp);
        }
        CenterY = Document.GetMillimeters(CenterY);


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