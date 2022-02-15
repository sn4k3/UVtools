/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;

namespace UVtools.WPF.Extensions
{
    public static class WindowExtensions
    {
        public static async Task<ButtonResult> MessageBoxGeneric(this Window window, string message, string title = null, 
            ButtonEnum buttons = ButtonEnum.Ok, Icon icon = Icon.None, bool topMost = false, WindowStartupLocation location = WindowStartupLocation.CenterOwner)
        {
            var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
                new MessageBoxStandardParams
                {
                    ButtonDefinitions = buttons,
                    ContentTitle = title ?? window.Title,
                    ContentMessage = message,
                    Icon = icon,
                    WindowIcon = new WindowIcon(App.GetAsset("/Assets/Icons/UVtools.ico")),
                    WindowStartupLocation = location,
                    CanResize = UserSettings.Instance.General.WindowsCanResize,
                    MaxWidth = window.GetScreenWorkingArea().Width - UserSettings.Instance.General.WindowsHorizontalMargin,
                    MaxHeight = window.GetScreenWorkingArea().Height - UserSettings.Instance.General.WindowsVerticalMargin,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    ShowInCenter = true,
                    Topmost = topMost
                });
            return await messageBoxStandardWindow.ShowDialog(window);
        }

        public static async Task<ButtonResult> MessageBoxInfo(this Window window, string message, string title = null, ButtonEnum buttons = ButtonEnum.Ok, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Information", buttons, Icon.Info, topMost, WindowStartupLocation.CenterOwner);

        public static async Task<ButtonResult> MessageBoxError(this Window window, string message, string title = null, ButtonEnum buttons = ButtonEnum.Ok, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Error", buttons, Icon.Error, topMost, WindowStartupLocation.CenterOwner);

        public static async Task<ButtonResult> MessageBoxQuestion(this Window window, string message, string title = null, ButtonEnum buttons = ButtonEnum.YesNo, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", buttons, Icon.Setting, topMost, WindowStartupLocation.CenterOwner);

        public static async Task<ButtonResult> MessageBoxWaring(this Window window, string message, string title = null, ButtonEnum buttons = ButtonEnum.Ok, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", buttons, Icon.Warning, topMost, WindowStartupLocation.CenterOwner);


        public static void ShowDialogSync(this Window window, Window parent = null)
        {
            parent ??= window;
            using var source = new CancellationTokenSource();
            window.ShowDialog(parent).ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.MainLoop(source.Token);
        }

        public static T ShowDialogSync<T>(this Window window, Window parent = null)
        {
            parent ??= window;
            using var source = new CancellationTokenSource();
            var task = window.ShowDialog<T>(parent);
            task.ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.MainLoop(source.Token);
            return task.Result;
        }

        public static void ResetDataContext(this Window window)
        {
            var old = window.DataContext;
            window.DataContext = new object();
            window.DataContext = old;
        }

        public static Screen GetCurrentScreen(this Window window)
        {
            return //window.Screens.ScreenFromVisual(window) ??
                window.Screens.ScreenFromVisual(App.MainWindow) ??
                window.Screens.Primary ??
                window.Screens.All[0];
        }

        public static System.Drawing.Size GetScreenWorkingArea(this Window window)
        {
            var screen = window.GetCurrentScreen();
            
            return new System.Drawing.Size(
                UserSettings.Instance.General.WindowsTakeIntoAccountScreenScaling ? (int)(screen.WorkingArea.Width / screen.PixelDensity) : screen.WorkingArea.Width,
                UserSettings.Instance.General.WindowsTakeIntoAccountScreenScaling ? (int)(screen.WorkingArea.Height / screen.PixelDensity) : screen.WorkingArea.Height);
        } 
       
    }
}
