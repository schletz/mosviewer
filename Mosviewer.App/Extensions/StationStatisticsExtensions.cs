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

        public static IEnumerable<StationValue> GetLocalMaxMin(this IEnumerable<StationValue> values, decimal lng)
        {
            double offset = (double)lng / 15;
            using var enumerator = values.GetEnumerator();
            StationValue? max = null, min = null;

            bool hasNext = enumerator.MoveNext();
            if (!hasNext) { yield break; }
            StationValue last = enumerator.Current;

            while (hasNext)
            {
                StationValue current = enumerator.Current;
                DateTime localDate = current.ForecastDate.AddHours(offset);
                hasNext = enumerator.MoveNext();
                bool dateChanged = localDate.Date != last.ForecastDate.AddHours(offset).Date;
                if (!dateChanged)
                {
                    if (current.Parameter == "TX" && current.Value.HasValue && localDate.Hour >= 12)
                    {
                        if (max == null || current.Value > max.Value)
                        {
                            max = current;
                        }
                    }
                    if (current.Parameter == "TN" && current.Value.HasValue && localDate.Hour < 12)
                    {
                        if (min == null || current.Value < min.Value)
                        {
                            min = current;
                        }
                    }
                }
                if (dateChanged || !hasNext)
                {
                    if (max != null)
                    {
                        yield return new StationValue
                        {
                            ForecastDate = max.ForecastDate,
                            Parameter = "TXLOCAL",
                            StationId = max.StationId,
                            Value = max.Value
                        };
                        max = null;
                    }

                    if (min != null)
                    {
                        yield return new StationValue
                        {
                            ForecastDate = min.ForecastDate,
                            Parameter = "TNLOCAL",
                            StationId = min.StationId,
                            Value = min.Value
                        };
                        min = null;
                    }
                }
                last = current;
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
