/*
 *                     GNU AFFERO GENERAL PUBLIC LICENSE
 *                       Version 3, 19 November 2007
 *  Copyright (C) 2007 Free Software Foundation, Inc. <https://fsf.org/>
 *  Everyone is permitted to copy and distribute verbatim copies
 *  of this license document, but changing it is not allowed.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UVtools.Core.Extensions
{
    public static class ParallelExtensions
    {
        public static void ForAllInApproximateOrder<TSource>(this ParallelQuery<TSource> source, Action<TSource> action)
        {
            Partitioner.Create(source)
                .AsParallel()
                .AsOrdered()
                .ForAll(action);
        }

        public static void ForEachInApproximateOrder<TSource>(this ParallelQuery<TSource> source, Action<TSource> action)
        {

            source = Partitioner.Create(source)
                .AsParallel()
                .AsOrdered();

            Parallel.ForEach(source, action);

        }

        public static IEnumerable<T1> OrderedParallel<T, T1>(this IEnumerable<T> list, Func<T, T1> action)
        {
            var unorderedResult = new ConcurrentBag<(long, T1)>();
            Parallel.ForEach(list, (o, state, i) =>
            {
                unorderedResult.Add((i, action.Invoke(o)));
            });
            var ordered = unorderedResult.OrderBy(o => o.Item1);
            return ordered.Select(o => o.Item2);
        }
    }
}
