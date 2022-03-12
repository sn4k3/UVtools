using Avalonia.Markup.Xaml;
using UVtools.Core.Suggestions;

namespace UVtools.WPF.Controls.Suggestions;

public partial class SuggestionBottomLayerCountControl : SuggestionControl
{
    public SuggestionBottomLayerCountControl() : this(new SuggestionBottomLayerCount())
    { }

    public SuggestionBottomLayerCountControl(Suggestion suggestion)
    {
        Suggestion = suggestion;
        DataContext = this;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}