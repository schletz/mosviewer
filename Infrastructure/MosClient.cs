using Mosviewer.Domain;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace Mosviewer.Infrastructure
{

    public class MosClient
    {
        private static readonly string _url = "https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/";
        private readonly HttpClient _httpClient;
        //private readonly Repository _repo;

        private static readonly Dictionary<string, Func<DateTime, decimal, decimal?>> _valueConverters = new()
        {
            { "TTT", (time, val) => val - 273.15M },
            { "T5CM", (time, val) => val - 273.15M },
            { "TD", (time, val) => val - 273.15M },
            { "TX", (time, val) => time.Hour % 6 == 0 ? val - 273.15M : null },
            { "TN", (time, val) => time.Hour % 6 == 0 ? val - 273.15M : null }
        };

        public MosClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            //_repo = repo;
        }

        public async Task<List<MosFile>> GetFilelisting()
        {
            using var response = await (await _httpClient.GetAsync(_url))
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsStreamAsync();
            using var reader = new StreamReader(response, System.Text.Encoding.UTF8);
            string? line;
            List<MosFile> result = new();
            while ((line = reader.ReadLine()) != null)
            {
                // Parse <a href="MOSMIX_S_2020121204_240.kmz">MOSMIX_S_2020121204_240.kmz</a>                        12-Dec-2020 04:23            35836678
                var match = Regex.Match(line, @"<a href=""([^""]+)"">([^<]+)</a>\s+(.{17})");
                if (match.Success)
                {
                    result.Add(new MosFile(
                        _url + match.Groups[1].Value,
                        match.Groups[2].Value,
                        DateTime.ParseExact(
                            match.Groups[3].Value,
                            "dd-MMM-yyyy HH:mm",
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.AssumeUniversal)));
                }
            }
            if (result.Count == 0)
            {
                throw new ApplicationException($"{_url} enthält keine Dateien.");
            }
            return result;
        }

        public async Task<MosFile> GetLatestFile() =>
            (await GetFilelisting()).OrderByDescending(f => f.LastUpdate).First();

        public async Task ReadStationData(MosFile file)
        {
            var timeSteps = new List<DateTime>();
            var taskList = new List<Task>();
            var station = new Station();

            using var zipStream = await (await _httpClient.GetAsync(file.Link))
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsStreamAsync();
            using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);
            var fileEntry = archive.Entries[0];
            using var decompressedStream = fileEntry.Open();
            using var decompressedStreamReader = new StreamReader(decompressedStream, System.Text.Encoding.GetEncoding("ISO-8859-1"));
            using var reader = XmlReader.Create(decompressedStreamReader);
            using var stationWriter = new BinaryWriter(File.Open(@"stations.dat", FileMode.Create, FileAccess.Write, FileShare.Read));
            Console.WriteLine($"{DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond} Lese XML...");
            reader.ReadToFollowing("dwd:ProductDefinition");
            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "dwd:TimeStep")
                    {
                        timeSteps.Add(DateTime.Parse(
                            reader.ReadElementContentAsString(),
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.AssumeUniversal));
                    }
                    if (reader.Name == "kml:Placemark")
                    {
                        station = new();
                    }
                    if (reader.Name == "kml:name")
                    {
                        station.Id = reader.ReadElementContentAsString();
                    }
                    if (reader.Name == "kml:coordinates")
                    {
                        var value = reader.ReadElementContentAsString().Split(",");
                        if (value.Length == 3)
                        {
                            station.Lng = decimal.Parse(value[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                            station.Lat = decimal.Parse(value[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                            station.Elevation = decimal.Parse(value[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);
                        }
                    }
                    if (reader.Name == "kml:description")
                    {
                        station.Name = reader.ReadElementContentAsString();
                    }
                    if (reader.Name == "kml:ExtendedData")
                    {
                        taskList.Add(ReadStation(station.Id, timeSteps, reader.ReadOuterXml()));
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.Name == "kml:Placemark")
                    {
                        station.Serialize(stationWriter);
                    }
                }
            }
            await Task.WhenAll(taskList);
            Console.WriteLine($"Fertig gelesen.");
        }

        private Task ReadStation(
            string stationId, List<DateTime> timeSteps, string xmlStr)
        {
            return Task.Run(() =>
            {
                var filename = Path.Combine("stationvalues", stationId + ".dat");
                var stationValues = new List<StationValue>(7200);
                string? curentElementName = null!;
                Func<DateTime, decimal, decimal?> converter = null!;

                using var xmlStreamReader = new StringReader(xmlStr);
                using var reader = XmlReader.Create(xmlStreamReader);
                using var stationValuesWriter = new BinaryWriter(File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read));


                while (reader.Read())
                {
                    if (reader.Name == "dwd:Forecast")
                    {
                        try
                        {
                            curentElementName = reader.GetAttribute("dwd:elementName")?.ToUpper()
                                ?? throw new Exception();
                            converter = _valueConverters[curentElementName];
                        }
                        catch
                        {
                            converter = (time, val) => val;
                            curentElementName = null;
                        }
                    }
                    if (reader.Name == "dwd:value" && curentElementName != null)
                    {
                        string[] values = Regex.Split(reader.ReadElementContentAsString().Trim(), @"\s+");

                        if (values.Length == timeSteps.Count)
                        {
                            var valueDictionary = new Dictionary<DateTime, decimal?>();
                            int i = 0;
                            foreach (string v in values)
                            {
                                decimal? value = null;
                                var time = timeSteps[i++];
                                if (decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal parsedValue))
                                {
                                    value = converter(time, parsedValue);
                                }
                                var stationValue = new StationValue
                                {
                                    StationId = stationId,
                                    Parameter = curentElementName,
                                    ForecastDate = time,
                                    Value = value
                                };
                                stationValue.Serialize(stationValuesWriter);
                            }
                        }
                    }
                }
            });


        }
    }
}
