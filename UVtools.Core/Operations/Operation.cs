/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.IO;
using System.Xml.Serialization;
using Emgu.CV;
using Emgu.CV.Util;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public abstract class Operation : BindableBase, IDisposable
    {
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
        private FileFormat _slicerFile;
        private OperationImportFrom _importedFrom = OperationImportFrom.None;
        private Rectangle _roi = Rectangle.Empty;
        private Point[][] _maskPoints;
        private uint _layerIndexEnd;
        private uint _layerIndexStart;
        private string _profileName;
        private bool _profileIsDefault;
        private Enumerations.LayerRangeSelection _layerRangeSelection = Enumerations.LayerRangeSelection.All;
        public const byte ClassNameLength = 9;
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

        [XmlIgnore]
        public FileFormat SlicerFile
        {
            get => _slicerFile;
            set
            {
                if(!RaiseAndSetIfChanged(ref _slicerFile, value)) return;
                InitWithSlicerFile();
            }
        }

        [XmlIgnore]
        public object Tag { get; set; }

        /// <summary>
        /// Gets the ID name of this operation, this comes from class name with "Operation" removed
        /// </summary>
        public string Id => GetType().Name.Remove(0, ClassNameLength);

        public virtual Enumerations.LayerRangeSelection StartLayerRangeSelection => Enumerations.LayerRangeSelection.All;

        /// <summary>
        /// Gets the last used layer range selection, returns none if custom
        /// </summary>
        public Enumerations.LayerRangeSelection LayerRangeSelection
        {
            get => _layerRangeSelection;
            set => RaiseAndSetIfChanged(ref _layerRangeSelection, value);
        }

        public virtual string LayerRangeString
        {
            get
            {
                if (LayerRangeSelection == Enumerations.LayerRangeSelection.None)
                {
                    return $" [Layers: {LayerIndexStart}-{LayerIndexEnd}]";
                }

                return $" [Layers: {LayerRangeSelection}]";
            }
        }

        /// <summary>
        /// Gets if this operation should set layer range to the actual layer index on layer preview
        /// </summary>
        public virtual bool PassActualLayerIndex => false;

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
                if(!RaiseAndSetIfChanged(ref _layerIndexStart, value)) return;
                RaisePropertyChanged(nameof(LayerRangeCount));
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
                if(!RaiseAndSetIfChanged(ref _layerIndexEnd, value)) return;
                RaisePropertyChanged(nameof(LayerRangeCount));
            }
        }

        public uint LayerRangeCount => LayerIndexEnd - LayerIndexStart + 1;

        /// <summary>
        /// Gets the name for this profile
        /// </summary>
        public string ProfileName
        {
            get => _profileName;
            set => RaiseAndSetIfChanged(ref _profileName, value);
        }

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

        public bool HaveROI => !ROI.IsEmpty;

        /// <summary>
        /// Gets or sets an Mask to process this operation
        /// </summary>
        [XmlIgnore]
        public Point[][] MaskPoints
        {
            get => _maskPoints;
            set
            {
                if (!CanMask) return;
                if(!RaiseAndSetIfChanged(ref _maskPoints, value)) return;
                //if(HaveMask) ROI = Rectangle.Empty;
            }
        }

        public bool HaveMask => _maskPoints is not null && _maskPoints.Length > 0;

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
        #endregion

        #region Constructor
        protected Operation() { }

        protected Operation(FileFormat slicerFile) : this()
        {
            _slicerFile = slicerFile;
            SelectAllLayers();
            InitWithSlicerFile();
        }
        #endregion

        #region Methods

        public virtual string ValidateInternally() => null;

        /// <summary>
        /// Validates the operation
        /// </summary>
        /// <returns>null or empty if validates, otherwise return a string with error message</returns>
        public string Validate()
        {
            IsValidated = true;
            return ValidateInternally();
        }

        public bool CanValidate()
        {
            return string.IsNullOrWhiteSpace(Validate());
        }

        public void SelectAllLayers()
        {
            LayerIndexStart = 0;
            LayerIndexEnd = SlicerFile.LastLayerIndex;
            LayerRangeSelection = Enumerations.LayerRangeSelection.All;
        }

        public void SelectCurrentLayer(uint layerIndex)
        {
            LayerIndexStart = LayerIndexEnd = layerIndex;
            LayerRangeSelection = Enumerations.LayerRangeSelection.Current;
        }

        public void SelectBottomLayers()
        {
            LayerIndexStart = 0;
            LayerIndexEnd = SlicerFile.BottomLayerCount - 1u;
            LayerRangeSelection = Enumerations.LayerRangeSelection.Bottom;
        }

        public void SelectNormalLayers()
        {
            LayerIndexStart = SlicerFile.BottomLayerCount;
            LayerIndexEnd = SlicerFile.LastLayerIndex;
            LayerRangeSelection = Enumerations.LayerRangeSelection.Normal;
        }

        public void SelectFirstLayer()
        {
            LayerIndexStart = LayerIndexEnd = 0;
            LayerRangeSelection = Enumerations.LayerRangeSelection.First;
        }

        public void SelectLastLayer()
        {
            LayerIndexStart = LayerIndexEnd = SlicerFile.LastLayerIndex; 
            LayerRangeSelection = Enumerations.LayerRangeSelection.Last;
        }

        public void SelectFirstToCurrentLayer(uint currentLayerIndex)
        {
            LayerIndexStart = 0;
            LayerIndexEnd = Math.Min(currentLayerIndex, SlicerFile.LastLayerIndex);
        }

        public void SelectCurrentToLastLayer(uint currentLayerIndex)
        {
            LayerIndexStart = Math.Min(currentLayerIndex, SlicerFile.LastLayerIndex);
            LayerIndexEnd = SlicerFile.LastLayerIndex;
        }

        public void SelectLayers(Enumerations.LayerRangeSelection range)
        {
            switch (range)
            {
                case Enumerations.LayerRangeSelection.None:
                    break;
                case Enumerations.LayerRangeSelection.All:
                    SelectAllLayers();
                    break;
                case Enumerations.LayerRangeSelection.Current:
                    //SelectCurrentLayer();
                    break;
                case Enumerations.LayerRangeSelection.Bottom:
                    SelectBottomLayers();
                    break;
                case Enumerations.LayerRangeSelection.Normal:
                    SelectNormalLayers();
                    break;
                case Enumerations.LayerRangeSelection.First:
                    SelectFirstLayer();
                    break;
                case Enumerations.LayerRangeSelection.Last:
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

        public void ClearROI()
        {
            ROI = Rectangle.Empty;
        }

        public void ClearROIandMasks()
        {
            ClearROI();
            ClearMasks();
        }

        public void SetROIIfEmpty(Rectangle roi)
        {
            if (HaveROI) return;
            ROI = roi;
        }

        public Mat GetRoiOrDefault(Mat defaultMat)
        {
            return HaveROI && defaultMat.Size != _roi.Size ? new Mat(defaultMat, _roi) : defaultMat;
        }

        public void ClearMasks()
        {
            MaskPoints = null;
        }

        public void SetMasksIfEmpty(Point[][] points)
        {
            if (HaveMask) return;
            MaskPoints = points;
        }

        public Mat GetMask(Mat mat) => GetMask(_maskPoints, mat);

        public Mat GetMask(Point[][] points, Mat mat)
        {
            if (!HaveMask) return null;

            var mask = EmguExtensions.InitMat(mat.Size);
            using VectorOfVectorOfPoint vec = new(points);
            CvInvoke.DrawContours(mask, vec, -1, EmguExtensions.WhiteByte, -1);
            return GetRoiOrDefault(mask);
        }

        public void ApplyMask(Mat original, Mat result, Mat mask)
        {
            if (mask is null) return;
            var originalRoi = GetRoiOrDefault(original);
            var resultRoi = result;
            if (originalRoi.Size != result.Size) // Accept a ROI mat
            {
                resultRoi = GetRoiOrDefault(result);
            }

            if (mask.Size != resultRoi.Size) // Accept a full size mask
            {
                mask = GetRoiOrDefault(mask);
            }

            using var tempMat = originalRoi.Clone();
            resultRoi.CopyTo(tempMat, mask);
            tempMat.CopyTo(resultRoi);
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


        protected virtual bool ExecuteInternally(OperationProgress progress)
        {
            throw new NotImplementedException();
        }

        public bool Execute(OperationProgress progress = null)
        {
            if (_slicerFile is null) throw new InvalidOperationException($"{Title} can't execute due the lacking of a file parent.");
            if (!IsValidated)
            {
                var msg = Validate();
                if(!string.IsNullOrWhiteSpace(msg)) throw new InvalidOperationException($"{Title} can't execute due some errors:\n{msg}");
            }

            

            progress ??= new OperationProgress();
            progress.Reset(ProgressAction, LayerRangeCount);
            HaveExecuted = true;

            var result = ExecuteInternally(progress);

            progress.Token.ThrowIfCancellationRequested();
            return result;
        }

        public virtual bool Execute(Mat mat, params object[] arguments)
        {
            throw new NotImplementedException();
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

        public void Serialize(string path)
        {
            XmlSerializer serializer = new(GetType());
            using StreamWriter writer = new(path);
            serializer.Serialize(writer, this);
        }

        public virtual Operation Clone()
        {
            var operation = MemberwiseClone() as Operation;
            operation.SlicerFile = _slicerFile;
            return operation;
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(ProfileName)) return ProfileName;

            var result = $"{Title}: {LayerRangeString}";
            return result;
        }

        public virtual void Dispose() { GC.SuppressFinalize(this); }
        #endregion

        #region Static Methods

        public static Operation Deserialize(string path, Type type)
        {
            XmlSerializer serializer = new(type);
            using var stream = File.OpenRead(path);
            var operation = (Operation)serializer.Deserialize(stream);
            operation.ImportedFrom = OperationImportFrom.Session;
            return operation;
        }

        public static Operation Deserialize(string path, Operation operation) => Deserialize(path, operation.GetType());

        #endregion
    }
}
