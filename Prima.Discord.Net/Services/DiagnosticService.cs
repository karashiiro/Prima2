using Discord.WebSocket;

namespace Prima.DiscordNet.Services
{
    public class DiagnosticService
    {
        private DiscordSocketClient _client;

        public DiagnosticService(DiscordSocketClient client) => _client = client;

        public int GetLatency() => _client.Latency;
    }
}
