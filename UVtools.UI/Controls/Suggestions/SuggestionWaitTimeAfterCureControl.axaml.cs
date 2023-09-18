using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionWaitTimeAfterCureControl : SuggestionControl
{
    public SuggestionWaitTimeAfterCure Suggestion => (BaseSuggestion as SuggestionWaitTimeAfterCure)!;

    public SuggestionWaitTimeAfterCureControl() : this(new SuggestionWaitTimeAfterCure())
    { }

    public SuggestionWaitTimeAfterCureControl(Suggestion suggestion) : base(suggestion)
    {
        InitializeComponent();
    }
}