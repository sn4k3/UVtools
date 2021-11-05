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

        /// <summary>
        /// Read from current position to the end of the file
        /// </summary>
        /// <param name="fs"></param>
        /// <returns></returns>
        public static byte[] ReadToEnd(this FileStream fs)
        {
            return fs.ReadBytes((uint)(fs.Length - fs.Position));
        }

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

        public static void WriteFloatLittleEndian(this FileStream fs, float value, int offset = 0)
        {
            var bytes = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian) Array.Reverse(bytes); //reverse it so we get little endian.
            fs.WriteBytes(BitConverter.GetBytes(value), offset);
        }
        public static void WriteFloatBigEndian(this FileStream fs, float value, int offset = 0)
        {
            var bytes = BitConverter.GetBytes(value);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes); //reverse it so we get big endian.
            fs.WriteBytes(BitConverter.GetBytes(value), offset);
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

        public static uint WriteString(this FileStream fs, string text, int offset = 0)
            => fs.WriteString(text, Encoding.UTF8, offset);

        public static uint WriteString(this FileStream fs, string text, Encoding encoding, int offset = 0)
        {
            var bytes = encoding.GetBytes(text);
            fs.WriteBytes(bytes, offset);
            return (uint)bytes.Length;
        }

        public static uint WriteLine(this FileStream fs) => fs.WriteString(Environment.NewLine);

        public static uint WriteLine(this FileStream fs, string text, int offset = 0)
            => fs.WriteLine(text, Encoding.UTF8, offset);

        public static uint WriteLine(this FileStream fs, string text, Encoding encoding, int offset = 0)
        {
            var bytes = encoding.GetBytes($"{text}{Environment.NewLine}");
            fs.WriteBytes(bytes, offset);
            return (uint)bytes.Length;
        }

        public static uint WriteLineLF(this FileStream fs)
        {
            fs.WriteByte(0x0A);
            return 1;
        }

        public static uint WriteLineLF(this FileStream fs, string text, int offset = 0)
            => fs.WriteLineLF(text, Encoding.UTF8, offset);

        public static uint WriteLineLF(this FileStream fs, string text, Encoding encoding, int offset = 0)
        {
            var bytes = encoding.GetBytes($"{text}\n");
            fs.WriteBytes(bytes, offset);
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
