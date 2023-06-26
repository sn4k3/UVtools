using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using System;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Markdown.Avalonia;
using UVtools.Core.Dialogs;
using UVtools.Core.SystemOS;
using UVtools.WPF.Controls;

namespace UVtools.WPF.Windows
{
    public partial class MessageWindow : WindowEx
    {
        #region Constants

        public const string IconHeaderInformation = "fa-solid fa-circle-info";
        public const string IconHeaderQuestion = "fa-solid fa-circle-question";
        public const string IconHeaderWarning = "fa-solid fa-triangle-exclamation";
        public const string IconHeaderError = "fa-solid fa-circle-exclamation";
        
        public const string IconButtonYes = "fa-solid fa-check";
        public const string IconButtonOk = IconButtonYes;
        public const string IconButtonNo = "fa-solid fa-xmark";
        public const string IconButtonNone = IconButtonNo;
        public const string IconButtonAbort = "fa-solid fa-ban";
        public const string IconButtonCancel = "fa-solid fa-ban";
        public const string IconButtonExit = "fa-solid fa-right-from-bracket";
        public const string IconButtonClose = IconButtonExit;

        public const string IconButtonDownload = "fa-solid fa-cloud-arrow-down";
        public const string IconButtonOpenBrowser = "fa-brands fa-edge";

        #endregion

        #region Members

        private TextWrapping _textWrap = TextWrapping.Wrap;
        private string? _headerIcon;
        private ushort _headerIconSize;
        private string _headerText;
        private bool _aboutButtonIsVisible;
        private string _messageText;
        private bool _renderMarkdown;

        private readonly TextBox _headerTextBox;
        private readonly TextBox _messageTextBox;
        private readonly Border _markdownBorder;
        private readonly StackPanel _buttonsRightPanel;

        #endregion

        #region Properties
        /// <summary>
        /// Gets the pressed button
        /// </summary>
        public ButtonWithIcon? PressedButton { get; private set; }

        public TextWrapping TextWrap
        {
            get => _textWrap;
            set
            {
                if(!RaiseAndSetIfChanged(ref _textWrap, value)) return;
                if (value == TextWrapping.Wrap)
                {
                    ScrollViewer.SetHorizontalScrollBarVisibility(_headerTextBox, ScrollBarVisibility.Disabled);
                    ScrollViewer.SetHorizontalScrollBarVisibility(_messageTextBox, ScrollBarVisibility.Disabled);
                }
                else
                {
                    ScrollViewer.SetHorizontalScrollBarVisibility(_headerTextBox, ScrollBarVisibility.Auto);
                    ScrollViewer.SetHorizontalScrollBarVisibility(_messageTextBox, ScrollBarVisibility.Auto);
                }
            }
        }

        public string? HeaderIcon
        {
            get => _headerIcon;
            set
            {
                if (!RaiseAndSetIfChanged(ref _headerIcon, value)) return;
                RaisePropertyChanged(nameof(HeaderIsVisible));
            }
        }

        /// <summary>
        /// Gets or sets the header icon size, if 0 or negative it will auto calculate the size based on text height within a limit
        /// </summary>
        public ushort HeaderIconSize
        {
            get => _headerIconSize;
            set => RaiseAndSetIfChanged(ref _headerIconSize, value);
        }

        public string? HeaderText
        {
            get => _headerText;
            set
            {
                if(!RaiseAndSetIfChanged(ref _headerText, value)) return;
                RaisePropertyChanged(nameof(HeaderIsVisible));
            }
        }

        public bool HeaderIsVisible => !string.IsNullOrWhiteSpace(HeaderText);

        public string MessageText
        {
            get => _messageText;
            set => RaiseAndSetIfChanged(ref _messageText, value);
        }

        /// <summary>
        /// Gets or sets to render the <see cref="MessageText"/> as markdown
        /// </summary>
        public bool RenderMarkdown
        {
            get => _renderMarkdown;
            set => RaiseAndSetIfChanged(ref _renderMarkdown, value);
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

            _headerTextBox = this.FindControl<TextBox>("HeaderTextBox");
            _messageTextBox = this.FindControl<TextBox>("MessageTextBox");
            _markdownBorder = this.FindControl<Border>("MarkdownBorder");
            _buttonsRightPanel = this.FindControl<StackPanel>("ButtonsRightPanel");
            DataContext = this;
        }

