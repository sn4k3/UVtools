/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Emgu.CV;
using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UVtools.Core.EmguCV;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;
using UVtools.Core.Objects;
using ZLinq;

namespace UVtools.Core.Operations;


public abstract class Operation : BindableBase, IDisposable
{
    #region Constants
    public const string NotSupportedMessage = "The current printer and/or file format is not compatible with this tool.";
    public string NotSupportedTitle => $"{Title} - Unable to run";
    #endregion

    #region Enums

    public enum OperationImportFrom : byte
    {
        None,
        Profile,
        Session,
        Undo,
    }
    #endregion

    #region Members

    public const byte ClassNameLength = 9;
    private FileFormat _slicerFile = null!;
    private Rectangle _originalBoundingRectangle;
    private OperationImportFrom _importedFrom = OperationImportFrom.None;
    private Rectangle _roi = Rectangle.Empty;
    private Point[][]? _maskPoints;
    private uint _layerIndexEnd;
    private uint _layerIndexStart;
    private string? _profileName;
    private bool _profileIsDefault;
    private LayerRangeSelection _layerRangeSelection = LayerRangeSelection.All;
    private string? _lastValidationMessage;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets from where this option got loaded/imported
    /// </summary>
    [XmlIgnore]
    public OperationImportFrom ImportedFrom
    {
        get => _importedFrom;
        set => RaiseAndSetIfChanged(ref _importedFrom, value);
    }

    /// <summary>
    /// Gets or sets the parent <see cref="FileFormat"/>
    /// </summary>
    [XmlIgnore]
    public FileFormat SlicerFile
    {
        get => _slicerFile;
        set
        {
            if(!RaiseAndSetIfChanged(ref _slicerFile, value)) return;
            OriginalBoundingRectangle = _slicerFile.BoundingRectangle;
            InitWithSlicerFile();
        }
    }

    /// <summary>
    /// Gets the bounding rectangle of the model, preserved from any change during and after execution
    /// </summary>
    [XmlIgnore]
    public Rectangle OriginalBoundingRectangle
    {
        get => _originalBoundingRectangle;
        private set => RaiseAndSetIfChanged(ref _originalBoundingRectangle, value);
    }

    /// <summary>
    /// Gets or sets any object which is not used internally
    /// </summary>
    [XmlIgnore]
    public object? Tag { get; set; }

    /// <summary>
    /// Gets the ID name of this operation, this comes from class name with "Operation" removed
    /// </summary>
    public string Id => GetType().Name[ClassNameLength..];

    /// <summary>
    /// Gets the starting layer selection
    /// </summary>
    public virtual LayerRangeSelection StartLayerRangeSelection => LayerRangeSelection.All;

    /// <summary>
    /// Gets the last used layer range selection, returns none if custom
    /// </summary>
    public LayerRangeSelection LayerRangeSelection
    {
        get => _layerRangeSelection;
        set
        {
            if (!RaiseAndSetIfChanged(ref _layerRangeSelection, value)) return;
            if(SlicerFile is not null) SelectLayers(_layerRangeSelection);
        }
    }

    /// <summary>
    /// Gets a string representing the layer range, used with profiles
    /// </summary>
    public virtual string LayerRangeString
    {
        get
        {
            if (_layerRangeSelection == LayerRangeSelection.None)
            {
                return $" [Layers: {LayerIndexStart}-{LayerIndexEnd}]";
            }

            return $" [Layers: {_layerRangeSelection}]";
        }
    }

    /// <summary>
    /// Gets if the LayerIndexEnd selector is enabled
    /// </summary>
    public virtual bool LayerIndexEndEnabled => true;

    /// <summary>
    /// Gets if this operation should set layer range to the actual layer index on layer preview
    /// </summary>
    public virtual bool PassActualLayerIndex => false;

    /// <summary>
    /// Gets if this operation can run in a file open as partial mode
    /// </summary>
    public virtual bool CanRunInPartialMode => false;

    /// <summary>
    /// Gets if this operation can make use of ROI
    /// </summary>
    public virtual bool CanROI => true;

    /// <summary>
    /// Gets if this operation can make use maskable areas
    /// </summary>
    public virtual bool CanMask => CanROI;

