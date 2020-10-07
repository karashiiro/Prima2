using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Prima.Attributes;
using Prima.Resources;
using Prima.Stable.Services;
using Serilog;

namespace Prima.Stable.Modules
{
    [Name("Run")]
    public class RunModule : ModuleBase<SocketCommandContext>
    {
        public MuteService Mute { get; set; }

        private const ulong HostSpeakerRoleId = 762072215356702741;
        private const ulong PrioritySpeakerRoleId = 762071904273432628;

        [Command("setpriority")]
        [Description("A command for BA hosts to use that sets a priority speaker for 3 hours.")]
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
            await ReplyAsync("Priority speaker permissions set!");
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000 * 60 * 60 * 3);
                await member.RemoveRoleAsync(prioritySpeakerRole);
            });
        }

        [Command("removepriority")]
        [Description("A command for BA hosts to use that removes the priority speaker role from someone.")]
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
        [Description("A command for BA hosts to use that VC-mutes a user until unmuted, or for 3 hours.")]
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
        [Description("A command for BA hosts to use that unmutes a user that they have previously muted.")]
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
