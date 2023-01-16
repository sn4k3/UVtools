/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using UVtools.Core.Dialogs;

namespace UVtools.Core.Managers;

public static class MessageBoxManager
{
    /// <summary>
    /// <para>Gets the standard message box for this session which will trigger a message with pre-defined buttons.</para>
    /// <para>If using console it will prompt there as text, otherwise if in UI will use dialogs.</para>
    /// </summary>
    public static AbstractMessageBoxStandard Standard { get; set; } = ConsoleMessageBoxStandard.Instance;
}