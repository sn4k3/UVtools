using Avalonia.Markup.Xaml;
using UVtools.Core.Suggestions;

namespace UVtools.WPF.Controls.Suggestions;

public partial class SuggestionLayerHeightControl : SuggestionControl
{
    public SuggestionLayerHeightControl() : this(new SuggestionLayerHeight())
    { }

    public SuggestionLayerHeightControl(Suggestion suggestion)
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