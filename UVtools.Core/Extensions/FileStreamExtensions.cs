/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System.IO;

namespace UVtools.Core.Extensions
{
    public static class FileStreamExtensions
    {
        public static uint ReadBytes(this FileStream fs, byte[] bytes, int offset = 0)
        {
            return (uint)fs.Read(bytes, offset, bytes.Length);
        }

        public static uint WriteStream(this FileStream fs, MemoryStream stream, int offset = 0)
        {
            return fs.WriteBytes(stream.ToArray(), offset);
        }

        public static uint WriteBytes(this FileStream fs, byte[] bytes, int offset = 0)
        {
            fs.Write(bytes, offset, bytes.Length);
            return (uint)bytes.Length;
        }

        public static uint WriteBytes(this Stream stream, byte[] bytes, int offset = 0)
        {
            stream.Write(bytes, offset, bytes.Length);
            return (uint)bytes.Length;
        }

        public static uint WriteSerialize(this FileStream fs, object data, int offset = 0)
        {
            return Helpers.SerializeWriteFileStream(fs, data, offset);
        }
    }
}
