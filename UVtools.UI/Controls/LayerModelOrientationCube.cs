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
    private const float TargetEdgeThreshold = 0.58f;

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
    private Vector3? _hoverDirection;

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
        if (_capturedPointer is not null) _hoverDirection = null;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(Bounds.Size);
        context.DrawRectangle(BackgroundBrush, BorderPen, bounds, 8, 8);

        GetCameraBasis(out var direction, out var right, out var up);
        var center = bounds.Center;
        var scale = Math.Max(1, Math.Min(bounds.Width, bounds.Height) * 0.27);
        for (var index = 0; index < Vertices.Length; index++)
        {
            var vertex = Vertices[index];
            _projectedVertices[index] = new Point(
                center.X + Vector3.Dot(vertex, right) * scale,
                center.Y - Vector3.Dot(vertex, up) * scale);
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

        for (var index = 0; index < visibleCount; index++)
        {
            var faceIndex = _visibleFaceIndices[index];
            var face = Faces[faceIndex];
            var highlighted = _hoverDirection is { } hoverDirection &&
                              FaceMatchesDirection(face.Normal, hoverDirection);
            var geometry = CreateFaceGeometry(face);
            context.DrawGeometry(highlighted ? HighlightBrush : index % 2 == 0 ? FaceBrush : FaceSecondaryBrush,
                highlighted ? HighlightPen : BorderPen, geometry);
            DrawFaceLabel(context, faceIndex, face);
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

        var hoverDirection = HitTestDirection(currentPosition);
        if (_hoverDirection == hoverDirection) return;
        _hoverDirection = hoverDirection;
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!ReferenceEquals(_capturedPointer, e.Pointer)) return;
        if (!_isDragging && HitTestDirection(e.GetPosition(this)) is { } direction)
        {
            SnapRequested?.Invoke(direction);
        }

        e.Pointer.Capture(null);
        ResetPointerState();
        e.Handled = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_capturedPointer is not null) return;
        _hoverDirection = null;
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
        _hoverDirection = null;
        InvalidateVisual();
    }

    private Vector3? HitTestDirection(Point point)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0) return null;
        var scale = Math.Max(1, size * 0.27);
        var center = new Rect(Bounds.Size).Center;
        GetCameraBasis(out var direction, out var right, out var up);
        var localX = (float)((point.X - center.X) / scale);
        var localY = (float)((center.Y - point.Y) / scale);
        var rayOrigin = right * localX + up * localY + direction * 4;
        var rayDirection = -direction;
        if (!TryIntersectCube(rayOrigin, rayDirection, out var hitPoint)) return null;

        var target = new Vector3(
            Math.Abs(hitPoint.X) >= TargetEdgeThreshold ? MathF.CopySign(1, hitPoint.X) : 0,
            Math.Abs(hitPoint.Y) >= TargetEdgeThreshold ? MathF.CopySign(1, hitPoint.Y) : 0,
            Math.Abs(hitPoint.Z) >= TargetEdgeThreshold ? MathF.CopySign(1, hitPoint.Z) : 0);
        return target.LengthSquared() > 0 ? Vector3.Normalize(target) : null;
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

    private static bool FaceMatchesDirection(Vector3 faceNormal, Vector3 direction)
    {
        return Math.Abs(Vector3.Dot(faceNormal, direction)) > 0.5f &&
               MathF.Sign(Vector3.Dot(faceNormal, direction)) > 0;
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