using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Reactive;
using UVtools.Core;
using UVtools.Core.Operations;
using UVtools.Core.Scripting;
using UVtools.Core.SystemOS;
using UVtools.UI.Extensions;
using UVtools.UI.Windows;

namespace UVtools.UI.Controls.Tools;

public partial class ToolScriptingControl : ToolControl
{
    public OperationScripting Operation => (BaseOperation as OperationScripting)!;

    public ToolScriptingControl()
    {
        BaseOperation = new OperationScripting(SlicerFile!);
        if (!ValidateSpawn()) return;
        InitializeComponent();
    }


    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                if(ParentWindow is not null) ParentWindow.ButtonOkEnabled = Operation.CanExecute;
                //ReloadGUI();
                if(callback == ToolWindow.Callbacks.AfterLoadProfile) Dispatcher.UIThread.Post(ReloadScript, DispatcherPriority.Loaded);
                Operation.PropertyChanged += (sender, e) =>
                {
                    if (e.PropertyName == nameof(Operation.CanExecute))
                    {
                        ParentWindow!.ButtonOkEnabled = Operation.CanExecute;
                    }
                };
                Operation.OnScriptReload += OnScriptReload!;
                
                break;
        }
    }

    private void OnScriptReload(object sender, EventArgs e)
    {
        if (!ReferenceEquals(sender, Operation))
        {
            return;
        }
        Dispatcher.UIThread.InvokeAsync(ReloadGUI);
    }

    public async void LoadScript()
    {
        var files = await App.MainWindow.OpenFilePickerAsync(
            await App.MainWindow.StorageProvider.TryGetFolderFromPathAsync(UserSettings.Instance.General.DefaultDirectoryScripts!),
            AvaloniaStatic.ScriptsFileFilter);

        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } filePath) return;

        Operation.FilePath = filePath;
        ReloadScript();
    }

    public async void ReloadScript()
    {
        if (ParentWindow is null) return;
        try
        {
            ParentWindow.IsEnabled = false;

            await Task.Run(() => Operation.ReloadScriptFromFile());

            if (Operation.ScriptGlobals is not null && About.Version.CompareTo(Operation.ScriptGlobals.Script.MinimumVersionToRun) < 0)
            {
                await ParentWindow.MessageBoxError(
                    $"Unable to run due {About.Software} version {About.VersionString} is lower than required {Operation.ScriptGlobals.Script.MinimumVersionToRun}\n" +
                    $"Please update {About.Software} in order to run this script.");
            }
        }
        catch (Exception e)
        {
            await ParentWindow.MessageBoxError(e.Message);
        }
        finally
        {
            ParentWindow.IsEnabled = true;
        }
            
    }

    public void OpenScriptFolder()
    {
        if (!Operation.HaveFile) return;
        SystemAware.SelectFileOnExplorer(Operation.FilePath!);
    }

    public void OpenScriptFile()
    {
        if (!Operation.HaveFile) return;
        SystemAware.StartProcess(Operation.FilePath!);
    }

    public void ReloadGUI()
    {
        if (!Operation.CanExecute) return;

        ScriptConfigurationPanel.Children.Clear();
        ScriptVariablesGrid.Children.Clear();
        ScriptVariablesGrid.RowDefinitions.Clear();

        TextBox tbScriptName = new()
        {
            IsReadOnly = true,
            Text = $"{Operation.ScriptGlobals!.Script.Name} | Version: {Operation.ScriptGlobals.Script.Version} by {Operation.ScriptGlobals.Script.Author}",
            UseFloatingWatermark = true,
            Watermark = "Script name, version and author"
        };

        TextBox tbScriptDescription = new()
        {
            IsReadOnly = true,
            Text = Operation.ScriptGlobals.Script.Description,
            AcceptsReturn = true,
            UseFloatingWatermark = true,
            Watermark = "Script description"
        };

        ScriptConfigurationPanel.Children.Add(tbScriptName);
        ScriptConfigurationPanel.Children.Add(tbScriptDescription);

        //Operation.ScriptGlobals.Script.UserInputs.Add(new ScriptBoolInput() { Label = "Hellow" });
        //Operation.ScriptGlobals.Script.UserInputs.Add(new ScriptTextBoxInput() { Label = "Hellow", Value = "m,e", MultiLine = true});
        if (Operation.ScriptGlobals.Script.UserInputs.Count == 0)
        {
            return;
        }

            
        string rowDefinitions = string.Empty;
        for (var i = 0; i < Operation.ScriptGlobals.Script.UserInputs.Count; i++)
        {
            if (i < Operation.ScriptGlobals.Script.UserInputs.Count - 1)
            {
                rowDefinitions += "Auto,10,";
            }
            else
            {
                rowDefinitions += "Auto";
            }
        }

        ScriptVariablesGrid.RowDefinitions = RowDefinitions.Parse(rowDefinitions);

        for (var i = 0; i < Operation.ScriptGlobals.Script.UserInputs.Count; i++)
        {
            var variable = Operation.ScriptGlobals.Script.UserInputs[i];

            if (!string.IsNullOrWhiteSpace(variable.Label) && variable is not ScriptCheckBoxInput and not ScriptToggleSwitchInput)
            {
                TextBlock tbLabel = new()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = $"{variable.Label}:"
                };

                if (!string.IsNullOrWhiteSpace(variable.ToolTip))
                {
                    ToolTip.SetTip(tbLabel, variable.ToolTip);
                }

                ScriptVariablesGrid.Children.Add(tbLabel);
                Grid.SetRow(tbLabel, i * 2);
                Grid.SetColumn(tbLabel, 0);
            }

            if (!string.IsNullOrWhiteSpace(variable.Unit))
            {
                TextBlock control = new()
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Text = variable.Unit
                };

                ScriptVariablesGrid.Children.Add(control);
                Grid.SetRow(control, i * 2);
                Grid.SetColumn(control, 4);
            }

            switch (variable)
            {
                case ScriptNumericalInput<sbyte> numSBYTE:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numSBYTE.Minimum,
                        Maximum = numSBYTE.Maximum,
                        Value = numSBYTE.Value,
                        Increment = numSBYTE.Increment,
                        MinWidth = 150
                    };

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if (!value.HasValue) return;
                        numSBYTE.Value = (sbyte)value;
                        control.Value = numSBYTE.Value;
                    }));

                    ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<byte> numBYTE:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numBYTE.Minimum,
                        Maximum = numBYTE.Maximum,
                        Value = numBYTE.Value,
                        Increment = numBYTE.Increment,
                        MinWidth = 150
                    };

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if (!value.HasValue) return;
                        numBYTE.Value = (byte)value;
                        control.Value = numBYTE.Value;
                    }));

                        ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<short> numSHORT:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numSHORT.Minimum,
                        Maximum = numSHORT.Maximum,
                        Value = numSHORT.Value,
                        Increment = numSHORT.Increment,
                        MinWidth = 150
                    };

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if (!value.HasValue) return;
                        numSHORT.Value = (short)value;
                        control.Value = numSHORT.Value;
                    }));

                        ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<ushort> numUSHORT:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numUSHORT.Minimum,
                        Maximum = numUSHORT.Maximum,
                        Value = numUSHORT.Value,
                        Increment = numUSHORT.Increment,
                        MinWidth = 150
                    };

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if (!value.HasValue) return;
                        numUSHORT.Value = (ushort)value;
                        control.Value = numUSHORT.Value;
                    }));

                        ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<int> numINT:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numINT.Minimum,
                        Maximum = numINT.Maximum,
                        Value = numINT.Value,
                        Increment = numINT.Increment,
                        MinWidth = 150
                    };

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if (!value.HasValue) return;
                        numINT.Value = (int)value;
                        control.Value = numINT.Value;
                    }));

                        ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<uint> numUINT:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numUINT.Minimum,
                        Maximum = numUINT.Maximum,
                        Value = numUINT.Value,
                        Increment = numUINT.Increment,
                        MinWidth = 150
                    };

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if (!value.HasValue) return;
                        numUINT.Value = (uint)value;
                        control.Value = numUINT.Value;
                    }));

                        ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<long> numLONG:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numLONG.Minimum,
                        Maximum = numLONG.Maximum,
                        Value = numLONG.Value,
                        Increment = numLONG.Increment,
                        MinWidth = 150
                    };

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if (!value.HasValue) return;
                        numLONG.Value = (long)value;
                        control.Value = numLONG.Value;
                    }));

                        ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<ulong> numULONG:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numULONG.Minimum,
                        Maximum = numULONG.Maximum,
                        Value = numULONG.Value,
                        Increment = numULONG.Increment,
                        MinWidth = 150
                    };

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if (!value.HasValue) return;
                        numULONG.Value = (ulong)value;
                        control.Value = numULONG.Value;
                    }));
                    
                    ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<float> numFLOAT:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = (decimal)numFLOAT.Minimum,
                        Maximum = (decimal)numFLOAT.Maximum,
                        Value = (decimal)numFLOAT.Value,
                        Increment = (decimal)numFLOAT.Increment,
                        MinWidth = 150
                    };

                    if (numFLOAT.DecimalPlates > 0)
                    {
                        control.FormatString = $"F{numFLOAT.DecimalPlates}";
                    }

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if(!value.HasValue) return;
                        numFLOAT.Value = (float)Math.Round((float)value, numFLOAT.DecimalPlates);
                        control.Value = (decimal)numFLOAT.Value;
                    }));

                    ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<double> numDOUBLE:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = (decimal)numDOUBLE.Minimum,
                        Maximum = (decimal)numDOUBLE.Maximum,
                        Value = (decimal)numDOUBLE.Value,
                        Increment = (decimal)numDOUBLE.Increment,
                        MinWidth = 150
                    };

                    if (numDOUBLE.DecimalPlates > 0)
                    {
                        control.FormatString = $"F{numDOUBLE.DecimalPlates}";
                    }

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if (!value.HasValue) return;
                        numDOUBLE.Value = Math.Round((double)value, numDOUBLE.DecimalPlates);
                        control.Value = (decimal)numDOUBLE.Value;
                    }));

                        ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<decimal> numDECIMAL:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numDECIMAL.Minimum,
                        Maximum = numDECIMAL.Maximum,
                        Value = numDECIMAL.Value,
                        Increment = numDECIMAL.Increment,
                        MinWidth = 150
                    };

                    if (numDECIMAL.DecimalPlates > 0)
                    {
                        control.FormatString = $"F{numDECIMAL.DecimalPlates}";
                    }

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(new AnonymousObserver<decimal?>(value =>
                    {
                        if (!value.HasValue) return;
                        numDECIMAL.Value = Math.Round(value.Value, numDECIMAL.DecimalPlates);
                        control.Value = numDECIMAL.Value;
                    }));

                    ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptCheckBoxInput inputCheckBox:
                {
                    var control = new CheckBox
                    {
                        Content = variable.Label,
                        IsChecked = inputCheckBox.Value
                    };

                    var valueProperty = control.GetObservable(CheckBox.IsCheckedProperty);
                    valueProperty.Subscribe(new AnonymousObserver<bool?>(value =>
                    {
                        if (value.HasValue) inputCheckBox.Value = value.Value;
                    }));

                    ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    if (!string.IsNullOrWhiteSpace(variable.ToolTip))
                    {
                        ToolTip.SetTip(control, variable.ToolTip);
                    }

                    continue;
                }
                case ScriptToggleSwitchInput inputToggleSwitch:
                {
                    var control = new ToggleSwitch
                    {
                        OnContent = inputToggleSwitch.OnText,
                        OffContent = inputToggleSwitch.OffText,
                        IsChecked = inputToggleSwitch.Value
                    };

                    var valueProperty = control.GetObservable(ToggleSwitch.IsCheckedProperty);
                    valueProperty.Subscribe(new AnonymousObserver<bool?>(value =>
                    {
                        if (value.HasValue) inputToggleSwitch.Value = value.Value;
                    }));

                    ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    if (!string.IsNullOrWhiteSpace(variable.ToolTip))
                    {
                        ToolTip.SetTip(control, variable.ToolTip);
                    }

                    continue;
                }
                case ScriptTextBoxInput inputTextBox:
                {
                    TextBox control = new()
                    {
                        AcceptsReturn = inputTextBox.MultiLine,
                        Text = inputTextBox.Value,
                    };

                    var valueProperty = control.GetObservable(TextBox.TextProperty);
                    valueProperty.Subscribe(new AnonymousObserver<string?>(value => inputTextBox.Value = value));

                    ScriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptOpenFolderDialogInput inputOpenFolder:
                {
                    var panel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5
                    };

                    var control = new TextBox
                    {
                        IsReadOnly = true,
                        Text = inputOpenFolder.Value,
                    };

                    var button = new Button
                    {
                        Content = "Select",
                    };

                    button.Click += async (sender, args) =>
                    {
                        var folders = await App.MainWindow.OpenFolderPickerAsync(inputOpenFolder.Value, inputOpenFolder.Title);

                        if (folders.Count <= 0 || folders[0].TryGetLocalPath() is not { } folderPath) return;
                        inputOpenFolder.Value = folderPath;
                        control.Text = folderPath;
                    };

                    panel.Children.Add(control);
                    panel.Children.Add(button);

                    ScriptVariablesGrid.Children.Add(panel);
                    Grid.SetRow(panel, i * 2);
                    Grid.SetColumn(panel, 2);

                    continue;
                }
                case ScriptSaveFileDialogInput inputSaveFile:
                {
                    var panel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5
                    };

                    var control = new TextBox
                    {
                        IsReadOnly = true,
                        Text = inputSaveFile.Value,
                    };

                    var button = new Button
                    {
                        Content = "Select",
                    };

                    button.Click += async (sender, args) =>
                    {
                        var dialog = new SaveFileDialog
                        {
                            Directory = inputSaveFile.Value,
                            Title = inputSaveFile.Title,
                            DefaultExtension = inputSaveFile.DefaultExtension,
                            InitialFileName = inputSaveFile.InitialFilename
                        };

                        if (inputSaveFile.Filters is not null)
                        {
                            foreach (var filter in inputSaveFile.Filters)
                            {
                                dialog.Filters.Add(new FileDialogFilter
                                {
                                    Extensions = filter.Extensions,
                                    Name = filter.Name
                                });
                            }
                        }

                        var result = await dialog.ShowAsync(ParentWindow!);
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            inputSaveFile.Value = result;
                            control.Text = result;
                        }
                    };

                    panel.Children.Add(control);
                    panel.Children.Add(button);

                    ScriptVariablesGrid.Children.Add(panel);
                    Grid.SetRow(panel, i * 2);
                    Grid.SetColumn(panel, 2);

                    continue;
                }
                case ScriptOpenFileDialogInput inputOpenFile:
                {
                    var panel = new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        Spacing = 5
                    };

                    var control = new TextBox
                    {
                        IsReadOnly = true,
                        Text = inputOpenFile.Value,
                        AcceptsReturn = true,
                    };

                    var button = new Button
                    {
                        Content = "Select",
                    };

                    button.Click += async (sender, args) =>
                    {
                        var dialog = new OpenFileDialog
                        {
                            Directory = inputOpenFile.Value,
                            Title = inputOpenFile.Title,
                            AllowMultiple = inputOpenFile.AllowMultiple,
                            InitialFileName = inputOpenFile.InitialFilename
                        };

                        if (inputOpenFile.Filters is not null)
                        {
                            foreach (var filter in inputOpenFile.Filters)
                            {
                                dialog.Filters.Add(new FileDialogFilter
                                {
                                    Extensions = filter.Extensions,
                                    Name = filter.Name
                                });
                            }
                        }

                        var result = await dialog.ShowAsync(ParentWindow!);
                        if (result is null || result.Length == 0) return;
                        inputOpenFile.Value = result[0];
                        inputOpenFile.Files = result;
                        control.Text = string.Join('\n', result);
                    };

                    panel.Children.Add(control);
                    panel.Children.Add(button);

                    ScriptVariablesGrid.Children.Add(panel);
                    Grid.SetRow(panel, i * 2);
                    Grid.SetColumn(panel, 2);

                    continue;
                }
            }
        }

        //ParentWindow?.FitToSize();
    }
}