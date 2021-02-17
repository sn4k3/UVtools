/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Drawing;
using System.Xml.Serialization;
using Emgu.CV;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Operations
{
    [Serializable]
    public abstract class Operation : BindableBase, IDisposable
    {
        #region Members
        private Rectangle _roi = Rectangle.Empty;
        private uint _layerIndexEnd;
        private uint _layerIndexStart;
        private string _profileName;
        private bool _profileIsDefault;
        private Enumerations.LayerRangeSelection _layerRangeSelection = Enumerations.LayerRangeSelection.All;
        private FileFormat _slicerFile;
        public const byte ClassNameLength = 9;
        #endregion

        #region Properties
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
            set => RaiseAndSetIfChanged(ref _layerIndexStart, value);
        }

        /// <summary>
        /// Gets the end layer index where operation will ends in
        /// </summary>
        public virtual uint LayerIndexEnd
        {
            get => _layerIndexEnd;
            set => RaiseAndSetIfChanged(ref _layerIndexEnd, value);
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
            set => RaiseAndSetIfChanged(ref _roi, value);
        }

        public bool HaveROI => !ROI.IsEmpty;

        /// <summary>
        /// Gets if this operation have been executed once
        /// </summary>
        [XmlIgnore]
        public bool HaveExecuted { get; private set; }
        #endregion

        #region Constructor
        protected Operation() { }

        protected Operation(FileFormat slicerFile)
        {
            _slicerFile = slicerFile;
            SelectAllLayers();
            InitWithSlicerFile();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Validates the operation
        /// </summary>
        /// <returns>null or empty if validates, or else, return a string with error message</returns>
        public virtual StringTag Validate(params object[] parameters) => null;

        public bool CanValidate(params object[] parameters)
        {
            var result = Validate(parameters);
            return result is null || string.IsNullOrEmpty(result.Content);
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
                    throw new ArgumentOutOfRangeException();
            }
        }
        

        /// <summary>
        /// Called to init the object when <see cref="SlicerFile"/> changes
        /// </summary>
        public virtual void InitWithSlicerFile() { }

        public Mat GetRoiOrDefault(Mat defaultMat)
        {
            return HaveROI ? new Mat(defaultMat, ROI) : defaultMat;
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
        

        protected virtual bool ExecuteInternally(OperationProgress progress)
        {
            throw new NotImplementedException();
        }

        public bool Execute(OperationProgress progress = null)
        {
            if (_slicerFile is null) throw new InvalidOperationException($"{Title} can't execute due the lacking of file parent.");
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

        public virtual void Dispose() { }
        #endregion
    }
}
