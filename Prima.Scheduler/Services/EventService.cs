using Prima.Resources;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Prima.Services;
using TimeZoneNames;

namespace Prima.Scheduler.Services
{
    public class EventService
    {
        private readonly IDbService _db;
        private readonly DiscordClient _client;
        private readonly SpreadsheetService _sheets;

        public EventService(IDbService db, DiscordClient client, SpreadsheetService sheets)
        {
            _db = db;
            _client = client;
            _sheets = sheets;
        }

        public async Task OnMessageEdit(DiscordClient client, MessageUpdateEventArgs updateEvent)
        {
            var message = updateEvent.Message;

            var guild = message.Channel.Guild;
            if (guild == null)
                return;

            var guildConfig = _db.Guilds.FirstOrDefault(g => g.Id == guild.Id);
            if (guildConfig == null)
                return;

            var run = _db.Events.FirstOrDefault(sr => sr.MessageId3 == message.Id);
            if (run == null || run.RunTime < DateTime.Now.ToBinary())
                return;

            Log.Information("Updating information for run from user {UserId}.", run.LeaderId);

            var splitIndex = message.Content.IndexOf("|", StringComparison.Ordinal);
            if (splitIndex == -1) // If for some silly reason they remove the pipe, it'll just take everything after the command name.
            {
                splitIndex = "~schedule ".Length;
            }
            run.Description = message.Content[(splitIndex + 1)..].Trim();
            await _db.UpdateScheduledEvent(run);

            var embed = message.Embeds.FirstOrDefault();
            if (embed == null)
                return;

            Log.Information("Updated description for run {MessageId} to:\n{RunDescription}", run.MessageId3, new string(run.Description.Take(1800).ToArray()));
            
            await message.ModifyAsync(properties => properties.Embed = new DiscordEmbedBuilder(embed)
                .WithDescription("React to the :vibration_mode: on their message to be notified 30 minutes before it begins!\n\n" +
                                 $"**<@{run.LeaderId}>'s full message: {message.JumpLink}**\n\n" +
                                 $"{new string(run.Description.Take(1650).ToArray())}{(run.Description.Length > 1650 ? "..." : "")}\n\n" +
                                 $"**Schedule Overview: <{(run.RunKindCastrum == RunDisplayTypeCastrum.None ? guildConfig.BASpreadsheetLink : guildConfig.CastrumSpreadsheetLink)}>**")
                .Build());
        }

        public async Task OnReactionAdd(DiscordClient client, MessageReactionAddEventArgs reactionEvent)
        {
            var emoji = reactionEvent.Emoji;
            var guild = reactionEvent.Guild;
            var user = reactionEvent.User;
            var message = reactionEvent.Message;

            if (emoji.Name != "📳" || guild == null)
                return;

            var run = _db.Events.FirstOrDefault(e => e.MessageId3 == message.Id);
            if (run == null || run.Notified || run.RunTime < DateTime.Now.ToBinary() || run.SubscribedUsers.Contains(user.Id.ToString()) || user.Id == run.LeaderId || user.Id == _client.CurrentUser.Id)
                return;

            await _db.AddMemberToEvent(run, user.Id);

            var leader = await guild.GetMemberAsync(run.LeaderId);
            var member = await guild.GetMemberAsync(user.Id);

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

            Log.Information("Added member {MemberId} to run {MessageId}.", user.Id, run.MessageId3);
        }

        public async Task OnReactionRemove(DiscordClient client, MessageReactionRemoveEventArgs reactionEvent)
        {
            var emoji = reactionEvent.Emoji;
            var guild = reactionEvent.Guild;
            var user = reactionEvent.User;
            var message = reactionEvent.Message;

            if (emoji.Name != "📳" || guild == null)
                return;

            var run = _db.Events.FirstOrDefault(e => e.MessageId3 == message.Id);
            if (run == null || run.Notified || run.RunTime < DateTime.Now.ToBinary() || !run.SubscribedUsers.Contains(user.Id.ToString()) || user.Id == run.LeaderId || user.Id == _client.CurrentUser.Id)
                return;

            await _db.RemoveMemberToEvent(run, user.Id);

            var leader = await guild.GetMemberAsync(run.LeaderId);
            var member = await guild.GetMemberAsync(user.Id);
            await member.SendMessageAsync($"You have un-RSVP'd for {leader.Nickname ?? leader.Username}'s run.");

            Log.Information("Removed member {MemberId} from run {MessageId}.", user.Id, run.MessageId3);
        }
    }
}
