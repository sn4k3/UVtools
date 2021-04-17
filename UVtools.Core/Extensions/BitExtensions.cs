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
    }
}
