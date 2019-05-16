#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sanakan.Extensions
{
    public static class IEnumerableExtension
    {
        public static IEnumerable<T> Shuffle<T> (this IEnumerable<T> list, Random rng)
        {
            var buffer = list.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }

        public static IEnumerable<T> Shuffle<T> (this IEnumerable<T> list)
        {
            var buffer = list.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = Services.Fun.GetRandomValue(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }
    }
}
