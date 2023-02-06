using Mosviewer.Domain;
using Mosviewer.Extensions;
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
            public long MinTime { get; init; }
            public long MaxTime { get; init; }
            public int Count { get; init; }
            public decimal? MinValue { get; init; }
            public decimal? MaxValue { get; init; }
            public List<decimal[]> Values { get; init; } = new();
        }

        public MosService(MosRepository repo)
        {
            _repo = repo;
        }

        public List<Station> GetAllStations() => _repo.GetAllStations();
        public Station? GetStation(string id) => _repo.GetStation(id);
        public Modelinfo? GetModelinfo() => _repo.GetModelinfo();

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

        public Dictionary<string, Forecast>? GetForecast(string stationId, decimal lng)
        {
            var values = _repo.GetStationValues(stationId, lng);
            if (values == null) { return null; }


            return values
                .GroupBy(v => v.Parameter)
                .Select(g => new Forecast
                {
                    Param = g.Key,
                    MinTime = g.Min(g => g.ForecastDate).ToJavascriptTimestamp(),
                    MaxTime = g.Max(g => g.ForecastDate).ToJavascriptTimestamp(),
                    MinValue = g.Min(g => g.Value),
                    MaxValue = g.Max(g => g.Value),
                    Count = g.Count(x => x.Value.HasValue),
                    Values = g
                        .Where(g => g.Value.HasValue)
                        .OrderBy(g => g.ForecastDate)
                        .Select(g => new decimal[] { g.ForecastDate.ToJavascriptTimestamp(), g.Value!.Value })
                        .ToList()
                })
                .ToDictionary(f => f.Param, f => f);
        }



    }
}
