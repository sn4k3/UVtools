using UVtools.Core.Suggestions;

namespace UVtools.UI.Controls.Suggestions;

public partial class SuggestionModelPositionControl : SuggestionControl
{
    public SuggestionModelPosition Suggestion => (BaseSuggestion as SuggestionModelPosition)!;
    public SuggestionModelPositionControl() : this(new SuggestionModelPosition())
    { }

    public SuggestionModelPositionControl(Suggestion suggestion) : base(suggestion)
    { }
}