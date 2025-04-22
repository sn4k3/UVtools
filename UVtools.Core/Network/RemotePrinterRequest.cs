/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
        [Description("HTTP GET")]
        GET,

        [Description("HTTP POST")]
        POST,

        [Description("HTTP PUT")]
        PUT,

        [Description("SOCK TCP")]
        TCP,

        [Description("SOCK UDP")]
        UDP
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
                if (value[0] == '/') value = value[1..];
                if (value[^1] == '/') value = value[..^2];
                value = value.Trim();
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

    public async Task<HttpResponseMessage> SendRequest(string host, ushort port = 0, OperationProgress? progress = null, string? param1 = null, string? uploadFilePath = null)
    {
        string url = $"http://{host}";
        if (port > 0) url += $":{port}";

        progress ??= new OperationProgress();
        progress.Title = $"Sending {_method} request to: {url}";
        progress.ItemName = "Megabyte(s)";
        progress.CanCancel = true;

        if(_path == "{0}") return new HttpResponseMessage(HttpStatusCode.OK);

        string? formattedPathWithParameter = null;
        if (!string.IsNullOrWhiteSpace(_path))
        {
            if (_path.StartsWith("<$") && _path.EndsWith("$>"))
            {
                var newPath = _path[2..^2].Trim();
                var split = newPath.Split('>', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (split.Length < 3) throw new ArgumentException($"Path '{_path}' is malformed", nameof(Path));

                var firstRequest = Clone();
                firstRequest.Path = split[0];
                using var response = await firstRequest.SendRequest(host, port, progress, param1);
                if (!response.IsSuccessStatusCode) return response;

                var content = await response.Content.ReadAsStringAsync(progress.Token);
                if (content is null) throw new InvalidDataException($"The request to {firstRequest.Path} returned no data");

                if (!string.IsNullOrWhiteSpace(param1)) split[1] = string.Format(split[1], Regex.Escape(param1));

                var match = Regex.Match(content, split[1]);
                if (!match.Success || match.Groups.Count < 2) throw new InvalidDataException($"Unable to parse any data from the content.\nRegex: {split[1]}\nContent: {content}");

                for (int i = 1; i < match.Groups.Count; i++)
                {
                    split[2] = split[2].Replace($"{{#{i}}}", match.Groups[i].Value);
                }

                formattedPathWithParameter = string.Format(split[2], param1);
            }
            else
            {
                formattedPathWithParameter = GetFormattedPath(param1);
            }
        }
        
        if (!string.IsNullOrWhiteSpace(_path) && !string.IsNullOrWhiteSpace(formattedPathWithParameter)) url += $"/{formattedPathWithParameter}";

        switch (_method)
        {
            case RequestMethod.GET:
            {
                using var response = await NetworkExtensions.HttpClient.GetAsync(url, progress.Token);
                return response;
            }
            case RequestMethod.POST:
            {
                if (string.IsNullOrWhiteSpace(uploadFilePath)) return await NetworkExtensions.HttpClient.PostAsync(url, null, progress.Token);

                await using var stream = File.OpenRead(uploadFilePath);
                using var httpContent = new StreamContent(stream);


                progress.ItemCount = (uint) (stream.Length / 1048576);
                bool isCopying = true;
                try
                {
                    var task = new Task(() =>
                    {
                        while (isCopying)
                        {
                            progress.ProcessedItems = (uint) (stream.Position / 1048576);
                            Thread.Sleep(200);
                        }
                    });
                }
                catch (Exception)
                {
                    // ignored
                }

                using var response = await NetworkExtensions.HttpClient.PostAsync(url, httpContent, progress.Token);
                isCopying = false;
                return response;

            }
            case RequestMethod.PUT:
            {
                if (string.IsNullOrWhiteSpace(uploadFilePath)) return await NetworkExtensions.HttpClient.PutAsync(url, null, progress.Token);

                await using var stream = File.OpenRead(uploadFilePath);
                using var httpContent = new StreamContent(stream);


                progress.ItemCount = (uint)(stream.Length / 1048576);
                bool isCopying = true;
                try
                {
                    var task = new Task(() =>
                    {
                        while (isCopying)
                        {
                            progress.ProcessedItems = (uint)(stream.Position / 1048576);
                            Thread.Sleep(200);
                        }
                    });
                }
                catch (Exception)
                {
                    // ignored
                }

                using var response = await NetworkExtensions.HttpClient.PutAsync(url, httpContent, progress.Token);
                isCopying = false;
                return response;
                }
            case RequestMethod.TCP:
            case RequestMethod.UDP:
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, _method == RequestMethod.TCP ? ProtocolType.Tcp : ProtocolType.Udp)
                {
                    ReceiveTimeout = 10
                };
                var ipAd = IPAddress.Parse(host);
                await socket.ConnectAsync(ipAd, port);

                if (string.IsNullOrWhiteSpace(uploadFilePath))
                {
                    if (string.IsNullOrWhiteSpace(formattedPathWithParameter))
                    {
                        await socket.SendAsync(ReadOnlyMemory<byte>.Empty, SocketFlags.None);
                    }
                    else
                    {
                        var requestBytes = Encoding.UTF8.GetBytes(formattedPathWithParameter);
                        var bytesSent = 0;
                        while (bytesSent < requestBytes.Length)
                        {
                            bytesSent += await socket.SendAsync(requestBytes.AsMemory(bytesSent), SocketFlags.None);
                        }
                    }
                }
                else // file upload
                {
                    if (string.IsNullOrWhiteSpace(formattedPathWithParameter))
                    {
                        await socket.SendFileAsync(uploadFilePath, progress.Token);
                    }
                    else
                    {
                        var preRequestBytes = Encoding.UTF8.GetBytes(formattedPathWithParameter);
                        await socket.SendFileAsync(uploadFilePath, preRequestBytes, ReadOnlyMemory<byte>.Empty, TransmitFileOptions.UseDefaultWorkerThread, progress.Token);
                    }
                }

                var responseBytes = new byte[1024];
                var sb = new StringBuilder();
                int bytesReceived; // Receiving 0 bytes means EOF has been reached
                while ((bytesReceived = await socket.ReceiveAsync(responseBytes, SocketFlags.None, progress.Token)) > 0)
                {
                    var result = Encoding.UTF8.GetString(responseBytes, 0, bytesReceived);
                    sb.Append(result);
                    if (result.EndsWith(",end")) break;
                }

                socket.Shutdown(SocketShutdown.Both);

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sb.ToString()),
                };
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(Method));
        }
    }

    public async Task<HttpResponseMessage> SendRequest(RemotePrinter remotePrinter,
        OperationProgress? progress = null, string? param1 = null, string? uploadFilePath = null)
        => await SendRequest(remotePrinter.Host, remotePrinter.Port, progress, param1, uploadFilePath);

    public RemotePrinterRequest Clone()
    {
        return (RemotePrinterRequest)MemberwiseClone();
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
        return HashCode.Combine(_path);
    }

    #endregion
}