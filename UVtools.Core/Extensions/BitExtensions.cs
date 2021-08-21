/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
namespace UVtools.Core.Extensions
{
    public static class BitExtensions
    {
        public static ushort ToUShortLittleEndian(byte byte1, byte byte2) => (ushort)(byte1 + (byte2 << 8));
        public static ushort ToUShortBigEndian(byte byte1, byte byte2) => (ushort)((byte1 << 8) + byte2);

        public static ushort ToUShortLittleEndian(byte[] buffer, int offset = 0)
            => (ushort)(buffer[offset] + (buffer[offset+1] << 8));
        public static ushort ToUShortBigEndian(byte[] buffer, int offset = 0)
            => (ushort)((buffer[offset] << 8) + buffer[offset+1]);

        public static uint ToUIntLittleEndian(byte byte1, byte byte2, byte byte3, byte byte4) 
            => (uint)(byte1 + (byte2 << 8) + (byte3 << 16) + (byte4 << 24));
        public static uint ToUIntBigEndian(byte byte1, byte byte2, byte byte3, byte byte4) 
            => (uint)((byte1 << 24) + (byte2 << 16) + (byte3 << 8) + byte4);

        public static uint ToUIntLittleEndian(byte[] buffer, int offset = 0)
            => (uint)(buffer[offset] + (buffer[offset + 1] << 8) + (buffer[offset + 2] << 16) + (buffer[offset + 3] << 24));
        public static uint ToUIntBigEndian(byte[] buffer, int offset = 0)
            => (uint)((buffer[offset] << 24) + (buffer[offset+1] << 16) + (buffer[offset+2] << 8) + buffer[offset+3]);

        public static byte[] ToBytesLittleEndian(ushort value)
        {
            var bytes = new byte[2];
            ToBytesLittleEndian(value, bytes);
            return bytes;
        }

        public static void ToBytesLittleEndian(ushort value, byte[] buffer, uint offset = 0)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
        }

        public static byte[] ToBytesBigEndian(ushort value)
        {
            var bytes = new byte[2];
            ToBytesBigEndian(value, bytes);
            return bytes;
        }

        public static void ToBytesBigEndian(ushort value, byte[] buffer, uint offset = 0)
        {
            buffer[offset] = (byte)(value >> 8);
            buffer[offset + 1] = (byte)value;
        }

        public static byte[] ToBytesLittleEndian(uint value)
        {
            var bytes = new byte[4];
            ToBytesLittleEndian(value, bytes);
            return bytes;
        }

        public static void ToBytesLittleEndian(uint value, byte[] buffer, uint offset = 0)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }

        public static byte[] ToBytesBigEndian(uint value)
        {
            var bytes = new byte[4];
            ToBytesBigEndian(value, bytes);
            return bytes;
        }

        public static void ToBytesBigEndian(uint value, byte[] buffer, uint offset = 0)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }

        public static byte[] ToBytesLittleEndian(int value)
        {
            var bytes = new byte[4];
            ToBytesLittleEndian(value, bytes);
            return bytes;
        }

        public static void ToBytesLittleEndian(int value, byte[] buffer, uint offset = 0)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);
        }

        public static byte[] ToBytesBigEndian(int value)
        {
            var bytes = new byte[4];
            ToBytesBigEndian(value, bytes);
            return bytes;
        }

        public static void ToBytesBigEndian(int value, byte[] buffer, uint offset = 0)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }


        public static byte[] ToBytesLittleEndian(ulong value)
        {
            var bytes = new byte[8];
            ToBytesLittleEndian(value, bytes);
            return bytes;
        }

        public static void ToBytesLittleEndian(ulong value, byte[] buffer, ulong offset = 0)
        {
            buffer[offset] = (byte)value;
            buffer[offset + 1] = (byte)(value >> 8);
            buffer[offset + 2] = (byte)(value >> 16);
            buffer[offset + 3] = (byte)(value >> 24);

            buffer[offset + 4] = (byte)(value >> 32);
            buffer[offset + 5] = (byte)(value >> 40);
            buffer[offset + 6] = (byte)(value >> 48);
            buffer[offset + 7] = (byte)(value >> 56);
        }

        public static byte[] ToBytesBigEndian(ulong value)
        {
            var bytes = new byte[8];
            ToBytesBigEndian(value, bytes);
            return bytes;
        }

        public static void ToBytesBigEndian(ulong value, byte[] buffer, ulong offset = 0)
        {
            buffer[offset] = (byte)(value >> 56);
            buffer[offset + 1] = (byte)(value >> 48);
            buffer[offset + 2] = (byte)(value >> 40);
            buffer[offset + 3] = (byte)(value >> 32);
            buffer[offset + 4] = (byte)(value >> 24);
            buffer[offset + 5] = (byte)(value >> 16);
            buffer[offset + 6] = (byte)(value >> 8);
            buffer[offset + 7] = (byte)value;
        }


    }
}
