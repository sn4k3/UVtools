/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Threading;
using System.Threading;
using System.Threading.Tasks;
using UVtools.Core.Dialogs;
using UVtools.UI.Controls;
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

    public static async Task<MessageButtonResult> MessageBoxGeneric(this Window window, string message, string? title = null, string? header = null,
        MessageButtons buttons = MessageButtons.Ok, string? headerIcon = null, bool markdown = false, bool topMost = false, WindowStartupLocation location = WindowStartupLocation.CenterOwner)
    {
        ButtonWithIcon[]? msgButtons;

        switch (buttons)
        {
            case MessageButtons.Ok:
                msgButtons =
                [
                    MessageWindow.CreateOkButton(isCancel: true)
                ];
                break;
            case MessageButtons.YesNo:
                msgButtons =
                [
                    MessageWindow.CreateYesButton(),
                    MessageWindow.CreateNoButton(isCancel: true)
                ];
                break;
            case MessageButtons.OkCancel:
                msgButtons =
                [
                    MessageWindow.CreateOkButton(),
                    MessageWindow.CreateCancelButton()
                ];
                break;
            case MessageButtons.OkAbort:
                msgButtons =
                [
                    MessageWindow.CreateOkButton(),
                    MessageWindow.CreateAbortButton()
                ];
                break;
            case MessageButtons.YesNoCancel:
                msgButtons =
                [
                    MessageWindow.CreateYesButton(),
                    MessageWindow.CreateNoButton(),
                    MessageWindow.CreateCancelButton()
                ];
                break;
            case MessageButtons.YesNoAbort:
                msgButtons =
                [
                    MessageWindow.CreateYesButton(),
                    MessageWindow.CreateNoButton(),
                    MessageWindow.CreateAbortButton()
                ];
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null);
        }

        var messageWindow = new MessageWindow(title ?? window.Title!, headerIcon, header, message, msgButtons, markdown)
        {
            WindowStartupLocation = location,
            Topmost = topMost
        };

        var result = (await messageWindow.ShowDialog<ButtonWithIcon>(window))?.Tag;
        if (result is null) return MessageButtonResult.Cancel;
        if (result is not MessageButtonResult resultButton) throw new NotImplementedException($"Message box interface is not correctly implemented, expecting a button result but got {result}.");

        return resultButton;
    }


    public static async Task<MessageButtonResult> MessageBoxInfo(this Window window, string message, string? title = null, MessageButtons buttons = MessageButtons.Ok, bool markdown = false, bool topMost = false)
        => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Information", null, buttons, MessageWindow.IconHeaderInformation, markdown, topMost, WindowStartupLocation.CenterOwner);

    public static async Task<MessageButtonResult> MessageBoxError(this Window window, string message, string? title = null, MessageButtons buttons = MessageButtons.Ok, bool markdown = false, bool topMost = false)
        => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Error", null, buttons, MessageWindow.IconHeaderError, markdown, topMost, WindowStartupLocation.CenterOwner);

    public static async Task<MessageButtonResult> MessageBoxQuestion(this Window window, string message, string? title = null, MessageButtons buttons = MessageButtons.YesNo, bool markdown = false, bool topMost = false)
        => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", null, buttons, MessageWindow.IconHeaderQuestion, markdown, topMost, WindowStartupLocation.CenterOwner);

    public static async Task<MessageButtonResult> MessageBoxWaring(this Window window, string message, string? title = null, MessageButtons buttons = MessageButtons.Ok, bool markdown = false, bool topMost = false)
        => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", null, buttons, MessageWindow.IconHeaderWarning, markdown, topMost, WindowStartupLocation.CenterOwner);

    public static async Task<MessageButtonResult> MessageBoxWithHeaderInfo(this Window window, string header, string message, string? title = null, MessageButtons buttons = MessageButtons.Ok, bool markdown = false, bool topMost = false)
        => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Information", header, buttons, MessageWindow.IconHeaderInformation, markdown, topMost, WindowStartupLocation.CenterOwner);

    public static async Task<MessageButtonResult> MessageBoxWithHeaderError(this Window window, string header, string message, string? title = null, MessageButtons buttons = MessageButtons.Ok, bool markdown = false, bool topMost = false)
        => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Error", header, buttons, MessageWindow.IconHeaderError, markdown, topMost, WindowStartupLocation.CenterOwner);

    public static async Task<MessageButtonResult> MessageBoxWithHeaderQuestion(this Window window, string header, string message, string? title = null, MessageButtons buttons = MessageButtons.YesNo, bool markdown = false, bool topMost = false)
        => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", header, buttons, MessageWindow.IconHeaderQuestion, markdown, topMost, WindowStartupLocation.CenterOwner);

    public static async Task<MessageButtonResult> MessageBoxWithHeaderWaring(this Window window, string header, string message, string? title = null, MessageButtons buttons = MessageButtons.Ok, bool markdown = false, bool topMost = false)
        => await window.MessageBoxGeneric(message, title ?? $"{window.Title} - Question", header, buttons, MessageWindow.IconHeaderWarning, markdown, topMost, WindowStartupLocation.CenterOwner);


    public static void ShowDialogSync(this Window window, Window? parent = null)
    {
        parent ??= window;
        using var source = new CancellationTokenSource();
        window.ShowDialog(parent).ContinueWith(t => source.Cancel(), TaskScheduler.FromCurrentSynchronizationContext());
        Dispatcher.UIThread.MainLoop(source.Token);
    }

    public static T ShowDialogSync<T>(this Window window, Window? parent = null)
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
            window.Screens.ScreenFromWindow(App.MainWindow ?? window) ??
            window.Screens.Primary ??
            window.Screens.All[0];
    }

    public static System.Drawing.Size GetScreenWorkingArea(this Window window)
    {
        var screen = window.GetCurrentScreen();

        return new System.Drawing.Size(
            UserSettings.Instance.General.WindowsTakeIntoAccountScreenScaling ? (int)(screen.WorkingArea.Width / screen.Scaling) : screen.WorkingArea.Width,
            UserSettings.Instance.General.WindowsTakeIntoAccountScreenScaling ? (int)(screen.WorkingArea.Height / screen.Scaling) : screen.WorkingArea.Height);
    }

}