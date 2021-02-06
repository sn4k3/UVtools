using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Media;
using UVtools.Core.Objects;

namespace UVtools.WPF.Structures
{
    public class PEProfileFolder : BindableBase
    {
        private ObservableCollection<CheckBox> _items = new ObservableCollection<CheckBox>();
        private ushort _installed;
        private ushort _updates;

        public enum FolderType
        {
            Print,
            Printer
        }

        public FolderType Type { get; }

        public string SourcePath { get; }
        public string TargetPath { get; }

        public ObservableCollection<CheckBox> Items
        {
            get => _items;
            set => RaiseAndSetIfChanged(ref _items, value);
        }

        public ushort Installed
        {
            get => _installed;
            set => RaiseAndSetIfChanged(ref _installed, value);
        }

        public ushort Updates
        {
            get => _updates;
            set => RaiseAndSetIfChanged(ref _updates, value);
        }

        public string SelectedFiles
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (CheckBox item in Items)
                {
                    if (!item.IsChecked.HasValue || !item.IsChecked.Value) continue;
                    sb.AppendLine(item.Content.ToString());
                }

                return sb.ToString();
            }
        }

        public PEProfileFolder(FolderType type)
        {
            Type = type;
            
            switch (type)
            {
                case FolderType.Print:
                    SourcePath = string.Format("{0}{1}Assets{1}PrusaSlicer{1}sla_print",
                        App.ApplicationPath, Path.DirectorySeparatorChar);
                    TargetPath = $"{App.GetPrusaSlicerDirectory()}{Path.DirectorySeparatorChar}sla_print";
                    break;
                case FolderType.Printer:
                    SourcePath = string.Format("{0}{1}Assets{1}PrusaSlicer{1}printer",
                        App.ApplicationPath, Path.DirectorySeparatorChar);
                    TargetPath = $"{App.GetPrusaSlicerDirectory()}{Path.DirectorySeparatorChar}printer";
                    break;
            }

            Reset();
        }

        public void Reset()
        {
            Items.Clear();
            Updates = 0;
            Installed = 0;
            if (!Directory.Exists(SourcePath)) return;
            DirectoryInfo di = new DirectoryInfo(SourcePath);
            var files = di.GetFiles("*.ini");
            if (files.Length == 0) return;

            bool folderExists = Directory.Exists(TargetPath);

            for (int i = 0; i < files.Length; i++)
            {
                Items.Add(new CheckBox
                {
                    Content = Path.GetFileNameWithoutExtension(files[i].Name),
                    Tag = files[i]
                });

                if (folderExists)
                {
                    var targetFile = $"{TargetPath}{Path.DirectorySeparatorChar}{files[i].Name}";
                    FileInfo targetFileInfo = new FileInfo(targetFile);
                    if (targetFileInfo.Exists)
                    {
                        Installed++;
                        if (targetFileInfo.Length != files[i].Length || !StaticObjects.GetHashSha256(targetFileInfo.FullName).SequenceEqual(StaticObjects.GetHashSha256(files[i].FullName)))
                        {
                            Items[i].Foreground = Brushes.Red;
                            Items[i].IsChecked = true;
                            Updates++;
                        }
                        else
                        {
                            Items[i].Foreground = Brushes.Green;
                            Items[i].IsEnabled = false;
                        }
                    }
                    else
                    {
                        Updates++;
                    }
                }
            }
        }

        public void SelectNone()
        {
            foreach (var checkBox in Items)
            {
                checkBox.IsChecked = false;
            }
        }

        public void SelectAll()
        {
            foreach (var checkBox in Items)
            {
                checkBox.IsChecked = checkBox.IsEnabled;
            }
        }
    }
}
