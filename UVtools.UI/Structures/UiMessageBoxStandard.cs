/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using UVtools.Core.Dialogs;
using UVtools.UI.Extensions;

namespace UVtools.UI.Structures;

public class UiMessageBoxStandard : AbstractMessageBoxStandard
{
    #region Instance
    private static readonly Lazy<UiMessageBoxStandard> _instance = new(() => new UiMessageBoxStandard());

    public static UiMessageBoxStandard Instance => _instance.Value;
    #endregion

    #region Constructor
    private UiMessageBoxStandard()
    { }
    #endregion

    #region Methods
    public override Task<MessageButtonResult> ShowDialog(string? title, string message, MessageButtons buttons = MessageButtons.Ok)
    {
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var result = await App.MainWindow.MessageBoxQuestion(message, string.IsNullOrWhiteSpace(title) ? "Question" : title, buttons, false, true);
            return result;
        });
    }
    #endregion
}