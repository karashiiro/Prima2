using Newtonsoft.Json;
using PeanutButter.SimpleHTTPServer;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima.Services
{
    public class UptimeMessageService
    {
        private readonly HttpClient _http;
        private readonly HttpServer _server;

        private readonly long _timeInitialized;

        private string _serviceName;
        private string _flavorText;

        public UptimeMessageService(HttpClient http, HttpServer server)
        {
            
            _http = http;
            _server = server;
            _server.AddJsonDocumentHandler(OnGet);
            
            _timeInitialized = DateTime.Now.Ticks;
        }

        public void Initialize(string serviceName, string flavorText = "")
        {
            _serviceName = serviceName;
            _flavorText = flavorText;
        }

        public async Task StartAsync()
        {
            // Send the process's configuration info to the uptime monitor.
            var timeout = 1000;
            var sent = false;
            while (!sent)
            {
                var meta = new ServiceMetadata
                {
                    name = _serviceName,
                    port = _server.Port,
                };

                try
                {
                    await _http.PostAsync(
                        new Uri(Environment.GetEnvironmentVariable("UPTIME_SERVICE_HOSTNAME") + "/PostUptime"),
                        new StringContent(JsonConvert.SerializeObject(meta)));
                    sent = true;
                }
                catch (HttpRequestException)
                {
                    await Task.Delay(timeout >= 60000 ? 60000 : timeout *= 2);
                }
            }
        }

        public void SetFlavorText(string newText) => _flavorText = newText;

        private object OnGet(HttpProcessor processor, Stream stream)
        {
            return new ServiceStatus
            {
                timeInitialized = _timeInitialized,
                flavorText = _flavorText,
            };
        }
    }

    public struct ServiceStatus
    {
        public long timeInitialized;
        public string flavorText;
    }

    public struct ServiceMetadata
    {
        public string name;
        public int port;
    }
}
