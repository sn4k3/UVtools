/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections.Generic;

namespace UVtools.Core.Scripting;

public abstract class ScriptFileDialogInput : ScriptBaseInput<string>
{
    public class ScriptFileDialogFilter
    {
        public string? Name { get; set; }
        public List<string> Extensions { get; set; } = [];
    }

    /// <summary>
    /// Gets or sets the title for the dialog
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Gets or sets the default directory to open the dialog in
    /// </summary>
    public string? Directory { get; set; }

    /// <summary>
    /// Gets or sets the initial filename to be on the dialog
    /// </summary>
    public string? InitialFilename { get; set; }

    /// <summary>
    /// Gets or sets the file filters on the dropdown list
    /// </summary>
    public List<ScriptFileDialogFilter>? Filters { get; set; }
}