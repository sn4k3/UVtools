/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using UVtools.Core.Extensions;

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
    public ushort VerticesCount => (ushort) Coordinates.Length;

    /// <summary>
    /// subsequent X and Y coordinates.
    /// The X and Y coordinates are not modal: both X and Y must be specified for all points.
    /// 2+n
    /// </summary>
    public string[] CoordinatesExpression { get; set; } = Array.Empty<string>();

    public PointF[] Coordinates { get; set; } = Array.Empty<PointF>();

    /// <summary>
    /// Rotation angle, in degrees counterclockwise, a decimal.
    /// The primitive is rotated around the origin of the macro definition, i.e. the (0, 0) point of macro coordinates.
    /// </summary>
    public string RotationExpression { get; set; } = "0";
    public float Rotation { get; set; } = 0;
    #endregion

    protected OutlinePrimitive(GerberDocument document) : base(document) { }

    public OutlinePrimitive(GerberDocument document, string exposureExpression, string[] coordinatesExpression, string rotationExpression) : base(document)
    {
        ExposureExpression = exposureExpression;
        CoordinatesExpression = coordinatesExpression;
        RotationExpression = rotationExpression;
    }


    public override void DrawFlashD3(Mat mat, PointF at, MCvScalar color, LineType lineType = LineType.EightConnected)
    {
        if (Coordinates.Length < 3) return;

        if (Exposure == 0) color = EmguExtensions.BlackColor;
        else if (color.V0 == 0) color = EmguExtensions.WhiteColor;

        var points = new List<Point>();
        for (int i = 0; i < Coordinates.Length-1; i++)
        {
            var pt = Document.PositionMmToPx(at.X + Coordinates[i].X, at.Y + Coordinates[i].Y);
            if(i > 0 && points[i-1] == pt) continue; // Prevent duplicates
            points.Add(pt);
        }

        using var vec = new VectorOfPoint(points.ToArray());
        CvInvoke.FillPoly(mat, vec, color, lineType);
    }

    public override void ParseExpressions(GerberDocument document, params string[] args)
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

        float? x = null;
        var coordinates = new List<PointF>();
        foreach (var coordinate in CoordinatesExpression)
        {
            if (!float.TryParse(coordinate, out num))
            {
                csharpExp = string.Format(Regex.Replace(coordinate, @"\$(\d+)", "{$1}"), args);
                result = exp.Compute(csharpExp, null).ToString()!;
                float.TryParse(result, out num);
            }

            if (x is null)
            {
                x = num;
            }
            else
            {
                coordinates.Add(document.GetMillimeters(new PointF(x.Value, num)));
                x = null;
            }
        }

        Coordinates = coordinates.ToArray();

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

    public override Primitive Clone()
    {
        var primitive = MemberwiseClone() as OutlinePrimitive;
        primitive!.CoordinatesExpression = primitive.CoordinatesExpression.ToArray();
        primitive.Coordinates = primitive.Coordinates.ToArray();
        return primitive;
    }
}