    /// <summary>
    /// Gets if this operation can store profiles
    /// </summary>
    public virtual bool CanHaveProfiles => true;

    /// <summary>
    /// Gets if this operation supports cancellation
    /// </summary>
    public virtual bool CanCancel => true;

    /// <summary>
    /// Gets the icon class to show on the UI
    /// </summary>
    public virtual string IconClass => null!;

    /// <summary>
    /// Gets the title of this operation
    /// </summary>
    public virtual string Title => Id;

    /// <summary>
    /// Gets a descriptive text of this operation
    /// </summary>
    public virtual string Description => Id;

    /// <summary>
    /// Gets the Ok button text
    /// </summary>
    public virtual string ButtonOkText => Title;

    /// <summary>
    /// Gets the confirmation text for the operation
    /// </summary>
    public virtual string ConfirmationText => $"Are you sure you want to {Id}";

    /// <summary>
    /// Gets the progress window title
    /// </summary>
    public virtual string ProgressTitle => "Processing items";

    /// <summary>
    /// Gets the progress action name
    /// </summary>
    public virtual string ProgressAction => Id;

    /// <summary>
    /// Gets if this operation have a action text
    /// </summary>
    public bool HaveAction => !string.IsNullOrEmpty(ProgressAction);

    /// <summary>
    /// Gets the start layer index where operation will starts in
    /// </summary>
    public virtual uint LayerIndexStart
    {
        get => _layerIndexStart;
        set
        {
            if (SlicerFile is not null)
            {
                SlicerFile.SanitizeLayerIndex(ref value);
            }
            if (!RaiseAndSetIfChanged(ref _layerIndexStart, value)) return;
            RaisePropertyChanged(nameof(LayerRangeCount));
            RaisePropertyChanged(nameof(LayerRangeHaveBottoms));
        }
    }

    /// <summary>
    /// Gets the end layer index where operation will ends in
    /// </summary>
    public virtual uint LayerIndexEnd
    {
        get => _layerIndexEnd;
        set
        {
            if (SlicerFile is not null)
            {
                SlicerFile.SanitizeLayerIndex(ref value);
            }
            if(!RaiseAndSetIfChanged(ref _layerIndexEnd, value)) return;
            RaisePropertyChanged(nameof(LayerRangeCount));
            RaisePropertyChanged(nameof(LayerRangeHaveNormals));
        }
    }

    /// <summary>
    /// Gets if any bottom layer is included in the selected layer range
    /// </summary>
    public bool LayerRangeHaveBottoms => LayerIndexStart < (SlicerFile.FirstNormalLayer?.Index ?? 0);

    /// <summary>
    /// Gets if any normal layer is included in the selected layer range
    /// </summary>
    public bool LayerRangeHaveNormals => LayerIndexEnd >= (SlicerFile.FirstNormalLayer?.Index ?? 0);

    /// <summary>
    /// Gets the number of selected layers
    /// </summary>
    public uint LayerRangeCount => (uint)Math.Max(0, (int)LayerIndexEnd - LayerIndexStart + 1);

    /// <summary>
    /// Gets the name for this profile
    /// </summary>
    public string? ProfileName
    {
        get => _profileName;
        set => RaiseAndSetIfChanged(ref _profileName, value);
    }

    /// <summary>
    /// Gets if this profile is the default to load
    /// </summary>
    public bool ProfileIsDefault
    {
        get => _profileIsDefault;
        set => RaiseAndSetIfChanged(ref _profileIsDefault, value);
    }

    /// <summary>
    /// Gets or sets an ROI to process this operation
    /// </summary>
    [XmlIgnore]
    public Rectangle ROI
    {
        get => _roi;
        set
        {
            if (!CanROI) return;
            RaiseAndSetIfChanged(ref _roi, value);
        }
    }

    /// <summary>
    /// Gets if there is an ROI associated
    /// </summary>
    public bool HaveROI => !_roi.IsEmpty;

    /// <summary>
    /// Gets or sets an Mask to process this operation
    /// </summary>
    [XmlIgnore]
    public Point[][]? MaskPoints
    {
        get => _maskPoints;
        set
        {
            if (!CanMask) return;
            if(!RaiseAndSetIfChanged(ref _maskPoints, value)) return;
            //if(HaveMask) ROI = Rectangle.Empty;
        }
    }

