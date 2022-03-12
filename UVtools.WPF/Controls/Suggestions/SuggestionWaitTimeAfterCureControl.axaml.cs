using Avalonia.Markup.Xaml;
using UVtools.Core.Suggestions;

namespace UVtools.WPF.Controls.Suggestions;

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

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}