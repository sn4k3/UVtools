/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace UVtools.Core.Extensions
{
    public static class XmlExtensions
    {
        public static string SerializeObject(object toSerialize)
        {
            var xmlSerializer = new XmlSerializer(toSerialize.GetType());
            
            using var textWriter = new StringWriter();
            xmlSerializer.Serialize(textWriter, toSerialize);
            return textWriter.ToString();
        }

        public static string SerializeObject(object toSerialize, XmlWriterSettings settings, bool standalone = false)
        {
            settings.CloseOutput = false;

            using var ms = new MemoryStream();
            using (var xw = XmlWriter.Create(ms, settings))
            {
                xw.WriteStartDocument(standalone); // that bool parameter is called "standalone"

                var s = new XmlSerializer(toSerialize.GetType());
                s.Serialize(xw, toSerialize);
            }

            return settings.Encoding.GetString(ms.ToArray());
        }

        public static string SerializeObject(object toSerialize, Encoding encoding, bool indent = true, bool omitXmlDeclaration = false, bool standalone = false)
        {
            var settings = new XmlWriterSettings
            {
                // If set to true XmlWriter would close MemoryStream automatically and using would then do double dispose
                // Code analysis does not understand that. That's why there is a suppress message.
                CloseOutput = false,
                Encoding = encoding,
                OmitXmlDeclaration = omitXmlDeclaration,
                Indent = indent,
            };
            return SerializeObject(toSerialize, settings, standalone);
        }

        public static T DeserializeObject<T>(string text)
        {
            var serializer = new XmlSerializer(typeof(T));
            using TextReader reader = new StringReader(text);
            return (T)serializer.Deserialize(reader);
        }

    }
}
