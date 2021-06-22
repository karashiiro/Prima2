using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Prima.DiscordNet;
using Prima.DiscordNet.Attributes;
using Prima.Models;
using Prima.Queue.Services;
using Prima.Resources;
using Prima.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Prima.Queue.Modules
{
    public class QueueModule : ModuleBase<SocketCommandContext>
    {
        private const ulong LfmRoleId =
#if DEBUG
            551521141135769600
#else
            551523366989725741
#endif
            ;

        private const ulong DelubrumSavageChannelId =
#if DEBUG
            766712049316265985
#else
            803636739343908894
#endif
            ;

        private const ulong ZadnorThingChannelId = 845106113082818560;

        private static readonly IList<(ulong, DateTime)> LfmPullTimeLog = new List<(ulong, DateTime)>();
        private static readonly string[] Elements = { "Earth", "Wind", "Water", "Fire", "Lightning", "Ice" };

        public IDbService Db { get; set; }
        public FFXIV3RoleQueueService QueueService { get; set; }
        public PasswordGenerator PwGen { get; set; }

        [Command("lfm", RunMode = RunMode.Async)]
        [Description("Group leaders can use this to pull up to 7 members from the queue in a particular channel. Usage: `~lfm <#d/#h/#t>`")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task LfmAsync([Remainder] string args = "")
        {
            if (!QueueInfo.LfgChannels.ContainsKey(Context.Channel.Id)) // Don't use this outside of LFG channels
                return;

            var queueName = QueueInfo.LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            var leader = Context.Guild.GetUser(Context.User.Id);
            var ptTuple = LfmPullTimeLog.FirstOrDefault(t => t.Item1 == leader.Id);
            if (leader.Roles.FirstOrDefault(r => r.Id == LfmRoleId) != null && ptTuple != default)
            {
                var (_, pt) = ptTuple;
                if ((DateTime.UtcNow - pt).TotalSeconds > 30) // Last pulled more than 30 seconds ago
                {
                    Log.Warning("User {User} failed to be deroled, deroling them now.", Context.User.ToString());
                    await ReplyAsync($"{Context.User.Mention}, something went wrong in your last pull and you weren't properly deregistered, so I tried to deregister you again. Please try pulling again.");
                    await RemoveLfm(leader);
                    LfmPullTimeLog.Remove(ptTuple);
                }
                else await ReplyAsync($"{Context.User.Mention}, you are already looking for members.");
                return;
            }

            if (ptTuple != default) // An entry for this person exists despite this person not having the role, remove them from the list and update their timer.
                LfmPullTimeLog.Remove(ptTuple);
            LfmPullTimeLog.Add((Context.User.Id, DateTime.UtcNow));

            var fixedRoles = args.Replace(" ", "").ToLowerInvariant();
            bool inEleChannel = false, inArsenalCategory = true;
            var partyType = Context.Channel.Id switch // Bit messy way of doing this but whatever
            {
                550708765490675773 => "learning/frag farming",
                550708833412972544 => "Absolute Virtue or Ozma prog",
                550708866497773599 => "Ozma clears or farms",
#if DEBUG
                766712049316265985
#else
                765994301850779709
#endif
                => "Castrum Lacus Litore",
                803636739343908894 => "Delubrum Reginae (Savage)",
                809241125373739058 => "fresh Delubrum Reginae (Savage) progression",
                806957742056013895 => "Delubrum Reginae (Normal)",
                845106113082818560 => "Dalriada",
                _ => throw new NotSupportedException(),
            };

            await AddLfm(leader);

            // Get progression role if supplied in Savage queue
            IRole requiredDiscordRole = null;
            if (Context.Channel.Id == DelubrumSavageChannelId)
            {
                requiredDiscordRole = GetRoleFromArgs(args);
            }

            var eventId = GetEventIdFromArgs(args) ?? "";

            var (dpsWanted, healersWanted, tanksWanted) = QueueUtil.GetDesiredRoleCounts(fixedRoles);
            var wantedSum = dpsWanted + healersWanted + tanksWanted;
            if (wantedSum > 7)
            {
                if (Context.Channel.Id == DelubrumSavageChannelId || Context.Channel.Id == 809241125373739058)
                {
                    if (wantedSum > 47)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you can't have more than 48 people in a Delubrum Reginae (Savage) raid (including yourself) :eyes:");
                        await RemoveLfm(leader);
                        return;
                    }
                }
                else if (Context.Channel.Id == 806957742056013895)
                {
                    if (wantedSum > 23)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you can't have more than 24 people in a Delubrum Reginae (Normal) raid (including yourself) :eyes:");
                        await RemoveLfm(leader);
                        return;
                    }
                }
                else
                {
                    await ReplyAsync($"{Context.User.Mention}, you can't have more than 8 people in a party (including yourself) :eyes:");
                    await RemoveLfm(leader);
                    return;
                }
            }
            if (wantedSum <= 0)
            {
                await ReplyAsync($"{Context.User.Mention}, your party can't be empty :confused:");
                await RemoveLfm(leader);
                return;
            }

            Log.Information(
                "User {User} executed ~lfm for {DPSCount} DPS, {HealerCount} healers, and {TankCount} tanks.",
                Context.User.ToString(),
                dpsWanted,
                healersWanted,
                tanksWanted);

            await ReplyAsync($"{Context.User.Mention}, you have begun a search for {dpsWanted} DPS, {healersWanted} Healer(s), and {tanksWanted} Tank(s)" +
                             (requiredDiscordRole == null ? "" : $" with the {requiredDiscordRole.Name} role") +
                             ".\n" +
                             "Party Finder information will be DM'd to you immediately.\n" +
                             "Party information will be sent to invitees after 30 seconds.\n" +
                             "You can cancel matchmaking by typing `~stop` within 30 seconds.");

            var pw = await PwGen.Get(Context.User.Id);
            try
            {
#if DEBUG
                const ulong castrumLfg = 766712049316265985;
#else
                const ulong castrumLfg = 765994301850779709;
#endif
                await leader.SendMessageAsync($"Your Party Finder password is {pw}.\n" +
                    $"Please join {(new ulong[] { castrumLfg, DelubrumSavageChannelId, ZadnorThingChannelId, 806957742056013895, 809241125373739058 }.Contains(Context.Channel.Id) ? "a" : "an elemental")} voice channel within the next 30 seconds to continue matching.\n" +
                    "Create the listing in Party Finder now; matching will begin in 30 seconds.");
            }
            catch (HttpException)
            {
                await RemoveLfm(leader);
                await ReplyAsync("You seem to have your DMs disabled. Please enable them temporarily, and then reattempt to use the command.");
                return;
            }

            const int stopPollingDelayMs = 250;
            for (var i = 0; i < 30000 / stopPollingDelayMs; i++)
            {
                var newMessages = await Context.Channel.GetMessagesAsync(limit: 1).FlattenAsync();
                if (newMessages.Any(m => m.Author.Id == leader.Id && m.Content.StartsWith("~stop")))
                {
                    await ReplyAsync($"{Context.User.Mention}, your matchmaking attempt has been cancelled.");
                    await RemoveLfm(leader);
                    return;
                }
                await Task.Delay(stopPollingDelayMs);
            }

            await RemoveLfm(leader);

            var vcName = "";
            if (leader.VoiceChannel == null ||
                !leader.VoiceChannel.Category.Name.ToLowerInvariant().Contains("arsenal") ||
                !leader.VoiceChannel.Category.Name.ToLowerInvariant().Contains("castrum") ||
                !leader.VoiceChannel.Category.Name.ToLowerInvariant().Contains("delubrum"))
            {
                inArsenalCategory = false;
            }
            else
            {
                foreach (var element in Elements)
                {
                    if (!leader.VoiceChannel.Name.Contains(element)) continue;

                    vcName = leader.VoiceChannel.Name;
                    inEleChannel = true;
                    break;
                }
            }

            var leaderDisplayName = leader.Nickname ?? leader.ToString();

            // Queue stuff now
            Log.Information("Removed user {User} from queue {QueueName}.", Context.User.ToString(), queueName);
            queue.RemoveAll(leader.Id, eventId);

            var fetchedTanks = new List<ulong>();
            for (var i = 0; i < tanksWanted; i++)
            {
                var nextTank = requiredDiscordRole == null
                    ? queue.Dequeue(FFXIVRole.Tank, eventId)
                    : queue.DequeueWithDiscordRole(FFXIVRole.Tank, requiredDiscordRole, Context, eventId);
                if (nextTank == null) break;
                Log.Information("Removed user {User} from queue {QueueName}.", nextTank.ToString(), queueName);
                fetchedTanks.Add(nextTank.Value);
            }
            var fetchedHealers = new List<ulong>();
            for (var i = 0; i < healersWanted; i++)
            {
                var nextHealer = requiredDiscordRole == null
                    ? queue.Dequeue(FFXIVRole.Healer, eventId)
                    : queue.DequeueWithDiscordRole(FFXIVRole.Healer, requiredDiscordRole, Context, eventId);
                if (nextHealer == null) break;
                Log.Information("Removed user {User} from queue {QueueName}.", nextHealer.ToString(), queueName);
                fetchedHealers.Add(nextHealer.Value);
            }
            var fetchedDps = new List<ulong>();
            for (var i = 0; i < dpsWanted; i++)
            {
                var nextDps = requiredDiscordRole == null
                    ? queue.Dequeue(FFXIVRole.DPS, eventId)
                    : queue.DequeueWithDiscordRole(FFXIVRole.DPS, requiredDiscordRole, Context, eventId);
                if (nextDps == null) break;
                Log.Information("Removed user {User} from queue {QueueName}.", nextDps.ToString(), queueName);
                fetchedDps.Add(nextDps.Value);
            }

            QueueService.Save();

            var fetchedSum = fetchedDps.Count + fetchedHealers.Count + fetchedTanks.Count;
            if (fetchedSum == 0)
            {
                await ReplyAsync($"{Context.User.Mention}, the queues you're trying to pull from are empty or unconfirmed" +
                                 (requiredDiscordRole == null ? "" : " for that role") +
                                 "!");
                return;
            }

            // Send embed to leader
            var fields = new List<EmbedFieldBuilder>();

            LfmAddUsersLeaderEmbed("DPS", fields, fetchedDps);
            LfmAddUsersLeaderEmbed("Healers", fields, fetchedHealers);
            LfmAddUsersLeaderEmbed("Tanks", fields, fetchedTanks);

            fields.Add(new EmbedFieldBuilder()
                .WithIsInline(false)
                .WithName("Someone didn't show?")
                .WithValue("If some/all of these people don't show up or respond to messages, feel free to run the command again (with the new party requirements) to fill your party. The PF password will stay the same until midnight PDT."));
            if (fetchedSum != wantedSum)
            {
                fields.Add(new EmbedFieldBuilder()
                    .WithIsInline(false)
                    .WithName("NOTICE:")
                    .WithValue("The queues you selected ran out of members of the roles you asked for. Feel free to use the notifiable roles to fill the rest of your party."));
            }

            try
            {
                var leaderEmbed = new EmbedBuilder()
                    .WithTitle("Invited the following people:")
                    .WithColor(new Discord.Color(0x00, 0x80, 0xFF))
                    .WithThumbnailUrl("https://i.imgur.com/aVEsVRb.png")
                    .WithFields(fields)
                    .Build();
                await leader.SendMessageAsync(embed: leaderEmbed);
            }
            catch
            {
                var leaderEmbedBuilder = new EmbedBuilder()
                    .WithTitle("Invited the following people:")
                    .WithColor(new Discord.Color(0x00, 0x80, 0xFF))
                    .WithThumbnailUrl("https://i.imgur.com/aVEsVRb.png");
                foreach (var field in fields)
                {
                    var lebCopy = leaderEmbedBuilder.WithFields(field);
                    await leader.SendMessageAsync(embed: lebCopy.Build());
                }
            }

            // Send member embeds
            var userParams = new LfgEmbedParameters
            {
                InArsenalCategory = inArsenalCategory,
                InElementalChannel = inEleChannel,
                VoiceChannelName = vcName,
                LeaderDisplayName = leaderDisplayName,
                Leader = leader,
                PartyType = partyType,
                Password = pw,
            };

            LfmNotifyUsers(fetchedDps, userParams, FFXIVRole.DPS);
            LfmNotifyUsers(fetchedHealers, userParams, FFXIVRole.Healer);
            LfmNotifyUsers(fetchedTanks, userParams, FFXIVRole.Tank);
        }

        private void LfmAddUsersLeaderEmbed(string label, ICollection<EmbedFieldBuilder> fields, ICollection<ulong> fetched)
        {
            if (fetched.Count != 0)
                fields.Add(new EmbedFieldBuilder()
                    .WithIsInline(false)
                    .WithName(label)
                    .WithValue(fetched
                        .Select(ft =>
                        {
                            IGuildUser user = Context.Guild.GetUser(ft);
                            if (user == null)
                            {
                                Task.Delay(250).GetAwaiter().GetResult();
                                user = Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, ft).Result;
                            }
                            return user?.Nickname ?? user?.ToString() ?? "(Unable to retrieve)";
                        })
                        .Aggregate(string.Empty, (ws, nextUser) => ws + $"{nextUser}\n")));
        }

        private void LfmNotifyUsers(IEnumerable<ulong> fetched, LfgEmbedParameters userParamsBase, FFXIVRole role)
        {
            foreach (var user in fetched)
            {
                var userParamsCopy = userParamsBase.Copy();
                userParamsCopy.TargetUser = user;
                userParamsCopy.Role = role;

                _ = Task.Run(() => SendLfgEmbed(userParamsCopy));
            }
        }

        private class LfgEmbedParameters
        {
            public ulong TargetUser { get; set; }
            public FFXIVRole Role { get; set; }

            public bool InArsenalCategory { get; set; }
            public bool InElementalChannel { get; set; }
            public string VoiceChannelName { get; set; }
            public SocketGuildUser Leader { get; set; }
            public string LeaderDisplayName { get; set; }
            public string PartyType { get; set; }
            public string Password { get; set; }

            public LfgEmbedParameters Copy()
            {
                return (LfgEmbedParameters)MemberwiseClone();
            }
        }

        private async Task SendLfgEmbed(LfgEmbedParameters args)
        {
            var inviteeFields = new List<EmbedFieldBuilder>
            {
                new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName("Group")
                    .WithValue(args.InArsenalCategory
                        ? args.Leader.VoiceChannel.Category.Name
                        : "Ask your party leader!"),
                new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName("Voice Channel")
                    .WithValue(args.InElementalChannel ? args.VoiceChannelName : "Ask your party leader!"),
                new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName("Your Role")
                    .WithValue(args.Role)
            };
            var inviteeEmbedText = $"Your queue for {args.PartyType} has popped! Check the PF for a party under `{args.LeaderDisplayName}` (or something similar) and use the password `{args.Password}` to join! " +
                                   $"Please DM them ({args.Leader}) if you have issues with joining or cannot find the party. ";

#if DEBUG
            const ulong castrumLfg = 766712049316265985;
#else
            const ulong castrumLfg = 765994301850779709;
#endif

            if (Context.Channel.Id != castrumLfg)
            {
                inviteeEmbedText += "Additionally, the map used to find your portal location can be found here: https://i.imgur.com/Gao2rzI.jpg";
            }
            var inviteeEmbed = new EmbedBuilder()
                .WithTitle("Your queue has popped!")
                .WithColor(new Discord.Color(0x00, 0x80, 0xFF))
                .WithThumbnailUrl("https://i.imgur.com/aVEsVRb.png")
                .WithDescription(inviteeEmbedText)
                .WithFields(inviteeFields)
                .Build();

            IGuildUser user = Context.Guild.GetUser(args.TargetUser);
            if (user == null)
            {
                await Task.Delay(250);
                user = await Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, args.TargetUser);
            }

            try
            {
                await user.SendMessageAsync(embed: inviteeEmbed);
            }
            catch (NullReferenceException)
            {
                await args.Leader.SendMessageAsync($"Couldn't send run information to {user.Mention}; they have server DMs disabled. Please ping them directly.");
                return;
            }
            catch (HttpException e) when (e.DiscordCode == 50007)
            {
                await args.Leader.SendMessageAsync($"Couldn't send run information to {user.Mention}; they have server DMs disabled. Please ping them directly.");
                return;
            }
            catch (HttpException)
            {
                try
                {
                    await user.SendMessageAsync(embed: inviteeEmbed);
                }
                catch (HttpException e)
                {
                    Log.Warning(e, "Sending run information for {User} failed.", args.Leader.ToString());
                    return;
                }
            }
            Log.Information("Run information for {User}'s party sent to {User}.", args.Leader.ToString(), user.ToString());
        }

        private static Task AddLfm(SocketGuildUser member)
        {
            var role = member.Guild.Roles.First(r => r.Id == LfmRoleId);
            return member.AddRoleAsync(role);
        }

        private static Task RemoveLfm(SocketGuildUser member)
        {
            var role = member.Guild.Roles.First(r => r.Id == LfmRoleId);
            return member.RemoveRoleAsync(role);
        }

        [Command("stop", RunMode = RunMode.Async)]
        [Description("Stops looking for members.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task LfmStopAsync()
        {
            // Just a dummy command to put it in the help list; the actual command is in the LFM command.
            return Task.CompletedTask;
        }

        [Command("lfg", RunMode = RunMode.Async)]
        [Description("Enter the queue in a queue channel. Takes a role as its first argument, e.g. `~lfg dps`")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task LfgAsync([Remainder] string args = "")
        {
            if (!QueueInfo.LfgChannels.ContainsKey(Context.Channel.Id))
                return;

            var queueName = QueueInfo.LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            // Get progression role if supplied in Savage queue
            IRole requiredDiscordRole = null;
            if (Context.Channel.Id == DelubrumSavageChannelId)
            {
                requiredDiscordRole = GetRoleFromArgs(args);
            }
            args = RemoveRoleFromArgs(args);

            var autoConfirmForEvent = false;
            var eventId = GetEventIdFromArgs(args);
            if (eventId != null)
            {
                args = RemoveEventIdFromArgs(args);
                if (!await IsEventReal(eventId))
                {
                    await ReplyAsync($"{Context.User.Mention}, that event ID does not correspond to a scheduled event!");
                    return;
                }
                else
                {
                    if (Context.Channel.Id == DelubrumSavageChannelId)
                    {
                        var guildConfig = Db.Guilds.FirstOrDefault(conf => conf.Id == Context.Guild.Id);
                        if (guildConfig == null) return;
#if DEBUG
                        var channels = new ulong[] { 571235821168885780 };
#else
                        var channels = new[]
                        {
                            guildConfig.DelubrumScheduleInputChannel,
                            guildConfig.DelubrumNormalScheduleInputChannel,
                            guildConfig.CastrumScheduleInputChannel,
                        };
#endif

                        var (_, embed) = await GetEvent(eventId);
                        if (embed.Timestamp == null)
                        {
                            Log.Error("Event {EventId} has no timestamp; aborting.", eventId);
                            return;
                        }

                        var eventTime = embed.Timestamp.Value;
                        if (eventTime.AddHours(-2) <= DateTimeOffset.Now)
                        {
#if DEBUG
                            Log.Information("Auto-confirmed.");
#endif
                            autoConfirmForEvent = true;
                        }

                        IMessage postMessage = null;
                        foreach (var c in channels)
                        {
                            var channel = Context.Guild.GetTextChannel(c);
                            if (!ulong.TryParse(eventId, out var messageId)) continue;
                            postMessage = await channel.GetMessageAsync(messageId);
                            if (postMessage != null) break;
                        }
                        if (postMessage == null) return; // Shouldn't happen at this point; could be merged with other branch

                        var author = Context.Guild.GetUser(postMessage.Author.Id);

                        var discordRoles = DelubrumProgressionRoles.Roles.Keys
                            .Select(rId => Context.Guild.GetRole(rId));
                        var authorHasProgressionRole = discordRoles.Any(dr => author.HasRole(dr));
                        if (!authorHasProgressionRole)
                        {
                            await ReplyAsync($"{Context.User.Mention}, this is not a queue for fresh progression runs.\n" +
                                             "Please queue in <#809241125373739058> instead.");
                            return;
                        }

                        // ReSharper disable once InvertIf
                        if (postMessage.Content.ToLowerInvariant().Contains("810201516291653643"))
                        {
                            var res = await ReplyAsync($"{Context.User.Mention}, that event mentions the LFG Fresh Prog role.\n" +
                                                       "If that event is a fresh progression run, please leave this queue and queue in <#809241125373739058> instead.");
                            _ = Task.Run(async () =>
                            {
                                await Task.Delay(20000);
                                await res.DeleteAsync();
                            });
                        }
                    }
                }
            }

            var roles = ParseRoles(args);
            if (roles == FFXIVRole.None)
            {
                await ReplyAsync($"You didn't provide a valid argument, {Context.User.Mention}!\n"
                    + "The proper usage would be: `~lfg <[d][h][t]>`");
                return;
            }

            var enqueuedRoles = FFXIVRole.None;
            foreach (var r in new[] { FFXIVRole.DPS, FFXIVRole.Healer, FFXIVRole.Tank })
            {
                if (!roles.HasFlag(r)) continue;
                if (requiredDiscordRole == null && queue.Enqueue(Context.User.Id, r, eventId))
                {
                    enqueuedRoles |= r;
                }
                else if (requiredDiscordRole != null)
                {
                    var result = queue.EnqueueWithDiscordRole(Context.User.Id, r, requiredDiscordRole, Context, eventId);
                    if (result == DiscordIntegratedEnqueueResult.Success)
                    {
                        enqueuedRoles |= r;
                    }
                    else if (result == DiscordIntegratedEnqueueResult.DoesNotHaveRole)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you don't have that role; check your progression roles!\n" +
                                         "If you are coming with experience from another server, please make a request for the appropriate role in <#808406722934210590> with a log or screenshot.");
                        return;
                    }
                }
            }

            var response = Context.User.Mention;
            const string queued0 = ", you're already in those queues. You can check your position in them with `~queue`.";
            const string queued1 = ", you have been added to the queue as a {0}. ";
            const string queued2 = ", you have been added to the queue as a {0} and a {1}. ";
            const string queued3 = ", you have been added to the queue as a {0}, {1}, and {2}. ";
            const string extra = " Don't forget to relevel if you're not level 60 yet!";
            var enqueuedRolesList = RolesToArray(enqueuedRoles);
            switch (enqueuedRolesList.Count)
            {
                case 0:
                    response += queued0;
                    break;
                case 1:
                    response += string.Format(queued1, enqueuedRolesList[0]) + GetPositionString(queue, Context.User, requiredDiscordRole, Context.User.Id, eventId);
                    break;
                case 2:
                    response += string.Format(queued2, enqueuedRolesList[0], enqueuedRolesList[1]) + GetPositionString(queue, Context.User, requiredDiscordRole, Context.User.Id, eventId);
                    break;
                case 3:
                    response += string.Format(queued3, enqueuedRolesList[0], enqueuedRolesList[1], enqueuedRolesList[2]) + GetPositionString(queue, Context.User, requiredDiscordRole, Context.User.Id, eventId);
                    break;
            }

            if (!new ulong[] { 765994301850779709, DelubrumSavageChannelId, ZadnorThingChannelId, 806957742056013895, 809241125373739058 }.Contains(Context.Channel.Id))
            {
                response += extra;
            }

            if (autoConfirmForEvent)
            {
                queue.ConfirmEvent(Context.User.Id, eventId);
            }
#if DEBUG
            else
            {
                Log.Information("Not auto-confirmed.");
            }
#endif

            QueueService.Save();
            RefreshQueuesEx();

            Log.Information(
                "User {User} joined queue {QueueName} for the roles [{Roles}].",
                Context.User.ToString(),
                queueName,
                string.Join(',', enqueuedRolesList.Select(r => r.ToString()).ToArray()));
            await ReplyAsync(response);
        }

        [Command("myevents", RunMode = RunMode.Async)]
        [Description("Check the events you're specifically in queue for, by role.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task MyEvents()
        {
            if (!QueueInfo.LfgChannels.ContainsKey(Context.Channel.Id))
                return;

            var queueName = QueueInfo.LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            const string responseTemplate = "```\n" +
                                            "You are in the following queues:\n" +
                                            "Tank:\n{0}\n" +
                                            "Healer:\n{1}\n" +
                                            "DPS:\n{2}\n" +
                                            "```";

            var tankEvents = queue.GetEventStates(Context.User.Id, FFXIVRole.Tank);
            var healerEvents = queue.GetEventStates(Context.User.Id, FFXIVRole.Healer);
            var dpsEvents = queue.GetEventStates(Context.User.Id, FFXIVRole.DPS);

            var tankEventStr = tankEvents
                .Aggregate("", (agg, next) =>
                {
                    var position = queue.GetPosition(Context.User.Id, FFXIVRole.Tank, next.EventId);
                    if (position == 0)
                        return agg;

                    var total = queue.Count(FFXIVRole.Tank, next.EventId);
                    if (string.IsNullOrEmpty(next.EventId))
                        return agg + $"  {position}/{total}\n";
                    else
                        return agg + $"  {position}/{total} ({next.EventId}){(next.Confirmed ? " CONFIRMED" : " ")}\n";
                });

            var healerEventStr = healerEvents
                .Aggregate("", (agg, next) =>
                {
                    var position = queue.GetPosition(Context.User.Id, FFXIVRole.Healer, next.EventId);
                    if (position == 0)
                        return agg;

                    var total = queue.Count(FFXIVRole.Healer, next.EventId);
                    if (string.IsNullOrEmpty(next.EventId))
                        return agg + $"  {position}/{total}\n";
                    else
                        return agg + $"  {position}/{total} ({next.EventId}){(next.Confirmed ? " CONFIRMED" : " ")}\n";
                });

            var dpsEventStr = dpsEvents
                .Aggregate("", (agg, next) =>
                {
                    var position = queue.GetPosition(Context.User.Id, FFXIVRole.DPS, next.EventId);
                    if (position == 0)
                        return agg;

                    var total = queue.Count(FFXIVRole.DPS, next.EventId);
                    if (string.IsNullOrEmpty(next.EventId))
                        return agg + $"  {position}/{total}\n";
                    else
                        return agg + $"  {position}/{total} ({next.EventId}){(next.Confirmed ? " CONFIRMED" : " ")}\n";
                });

            await ReplyAsync(string.Format(
                responseTemplate,
                string.IsNullOrEmpty(tankEventStr) ? "  None\n" : tankEventStr,
                string.IsNullOrEmpty(healerEventStr) ? "  None\n" : healerEventStr,
                string.IsNullOrEmpty(dpsEventStr) ? "  None\n" : dpsEventStr));
        }

        [Command("leavequeue", RunMode = RunMode.Async)]
        [Alias("unqueue", "leave")]
        [Description("Leaves one or more roles in a channel's queue. Using this with no roles specified removes you from all roles in the channel.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task LeaveQueueAsync([Remainder] string args = "")
        {
            if (!QueueInfo.LfgChannels.ContainsKey(Context.Channel.Id))
                return;

            var queueName = QueueInfo.LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            if (args.StartsWith("all"))
            {
                queue.RemoveAll(Context.User.Id);
                QueueService.Save();
                await ReplyAsync($"{Context.User.Mention}, you have been removed from all queues in this channel.");
                return;
            }

            var eventId = GetEventIdFromArgs(args);

            var roles = ParseRoles(args);
            var removedRoles = FFXIVRole.None;

            if (roles == FFXIVRole.None)
            {
                // Remove from all
                foreach (var r in new[] { FFXIVRole.DPS, FFXIVRole.Healer, FFXIVRole.Tank })
                    if (queue.Remove(Context.User.Id, r, eventId))
                        removedRoles |= r;
            }
            else
            {
                // Remove from specified
                foreach (var r in new[] { FFXIVRole.DPS, FFXIVRole.Healer, FFXIVRole.Tank })
                    if (roles.HasFlag(r))
                        if (queue.Remove(Context.User.Id, r, eventId))
                            removedRoles |= r;
            }

            var response = Context.User.Mention;
            var removedRolesList = RolesToArray(removedRoles);
            const string removed0 = ", you weren't found in the non-event queue in this channel.";
            const string removedCommon = ", you have been removed from the queue";
            const string removed1 = " for {0}.";
            const string removed2 = "s for {0} and {1}.";
            const string removed3 = "s for {0}, {1}, and {2}.";
            switch (removedRolesList.Count)
            {
                case 0:
                    response += removed0;
                    break;
                case 1:
                    response += removedCommon + string.Format(removed1, removedRolesList[0]);
                    break;
                case 2:
                    response += removedCommon + string.Format(removed2, removedRolesList[0], removedRolesList[1]);
                    break;
                case 3:
                    response += removedCommon + string.Format(removed3, removedRolesList[0], removedRolesList[1], removedRolesList[2]);
                    break;
            }

            QueueService.Save();

            Log.Information(
                "User {User} dropped-out of queue {QueueName} for the roles [{Roles}] (Event ID: {EventID}).",
                Context.User.ToString(),
                queueName,
                string.Join(',', removedRolesList.Select(r => r.ToString()).ToArray()),
                eventId);
            await ReplyAsync(response);
        }

        [Command("queue", RunMode = RunMode.Async)]
        [Description("Checks your position in the queues of the channel you enter it in.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task QueueAsync([Remainder] string args = "")
        {
            if (args.StartsWith("leave")) // See below
            {
                await LeaveQueueAsync(args.Substring("leave".Length));
                return;
            }

            if (args.StartsWith("refresh")) // See below
            {
                await RefreshQueues();
                return;
            }

            if (args.StartsWith("list")) // See below
            {
                await QueueListAsync();
                return;
            }

            var eventId = GetEventIdFromArgs(args);

            var dumbArgs = RemoveRoleFromArgs(args);
            dumbArgs = RemoveEventIdFromArgs(dumbArgs).Trim();

            if (args.Length == 0 || args.Split(' ').Length == 1 && eventId != null || string.IsNullOrEmpty(dumbArgs))
            {
                await MyEvents();
            }
            else // Because people always try to type "~queue dps" etc., just give it to them.
            {
                await LfgAsync(args);
            }
        }

        private string GetPositionStringAllEvents(FFXIVDiscordIntegratedQueue queue, ulong uid, string dpsEventId, string healerEventId, string tankEventId)
        {
            const string responseTemplate = "You are in the following queues in this channel:\n" +
                                            "Tank: {0}\n" +
                                            "Healer: {1}\n" +
                                            "DPS: {2}";
            var dpsRole = Context.Guild.GetRole(queue.GetSlotDiscordRoles(uid, FFXIVRole.DPS, Context)?.FirstOrDefault() ?? 0);
            var healerRole = Context.Guild.GetRole(queue.GetSlotDiscordRoles(uid, FFXIVRole.Healer, Context)?.FirstOrDefault() ?? 0);
            var tankRole = Context.Guild.GetRole(queue.GetSlotDiscordRoles(uid, FFXIVRole.Tank, Context)?.FirstOrDefault() ?? 0);

            var dpsCount = dpsRole == null
                ? queue.Count(FFXIVRole.DPS, dpsEventId)
                : queue.CountWithDiscordRole(FFXIVRole.DPS, dpsRole, Context, dpsEventId);
            var healerCount = healerRole == null
                ? queue.Count(FFXIVRole.Healer, healerEventId)
                : queue.CountWithDiscordRole(FFXIVRole.Healer, healerRole, Context, healerEventId);
            var tankCount = tankRole == null
                ? queue.Count(FFXIVRole.Tank, tankEventId)
                : queue.CountWithDiscordRole(FFXIVRole.Tank, tankRole, Context, tankEventId);

            var dpsPos = dpsRole == null
                ? queue.GetPosition(uid, FFXIVRole.DPS, dpsEventId)
                : queue.GetPositionWithDiscordRole(uid, FFXIVRole.DPS, dpsRole, Context, dpsEventId);
            var healerPos = healerRole == null
                ? queue.GetPosition(uid, FFXIVRole.Healer, healerEventId)
                : queue.GetPositionWithDiscordRole(uid, FFXIVRole.Healer, healerRole, Context, healerEventId);
            var tankPos = tankRole == null
                ? queue.GetPosition(uid, FFXIVRole.Tank, tankEventId)
                : queue.GetPositionWithDiscordRole(uid, FFXIVRole.Tank, tankRole, Context, tankEventId);

            var dpsStr = "Not in queue";
            if (dpsPos != 0)
            {
                dpsStr = $"{(dpsRole != null ? dpsRole.Name + " " : "")}{dpsPos}/{dpsCount}{(!string.IsNullOrEmpty(dpsEventId) ? $" ({dpsEventId})" : "")}";
            }

            var healerStr = "Not in queue";
            if (healerPos != 0)
            {
                healerStr = $"{(healerRole != null ? healerRole.Name + " " : "")}{healerPos}/{healerCount}{(!string.IsNullOrEmpty(healerEventId) ? $" ({healerEventId})" : "")}";
            }

            var tankStr = "Not in queue";
            if (tankPos != 0)
            {
                tankStr = $"{(tankRole != null ? tankRole.Name + " " : "")}{tankPos}/{tankCount}{(!string.IsNullOrEmpty(tankEventId) ? $" ({tankEventId})" : "")}";
            }

            return string.Format(responseTemplate, tankStr, healerStr, dpsStr);
        }

        private (int, int, int, int) GetListCounts(FFXIVDiscordIntegratedQueue queue, IRole reqRole, string eventId)
        {
            var dpsCount = reqRole == null
                ? queue.Count(FFXIVRole.DPS, eventId)
                : queue.CountWithDiscordRole(FFXIVRole.DPS, reqRole, Context, eventId);
            var healerCount = reqRole == null
                ? queue.Count(FFXIVRole.Healer, eventId)
                : queue.CountWithDiscordRole(FFXIVRole.Healer, reqRole, Context, eventId);
            var tankCount = reqRole == null
                ? queue.Count(FFXIVRole.Tank, eventId)
                : queue.CountWithDiscordRole(FFXIVRole.Tank, reqRole, Context, eventId);

            var distinctCount = reqRole == null
                ? queue.CountDistinct(eventId)
                : queue.CountDistinctWithDiscordRole(reqRole, Context, eventId);

            return (dpsCount, healerCount, tankCount, distinctCount);
        }

        private static string Padding(int length)
        {
            var agg = "";
            for (var i = 0; i < length; i++)
            {
                agg += ' ';
            }
            return agg;
        }

        [Command("queuelist", RunMode = RunMode.Async)]
        [Description("Checks the queued member counts of the queues of the channel you enter it in.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task QueueListAsync([Remainder] string args = "")
        {
            if (!QueueInfo.LfgChannels.ContainsKey(Context.Channel.Id))
                return;

            // Get progression role if supplied in Savage queue
            IRole requiredDiscordRole = null;
            var delubrumAllRoles = false;
            if (Context.Channel.Id == DelubrumSavageChannelId)
            {
                if (args.ToLowerInvariant().StartsWith("all"))
                {
                    delubrumAllRoles = true;
                }
                else
                {
                    requiredDiscordRole = GetRoleFromArgs(args);
                }
            }

            var eventId = GetEventIdFromArgs(args) ?? "";

            var queueName = QueueInfo.LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            if (delubrumAllRoles)
            {
                var roleIds = DelubrumProgressionRoles.Roles.Keys;
                var events = string.IsNullOrEmpty(eventId) ? queue.GetEvents().Append("") : new[] { eventId };
                foreach (var eId in events)
                {
                    var response = roleIds
                        .Select(rid => Context.Guild.GetRole(rid))
                        .Where(r => r != null)
                        .Select(r =>
                        {
                            var (dpsCount, healerCount, tankCount, distinctCount) = GetListCounts(queue, r, eId);
                            if (distinctCount != 0)
                            {
                                return $"{r.Name}:" +
                                       $"{Padding(33 - $"{r.Name}:".Length - $"{tankCount}".Length)}{tankCount} tank(s)" +
                                       $"{Padding(15 - $"{tankCount:D3} tank(s)".Length - $"{healerCount}".Length)}{healerCount} healer(s)" +
                                       $"{Padding(15 - $"{tankCount:D3} tank(s)".Length - $"{dpsCount}".Length)}{dpsCount} DPS" +
                                       $"{Padding(15 - $"{tankCount:D3} tank(s)".Length - $"{distinctCount}".Length)}{distinctCount} unique players";
                            }
                            else
                            {
                                return string.Empty;
                            }
                        })
                        .Where(line => !string.IsNullOrEmpty(line))
                        .ToList();

                    if (response.Any())
                    {
                        await ReplyAsync(response
                            .Aggregate($"Current queue status {(string.IsNullOrEmpty(eId) ? "across all roles" : "for event " + eId)}:\n```c++\n",
                                (agg, next) => agg + next + '\n') + "```");
                    }
                    else
                    {
                        await ReplyAsync($"Current queue status {(string.IsNullOrEmpty(eId) ? "across all roles" : "for event " + eId)}:\n```c++\nNo members in queue.```");
                    }
                }
            }
            else
            {
                IEnumerable<string> events = new[] { eventId };
                if (string.IsNullOrEmpty(eventId))
                {
                    events = queue.GetEvents().Append("");
                }

                foreach (var eId in events)
                {
                    var (dpsCount, healerCount, tankCount, distinctCount) = GetListCounts(queue, requiredDiscordRole, eId);

                    var roleExtraString = requiredDiscordRole == null
                        ? ""
                        : $"s for {requiredDiscordRole.Name}";

                    var eventExtraString = string.IsNullOrEmpty(eId)
                        ? ""
                        : $", for event {eId}";

                    var noEventExtraString = string.IsNullOrEmpty(eId)
                        ? "non-event "
                        : "";

                    await ReplyAsync($"There are currently {tankCount} tank(s), {healerCount} healer(s), and {dpsCount} DPS in the {noEventExtraString}queue{roleExtraString}{eventExtraString}. (Unique players: {distinctCount})");
                }
            }
        }

        private static IList<FFXIVRole> RolesToArray(FFXIVRole roles)
        {
            var rolesList = new List<FFXIVRole>();
            foreach (var r in new[] { FFXIVRole.DPS, FFXIVRole.Healer, FFXIVRole.Tank })
                if (roles.HasFlag(r))
                    rolesList.Add(r);
            return rolesList;
        }

        private string GetPositionString(FFXIVDiscordIntegratedQueue queue, IUser user, IRole role, ulong uid, string eventId)
        {
            var dpsCount = role == null
                ? queue.Count(FFXIVRole.DPS, eventId)
                : queue.CountWithDiscordRole(FFXIVRole.DPS, role, Context, eventId);
            var healerCount = role == null
                ? queue.Count(FFXIVRole.Healer, eventId)
                : queue.CountWithDiscordRole(FFXIVRole.Healer, role, Context, eventId);
            var tankCount = role == null
                ? queue.Count(FFXIVRole.Tank, eventId)
                : queue.CountWithDiscordRole(FFXIVRole.Tank, role, Context, eventId);

            var dpsPos = role == null
                ? queue.GetPosition(uid, FFXIVRole.DPS, eventId)
                : queue.GetPositionWithDiscordRole(uid, FFXIVRole.DPS, role, Context, eventId);
            var healerPos = role == null
                ? queue.GetPosition(uid, FFXIVRole.Healer, eventId)
                : queue.GetPositionWithDiscordRole(uid, FFXIVRole.Healer, role, Context, eventId);
            var tankPos = role == null
                ? queue.GetPosition(uid, FFXIVRole.Tank, eventId)
                : queue.GetPositionWithDiscordRole(uid, FFXIVRole.Tank, role, Context, eventId);

            var output = "you are number ";
            if (tankPos > 0)
            {
                output += $"{tankPos}/{tankCount} in the Tank queue";
            }
            if (healerPos > 0)
            {
                if (tankPos > 0 && dpsPos == -1)
                { // Healer and tank, but not DPS
                    output += $" and {healerPos}/{healerCount} in the Healer queue";
                }
                else if (tankPos > 0)
                { // Healer, tank, and DPS
                    output += $", {healerPos}/{healerCount} in the Healer queue";
                }
                else
                { // Only healer
                    output += $"{healerPos}/{healerCount} in the Healer queue";
                }
            }
            if (dpsPos > 0)
            {
                if (healerPos > 0 && tankPos > 0)
                { // Healer, tank, and dps
                    output += $", and {dpsPos}/{dpsCount} in the DPS queue";
                }
                else if (healerPos > 0 || tankPos > 0)
                { // Healer or tank, and dps
                    output += $" and {dpsPos}/{dpsCount} in the DPS queue";
                }
                else
                { // Just DPS
                    output += $"{dpsPos}/{dpsCount} in the DPS queue";
                }
            }

            if (role != null)
            {
                if (!user.HasRole(role, Context))
                {
                    return $"<@{uid}>, you don't have that role; check your progression roles!\n" +
                           "If you are coming with experience from another server, please make a request for the appropriate role in <#808406722934210590> with a log or screenshot.";
                }

                if (dpsPos != 0 || healerPos != 0 || tankPos != 0)
                {
                    output += $" for the role {role.Name}";
                }
            }

            if (!string.IsNullOrEmpty(eventId))
            {
                if (dpsPos != 0 || healerPos != 0 || tankPos != 0)
                {
                    output += $", for event {eventId}";
                }
                else
                {
                    return $"<@{uid}>, you are not in queue for that event. Please set your event with `~setevent <[d][h][t]> <event ID>`.";
                }
            }
            else
            {
                output += ", in the non-event queue";
            }

            output += ".";

            return output == "you are number ." ? $"<@{uid}>, you are not in any queues. If you meant to join the queue, use `~lfg <role>`." : $"<@{uid}>, {output}";
        }

        [Command("refreshqueues", RunMode = RunMode.Async)]
        [Alias("refresh", "refreshqueue")]
        [Description("Refreshes your position in the Bozja queues.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task RefreshQueues([Remainder] string args = "")
        {
            if (!QueueInfo.LfgChannels.ContainsKey(Context.Channel.Id) && Context.Guild != null)
                return Task.CompletedTask;
            if (Context.Guild != null)
            {
                var queueName = QueueInfo.LfgChannels[Context.Channel.Id];
                if (queueName != "lfg-castrum")
                {
                    return ReplyAsync($"`~refresh` has been removed, per <#550706420543389696>. Please queue in up to {Math.Truncate(QueueInfo.DelubrumQueueTimeout / Time.Hour)} hours before you want to run.");
                }
            }

            RefreshQueuesEx();
            return ReplyAsync($"{Context.User.Mention}, your timeouts in the Castrum queue have been refreshed!");
        }

        private void RefreshQueuesEx()
        {
            QueueService.GetOrCreateQueue("lfg-castrum").Refresh(Context.User.Id);
            Log.Information("User {User} refreshed queue times.", Context.User.ToString());
        }

        private static FFXIVRole ParseRoles(string roleString)
        {
            var ret = FFXIVRole.None;
            if (roleString.ToLowerInvariant().Contains('d'))
                ret |= FFXIVRole.DPS;
            if (roleString.ToLowerInvariant().Contains('h'))
                ret |= FFXIVRole.Healer;
            if (roleString.ToLowerInvariant().Contains('t'))
                ret |= FFXIVRole.Tank;
            return ret;
        }

        [Command("shove", RunMode = RunMode.Async)]
        [Alias("queueshove")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task ShoveUser(IUser user, [Remainder] string args)
        {
            if (!QueueInfo.LfgChannels.ContainsKey(Context.Channel.Id))
                return ReplyAsync("This is not a queue channel!");

            const ulong mentor = 579916868035411968;
            var sender = Context.Guild.GetUser(Context.User.Id);
            if (sender.Roles.All(r => r.Id != mentor) && !sender.GuildPermissions.KickMembers)
                return Task.CompletedTask;

            var queueName = QueueInfo.LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            var roles = ParseRoles(args);
            if (roles == FFXIVRole.None)
            {
                return ReplyAsync($"You didn't provide a valid argument, {Context.User.Mention}!\n" +
                                  "The proper usage would be: `~shove @User <[d][h][t]>`");
            }

            foreach (var role in RolesToArray(roles))
            {
                queue.Shove(user.Id, role);
            }

            Log.Information("User {User} shoved to front of queue {QueueName}.", user.ToString(), queueName);
            return ReplyAsync("User shoved to front of queue.");
        }

        [Command("insert", RunMode = RunMode.Async)]
        [Alias("queueinsert")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task InsertUser(IUser user, int position, [Remainder] string args)
        {
            if (!QueueInfo.LfgChannels.ContainsKey(Context.Channel.Id))
                return ReplyAsync("This is not a queue channel!");

            const ulong mentor = 579916868035411968;
            var sender = Context.Guild.GetUser(Context.User.Id);
            if (sender.Roles.All(r => r.Id != mentor) && !sender.GuildPermissions.KickMembers)
                return Task.CompletedTask;

            var queueName = QueueInfo.LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            var roles = ParseRoles(args);
            if (roles == FFXIVRole.None)
            {
                return ReplyAsync($"You didn't provide a valid argument, {Context.User.Mention}!\n" +
                                  "The proper usage would be: `~insert @User <position> <[d][h][t]>`");
            }

            foreach (var role in RolesToArray(roles))
            {
                queue.Insert(user.Id, position, role);
            }

            Log.Information("User {User} inserted at position {Position} in queue {QueueName}.", user.ToString(), position, queueName);
            return ReplyAsync($"User inserted in position {position}.");
        }

        [Command("expirequeues", RunMode = RunMode.Async)]
        [Description("Expires all members from nonexistent events.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task ExpireEventQueue([Remainder] string args = "")
        {
            const ulong mentor = 579916868035411968;
            var sender = Context.Guild.GetUser(Context.User.Id);
            if (sender.Roles.All(r => r.Id != mentor) && !sender.GuildPermissions.KickMembers)
                return;

            using var typing = Context.Channel.EnterTypingState();

            var eventIds = (await GetEvents(int.MaxValue))
                .Select(@event => @event.Item2.Footer?.Text)
                .Where(id => id != null)
                .ToList();
            var queueEventIds = new List<string>();

            var queues = QueueInfo.LfgChannels
                .Select(kvp => kvp.Value)
                .Select(QueueService.GetOrCreateQueue)
                .ToList();
            foreach (var queue in queues)
            {
                queueEventIds.AddRange(queue.GetEvents());
            }

            var inactiveEventIds = queueEventIds
                .Except(eventIds)
                .Distinct()
                .ToList();
            foreach (var queue in queues)
            {
                foreach (var eventId in inactiveEventIds)
                {
                    queue.ExpireEvent(eventId);
                    Log.Information("Cleared queue for event {EventId}", eventId);
                }
            }

            QueueService.Save();

            await ReplyAsync($"Done! Events cleared:```\n{inactiveEventIds.Aggregate("", (agg, next) => agg + $"{next}\n")}```");
        }

        [Command("confirm", RunMode = RunMode.Async)]
        public async Task ConfirmEvent()
        {
            var events = (await GetEvents(2)).ToList();
            if (!events.Any())
            {
                await ReplyAsync("You are not in queue for any events starting in the next 2 hours.");
                return;
            }

            var queues = QueueInfo.LfgChannels
                .Select(kvp => kvp.Value)
                .Select(QueueService.GetOrCreateQueue);
            var notInQueue = true;
            var confirmedNone = false;
            foreach (var queue in queues)
            {
                notInQueue &= queue.GetEventStates(Context.User.Id, FFXIVRole.DPS)
                    .Any(es => !string.IsNullOrEmpty(es.EventId));
                notInQueue &= queue.GetEventStates(Context.User.Id, FFXIVRole.Healer)
                    .Any(es => !string.IsNullOrEmpty(es.EventId));
                notInQueue &= queue.GetEventStates(Context.User.Id, FFXIVRole.Tank)
                    .Any(es => !string.IsNullOrEmpty(es.EventId));
                foreach (var (_, embed) in events)
                {
                    var eventId = embed?.Footer?.Text;
                    var timestamp = embed?.Timestamp;
                    if (timestamp.HasValue && timestamp.Value.AddHours(-2) > DateTimeOffset.Now)
                        continue;
                    confirmedNone |= queue.ConfirmEvent(Context.User.Id, eventId);
                }
            }
            Log.Information("{Confirmed}", confirmedNone);

            if (notInQueue)
            {
                await ReplyAsync("You are not in queue for any events starting in the next 2 hours.");
                return;
            }

            if (confirmedNone)
            {
                await ReplyAsync("You are already confirmed for the event.");
                return;
            }

            QueueService.Save();
            Log.Information("{User} has confirmed their queue position.", Context.User);
            await ReplyAsync("Queue position confirmed.");
        }

        [Command("confirmedfor", RunMode = RunMode.Async)]
        public async Task ConfirmedForEvent([Remainder] string args = "")
        {
            var eventId = args;
            if (string.IsNullOrEmpty(eventId))
            {
                await ReplyAsync($"{Context.User.Mention}, that command requires an event ID!");
                return;
            }

            var queues = QueueInfo.LfgChannels
                .Select(kvp => kvp.Value)
                .Select(QueueService.GetOrCreateQueue);
            foreach (var queue in queues)
            {
                foreach (var role in FFXIV3RoleQueue.Roles)
                {
                    var eventStates = queue.GetEventStates(Context.User.Id, role);
                    var eventState = eventStates.FirstOrDefault(es => es.EventId == eventId);
#if DEBUG
                    Log.Information("Queue {HashCode}, role {Role}, event {EventID}, confirmed: {Confirmed}.", queue.GetHashCode(), role, eventState?.EventId ?? eventId, eventState?.Confirmed ?? false);
#endif
                    if (eventState?.Confirmed == true)
                    {
                        await ReplyAsync($"{Context.User.Mention}, you are confirmed for that event!");
                        return;
                    }
                }
            }

            await ReplyAsync("You are not confirmed for that event.");
        }

        private IEnumerable<FFXIV3RoleQueue> GetQueues()
        {
            return null;
        }

        private async Task<bool> IsEventReal(string eventId)
        {
            var (m, _) = await GetEvent(eventId);
            return m != null;
        }

        private IEnumerable<ITextChannel> GetOutputChannels(SocketGuild guild)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == (guild?.Id ?? 0));
            if (guildConfig == null) return new List<ITextChannel>();

            var scheduleOutputChannels = typeof(DiscordGuildConfiguration).GetFields()
                .Where(f => RegexSearches.ScheduleOutputFieldNameRegex.IsMatch(f.Name))
                .Select(f => (ulong?)f.GetValue(guildConfig))
                .Select(cId => guild.GetTextChannel(cId ?? 0));

            return scheduleOutputChannels;
        }

        private async Task<IEnumerable<(IMessage, IEmbed)>> GetEvents(int inHours)
        {
            var guild = Context.Client.GetGuild(SpecialGuilds.CrystalExploratoryMissions);

            var guildConfig = Db.Guilds.FirstOrDefault(conf => conf.Id == guild.Id);
            if (guildConfig == null) return new List<(IMessage, IEmbed)>();

            var channels = GetOutputChannels(guild);

            var events = new List<(IMessage, IEmbed)>();
            foreach (var channel in channels)
            {
                if (channel == null) continue;
                await foreach (var page in channel.GetMessagesAsync())
                {
                    // ReSharper disable once LoopCanBeConvertedToQuery
                    foreach (var message in page)
                    {
                        var embed = message.Embeds.FirstOrDefault(e => e.Type == EmbedType.Rich);
                        if (embed?.Footer == null) continue;

                        if (embed.Timestamp == null) continue;
                        var timestamp = embed.Timestamp.Value;
                        if ((DateTimeOffset.UtcNow - timestamp).TotalHours > inHours) continue;

                        events.Add((message, embed));
                    }
                }
            }

            return events;
        }

        private async Task<(IMessage, IEmbed)> GetEvent(string eventId)
        {
            var guild = Context.Client.GetGuild(SpecialGuilds.CrystalExploratoryMissions);
            var guildConfig = Db.Guilds.FirstOrDefault(conf => conf.Id == guild.Id);
            if (guildConfig == null) return (null, null);

            var channels = GetOutputChannels(guild);

            foreach (var channel in channels)
            {
                if (channel == null) continue;
                await foreach (var page in channel.GetMessagesAsync())
                {
                    foreach (var message in page)
                    {
                        var embed = message.Embeds.FirstOrDefault(e => e.Type == EmbedType.Rich);
                        if (embed?.Footer == null) continue;

                        if (embed.Footer?.Text == eventId)
                        {
                            return (message, embed);
                        }
                    }
                }
            }

            return (null, null);
        }

        private static readonly Regex EventIdRegex = new Regex(@"\d{5}\d+", RegexOptions.Compiled);
        private static string RemoveEventIdFromArgs(string args)
        {
            var match = EventIdRegex.Match(args);
            if (match.Success)
            {
                return args.Replace(match.Value, "");
            }
            else
            {
                return args;
            }
        }

        private static string GetEventIdFromArgs(string args)
        {
            var match = EventIdRegex.Match(args);
            if (match.Success)
            {
                return match.Value;
            }
            else
            {
                return null;
            }
        }

        private static string RemoveRoleFromArgs(string args)
        {
            var roleName =
                DelubrumProgressionRoles.Roles.Values.FirstOrDefault(rn =>
                    args.ToLowerInvariant().Contains(rn.ToLowerInvariant()));
            if (roleName != null)
            {
                return args.Replace(roleName, "", StringComparison.InvariantCultureIgnoreCase);
            }

            return args;
        }

        private IRole GetRoleFromArgs(string args)
        {
            var roleName =
                DelubrumProgressionRoles.Roles.Values.FirstOrDefault(rn =>
                    args.ToLowerInvariant().Contains(rn.ToLowerInvariant()));
            if (roleName != null)
            {
                roleName = RegexSearches.UnicodeApostrophe.Replace(roleName, "'");
                return Context.Guild.Roles.FirstOrDefault(r => r.Name == roleName);
            }

            return null;
        }
    }
}
