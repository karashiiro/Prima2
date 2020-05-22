using Discord;
using Discord.WebSocket;
using Prima.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Prima.Resources;

namespace Prima.Scheduler.Services
{
    public class EventService
    {
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;

        public EventService(DbService db, DiscordSocketClient client)
        {
            _db = db;
            _client = client;
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

            var run = _db.Events.FirstOrDefault(sr => sr.MessageId == newMessage.Id);
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

            var embedChannel = guild.GetTextChannel(guildConfig.ScheduleOutputChannel);
            var message = await embedChannel.GetMessageAsync(run.EmbedMessageId) as IUserMessage;

            var embed = message?.Embeds.FirstOrDefault()?.ToEmbedBuilder()
                .WithDescription("React to the :vibration_mode: on their message to be notified 30 minutes before it begins!\n\n" +
                                 $"**{guild.GetUser(run.LeaderId).Mention}'s full message: {newMessage.GetJumpUrl()}**\n\n" +
                                 $"{new string(run.Description.Take(1900).ToArray())}{(run.Description.Length > 1900 ? "..." : "")}\n\n" +
                                 $"**Schedule Overview: <{guildConfig.BASpreadsheetLink}>**")
                .Build();

            Log.Information("Updated description for run {MessageId} to:\n{RunDescription}", run.MessageId, new string(run.Description.Take(1800).ToArray()));

            if (embed == null)
                return;
            await message.ModifyAsync(properties => properties.Embed = embed);
        }

        public async Task OnReactionAdd(Cacheable<IUserMessage, ulong> cmessage, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            if (!(reaction.Emote is Emoji emoji) || emoji.Name != "📳" || !(ichannel is SocketGuildChannel channel))
                return;

            var run = _db.Events.FirstOrDefault(e => e.MessageId == cmessage.Id);
            if (run == null || run.Notified || run.RunTime < DateTime.Now.ToBinary() || run.SubscribedUsers.Contains(reaction.UserId) || reaction.UserId == run.LeaderId || reaction.UserId == _client.CurrentUser.Id)
                return;

            await _db.AddMemberToEvent(run, reaction.UserId);

            var leader = channel.GetUser(run.LeaderId);
            var member = _client.GetUser(reaction.UserId);
            var runTime = DateTime.FromBinary(run.RunTime);
            await member.SendMessageAsync($"You have RSVP'd for {leader.Nickname ?? leader.Username}'s run on on {runTime.DayOfWeek} at {runTime.ToShortTimeString()} (PDT) [{runTime.DayOfWeek}, {(Month)runTime.Month} {runTime.Day}]! :thumbsup:");

            Log.Information("Added member {MemberId} to run {MessageId}.", reaction.UserId, run.MessageId);
        }

        public async Task OnReactionRemove(Cacheable<IUserMessage, ulong> cmessage, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            if (!(reaction.Emote is Emoji emoji) || emoji.Name != "📳" || !(ichannel is SocketGuildChannel channel))
                return;

            var run = _db.Events.FirstOrDefault(e => e.MessageId == cmessage.Id);
            if (run == null || run.Notified || run.RunTime < DateTime.Now.ToBinary() || !run.SubscribedUsers.Contains(reaction.UserId) || reaction.UserId == run.LeaderId || reaction.UserId == _client.CurrentUser.Id)
                return;

            await _db.RemoveMemberToEvent(run, reaction.UserId);

            var leader = channel.GetUser(run.LeaderId);
            var member = _client.GetUser(reaction.UserId);
            await member.SendMessageAsync($"You have un-RSVP'd for {leader.Nickname ?? leader.Username}'s run.");

            Log.Information("Removed member {MemberId} from run {MessageId}.", reaction.UserId, run.MessageId);
        }
    }
}
