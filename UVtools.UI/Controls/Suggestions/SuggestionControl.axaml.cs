using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionControl : UserControlEx
{
    public Suggestion Suggestion { get; set; }

    public SuggestionControl() : this(null!) { }

    public SuggestionControl(Suggestion suggestion)
    {
        Suggestion = suggestion;
        InitializeComponent();
        DataContext = this;
    }
}