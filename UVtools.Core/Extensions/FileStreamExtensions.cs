/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */

using System;
using System.IO;

namespace UVtools.Core.Extensions
{
    public static class FileStreamExtensions
    {
        public static uint ReadBytes(this FileStream fs, byte[] bytes, int offset = 0)
        {
            return (uint)fs.Read(bytes, offset, bytes.Length);
        }

        public static byte[] ReadBytes(this FileStream fs, int length, int offset = 0)
        {
            var buffer = new byte[length];
            fs.Read(buffer, offset, length);
            return buffer;
        }

        public static byte[] ReadBytes(this FileStream fs, uint length, int offset = 0)
            => fs.ReadBytes((int)length, offset);

        public static uint ReadUShortLittleEndian(this FileStream fs, int offset = 0)
        {
            return BitExtensions.ToUShortLittleEndian(fs.ReadBytes(2, offset));
        }

        public static uint ReadUShortBigEndian(this FileStream fs, int offset = 0)
        {
            return BitExtensions.ToUShortBigEndian(fs.ReadBytes(2, offset));
        }

        public static uint ReadUIntLittleEndian(this FileStream fs, int offset = 0)
        {
            return BitExtensions.ToUIntLittleEndian(fs.ReadBytes(4, offset));
        }

        public static uint ReadUIntBigEndian(this FileStream fs, int offset = 0)
        {
            return BitExtensions.ToUIntBigEndian(fs.ReadBytes(4, offset));
        }

        public static void WriteUShortLittleEndian(this FileStream fs, ushort value, int offset = 0)
        {
            fs.WriteBytes(BitExtensions.ToBytesLittleEndian(value), offset);
        }

        public static void WriteUShortBigEndian(this FileStream fs, ushort value, int offset = 0)
        {
            fs.WriteBytes(BitExtensions.ToBytesBigEndian(value), offset);
        }

        public static void WriteUIntLittleEndian(this FileStream fs, uint value, int offset = 0)
        {
            fs.WriteBytes(BitExtensions.ToBytesLittleEndian(value), offset);
        }

        public static void WriteUIntBigEndian(this FileStream fs, uint value, int offset = 0)
        {
            fs.WriteBytes(BitExtensions.ToBytesBigEndian(value), offset);
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

        /// <summary>
        /// Seek to a position, do work and rewind to initial position
        /// </summary>
        /// <param name="fs"></param>
        /// <param name="action"></param>
        /// <param name="offset"></param>
        /// <param name="seekOrigin"></param>
        public static void SeekDoWorkAndRewind(this FileStream fs, long offset, Action action)
            => fs.SeekDoWorkAndRewind(offset, SeekOrigin.Begin, action);

        public static void SeekDoWorkAndRewind(this FileStream fs, long offset, SeekOrigin seekOrigin, Action action)
        {
            var currentPos = fs.Position;
            fs.Seek(offset, seekOrigin); // Go to
            action.Invoke(); // Do work
            fs.Seek(currentPos, SeekOrigin.Begin); // Rewind
        }
    }
}
