using Mosviewer.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mosviewer.Extensions
{
    public static class StationStatisticsExtensions
    {
        /// <summary>
        /// Berechnet das gleitende Mittel aus einem Array von Werten.
        /// </summary>
        /// <returns>Enumerator mit der gleien Anzahl von Elementen wie values.</returns>
        public static IEnumerable<StationValue> MovingAverage(
            this IEnumerable<StationValue> values,
            int windowSize)
        {
            var buffer = new StationValue[windowSize];
            using var enumerator = values.GetEnumerator();
            int i, j;

            for (i = 0; i < windowSize - 1 && enumerator.MoveNext(); i++)
            {
                buffer[i] = enumerator.Current;
            }
            for (j = windowSize / 2; enumerator.MoveNext(); i = (i + 1) % 24, j = (j + 1) % 24)
            {
                buffer[i] = enumerator.Current;

                yield return new StationValue
                {
                    ForecastDate = buffer[j].ForecastDate,
                    Parameter = "TAVG",
                    StationId = buffer[j].StationId,
                    Value = buffer.Average(s => s.Value)
                };
            }
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
