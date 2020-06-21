/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using BinarySerialization;
using Newtonsoft.Json;
using UVtools.Core.Extensions;

namespace UVtools.Core
{
    /// <summary>
    /// A helper class with utilities
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Gets the <see cref="BinarySerializer"/> instance
        /// </summary>
        public static BinarySerializer Serializer { get; } = new BinarySerializer {Endianness = Endianness.Little };

        public static MemoryStream Serialize(object value)
        {
            MemoryStream stream = new MemoryStream();
            Serializer.Serialize(stream, value);
            return stream;
        }

        public static T Deserialize<T>(Stream stream)
        {
            return Serializer.Deserialize<T>(stream);
        }

        public static uint SerializeWriteFileStream(FileStream fs, object value, int offset = 0)
        {
            using (MemoryStream stream = Helpers.Serialize(value))
            {
                return fs.WriteStream(stream, offset);
            }
        }

        public static T JsonDeserializeObject<T>(Stream stream)
        {
            using (TextReader tr = new StreamReader(stream))
            {
                return JsonConvert.DeserializeObject<T>(tr.ReadToEnd());
            }
        }

        public static SHA1CryptoServiceProvider SHA1 { get; } = new SHA1CryptoServiceProvider();
        public static string ComputeSHA1Hash(byte[] input)
        {
            return Convert.ToBase64String(SHA1.ComputeHash(input));
        }

        public static bool SetPropertyValue(PropertyInfo attribute, object obj, string value)
        {
            var name = attribute.PropertyType.Name.ToLower();
            switch (name)
            {
                case "string":
                    attribute.SetValue(obj, value.Convert<string>());
                    return true;
                case "boolean":
                    if(char.IsDigit(value[0]))
                        attribute.SetValue(obj, !value.Equals(0));
                    else
                        attribute.SetValue(obj, value.Equals("True", StringComparison.InvariantCultureIgnoreCase));
                    return true;
                case "byte":
                    attribute.SetValue(obj, value.Convert<byte>());
                    return true;
                case "uint16":
                    attribute.SetValue(obj, value.Convert<ushort>());
                    return true;
                case "uint32":
                    attribute.SetValue(obj, value.Convert<uint>());
                    return true;
                case "single":
                    attribute.SetValue(obj, (float)Math.Round(float.Parse(value, CultureInfo.InvariantCulture.NumberFormat), 3));
                    return true;
                case "double":
                    attribute.SetValue(obj, Math.Round(double.Parse(value, CultureInfo.InvariantCulture.NumberFormat), 3));
                    return true;
                case "decimal":
                    attribute.SetValue(obj, Math.Round(decimal.Parse(value, CultureInfo.InvariantCulture.NumberFormat), 3));
                    return true;
                default:
                    throw new Exception($"Data type '{name}' not recognized, contact developer.");
            }
        }
    }
}
