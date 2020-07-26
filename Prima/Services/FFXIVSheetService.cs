using CsvHelper;
using Prima.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace Prima.Services
{
    public class FFXIVSheetService
    {
        private readonly IDictionary<Type, object[]> _savedSheets;
        private readonly HttpClient _http;

        public FFXIVSheetService(HttpClient http)
        {
            _savedSheets = new Dictionary<Type, object[]>();
            _http = http;

            DownloadPlaceNameSheet();
            DownloadTerritoryTypeSheet();
        }

        public T[] GetSheet<T>() where T : class
        {
            return (T[])_savedSheets.FirstOrDefault(kvp => kvp.Key == typeof(T)).Value;
        }

        private void DownloadPlaceNameSheet()
        {
            const string url = "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/PlaceName.csv";
            ProcessSheet(url, csv => new PlaceName
            {
                RowId = csv.GetField<int>(0),
                Name = csv.GetField(1),
                NameNoArticle = csv.GetField(3),
            });
        }

        private void DownloadTerritoryTypeSheet()
        {
            const string url = "https://raw.githubusercontent.com/xivapi/ffxiv-datamining/master/csv/TerritoryType.csv";
            ProcessSheet(url, csv => new TerritoryType
            {
                RowId = csv.GetField<int>(0),
                PlaceName = csv.GetField<int>(6),
            });
        }

        private void ProcessSheet<T>(string url, Func<CsvReader, T> Fn) where T : class
        {
            var list = new List<T>();
            var csvRaw = _http.GetStreamAsync(new Uri(url)).GetAwaiter().GetResult();
            using var sr = new StreamReader(csvRaw);
            using var csv = new CsvReader(sr, CultureInfo.InvariantCulture);
            for (var i = 0; i < 3; i++) csv.Read();
            while (csv.Read())
            {
                list.Add(Fn(csv));
            }
            _savedSheets.Add(typeof(T), list.ToArray());
        }
    }
}
