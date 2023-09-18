using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionTransitionLayerCountControl : SuggestionControl
{
    public SuggestionTransitionLayerCount Suggestion => (BaseSuggestion as SuggestionTransitionLayerCount)!;

    public SuggestionTransitionLayerCountControl() : this(new SuggestionTransitionLayerCount())
    { }

    public SuggestionTransitionLayerCountControl(Suggestion suggestion) : base(suggestion)
    {
        InitializeComponent();
    }
}