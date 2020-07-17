using Discord.Commands;
using System.Threading.Tasks;
using Prima.Stable.Services;

namespace Prima.Modules
{
    /// <summary>
    /// Includes control commands for the <see cref="PresenceService"/>.
    /// </summary>
    [Name("Presence")]
    [RequireOwner]
    public class PresenceModule : ModuleBase<SocketCommandContext>
    {
        public PresenceService Presence { get; set; }

        // Change the delay time on the SetPresence Task.
        [Command("presencedelay")]
        public async Task SetPresenceDelay(string inputTime)
        {
            if (!int.TryParse(inputTime, out var time))
            {
                await ReplyAsync(Properties.Resources.InvalidNumberError);
                return;
            }
            Presence.SetDelay(time);
            await ReplyAsync(Properties.Resources.GenericSuccess);
        }

        // Switch to a new presence.
        [Command("nextpresence")]
        public async Task NextPresenceAsync()
        {
            await Presence.NextPresence();
        }
    }
}
