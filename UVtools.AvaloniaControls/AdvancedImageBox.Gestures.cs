/*
 *                               The MIT License (MIT)
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so.
 */

using System;
using System.Linq;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace UVtools.AvaloniaControls;

public partial class AdvancedImageBox
{
    #region Touch and touchpad gestures

    private bool _isPinching;
    private double _pinchStartZoom;
    private Point _pinchAnchor;
    private double _magnifyZoom;
    private IPointer? _activePointer;

    public static readonly StyledProperty<bool> EnableTouchGesturesProperty =
        AvaloniaProperty.Register<AdvancedImageBox, bool>(nameof(EnableTouchGestures), true);

    /// <summary>
    /// Gets or sets if the touch and touchpad gestures zoom and pan the image. Default is <c>true</c>.
    /// </summary>
    /// <remarks>
    /// <para>Pinching with two fingers zooms around the center of the fingers, which is also panned with them: the
    /// point of the image under the center of the fingers when the pinch started stays under it. The magnify gesture of
    /// a touchpad (macOS) zooms around the pointer. The zoom is limited by <see cref="MinZoom"/>, <see cref="MaxZoom"/>
    /// and <see cref="ConstrainZoomOutToFitLevel"/> as any other zoom, and the gestures work in
    /// <see cref="SizeModes.Normal"/> mode, like the mouse wheel.</para>
    /// <para>Dragging a single finger keeps panning like the left mouse button does, see
    /// <see cref="PanWithMouseButtons"/>. The two-finger scroll of a touchpad arrives as mouse wheel events, so it
    /// keeps working as the mouse wheel does.</para>
    /// <para>It is on by default because the pinch and magnify events only exist for touch screens and touchpads, so
    /// the behaviour of a mouse is not touched.</para>
    /// </remarks>
    public bool EnableTouchGestures
    {
        get => GetValue(EnableTouchGesturesProperty);
        set => SetValue(EnableTouchGesturesProperty, value);
    }

    /// <summary>
    /// Subscribes to the gestures, called by the constructor.
    /// </summary>
    private void InitializeGestures()
    {
        AddHandler(PinchEvent, OnPinch);
        AddHandler(PinchEndedEvent, OnPinchEnded);
        AddHandler(PointerTouchPadGestureMagnifyEvent, OnTouchPadMagnify);
    }

    /// <summary>
    /// Makes sure the viewport recognizes the pinch gesture.
    /// </summary>
    private void EnsurePinchRecognizer()
    {
        var viewPort = ViewPort;
        if (viewPort is null) return;
        if (viewPort.GestureRecognizers.Any(recognizer => recognizer is PinchGestureRecognizer)) return;
        viewPort.GestureRecognizers.Add(new PinchGestureRecognizer());
    }

    private void OnPinch(object? sender, PinchEventArgs e)
    {
        if (!EnableTouchGestures || !IsImageLoaded || SizeMode != SizeModes.Normal) return;

        if (!_isPinching)
        {
            // The finger that was panning before the second one touched stops, the pinch pans from now on
            _isPinching = true;
            _pinchStartZoom = Zoom;
            _pinchAnchor = PointToView(e.ScaleOrigin);
            IsPanning = false;
            IsSelecting = false;
        }

        var scale = e.Scale;
        if (!double.IsFinite(scale) || scale <= 0) return;

        Zoom = (int)Math.Round(_pinchStartZoom * scale);
        ScrollToView(_pinchAnchor, e.ScaleOrigin);
        e.Handled = true;
    }

    private void OnPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        _isPinching = false;
    }

    private void OnTouchPadMagnify(object? sender, PointerDeltaEventArgs e)
    {
        if (!EnableTouchGestures || !IsImageLoaded || SizeMode != SizeModes.Normal) return;

        var viewPort = ViewPort;
        if (viewPort is null) return;

        var delta = e.Delta.X;
        if (!double.IsFinite(delta) || delta <= -1) return;

        // The zoom is an integer, so a small gesture is accumulated until it changes it
        if ((int)Math.Round(_magnifyZoom) != Zoom) _magnifyZoom = Zoom;
        _magnifyZoom *= 1 + delta;

        var position = e.GetPosition(viewPort);
        var anchor = PointToView(position);
        Zoom = (int)Math.Round(_magnifyZoom);
        if (Zoom != (int)Math.Round(_magnifyZoom)) _magnifyZoom = Zoom; // Stopped by a limit
        ScrollToView(anchor, position);
        e.Handled = true;
    }

    #endregion
}
