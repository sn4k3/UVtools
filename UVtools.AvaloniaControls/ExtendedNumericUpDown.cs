/*
*                               The MIT License (MIT)
* Permission is hereby granted, free of charge, to any person obtaining a copy of
* this software and associated documentation files (the "Software"), to deal in
* the Software without restriction, including without limitation the rights to
* use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
* the Software, and to permit persons to whom the Software is furnished to do so.
*/

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using System;

namespace UVtools.AvaloniaControls;

/// <summary>
/// Extends the <see cref="NumericUpDown"/> with initial value, reset and value unit text
/// </summary>
[TemplatePart("PART_OldTextBox", typeof(TextBox))]
[TemplatePart("PART_ValueUnitTextBox", typeof(TextBox))]
[TemplatePart("PART_ResetButton", typeof(Button))]
public class ExtendedNumericUpDown : NumericUpDown
{
    #region Enums

    public enum ResetVisibilityType
    {
        /// <summary>
        /// Hidden
        /// </summary>
        Hidden,

        /// <summary>
        /// Always visible
        /// </summary>
        Visible,

        /// <summary>
        /// Visible if it's possible to reset the value, otherwise hidden
        /// </summary>
        Auto
    }

    #endregion

    #region Members
    private bool _firstTime = true;
    private decimal _initialValue;
    private string? _initialText;
    private bool _isResetEnabled;
    private bool _isResetVisible;
    #endregion

    #region Avalonia Properties
    /// <summary>
    /// Defines the <see cref="IsInitialValueVisible"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsInitialValueVisibleProperty =
        AvaloniaProperty.Register<ExtendedNumericUpDown, bool>(nameof(IsInitialValueVisible));

    /// <summary>
    /// Defines the <see cref="InitialText"/> property.
    /// </summary>
    public static readonly DirectProperty<ExtendedNumericUpDown, string?> InitialTextProperty =
        AvaloniaProperty.RegisterDirect<ExtendedNumericUpDown, string?>(nameof(InitialText), updown => updown.InitialText,
            (updown, v) => updown.InitialText = v);

    /// <summary>
    /// Defines the <see cref="InitialValue"/> property.
    /// </summary>
    public static readonly DirectProperty<ExtendedNumericUpDown, decimal> InitialValueProperty =
        AvaloniaProperty.RegisterDirect<ExtendedNumericUpDown, decimal>(nameof(InitialValue), updown => updown.InitialValue,
            (updown, v) => updown.InitialValue = v);

    /// <summary>
    /// Defines the <see cref="ValueUnit"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> ValueUnitProperty =
        AvaloniaProperty.Register<ExtendedNumericUpDown, string?>(nameof(ValueUnit));

    /// <summary>
    /// Defines the <see cref="IsResetVisible"/> property.
    /// </summary>
    public static readonly DirectProperty<ExtendedNumericUpDown, bool> IsResetVisibleProperty =
        AvaloniaProperty.RegisterDirect<ExtendedNumericUpDown, bool>(nameof(IsResetVisible), updown => updown.IsResetVisible,
            (updown, v) => updown.IsResetVisible = v);

    /// <summary>
    /// Defines the <see cref="ResetVisibility"/> property.
    /// </summary>
    public static readonly StyledProperty<ResetVisibilityType> ResetVisibilityProperty =
        AvaloniaProperty.Register<ExtendedNumericUpDown, ResetVisibilityType>(nameof(ResetVisibility));

    /// <summary>
    /// Defines the <see cref="IsResetEnabled"/> property.
    /// </summary>
    public static readonly DirectProperty<ExtendedNumericUpDown, bool> IsResetEnabledProperty =
        AvaloniaProperty.RegisterDirect<ExtendedNumericUpDown, bool>(nameof(IsResetEnabled), updown => updown.IsResetEnabled,
            (updown, v) => updown.IsResetEnabled = v);
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets if the initial value should be visible
    /// </summary>
    public bool IsInitialValueVisible
    {
        get => GetValue(IsInitialValueVisibleProperty);
        set => SetValue(IsInitialValueVisibleProperty, value);
    }

    /// <summary>
    /// Gets or sets the initial value.
    /// </summary>
    public decimal InitialValue
    {
        get => _initialValue;
        private set => SetAndRaise(InitialValueProperty, ref _initialValue, value);
    }

    /// <summary>
    /// Gets or sets the formatted string representation of the <see cref="InitialValue"/>.
    /// </summary>
    public string? InitialText
    {
        get => _initialText;
        private set => SetAndRaise(InitialTextProperty, ref _initialText, value);
    }

    /// <summary>
    /// Gets or sets the value unit text
    /// </summary>
    public string? ValueUnit
    {
        get => GetValue(ValueUnitProperty);
        set => SetValue(ValueUnitProperty, value);
    }

    /// <summary>
    /// Gets if the reset button is visible
    /// </summary>
    public bool IsResetVisible
    {
        get => _isResetVisible;
        private set => SetAndRaise(IsResetVisibleProperty, ref _isResetVisible, value);
    }

    /// <summary>
    /// <para>Gets or sets if the reset button should auto show (When Value is different from <see cref="InitialValue"/>) and auto hide (When Value is equal to <see cref="InitialValue"/>).</para>
    /// <para>The <see cref="IsResetVisible"/> property will auto change.</para>
    /// </summary>
    public ResetVisibilityType ResetVisibility
    {
        get => GetValue(ResetVisibilityProperty);
        set
        {
            SetValue(ResetVisibilityProperty, value);
            SetResetVisibility();
        }
    }

    /// <summary>
    /// Gets if the reset button is enable, aka ready to click (Value != <see cref="InitialValue"/>)
    /// </summary>
    public bool IsResetEnabled
    {
        get => _isResetEnabled;
        private set
        {
            if(!SetAndRaise(IsResetEnabledProperty, ref _isResetEnabled, value)) return;
            if (ResetVisibility == ResetVisibilityType.Auto)
            {
                IsResetVisible = _isResetEnabled;
            }
        }
    }

    #endregion

    #region Constructor
    /// <summary>
    /// Constructor
    /// </summary>
    public ExtendedNumericUpDown()
    {
    }
    #endregion

    #region Overrides

    protected override void OnTextChanged(string? oldValue, string? newValue)
    {
        base.OnTextChanged(oldValue, newValue);
        if (_firstTime)
        {
            RedefineOldValue();
            _firstTime = false;
        }
        else
        {
            IsResetEnabled = !string.Equals(newValue, _initialText, StringComparison.Ordinal);
        }
    }
    #endregion

    #region Methods

    /// <summary>
    /// Sets the <see cref="InitialValue"/> to the current Value
    /// </summary>
    public void RedefineOldValue()
    {
        InitialValue = Value ?? 0;
        InitialText = Text;
        IsResetEnabled = false;
    }

    /// <summary>
    /// Resets the Value with <see cref="InitialValue"/>
    /// </summary>
    public void ResetValue()
    {
        Value = _initialValue;
        IsResetEnabled = false;
    }


    private void SetResetVisibility()
    {
        IsResetVisible = ResetVisibility switch
        {
            ResetVisibilityType.Hidden => false,
            ResetVisibilityType.Visible => true,
            ResetVisibilityType.Auto => IsResetEnabled,
            _ => throw new ArgumentOutOfRangeException(nameof(ResetVisibility), ResetVisibility, "Value not processed."),
        };
    }
    #endregion
}