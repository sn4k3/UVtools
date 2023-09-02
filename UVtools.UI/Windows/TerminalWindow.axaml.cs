/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.IO;
using System.Text;
using Avalonia.Platform.Storage;
using UVtools.Core;
using UVtools.UI.Controls;

namespace UVtools.UI.Windows;

public partial class TerminalWindow : WindowEx
{
    private static readonly string DefaultTerminalText = $"> Welcome to {About.Software} interactive terminal.\n" +
                                                         "> Type in some commands in C# language to inject code.\n" +
                                                         "> Example 1: SlicerFile.FirstLayer\n" +
                                                         "> Example 2: SlicerFile.ExposureTime = 3\n\n";

    private string _terminalText = DefaultTerminalText;
    private string _commandText = string.Empty;

    private bool _multiLine = true;
    private bool _autoScroll = true;
    private bool _verbose = true;
    private bool _clearCommandAfterSend = true;
        
    public ScriptState? _scriptState;

    #region Properties

    public string TerminalText
    {
        get => _terminalText;
        set
        {
            if(!RaiseAndSetIfChanged(ref _terminalText, value)) return;
            if(_autoScroll) TerminalTextBox.CaretIndex = _terminalText.Length - 1;
        }
    }

    public string CommandText
    {
        get => _commandText;
        set => RaiseAndSetIfChanged(ref _commandText, value ?? string.Empty);
    }

    public bool MultiLine
    {
        get => _multiLine;
        set => RaiseAndSetIfChanged(ref _multiLine, value);
    }

    public bool AutoScroll
    {
        get => _autoScroll;
        set => RaiseAndSetIfChanged(ref _autoScroll, value);
    }

    public bool Verbose
    {
        get => _verbose;
        set => RaiseAndSetIfChanged(ref _verbose, value);
    }

    public bool ClearCommandAfterSend
    {
        get => _clearCommandAfterSend;
        set => RaiseAndSetIfChanged(ref _clearCommandAfterSend, value);
    }

    #endregion

    public TerminalWindow()
    {
        InitializeComponent();

        AddHandler(DragDrop.DropEvent, (sender, e) =>
        {
            var text = e.Data.GetText();
            if (text is not null)
            {
                CommandText = text;
                return;
            }

            var fileNames = e.Data.GetFiles();
            if (fileNames is not null)
            {
                var sb = new StringBuilder();
                foreach (var fileName in fileNames)
                {
                    try
                    {
                        var filePath = fileName.TryGetLocalPath()!;
                        if (!File.Exists(filePath)) continue;
                        var fi = new FileInfo(filePath);
                        if(fi.Length > 5000000) continue; // 5Mb only!
                        sb.AppendLine(File.ReadAllText(fi.FullName));
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                        
                }
                    
                CommandText = sb.ToString();
            }
        });

        DataContext = this;
    }

    public void Clear()
    {
        TerminalText = DefaultTerminalText;
    }

    public async void SendCommand()
    {
        if (string.IsNullOrWhiteSpace(_commandText))
        {
            TerminalText += '\n';
            return;
        }

        var output = new StringBuilder(_terminalText);
        if (_verbose) output.AppendLine(_commandText);
            
        try
        {
            if (_scriptState is null)
            {
                _scriptState = CSharpScript.RunAsync(_commandText,
                    ScriptOptions.Default
                        .AddReferences(typeof(About).Assembly)
                        .AddImports(
                            "System",
                            "System.Collections.Generic",
                            "System.Math",
                            "System.IO",
                            "System.Linq",
                            "System.Threading",
                            "System.Threading.Tasks",
                            "UVtools.Core",
                            "UVtools.Core.EmguCV",
                            "UVtools.Core.Extensions",
                            "UVtools.Core.FileFormats",
                            "UVtools.Core.GCode",
                            "UVtools.Core.Gerber",
                            "UVtools.Core.Layers",
                            "UVtools.Core.Managers",
                            "UVtools.Core.MeshFormats",
                            "UVtools.Core.Objects",
                            "UVtools.Core.Operations",
                            "UVtools.Core.PixelEditor",
                            "UVtools.Core.Printer",
                            "UVtools.Core.Suggestions",
                            "UVtools.Core.SystemOS"
                        )
                        .WithAllowUnsafe(true), App.MainWindow).Result;
            }
            else
            {
                _scriptState = await _scriptState.ContinueWithAsync(_commandText);
            }

            DialogResult = DialogResults.OK;
            App.MainWindow.CanSave = true;

			if (_scriptState.ReturnValue is not null)
            {
                output.AppendLine(_scriptState.ReturnValue.ToString());
            }
            else if (_scriptState.Exception is not null)
            {
                output.AppendLine(_scriptState.Exception.ToString());
            }
            else if(!_verbose)
            {
                output.AppendLine(_commandText);
            }
        }
        catch (Exception e)
        {
            output.AppendLine(e.Message);
        }
            
        TerminalText = output.ToString();

        if (_clearCommandAfterSend) CommandText = string.Empty;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (DialogResult == DialogResults.OK)
        {
            App.MainWindow.ResetDataContext();
            App.MainWindow.ShowLayer();
        }
        base.OnClosed(e);
    }
}