/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Windows.Forms;

namespace UVtools.Core.Extensions
{
    public static class GUIExtensions
    {
        public static DialogResult MessageBoxError(string title, string message, MessageBoxButtons buttons = MessageBoxButtons.OK) => 
            MessageBox.Show(message, title, buttons, MessageBoxIcon.Error);

        public static DialogResult MessageBoxQuestion(string title, string message, MessageBoxButtons buttons = MessageBoxButtons.YesNo) =>
            MessageBox.Show(message, title, buttons, MessageBoxIcon.Question);

        public static DialogResult MessageBoxInformation(string title, string message, MessageBoxButtons buttons = MessageBoxButtons.OK) =>
            MessageBox.Show(message, title, buttons, MessageBoxIcon.Information);
    }
}
