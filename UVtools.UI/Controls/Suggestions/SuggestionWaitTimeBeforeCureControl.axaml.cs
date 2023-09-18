using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionWaitTimeBeforeCureControl : SuggestionControl
{
    public SuggestionWaitTimeBeforeCure Suggestion => (BaseSuggestion as SuggestionWaitTimeBeforeCure)!;
    public SuggestionWaitTimeBeforeCureControl() : this(new SuggestionWaitTimeBeforeCure())
    { }

    public SuggestionWaitTimeBeforeCureControl(Suggestion suggestion) : base(suggestion)
    {
        InitializeComponent();
    }
}