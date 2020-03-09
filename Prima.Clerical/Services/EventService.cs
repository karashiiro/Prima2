using Discord.WebSocket;
using Prima.Services;

namespace Prima.Clerical.Services
{
    public class EventService
    {
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;

        public EventService(DiscordSocketClient client, DbService db)
        {
            _client = client;
            _db = db;
        }
    }
}
