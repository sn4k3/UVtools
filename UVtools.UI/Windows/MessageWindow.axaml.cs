using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using System;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Material.Icons;
using Material.Icons.Avalonia;
using UVtools.Core.Dialogs;
using UVtools.Core.SystemOS;

namespace UVtools.UI.Windows;

public partial class MessageWindow : GenericWindow
{
    #region Constants

    public const MaterialIconKind IconHeaderInformation = MaterialIconKind.InformationCircle;
    public const MaterialIconKind IconHeaderQuestion = MaterialIconKind.QuestionMarkCircle;
    public const MaterialIconKind IconHeaderWarning = MaterialIconKind.Alert;
    public const MaterialIconKind IconHeaderError = MaterialIconKind.Error;

    public const MaterialIconKind IconButtonYes = MaterialIconKind.Check;
    public const MaterialIconKind IconButtonOk = IconButtonYes;
    public const MaterialIconKind IconButtonNo = MaterialIconKind.Close;
    public const MaterialIconKind IconButtonNone = IconButtonNo;
    public const MaterialIconKind IconButtonAbort = MaterialIconKind.Ban;
    public const MaterialIconKind IconButtonCancel = MaterialIconKind.Cancel;
    public const MaterialIconKind IconButtonExit = MaterialIconKind.SignOut;
    public const MaterialIconKind IconButtonClose = IconButtonExit;

    public const MaterialIconKind IconButtonDownload = MaterialIconKind.CloudDownload;
    public const MaterialIconKind IconButtonOpenBrowser = MaterialIconKind.MicrosoftEdge;

    #endregion

    #region Members

    private TextWrapping _textWrap = TextWrapping.Wrap;
    private MaterialIconKind? _headerIcon;
    private ushort _headerIconSize;
    private string? _headerText;
    private bool _aboutButtonIsVisible;
    private string _messageText = null!;
    private bool _renderMarkdown;
    #endregion

    #region Properties
    /// <summary>
    /// Gets the pressed button
    /// </summary>
    public Button? PressedButton { get; private set; }

    public TextWrapping TextWrap
    {
        get => _textWrap;
        set
        {
            if(!RaiseAndSetIfChanged(ref _textWrap, value)) return;
            if (value == TextWrapping.Wrap)
            {
                ScrollViewer.SetHorizontalScrollBarVisibility(HeaderTextBox, ScrollBarVisibility.Disabled);
                ScrollViewer.SetHorizontalScrollBarVisibility(MessageTextBox, ScrollBarVisibility.Disabled);
            }
            else
            {
                ScrollViewer.SetHorizontalScrollBarVisibility(HeaderTextBox, ScrollBarVisibility.Auto);
                ScrollViewer.SetHorizontalScrollBarVisibility(MessageTextBox, ScrollBarVisibility.Auto);
            }
        }
    }

