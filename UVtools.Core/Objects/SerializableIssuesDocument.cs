/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using UVtools.Core.Extensions;
using UVtools.Core.FileFormats;
using UVtools.Core.Layers;

namespace UVtools.Core.Objects;

public sealed class SerializableIssuesDocument : IReadOnlyList<MainIssue>
{
    public string? FileFullPath { get; init; }
    public string? MachineName { get; init; }
    public uint ResolutionX { get; init; }
    public uint ResolutionY { get; init; }
    public float DisplayWidth { get; init; }
    public float DisplayHeight { get; init; }
    public FlipDirection DisplayMirror { get; init; }
    public float PrintHeight { get; init; }
    public uint LayerCount { get; init; }
    public MainIssue[] Issues { get; init; }

    public SerializableIssuesDocument(FileFormat slicerFile)
    {
        FileFullPath = slicerFile.FileFullPath;
        MachineName = slicerFile.MachineName;
        ResolutionX = slicerFile.ResolutionX;
        ResolutionY = slicerFile.ResolutionY;
        DisplayWidth = slicerFile.DisplayWidth;
        DisplayHeight = slicerFile.DisplayHeight;
        PrintHeight = slicerFile.PrintHeight;
        LayerCount = slicerFile.LayerCount;
        DisplayMirror = slicerFile.DisplayMirror;

        Issues = slicerFile.IssueManager.ToArray();
    }

    public async Task SerializeAsync(string path)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, this, JsonExtensions.SettingsIndent).ConfigureAwait(false);
    }

    public IEnumerator<MainIssue> GetEnumerator()
    {
        return ((IEnumerable<MainIssue>)Issues).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return Issues.GetEnumerator();
    }

    public int Count => Issues.Length;

    public MainIssue this[int index] => Issues[index];
}