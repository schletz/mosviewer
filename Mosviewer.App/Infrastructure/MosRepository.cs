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
            var data = new List<StationValue>(10000);
            data.AddRange(ReadStationValueFile(id));

            data.AddRange(data
                .Where(v => v.Parameter == "TTT")
                .MovingAverage(24)
                .ToList());

            // Ermittelt die Maximaltemperaturen des lokalen Tages (Zeitzone).
            // Es werden nur die Maximaltemperaturen, die nach 12:00 Lokalzeit auftreten, verwendet.
            data.AddRange(data.Where(v => v.Parameter == "TX" && v.Value.HasValue && v.ForecastDate.AddHours((double)lng / 15).Hour >= 12)
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
                })
                .ToList());

            // Ermittelt die Minimaltemperaturen des lokalen Tages (Zeitzone).
            // Es werden nur die Minimaltemperaturen, die bis 12:00 Lokalzeit auftreten, verwendet.
            data.AddRange(data.Where(v => v.Parameter == "TN" && v.Value.HasValue && v.ForecastDate.AddHours((double)lng / 15).Hour < 12)
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
                })
                .ToList());

            return data;
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