    /// <summary>
    /// Gets if there is masks associated
    /// </summary>
    public bool HaveMask => _maskPoints is not null && _maskPoints.Length > 0;

    /// <summary>
    /// Gets if there is roi or masks associated
    /// </summary>
    public bool HaveROIorMask => HaveROI || HaveMask;

    /// <summary>
    /// Gets if this operation have been executed once
    /// </summary>
    [XmlIgnore]
    public bool HaveExecuted { get; private set; }

    /// <summary>
    /// Gets if this operation have validated at least once
    /// </summary>
    [XmlIgnore]
    public bool IsValidated { get; private set; }

    /// <summary>
    /// Gets the last validation message
    /// </summary>
    [XmlIgnore]
    public string? LastValidationMessage
    {
        get => _lastValidationMessage;
        private set
        {
            if(!RaiseAndSetIfChanged(ref _lastValidationMessage, value)) return;
            RaisePropertyChanged(nameof(IsLastValidationSuccess));
        }
    }

    /// <summary>
    /// Gets if the last validation pass with success
    /// </summary>
    [XmlIgnore]
    public bool IsLastValidationSuccess => string.IsNullOrWhiteSpace(_lastValidationMessage);

    /// <summary>
    /// Gets or sets an report to show to the user after complete the operation with success
    /// </summary>
    [XmlIgnore]
    public string? AfterCompleteReport { get; set; }
    #endregion

    #region Constructor

    protected Operation()
    {
        _layerRangeSelection = StartLayerRangeSelection;
    }

    protected Operation(FileFormat slicerFile) : this()
    {
        _slicerFile = slicerFile;
        _originalBoundingRectangle = slicerFile.BoundingRectangle;
        _layerIndexEnd = slicerFile.LastLayerIndex;
        SelectLayers(_layerRangeSelection);
        InitWithSlicerFile();
    }
    #endregion

    #region Methods

    /// <summary>
    /// Gets if the operation can spawn
    /// </summary>
    public bool CanSpawn => string.IsNullOrWhiteSpace(ValidateSpawn());

    /// <summary>
    /// Gets if this operation can spawn under the <see cref="SlicerFile"/>
    /// </summary>
    public virtual string? ValidateSpawn() => null;

    /// <summary>
    /// Gets if this operation can spawn under the <see cref="SlicerFile"/>
    /// </summary>
    public bool ValidateSpawn(out string? message)
    {
        message = ValidateSpawn();
        return string.IsNullOrWhiteSpace(message);
    }

    /// <summary>
    /// Validates the operation, return null or empty if validates
    /// </summary>
    /// <returns></returns>
    public virtual string? ValidateInternally()
    {
        if (!ValidateSpawn(out var message))
        {
            return message;
        }
        return null;
    }

    /// <summary>
    /// Validates the operation
    /// </summary>
    /// <returns>null or empty if validates, otherwise return a string with error message</returns>
    public string? Validate()
    {
        IsValidated = true;
        LastValidationMessage = ValidateInternally();
        return _lastValidationMessage;
    }

    /// <summary>
    /// Gets if the operation is able to execute
    /// </summary>
    /// <returns></returns>
    public bool CanValidate()
    {
        return string.IsNullOrWhiteSpace(Validate());
    }

    /// <summary>
    /// Selects all layers from first to last layer
    /// </summary>
    public void SelectAllLayers()
    {
        LayerIndexStart = 0;
        LayerIndexEnd = SlicerFile.LastLayerIndex;
        LayerRangeSelection = LayerRangeSelection.All;
    }

    /// <summary>
    /// Selects one layer
    /// </summary>
    /// <param name="layerIndex">Layer index to select</param>
    public void SelectCurrentLayer(uint layerIndex)
    {
        LayerIndexStart = LayerIndexEnd = layerIndex;
        LayerRangeSelection = LayerRangeSelection.Current;
    }

