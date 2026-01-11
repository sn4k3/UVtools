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
using SukiUI.MessageBox;
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
        var sukiButtons = buttons switch
        {
            MessageButtons.Ok => SukiMessageBoxButtons.OK,
            MessageButtons.YesNo => SukiMessageBoxButtons.YesNo,
            MessageButtons.OkCancel => SukiMessageBoxButtons.OKCancel,
            MessageButtons.OkAbort => SukiMessageBoxButtons.OKCancel,
            MessageButtons.YesNoCancel => SukiMessageBoxButtons.YesNoCancel,
            MessageButtons.YesNoAbort => SukiMessageBoxButtons.YesNoCancel,
            _ => throw new ArgumentOutOfRangeException(nameof(buttons), buttons, null)
        };
        return Dispatcher.UIThread.InvokeAsync(async () =>
        {
            var result = await App.MainWindow.MessageBoxQuestion(message, string.IsNullOrWhiteSpace(title) ? "Question" : title, sukiButtons, false, true);
            return result switch
            {
                SukiMessageBoxResult.OK => MessageButtonResult.Ok,
                SukiMessageBoxResult.Yes => MessageButtonResult.Yes,
                SukiMessageBoxResult.No => MessageButtonResult.No,
                SukiMessageBoxResult.Cancel => MessageButtonResult.Cancel,
                SukiMessageBoxResult.Close => MessageButtonResult.None,
                SukiMessageBoxResult.Apply => MessageButtonResult.Ok,
                SukiMessageBoxResult.Ignore => MessageButtonResult.None,
                SukiMessageBoxResult.Retry => MessageButtonResult.Yes,
                SukiMessageBoxResult.Abort => MessageButtonResult.Abort,
                SukiMessageBoxResult.Continue => MessageButtonResult.Ok,
                _ => throw new ArgumentOutOfRangeException()
            };
        });
    }
    #endregion
}