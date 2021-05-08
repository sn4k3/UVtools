/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

namespace UVtools.Core.Scripting
{
    public abstract class ScriptBaseInput
    {
        /// <summary>
        /// Gets the input label
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// Gets the hover tooltip for this input
        /// </summary>
        public string ToolTip { get; set; }

        /// <summary>
        /// Gets the value representative unit name
        /// </summary>
        public string Unit { get; set; }
    }

    public abstract class ScriptBaseInput<T> : ScriptBaseInput
    {
        /// <summary>
        /// Gets or sets the value for this input
        /// </summary>
        public T Value { get; set; }
    }
}
