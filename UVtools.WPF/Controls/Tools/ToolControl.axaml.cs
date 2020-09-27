using System.Collections.Generic;
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
        private PropertyChangedEventHandler _propertyChanged;
        private List<string> events = new List<string>();

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { _propertyChanged += value; events.Add("added"); }
            remove { _propertyChanged -= value; events.Add("removed"); }
        }

        protected bool RaiseAndSetIfChanged<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
        }

        /// <summary>
        ///     Notifies listeners that a property value has changed.
        /// </summary>
        /// <param name="propertyName">
        ///     Name of the property used to notify listeners.  This
        ///     value is optional and can be provided automatically when invoked from compilers
        ///     that support <see cref="CallerMemberNameAttribute" />.
        /// </param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            OnPropertyChanged(e);
            _propertyChanged?.Invoke(this, e);
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
