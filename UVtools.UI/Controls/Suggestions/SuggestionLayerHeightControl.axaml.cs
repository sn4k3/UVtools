using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

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
}