/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2020-2026 PTRTECH
 */

using System;
using System.Numerics;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.Core.Voxel;
using UVtools.UI.Extensions;

namespace UVtools.UI;

public partial class MainWindow
{
    private VoxelObjectSelection? _selected3DObject;
    private bool _isLayer3DObjectSelectionMode;
    private bool _refresh3DPreviewOnHistoryChange;
    private bool _layer3DObjectDuplicateOnDrag;

    /// <summary>A pixel of the object the last edit moved or copied, selected again after the preview rebuild.</summary>
    private (uint LayerIndex, System.Drawing.Point Pixel)? _pending3DObjectAnchor;
    private decimal? _layer3DObjectOffsetX = 0;
    private decimal? _layer3DObjectOffsetY = 0;

    public bool IsLayer3DObjectSelectionMode
    {
        get => _isLayer3DObjectSelectionMode;
        set
        {
            if (!RaiseAndSetIfChanged(ref _isLayer3DObjectSelectionMode, value)) return;
            LayerModel3DView.IsObjectSelectionMode = value;
            if (value) Settings.Layer3DPreview.ShowMeasure = false;
            else SetSelected3DObject(null);
        }
    }

    public bool HasSelected3DObject =>
        SlicerFile is { } file && _selected3DObject?.IsCurrentFor(file) == true;

    /// <summary>Whether dropping a dragged object duplicates it instead of moving it.</summary>
    public bool Layer3DObjectDuplicateOnDrag
    {
        get => _layer3DObjectDuplicateOnDrag;
        set
        {
            if (!RaiseAndSetIfChanged(ref _layer3DObjectDuplicateOnDrag, value)) return;
            LayerModel3DView.DuplicateOnDrag = value;
        }
    }

    public decimal? Layer3DObjectOffsetX
    {
        get => _layer3DObjectOffsetX;
        set => RaiseAndSetIfChanged(ref _layer3DObjectOffsetX, value);
    }

    public decimal? Layer3DObjectOffsetY
    {
        get => _layer3DObjectOffsetY;
        set => RaiseAndSetIfChanged(ref _layer3DObjectOffsetY, value);
    }

    private async void OnLayer3DObjectPointClicked(Vector3? hit, Vector3 normal)
    {
        if (!IsGUIEnabled) return;
        var file = SlicerFile;
        var mesh = _layer3DMesh;
        SetSelected3DObject(null);
        if (hit is not { } point || file is null || mesh is null) return;

        if (!mesh.IsCurrentFor(file))
        {
            await this.MessageBoxInfo("Rebuild the 3D preview before selecting an object.");
            return;
        }

        var selection = await Find3DObject(file, mesh, () => VoxelObjectSelection.Find(file, mesh, point, normal, Progress));
        if (selection.IsCanceled) return;
        if (selection.Value is null)
        {
            await this.MessageBoxInfo("No slice pixels could be selected at this point. Click the object again or rebuild at higher detail.");
            return;
        }
        SetSelected3DObject(selection.Value);
    }

    /// <summary>
    /// Selects the object an edit has just moved or copied, once the preview was rebuilt for the edited slices.
    /// </summary>
    private async Task ApplyPending3DObjectSelection()
    {
        if (_pending3DObjectAnchor is not { } anchor) return;
        _pending3DObjectAnchor = null;
        if (!IsLayer3DObjectSelectionMode || SlicerFile is not { } file || _layer3DMesh is not { } mesh ||
            !mesh.IsCurrentFor(file)) return;

        var selection = await Find3DObject(file, mesh,
            () => VoxelObjectSelection.FindAtPixel(file, mesh, anchor.LayerIndex, anchor.Pixel, Progress));
        if (!selection.IsCanceled && selection.Value is { } found) SetSelected3DObject(found);
    }

