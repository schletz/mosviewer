using Mosviewer.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mosviewer.Extensions
{
    public static class IEnumerableExtensions
    {
        public static decimal?[] MovingAverage(this IEnumerable<decimal?> values, int count, int windowSize)
        {
            var result = new decimal?[count];
            var buffer = new decimal?[windowSize];

            using IEnumerator<decimal?> enumerator = values.GetEnumerator();
            for (int i = 0; i < windowSize - 1; i++)
            {
                if (!enumerator.MoveNext())
                {
                    return Array.Empty<decimal?>();
                }
                buffer[i] = enumerator.Current;
            }
            for (int i = windowSize - 1, j = windowSize / 2; enumerator.MoveNext(); i++, j++)
            {
                buffer[i % 24] = enumerator.Current;
                result[j] = buffer.Average();
            }
            return result;
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
