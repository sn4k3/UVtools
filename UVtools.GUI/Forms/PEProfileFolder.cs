using System;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace UVtools.GUI.Forms
{
    public class PEProfileFolder
    {
        public enum FolderType
        {
            Print,
            Printer
        }

        public FolderType Type { get; }

        public ListView ListView { get; }
        public ToolStripLabel LabelCount { get; }
        public string SourcePath { get; }
        public string TargetPath { get; }

        public string SelectedFiles
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (ListViewItem item in ListView.Items)
                {
                    if (!item.Checked) continue;
                    sb.AppendLine(item.Text);
                }

                return sb.ToString();
            }
        }

        public PEProfileFolder(FolderType type, ListView listView, ToolStripLabel labelCount)
        {
            Type = type;
            ListView = listView;
            LabelCount = labelCount;

            switch (type)
            {
                case FolderType.Print:
                    SourcePath = $"{Application.StartupPath}{Path.DirectorySeparatorChar}PrusaSlicer{Path.DirectorySeparatorChar}sla_print";
                    TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{Path.DirectorySeparatorChar}PrusaSlicer{Path.DirectorySeparatorChar}sla_print";
                    break;
                case FolderType.Printer:
                    SourcePath = $"{Application.StartupPath}{Path.DirectorySeparatorChar}PrusaSlicer{Path.DirectorySeparatorChar}printer";
                    TargetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}{Path.DirectorySeparatorChar}PrusaSlicer{Path.DirectorySeparatorChar}printer";
                    break;
            }
        }
    }
}
