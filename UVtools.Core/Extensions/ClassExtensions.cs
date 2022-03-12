/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.Text.Json;

namespace UVtools.Core.Extensions;

public static class ClassExtensions
{
    public static T? CloneByJsonSerialization<T>(this T classToClone) where T : class
    {
        var clone = JsonSerializer.SerializeToUtf8Bytes(classToClone);
        return JsonSerializer.Deserialize<T>(clone);
    }

    public static T CloneByXmlSerialization<T>(this T classToClone) where T : class
    {
        var clone = XmlExtensions.SerializeObject(classToClone);
        return XmlExtensions.DeserializeFromText<T>(clone);
    }
}