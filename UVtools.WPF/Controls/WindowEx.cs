/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
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
