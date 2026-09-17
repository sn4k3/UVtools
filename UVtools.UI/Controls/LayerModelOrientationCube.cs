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
using Material.Icons;
using Material.Icons.Avalonia;

namespace UVtools.UI.Controls;

public sealed class LayerModelOrientationCube : Control
{
    public enum CubeHitTarget
    {
        None,
        Cube,
        Home,
        ArrowUp,
        ArrowDown,
        ArrowLeft,
        ArrowRight,
        RollCcw,
        RollCw,
        Turntable,
        Menu
    }

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

    private const double ProjectionScale = 0.22;

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

    private static readonly IBrush BackgroundBrush = new SolidColorBrush(Color.FromArgb(195, 18, 22, 29));
    private static readonly IBrush ShadowBrush = new SolidColorBrush(Color.FromArgb(90, 0, 0, 0));
    private static readonly IBrush FaceBrush = new SolidColorBrush(Color.FromRgb(74, 84, 99));
    private static readonly IBrush FaceSecondaryBrush = new SolidColorBrush(Color.FromRgb(61, 70, 84));
    private static readonly IBrush ActiveFaceBrush = new SolidColorBrush(Color.FromRgb(140, 185, 225));
    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.FromRgb(36, 150, 215));
    private static readonly IBrush ElementBrush = new SolidColorBrush(Color.FromRgb(190, 202, 218));
    private static readonly IBrush ElementHoverBrush = new SolidColorBrush(Color.FromRgb(56, 155, 255));
    private static readonly IBrush TurntableActiveBgBrush = new SolidColorBrush(Color.FromArgb(60, 56, 155, 255));
    private static readonly IBrush TurntableDotBrush = new SolidColorBrush(Color.FromRgb(0, 230, 160));
    private static readonly IBrush TextBrush = Brushes.White;
    private static readonly IBrush ActiveTextBrush = new SolidColorBrush(Color.FromRgb(35, 42, 54));

    private static readonly Pen BorderPen = new(new SolidColorBrush(Color.FromRgb(145, 157, 175)), 1);
    private static readonly Pen ActiveFacePen = new(new SolidColorBrush(Color.FromRgb(175, 212, 245)), 1.5);
    private static readonly Pen HighlightPen = new(new SolidColorBrush(Color.FromRgb(121, 213, 255)), 2);
    private static readonly Pen ElementBorderPen = new(new SolidColorBrush(Color.FromArgb(180, 20, 24, 32)), 1);
    private static readonly Pen ElementHoverPen = new(new SolidColorBrush(Color.FromRgb(121, 213, 255)), 1.5);
    private static readonly Pen TurntableActivePen = new(new SolidColorBrush(Color.FromRgb(56, 155, 255)), 1.2);
    private static readonly Pen ArcPen = new(new SolidColorBrush(Color.FromRgb(185, 198, 215)), 2.2);
    private static readonly Pen ArcHoverPen = new(new SolidColorBrush(Color.FromRgb(56, 155, 255)), 2.8);

    private static readonly Typeface LabelTypeface = new("Inter", FontStyle.Normal, FontWeight.SemiBold);
    private static readonly Cursor HandCursor = new(StandardCursorType.Hand);

    public static readonly StyledProperty<bool> IsTurntableActiveProperty =
        AvaloniaProperty.Register<LayerModelOrientationCube, bool>(nameof(IsTurntableActive), false);

    private readonly FormattedText?[] _activeFaceTexts = new FormattedText?[Faces.Length];

    private readonly FormattedText?[] _faceTexts = new FormattedText?[Faces.Length];

    private readonly Point[] _projectedVertices = new Point[Vertices.Length];
    private readonly int[] _visibleFaceIndices = new int[3];
    private float _cameraPitch = 0.55f;
    private float _cameraYaw = -0.8f;
    private float _cameraRoll;
    private IPointer? _capturedPointer;

    /// <summary>
    /// The hovered target cell, each component being -1, 0 or 1, where 0 spans the middle of that axis.
    /// One non-zero component targets a face, two an edge and three a corner.
    /// </summary>
    private Vector3? _hoverCell;

    private CubeHitTarget _hoverTarget = CubeHitTarget.None;
    private bool _isDragging;
    private Point _lastPointerPosition;
    private Point _pressedPosition;
    private CubeHitTarget _pressedTarget = CubeHitTarget.None;
    private ContextMenu? _contextMenu;
    private MenuItem? _turntableMenuItem;

    static LayerModelOrientationCube()
    {
        IsTurntableActiveProperty.Changed.AddClassHandler<LayerModelOrientationCube>((control, _) =>
            control.InvalidateVisual());
    }

    public LayerModelOrientationCube()
    {
        Cursor = HandCursor;
        Focusable = true;
    }

    public bool IsTurntableActive
    {
        get => GetValue(IsTurntableActiveProperty);
        set => SetValue(IsTurntableActiveProperty, value);
    }

    public event Action<double, double>? OrbitRequested;
    public event Action<Vector3>? SnapRequested;
    public event Action? HomeRequested;
    public event Action<float, float>? RotateRequested;
    public event Action<float>? RollRequested;
    public event Action? TurntableToggleRequested;
    public event Action? ProjectionToggleRequested;
    public event Action? FitToViewRequested;

    public void SetCameraOrientation(float yaw, float pitch, float roll = 0f)
    {
        if (_cameraYaw.Equals(yaw) && _cameraPitch.Equals(pitch) && _cameraRoll.Equals(roll)) return;
        _cameraYaw = yaw;
        _cameraPitch = pitch;
        _cameraRoll = roll;
        if (_capturedPointer is not null) _hoverCell = null;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.DrawRectangle(BackgroundBrush, BorderPen, new Rect(Bounds.Size), 10, 10);

        GetProjection(out var center, out var scale, out var direction, out var right, out var up);

        // Ground drop shadow under cube
        context.DrawEllipse(ShadowBrush, null, new Point(center.X, center.Y + scale * 1.42), scale * 1.25,
            scale * 0.22);

        // Project cube vertices
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

        /* Visible faces of a convex cube tile the silhouette without overlapping each other. */
        for (var index = 0; index < visibleCount; index++)
        {
            var face = Faces[_visibleFaceIndices[index]];
            var isFacingCamera = Vector3.Dot(face.Normal, direction) > 0.96f;

            context.DrawGeometry(index % 2 == 0 ? FaceBrush : FaceSecondaryBrush, BorderPen,
                CreateFaceGeometry(face));

            if (isFacingCamera)
            {
                // Active front-facing face has a light blue center highlight
                context.DrawGeometry(ActiveFaceBrush, ActiveFacePen,
                    CreateCellGeometry(face, face.Normal, center, scale, right, up));
            }
        }

        if (_hoverTarget == CubeHitTarget.Cube && _hoverCell is { } hoverCell)
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
            var face = Faces[faceIndex];
            var isFacingCamera = Vector3.Dot(face.Normal, direction) > 0.96f;
            DrawFaceLabel(context, faceIndex, face, isFacingCamera);
        }

        // Draw ViewCube UI controls: Home, 4 Directional Arrows, Roll Arc, Turntable, and Menu
        DrawViewCubeControls(context, center, scale);
    }

    private void DrawViewCubeControls(DrawingContext context, Point center, double scale)
    {
        // 1. Home Icon (top-left)
        var isHomeHover = _hoverTarget == CubeHitTarget.Home;
        var homeBrush = isHomeHover ? ElementHoverBrush : ElementBrush;
        var homePen = isHomeHover ? ElementHoverPen : ElementBorderPen;
        context.DrawGeometry(homeBrush, homePen, CreateHomeGeometry(center, scale));

        // 2. Directional Arrows
        var isUpHover = _hoverTarget == CubeHitTarget.ArrowUp;
        context.DrawGeometry(isUpHover ? ElementHoverBrush : ElementBrush,
            isUpHover ? ElementHoverPen : ElementBorderPen, CreateArrowUpGeometry(center, scale));

        var isDownHover = _hoverTarget == CubeHitTarget.ArrowDown;
        context.DrawGeometry(isDownHover ? ElementHoverBrush : ElementBrush,
            isDownHover ? ElementHoverPen : ElementBorderPen, CreateArrowDownGeometry(center, scale));

        var isLeftHover = _hoverTarget == CubeHitTarget.ArrowLeft;
        context.DrawGeometry(isLeftHover ? ElementHoverBrush : ElementBrush,
            isLeftHover ? ElementHoverPen : ElementBorderPen, CreateArrowLeftGeometry(center, scale));

        var isRightHover = _hoverTarget == CubeHitTarget.ArrowRight;
        context.DrawGeometry(isRightHover ? ElementHoverBrush : ElementBrush,
            isRightHover ? ElementHoverPen : ElementBorderPen, CreateArrowRightGeometry(center, scale));

        // 3. Roll Curved Arrows (top-right corner with ample clearance from cube)
        GetRollArcPoints(center, scale, out var pCcw, out var pCw, out var rArc);
        var isCcwHover = _hoverTarget == CubeHitTarget.RollCcw;
        var isCwHover = _hoverTarget == CubeHitTarget.RollCw;

        context.DrawGeometry(null, isCcwHover || isCwHover ? ArcHoverPen : ArcPen,
            CreateRollArcGeometry(pCcw, pCw, rArc));

        context.DrawGeometry(isCcwHover ? ElementHoverBrush : ElementBrush,
            isCcwHover ? ElementHoverPen : ElementBorderPen, CreateRollCcwArrowGeometry(pCcw));

        context.DrawGeometry(isCwHover ? ElementHoverBrush : ElementBrush,
            isCwHover ? ElementHoverPen : ElementBorderPen, CreateRollCwArrowGeometry(pCw));

        // 4. Turntable / Auto-Rotate Button (bottom-left, styled equal to Home)
        var isTurntableHover = _hoverTarget == CubeHitTarget.Turntable;
        var isTurntableOn = IsTurntableActive;
        var ttCenter = new Point(center.X - scale * 1.55, center.Y + scale * 1.75);

        if (isTurntableOn)
        {
            context.DrawRectangle(TurntableActiveBgBrush, TurntableActivePen,
                new Rect(ttCenter.X - 10, ttCenter.Y - 9, 20, 19), 3, 3);
            context.DrawEllipse(TurntableDotBrush, null,
                new Point(ttCenter.X + 6.5, ttCenter.Y - 6.5), 2, 2);
        }

        var ttBrush = isTurntableOn || isTurntableHover ? ElementHoverBrush : ElementBrush;
        var ttPen = isTurntableOn || isTurntableHover ? ElementHoverPen : ElementBorderPen;
        context.DrawGeometry(ttBrush, ttPen, CreateTurntableGeometry(center, scale));

        // 5. Menu Button (bottom-right dropdown arrow, pushed more to bottom)
        var isMenuHover = _hoverTarget == CubeHitTarget.Menu;
        var menuBrush = isMenuHover ? ElementHoverBrush : ElementBrush;
        var menuPen = isMenuHover ? ElementHoverPen : ElementBorderPen;
        context.DrawGeometry(menuBrush, menuPen, CreateMenuGeometry(center, scale));
    }

    private static StreamGeometry CreateHomeGeometry(Point center, double scale)
    {
        var hc = new Point(center.X - scale * 1.55, center.Y - scale * 1.55);
        const double half = 7.5;
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(hc.X, hc.Y - half));
        ctx.LineTo(new Point(hc.X + half + 1, hc.Y - 1));
        ctx.LineTo(new Point(hc.X + half - 1.5, hc.Y - 1));
        ctx.LineTo(new Point(hc.X + half - 1.5, hc.Y + half));
        ctx.LineTo(new Point(hc.X + 2, hc.Y + half));
        ctx.LineTo(new Point(hc.X + 2, hc.Y + 2));
        ctx.LineTo(new Point(hc.X - 2, hc.Y + 2));
        ctx.LineTo(new Point(hc.X - 2, hc.Y + half));
        ctx.LineTo(new Point(hc.X - half + 1.5, hc.Y + half));
        ctx.LineTo(new Point(hc.X - half + 1.5, hc.Y - 1));
        ctx.LineTo(new Point(hc.X - half - 1, hc.Y - 1));
        ctx.EndFigure(true);
        return geometry;
    }

    private static StreamGeometry CreateArrowUpGeometry(Point center, double scale)
    {
        var dTip = scale * 1.25;
        var dBase = dTip + 8.5;
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(center.X, center.Y - dTip));
        ctx.LineTo(new Point(center.X - 6.5, center.Y - dBase));
        ctx.LineTo(new Point(center.X + 6.5, center.Y - dBase));
        ctx.EndFigure(true);
        return geometry;
    }

    private static StreamGeometry CreateArrowDownGeometry(Point center, double scale)
    {
        var dTip = scale * 1.25;
        var dBase = dTip + 8.5;
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(center.X, center.Y + dTip));
        ctx.LineTo(new Point(center.X - 6.5, center.Y + dBase));
        ctx.LineTo(new Point(center.X + 6.5, center.Y + dBase));
        ctx.EndFigure(true);
        return geometry;
    }

    private static StreamGeometry CreateArrowLeftGeometry(Point center, double scale)
    {
        var dTip = scale * 1.25;
        var dBase = dTip + 8.5;
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(center.X - dTip, center.Y));
        ctx.LineTo(new Point(center.X - dBase, center.Y - 6.5));
        ctx.LineTo(new Point(center.X - dBase, center.Y + 6.5));
        ctx.EndFigure(true);
        return geometry;
    }

    private static StreamGeometry CreateArrowRightGeometry(Point center, double scale)
    {
        var dTip = scale * 1.25;
        var dBase = dTip + 8.5;
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(new Point(center.X + dTip, center.Y));
        ctx.LineTo(new Point(center.X + dBase, center.Y - 6.5));
        ctx.LineTo(new Point(center.X + dBase, center.Y + 6.5));
        ctx.EndFigure(true);
        return geometry;
    }

    private static void GetRollArcPoints(Point center, double scale,
        out Point pCcw, out Point pCw, out double rArc)
    {
        rArc = scale * 2.1;
        const double aStart = -1.2566; // -72 deg
        const double aEnd = -0.3142; // -18 deg
        pCcw = new Point(center.X + rArc * Math.Cos(aStart), center.Y + rArc * Math.Sin(aStart));
        pCw = new Point(center.X + rArc * Math.Cos(aEnd), center.Y + rArc * Math.Sin(aEnd));
    }

    private static StreamGeometry CreateRollArcGeometry(Point pCcw, Point pCw, double rArc)
    {
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(pCcw);
        ctx.ArcTo(pCw, new Size(rArc, rArc), 0, false, SweepDirection.Clockwise);
        ctx.EndFigure(false);
        return geometry;
    }

    private static StreamGeometry CreateRollCcwArrowGeometry(Point pCcw)
    {
        const double tx = -0.951;
        const double ty = -0.309;
        const double nx = 0.309;
        const double ny = -0.951;
        const double len = 6.5;
        const double w = 4.0;

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(pCcw);
        ctx.LineTo(new Point(pCcw.X - tx * len + nx * w, pCcw.Y - ty * len + ny * w));
        ctx.LineTo(new Point(pCcw.X - tx * len - nx * w, pCcw.Y - ty * len - ny * w));
        ctx.EndFigure(true);
        return geometry;
    }

    private static StreamGeometry CreateRollCwArrowGeometry(Point pCw)
    {
        const double tx = 0.309;
        const double ty = 0.951;
        const double nx = -0.951;
        const double ny = 0.309;
        const double len = 6.5;
        const double w = 4.0;

        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();
        ctx.BeginFigure(pCw);
        ctx.LineTo(new Point(pCw.X - tx * len + nx * w, pCw.Y - ty * len + ny * w));
        ctx.LineTo(new Point(pCw.X - tx * len - nx * w, pCw.Y - ty * len - ny * w));
        ctx.EndFigure(true);
        return geometry;
    }

    private static StreamGeometry CreateTurntableGeometry(Point center, double scale)
    {
        var tc = new Point(center.X - scale * 1.55, center.Y + scale * 1.75);
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();

        // Figure 1: Circular orbit sweep ribbon with arrowhead
        ctx.BeginFigure(new Point(tc.X - 3.5, tc.Y + 3.0), true);
        ctx.ArcTo(new Point(tc.X, tc.Y - 7.0), new Size(7.0, 7.0), 0, false, SweepDirection.Clockwise);
        ctx.ArcTo(new Point(tc.X + 6.0, tc.Y - 1.5), new Size(7.0, 7.0), 0, false, SweepDirection.Clockwise);
        ctx.LineTo(new Point(tc.X + 8.5, tc.Y - 1.5));
        ctx.LineTo(new Point(tc.X + 5.0, tc.Y + 3.5));
        ctx.LineTo(new Point(tc.X + 1.8, tc.Y + 0.0));
        ctx.LineTo(new Point(tc.X + 3.6, tc.Y + 0.0));
        ctx.ArcTo(new Point(tc.X, tc.Y - 4.2), new Size(4.2, 4.2), 0, false, SweepDirection.CounterClockwise);
        ctx.ArcTo(new Point(tc.X - 2.1, tc.Y + 1.8), new Size(4.2, 4.2), 0, false, SweepDirection.CounterClockwise);
        ctx.EndFigure(true);

        // Figure 2: Central turntable platter pedestal
        ctx.BeginFigure(new Point(tc.X - 4.5, tc.Y + 4.5), true);
        ctx.LineTo(new Point(tc.X + 4.5, tc.Y + 4.5));
        ctx.LineTo(new Point(tc.X + 3.5, tc.Y + 6.8));
        ctx.LineTo(new Point(tc.X - 3.5, tc.Y + 6.8));
        ctx.EndFigure(true);

        // Figure 3: Spindle pin
        ctx.BeginFigure(new Point(tc.X - 1.2, tc.Y - 0.5), true);
        ctx.LineTo(new Point(tc.X + 1.2, tc.Y - 0.5));
        ctx.LineTo(new Point(tc.X + 1.2, tc.Y + 3.5));
        ctx.LineTo(new Point(tc.X - 1.2, tc.Y + 3.5));
        ctx.EndFigure(true);

        return geometry;
    }

    private static StreamGeometry CreateMenuGeometry(Point center, double scale)
    {
        var mc = new Point(center.X + scale * 1.55, center.Y + scale * 1.75);
        var geometry = new StreamGeometry();
        using var ctx = geometry.Open();

        // Figure 1: Horizontal header bar
        ctx.BeginFigure(new Point(mc.X - 5.5, mc.Y - 3.5), true);
        ctx.LineTo(new Point(mc.X + 5.5, mc.Y - 3.5));
        ctx.LineTo(new Point(mc.X + 5.5, mc.Y - 1.5));
        ctx.LineTo(new Point(mc.X - 5.5, mc.Y - 1.5));
        ctx.EndFigure(true);

        // Figure 2: Downward triangle
        ctx.BeginFigure(new Point(mc.X - 4.5, mc.Y + 0.5), true);
        ctx.LineTo(new Point(mc.X + 4.5, mc.Y + 0.5));
        ctx.LineTo(new Point(mc.X, mc.Y + 5.0));
        ctx.EndFigure(true);

        return geometry;
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

        static float ComponentCenter(float normal, float cell)
        {
            return normal != 0
                ? normal
                : cell == 0
                    ? 0
                    : MathF.CopySign(CellOuterCenter, cell);
        }

        static float ComponentHalf(float normal, float cell)
        {
            return normal != 0
                ? 0
                : cell == 0
                    ? TargetEdgeThreshold
                    : CellOuterHalf;
        }
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

    private void DrawFaceLabel(DrawingContext context, int faceIndex, Face face, bool isFacingCamera)
    {
        var center = new Point(
            (_projectedVertices[face.Vertex0].X + _projectedVertices[face.Vertex1].X +
             _projectedVertices[face.Vertex2].X + _projectedVertices[face.Vertex3].X) / 4,
            (_projectedVertices[face.Vertex0].Y + _projectedVertices[face.Vertex1].Y +
             _projectedVertices[face.Vertex2].Y + _projectedVertices[face.Vertex3].Y) / 4);

        var text = isFacingCamera
            ? _activeFaceTexts[faceIndex] ??= new FormattedText(face.Label, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, LabelTypeface, face.Label.Length > 5 ? 7.5 : 8.5, ActiveTextBrush)
            : _faceTexts[faceIndex] ??= new FormattedText(face.Label, CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight, LabelTypeface, face.Label.Length > 5 ? 7 : 8, TextBrush);

        context.DrawText(text, new Point(center.X - text.Width / 2, center.Y - text.Height / 2));
    }

    private CubeHitTarget HitTest(Point p)
    {
        if (Math.Min(Bounds.Width, Bounds.Height) <= 0) return CubeHitTarget.None;
        GetProjection(out var center, out var scale, out _, out _, out _);

        // Home button (top-left)
        var homeCenter = new Point(center.X - scale * 1.55, center.Y - scale * 1.55);
        if (new Rect(homeCenter.X - 10, homeCenter.Y - 10, 20, 20).Contains(p))
            return CubeHitTarget.Home;

        // Turntable button (bottom-left)
        var turntableCenter = new Point(center.X - scale * 1.55, center.Y + scale * 1.75);
        if (new Rect(turntableCenter.X - 10, turntableCenter.Y - 10, 20, 20).Contains(p))
            return CubeHitTarget.Turntable;

        // Menu button (bottom-right)
        var menuCenter = new Point(center.X + scale * 1.55, center.Y + scale * 1.75);
        if (new Rect(menuCenter.X - 10, menuCenter.Y - 10, 20, 20).Contains(p))
            return CubeHitTarget.Menu;

        // Directional arrows
        var dTip = scale * 1.25;
        var dBase = dTip + 8.5;

        // ArrowUp
        if (new Rect(center.X - 11, center.Y - dBase - 2, 22, 14).Contains(p))
            return CubeHitTarget.ArrowUp;

        // ArrowDown
        if (new Rect(center.X - 11, center.Y + dTip - 1, 22, 14).Contains(p))
            return CubeHitTarget.ArrowDown;

        // ArrowLeft
        if (new Rect(center.X - dBase - 2, center.Y - 11, 14, 22).Contains(p))
            return CubeHitTarget.ArrowLeft;

        // ArrowRight
        if (new Rect(center.X + dTip - 1, center.Y - 11, 14, 22).Contains(p))
            return CubeHitTarget.ArrowRight;

        // Roll arc in top-right corner
        var dx = p.X - center.X;
        var dy = p.Y - center.Y;
        var distSq = dx * dx + dy * dy;
        var rMin = scale * 1.7;
        var rMax = scale * 2.45;
        if (distSq >= rMin * rMin && distSq <= rMax * rMax && dx > 8 && dy < -8)
        {
            var angle = Math.Atan2(dy, dx);
            if (angle is >= -Math.PI / 2 and <= 0)
            {
                return angle < -Math.PI / 4 ? CubeHitTarget.RollCcw : CubeHitTarget.RollCw;
            }
        }

        // Test 3D cube
        if (HitTestCell(p) != null)
            return CubeHitTarget.Cube;

        return CubeHitTarget.None;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        var pointerPoint = e.GetCurrentPoint(this);
        if (pointerPoint.Properties.IsRightButtonPressed)
        {
            ShowContextMenu();
            e.Handled = true;
            return;
        }

        if (!pointerPoint.Properties.IsLeftButtonPressed) return;
        Focus();
        _capturedPointer = e.Pointer;
        _pressedPosition = _lastPointerPosition = e.GetPosition(this);
        _pressedTarget = HitTest(_pressedPosition);
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
            if (_isDragging && _pressedTarget is CubeHitTarget.Cube or CubeHitTarget.None)
            {
                OrbitRequested?.Invoke(delta.X, delta.Y);
            }

            e.Handled = true;
            return;
        }

        var newTarget = HitTest(currentPosition);
        var newHoverCell = newTarget == CubeHitTarget.Cube ? HitTestCell(currentPosition) : null;

        if (_hoverTarget != newTarget || _hoverCell != newHoverCell)
        {
            _hoverTarget = newTarget;
            _hoverCell = newHoverCell;
            UpdateTooltip(newTarget);
            InvalidateVisual();
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!ReferenceEquals(_capturedPointer, e.Pointer)) return;

        var releasePosition = e.GetPosition(this);
        var releaseTarget = HitTest(releasePosition);

        if (!_isDragging && releaseTarget == _pressedTarget)
        {
            switch (releaseTarget)
            {
                case CubeHitTarget.Home:
                    HomeRequested?.Invoke();
                    break;
                case CubeHitTarget.ArrowUp:
                    RotateRequested?.Invoke(0, MathF.PI / 2f);
                    break;
                case CubeHitTarget.ArrowDown:
                    RotateRequested?.Invoke(0, -MathF.PI / 2f);
                    break;
                case CubeHitTarget.ArrowLeft:
                    RotateRequested?.Invoke(-MathF.PI / 2f, 0);
                    break;
                case CubeHitTarget.ArrowRight:
                    RotateRequested?.Invoke(MathF.PI / 2f, 0);
                    break;
                case CubeHitTarget.RollCcw:
                    RollRequested?.Invoke(-MathF.PI / 2f);
                    break;
                case CubeHitTarget.RollCw:
                    RollRequested?.Invoke(MathF.PI / 2f);
                    break;
                case CubeHitTarget.Turntable:
                    TurntableToggleRequested?.Invoke();
                    break;
                case CubeHitTarget.Menu:
                    ShowContextMenu();
                    break;
                case CubeHitTarget.Cube:
                    if (HitTestCell(releasePosition) is { } cell)
                    {
                        SnapRequested?.Invoke(Vector3.Normalize(cell));
                    }

                    break;
            }
        }

        e.Pointer.Capture(null);
        ResetPointerState();
        e.Handled = true;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (_capturedPointer is not null) return;
        _hoverTarget = CubeHitTarget.None;
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
        _hoverTarget = CubeHitTarget.None;
        _hoverCell = null;
        InvalidateVisual();
    }

    private void UpdateTooltip(CubeHitTarget target)
    {
        ToolTip.SetTip(this, target switch
        {
            CubeHitTarget.Home => "Home view (Reset camera) [0 / Home]",
            CubeHitTarget.ArrowUp => "Rotate view up 90° [Up]",
            CubeHitTarget.ArrowDown => "Rotate view down 90° [Down]",
            CubeHitTarget.ArrowLeft => "Rotate view left 90° [Left]",
            CubeHitTarget.ArrowRight => "Rotate view right 90° [Right]",
            CubeHitTarget.RollCcw => "Rotate 90° counter-clockwise",
            CubeHitTarget.RollCw => "Rotate 90° clockwise",
            CubeHitTarget.Turntable => IsTurntableActive
                ? "Stop turntable / auto-rotate view [T or Space]"
                : "Turntable / auto-rotate view [T or Space]",
            CubeHitTarget.Menu => "View options & projections",
            _ => "Drag to orbit. Click a face, edge, or corner to align the view."
        });
    }

    public void ShowContextMenu()
    {
        if (_contextMenu is null)
        {
            _contextMenu = new ContextMenu();

            var homeItem = new MenuItem
            {
                Icon = new MaterialIcon
                {
                    Kind = MaterialIconKind.CubeScan
                },
                InputGesture = new KeyGesture(Key.Home),
                Header = "Home View (Isometric)"
            };
            homeItem.Click += (_, _) => HomeRequested?.Invoke();
            _contextMenu.Items.Add(homeItem);

            _turntableMenuItem = new MenuItem
            {
                Icon = new MaterialIcon
                {
                    Kind = MaterialIconKind.Rotate360
                },
                InputGesture = new KeyGesture(Key.T),
                ToggleType = MenuItemToggleType.CheckBox
            };
            _turntableMenuItem.Click += (_, _) => TurntableToggleRequested?.Invoke();
            _contextMenu.Items.Add(_turntableMenuItem);

            var fitItem = new MenuItem
            {
                Icon = new MaterialIcon
                {
                    Kind = MaterialIconKind.FitToScreen
                },
                InputGesture = new KeyGesture(Key.F),
                Header = "Fit to View"
            };
            fitItem.Click += (_, _) => FitToViewRequested?.Invoke();
            _contextMenu.Items.Add(fitItem);

            var projItem = new MenuItem
            {
                Icon = new MaterialIcon
                {
                    Kind = MaterialIconKind.CameraOutline
                },
                InputGesture = new KeyGesture(Key.P),
                Header = "Toggle Orthographic / Perspective"
            };
            projItem.Click += (_, _) => ProjectionToggleRequested?.Invoke();
            _contextMenu.Items.Add(projItem);

            _contextMenu.Items.Add(new Separator());

            var frontItem = new MenuItem
            {
                Header = "Front View [1]",
                InputGesture = new KeyGesture(Key.D1)
            };
            frontItem.Click += (_, _) => SnapRequested?.Invoke(-Vector3.UnitY);
            _contextMenu.Items.Add(frontItem);

            var backItem = new MenuItem
            {
                Header = "Back View [2]",
                InputGesture = new KeyGesture(Key.D2)
            };
            backItem.Click += (_, _) => SnapRequested?.Invoke(Vector3.UnitY);
            _contextMenu.Items.Add(backItem);

            var leftItem = new MenuItem
            {
                Header = "Left View [3]",
                InputGesture = new KeyGesture(Key.D3)
            };
            leftItem.Click += (_, _) => SnapRequested?.Invoke(-Vector3.UnitX);
            _contextMenu.Items.Add(leftItem);

            var rightItem = new MenuItem
            {
                Header = "Right View [4]",
                InputGesture = new KeyGesture(Key.D4)
            };
            rightItem.Click += (_, _) => SnapRequested?.Invoke(Vector3.UnitX);
            _contextMenu.Items.Add(rightItem);

            var topItem = new MenuItem
            {
                Header = "Top View [5]",
                InputGesture = new KeyGesture(Key.D5)
            };
            topItem.Click += (_, _) => SnapRequested?.Invoke(Vector3.UnitZ);
            _contextMenu.Items.Add(topItem);

            var bottomItem = new MenuItem
            {
                Header = "Bottom View [6]",
                InputGesture = new KeyGesture(Key.D6)
            };
            bottomItem.Click += (_, _) => SnapRequested?.Invoke(-Vector3.UnitZ);
            _contextMenu.Items.Add(bottomItem);

            ContextMenu = _contextMenu;
        }

        if (_turntableMenuItem is not null)
        {
            _turntableMenuItem.IsChecked = IsTurntableActive;
            _turntableMenuItem.Header = IsTurntableActive ? "Stop Turntable [T]" : "Start Turntable [T]";
        }

        _contextMenu.Open(this);
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
        var baseRight = new Vector3(-MathF.Sin(_cameraYaw), MathF.Cos(_cameraYaw), 0);
        var baseUp = Vector3.Normalize(Vector3.Cross(direction, baseRight));

        if (MathF.Abs(_cameraRoll) > 0.0001f)
        {
            var rollMatrix = Matrix4x4.CreateFromAxisAngle(direction, _cameraRoll);
            right = Vector3.Transform(baseRight, rollMatrix);
            up = Vector3.Transform(baseUp, rollMatrix);
        }
        else
        {
            right = baseRight;
            up = baseUp;
        }
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