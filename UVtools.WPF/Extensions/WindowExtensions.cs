using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.Models;

namespace UVtools.WPF.Extensions
{
    public static class WindowExtensions
    {
        public static async Task<ButtonResult> MessageBoxGeneric(this Window window, string message, string title = null, 
            ButtonEnum buttons = ButtonEnum.Ok, Icon icon = Icon.None, WindowStartupLocation location = WindowStartupLocation.CenterOwner, Style style = Style.None)
        {
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                new MessageBoxStandardParams
                {
                    ButtonDefinitions = buttons,
                    ContentTitle = title ?? window.Title,
                    ContentMessage = message,
                    Icon = icon,
                    Style = style,
                    WindowStartupLocation = location,
                    CanResize = false
                });
            
            return await messageBoxStandardWindow.ShowDialog(window);
        }

        public static async Task<ButtonResult> MessageBoxInfo(this Window window, string message, string title = null, ButtonEnum buttons = ButtonEnum.Ok, Style style = Style.None)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} Information", buttons, Icon.Info, WindowStartupLocation.CenterOwner, style);

        public static async Task<ButtonResult> MessageBoxError(this Window window, string message, string title = null, ButtonEnum buttons = ButtonEnum.Ok, Style style = Style.None)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} Error", buttons, Icon.Error, WindowStartupLocation.CenterOwner, style);

        public static async Task<ButtonResult> MessageBoxQuestion(this Window window, string message, string title = null, ButtonEnum buttons = ButtonEnum.YesNo, Style style = Style.None)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} Question", buttons, Icon.Info, WindowStartupLocation.CenterOwner, style);

    }
}
