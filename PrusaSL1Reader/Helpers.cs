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
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BinarySerialization;
using Newtonsoft.Json;
using PrusaSL1Reader.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    /// <summary>
    /// A helper class with utilities
    /// </summary>
    public static class Helpers
    {
        public static PngEncoder PngEncoder { get; } = new PngEncoder();
        public static BmpEncoder BmpEncoder { get; } = new BmpEncoder{SupportTransparency = true, BitsPerPixel = BmpBitsPerPixel.Pixel32};
        /// <summary>
        /// Gets the <see cref="BinarySerializer"/> instance
        /// </summary>
        public static BinarySerializer Serializer { get; } = new BinarySerializer {Endianness = Endianness.Little };

        /// <summary>
        /// Gets a white color of <see cref="L8"/>
        /// </summary>
        public static L8 L8White { get; } = new L8(255);
        public static L8 L8Black { get; } = new L8(0);

        public static byte[] ImageL8ToBytes(Image<L8> image)
        {
            return image.TryGetSinglePixelSpan(out var pixelSpan) ? MemoryMarshal.AsBytes(pixelSpan).ToArray() : null;
        }

        /*public static T ByteToType<T>(BinaryReader reader)
        {
            byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }

        public static byte[] SerializeToBytes<T>(T item)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, item);
                stream.Seek(0, SeekOrigin.Begin);
                return stream.ToArray();
            }
        }*/

        public static MemoryStream Serialize(object value)
        {
            MemoryStream stream = new MemoryStream();
            Serializer.Serialize(stream, value);
            return stream;
        }

        public static T Deserialize<T>(BinaryReader binaryReader)
        {
            return Deserialize<T>(binaryReader.BaseStream);
        }

        public static T Deserialize<T>(Stream stream)
        {
            return Serializer.Deserialize<T>(stream);
        }

        public static uint WriteFileStream(FileStream fs, MemoryStream stream, uint offset = 0)
        {
            return WriteFileStream(fs, stream.ToArray(), offset);
        }

        public static uint WriteFileStream(FileStream fs, byte[] bytes, uint offset = 0)
        {
            fs.Write(bytes, 0, bytes.Length);
            return (uint)bytes.Length;
        }

        public static uint SerializeWriteFileStream(FileStream fs, object value, uint offset = 0)
        {
            using (MemoryStream stream = Helpers.Serialize(value))
            {
                return WriteFileStream(fs, stream);
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

        public static uint CoordinatesToPixelIndex(uint x, uint y, uint width)
        {
            return y * width + x;
        }
    }
}
