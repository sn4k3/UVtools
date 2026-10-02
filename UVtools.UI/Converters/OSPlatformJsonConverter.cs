/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UVtools.UI.Converters;

public sealed class OSPlatformJsonConverter : JsonConverter<OSPlatform>
{
    public override OSPlatform Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("Expected a non-empty operating system platform name.");
        }

        var platform = reader.GetString();
        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new JsonException("Expected a non-empty operating system platform name.");
        }

        return OSPlatform.Create(platform);
    }

    public override void Write(Utf8JsonWriter writer, OSPlatform value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
