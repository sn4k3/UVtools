/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace UVtools.Core.Extensions;

public static class SpanExtensions
{
    public static unsafe void Fill<T>(this Span<T> span, Func<T> provider) where T : struct
    {
        int
            cores = Environment.ProcessorCount,
            batch = span.Length / cores,
            mod = span.Length % cores,
            size = Unsafe.SizeOf<T>();
        ref T r0 = ref span.GetPinnableReference();
        fixed (byte* p0 = &Unsafe.As<T, byte>(ref r0))
        {
            byte* p = p0;
            Parallel.For(0, cores, i =>
            {
                byte* pi = p + i * batch * size;
                for (int j = 0; j < batch; j++, pi += size)
                    Unsafe.Write(pi, provider());
            });

            // Remaining values
            if (mod < 1) return;
            for (int i = span.Length - mod; i < span.Length; i++)
                Unsafe.Write(p + i * size, provider());
        }
    }
}