using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionLayerHeightControl : SuggestionControl
{
    public SuggestionLayerHeight Suggestion => (BaseSuggestion as SuggestionLayerHeight)!;

    public SuggestionLayerHeightControl() : this(new SuggestionLayerHeight())
    { }

    public SuggestionLayerHeightControl(Suggestion suggestion) : base(suggestion)
    {
        InitializeComponent();
    }
}