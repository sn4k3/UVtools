/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.ComponentModel;
using System.Drawing.Design;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.GUI.Annotations;
using UVtools.GUI.Forms;

namespace UVtools.GUI.Controls
{
    public partial class CtrlToolWindowContent : UserControl, INotifyPropertyChanged
    {
        #region BindableBase
        /// <summary>
        ///     Multicast event for property change notifications.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///     Checks if a property already matches a desired value.  Sets the property and
        ///     notifies listeners only when necessary.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="storage">Reference to a property with both getter and setter.</param>
        /// <param name="value">Desired value for the property.</param>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers that
        ///     support CallerMemberName.
        /// </param>
        /// <returns>
        ///     True if the value was changed, false if the existing value matched the
        ///     desired value.
        /// </returns>
        [NotifyPropertyChangedInvocator]
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return false;
            }

            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers
        ///     that support <see cref="CallerMemberNameAttribute" />.
        /// </param>
        [NotifyPropertyChangedInvocator]
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = PropertyChanged;
            if (!ReferenceEquals(eventHandler, null))
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Properties

        [ReadOnly(true)] [Browsable(false)] public Operation BaseOperation { get; private set; }

        [ReadOnly(true)] [Browsable(false)] public FrmToolWindow ParentToolWindow => ParentForm as FrmToolWindow;

        [SettingsBindable(true)] public bool CanRun { get; set; } = true;

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string Description { get; set; }

        [SettingsBindable(true)] public bool ButtonResetDefaultsVisible { get; set; } = false;

        private bool _buttonOkEnabled = true;
        [SettingsBindable(true)]
        public bool ButtonOkEnabled
        {
            get => _buttonOkEnabled;
            set => SetProperty(ref _buttonOkEnabled, value);
        }

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string ButtonOkText { get; set; } = "Ok";

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string ButtonCancelText { get; set; } = "Cancel";

        [SettingsBindable(true)] public bool LayerRangeVisible { get; set; } = true;

        [SettingsBindable(true)] public bool LayerRangeEndVisible { get; set; } = true;

        /*[SettingsBindable(true)]
        public uint LayerRangeStart { get; set; }

        [SettingsBindable(true)]
        public uint LayerRangeEnd { get; set; }*/

        [ReadOnly(true)]
        [Browsable(false)]
        public virtual string ConfirmationText
        {
            get
            {
                if (BaseOperation is null || string.IsNullOrEmpty(BaseOperation.ConfirmationText))
                {
                    return $"{Text}?";
                }

                return BaseOperation.ConfirmationText;
            }
        }

        #endregion

        #region Constructor
        public CtrlToolWindowContent()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods

        public void SetOperation(Operation operation)
        {
            BaseOperation = operation;
            if(!string.IsNullOrEmpty(operation.Title)) Text = operation.Title;
            if (!string.IsNullOrEmpty(operation.Description)) Description = operation.Description;
            if (!string.IsNullOrEmpty(operation.ButtonOkText)) ButtonOkText = operation.ButtonOkText;
        }

        /// <summary>
        /// Updates operation object with items retrieved from form fields
        /// </summary>
        public virtual void UpdateOperation()
        {
            if (ParentToolWindow is null) return;
            BaseOperation.LayerIndexStart = ParentToolWindow.LayerRangeStart;
            BaseOperation.LayerIndexEnd = ParentToolWindow.LayerRangeEnd;
        }

        /// <summary>
        /// Validates if is safe to continue with operation
        /// </summary>
        /// <returns></returns>
        public virtual bool ValidateForm()
        {
            if (BaseOperation is null) return true;
            UpdateOperation();
            return ValidateFormFromString(BaseOperation.Validate());
        }

        /// <summary>
        /// Validates if is safe to continue with operation, if not shows a message box with the error
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool ValidateFormFromString(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            MessageBoxError(text);
            return false;
        }

        /// <summary>
        /// Validates if is safe to continue with operation, if not shows a message box with the error
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool ValidateFormFromString(StringTag text)
        {
            if (text is null) return true;
            if (string.IsNullOrEmpty(text.ToString())) return true;
            MessageBoxError(text.ToString());
            return false;
        }

        public DialogResult MessageBoxError(string message, MessageBoxButtons buttons = MessageBoxButtons.OK) => GUIExtensions.MessageBoxError($"{Text} Error", message, buttons);
        public DialogResult MessageQuestionBox(string message, string title = null, MessageBoxButtons buttons = MessageBoxButtons.YesNo) => GUIExtensions.MessageBoxQuestion($"{title ?? Text}", message, buttons);

        #endregion


        public virtual void ResetDefaults() { }
    }
}
