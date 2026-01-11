/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using Avalonia.Controls;
using Avalonia.Threading;
using System.Threading;
using System.Threading.Tasks;
using Material.Icons;
using Material.Icons.Avalonia;
using SukiUI.Controls;
using SukiUI.Extensions;
using SukiUI.MessageBox;
using UVtools.UI.Structures;
using UVtools.UI.Windows;

namespace UVtools.UI.Extensions;

public static class WindowExtensions
{
    /*public static async Task<ButtonResult> MessageBoxGeneric(this Window window, string message, string title = null, string header = null,
        ButtonEnum buttons = ButtonEnum.Ok, Icon icon = Icon.None, bool markdown = false, bool topMost = false, WindowStartupLocation location = WindowStartupLocation.CenterOwner)
    {
        var messageBoxStandardWindow = MessageBox.Avalonia.MessageBoxManager.GetMessageBoxStandardWindow(
            new MessageBoxStandardParams
            {
                ButtonDefinitions = buttons,
                ContentTitle = title ?? window.Title,
                ContentHeader = header,
                ContentMessage = message,
                Markdown = markdown,
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
    }*/

    extension(Window window)
    {
        public async Task<SukiMessageBoxResult> MessageBoxGeneric(string message, string? title = null, string? header = null,
            SukiMessageBoxButtons buttons = SukiMessageBoxButtons.OK, MaterialIconKind? headerIcon = null, bool markdown = false, bool topMost = false, WindowStartupLocation location = WindowStartupLocation.CenterOwner)
        {
            var options = SukiMessageBoxUtilities.GetDefaultOptions();
            options = options with
            {
                WindowStartupLocation = location,
            };

            var host = new SukiMessageBoxHost()
            {
                Header = header,
                Content = message,
                ActionButtonsPreset = buttons
            };

            if (headerIcon is not null)
            {
                host.Icon = new MaterialIcon() { Kind = headerIcon.Value, IconSize = 32 };
            }

            if (markdown)
            {
                host.Content = new MarkdownViewer.Core.Controls.MarkdownViewer() { MarkdownText = message };
            }



            var result = await SukiMessageBox.ShowDialog(window, host, options);
            if (result is null) return SukiMessageBoxResult.Cancel;
            if (result is not SukiMessageBoxResult resultButton) throw new NotImplementedException($"Message box interface is not correctly implemented, expecting a button result but got {result}.");

            return resultButton;
        }

        public async Task<SukiMessageBoxResult> MessageBoxInfo(string message, string? title = null, SukiMessageBoxButtons buttons = SukiMessageBoxButtons.OK, bool markdown = false, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Information", null, buttons, MessageWindow.IconHeaderInformation, markdown, topMost, WindowStartupLocation.CenterOwner);

        public async Task<SukiMessageBoxResult> MessageBoxError(string message, string? title = null, SukiMessageBoxButtons buttons = SukiMessageBoxButtons.OK, bool markdown = false, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Error", null, buttons, MessageWindow.IconHeaderError, markdown, topMost, WindowStartupLocation.CenterOwner);

        public async Task<SukiMessageBoxResult> MessageBoxQuestion(string message, string? title = null, SukiMessageBoxButtons buttons = SukiMessageBoxButtons.YesNo, bool markdown = false, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", null, buttons, MessageWindow.IconHeaderQuestion, markdown, topMost, WindowStartupLocation.CenterOwner);

        public async Task<SukiMessageBoxResult> MessageBoxWaring(string message, string? title = null, SukiMessageBoxButtons buttons = SukiMessageBoxButtons.OK, bool markdown = false, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", null, buttons, MessageWindow.IconHeaderWarning, markdown, topMost, WindowStartupLocation.CenterOwner);

        public async Task<SukiMessageBoxResult> MessageBoxWithHeaderInfo(string header, string message, string? title = null, SukiMessageBoxButtons buttons = SukiMessageBoxButtons.OK, bool markdown = false, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Information", header, buttons, MessageWindow.IconHeaderInformation, markdown, topMost, WindowStartupLocation.CenterOwner);

        public async Task<SukiMessageBoxResult> MessageBoxWithHeaderError(string header, string message, string? title = null, SukiMessageBoxButtons buttons = SukiMessageBoxButtons.OK, bool markdown = false, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Error", header, buttons, MessageWindow.IconHeaderError, markdown, topMost, WindowStartupLocation.CenterOwner);

        public async Task<SukiMessageBoxResult> MessageBoxWithHeaderQuestion(string header, string message, string? title = null, SukiMessageBoxButtons buttons = SukiMessageBoxButtons.YesNo, bool markdown = false, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", header, buttons, MessageWindow.IconHeaderQuestion, markdown, topMost, WindowStartupLocation.CenterOwner);

        public async Task<SukiMessageBoxResult> MessageBoxWithHeaderWaring(string header, string message, string? title = null, SukiMessageBoxButtons buttons = SukiMessageBoxButtons.OK, bool markdown = false, bool topMost = false)
            => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", header, buttons, MessageWindow.IconHeaderWarning, markdown, topMost, WindowStartupLocation.CenterOwner);

        public void ShowDialogSync(Window? parent = null)
        {
            parent ??= window;
            using var source = new CancellationTokenSource();
            window.ShowDialog(parent).ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.MainLoop(source.Token);
        }

        public T ShowDialogSync<T>(Window? parent = null)
        {
            parent ??= window;
            using var source = new CancellationTokenSource();
            var task = window.ShowDialog<T>(parent);
            task.ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
            Dispatcher.UIThread.MainLoop(source.Token);
            return task.Result;
        }

        public void ResetDataContext()
        {
            var old = window.DataContext;
            window.DataContext = new object();
            window.DataContext = old;
        }



        public System.Drawing.Size GetScreenWorkingArea()
        {
            var screen = window.GetHostScreen()!;

            return new System.Drawing.Size(
                UserSettings.Instance.General.WindowsTakeIntoAccountScreenScaling ? (int)(screen.WorkingArea.Width / window.RenderScaling) : screen.WorkingArea.Width,
                UserSettings.Instance.General.WindowsTakeIntoAccountScreenScaling ? (int)(screen.WorkingArea.Height / window.RenderScaling) : screen.WorkingArea.Height);
        }
    }
}