/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Xml.Serialization;
using UVtools.Core.FileFormats;
using UVtools.Core.Objects;

namespace UVtools.Core.Suggestions
{
    public abstract class Suggestion : BindableBase
    {
        #region Members

        private bool _enabled = true;
        private bool _autoApply;
        private FileFormat _slicerFile;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets if this suggestion is enabled
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set => RaiseAndSetIfChanged(ref _enabled, value);
        }

        /// <summary>
        /// Gets or sets if this suggestion can be auto applied once file load
        /// </summary>
        public bool AutoApply
        {
            get => _autoApply;
            set => RaiseAndSetIfChanged(ref _autoApply, value);
        }

        /// <summary>
        /// Gets or sets the <see cref="FileFormat"/>
        /// </summary>
        [XmlIgnore]
        public FileFormat SlicerFile
        {
            get => _slicerFile;
            set
            {
                if (ReferenceEquals(_slicerFile, value)) return;
                _slicerFile = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(IsAvailable));
                RaisePropertyChanged(nameof(IsApplied));
            }
        }

        /// <summary>
        /// Gets if this suggestion is informative only and contain no actions to execute
        /// </summary>
        public virtual bool IsInformativeOnly => false;

        /// <summary>
        /// Gets if this suggestion is available given the <see cref="SlicerFile"/>
        /// </summary>
        public virtual bool IsAvailable => true;

        /// <summary>
        /// Gets if this suggestion is already applied given the <see cref="SlicerFile"/>
        /// </summary>
        public abstract bool IsApplied { get; }

        /// <summary>
        /// Gets the title for this suggestion
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// Gets the message for this suggestion
        /// </summary>
        public abstract string Message { get; } 

        /// <summary>
        /// Gets the tooltip message
        /// </summary>
        public virtual string ToolTip => null;

        public virtual string InformationUrl => null;

        /// <summary>
        /// Gets the confirmation message before apply the suggestion
        /// </summary>
        public virtual string ConfirmationMessage => null;
        

        public string GlobalAppliedMessage => $"✓ {Title}";
        public string GlobalNotAppliedMessage => $"⚠ {Title}";

        #endregion

        #region Methods

        public void RefreshNotifyAll()
        {
            RaisePropertyChanged(nameof(IsAvailable));
            RaisePropertyChanged(nameof(IsApplied));
            RefreshNotifyMessage();
        }

        public void RefreshNotifyMessage()
        {
            RaisePropertyChanged(nameof(Message));
            RaisePropertyChanged(nameof(ToolTip));
        }

        /// <summary>
        /// Executes and applies the suggestion
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        protected virtual bool ExecuteInternally()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Executes and applies the suggestion
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public bool Execute()
        {
            if (_slicerFile is null) throw new InvalidOperationException($"The suggestion '{Title}' can't execute due the lacking of a file parent.");
            if (!Enabled || !IsAvailable || IsApplied || IsInformativeOnly) return false;
            
            var result = ExecuteInternally();

            RaisePropertyChanged(nameof(IsApplied));
            RefreshNotifyMessage();

            return result;
        }

        /// <summary>
        /// Executes only if this suggestion is marked with <see cref="AutoApply"/> as true
        /// </summary>
        /// <returns></returns>
        public bool ExecuteIfAutoApply()
        {
            return _autoApply && Execute();
        }
        #endregion
    }
}
