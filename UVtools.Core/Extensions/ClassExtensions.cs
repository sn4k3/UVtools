/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Serialization;

namespace UVtools.Core.Extensions;

public static class ClassExtensions
{
    private static readonly JsonSerializerOptions JsonCloneSerializeSettings = new()
    {
        //DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters =
        {
            new System.Text.Json.Serialization.JsonStringEnumConverter()
        },
        IncludeFields = true,
    };

    private static readonly XmlWriterSettings XmlCloneSerializeSettings = new()
    {
        // If set to true XmlWriter would close MemoryStream automatically and using would then do double dispose
        // Code analysis does not understand that. That's why there is a suppress message.
        CloseOutput = false,
        Encoding = Encoding.UTF8,
        OmitXmlDeclaration = false,
        Indent = false,
    };

    public static T CloneByJsonSerialization<T>(this T classToClone) where T : class
    {
        var clone = JsonSerializer.SerializeToUtf8Bytes(classToClone, JsonCloneSerializeSettings);
        return JsonSerializer.Deserialize<T>(clone, JsonCloneSerializeSettings)!;
    }

    public static T CloneByXmlSerialization<T>(this T classToClone) where T : class
    {
        //var clone = XmlExtensions.SerializeObject(classToClone, Encoding.UTF8, false);
        //return XmlExtensions.DeserializeFromText<T>(clone);

        using var stream = new MemoryStream();
        using var xmlWriter = XmlWriter.Create(stream, XmlCloneSerializeSettings);
        xmlWriter.WriteStartDocument(false); // that bool parameter is called "standalone"
        var xmlSerializer = new XmlSerializer(classToClone.GetType());
        //XmlSerializerNamespaces? ns = true ? new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty }) : null;
        xmlSerializer.Serialize(xmlWriter, classToClone);

        stream.Seek(0, SeekOrigin.Begin);
        return (T)xmlSerializer.Deserialize(stream)!;
    }
}