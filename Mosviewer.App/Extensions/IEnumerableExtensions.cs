using Mosviewer.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mosviewer.Extensions
{
    public static class IEnumerableExtensions
    {
        /// <summary>
        /// Berechnet das gleitende Mittel aus einem Array von Werten.
        /// </summary>
        /// <returns>Enumerator mit der gleien Anzahl von Elementen wie values.</returns>
        public static IEnumerable<decimal?> MovingAverage<TSource>(
            this IEnumerable<TSource> values,
            Func<TSource, decimal?> selector,
            int windowSize)
        {
            var buffer = new decimal?[windowSize];
            int halfWindow = windowSize / 2;
            int i = 0;

            using IEnumerator<TSource> enumerator = values.GetEnumerator();
            for (; i < halfWindow - 1; i++)
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }
                buffer[i] = selector(enumerator.Current);
            }
            for (; i < windowSize - 1; i++)
            {
                if (!enumerator.MoveNext())
                {
                    yield break;
                }
                buffer[i] = selector(enumerator.Current);
                yield return default;
            }
            while (enumerator.MoveNext())
            {
                buffer[i++ % 24] = selector(enumerator.Current);
                yield return buffer.Average();
            }
            for (int j = halfWindow - 1; j > 0; j--)
                yield return default;
        }

        //public static decimal? Average(this Span<decimal?> data)
        //{
        //    int count = 0;
        //    decimal sum = 0;
        //    foreach (var x in data)
        //    {
        //        if (x.HasValue) { count++; sum += x.Value; }
        //    }
        //    return count > 0 ? sum / count : null;
        //}
    }
}
