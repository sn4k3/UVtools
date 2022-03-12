/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

namespace UVtools.Core.Scripting;

public class ScriptOpenFolderDialogInput : ScriptBaseInput<string>
{
    /// <summary>
    /// Gets the title for the dialog
    /// </summary>
    public string? Title { get; set; }
}