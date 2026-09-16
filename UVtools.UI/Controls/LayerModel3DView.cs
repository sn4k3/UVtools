/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Avalonia.Threading;
using Silk.NET.OpenGL;
using UVtools.Core.Layers;
using UVtools.Core.Voxel;

namespace UVtools.UI.Controls;

public sealed class LayerModel3DView : OpenGlControlBase, ICustomHitTest
{
    private const string DesktopVertexShader = """
                                               #version 330 core
                                               layout (location = 0) in vec3 aPosition;
                                               layout (location = 1) in vec3 aNormal;
                                               uniform mat4 uViewProjection;
                                               out vec3 vNormal;
                                               out vec3 vWorldPosition;
                                               void main()
                                               {
                                                   vNormal = aNormal;
                                                   vWorldPosition = aPosition;
                                                   gl_Position = uViewProjection * vec4(aPosition, 1.0);
                                               }
                                               """;

    private const string DesktopFragmentShader = """
                                                 #version 330 core
                                                 in vec3 vNormal;
                                                 in vec3 vWorldPosition;
                                                  uniform vec3 uColor;
                                                  uniform float uAlpha;
                                                  uniform int uUnlit;
                                                  uniform vec3 uLightDirection;
                                                  uniform float uAmbientLight;
                                                 uniform float uClipZ;
                                                 uniform int uClipEnabled;
                                                 out vec4 fragmentColor;
                                                 void main()
                                                 {
                                                     if (uClipEnabled != 0 && vWorldPosition.z > uClipZ) discard;
                                                     float diffuse = max(dot(normalize(vNormal), uLightDirection), 0.0);
                                                     float lighting = uAmbientLight + diffuse * (1.0 - uAmbientLight);
                                                      vec3 color = uUnlit != 0 ? uColor : uColor * lighting;
                                                      fragmentColor = vec4(color, uAlpha);
                                                 }
                                                 """;

    private const string EsVertexShader = """
                                          #version 300 es
                                          precision highp float;
                                          layout (location = 0) in vec3 aPosition;
                                          layout (location = 1) in vec3 aNormal;
                                          uniform mat4 uViewProjection;
                                          out vec3 vNormal;
                                          out vec3 vWorldPosition;
                                          void main()
                                          {
                                              vNormal = aNormal;
                                              vWorldPosition = aPosition;
                                              gl_Position = uViewProjection * vec4(aPosition, 1.0);
                                          }
                                          """;

    private const string EsFragmentShader = """
                                            #version 300 es
                                            precision highp float;
                                            in vec3 vNormal;
                                            in vec3 vWorldPosition;
                                             uniform vec3 uColor;
                                             uniform float uAlpha;
                                             uniform int uUnlit;
                                             uniform vec3 uLightDirection;
                                             uniform float uAmbientLight;
                                            uniform float uClipZ;
                                            uniform int uClipEnabled;
                                            out vec4 fragmentColor;
                                            void main()
                                            {
                                                if (uClipEnabled != 0 && vWorldPosition.z > uClipZ) discard;
                                                float diffuse = max(dot(normalize(vNormal), uLightDirection), 0.0);
                                                float lighting = uAmbientLight + diffuse * (1.0 - uAmbientLight);
                                                 vec3 color = uUnlit != 0 ? uColor : uColor * lighting;
                                                 fragmentColor = vec4(color, uAlpha);
                                            }
                                            """;

    public static readonly StyledProperty<Avalonia.Media.Color> VoxelColorProperty =
        AvaloniaProperty.Register<LayerModel3DView, Avalonia.Media.Color>(nameof(VoxelColor),
            Avalonia.Media.Color.FromRgb(51, 184, 235));

    public static readonly StyledProperty<Avalonia.Media.Color> BackgroundColorProperty =
        AvaloniaProperty.Register<LayerModel3DView, Avalonia.Media.Color>(nameof(BackgroundColor),
            Avalonia.Media.Color.FromRgb(14, 17, 20));

