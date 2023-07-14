using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

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
}