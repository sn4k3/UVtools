/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.MeshFormats;
using UVtools.Core.Objects;
using UVtools.Core.Voxel;
using UVtools.UI.Controls;
using UVtools.UI.Extensions;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using EmguExtensions;
using Color = Avalonia.Media.Color;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;

namespace UVtools.UI;

public partial class MainWindow
{
    private bool _isLayer3DBuilding;
    private readonly object _layer3DCapLock = new();
    private bool _isLayer3DCapBuilding;
    private Layer? _layer3DCapTargetLayer;
    private VoxelPreviewMesh? _layer3DCapTargetMesh;
    private int _layer3DCapTargetGeneration;
    private int _layer3DAppliedCapGeneration;
    private int _layer3DIssueBuildGeneration;
    private float[]? _layerAreas;
    private float[]? _layerLiftSpeeds;
    private float[]? _layerLiftSpeeds2;
    private float[]? _layerLiftHeights;
    private float[]? _layerLiftHeights2;
    private List<int>? _peelSpikes;
    private float _maxLayerArea;
    private int _peakLayerIndex;
    private readonly List<MainIssue> _suctionCupIssues = new();
    private bool _hasSuctionCups;
    private string _suctionCupSummary = string.Empty;
    private readonly List<MainIssue> _all3DIssues = new();
    private int _current3DIssueIndex = -1;
    private bool _has3DIssues;
    private string _current3DIssueSummary = string.Empty;
    private string _current3DIssueCounterText = "0 / 0";
    private string _current3DIssueIcon = "AlertCircle";
    private string _current3DIssueBorderColor = "#00D2FF";
    private bool _canRepairCurrent3DIssue;

    private DispatcherTimer? _printSimulationTimer;
    private bool _isPrintSimulationPlaying;
    private int _printSimulationSpeed = 1;

    public bool Has3DIssues => _has3DIssues;
    public string Current3DIssueSummary => _current3DIssueSummary;
    public string Current3DIssueCounterText => _current3DIssueCounterText;
    public string Current3DIssueIcon => _current3DIssueIcon;
    public string Current3DIssueBorderColor => _current3DIssueBorderColor;
    public bool CanRepairCurrent3DIssue => _canRepairCurrent3DIssue;

    public bool IsPrintSimulationPlaying
    {
        get => _isPrintSimulationPlaying;
        set
        {
            if (_isPrintSimulationPlaying == value) return;
            _isPrintSimulationPlaying = value;
            RaisePropertyChanged(nameof(IsPrintSimulationPlaying));
            RaisePropertyChanged(nameof(PrintSimulationPlayIcon));
        }
    }

    public string PrintSimulationPlayIcon => _isPrintSimulationPlaying ? "Pause" : "Play";

    public int PrintSimulationSpeed
    {
        get => _printSimulationSpeed;
        set
        {
            if (_printSimulationSpeed == value) return;
            _printSimulationSpeed = value;
            RaisePropertyChanged(nameof(PrintSimulationSpeed));
            RaisePropertyChanged(nameof(PrintSimulationSpeedText));
        }
    }

    public string PrintSimulationSpeedText => $"{_printSimulationSpeed}x";

    /// <summary>Elapsed print time estimate at the current layer, over the total estimated print time.</summary>
    public string PrintSimulationElapsedTimeText =>
        SlicerFile is not null && SlicerFile.ContainsLayer(ActualLayer)
            ? $"{SlicerFile[ActualLayer].StartTimeString} / {SlicerFile.PrintTimeString}"
            : "--:--:-- / --:--:--";

    public bool IsCutawayActive => Settings.Layer3DPreview.CutawayAxis != VoxelPreviewCutawayAxis.Off;
    public bool IsCutawayX => Settings.Layer3DPreview.CutawayAxis == VoxelPreviewCutawayAxis.X;
    public bool IsCutawayY => Settings.Layer3DPreview.CutawayAxis == VoxelPreviewCutawayAxis.Y;
    public bool IsCutawayOff => Settings.Layer3DPreview.CutawayAxis == VoxelPreviewCutawayAxis.Off;

    public float[]? LayerAreas => _layerAreas;
    public float[]? LayerLiftSpeeds => _layerLiftSpeeds;
    public float[]? LayerLiftSpeeds2 => _layerLiftSpeeds2;
    public float[]? LayerLiftHeights => _layerLiftHeights;
    public float[]? LayerLiftHeights2 => _layerLiftHeights2;
    public List<int>? PeelSpikes => _peelSpikes;
    public float MaxLayerArea => _maxLayerArea;
    public int PeakLayerIndex => _peakLayerIndex;
    public bool HasSuctionCups => _hasSuctionCups;
    public string SuctionCupSummary => _suctionCupSummary;

    private VoxelPreviewIssueMesh? _layer3DIssueMesh;
    private string? _layer3DIssueOverlayError;
    private VoxelPreviewMesh? _layer3DMesh;
    private int _layerPreviewTabIndex;
    private string? _layer3DRendererError;

    public static VoxelPreviewQuality[] Layer3DQualityOptions { get; } = Enum.GetValues<VoxelPreviewQuality>();

    public int LayerPreviewTabIndex
    {
        get => _layerPreviewTabIndex;
        set
        {
            if (!RaiseAndSetIfChanged(ref _layerPreviewTabIndex, value)) return;
            this.RaisePropertyChanged(nameof(IsLayerPreviewVisible));
            this.RaisePropertyChanged(nameof(Is3DPreviewVisible));
            this.RaisePropertyChanged(nameof(IsDualPreviewTabActive));
            this.RaisePropertyChanged(nameof(IsLayerPreviewTabActive));
            this.RaisePropertyChanged(nameof(Is3DPreviewTabActive));
            this.RaisePropertyChanged(nameof(IsDualPreviewTabSelected));
            this.RaisePropertyChanged(nameof(LayerPreview2DWidth));
            this.RaisePropertyChanged(nameof(LayerPreview3DWidth));
            InvalidateLayer3DPreviewStatus();

            if (value is 0 or 2)
            {
                ShowLayer();
            }

            if (value is 1 or 2)
            {
                if (_layerAreas is null)
                {
                    UpdateLayerAreas();
                    UpdateSuctionCupStatus();
                }

                /* Focus the viewport so that the camera shortcuts work without clicking into it first. */
                Dispatcher.UIThread.Post(() => LayerModel3DView.Focus());

                if (_layer3DMesh is null && !_isLayer3DBuilding && string.IsNullOrEmpty(_layer3DRendererError))
                {
                    Dispatcher.UIThread.InvokeAsync(RebuildLayer3DPreview);
                }
                else if (Layer3DClipToCurrentLayer && _layer3DMesh is not null)
                {
                    UpdateLayer3DClip();
                }
            }
        }
    }

    public bool IsLayerPreviewVisible => LayerPreviewTabIndex is 0 or 2;
    public bool Is3DPreviewVisible => LayerPreviewTabIndex is 1 or 2;
    public bool IsDualPreviewTabActive => LayerPreviewTabIndex == 2;

    public bool IsLayerPreviewTabActive
    {
        get => LayerPreviewTabIndex == 0;
        set { if (value) LayerPreviewTabIndex = 0; }
    }

    public bool Is3DPreviewTabActive
    {
        get => LayerPreviewTabIndex == 1;
        set { if (value) LayerPreviewTabIndex = 1; }
    }

    public bool IsDualPreviewTabSelected
    {
        get => LayerPreviewTabIndex == 2;
        set { if (value) LayerPreviewTabIndex = 2; }
    }

    public GridLength LayerPreview2DWidth => LayerPreviewTabIndex == 1 ? new GridLength(0) : new GridLength(1, GridUnitType.Star);
    public GridLength LayerPreview3DWidth => LayerPreviewTabIndex == 0 ? new GridLength(0) : new GridLength(1, GridUnitType.Star);

    public bool IsLayer3DBuilding
    {
        get => _isLayer3DBuilding;
        private set
        {
            if (!RaiseAndSetIfChanged(ref _isLayer3DBuilding, value)) return;
            InvalidateLayer3DPreviewStatus();
        }
    }

    public bool Layer3DClipToCurrentLayer
    {
        get => Settings.Layer3DPreview.ClipToCurrentLayer;
        set
        {
            if (Settings.Layer3DPreview.ClipToCurrentLayer == value) return;
            Settings.Layer3DPreview.ClipToCurrentLayer = value;
            RaisePropertyChanged();
            LayerModel3DView.ClipToLayer = value;
            UpdateLayer3DClip();
        }
    }

    public static ValueDescription[] Layer3DRenderModeOptions { get; } =
        EnumExtensions.GetAllValuesAndDescriptions(typeof(VoxelPreviewRenderMode));

    public ValueDescription? SelectedLayer3DRenderMode
    {
        get => Layer3DRenderModeOptions.FirstOrDefault(vd => Equals(vd.Value, Settings.Layer3DPreview.RenderMode))
               ?? Layer3DRenderModeOptions[0];
        set
        {
            if (value?.Value is VoxelPreviewRenderMode mode)
            {
                Layer3DRenderMode = mode;
            }
        }
    }

    public VoxelPreviewRenderMode Layer3DRenderMode
    {
        get => Settings.Layer3DPreview.RenderMode;
        set
        {
            if (Settings.Layer3DPreview.RenderMode == value && LayerModel3DView.RenderMode == value) return;
            Settings.Layer3DPreview.RenderMode = value;
            LayerModel3DView.RenderMode = value;
            RaisePropertyChanged(nameof(Layer3DRenderMode));
            RaisePropertyChanged(nameof(SelectedLayer3DRenderMode));
        }
    }

