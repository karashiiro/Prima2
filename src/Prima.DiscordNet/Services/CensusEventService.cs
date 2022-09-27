using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Prima.Game.FFXIV;
using Prima.Models;
using Prima.Resources;
using Prima.Services;
using Serilog;

namespace Prima.DiscordNet.Services
{
    public class CensusEventService
    {
        private readonly DiscordSocketClient _client;
        private readonly IDbService _db;

        public CensusEventService(DiscordSocketClient client, IDbService db)
        {
            _client = client;
            _db = db;
        }

        public async Task GuildMemberUpdated(Cacheable<SocketGuildUser, ulong> cacheableOldMember,
            SocketGuildUser newMember)
        {
            // Note: Use EnforcementEnabled or something in DB.
            var oldMember = await cacheableOldMember.GetOrDownloadAsync();
            if (oldMember == null)
            {
                Log.Warning("Tried to respond to a guild member update for user {UserId}, but they were null!",
                    cacheableOldMember.Id);
                return;
            }

            switch (oldMember.Guild.Id)
            {
                case 550702475112480769:
                    await CEMNamingScheme(SpecialGuilds.CrystalExploratoryMissions, cacheableOldMember, newMember);
                    break;
                case 550910482194890781:
                    await CEMNamingScheme(SpecialGuilds.CrystalExploratoryMissions, cacheableOldMember, newMember);
                    break;
                case 318592736066273280:
                    await CEMNamingScheme(SpecialGuilds.CrystalExploratoryMissions, cacheableOldMember, newMember);
                    break;
            }
        }

        // Enforce naming scheme.
        // We use a flair system, so editing your nickname actually just edits a flair.
        // For example, if someone's default nickname is "(Balmung) Nota Realuser" and
        // they set their Discord nickname to "Absolutely", their nickname will change
        // to "(Absolutely) Nota Realuser".
        private async Task CEMNamingScheme(ulong guildId, Cacheable<SocketGuildUser, ulong> cOldMember,
            SocketGuildUser nullableNewMember)
        {
            var newMember = nullableNewMember ??
                            (IGuildUser)await _client.Rest.GetGuildUserAsync(guildId, cOldMember.Id);

            DiscordGuildConfiguration guildConfig;
            try
            {
                guildConfig = _db.Guilds.Single(g => g.Id == newMember.Guild.Id);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            SocketGuildUser oldMember = null;
            if (cOldMember.HasValue)
            {
                oldMember = cOldMember.Value;
                // No download branch; downloaded data will have the new data
            }

            var statusChannel = await newMember.Guild.GetChannelAsync(guildConfig.StatusChannel) as SocketTextChannel;
            if (statusChannel == null)
            {
                Log.Warning("Failed to get status channel for guild {GuildName}", newMember.Guild.Name);
                return;
            }

            if (oldMember?.Nickname == newMember.Nickname)
                return; // They might just be editing their avatar or something.

            Log.Information("Checking nickname for user {UserId}", oldMember?.Id ?? 0);
            try
            {
                var user = _db.Users.Single(u => u.DiscordId == newMember.Id);

                if (string.IsNullOrEmpty(newMember.Nickname)) // They want no flair.
                {
                    await newMember.ModifyAsync(properties => { properties.Nickname = user.Name; });
                    return;
                }

                if (newMember.Nickname.EndsWith(user.Name)) // Avoid recursion and loopholes.
                {
                    if (newMember.Nickname.Length !=
                        user.Name.Length) // Their nickname is not just their character name.
                    {
                        if (newMember.Nickname[0] != '(' ||
                            !newMember.Nickname
                                .EndsWith(
                                    $") {user.Name}")) // Their nickname is not in the format (something) First Last.
                        {
                            await newMember.ModifyAsync(properties =>
                            {
                                properties.Nickname = GetDefaultNickname(user);
                            });
                            return;
                        }
                    }

                    return; // Nothing to do; their nickname is fine.
                }

                var nickname = $"({newMember.Nickname}) {user.Name}";
                if (nickname.Length > 32) // Throws an exception otherwise
                {
                    var userDm = await newMember.CreateDMChannelAsync();
                    await userDm.SendMessageAsync(
                        "Because of Discord's limitations, nicknames must be 32 characters or fewer in length. " +
                        $"That nickname, `{nickname}`, exceeds 32 characters.\n" +
                        "If you meant to change your character name, please use `~iam` again in the welcome channel.");
                    await newMember.ModifyAsync(properties => { properties.Nickname = GetDefaultNickname(user); });
                    return;
                }

                await newMember.ModifyAsync(properties => { properties.Nickname = nickname; });

                await statusChannel.SendMessageAsync(
                    $"User {oldMember?.Nickname ?? "(No nickname)"} changed their nickname to {newMember.Nickname}.");
            }
            catch (InvalidOperationException)
            {
            } // User is not in the database
            catch (HttpException)
            {
            } // User has a higher permission level than this bot
        }

        private static string GetDefaultNickname(DiscordXIVUser user)
        {
            var nickname = $"({user.World}) {user.Name}";
            if (nickname.Length > 32)
            {
                nickname = user.Name;
            }

            return nickname;
        }
    }
}