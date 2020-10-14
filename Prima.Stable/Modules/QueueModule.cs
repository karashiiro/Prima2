using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Prima.Attributes;
using Prima.Resources;
using Prima.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima.Stable.Modules
{
    public class QueueModule : ModuleBase<SocketCommandContext>
    {
        private const ulong LfmRoleId = 551523366989725741;

        private static readonly IDictionary<ulong, string> LfgChannels = new Dictionary<ulong, string>
        {
            //{ 550708765490675773, "learning-and-frag-farm" },
            //{ 550708833412972544, "av-and-ozma-prog" },
            //{ 550708866497773599, "clears-and-farming" },
            { 765994301850779709, "lfg-castrum" },
        };
        private static readonly IList<(ulong, DateTime)> LfmPullTimeLog = new List<(ulong, DateTime)>();
        private static readonly string[] Elements = new string[] { "Earth", "Wind", "Water", "Fire", "Lightning", "Ice" };

        public DbService Db { get; set; }
        public FFXIV3RoleQueueService QueueService { get; set; }
        public PasswordGenerator PwGen { get; set; }

        [Command("lfm", RunMode = RunMode.Async)]
        [Description("Group leaders can use this to pull up to 7 members from the queue in a particular channel. Usage: `~lfm <#d/#h/#t>`")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task LfmAsync([Remainder] string args = "")
        {
            if (!LfgChannels.ContainsKey(Context.Channel.Id)) // Don't use this outside of LFG channels
                return;

            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;

            var queueName = LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            var leader = Context.Guild.GetUser(Context.User.Id);
            var ptTuple = LfmPullTimeLog.FirstOrDefault(t => t.Item1 == leader.Id);
            if (leader.Roles.FirstOrDefault(r => r.Id == LfmRoleId) != null && ptTuple != default)
            {
                var (_, pt) = ptTuple;
                if ((DateTime.UtcNow - pt).TotalSeconds > 30) // Last pulled more than 30 seconds ago
                {
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
                765994301850779709 => "Castrum Lacus Litore",
                _ => throw new NotSupportedException(),
            };

            await AddLfm(leader);

            var (dpsWanted, healersWanted, tanksWanted) = GetDesiredRoleCounts(fixedRoles);
            var wantedSum = dpsWanted + healersWanted + tanksWanted;
            if (wantedSum > 7)
            {
                await ReplyAsync($"{Context.User.Mention}, you can't have more than 8 people in a party (including yourself) :eyes:");
                await RemoveLfm(leader);
                return;
            }
            if (wantedSum <= 0)
            {
                await ReplyAsync($"{Context.User.Mention}, your party can't be empty :confused:");
                await RemoveLfm(leader);
                return;
            }

            await ReplyAsync($"{Context.User.Mention}, you have begun a search for {dpsWanted} DPS, {healersWanted} Healer(s), and {tanksWanted} Tank(s).\n" +
                "Party Finder information will be DM'd to you immediately.\n" +
                "Arsenal information will be sent to invitees after 30 seconds.\n" +
                "You can cancel matchmaking by typing `~stop` within 30 seconds.");

            var pw = await PwGen.Get(Context.User.Id);
            try
            {
                await leader.SendMessageAsync($"Your Party Finder password is {pw}.\n" +
                    $"Please join {(Context.Channel.Id == guildConfig.CastrumScheduleInputChannel ? "a Castrum" : "an elemental")} voice channel within the next 30 seconds to continue matching.\n" +
                    "Create the listing in Party Finder now; matching will begin in 30 seconds.");
            }
            catch (HttpException)
            {
                await RemoveLfm(leader);
                await ReplyAsync("You seem to have your DMs disabled. Please enable them temporarily, and then reattempt to use the command.");
                return;
            }

            for (var i = 0; i < 30; i++)
            {
                var newMessages = await Context.Channel.GetMessagesAsync(limit: 10).FlattenAsync();
                if (newMessages.Any(m => m.Author.Id == leader.Id && m.Content.StartsWith("~stop")))
                {
                    await ReplyAsync($"{Context.User.Mention}, your matchmaking attempt has been cancelled.");
                    await RemoveLfm(leader);
                    return;
                }
                await Task.Delay(1000);
            }

            await RemoveLfm(leader);

            var vcName = "";
            if (leader.VoiceChannel == null ||
                !leader.VoiceChannel.Category.Name.ToLowerInvariant().Contains("arsenal") ||
                !leader.VoiceChannel.Category.Name.ToLowerInvariant().Contains("castrum"))
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
            // Remove the leader from all queues
            queue.Remove(leader.Id, FFXIVRole.DPS);
            queue.Remove(leader.Id, FFXIVRole.Healer);
            queue.Remove(leader.Id, FFXIVRole.Tank);

            var fetchedDps = new List<ulong>();
            for (var i = 0; i < dpsWanted; i++)
            {
                var nextDps = queue.Dequeue(FFXIVRole.DPS);
                if (nextDps == null) break;
                fetchedDps.Add(nextDps.Value);
            }
            var fetchedHealers = new List<ulong>();
            for (var i = 0; i < healersWanted; i++)
            {
                var nextHealer = queue.Dequeue(FFXIVRole.Healer);
                if (nextHealer == null) break;
                fetchedHealers.Add(nextHealer.Value);
            }
            var fetchedTanks = new List<ulong>();
            for (var i = 0; i < tanksWanted; i++)
            {
                var nextTank = queue.Dequeue(FFXIVRole.Tank);
                if (nextTank == null) break;
                fetchedTanks.Add(nextTank.Value);
            }

            var fetchedSum = fetchedDps.Count + fetchedHealers.Count + fetchedTanks.Count;
            if (fetchedSum == 0)
            {
                await ReplyAsync($"{Context.User.Mention}, the queues you're trying to pull from are empty!");
                return;
            }

            // Send embed to leader
            var fields = new List<EmbedFieldBuilder>();
            if (fetchedDps.Count != 0)
                fields.Add(new EmbedFieldBuilder()
                    .WithIsInline(false)
                    .WithName("DPS")
                    .WithValue(fetchedDps
                        .Select(fd =>
                        {
                            var user = Context.Guild.GetUser(fd);
                            return user.Nickname ?? user.ToString();
                        })
                        .Aggregate(string.Empty, (ws, nextUser) => ws + $"{nextUser}\n")));
            if (fetchedHealers.Count != 0)
                fields.Add(new EmbedFieldBuilder()
                    .WithIsInline(false)
                    .WithName("Healers")
                    .WithValue(fetchedHealers
                        .Select(fh =>
                        {
                            var user = Context.Guild.GetUser(fh);
                            return user.Nickname ?? user.ToString();
                        })
                        .Aggregate(string.Empty, (ws, nextUser) => ws + $"{nextUser}\n")));
            if (fetchedTanks.Count != 0)
                fields.Add(new EmbedFieldBuilder()
                    .WithIsInline(false)
                    .WithName("Tanks")
                    .WithValue(fetchedTanks
                        .Select(ft =>
                        {
                            var user = Context.Guild.GetUser(ft);
                            return user.Nickname ?? user.ToString();
                        })
                        .Aggregate(string.Empty, (ws, nextUser) => ws + $"{nextUser}\n")));
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

            var leaderEmbed = new EmbedBuilder()
                .WithTitle("Invited the following people:")
                .WithColor(new Discord.Color(0x00, 0x80, 0xFF))
                .WithThumbnailUrl("https://i.imgur.com/4ogfA2S.png")
                .WithFields(fields)
                .Build();
            await leader.SendMessageAsync(embed: leaderEmbed);

            // Send member embeds
            var baseParams = new LfgEmbedParameters
            {
                InArsenalCategory = inArsenalCategory,
                InElementalChannel = inEleChannel,
                VoiceChannelName = vcName,
                LeaderDisplayName = leaderDisplayName,
                Leader = leader,
                PartyType = partyType,
                Password = pw,
            };

            foreach (var user in fetchedDps)
            {
                var userParams = (LfgEmbedVarParameters)baseParams;
                userParams.TargetUser = user;
                userParams.Role = FFXIVRole.DPS;

                await SendLfgEmbed(userParams);
            }

            foreach (var user in fetchedHealers)
            {
                var userParams = (LfgEmbedVarParameters)baseParams;
                userParams.TargetUser = user;
                userParams.Role = FFXIVRole.Healer;

                await SendLfgEmbed(userParams);
            }

            foreach (var user in fetchedTanks)
            {
                var userParams = (LfgEmbedVarParameters)baseParams;
                userParams.TargetUser = user;
                userParams.Role = FFXIVRole.Tank;

                await SendLfgEmbed(userParams);
            }
        }

        private class LfgEmbedParameters
        {
            public bool InArsenalCategory { get; set; }
            public bool InElementalChannel { get; set; }
            public string VoiceChannelName { get; set; }
            public SocketGuildUser Leader { get; set; }
            public string LeaderDisplayName { get; set; }
            public string PartyType { get; set; }
            public string Password { get; set; }
        }

        private class LfgEmbedVarParameters : LfgEmbedParameters
        {
            public ulong TargetUser { get; set; }
            public FFXIVRole Role { get; set; }
        }

        private Task SendLfgEmbed(LfgEmbedVarParameters args)
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
            var inviteeEmbed = new EmbedBuilder()
                .WithTitle("Your queue has popped!")
                .WithColor(new Discord.Color(0x00, 0x80, 0xFF))
                .WithThumbnailUrl("https://i.imgur.com/4ogfA2S.png")
                .WithDescription($"Your queue for {args.PartyType} has popped! Check the PF for a party under `{args.LeaderDisplayName}` (or something similar) and use the password `{args.Password}` to join! " +
                                 $"Please DM them ({args.Leader}) if you have issues with joining or cannot find the party. " +
                                 "Additionally, the map used to find your portal location can be found here: https://i.imgur.com/Gao2rzI.jpg")
                .WithFields(inviteeFields)
                .Build();
            return Context.Guild.GetUser(args.TargetUser).SendMessageAsync(embed: inviteeEmbed);
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

        private static (int, int, int) GetDesiredRoleCounts(string input)
        {
            input = " " + input;
            var d = input.IndexOf('d');
            var h = input.IndexOf('h');
            var t = input.IndexOf('t');
            int countd = 0, counth = 0, countt = 0;
            if (d != -1) int.TryParse("" + input[d - 1], out countd);
            if (h != -1) int.TryParse("" + input[h - 1], out counth);
            if (t != -1) int.TryParse("" + input[t - 1], out countt);
            return (countd, counth, countt);
        }

        [Command("lfg")]
        [Description("Enter the queue in a queue channel. Takes a role as its first argument, e.g. `~lfg dps`")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task LfgAsync([Remainder] string args = "")
        {
            if (!LfgChannels.ContainsKey(Context.Channel.Id))
                return;

            var queueName = LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            var roles = ParseRoles(args);
            if (roles == FFXIVRole.None)
            {
                await ReplyAsync($"You didn't provide a valid argument, {Context.User.Mention}!\n"
                    + "The proper usage would be: `~lfg <[d][h][t]>`");
                return;
            }

            var enqueuedRoles = new[] {FFXIVRole.DPS, FFXIVRole.Healer, FFXIVRole.Tank}
                .Where(r => roles.HasFlag(r))
                .Where(r => queue.Enqueue(Context.User.Id, r))
                .Aggregate(FFXIVRole.None, (current, r) => current | r);

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
                    response += queued0 + extra;
                    break;
                case 1:
                    response += string.Format(queued1, enqueuedRolesList[0]) + GetPositionString(queue, Context.User.Id) + extra;
                    break;
                case 2:
                    response += string.Format(queued2, enqueuedRolesList[0], enqueuedRolesList[1]) + GetPositionString(queue, Context.User.Id) + extra;
                    break;
                case 3:
                    response += string.Format(queued3, enqueuedRolesList[0], enqueuedRolesList[1], enqueuedRolesList[2]) + GetPositionString(queue, Context.User.Id) + extra;
                    break;
            }

            await ReplyAsync(response);
        }

        [Command("leavequeue")]
        [Alias("unqueue", "leave")]
        [Description("Leaves one or more roles in a channel's queue. Using this with no roles specified removes you from all roles in the channel.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task LeaveQueueAsync([Remainder] string args = "")
        {
            if (!LfgChannels.ContainsKey(Context.Channel.Id))
                return;

            var queueName = LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            var roles = ParseRoles(args);
            var removedRoles = FFXIVRole.None;

            if (roles == FFXIVRole.None)
            {
                // Remove from all
                removedRoles = new[] {FFXIVRole.DPS, FFXIVRole.Healer, FFXIVRole.Tank}
                    .Where(r => queue.Remove(Context.User.Id, r))
                    .Aggregate(removedRoles, (current, r) => current | r);
            }
            else
            {
                // Remove from specified
                removedRoles = new[] {FFXIVRole.DPS, FFXIVRole.Healer, FFXIVRole.Tank}
                    .Where(r => roles.HasFlag(r))
                    .Where(r => queue.Remove(Context.User.Id, r))
                    .Aggregate(removedRoles, (current, r) => current | r);
            }

            var response = Context.User.Mention;
            var removedRolesList = RolesToArray(removedRoles);
            const string removed0 = ", you weren't found in any queues in this channel.";
            const string removedCommon = ", you have been removed from this channel's queue";
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
            await ReplyAsync(response);
        }

        [Command("queue")]
        [Description("Checks your position in the queues of the channel you enter it in.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task QueueAsync([Remainder] string args = "")
        {
            if (args.StartsWith("leave")) // See below
            {
                await LeaveQueueAsync(args.Substring(5));
                return;
            }
            else if (args.Length != 0) // Because people always try to type "~queue dps" etc., just give it to them.
            {
                await LfgAsync(args);
                return;
            }

            // Regular command body:
            if (!LfgChannels.ContainsKey(Context.Channel.Id))
                return;

            var queueName = LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            await ReplyAsync(GetPositionString(queue, Context.User.Id));
        }

        [Command("queuelist")]
        [Description("Checks the queued member counts of the queues of the channel you enter it in.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task QueueListAsync()
        {
            if (!LfgChannels.ContainsKey(Context.Channel.Id))
                return;

            var queueName = LfgChannels[Context.Channel.Id];
            var queue = QueueService.GetOrCreateQueue(queueName);

            await ReplyAsync($"There are currently {queue.Count(FFXIVRole.Tank)} tank(s), {queue.Count(FFXIVRole.Healer)} healer(s), and {queue.Count(FFXIVRole.DPS)} DPS in the queue. (Unique players: {queue.CountDistinct()})");
        }

        private static IList<FFXIVRole> RolesToArray(FFXIVRole roles)
        {
            return new[] {FFXIVRole.DPS, FFXIVRole.Healer, FFXIVRole.Tank}
                .Where(r => roles.HasFlag(r))
                .ToList();
        }

        private static string GetPositionString(FFXIV3RoleQueue queue, ulong uid)
        {
            var dpsCount = queue.Count(FFXIVRole.DPS);
            var healerCount = queue.Count(FFXIVRole.Healer);
            var tankCount = queue.Count(FFXIVRole.Tank);

            var dpsPos = queue.GetPosition(uid, FFXIVRole.DPS);
            var healerPos = queue.GetPosition(uid, FFXIVRole.Healer);
            var tankPos = queue.GetPosition(uid, FFXIVRole.Tank);

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
                    output += $"{healerPos}/${healerCount} in the Healer queue";
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
            output += ".";

            return output == "you are number ." ? $"<@{uid}>, you are not in any queues. If you meant to join the queue, use `~lfg <role>`." : $"<@{uid}>, {output}";
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
    }
}
