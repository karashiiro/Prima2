using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Prima.Attributes;
using Prima.Resources;
using Serilog;

namespace Prima.Stable.Modules
{
    [Name("Run")]
    public class RunModule : ModuleBase<SocketCommandContext>
    {
        [Command("setpriority")]
        [Description("A command for BA hosts to use that sets a priority speaker for 3 hours.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task SetPrioritySpeaker(IUser other)
        {
            if (Context.Guild == null) return;

            var senderMember = Context.Guild.GetUser(Context.User.Id);
            var hostRole = Context.Guild.GetRole(762072215356702741);
            if (senderMember.Roles.All(r => r.Id != hostRole.Id)) return;

            var prioritySpeakerRole = Context.Guild.GetRole(762071904273432628);

            var member = Context.Guild.GetUser(other.Id);
            await member.AddRoleAsync(prioritySpeakerRole);
            await ReplyAsync("Priority speaker permissions set!");
            _ = Task.Run(async () =>
            {
                await Task.Delay(1000 * 60 * 60 * 3);
                await member.RemoveRoleAsync(prioritySpeakerRole);
            });
        }

        [Command("mute")]
        [Description("A command for BA hosts to use that VC-mutes a user until unmuted, or for 3 hours.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task MuteUser(IUser other)
        {
            if (Context.Guild == null) return;

            var senderMember = Context.Guild.GetUser(Context.User.Id);
            var hostRole = Context.Guild.GetRole(762072215356702741);
            if (senderMember.Roles.All(r => r.Id != hostRole.Id)) return;

            var otherMember = Context.Guild.GetUser(other.Id);
            try
            {
                await otherMember.ModifyAsync(props => { props.Mute = true; });
            }
            catch
            {
                await ReplyAsync("Failed to mute user. Are they already muted?");
                return;
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(10800000);
                try
                {
                    await otherMember.ModifyAsync(props => { props.Mute = false; });
                }
                catch (Exception e)
                {
                    Log.Error(e, "Unmuting failed. Has this user been manually unmuted?");
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
            var hostRole = Context.Guild.GetRole(762072215356702741);
            if (senderMember.Roles.All(r => r.Id != hostRole.Id)) return;

            var otherMember = Context.Guild.GetUser(other.Id);
            try
            {
                await otherMember.ModifyAsync(props => { props.Mute = false; });
            }
            catch
            {
                await ReplyAsync("Failed to unmute user. Are they already unmuted?");
                return;
            }

            await ReplyAsync("User unmuted.");
        }
    }
}
