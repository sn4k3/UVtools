using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using UVtools.Core.Objects;
using UVtools.Core.Operations;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolControl : UserControlEx
    {
        private Operation _baseOperation;

        public Operation BaseOperation
        {
            get => _baseOperation;
            set
            {
                _baseOperation = value;
                RaisePropertyChanged();
                if (DataContext is null) return;
                ResetDataContext();
            }
        }

        public ToolWindow ParentWindow { get; set; } = null;

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

        /*public virtual void SetOperation(Operation operation)
        {
            BaseOperation = operation;
            ResetDataContext();
        }*/

        /// <summary>
        /// Validates if is safe to continue with operation
        /// </summary>
        /// <returns></returns>
        public virtual async Task<bool> ValidateForm()
        {
            if (BaseOperation is null) return true;
            if (!UpdateOperation()) return false;
            return await ValidateFormFromString(BaseOperation.Validate());
        }

        /// <summary>
        /// Validates if is safe to continue with operation, if not shows a message box with the error
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<bool> ValidateFormFromString(string text)
        {
            if (string.IsNullOrEmpty(text)) return true;
            await ParentWindow.MessageBoxError(text);
            return false;
        }

        /// <summary>
        /// Validates if is safe to continue with operation, if not shows a message box with the error
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async Task<bool> ValidateFormFromString(StringTag text) => await ValidateFormFromString(text?.ToString());
    }
}
