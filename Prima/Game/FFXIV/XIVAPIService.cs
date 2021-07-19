using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima.Game.FFXIV
{
    public class XIVAPIService
    {
        private readonly HttpClient _http;

        private const string BASE_URL = "https://xivapi.com";

        public XIVAPIService(HttpClient http) => _http = http;

        /// <summary>
        /// Search all of XIVAPI for a piece of data.
        /// </summary>
        public async Task<IList<T>> Search<T>(string contentName)
        {
            var xivapiResponse = await _http.GetAsync(new Uri($"{BASE_URL}/search?string={contentName}"));
            var dataObject = await xivapiResponse.Content.ReadAsStringAsync();
            return JObject.Parse(dataObject)["Results"].ToObject<IList<T>>();
        }
    }
}