/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using BrightIdeasSoftware;

namespace UVtools.GUI.Controls
{
    public sealed class SlicerPropertyItem
    {
        [OLVColumn(Width = 185)]
        public string Key { get; set; }

        [OLVColumn(Width = 0)]
        public string Value { get; set; }

        [OLVColumn(IsVisible = false)]
        public string Group { get; set; }

        public SlicerPropertyItem(string key, string value, string group)
        {
            Key = key;
            Value = value;
            Group = group;
        }
    }
}
