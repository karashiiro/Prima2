using Discord;
using Discord.Commands;
using Prima.DiscordNet.Attributes;
using Prima.Resources;
using Prima.Services;
using Serilog;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Prima.DiscordNet.Services;

namespace Prima.Stable.Modules
{
    [Name("Run")]
    public class RunModule : ModuleBase<SocketCommandContext>
    {
        public IDbService Db { get; set; }
        public MuteService Mute { get; set; }

        private const ulong HostSpeakerRoleId = 762072215356702741;
        private const ulong PrioritySpeakerRoleId = 762071904273432628;

        private static readonly Regex MessageRef =
            new(@"discord(?:app)?\.com\/channels\/\d+\/\d+\/(?<MessageID>\d+)", RegexOptions.Compiled);

        [Command("pin", RunMode = RunMode.Async)]
        [Description("Temporarily pins a message in a run channel.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        [CEMRequireRoleOrMentorPlus(RunHostData.PinnerRoleId)]
        public async Task PinMessage(string messageRef = "")
        {
            if (!Context.Channel.Name.Contains("group-chat") && !new ulong[] { 766444330880598076, 858440602588020736 }.Contains(Context.Channel.Id))
            {
                Log.Warning("Command not used in a run channel!");
                return;
            }

            ulong messageId;
            if (Context.Message.ReferencedMessage != null)
            {
                messageId = Context.Message.ReferencedMessage.Id;
            }
            else
            {
                var match = MessageRef.Match(messageRef);
                var result = match.Success
                    ? ulong.TryParse(match.Groups["MessageID"].Value, out messageId)
                    : ulong.TryParse(messageRef, out messageId);
                if (!result)
                {
                    await ReplyAsync("Failed to parse message!");
                    return;
                }
            }

            if (messageId == 0)
            {
                await ReplyAsync("Could not retrieve referenced message!");
                return;
            }

            var message = await Context.Channel.GetMessageAsync(messageId);
            if (message is not IUserMessage userMessage)
            {
                await ReplyAsync("That message is not in this channel!");
                return;
            }

            await Db.AddEphemeralPin(Context.Guild.Id, Context.Channel.Id, messageId, RunHostData.PinnerRoleId, Context.User.Id, DateTime.UtcNow);

            await userMessage.PinAsync();
            await ReplyAsync($"Message pinned for {EphemeralPinManager.HoursUntilRemoval} hours.");
        }

        [Command("unpin", RunMode = RunMode.Async)]
        [Description("Unpins a message pinned by a run member in a run channel.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        [CEMRequireRoleOrMentorPlus(RunHostData.PinnerRoleId)]
        public async Task UnpinMessage(string messageRef = "")
        {
            if (!Context.Channel.Name.Contains("group-chat") && !new ulong[] { 766444330880598076, 858440602588020736 }.Contains(Context.Channel.Id))
            {
                Log.Warning("Command not used in a run channel!");
                return;
            }

            ulong messageId;
            if (Context.Message.ReferencedMessage != null)
            {
                messageId = Context.Message.ReferencedMessage.Id;
            }
            else
            {
                var match = MessageRef.Match(messageRef);
                if (match.Success)
                {
                    ulong.TryParse(match.Groups["MessageID"].Value, out messageId);
                }
                else
                {
                    ulong.TryParse(messageRef, out messageId);
                }
            }

            var message = await Context.Channel.GetMessageAsync(messageId);
            if (message is not IUserMessage userMessage)
            {
                await ReplyAsync("That message is not in this channel!");
                return;
            }

            if (!message.IsPinned)
            {
                await ReplyAsync("That message is not pinned!");
                return;
            }

            // Pinners may only unpin messages that pinners have pinned
            var pinInfo = await Db.EphemeralPins.FirstOrDefaultAsync(e => e.MessageId == messageId);
            if (pinInfo?.PinnerRoleId != RunHostData.PinnerRoleId)
            {
                await ReplyAsync("That message wasn't pinned with `~pin`!");
                return;
            }

            await Db.RemoveEphemeralPin(messageId);
            await userMessage.UnpinAsync();
            await ReplyAsync("Message unpinned.");
        }

        [Command("setpriority")]
        [Description("A command for hosts to use that sets a priority speaker for 3 hours.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task SetPrioritySpeaker(IUser other)
        {
            if (Context.Guild == null) return;

            var senderMember = Context.Guild.GetUser(Context.User.Id);
            var hostRole = Context.Guild.GetRole(HostSpeakerRoleId);
            if (senderMember.Roles.All(r => r.Id != hostRole.Id)) return;

            var prioritySpeakerRole = Context.Guild.GetRole(PrioritySpeakerRoleId);
            var member = Context.Guild.GetUser(other.Id);
            await member.AddRoleAsync(prioritySpeakerRole);
            await Db.AddTimedRole(PrioritySpeakerRoleId, Context.Guild.Id, member.Id, DateTime.UtcNow.AddHours(4.5));
            await ReplyAsync("Priority speaker permissions set!");
        }

        [Command("removepriority")]
        [Description("A command for hosts to use that removes the priority speaker role from someone.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task RemovePrioritySpeaker(IUser other)
        {
            if (Context.Guild == null) return;

            var senderMember = Context.Guild.GetUser(Context.User.Id);
            var hostRole = Context.Guild.GetRole(HostSpeakerRoleId);
            if (senderMember.Roles.All(r => r.Id != hostRole.Id)) return;

            var prioritySpeakerRole = Context.Guild.GetRole(PrioritySpeakerRoleId);
            var member = Context.Guild.GetUser(other.Id);
            await member.RemoveRoleAsync(prioritySpeakerRole);

            await ReplyAsync("Priority speaker permissions removed!");
        }

        [Command("mute")]
        [Description("A command for hosts to use that VC-mutes a user until unmuted, or for 3 hours.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task MuteUser(IUser other)
        {
            if (Context.Guild == null) return;

            var senderMember = Context.Guild.GetUser(Context.User.Id);
            var hostRole = Context.Guild.GetRole(HostSpeakerRoleId);
            if (senderMember.Roles.All(r => r.Id != hostRole.Id)) return;

            var otherMember = Context.Guild.GetUser(other.Id);
            await otherMember.ModifyAsync(props => { props.Mute = true; });

            _ = Task.Run(async () =>
            {
                await Task.Delay(1000 * 60 * 60 * 3);
                try
                {
                    await otherMember.ModifyAsync(props => { props.Mute = false; });
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unmuting failed. Deferring unmute until next connection.");
                    Mute.DeferUnmute(otherMember);
                }
            });

            await ReplyAsync(
                "User muted for 3 hours. You can unmute them at any point before then with `~unmute @user`.");
        }

        [Command("unmute")]
        [Description("A command for hosts to use that unmutes a user that they have previously muted.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task UnmuteUser(IUser other)
        {
            if (Context.Guild == null) return;

            var senderMember = Context.Guild.GetUser(Context.User.Id);
            var hostRole = Context.Guild.GetRole(HostSpeakerRoleId);
            if (senderMember.Roles.All(r => r.Id != hostRole.Id)) return;

            var otherMember = Context.Guild.GetUser(other.Id);
            try
            {
                await otherMember.ModifyAsync(props => { props.Mute = false; });
            }
            catch
            {
                await ReplyAsync("Failed to unmute user; deferring unmute until the next time they connect.");
                Mute.DeferUnmute(otherMember);
                return;
            }

            await ReplyAsync("User unmuted.");
        }
    }
}
