using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;

namespace UVtools.WPF.Controls;

public class ToggleButtonWithIcon : ToggleButton, IStyleable
{
    Type IStyleable.StyleKey => typeof(ToggleButton);

    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<ButtonWithIcon, string>(nameof(Text));

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly StyledProperty<ButtonWithIcon.IconPlacementType> IconPlacementProperty =
        AvaloniaProperty.Register<ButtonWithIcon, ButtonWithIcon.IconPlacementType>(nameof(IconPlacement));

    public ButtonWithIcon.IconPlacementType IconPlacement
    {
        get => GetValue(IconPlacementProperty);
        set => SetValue(IconPlacementProperty, value);
    }

    public static readonly StyledProperty<string> IconProperty =
        AvaloniaProperty.Register<ButtonWithIcon, string>(nameof(Icon));

    public string Icon
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

    public ToggleButtonWithIcon()
    {
    }

    protected override void OnInitialized()
    {
        base.OnInitialized();

        Dispatcher.UIThread.Post(() =>
        {
            TextProperty.Changed.Subscribe(_ => RebuildContent());
            IconProperty.Changed.Subscribe(_ => RebuildContent());
            IconPlacementProperty.Changed.Subscribe(_ => RebuildContent());
            RebuildContent();
        }, DispatcherPriority.Loaded);
    }

    public IControl MakeIcon()
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