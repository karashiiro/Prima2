using System;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;

namespace Prima.Services
{
    public class PasswordGenerator
    {
        private readonly HttpClient _http;

        public PasswordGenerator(HttpClient http)
        {
            _http = http;
        }

        public async Task<string> Get(ulong uid)
        {
            using var req = new StringContent(uid.ToString());
            try
            {
                var res = await _http.PostAsync(new Uri("http://localhost:9000/"), req);
                return await res.Content.ReadAsStringAsync();
            }
            catch (HttpRequestException e)
            {
                Log.Error(e, "Password generator offline");
                return "0000";
            }
        }
    }
}