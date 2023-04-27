using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UVtools.Core;
using UVtools.Core.Operations;
using UVtools.Core.Scripting;
using UVtools.Core.SystemOS;
using UVtools.WPF.Extensions;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools;

public class ToolScriptingControl : ToolControl
{
    public OperationScripting Operation => BaseOperation as OperationScripting;

    private readonly StackPanel _scriptConfigurationPanel;
    private readonly Grid _scriptVariablesGrid;


    public ToolScriptingControl()
    {
        BaseOperation = new OperationScripting(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();
        _scriptConfigurationPanel = this.FindControl<StackPanel>("ScriptConfigurationPanel");
        _scriptVariablesGrid = this.FindControl<Grid>("ScriptVariablesGrid");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
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
                        ParentWindow.ButtonOkEnabled = Operation.CanExecute;
                    }
                };
                Operation.OnScriptReload += OnScriptReload;
                
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
        var dialog = new OpenFileDialog
        {
            AllowMultiple = false,
            Directory = UserSettings.Instance.General.DefaultDirectoryScripts,
            Filters = Helpers.ScriptsFileFilter,
        };

        var files = await dialog.ShowAsync(ParentWindow);
        if (files is null || files.Length == 0) return;

        Operation.FilePath = files[0];
        ReloadScript();
    }

    public async void ReloadScript()
    {
        try
        {
            ParentWindow.IsEnabled = false;

            await Task.Run(() => Operation.ReloadScriptFromFile());

            if (Operation.ScriptGlobals is not null && About.Version.CompareTo(Operation.ScriptGlobals.Script.MinimumVersionToRun) < 0)
            {
                await ParentWindow.MessageBoxError(
                    $"Unable to run due {About.Software} version {About.VersionStr} is lower than required {Operation.ScriptGlobals.Script.MinimumVersionToRun}\n" +
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

        _scriptConfigurationPanel.Children.Clear();
        _scriptVariablesGrid.Children.Clear();
        _scriptVariablesGrid.RowDefinitions.Clear();

        TextBox tbScriptName = new()
        {
            IsReadOnly = true,
            Text = $"{Operation.ScriptGlobals.Script.Name} | Version: {Operation.ScriptGlobals.Script.Version} by {Operation.ScriptGlobals.Script.Author}",
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

        _scriptConfigurationPanel.Children.Add(tbScriptName);
        _scriptConfigurationPanel.Children.Add(tbScriptDescription);

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

        _scriptVariablesGrid.RowDefinitions = RowDefinitions.Parse(rowDefinitions);

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

                _scriptVariablesGrid.Children.Add(tbLabel);
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

                _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        numSBYTE.Value = (sbyte)value;
                        control.Value = numSBYTE.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        numBYTE.Value = (byte)value;
                        control.Value = numBYTE.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        numSHORT.Value = (short)value;
                        control.Value = numSHORT.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        numUSHORT.Value = (ushort)value;
                        control.Value = numUSHORT.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        numINT.Value = (int)value;
                        control.Value = numINT.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        numUINT.Value = (uint)value;
                        control.Value = numUINT.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        numLONG.Value = (long)value;
                        control.Value = numLONG.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        numULONG.Value = (ulong)value;
                        control.Value = numULONG.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<float> numFLOAT:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numFLOAT.Minimum,
                        Maximum = numFLOAT.Maximum,
                        Value = numFLOAT.Value,
                        Increment = numFLOAT.Increment,
                        MinWidth = 150
                    };

                    if (numFLOAT.DecimalPlates > 0)
                    {
                        control.FormatString = $"F{numFLOAT.DecimalPlates}";
                    }

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(value =>
                    {
                        numFLOAT.Value = (float) Math.Round(value, numFLOAT.DecimalPlates);
                        control.Value = numFLOAT.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<double> numDOUBLE:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = numDOUBLE.Minimum,
                        Maximum = numDOUBLE.Maximum,
                        Value = numDOUBLE.Value,
                        Increment = numDOUBLE.Increment,
                        MinWidth = 150
                    };

                    if (numDOUBLE.DecimalPlates > 0)
                    {
                        control.FormatString = $"F{numDOUBLE.DecimalPlates}";
                    }

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(value =>
                    {
                        numDOUBLE.Value = Math.Round(value, numDOUBLE.DecimalPlates);
                        control.Value = numDOUBLE.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
                    Grid.SetRow(control, i * 2);
                    Grid.SetColumn(control, 2);

                    continue;
                }
                case ScriptNumericalInput<decimal> numDECIMAL:
                {
                    NumericUpDown control = new()
                    {
                        Minimum = (double)numDECIMAL.Minimum,
                        Maximum = (double)numDECIMAL.Maximum,
                        Value = (double)numDECIMAL.Value,
                        Increment = (double)numDECIMAL.Increment,
                        MinWidth = 150
                    };

                    if (numDECIMAL.DecimalPlates > 0)
                    {
                        control.FormatString = $"F{numDECIMAL.DecimalPlates}";
                    }

                    var valueProperty = control.GetObservable(NumericUpDown.ValueProperty);
                    valueProperty.Subscribe(value =>
                    {
                        numDECIMAL.Value = (decimal)Math.Round(value, numDECIMAL.DecimalPlates);
                        control.Value = (double)numDECIMAL.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        if (value != null) inputCheckBox.Value = value.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        if (value != null) inputToggleSwitch.Value = value.Value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                    valueProperty.Subscribe(value =>
                    {
                        inputTextBox.Value = value;
                    });

                    _scriptVariablesGrid.Children.Add(control);
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
                        var dialog = new OpenFolderDialog
                        {
                            Directory = inputOpenFolder.Value,
                            Title = inputOpenFolder.Title
                        };
                        var result = await dialog.ShowAsync(ParentWindow);
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            inputOpenFolder.Value = result;
                            control.Text = result;
                        }
                    };

                    panel.Children.Add(control);
                    panel.Children.Add(button);

                    _scriptVariablesGrid.Children.Add(panel);
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

                        var result = await dialog.ShowAsync(ParentWindow);
                        if (!string.IsNullOrWhiteSpace(result))
                        {
                            inputSaveFile.Value = result;
                            control.Text = result;
                        }
                    };

                    panel.Children.Add(control);
                    panel.Children.Add(button);

                    _scriptVariablesGrid.Children.Add(panel);
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

                        var result = await dialog.ShowAsync(ParentWindow);
                        if (result is null || result.Length == 0) return;
                        inputOpenFile.Value = result[0];
                        inputOpenFile.Files = result;
                        control.Text = string.Join('\n', result);
                    };

                    panel.Children.Add(control);
                    panel.Children.Add(button);

                    _scriptVariablesGrid.Children.Add(panel);
                    Grid.SetRow(panel, i * 2);
                    Grid.SetColumn(panel, 2);

                    continue;
                }
            }
        }

        //ParentWindow?.FitToSize();
    }
}