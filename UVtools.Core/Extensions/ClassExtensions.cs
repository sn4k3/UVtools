/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.IO;
using System.Reflection;
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

    /// <summary>
    /// Copies every readable public property value from <paramref name="source"/> onto <paramref name="target"/>,
    /// in place. A property whose type is nested inside the type currently being copied (e.g. a settings class
    /// holding sub-settings objects) is recursed into instead of having its reference overwritten, so the
    /// target object graph keeps its original object identity throughout. This matters for objects with live data
    /// bindings or event subscribers keyed to that identity, such as a singleton settings object: restoring it by
    /// reference-swap (<c>target = source</c>) would silently orphan every binding/subscriber still pointing at
    /// the old object, while restoring values in place lets them observe the change through their own setters.
    /// </summary>
    public static void CopyValuesFrom(this object target, object source)
    {
        var type = target.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0) continue;

            var sourceValue = property.GetValue(source);

            if (sourceValue is not null && property.PropertyType.DeclaringType == type)
            {
                var targetValue = property.GetValue(target);
                if (targetValue is not null)
                {
                    targetValue.CopyValuesFrom(sourceValue);
                    continue;
                }
            }

            if (property.CanWrite) property.SetValue(target, sourceValue);
        }
    }
}