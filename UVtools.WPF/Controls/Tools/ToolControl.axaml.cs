using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolControl : UserControl, INotifyPropertyChanged
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
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = PropertyChanged;
            eventHandler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        public Operation BaseOperation = null;
        public ToolWindow ParentWindow = null;

        public bool CanRun { get; set; } = true;

        public ToolControl()
        {
            InitializeComponent();
        }

        public ToolControl(Operation operation) : this()
        {
            BaseOperation = operation;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public virtual void Callback(ToolWindow.Callbacks callback) { }

        public virtual bool UpdateOperation() => true;

        /// <summary>
        /// Validates if is safe to continue with operation
        /// </summary>
        /// <returns></returns>
        public virtual bool ValidateForm()
        {
            if (BaseOperation is null) return true;
            if (!UpdateOperation()) return false;
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
            ParentWindow.MessageBoxError(text);
            return false;
        }

        /// <summary>
        /// Validates if is safe to continue with operation, if not shows a message box with the error
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public bool ValidateFormFromString(StringTag text) => ValidateFormFromString(text?.ToString());
    }
}
