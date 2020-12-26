using Mosviewer.Domain;
using Mosviewer.Extensions;
using System;
using System.Collections.Generic;
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

        public List<StationValue> GetStationValues(string id)
        {
            var data = ReadStationValueFile(id).ToList();

            var temp = data.Where(v => v.Parameter == "TTT");
            var tavgValues = temp.Select(v => v.Value).MovingAverage(240, 24);
            int i = 0;
            var tavg = temp
                .Select(v => new StationValue
                {
                    ForecastDate = v.ForecastDate,
                    Parameter = "TAVG",
                    StationId = v.StationId,
                    Value = tavgValues[i++]
                });
            return data.Concat(tavg).ToList();
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
