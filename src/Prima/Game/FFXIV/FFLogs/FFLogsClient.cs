using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;

namespace Prima.Game.FFXIV.FFLogs
{
    public class FFLogsClient : IFFLogsClient
    {
        private readonly HttpClient _http;

        private readonly string _clientId;
        private readonly string _clientSecret;
        
        private DateTime _expirationTime;

        public FFLogsClient(HttpClient http)
        {
            _http = http;
            _clientId = Environment.GetEnvironmentVariable("FFLOGS_CLIENT_ID");
            _clientSecret = Environment.GetEnvironmentVariable("FFLOGS_CLIENT_SECRET");
        }

        public async Task Initialize()
        {
            if ((_clientId ?? _clientSecret) == null)
            {
                Log.Error("FFLogs authentication data not found in the environment!");
                return;
            }

            await LoadAccessToken();
        }

        public async Task<T> MakeGraphQLRequest<T>(string query)
        {
            const string publicApi = "https://www.fflogs.com/api/v2/client";

            if (DateTime.UtcNow.AddSeconds(-60) > _expirationTime)
            {
                await LoadAccessToken();
            }

            using var content = new StringContent(JsonConvert.SerializeObject(new { query }), Encoding.UTF8, "application/json");
            var res = await _http.PostAsync(publicApi, content);
            return JsonConvert.DeserializeObject<T>(await res.Content.ReadAsStringAsync());
        }

        private async Task LoadAccessToken()
        {
            const string tokenUri = "https://www.fflogs.com/oauth/token";

            // Load the basic token
            var authorizationBytes = Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}");
            var authorization = Convert.ToBase64String(authorizationBytes);
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", authorization);

            // Make the token request
            using var content = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("grant_type", "client_credentials"),
            });
            var res = await _http.PostAsync(tokenUri, content);
            var resBody = await res.Content.ReadAsStringAsync();
            var resParsed = JsonConvert.DeserializeObject<AccessTokenResponse>(resBody);

            // Install the token
            _expirationTime = DateTime.UtcNow.AddSeconds(resParsed.ExpiresIn);
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", resParsed.AccessToken);
        }

        private class AccessTokenResponse
        {
            [JsonProperty("token_type")]
            public string TokenType { get; set; }

            [JsonProperty("expires_in")]
            public int ExpiresIn { get; set; }

            [JsonProperty("access_token")]
            public string AccessToken { get; set; }
        }
    }
}
