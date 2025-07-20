/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace UVtools.Core.Extensions;

public static class NetworkExtensions
{
    public static readonly HttpClient HttpClient = new()
    {
        DefaultRequestHeaders = { UserAgent = { new ProductInfoHeaderValue(About.Software, About.VersionString) } }
    };

    public static async Task<HttpResponseMessage> DownloadAsync(this HttpClient client, string requestUri, Stream destination, IProgress<(long total, long bytes)>? progress = null, CancellationToken cancellationToken = default)
    {
        // Get the http headers first to examine the content length
        var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        var contentLength = response.Content.Headers.ContentLength;

        await using var download = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        // Ignore progress reporting when no progress reporter was
        // passed or when the content length is unknown
        if (progress is null || !contentLength.HasValue)
        {
            await download.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
            return response;
        }

        // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
        var relativeProgress = new Progress<long>(downloadedBytes => progress.Report(new (contentLength.Value, downloadedBytes)));
        // Use extension method to report progress while downloading
        await download.CopyToAsync(destination, relativeProgress, cancellationToken).ConfigureAwait(false);
        progress.Report(new(contentLength.Value, contentLength.Value));
        return response;
    }

    public static async Task<HttpResponseMessage> DownloadAsync(this HttpClient client, string requestUri, string destinationFilePath, IProgress<(long total, long bytes)>? progress = null, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Open(destinationFilePath, FileMode.Create, FileAccess.Write);
        return await DownloadAsync(client, requestUri, stream, progress, cancellationToken).ConfigureAwait(false);
    }


    public static async Task<HttpResponseMessage> DownloadAsync(string requestUri, Stream destination, IProgress<(long total, long bytes)>? progress = null, CancellationToken cancellationToken = default)
        => await HttpClient.DownloadAsync(requestUri, destination, progress, cancellationToken).ConfigureAwait(false);

    public static async Task<HttpResponseMessage> DownloadAsync(string requestUri, string destinationFilePath, IProgress<(long total, long bytes)>? progress = null, CancellationToken cancellationToken = default)
        => await HttpClient.DownloadAsync(requestUri, destinationFilePath, progress, cancellationToken).ConfigureAwait(false);

}