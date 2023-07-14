using UVtools.Core.Suggestions;

namespace UVtools.WPF.Controls.Suggestions;

public partial class SuggestionWaitTimeBeforeCureControl : SuggestionControl
{
    public SuggestionWaitTimeBeforeCureControl() : this(new SuggestionWaitTimeBeforeCure())
    { }

    public SuggestionWaitTimeBeforeCureControl(Suggestion suggestion)
    {
        Suggestion = suggestion;
        DataContext = this;
        InitializeComponent();
    }
}