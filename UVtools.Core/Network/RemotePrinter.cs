/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using CommunityToolkit.Mvvm.ComponentModel;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;

namespace UVtools.Core.Network;

public partial class RemotePrinter : ObservableObject
{
    #region Members
    private string _host = "0.0.0.0";

    #endregion

    #region Properties

    [ObservableProperty]
    public partial bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the alias name for this printer.
    /// Not used on requests
    /// </summary>
    [ObservableProperty]
    public partial string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the host/ip for the requests
    /// </summary>
    public string Host
    {
        get => _host;
        set
        {
            if (!SetProperty(ref _host!, value?.Trim())) return;
            OnPropertyChanged(nameof(HostUrl));
            OnPropertyChanged(nameof(IsValid));
        }
    }

    /// <summary>
    /// Gets or sets the host port for the requests.
    /// Use 0 to not use a port
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HostUrl))]
    public partial ushort Port { get; set; }

    public string HostUrl => Port > 0 ? $"{_host}:{Port}" : _host;

    /// <summary>
    /// Gets or sets the compatible extensions with this device.
    /// Empty or null to be compatible with everything
    /// </summary>
    [ObservableProperty]
    public partial string? CompatibleExtensions { get; set; }

    /// <summary>
    /// Gets if this host is valid
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(_host) && _host != "0.0.0.0";

    [ObservableProperty] public partial RemotePrinterRequest RequestUploadFile { get; set; } = new(RemotePrinterRequest.RequestType.UploadFile, RemotePrinterRequest.RequestMethod.PUT);
    [ObservableProperty] public partial RemotePrinterRequest RequestPrintFile { get; set; } = new(RemotePrinterRequest.RequestType.PrintFile, RemotePrinterRequest.RequestMethod.GET);
    [ObservableProperty] public partial RemotePrinterRequest RequestDeleteFile { get; set; } = new(RemotePrinterRequest.RequestType.DeleteFile, RemotePrinterRequest.RequestMethod.GET);
    [ObservableProperty] public partial RemotePrinterRequest RequestPausePrint { get; set; } = new(RemotePrinterRequest.RequestType.PausePrint, RemotePrinterRequest.RequestMethod.GET);
    [ObservableProperty] public partial RemotePrinterRequest RequestResumePrint { get; set; } = new(RemotePrinterRequest.RequestType.ResumePrint, RemotePrinterRequest.RequestMethod.GET);
    [ObservableProperty] public partial RemotePrinterRequest RequestStopPrint { get; set; } = new(RemotePrinterRequest.RequestType.StopPrint, RemotePrinterRequest.RequestMethod.GET);
    [ObservableProperty] public partial RemotePrinterRequest RequestGetFiles { get; set; } = new(RemotePrinterRequest.RequestType.GetFiles, RemotePrinterRequest.RequestMethod.GET);
    [ObservableProperty] public partial RemotePrinterRequest RequestPrintStatus { get; set; } = new(RemotePrinterRequest.RequestType.PrintStatus, RemotePrinterRequest.RequestMethod.GET);
    [ObservableProperty] public partial RemotePrinterRequest RequestPrinterInfo { get; set; } = new(RemotePrinterRequest.RequestType.PrinterInfo, RemotePrinterRequest.RequestMethod.GET);


    public RemotePrinterRequest[] Requests =>
    [
        RequestUploadFile, RequestPrintFile, RequestDeleteFile, RequestPausePrint, RequestResumePrint,
        RequestStopPrint, RequestGetFiles, RequestPrintStatus, RequestPrinterInfo
    ];

    #endregion

    #region Constructor

    public RemotePrinter()
    {
    }

    public RemotePrinter(string host = "0.0.0.0", ushort port = 0, string name = "", bool isEnabled = false)
    {
        IsEnabled = isEnabled;
        Name = name;
        _host = host;
        Port = port;
    }

    #endregion

    #region Methods

    public override string ToString()
    {
        var result = HostUrl;
        if (!string.IsNullOrWhiteSpace(Name)) result += $"  ({Name})";
        return result;
    }

    public RemotePrinter Clone()
    {
        return this.CloneByXmlSerialization();
    }

    #endregion
}
