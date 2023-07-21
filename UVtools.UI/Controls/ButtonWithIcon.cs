/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.Collections.Generic;

namespace UVtools.UI.Controls;

public class ButtonWithIcon : Button
{
    public enum IconPlacementType : byte
    {
        Left,
        Right,
        Top,
        Bottom
    }

    protected override Type StyleKeyOverride => typeof(Button);

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ButtonWithIcon, string?>(nameof(Text));

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<IconPlacementType> IconPlacementProperty =
        AvaloniaProperty.Register<ButtonWithIcon, IconPlacementType>(nameof(IconPlacement));

    public IconPlacementType IconPlacement
    {
        get => GetValue(IconPlacementProperty);
        set => SetValue(IconPlacementProperty, value);
    }

    public static readonly StyledProperty<string?> IconProperty =
        AvaloniaProperty.Register<ButtonWithIcon, string?>(nameof(Icon));

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<ButtonWithIcon, double>(nameof(Spacing), 10);

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public ButtonWithIcon()
    {
    }
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (ReferenceEquals(e.Property, TextProperty)
            || ReferenceEquals(e.Property, IconProperty)
            || ReferenceEquals(e.Property, IconPlacementProperty))
        {
            RebuildContent();
        }
    }

    public Projektanker.Icons.Avalonia.Icon MakeIcon()
    {
        return new Projektanker.Icons.Avalonia.Icon { Value = Icon };
    }

    private void RebuildContent()
    {
        if (string.IsNullOrWhiteSpace(Icon))
        {
            if (!string.IsNullOrWhiteSpace(Text))
            {
                Content = Text;
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(Text))
        {
            if (!string.IsNullOrWhiteSpace(Icon))
            {
                Content = MakeIcon();
            }

            return;
        }

        var panel = new StackPanel
        {
            Spacing = Spacing,
            VerticalAlignment = VerticalAlignment.Stretch,
            Orientation = IconPlacement is IconPlacementType.Left or IconPlacementType.Right 
                ? Orientation.Horizontal
                : Orientation.Vertical
        };

        if(IconPlacement is IconPlacementType.Left or IconPlacementType.Top) panel.Children.Add(MakeIcon());
        panel.Children.Add(new TextBlock{ VerticalAlignment = VerticalAlignment.Center, Text = Text});
        if (IconPlacement is IconPlacementType.Right or IconPlacementType.Bottom) panel.Children.Add(MakeIcon());

        Content = panel;
    }
}