using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mosviewer.Domain
{
    public class Station : IEquatable<Station?>
    {
        public Station() { }

        public string StationId { get; set; } = "";
        public string StationName { get; set; } = "";
        public decimal Lat { get; set; }
        public decimal Lng { get; set; }

        public override bool Equals(object? obj)
        {
            return Equals(obj as Station);
        }

        public bool Equals(Station? other)
        {
            return other != null &&
                   StationId == other.StationId;
        }

        public double GetDistance(Station other)
        {
            return (double)(other.Lat - Lat);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StationId);
        }
    }
    public class MosStationData
    {
        public Station Station { get; set; } = new();
        public Dictionary<string, Dictionary<DateTime, decimal?>> Values { get; set; } = new();
    }



}
