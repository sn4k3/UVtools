using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using UVtools.Core.Objects;

namespace UVtools.UI.Structures;

public class PSProfileFolder : BindableBase
{
    public static string AssetsPrusaSlicer => Path.Combine(App.ApplicationPath, "Assets", "PrusaSlicer");
    private RangeObservableCollection<CheckBox> _items = new ();
    private ushort _installed;
    private ushort _updates;

    public enum FolderType
    {
        Print,
        Printer
    }

    public FolderType Type { get; }

    public bool IsSuperSlicer { get; }

    public string SourcePath { get; }
    public string TargetPath { get; }

    public RangeObservableCollection<CheckBox> Items
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
            StringBuilder sb = new();
            foreach (var item in Items)
            {
                if (!item.IsChecked.HasValue || !item.IsChecked.Value) continue;
                sb.AppendLine(item.Content?.ToString());
            }

            return sb.ToString();
        }
    }

    public PSProfileFolder(FolderType type, bool isSuperSlicer = false)
    {
        Type = type;
        IsSuperSlicer = isSuperSlicer;

        switch (type)
        {
            case FolderType.Print:
                SourcePath = Path.Combine(AssetsPrusaSlicer, "sla_print");
                TargetPath = Path.Combine(App.GetPrusaSlicerDirectory(isSuperSlicer)!, "sla_print");
                break;
            case FolderType.Printer:
                SourcePath = Path.Combine(AssetsPrusaSlicer, "printer");
                TargetPath = Path.Combine(App.GetPrusaSlicerDirectory(isSuperSlicer)!, "printer");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        Reset();
    }

    public void Reset()
    {
        Items.Clear();
        Updates = 0;
        Installed = 0;
        if (!Directory.Exists(SourcePath)) return;
        DirectoryInfo di = new(SourcePath);
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

            if (!folderExists) continue;
            var targetFile = $"{TargetPath}{Path.DirectorySeparatorChar}{files[i].Name}";
            FileInfo targetFileInfo = new(targetFile);
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