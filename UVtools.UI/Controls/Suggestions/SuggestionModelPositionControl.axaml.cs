using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionModelPositionControl : SuggestionControl
{
    public SuggestionModelPositionControl() : this(new SuggestionModelPosition())
    { }

    public SuggestionModelPositionControl(Suggestion suggestion)
    {
        Suggestion = suggestion;
        DataContext = this;
        InitializeComponent();
    }
}