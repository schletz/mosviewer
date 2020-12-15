using Mosviewer.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Mosviewer.Infrastructure
{
    public class MosRepository
    {
        private static readonly string _directory = "mosdata";

        public List<Station> GetAllStations()
        {
            return ReadStationFile().ToList();
        }

        public List<Station> GetStations(Func<Station, bool> predicate)
        {
            return ReadStationFile().Where(predicate).ToList();
        }


        public IEnumerable<Station> GetStationsWithValues(Func<Station, bool> predicate)
        {
            return ReadStationFile()
                .Where(predicate)
                .Select(s =>
                {
                    s.Values = ReadStationValueFile(s.Id).ToList();
                    return s;
                });

        }

        private IEnumerable<Station> ReadStationFile()
        {
            var filename = Path.Combine(_directory, "stations.dat");
            if (!File.Exists(filename)) { yield break; }

            using var reader = new BinaryReader(File.Open(
                filename, 
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            bool endOfStream = false;
            while (!endOfStream)
            {
                Station? station = null;
                try
                {
                    station = Station.Deserialize(reader);
                }
                catch (EndOfStreamException)
                {
                    endOfStream = true;
                }
                if (station != null) { yield return station; }
            }
        }

        private IEnumerable<StationValue> ReadStationValueFile(string stationId)
        {
            var filename = Path.Combine(_directory, stationId + ".dat");
            if (!File.Exists(filename)) { yield break; }

            using var reader = new BinaryReader(File.Open(
                filename, 
                FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            bool endOfStream = false;
            while (!endOfStream)
            {
                StationValue? stationValue = null;
                try
                {
                    stationValue = StationValue.Deserialize(reader);
                }
                catch (EndOfStreamException)
                {
                    endOfStream = true;
                }
                if (stationValue != null) { yield return stationValue; }
            }
        }
    }
}
