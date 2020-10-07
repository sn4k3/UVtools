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

        public PEProfileFolder(FolderType type)
        {
            Type = type;
            
            switch (type)
            {
                case FolderType.Print:
                    SourcePath = string.Format("{0}{1}Assets{1}PrusaSlicer{1}sla_print",
                        App.GetApplicationPath(), Path.DirectorySeparatorChar);
                    TargetPath = string.Format("{0}{1}PrusaSlicer{1}sla_print",
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        Path.DirectorySeparatorChar);
                    break;
                case FolderType.Printer:
                    SourcePath = string.Format("{0}{1}Assets{1}PrusaSlicer{1}printer",
                        App.GetApplicationPath(), Path.DirectorySeparatorChar);
                    TargetPath = string.Format("{0}{1}PrusaSlicer{1}printer",
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        Path.DirectorySeparatorChar);
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
                    Content = files[i].Name,
                    Tag = files
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
