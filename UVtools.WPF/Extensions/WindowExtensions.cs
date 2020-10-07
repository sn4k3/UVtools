/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;

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
                    WindowIcon = new WindowIcon(App.GetAsset("/Assets/Icons/UVtools.ico")),
                    WindowStartupLocation = location,
                    CanResize = false
                });

            return await messageBoxStandardWindow.ShowDialog(window);
        }

        public static async Task<ButtonResult> MessageBoxInfo(this Window window, string message, string title = null, ButtonEnum buttons = ButtonEnum.Ok, Style style = Style.None)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Information", buttons, Icon.Info, WindowStartupLocation.CenterOwner, style);

        public static async Task<ButtonResult> MessageBoxError(this Window window, string message, string title = null, ButtonEnum buttons = ButtonEnum.Ok, Style style = Style.None)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Error", buttons, Icon.Error, WindowStartupLocation.CenterOwner, style);

        public static async Task<ButtonResult> MessageBoxQuestion(this Window window, string message, string title = null, ButtonEnum buttons = ButtonEnum.YesNo, Style style = Style.None)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", buttons, Icon.Setting, WindowStartupLocation.CenterOwner, style);


        public static void ShowDialogSync(this Window window, Window parent = null)
        {
            if (parent is null) parent = window;
            using (var source = new CancellationTokenSource())
            {
                window.ShowDialog(parent).ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
                Dispatcher.UIThread.MainLoop(source.Token);
            }
        }

        public static T ShowDialogSync<T>(this Window window, Window parent = null)
        {
            if (parent is null) parent = window;
            using (var source = new CancellationTokenSource())
            {
                var task = window.ShowDialog<T>(parent);
                task.ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
                Dispatcher.UIThread.MainLoop(source.Token);
                return task.Result;
            }

            return default(T);
        }

        public static void ResetDataContext(this Window window)
        {
            var old = window.DataContext;
            window.DataContext = new object();
            window.DataContext = old;
        }
    }
}
