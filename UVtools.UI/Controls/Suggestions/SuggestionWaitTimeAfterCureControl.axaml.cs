using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionWaitTimeAfterCureControl : SuggestionControl
{
    public SuggestionWaitTimeAfterCureControl() : this(new SuggestionWaitTimeAfterCure())
    { }

    public SuggestionWaitTimeAfterCureControl(Suggestion suggestion)
    {
        Suggestion = suggestion;
        DataContext = this;
        InitializeComponent();
    }
}