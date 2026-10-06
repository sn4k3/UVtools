using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Avalonia.Reactive;
using StageKit.Primitives.System;
using UVtools.Core;
using UVtools.Core.Operations;
using UVtools.Core.Scripting;
using UVtools.Core.SystemOS;
using UVtools.UI.Extensions;
using UVtools.UI.Windows;
using ZLinq;

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
                if(callback == ToolWindow.Callbacks.AfterLoadProfile) Dispatcher.UIThread.InvokeAsync(ReloadScript, DispatcherPriority.Loaded);
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

    public async Task LoadScript()
    {
        var files = await App.MainWindow.OpenFilePickerAsync(
            await App.MainWindow.StorageProvider.TryGetFolderFromPathAsync(UserSettings.Instance.General.DefaultDirectoryScripts!),
            AvaloniaStatic.ScriptsFileFilter);

        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } filePath) return;

        Operation.FilePath = filePath;
        await ReloadScript();
    }

    public async Task ReloadScript()
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
        HostSystem.ShowFileInFileManager(Operation.FilePath!);
    }

    public void OpenScriptFile()
    {
        if (!Operation.HaveFile) return;
        HostSystem.OpenFile(Operation.FilePath!);
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
            UseFloatingPlaceholder = true,
            PlaceholderText = "Script name, version and author"
        };

        TextBox tbScriptDescription = new()
        {
            IsReadOnly = true,
            Text = Operation.ScriptGlobals.Script.Description,
            AcceptsReturn = true,
            UseFloatingPlaceholder = true,
            PlaceholderText = "Script description"
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
                case ScriptNumericalInput<sbyte> numSBYTE: AddNumericInput(numSBYTE, i); continue;
                case ScriptNumericalInput<byte> numBYTE: AddNumericInput(numBYTE, i); continue;
                case ScriptNumericalInput<short> numSHORT: AddNumericInput(numSHORT, i); continue;
                case ScriptNumericalInput<ushort> numUSHORT: AddNumericInput(numUSHORT, i); continue;
                case ScriptNumericalInput<int> numINT: AddNumericInput(numINT, i); continue;
                case ScriptNumericalInput<uint> numUINT: AddNumericInput(numUINT, i); continue;
                case ScriptNumericalInput<long> numLONG: AddNumericInput(numLONG, i); continue;
                case ScriptNumericalInput<ulong> numULONG: AddNumericInput(numULONG, i); continue;
                case ScriptNumericalInput<float> numFLOAT: AddNumericInput(numFLOAT, i); continue;
                case ScriptNumericalInput<double> numDOUBLE: AddNumericInput(numDOUBLE, i); continue;
                case ScriptNumericalInput<decimal> numDECIMAL: AddNumericInput(numDECIMAL, i); continue;                case ScriptCheckBoxInput inputCheckBox:
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
                        var result = await App.MainWindow.SaveFilePickerAsync(inputSaveFile.Value, inputSaveFile.InitialFilename,
                            AvaloniaStatic.ToAvaloniaFileFilter(inputSaveFile.Filters),
                            inputSaveFile.Title);

                        if (result is not null)
                        {
                            var filePath = result.TryGetLocalPath();
                            if (!string.IsNullOrWhiteSpace(filePath))
                            {
                                inputSaveFile.Value = filePath;
                                control.Text = filePath;
                            }
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
                        var result = await App.MainWindow.OpenFilePickerAsync(inputOpenFile.Value, AvaloniaStatic.ToAvaloniaFileFilter(inputOpenFile.Filters), inputOpenFile.Title, inputOpenFile.AllowMultiple);
                        if (result.Count == 0) return;
                        inputOpenFile.Value = result[0].TryGetLocalPath();
                        inputOpenFile.Files = result.AsValueEnumerable().Select(file => file.TryGetLocalPath()).OfType<string>().ToArray();
                        control.Text = string.Join('\n', inputOpenFile.Files);
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

    /// <summary>
    /// Adds a numeric input of any supported numeric type to the variables grid.
    /// </summary>
    private void AddNumericInput<T>(ScriptNumericalInput<T> input, int index)
        where T : struct, INumber<T>, IMinMaxValue<T>
    {
        var isInteger = T.IsInteger(T.Zero);
        var decimalPlates = input.DecimalPlates;

        var minimum = decimal.CreateSaturating(input.Minimum);
        var maximum = decimal.CreateSaturating(input.Maximum);
        if (maximum <= minimum)
        {
            // The script did not configure a range, do not lock the value to a single number
            minimum = decimal.CreateSaturating(T.MinValue);
            maximum = decimal.CreateSaturating(T.MaxValue);
        }

        var increment = decimal.CreateSaturating(input.Increment);
        if (increment <= 0)
        {
            increment = isInteger ? 1 : 1m / (decimal)Math.Pow(10, Math.Min(decimalPlates, (byte)10));
        }

        NumericUpDown control = new()
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = decimal.CreateSaturating(input.Value),
            Increment = increment,
            MinWidth = 150
        };

        if (!isInteger && decimalPlates > 0)
        {
            control.FormatString = $"F{decimalPlates}";
        }

        control.GetObservable(NumericUpDown.ValueProperty).Subscribe(new AnonymousObserver<decimal?>(value =>
        {
            if (!value.HasValue) return;
            var rounded = isInteger ? value.Value : Math.Round(value.Value, Math.Min(decimalPlates, (byte)28));
            input.Value = T.CreateSaturating(rounded);
            control.Value = decimal.CreateSaturating(input.Value);
        }));

        ScriptVariablesGrid.Children.Add(control);
        Grid.SetRow(control, index * 2);
        Grid.SetColumn(control, 2);
    }
}