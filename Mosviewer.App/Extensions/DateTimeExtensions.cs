using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mosviewer.Extensions
{
    public static class DateTimeExtensions
    {
        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static long ToJavascriptTimestamp(this DateTime dateTime) => (long)(dateTime - _epoch).TotalMilliseconds;
    }
}
