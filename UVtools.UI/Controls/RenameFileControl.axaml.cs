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
    private bool _onlyAsciiCharacters;

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
            Validate();
        }
    }

    public string NewFileName => $"{_newFileNameNoExt}.{_fileExtension}";

    public string NewFilePath => Path.Combine(SlicerFile!.DirectoryPath!, NewFileName);

    public bool OnlyAsciiCharacters
    {
        get => _onlyAsciiCharacters;
        set
        {
            if(!RaiseAndSetIfChanged(ref _onlyAsciiCharacters, value)) return;
            Validate();
        }
    }

    public bool Overwrite
    {
        get => _overwrite;
        set => RaiseAndSetIfChanged(ref _overwrite, value);
    }

    public RenameFileControl()
    {
        OldFilePath = SlicerFile!.FileFullPath!;
        _newFileNameNoExt = OldFileNameNoExt = FileFormat.GetFileNameStripExtensions(OldFilePath, out _fileExtension);
        _onlyAsciiCharacters = UserSettings.Instance.Automations.FileNameOnlyAsciiCharacters;
            
        DataContext = this;
        InitializeComponent();
    }

    public RenameFileControl(string newFileNameNoExt) : this()
    {
        _newFileNameNoExt = newFileNameNoExt;
    }

    protected override void OnInitialized()
    {
        ParentWindow!.IsROIOrMasksVisible = false;
        Validate();
    }

    protected override string? ValidateInternally()
    {
        var sb = new StringBuilder();

        var newFileName = _newFileNameNoExt.Trim();
        if (string.IsNullOrWhiteSpace(newFileName))
        {
            return "The file name can not be blank.";
        }

        if (string.Equals(OldFileNameNoExt, newFileName, StringComparison.Ordinal))
        {
            return "The file name must be different from the previous.";
        }

        if (!FileFormat.IsFileNameValid(newFileName, out var message, _onlyAsciiCharacters))
        {
            return message;
        }

        /*var invalidChars = Path.GetInvalidFileNameChars();
        if (newFileName.IndexOfAny(invalidChars) >= 0)
        {
            return $"The filename have invalid characters. The following characters are forbidden:\n{string.Join(", ", invalidChars)}";
        }

        if (_onlyAsciiCharacters)
        {
            var nonAscii = newFileName.Where(c => !char.IsAscii(c)).Distinct();
            if (nonAscii.Any())
            {
                return $"The filename have non-ASCII characters. The following characters are forbidden:\n{string.Join(", ", nonAscii)}";
            }
        }*/

        return sb.ToString();
    }

    protected override void OnAfterValidate()
    {
        ParentWindow!.ButtonOkEnabled = IsLastValidationSuccess;
    }

    public override async Task<bool> OnAfterProcess()
    {
        NewFileNameNoExt = _newFileNameNoExt.Trim();
        if (!IsLastValidationSuccess) return false;

        if (_overwrite) return await RenameCurrentSlicerFile();

        if (!File.Exists(NewFilePath)) return await RenameCurrentSlicerFile();
        if (await ParentWindow!.MessageBoxQuestion(
                $"The file \"{_newFileNameNoExt}\" already exists, do you want to overwrite?",
                "File already exists") == MessageButtonResult.Yes)
        {
            Overwrite = true;
            return await RenameCurrentSlicerFile();
        }

        return false;
    }

    private async Task<bool> RenameCurrentSlicerFile()
    {
        try
        {
            return SlicerFile!.RenameFile(_newFileNameNoExt, _overwrite);
        }
        catch (Exception e)
        {
            await ParentWindow!.MessageBoxError(e.ToString(), "Error while trying to rename the file");
            return false;
        }
    }
}