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

        [ReadOnly(true)] [Browsable(false)] public FrmToolWindow ParentToolWindow => ParentForm as FrmToolWindow;

        [Editor("System.ComponentModel.Design.MultilineStringEditor", typeof(UITypeEditor))]
        [SettingsBindable(true)]
        public string Description { get; set; }

        private bool _buttonOkEnabled;
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
        public virtual string ConfirmationText => $"{Text}?";

        #endregion

        #region Constructor
        public CtrlToolWindowContent()
        {
            InitializeComponent();
        }
        #endregion

        #region Methods

        public virtual bool ValidateForm() => true;

        public DialogResult MessageErrorBox(string message, MessageBoxButtons buttons = MessageBoxButtons.OK) => GUIExtensions.MessageErrorBox($"{Text} Error", message, buttons);
        public DialogResult MessageQuestionBox(string message, string title = null, MessageBoxButtons buttons = MessageBoxButtons.YesNo) => GUIExtensions.MessageQuestionBox($"{title ?? Text}", message, buttons);

        #endregion

        
    }
}