    public static ValueDescription[] Layer3DLightingModeOptions { get; } =
        EnumExtensions.GetAllValuesAndDescriptions(typeof(VoxelPreviewLightingMode));

    public ValueDescription? SelectedLayer3DLightingMode
    {
        get => Layer3DLightingModeOptions.FirstOrDefault(vd => Equals(vd.Value, Settings.Layer3DPreview.LightingMode))
               ?? Layer3DLightingModeOptions[0];
        set
        {
            if (value?.Value is VoxelPreviewLightingMode mode)
            {
                Layer3DLightingMode = mode;
            }
        }
    }

    public VoxelPreviewLightingMode Layer3DLightingMode
    {
        get => Settings.Layer3DPreview.LightingMode;
        set
        {
            if (Settings.Layer3DPreview.LightingMode == value && LayerModel3DView.LightingMode == value) return;
            Settings.Layer3DPreview.LightingMode = value;
            LayerModel3DView.LightingMode = value;
            RaisePropertyChanged(nameof(Layer3DLightingMode));
            RaisePropertyChanged(nameof(SelectedLayer3DLightingMode));
        }
    }

    public static ValueDescription[] Layer3DColorModeOptions { get; } =
        EnumExtensions.GetAllValuesAndDescriptions(typeof(VoxelPreviewColorMode));

    public ValueDescription? SelectedLayer3DColorMode
    {
        get => Layer3DColorModeOptions.FirstOrDefault(vd => Equals(vd.Value, Settings.Layer3DPreview.ColorMode))
               ?? Layer3DColorModeOptions[0];
        set
        {
            if (value?.Value is VoxelPreviewColorMode mode)
            {
                Layer3DColorMode = mode;
            }
        }
    }

    public VoxelPreviewColorMode Layer3DColorMode
    {
        get => Settings.Layer3DPreview.ColorMode;
        set
        {
            if (Settings.Layer3DPreview.ColorMode == value && LayerModel3DView.ColorMode == value) return;
            Settings.Layer3DPreview.ColorMode = value;
            LayerModel3DView.ColorMode = value;
            RaisePropertyChanged(nameof(Layer3DColorMode));
            RaisePropertyChanged(nameof(SelectedLayer3DColorMode));
        }
    }

    public static ValueDescription[] Layer3DClipModeOptions { get; } =
        EnumExtensions.GetAllValuesAndDescriptions(typeof(VoxelPreviewClipMode));

    public ValueDescription? SelectedLayer3DClipMode
    {
        get => Layer3DClipModeOptions.FirstOrDefault(vd => Equals(vd.Value, Settings.Layer3DPreview.ClipMode))
               ?? Layer3DClipModeOptions[0];
        set
        {
            if (value?.Value is VoxelPreviewClipMode mode)
            {
                Layer3DClipMode = mode;
            }
        }
    }

    public VoxelPreviewClipMode Layer3DClipMode
    {
        get => Settings.Layer3DPreview.ClipMode;
        set
        {
            if (Settings.Layer3DPreview.ClipMode == value && LayerModel3DView.ClipMode == value) return;
            Settings.Layer3DPreview.ClipMode = value;
            LayerModel3DView.ClipMode = value;
            RaisePropertyChanged(nameof(Layer3DClipMode));
            RaisePropertyChanged(nameof(SelectedLayer3DClipMode));
            UpdateLayer3DClip();
        }
    }

    public VoxelPreviewQuality SelectedLayer3DQuality
    {
        get => Settings.Layer3DPreview.Quality;
        set
        {
            if (Settings.Layer3DPreview.Quality == value) return;
            Settings.Layer3DPreview.Quality = value;
            RaisePropertyChanged();
            InvalidateLayer3DPreviewStatus();
        }
    }

    public bool IsLayer3DPreviewStale =>
        _layer3DMesh is not null && SlicerFile is not null &&
        (!_layer3DMesh.IsCurrentFor(SlicerFile) ||
         _layer3DMesh.Quality != SelectedLayer3DQuality);

    public bool IsLayer3DRendererAvailable => string.IsNullOrEmpty(_layer3DRendererError);
    public bool HasLayer3DMesh => _layer3DMesh is not null;
    public bool CanBuildLayer3DPreview => SlicerFile is not null && !IsLayer3DBuilding && IsLayer3DRendererAvailable;
    public bool ShowLayer3DStatusOverlay => !HasLayer3DMesh || !IsLayer3DRendererAvailable;
    public string Layer3DBuildButtonText => _layer3DMesh is null ? "Build" : "Rebuild";

    public string Layer3DStatus
    {
        get
        {
            if (!string.IsNullOrEmpty(_layer3DRendererError)) return _layer3DRendererError;
            if (IsLayer3DBuilding) return "Generating the slice-derived 3D model…";
            if (_layer3DMesh is null) return "Select this tab or press Build model to generate a cached 3D preview.";

            var stale = IsLayer3DPreviewStale ? " • stale — rebuild to include current slices" : string.Empty;
            var issues = Settings.Layer3DPreview.ShowLayerIssues && _layer3DIssueMesh is { IndexCount: > 0 }
                ? $" • {_layer3DIssueMesh.TriangleCount:N0} issue triangles"
                : string.Empty;
            var issueError = string.IsNullOrWhiteSpace(_layer3DIssueOverlayError)
                ? string.Empty
                : $" • issue overlay unavailable: {_layer3DIssueOverlayError}";

            if (Layer3DClipToCurrentLayer && SlicerFile is not null && SlicerFile.ContainsLayer(ActualLayer))
            {
                var curLayer = SlicerFile[ActualLayer];
                var maxZ = _layer3DMesh.MaximumBounds.Z;
                var pct = maxZ > 0 ? (curLayer.PositionZ / maxZ * 100f) : 0f;
                return $"Layer {ActualLayer + 1}/{SlicerFile.LayerCount} • Z: {curLayer.PositionZ:F2}/{maxZ:F2} mm ({pct:F1}%) • {_layer3DMesh.TriangleCount:N0} triangles{issues}{stale}{issueError}";
            }

            return $"{_layer3DMesh.TriangleCount:N0} triangles • detail 1:{_layer3DMesh.SamplingStride} • " +
                   $"built in {_layer3DMesh.BuildDuration.TotalSeconds:F2}s{issues}{stale}{issueError}";
        }
    }

