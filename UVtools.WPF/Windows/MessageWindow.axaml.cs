using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using UVtools.WPF.Controls;

namespace UVtools.WPF.Windows
{
    public partial class MessageWindow : WindowEx
    {
        private string? _headerIcon;
        private ushort _headerIconSize = 64;
        private string _headerTitle;
        private bool _aboutButtonIsVisible = true;
        private string _message;

        private readonly StackPanel _buttonsRightPanel;

        public string? HeaderIcon
        {
            get => _headerIcon;
            set
            {
                if (!RaiseAndSetIfChanged(ref _headerIcon, value)) return;
                RaisePropertyChanged(nameof(HeaderIsVisible));
            }
        }

        public ushort HeaderIconSize
        {
            get => _headerIconSize;
            set => RaiseAndSetIfChanged(ref _headerIconSize, value);
        }

        public string? HeaderTitle
        {
            get => _headerTitle;
            set
            {
                if(!RaiseAndSetIfChanged(ref _headerTitle, value)) return;
                RaisePropertyChanged(nameof(HeaderIsVisible));
            }
        }

        public bool HeaderIsVisible => !string.IsNullOrWhiteSpace(HeaderIcon) || !string.IsNullOrWhiteSpace(HeaderTitle);

        public string Message
        {
            get => _message;
            set => RaiseAndSetIfChanged(ref _message, value);
        }

        public bool AboutButtonIsVisible
        {
            get => _aboutButtonIsVisible;
            set => RaiseAndSetIfChanged(ref _aboutButtonIsVisible, value);
        }

        public MessageWindow()
        {
            InitializeComponent();

            CanResize = Settings.General.WindowsCanResize;
            if (WindowStartupLocation == WindowStartupLocation.CenterOwner && Owner is null)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }

            AutoConstainsWindowMaxSize();

            _buttonsRightPanel = this.FindControl<StackPanel>("ButtonsRightPanel");
            DataContext = this;
        }

        public MessageWindow(string title, string? headerIcon, string? headerTitle, string message, ButtonWithIcon[]? buttons = null) : this()
        {
            Title = title;
            HeaderIcon = headerIcon;
            HeaderTitle = headerTitle;
            Message = message;

            if (buttons is not null && buttons.Length > 0)
            {
                _buttonsRightPanel.Children.Clear();
                _buttonsRightPanel.Children.AddRange(buttons);

                foreach (var button in buttons)
                {
                    button.Click += (sender, e) =>
                    {
                        if (e.Handled) return;
                        Close(button);
                        e.Handled = true;
                    };
                }
            }
        }

        public MessageWindow(string title, string message, ButtonWithIcon[]? buttons = null) : this(title, null, null, message, buttons) { }

        /*protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
        }*/

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public async void OpenAboutWindow()
        {
            await new AboutWindow().ShowDialog(this);
        }

        public static ButtonWithIcon CreateButton(string? text, string? icon, int padding = 10) => 
            new()
            {
                Icon = icon, 
                Text = text, 
                VerticalAlignment = VerticalAlignment.Center,
                Padding = new Thickness(padding)
            };

        public static ButtonWithIcon CreateButton(string? text, int padding = 10) => CreateButton(text, null, padding);

        public static ButtonWithIcon CreateCancelButton(string? icon = null, int padding = 10)
        {
            var btn = CreateButton("Cancel", icon, padding);
            btn.IsCancel = true;
            return btn;
        }

        public static ButtonWithIcon CreateCloseButton(string? icon = null, int padding = 10)
        {
            var btn = CreateButton("Close", icon, padding);
            btn.IsCancel = true;
            return btn;
        }
    }
}
