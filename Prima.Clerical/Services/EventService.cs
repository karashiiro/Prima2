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
        private readonly DbService _db;

        public EventService(DbService db)
            => _db = db;

        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> _, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            if (ichannel is SocketGuildChannel channel)
            {
                var guild = channel.Guild;
                var member = guild.GetUser(reaction.UserId);
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
                else if (guild.Id.ToString() == "550702475112480769" && reaction.Emote.Name == "✅")
                {
                    await member.SendMessageAsync($"You have begun the verification process. Your **Discord account ID** is `{member.Id}`.\n"
			            + "Please add this somewhere in your FFXIV Lodestone account description.\n"
			            + "You can edit your account description here: https://na.finalfantasyxiv.com/lodestone/my/setting/profile/\n\n"
                        + $"After you have put your Discord account ID in your Lodestone profile, please use {_db.Config.Prefix}verify `Lodestone ID` to tell me your Lodestone ID **(located in your character profile URL)**.\n"
                        + "The API may not immediately update after you do this, so please wait a couple of minutes and use the command again if that happens.");
                }
            }
        }

        public async Task ReactionRemoved(Cacheable<IUserMessage, ulong> _, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            if (ichannel is SocketGuildChannel channel)
            {
                var guild = channel.Guild;
                var member = guild.GetUser(reaction.UserId);
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
                if (disConfig.RoleEmotes.TryGetValue(emote.Id.ToString(), out var roleIdString))
                {
                    var roleId = ulong.Parse(roleIdString);
                    var role = member.Guild.GetRole(roleId);
                    await member.RemoveRoleAsync(role);
                    Log.Information("Role {Role} was removed from {DiscordUser}", role.Name, member.ToString());
                }
            }
        }
    }
}
