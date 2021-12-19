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

        public Captcha(HttpClient http)
        {
            _http = http;
        }

        public Task<Stream> Generate(string id)
        {
            return _http.GetStreamAsync(ServiceLocation + $"/generate/{id}");
        }

        public async Task<bool> Verify(string id, string test)
        {
            var res = await _http.GetStringAsync(ServiceLocation + $"/verify/{id}/{test}");
            var resParsed = JsonConvert.DeserializeObject<CaptchaVerificationResult>(res);
            return resParsed?.Result == true;
        }

        private class CaptchaVerificationResult
        {
            [JsonProperty("result")]
            public bool Result { get; set; }
        }
    }
}