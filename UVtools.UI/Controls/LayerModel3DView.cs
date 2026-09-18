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
using Avalonia.Media.Imaging;
using System.Threading.Tasks;
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
                                                 uniform float uClipZMin;
                                                 uniform float uClipZMax;
                                                 uniform int uClipEnabled;
                                                 uniform int uColorMode;
                                                 uniform float uOverhangThreshold;
                                                 uniform float uBottomZ;
                                                 uniform float uTransitionZ;
                                                 uniform vec3 uBottomColor;
                                                 uniform vec3 uBuildVolumeMin;
                                                 uniform vec3 uBuildVolumeMax;
                                                 uniform int uHighlightOutOfBounds;
                                                 uniform int uCutawayAxis;
                                                 uniform float uCutawayPosition;
                                                 uniform int uCutawayInvert;
                                                 uniform sampler2D uPeelTexture;
                                                 uniform float uModelMaxZ;
                                                 uniform float uFirstLayerZ;
                                                 out vec4 fragmentColor;
                                                 void main()
                                                 {
                                                     if (uClipEnabled != 0 && (vWorldPosition.z < uClipZMin || vWorldPosition.z > uClipZMax)) discard;
                                                     if (uCutawayAxis == 1)
                                                     {
                                                         if (uCutawayInvert == 0 ? (vWorldPosition.x > uCutawayPosition) : (vWorldPosition.x < uCutawayPosition)) discard;
                                                     }
                                                     else if (uCutawayAxis == 2)
                                                     {
                                                         if (uCutawayInvert == 0 ? (vWorldPosition.y > uCutawayPosition) : (vWorldPosition.y < uCutawayPosition)) discard;
                                                     }

                                                     vec3 baseColor = uColor;
                                                     if (uHighlightOutOfBounds != 0 && (
                                                         vWorldPosition.x < uBuildVolumeMin.x || vWorldPosition.x > uBuildVolumeMax.x ||
                                                         vWorldPosition.y < uBuildVolumeMin.y || vWorldPosition.y > uBuildVolumeMax.y ||
                                                         vWorldPosition.z < uBuildVolumeMin.z || vWorldPosition.z > uBuildVolumeMax.z))
                                                     {
                                                         baseColor = vec3(1.0, 0.08, 0.18);
                                                     }
                                                     else if (uColorMode == 1)
                                                     {
                                                         vec3 norm = normalize(vNormal);
                                                         float downward = -norm.z;
                                                         if (downward > 0.0)
                                                         {
                                                             if (downward >= uOverhangThreshold)
                                                             {
                                                                 baseColor = vec3(1.0, 0.15, 0.15);
                                                             }
                                                             else if (downward >= 0.5)
                                                             {
                                                                 float t = (downward - 0.5) / max(uOverhangThreshold - 0.5, 0.001);
                                                                 baseColor = mix(vec3(1.0, 0.85, 0.0), vec3(1.0, 0.15, 0.15), t);
                                                             }
                                                         }
                                                     }
                                                     else if (uColorMode == 2)
                                                     {
                                                         if (vWorldPosition.z <= uBottomZ)
                                                         {
                                                             baseColor = uBottomColor;
                                                         }
                                                         else if (vWorldPosition.z <= uTransitionZ)
                                                         {
                                                             float t = (vWorldPosition.z - uBottomZ) / max(uTransitionZ - uBottomZ, 0.001);
                                                             baseColor = mix(uBottomColor, uColor, t);
                                                         }
                                                     }
                                                     else if (uColorMode == 3)
                                                     {
                                                         float normZ = clamp(vWorldPosition.z / max(uModelMaxZ, 0.001), 0.0, 1.0);
                                                         baseColor = texture(uPeelTexture, vec2(normZ, 0.5)).rgb;
                                                     }
                                                     else if (uColorMode == 4)
                                                     {
                                                         vec3 norm = normalize(vNormal);
                                                         bool isAtBase = vWorldPosition.z <= (uFirstLayerZ + 0.02);
                                                         bool isDownFacing = norm.z < -0.3;
                                                         if (isAtBase && isDownFacing)
                                                         {
                                                             baseColor = vec3(0.0, 1.0, 0.35);
                                                         }
                                                         else if (vWorldPosition.z <= uBottomZ)
                                                         {
                                                             baseColor = vec3(0.18, 0.32, 0.65);
                                                         }
                                                         else if (vWorldPosition.z <= uTransitionZ)
                                                         {
                                                             baseColor = vec3(0.2, 0.6, 0.65);
                                                         }
                                                         else
                                                         {
                                                             baseColor = vec3(0.55, 0.62, 0.7);
                                                         }

                                                         if (vWorldPosition.z > (uFirstLayerZ + 0.02) && vWorldPosition.z <= (uFirstLayerZ + 3.0) && isDownFacing)
                                                         {
                                                             baseColor = vec3(1.0, 0.25, 0.1);
                                                         }
                                                     }

                                                     float diffuse = max(dot(normalize(vNormal), uLightDirection), 0.0);
                                                     float lighting = uAmbientLight + diffuse * (1.0 - uAmbientLight);
                                                     vec3 color = uUnlit != 0 ? baseColor : baseColor * lighting;
                                                     fragmentColor = vec4(color, uAlpha);
                                                 }
                                                 """;

    private const string DesktopCapVertexShader = """
                                                  #version 330 core
                                                  layout (location = 0) in vec3 aPosition;
                                                  layout (location = 1) in vec2 aTexCoord;
                                                  uniform mat4 uViewProjection;
                                                  out vec2 vTexCoord;
                                                  out vec3 vWorldPosition;
                                                  void main()
                                                  {
                                                      vTexCoord = aTexCoord;
                                                      vWorldPosition = aPosition;
                                                      gl_Position = uViewProjection * vec4(aPosition, 1.0);
                                                  }
                                                  """;

    private const string DesktopCapFragmentShader = """
                                                    #version 330 core
                                                    in vec2 vTexCoord;
                                                    in vec3 vWorldPosition;
                                                    uniform sampler2D uCapTexture;
                                                    uniform vec3 uColor;
                                                    uniform float uAlpha;
                                                    uniform int uUnlit;
                                                    uniform vec3 uLightDirection;
                                                    uniform float uAmbientLight;
                                                    uniform int uCutawayAxis;
                                                    uniform float uCutawayPosition;
                                                    uniform int uCutawayInvert;
                                                    out vec4 fragmentColor;
                                                    void main()
                                                    {
                                                        if (uCutawayAxis == 1)
                                                        {
                                                            if (uCutawayInvert == 0 ? (vWorldPosition.x > uCutawayPosition) : (vWorldPosition.x < uCutawayPosition)) discard;
                                                        }
                                                        else if (uCutawayAxis == 2)
                                                        {
                                                            if (uCutawayInvert == 0 ? (vWorldPosition.y > uCutawayPosition) : (vWorldPosition.y < uCutawayPosition)) discard;
                                                        }
                                                        float mask = texture(uCapTexture, vTexCoord).r;
                                                        if (mask < 0.5) discard;
                                                        vec3 normal = gl_FrontFacing ? vec3(0.0, 0.0, 1.0) : vec3(0.0, 0.0, -1.0);
                                                        float diffuse = max(dot(normal, uLightDirection), 0.0);
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
                                            uniform float uClipZMin;
                                            uniform float uClipZMax;
                                            uniform int uClipEnabled;
                                            uniform int uColorMode;
                                            uniform float uOverhangThreshold;
                                            uniform float uBottomZ;
                                            uniform float uTransitionZ;
                                            uniform vec3 uBottomColor;
                                            uniform vec3 uBuildVolumeMin;
                                            uniform vec3 uBuildVolumeMax;
                                            uniform int uHighlightOutOfBounds;
                                            uniform int uCutawayAxis;
                                            uniform float uCutawayPosition;
                                            uniform int uCutawayInvert;
                                            uniform sampler2D uPeelTexture;
                                            uniform float uModelMaxZ;
                                            uniform float uFirstLayerZ;
                                            out vec4 fragmentColor;
                                            void main()
                                            {
                                                if (uClipEnabled != 0 && (vWorldPosition.z < uClipZMin || vWorldPosition.z > uClipZMax)) discard;
                                                if (uCutawayAxis == 1)
                                                {
                                                    if (uCutawayInvert == 0 ? (vWorldPosition.x > uCutawayPosition) : (vWorldPosition.x < uCutawayPosition)) discard;
                                                }
                                                else if (uCutawayAxis == 2)
                                                {
                                                    if (uCutawayInvert == 0 ? (vWorldPosition.y > uCutawayPosition) : (vWorldPosition.y < uCutawayPosition)) discard;
                                                }

                                                vec3 baseColor = uColor;
                                                if (uHighlightOutOfBounds != 0 && (
                                                    vWorldPosition.x < uBuildVolumeMin.x || vWorldPosition.x > uBuildVolumeMax.x ||
                                                    vWorldPosition.y < uBuildVolumeMin.y || vWorldPosition.y > uBuildVolumeMax.y ||
                                                    vWorldPosition.z < uBuildVolumeMin.z || vWorldPosition.z > uBuildVolumeMax.z))
                                                {
                                                    baseColor = vec3(1.0, 0.08, 0.18);
                                                }
                                                else if (uColorMode == 1)
                                                {
                                                    vec3 norm = normalize(vNormal);
                                                    float downward = -norm.z;
                                                    if (downward > 0.0)
                                                    {
                                                        if (downward >= uOverhangThreshold)
                                                        {
                                                            baseColor = vec3(1.0, 0.15, 0.15);
                                                        }
                                                        else if (downward >= 0.5)
                                                        {
                                                            float t = (downward - 0.5) / max(uOverhangThreshold - 0.5, 0.001);
                                                            baseColor = mix(vec3(1.0, 0.85, 0.0), vec3(1.0, 0.15, 0.15), t);
                                                        }
                                                    }
                                                }
                                                else if (uColorMode == 2)
                                                {
                                                    if (vWorldPosition.z <= uBottomZ)
                                                    {
                                                        baseColor = uBottomColor;
                                                    }
                                                    else if (vWorldPosition.z <= uTransitionZ)
                                                    {
                                                        float t = (vWorldPosition.z - uBottomZ) / max(uTransitionZ - uBottomZ, 0.001);
                                                        baseColor = mix(uBottomColor, uColor, t);
                                                    }
                                                }
                                                else if (uColorMode == 3)
                                                {
                                                    float normZ = clamp(vWorldPosition.z / max(uModelMaxZ, 0.001), 0.0, 1.0);
                                                    baseColor = texture(uPeelTexture, vec2(normZ, 0.5)).rgb;
                                                }
                                                else if (uColorMode == 4)
                                                {
                                                    vec3 norm = normalize(vNormal);
                                                    bool isAtBase = vWorldPosition.z <= (uFirstLayerZ + 0.02);
                                                    bool isDownFacing = norm.z < -0.3;
                                                    if (isAtBase && isDownFacing)
                                                    {
                                                        baseColor = vec3(0.0, 1.0, 0.35);
                                                    }
                                                    else if (vWorldPosition.z <= uBottomZ)
                                                    {
                                                        baseColor = vec3(0.18, 0.32, 0.65);
                                                    }
                                                    else if (vWorldPosition.z <= uTransitionZ)
                                                    {
                                                        baseColor = vec3(0.2, 0.6, 0.65);
                                                    }
                                                    else
                                                    {
                                                        baseColor = vec3(0.55, 0.62, 0.7);
                                                    }

                                                    if (vWorldPosition.z > (uFirstLayerZ + 0.02) && vWorldPosition.z <= (uFirstLayerZ + 3.0) && isDownFacing)
                                                    {
                                                        baseColor = vec3(1.0, 0.25, 0.1);
                                                    }
                                                }

                                                float diffuse = max(dot(normalize(vNormal), uLightDirection), 0.0);
                                                float lighting = uAmbientLight + diffuse * (1.0 - uAmbientLight);
                                                vec3 color = uUnlit != 0 ? baseColor : baseColor * lighting;
                                                fragmentColor = vec4(color, uAlpha);
                                            }
                                            """;

    private const string EsCapVertexShader = """
                                             #version 300 es
                                             precision highp float;
                                             layout (location = 0) in vec3 aPosition;
                                             layout (location = 1) in vec2 aTexCoord;
                                             uniform mat4 uViewProjection;
                                             out vec2 vTexCoord;
                                             out vec3 vWorldPosition;
                                             void main()
                                             {
                                                 vTexCoord = aTexCoord;
                                                 vWorldPosition = aPosition;
                                                 gl_Position = uViewProjection * vec4(aPosition, 1.0);
                                             }
                                             """;

    private const string EsCapFragmentShader = """
                                               #version 300 es
                                               precision highp float;
                                               in vec2 vTexCoord;
                                               in vec3 vWorldPosition;
                                               uniform sampler2D uCapTexture;
                                               uniform vec3 uColor;
                                               uniform float uAlpha;
                                               uniform int uUnlit;
                                               uniform vec3 uLightDirection;
                                               uniform float uAmbientLight;
                                               uniform int uCutawayAxis;
                                               uniform float uCutawayPosition;
                                               uniform int uCutawayInvert;
                                               out vec4 fragmentColor;
                                               void main()
                                               {
                                                   if (uCutawayAxis == 1)
                                                   {
                                                       if (uCutawayInvert == 0 ? (vWorldPosition.x > uCutawayPosition) : (vWorldPosition.x < uCutawayPosition)) discard;
                                                   }
                                                   else if (uCutawayAxis == 2)
                                                   {
                                                       if (uCutawayInvert == 0 ? (vWorldPosition.y > uCutawayPosition) : (vWorldPosition.y < uCutawayPosition)) discard;
                                                   }
                                                   float mask = texture(uCapTexture, vTexCoord).r;
                                                   if (mask < 0.5) discard;
                                                   vec3 normal = gl_FrontFacing ? vec3(0.0, 0.0, 1.0) : vec3(0.0, 0.0, -1.0);
                                                   float diffuse = max(dot(normal, uLightDirection), 0.0);
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

    public static readonly StyledProperty<VoxelPreviewColorMode> ColorModeProperty =
        AvaloniaProperty.Register<LayerModel3DView, VoxelPreviewColorMode>(nameof(ColorMode));

    public static readonly StyledProperty<VoxelPreviewClipMode> ClipModeProperty =
        AvaloniaProperty.Register<LayerModel3DView, VoxelPreviewClipMode>(nameof(ClipMode));

    public static readonly StyledProperty<bool> ShowBuildPlateGridProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(ShowBuildPlateGrid), true);

    public static readonly StyledProperty<bool> ShowLayerIssuesProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(ShowLayerIssues), true);

    public static readonly StyledProperty<bool> GhostClippedModelProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(GhostClippedModel), true);

    public static readonly StyledProperty<bool> ShowBoundingBoxProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(ShowBoundingBox), false);

    public static readonly DirectProperty<LayerModel3DView, string?> ModelDimensionsTextProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, string?>(
            nameof(ModelDimensionsText),
            o => o.ModelDimensionsText);

    public static readonly StyledProperty<bool> IsTurntableActiveProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(IsTurntableActive), false);

    public static readonly StyledProperty<bool> IsMeasureModeProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(IsMeasureMode), false);

    public static readonly DirectProperty<LayerModel3DView, string?> MeasureDistanceTextProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, string?>(
            nameof(MeasureDistanceText),
            o => o.MeasureDistanceText);

    public static readonly StyledProperty<float> SlabThicknessProperty =
        AvaloniaProperty.Register<LayerModel3DView, float>(nameof(SlabThickness), 5.0f);

    public static readonly StyledProperty<float> PlateWidthProperty =
        AvaloniaProperty.Register<LayerModel3DView, float>(nameof(PlateWidth));

    public static readonly StyledProperty<float> PlateHeightProperty =
        AvaloniaProperty.Register<LayerModel3DView, float>(nameof(PlateHeight));

    public static readonly StyledProperty<float> PrintHeightProperty =
        AvaloniaProperty.Register<LayerModel3DView, float>(nameof(PrintHeight));

    public static readonly StyledProperty<float> BottomLayersHeightProperty =
        AvaloniaProperty.Register<LayerModel3DView, float>(nameof(BottomLayersHeight));

    public static readonly StyledProperty<float> TransitionLayersHeightProperty =
        AvaloniaProperty.Register<LayerModel3DView, float>(nameof(TransitionLayersHeight));

    public static readonly StyledProperty<bool> ShowModelStatsProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(ShowModelStats), false);

    public static readonly DirectProperty<LayerModel3DView, string?> ModelStatsTextProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, string?>(
            nameof(ModelStatsText),
            o => o.ModelStatsText);

    public static readonly DirectProperty<LayerModel3DView, float> ModelVolumeMlProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, float>(
            nameof(ModelVolumeMl),
            o => o.ModelVolumeMl);

    public static readonly DirectProperty<LayerModel3DView, float> ModelWeightGramsProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, float>(
            nameof(ModelWeightGrams),
            o => o.ModelWeightGrams);

    public static readonly DirectProperty<LayerModel3DView, float> ModelResinCostProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, float>(
            nameof(ModelResinCost),
            o => o.ModelResinCost);

    public static readonly StyledProperty<bool> ShowCenterOfMassProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(ShowCenterOfMass), false);

    public static readonly DirectProperty<LayerModel3DView, string?> CenterOfMassTextProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, string?>(
            nameof(CenterOfMassText),
            o => o.CenterOfMassText);

    public static readonly StyledProperty<VoxelPreviewCutawayAxis> CutawayAxisProperty =
        AvaloniaProperty.Register<LayerModel3DView, VoxelPreviewCutawayAxis>(nameof(CutawayAxis), VoxelPreviewCutawayAxis.Off);

    public static readonly StyledProperty<float> CutawayPositionProperty =
        AvaloniaProperty.Register<LayerModel3DView, float>(nameof(CutawayPosition), 0f);

    public static readonly StyledProperty<bool> CutawayInvertProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(CutawayInvert), false);

    public static readonly DirectProperty<LayerModel3DView, float> CutawayMinProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, float>(nameof(CutawayMin), o => o.CutawayMin);

    public static readonly DirectProperty<LayerModel3DView, float> CutawayMaxProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, float>(nameof(CutawayMax), o => o.CutawayMax);

    public static readonly DirectProperty<LayerModel3DView, string> CutawayTextProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, string>(nameof(CutawayText), o => o.CutawayText);

    public static readonly StyledProperty<bool> ShowPeelCurveProperty =
        AvaloniaProperty.Register<LayerModel3DView, bool>(nameof(ShowPeelCurve), false);

    public static readonly DirectProperty<LayerModel3DView, Vector3> CenterOfMassProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, Vector3>(
            nameof(CenterOfMass),
            o => o.CenterOfMass);

    public static readonly DirectProperty<LayerModel3DView, float> BaseContactAreaProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, float>(
            nameof(BaseContactArea),
            o => o.BaseContactArea);

    public static readonly DirectProperty<LayerModel3DView, bool> IsOutOfBoundsProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, bool>(
            nameof(IsOutOfBounds),
            o => o.IsOutOfBounds);

    public static readonly DirectProperty<LayerModel3DView, string?> OutOfBoundsWarningTextProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, string?>(
            nameof(OutOfBoundsWarningText),
            o => o.OutOfBoundsWarningText);

    public static readonly StyledProperty<float> FirstLayerHeightProperty =
        AvaloniaProperty.Register<LayerModel3DView, float>(nameof(FirstLayerHeight), 0.05f);

    public float FirstLayerHeight
    {
        get => GetValue(FirstLayerHeightProperty);
        set => SetValue(FirstLayerHeightProperty, value);
    }

    public static readonly DirectProperty<LayerModel3DView, bool> IsPeelRiskActiveProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, bool>(
            nameof(IsPeelRiskActive),
            o => o.IsPeelRiskActive);

    public bool IsPeelRiskActive => ColorMode == VoxelPreviewColorMode.PeelForceRisk;

    public static readonly DirectProperty<LayerModel3DView, bool> IsBedAdhesionActiveProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, bool>(
            nameof(IsBedAdhesionActive),
            o => o.IsBedAdhesionActive);

    public bool IsBedAdhesionActive => ColorMode == VoxelPreviewColorMode.BedAdhesion;

    public static readonly DirectProperty<LayerModel3DView, string?> BedAdhesionTextProperty =
        AvaloniaProperty.RegisterDirect<LayerModel3DView, string?>(
            nameof(BedAdhesionText),
            o => o.BedAdhesionText);

    private string? _bedAdhesionText;
    public string? BedAdhesionText
    {
        get => _bedAdhesionText;
        private set => SetAndRaise(BedAdhesionTextProperty, ref _bedAdhesionText, value);
    }

    public event Action? SnapshotToClipboardRequested;
    public event Action? SnapshotToFileRequested;

    private const float OrbitSensitivity = 0.008f;
    private const double CameraAnimationSeconds = 0.2;
    private const float XRayOpacity = 0.18f;
    private const float ZoomSensitivity = 0.14f;
    private static readonly Vector3 StudioLightDirection = Vector3.Normalize(new Vector3(0.45f, -0.55f, 0.75f));

    /// <summary>Orbit amount of a single arrow key press.</summary>
    private const float OrbitKeyStep = MathF.PI / 12;

    private float _cameraDistance = 100;
    private float _cameraPitch = 0.55f;
    private float _cameraRoll;

    private Vector3 _cameraTarget;
    private float _cameraYaw = -0.8f;
    private int _pitchCycleState;
    private float _pitchBaseYaw = -MathF.PI / 2f;
    private IPointer? _capturedPointer;
    private Point? _pointerDownPosition;
    private bool _isDragging;
    private int _clipEnabledLocation;
    private int _alphaLocation;

    private bool _clipToLayer;
    private float _clipZ = float.MaxValue;
    private int _clipZMinLocation;
    private int _clipZMaxLocation;
    private int _colorModeLocation;
    private int _overhangThresholdLocation;
    private int _bottomZLocation;
    private int _transitionZLocation;
    private int _bottomColorLocation;
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
    private long _turntableLastTimestamp;
    private long _cameraAnimationStartTimestamp;
    private float _cameraAnimationStartYaw;
    private float _cameraAnimationStartPitch;
    private float _cameraAnimationStartRoll;
    private float _cameraAnimationYawDelta;
    private float _cameraAnimationRollDelta;
    private float _cameraAnimationTargetPitch;
    private float _cameraAnimationTargetRoll;

    private uint _capVertexArray;
    private uint _capVertexBuffer;
    private uint _capIndexBuffer;
    private uint _capTexture;
    private uint _capShaderProgram;
    private int _capViewProjectionLocation;
    private int _capColorLocation;
    private int _capAlphaLocation;
    private int _capUnlitLocation;
    private int _capLightDirectionLocation;
    private int _capAmbientLightLocation;

    private uint _peelTexture;
    private int _peelTextureLocation;
    private int _modelMaxZLocation;
    private int _firstLayerZLocation;
    private float[]? _peelAreas;
    private List<int>? _peelSpikes;
    private float _peelMaxArea;
    private bool _needsPeelUpload;

    public readonly record struct CavityMarker3D(Vector3 Min, Vector3 Max, bool IsSuctionCup, bool IsResinTrap);
    private readonly List<CavityMarker3D> _cavityMarkers = [];
    private uint _cavityVertexArray;
    private uint _cavityVertexBuffer;
    private int _uploadedCavityVertexCount;
    private bool _needsCavityUpload;
    private int _capTextureLocation;
    private int _capTextureWidth;
    private int _capTextureHeight;

    private byte[]? _pendingCapData;
    private int _pendingCapWidth;
    private int _pendingCapHeight;
    private float _capMinX;
    private float _capMinY;
    private float _capMaxX;
    private float _capMaxY;
    private bool _hasCapData;
    private bool _needsCapUpload;
    private bool _needsCapGeometryUpload;

    private uint _gridVertexArray;
    private uint _gridVertexBuffer;
    private int _gridVertexCount;
    private bool _needsGridUpload = true;

    private uint _focusBoxVertexArray;
    private uint _focusBoxVertexBuffer;
    private bool _hasFocusedBox;
    private Vector3 _focusBoxMin;
    private Vector3 _focusBoxMax;
    private bool _needsFocusBoxUpload;

    private Vector3? _measurePoint1;
    private Vector3? _measurePoint2;
    private uint _measureVertexArray;
    private uint _measureVertexBuffer;
    private int _measureVertexCount;
    private bool _needsMeasureUpload;
    private string? _measureDistanceText;
    private string? _modelDimensionsText;
    private uint _boundingBoxVertexArray;
    private uint _boundingBoxVertexBuffer;
    private bool _needsBoundingBoxUpload;

    private int _buildVolumeMinLocation;
    private int _buildVolumeMaxLocation;
    private int _highlightOutOfBoundsLocation;

    private uint _comVertexArray;
    private uint _comVertexBuffer;
    private int _comVertexCount;
    private bool _needsComUpload;

    private bool _isOutOfBounds;
    private string? _outOfBoundsWarningText;
    private float _modelVolumeMl;
    private float _modelWeightGrams;
    private float _modelResinCost;
    private string? _modelStatsText;
    private Vector3 _centerOfMass;
    private string? _centerOfMassText;
    private float _baseContactArea;

    private float _cutawayMin = -100f;
    private float _cutawayMax = 100f;
    private string _cutawayText = "Cut: Off";

    private int _cutawayAxisLocation = -1;
    private int _cutawayPositionLocation = -1;
    private int _cutawayInvertLocation = -1;
    private int _capCutawayAxisLocation = -1;
    private int _capCutawayPositionLocation = -1;
    private int _capCutawayInvertLocation = -1;

    private TaskCompletionSource<WriteableBitmap?>? _snapshotCompletionSource;

    public event Action<Vector3>? ModelPointClicked;

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
        ColorModeProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            control.RaisePropertyChanged(IsPeelRiskActiveProperty, !control.IsPeelRiskActive, control.IsPeelRiskActive);
            control.RaisePropertyChanged(IsBedAdhesionActiveProperty, !control.IsBedAdhesionActive, control.IsBedAdhesionActive);
            control.RequestNextFrameRendering();
        });
        FirstLayerHeightProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        ClipModeProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        ShowBuildPlateGridProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        ShowLayerIssuesProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        GhostClippedModelProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        ShowBoundingBoxProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            control._needsBoundingBoxUpload = true;
            control.RequestNextFrameRendering();
        });
        IsTurntableActiveProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            if (control.IsTurntableActive)
            {
                control.StartTurntable();
            }
            else
            {
                control.StopTurntable();
            }
        });
        IsMeasureModeProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            if (!control.IsMeasureMode)
            {
                control.ClearMeasure();
            }
            else
            {
                control.UpdateMeasureText();
                control.RequestNextFrameRendering();
            }
        });
        SlabThicknessProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        TransitionLayersHeightProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        ShowModelStatsProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
            control.RequestNextFrameRendering());
        ShowCenterOfMassProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            control._needsComUpload = true;
            control.RequestNextFrameRendering();
        });
        CutawayAxisProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            control.UpdateCutawayRange();
            control.UpdateCutawayText();
            control.RequestNextFrameRendering();
        });
        CutawayPositionProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            control.UpdateCutawayText();
            control.RequestNextFrameRendering();
        });
        CutawayInvertProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            control.UpdateCutawayText();
            control.RequestNextFrameRendering();
        });
        ShowPeelCurveProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            control.RequestNextFrameRendering();
        });
        PlateWidthProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            control._needsGridUpload = true;
            control.UpdateModelMetrics();
            control.RequestNextFrameRendering();
        });
        PlateHeightProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            control._needsGridUpload = true;
            control.UpdateModelMetrics();
            control.RequestNextFrameRendering();
        });
        PrintHeightProperty.Changed.AddClassHandler<LayerModel3DView>((control, _) =>
        {
            control._needsGridUpload = true;
            control.UpdateModelMetrics();
            control.RequestNextFrameRendering();
        });
    }

    public LayerModel3DView()
    {
        Focusable = true;
        _cameraAnimationTimer = new DispatcherTimer(DispatcherPriority.Render) { Interval = TimeSpan.FromMilliseconds(16) };
        _cameraAnimationTimer.Tick += CameraAnimationTimerOnTick;
        UpdateMeasureText();
        UpdateModelMetrics();
        UpdateCutawayRange();
        UpdateCutawayText();
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
            _needsGridUpload = true;
            _needsBoundingBoxUpload = true;
            UpdateModelMetrics();
            IsMeasureMode = false;
            ClearMeasure();
            if (value is null)
            {
                IsTurntableActive = false;
                ClearCap();
                ClearFocusedBoundingBox();
            }
            if (resetCamera) ResetCamera();
            RequestNextFrameRendering();
        }
    }

    public bool ShowLayerIssues
    {
        get => GetValue(ShowLayerIssuesProperty);
        set => SetValue(ShowLayerIssuesProperty, value);
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
            if (!value) ClearCap();
            RequestNextFrameRendering();
        }
    }

    public float ClipZ
    {
        get => _clipZ;
        set
        {
            if (Math.Abs(_clipZ - value) < 0.0001f) return;
            _clipZ = value;
            _needsCapGeometryUpload = true;
            RequestNextFrameRendering();
        }
    }

    public void SetCap(byte[] data, int width, int height, float minX, float minY, float maxX, float maxY)
    {
        if (_pendingCapData is not null)
        {
            ArrayPool<byte>.Shared.Return(_pendingCapData);
        }

        _pendingCapData = data;
        _pendingCapWidth = width;
        _pendingCapHeight = height;
        _capMinX = minX;
        _capMinY = minY;
        _capMaxX = maxX;
        _capMaxY = maxY;
        _needsCapUpload = true;
        _needsCapGeometryUpload = true;
        RequestNextFrameRendering();
    }

    public void ClearCap()
    {
        if (_pendingCapData is not null)
        {
            ArrayPool<byte>.Shared.Return(_pendingCapData);
            _pendingCapData = null;
        }

        _hasCapData = false;
        RequestNextFrameRendering();
    }

    public void FocusOnRegion(Vector3 center, float radius)
    {
        CancelCameraAnimation();
        _cameraTarget = center;
        var aspect = Bounds.Height > 0 ? (float)(Bounds.Width / Bounds.Height) : 1.5f;
        aspect = Math.Max(aspect, 0.1f);
        var tanHalfFov = MathF.Tan(MathF.PI / 8f) * MathF.Min(1.0f, aspect);
        var optimalDist = (radius / 0.75f) / tanHalfFov;
        var maxDistance = Math.Max(20f, _modelRadius * 2.6f);
        _cameraDistance = Math.Clamp(optimalDist, 8f, maxDistance);
        CameraChanged();
    }

    public void FocusOnBoundingBox(Vector3 min, Vector3 max)
    {
        CancelCameraAnimation();
        var center = (min + max) / 2f;
        _cameraTarget = center;

        GetCameraBasis(out var direction, out var right, out var up);

        var corners = new Vector3[]
        {
            new(min.X, min.Y, min.Z),
            new(min.X, min.Y, max.Z),
            new(min.X, max.Y, min.Z),
            new(min.X, max.Y, max.Z),
            new(max.X, min.Y, min.Z),
            new(max.X, min.Y, max.Z),
            new(max.X, max.Y, min.Z),
            new(max.X, max.Y, max.Z)
        };

        var maxCamX = 0.001f;
        var maxCamY = 0.001f;
        var maxCamZ = 0f;

        foreach (var p in corners)
        {
            var v = p - center;
            maxCamX = MathF.Max(maxCamX, MathF.Abs(Vector3.Dot(v, right)));
            maxCamY = MathF.Max(maxCamY, MathF.Abs(Vector3.Dot(v, up)));
            maxCamZ = MathF.Max(maxCamZ, Vector3.Dot(v, direction));
        }

        var aspect = Bounds.Height > 0 ? (float)(Bounds.Width / Bounds.Height) : 1.5f;
        aspect = Math.Max(aspect, 0.1f);

        var tanHalfFovY = MathF.Tan(MathF.PI / 8f); // 22.5 deg = ~0.4142
        var tanHalfFovX = aspect * tanHalfFovY;

        // Target filling ~75% of the viewport so the issue is clearly framed with a comfortable margin
        const float targetFill = 0.75f;
        var distY = (maxCamY / targetFill) / tanHalfFovY + maxCamZ;
        var distX = (maxCamX / targetFill) / tanHalfFovX + maxCamZ;

        var optimalDist = MathF.Max(distX, distY);
        var maxDistance = Math.Max(20f, _modelRadius * 2.6f);
        _cameraDistance = Math.Clamp(optimalDist, 8f, maxDistance);

        CameraChanged();
    }

    public void SetFocusedBoundingBox(Vector3 min, Vector3 max)
    {
        _focusBoxMin = min;
        _focusBoxMax = max;
        _hasFocusedBox = true;
        _needsFocusBoxUpload = true;
        RequestNextFrameRendering();
    }

    public void ClearFocusedBoundingBox()
    {
        _hasFocusedBox = false;
        RequestNextFrameRendering();
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

    public VoxelPreviewColorMode ColorMode
    {
        get => GetValue(ColorModeProperty);
        set => SetValue(ColorModeProperty, value);
    }

    public VoxelPreviewClipMode ClipMode
    {
        get => GetValue(ClipModeProperty);
        set => SetValue(ClipModeProperty, value);
    }

    public bool ShowBuildPlateGrid
    {
        get => GetValue(ShowBuildPlateGridProperty);
        set => SetValue(ShowBuildPlateGridProperty, value);
    }

    public bool GhostClippedModel
    {
        get => GetValue(GhostClippedModelProperty);
        set => SetValue(GhostClippedModelProperty, value);
    }

    public bool ShowBoundingBox
    {
        get => GetValue(ShowBoundingBoxProperty);
        set => SetValue(ShowBoundingBoxProperty, value);
    }

    public string? ModelDimensionsText
    {
        get => _modelDimensionsText;
        private set => SetAndRaise(ModelDimensionsTextProperty, ref _modelDimensionsText, value);
    }

    public bool ShowModelStats
    {
        get => GetValue(ShowModelStatsProperty);
        set => SetValue(ShowModelStatsProperty, value);
    }

    public string? ModelStatsText
    {
        get => _modelStatsText;
        private set => SetAndRaise(ModelStatsTextProperty, ref _modelStatsText, value);
    }

    public float ModelVolumeMl
    {
        get => _modelVolumeMl;
        private set => SetAndRaise(ModelVolumeMlProperty, ref _modelVolumeMl, value);
    }

    public float ModelWeightGrams
    {
        get => _modelWeightGrams;
        private set => SetAndRaise(ModelWeightGramsProperty, ref _modelWeightGrams, value);
    }

    public float ModelResinCost
    {
        get => _modelResinCost;
        private set => SetAndRaise(ModelResinCostProperty, ref _modelResinCost, value);
    }

    public bool ShowCenterOfMass
    {
        get => GetValue(ShowCenterOfMassProperty);
        set => SetValue(ShowCenterOfMassProperty, value);
    }

    public string? CenterOfMassText
    {
        get => _centerOfMassText;
        private set => SetAndRaise(CenterOfMassTextProperty, ref _centerOfMassText, value);
    }

    public VoxelPreviewCutawayAxis CutawayAxis
    {
        get => GetValue(CutawayAxisProperty);
        set => SetValue(CutawayAxisProperty, value);
    }

    public float CutawayPosition
    {
        get => GetValue(CutawayPositionProperty);
        set => SetValue(CutawayPositionProperty, value);
    }

    public bool CutawayInvert
    {
        get => GetValue(CutawayInvertProperty);
        set => SetValue(CutawayInvertProperty, value);
    }

    public float CutawayMin
    {
        get => _cutawayMin;
        private set => SetAndRaise(CutawayMinProperty, ref _cutawayMin, value);
    }

    public float CutawayMax
    {
        get => _cutawayMax;
        private set => SetAndRaise(CutawayMaxProperty, ref _cutawayMax, value);
    }

    public string CutawayText
    {
        get => _cutawayText;
        private set => SetAndRaise(CutawayTextProperty, ref _cutawayText, value);
    }

    public bool ShowPeelCurve
    {
        get => GetValue(ShowPeelCurveProperty);
        set => SetValue(ShowPeelCurveProperty, value);
    }

    public Vector3 CenterOfMass
    {
        get => _centerOfMass;
        private set => SetAndRaise(CenterOfMassProperty, ref _centerOfMass, value);
    }

    public float BaseContactArea
    {
        get => _baseContactArea;
        private set => SetAndRaise(BaseContactAreaProperty, ref _baseContactArea, value);
    }

    public bool IsOutOfBounds
    {
        get => _isOutOfBounds;
        private set => SetAndRaise(IsOutOfBoundsProperty, ref _isOutOfBounds, value);
    }

    public string? OutOfBoundsWarningText
    {
        get => _outOfBoundsWarningText;
        private set => SetAndRaise(OutOfBoundsWarningTextProperty, ref _outOfBoundsWarningText, value);
    }

    public Task<WriteableBitmap?> CaptureSnapshotAsync()
    {
        var tcs = new TaskCompletionSource<WriteableBitmap?>();
        _snapshotCompletionSource = tcs;
        RequestNextFrameRendering();
        return tcs.Task;
    }

    private void UpdateModelMetrics()
    {
        if (_mesh is not { } mesh || mesh.VertexCount == 0)
        {
            ModelDimensionsText = null;
            ModelStatsText = null;
            CenterOfMassText = null;
            IsOutOfBounds = false;
            OutOfBoundsWarningText = null;
            ModelVolumeMl = 0f;
            ModelWeightGrams = 0f;
            ModelResinCost = 0f;
            CenterOfMass = Vector3.Zero;
            BaseContactArea = 0f;
            return;
        }

        var size = mesh.Size;
        ModelDimensionsText = $"{size.X:F2} × {size.Y:F2} × {size.Z:F2} mm";

        // Build volume & out-of-bounds check
        if (PlateWidth > 0 && PlateHeight > 0)
        {
            var min = mesh.MinimumBounds;
            var max = mesh.MaximumBounds;
            var oobList = new List<string>(3);

            if (min.X < -0.05f || max.X > PlateWidth + 0.05f)
            {
                var left = min.X < -0.05f ? -min.X : 0f;
                var right = max.X > PlateWidth + 0.05f ? max.X - PlateWidth : 0f;
                oobList.Add($"X: +{Math.Max(left, right):F1}mm");
            }
            if (min.Y < -0.05f || max.Y > PlateHeight + 0.05f)
            {
                var front = min.Y < -0.05f ? -min.Y : 0f;
                var back = max.Y > PlateHeight + 0.05f ? max.Y - PlateHeight : 0f;
                oobList.Add($"Y: +{Math.Max(front, back):F1}mm");
            }
            if (PrintHeight > 0 && max.Z > PrintHeight + 0.05f)
            {
                oobList.Add($"Z: +{max.Z - PrintHeight:F1}mm");
            }

            var isOob = oobList.Count > 0;
            IsOutOfBounds = isOob;
            OutOfBoundsWarningText = isOob ? string.Join(", ", oobList) : null;
        }
        else
        {
            IsOutOfBounds = false;
            OutOfBoundsWarningText = null;
        }

        // Divergence theorem for exact mesh volume & center of mass (tetrahedral decomposition)
        double totalVolume = 0.0;
        double sumVx = 0.0, sumVy = 0.0, sumVz = 0.0;
        var vertices = mesh.Vertices;
        var indices = mesh.Indices;
        var minZ = mesh.MinimumBounds.Z;
        var contactZThreshold = minZ + 0.06f;
        double contactArea = 0.0;
        float contactMinX = float.MaxValue, contactMaxX = float.MinValue;
        float contactMinY = float.MaxValue, contactMaxY = float.MinValue;

        for (int i = 0; i < indices.Length; i += 3)
        {
            var p0 = vertices[(int)indices[i]].Position;
            var p1 = vertices[(int)indices[i + 1]].Position;
            var p2 = vertices[(int)indices[i + 2]].Position;

            double vDet = p0.X * (p1.Y * p2.Z - p1.Z * p2.Y) -
                         p0.Y * (p1.X * p2.Z - p1.Z * p2.X) +
                         p0.Z * (p1.X * p2.Y - p1.Y * p2.X);
            double vol = vDet / 6.0;
            totalVolume += vol;

            double cx = (p0.X + p1.X + p2.X) * 0.25;
            double cy = (p0.Y + p1.Y + p2.Y) * 0.25;
            double cz = (p0.Z + p1.Z + p2.Z) * 0.25;

            sumVx += vol * cx;
            sumVy += vol * cy;
            sumVz += vol * cz;

            if (p0.Z <= contactZThreshold && p1.Z <= contactZThreshold && p2.Z <= contactZThreshold)
            {
                var crossX = (p1.Y - p0.Y) * (p2.Z - p0.Z) - (p1.Z - p0.Z) * (p2.Y - p0.Y);
                var crossY = (p1.Z - p0.Z) * (p2.X - p0.X) - (p1.X - p0.X) * (p2.Z - p0.Z);
                var crossZ = (p1.X - p0.X) * (p2.Y - p0.Y) - (p1.Y - p0.Y) * (p2.X - p0.X);
                var triArea = 0.5 * Math.Sqrt(crossX * crossX + crossY * crossY + crossZ * crossZ);
                contactArea += triArea;

                contactMinX = Math.Min(contactMinX, Math.Min(p0.X, Math.Min(p1.X, p2.X)));
                contactMaxX = Math.Max(contactMaxX, Math.Max(p0.X, Math.Max(p1.X, p2.X)));
                contactMinY = Math.Min(contactMinY, Math.Min(p0.Y, Math.Min(p1.Y, p2.Y)));
                contactMaxY = Math.Max(contactMaxY, Math.Max(p0.Y, Math.Max(p1.Y, p2.Y)));
            }
        }

        double absVolume = Math.Abs(totalVolume);
        float volumeMl = (float)(absVolume / 1000.0);
        float weightG = volumeMl * 1.1f;
        float bottleCost = UserSettings.Instance.General.AverageResin1000MlBottleCost;
        float cost = volumeMl * (bottleCost / 1000.0f);

        ModelVolumeMl = volumeMl;
        ModelWeightGrams = weightG;
        ModelResinCost = cost;
        ModelStatsText = $"Volume: {volumeMl:F1} mL (cm³)  •  Weight: {weightG:F1} g  •  Cost: ${cost:F2}";

        Vector3 com;
        if (absVolume > 1e-4)
        {
            com = new Vector3(
                (float)(sumVx / totalVolume),
                (float)(sumVy / totalVolume),
                (float)(sumVz / totalVolume));
        }
        else
        {
            com = mesh.Center;
        }

        if (float.IsNaN(com.X) || float.IsNaN(com.Y) || float.IsNaN(com.Z) ||
            float.IsInfinity(com.X) || float.IsInfinity(com.Y) || float.IsInfinity(com.Z))
        {
            com = mesh.Center;
        }

        CenterOfMass = com;
        BaseContactArea = (float)contactArea;

        string stabilityText;
        if (contactArea < 0.5)
        {
            stabilityText = "No direct base contact (raft/supports needed)";
        }
        else
        {
            bool withinContact = com.X >= (contactMinX - 1.0f) && com.X <= (contactMaxX + 1.0f) &&
                                 com.Y >= (contactMinY - 1.0f) && com.Y <= (contactMaxY + 1.0f);
            stabilityText = withinContact ? "Stable (CoM over base)" : "High peel/tilt risk (CoM outside base)";
        }

        CenterOfMassText = $"CoM: ({com.X:F1}, {com.Y:F1}, {com.Z:F1}) mm  •  Base Contact: {contactArea:F1} mm²  •  {stabilityText}";
        UpdateBedAdhesionText();
        _needsComUpload = true;
    }

    public bool IsTurntableActive
    {
        get => GetValue(IsTurntableActiveProperty);
        set => SetValue(IsTurntableActiveProperty, value);
    }

    public void StartTurntable()
    {
        _turntableLastTimestamp = Stopwatch.GetTimestamp();
        RequestNextFrameRendering();
    }

    public void StopTurntable()
    {
        _turntableLastTimestamp = 0;
    }

    public bool IsMeasureMode
    {
        get => GetValue(IsMeasureModeProperty);
        set => SetValue(IsMeasureModeProperty, value);
    }

    public string? MeasureDistanceText
    {
        get => _measureDistanceText;
        private set => SetAndRaise(MeasureDistanceTextProperty, ref _measureDistanceText, value);
    }

    public float CameraYaw => _cameraYaw;
    public float CameraPitch => _cameraPitch;
    public float CameraRoll => _cameraRoll;

    public void SetCameraAngles(float yaw, float pitch)
    {
        _cameraYaw = NormalizeAngle(yaw);
        _cameraPitch = Math.Clamp(pitch, -MathF.PI / 2, MathF.PI / 2);
        _cameraRoll = 0f;
        CameraChanged();
    }

    public void UpdateCutawayRange()
    {
        if (_mesh is not null && _mesh.VertexCount > 0)
        {
            var min = _mesh.MinimumBounds;
            var max = _mesh.MaximumBounds;
            if (CutawayAxis == VoxelPreviewCutawayAxis.X)
            {
                CutawayMin = min.X;
                CutawayMax = max.X;
            }
            else if (CutawayAxis == VoxelPreviewCutawayAxis.Y)
            {
                CutawayMin = min.Y;
                CutawayMax = max.Y;
            }
            else
            {
                CutawayMin = Math.Min(min.X, min.Y);
                CutawayMax = Math.Max(max.X, max.Y);
            }
            if (CutawayPosition < CutawayMin || CutawayPosition > CutawayMax)
            {
                CutawayPosition = (CutawayMin + CutawayMax) / 2f;
            }
        }
    }

    public void UpdateCutawayText()
    {
        if (CutawayAxis == VoxelPreviewCutawayAxis.Off)
        {
            CutawayText = "Cut: Off";
        }
        else
        {
            var axisName = CutawayAxis == VoxelPreviewCutawayAxis.X ? "Sagittal (X)" : "Coronal (Y)";
            var inv = CutawayInvert ? " [Inverted]" : string.Empty;
            CutawayText = $"{axisName}: {CutawayPosition:F1} mm{inv}";
        }
    }

    public void ClearMeasure()
    {
        _measurePoint1 = null;
        _measurePoint2 = null;
        _needsMeasureUpload = true;
        UpdateMeasureText();
        RequestNextFrameRendering();
    }

    private void UpdateMeasureText()
    {
        if (_measurePoint1 is { } p1 && _measurePoint2 is { } p2)
        {
            var dist = (p2 - p1).Length();
            var dx = Math.Abs(p2.X - p1.X);
            var dy = Math.Abs(p2.Y - p1.Y);
            var dz = Math.Abs(p2.Z - p1.Z);
            MeasureDistanceText = $"Distance: {dist:F3} mm  |  dx: {dx:F3}  dy: {dy:F3}  dz: {dz:F3}";
        }
        else if (_measurePoint1 is not null)
        {
            MeasureDistanceText = "Click second point...";
        }
        else
        {
            MeasureDistanceText = "Click two points on the model...";
        }
    }

    public float SlabThickness
    {
        get => GetValue(SlabThicknessProperty);
        set => SetValue(SlabThicknessProperty, value);
    }

    public float PlateWidth
    {
        get => GetValue(PlateWidthProperty);
        set => SetValue(PlateWidthProperty, value);
    }

    public float PlateHeight
    {
        get => GetValue(PlateHeightProperty);
        set => SetValue(PlateHeightProperty, value);
    }

    public float PrintHeight
    {
        get => GetValue(PrintHeightProperty);
        set => SetValue(PrintHeightProperty, value);
    }

    public float BottomLayersHeight
    {
        get => GetValue(BottomLayersHeightProperty);
        set => SetValue(BottomLayersHeightProperty, value);
    }

    public float TransitionLayersHeight
    {
        get => GetValue(TransitionLayersHeightProperty);
        set => SetValue(TransitionLayersHeightProperty, value);
    }

    bool ICustomHitTest.HitTest(Point point)
    {
        return new Rect(Bounds.Size).Contains(point);
    }

    public event Action<string?>? RendererStatusChanged;
    public event Action<float, float, float>? CameraOrientationChanged;

    /// <summary>
    /// Raised when the user asks to flip between the perspective and the orthographic projection.
    /// <see cref="IsOrthographic"/> is bound to an user setting, so the owner is the one to flip it.
    /// </summary>
    public event Action? ProjectionToggleRequested;

    public void ResetCamera()
    {
        if (_mesh is null || _mesh.VertexCount == 0) return;
        _cameraTarget = _mesh.Center;
        _modelRadius = Math.Max(_mesh.Size.Length() / 2, 0.5f);
        _cameraDistance = _modelRadius * 2.6f;
        _pitchBaseYaw = -MathF.PI / 2f;
        _pitchCycleState = 0;
        _cameraRoll = 0f;
        AnimateToOrientation(-0.8f, 0.55f, 0f);
    }

    /// <summary>
    /// Centers the model target and zooms the camera distance to fit the model in the viewport,
    /// without modifying the camera orientation (yaw, pitch, roll).
    /// </summary>
    public void FitToView()
    {
        if (_mesh is null || _mesh.VertexCount == 0) return;
        CancelCameraAnimation();
        _cameraTarget = _mesh.Center;
        _modelRadius = Math.Max(_mesh.Size.Length() / 2, 0.5f);
        _cameraDistance = _modelRadius * 2.6f;
        CameraChanged();
    }

    /// <summary>
    /// Rotates the camera orientation by 90-degree increments in the specified direction,
    /// continuously rotating around the axis without getting stuck.
    /// </summary>
    public void RotateStep(float yawDelta, float pitchDelta)
    {
        const float halfPi = MathF.PI / 2f;
        if (pitchDelta != 0)
        {
            if (_cameraPitch >= MathF.PI / 4f)
            {
                _pitchCycleState = 1;
            }
            else if (_cameraPitch <= -MathF.PI / 4f)
            {
                _pitchCycleState = 3;
            }
            else
            {
                var yawDiff = MathF.Abs(ShortestAngleDelta(_cameraYaw, _pitchBaseYaw));
                if (yawDiff > MathF.PI / 2f)
                {
                    _pitchCycleState = 2;
                }
                else
                {
                    _pitchBaseYaw = MathF.Round(_cameraYaw / halfPi) * halfPi;
                    _pitchCycleState = 0;
                }
            }

            var step = pitchDelta > 0 ? 1 : 3;
            _pitchCycleState = (_pitchCycleState + step) % 4;

            float targetPitch;
            float targetYaw;
            switch (_pitchCycleState)
            {
                case 0:
                    targetPitch = 0f;
                    targetYaw = _pitchBaseYaw;
                    break;
                case 1:
                    targetPitch = halfPi;
                    targetYaw = _pitchBaseYaw;
                    break;
                case 2:
                    targetPitch = 0f;
                    targetYaw = NormalizeAngle(_pitchBaseYaw + MathF.PI);
                    break;
                default: // 3
                    targetPitch = -halfPi;
                    targetYaw = _pitchBaseYaw;
                    break;
            }

            AnimateToOrientation(targetYaw, targetPitch, 0f);
        }
        else if (yawDelta != 0)
        {
            var currentYawStep = MathF.Round(_cameraYaw / halfPi);
            var targetYaw = (currentYawStep + MathF.Sign(yawDelta)) * halfPi;
            var targetPitch = MathF.Abs(_cameraPitch) < MathF.PI / 4f ? 0f : MathF.Round(_cameraPitch / halfPi) * halfPi;
            _pitchBaseYaw = targetYaw;
            _pitchCycleState = 0;
            AnimateToOrientation(targetYaw, targetPitch, 0f);
        }
    }

    public void RollStep(float deltaRoll)
    {
        var halfPi = MathF.PI / 2f;
        var currentRollStep = MathF.Round(_cameraRoll / halfPi);
        var targetRoll = (currentRollStep + MathF.Sign(deltaRoll)) * halfPi;
        AnimateToOrientation(_cameraYaw, _cameraPitch, targetRoll);
    }

    public void Orbit(double deltaX, double deltaY)
    {
        CancelCameraAnimation();
        _cameraRoll = 0f;
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

        if (MathF.Abs(targetPitch) < MathF.PI / 4f)
        {
            _pitchBaseYaw = targetYaw;
            _pitchCycleState = 0;
        }
        else
        {
            _pitchCycleState = targetPitch > 0 ? 1 : 3;
        }

        AnimateToOrientation(targetYaw, targetPitch, 0f);
    }

    /// <summary>Orbits by a fixed amount, animated, so that key presses feel like the orientation cube clicks.</summary>
    public void OrbitStep(float yawDelta, float pitchDelta)
    {
        AnimateToOrientation(_cameraYaw + yawDelta,
            Math.Clamp(_cameraPitch + pitchDelta, -MathF.PI / 2, MathF.PI / 2), 0f);
    }

    /// <summary>Zooms by <paramref name="steps"/> wheel notches, positive being closer to the model.</summary>
    public void Zoom(float steps)
    {
        CancelCameraAnimation();
        _cameraDistance *= MathF.Exp(-steps * ZoomSensitivity);
        _cameraDistance = Math.Clamp(_cameraDistance, _modelRadius * 0.08f, _modelRadius * 100);
        RequestNextFrameRendering();
    }

    private void AnimateToOrientation(float targetYaw, float targetPitch, float targetRoll = 0f)
    {
        _cameraAnimationStartYaw = _cameraYaw;
        _cameraAnimationStartPitch = _cameraPitch;
        _cameraAnimationStartRoll = _cameraRoll;
        _cameraAnimationYawDelta = ShortestAngleDelta(_cameraYaw, targetYaw);
        _cameraAnimationRollDelta = ShortestAngleDelta(_cameraRoll, targetRoll);
        _cameraAnimationTargetPitch = targetPitch;
        _cameraAnimationTargetRoll = targetRoll;
        _cameraAnimationStartTimestamp = Stopwatch.GetTimestamp();
        _cameraAnimationTimer.Start();
    }

    protected override unsafe void OnOpenGlInit(GlInterface gl)
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
            _clipZMinLocation = _gl.GetUniformLocation(_shaderProgram, "uClipZMin");
            _clipZMaxLocation = _gl.GetUniformLocation(_shaderProgram, "uClipZMax");
            _clipEnabledLocation = _gl.GetUniformLocation(_shaderProgram, "uClipEnabled");
            _colorModeLocation = _gl.GetUniformLocation(_shaderProgram, "uColorMode");
            _overhangThresholdLocation = _gl.GetUniformLocation(_shaderProgram, "uOverhangThreshold");
            _bottomZLocation = _gl.GetUniformLocation(_shaderProgram, "uBottomZ");
            _transitionZLocation = _gl.GetUniformLocation(_shaderProgram, "uTransitionZ");
            _bottomColorLocation = _gl.GetUniformLocation(_shaderProgram, "uBottomColor");
            _buildVolumeMinLocation = _gl.GetUniformLocation(_shaderProgram, "uBuildVolumeMin");
            _buildVolumeMaxLocation = _gl.GetUniformLocation(_shaderProgram, "uBuildVolumeMax");
            _highlightOutOfBoundsLocation = _gl.GetUniformLocation(_shaderProgram, "uHighlightOutOfBounds");
            _cutawayAxisLocation = _gl.GetUniformLocation(_shaderProgram, "uCutawayAxis");
            _cutawayPositionLocation = _gl.GetUniformLocation(_shaderProgram, "uCutawayPosition");
            _cutawayInvertLocation = _gl.GetUniformLocation(_shaderProgram, "uCutawayInvert");
            _peelTextureLocation = _gl.GetUniformLocation(_shaderProgram, "uPeelTexture");
            _modelMaxZLocation = _gl.GetUniformLocation(_shaderProgram, "uModelMaxZ");
            _firstLayerZLocation = _gl.GetUniformLocation(_shaderProgram, "uFirstLayerZ");

            _peelTexture = _gl.GenTexture();
            _gl.BindTexture(TextureTarget.Texture2D, _peelTexture);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            _gl.BindTexture(TextureTarget.Texture2D, 0);

            _cavityVertexArray = _gl.GenVertexArray();
            _cavityVertexBuffer = _gl.GenBuffer();
            _needsPeelUpload = true;
            _needsCavityUpload = true;

            _vertexArray = _gl.GenVertexArray();
            _vertexBuffer = _gl.GenBuffer();
            _indexBuffer = _gl.GenBuffer();
            _wireframeIndexBuffer = _gl.GenBuffer();
            _issueVertexArray = _gl.GenVertexArray();
            _issueVertexBuffer = _gl.GenBuffer();
            _issueIndexBuffer = _gl.GenBuffer();

            _gridVertexArray = _gl.GenVertexArray();
            _gridVertexBuffer = _gl.GenBuffer();

            _focusBoxVertexArray = _gl.GenVertexArray();
            _focusBoxVertexBuffer = _gl.GenBuffer();
            
            _measureVertexArray = _gl.GenVertexArray();
            _measureVertexBuffer = _gl.GenBuffer();
            _boundingBoxVertexArray = _gl.GenVertexArray();
            _boundingBoxVertexBuffer = _gl.GenBuffer();
            _comVertexArray = _gl.GenVertexArray();
            _comVertexBuffer = _gl.GenBuffer();

            _capShaderProgram = CreateShaderProgram(
                isOpenGles ? EsCapVertexShader : DesktopCapVertexShader,
                isOpenGles ? EsCapFragmentShader : DesktopCapFragmentShader);
            _capViewProjectionLocation = _gl.GetUniformLocation(_capShaderProgram, "uViewProjection");
            _capCutawayAxisLocation = _gl.GetUniformLocation(_capShaderProgram, "uCutawayAxis");
            _capCutawayPositionLocation = _gl.GetUniformLocation(_capShaderProgram, "uCutawayPosition");
            _capCutawayInvertLocation = _gl.GetUniformLocation(_capShaderProgram, "uCutawayInvert");
            _capColorLocation = _gl.GetUniformLocation(_capShaderProgram, "uColor");
            _capAlphaLocation = _gl.GetUniformLocation(_capShaderProgram, "uAlpha");
            _capUnlitLocation = _gl.GetUniformLocation(_capShaderProgram, "uUnlit");
            _capLightDirectionLocation = _gl.GetUniformLocation(_capShaderProgram, "uLightDirection");
            _capAmbientLightLocation = _gl.GetUniformLocation(_capShaderProgram, "uAmbientLight");
            _capTextureLocation = _gl.GetUniformLocation(_capShaderProgram, "uCapTexture");

            _capVertexArray = _gl.GenVertexArray();
            _capVertexBuffer = _gl.GenBuffer();
            _capIndexBuffer = _gl.GenBuffer();
            _capTexture = _gl.GenTexture();

            _gl.BindVertexArray(_capVertexArray);
            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _capVertexBuffer);
            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _capIndexBuffer);

            uint[] capIndices = [0, 1, 2, 0, 2, 3];
            fixed (uint* indexPointer = capIndices)
            {
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(6 * sizeof(uint)), indexPointer,
                    BufferUsageARB.StaticDraw);
            }

            var capVertexSize = (uint)sizeof(CapVertex);
            _gl.EnableVertexAttribArray(0);
            _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, capVertexSize, (void*)0);
            _gl.EnableVertexAttribArray(1);
            _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, capVertexSize,
                (void*)sizeof(Vector3));

            _gl.BindTexture(TextureTarget.Texture2D, _capTexture);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);
            _gl.BindTexture(TextureTarget.Texture2D, 0);
            _gl.BindVertexArray(0);

            _rendererInitialized = true;
            _needsUpload = true;
            _needsWireframeUpload = true;
            _needsIssueUpload = true;
            _needsCapUpload = true;
            _needsCapGeometryUpload = true;
            _needsGridUpload = true;
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
        if (_needsCapUpload) UploadCapTexture();
        if (_needsCapGeometryUpload) UploadCapGeometry();
        if (_needsGridUpload) UploadBuildPlateGrid();
        if (_needsFocusBoxUpload) UploadFocusedBoundingBox();
        if (_needsMeasureUpload) UploadMeasureLine();
        if (_needsBoundingBoxUpload) UploadModelBoundingBox();
        if (_needsComUpload) UploadCenterOfMass();
        if (_needsPeelUpload) UploadPeelTexture();
        if (_needsCavityUpload) UploadCavityMarkers();

        if ((_uploadedIndexCount == 0 || _mesh is null) &&
            (!ShowLayerIssues || _uploadedIssueIndexCount == 0 || _issueMesh is null) &&
            !ShowBuildPlateGrid) return;

        if (IsTurntableActive && !_isDragging && _mesh is not null)
        {
            var now = Stopwatch.GetTimestamp();
            if (_turntableLastTimestamp != 0)
            {
                var dt = (float)Stopwatch.GetElapsedTime(_turntableLastTimestamp, now).TotalSeconds;
                if (dt > 0f)
                {
                    if (dt > 0.1f) dt = 0.033f;
                    const float rotateSpeedRadPerSec = MathF.PI / 6f; // 30 deg/sec
                    _cameraYaw = NormalizeAngle(_cameraYaw + rotateSpeedRadPerSec * dt);
                    NotifyCameraOrientationChanged();
                }
            }
            _turntableLastTimestamp = now;
            RequestNextFrameRendering();
        }
        else if (IsTurntableActive)
        {
            _turntableLastTimestamp = Stopwatch.GetTimestamp();
            RequestNextFrameRendering();
        }

        var viewProjection = GetViewProjection(width / (float)height);
        _gl.UseProgram(_shaderProgram);
        ApplyLighting(_lightDirectionLocation, _ambientLightLocation);

        var clipMinZ = -1e9f;
        var clipMaxZ = 1e9f;
        var ghostMinZ = 0f;
        var ghostMaxZ = 0f;
        var hasGhost = false;

        if (_clipToLayer)
        {
            switch (ClipMode)
            {
                case VoxelPreviewClipMode.Below:
                    clipMaxZ = _clipZ;
                    ghostMinZ = _clipZ;
                    ghostMaxZ = 1e9f;
                    hasGhost = true;
                    break;
                case VoxelPreviewClipMode.Above:
                    clipMinZ = _clipZ;
                    ghostMinZ = -1e9f;
                    ghostMaxZ = _clipZ;
                    hasGhost = true;
                    break;
                case VoxelPreviewClipMode.Slab:
                    clipMinZ = _clipZ - SlabThickness;
                    clipMaxZ = _clipZ;
                    break;
            }
        }

        _gl.Uniform1(_clipZMinLocation, clipMinZ);
        _gl.Uniform1(_clipZMaxLocation, clipMaxZ);
        _gl.Uniform1(_clipEnabledLocation, _clipToLayer ? 1 : 0);
        _gl.Uniform1(_colorModeLocation, (int)ColorMode);
        _gl.Uniform1(_overhangThresholdLocation, 0.7071f);
        _gl.Uniform1(_bottomZLocation, BottomLayersHeight);
        _gl.Uniform1(_transitionZLocation, TransitionLayersHeight);
        _gl.Uniform3(_bottomColorLocation, 0.15f, 0.6f, 1.0f);
        _gl.Uniform1(_cutawayAxisLocation, (int)CutawayAxis);
        _gl.Uniform1(_cutawayPositionLocation, CutawayPosition);
        _gl.Uniform1(_cutawayInvertLocation, CutawayInvert ? 1 : 0);
        _gl.Uniform1(_firstLayerZLocation, FirstLayerHeight);
        _gl.Uniform1(_modelMaxZLocation, _mesh?.MaximumBounds.Z ?? PrintHeight);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _peelTexture);
        _gl.Uniform1(_peelTextureLocation, 0);
        _gl.UniformMatrix4(_viewProjectionLocation, 1, false, (float*)&viewProjection);

        if (_uploadedIndexCount > 0 && _mesh is not null)
        {
            DrawModel();
            if (_clipToLayer) DrawCap(viewProjection);

            if (_clipToLayer && GhostClippedModel && hasGhost && RenderMode != VoxelPreviewRenderMode.Wireframe)
            {
                DrawGhost(ghostMinZ, ghostMaxZ);
            }

        }

        if (ShowBuildPlateGrid)
        {
            DrawBuildPlate();
        }

        if (ShowLayerIssues && _uploadedIssueIndexCount > 0 && _issueMesh is not null)
        {
            _gl.UseProgram(_shaderProgram);
            DrawIssueOverlay();
        }

        if (_hasFocusedBox)
        {
            DrawFocusedBoundingBox();
        }

        if (ShowLayerIssues && _uploadedCavityVertexCount > 0)
        {
            DrawCavityMarkers();
        }

        if (ShowBoundingBox && _mesh is not null && _mesh.VertexCount > 0)
        {
            DrawModelBoundingBox();
        }

        if (ShowCenterOfMass && _mesh is not null && _mesh.VertexCount > 0)
        {
            DrawCenterOfMass();
        }

        if (IsMeasureMode)
        {
            DrawMeasureLine();
        }

        if (_snapshotCompletionSource is { } snapshotTcs)
        {
            _snapshotCompletionSource = null;
            try
            {
                var bitmap = ReadFramebufferToBitmap(width, height, renderScaling);
                snapshotTcs.TrySetResult(bitmap);
            }
            catch (Exception ex)
            {
                snapshotTcs.TrySetException(ex);
            }
        }
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

    private unsafe void UploadCapTexture()
    {
        if (_gl is null || _capTexture == 0) return;
        _needsCapUpload = false;

        var data = _pendingCapData;
        _pendingCapData = null;
        var width = _pendingCapWidth;
        var height = _pendingCapHeight;

        if (data is null || width <= 0 || height <= 0)
        {
            _hasCapData = false;
            return;
        }

        try
        {
            _gl.BindTexture(TextureTarget.Texture2D, _capTexture);
            _gl.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
            fixed (byte* ptr = data)
            {
                if (_capTextureWidth == width && _capTextureHeight == height)
                {
                    _gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)width, (uint)height,
                        PixelFormat.Red, PixelType.UnsignedByte, ptr);
                }
                else
                {
                    _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)GLEnum.R8, (uint)width, (uint)height, 0,
                        PixelFormat.Red, PixelType.UnsignedByte, ptr);
                    _capTextureWidth = width;
                    _capTextureHeight = height;
                }
            }

            _hasCapData = true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }

    private unsafe void UploadCapGeometry()
    {
        if (_gl is null || _capVertexBuffer == 0) return;
        _needsCapGeometryUpload = false;

        Span<CapVertex> vertices = stackalloc CapVertex[4]
        {
            new(new Vector3(_capMinX, _capMinY, _clipZ), new Vector2(0, 0)),
            new(new Vector3(_capMaxX, _capMinY, _clipZ), new Vector2(1, 0)),
            new(new Vector3(_capMaxX, _capMaxY, _clipZ), new Vector2(1, 1)),
            new(new Vector3(_capMinX, _capMaxY, _clipZ), new Vector2(0, 1))
        };

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _capVertexBuffer);
        fixed (CapVertex* vertexPointer = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(4 * sizeof(CapVertex)), vertexPointer,
                BufferUsageARB.DynamicDraw);
        }
    }

    private unsafe void UploadBuildPlateGrid()
    {
        if (_gl is null || _gridVertexBuffer == 0) return;
        _needsGridUpload = false;

        var plateW = PlateWidth > 0 ? PlateWidth : (_mesh?.DisplayWidth > 0 ? _mesh.DisplayWidth : (_mesh?.MaximumBounds.X ?? 120f));
        var plateH = PlateHeight > 0 ? PlateHeight : (_mesh?.DisplayHeight > 0 ? _mesh.DisplayHeight : (_mesh?.MaximumBounds.Y ?? 68f));
        var maxZ = PrintHeight > 0 ? PrintHeight : (_mesh is not null ? Math.Max(_mesh.MaximumBounds.Z + 10f, 50f) : 150f);

        var lines = new List<VoxelPreviewVertex>();

        // Grid lines every 10mm along X
        for (var x = 0f; x <= plateW + 0.001f; x += 10f)
        {
            lines.Add(new VoxelPreviewVertex(new Vector3(x, 0, 0), Vector3.UnitZ));
            lines.Add(new VoxelPreviewVertex(new Vector3(x, plateH, 0), Vector3.UnitZ));
        }

        // Grid lines every 10mm along Y
        for (var y = 0f; y <= plateH + 0.001f; y += 10f)
        {
            lines.Add(new VoxelPreviewVertex(new Vector3(0, y, 0), Vector3.UnitZ));
            lines.Add(new VoxelPreviewVertex(new Vector3(plateW, y, 0), Vector3.UnitZ));
        }

        // 4 vertical columns of print volume box
        lines.Add(new VoxelPreviewVertex(new Vector3(0, 0, 0), Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(new Vector3(0, 0, maxZ), Vector3.UnitZ));

        lines.Add(new VoxelPreviewVertex(new Vector3(plateW, 0, 0), Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(new Vector3(plateW, 0, maxZ), Vector3.UnitZ));

        lines.Add(new VoxelPreviewVertex(new Vector3(plateW, plateH, 0), Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(new Vector3(plateW, plateH, maxZ), Vector3.UnitZ));

        lines.Add(new VoxelPreviewVertex(new Vector3(0, plateH, 0), Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(new Vector3(0, plateH, maxZ), Vector3.UnitZ));

        // Top rectangle at maxZ
        lines.Add(new VoxelPreviewVertex(new Vector3(0, 0, maxZ), Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(new Vector3(plateW, 0, maxZ), Vector3.UnitZ));

        lines.Add(new VoxelPreviewVertex(new Vector3(plateW, 0, maxZ), Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(new Vector3(plateW, plateH, maxZ), Vector3.UnitZ));

        lines.Add(new VoxelPreviewVertex(new Vector3(plateW, plateH, maxZ), Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(new Vector3(0, plateH, maxZ), Vector3.UnitZ));

        lines.Add(new VoxelPreviewVertex(new Vector3(0, plateH, maxZ), Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(new Vector3(0, 0, maxZ), Vector3.UnitZ));

        _gridVertexCount = lines.Count;
        _gl.BindVertexArray(_gridVertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _gridVertexBuffer);

        var span = CollectionsMarshal.AsSpan(lines);
        fixed (VoxelPreviewVertex* ptr = span)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(lines.Count * sizeof(VoxelPreviewVertex)), ptr,
                BufferUsageARB.StaticDraw);
        }

        var vertexSize = (uint)sizeof(VoxelPreviewVertex);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)sizeof(Vector3));
        _gl.BindVertexArray(0);
    }

    private unsafe void UploadFocusedBoundingBox()
    {
        if (_gl is null || _focusBoxVertexBuffer == 0) return;
        _needsFocusBoxUpload = false;

        var min = _focusBoxMin;
        var max = _focusBoxMax;

        Span<VoxelPreviewVertex> boxVertices = stackalloc VoxelPreviewVertex[24]
        {
            new(new Vector3(min.X, min.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(max.X, min.Y, min.Z), Vector3.UnitZ),

            new(new Vector3(max.X, min.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(max.X, max.Y, min.Z), Vector3.UnitZ),

            new(new Vector3(max.X, max.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(min.X, max.Y, min.Z), Vector3.UnitZ),

            new(new Vector3(min.X, max.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(min.X, min.Y, min.Z), Vector3.UnitZ),

            new(new Vector3(min.X, min.Y, max.Z), Vector3.UnitZ),
            new(new Vector3(max.X, min.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(max.X, min.Y, max.Z), Vector3.UnitZ),
            new(new Vector3(max.X, max.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(max.X, max.Y, max.Z), Vector3.UnitZ),
            new(new Vector3(min.X, max.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(min.X, max.Y, max.Z), Vector3.UnitZ),
            new(new Vector3(min.X, min.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(min.X, min.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(min.X, min.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(max.X, min.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(max.X, min.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(max.X, max.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(max.X, max.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(min.X, max.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(min.X, max.Y, max.Z), Vector3.UnitZ)
        };

        _gl.BindVertexArray(_focusBoxVertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _focusBoxVertexBuffer);
        fixed (VoxelPreviewVertex* ptr = boxVertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(24 * sizeof(VoxelPreviewVertex)), ptr,
                BufferUsageARB.DynamicDraw);
        }

        var vertexSize = (uint)sizeof(VoxelPreviewVertex);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)sizeof(Vector3));
        _gl.BindVertexArray(0);
    }

    private unsafe void DrawBuildPlate()
    {
        if (_gl is null || !ShowBuildPlateGrid || _gridVertexCount == 0 || _gridVertexArray == 0) return;

        _gl.UseProgram(_shaderProgram);
        _gl.Uniform1(_unlitLocation, 1);
        _gl.Uniform1(_clipEnabledLocation, 0);
        _gl.Uniform1(_highlightOutOfBoundsLocation, 0);
        _gl.Uniform1(_alphaLocation, IsOutOfBounds ? 0.65f : 0.35f);
        if (IsOutOfBounds)
        {
            _gl.Uniform3(_colorLocation, 1.0f, 0.25f, 0.25f);
        }
        else
        {
            _gl.Uniform3(_colorLocation, 0.35f, 0.55f, 0.85f);
        }
        _gl.BindVertexArray(_gridVertexArray);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)_gridVertexCount);
        _gl.Disable(EnableCap.Blend);
    }

    
    private unsafe void UploadModelBoundingBox()
    {
        if (_gl is null || _boundingBoxVertexBuffer == 0 || _mesh is null || _mesh.VertexCount == 0) return;
        _needsBoundingBoxUpload = false;

        var min = _mesh.MinimumBounds;
        var max = _mesh.MaximumBounds;

        Span<VoxelPreviewVertex> boxVertices = stackalloc VoxelPreviewVertex[24]
        {
            // Bottom 4 edges
            new(new Vector3(min.X, min.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(max.X, min.Y, min.Z), Vector3.UnitZ),

            new(new Vector3(max.X, min.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(max.X, max.Y, min.Z), Vector3.UnitZ),

            new(new Vector3(max.X, max.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(min.X, max.Y, min.Z), Vector3.UnitZ),

            new(new Vector3(min.X, max.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(min.X, min.Y, min.Z), Vector3.UnitZ),

            // Top 4 edges
            new(new Vector3(min.X, min.Y, max.Z), Vector3.UnitZ),
            new(new Vector3(max.X, min.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(max.X, min.Y, max.Z), Vector3.UnitZ),
            new(new Vector3(max.X, max.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(max.X, max.Y, max.Z), Vector3.UnitZ),
            new(new Vector3(min.X, max.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(min.X, max.Y, max.Z), Vector3.UnitZ),
            new(new Vector3(min.X, min.Y, max.Z), Vector3.UnitZ),

            // 4 Vertical Pillars
            new(new Vector3(min.X, min.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(min.X, min.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(max.X, min.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(max.X, min.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(max.X, max.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(max.X, max.Y, max.Z), Vector3.UnitZ),

            new(new Vector3(min.X, max.Y, min.Z), Vector3.UnitZ),
            new(new Vector3(min.X, max.Y, max.Z), Vector3.UnitZ)
        };

        _gl.BindVertexArray(_boundingBoxVertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _boundingBoxVertexBuffer);
        fixed (VoxelPreviewVertex* ptr = boxVertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(24 * sizeof(VoxelPreviewVertex)), ptr,
                BufferUsageARB.DynamicDraw);
        }

        var vertexSize = (uint)sizeof(VoxelPreviewVertex);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)sizeof(Vector3));
        _gl.BindVertexArray(0);
    }

    private unsafe void DrawModelBoundingBox()
    {
        if (_gl is null || !ShowBoundingBox || _boundingBoxVertexArray == 0 || _mesh is null) return;

        _gl.UseProgram(_shaderProgram);
        _gl.Uniform1(_unlitLocation, 1);
        _gl.Uniform1(_clipEnabledLocation, 0);
        _gl.Uniform3(_colorLocation, 0.0f, 0.82f, 1.0f);
        _gl.BindVertexArray(_boundingBoxVertexArray);

        // Pass 1: Draw faint behind occluded geometry
        _gl.Disable(EnableCap.DepthTest);
        _gl.Uniform1(_alphaLocation, 0.35f);
        _gl.DrawArrays(PrimitiveType.Lines, 0, 24);

        // Pass 2: Draw crisp in front of geometry
        _gl.Enable(EnableCap.DepthTest);
        _gl.Uniform1(_alphaLocation, 0.95f);
        _gl.DrawArrays(PrimitiveType.Lines, 0, 24);
    }

    private unsafe void UploadCenterOfMass()
    {
        if (_gl is null || _comVertexBuffer == 0) return;
        _needsComUpload = false;

        if (_mesh is null || _mesh.VertexCount == 0)
        {
            _comVertexCount = 0;
            return;
        }

        var lines = new List<VoxelPreviewVertex>(64);
        var c = _centerOfMass;
        var r = 7.0f;
        var d = 3.5f;

        // 1. 3D Crosshair at CoM
        lines.Add(new(new Vector3(c.X - r, c.Y, c.Z), Vector3.UnitZ));
        lines.Add(new(new Vector3(c.X + r, c.Y, c.Z), Vector3.UnitZ));

        lines.Add(new(new Vector3(c.X, c.Y - r, c.Z), Vector3.UnitZ));
        lines.Add(new(new Vector3(c.X, c.Y + r, c.Z), Vector3.UnitZ));

        lines.Add(new(new Vector3(c.X, c.Y, c.Z - r), Vector3.UnitZ));
        lines.Add(new(new Vector3(c.X, c.Y, c.Z + r), Vector3.UnitZ));

        // 2. Diamond / octahedron wireframe around CoM
        var top = new Vector3(c.X, c.Y, c.Z + d);
        var bot = new Vector3(c.X, c.Y, c.Z - d);
        var pXp = new Vector3(c.X + d, c.Y, c.Z);
        var pXm = new Vector3(c.X - d, c.Y, c.Z);
        var pYp = new Vector3(c.X, c.Y + d, c.Z);
        var pYm = new Vector3(c.X - d, c.Y, c.Z);

        lines.Add(new(top, Vector3.UnitZ)); lines.Add(new(pXp, Vector3.UnitZ));
        lines.Add(new(top, Vector3.UnitZ)); lines.Add(new(pXm, Vector3.UnitZ));
        lines.Add(new(top, Vector3.UnitZ)); lines.Add(new(pYp, Vector3.UnitZ));
        lines.Add(new(top, Vector3.UnitZ)); lines.Add(new(pYm, Vector3.UnitZ));

        lines.Add(new(bot, Vector3.UnitZ)); lines.Add(new(pXp, Vector3.UnitZ));
        lines.Add(new(bot, Vector3.UnitZ)); lines.Add(new(pXm, Vector3.UnitZ));
        lines.Add(new(bot, Vector3.UnitZ)); lines.Add(new(pYp, Vector3.UnitZ));
        lines.Add(new(bot, Vector3.UnitZ)); lines.Add(new(pYm, Vector3.UnitZ));

        lines.Add(new(pXp, Vector3.UnitZ)); lines.Add(new(pYp, Vector3.UnitZ));
        lines.Add(new(pYp, Vector3.UnitZ)); lines.Add(new(pXm, Vector3.UnitZ));
        lines.Add(new(pXm, Vector3.UnitZ)); lines.Add(new(pYm, Vector3.UnitZ));
        lines.Add(new(pYm, Vector3.UnitZ)); lines.Add(new(pXp, Vector3.UnitZ));

        // 3. Plumb line down to build plate (Z = 0)
        lines.Add(new(new Vector3(c.X, c.Y, c.Z), Vector3.UnitZ));
        lines.Add(new(new Vector3(c.X, c.Y, 0f), Vector3.UnitZ));

        // 4. Base landing target (cross & circle at Z = 0.05f to avoid z-fighting)
        var bz = 0.05f;
        lines.Add(new(new Vector3(c.X - r, c.Y, bz), Vector3.UnitZ));
        lines.Add(new(new Vector3(c.X + r, c.Y, bz), Vector3.UnitZ));
        lines.Add(new(new Vector3(c.X, c.Y - r, bz), Vector3.UnitZ));
        lines.Add(new(new Vector3(c.X, c.Y + r, bz), Vector3.UnitZ));

        const int segments = 12;
        for (int i = 0; i < segments; i++)
        {
            var a1 = i * (MathF.Tau / segments);
            var a2 = (i + 1) * (MathF.Tau / segments);
            lines.Add(new(new Vector3(c.X + MathF.Cos(a1) * (r * 0.5f), c.Y + MathF.Sin(a1) * (r * 0.5f), bz), Vector3.UnitZ));
            lines.Add(new(new Vector3(c.X + MathF.Cos(a2) * (r * 0.5f), c.Y + MathF.Sin(a2) * (r * 0.5f), bz), Vector3.UnitZ));
        }

        _comVertexCount = lines.Count;
        _gl.BindVertexArray(_comVertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _comVertexBuffer);

        var span = CollectionsMarshal.AsSpan(lines);
        fixed (VoxelPreviewVertex* ptr = span)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(lines.Count * sizeof(VoxelPreviewVertex)), ptr,
                BufferUsageARB.StaticDraw);
        }

        var vertexSize = (uint)sizeof(VoxelPreviewVertex);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)sizeof(Vector3));
        _gl.BindVertexArray(0);
    }

    private unsafe void DrawCenterOfMass()
    {
        if (_gl is null || !ShowCenterOfMass || _comVertexCount == 0 || _comVertexArray == 0) return;

        _gl.UseProgram(_shaderProgram);
        _gl.Uniform1(_unlitLocation, 1);
        _gl.Uniform1(_clipEnabledLocation, 0);
        _gl.Uniform1(_highlightOutOfBoundsLocation, 0);
        _gl.BindVertexArray(_comVertexArray);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Pass 1: X-ray / through model
        _gl.Disable(EnableCap.DepthTest);
        _gl.Uniform1(_alphaLocation, 0.35f);
        _gl.Uniform3(_colorLocation, 1.0f, 0.82f, 0.1f);
        _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)_comVertexCount);

        // Pass 2: In-front crisp
        _gl.Enable(EnableCap.DepthTest);
        _gl.Uniform1(_alphaLocation, 1.0f);
        _gl.Uniform3(_colorLocation, 1.0f, 0.85f, 0.15f);
        _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)_comVertexCount);

        _gl.Disable(EnableCap.Blend);
    }

    private unsafe WriteableBitmap ReadFramebufferToBitmap(uint width, uint height, double renderScaling)
    {
        var pixelWidth = (int)width;
        var pixelHeight = (int)height;
        var stride = pixelWidth * 4;
        var rawBytes = new byte[stride * pixelHeight];

        fixed (byte* pRaw = rawBytes)
        {
            _gl!.PixelStore(PixelStoreParameter.PackAlignment, 1);
            _gl.ReadPixels(0, 0, width, height, Silk.NET.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, pRaw);
        }

        var dpi = 96.0 * renderScaling;
        var bitmap = new WriteableBitmap(
            new PixelSize(pixelWidth, pixelHeight),
            new Avalonia.Vector(dpi, dpi),
            Avalonia.Platform.PixelFormat.Rgba8888,
            Avalonia.Platform.AlphaFormat.Premul);

        using (var fb = bitmap.Lock())
        {
            var destPtr = (byte*)fb.Address;
            var destStride = fb.RowBytes;
            fixed (byte* srcPtr = rawBytes)
            {
                for (var y = 0; y < pixelHeight; y++)
                {
                    var srcRow = srcPtr + (pixelHeight - 1 - y) * stride;
                    var destRow = destPtr + y * destStride;
                    System.Buffer.MemoryCopy(srcRow, destRow, destStride, stride);
                }
            }
        }

        return bitmap;
    }

    private unsafe void UploadMeasureLine()
    {
        if (_gl is null || _measureVertexBuffer == 0) return;
        _needsMeasureUpload = false;

        if (_measurePoint1 is not { } p1) 
        {
            _measureVertexCount = 0;
            return;
        }

        var p2 = _measurePoint2 ?? p1;
        var lines = new System.Collections.Generic.List<VoxelPreviewVertex>();
        
        // Main line between points
        lines.Add(new VoxelPreviewVertex(p1, Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(p2, Vector3.UnitZ));

        // Cross for point 1
        float crossSize = 1.0f;
        lines.Add(new VoxelPreviewVertex(p1 - Vector3.UnitX * crossSize, Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(p1 + Vector3.UnitX * crossSize, Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(p1 - Vector3.UnitY * crossSize, Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(p1 + Vector3.UnitY * crossSize, Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(p1 - Vector3.UnitZ * crossSize, Vector3.UnitZ));
        lines.Add(new VoxelPreviewVertex(p1 + Vector3.UnitZ * crossSize, Vector3.UnitZ));

        if (_measurePoint2 is not null)
        {
            // Cross for point 2
            lines.Add(new VoxelPreviewVertex(p2 - Vector3.UnitX * crossSize, Vector3.UnitZ));
            lines.Add(new VoxelPreviewVertex(p2 + Vector3.UnitX * crossSize, Vector3.UnitZ));
            lines.Add(new VoxelPreviewVertex(p2 - Vector3.UnitY * crossSize, Vector3.UnitZ));
            lines.Add(new VoxelPreviewVertex(p2 + Vector3.UnitY * crossSize, Vector3.UnitZ));
            lines.Add(new VoxelPreviewVertex(p2 - Vector3.UnitZ * crossSize, Vector3.UnitZ));
            lines.Add(new VoxelPreviewVertex(p2 + Vector3.UnitZ * crossSize, Vector3.UnitZ));
        }

        _measureVertexCount = lines.Count;

        _gl.BindVertexArray(_measureVertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _measureVertexBuffer);

        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(lines);
        fixed (VoxelPreviewVertex* ptr = span)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(lines.Count * sizeof(VoxelPreviewVertex)), ptr,
                BufferUsageARB.StaticDraw);
        }

        var vertexSize = (uint)sizeof(VoxelPreviewVertex);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)sizeof(Vector3));
        _gl.BindVertexArray(0);
    }

    private unsafe void DrawMeasureLine()
    {
        if (_gl is null || _measureVertexCount == 0 || _measureVertexArray == 0) return;

        // Draw opaque geometry on top of model without depth testing so it's always visible
        _gl.UseProgram(_shaderProgram);
        _gl.Uniform1(_unlitLocation, 1);
        _gl.Uniform1(_clipEnabledLocation, 0);
        _gl.Uniform1(_alphaLocation, 1f);
        _gl.Uniform3(_colorLocation, 1.0f, 0.4f, 0.7f); // Distinct pinkish/magenta color
        
        _gl.Disable(EnableCap.DepthTest);
        _gl.BindVertexArray(_measureVertexArray);
        _gl.DrawArrays(PrimitiveType.Lines, 0, (uint)_measureVertexCount);
        _gl.Enable(EnableCap.DepthTest);
    }

    public void SetPeelData(float[]? areas, List<int>? spikes, float maxArea)
    {
        _peelAreas = areas;
        _peelSpikes = spikes;
        _peelMaxArea = maxArea;
        _needsPeelUpload = true;
        UpdateBedAdhesionText();
        RequestNextFrameRendering();
    }

    private void UpdateBedAdhesionText()
    {
        float contact = BaseContactArea;
        float maxA = _peelMaxArea > 0.001f ? _peelMaxArea : (_mesh is not null ? (_mesh.MaximumBounds.X - _mesh.MinimumBounds.X) * (_mesh.MaximumBounds.Y - _mesh.MinimumBounds.Y) : 0f);
        if (contact <= 0.001f)
        {
            BedAdhesionText = "No direct bed contact detected (raft / supports required)";
            return;
        }

        float ratio = maxA > 0.001f ? MathF.Min((contact / maxA) * 100f, 100f) : 100f;
        string assessment = ratio switch
        {
            >= 30f => "Excellent direct adhesion (> 30% of peak)",
            >= 15f => "Adequate direct adhesion (15%–30%)",
            >= 5f => "Caution: Weak adhesion (< 15%). Delamination risk",
            _ => "Critical: Severe detachment risk (< 5%). Add raft!"
        };

        BedAdhesionText = $"Base Contact: {contact:F1} mm² ({ratio:F1}% of peak) • {assessment}";
    }

    private unsafe void UploadPeelTexture()
    {
        if (_gl is null || _peelTexture == 0) return;
        _needsPeelUpload = false;

        int width = _peelAreas is { Length: > 0 } ? Math.Max(_peelAreas.Length, 256) : 256;
        byte[] rgba = new byte[width * 4];

        if (_peelAreas is { Length: > 0 } areas && _peelMaxArea > 0.001f)
        {
            var spikes = _peelSpikes;
            for (int x = 0; x < width; x++)
            {
                int layerIdx = Math.Clamp((int)Math.Round((float)x / (width - 1) * (areas.Length - 1)), 0, areas.Length - 1);
                float a = areas[layerIdx];
                float normA = Math.Clamp(a / _peelMaxArea, 0f, 1f);
                bool isSpike = spikes is not null && spikes.Contains(layerIdx);

                byte r, g, b;
                if (isSpike)
                {
                    r = 255;
                    g = 20;
                    b = 85;
                }
                else if (normA < 0.25f)
                {
                    float t = normA / 0.25f;
                    r = (byte)(35 + (0 - 35) * t);
                    g = (byte)(90 + (210 - 90) * t);
                    b = (byte)(225 + (225 - 225) * t);
                }
                else if (normA < 0.55f)
                {
                    float t = (normA - 0.25f) / 0.30f;
                    r = (byte)(0 + (25 - 0) * t);
                    g = (byte)(210 + (220 - 210) * t);
                    b = (byte)(225 + (70 - 225) * t);
                }
                else if (normA < 0.80f)
                {
                    float t = (normA - 0.55f) / 0.25f;
                    r = (byte)(25 + (255 - 25) * t);
                    g = (byte)(220 + (190 - 220) * t);
                    b = (byte)(70 + (0 - 70) * t);
                }
                else
                {
                    float t = (normA - 0.80f) / 0.20f;
                    r = (byte)(255 + (245 - 255) * t);
                    g = (byte)(190 + (30 - 190) * t);
                    b = 0;
                }

                int offset = x * 4;
                rgba[offset] = r;
                rgba[offset + 1] = g;
                rgba[offset + 2] = b;
                rgba[offset + 3] = 255;
            }
        }
        else
        {
            for (int x = 0; x < width; x++)
            {
                int offset = x * 4;
                rgba[offset] = 0;
                rgba[offset + 1] = 190;
                rgba[offset + 2] = 235;
                rgba[offset + 3] = 255;
            }
        }

        _gl.BindTexture(TextureTarget.Texture2D, _peelTexture);
        fixed (byte* p = rgba)
        {
            _gl.TexImage2D(TextureTarget.Texture2D, 0, (int)GLEnum.Rgba8, (uint)width, 1, 0, GLEnum.Rgba, GLEnum.UnsignedByte, p);
        }
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void SetCavityMarkers(IReadOnlyList<CavityMarker3D>? markers)
    {
        _cavityMarkers.Clear();
        if (markers is { Count: > 0 })
        {
            _cavityMarkers.AddRange(markers);
        }
        _needsCavityUpload = true;
        RequestNextFrameRendering();
    }

    private unsafe void UploadCavityMarkers()
    {
        if (_gl is null || _cavityVertexBuffer == 0) return;
        _needsCavityUpload = false;

        if (_cavityMarkers.Count == 0)
        {
            _uploadedCavityVertexCount = 0;
            return;
        }

        int totalVertices = _cavityMarkers.Count * 30;
        var vertices = new VoxelPreviewVertex[totalVertices];
        int vIdx = 0;

        foreach (var marker in _cavityMarkers)
        {
            var min = marker.Min;
            var max = marker.Max;

            // 12 edges (24 vertices)
            vertices[vIdx++] = new(new Vector3(min.X, min.Y, min.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(max.X, min.Y, min.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(max.X, min.Y, min.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(max.X, max.Y, min.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(max.X, max.Y, min.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(min.X, max.Y, min.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(min.X, max.Y, min.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(min.X, min.Y, min.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(min.X, min.Y, max.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(max.X, min.Y, max.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(max.X, min.Y, max.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(max.X, max.Y, max.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(max.X, max.Y, max.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(min.X, max.Y, max.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(min.X, max.Y, max.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(min.X, min.Y, max.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(min.X, min.Y, min.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(min.X, min.Y, max.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(max.X, min.Y, min.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(max.X, min.Y, max.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(max.X, max.Y, min.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(max.X, max.Y, max.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(min.X, max.Y, min.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(min.X, max.Y, max.Z), Vector3.UnitZ);

            // Center crosshair (6 vertices)
            var center = (min + max) * 0.5f;
            var ext = (max - min) * 0.35f;

            vertices[vIdx++] = new(new Vector3(center.X - ext.X, center.Y, center.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(center.X + ext.X, center.Y, center.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(center.X, center.Y - ext.Y, center.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(center.X, center.Y + ext.Y, center.Z), Vector3.UnitZ);

            vertices[vIdx++] = new(new Vector3(center.X, center.Y, center.Z - ext.Z), Vector3.UnitZ);
            vertices[vIdx++] = new(new Vector3(center.X, center.Y, center.Z + ext.Z), Vector3.UnitZ);
        }

        _uploadedCavityVertexCount = totalVertices;

        _gl.BindVertexArray(_cavityVertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _cavityVertexBuffer);
        fixed (VoxelPreviewVertex* ptr = vertices)
        {
            _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(totalVertices * sizeof(VoxelPreviewVertex)), ptr,
                BufferUsageARB.DynamicDraw);
        }

        var vertexSize = (uint)sizeof(VoxelPreviewVertex);
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)0);
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, vertexSize, (void*)sizeof(Vector3));
        _gl.BindVertexArray(0);
    }

    private unsafe void DrawCavityMarkers()
    {
        if (_gl is null || !ShowLayerIssues || _uploadedCavityVertexCount == 0 || _cavityVertexArray == 0) return;

        _gl.UseProgram(_shaderProgram);
        _gl.Uniform1(_unlitLocation, 1);
        _gl.Uniform1(_clipEnabledLocation, 0);
        _gl.Uniform1(_alphaLocation, 0.90f);
        _gl.BindVertexArray(_cavityVertexArray);
        _gl.Disable(EnableCap.DepthTest);

        int currentVertex = 0;
        foreach (var marker in _cavityMarkers)
        {
            if (marker.IsSuctionCup)
            {
                _gl.Uniform3(_colorLocation, 1.0f, 0.65f, 0.15f);
            }
            else
            {
                _gl.Uniform3(_colorLocation, 1.0f, 0.34f, 0.13f);
            }

            _gl.DrawArrays(PrimitiveType.Lines, currentVertex, 30);
            currentVertex += 30;
        }

        _gl.Enable(EnableCap.DepthTest);
    }

    private unsafe void DrawFocusedBoundingBox()
    {
        if (_gl is null || !_hasFocusedBox || _focusBoxVertexArray == 0) return;

        _gl.UseProgram(_shaderProgram);
        _gl.Uniform1(_unlitLocation, 1);
        _gl.Uniform1(_clipEnabledLocation, 0);
        _gl.Uniform1(_alphaLocation, 0.95f);
        _gl.Uniform3(_colorLocation, 1.0f, 0.85f, 0.1f);
        _gl.BindVertexArray(_focusBoxVertexArray);
        _gl.Disable(EnableCap.DepthTest);
        _gl.DrawArrays(PrimitiveType.Lines, 0, 24);
        _gl.Enable(EnableCap.DepthTest);
    }

    private unsafe void DrawGhost(float ghostMinZ, float ghostMaxZ)
    {
        if (_gl is null || _mesh is null) return;

        _gl.UseProgram(_shaderProgram);
        _gl.Uniform1(_clipEnabledLocation, 1);
        _gl.Uniform1(_clipZMinLocation, ghostMinZ);
        _gl.Uniform1(_clipZMaxLocation, ghostMaxZ);
        _gl.Uniform1(_alphaLocation, 0.12f);
        _gl.Uniform1(_unlitLocation, 1);
        _gl.Uniform3(_colorLocation, VoxelColor.R / 255f * 0.7f, VoxelColor.G / 255f * 0.7f, VoxelColor.B / 255f * 0.7f);

        _gl.BindVertexArray(_vertexArray);
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.DepthMask(false);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)_uploadedIndexCount, DrawElementsType.UnsignedInt, null);
        _gl.DepthMask(true);
        _gl.Disable(EnableCap.Blend);
    }

    private unsafe void DrawCap(Matrix4x4 viewProjection)
    {
        if (_gl is null || !_hasCapData || _capShaderProgram == 0 ||
            RenderMode == VoxelPreviewRenderMode.Wireframe) return;

        _gl.UseProgram(_capShaderProgram);
        _gl.UniformMatrix4(_capViewProjectionLocation, 1, false, (float*)&viewProjection);
        _gl.Uniform3(_capColorLocation, VoxelColor.R / 255f, VoxelColor.G / 255f, VoxelColor.B / 255f);
        _gl.Uniform1(_capAlphaLocation, RenderMode == VoxelPreviewRenderMode.XRay ? XRayOpacity : 1f);
        _gl.Uniform1(_capUnlitLocation, 0);
        _gl.Uniform1(_capCutawayAxisLocation, (int)CutawayAxis);
        _gl.Uniform1(_capCutawayPositionLocation, CutawayPosition);
        _gl.Uniform1(_capCutawayInvertLocation, CutawayInvert ? 1 : 0);
        ApplyLighting(_capLightDirectionLocation, _capAmbientLightLocation);

        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, _capTexture);
        _gl.Uniform1(_capTextureLocation, 0);

        _gl.BindVertexArray(_capVertexArray);
        _gl.Disable(EnableCap.CullFace);

        if (RenderMode == VoxelPreviewRenderMode.XRay)
        {
            _gl.Enable(EnableCap.Blend);
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            _gl.DepthMask(false);
            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
            _gl.DepthMask(true);
            _gl.Disable(EnableCap.Blend);
        }
        else
        {
            _gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, null);
        }

        _gl.Enable(EnableCap.CullFace);
    }

    private unsafe void DrawModel()
    {
        if (_gl is null) return;

        if (IsOutOfBounds && PlateWidth > 0 && PlateHeight > 0)
        {
            _gl.Uniform1(_highlightOutOfBoundsLocation, 1);
            _gl.Uniform3(_buildVolumeMinLocation, 0f, 0f, 0f);
            _gl.Uniform3(_buildVolumeMaxLocation, PlateWidth, PlateHeight, PrintHeight > 0 ? PrintHeight : 1e9f);
        }
        else
        {
            _gl.Uniform1(_highlightOutOfBoundsLocation, 0);
        }

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

    private void ApplyLighting(int lightDirectionLocation, int ambientLightLocation)
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

        _gl.Uniform3(lightDirectionLocation, direction.X, direction.Y, direction.Z);
        _gl.Uniform1(ambientLightLocation, ambientLight);
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
        if (_gl is null || !ShowLayerIssues || _issueMesh is null) return;

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

    public bool TryPickModel(Point screenPoint, out Vector3 hitPoint)
    {
        hitPoint = Vector3.Zero;
        if (_mesh is null || _mesh.VertexCount == 0 || Bounds.Width <= 0 || Bounds.Height <= 0)
            return false;

        var width = (float)Bounds.Width;
        var height = (float)Bounds.Height;
        var ndcX = (float)(2.0 * screenPoint.X / width - 1.0);
        var ndcY = (float)(1.0 - 2.0 * screenPoint.Y / height);

        var viewProjection = GetViewProjection(width / height);
        if (!Matrix4x4.Invert(viewProjection, out var invViewProj))
            return false;

        var nearSource = new Vector4(ndcX, ndcY, -1.0f, 1.0f);
        var farSource = new Vector4(ndcX, ndcY, 1.0f, 1.0f);

        var nearWorld = Vector4.Transform(nearSource, invViewProj);
        var farWorld = Vector4.Transform(farSource, invViewProj);

        if (Math.Abs(nearWorld.W) < 1e-6f || Math.Abs(farWorld.W) < 1e-6f)
            return false;

        var rayOrigin = new Vector3(nearWorld.X / nearWorld.W, nearWorld.Y / nearWorld.W, nearWorld.Z / nearWorld.W);
        var rayFar = new Vector3(farWorld.X / farWorld.W, farWorld.Y / farWorld.W, farWorld.Z / farWorld.W);
        var rayDirection = Vector3.Normalize(rayFar - rayOrigin);

        var vertices = _mesh.Vertices;
        var indices = _mesh.Indices;
        var closestT = float.MaxValue;
        var hasHit = false;

        var clipMinZ = -1e9f;
        var clipMaxZ = 1e9f;
        if (ClipToLayer)
        {
            switch (ClipMode)
            {
                case VoxelPreviewClipMode.Below:
                    clipMaxZ = _clipZ;
                    break;
                case VoxelPreviewClipMode.Above:
                    clipMinZ = _clipZ;
                    break;
                case VoxelPreviewClipMode.Slab:
                    clipMinZ = _clipZ - SlabThickness;
                    clipMaxZ = _clipZ;
                    break;
            }
        }

        // Test cap quad plane if clipped and cap exists
        if (ClipToLayer && _hasCapData && Math.Abs(rayDirection.Z) > 1e-6f)
        {
            var tCap = (_clipZ - rayOrigin.Z) / rayDirection.Z;
            if (tCap > 0 && tCap < closestT)
            {
                var pCap = rayOrigin + rayDirection * tCap;
                if (pCap.X >= _capMinX && pCap.X <= _capMaxX && pCap.Y >= _capMinY && pCap.Y <= _capMaxY)
                {
                    var isCut = false;
                    if (CutawayAxis == VoxelPreviewCutawayAxis.X)
                        isCut = CutawayInvert ? (pCap.X < CutawayPosition) : (pCap.X > CutawayPosition);
                    else if (CutawayAxis == VoxelPreviewCutawayAxis.Y)
                        isCut = CutawayInvert ? (pCap.Y < CutawayPosition) : (pCap.Y > CutawayPosition);

                    if (!isCut)
                    {
                        closestT = tCap;
                        hitPoint = pCap;
                        hasHit = true;
                    }
                }
            }
        }

        var filterByClip = ClipToLayer && !GhostClippedModel;

        for (var i = 0; i < indices.Length; i += 3)
        {
            var p0 = vertices[(int)indices[i]].Position;
            var p1 = vertices[(int)indices[i + 1]].Position;
            var p2 = vertices[(int)indices[i + 2]].Position;

            if (filterByClip)
            {
                if (p0.Z < clipMinZ && p1.Z < clipMinZ && p2.Z < clipMinZ) continue;
                if (p0.Z > clipMaxZ && p1.Z > clipMaxZ && p2.Z > clipMaxZ) continue;
            }

            if (RayIntersectsTriangle(rayOrigin, rayDirection, p0, p1, p2, out var t) && t < closestT)
            {
                var candidatePoint = rayOrigin + rayDirection * t;
                if (CutawayAxis == VoxelPreviewCutawayAxis.X)
                {
                    if (CutawayInvert ? (candidatePoint.X < CutawayPosition) : (candidatePoint.X > CutawayPosition))
                        continue;
                }
                else if (CutawayAxis == VoxelPreviewCutawayAxis.Y)
                {
                    if (CutawayInvert ? (candidatePoint.Y < CutawayPosition) : (candidatePoint.Y > CutawayPosition))
                        continue;
                }

                closestT = t;
                hitPoint = candidatePoint;
                hasHit = true;
            }
        }

        return hasHit;
    }

    private static bool RayIntersectsTriangle(
        Vector3 rayOrigin, Vector3 rayDirection,
        Vector3 v0, Vector3 v1, Vector3 v2,
        out float distance)
    {
        distance = 0;
        var edge1 = v1 - v0;
        var edge2 = v2 - v0;
        var h = Vector3.Cross(rayDirection, edge2);
        var a = Vector3.Dot(edge1, h);

        if (a > -1e-6f && a < 1e-6f) return false;

        var f = 1.0f / a;
        var s = rayOrigin - v0;
        var u = f * Vector3.Dot(s, h);
        if (u < 0.0f || u > 1.0f) return false;

        var q = Vector3.Cross(s, edge1);
        var v = f * Vector3.Dot(rayDirection, q);
        if (v < 0.0f || u + v > 1.0f) return false;

        var t = f * Vector3.Dot(edge2, q);
        if (t > 1e-4f)
        {
            distance = t;
            return true;
        }

        return false;
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

    private void CameraAnimationTimerOnTick(object? sender, EventArgs e)
    {
        var progress = Math.Clamp(
            Stopwatch.GetElapsedTime(_cameraAnimationStartTimestamp).TotalSeconds / CameraAnimationSeconds, 0, 1);
        var easedProgress = (float)(progress * progress * (3 - 2 * progress));
        _cameraYaw = NormalizeAngle(_cameraAnimationStartYaw + _cameraAnimationYawDelta * easedProgress);
        _cameraPitch = float.Lerp(_cameraAnimationStartPitch, _cameraAnimationTargetPitch, easedProgress);
        _cameraRoll = NormalizeAngle(_cameraAnimationStartRoll + _cameraAnimationRollDelta * easedProgress);
        CameraChanged();
        if (progress >= 1) _cameraAnimationTimer.Stop();
    }

    private void CameraChanged()
    {
        NotifyCameraOrientationChanged();
        RequestNextFrameRendering();
    }

    private void NotifyCameraOrientationChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            CameraOrientationChanged?.Invoke(_cameraYaw, _cameraPitch, _cameraRoll);
        }
        else
        {
            var yaw = _cameraYaw;
            var pitch = _cameraPitch;
            var roll = _cameraRoll;
            Dispatcher.UIThread.Post(() => CameraOrientationChanged?.Invoke(yaw, pitch, roll));
        }
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
        if (_pendingCapData is not null)
        {
            ArrayPool<byte>.Shared.Return(_pendingCapData);
            _pendingCapData = null;
        }
        if (_capVertexArray != 0) _gl.DeleteVertexArray(_capVertexArray);
        if (_capVertexBuffer != 0) _gl.DeleteBuffer(_capVertexBuffer);
        if (_capIndexBuffer != 0) _gl.DeleteBuffer(_capIndexBuffer);
        if (_capTexture != 0) _gl.DeleteTexture(_capTexture);
        if (_capShaderProgram != 0) _gl.DeleteProgram(_capShaderProgram);
        _capVertexArray = 0;
        _capVertexBuffer = 0;
        _capIndexBuffer = 0;
        _capTexture = 0;
        _capShaderProgram = 0;
        _capTextureWidth = 0;
        _capTextureHeight = 0;
        _hasCapData = false;

        if (_gridVertexArray != 0) _gl.DeleteVertexArray(_gridVertexArray);
        if (_gridVertexBuffer != 0) _gl.DeleteBuffer(_gridVertexBuffer);
        _gridVertexArray = 0;
        _gridVertexBuffer = 0;
        _gridVertexCount = 0;

        if (_focusBoxVertexArray != 0) _gl.DeleteVertexArray(_focusBoxVertexArray);
        if (_focusBoxVertexBuffer != 0) _gl.DeleteBuffer(_focusBoxVertexBuffer);
        if (_measureVertexArray != 0) _gl.DeleteVertexArray(_measureVertexArray);
        if (_measureVertexBuffer != 0) _gl.DeleteBuffer(_measureVertexBuffer);
        if (_boundingBoxVertexArray != 0) _gl.DeleteVertexArray(_boundingBoxVertexArray);
        if (_boundingBoxVertexBuffer != 0) _gl.DeleteBuffer(_boundingBoxVertexBuffer);
        _boundingBoxVertexArray = 0;
        _boundingBoxVertexBuffer = 0;
        if (_comVertexArray != 0) _gl.DeleteVertexArray(_comVertexArray);
        if (_comVertexBuffer != 0) _gl.DeleteBuffer(_comVertexBuffer);
        _comVertexArray = 0;
        _comVertexBuffer = 0;
        _comVertexCount = 0;
        _focusBoxVertexArray = 0;
        _focusBoxVertexBuffer = 0;
        _hasFocusedBox = false;

        if (_peelTexture != 0) _gl.DeleteTexture(_peelTexture);
        _peelTexture = 0;
        if (_cavityVertexArray != 0) _gl.DeleteVertexArray(_cavityVertexArray);
        if (_cavityVertexBuffer != 0) _gl.DeleteBuffer(_cavityVertexBuffer);
        _cavityVertexArray = 0;
        _cavityVertexBuffer = 0;
        _uploadedCavityVertexCount = 0;

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
        CancelCameraAnimation();
        Focus();
        _capturedPointer = e.Pointer;
        _lastPointerPosition = e.GetPosition(this);
        _pointerDownPosition = _lastPointerPosition;
        _isDragging = false;
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

        if (_pointerDownPosition is { } downPos)
        {
            var moveDist = current - downPos;
            if (moveDist.X * moveDist.X + moveDist.Y * moveDist.Y > 25.0)
            {
                _isDragging = true;
            }
        }

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
            if (!_isDragging && e.InitialPressMouseButton == MouseButton.Left)
            {
                var clickPos = e.GetPosition(this);
                if (TryPickModel(clickPos, out var hitPoint))
                {
                    if (IsMeasureMode)
                    {
                        if (_measurePoint1 is null || _measurePoint2 is not null)
                        {
                            _measurePoint1 = hitPoint;
                            _measurePoint2 = null;
                        }
                        else
                        {
                            _measurePoint2 = hitPoint;
                        }
                        _needsMeasureUpload = true;
                        UpdateMeasureText();
                        RequestNextFrameRendering();
                    }
                    else
                    {
                        ModelPointClicked?.Invoke(hitPoint);
                    }
                }
            }

            e.Pointer.Capture(null);
            _capturedPointer = null;
            _lastPointerPosition = null;
            _pointerDownPosition = null;
            _isDragging = false;
        }

        e.Handled = true;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        IsTurntableActive = false;
        StopTurntable();
        CancelCameraAnimation();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _capturedPointer = null;
        _lastPointerPosition = null;
        _pointerDownPosition = null;
        _isDragging = false;
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

        if ((e.KeyModifiers & KeyModifiers.Control) != 0 && e.Key == Key.C)
        {
            SnapshotToClipboardRequested?.Invoke();
            return true;
        }

        if ((e.KeyModifiers & KeyModifiers.Shift) != 0 && e.Key == Key.Space)
        {
            App.MainWindow.TogglePrintSimulation();
            return true;
        }

        if ((e.KeyModifiers & KeyModifiers.Shift) != 0 && e.Key == Key.C)
        {
            App.MainWindow.Layer3DClipMode = App.MainWindow.Layer3DClipMode == VoxelPreviewClipMode.Below
                ? VoxelPreviewClipMode.Above
                : VoxelPreviewClipMode.Below;
            return true;
        }

        if ((e.KeyModifiers & KeyModifiers.Shift) != 0 && e.Key == Key.X)
        {
            CutawayAxis = CutawayAxis switch
            {
                VoxelPreviewCutawayAxis.Off => VoxelPreviewCutawayAxis.X,
                VoxelPreviewCutawayAxis.X => VoxelPreviewCutawayAxis.Y,
                _ => VoxelPreviewCutawayAxis.Off
            };
            UserSettings.Instance.Layer3DPreview.CutawayAxis = CutawayAxis;
            return true;
        }

        if (e.KeyModifiers != KeyModifiers.None) return false;
        switch (e.Key)
        {
            case Key.F12:
                SnapshotToFileRequested?.Invoke();
                return true;
            case Key.I:
                ShowLayerIssues = !ShowLayerIssues;
                UserSettings.Instance.Layer3DPreview.ShowLayerIssues = ShowLayerIssues;
                return true;
            case Key.V:
                ShowModelStats = !ShowModelStats;
                UserSettings.Instance.Layer3DPreview.ShowModelStats = ShowModelStats;
                return true;
            case Key.K:
                ShowCenterOfMass = !ShowCenterOfMass;
                UserSettings.Instance.Layer3DPreview.ShowCenterOfMass = ShowCenterOfMass;
                return true;
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
            case Key.F:
                FitToView();
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
            case Key.Q:
                App.MainWindow.GoPreviousLayer();
                return true;
            case Key.E:
                App.MainWindow.GoNextLayer();
                return true;
            case Key.S:
                App.MainWindow.Layer3DRenderMode = VoxelPreviewRenderMode.Solid;
                return true;
            case Key.X:
                App.MainWindow.Layer3DRenderMode = VoxelPreviewRenderMode.XRay;
                return true;
            case Key.W:
                App.MainWindow.Layer3DRenderMode = VoxelPreviewRenderMode.Wireframe;
                return true;
            case Key.C:
                App.MainWindow.Layer3DClipToCurrentLayer = !App.MainWindow.Layer3DClipToCurrentLayer;
                return true;
            case Key.P:
                ProjectionToggleRequested?.Invoke();
                return true;
            case Key.G:
                ShowBuildPlateGrid = !ShowBuildPlateGrid;
                UserSettings.Instance.Layer3DPreview.ShowBuildPlateGrid = ShowBuildPlateGrid;
                return true;
            case Key.H:
                var nextColor = ColorMode switch
                {
                    VoxelPreviewColorMode.Solid => VoxelPreviewColorMode.OverhangHeatmap,
                    VoxelPreviewColorMode.OverhangHeatmap => VoxelPreviewColorMode.LayerZones,
                    VoxelPreviewColorMode.LayerZones => VoxelPreviewColorMode.PeelForceRisk,
                    VoxelPreviewColorMode.PeelForceRisk => VoxelPreviewColorMode.BedAdhesion,
                    _ => VoxelPreviewColorMode.Solid
                };
                App.MainWindow.Layer3DColorMode = nextColor;
                return true;
            case Key.A:
                ShowPeelCurve = !ShowPeelCurve;
                UserSettings.Instance.Layer3DPreview.ShowPeelCurve = ShowPeelCurve;
                return true;
            case Key.O:
                GhostClippedModel = !GhostClippedModel;
                UserSettings.Instance.Layer3DPreview.GhostClippedModel = GhostClippedModel;
                return true;

            case Key.L:
                var nextLighting = LightingMode switch
                {
                    VoxelPreviewLightingMode.Camera => VoxelPreviewLightingMode.Studio,
                    VoxelPreviewLightingMode.Studio => VoxelPreviewLightingMode.Flat,
                    _ => VoxelPreviewLightingMode.Camera
                };
                App.MainWindow.Layer3DLightingMode = nextLighting;
                return true;
            case Key.B:
                ShowBoundingBox = !ShowBoundingBox;
                UserSettings.Instance.Layer3DPreview.ShowBoundingBox = ShowBoundingBox;
                return true;
            case Key.T or Key.Space:
                IsTurntableActive = !IsTurntableActive;
                return true;
            case Key.M:
                IsMeasureMode = !IsMeasureMode;
                UserSettings.Instance.Layer3DPreview.ShowMeasure = IsMeasureMode;
                return true;
            case Key.Escape when IsMeasureMode:
                ClearMeasure();
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

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct CapVertex(Vector3 position, Vector2 texCoord)
    {
        public readonly Vector3 Position = position;
        public readonly Vector2 TexCoord = texCoord;
    }
}
