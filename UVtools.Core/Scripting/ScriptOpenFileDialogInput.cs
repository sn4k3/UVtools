/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

namespace UVtools.Core.Scripting
{
    public class ScriptOpenFileDialogInput : ScriptFileDialogInput
    {
        /// <summary>
        /// Gets or sets if allow multiple file selection
        /// </summary>
        public bool AllowMultiple { get; set; }

        /// <summary>
        /// Gets or sets the selected files
        /// </summary>
        public string[] Files {get; set; }
    }
}