    /// <summary>
    /// Runs an object search in the background with the progress window, and checks that the result still applies.
    /// </summary>
    /// <returns>The selection, which is null when nothing was found, or canceled when it no longer applies.</returns>
    private async Task<(bool IsCanceled, VoxelObjectSelection? Value)> Find3DObject(FileFormat file,
        VoxelPreviewMesh mesh, Func<VoxelObjectSelection?> find)
    {
        VoxelObjectSelection? selection;
        IsPrintSimulationPlaying = false;
        _printSimulationTimer?.Stop();
        IsGUIEnabled = false;
        ShowProgressWindow("Selecting connected object");
        try
        {
            selection = await Task.Run(find, Progress.Token);
            if (Progress.Token.IsCancellationRequested) return (true, null);
        }
        catch (OperationCanceledException)
        {
            return (true, null);
        }
        catch (Exception exception)
        {
            await HandleException(exception, "Unable to select 3D object");
            return (true, null);
        }
        finally
        {
            IsGUIEnabled = true;
        }

        if (!IsLayer3DObjectSelectionMode || !ReferenceEquals(file, SlicerFile) ||
            !ReferenceEquals(mesh, _layer3DMesh) || !mesh.IsCurrentFor(file)) return (true, null);
        return (false, selection);
    }

    private void OnLayer3DObjectDragging(Vector2 offset)
    {
        Layer3DObjectOffsetX = Math.Round((decimal)offset.X, 3);
        Layer3DObjectOffsetY = Math.Round((decimal)offset.Y, 3);
    }

    private async void OnLayer3DObjectDropped(Vector2 offset, bool duplicate)
    {
        try
        {
            OnLayer3DObjectDragging(offset);
            await EditSelected3DObject(duplicate ? VoxelObjectEdit.Duplicate : VoxelObjectEdit.Move);
        }
        catch (Exception exception)
        {
            await HandleException(exception, "Unable to move 3D object");
        }
        finally
        {
            LayerModel3DView.EndObjectDrag();
        }
    }

    /// <summary>Gets the state of the object selection shown in the object toolbar.</summary>
    public string Layer3DObjectSelectionText { get; private set; } = "Click an object on the model to select it";

    private void SetSelected3DObject(VoxelObjectSelection? selection)
    {
        _selected3DObject = selection;
        var mesh = _layer3DMesh;
        if (selection is not null && mesh is not null)
        {
            var (min, max) = selection.GetPreviewBounds(mesh);
            var size = max - min;
            Layer3DObjectSelectionText = $"{size.X:0.##} × {size.Y:0.##} × {size.Z:0.##} mm";
            LayerModel3DView.SetObjectSelectionBounds((min, max), selection.GetPreviewOffsetLimits(mesh));
        }
        else
        {
            Layer3DObjectSelectionText = "Click an object on the model to select it";
            LayerModel3DView.SetObjectSelectionBounds(null);
        }
        RaisePropertyChanged(nameof(HasSelected3DObject));
        RaisePropertyChanged(nameof(Layer3DObjectSelectionText));
    }

    [RelayCommand]
    private void ClearSelected3DObject() => SetSelected3DObject(null);

    [RelayCommand]
    private async Task EditSelected3DObject(VoxelObjectEdit edit)
    {
        if (!IsGUIEnabled || !IsLayer3DObjectSelectionMode || !HasSelected3DObject ||
            SlicerFile is not { } file || _selected3DObject is not { } selection || _layer3DMesh is not { } mesh)
        {
            return;
        }

        var offset = VoxelObjectSelection.GetPixelOffset(mesh, (double)(Layer3DObjectOffsetX ?? 0),
            (double)(Layer3DObjectOffsetY ?? 0));
        var operation = new OperationEditVoxelObject(file, selection, edit, offset);
        if (operation.Validate() is { Length: > 0 } error)
        {
            await this.MessageBoxInfo(error, operation.Title);
            return;
        }

        /* Keep the moved or copied object selected. The preview rebuild that follows the edit selects it again, set
         * it before running as the history change can already queue that rebuild. */
        if (edit != VoxelObjectEdit.Delete)
        {
            var (layerIndex, pixel) = selection.Anchor;
            pixel.Offset(offset);
            _pending3DObjectAnchor = (layerIndex, pixel);
        }

        if (await RunOperation(operation))
        {
            _refresh3DPreviewOnHistoryChange = true;
        }
        else
        {
            _pending3DObjectAnchor = null;
        }

        if (!selection.IsCurrentFor(file))
        {
            SetSelected3DObject(null);
            if (ReferenceEquals(SlicerFile, file) && Is3DPreviewVisible)
            {
                await RebuildLayer3DPreview();
            }
        }
    }
}