    /// <summary>
    /// Selects all bottom layers
    /// </summary>
    public void SelectBottomLayers()
    {
        LayerIndexStart = 0;
        LayerIndexEnd = Math.Max(1, SlicerFile.FirstNormalLayer?.Index ?? 1) - 1u;
        LayerRangeSelection = LayerRangeSelection.Bottom;
    }

    /// <summary>
    /// Selects all normal layers
    /// </summary>
    public void SelectNormalLayers()
    {
        LayerIndexStart = SlicerFile.FirstNormalLayer?.Index ?? 0;
        LayerIndexEnd = SlicerFile.LastLayerIndex;
        LayerRangeSelection = LayerRangeSelection.Normal;
    }

    /// <summary>
    /// Select the first layer (0)
    /// </summary>
    public void SelectFirstLayer()
    {
        LayerIndexStart = LayerIndexEnd = 0;
        LayerRangeSelection = LayerRangeSelection.First;
    }

    /// <summary>
    /// Select the last layer
    /// </summary>
    public void SelectLastLayer()
    {
        LayerIndexStart = LayerIndexEnd = SlicerFile.LastLayerIndex;
        LayerRangeSelection = LayerRangeSelection.Last;
    }

    /// <summary>
    /// Selects from first to a layer index
    /// </summary>
    /// <param name="currentLayerIndex">To layer index to select</param>
    public void SelectFirstToCurrentLayer(uint currentLayerIndex)
    {
        LayerIndexStart = 0;
        LayerIndexEnd = SlicerFile.SanitizeLayerIndex(currentLayerIndex);
    }

    /// <summary>
    /// Selects from a layer index to the last layer
    /// </summary>
    /// <param name="currentLayerIndex">From layer index to select</param>
    public void SelectCurrentToLastLayer(uint currentLayerIndex)
    {
        LayerIndexStart = SlicerFile.SanitizeLayerIndex(currentLayerIndex);
        LayerIndexEnd = SlicerFile.LastLayerIndex;
    }

