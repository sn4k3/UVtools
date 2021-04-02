/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;

namespace UVtools.Core.Scripting
{
    public class ScriptGlobals
    {
        /// <summary>
        /// Gets the loaded slicer file
        /// </summary>
        public FileFormat SlicerFile { get; init; }

        /// <summary>
        /// Gets the progress operation for loading bar
        /// </summary>
        public OperationProgress Progress { get; set; } = new("Unknown");

        /// <summary>
        /// Gets the current operation holding the layer range, mask, roi, etc
        /// </summary>
        public Operation Operation { get; init; }

        /// <summary>
        /// Gets the script configuration
        /// </summary>
        public ScriptConfiguration Script { get; } = new();
    }
}
