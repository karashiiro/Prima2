using Discord;
using Discord.Net;
using Discord.WebSocket;
using Prima.Models;
using Prima.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.Clerical.Services
{
    public class CensusEventService
    {
        private readonly DiscordSocketClient _client;
        private readonly DbService _db;
        private readonly XIVAPIService _XIVAPI;

        private readonly List<ulong> _cemUnverifiedMembers;

        public CensusEventService(DiscordSocketClient client, XIVAPIService XIVAPI, DbService db)
        {
            _client = client;
            _db = db;
            _XIVAPI = XIVAPI;

            _cemUnverifiedMembers = new List<ulong>();
        }

        public async Task GuildMemberUpdated(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            if (oldMember == null || newMember == null)
            {
                throw new ArgumentNullException(oldMember == null ? nameof(oldMember) : nameof(newMember));
            }

            // Note: Use EnforcementEnabled or something in DB.
            switch (newMember.Guild.Id)
            {
                case 550702475112480769:
                    await CEMNamingScheme(oldMember, newMember);
                    break;
                case 550910482194890781:
                    await CEMNamingScheme(oldMember, newMember);
                    break;
                case 318592736066273280:
                    await CEMNamingScheme(oldMember, newMember);
                    break;
            }
        }

        private async Task CEMMessageRecieved(SocketMessage rawMessage, SocketGuildChannel guildChannel)
        {
            await CEMRecoverData(rawMessage, guildChannel);
        }

        private async Task CEMRecoverData(SocketMessage rawMessage, SocketGuildChannel guildChannel)
        {
            var guild = guildChannel.Guild;
            var member = guildChannel.Guild.GetUser(rawMessage.Author.Id);
            var guildConfig = _db.Guilds.Single(g => g.Id == guild.Id);
            try
            {
                var user = _db.Users
                    .Single(user => user.DiscordId == member.Id);
            }
            catch (InvalidOperationException)
            {
                // Skip if not a member anyways.
                if (!member.Roles.Contains(guild.GetRole(ulong.Parse(guildConfig.Roles["Member"])))) return;

                if (member.Nickname[0] != '(')
                {
                    await CEMRecoverDataFailed(member);
                    return;
                }

                var world = member.Nickname[1..member.Nickname.LastIndexOf(')')];
                var name = member.Nickname.Substring(member.Nickname.LastIndexOf(')') + 2);

                DiscordXIVUser foundCharacter;
                try
                {
                    foundCharacter = await _XIVAPI.GetDiscordXIVUser(world, name, 0);
                    foundCharacter.DiscordId = member.Id;
                    await _db.AddUser(foundCharacter);
                    Log.Information("Recovered data for {User}", $"{member.Username}#{member.Discriminator}");
                }
                catch (XIVAPICharacterNotFoundException)
                {
                    await CEMRecoverDataFailed(member);
                    return;
                }
            }
        }

        private async Task CEMRecoverDataFailed(SocketGuildUser member)
        {
            if (_cemUnverifiedMembers.Contains(member.Id)) return;
            await (await _client.GetUser(_db.Config.BotMaster)
                .GetOrCreateDMChannelAsync())
                .SendMessageAsync($"Please manually recover data for {member.Mention}.");
            _cemUnverifiedMembers.Add(member.Id);
        }

        // Enforce naming scheme.
        // We use a flair system, so editing your nickname actually just edits a flair.
        // For example, if someone's default nickname is "(Balmung) Nota Realuser" and
        // they set their Discord nickname to "Absolutely", their nickname will change
        // to "(Absolutely) Nota Realuser".
        private async Task CEMNamingScheme(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            DiscordGuildConfiguration guildConfig;
            try
            {
                guildConfig= _db.Guilds.Single(g => g.Id == newMember.Guild.Id);
            }
            catch (InvalidOperationException)
            {
                return;
            }
            var statusChannel = newMember.Guild.GetChannel(guildConfig.StatusChannel) as SocketTextChannel;
            if (oldMember.Nickname == newMember.Nickname) return; // They might just be editing their avatar or something.
            try
            {
                var user = _db.Users.Single(user => user.DiscordId == newMember.Id);

                if (string.IsNullOrEmpty(newMember.Nickname)) // They want no flair.
                {
                    await newMember.ModifyAsync(properties =>
                    {
                        properties.Nickname = user.Name;
                    });
                    return;
                }

                if (newMember.Nickname.EndsWith(user.Name)) // Avoid recursion and loopholes.
                {
                    if (newMember.Nickname.Length != user.Name.Length) // Their nickname is not just their character name.
                    {
                        if (newMember.Nickname[0] != '(' || !newMember.Nickname.EndsWith($") {user.Name}")) // Their nickname is not in the format (something) First Last.
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
                    var userDm = await newMember.GetOrCreateDMChannelAsync();
                    await userDm.SendMessageAsync(Properties.Resources.DiscordNicknameTooLongError);
                    await newMember.ModifyAsync(properties =>
                    {
                        properties.Nickname = GetDefaultNickname(user);
                    });
                    return;
                }

                await newMember.ModifyAsync(properties =>
                {
                    properties.Nickname = nickname;
                });

                await statusChannel.SendMessageAsync($"User {oldMember.Nickname} changed their nickname to {newMember.Nickname}.");
            }
            catch (InvalidOperationException) {} // User is not in the database
            catch (HttpException) {} // User has a higher permission level than this bot
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