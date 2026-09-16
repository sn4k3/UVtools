/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace UVtools.UI.Controls;

public sealed class LayerModelOrientationCube : Control
{
    private const double DragThreshold = 3;

    /// <summary>
    /// Splits each cube face into a 3x3 grid of targets: the centre cell aims at the face itself,
    /// the border cells at the shared edges and the four corner cells at the cube corners.
    /// </summary>
    private const float TargetEdgeThreshold = 0.58f;

    /// <summary>Distance from the cube centre to the middle of an edge/corner cell, per axis.</summary>
    private const float CellOuterCenter = (1 + TargetEdgeThreshold) / 2f;

    /// <summary>Half size of an edge/corner cell, per axis.</summary>
    private const float CellOuterHalf = (1 - TargetEdgeThreshold) / 2f;

    private const double ProjectionScale = 0.27;

    private static readonly Vector3[] Vertices =
    [
        new(-1, -1, -1), new(1, -1, -1), new(1, 1, -1), new(-1, 1, -1),
        new(-1, -1, 1), new(1, -1, 1), new(1, 1, 1), new(-1, 1, 1)
    ];

    private static readonly Face[] Faces =
    [
        new(Vector3.UnitZ, "TOP", 4, 5, 6, 7),
        new(-Vector3.UnitZ, "BOTTOM", 0, 3, 2, 1),
        new(Vector3.UnitX, "RIGHT", 1, 2, 6, 5),
        new(-Vector3.UnitX, "LEFT", 0, 4, 7, 3),
        new(-Vector3.UnitY, "FRONT", 0, 1, 5, 4),
        new(Vector3.UnitY, "BACK", 3, 7, 6, 2)
    ];

    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromArgb(178, 18, 22, 29));
    private static readonly IBrush FaceBrush = new SolidColorBrush(Color.FromRgb(74, 84, 99));
    private static readonly IBrush FaceSecondaryBrush = new SolidColorBrush(Color.FromRgb(61, 70, 84));
    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.FromRgb(36, 150, 215));
    private static readonly IBrush TextBrush = Brushes.White;
    private static readonly Pen BorderPen = new(new SolidColorBrush(Color.FromRgb(145, 157, 175)), 1);
    private static readonly Pen HighlightPen = new(new SolidColorBrush(Color.FromRgb(121, 213, 255)), 2);
    private static readonly Typeface LabelTypeface = new("Inter", FontStyle.Normal, FontWeight.SemiBold);
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);

    private readonly Point[] _projectedVertices = new Point[Vertices.Length];
    private readonly int[] _visibleFaceIndices = new int[3];

    private readonly FormattedText?[] _faceTexts = new FormattedText?[Faces.Length];

    private float _cameraYaw = -0.8f;
    private float _cameraPitch = 0.55f;
    private IPointer? _capturedPointer;
    private Point _pressedPosition;
    private Point _lastPointerPosition;
    private bool _isDragging;

    /// <summary>
    /// The hovered target cell, each component being -1, 0 or 1, where 0 spans the middle of that axis.
    /// One non-zero component targets a face, two an edge and three a corner.
    /// </summary>
    private Vector3? _hoverCell;

    public LayerModelOrientationCube()
    {
        Cursor = HandCursor;
        Focusable = true;
    }

    public event Action<double, double>? OrbitRequested;
    public event Action<Vector3>? SnapRequested;

    public void SetCameraOrientation(float yaw, float pitch)
    {
        if (_cameraYaw.Equals(yaw) && _cameraPitch.Equals(pitch)) return;
        _cameraYaw = yaw;
        _cameraPitch = pitch;
        if (_capturedPointer is not null) _hoverCell = null;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(BackgroundBrush, BorderPen, new Rect(Bounds.Size), 8, 8);

        GetProjection(out var center, out var scale, out var direction, out var right, out var up);
        for (var index = 0; index < Vertices.Length; index++)
        {
            _projectedVertices[index] = Project(Vertices[index], center, scale, right, up);
        }

        var visibleCount = 0;
        for (var faceIndex = 0; faceIndex < Faces.Length; faceIndex++)
        {
            if (Vector3.Dot(Faces[faceIndex].Normal, direction) <= 0.001f) continue;
            _visibleFaceIndices[visibleCount++] = faceIndex;
        }

        for (var index = 1; index < visibleCount; index++)
        {
            var value = _visibleFaceIndices[index];
            var valueDepth = Vector3.Dot(Faces[value].Normal, direction);
            var position = index;
            while (position > 0 && Vector3.Dot(Faces[_visibleFaceIndices[position - 1]].Normal, direction) > valueDepth)
            {
                _visibleFaceIndices[position] = _visibleFaceIndices[position - 1];
                position--;
            }

            _visibleFaceIndices[position] = value;
        }

        /* Visible faces of a convex cube tile the silhouette without overlapping each other, so the
         * highlight and the labels can be drawn as later passes without any of them bleeding over a face
         * that should occlude them. */
        for (var index = 0; index < visibleCount; index++)
        {
            var face = Faces[_visibleFaceIndices[index]];
            context.DrawGeometry(index % 2 == 0 ? FaceBrush : FaceSecondaryBrush, BorderPen,
                CreateFaceGeometry(face));
        }

        if (_hoverCell is { } hoverCell)
        {
            for (var index = 0; index < visibleCount; index++)
            {
                var face = Faces[_visibleFaceIndices[index]];
                /* An edge or corner cell lies on every face it touches, drawing one strip/square per face. */
                if (Vector3.Dot(face.Normal, hoverCell) <= 0) continue;
                context.DrawGeometry(HighlightBrush, HighlightPen,
                    CreateCellGeometry(face, hoverCell, center, scale, right, up));
            }
        }

        for (var index = 0; index < visibleCount; index++)
        {
            var faceIndex = _visibleFaceIndices[index];
            DrawFaceLabel(context, faceIndex, Faces[faceIndex]);
        }
    }

    private StreamGeometry CreateFaceGeometry(Face face)
    {
        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        geometryContext.BeginFigure(_projectedVertices[face.Vertex0]);
        geometryContext.LineTo(_projectedVertices[face.Vertex1]);
        geometryContext.LineTo(_projectedVertices[face.Vertex2]);
        geometryContext.LineTo(_projectedVertices[face.Vertex3]);
        geometryContext.EndFigure(true);
        return geometry;
    }

    /// <summary>
    /// Builds the quad of the <paramref name="cell"/> target as it lies on <paramref name="face"/>: the centre
    /// square for a face target, a border strip for an edge and a corner square for a corner.
    /// </summary>
    private static StreamGeometry CreateCellGeometry(Face face, Vector3 cell, Point center, double scale,
        Vector3 right, Vector3 up)
    {
        GetCellBox(face.Normal, cell, out var cellCenter, out var cellHalf);
        GetFaceAxes(face.Normal, out var axisA, out var axisB);

        /* cellHalf is zero on the face axis, so both extents stay within the face plane. */
        var extentA = axisA * Vector3.Dot(cellHalf, axisA);
        var extentB = axisB * Vector3.Dot(cellHalf, axisB);

        var geometry = new StreamGeometry();
        using var geometryContext = geometry.Open();
        geometryContext.BeginFigure(Project(cellCenter - extentA - extentB, center, scale, right, up));
        geometryContext.LineTo(Project(cellCenter + extentA - extentB, center, scale, right, up));
        geometryContext.LineTo(Project(cellCenter + extentA + extentB, center, scale, right, up));
        geometryContext.LineTo(Project(cellCenter - extentA + extentB, center, scale, right, up));
        geometryContext.EndFigure(true);
        return geometry;
    }

    /// <summary>
    /// Gets the centre and the half size of a target cell, sharing <see cref="TargetEdgeThreshold"/> with
    /// <see cref="HitTestCell"/> so the highlighted region is always the region that gets picked.
    /// </summary>
    private static void GetCellBox(Vector3 faceNormal, Vector3 cell, out Vector3 center, out Vector3 half)
    {
        center = new Vector3(
            ComponentCenter(faceNormal.X, cell.X),
            ComponentCenter(faceNormal.Y, cell.Y),
            ComponentCenter(faceNormal.Z, cell.Z));
        half = new Vector3(
            ComponentHalf(faceNormal.X, cell.X),
            ComponentHalf(faceNormal.Y, cell.Y),
            ComponentHalf(faceNormal.Z, cell.Z));

        static float ComponentCenter(float normal, float cell) => normal != 0
            ? normal
            : cell == 0 ? 0 : MathF.CopySign(CellOuterCenter, cell);

        static float ComponentHalf(float normal, float cell) => normal != 0
            ? 0
            : cell == 0 ? TargetEdgeThreshold : CellOuterHalf;
    }

    /// <summary>Gets the two unit axes that span the plane of an axis aligned <paramref name="faceNormal"/>.</summary>
    private static void GetFaceAxes(Vector3 faceNormal, out Vector3 axisA, out Vector3 axisB)
    {
        if (faceNormal.X != 0)
        {
            axisA = Vector3.UnitY;
            axisB = Vector3.UnitZ;
            return;
        }

        if (faceNormal.Y != 0)
        {
            axisA = Vector3.UnitX;
            axisB = Vector3.UnitZ;
            return;
        }

        axisA = Vector3.UnitX;
        axisB = Vector3.UnitY;
    }

    private void DrawFaceLabel(DrawingContext context, int faceIndex, Face face)
    {
        var center = new Point(
            (_projectedVertices[face.Vertex0].X + _projectedVertices[face.Vertex1].X +
             _projectedVertices[face.Vertex2].X + _projectedVertices[face.Vertex3].X) / 4,
            (_projectedVertices[face.Vertex0].Y + _projectedVertices[face.Vertex1].Y +
             _projectedVertices[face.Vertex2].Y + _projectedVertices[face.Vertex3].Y) / 4);
        var text = _faceTexts[faceIndex] ??= new FormattedText(face.Label, CultureInfo.CurrentCulture,
            FlowDirection.LeftToRight, LabelTypeface, face.Label.Length > 5 ? 7 : 8, TextBrush);
        context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        Focus();
        _capturedPointer = e.Pointer;
        _pressedPosition = _lastPointerPosition = e.GetPosition(this);
        _isDragging = false;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        var currentPosition = e.GetPosition(this);
        if (ReferenceEquals(_capturedPointer, e.Pointer))
        {
            var delta = currentPosition - _lastPointerPosition;
            _lastPointerPosition = currentPosition;
            var totalDelta = currentPosition - _pressedPosition;
            _isDragging |= Math.Abs(totalDelta.X) >= DragThreshold || Math.Abs(totalDelta.Y) >= DragThreshold;
            if (_isDragging) OrbitRequested?.Invoke(delta.X, delta.Y);
            e.Handled = true;
            return;
        }

        var hoverCell = HitTestCell(currentPosition);
        if (_hoverCell == hoverCell) return;
        _hoverCell = hoverCell;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!ReferenceEquals(_capturedPointer, e.Pointer)) return;
        if (!_isDragging && HitTestCell(e.GetPosition(this)) is { } cell)
        {
            SnapRequested?.Invoke(Vector3.Normalize(cell));
        }

        e.Pointer.Capture(null);
        ResetPointerState();
        e.Handled = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_capturedPointer is not null) return;
        _hoverCell = null;
        InvalidateVisual();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        ResetPointerState();
    }

    private void ResetPointerState()
    {
        _capturedPointer = null;
        _isDragging = false;
        _hoverCell = null;
        InvalidateVisual();
    }

    /// <summary>
    /// Gets the target cell under <paramref name="point"/>, or null when the pointer misses the cube.
    /// Each component is -1, 0 or 1: one non-zero component targets a face, two an edge and three a corner.
    /// </summary>
    private Vector3? HitTestCell(Point point)
    {
        if (Math.Min(Bounds.Width, Bounds.Height) <= 0) return null;
        GetProjection(out var center, out var scale, out var direction, out var right, out var up);
        var localX = (float)((point.X - center.X) / scale);
        var localY = (float)((center.Y - point.Y) / scale);
        var rayOrigin = right * localX + up * localY + direction * 4;
        var rayDirection = -direction;
        if (!TryIntersectCube(rayOrigin, rayDirection, out var hitPoint)) return null;

        var target = new Vector3(
            Math.Abs(hitPoint.X) >= TargetEdgeThreshold ? MathF.CopySign(1, hitPoint.X) : 0,
            Math.Abs(hitPoint.Y) >= TargetEdgeThreshold ? MathF.CopySign(1, hitPoint.Y) : 0,
            Math.Abs(hitPoint.Z) >= TargetEdgeThreshold ? MathF.CopySign(1, hitPoint.Z) : 0);
        return target.LengthSquared() > 0 ? target : null;
    }

    private static bool TryIntersectCube(Vector3 origin, Vector3 direction, out Vector3 hitPoint)
    {
        var near = float.NegativeInfinity;
        var far = float.PositiveInfinity;
        if (!IntersectAxis(origin.X, direction.X, ref near, ref far) ||
            !IntersectAxis(origin.Y, direction.Y, ref near, ref far) ||
            !IntersectAxis(origin.Z, direction.Z, ref near, ref far) || far < Math.Max(near, 0))
        {
            hitPoint = default;
            return false;
        }

        hitPoint = origin + direction * Math.Max(near, 0);
        return true;
    }

    private static bool IntersectAxis(float origin, float direction, ref float near, ref float far)
    {
        if (Math.Abs(direction) < 0.000001f) return origin is >= -1 and <= 1;
        var first = (-1 - origin) / direction;
        var second = (1 - origin) / direction;
        if (first > second) (first, second) = (second, first);
        near = Math.Max(near, first);
        far = Math.Min(far, second);
        return near <= far;
    }

    /// <summary>Gets everything needed to map cube space to control space, and back on the hit test.</summary>
    private void GetProjection(out Point center, out double scale, out Vector3 direction, out Vector3 right,
        out Vector3 up)
    {
        center = new Rect(Bounds.Size).Center;
        scale = Math.Max(1, Math.Min(Bounds.Width, Bounds.Height) * ProjectionScale);
        GetCameraBasis(out direction, out right, out up);
    }

    private static Point Project(Vector3 point, Point center, double scale, Vector3 right, Vector3 up)
    {
        return new Point(
            center.X + Vector3.Dot(point, right) * scale,
            center.Y - Vector3.Dot(point, up) * scale);
    }

    private void GetCameraBasis(out Vector3 direction, out Vector3 right, out Vector3 up)
    {
        var cosPitch = MathF.Cos(_cameraPitch);
        direction = new Vector3(
            cosPitch * MathF.Cos(_cameraYaw),
            cosPitch * MathF.Sin(_cameraYaw),
            MathF.Sin(_cameraPitch));
        right = new Vector3(-MathF.Sin(_cameraYaw), MathF.Cos(_cameraYaw), 0);
        up = Vector3.Normalize(Vector3.Cross(direction, right));
    }

    private readonly record struct Face
    {
        public Face(Vector3 normal, string label, int vertex0, int vertex1, int vertex2, int vertex3)
        {
            Normal = normal;
            Vertex0 = vertex0;
            Vertex1 = vertex1;
            Vertex2 = vertex2;
            Vertex3 = vertex3;
            Label = label;
        }

        public Vector3 Normal { get; }
        public string Label { get; }
        public int Vertex0 { get; }
        public int Vertex1 { get; }
        public int Vertex2 { get; }
        public int Vertex3 { get; }
    }
}