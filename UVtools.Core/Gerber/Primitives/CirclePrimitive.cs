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

        var center = RotateAroundMacroOrigin(CenterX, CenterY, Rotation);
        CvInvoke.Ellipse(mat, Document.PositionMmToPx(at.X + center.X, at.Y + center.Y),
            Document.SizeMmToPx(Diameter / 2.0, Diameter / 2.0), 0, 0, 360,
            Document.GetPolarityColor(Exposure), -1, lineType);
    }

    public override void ParseExpressions(params string[] args)
    {
        IsParsed = false;
        using var evaluator = new DataTable();
        if (!TryEvaluateByte(evaluator, ExposureExpression, args, 0, 1, out var exposure) ||
            !TryEvaluateLength(evaluator, DiameterExpression, args, out var diameter) ||
            !TryEvaluateLength(evaluator, CenterXExpression, args, out var centerX) ||
            !TryEvaluateLength(evaluator, CenterYExpression, args, out var centerY) ||
            !TryEvaluateExpression(evaluator, RotationExpression, args, out var rotation) ||
            rotation is < float.MinValue or > float.MaxValue)
        {
            return;
        }

        Exposure = exposure;
        Diameter = diameter;
        CenterX = centerX;
        CenterY = centerY;
        Rotation = (float)rotation;
        IsParsed = true;
    }
}
