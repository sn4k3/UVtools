/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using UVtools.AvaloniaControls;
using UVtools.Core.Layers;
using UVtools.Core.Managers;

namespace UVtools.UI;

/// <summary>
/// The compare mode of the layer preview: the current layer is shown on the left of a split line that the user drags,
/// and another layer on its right, drawn by the layer image box as a composite layer.
/// </summary>
public partial class MainWindow
{
    /// <summary>
    /// What is shown on the right side of the compare split.
    /// </summary>
    public enum LayerCompareModes : byte
    {
        PreviousLayer,
        NextLayer,
        Previous10Layers,
        Next10Layers,
        BeforeLastOperation
    }

    private bool _isLayerCompareActive;
    private LayerCompareModes _layerCompareMode = LayerCompareModes.PreviousLayer;
    private GrayscaleImage? _layerCompareImage;
    private ImageBoxCompositeLayer? _layerCompareComposite;

    /// <summary>
    /// Gets or sets if the layer preview compares the current layer, on the left of the split, with another layer on
    /// its right. It turns off the layer difference, both use the layers of the layer image box.
    /// </summary>
    public bool IsLayerCompareActive
    {
        get => _isLayerCompareActive;
        set
        {
            if (!RaiseAndSetIfChanged(ref _isLayerCompareActive, value)) return;
            if (value && _showLayerImageDifference)
            {
                ShowLayerImageDifference = false; // Shows the layer
                return;
            }

            ShowLayer();
        }
    }

    public bool IsLayerCompareModePreviousLayer
    {
        get => _layerCompareMode == LayerCompareModes.PreviousLayer;
        set => SetLayerCompareMode(value, LayerCompareModes.PreviousLayer);
    }

    public bool IsLayerCompareModeNextLayer
    {
        get => _layerCompareMode == LayerCompareModes.NextLayer;
        set => SetLayerCompareMode(value, LayerCompareModes.NextLayer);
    }

    public bool IsLayerCompareModePrevious10Layers
    {
        get => _layerCompareMode == LayerCompareModes.Previous10Layers;
        set => SetLayerCompareMode(value, LayerCompareModes.Previous10Layers);
    }

    public bool IsLayerCompareModeNext10Layers
    {
        get => _layerCompareMode == LayerCompareModes.Next10Layers;
        set => SetLayerCompareMode(value, LayerCompareModes.Next10Layers);
    }

    public bool IsLayerCompareModeBeforeLastOperation
    {
        get => _layerCompareMode == LayerCompareModes.BeforeLastOperation;
        set => SetLayerCompareMode(value, LayerCompareModes.BeforeLastOperation);
    }

    private void SetLayerCompareMode(bool isSelected, LayerCompareModes mode)
    {
        if (!isSelected) return;
        _layerCompareMode = mode;
        RaisePropertyChanged(nameof(IsLayerCompareModePreviousLayer));
        RaisePropertyChanged(nameof(IsLayerCompareModeNextLayer));
        RaisePropertyChanged(nameof(IsLayerCompareModePrevious10Layers));
        RaisePropertyChanged(nameof(IsLayerCompareModeNext10Layers));
        RaisePropertyChanged(nameof(IsLayerCompareModeBeforeLastOperation));

        if (_isLayerCompareActive) ShowLayer();
        else IsLayerCompareActive = true;
    }

    /// <summary>
    /// Gets the text that tells what each side of the compare split shows, or <see langword="null"/> when the compare
    /// mode is off.
    /// </summary>
    public string? LayerCompareText { get; private set; }

    private void SetLayerCompareText(string? text)
    {
        if (text == LayerCompareText) return;
        LayerCompareText = text;
        RaisePropertyChanged(nameof(LayerCompareText));
    }

    /// <summary>
    /// Gets the layer to show on the right side of the split and its description.
    /// </summary>
    private (Layer? Layer, string Description) GetLayerCompareTarget()
    {
        var slicerFile = SlicerFile!;
        Layer? LayerAt(long index) => index >= 0 && index < slicerFile.LayerCount ? slicerFile[(uint)index] : null;

        switch (_layerCompareMode)
        {
            case LayerCompareModes.PreviousLayer:
                return (LayerAt(_actualLayer - 1L), $"Layer {_actualLayer - 1L}");
            case LayerCompareModes.NextLayer:
                return (LayerAt(_actualLayer + 1L), $"Layer {_actualLayer + 1L}");
            case LayerCompareModes.Previous10Layers:
                return (LayerAt(_actualLayer - 10L), $"Layer {_actualLayer - 10L}");
            case LayerCompareModes.Next10Layers:
                return (LayerAt(_actualLayer + 10L), $"Layer {_actualLayer + 10L}");
            case LayerCompareModes.BeforeLastOperation:
            {
                var clipboard = ClipboardManager.Instance;
                var current = clipboard.CurrentIndex;
                if (current < 0 || current + 1 >= clipboard.Count) return (null, "Nothing to undo");
                return (clipboard.GetLayerAtClip(current + 1, _actualLayer),
                    $"Before: {clipboard[current].Description}");
            }
            default:
                return (null, string.Empty);
        }
    }

    /// <summary>
    /// Shows the compared layer on the right side of the split after the layer was shown, or removes it when the compare
    /// mode is off.
    /// </summary>
    private void UpdateLayerCompare()
    {
        if (!_isLayerCompareActive || SlicerFile is null || LayerCache.Image is null)
        {
            ClearLayerCompare();
            return;
        }

        var image = LayerCache.Image;
        var (layer, description) = GetLayerCompareTarget();

        var isAvailable = false;
        using (var mat = layer?.LayerMat)
        {
            if (mat is not null && !mat.IsEmpty && mat.Width == image.Width && mat.Height == image.Height)
            {
                if (_layerCompareImage is null || _layerCompareImage.Width != mat.Width ||
                    _layerCompareImage.Height != mat.Height)
                {
                    var previous = _layerCompareImage;
                    _layerCompareImage = new GrayscaleImage(mat.Width, mat.Height);
                    if (_layerCompareComposite is not null) _layerCompareComposite.GrayscaleImage = _layerCompareImage;
                    previous?.Dispose();
                }

                _layerCompareImage.CopyFrom(mat.DataPointer, mat.Step);
                isAvailable = true;
            }
        }

        if (_layerCompareComposite is null)
        {
            _layerCompareComposite = new ImageBoxCompositeLayer
            {
                GrayscaleImage = _layerCompareImage,
                CompareSide = ImageBoxCompareSide.Right
            };
            LayerImageBox.CompositeLayers.Add(_layerCompareComposite);
        }

        _layerCompareComposite.GrayscaleImage = _layerCompareImage;
        _layerCompareComposite.IsVisible = isAvailable;
        LayerImageBox.CompareSplitPosition ??= 0.5;

        SetLayerCompareText(isAvailable
            ? $"◀ Layer {_actualLayer}  │  {description} ▶"
            : $"◀ Layer {_actualLayer}  │  {description} (not available) ▶");
    }

    /// <summary>
    /// Removes the compared layer and the split from the layer image box.
    /// </summary>
    private void ClearLayerCompare()
    {
        SetLayerCompareText(null);
        if (_layerCompareComposite is null && _layerCompareImage is null) return;

        if (_layerCompareComposite is not null) LayerImageBox.CompositeLayers.Remove(_layerCompareComposite);
        _layerCompareComposite = null;
        LayerImageBox.CompareSplitPosition = null;
        _layerCompareImage?.Dispose();
        _layerCompareImage = null;
    }
}
