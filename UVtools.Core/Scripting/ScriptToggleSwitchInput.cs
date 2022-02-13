/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

namespace UVtools.Core.Scripting
{
    public class ScriptToggleSwitchInput : ScriptBaseInput<bool>
    {
        /// <summary>
        /// Gets or sets the text when the switch is turned off
        /// </summary>
        public string OffText { get; set; }

        /// <summary>
        /// Gets or sets the text when the switch is turned on
        /// </summary>
        public string OnText { get; set; }
    }
}
