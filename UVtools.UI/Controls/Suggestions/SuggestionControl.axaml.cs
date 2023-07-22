using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionControl : UserControlEx
{
    public Suggestion BaseSuggestion { get; set; }

    public SuggestionControl() : this(null!) { }

    public SuggestionControl(Suggestion suggestion)
    {
        BaseSuggestion = suggestion;
        InitializeComponent();
        DataContext = this;
    }
}