    public MaterialIconKind? HeaderIcon
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
        DataContext = this;
    }

    public MessageWindow(string title, MaterialIconKind? headerIcon, string? headerText, string messageText, TextWrapping textWrap, Button[]? rightButtons = null, bool renderMarkdown = false) : this()
    {
        Title = title.Trim();
        TextWrap = textWrap;
        HeaderIcon = headerIcon;
        HeaderText = headerText?.Trim();
        MessageText = messageText.Trim();
        RenderMarkdown = renderMarkdown;

        if (renderMarkdown)
        {
            MarkdownBorder.Content = new MarkdownViewer.Core.Controls.MarkdownViewer
            {
                MarkdownText = messageText
            };
        }

        if (rightButtons is not null && rightButtons.Length > 0)
        {
            ButtonsRightPanel.Children.Clear();
            ButtonsRightPanel.Children.AddRange(rightButtons);

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

    public MessageWindow(string title, MaterialIconKind? headerIcon, string? headerText, string messageText, Button[]? rightButtons = null, bool renderMarkdown = false) : this(title, headerIcon, headerText, messageText, TextWrapping.Wrap, rightButtons, renderMarkdown) { }
    public MessageWindow(string title, string message, TextWrapping textWrap, Button[]? buttons = null, bool renderMarkdown = false) : this(title, null, null, message, textWrap, buttons, renderMarkdown) { }
    public MessageWindow(string title, string message, Button[]? buttons = null, bool renderMarkdown = false) : this(title, null, null, message, buttons, renderMarkdown) { }


    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (_headerIconSize <= 0 && _headerIcon is not null)
        {
            if (HeaderIsVisible)
            {
                HeaderIconSize = (ushort)Math.Clamp(HeaderTextBox.DesiredSize.Height, 48, 256);
            }
            else
            {
                HeaderIconSize = (ushort)Math.Clamp(MessageTextBox.DesiredSize.Height, 32, 128);
            }
        }
    }

    #endregion

    #region Methods
    public async Task OpenAboutWindow()
    {
        await new AboutWindow().ShowDialog(this);
    }
    #endregion

    #region Static methods
    public static Button CreateButtonFunc(string? text, MaterialIconKind? icon, Func<bool> customAction, int padding = 10, object? tag = null)
    {
        var button = CreateButton(text, icon, padding);
        button.Click += (sender, e) => e.Handled = customAction.Invoke();
        return button;
    }

    public static Button CreateButtonAction(string? text, MaterialIconKind? icon, Action customAction, int padding = 10, object? tag = null)
    {
        var button = CreateButton(text, icon, padding);
        button.Click += (sender, e) => customAction.Invoke();
        return button;
    }

    public static Button CreateButtonFunc(string? text, Func<bool> customAction, int padding = 10, object? tag = null) => CreateButtonFunc(text, null, customAction, padding);
    public static Button CreateButtonAction(string? text, Action customAction, int padding = 10, object? tag = null) => CreateButtonAction(text, null, customAction, padding);

    public static Button CreateButton(string? text, MaterialIconKind? icon, int padding = 10, object? tag = null)
    {
        var kind = MaterialIconKind.Check;
        return new Button
        {
            Content = new MaterialIconTextExt
        {
            Kind = kind,
            Text = text
        },
        VerticalAlignment = VerticalAlignment.Center,
        Padding = new Thickness(padding),
        Tag = tag
        };
    }

    public static Button CreateButton(string? text, int padding = 10, object? tag = null) => CreateButton(text, null, padding, tag);


    public static Button CreateLinkButtonAction(string? text, MaterialIconKind? icon, string url, Action customAction, int padding = 10, object? tag = null)
    {
        var button = CreateButtonFunc(text, icon, () =>
        {
            customAction.Invoke();
            SystemAware.OpenBrowser(url);
            return true;
        }, padding, tag);
        return button;
    }

    public static Button CreateLinkButton(string? text, MaterialIconKind? icon, string url, int padding = 10, object? tag = null)
    {
        var button = CreateButtonFunc(text, icon, () =>
        {
            SystemAware.OpenBrowser(url);
            return true;
        }, padding, tag);
        return button;
    }
    public static Button CreateLinkButtonAction(string? text, string url, Action customAction, int padding = 10, object? tag = null) => CreateLinkButtonAction(text, null, url, customAction, padding, tag);
    public static Button CreateLinkButton(string? text, string url, int padding = 10, object? tag = null) => CreateLinkButton(text, null, url, padding, tag);

    public static Button CreateOkButton(MaterialIconKind? icon = IconButtonOk, int padding = 10, bool isDefault = true, bool isCancel = false)
    {
        var btn = CreateButton("Ok", icon, padding, MessageButtonResult.Ok);
        btn.IsDefault = isDefault;
        btn.IsCancel = isCancel;
        return btn;
    }

    public static Button CreateYesButton(MaterialIconKind? icon = IconButtonYes, int padding = 10, bool isDefault = true, bool isCancel = false)
    {
        var btn = CreateButton("Yes", icon, padding, MessageButtonResult.Yes);
        btn.IsDefault = isDefault;
        btn.IsCancel = isCancel;
        return btn;
    }

    public static Button CreateNoButton(MaterialIconKind? icon = IconButtonNo, int padding = 10, bool isDefault = false, bool isCancel = false)
    {
        var btn = CreateButton("No", icon, padding, MessageButtonResult.No);
        btn.IsDefault = isDefault;
        btn.IsCancel = isCancel;
        return btn;
    }

    public static Button CreateNoneButton(MaterialIconKind? icon = IconButtonNone, int padding = 10, bool isDefault = false, bool isCancel = false)
    {
        var btn = CreateButton("None", icon, padding, MessageButtonResult.None);
        btn.IsDefault = isDefault;
        btn.IsCancel = isCancel;
        return btn;
    }

    public static Button CreateAbortButton(MaterialIconKind? icon = IconButtonAbort, int padding = 10, bool isDefault = false, bool isCancel = true)
    {
        var btn = CreateButton("Abort", icon, padding, MessageButtonResult.Abort);
        btn.IsDefault = isDefault;
        btn.IsCancel = isCancel;
        return btn;
    }

    public static Button CreateCancelButton(MaterialIconKind? icon = IconButtonCancel, int padding = 10, bool isDefault = false, bool isCancel = true)
    {
        var btn = CreateButton("Cancel", icon, padding, MessageButtonResult.Cancel);
        btn.IsDefault = isDefault;
        btn.IsCancel = isCancel;
        return btn;
    }

    public static Button CreateCloseButton(MaterialIconKind? icon = IconButtonClose, int padding = 10, bool isDefault = false, bool isCancel = true)
    {
        var btn = CreateButton("Close", icon, padding, MessageButtonResult.Cancel);
        btn.IsDefault = isDefault;
        btn.IsCancel = isCancel;
        return btn;
    }
    #endregion
}