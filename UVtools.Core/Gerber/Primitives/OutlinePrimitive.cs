/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using EmguExtensions;
using ZLinq;

namespace UVtools.Core.Gerber.Primitives;

/// <summary>
/// An outline primitive is an area defined by its outline or contour.
/// The outline is a polygon, consisting of linear segments only, defined by its start vertex and n subsequent vertices.
/// The outline must be closed, i.e. the last vertex must be equal to the start vertex.
/// The outline must comply with all the requirements of a contour according to 4.10.3.
/// </summary>
public class OutlinePrimitive : Primitive
{
    #region Constants
    public const byte Code = 4;
    #endregion

    #region Properties
    public override string Name => "Outline";

    /// <summary>
    /// Exposure off/on (0/1)
    /// 1
    /// </summary>
    public string ExposureExpression { get; set; } = "1";
    public byte Exposure { get; set; } = 1;

    /// <summary>
    /// The number of vertices of the outline = the number of coordinate pairs minus one. An integer ≥3.
    /// 2
    /// </summary>
    public string VerticesCountExpression { get; set; } = string.Empty;
    public ushort VerticesCount => Coordinates.Length == 0 ? (ushort)0 : (ushort)(Coordinates.Length - 1);

    /// <summary>
    /// subsequent X and Y coordinates.
    /// The X and Y coordinates are not modal: both X and Y must be specified for all points.
    /// 2+n
    /// </summary>
    public string[] CoordinatesExpression { get; set; } = [];

    public PointF[] Coordinates { get; set; } = [];

    /// <summary>
    /// Rotation angle, in degrees counterclockwise, a decimal.
    /// The primitive is rotated around the origin of the macro definition, i.e. the (0, 0) point of macro coordinates.
    /// </summary>
    public string RotationExpression { get; set; } = "0";
    public float Rotation { get; set; } = 0;
    #endregion

    protected OutlinePrimitive(GerberFormat document) : base(document) { }

    public OutlinePrimitive(GerberFormat document, string exposureExpression,
        string verticesCountExpression, string[] coordinatesExpression,
        string rotationExpression) : base(document)
    {
        ExposureExpression = exposureExpression;
        VerticesCountExpression = verticesCountExpression;
        CoordinatesExpression = coordinatesExpression;
        RotationExpression = rotationExpression;
    }


    public override void DrawFlashD3(Mat mat, PointF at, LineType lineType = LineType.EightConnected)
    {
        if (!IsParsed || Coordinates.Length < 4) return;

        var points = new List<Point>(Coordinates.Length);
        for (var index = 0; index < Coordinates.Length; index++)
        {
            var coordinate = RotateAroundMacroOrigin(Coordinates[index].X, Coordinates[index].Y, Rotation);
            var point = Document.PositionMmToPx(at.X + coordinate.X, at.Y + coordinate.Y);
            if (points.Count == 0 || points[^1] != point) points.Add(point);
        }

        if (points.Count > 1 && points[0] == points[^1]) points.RemoveAt(points.Count - 1);
        if (points.Count < 3) return;

        using var vec = new VectorOfPoint(points.ToArray());
        CvInvoke.FillPoly(mat, vec, Document.GetPolarityColor(Exposure), lineType);
    }

    public override void ParseExpressions(params string[] args)
    {
        IsParsed = false;
        if (CoordinatesExpression.Length < 8 || CoordinatesExpression.Length % 2 != 0) return;

        using var evaluator = new DataTable();
        if (!TryEvaluateByte(evaluator, ExposureExpression, args, 0, 1, out var exposure) ||
            !TryEvaluateExpression(evaluator, VerticesCountExpression, args, out var verticesCount) ||
            verticesCount < 3 ||
            verticesCount > ushort.MaxValue ||
            verticesCount != Math.Truncate(verticesCount) ||
            Math.Round(verticesCount, MidpointRounding.AwayFromZero) != CoordinatesExpression.Length / 2 - 1 ||
            !TryEvaluateExpression(evaluator, RotationExpression, args, out var rotation) ||
            rotation is < float.MinValue or > float.MaxValue)
        {
            return;
        }

        var coordinates = new PointF[CoordinatesExpression.Length / 2];
        for (var index = 0; index < CoordinatesExpression.Length; index += 2)
        {
            if (!TryEvaluateLength(evaluator, CoordinatesExpression[index], args, out var x) ||
                !TryEvaluateLength(evaluator, CoordinatesExpression[index + 1], args, out var y))
            {
                return;
            }

            coordinates[index / 2] = new PointF(x, y);
        }

        Exposure = exposure;
        Coordinates = coordinates;
        Rotation = (float)rotation;
        IsParsed = true;
    }

    public override Primitive Clone()
    {
        var primitive = MemberwiseClone() as OutlinePrimitive;
        primitive!.CoordinatesExpression = primitive.CoordinatesExpression.AsValueEnumerable().ToArray();
        primitive.Coordinates = primitive.Coordinates.AsValueEnumerable().ToArray();
        return primitive;
    }
}
