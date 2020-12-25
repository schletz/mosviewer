using Mosviewer.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mosviewer.Extensions
{
    public static class IEnumerableExtensions
    {
        public static StationValue[] MovingAverage(this IEnumerable<StationValue> data, string paramName, int windowSize = 24)
        {
            var values = new Span<decimal?>(data.Select(s => s.Value).ToArray());
            var result = new StationValue[values.Length];
            int halfWindow = windowSize / 2;
            int maxIndex = values.Length - halfWindow;
            int i = 0;

            foreach (var s in data)
            {
                result[i] = new StationValue
                {
                    ForecastDate = s.ForecastDate,
                    Parameter = paramName,
                    StationId = s.StationId,
                    Value = (i >= halfWindow && i <= maxIndex) ? values.Slice(i - halfWindow, windowSize).Average() : null
                };
                i++;
            }
            return result;
        }

        public static decimal? Average(this Span<decimal?> data)
        {
            int count = 0;
            decimal sum = 0;
            foreach (var x in data)
            {
                if (x.HasValue) { count++; sum += x.Value; }
            }
            return count > 0 ? sum / count : null;
        }
    }
}
