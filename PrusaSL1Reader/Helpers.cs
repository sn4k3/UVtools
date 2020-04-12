/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using BinarySerialization;
using SixLabors.ImageSharp.PixelFormats;

namespace PrusaSL1Reader
{
    public static class Helpers
    {
        public static Type[] AvaliableFileFormats { get; } = {
            typeof(SL1File), 
            typeof(CbddlpFile)
        };
        public static BinarySerializer Serializer { get; } = new BinarySerializer {Endianness = Endianness.Little };
        public static Gray8 Gray8White { get; } = new Gray8(255);
        public static T ByteToType<T>(BinaryReader reader)
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
        }

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
            return Helpers.Serializer.Deserialize<T>(stream);
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
                return (uint)Helpers.WriteFileStream(fs, stream);
            }
        }
    }
}
