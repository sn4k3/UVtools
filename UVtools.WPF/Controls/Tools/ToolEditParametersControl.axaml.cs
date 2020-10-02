using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Operations;
using UVtools.WPF.Windows;

namespace UVtools.WPF.Controls.Tools
{
    public class ToolEditParametersControl : ToolControl
    {
        public OperationEditParameters Operation { get; }

        public RowControl[] RowControls;

        public sealed class RowControl
        {
            public FileFormat.PrintParameterModifier Modifier { get; }

            public TextBlock Name { get; }
            public TextBlock OldValue { get; }
            public NumericUpDown NewValue { get; }
            public TextBlock Unit { get; }
            public Button ResetButton { get; }

            public RowControl(FileFormat.PrintParameterModifier modifier)
            {
                Modifier = modifier;

                modifier.NewValue = modifier.OldValue.Clamp(modifier.Minimum, modifier.Maximum);

                Name = new TextBlock
                {
                    Text = $"{modifier.Name}:",
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(15, 0),
                    Tag = this,
                };

                OldValue = new TextBlock
                {
                    Text = modifier.OldValue.ToString(CultureInfo.InvariantCulture),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    //Padding = new Thickness(15, 0),
                    Tag = this
                };

                NewValue = new NumericUpDown
                {
                    //DecimalPlaces = modifier.DecimalPlates,
                    VerticalAlignment = VerticalAlignment.Center,
                    Minimum = (double) modifier.Minimum,
                    Maximum = (double) modifier.Maximum,
                    Increment = modifier.DecimalPlates == 0 ? 1 : 0.01,
                    Value = (double)modifier.NewValue,
                    Tag = this,
                    Width = 100,
                };
                if (modifier.DecimalPlates > 0)
                {
                    NewValue.FormatString = "{0:#,0.00}";
                }

                Unit = new TextBlock
                {
                    Text = modifier.ValueUnit,
                    VerticalAlignment = VerticalAlignment.Center,
                    Padding = new Thickness(10, 0, 15, 0),
                    Tag = this
                };

                ResetButton = new Button
                {
                    IsVisible = false,
                    IsEnabled = false,
                    VerticalAlignment = VerticalAlignment.Center,
                    Tag = this,
                    Padding = new Thickness(5),
                    Content = new Image {Source = App.GetBitmapFromAsset("/Assets/Icons/undo-16x16.png")}
                };
                ResetButton.Click += ResetButtonOnClick;
                NewValue.ValueChanged += NewValueOnValueChanged;
            }

            private void NewValueOnValueChanged(object? sender, NumericUpDownValueChangedEventArgs e)
            {
                Modifier.NewValue = (decimal) NewValue.Value;
                ResetButton.IsVisible = ResetButton.IsEnabled = Modifier.HasChanged;
            }

            private void ResetButtonOnClick(object? sender, RoutedEventArgs e)
            {
                NewValue.Value = (double) Modifier.OldValue;
                NewValue.Focus();
            }
        }

        public ToolEditParametersControl()
        {
            InitializeComponent();

            App.SlicerFile.RefreshPrintParametersModifiersValues();
            BaseOperation = Operation = new OperationEditParameters(App.SlicerFile.PrintParameterModifiers);

            if (Operation.Modifiers is null || Operation.Modifiers.Length == 0)
            {
                CanRun = false;
                return;
            }

            Grid grid = this.FindControl<Grid>("grid");

            int rowIndex = 1;
            RowControls = new RowControl[Operation.Modifiers.Length];
            //table.RowCount = Operation.Modifiers.Length+1;
            foreach (var modifier in Operation.Modifiers)
            {
                grid.RowDefinitions.Add(new RowDefinition());
                byte column = 0;
                
                var rowControl = new RowControl(modifier);
                grid.Children.Add(rowControl.Name);
                grid.Children.Add(rowControl.OldValue);
                grid.Children.Add(rowControl.NewValue);
                grid.Children.Add(rowControl.Unit);
                grid.Children.Add(rowControl.ResetButton);
                Grid.SetRow(rowControl.Name, rowIndex);
                Grid.SetColumn(rowControl.Name, column++);

                Grid.SetRow(rowControl.OldValue, rowIndex);
                Grid.SetColumn(rowControl.OldValue, column++);

                Grid.SetRow(rowControl.NewValue, rowIndex);
                Grid.SetColumn(rowControl.NewValue, column++);

                Grid.SetRow(rowControl.Unit, rowIndex);
                Grid.SetColumn(rowControl.Unit, column++);

                Grid.SetRow(rowControl.ResetButton, rowIndex);
                Grid.SetColumn(rowControl.ResetButton, column++);
                /*table.Controls.Add(rowControl.Name, column++, rowIndex);
                table.Controls.Add(rowControl.OldValue, column++, rowIndex);
                table.Controls.Add(rowControl.NewValue, column++, rowIndex);
                table.Controls.Add(rowControl.Unit, column++, rowIndex);
                table.Controls.Add(rowControl.ResetButton, column++, rowIndex);
                */
                RowControls[rowIndex - 1] = rowControl;

                rowIndex++;
            }
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
                    ParentWindow.IsButton1Visible = true;
                    break;
                case ToolWindow.Callbacks.Button1:
                    foreach (var rowControl in RowControls)
                    {
                        rowControl.NewValue.Value = (double) rowControl.Modifier.OldValue;
                    }
                    break;
            }
        }
    }
}
