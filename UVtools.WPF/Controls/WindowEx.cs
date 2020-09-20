using Avalonia.Controls;

namespace UVtools.WPF.Controls
{
    public class WindowEx : Window
    {
        public DialogResults DialogResult { get; set; } = DialogResults.Unknown;
        public enum DialogResults
        {
            Unknown,
            OK,
            Cancel
        }

        public void CloseWithResult()
        {
            Close(DialogResult);
        }

        public void ResetDataContext()
        {
            var old = DataContext;
            DataContext = new object();
            DataContext = old;
        }
    }
}
