/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using UVtools.Core.Voxel;

namespace UVtools.UI;

public partial class MainWindow
{
    private bool _isLayer3DBuilding;
    private bool _layer3DClipToCurrentLayer;
    private VoxelPreviewMesh? _layer3DMesh;
    private int _layer3DPreviewTabIndex;
    private string? _layer3DRendererError;

    public static VoxelPreviewQuality[] Layer3DQualityOptions { get; } = Enum.GetValues<VoxelPreviewQuality>();

    public int Layer3DPreviewTabIndex
    {
        get => _layer3DPreviewTabIndex;
        set
        {
            if (!RaiseAndSetIfChanged(ref _layer3DPreviewTabIndex, value)) return;
            InvalidateLayer3DPreviewStatus();
            if (value != 1) return;

            /* Focus the viewport so that the camera shortcuts work without clicking into it first. */
            Dispatcher.UIThread.Post(() => LayerModel3DView.Focus());

            if (_layer3DMesh is null && !_isLayer3DBuilding && string.IsNullOrEmpty(_layer3DRendererError))
            {
                Dispatcher.UIThread.InvokeAsync(RebuildLayer3DPreview);
            }
        }
    }

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
        get => _layer3DClipToCurrentLayer;
        set
        {
            if (!RaiseAndSetIfChanged(ref _layer3DClipToCurrentLayer, value)) return;
            LayerModel3DView.ClipToLayer = value;
            UpdateLayer3DClip();
        }
    }

    public VoxelPreviewQuality SelectedLayer3DQuality
    {
        get => Settings.LayerPreview.Preview3DQuality;
        set
        {
            if (Settings.LayerPreview.Preview3DQuality == value) return;
            Settings.LayerPreview.Preview3DQuality = value;
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
    public string Layer3DBuildButtonText => _layer3DMesh is null ? "Build model" : "Rebuild model";

    public string Layer3DStatus
    {
        get
        {
            if (!string.IsNullOrEmpty(_layer3DRendererError)) return _layer3DRendererError;
            if (IsLayer3DBuilding) return "Generating the slice-derived 3D model…";
            if (_layer3DMesh is null) return "Select this tab or press Build model to generate a cached 3D preview.";

            var stale = IsLayer3DPreviewStale ? " • stale — rebuild to include current slices" : string.Empty;
            return $"{_layer3DMesh.TriangleCount:N0} triangles • detail 1:{_layer3DMesh.SamplingStride} • " +
                   $"built in {_layer3DMesh.BuildDuration.TotalSeconds:F2}s{stale}";
        }
    }

    private void InitLayer3DPreview()
    {
        LayerModel3DView.CameraOrientationChanged += LayerModelOrientationCube.SetCameraOrientation;
        LayerModelOrientationCube.OrbitRequested += LayerModel3DView.Orbit;
        LayerModelOrientationCube.SnapRequested += LayerModel3DView.SnapToDirection;
        LayerModelOrientationCube.SetCameraOrientation(LayerModel3DView.CameraYaw, LayerModel3DView.CameraPitch);

        /* The cube takes the focus when clicked, keep the camera shortcuts working from there as well. */
        LayerModelOrientationCube.KeyDown += (_, e) => e.Handled = LayerModel3DView.HandleCameraKey(e);

        LayerModel3DView.ProjectionToggleRequested += () =>
        {
            Settings.LayerPreview.Preview3DOrthographic = !Settings.LayerPreview.Preview3DOrthographic;
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
                if (error is null && Layer3DPreviewTabIndex == 1 && _layer3DMesh is null)
                {
                    Dispatcher.UIThread.InvokeAsync(RebuildLayer3DPreview);
                }
            });
        };

        if (OperatingSystem.IsMacOS())
        {
            _layer3DRendererError =
                "3D preview is unavailable while Avalonia is using the macOS Metal backend. The layer preview remains available.";
        }

        LayerModel3DView.ClipToLayer = Layer3DClipToCurrentLayer;
        UpdateLayer3DClip();
        InvalidateLayer3DPreviewStatus();
    }

    private void RefreshLayer3DPreviewSettings()
    {
        LayerModel3DView.VoxelColor = Settings.LayerPreview.Preview3DVoxelBrush;
        LayerModel3DView.IsOrthographic = Settings.LayerPreview.Preview3DOrthographic;
        RaisePropertyChanged(nameof(SelectedLayer3DQuality));
        InvalidateLayer3DPreviewStatus();
    }

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
        UpdateLayer3DClip();
        InvalidateLayer3DPreviewStatus();
    }

    [RelayCommand]
    public void ResetLayer3DCamera()
    {
        LayerModel3DView.ResetCamera();
    }

    private void UpdateLayer3DClip()
    {
        if (SlicerFile is null || !SlicerFile.ContainsLayer(ActualLayer)) return;
        LayerModel3DView.ClipZ = SlicerFile[ActualLayer].PositionZ;
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
    }

    private void DisposeLayer3DPreview()
    {
        if (IsLayer3DBuilding && Progress.CanCancel) Progress.TokenSource.Cancel();
        LayerModel3DView.Mesh = null;
        _layer3DMesh?.Dispose();
        _layer3DMesh = null;
        _layer3DPreviewTabIndex = 0;
        RaisePropertyChanged(nameof(Layer3DPreviewTabIndex));
        InvalidateLayer3DPreviewStatus();
    }
}