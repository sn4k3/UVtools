using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UVtools.Core.Dialogs;
using UVtools.Core.FileFormats;
using UVtools.UI.Extensions;

namespace UVtools.UI.Controls;

public partial class RenameFileControl : ToolBaseControl
{
    private string _newFileNameNoExt;
    private readonly string _fileExtension;
    private bool _overwrite;

    public string OldFilePath { get; }
    public string OldFileNameNoExt { get; }

    public string FileExtension => _fileExtension;

    public string NewFileNameNoExt
    {
        get => _newFileNameNoExt;
        set
        {
            if (!RaiseAndSetIfChanged(ref _newFileNameNoExt, value)) return;
            RaisePropertyChanged(nameof(NewFileName));
            RaisePropertyChanged(nameof(NewFilePath));
            ParentWindow.ButtonOkEnabled = CanRename;
        }
    }

    public string NewFileName => $"{_newFileNameNoExt}.{_fileExtension}";

    public string NewFilePath => Path.Combine(SlicerFile.DirectoryPath, NewFileName);

    public bool Overwrite
    {
        get => _overwrite;
        set => RaiseAndSetIfChanged(ref _overwrite, value);
    }

    public bool CanRename => !string.IsNullOrWhiteSpace(_newFileNameNoExt) && !string.Equals(OldFileNameNoExt, _newFileNameNoExt.Trim(), StringComparison.Ordinal) && _newFileNameNoExt.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;

    public RenameFileControl()
    {
        OldFilePath = SlicerFile.FileFullPath;
        _newFileNameNoExt = OldFileNameNoExt = FileFormat.GetFileNameStripExtensions(OldFilePath, out _fileExtension);
            
        DataContext = this;
        InitializeComponent();
    }

    protected override void OnInitialized()
    {
        ParentWindow.ButtonOkEnabled = false;
    }

    public override string Validate()
    {
        var sb = new StringBuilder();

        if (string.IsNullOrWhiteSpace(_newFileNameNoExt))
        {
            sb.AppendLine("The filename can not be blank.");
        }

        if (string.Equals(OldFileNameNoExt, _newFileNameNoExt, StringComparison.Ordinal))
        {
            sb.AppendLine("The old and new filename can not be the same.");
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        if (_newFileNameNoExt.IndexOfAny(invalidChars) >= 0)
        {
            return $"The filename have invalid characters. The following characters are forbidden:\n{string.Join(", ", Path.GetInvalidFileNameChars())}";
        }

        return sb.ToString();
    }

    public override async Task<bool> OnBeforeProcess()
    {
        NewFileNameNoExt = _newFileNameNoExt.Trim();
        if (!CanRename)
        {
            return false;
        }

        if (_overwrite) return true;

        if (!File.Exists(NewFilePath)) return true;
        if (await ParentWindow.MessageBoxQuestion(
                $"The file \"{_newFileNameNoExt}\" already exists, do you want to overwrite?",
                "File already exists") == MessageButtonResult.Yes)
        {
            Overwrite = true;
            return true;
        }

        return false;
    }
}