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
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using StageKit.Primitives;
using UVtools.Core;
using UVtools.Core.Extensions;
using UVtools.Core.Layers;
using UVtools.Core.MeshFormats;
using UVtools.Core.Voxel;
using UVtools.UI.Controls;
using UVtools.UI.Extensions;

namespace UVtools.UI;

public partial class MainWindow
{
    private bool _isLayer3DBuilding;
    private bool _layer3DClipToCurrentLayer;
    private readonly object _layer3DCapLock = new();
    private bool _isLayer3DCapBuilding;
    private Layer? _layer3DCapTargetLayer;
    private VoxelPreviewMesh? _layer3DCapTargetMesh;
    private int _layer3DCapTargetGeneration;
    private int _layer3DAppliedCapGeneration;
    private int _layer3DIssueBuildGeneration;
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
            InvalidateLayer3DPreviewStatus();
            if (value == 0)
            {
                ShowLayer();
                return;
            }
            if (value != 1) return;

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
    public string Layer3DBuildButtonText => _layer3DMesh is null ? "Build model" : "Rebuild model";

    public string Layer3DStatus
    {
        get
        {
            if (!string.IsNullOrEmpty(_layer3DRendererError)) return _layer3DRendererError;
            if (IsLayer3DBuilding) return "Generating the slice-derived 3D model…";
            if (_layer3DMesh is null) return "Select this tab or press Build model to generate a cached 3D preview.";

            var stale = IsLayer3DPreviewStale ? " • stale — rebuild to include current slices" : string.Empty;
            var issues = _layer3DIssueMesh is { IndexCount: > 0 }
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
        LayerModelOrientationCube.SetCameraOrientation(LayerModel3DView.CameraYaw, LayerModel3DView.CameraPitch);
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

        if (OperatingSystem.IsMacOS())
        {
            _layer3DRendererError =
                "3D preview is unavailable while Avalonia is using the macOS Metal backend. The layer preview remains available.";
        }

        LayerModel3DView.ClipToLayer = Layer3DClipToCurrentLayer;
        UpdateLayer3DClip();
        RefreshLayer3DPreviewSettings();
    }

    private void RefreshLayer3DPreviewSettings()
    {
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
        LayerModel3DView.GhostClippedModel = Settings.Layer3DPreview.GhostClippedModel;
        LayerModel3DView.SlabThickness = Settings.Layer3DPreview.SlabThicknessMm;
        LayerModel3DView.ShowModelStats = Settings.Layer3DPreview.ShowModelStats;
        LayerModel3DView.ShowCenterOfMass = Settings.Layer3DPreview.ShowCenterOfMass;

        if (SlicerFile is not null)
        {
            LayerModel3DView.PlateWidth = SlicerFile.DisplayWidth;
            LayerModel3DView.PlateHeight = SlicerFile.DisplayHeight;
            LayerModel3DView.PrintHeight = SlicerFile.MachineZ > 0 ? SlicerFile.MachineZ : (SlicerFile.Layers.Length > 0 ? SlicerFile.Layers[^1].PositionZ : 0);

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

    public void FocusIssueIn3D(Issue issue)
    {
        _lastFocusedIssue = issue;
        if (SlicerFile is null || !SlicerFile.ContainsLayer(issue.LayerIndex)) return;
        var rect = issue.BoundingRectangle;
        if (rect.IsEmpty)
        {
            if (issue.Parent is not null && !issue.Parent.BoundingRectangle.IsEmpty)
            {
                rect = issue.Parent.BoundingRectangle;
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

        var padX = Math.Max((maxX - minX) * 0.1f, 0.5f);
        var padY = Math.Max((maxY - minY) * 0.1f, 0.5f);
        var padZ = Math.Max(layerH * 0.5f, 0.3f);

        var min = new System.Numerics.Vector3(minX - padX, minY - padY, Math.Max(0f, z - layerH - padZ));
        var max = new System.Numerics.Vector3(maxX + padX, maxY + padY, z + padZ);
        var center = (min + max) / 2;
        var radius = Math.Max((max - min).Length() / 2, 3.0f);

        LayerModel3DView.FocusOnRegion(center, radius);
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
    }

    private void DisposeLayer3DPreview()
    {
        if (IsLayer3DBuilding && Progress.CanCancel) Progress.TokenSource.Cancel();
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
        _layer3DIssueBuildGeneration++;
        _layerPreviewTabIndex = 0;
        RaisePropertyChanged(nameof(LayerPreviewTabIndex));
        InvalidateLayer3DPreviewStatus();
    }
}
