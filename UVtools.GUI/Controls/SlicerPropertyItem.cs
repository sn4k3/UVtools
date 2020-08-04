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
