using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using System;

namespace UVtools.UI.Controls;

public class ToggleButtonWithIcon : ToggleButton
{
    protected override Type StyleKeyOverride => typeof(ToggleButton);

    public static readonly StyledProperty<string?> TextProperty =
        ButtonWithIcon.TextProperty.AddOwner<ToggleButtonWithIcon>();

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<ButtonWithIcon.IconPlacementType> IconPlacementProperty =
        ButtonWithIcon.IconPlacementProperty.AddOwner<ToggleButtonWithIcon>();

    public ButtonWithIcon.IconPlacementType IconPlacement
    {
        get => GetValue(IconPlacementProperty);
        set => SetValue(IconPlacementProperty, value);
    }

    public static readonly StyledProperty<string?> IconProperty =
        ButtonWithIcon.IconProperty.AddOwner<ToggleButtonWithIcon>();

    public string? Icon
    {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly StyledProperty<double> SpacingProperty =
        ButtonWithIcon.SpacingProperty.AddOwner<ToggleButtonWithIcon>();

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public ToggleButtonWithIcon()
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
            Orientation = IconPlacement is ButtonWithIcon.IconPlacementType.Left or ButtonWithIcon.IconPlacementType.Right
                ? Orientation.Horizontal
                : Orientation.Vertical
        };

        if (IconPlacement is ButtonWithIcon.IconPlacementType.Left or ButtonWithIcon.IconPlacementType.Top) panel.Children.Add(MakeIcon());
        panel.Children.Add(new TextBlock { VerticalAlignment = VerticalAlignment.Center, Text = Text });
        if (IconPlacement is ButtonWithIcon.IconPlacementType.Right or ButtonWithIcon.IconPlacementType.Bottom) panel.Children.Add(MakeIcon());

        Content = panel;
    }
}