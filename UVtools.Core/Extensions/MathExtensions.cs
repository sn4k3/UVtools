using System;

namespace UVtools.Core.Extensions
{
    public static class MathExtensions
    {
        public static byte Clamp(this byte value, byte min, byte max)
        {
            return value <= min ? min : value >= max ? max : value;
        }

        public static sbyte Clamp(this sbyte value, sbyte min, sbyte max)
        {
            return value <= min ? min : value >= max ? max : value;
        }

        public static ushort Clamp(this ushort value, ushort min, ushort max)
        {
            return value <= min ? min : value >= max ? max : value;
        }

        public static short Clamp(this short value, short min, short max)
        {
            return value <= min ? min : value >= max ? max : value;
        }

        public static uint Clamp(this uint value, uint min, uint max)
        {
            return value <= min ? min : value >= max ? max : value;
        }

        public static int Clamp(this int value, int min, int max)
        {
            return value <= min ? min : value >= max ? max : value;
        }

        public static ulong Clamp(this ulong value, ulong min, ulong max)
        {
            return value <= min ? min : value >= max ? max : value;
        }

        public static long Clamp(this long value, long min, long max)
        {
            return value <= min ? min : value >= max ? max : value;
        }

        public static T Clamp<T>(T value, T min, T max) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0)
                return min;
            if (value.CompareTo(max) > 0)
                return max;

            return value;
        }
    }
}
