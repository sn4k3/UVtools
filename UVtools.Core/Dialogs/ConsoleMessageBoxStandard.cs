/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Threading.Tasks;

namespace UVtools.Core.Dialogs;

public class ConsoleMessageBoxStandard : AbstractMessageBoxStandard
{
    #region Instance
    private static readonly Lazy<ConsoleMessageBoxStandard> _instance = new(() => new ConsoleMessageBoxStandard());

    public static ConsoleMessageBoxStandard Instance => _instance.Value;
    #endregion

    #region Constructor
    private ConsoleMessageBoxStandard()
    { }
    #endregion

    #region Methods
    public override Task<MessageButtonResult> ShowDialog(string? title, string message, MessageButtons buttons = MessageButtons.Ok)
    {
        if(!string.IsNullOrWhiteSpace(title)) Console.WriteLine(title);
        Console.WriteLine(message);

        while (true)
        {
            Console.Write("Response ");

            switch (buttons)
            {
                case MessageButtons.Ok:
                    Console.Write("[OK]");
                    break;
                case MessageButtons.OkCancel:
                    Console.Write("[OK/Cancel]");
                    break;
                case MessageButtons.OkAbort:
                    Console.Write("[OK/Abort]");
                    break;
                case MessageButtons.YesNo:
                    Console.Write("[Yes/No]");
                    break;
                case MessageButtons.YesNoCancel:
                    Console.Write("[Yes/No/Cancel]");
                    break;
                case MessageButtons.YesNoAbort:
                    Console.Write("[Yes/No/Abort]");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buttons));
            }

            Console.Write(": ");

            var line = Console.ReadLine();

            line = line?.Trim().ToLower();
            
            switch (buttons)
            {
                case MessageButtons.Ok:
                    /*if (line is "ok" or "y" or "yes") */return Task.FromResult(MessageButtonResult.Ok);
                case MessageButtons.OkCancel:
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line is "ok" or "y" or "yes") return Task.FromResult(MessageButtonResult.Ok);
                    if (line is "cancel" or "c") return Task.FromResult(MessageButtonResult.Cancel);
                    break;
                case MessageButtons.OkAbort:
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line is "ok" or "y" or "yes") return Task.FromResult(MessageButtonResult.Ok);
                    if (line is "a" or "abort") return Task.FromResult(MessageButtonResult.Abort);
                    break;
                case MessageButtons.YesNo:
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line is "y" or "yes") return Task.FromResult(MessageButtonResult.Yes);
                    if (line is "n" or "no") return Task.FromResult(MessageButtonResult.No);
                    break;
                case MessageButtons.YesNoCancel:
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line is "y" or "yes") return Task.FromResult(MessageButtonResult.Yes);
                    if (line is "n" or "no") return Task.FromResult(MessageButtonResult.No);
                    if (line is "cancel" or "c") return Task.FromResult(MessageButtonResult.Cancel);
                    break;
                case MessageButtons.YesNoAbort:
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line is "y" or "yes") return Task.FromResult(MessageButtonResult.Yes);
                    if (line is "n" or "no") return Task.FromResult(MessageButtonResult.No);
                    if (line is "a" or "abort") return Task.FromResult(MessageButtonResult.Abort);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(buttons));
            }

            //Console.Write("\b\b -- Invalid option!");
        }
    }
    #endregion
}