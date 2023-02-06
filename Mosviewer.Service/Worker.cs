using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Mosviewer.Domain;

namespace Mosviewer.Service
{
    public class Worker : BackgroundService
    {
        private static readonly string _url = "https://opendata.dwd.de/weather/local_forecasts/mos/MOSMIX_S/all_stations/kml/";
        private static readonly string _directory = "../mosdata";
        private static readonly Dictionary<string, Func<DateTime, decimal, decimal?>> _valueConverters = new()
        {
            { "TTT", (time, val) => val - 273.15M },
            { "T5CM", (time, val) => val - 273.15M },
            { "TD", (time, val) => val - 273.15M },
            { "TX", (time, val) => time.Hour % 6 == 0 ? val - 273.15M : null },
            { "TN", (time, val) => time.Hour % 6 == 0 ? val - 273.15M : null },
            { "PPPP", (time, val) => val / 100M },
            { "SUND1", (time, val) => val / 3600 * 100 },
            { "FF", (time, val) => val * 3.6M },
            { "FX1", (time, val) => val * 3.6M },
            { "FX3", (time, val) => val * 3.6M },
            { "FXH", (time, val) => val * 3.6M }
        };

        private readonly HttpClient _httpClient = new HttpClient();
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public async Task<List<MosFile>> GetFilelisting(CancellationToken cancellationToken)
        {
            using var response = await (await _httpClient.GetAsync(_url, cancellationToken))
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsStreamAsync(cancellationToken);
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
                            System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime()));
                }
            }
            if (result.Count == 0)
            {
                throw new ApplicationException($"{_url} enthält keine Dateien.");
            }
            return result;
        }

        public async Task<MosFile> GetLatestFile(CancellationToken cancellationToken) =>
            (await GetFilelisting(cancellationToken)).OrderByDescending(f => f.FileChanged).First();

        public async Task ReadFileAsync(MosFile file, CancellationToken cancellationToken)
        {
            var taskList = new List<Task>(6000);
            var stations = new List<Station>(6000);
            var modelinfo = new Modelinfo();
            var station = new Station();

            using var zipStream = await (await _httpClient.GetAsync(file.Link, cancellationToken))
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsStreamAsync(cancellationToken);
            using var archive = new System.IO.Compression.ZipArchive(zipStream, System.IO.Compression.ZipArchiveMode.Read);
            var fileEntry = archive.Entries[0];
            using var decompressedStream = fileEntry.Open();
            using var decompressedStreamReader = new StreamReader(decompressedStream, System.Text.Encoding.GetEncoding("ISO-8859-1"));
            using var reader = XmlReader.Create(decompressedStreamReader);

            reader.ReadToFollowing("dwd:ProductDefinition");
            while (reader.Read() && !cancellationToken.IsCancellationRequested)
            {
                if (reader.NodeType == XmlNodeType.Element)
                {
                    if (reader.Name == "dwd:IssueTime")
                    {
                        modelinfo.IssueTime = DateTime.Parse(
                            reader.ReadElementContentAsString(),
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime();
                    }
                    if (reader.Name == "dwd:Model")
                    {
                        modelinfo.ModelReferenceTimes.Add(
                            reader.GetAttribute("dwd:name")!,
                            DateTime.Parse(
                                reader.GetAttribute("dwd:referenceTime")!,
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime());
                    }
                    if (reader.Name == "dwd:TimeStep")
                    {
                        modelinfo.TimeSteps.Add(DateTime.Parse(
                            reader.ReadElementContentAsString(),
                            System.Globalization.CultureInfo.InvariantCulture,
                            System.Globalization.DateTimeStyles.AssumeUniversal).ToUniversalTime());
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
                        taskList.Add(ReadStationData(station.Id, modelinfo.TimeSteps, reader.ReadOuterXml(), cancellationToken));
                    }
                }
                if (reader.NodeType == XmlNodeType.EndElement)
                {
                    if (reader.Name == "kml:Placemark")
                    {
                        stations.Add(station);
                    }
                }
            }
            await Task.WhenAll(taskList);

            using (var stationWriter = new BinaryWriter(File.Open(
                Path.Combine(_directory, "stations.dat"),
                FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                foreach (var s in stations)
                {
                    s.Serialize(stationWriter);
                }
            }


            using (var modelinfoWriter = new BinaryWriter(File.Open(
                Path.Combine(_directory, "modelinfo.dat"),
                FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                modelinfo.Serialize(modelinfoWriter);
            }

        }

        private Task ReadStationData(
            string stationId, List<DateTime> timeSteps, string xmlStr,
            CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                try
                {
                    var filename = Path.Combine(_directory, stationId + ".dat");
                    string? curentElementName = null!;
                    Func<DateTime, decimal, decimal?> converter = null!;
                    StationValue stationValue = new StationValue() { StationId = stationId };

                    using var xmlStreamReader = new StringReader(xmlStr);
                    using var reader = XmlReader.Create(xmlStreamReader);
                    using var stationValuesWriter = new BinaryWriter(File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read));

                    while (reader.Read())
                    {
                        if (reader.Name == "dwd:Forecast")
                        {
                            curentElementName = reader.GetAttribute("dwd:elementName")?.ToUpper();
                            if (curentElementName != null)
                            {
                                converter = _valueConverters.TryGetValue(curentElementName, out var c)
                                ? c
                                : (time, val) => val;
                                stationValue.Parameter = curentElementName;
                            }
                        }
                        if (reader.Name == "dwd:value" && curentElementName != null)
                        {
                            var data = reader.ReadElementContentAsString().AsSpan().Trim();
                            int valCount = 0;
                            while (data.Length > 0)
                            {
                                int end = data.IndexOf(' ');
                                int len = end != -1 ? end : data.Length;
                                DateTime time = timeSteps[valCount++];

                                stationValue.Value = decimal.TryParse(data.Slice(0, len),
                                    System.Globalization.NumberStyles.Any,
                                    System.Globalization.CultureInfo.InvariantCulture,
                                    out decimal v) ? converter(time, v) : null;
                                stationValue.ForecastDate = time;
                                stationValue.Serialize(stationValuesWriter);
                                data = data[len..].TrimStart();
                            }
                        }
                    }
                }
                catch (TaskCanceledException) { }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }, cancellationToken);


        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            TimeSpan delay = TimeSpan.FromMinutes(1);
            DateTime nextDownload = DateTime.MinValue;
            DateTime lastFileDate = DateTime.MinValue;

            if (!Directory.Exists(_directory))
            {
                Directory.CreateDirectory(_directory);
            }
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (nextDownload < DateTime.UtcNow)
                    {
                        _logger.LogInformation($"Prüfe {_url} auf neue Dateien.");
                        var latestFile = await GetLatestFile(cancellationToken);
                        if (lastFileDate < latestFile.FileChanged)
                        {
                            _logger.LogInformation($"Lese {latestFile.Link}.");
                            var start = DateTime.UtcNow;
                            await ReadFileAsync(latestFile, cancellationToken);
                            lastFileDate = latestFile.FileChanged;
                            nextDownload = new DateTime((lastFileDate.Ticks / TimeSpan.TicksPerHour + 1) * TimeSpan.TicksPerHour, DateTimeKind.Utc);
                            _logger.LogInformation($"Daten in {(DateTime.UtcNow - start).TotalSeconds:0.0} Sekunden gelesen. Nächster Download um {nextDownload:HH:mm} UTC.");
                        }

                    }
                }
                catch (TaskCanceledException)
                {
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
                await Task.Delay(delay, cancellationToken);
            }
        }
    }
}
