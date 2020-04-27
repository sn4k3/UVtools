/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.IO;
using BinarySerialization;
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
        /// <summary>
        /// Gets the <see cref="BinarySerializer"/> instance
        /// </summary>
        public static BinarySerializer Serializer { get; } = new BinarySerializer {Endianness = Endianness.Little };

        /// <summary>
        /// Gets a white color of <see cref="Gray8"/>
        /// </summary>
        public static Gray8 Gray8White { get; } = new Gray8(255);

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
    }
}