        public MessageWindow(string title, string? headerIcon, string? headerText, string messageText, TextWrapping textWrap, ButtonWithIcon[]? rightButtons = null, bool renderMarkdown = false) : this()
        {
            Title = title.Trim();
            TextWrap = textWrap;
            HeaderIcon = headerIcon;
            HeaderText = headerText?.Trim();
            MessageText = messageText?.Trim();
            RenderMarkdown = renderMarkdown;

            if (renderMarkdown)
            {
                _markdownBorder.Child = new MarkdownScrollViewer
                {
                    Markdown = messageText
                };
            }

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

        public MessageWindow(string title, string? headerIcon, string? headerText, string messageText, ButtonWithIcon[]? rightButtons = null, bool renderMarkdown = false) : this(title, headerIcon, headerText, messageText, TextWrapping.Wrap, rightButtons, renderMarkdown) { }
        public MessageWindow(string title, string message, TextWrapping textWrap, ButtonWithIcon[]? buttons = null, bool renderMarkdown = false) : this(title, null, null, message, textWrap, buttons, renderMarkdown) { }
        public MessageWindow(string title, string message, ButtonWithIcon[]? buttons = null, bool renderMarkdown = false) : this(title, null, null, message, buttons, renderMarkdown) { }
        

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        protected override void OnOpened(EventArgs e)
        {
            base.OnOpened(e);
            if (_headerIconSize <= 0 && !string.IsNullOrWhiteSpace(_headerIcon))
            {
                if (HeaderIsVisible)
                {
                    HeaderIconSize = (ushort)Math.Clamp(_headerTextBox.DesiredSize.Height, 48, 256);
                }
                else
                {
                    HeaderIconSize = (ushort)Math.Clamp(_messageTextBox.DesiredSize.Height, 32, 128);
                }
            }
        }

        #endregion

        #region Methods
        public async void OpenAboutWindow()
        {
            await new AboutWindow().ShowDialog(this);
        }
        #endregion

        #region Static methods
        public static ButtonWithIcon CreateButtonFunc(string? text, string? icon, Func<bool> customAction, int padding = 10, object? tag = null)
        {
            var button = CreateButton(text, icon, padding);
            button.Click += (sender, e) => e.Handled = customAction.Invoke();
            return button;
        }

        public static ButtonWithIcon CreateButtonAction(string? text, string? icon, Action customAction, int padding = 10, object? tag = null)
        {
            var button = CreateButton(text, icon, padding);
            button.Click += (sender, e) => customAction.Invoke();
            return button;
        }

        public static ButtonWithIcon CreateButtonFunc(string? text, Func<bool> customAction, int padding = 10, object? tag = null) => CreateButtonFunc(text, null, customAction, padding);
        public static ButtonWithIcon CreateButtonAction(string? text, Action customAction, int padding = 10, object? tag = null) => CreateButtonAction(text, null, customAction, padding);

        public static ButtonWithIcon CreateButton(string? text, string? icon, int padding = 10, object? tag = null) => new()
        {
            Icon = icon,
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
            Padding = new Thickness(padding),
            Tag = tag
        };

        public static ButtonWithIcon CreateButton(string? text, int padding = 10, object? tag = null) => CreateButton(text, null, padding, tag);


        public static ButtonWithIcon CreateLinkButtonAction(string? text, string? icon, string url, Action customAction, int padding = 10, object? tag = null)
        {
            var button = CreateButtonFunc(text, icon, () =>
            {
                customAction.Invoke();
                SystemAware.OpenBrowser(url);
                return true;
            }, padding, tag);
            return button;
        }

        public static ButtonWithIcon CreateLinkButton(string? text, string? icon, string url, int padding = 10, object? tag = null)
        {
            var button = CreateButtonFunc(text, icon, () =>
            {
                SystemAware.OpenBrowser(url);
                return true;
            }, padding, tag);
            return button;
        }
        public static ButtonWithIcon CreateLinkButtonAction(string? text, string url, Action customAction, int padding = 10, object? tag = null) => CreateLinkButtonAction(text, null, url, customAction, padding, tag);
        public static ButtonWithIcon CreateLinkButton(string? text, string url, int padding = 10, object? tag = null) => CreateLinkButton(text, null, url, padding, tag);

        public static ButtonWithIcon CreateOkButton(string? icon = IconButtonOk, int padding = 10, bool isDefault = true, bool isCancel = false)
        {
            var btn = CreateButton("Ok", icon, padding, MessageButtonResult.Ok);
            btn.IsDefault = isDefault;
            btn.IsCancel = isCancel;
            return btn;
        }

        public static ButtonWithIcon CreateYesButton(string? icon = IconButtonYes, int padding = 10, bool isDefault = true, bool isCancel = false)
        {
            var btn = CreateButton("Yes", icon, padding, MessageButtonResult.Yes);
            btn.IsDefault = isDefault;
            btn.IsCancel = isCancel;
            return btn;
        }

        public static ButtonWithIcon CreateNoButton(string? icon = IconButtonNo, int padding = 10, bool isDefault = false, bool isCancel = false)
        {
            var btn = CreateButton("No", icon, padding, MessageButtonResult.No);
            btn.IsDefault = isDefault;
            btn.IsCancel = isCancel;
            return btn;
        }

        public static ButtonWithIcon CreateNoneButton(string? icon = IconButtonNone, int padding = 10, bool isDefault = false, bool isCancel = false)
        {
            var btn = CreateButton("None", icon, padding, MessageButtonResult.None);
            btn.IsDefault = isDefault;
            btn.IsCancel = isCancel;
            return btn;
        }

        public static ButtonWithIcon CreateAbortButton(string? icon = IconButtonAbort, int padding = 10, bool isDefault = false, bool isCancel = true)
        {
            var btn = CreateButton("Abort", icon, padding, MessageButtonResult.Abort);
            btn.IsDefault = isDefault;
            btn.IsCancel = isCancel;
            return btn;
        }

        public static ButtonWithIcon CreateCancelButton(string? icon = IconButtonCancel, int padding = 10, bool isDefault = false, bool isCancel = true)
        {
            var btn = CreateButton("Cancel", icon, padding, MessageButtonResult.Cancel);
            btn.IsDefault = isDefault;
            btn.IsCancel = isCancel;
            return btn;
        }

        public static ButtonWithIcon CreateCloseButton(string? icon = IconButtonClose, int padding = 10, bool isDefault = false, bool isCancel = true)
        {
            var btn = CreateButton("Close", icon, padding, MessageButtonResult.Cancel);
            btn.IsDefault = isDefault;
            btn.IsCancel = isCancel;
            return btn;
        }
        #endregion
    }
}
