using Mosviewer.Domain;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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

        private static Dictionary<string, Func<DateTime, decimal, decimal?>> _valueConverters = new()
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

        public async Task<List<MosStationData>> ReadStationData(MosFile file)
        {
            var timeSteps = new List<DateTime>();
            var result = new List<MosStationData>();
            var stationData = new MosStationData();
            string? curentElementName = null;
            Func<DateTime, decimal, decimal?>? converter = null;

            using var zipStream = await (await _httpClient.GetAsync(file.Link))
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsStreamAsync();
            using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                ZipArchiveEntry fileEntry = archive.Entries[0];
                using var decompressedStream = fileEntry.Open();
                using var decompressedStreamReader = new StreamReader(decompressedStream, System.Text.Encoding.GetEncoding("ISO-8859-1"));
                using var reader = XmlReader.Create(decompressedStreamReader, new XmlReaderSettings() { Async = true });
                while (await reader.ReadAsync())
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
                            stationData = new();
                        }
                        if (reader.Name == "kml:name")
                        {
                            stationData.Station.StationId = reader.ReadElementContentAsString();
                        }
                        if (reader.Name == "kml:description")
                        {
                            stationData.Station.StationName = reader.ReadElementContentAsString();
                        }
                        if (reader.Name == "dwd:Forecast")
                        {
                            curentElementName = reader.GetAttribute("dwd:elementName")?.ToUpper();
                            if (!string.IsNullOrEmpty(curentElementName))
                            {
                                _valueConverters.TryGetValue(curentElementName, out converter);
                            }
                            else
                            {
                                converter = null;
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
                                    var time = timeSteps[i++];
                                    if (decimal.TryParse(v, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal value))
                                    {
                                        if (converter != null)
                                            valueDictionary.Add(time, converter(time, value));
                                        else
                                            valueDictionary.Add(time, value);
                                    }
                                    else
                                    {
                                        valueDictionary.Add(time, null);
                                    }
                                }
                                stationData.Values.Add(curentElementName, valueDictionary);
                            }
                        }
                    }
                    if (reader.NodeType == XmlNodeType.EndElement)
                    {
                        if (reader.Name == "kml:Placemark")
                        {
                            result.Add(stationData);
                        }
                    }
                }
                return result;
            }

        }
    }
}
