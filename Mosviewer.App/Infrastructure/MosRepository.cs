using Mosviewer.Domain;
using Mosviewer.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Mosviewer.Infrastructure
{
    public class MosRepository
    {
        private static readonly string _directory = "../mosdata";


        public Modelinfo? GetModelinfo()
        {
            var filename = Path.Combine(_directory, "modelinfo.dat");
            if (!File.Exists(filename)) { return null; }

            using var reader = new BinaryReader(File.Open(
                filename,
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            return Modelinfo.Deserialize(reader);
        }

        public Station? GetStation(string id) =>
            ReadStationFile().FirstOrDefault(s => s.Id == id);

        public List<Station> GetStations(Func<Station, bool> predicate) =>
            ReadStationFile().Where(predicate).ToList();

        public List<Station> GetAllStations() =>
            ReadStationFile().ToList();

        public List<StationValue> GetStationValues(string id, decimal lng)
        {
            var data = ReadStationValueFile(id).ToList();

            var temp = data.Where(v => v.Parameter == "TTT");
            Debug.Assert(temp.MovingAverage(v => v.Value, 24).Count() == temp.Count());
            using var tavgValues = temp.MovingAverage(v => v.Value, 24).GetEnumerator();
            var tavg = temp
                .Select(v =>
                {
                    tavgValues.MoveNext();
                    return new StationValue
                    {
                        ForecastDate = v.ForecastDate,
                        Parameter = "TAVG",
                        StationId = v.StationId,
                        Value = tavgValues.Current
                    };
                });

            // Ermittelt die Maximaltemperaturen des lokalen Tages (Zeitzone).
            // Es werden nur die Maximaltemperaturen, die nach 12:00 Lokalzeit auftreten, verwendet.
            var tmaxLocal = data.Where(v => v.Parameter == "TX" && v.Value.HasValue && v.ForecastDate.Hour >= 12 - lng / 15)
                .GroupBy(v => v.ForecastDate.AddHours((double)lng / 15).Date)
                .Select(g =>
                {
                    var max = g.OrderByDescending(v => v.Value).First();
                    return new StationValue
                    {
                        ForecastDate = max.ForecastDate,
                        Parameter = "TXLOCAL",
                        StationId = max.StationId,
                        Value = max.Value
                    };
                });

            // Ermittelt die Minimaltemperaturen des lokalen Tages (Zeitzone).
            // Es werden nur die Minimaltemperaturen, die bis 12:00 Lokalzeit auftreten, verwendet.
            var tminLocal = data.Where(v => v.Parameter == "TN" && v.Value.HasValue && v.ForecastDate.Hour < 12 - lng / 15)
                .GroupBy(v => v.ForecastDate.AddHours((double)lng / 15).Date)
                .Select(g =>
                {
                    var min = g.OrderBy(v => v.Value).First();
                    return new StationValue
                    {
                        ForecastDate = min.ForecastDate,
                        Parameter = "TNLOCAL",
                        StationId = min.StationId,
                        Value = min.Value
                    };
                });

            return data.Concat(tavg).Concat(tmaxLocal).Concat(tminLocal).ToList();
        }

        private IEnumerable<Station> ReadStationFile()
        {
            var filename = Path.Combine(_directory, "stations.dat");
            if (!File.Exists(filename)) { yield break; }

            using var reader = new BinaryReader(File.Open(
                filename,
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            long length = reader.BaseStream.Length;

            while (reader.BaseStream.Position != length)
            {
                yield return Station.Deserialize(reader);
            }
        }

        private IEnumerable<StationValue> ReadStationValueFile(string stationId)
        {
            var filename = Path.Combine(_directory, stationId + ".dat");
            if (!File.Exists(filename)) { yield break; }

            using var reader = new BinaryReader(File.Open(
                filename,
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            long length = reader.BaseStream.Length;

            while (reader.BaseStream.Position != length)
            {
                yield return StationValue.Deserialize(reader);
            }
        }
    }
}
