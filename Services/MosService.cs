using Mosviewer.Domain;
using Mosviewer.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Mosviewer.Services
{
    public class MosService
    {
        private readonly MosRepository _repo;

        public class Forecast
        {
            public string Param { get; init; } = null!;
            public int Count { get; init; }
            public List<ForecastValue> Values { get; init; } = null!;
        }
        public class ForecastValue
        {
            private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            [JsonIgnore]
            public DateTime ForecastDate { get; init; }
            public int Time => (int)(ForecastDate - _epoch).TotalSeconds;
            public decimal Value { get; init; }
        }

        public MosService(MosRepository repo)
        {
            _repo = repo;
        }

        public List<Station> GetAllStations() => _repo.GetAllStations();

        public List<Station> GetNearbyStations(double lat, double lng, double distance)
        {
            if (distance < 0) { return new(); }

            double rad = Math.PI / 180.0;
            double kmPerLat = 6371.2 * 2 * Math.PI / 360.0;
            double kmPerLng = Math.Max(1e-6, Math.Abs(kmPerLat * Math.Cos(lat * rad)));

            return _repo.GetStations(s =>
            (double)s.Lat >= lat - distance / kmPerLat && (double)s.Lat <= lat + distance / kmPerLat &&
            (double)s.Lng >= lng - distance / kmPerLng && (double)s.Lng <= lng + distance / kmPerLng);
        }

        public List<Forecast>? GetStationValues(string id)
        {
            var values = _repo.GetStationsWithValues(s => s.Id == id).FirstOrDefault()?.Values;
            if (values == null) { return null; }

            return values
                .GroupBy(v => v.Parameter)
                .Select(g => new Forecast
                {
                    Param = g.Key,
                    Count = g.Count(x => x.Value.HasValue),
                    Values = g
                        .Where(g => g.Value.HasValue)
                        .OrderBy(g => g.ForecastDate)
                        .Select(g => new ForecastValue { ForecastDate = g.ForecastDate, Value = g.Value!.Value })
                        .ToList()
                })
                .ToList();
        }



    }
}
