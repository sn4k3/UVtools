using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionBottomLayerCountControl : SuggestionControl
{
    public SuggestionBottomLayerCount Suggestion => (BaseSuggestion as SuggestionBottomLayerCount)!;

    public SuggestionBottomLayerCountControl() : this(new SuggestionBottomLayerCount())
    { }

    public SuggestionBottomLayerCountControl(Suggestion suggestion) : base(suggestion)
    {
        InitializeComponent();
    }
}