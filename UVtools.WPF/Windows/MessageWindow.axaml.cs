using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using System;
using UVtools.Core.SystemOS;
using UVtools.WPF.Controls;

namespace UVtools.WPF.Windows
{
    public partial class MessageWindow : WindowEx
    {
        #region Members
        private string? _headerIcon;
        private ushort _headerIconSize = 64;
        private string _headerTitle;
        private bool _aboutButtonIsVisible = true;
        private string _message;

        private readonly StackPanel _buttonsRightPanel;
        #endregion

        #region Properties
        /// <summary>
        /// Gets the pressed button
        /// </summary>
        public ButtonWithIcon? PressedButton { get; private set; }

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
        #endregion

        #region Constructor
        public MessageWindow()
        {
            CanResize = Settings.General.WindowsCanResize;

            InitializeComponent();

            _buttonsRightPanel = this.FindControl<StackPanel>("ButtonsRightPanel");
            DataContext = this;
        }

        public MessageWindow(string title, string? headerIcon, string? headerTitle, string message, ButtonWithIcon[]? rightButtons = null) : this()
        {
            Title = title;
            HeaderIcon = headerIcon;
            HeaderTitle = headerTitle;
            Message = message;

            if (rightButtons is not null && rightButtons.Length > 0)
            {
                _buttonsRightPanel.Children.Clear();
                _buttonsRightPanel.Children.AddRange(rightButtons);

                foreach (var button in rightButtons)
                {
                    button.Click += (sender, e) =>
                    {
                        PressedButton = button;
                        if (e.Handled) return;
                        Close(button);
                        e.Handled = true;
                    };
                }
            }
        }

        public MessageWindow(string title, string message, ButtonWithIcon[]? buttons = null) : this(title, null, null, message, buttons) { }
        

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        #endregion

        #region Methods
        public async void OpenAboutWindow()
        {
            await new AboutWindow().ShowDialog(this);
        }
        #endregion

        #region Static methods
        public static ButtonWithIcon CreateButtonFunc(string? text, string? icon, Func<bool> customAction, int padding = 10)
        {
            var button = CreateButton(text, icon, padding);
            button.Click += (sender, e) => e.Handled = customAction.Invoke();
            return button;
        }

        public static ButtonWithIcon CreateButtonAction(string? text, string? icon, Action customAction, int padding = 10)
        {
            var button = CreateButton(text, icon, padding);
            button.Click += (sender, e) => customAction.Invoke();
            return button;
        }

        public static ButtonWithIcon CreateButtonFunc(string? text, Func<bool> customAction, int padding = 10) => CreateButtonFunc(text, null, customAction, padding);
        public static ButtonWithIcon CreateButtonAction(string? text, Action customAction, int padding = 10) => CreateButtonAction(text, null, customAction, padding);

        public static ButtonWithIcon CreateButton(string? text, string? icon, int padding = 10) => new()
        {
            Icon = icon,
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(padding)
        };

        public static ButtonWithIcon CreateButton(string? text, int padding = 10) => CreateButton(text, null, padding);


        public static ButtonWithIcon CreateLinkButtonAction(string? text, string? icon, string url, Action customAction, int padding = 10)
        {
            var button = CreateButtonFunc(text, icon, () =>
            {
                customAction.Invoke();
                SystemAware.OpenBrowser(url);
                return true;
            }, padding);
            return button;
        }

        public static ButtonWithIcon CreateLinkButton(string? text, string? icon, string url, int padding = 10)
        {
            var button = CreateButtonFunc(text, icon, () =>
            {
                SystemAware.OpenBrowser(url);
                return true;
            }, padding);
            return button;
        }
        public static ButtonWithIcon CreateLinkButtonAction(string? text, string url, Action customAction, int padding = 10) => CreateLinkButtonAction(text, null, url, customAction, padding);
        public static ButtonWithIcon CreateLinkButton(string? text, string url, int padding = 10) => CreateLinkButton(text, null, url, padding);

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
        #endregion
    }
}
