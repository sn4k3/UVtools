using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Projektanker.Icons.Avalonia;
using UVtools.AvaloniaControls;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools;

public class ToolEditParametersControl : ToolControl
{
    public OperationEditParameters Operation => BaseOperation as OperationEditParameters;

    public RowControl[] RowControls;
    private Grid globalGrid;
    private Grid perLayerGrid;

    public sealed class RowControl
    {
        public FileFormat.PrintParameterModifier Modifier { get; }

        public Control FirstColumn;

        public TextBlock Name { get; }
        public ExtendedNumericUpDown NumericUpDown { get; }

        public RowControl(FileFormat.PrintParameterModifier modifier)
        {
            Modifier = modifier;

            modifier.NewValue = Math.Clamp(modifier.OldValue, modifier.Minimum, modifier.Maximum);
            var label = ReferenceEquals(modifier, FileFormat.PrintParameterModifier.BottomLayerCount)
                ? modifier.Name:
                modifier.Name.Replace("Bottom ", string.Empty, StringComparison.InvariantCultureIgnoreCase).FirstCharToUpper();

            var stackPanel = new StackPanel{ Orientation = Orientation.Horizontal, Spacing = 5 };

            if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.PositionZ))
            {
                stackPanel.Children.Add(new Icon { Value = "fa-solid fa-stairs" });
            }
            else if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.BottomLayerCount))
            {
                stackPanel.Children.Add(new Icon { Value = "fa-solid fa-fire-burner" });
            }
            else if (ReferenceEquals(modifier, FileFormat.PrintParameterModifier.TransitionLayerCount))
            {
                stackPanel.Children.Add(new Icon { Value = "fa-solid fa-layer-group" });
            }
            else if (label.Contains("delay", StringComparison.InvariantCultureIgnoreCase) || label.Contains("wait", StringComparison.InvariantCultureIgnoreCase))
            {
                stackPanel.Children.Add(new Icon { Value = "fa-solid fa-stopwatch" });
            }
            else if (label.Contains("exposure", StringComparison.InvariantCultureIgnoreCase))
            {
                stackPanel.Children.Add(new Icon { Value = "fa-regular fa-eye" });
            }
            else if (label.Contains("lift", StringComparison.InvariantCultureIgnoreCase))
            {
                stackPanel.Children.Add(new Icon { Value = "fa-solid fa-arrow-up-from-bracket" });
            }
            else if (label.Contains("retract", StringComparison.InvariantCultureIgnoreCase))
            {
                stackPanel.Children.Add(new Icon { Value = "fa-solid fa-arrows-down-to-line" });
            }
            else if (label.Contains("pwm", StringComparison.InvariantCultureIgnoreCase))
            {
                stackPanel.Children.Add(new Icon { Value = "fa-regular fa-sun" });
            }

            Name = new TextBlock
            {
                Text = $"{label}:",
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(0,0,10, 0),
                Tag = this,
            };

            stackPanel.Children.Add(Name);

            FirstColumn = stackPanel;



            if (!string.IsNullOrWhiteSpace(modifier.Description)) ToolTip.SetTip(Name, modifier.Description);

            NumericUpDown = new ExtendedNumericUpDown
            {
                //DecimalPlaces = modifier.DecimalPlates,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0,2,0,0),
                Minimum = (double) modifier.Minimum,
                Maximum = (double) modifier.Maximum,
                Increment = modifier.Increment,
                FormatString = modifier.DecimalPlates > 0 ? $"F{modifier.DecimalPlates}" : string.Empty,
                Value = (double)modifier.NewValue,
                ValueUnit = modifier.ValueUnit,
                IsInitialValueVisible = true,
                ResetVisibility = ExtendedNumericUpDown.ResetVisibilityType.Auto,
                Tag = this,
                //Width = 100,
                MinWidth = 320,
                ClipValueToMinMax = true
            };

            NumericUpDown.ValueChanged += NewValueOnValueChanged;
        }

        private void NewValueOnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
        {
            Modifier.NewValue = (decimal) NumericUpDown.Value;
        }
    }

    private static (FileFormat.PrintParameterModifier modifierLeft, FileFormat.PrintParameterModifier modifierRight)[] TSMCModifierPairs =
        {
            new (FileFormat.PrintParameterModifier.BottomLiftHeight, FileFormat.PrintParameterModifier.BottomLiftHeight2),
            new (FileFormat.PrintParameterModifier.BottomLiftSpeed, FileFormat.PrintParameterModifier.BottomLiftSpeed2),
            //new (FileFormat.PrintParameterModifier.BottomRetractHeight, FileFormat.PrintParameterModifier.BottomRetractHeight2),
            new (FileFormat.PrintParameterModifier.BottomRetractSpeed, FileFormat.PrintParameterModifier.BottomRetractSpeed2),

            new (FileFormat.PrintParameterModifier.LiftHeight, FileFormat.PrintParameterModifier.LiftHeight2),
            new (FileFormat.PrintParameterModifier.LiftSpeed, FileFormat.PrintParameterModifier.LiftSpeed2),
            //new (FileFormat.PrintParameterModifier.RetractHeight, FileFormat.PrintParameterModifier.RetractHeight2),
            new (FileFormat.PrintParameterModifier.RetractSpeed, FileFormat.PrintParameterModifier.RetractSpeed2),
        };

    public ToolEditParametersControl()
    {
        BaseOperation = new OperationEditParameters(SlicerFile);
        if (!ValidateSpawn()) return;
        InitializeComponent();

        globalGrid = this.FindControl<Grid>("GlobalGrid");
        perLayerGrid = this.FindControl<Grid>("PerLayerGrid");
    }

    public void PopulateGrid()
    {
        var grid = Operation.PerLayerOverride ? perLayerGrid : globalGrid;

        var gridCols = 2 - Convert.ToInt32(Operation.PerLayerOverride);
        if (grid.Children.Count > gridCols)
        {
            grid.Children.RemoveRange(gridCols, grid.Children.Count - gridCols);
        }
        if (grid.RowDefinitions.Count > 1)
        {
            grid.RowDefinitions.RemoveRange(1, grid.RowDefinitions.Count - 1);
        }

        RowControls = new RowControl[Operation.Modifiers.Length];
        var addedModifiers = new List<FileFormat.PrintParameterModifier>();

        var controlIndex = 0;
        var rowDict = new Dictionary<bool, int>
        {
            { true, 1 },
            { false, 1 },
        };

        Control CreateTSMCfields(RowControl left, RowControl right)
        {
            var grid = new Grid
            {
                ColumnDefinitions = Operation.PerLayerOverride 
                    ? new ColumnDefinitions("*,Auto,*")
                    : new ColumnDefinitions("Auto,Auto,*")
            };

            var textBox = new TextBlock
            {
                Text = ">",
                Margin = new Thickness(5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            left.NumericUpDown.MinWidth /= 2;
            left.NumericUpDown.ShowButtonSpinner = false;
            right.NumericUpDown.MinWidth /= 2;
            right.NumericUpDown.ShowButtonSpinner = false;

            if (!Operation.PerLayerOverride)
            {
                left.NumericUpDown.ValueUnit = null;
                left.NumericUpDown.IsInitialValueVisible = false;
                left.NumericUpDown.Width = 110;

                right.NumericUpDown.IsInitialValueVisible = false;
                right.NumericUpDown.Width = 180;
            }

            grid.Children.Add(left.NumericUpDown);
            grid.Children.Add(textBox);
            grid.Children.Add(right.NumericUpDown);

            Grid.SetColumn(left.NumericUpDown, 0);
            Grid.SetColumn(textBox, 1);
            Grid.SetColumn(right.NumericUpDown, 2);

            return grid;
        }

        foreach (var modifier in Operation.Modifiers)
        {
            if(addedModifiers.Contains(modifier)) continue;

            grid.RowDefinitions.Add(new RowDefinition());

            bool isBottomLayer = !Operation.PerLayerOverride && modifier.Name.Contains("Bottom");
            int column = isBottomLayer || Operation.PerLayerOverride ? 0 : 3;

            var rowControl1 = new RowControl(modifier);
            RowControl? rowControl2 = null;
            grid.Children.Add(rowControl1.FirstColumn);

            Control valueContainer = rowControl1.NumericUpDown;
            
            Grid.SetRow(rowControl1.FirstColumn, rowDict[isBottomLayer]);
            Grid.SetColumn(rowControl1.FirstColumn, column++);

            foreach (var modifierPair in TSMCModifierPairs)
            {
                if (!ReferenceEquals(modifierPair.modifierLeft, modifier)) continue;
                if(!Operation.Modifiers.Contains(modifierPair.modifierRight)) break;

                rowControl2 = new RowControl(modifierPair.modifierRight);
                valueContainer = CreateTSMCfields(rowControl1, rowControl2);

                break;
            }
            
            if (rowControl2 is null && ReferenceEquals(modifier, FileFormat.PrintParameterModifier.BottomRetractHeight2))
            {
                rowControl1.Name.Text = rowControl1.Name.Text.Replace("2) ", string.Empty).FirstCharToUpper();
                var rowControlVirtual = new RowControl(FileFormat.PrintParameterModifier.BottomRetractHeight2.Clone())
                    {
                        NumericUpDown =
                        {
                            Value = Operation.PerLayerOverride
                                ? SlicerFile[Operation.LayerIndexStart].RetractHeight
                                : SlicerFile.BottomRetractHeight,
                            IsReadOnly = true,
                            IsEnabled = false
                        }
                    };
                if (Operation.PerLayerOverride)
                {
                    rowControlVirtual.NumericUpDown.IsInitialValueVisible = false;
                }
                rowControlVirtual.NumericUpDown.RedefineOldValue();
                valueContainer = CreateTSMCfields(rowControlVirtual, rowControl1);
            }
            else if (rowControl2 is null && ReferenceEquals(modifier, FileFormat.PrintParameterModifier.RetractHeight2))
            {
                rowControl1.Name.Text = rowControl1.Name.Text.Replace("2) ", string.Empty).FirstCharToUpper();
                var rowControlVirtual = new RowControl(FileFormat.PrintParameterModifier.RetractHeight2.Clone())
                {
                    NumericUpDown =
                    {
                        Value = Operation.PerLayerOverride
                            ? SlicerFile[Operation.LayerIndexStart].RetractHeight
                            : SlicerFile.RetractHeight,
                        IsReadOnly = true,
                        IsEnabled = false
                    }
                };
                if (Operation.PerLayerOverride)
                {
                    rowControlVirtual.NumericUpDown.IsInitialValueVisible = false;
                }
                rowControlVirtual.NumericUpDown.RedefineOldValue();
                valueContainer = CreateTSMCfields(rowControlVirtual, rowControl1);
            }


            grid.Children.Add(valueContainer);
            Grid.SetRow(valueContainer, rowDict[isBottomLayer]);
            Grid.SetColumn(valueContainer, column);

            RowControls[controlIndex++] = rowControl1;
            addedModifiers.Add(modifier);
            if (rowControl2 is not null)
            {
                RowControls[controlIndex++] = rowControl2;
                addedModifiers.Add(rowControl2.Modifier);
            }
            rowDict[isBottomLayer]++;
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnInitialized()
    {
        ParentWindow.CloseWindowAfterProcess = false;
    }

    public void RefreshModifiers()
    {
        if (Operation.PerLayerOverride)
        {
            Operation.Modifiers = SlicerFile.PrintParameterPerLayerModifiers;
            SlicerFile.RefreshPrintParametersPerLayerModifiersValues(Operation.LayerIndexStart);
        }
        else
        {
            Operation.Modifiers = SlicerFile.PrintParameterModifiers;
            SlicerFile.RefreshPrintParametersModifiersValues();
        }
    }

    public override void Callback(ToolWindow.Callbacks callback)
    {
        switch (callback)
        {
            case ToolWindow.Callbacks.Init:
            case ToolWindow.Callbacks.AfterLoadProfile:
                if (callback is ToolWindow.Callbacks.Init)
                {
                    ParentWindow.SelectCurrentLayer();
                    ParentWindow.LayerRangeSync = true;
                }

                PopulateGrid();
                Operation.PropertyChanged += OperationOnPropertyChanged;
                break;
        }
    }

    private void OperationOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Operation.LayerIndexStart) && Operation.PerLayerOverride)
        {
            SlicerFile.RefreshPrintParametersPerLayerModifiersValues(Operation.LayerIndexStart);
            PopulateGrid();
            return;
        }
        if (e.PropertyName == nameof(Operation.PerLayerOverride))
        {
            RefreshModifiers();
            ParentWindow.LayerRangeVisible = Operation.PerLayerOverride;
            PopulateGrid();
            return;
        }
    }

    public override Task<bool> OnAfterProcess()
    {
        Operation.Execute();
        RefreshModifiers();
        PopulateGrid();
        return Task.FromResult(true);
    }
}