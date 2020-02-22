using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Prima.Attributes;
using Prima.Services;
using Serilog;
using System;
using System.Threading.Tasks;

namespace Prima.Modules
{
    /// <summary>
    /// This includes extraneous server operation functions.
    /// </summary>
    [Name("Extra")]
    [ConfigurationPreset(Preset.Extra)]
    public class ExtraModule : ModuleBase<SocketCommandContext>
    {
        public ServerClockService Clocks { get; set; }

        // Add a clock to a voice channel.
        [Command("addclock")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task AddClockAsync(ulong channelId, string tzId)
        {
            if (Context.Guild == null)
            {
                await ReplyAsync(Properties.Resources.MustBeUsedInGuildError);
                return;
            }
            if (Context.Guild.GetChannel(channelId) is SocketTextChannel)
            {
                await ReplyAsync(Properties.Resources.MustBeUsedOnVoiceChannelError);
                return;
            }
            try
            {
                await Clocks.AddClock(Context.Guild.Id, channelId, tzId);
            }
            catch (ArgumentNullException)
            {
                await ReplyAsync(Properties.Resources.NotATimezoneIdError);
                return;
            }
            await ReplyAsync(Properties.Resources.ClockAddSuccess);
            Log.Information(Properties.Resources.ClockAddSuccess);
        }

        // Remove a clock from a voice channel.
        [Command("removeclock")]
        [RequireUserPermission(GuildPermission.Administrator)]
        public async Task RemoveClockAsync(ulong channelId)
        {
            if (Context.Guild == null)
            {
                await ReplyAsync(Properties.Resources.MustBeUsedInGuildError);
                return;
            }
            if (Context.Guild.GetChannel(channelId) is SocketTextChannel)
            {
                await ReplyAsync(Properties.Resources.MustBeUsedOnVoiceChannelError);
                return;
            }
            await Clocks.RemoveClock(channelId);
            await ReplyAsync(Properties.Resources.ClockRemoveSuccess);
            Log.Information(Properties.Resources.ClockRemoveSuccess);
        }
    }
}
