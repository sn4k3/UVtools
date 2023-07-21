using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Metadata;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace UVtools.AvaloniaControls;

[TemplatePart("PART_PreviousButton", typeof(RepeatButton))]
[TemplatePart("PART_Text", typeof(SelectableTextBlock))]
[TemplatePart("PART_NextButton", typeof(RepeatButton))]
public class IndexSelector : TemplatedControl
{
    #region Members

    private RepeatButton? previousButton;
    private SelectableTextBlock? selectableTextBlock;
    private RepeatButton? nextButton;
    private int _count;
    private int _selectedIndex = -1;
    private int _selectedNumber;

    #endregion

    #region Avalonia Properties
    /// <summary>
    /// Defines the <see cref="Interval"/> property.
    /// </summary>
    public static readonly StyledProperty<int> IntervalProperty =
        RepeatButton.IntervalProperty.AddOwner<IndexSelector>();

    /// <summary>
    /// Defines the <see cref="Delay"/> property.
    /// </summary>
    public static readonly StyledProperty<int> DelayProperty =
        RepeatButton.DelayProperty.AddOwner<IndexSelector>();

    /// <summary>
    /// Defines the <see cref="ZeroLeading"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ZeroLeadingProperty =
        AvaloniaProperty.Register<IndexSelector, bool>(nameof(ZeroLeading));

    /// <summary>
    /// Defines the <see cref="FormatString"/> property.
    /// </summary>
    public static readonly StyledProperty<string> FormatStringProperty =
        AvaloniaProperty.Register<IndexSelector, string>(nameof(FormatString), "{0}/{1}");

    /// <summary>
    /// Defines the <see cref="AllowSelectNone"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> AllowNoneSelectProperty =
        AvaloniaProperty.Register<IndexSelector, bool>(nameof(AllowSelectNone));

    /// <summary>
    /// Defines the <see cref="SelectedIndex"/> property.
    /// </summary>
    public static readonly DirectProperty<IndexSelector, int> SelectedIndexProperty =
        SelectingItemsControl.SelectedIndexProperty.AddOwner<IndexSelector>(selector => selector.SelectedIndex,
            (selector, i) => selector.SelectedIndex = i, -1, BindingMode.TwoWay);

    /// <summary>
    /// Defines the <see cref="SelectedNumber"/> property.
    /// </summary>
    public static readonly DirectProperty<IndexSelector, int> SelectedNumberProperty =
        AvaloniaProperty.RegisterDirect<IndexSelector, int>(nameof(SelectedNumber), indexSelector => indexSelector.SelectedNumber);

    /// <summary>
    /// Defines the <see cref="CountProperty"/> property.
    /// </summary>
    public static readonly DirectProperty<IndexSelector, int> CountProperty =
        AvaloniaProperty.RegisterDirect<IndexSelector, int>(nameof(Count), indexSelector => indexSelector.Count,
            (indexSelector, v) => indexSelector.Count = v);

    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the amount of time, in milliseconds, of repeating clicks.
    /// </summary>
    public int Interval
    {
        get => GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    /// <summary>
    /// Gets or sets the amount of time, in milliseconds, to wait before repeating begins.
    /// </summary>
    public int Delay
    {
        get => GetValue(DelayProperty);
        set => SetValue(DelayProperty, value);
    }

    /// <summary>
    /// Gets or sets if the text should have zero leading index based on <see cref="Count"/>
    /// </summary>
    public bool ZeroLeading
    {
        get => GetValue(ZeroLeadingProperty);
        set => SetValue(ZeroLeadingProperty, value);
    }

    /// <summary>
    /// Gets or sets the display format.
    /// </summary>
    public string FormatString
    {
        get => GetValue(FormatStringProperty);
        set => SetValue(FormatStringProperty, value);
    }

    /// <summary>
    /// Gets or sets if is possible to have no selected index (-1)
    /// </summary>
    public bool AllowSelectNone
    {
        get => GetValue(AllowNoneSelectProperty);
        set => SetValue(AllowNoneSelectProperty, value);
    }

    /// <summary>
    /// Gets or sets the selected index
    /// </summary>
    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            if (_count == 0 || AllowSelectNone)
            {
                value = Math.Clamp(value, -1, _count - 1);
            }
            else
            {
                value = Math.Clamp(value, 0, _count - 1);
            }

            if(!SetAndRaise(SelectedIndexProperty, ref _selectedIndex, value)) return;
            SelectedNumber = value + 1;
            UpdateControl();
        }
    }

    public int SelectedNumber
    {
        get => _selectedNumber;
        private set => SetAndRaise(SelectedNumberProperty, ref _selectedNumber, value);
    }

    /// <summary>
    /// Gets or sets the count of items.
    /// </summary>
    public int Count
    {
        get => _count;
        set
        {
            value = Math.Max(0, value);
            if (!SetAndRaise(CountProperty, ref _count, value)) return;
            var oldSelectedIndex = SelectedIndex;
            SelectedIndex = oldSelectedIndex;
            UpdateControl();
        }
    }

    #endregion

    #region Constructor
    public IndexSelector()
    {
    }
    #endregion

    #region Overrides
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        if (previousButton is not null)
        {
            previousButton.Click -= PreviousButtonOnClick;
            nextButton!.Click -= NextButtonOnClick;
        }


        previousButton = e.NameScope.Find<RepeatButton>("PART_PreviousButton")!;
        selectableTextBlock = e.NameScope.Find<SelectableTextBlock>("PART_Text")!;
        nextButton = e.NameScope.Find<RepeatButton>("PART_NextButton")!;

        
        previousButton.Click += PreviousButtonOnClick;
        nextButton.Click += NextButtonOnClick;

        UpdateControl();
    }
    #endregion

    #region Methods
    private void PreviousButtonOnClick(object? sender, RoutedEventArgs e)
    {
        SelectPrevious();
    }

    private void NextButtonOnClick(object? sender, RoutedEventArgs e)
    {
        SelectNext();
    }


    private void UpdateControl()
    {
        if (previousButton is null) return;
        previousButton.IsEnabled = (_count > 0 && _selectedIndex > 0) || (AllowSelectNone && _selectedIndex >= 0);
        nextButton!.IsEnabled = _count > 0 && _selectedIndex < _count - 1;
        
        selectableTextBlock!.Text = ZeroLeading 
                ? string.Format(FormatString, (_selectedIndex + 1).ToString($"D{DigitCount(_count)}"), _count)
                : string.Format(FormatString, _selectedIndex + 1, _count);
    }

    public void SelectPrevious()
    {
        SelectedIndex--;
    }

    public void SelectNext()
    {
        SelectedIndex++;
    }



    private int DigitCount(int n)
    {
        if (n >= 0)
        {
            return n switch
            {
                < 10 => 1,
                < 100 => 2,
                < 1000 => 3,
                < 10000 => 4,
                < 100000 => 5,
                < 1000000 => 6,
                < 10000000 => 7,
                < 100000000 => 8,
                < 1000000000 => 9,
                _ => 10
            };
        }

        return n switch
        {
            > -10 => 1,
            > -100 => 2,
            > -1000 => 3,
            > -10000 => 4,
            > -100000 => 5,
            > -1000000 => 6,
            > -10000000 => 7,
            > -100000000 => 8,
            > -1000000000 => 9,
            _ => 10
        };
    }
    #endregion
}