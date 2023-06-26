/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using Avalonia;
using Avalonia.Controls;
using System;

namespace UVtools.AvaloniaControls;

/// <summary>
/// Extends the <see cref="NumericUpDown"/> with initial value, reset and value unit text
/// </summary>
public class ExtendedNumericUpDown : NumericUpDown
{
    #region Members
    private bool _firstTime = true;
    private double _initialValue;
    private string? _initialText;
    private bool _isResetEnabled;
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
    public static readonly DirectProperty<ExtendedNumericUpDown, double> InitialValueProperty =
        AvaloniaProperty.RegisterDirect<ExtendedNumericUpDown, double>(nameof(InitialValue), updown => updown.InitialValue,
            (updown, v) => updown.InitialValue = v);

    /// <summary>
    /// Defines the <see cref="ValueUnit"/> property.
    /// </summary>
    public static readonly StyledProperty<string?> ValueUnitProperty =
        AvaloniaProperty.Register<ExtendedNumericUpDown, string?>(nameof(ValueUnit));

    /// <summary>
    /// Defines the <see cref="IsResetVisible"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> IsResetVisibleProperty =
        AvaloniaProperty.Register<ExtendedNumericUpDown, bool>(nameof(IsResetVisible));

    /// <summary>
    /// Defines the <see cref="ResetAutoVisibility"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> ResetAutoVisibilityProperty =
        AvaloniaProperty.Register<ExtendedNumericUpDown, bool>(nameof(ResetAutoVisibility));

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
    public double InitialValue
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
    /// Gets or sets if the reset button should be visible
    /// </summary>
    public bool IsResetVisible
    {
        get => GetValue(IsResetVisibleProperty);
        set => SetValue(IsResetVisibleProperty, value);
    }

    /// <summary>
    /// <para>Gets or sets if the reset button should auto show (When Value is different from <see cref="InitialValue"/>) and auto hide (When Value is equal to <see cref="InitialValue"/>).</para>
    /// <para>The <see cref="IsResetVisible"/> property will auto change.</para>
    /// </summary>
    public bool ResetAutoVisibility
    {
        get => GetValue(ResetAutoVisibilityProperty);
        set => SetValue(ResetAutoVisibilityProperty, value);
    }

    /// <summary>
    /// Gets if the reset button is enable, aka ready to click (Value != <see cref="InitialValue"/>)
    /// </summary>
    public bool IsResetEnabled
    {
        get => _isResetEnabled;
        private set
        {
            SetAndRaise(IsResetEnabledProperty, ref _isResetEnabled, value);
            if (ResetAutoVisibility)
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

    protected override void OnTextChanged(string oldValue, string newValue)
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
        InitialValue = Value;
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
    #endregion
}