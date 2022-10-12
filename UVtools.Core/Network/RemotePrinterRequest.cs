/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.Net.Http;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.Objects;
using UVtools.Core.Operations;

namespace UVtools.Core.Network;

public class RemotePrinterRequest : BindableBase
{
    #region Enums

    public enum RequestMethod : byte
    {
        [Description("GET")]
        GET,

        [Description("POST")]
        POST,

        [Description("PUT")]
        PUT
    }

    public enum RequestType : byte
    {
        UploadFile,
        PrintFile,
        DeleteFile,
        PausePrint,
        ResumePrint,
        StopPrint,
        GetFiles,
        PrintStatus,
        PrinterInfo,
    }

    #endregion

    #region Members
    private RequestType _type;
    private RequestMethod _method;
    private string _path = string.Empty;
    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets this request type
    /// </summary>
    public RequestType Type
    {
        get => _type;
        set => RaiseAndSetIfChanged(ref _type, value);
    }


    /// <summary>
    /// Gets or sets this request method
    /// </summary>
    public RequestMethod Method
    {
        get => _method;
        set => RaiseAndSetIfChanged(ref _method, value);
    }

    /// <summary>
    /// Gets or sets the request path, eg: print/file/{0}
    /// </summary>
    public string Path
    {
        get => _path;
        set
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                value = value.Trim();
                if (value[0] == '/') value = value.Remove(0, 1);
                if(value[^1] == '/') value = value.Remove(value.Length-1, 1);
            }
            if(!RaiseAndSetIfChanged(ref _path, value)) return;
            RaisePropertyChanged(nameof(IsValid));
        }
    }

    public bool IsValid => !string.IsNullOrWhiteSpace(_path);

    #endregion

    #region Constructors

    public RemotePrinterRequest() { }

    public RemotePrinterRequest(RequestType type, RequestMethod method, string path = "")
    {
        _type = type;
        _method = method;
        Path = path;
    }

    #endregion


    #region Methods

    /// <summary>
    /// Gets the path with formatted arguments
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public string GetFormattedPath(params object?[] parameters) => string.Format(_path, parameters);

    public async Task<HttpResponseMessage> SendRequest(string host, ushort port = 0, OperationProgress? progress = null, string? param1 = null, HttpContent? content = null)
    {
        string url = $"http://{host}";
        if (port > 0) url += $":{port}";

        progress ??= new();
        progress.Title = $"Sending {_method} request to: {url}";
        progress.ItemName = "Megabyte(s)";
        progress.CanCancel = true;

            
        if (!string.IsNullOrWhiteSpace(_path)) url += $"/{GetFormattedPath(param1)}";

            
        switch (_method)
        {
            case RequestMethod.GET:
            {
                using var response = await NetworkExtensions.HttpClient.GetAsync(url, progress.Token);
                return response;
            }
            case RequestMethod.POST:
            {
                using var response = await NetworkExtensions.HttpClient.PostAsync(url, content, progress.Token);
                return response;
            }
            case RequestMethod.PUT:
            {
                using var response = await NetworkExtensions.HttpClient.PutAsync(url, content, progress.Token);
                return response;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(Method));
        }
    }

    public async Task<HttpResponseMessage> SendRequest(RemotePrinter remotePrinter,
        OperationProgress? progress = null, string? param1 = null, HttpContent? content = null)
        => await SendRequest(remotePrinter.Host, remotePrinter.Port, progress, param1, content);

    public RemotePrinterRequest Clone()
    {
        return (MemberwiseClone() as RemotePrinterRequest)!;
    }

    public override string ToString()
    {
        return _path;
    }

    protected bool Equals(RemotePrinterRequest other)
    {
        return _path == other._path;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RemotePrinterRequest)obj);
    }

    public override int GetHashCode()
    {
        return (_path != null ? _path.GetHashCode() : 0);
    }

    #endregion
}