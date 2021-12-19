using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Prima.Services
{
    public class Captcha
    {
        private const string ServiceLocation = "http://localhost:2539";

        private readonly HttpClient _http;
        private readonly IDictionary<string, bool> _pending;

        public Captcha(HttpClient http)
        {
            _http = http;
            _pending = new ConcurrentDictionary<string, bool>();
        }

        public Task<Stream> Generate(string id)
        {
            _pending[id] = true;
            return _http.GetStreamAsync(ServiceLocation + $"/generate/{id}");
        }

        public async Task<bool> Verify(string id, string test)
        {
            _pending.Remove(id);
            var res = await _http.GetStringAsync(ServiceLocation + $"/verify/{id}/{test}");
            var resParsed = JsonConvert.DeserializeObject<CaptchaVerificationResult>(res);
            return resParsed?.Result == true;
        }

        public bool IsPending(string id)
        {
            return _pending.ContainsKey(id) && _pending[id];
        }

        private class CaptchaVerificationResult
        {
            [JsonProperty("result")]
            public bool Result { get; set; }
        }
    }
}