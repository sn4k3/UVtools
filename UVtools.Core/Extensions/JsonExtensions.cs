/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.Text.Json;

namespace UVtools.Core.Extensions;

public static class JsonExtensions
{
    public static readonly JsonSerializerOptions SettingsIndent = new()
    {
        WriteIndented = true
    };
}