    private void InitLayer3DPreview()
    {
        LayerModel3DView.CameraOrientationChanged += LayerModelOrientationCube.SetCameraOrientation;
        LayerModelOrientationCube.OrbitRequested += LayerModel3DView.Orbit;
        LayerModelOrientationCube.SnapRequested += LayerModel3DView.SnapToDirection;
        LayerModelOrientationCube.HomeRequested += () => LayerModel3DView.ResetCamera();
        LayerModelOrientationCube.RotateRequested += (yawDelta, pitchDelta) => LayerModel3DView.RotateStep(yawDelta, pitchDelta);
        LayerModelOrientationCube.RollRequested += LayerModel3DView.RollStep;
        LayerModelOrientationCube.TurntableToggleRequested += () => LayerModel3DView.IsTurntableActive = !LayerModel3DView.IsTurntableActive;
        LayerModelOrientationCube.ProjectionToggleRequested += () =>
        {
            Settings.Layer3DPreview.UseOthographicProjection = !Settings.Layer3DPreview.UseOthographicProjection;
            RefreshLayer3DPreviewSettings();
        };
        LayerModelOrientationCube.FitToViewRequested += LayerModel3DView.FitToView;
        LayerModelOrientationCube.SetCameraOrientation(LayerModel3DView.CameraYaw, LayerModel3DView.CameraPitch, LayerModel3DView.CameraRoll);
        LayerModel3DView.ModelPointClicked += OnLayer3DModelPointClicked;

        /* The cube takes the focus when clicked, keep the camera shortcuts working from there as well. */
        LayerModelOrientationCube.KeyDown += (_, e) => e.Handled = LayerModel3DView.HandleCameraKey(e);

        LayerModel3DView.ProjectionToggleRequested += () =>
        {
            Settings.Layer3DPreview.UseOthographicProjection = !Settings.Layer3DPreview.UseOthographicProjection;
            RefreshLayer3DPreviewSettings();
        };

        LayerModel3DView.RendererStatusChanged += error =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                _layer3DRendererError = error;
                if (error is not null && IsLayer3DBuilding && Progress.CanCancel)
                {
                    Progress.TokenSource.Cancel();
                }

                InvalidateLayer3DPreviewStatus();
                if (error is null && LayerPreviewTabIndex == 1 && _layer3DMesh is null)
                {
                    Dispatcher.UIThread.InvokeAsync(RebuildLayer3DPreview);
                }
            });
        };

        LayerModel3DView.SnapshotToClipboardRequested += () => Dispatcher.UIThread.InvokeAsync(CopyLayer3DSnapshotToClipboard);
        LayerModel3DView.SnapshotToFileRequested += () => Dispatcher.UIThread.InvokeAsync(SaveLayer3DSnapshotToFile);

        Settings.Layer3DPreview.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Settings.Layer3DPreview.ClipToCurrentLayer))
            {
                LayerModel3DView.ClipToLayer = Settings.Layer3DPreview.ClipToCurrentLayer;
                UpdateLayer3DClip();
                RaisePropertyChanged(nameof(Layer3DClipToCurrentLayer));
            }
            if (e.PropertyName == nameof(Settings.Layer3DPreview.ShowLayerIssues))
            {
                LayerModel3DView.ShowLayerIssues = Settings.Layer3DPreview.ShowLayerIssues;
                InvalidateLayer3DPreviewStatus();
            }
            if (e.PropertyName == nameof(Settings.Layer3DPreview.CutawayAxis))
            {
                if (Settings.Layer3DPreview.CutawayAxis != VoxelPreviewCutawayAxis.Off && Settings.Layer3DPreview.CutawayPosition == 0f)
                {
                    float mid = (LayerModel3DView.CutawayMin + LayerModel3DView.CutawayMax) / 2f;
                    Settings.Layer3DPreview.CutawayPosition = mid;
                    LayerModel3DView.CutawayPosition = mid;
                }
                RaisePropertyChanged(nameof(IsCutawayActive));
                RaisePropertyChanged(nameof(IsCutawayX));
                RaisePropertyChanged(nameof(IsCutawayY));
                RaisePropertyChanged(nameof(IsCutawayOff));
            }
        };

        LayerModel3DView.ClipToLayer = Layer3DClipToCurrentLayer;
        UpdateLayer3DClip();
        RefreshLayer3DPreviewSettings();
    }

    private void RefreshLayer3DPreviewSettings()
    {
        LayerModel3DView.ClipToLayer = Settings.Layer3DPreview.ClipToCurrentLayer;
        RaisePropertyChanged(nameof(Layer3DClipToCurrentLayer));
        LayerModel3DView.VoxelColor = Settings.Layer3DPreview.VoxelBrush;
        var issueColors = new Dictionary<MainIssue.IssueType, Color>();
        foreach (var (type, brush) in GetIssueColors())
        {
            issueColors[type] = brush.Color;
        }

        LayerModel3DView.SetIssueColors(issueColors);
        LayerModel3DView.IsOrthographic = Settings.Layer3DPreview.UseOthographicProjection;
        LayerModel3DView.ColorMode = Settings.Layer3DPreview.ColorMode;
        LayerModel3DView.ClipMode = Settings.Layer3DPreview.ClipMode;
        LayerModel3DView.ShowBuildPlateGrid = Settings.Layer3DPreview.ShowBuildPlateGrid;
        LayerModel3DView.ShowLayerIssues = Settings.Layer3DPreview.ShowLayerIssues;
        LayerModel3DView.GhostClippedModel = Settings.Layer3DPreview.GhostClippedModel;
        LayerModel3DView.SlabThickness = Settings.Layer3DPreview.SlabThicknessMm;
        LayerModel3DView.ShowBoundingBox = Settings.Layer3DPreview.ShowBoundingBox;
        LayerModel3DView.ShowModelStats = Settings.Layer3DPreview.ShowModelStats;
        LayerModel3DView.ShowCenterOfMass = Settings.Layer3DPreview.ShowCenterOfMass;
        LayerModel3DView.ShowPeelCurve = Settings.Layer3DPreview.ShowPeelCurve;
        LayerModel3DView.IsMeasureMode = Settings.Layer3DPreview.ShowMeasure;
        LayerModel3DView.CutawayAxis = Settings.Layer3DPreview.CutawayAxis;
        LayerModel3DView.CutawayPosition = Settings.Layer3DPreview.CutawayPosition;
        LayerModel3DView.CutawayInvert = Settings.Layer3DPreview.CutawayInvert;
        RaisePropertyChanged(nameof(IsCutawayActive));
        RaisePropertyChanged(nameof(IsCutawayX));
        RaisePropertyChanged(nameof(IsCutawayY));
        RaisePropertyChanged(nameof(IsCutawayOff));

        if (SlicerFile is not null)
        {
            LayerModel3DView.PlateWidth = SlicerFile.DisplayWidth;
            LayerModel3DView.PlateHeight = SlicerFile.DisplayHeight;
            LayerModel3DView.PrintHeight = SlicerFile.MachineZ > 0 ? SlicerFile.MachineZ : (SlicerFile.Layers.Length > 0 ? SlicerFile.Layers[^1].PositionZ : 0);
            LayerModel3DView.FirstLayerHeight = SlicerFile.HaveLayers && SlicerFile.LayerCount > 0 ? (SlicerFile[0].LayerHeight > 0 ? SlicerFile[0].LayerHeight : SlicerFile.LayerHeight) : SlicerFile.LayerHeight;

            var bottomLayerCount = SlicerFile.BottomLayerCount;
            var transitionLayerCount = SlicerFile.TransitionLayerCount;
            if (bottomLayerCount > 0 && bottomLayerCount <= SlicerFile.LayerCount)
            {
                LayerModel3DView.BottomLayersHeight = SlicerFile[bottomLayerCount - 1].PositionZ;
            }
            if (transitionLayerCount > 0 && bottomLayerCount + transitionLayerCount <= SlicerFile.LayerCount)
            {
                LayerModel3DView.TransitionLayersHeight = SlicerFile[bottomLayerCount + transitionLayerCount - 1].PositionZ;
            }
        }

        RaisePropertyChanged(nameof(SelectedLayer3DQuality));
        RaisePropertyChanged(nameof(SelectedLayer3DRenderMode));
        RaisePropertyChanged(nameof(Layer3DRenderMode));
        RaisePropertyChanged(nameof(SelectedLayer3DLightingMode));
        RaisePropertyChanged(nameof(Layer3DLightingMode));
        RaisePropertyChanged(nameof(SelectedLayer3DColorMode));
        RaisePropertyChanged(nameof(Layer3DColorMode));
        RaisePropertyChanged(nameof(SelectedLayer3DClipMode));
        RaisePropertyChanged(nameof(Layer3DClipMode));
        InvalidateLayer3DPreviewStatus();
    }

    [RelayCommand]
    public void ToggleLayerIssues()
    {
        Settings.Layer3DPreview.ShowLayerIssues = !Settings.Layer3DPreview.ShowLayerIssues;
        LayerModel3DView.ShowLayerIssues = Settings.Layer3DPreview.ShowLayerIssues;
    }

    [RelayCommand]
    public void ToggleBoundingBox()
    {
        Settings.Layer3DPreview.ShowBoundingBox = !Settings.Layer3DPreview.ShowBoundingBox;
        LayerModel3DView.ShowBoundingBox = Settings.Layer3DPreview.ShowBoundingBox;
    }

    [RelayCommand]
    public void ToggleModelStats()
    {
        Settings.Layer3DPreview.ShowModelStats = !Settings.Layer3DPreview.ShowModelStats;
        LayerModel3DView.ShowModelStats = Settings.Layer3DPreview.ShowModelStats;
    }

    [RelayCommand]
    public void ToggleCenterOfMass()
    {
        Settings.Layer3DPreview.ShowCenterOfMass = !Settings.Layer3DPreview.ShowCenterOfMass;
        LayerModel3DView.ShowCenterOfMass = Settings.Layer3DPreview.ShowCenterOfMass;
    }

    [RelayCommand]
    public void ToggleMeasureMode()
    {
        Settings.Layer3DPreview.ShowMeasure = !Settings.Layer3DPreview.ShowMeasure;
        LayerModel3DView.IsMeasureMode = Settings.Layer3DPreview.ShowMeasure;
    }

    [RelayCommand]
    public void TogglePeelCurve()
    {
        Settings.Layer3DPreview.ShowPeelCurve = !Settings.Layer3DPreview.ShowPeelCurve;
        LayerModel3DView.ShowPeelCurve = Settings.Layer3DPreview.ShowPeelCurve;
    }

    [RelayCommand]
    public void SelectLayer(int layer)
    {
        if (SlicerFile is not null && layer >= 0 && layer < SlicerFile.LayerCount)
        {
            ActualLayer = (uint)layer;
        }
    }



    [RelayCommand]
    public void ToggleCutawayInvert()
    {
        Settings.Layer3DPreview.CutawayInvert = !Settings.Layer3DPreview.CutawayInvert;
    }

    [RelayCommand]
    public void SetCutawayOff() => Settings.Layer3DPreview.CutawayAxis = VoxelPreviewCutawayAxis.Off;

    [RelayCommand]
    public void SetCutawayX() => Settings.Layer3DPreview.CutawayAxis = VoxelPreviewCutawayAxis.X;

    [RelayCommand]
    public void SetCutawayY() => Settings.Layer3DPreview.CutawayAxis = VoxelPreviewCutawayAxis.Y;

    [RelayCommand]
    public async Task RebuildLayer3DPreview()
    {
        if (SlicerFile is null || IsLayer3DBuilding || !IsLayer3DRendererAvailable) return;

        var slicerFile = SlicerFile;
        var options = VoxelPreviewMeshOptions.FromQuality(SelectedLayer3DQuality);
        VoxelPreviewMesh? newMesh = null;
        IsLayer3DBuilding = true;
        IsGUIEnabled = false;
        ShowProgressWindow("Generating 3D preview");

        try
        {
            newMesh = await Task.Run(() => VoxelPreviewMeshBuilder.Build(slicerFile, options, Progress),
                Progress.Token);
        }
        catch (OperationCanceledException)
        {
            // The existing cached model remains valid after cancellation.
        }
        catch (Exception exception)
        {
            await HandleException(exception, "Unable to generate 3D preview");
        }
        finally
        {
            IsGUIEnabled = true;
            IsLayer3DBuilding = false;
        }

        if (newMesh is null) return;
        if (!ReferenceEquals(SlicerFile, slicerFile))
        {
            newMesh.Dispose();
            return;
        }

        var previousMesh = _layer3DMesh;
        _layer3DMesh = newMesh;
        LayerModel3DView.Mesh = newMesh;
        previousMesh?.Dispose();
        UpdateLayerAreas();
        UpdateSuctionCupStatus();
        RefreshLayer3DPreviewSettings();
        await RebuildLayer3DIssueOverlay();
        UpdateLayer3DClip();
        if (_lastFocusedIssue is not null)
        {
            FocusIssueIn3D(_lastFocusedIssue);
        }
        InvalidateLayer3DPreviewStatus();
    }

    private async Task RebuildLayer3DIssueOverlay()
    {
        var slicerFile = SlicerFile;
        var baseMesh = _layer3DMesh;
        var generation = ++_layer3DIssueBuildGeneration;

        if (slicerFile is null || baseMesh is null || !baseMesh.IsCurrentFor(slicerFile) ||
            slicerFile.IssueManager.Count == 0)
        {
            var previous = _layer3DIssueMesh;
            _layer3DIssueMesh = null;
            LayerModel3DView.IssueMesh = null;
            previous?.Dispose();
            _layer3DIssueOverlayError = null;
            InvalidateLayer3DPreviewStatus();
            return;
        }

        VoxelPreviewIssueMesh? newIssueMesh = null;
        try
        {
            var options = VoxelPreviewMeshOptions.FromQuality(baseMesh.Quality);
            newIssueMesh = await Task.Run(() => VoxelPreviewIssueMeshBuilder.Build(slicerFile,
                baseMesh.SamplingStride, options.MaximumTriangleCount));
        }
        catch (Exception exception)
        {
            if (generation != _layer3DIssueBuildGeneration) return;
            var previousMesh = _layer3DIssueMesh;
            _layer3DIssueMesh = null;
            LayerModel3DView.IssueMesh = null;
            previousMesh?.Dispose();
            _layer3DIssueOverlayError = exception.Message;
            InvalidateLayer3DPreviewStatus();
            return;
        }

        if (generation != _layer3DIssueBuildGeneration || !ReferenceEquals(SlicerFile, slicerFile) ||
            !ReferenceEquals(_layer3DMesh, baseMesh) || !newIssueMesh.IsCurrentFor(slicerFile))
        {
            newIssueMesh.Dispose();
            return;
        }

        var previousIssueMesh = _layer3DIssueMesh;
        _layer3DIssueMesh = newIssueMesh;
        LayerModel3DView.IssueMesh = newIssueMesh;
        previousIssueMesh?.Dispose();
        UpdateSuctionCupStatus();
        _layer3DIssueOverlayError = null;
        InvalidateLayer3DPreviewStatus();
    }

    [RelayCommand]
    public void ResetLayer3DCamera()
    {
        LayerModel3DView.ResetCamera();
    }

    [RelayCommand]
    public async Task CopyLayer3DSnapshotToClipboard()
    {
        if (!HasLayer3DMesh) return;

        try
        {
            var bitmap = await LayerModel3DView.CaptureSnapshotAsync();
            if (bitmap is null) return;

            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is not null)
            {
                await clipboard.SetBitmapAsync(bitmap);
                AddLog("3D preview snapshot copied to clipboard.");
            }
        }
        catch (Exception ex)
        {
            await this.MessageBoxError(ex.Message, "Snapshot Error");
        }
    }

    [RelayCommand]
    public async Task SaveLayer3DSnapshotToFile()
    {
        if (!HasLayer3DMesh) return;

        try
        {
            var bitmap = await LayerModel3DView.CaptureSnapshotAsync();
            if (bitmap is null) return;

            var defaultName = SlicerFile is not null
                ? $"{SlicerFile.FilenameNoExt}_3D.png"
                : "UVtools_3D_snapshot.png";
            var dir = SlicerFile?.DirectoryPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

            using var file = await SaveFilePickerAsync(dir, defaultName, AvaloniaStatic.PngFileFilter);
            if (file?.TryGetLocalPath() is not { } filePath) return;

            bitmap.Save(filePath);
            AddLog($"3D preview snapshot saved to {filePath}");
        }
        catch (Exception ex)
        {
            await this.MessageBoxError(ex.Message, "Snapshot Error");
        }
    }

    /// <summary>
    /// Exports the actual, full-resolution layer image at <see cref="ActualLayer"/>, cropped to the model's
    /// bounding rectangle, as a PNG. Unlike the mesh/snapshot exports, this reads straight from the decoded
    /// layer instead of the coarser, sampled voxel mesh, so the cross-section keeps full pixel detail.
    /// </summary>
    [RelayCommand]
    public async Task ExportCrossSectionToPng()
    {
        if (SlicerFile is null || !SlicerFile.ContainsLayer(ActualLayer)) return;

        var bounds = SlicerFile.BoundingRectangle;
        if (bounds.IsEmpty) return;

        var defaultName = !string.IsNullOrEmpty(SlicerFile.FileFullPath)
            ? $"{SlicerFile.FilenameNoExt}_layer{ActualLayer + 1}_cross-section.png"
            : "UVtools_cross-section.png";
        var dir = SlicerFile.DirectoryPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);

        using var file = await SaveFilePickerAsync(dir, defaultName, AvaloniaStatic.PngFileFilter);
        if (file?.TryGetLocalPath() is not { } filePath) return;

        try
        {
            var layerIndex = ActualLayer;
            using var mat = SlicerFile[layerIndex].LayerMat;
            using var roi = mat.Roi(bounds);
            roi.Save(filePath);
            await this.MessageBoxInfo($"Saved cross-section of layer {layerIndex + 1} to {file.Name}.",
                "Cross-Section Exported");
        }
        catch (Exception ex)
        {
            await this.MessageBoxError(ex.Message, "Cross-Section Export Failed");
        }
    }

    [RelayCommand]
    public async Task ExportLayer3DMesh()
    {
        var mesh = _layer3DMesh;
        if (mesh is null || SlicerFile is null) return;

        using var file = await SaveFilePickerAsync(SlicerFile.DirectoryPath,
            $"{SlicerFile.FilenameNoExt}.{STLMeshFile.FileExtension.Extension}", AvaloniaStatic.MeshFileFilter);
        if (file?.TryGetLocalPath() is not { } filePath) return;

        var fileExtension = MeshFile.FindFileExtension(filePath);
        if (fileExtension is null)
        {
            await this.MessageBoxError("The chosen file extension is not a supported mesh format.",
                "Unable to export the 3D mesh");
            return;
        }

        /* Vertices/Indices are spans and can't cross the Task.Run boundary below, so copy them once here
         * on the UI thread before doing the (fast, but still worth offloading) triangle write in the background. */
        var vertices = mesh.Vertices.ToArray();
        var indices = mesh.Indices.ToArray();
        var tmpFile = $"{filePath}.tmp";

        IsGUIEnabled = false;
        try
        {
            await Task.Run(() =>
            {
                using var meshFile = fileExtension.FileFormatType.CreateInstance<MeshFile>(tmpFile, FileMode.Create, MeshFile.MeshFileFormat.BINARY, SlicerFile)
                    ?? throw new InvalidOperationException(
                        $"Unable to create mesh exporter for '.{fileExtension.Extension}'.");
                meshFile.BeginWrite();
                for (var i = 0; i < indices.Length; i += 3)
                {
                    var v0 = vertices[indices[i]];
                    var v1 = vertices[indices[i + 1]];
                    var v2 = vertices[indices[i + 2]];
                    meshFile.WriteTriangle(v0.Position, v1.Position, v2.Position, v0.Normal);
                }

                meshFile.EndWrite();
            });

            File.Move(tmpFile, filePath, true);
        }
        catch (Exception exception)
        {
            if (File.Exists(tmpFile)) File.Delete(tmpFile);
            await HandleException(exception, "Unable to export the 3D mesh");
            return;
        }
        finally
        {
            IsGUIEnabled = true;
        }

        await this.MessageBoxInfo($"The 3D mesh ({mesh.TriangleCount:N0} triangles) was exported to:\n{filePath}",
            "3D mesh exported");
    }

    private void UpdateLayer3DClip()
    {
        if (SlicerFile is null || !SlicerFile.ContainsLayer(ActualLayer)) return;
        var layer = SlicerFile[ActualLayer];
        LayerModel3DView.ClipZ = layer.PositionZ;
        InvalidateLayer3DPreviewStatus();

        if (!Layer3DClipToCurrentLayer || _layer3DMesh is null)
        {
            lock (_layer3DCapLock)
            {
                _layer3DCapTargetGeneration++;
                _layer3DAppliedCapGeneration = _layer3DCapTargetGeneration;
                _layer3DCapTargetLayer = null;
                _layer3DCapTargetMesh = null;
            }
            LayerModel3DView.ClearCap();
            return;
        }

        var slicerFile = SlicerFile;
        var mesh = _layer3DMesh;

        lock (_layer3DCapLock)
        {
            _layer3DCapTargetLayer = layer;
            _layer3DCapTargetMesh = mesh;
            _layer3DCapTargetGeneration++;

            if (_isLayer3DCapBuilding)
            {
                return;
            }

            _isLayer3DCapBuilding = true;
        }

        Task.Run(() =>
        {
            while (true)
            {
                Layer currentLayer;
                VoxelPreviewMesh currentMesh;
                int currentGeneration;

                lock (_layer3DCapLock)
                {
                    if (_layer3DCapTargetLayer is null || _layer3DCapTargetMesh is null)
                    {
                        _isLayer3DCapBuilding = false;
                        break;
                    }

                    currentLayer = _layer3DCapTargetLayer;
                    currentMesh = _layer3DCapTargetMesh;
                    currentGeneration = _layer3DCapTargetGeneration;
                    _layer3DCapTargetLayer = null;
                }

                if (!ReferenceEquals(SlicerFile, slicerFile) || !ReferenceEquals(_layer3DMesh, currentMesh))
                {
                    lock (_layer3DCapLock)
                    {
                        _isLayer3DCapBuilding = false;
                    }
                    break;
                }

                byte[] occupancy;
                try
                {
                    occupancy = VoxelPreviewMeshBuilder.BuildLayerOccupancy(slicerFile, currentLayer, currentMesh);
                }
                catch
                {
                    lock (_layer3DCapLock)
                    {
                        _isLayer3DCapBuilding = false;
                    }
                    break;
                }

                Dispatcher.UIThread.Post(() =>
                {
                    if (!ReferenceEquals(_layer3DMesh, currentMesh) || currentGeneration < _layer3DAppliedCapGeneration)
                    {
                        if (occupancy.Length > 0)
                        {
                            ArrayPool<byte>.Shared.Return(occupancy);
                        }
                        return;
                    }

                    _layer3DAppliedCapGeneration = currentGeneration;

                    if (occupancy.Length == 0)
                    {
                        LayerModel3DView.ClearCap();
                        return;
                    }

                    LayerModel3DView.SetCap(occupancy, currentMesh.GridWidth, currentMesh.GridHeight,
                        currentMesh.BoundsMinX, currentMesh.BoundsMinY, currentMesh.BoundsMaxX, currentMesh.BoundsMaxY);
                });

                lock (_layer3DCapLock)
                {
                    if (_layer3DCapTargetLayer is null)
                    {
                        _isLayer3DCapBuilding = false;
                        break;
                    }
                }
            }
        });
    }

    private void OnLayer3DModelPointClicked(System.Numerics.Vector3 hitPoint)
    {
        if (SlicerFile is null || SlicerFile.LayerCount == 0) return;

        var closestIndex = 0;
        var minDiff = float.MaxValue;
        for (var i = 0; i < SlicerFile.LayerCount; i++)
        {
            var diff = Math.Abs(SlicerFile[i].PositionZ - hitPoint.Z);
            if (diff < minDiff)
            {
                minDiff = diff;
                closestIndex = i;
            }
        }

        ActualLayer = (uint)closestIndex;
    }

    private Issue? _lastFocusedIssue;

    private void Update3DCavityMarkers()
    {
        if (SlicerFile is null || _suctionCupIssues.Count == 0)
        {
            LayerModel3DView.SetCavityMarkers(null);
            return;
        }

        var bounds = _layer3DMesh?.ModelBounds ?? SlicerFile.BoundingRectangle;
        if (bounds.IsEmpty)
        {
            LayerModel3DView.SetCavityMarkers(null);
            return;
        }

        var pixelSize = SlicerFile.PixelSize;
        var pixelW = _layer3DMesh?.PixelWidth ?? (pixelSize.Width > 0 ? pixelSize.Width : 0.035f);
        var pixelH = _layer3DMesh?.PixelHeight ?? (pixelSize.Height > 0 ? pixelSize.Height : 0.035f);

        var flip = _layer3DMesh?.WorkAroundFlip ?? SlicerFile.DisplayMirror switch
        {
            FlipDirection.None => FlipDirection.Vertically,
            FlipDirection.Horizontally => FlipDirection.Both,
            FlipDirection.Vertically => FlipDirection.None,
            FlipDirection.Both => FlipDirection.Horizontally,
            _ => FlipDirection.None
        };

        var flipH = flip is FlipDirection.Horizontally or FlipDirection.Both;
        var flipV = flip is FlipDirection.Vertically or FlipDirection.Both;

        var markers = new List<LayerModel3DView.CavityMarker3D>(_suctionCupIssues.Count);

        foreach (var mainIssue in _suctionCupIssues)
        {
            var rect = mainIssue.BoundingRectangle;
            if (rect.IsEmpty) continue;

            var minXPixel = flipH ? bounds.X + bounds.Right - rect.Right : rect.Left;
            var maxXPixel = flipH ? bounds.X + bounds.Right - rect.Left : rect.Right;

            var minYPixel = flipV ? bounds.Y + bounds.Bottom - rect.Bottom : rect.Top;
            var maxYPixel = flipV ? bounds.Y + bounds.Bottom - rect.Top : rect.Bottom;

            var minX = Math.Min(minXPixel, maxXPixel) * pixelW;
            var maxX = Math.Max(minXPixel, maxXPixel) * pixelW;
            var minY = Math.Min(minYPixel, maxYPixel) * pixelH;
            var maxY = Math.Max(minYPixel, maxYPixel) * pixelH;

            int startL = (int)Math.Clamp((long)mainIssue.StartLayerIndex, 0L, (long)SlicerFile.LayerCount - 1L);
            int endL = (int)Math.Clamp((long)mainIssue.EndLayerIndex, 0L, (long)SlicerFile.LayerCount - 1L);

            float minZ = SlicerFile[startL].PositionZ;
            float maxZ = SlicerFile[endL].PositionZ + SlicerFile[endL].LayerHeight;

            var padX = Math.Max((maxX - minX) * 0.05f, 0.3f);
            var padY = Math.Max((maxY - minY) * 0.05f, 0.3f);
            var padZ = 0.2f;

            var min = new System.Numerics.Vector3(minX - padX, minY - padY, Math.Max(0f, minZ - padZ));
            var max = new System.Numerics.Vector3(maxX + padX, maxY + padY, maxZ + padZ);

            markers.Add(new(min, max, mainIssue.IsSuctionCup, mainIssue.IsResinTrap));
        }

        LayerModel3DView.SetCavityMarkers(markers);
    }

    public void FocusIssueIn3D(Issue issue)
    {
        _lastFocusedIssue = issue;
        if (issue.Parent is { } parent)
        {
            SetCurrent3DIssue(parent);
        }
        if (SlicerFile is null || !SlicerFile.ContainsLayer(issue.LayerIndex)) return;
        var p = issue.Parent;
        var rect = (p is not null && !p.BoundingRectangle.IsEmpty && p.Count > 1)
            ? p.BoundingRectangle
            : issue.BoundingRectangle;

        if (rect.IsEmpty)
        {
            if (p is not null && !p.BoundingRectangle.IsEmpty)
            {
                rect = p.BoundingRectangle;
            }
            else
            {
                return;
            }
        }

        var bounds = _layer3DMesh?.ModelBounds ?? SlicerFile.BoundingRectangle;
        if (bounds.IsEmpty) return;

        var pixelSize = SlicerFile.PixelSize;
        var pixelW = _layer3DMesh?.PixelWidth ?? (pixelSize.Width > 0 ? pixelSize.Width : 0.035f);
        var pixelH = _layer3DMesh?.PixelHeight ?? (pixelSize.Height > 0 ? pixelSize.Height : 0.035f);

        var flip = _layer3DMesh?.WorkAroundFlip ?? SlicerFile.DisplayMirror switch
        {
            FlipDirection.None => FlipDirection.Vertically,
            FlipDirection.Horizontally => FlipDirection.Both,
            FlipDirection.Vertically => FlipDirection.None,
            FlipDirection.Both => FlipDirection.Horizontally,
            _ => FlipDirection.None
        };

        var flipH = flip is FlipDirection.Horizontally or FlipDirection.Both;
        var flipV = flip is FlipDirection.Vertically or FlipDirection.Both;

        var minXPixel = flipH ? bounds.X + bounds.Right - rect.Right : rect.Left;
        var maxXPixel = flipH ? bounds.X + bounds.Right - rect.Left : rect.Right;

        var minYPixel = flipV ? bounds.Y + bounds.Bottom - rect.Bottom : rect.Top;
        var maxYPixel = flipV ? bounds.Y + bounds.Bottom - rect.Top : rect.Bottom;

        var minX = Math.Min(minXPixel, maxXPixel) * pixelW;
        var maxX = Math.Max(minXPixel, maxXPixel) * pixelW;
        var minY = Math.Min(minYPixel, maxYPixel) * pixelH;
        var maxY = Math.Max(minYPixel, maxYPixel) * pixelH;

        var z = SlicerFile[issue.LayerIndex].PositionZ;
        var layerH = Math.Max(SlicerFile[issue.LayerIndex].LayerHeight, SlicerFile.LayerHeight);

        var padX = Math.Max((maxX - minX) * 0.08f, 0.4f);
        var padY = Math.Max((maxY - minY) * 0.08f, 0.4f);
        var padZ = Math.Max(layerH * 0.5f, 0.2f);

        var minZ = Math.Max(0f, z - layerH - padZ);
        var maxZ = z + padZ;

        if (p is not null && p.Count > 1 && SlicerFile.ContainsLayer(p.StartLayerIndex) && SlicerFile.ContainsLayer(p.EndLayerIndex))
        {
            var pStartZ = SlicerFile[p.StartLayerIndex].PositionZ;
            var pEndZ = SlicerFile[p.EndLayerIndex].PositionZ + SlicerFile[p.EndLayerIndex].LayerHeight;
            minZ = Math.Min(minZ, Math.Max(0f, pStartZ - padZ));
            maxZ = Math.Max(maxZ, pEndZ + padZ);
        }

        var min = new System.Numerics.Vector3(minX - padX, minY - padY, minZ);
        var max = new System.Numerics.Vector3(maxX + padX, maxY + padY, maxZ);

        LayerModel3DView.FocusOnBoundingBox(min, max);
        LayerModel3DView.SetFocusedBoundingBox(min, max);
    }

    private void InvalidateLayer3DPreviewStatus()
    {
        RaisePropertyChanged(nameof(IsLayer3DPreviewStale));
        RaisePropertyChanged(nameof(IsLayer3DRendererAvailable));
        RaisePropertyChanged(nameof(HasLayer3DMesh));
        RaisePropertyChanged(nameof(CanBuildLayer3DPreview));
        RaisePropertyChanged(nameof(ShowLayer3DStatusOverlay));
        RaisePropertyChanged(nameof(Layer3DBuildButtonText));
        RaisePropertyChanged(nameof(Layer3DStatus));
        RaisePropertyChanged(nameof(PrintSimulationElapsedTimeText));
    }

    public void UpdateLayerAreas()
    {
        var file = SlicerFile;
        if (file is null || file.LayerCount == 0)
        {
            _layerAreas = null;
            _layerLiftSpeeds = null;
            _layerLiftSpeeds2 = null;
            _layerLiftHeights = null;
            _layerLiftHeights2 = null;
            _peelSpikes = null;
            _maxLayerArea = 0f;
            _peakLayerIndex = 0;
            LayerModel3DView.SetPeelData(null, null, 0f);
            RaisePropertyChanged(nameof(LayerAreas));
            RaisePropertyChanged(nameof(LayerLiftSpeeds));
            RaisePropertyChanged(nameof(LayerLiftSpeeds2));
            RaisePropertyChanged(nameof(LayerLiftHeights));
            RaisePropertyChanged(nameof(LayerLiftHeights2));
            RaisePropertyChanged(nameof(PeelSpikes));
            RaisePropertyChanged(nameof(MaxLayerArea));
            RaisePropertyChanged(nameof(PeakLayerIndex));
            return;
        }

        int count = (int)file.LayerCount;
        var areas = new float[count];
        var spikes = new List<int>();
        float maxA = 0f;
        int peakIdx = 0;

        var speeds = new float[count];
        var speeds2 = new float[count];
        var heights = new float[count];
        var heights2 = new float[count];

        for (int i = 0; i < count; i++)
        {
            var layer = file.Layers[i];
            float a = (float)layer.GetArea();
            areas[i] = a;
            speeds[i] = layer.LiftSpeed;
            speeds2[i] = layer.LiftSpeed2;
            heights[i] = layer.LiftHeight;
            heights2[i] = layer.LiftHeight2;

            if (a > maxA)
            {
                maxA = a;
                peakIdx = i;
            }

            if (i > 0 && areas[i - 1] > 0.05f)
            {
                float delta = a - areas[i - 1];
                if (delta / areas[i - 1] >= 0.35f && delta > 4.0f)
                {
                    spikes.Add(i);
                }
            }
        }

        _layerAreas = areas;
        _layerLiftSpeeds = speeds;
        _layerLiftSpeeds2 = speeds2;
        _layerLiftHeights = heights;
        _layerLiftHeights2 = heights2;
        _peelSpikes = spikes;
        _maxLayerArea = maxA;
        _peakLayerIndex = peakIdx;

        LayerModel3DView.SetPeelData(areas, spikes, maxA);
        LayerModel3DView.FirstLayerHeight = SlicerFile is not null && SlicerFile.HaveLayers && SlicerFile.LayerCount > 0 ? (SlicerFile[0].LayerHeight > 0 ? SlicerFile[0].LayerHeight : SlicerFile.LayerHeight) : (SlicerFile?.LayerHeight ?? 0.05f);

        RaisePropertyChanged(nameof(LayerAreas));
        RaisePropertyChanged(nameof(LayerLiftSpeeds));
        RaisePropertyChanged(nameof(LayerLiftSpeeds2));
        RaisePropertyChanged(nameof(LayerLiftHeights));
        RaisePropertyChanged(nameof(LayerLiftHeights2));
        RaisePropertyChanged(nameof(PeelSpikes));
        RaisePropertyChanged(nameof(MaxLayerArea));
        RaisePropertyChanged(nameof(PeakLayerIndex));
    }

    public void UpdateSuctionCupStatus()
    {
        _suctionCupIssues.Clear();
        _all3DIssues.Clear();
        var manager = SlicerFile?.IssueManager;
        if (manager is not null)
        {
            foreach (var issue in manager.GetVisible())
            {
                _all3DIssues.Add(issue);
                if (issue.IsSuctionCup || issue.IsResinTrap)
                {
                    _suctionCupIssues.Add(issue);
                }
            }
        }

        _hasSuctionCups = _suctionCupIssues.Count > 0;
        if (_hasSuctionCups)
        {
            _suctionCupSummary = $"{_suctionCupIssues.Count} Cavity/Suction issue{(_suctionCupIssues.Count > 1 ? "s" : "")} detected";
        }
        else
        {
            _suctionCupSummary = "No suction cups detected";
        }

        _has3DIssues = _all3DIssues.Count > 0;
        if (_has3DIssues)
        {
            if (_current3DIssueIndex < 0 || _current3DIssueIndex >= _all3DIssues.Count)
            {
                _current3DIssueIndex = 0;
            }
            UpdateCurrent3DIssueDetails();
        }
        else
        {
            _current3DIssueIndex = -1;
            _current3DIssueSummary = "No issues detected";
            _current3DIssueCounterText = "0 / 0";
            _canRepairCurrent3DIssue = false;
        }

        RaisePropertyChanged(nameof(HasSuctionCups));
        RaisePropertyChanged(nameof(SuctionCupSummary));
        RaisePropertyChanged(nameof(Has3DIssues));
        RaisePropertyChanged(nameof(Current3DIssueSummary));
        RaisePropertyChanged(nameof(Current3DIssueCounterText));
        RaisePropertyChanged(nameof(Current3DIssueIcon));
        RaisePropertyChanged(nameof(Current3DIssueBorderColor));
        RaisePropertyChanged(nameof(CanRepairCurrent3DIssue));
        Update3DCavityMarkers();
    }

    public void SetCurrent3DIssue(MainIssue mainIssue)
    {
        if (_all3DIssues.Count == 0 && SlicerFile?.IssueManager is not null)
        {
            UpdateSuctionCupStatus();
        }

        var idx = _all3DIssues.IndexOf(mainIssue);
        if (idx >= 0)
        {
            _current3DIssueIndex = idx;
            UpdateCurrent3DIssueDetails();
        }
        else
        {
            _current3DIssueCounterText = "-";
            SetCurrent3DIssueDisplay(mainIssue);
        }

        if (!_has3DIssues)
        {
            _has3DIssues = true;
            RaisePropertyChanged(nameof(Has3DIssues));
        }
    }

    private void UpdateCurrent3DIssueDetails()
    {
        if (_current3DIssueIndex < 0 || _current3DIssueIndex >= _all3DIssues.Count) return;
        var issue = _all3DIssues[_current3DIssueIndex];
        _current3DIssueCounterText = $"{_current3DIssueIndex + 1} / {_all3DIssues.Count}";
        SetCurrent3DIssueDisplay(issue);
    }

    private void SetCurrent3DIssueDisplay(MainIssue issue)
    {
        if (issue.IsSuctionCup)
        {
            double ml = issue.Area / 1000.0;
            _current3DIssueSummary = $"Suction Cup • L{issue.LayerInfoString} • ~{ml:F2} mL resin risk";
            _current3DIssueIcon = "AlertDecagram";
            _current3DIssueBorderColor = "#FF9800";
            _canRepairCurrent3DIssue = true;
        }
        else if (issue.IsResinTrap)
        {
            double ml = issue.Area / 1000.0;
            _current3DIssueSummary = $"Resin Trap • L{issue.LayerInfoString} • ~{ml:F2} mL trapped";
            _current3DIssueIcon = "WaterAlert";
            _current3DIssueBorderColor = "#FF5722";
            _canRepairCurrent3DIssue = true;
        }
        else if (issue.IsIsland)
        {
            _current3DIssueSummary = $"Island • L{issue.LayerInfoString} • {issue.Area:F2} mm²";
            _current3DIssueIcon = "Island";
            _current3DIssueBorderColor = "#FF1744";
            _canRepairCurrent3DIssue = false;
        }
        else if (issue.IsOverhang)
        {
            _current3DIssueSummary = $"Overhang • L{issue.LayerInfoString} • {issue.Area:F2} mm²";
            _current3DIssueIcon = "SlopeUphill";
            _current3DIssueBorderColor = "#FFD600";
            _canRepairCurrent3DIssue = false;
        }
        else if (issue.IsEmptyLayer)
        {
            _current3DIssueSummary = $"Empty Layer • L{issue.LayerInfoString}";
            _current3DIssueIcon = "LayersOff";
            _current3DIssueBorderColor = "#9E9E9E";
            _canRepairCurrent3DIssue = false;
        }
        else
        {
            _current3DIssueSummary = $"{issue.Type} • L{issue.LayerInfoString}";
            _current3DIssueIcon = "AlertCircle";
            _current3DIssueBorderColor = "#00D2FF";
            _canRepairCurrent3DIssue = false;
        }

        RaisePropertyChanged(nameof(Current3DIssueSummary));
        RaisePropertyChanged(nameof(Current3DIssueCounterText));
        RaisePropertyChanged(nameof(Current3DIssueIcon));
        RaisePropertyChanged(nameof(Current3DIssueBorderColor));
        RaisePropertyChanged(nameof(CanRepairCurrent3DIssue));
    }

    [RelayCommand]
    public void GoToNext3DIssue()
    {
        if (_all3DIssues.Count == 0) return;
        _current3DIssueIndex = (_current3DIssueIndex + 1) % _all3DIssues.Count;
        NavigateToCurrent3DIssue();
    }

    [RelayCommand]
    public void GoToPrevious3DIssue()
    {
        if (_all3DIssues.Count == 0) return;
        _current3DIssueIndex = (_current3DIssueIndex - 1 + _all3DIssues.Count) % _all3DIssues.Count;
        NavigateToCurrent3DIssue();
    }

    private void NavigateToCurrent3DIssue()
    {
        if (_current3DIssueIndex < 0 || _current3DIssueIndex >= _all3DIssues.Count) return;
        var mainIssue = _all3DIssues[_current3DIssueIndex];
        UpdateCurrent3DIssueDetails();
        IssuesGrid.SelectedItem = mainIssue;
        var issue = mainIssue.Count > 0 ? mainIssue[0] : null;
        if (issue is not null)
        {
            ZoomToIssue(issue, true);
            FocusIssueIn3D(issue);
        }
        else
        {
            ActualLayer = mainIssue.StartLayerIndex;
        }
    }

    [RelayCommand]
    public async Task RepairCurrent3DIssueSolidify()
    {
        if (_current3DIssueIndex < 0 || _current3DIssueIndex >= _all3DIssues.Count) return;
        var mainIssue = _all3DIssues[_current3DIssueIndex];
        await RemoveRepairIssues([mainIssue], promptConfirmation: true, suctionCupDrill: false);
    }

    [RelayCommand]
    public async Task RepairCurrent3DIssueDrill()
    {
        if (_current3DIssueIndex < 0 || _current3DIssueIndex >= _all3DIssues.Count) return;
        var mainIssue = _all3DIssues[_current3DIssueIndex];
        await RemoveRepairIssues([mainIssue], promptConfirmation: true, suctionCupDrill: true);
    }

    [RelayCommand]
    public void SetCutawayAxis(VoxelPreviewCutawayAxis axis)
    {
        Settings.Layer3DPreview.CutawayAxis = axis;
        LayerModel3DView.CutawayAxis = axis;
        if (axis != VoxelPreviewCutawayAxis.Off && Settings.Layer3DPreview.CutawayPosition == 0f)
        {
            float mid = (LayerModel3DView.CutawayMin + LayerModel3DView.CutawayMax) / 2f;
            Settings.Layer3DPreview.CutawayPosition = mid;
            LayerModel3DView.CutawayPosition = mid;
        }
        RaisePropertyChanged(nameof(IsCutawayActive));
        RaisePropertyChanged(nameof(IsCutawayX));
        RaisePropertyChanged(nameof(IsCutawayY));
        RaisePropertyChanged(nameof(IsCutawayOff));
        RaisePropertyChanged(nameof(Settings));
    }

    [RelayCommand]
    public void ResetCutawayPosition()
    {
        float mid = (LayerModel3DView.CutawayMin + LayerModel3DView.CutawayMax) / 2f;
        Settings.Layer3DPreview.CutawayPosition = mid;
        LayerModel3DView.CutawayPosition = mid;
        RaisePropertyChanged(nameof(Settings));
    }

    [RelayCommand]
    public void TogglePrintSimulation()
    {
        if (SlicerFile is null || SlicerFile.LayerCount == 0) return;

        if (IsPrintSimulationPlaying)
        {
            _printSimulationTimer?.Stop();
            IsPrintSimulationPlaying = false;
        }
        else
        {
            if (!Layer3DClipToCurrentLayer)
            {
                Layer3DClipToCurrentLayer = true;
            }

            if (ActualLayer >= SlicerFile.LayerCount - 1)
            {
                ActualLayer = 0;
            }

            if (_printSimulationTimer is null)
            {
                _printSimulationTimer = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(33)
                };
                _printSimulationTimer.Tick += (_, _) =>
                {
                    if (!IsPrintSimulationPlaying || SlicerFile is null || SlicerFile.LayerCount == 0)
                    {
                        _printSimulationTimer?.Stop();
                        IsPrintSimulationPlaying = false;
                        return;
                    }

                    uint next = ActualLayer + (uint)_printSimulationSpeed;
                    if (next >= SlicerFile.LayerCount)
                    {
                        ActualLayer = 0; // loop playback
                    }
                    else
                    {
                        ActualLayer = next;
                    }
                };
            }

            IsPrintSimulationPlaying = true;
            _printSimulationTimer.Start();
        }
    }

    [RelayCommand]
    public void CyclePrintSimulationSpeed()
    {
        PrintSimulationSpeed = PrintSimulationSpeed switch
        {
            1 => 2,
            2 => 5,
            5 => 10,
            10 => 25,
            25 => 50,
            _ => 1
        };
    }




    [RelayCommand]
    public async Task CopyLayer3DModelStatsToClipboard()
    {
        if (!HasLayer3DMesh) return;

        try
        {
            var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
            if (clipboard is null) return;

            var sb = new StringBuilder();
            sb.AppendLine("=== 3D Model Estimates ===");
            if (!string.IsNullOrEmpty(LayerModel3DView.ModelDimensionsText))
            {
                sb.AppendLine($"Dimensions: {LayerModel3DView.ModelDimensionsText}");
            }
            if (!string.IsNullOrEmpty(LayerModel3DView.ModelStatsText))
            {
                sb.AppendLine($"Estimates: {LayerModel3DView.ModelStatsText}");
            }
            if (!string.IsNullOrEmpty(LayerModel3DView.CenterOfMassText))
            {
                sb.AppendLine($"Center of Mass & Contact: {LayerModel3DView.CenterOfMassText}");
            }
            if (SlicerFile is not null)
            {
                sb.AppendLine($"Total Layers: {SlicerFile.LayerCount}");
                sb.AppendLine($"Print Height: {SlicerFile.PrintHeight:F2} mm");
                sb.AppendLine($"Layer Height: {SlicerFile.LayerHeight:F3} mm");
            }

            await clipboard.SetTextAsync(sb.ToString().TrimEnd());
            AddLog("3D model estimates copied to clipboard.");
        }
        catch (Exception ex)
        {
            await this.MessageBoxError(ex.Message, "Copy Error");
        }
    }

    [RelayCommand]
    public async Task ExportMeshToStl(bool clipped = false)
    {
        var mesh = _layer3DMesh;
        if (mesh is null || mesh.VertexCount == 0) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null) return;

        var baseName = !string.IsNullOrEmpty(SlicerFile?.FileFullPath) ? Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath) : "model";
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = clipped ? "Export Clipped 3D Mesh to STL" : "Export 3D Mesh to STL",
            DefaultExtension = "stl",
            SuggestedFileName = baseName + (clipped ? "_clipped.stl" : ".stl"),
            FileTypeChoices = [new FilePickerFileType("STL 3D Model (*.stl)") { Patterns = ["*.stl"] }]
        });

        if (file is null) return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            using var writer = new BinaryWriter(stream);

            var vertices = mesh.Vertices;
            var indices = mesh.Indices;
            float clipZ = LayerModel3DView.ClipZ;
            bool doClipZ = clipped && Layer3DClipToCurrentLayer;
            var clipMode = Settings.Layer3DPreview.ClipMode;
            float slabThick = Settings.Layer3DPreview.SlabThicknessMm;

            var cutAxis = Settings.Layer3DPreview.CutawayAxis;
            float cutPos = Settings.Layer3DPreview.CutawayPosition;
            bool cutInvert = Settings.Layer3DPreview.CutawayInvert;
            bool doCut = clipped && cutAxis != VoxelPreviewCutawayAxis.Off;

            var trianglesToExport = new List<int>(indices.Length / 3);
            for (int i = 0; i < indices.Length; i += 3)
            {
                var p0 = vertices[(int)indices[i]].Position;
                var p1 = vertices[(int)indices[i + 1]].Position;
                var p2 = vertices[(int)indices[i + 2]].Position;

                if (doClipZ)
                {
                    if (clipMode == VoxelPreviewClipMode.Below && (p0.Z > clipZ || p1.Z > clipZ || p2.Z > clipZ))
                        continue;
                    if (clipMode == VoxelPreviewClipMode.Above && (p0.Z < clipZ || p1.Z < clipZ || p2.Z < clipZ))
                        continue;
                    if (clipMode == VoxelPreviewClipMode.Slab &&
                        ((p0.Z > clipZ || p0.Z < clipZ - slabThick) ||
                         (p1.Z > clipZ || p1.Z < clipZ - slabThick) ||
                         (p2.Z > clipZ || p2.Z < clipZ - slabThick)))
                        continue;
                }

                if (doCut)
                {
                    if (cutAxis == VoxelPreviewCutawayAxis.X)
                    {
                        bool d0 = cutInvert ? p0.X < cutPos : p0.X > cutPos;
                        bool d1 = cutInvert ? p1.X < cutPos : p1.X > cutPos;
                        bool d2 = cutInvert ? p2.X < cutPos : p2.X > cutPos;
                        if (d0 || d1 || d2) continue;
                    }
                    else if (cutAxis == VoxelPreviewCutawayAxis.Y)
                    {
                        bool d0 = cutInvert ? p0.Y < cutPos : p0.Y > cutPos;
                        bool d1 = cutInvert ? p1.Y < cutPos : p1.Y > cutPos;
                        bool d2 = cutInvert ? p2.Y < cutPos : p2.Y > cutPos;
                        if (d0 || d1 || d2) continue;
                    }
                }

                trianglesToExport.Add(i);
            }

            byte[] header = new byte[80];
            Encoding.ASCII.GetBytes("Exported by UVtools 3D Voxel Preview", 0, 36, header, 0);
            writer.Write(header);
            writer.Write((uint)trianglesToExport.Count);

            foreach (int idx in trianglesToExport)
            {
                var v0 = vertices[(int)indices[idx]];
                var v1 = vertices[(int)indices[idx + 1]];
                var v2 = vertices[(int)indices[idx + 2]];

                var edge1 = v1.Position - v0.Position;
                var edge2 = v2.Position - v0.Position;
                var normal = Vector3.Normalize(Vector3.Cross(edge1, edge2));
                if (float.IsNaN(normal.X)) normal = v0.Normal;

                writer.Write(normal.X); writer.Write(normal.Y); writer.Write(normal.Z);
                writer.Write(v0.Position.X); writer.Write(v0.Position.Y); writer.Write(v0.Position.Z);
                writer.Write(v1.Position.X); writer.Write(v1.Position.Y); writer.Write(v1.Position.Z);
                writer.Write(v2.Position.X); writer.Write(v2.Position.Y); writer.Write(v2.Position.Z);
                writer.Write((ushort)0);
            }

            await this.MessageBoxInfo($"Saved {trianglesToExport.Count:N0} triangles to {file.Name}.", "3D Model Exported");
        }
        catch (Exception ex)
        {
            await this.MessageBoxError(ex.Message, "Export Failed");
        }
    }

    [RelayCommand]
    public async Task ExportMeshToObj(bool clipped = false)
    {
        var mesh = _layer3DMesh;
        if (mesh is null || mesh.VertexCount == 0) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null) return;

        var baseName = !string.IsNullOrEmpty(SlicerFile?.FileFullPath) ? Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath) : "model";
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = clipped ? "Export Clipped 3D Mesh to OBJ" : "Export 3D Mesh to OBJ",
            DefaultExtension = "obj",
            SuggestedFileName = baseName + (clipped ? "_clipped.obj" : ".obj"),
            FileTypeChoices = [new FilePickerFileType("Wavefront OBJ (*.obj)") { Patterns = ["*.obj"] }]
        });

        if (file is null) return;

        try
        {
            await using var stream = await file.OpenWriteAsync();
            using var writer = new StreamWriter(stream, Encoding.UTF8);

            writer.WriteLine("# UVtools 3D Voxel Preview Export");
            var vertices = mesh.Vertices;
            var indices = mesh.Indices;

            for (int i = 0; i < vertices.Length; i++)
            {
                var p = vertices[i].Position;
                writer.WriteLine(FormattableString.Invariant($"v {p.X:F4} {p.Y:F4} {p.Z:F4}"));
            }

            for (int i = 0; i < vertices.Length; i++)
            {
                var n = vertices[i].Normal;
                writer.WriteLine(FormattableString.Invariant($"vn {n.X:F4} {n.Y:F4} {n.Z:F4}"));
            }

            for (int i = 0; i < indices.Length; i += 3)
            {
                int i0 = (int)indices[i] + 1;
                int i1 = (int)indices[i + 1] + 1;
                int i2 = (int)indices[i + 2] + 1;
                writer.WriteLine($"f {i0}//{i0} {i1}//{i1} {i2}//{i2}");
            }

            await this.MessageBoxInfo($"Saved {mesh.TriangleCount:N0} triangles to {file.Name}.", "3D Model Exported");
        }
        catch (Exception ex)
        {
            await this.MessageBoxError(ex.Message, "Export Failed");
        }
    }

    [RelayCommand]
    public async Task ExportTurntableAnimation()
    {
        if (_layer3DMesh is null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel?.StorageProvider is null) return;

        var baseName = !string.IsNullOrEmpty(SlicerFile?.FileFullPath) ? Path.GetFileNameWithoutExtension(SlicerFile.FileFullPath) : "model";
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export 360° Turntable Animation",
            DefaultExtension = "gif",
            SuggestedFileName = baseName + "_turntable.gif",
            FileTypeChoices = [new FilePickerFileType("Animated GIF (*.gif)") { Patterns = ["*.gif"] }]
        });

        if (file is null) return;

        try
        {
            float originalYaw = LayerModel3DView.CameraYaw;
            float originalPitch = LayerModel3DView.CameraPitch;
            bool originalTurntable = LayerModel3DView.IsTurntableActive;
            LayerModel3DView.IsTurntableActive = false;

            const int frameCount = 24;
            const int delay = 6; // 60ms / 10

            SixLabors.ImageSharp.Image<Bgra32>? masterGif = null;

            try
            {
                for (int f = 0; f < frameCount; f++)
                {
                    float yaw = originalYaw + (f * (MathF.Tau / frameCount));
                    LayerModel3DView.SetCameraAngles(yaw, originalPitch);
                    await Task.Delay(35);

                    var bmp = await LayerModel3DView.CaptureSnapshotAsync();
                    if (bmp is null) continue;

                    using var fb = bmp.Lock();
                    var width = bmp.PixelSize.Width;
                    var height = bmp.PixelSize.Height;
                    byte[] raw = new byte[width * height * 4];
                    Marshal.Copy(fb.Address, raw, 0, raw.Length);

                    using var frameImg = SixLabors.ImageSharp.Image.LoadPixelData<Bgra32>(raw, width, height);

                    if (width > 640)
                    {
                        int newH = (int)(height * (640f / width));
                        frameImg.Mutate(x => x.Resize(640, newH));
                    }

                    var frameMeta = frameImg.Frames.RootFrame.Metadata.GetGifMetadata();
                    frameMeta.FrameDelay = delay;
                    frameMeta.DisposalMode = FrameDisposalMode.RestoreToBackground;

                    if (masterGif is null)
                    {
                        /* Seed the animation with the first captured frame instead of a freshly allocated image:
                         * a new Image<Bgra32> keeps its own blank root frame, which showed up as a flash at the
                         * start of every loop of the exported gif. */
                        masterGif = frameImg.Clone();
                        masterGif.Metadata.GetGifMetadata().RepeatCount = 0;
                        continue;
                    }

                    masterGif.Frames.AddFrame(frameImg.Frames.RootFrame);
                }

                if (masterGif is null) return;

                await using var outStream = await file.OpenWriteAsync();
                await masterGif.SaveAsGifAsync(outStream);
            }
            finally
            {
                masterGif?.Dispose();
                LayerModel3DView.SetCameraAngles(originalYaw, originalPitch);
                LayerModel3DView.IsTurntableActive = originalTurntable;
            }

            await this.MessageBoxInfo($"Saved 360° turntable animation to {file.Name}.", "Turntable Exported");
        }
        catch (Exception ex)
        {
            await this.MessageBoxError(ex.Message, "Turntable Export Failed");
        }
    }

    private void DisposeLayer3DPreview()
    {
        if (IsLayer3DBuilding && Progress.CanCancel) Progress.TokenSource.Cancel();

        /* Stop the simulation before the model goes away, otherwise it keeps driving ActualLayer and would
         * resume against whatever file is loaded next. */
        _printSimulationTimer?.Stop();
        IsPrintSimulationPlaying = false;

        lock (_layer3DCapLock)
        {
            _layer3DCapTargetGeneration++;
            _layer3DAppliedCapGeneration = _layer3DCapTargetGeneration;
            _layer3DCapTargetLayer = null;
            _layer3DCapTargetMesh = null;
        }
        LayerModel3DView.ClearCap();
        LayerModel3DView.Mesh = null;
        LayerModel3DView.IssueMesh = null;
        _layer3DMesh?.Dispose();
        _layer3DIssueMesh?.Dispose();
        _layer3DMesh = null;
        _layer3DIssueMesh = null;
        _layerAreas = null;
        _peelSpikes = null;
        _maxLayerArea = 0f;
        _peakLayerIndex = 0;
        _suctionCupIssues.Clear();
        _all3DIssues.Clear();
        _current3DIssueIndex = -1;
        _has3DIssues = false;
        _current3DIssueSummary = string.Empty;
        _hasSuctionCups = false;
        _suctionCupSummary = string.Empty;
        _layer3DIssueBuildGeneration++;
        _layerPreviewTabIndex = 0;
        RaisePropertyChanged(nameof(LayerPreviewTabIndex));
        InvalidateLayer3DPreviewStatus();
    }
}
