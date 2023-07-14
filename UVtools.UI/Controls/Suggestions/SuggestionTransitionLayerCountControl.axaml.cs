using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionTransitionLayerCountControl : SuggestionControl
{
    public SuggestionTransitionLayerCountControl() : this(new SuggestionTransitionLayerCount())
    { }

    public SuggestionTransitionLayerCountControl(Suggestion suggestion)
    {
        Suggestion = suggestion;
        DataContext = this;
        InitializeComponent();
    }
}