using Discord;
using Discord.WebSocket;
using Prima.Models;
using Prima.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

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

        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> imessage, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            if (ichannel is SocketGuildChannel)
            {
                var guild = (ichannel as SocketGuildChannel).Guild;
                var member = guild.GetUser((await imessage.GetOrDownloadAsync()).Author.Id);
                var emote = reaction.Emote as Emote;
                DiscordGuildConfiguration disConfig;
                try
                {
                    disConfig = _db.Guilds.Single(g => g.Id == guild.Id);
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                if (disConfig.RoleEmotes.TryGetValue(emote.Id.ToString(), out string roleIdString))
                {
                    ulong roleId = ulong.Parse(roleIdString);
                    var role = member.Guild.GetRole(roleId);
                    await member.AddRoleAsync(role);
                    Log.Information("Role {Role} was added to {DiscordUser}", role.Name, member.ToString());
                }
            }
        }

        public async Task ReactionRemoved(Cacheable<IUserMessage, ulong> imessage, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            if (ichannel is SocketGuildChannel)
            {
                var guild = (ichannel as SocketGuildChannel).Guild;
                var member = guild.GetUser((await imessage.GetOrDownloadAsync()).Author.Id);
                var emote = reaction.Emote as Emote;
                DiscordGuildConfiguration disConfig;
                try
                {
                    disConfig = _db.Guilds.Single(g => g.Id == guild.Id);
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                if (disConfig.RoleEmotes.TryGetValue(emote.Id.ToString(), out string roleIdString))
                {
                    ulong roleId = ulong.Parse(roleIdString);
                    var role = member.Guild.GetRole(roleId);
                    await member.RemoveRoleAsync(role);
                    Log.Information("Role {Role} was removed from {DiscordUser}", role.Name, member.ToString());
                }
            }
        }
    }
}