    /// <summary>
    /// Selects layer given a range type
    /// </summary>
    /// <param name="range"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void SelectLayers(LayerRangeSelection range)
    {
        switch (range)
        {
            case LayerRangeSelection.None:
                break;
            case LayerRangeSelection.All:
                SelectAllLayers();
                break;
            case LayerRangeSelection.Current:
                //SelectCurrentLayer();
                break;
            case LayerRangeSelection.Bottom:
                SelectBottomLayers();
                break;
            case LayerRangeSelection.Normal:
                SelectNormalLayers();
                break;
            case LayerRangeSelection.First:
                SelectFirstLayer();
                break;
            case LayerRangeSelection.Last:
                SelectLastLayer();
                break;
            default:
                throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Called to init the object when <see cref="SlicerFile"/> changes
    /// </summary>
    public virtual void InitWithSlicerFile() { }

    /// <summary>
    /// Clears the ROI and set to empty
    /// </summary>
    public void ClearROI()
    {
        ROI = Rectangle.Empty;
    }

    /// <summary>
    /// Clear <see cref="ROI"/> and <see cref="MaskPoints"/>
    /// </summary>
    public void ClearROIandMasks()
    {
        ClearROI();
        ClearMasks();
    }

    /// <summary>
    /// Set <see cref="ROI"/> only if not set already
    /// </summary>
    /// <param name="roi">ROI to set</param>
    public void SetROIIfEmpty(Rectangle roi)
    {
        if (HaveROI) return;
        ROI = roi;
    }

    /// <summary>
    /// Gets the <see cref="ROI"/> size, but if empty returns the file resolution size instead
    /// </summary>
    /// <returns></returns>
    public Size GetRoiSizeOrDefault() => GetRoiSizeOrDefault(SlicerFile.Resolution);

    /// <summary>
    /// Gets the <see cref="ROI"/> size, but if empty returns <paramref name="src"/> size instead
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    public Size GetRoiSizeOrDefault(Mat? src) => src is null ? GetRoiSizeOrDefault() : GetRoiSizeOrDefault(src.Size);

    /// <summary>
    /// Gets the <see cref="ROI"/> size, but if empty returns the size from <paramref name="fallbackRectangle"/> instead
    /// </summary>
    /// <param name="fallbackRectangle"></param>
    /// <returns></returns>
    public Size GetRoiSizeOrDefault(Rectangle fallbackRectangle) => GetRoiSizeOrDefault(fallbackRectangle.Size);

    /// <summary>
    /// Gets the <see cref="ROI"/> size, but if empty returns the <paramref name="fallbackSize"/> instead
    /// </summary>
    /// <param name="fallbackSize"></param>
    /// <returns></returns>
    public Size GetRoiSizeOrDefault(Size fallbackSize)
    {
        return HaveROI ? _roi.Size : fallbackSize;
    }

    /// <summary>
    /// Gets the <see cref="ROI"/> size, but if empty returns the model volume bounds size instead
    /// </summary>
    /// <returns></returns>
    public Size GetRoiSizeOrVolumeSize() => GetRoiSizeOrVolumeSize(_originalBoundingRectangle.Size);

    /// <summary>
    /// Gets the <see cref="ROI"/> size, but if empty returns the <paramref name="fallbackSize"/> instead
    /// </summary>
    /// <param name="fallbackSize"></param>
    /// <returns></returns>
    public Size GetRoiSizeOrVolumeSize(Size fallbackSize)
    {
        return HaveROI ? _roi.Size : fallbackSize;
    }

    /// <summary>
    /// Gets a cropped shared <see cref="Mat"/> from <paramref name="src"/> by the <see cref="ROI"/>, but if empty return the <paramref name="src"/> instead
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    public Mat GetRoiOrDefault(Mat src)
    {
        return src.Roi(_roi, EmptyRoiBehaviour.CaptureSource);
    }

    /// <summary>
    /// Gets a cropped shared <see cref="Mat"/> from <paramref name="src"/> by the <see cref="ROI"/>, but if empty crop by <paramref name="fallbackRoi"/>
    /// </summary>
    /// <param name="src"></param>
    /// <param name="fallbackRoi"></param>
    /// <returns></returns>
    public Mat GetRoiOrDefault(Mat src, Rectangle fallbackRoi)
    {
        return HaveROI ? src.Roi(_roi) : src.Roi(fallbackRoi, EmptyRoiBehaviour.CaptureSource);
    }

    /// <summary>
    /// Gets a cropped shared <see cref="Mat"/> by the <see cref="ROI"/>, but if empty crop by <see cref="OriginalBoundingRectangle"/>
    /// </summary>
    /// <param name="defaultMat"></param>
    /// <returns></returns>
    public Mat GetRoiOrVolumeBounds(Mat defaultMat)
    {
        return GetRoiOrDefault(defaultMat, _originalBoundingRectangle);
    }

    /// <summary>
    /// Gets the <see cref="ROI"/>, but if empty returns <see cref="OriginalBoundingRectangle"/>
    /// </summary>
    /// <returns></returns>
    public Rectangle GetRoiOrVolumeBounds() => HaveROI ? _roi : _originalBoundingRectangle;

    /// <summary>
    /// Clears all masks
    /// </summary>
    public void ClearMasks()
    {
        MaskPoints = null;
    }

    /// <summary>
    /// Sets masks only if they are empty
    /// </summary>
    /// <param name="points"></param>
    public void SetMasksIfEmpty(Point[][] points)
    {
        if (HaveMask) return;
        MaskPoints = points;
    }

    /// <summary>
    /// Returns a mask given <see cref="MaskPoints"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public Mat? GetMask(Mat mat, Point offset = default) => GetMask(_maskPoints, mat, offset);

    public Mat? GetMask(Point[][]? points, Mat mat, Point offset = default)
    {
        if (!HaveMask) return null;

        if (!HaveROI) return mat.CreateMask(points!, offset);
        using var mask = mat.CreateMask(points!, offset);
        return GetRoiOrDefault(mask).Clone();
    }

    /// <summary>
    /// Apply a mask to a mat <paramref name="result"/>
    /// </summary>
    /// <param name="original">Original untouched mat</param>
    /// <param name="result">Mat to modify and apply the mask</param>
    /// <param name="mask">Mask</param>
    public void ApplyMask(Mat original, Mat result, Mat? mask)
    {
        if (mask is null) return;
        var originalRoi = original;
        bool needDisposeOriginalRoi = false;
        if (originalRoi.Size != result.Size) // Accept a ROI mat
        {
            originalRoi = GetRoiOrDefault(original);
            needDisposeOriginalRoi = true;
        }

        var resultRoi = result;
        bool needDisposeResultRoi = false;
        if (originalRoi.Size != result.Size) // Accept a ROI mat
        {
            resultRoi = GetRoiOrDefault(result);
            needDisposeResultRoi = true;
        }

        bool needDisposeMask = false;
        if (mask.Size != resultRoi.Size) // Accept a full size mask
        {
            mask = GetRoiOrDefault(mask);
            needDisposeMask = true;
        }

        using var tempMat = originalRoi.Clone();
        resultRoi.CopyTo(tempMat, mask);
        tempMat.CopyTo(resultRoi);

        if (needDisposeOriginalRoi) originalRoi.Dispose();
        if (needDisposeResultRoi) resultRoi.Dispose();
        if (needDisposeMask) mask.Dispose();
    }

    /// <summary>
    /// Gets a mask and apply it
    /// </summary>
    /// <param name="original">Original unmodified image</param>
    /// <param name="result">Result image which will also be modified</param>
    public void ApplyMask(Mat original, Mat result)
    {
        using var mask = GetMask(original);
        ApplyMask(original, result, mask);
    }

    /// <summary>
    /// Execute the operation internally, to be override by class
    /// </summary>
    /// <param name="progress"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    protected virtual bool ExecuteInternally(OperationProgress progress)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Execute the operation
    /// </summary>
    /// <param name="progress"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public bool Execute(OperationProgress? progress = null)
    {
        if (_slicerFile is null) throw new InvalidOperationException($"{Title} can't execute due the lacking of a file parent.");
        if (_slicerFile.DecodeType == FileFormat.FileDecodeType.Partial && !CanRunInPartialMode) throw new InvalidOperationException($"The file was open in partial mode and the tool \"{Title}\" is unable to run in this mode.\nPlease reload the file in full mode in order to use this tool.");

        AfterCompleteReport = null;

        if (!IsValidated)
        {
            var msg = Validate();
            if(!string.IsNullOrWhiteSpace(msg)) throw new InvalidOperationException($"{Title} can't execute due some errors:\n{msg}");
        }

        progress ??= new OperationProgress();
        progress.Reset(ProgressAction, LayerRangeCount);
        HaveExecuted = true;

        var result = ExecuteInternally(progress);

        progress.PauseOrCancelIfRequested();
        return result;
    }

    public Task<bool> ExecuteAsync(OperationProgress? progress = null) => Task.Run(() => Execute(progress), progress?.Token ?? default);

    /// <summary>
    /// Execute the operation on a given <see cref="Mat"/>
    /// </summary>
    /// <param name="mat"></param>
    /// <param name="arguments"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public virtual bool Execute(Mat mat, params object[]? arguments)
    {
        throw new NotImplementedException();
    }

    public Task<bool> ExecuteAsync(Mat mat, params object[]? arguments) => Task.Run(() => Execute(mat, arguments));

    /// <summary>
    /// Get the selected layer range in a new array, array index will not match layer index when a range is selected
    /// </summary>
    /// <returns></returns>
    public Layer[] GetSelectedLayerRange()
    {
        return LayerRangeCount == SlicerFile.LayerCount
            ? SlicerFile.AsValueEnumerable().ToArray()
            : SlicerFile.AsValueEnumerable().Where((_, layerIndex) => layerIndex >= _layerIndexStart && layerIndex <= _layerIndexEnd).ToArray();
    }

    /// <summary>
    /// Copy this operation base configuration to another operation.
    /// Layer range, ROI, Masks
    /// </summary>
    /// <param name="operation"></param>
    public void CopyConfigurationTo(Operation operation)
    {
        operation.LayerIndexStart = LayerIndexStart;
        operation.LayerIndexEnd = LayerIndexEnd;
        operation.ROI = ROI;
        operation.MaskPoints = MaskPoints;
    }

    /// <summary>
    /// Serialize class to XML file
    /// </summary>
    /// <param name="path"></param>
    /// <param name="indent"></param>
    public void Serialize(string path, bool indent = false)
    {
        if(indent) XmlExtensions.SerializeToFile(this, path, XmlExtensions.SettingsIndent);
        else XmlExtensions.SerializeToFile(this, path);
    }

    /// <summary>
    /// Clone object
    /// </summary>
    /// <returns></returns>
    public virtual Operation Clone()
    {
        var operation = MemberwiseClone() as Operation;
        operation!.SlicerFile = _slicerFile;
        return operation;
    }

    public override string ToString()
    {
        if (!string.IsNullOrEmpty(ProfileName)) return ProfileName;

        var result = $"{Title}: {LayerRangeString}";
        return result;
    }

    public virtual void Dispose() { /*GC.SuppressFinalize(this);*/ }
    #endregion

    #region Static Methods

    /// <summary>
    /// Create an instance from a class name or file path
    /// </summary>
    /// <param name="classNamePath">Classname or path to a file</param>
    /// <param name="enableXmlProfileFile">If true, it will attempt to deserialize the operation from a file profile.</param>
    /// <param name="slicerFile"></param>
    /// <returns></returns>
    public static Operation? CreateInstance(string classNamePath, bool enableXmlProfileFile = false, FileFormat? slicerFile = null)
    {
        if (string.IsNullOrWhiteSpace(classNamePath)) return null;
        if (enableXmlProfileFile)
        {
            var operation = Deserialize(classNamePath, slicerFile);
            if (operation is not null) return operation;
        }

        var baseName = "Operation";
        if (classNamePath.StartsWith(baseName)) classNamePath = classNamePath[baseName.Length..];
        if (classNamePath == string.Empty) return null;

        var baseType = typeof(Operation).FullName;
        if (string.IsNullOrWhiteSpace(baseType)) return null;
        var classname = baseType + classNamePath + ", UVtools.Core";
        var type = Type.GetType(classname);

        return (slicerFile is null ? type?.CreateInstance() : type?.CreateInstance(slicerFile)) as Operation;
    }

    /// <summary>
    /// Deserialize <see cref="Operation"/> from a XML file
    /// </summary>
    /// <param name="path">XML file path</param>
    /// <param name="slicerFile"></param>
    /// <returns></returns>
    public static Operation? Deserialize(string path, FileFormat? slicerFile = null)
    {
        if (!File.Exists(path)) return null;

        var fileText = File.ReadAllText(path);
        var match = Regex.Match(fileText, @"(?:<\/\s*Operation)([a-zA-Z0-9_]+)(?:\s*>)");
        if (!match.Success) return null;
        if (match.Groups.Count < 1) return null;
        var operationName = match.Groups[1].Value;
        var baseType = typeof(Operation).FullName;
        if (string.IsNullOrWhiteSpace(baseType)) return null;
        var classname = baseType + operationName + ", UVtools.Core";
        var type = Type.GetType(classname);
        if (type is null) return null;

        return Deserialize(path, type, slicerFile);
    }

    /// <summary>
    /// Deserialize <see cref="Operation"/> from a XML file
    /// </summary>
    /// <param name="path">XML file path</param>
    /// <param name="type"></param>
    /// <param name="slicerFile"></param>
    /// <returns></returns>
    public static Operation? Deserialize(string path, Type type, FileFormat? slicerFile = null)
    {
        var serializer = new XmlSerializer(type);
        using var stream = File.OpenRead(path);
        if (serializer.Deserialize(stream) is not Operation operation) return null;

        operation.ImportedFrom = OperationImportFrom.Session;
        if (slicerFile is not null)
        {
            operation.SlicerFile = slicerFile;
            operation.SelectLayers(operation.LayerRangeSelection);
        }
        return operation;
    }

    /// <summary>
    /// Deserialize <see cref="Operation"/> from a XML file
    /// </summary>
    /// <param name="path">XML file path</param>
    /// <param name="operation"></param>
    /// <param name="slicerFile"></param>
    /// <returns></returns>
    public static Operation? Deserialize(string path, Operation operation, FileFormat? slicerFile = null) => Deserialize(path, operation.GetType(), slicerFile);

    #endregion
}