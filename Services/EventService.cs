using Discord;
using Discord.Net;
using Discord.WebSocket;
using Prima.Contexts;
using Prima.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Prima.Services
{
    public class EventService
    {
        private readonly ConfigurationService _config;
        private readonly DiscordSocketClient _client;
        private readonly HttpClient _http;
        private readonly XIVAPIService _XIVAPI;

        public string LastCaughtRegex { get; private set; }

        private IDMChannel _botMaster;
        private readonly List<ulong> _cemUnverifiedMembers;

        public EventService(ConfigurationService config, DiscordSocketClient client, HttpClient http, XIVAPIService XIVAPI)
        {
            _config = config;
            _client = client;
            _http = http;
            _XIVAPI = XIVAPI;

            _cemUnverifiedMembers = new List<ulong>();

            LastCaughtRegex = string.Empty;
        }

        public async Task InitializeAsync()
        {
            _botMaster = await _client.GetUser(_config.GetULong("BotMaster")).GetOrCreateDMChannelAsync();
        }

        public async Task GuildMemberUpdated(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            if (_config.CurrentPreset != Preset.Clerical) return;

            if (oldMember == null || newMember == null)
            {
                throw new ArgumentNullException(oldMember == null ? "oldMember" : "newMember");
            }

            switch (newMember.Guild.Id)
            {
                case 550702475112480769:
                    await CEMNamingScheme(oldMember, newMember);
                    break;
            }
        }

        public async Task MessageDeleted(Cacheable<IMessage, ulong> cmessage, ISocketMessageChannel ichannel)
        {
            if (_config.CurrentPreset != Preset.Moderation) return;

            if (!(ichannel is SocketGuildChannel)) return;

            SocketGuildChannel channel = ichannel as SocketGuildChannel;
            SocketGuild guild = channel.Guild;
            SocketUserMessage message = await cmessage.GetOrDownloadAsync() as SocketUserMessage;

            SocketTextChannel deletedMessageChannel = guild.GetChannel(_config.GetULong(guild.Id.ToString(), "Channels", "deleted-messages")) as SocketTextChannel;
            SocketTextChannel deletedCommandChannel = guild.GetChannel(_config.GetULong(guild.Id.ToString(), "Channels", "deleted-commands")) as SocketTextChannel;

            // Get executor of the deletion.
            var auditLogs = await guild.GetAuditLogsAsync(10).FlattenAsync();
            var thisLog = auditLogs
                .Where(log => log.Action == ActionType.MessageDeleted)
                .First();
            IUser executor = thisLog.User ?? message.Author; // If no user is listed as the executor, the executor is the author of the message.

            // Build the embed.
            Embed messageEmbed = new EmbedBuilder()
                .WithTitle($"#{ichannel.Name} at {message.Timestamp}")
                .WithColor(Discord.Color.Blue)
                .WithAuthor(message.Author)
                .WithDescription(message.Content)
                .WithFooter($"Deleted by {executor.Username}#{executor.Discriminator}", executor.GetAvatarUrl())
                .WithCurrentTimestamp()
                .Build();

            // Send the embed.
            IMessage sentMessage;
            if (message.Author.Id == _client.CurrentUser.Id || message.Content.StartsWith(_config.GetSection("Prefix").Value))
            {
                sentMessage = await deletedCommandChannel.SendMessageAsync(embed: messageEmbed);
            }
            else
            {
                sentMessage = await deletedMessageChannel.SendMessageAsync(embed: messageEmbed);
            }

            // Attach attachments as well.
            string unsaved = "";
            foreach (Attachment attachment in message.Attachments)
            {
                try
                {
                    Stream data = await _http.GetStreamAsync(new Uri(attachment.Url));
                    data.Seek(0, SeekOrigin.Begin);
                    await deletedMessageChannel.SendFileAsync(data, attachment.Filename, string.Empty);
                }
                catch (HttpException)
                {
                    unsaved += $"\n{attachment.Url}";
                }
            }
            if (!string.IsNullOrEmpty(unsaved))
            {
                await deletedMessageChannel.SendMessageAsync(Properties.Resources.UnsavedMessageAttachmentsWarning + unsaved);
            }

            // Copy reactions and send those, too.
            if (message.Reactions.Count > 0)
            {
                foreach (var reactionEntry in message.Reactions)
                {
                    var emote = reactionEntry.Key as Emote;

                    string userString = $"Users who reacted with {emote}:";
                    IEnumerable<IUser> users = await message.GetReactionUsersAsync(emote, int.MaxValue).FlattenAsync();
                    foreach (IUser user in users)
                    {
                        userString += $"\n{user.Mention}";
                    }

                    await deletedMessageChannel.SendMessageAsync(userString);
                }
            }
        }

        public async Task MessageRecieved(SocketMessage rawMessage)
        {
            if (rawMessage == null)
            {
                throw new ArgumentNullException(nameof(rawMessage));
            }

            if (_client.GetChannel(rawMessage.Channel.Id) is SocketGuildChannel)
            {
                SocketGuildChannel guildChannel = rawMessage.Channel as SocketGuildChannel;
                await ProcessAttachments(rawMessage, guildChannel);
                switch (guildChannel.Id)
                {
                    case 550702475112480769:
                        await CEMMessageRecieved(rawMessage, guildChannel);
                        break;
                }
            }
        }

        private async Task ProcessAttachments(SocketMessage rawMessage, SocketGuildChannel guildChannel)
        {
            if (!rawMessage.Attachments.Any()) return;

            foreach (Attachment attachment in rawMessage.Attachments)
            {
                if (attachment.Filename.ToLower().EndsWith(".bmp"))
                {
                    string justFileName = attachment.Filename.Substring(0, attachment.Filename.LastIndexOf("."));
                    Stopwatch timer = new Stopwatch();
                    Stream fileData = await _http.GetStreamAsync(new Uri(attachment.Url));
                    fileData.Seek(0, SeekOrigin.Begin);
                    Bitmap bitmap = new Bitmap(fileData);
                    Stream outFileData = new MemoryStream();
                    bitmap.Save(outFileData, System.Drawing.Imaging.ImageFormat.Png);
                    timer.Stop();
                    Console.WriteLine($"Processed BMP from {rawMessage.Author.Username}#{rawMessage.Author.Discriminator}, ({timer.ElapsedMilliseconds}ms)!");
                    await (guildChannel as ITextChannel).SendFileAsync(outFileData, justFileName + ".png", $"{rawMessage.Author.Mention}: Your file has been automatically converted from BMP to PNG (BMP files don't render automatically).");
                    bitmap.Dispose();
                    outFileData.Dispose();
                }
            }
        }

        private async Task CEMMessageRecieved(SocketMessage rawMessage, SocketGuildChannel guildChannel)
        {
            switch (_config.CurrentPreset)
            {
                case Preset.Clerical:
                    await CEMTextBlacklist(rawMessage, guildChannel);
                    await CEMRecoverData(rawMessage, guildChannel);
                    break;
            }
        }

        private async Task CEMTextBlacklist(SocketMessage rawMessage, SocketGuildChannel guildChannel)
        {
            using var db = new TextBlacklistContext();
            IQueryable<GuildTextBlacklistEntry> blacklist = db.RegexStrings.Where(rs => rs.GuildId == guildChannel.Guild.Id);
            foreach (var entry in blacklist)
            {
                var match = Regex.Match(rawMessage.Content, entry.RegexString);
                if (match.Success)
                {
                    LastCaughtRegex = entry.RegexString;
                    await rawMessage.DeleteAsync();
                }
            }
        }

        private async Task CEMRecoverData(SocketMessage rawMessage, SocketGuildChannel guildChannel)
        {
            SocketGuild guild = guildChannel.Guild;
            SocketGuildUser member = guildChannel.Guild.GetUser(rawMessage.Author.Id);
            using var db = new DiscordXIVUserContext();
            try
            {
                DiscordXIVUser user = db.Users
                    .Single(user => user.DiscordId == member.Id);
            }
            catch (InvalidOperationException)
            {
                if (!member.Roles.Contains(guild.GetRole(_config.GetULong(guild.Id.ToString(), "Roles", "Member")))) return;

                if (member.Nickname[0] != '(' && !_cemUnverifiedMembers.Contains(member.Id))
                {
                    await CEMRecoverDataFailed(member);
                    return;
                }

                string world = member.Nickname.Substring(1, member.Nickname.LastIndexOf(')') - 2);
                Console.WriteLine(world);
                string name = member.Nickname.Substring(member.Nickname.LastIndexOf(')') + 2);
                Console.WriteLine(name);

                DiscordXIVUser foundCharacter;
                try
                {
                    foundCharacter = await _XIVAPI.GetDiscordXIVUser(world, name, 0);
                    foundCharacter.DiscordId = member.Id;
                    db.Users.Add(foundCharacter);
                    await db.SaveChangesAsync();
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
            await _botMaster.SendMessageAsync($"Please manually recover data for {member.Mention}.");
                    _cemUnverifiedMembers.Add(member.Id);
        }

        public async Task ReactionAdded(Cacheable<IUserMessage, ulong> cmessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage message = await cmessage.GetOrDownloadAsync();
            if (channel == null || reaction == null)
            {
                throw new ArgumentNullException(channel == null ? nameof(channel) : nameof(reaction));
            }

            if (_client.GetChannel(channel.Id) is SocketGuildChannel)
            {
                SocketGuildChannel guildChannel = channel as SocketGuildChannel;
                SocketGuildUser guildUser = guildChannel.GetUser(reaction.UserId);
                switch ((channel as SocketGuildChannel).Id)
                {
                    case 550702475112480769:
                        await CEMReactionAdded(message, guildChannel, reaction, guildUser);
                        break;
                }
            }
        }
        
        public async Task ReactionRemoved(Cacheable<IUserMessage, ulong> cmessage, ISocketMessageChannel channel, SocketReaction reaction)
        {
            IUserMessage message = await cmessage.GetOrDownloadAsync();
            if (channel == null || reaction == null)
            {
                throw new ArgumentNullException(channel == null ? nameof(channel) : nameof(reaction));
            }

            if (_client.GetChannel(channel.Id) is SocketGuildChannel)
            {
                SocketGuildChannel guildChannel = channel as SocketGuildChannel;
                SocketGuildUser guildUser = guildChannel.GetUser(reaction.UserId);
                switch ((channel as SocketGuildChannel).Id)
                {
                    case 550702475112480769:
                        await CEMReactionRemoved(message, guildChannel, reaction, guildUser);
                        break;
                }
            }
        }

        private async Task CEMReactionAdded(IUserMessage message, SocketGuildChannel channel, SocketReaction reaction, SocketGuildUser user)
        {
            switch (_config.CurrentPreset)
            {
                case Preset.Clerical:
                    await AddRole(message, channel, reaction, user);
                    break;
                case Preset.Scheduler:
                    break;
            }
        }

        private async Task CEMReactionRemoved(IUserMessage message, SocketGuildChannel channel, SocketReaction reaction, SocketGuildUser user)
        {
            switch (_config.CurrentPreset)
            {
                case Preset.Clerical:
                    await RemoveRole(message, channel, reaction, user);
                    break;
                case Preset.Scheduler:
                    break;
            }
        }

        private async Task AddRole(IUserMessage message, SocketGuildChannel channel, SocketReaction reaction, SocketGuildUser user)
        {
            ulong roleId;
            try
            {
                _config.GetSection(channel.Guild.Id.ToString(), "ReactionRoleChannels").GetChildren()
                    .Single(ch => ch.Value == channel.Id.ToString());
                roleId = _config.GetULong(channel.Guild.Id.ToString(), "ReactionRoles", RegexSearches.NonNumbers.Replace(reaction.Emote.Name, ""));
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (roleId == _config.GetULong(channel.Guild.Id.ToString(), "ReactionRoles", "Verification"))
            {
                await user.SendMessageAsync(Properties.Resources.VerificationProcess1 + user.Id.ToString()
                    + Properties.Resources.VerificationProcess2 + _config.GetSection("Prefix").Value
                    + Properties.Resources.VerificationProcess3);
            } else
            {
                SocketRole guildRole = channel.Guild.GetRole(roleId);
                await user.AddRoleAsync(guildRole);
            }
        }

        private async Task RemoveRole(IUserMessage message, SocketGuildChannel channel, SocketReaction reaction, SocketGuildUser user)
        {
            ulong roleId;
            try
            {
                _config.GetSection(channel.Guild.Id.ToString(), "ReactionRoleChannels").GetChildren()
                    .Single(ch => ch.Value == channel.Id.ToString());
                roleId = _config.GetULong(channel.Guild.Id.ToString(), "ReactionRoles", RegexSearches.NonNumbers.Replace(reaction.Emote.Name, ""));
            }
            catch (InvalidOperationException)
            {
                return;
            }

            SocketRole guildRole = channel.Guild.GetRole(roleId);
            await user.RemoveRoleAsync(guildRole);
        }

        // Enforce naming scheme.
        // We use a flair system, so editing your nickname actually just edits a flair.
        // For example, if someone's default nickname is "(Balmung) Nota Realuser" and
        // they set their Discord nickname to "Absolutely", their nickname will change
        // to "(Absolutely) Nota Realuser".
        private async Task CEMNamingScheme(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            SocketTextChannel statusChannel = newMember.Guild.GetChannel(_config.GetULong(newMember.Guild.Id.ToString(), "Channels", "status")) as SocketTextChannel;
            if (oldMember.Nickname == newMember.Nickname) return; // They might just be editing their avatar or something.
            using var db = new DiscordXIVUserContext();
            try
            {
                DiscordXIVUser user = db.Users.Single(user => user.DiscordId == newMember.Id);

                if (string.IsNullOrEmpty(newMember.Nickname)) // They want no flair.
                {
                    await newMember.ModifyAsync(properties =>
                    {
                        properties.Nickname = $"{user.Name}";
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

                string nickname = $"({newMember.Nickname}) ${user.Name}";
                if (nickname.Length > 32) // Throws an exception otherwise
                {
                    IDMChannel userDm = await newMember.GetOrCreateDMChannelAsync();
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
            catch (InvalidOperationException) {}
        }

        private static string GetDefaultNickname(DiscordXIVUser user)
        {
            string nickname = $"({user.World}) ${user.Name}";
            if (nickname.Length > 32)
            {
                nickname = user.Name;
            }
            return nickname;
        }
    }
}
