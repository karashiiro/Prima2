using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima.Game.FFXIV.XIVAPI
{
    public class XIVAPIClient
    {
        private readonly HttpClient _http;

        public XIVAPIClient(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Search XIVAPI for an item.
        /// </summary>
        public async Task<IList<Item>> SearchItem(string itemName)
        {
            var res = await _http.GetAsync(new Uri($"https://xivapi.com/search?string={itemName}&indexes=Item"));

            try
            {
                res.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException e)
            {
                throw new XIVAPIServiceFailure("Failed to request data from XIVAPI.", e);
            }

            var dataObject = await res.Content.ReadAsStringAsync();
            return JObject.Parse(dataObject)["Results"]?.ToObject<IList<Item>>();
        }
    }
}