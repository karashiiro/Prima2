using Discord;
using Discord.WebSocket;
using Prima.Resources;
using Prima.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using TimeZoneNames;

namespace Prima.Scheduler.Services
{
    public class EventService
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;
        private readonly SpreadsheetService _sheets;

        public EventService(DbService db, DiscordSocketClient client, SpreadsheetService sheets)
        {
            _db = db;
            _client = client;
            _sheets = sheets;
        }

        public async Task OnMessageEdit(Cacheable<IMessage, ulong> cmessage, SocketMessage smessage, ISocketMessageChannel ichannel)
        {
            var newMessage = await cmessage.DownloadAsync();

            if (!(ichannel is SocketGuildChannel channel))
                return;

            var guild = channel.Guild;
            var guildConfig = _db.Guilds.FirstOrDefault(g => g.Id == guild.Id);
            if (guildConfig == null)
                return;

            var run = _db.Events.FirstOrDefault(sr => sr.MessageId3 == newMessage.Id);
            if (run == null || run.RunTime < DateTime.Now.ToBinary())
                return;

            Log.Information("Updating information for run from user {UserId}.", run.LeaderId);

            var splitIndex = newMessage.Content.IndexOf("|", StringComparison.Ordinal);
            if (splitIndex == -1) // If for some silly reason they remove the pipe, it'll just take everything after the command name.
            {
                splitIndex = "~schedule ".Length;
            }
            run.Description = newMessage.Content.Substring(splitIndex + 1).Trim();
            await _db.UpdateScheduledEvent(run);

            var embedChannel = guild.GetTextChannel(run.RunKindCastrum == RunDisplayTypeCastrum.None ? guildConfig.ScheduleOutputChannel : guildConfig.CastrumScheduleOutputChannel);
            if (!(await embedChannel.GetMessageAsync(run.EmbedMessageId) is IUserMessage message))
                return;

            var embed = message.Embeds.FirstOrDefault()?.ToEmbedBuilder()
                .WithDescription("React to the :vibration_mode: on their message to be notified 30 minutes before it begins!\n\n" +
                                 $"**{guild.GetUser(run.LeaderId).Mention}'s full message: {newMessage.GetJumpUrl()}**\n\n" +
                                 $"{new string(run.Description.Take(1650).ToArray())}{(run.Description.Length > 1650 ? "..." : "")}\n\n" +
                                 $"**Schedule Overview: <{(run.RunKindCastrum == RunDisplayTypeCastrum.None ? guildConfig.BASpreadsheetLink : guildConfig.CastrumSpreadsheetLink)}>**")
                .Build();

            Log.Information("Updated description for run {MessageId} to:\n{RunDescription}", run.MessageId3, new string(run.Description.Take(1800).ToArray()));

            if (embed == null)
                return;
            await message.ModifyAsync(properties => properties.Embed = embed);
        }

        public async Task OnReactionAdd(Cacheable<IUserMessage, ulong> cmessage, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            if (!(reaction.Emote is Emoji emoji) || emoji.Name != "📳" || !(ichannel is SocketGuildChannel channel))
                return;

            var run = _db.Events.FirstOrDefault(e => e.MessageId3 == cmessage.Id);
            if (run == null || run.Notified || run.RunTime < DateTime.Now.ToBinary() || run.SubscribedUsers.Contains(reaction.UserId.ToString()) || reaction.UserId == run.LeaderId || reaction.UserId == _client.CurrentUser.Id)
                return;

            await _db.AddMemberToEvent(run, reaction.UserId);

            var leader = channel.GetUser(run.LeaderId);
            var member = _client.GetUser(reaction.UserId);

            var runTime = DateTime.FromBinary(run.RunTime);

            var dbUser = _db.Users.FirstOrDefault(u => u.DiscordId == member.Id);
            // ReSharper disable once JoinDeclarationAndInitializer
            TimeZoneInfo tzi;
            var (customTzi, localizedRunTime) = Util.GetLocalizedTimeForUser(dbUser, runTime);
            tzi = customTzi ?? TimeZoneInfo.FindSystemTimeZoneById("America/Los_Angeles");
            if (localizedRunTime != default)
            {
                runTime = localizedRunTime;
                runTime = runTime.AddHours(8);
            }

            var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
            var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

            await member.SendMessageAsync($"You have RSVP'd for {leader.Nickname ?? leader.Username}'s run on on {runTime.DayOfWeek} at {runTime.ToShortTimeString()} ({tzAbbr}) [{runTime.DayOfWeek}, {(Month)runTime.Month} {runTime.Day}]! :thumbsup:");

            Log.Information("Added member {MemberId} to run {MessageId}.", reaction.UserId, run.MessageId3);
        }

        public async Task OnReactionRemove(Cacheable<IUserMessage, ulong> cmessage, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            if (!(reaction.Emote is Emoji emoji) || emoji.Name != "📳" || !(ichannel is SocketGuildChannel channel))
                return;

            var run = _db.Events.FirstOrDefault(e => e.MessageId3 == cmessage.Id);
            if (run == null || run.Notified || run.RunTime < DateTime.Now.ToBinary() || !run.SubscribedUsers.Contains(reaction.UserId.ToString()) || reaction.UserId == run.LeaderId || reaction.UserId == _client.CurrentUser.Id)
                return;

            await _db.RemoveMemberToEvent(run, reaction.UserId);

            var leader = channel.GetUser(run.LeaderId);
            var member = _client.GetUser(reaction.UserId);
            await member.SendMessageAsync($"You have un-RSVP'd for {leader.Nickname ?? leader.Username}'s run.");

            Log.Information("Removed member {MemberId} from run {MessageId}.", reaction.UserId, run.MessageId3);
        }
    }
}