    public static readonly StyledProperty<bool> IsOrthographicProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(IsOrthographic));

    public static readonly StyledProperty<VoxelPreviewLightingMode> LightingModeProperty =
        AvaloniaProperty.Register<LayerModel3DView, VoxelPreviewLightingMode>(nameof(LightingMode));

    public static readonly StyledProperty<VoxelPreviewRenderMode> RenderModeProperty =
        AvaloniaProperty.Register<LayerModel3DView, VoxelPreviewRenderMode>(nameof(RenderMode));

    private const float OrbitSensitivity = 0.008f;
    private const double CameraAnimationSeconds = 0.2;
    private const float XRayOpacity = 0.18f;
    private const float ZoomSensitivity = 0.14f;
    private static readonly Vector3 StudioLightDirection = Vector3.Normalize(new Vector3(0.45f, -0.55f, 0.75f));

    /// <summary>Orbit amount of a single arrow key press.</summary>
    private const float OrbitKeyStep = MathF.PI / 12;

    private float _cameraDistance = 100;
    private float _cameraPitch = 0.55f;

    private Vector3 _cameraTarget;
    private float _cameraYaw = -0.8f;
    private IPointer? _capturedPointer;
    private int _clipEnabledLocation;
    private int _alphaLocation;

    private bool _clipToLayer;
    private float _clipZ = float.MaxValue;
    private int _clipZLocation;
    private int _colorLocation;
    private int _unlitLocation;
    private int _lightDirectionLocation;
    private int _ambientLightLocation;

    private GL? _gl;
    private uint _indexBuffer;
    private uint _wireframeIndexBuffer;
    private uint _issueIndexBuffer;
    private VoxelPreviewIssueMesh? _issueMesh;
    private bool _needsIssueUpload;
    private Point? _lastPointerPosition;
    private VoxelPreviewMesh? _mesh;
    private float _modelRadius = 10;
    private bool _needsUpload;
    private bool _needsWireframeUpload;
    private bool _rendererInitialized;
    private uint _shaderProgram;
    private int _uploadedIndexCount;
    private int _uploadedWireframeIndexCount;
    private int _uploadedIssueIndexCount;
    private uint _vertexArray;
    private uint _vertexBuffer;
    private uint _issueVertexArray;
    private uint _issueVertexBuffer;
    private readonly Dictionary<MainIssue.IssueType, Avalonia.Media.Color> _issueColors = [];
    private int _viewProjectionLocation;
    private readonly DispatcherTimer _cameraAnimationTimer;
    private long _cameraAnimationStartTimestamp;
    private float _cameraAnimationStartYaw;
    private float _cameraAnimationStartPitch;
    private float _cameraAnimationYawDelta;
    private float _cameraAnimationTargetPitch;

    static LayerModel3DView()
    {
        VoxelColorProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        BackgroundColorProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        IsOrthographicProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        LightingModeProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        RenderModeProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
    }

    public LayerModel3DView()
    {
        Focusable = true;
        _cameraAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _cameraAnimationTimer.Tick += CameraAnimationTimerOnTick;
    }

    public VoxelPreviewMesh? Mesh
    {
        get => _mesh;
        set
        {
            if (ReferenceEquals(_mesh, value)) return;
            var resetCamera = _mesh is null && value is not null;
            _mesh = value;
            _needsUpload = true;
            if (resetCamera) ResetCamera();
            RequestNextFrameRendering();
        }
    }

    public VoxelPreviewIssueMesh? IssueMesh
    {
        get => _issueMesh;
        set
        {
            if (ReferenceEquals(_issueMesh, value)) return;
            _issueMesh = value;
            _needsIssueUpload = true;
            RequestNextFrameRendering();
        }
    }

    public void SetIssueColors(IReadOnlyDictionary<MainIssue.IssueType, Avalonia.Media.Color> colors)
    {
        _issueColors.Clear();
        foreach (var (type, color) in colors) _issueColors[type] = color;
        RequestNextFrameRendering();
    }

    public bool ClipToLayer
    {
        get => _clipToLayer;
        set
        {
            if (_clipToLayer == value) return;
            _clipToLayer = value;
            RequestNextFrameRendering();
        }
    }

    public float ClipZ
    {
        get => _clipZ;
        set
        {
            if (_clipZ.Equals(value)) return;
            _clipZ = value;
            RequestNextFrameRendering();
        }
    }

    public Avalonia.Media.Color VoxelColor
    {
        get => GetValue(VoxelColorProperty);
        set => SetValue(VoxelColorProperty, value);
    }

    public Avalonia.Media.Color BackgroundColor
    {
        get => GetValue(BackgroundColorProperty);
        set => SetValue(BackgroundColorProperty, value);
    }

    public bool IsOrthographic
    {
        get => GetValue(IsOrthographicProperty);
        set => SetValue(IsOrthographicProperty, value);
    }

    public VoxelPreviewLightingMode LightingMode
    {
        get => GetValue(LightingModeProperty);
        set => SetValue(LightingModeProperty, value);
    }

    public VoxelPreviewRenderMode RenderMode
    {
        get => GetValue(RenderModeProperty);
        set => SetValue(RenderModeProperty, value);
    }

    public float CameraYaw => _cameraYaw;
    public float CameraPitch => _cameraPitch;

    bool ICustomHitTest.HitTest(Point point)
    {
        return new Rect(Bounds.Size).Contains(point);
    }

    public event Action<string?>? RendererStatusChanged;
    public event Action<float, float>? CameraOrientationChanged;

    /// <summary>
    /// Raised when the user asks to flip between the perspective and the orthographic projection.
    /// <see cref="IsOrthographic"/> is bound to an user setting, so the owner is the one to flip it.
    /// </summary>
    public event Action? ProjectionToggleRequested;

    public void ResetCamera()
    {
        if (_mesh is null || _mesh.VertexCount == 0) return;
        CancelCameraAnimation();
        _cameraTarget = _mesh.Center;
        _modelRadius = Math.Max(_mesh.Size.Length() / 2, 0.5f);
        _cameraDistance = _modelRadius * 2.6f;
        _cameraYaw = -0.8f;
        _cameraPitch = 0.55f;
        CameraChanged();
    }

    public void Orbit(double deltaX, double deltaY)
    {
        CancelCameraAnimation();
        _cameraYaw = NormalizeAngle(_cameraYaw - (float)deltaX * OrbitSensitivity);
        _cameraPitch = Math.Clamp(_cameraPitch + (float)deltaY * OrbitSensitivity,
            -MathF.PI / 2, MathF.PI / 2);
        CameraChanged();
    }

    public void SnapToDirection(Vector3 direction)
    {
        if (direction.LengthSquared() <= float.Epsilon) return;
        direction = Vector3.Normalize(direction);

        var horizontalLength = MathF.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        var targetYaw = horizontalLength > 0.0001f
            ? MathF.Atan2(direction.Y, direction.X)
            : -MathF.PI / 2;
        var targetPitch = MathF.Asin(Math.Clamp(direction.Z, -1, 1));

        AnimateToOrientation(targetYaw, targetPitch);
    }

    /// <summary>Orbits by a fixed amount, animated, so that key presses feel like the orientation cube clicks.</summary>
    public void OrbitStep(float yawDelta, float pitchDelta)
    {
        AnimateToOrientation(_cameraYaw + yawDelta,
            Math.Clamp(_cameraPitch + pitchDelta, -MathF.PI / 2, MathF.PI / 2));
    }

    /// <summary>Zooms by <paramref name="steps"/> wheel notches, positive being closer to the model.</summary>
    public void Zoom(float steps)
    {
        CancelCameraAnimation();
        _cameraDistance *= MathF.Exp(-steps * ZoomSensitivity);
        _cameraDistance = Math.Clamp(_cameraDistance, _modelRadius * 0.08f, _modelRadius * 100);
        RequestNextFrameRendering();
    }

    private void AnimateToOrientation(float targetYaw, float targetPitch)
    {
        _cameraAnimationStartYaw = _cameraYaw;
        _cameraAnimationStartPitch = _cameraPitch;
        _cameraAnimationYawDelta = ShortestAngleDelta(_cameraYaw, targetYaw);
        _cameraAnimationTargetPitch = targetPitch;
        _cameraAnimationStartTimestamp = Stopwatch.GetTimestamp();
        _cameraAnimationTimer.Start();
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        try
        {
            var isOpenGles = GlVersion.Type == GlProfileType.OpenGLES;
            if (GlVersion.Major < 3 || (!isOpenGles && GlVersion.Major == 3 && GlVersion.Minor < 3))
            {
                throw new NotSupportedException($"OpenGL 3.3 or OpenGL ES 3.0 is required; detected {GlVersion}.");
            }

            _gl = GL.GetApi(gl.GetProcAddress);
            _shaderProgram = CreateShaderProgram(
                isOpenGles ? EsVertexShader : DesktopVertexShader,
                isOpenGles ? EsFragmentShader : DesktopFragmentShader);
            _viewProjectionLocation = _gl.GetUniformLocation(_shaderProgram, "uViewProjection");
            _colorLocation = _gl.GetUniformLocation(_shaderProgram, "uColor");
            _alphaLocation = _gl.GetUniformLocation(_shaderProgram, "uAlpha");
            _unlitLocation = _gl.GetUniformLocation(_shaderProgram, "uUnlit");
            _lightDirectionLocation = _gl.GetUniformLocation(_shaderProgram, "uLightDirection");
            _ambientLightLocation = _gl.GetUniformLocation(_shaderProgram, "uAmbientLight");
            _clipZLocation = _gl.GetUniformLocation(_shaderProgram, "uClipZ");
            _clipEnabledLocation = _gl.GetUniformLocation(_shaderProgram, "uClipEnabled");

            _vertexArray = _gl.GenVertexArray();
            _vertexBuffer = _gl.GenBuffer();
            _indexBuffer = _gl.GenBuffer();
            _wireframeIndexBuffer = _gl.GenBuffer();
            _issueVertexArray = _gl.GenVertexArray();
            _issueVertexBuffer = _gl.GenBuffer();
            _issueIndexBuffer = _gl.GenBuffer();
            _rendererInitialized = true;
            _needsUpload = true;
            _needsWireframeUpload = true;
            _needsIssueUpload = true;
            RendererStatusChanged?.Invoke(null);
        }
        catch (Exception exception)
        {
            DeleteGpuResources();
            _gl?.Dispose();
            _gl = null;
            _rendererInitialized = false;
            RendererStatusChanged?.Invoke(exception.Message);
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        DeleteGpuResources();
        _gl?.Dispose();
        _gl = null;
        _rendererInitialized = false;
    }

    protected override void OnOpenGlLost()
    {
        _rendererInitialized = false;
        _gl = null;
        RendererStatusChanged?.Invoke("The OpenGL context was lost. Switch tabs or reopen the file to retry.");
    }

    protected override unsafe void OnOpenGlRender(GlInterface gl, int framebuffer)
    {
        if (!_rendererInitialized || _gl is null) return;

        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)framebuffer);
        var renderScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        var width = (uint)Math.Max(1, Math.Round(Bounds.Width * renderScaling));
        var height = (uint)Math.Max(1, Math.Round(Bounds.Height * renderScaling));
        _gl.Viewport(0, 0, width, height);
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);
        _gl.CullFace(TriangleFace.Back);
        _gl.ClearColor(BackgroundColor.R / 255f, BackgroundColor.G / 255f, BackgroundColor.B / 255f, 1);
        _gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));

        if (_needsUpload) UploadMesh();
        if (RenderMode == VoxelPreviewRenderMode.Wireframe && _needsWireframeUpload)
            UploadWireframeIndices();
        if (_needsIssueUpload) UploadIssueMesh();
        if ((_uploadedIndexCount == 0 || _mesh is null) &&
            (_uploadedIssueIndexCount == 0 || _issueMesh is null)) return;

        var viewProjection = GetViewProjection(width / (float)height);
        _gl.UseProgram(_shaderProgram);
        ApplyLighting();
        _gl.Uniform1(_clipZLocation, _clipZ);
        _gl.Uniform1(_clipEnabledLocation, _clipToLayer ? 1 : 0);
        _gl.UniformMatrix4(_viewProjectionLocation, 1, false, (float*)&viewProjection);

        if (_uploadedIndexCount > 0 && _mesh is not null)
        {
            DrawModel();
        }

        if (_uploadedIssueIndexCount > 0 && _issueMesh is not null) DrawIssueOverlay();
    }

    private unsafe void UploadMesh()
    {
        if (_gl is null) return;
        _needsUpload = false;
        _uploadedIndexCount = 0;
        _uploadedWireframeIndexCount = 0;
        _needsWireframeUpload = true;

        _gl.BindVertexArray(_vertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);

        if (_mesh is null || _mesh.VertexCount == 0) return;

        var vertices = _mesh.Vertices;
        fixed (VoxelPreviewVertex* vertexPointer = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint)(vertices.Length * Marshal.SizeOf<VoxelPreviewVertex>()), vertexPointer,
                BufferUsageARB.StaticDraw);
        }

        var indices = _mesh.Indices;
        fixed (uint* indexPointer = indices)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), indexPointer,
                BufferUsageARB.StaticDraw);
        }

        var vertexSize = (uint)Marshal.SizeOf<VoxelPreviewVertex>();
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize,
            (void*)Marshal.OffsetOf<VoxelPreviewVertex>(nameof(VoxelPreviewVertex.Normal)));
        _uploadedIndexCount = indices.Length;
    }

    private unsafe void UploadWireframeIndices()
    {
        if (_gl is null) return;
        _needsWireframeUpload = false;
        _uploadedWireframeIndexCount = 0;

        if (_mesh is null || _mesh.VertexCount == 0) return;
        if (_mesh.VertexCount % 4 != 0)
            throw new InvalidOperationException("The 3D preview mesh does not contain complete quad faces.");

        var quadCount = _mesh.VertexCount / 4;
        var indexCount = checked(quadCount * 8);
        var wireframeIndices = ArrayPool<uint>.Shared.Rent(indexCount);
        try
        {
            var destination = wireframeIndices.AsSpan(0, indexCount);
            for (var quadIndex = 0; quadIndex < quadCount; quadIndex++)
            {
                var vertex = (uint)(quadIndex * 4);
                var offset = quadIndex * 8;
                destination[offset] = vertex;
                destination[offset + 1] = vertex + 1;
                destination[offset + 2] = vertex + 1;
                destination[offset + 3] = vertex + 2;
                destination[offset + 4] = vertex + 2;
                destination[offset + 5] = vertex + 3;
                destination[offset + 6] = vertex + 3;
                destination[offset + 7] = vertex;
            }

            _gl.BindVertexArray(_vertexArray);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _wireframeIndexBuffer);
            fixed (uint* indexPointer = destination)
            {
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indexCount * sizeof(uint)), indexPointer,
                    BufferUsageARB.StaticDraw);
            }

            _uploadedWireframeIndexCount = indexCount;
        }
        finally
        {
            ArrayPool<uint>.Shared.Return(wireframeIndices);
        }
    }

    private unsafe void DrawModel()
    {
        if (_gl is null) return;

        _gl.Uniform3(_colorLocation, VoxelColor.R / 255f, VoxelColor.G / 255f, VoxelColor.B / 255f);
        _gl.BindVertexArray(_vertexArray);

        switch (RenderMode)
        {
            case VoxelPreviewRenderMode.Solid:
                _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
                _gl.Uniform1(_alphaLocation, 1f);
                _gl.Uniform1(_unlitLocation, 0);
                _gl.DrawElements(PrimitiveType.Triangles, (uint)_uploadedIndexCount,
                    DrawElementsType.UnsignedInt, null);
                break;
            case VoxelPreviewRenderMode.XRay:
                DrawDepthPrepass();
                _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
                _gl.Uniform1(_alphaLocation, XRayOpacity);
                _gl.Uniform1(_unlitLocation, 0);
                _gl.Disable(EnableCap.DepthTest);
                _gl.Disable(EnableCap.CullFace);
                _gl.Enable(EnableCap.Blend);
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                _gl.DepthMask(false);
                _gl.DrawElements(PrimitiveType.Triangles, (uint)_uploadedIndexCount,
                    DrawElementsType.UnsignedInt, null);
                _gl.DepthMask(true);
                _gl.Disable(EnableCap.Blend);
                _gl.Enable(EnableCap.CullFace);
                _gl.Enable(EnableCap.DepthTest);
                break;
            case VoxelPreviewRenderMode.Wireframe:
                DrawDepthPrepass();
                if (_uploadedWireframeIndexCount == 0) break;
                _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _wireframeIndexBuffer);
                _gl.Uniform1(_alphaLocation, 1f);
                _gl.Uniform1(_unlitLocation, 1);
                _gl.DepthFunc(DepthFunction.Lequal);
                _gl.DepthMask(false);
                _gl.Disable(EnableCap.CullFace);
                _gl.DrawElements(PrimitiveType.Lines, (uint)_uploadedWireframeIndexCount,
                    DrawElementsType.UnsignedInt, null);
                _gl.Enable(EnableCap.CullFace);
                _gl.DepthMask(true);
                _gl.DepthFunc(DepthFunction.Less);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(RenderMode), RenderMode, null);
        }
    }

    private void ApplyLighting()
    {
        if (_gl is null) return;

        Vector3 direction;
        float ambientLight;
        switch (LightingMode)
        {
            case VoxelPreviewLightingMode.Studio:
                direction = StudioLightDirection;
                ambientLight = 0.28f;
                break;
            case VoxelPreviewLightingMode.Camera:
                GetCameraBasis(out direction, out _, out _);
                ambientLight = 0.2f;
                break;
            case VoxelPreviewLightingMode.Flat:
                direction = StudioLightDirection;
                ambientLight = 1;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(LightingMode), LightingMode, null);
        }

        _gl.Uniform3(_lightDirectionLocation, direction.X, direction.Y, direction.Z);
        _gl.Uniform1(_ambientLightLocation, ambientLight);
    }

    private unsafe void DrawDepthPrepass()
    {
        if (_gl is null) return;
        _gl.BindVertexArray(_vertexArray);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
        _gl.Uniform1(_alphaLocation, 1f);
        _gl.Uniform1(_unlitLocation, 0);
        _gl.ColorMask(false, false, false, false);
        _gl.DepthMask(true);
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.CullFace);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_uploadedIndexCount, DrawElementsType.UnsignedInt, null);
        _gl.ColorMask(true, true, true, true);
    }

    private unsafe void UploadIssueMesh()
    {
        if (_gl is null) return;
        _needsIssueUpload = false;
        _uploadedIssueIndexCount = 0;

        _gl.BindVertexArray(_issueVertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _issueVertexBuffer);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _issueIndexBuffer);

        if (_issueMesh is null || _issueMesh.VertexCount == 0) return;

        var vertices = _issueMesh.Vertices;
        fixed (VoxelPreviewVertex* vertexPointer = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer,
                (nuint)(vertices.Length * Marshal.SizeOf<VoxelPreviewVertex>()), vertexPointer,
                BufferUsageARB.StaticDraw);
        }

        var indices = _issueMesh.Indices;
        fixed (uint* indexPointer = indices)
        {
            _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), indexPointer,
                BufferUsageARB.StaticDraw);
        }

        var vertexSize = (uint)Marshal.SizeOf<VoxelPreviewVertex>();
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize,
            (void*)Marshal.OffsetOf<VoxelPreviewVertex>(nameof(VoxelPreviewVertex.Normal)));
        _uploadedIssueIndexCount = indices.Length;
    }

    private unsafe void DrawIssueOverlay()
    {
        if (_gl is null || _issueMesh is null) return;

        _gl.BindVertexArray(_issueVertexArray);
        _gl.Uniform1(_unlitLocation, 1);
        _gl.Disable(EnableCap.CullFace);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.DepthMask(false);

        // A subtle x-ray pass keeps internal traps and buried islands discoverable.
        _gl.Disable(EnableCap.DepthTest);
        DrawIssueRanges(0.28f);

        // Draw visible regions at full strength, biased forward to avoid coplanar flicker.
        _gl.Enable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.PolygonOffsetFill);
        _gl.PolygonOffset(-1f, -1f);
        DrawIssueRanges(1f);

        _gl.Disable(EnableCap.PolygonOffsetFill);
        _gl.DepthMask(true);
        _gl.Disable(EnableCap.Blend);
        _gl.Enable(EnableCap.CullFace);
    }

    private unsafe void DrawIssueRanges(float alpha)
    {
        if (_gl is null || _issueMesh is null) return;
        _gl.Uniform1(_alphaLocation, alpha);

        foreach (var range in _issueMesh.DrawRanges)
        {
            var color = _issueColors.TryGetValue(range.Type, out var configured)
                ? configured
                : Avalonia.Media.Colors.Red;
            _gl.Uniform3(_colorLocation, color.R / 255f, color.G / 255f, color.B / 255f);
            _gl.DrawElements(PrimitiveType.Triangles, (uint)range.IndexCount, DrawElementsType.UnsignedInt,
                (void*)(range.IndexOffset * sizeof(uint)));
        }
    }

    private Matrix4x4 GetViewProjection(float aspectRatio)
    {
        GetCameraBasis(out var direction, out _, out var up);
        var cameraOffset = direction * _cameraDistance;
        var cameraPosition = _cameraTarget + cameraOffset;
        var nearPlane = Math.Max(0.001f, _cameraDistance - _modelRadius * 1.5f);
        var farPlane = Math.Max(nearPlane + 1, _cameraDistance + _modelRadius * 4);
        var view = Matrix4x4.CreateLookAt(cameraPosition, _cameraTarget, up);
        var safeAspectRatio = Math.Max(aspectRatio, 0.01f);
        var projection = IsOrthographic
            ? Matrix4x4.CreateOrthographic(
                2 * _cameraDistance * MathF.Tan(MathF.PI / 8) * safeAspectRatio,
                2 * _cameraDistance * MathF.Tan(MathF.PI / 8), nearPlane, farPlane)
            : Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, safeAspectRatio, nearPlane, farPlane);
        return view * projection;
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

    private void CameraAnimationTimerOnTick(object? sender, EventArgs e)
    {
        var progress = Math.Clamp(
            Stopwatch.GetElapsedTime(_cameraAnimationStartTimestamp).TotalSeconds / CameraAnimationSeconds, 0, 1);
        var easedProgress = (float)(progress * progress * (3 - 2 * progress));
        _cameraYaw = NormalizeAngle(_cameraAnimationStartYaw + _cameraAnimationYawDelta * easedProgress);
        _cameraPitch = float.Lerp(_cameraAnimationStartPitch, _cameraAnimationTargetPitch, easedProgress);
        CameraChanged();
        if (progress >= 1) _cameraAnimationTimer.Stop();
    }

    private void CameraChanged()
    {
        CameraOrientationChanged?.Invoke(_cameraYaw, _cameraPitch);
        RequestNextFrameRendering();
    }

    private void CancelCameraAnimation()
    {
        _cameraAnimationTimer.Stop();
    }

    private static float ShortestAngleDelta(float from, float to)
    {
        return NormalizeAngle(to - from);
    }

    private static float NormalizeAngle(float angle)
    {
        return MathF.IEEERemainder(angle, MathF.Tau);
    }

    private uint CreateShaderProgram(string vertexSource, string fragmentSource)
    {
        var vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        try
        {
            var fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
            try
            {
                var program = _gl!.CreateProgram();
                try
                {
                    _gl.AttachShader(program, vertexShader);
                    _gl.AttachShader(program, fragmentShader);
                    _gl.LinkProgram(program);
                    _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);
                    if (status == 0)
                    {
                        var error = _gl.GetProgramInfoLog(program);
                        throw new InvalidOperationException($"Unable to link the 3D preview shader: {error}");
                    }

                    return program;
                }
                catch
                {
                    _gl.DeleteProgram(program);
                    throw;
                }
            }
            finally
            {
                _gl!.DeleteShader(fragmentShader);
            }
        }
        finally
        {
            _gl!.DeleteShader(vertexShader);
        }
    }

    private uint CompileShader(ShaderType shaderType, string source)
    {
        var shader = _gl!.CreateShader(shaderType);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);
        if (status != 0) return shader;

        var error = _gl.GetShaderInfoLog(shader);
        _gl.DeleteShader(shader);
        throw new InvalidOperationException($"Unable to compile the 3D preview shader: {error}");
    }

    private void DeleteGpuResources()
    {
        if (_gl is null) return;
        if (_vertexArray != 0) _gl.DeleteVertexArray(_vertexArray);
        if (_vertexBuffer != 0) _gl.DeleteBuffer(_vertexBuffer);
        if (_indexBuffer != 0) _gl.DeleteBuffer(_indexBuffer);
        if (_wireframeIndexBuffer != 0) _gl.DeleteBuffer(_wireframeIndexBuffer);
        if (_issueVertexArray != 0) _gl.DeleteVertexArray(_issueVertexArray);
        if (_issueVertexBuffer != 0) _gl.DeleteBuffer(_issueVertexBuffer);
        if (_issueIndexBuffer != 0) _gl.DeleteBuffer(_issueIndexBuffer);
        if (_shaderProgram != 0) _gl.DeleteProgram(_shaderProgram);
        _vertexArray = 0;
        _vertexBuffer = 0;
        _indexBuffer = 0;
        _wireframeIndexBuffer = 0;
        _issueVertexArray = 0;
        _issueVertexBuffer = 0;
        _issueIndexBuffer = 0;
        _shaderProgram = 0;
        _uploadedIndexCount = 0;
        _uploadedWireframeIndexCount = 0;
        _uploadedIssueIndexCount = 0;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();
        _capturedPointer = e.Pointer;
        _lastPointerPosition = e.GetPosition(this);
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_lastPointerPosition is not { } previous || !ReferenceEquals(_capturedPointer, e.Pointer)) return;

        var current = e.GetPosition(this);
        var delta = current - previous;
        _lastPointerPosition = current;
        var properties = e.GetCurrentPoint(this).Properties;

        if (properties.IsLeftButtonPressed)
        {
            Orbit(delta.X, delta.Y);
        }
        else if (properties.IsMiddleButtonPressed || properties.IsRightButtonPressed)
        {
            CancelCameraAnimation();
            GetCameraBasis(out _, out var right, out var up);
            var scale = _cameraDistance * 0.0015f;
            _cameraTarget -= right * (float)delta.X * scale;
            _cameraTarget += up * (float)delta.Y * scale;
            RequestNextFrameRendering();
        }

        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (ReferenceEquals(_capturedPointer, e.Pointer))
        {
            e.Pointer.Capture(null);
            _capturedPointer = null;
            _lastPointerPosition = null;
        }

        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _capturedPointer = null;
        _lastPointerPosition = null;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        Zoom((float)e.Delta.Y);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (HandleCameraKey(e)) e.Handled = true;
    }

    /// <summary>
    /// Runs the camera shortcut bound to <paramref name="e"/>, if any, and reports whether it was consumed.
    /// Public so that the owner can forward the keys of controls that take the focus away from the viewport,
    /// such as the orientation cube.
    /// </summary>
    public bool HandleCameraKey(KeyEventArgs e)
    {
        if (e.Handled || _mesh is null) return false;

        if (e.KeyModifiers == AppSettings.SystemCommandKeyModifier)
        {
            switch (e.Key)
            {
                /* The six axis views, matching the orientation cube faces. */
                case Key.D1 or Key.NumPad1:
                    UserSettings.Instance.Layer3DPreview.LightingMode = VoxelPreviewLightingMode.Camera;
                    return true;
                case Key.D2 or Key.NumPad2:
                    UserSettings.Instance.Layer3DPreview.LightingMode = VoxelPreviewLightingMode.Studio;
                    return true;
                case Key.D3 or Key.NumPad3:
                    UserSettings.Instance.Layer3DPreview.LightingMode = VoxelPreviewLightingMode.Flat;
                    return true;
            }
            
            return false;
        }
        
        if (e.KeyModifiers != KeyModifiers.None) return false;
        switch (e.Key)
        {
            /* The six axis views, matching the orientation cube faces. */
            case Key.D1 or Key.NumPad1:
                SnapToDirection(-Vector3.UnitY); // Front
                return true;
            case Key.D2 or Key.NumPad2:
                SnapToDirection(Vector3.UnitY); // Back
                return true;
            case Key.D3 or Key.NumPad3:
                SnapToDirection(-Vector3.UnitX); // Left
                return true;
            case Key.D4 or Key.NumPad4:
                SnapToDirection(Vector3.UnitX); // Right
                return true;
            case Key.D5 or Key.NumPad5:
                SnapToDirection(Vector3.UnitZ); // Top
                return true;
            case Key.D6 or Key.NumPad6:
                SnapToDirection(-Vector3.UnitZ); // Bottom
                return true;
            case Key.D0 or Key.NumPad0 or Key.Home:
                ResetCamera();
                return true;
            /* Arrows orbit as if dragging the model towards that direction, matching the pointer and the cube. */
            case Key.Left:
                OrbitStep(OrbitKeyStep, 0);
                return true;
            case Key.Right:
                OrbitStep(-OrbitKeyStep, 0);
                return true;
            case Key.Up:
                OrbitStep(0, -OrbitKeyStep);
                return true;
            case Key.Down:
                OrbitStep(0, OrbitKeyStep);
                return true;
            case Key.OemPlus or Key.Add:
                Zoom(1);
                return true;
            case Key.OemMinus or Key.Subtract:
                Zoom(-1);
                return true;
            case Key.S:
                UserSettings.Instance.Layer3DPreview.RenderMode = VoxelPreviewRenderMode.Solid;
                return true;
            case Key.X:
                UserSettings.Instance.Layer3DPreview.RenderMode = VoxelPreviewRenderMode.XRay;
                return true;
            case Key.W:
                UserSettings.Instance.Layer3DPreview.RenderMode = VoxelPreviewRenderMode.Wireframe;
                return true;
            case Key.C:
                App.MainWindow.Layer3DClipToCurrentLayer = !App.MainWindow.Layer3DClipToCurrentLayer;
                return true;
            case Key.P:
                ProjectionToggleRequested?.Invoke();
                return true;
            default:
                return false;
        }
    }

    protected override void OnDoubleTapped(TappedEventArgs e)
    {
        base.OnDoubleTapped(e);
        ResetCamera();
        e.Handled = true;
    }
}