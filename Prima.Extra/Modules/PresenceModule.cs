using Discord.Commands;
using Prima.Extra.Services;
using System.Threading.Tasks;

namespace Prima.Modules
{
    /// <summary>
    /// Includes control commands for the <see cref="PresenceService"/>.
    /// </summary>
    [Name("Presence")]
    public class PresenceModule : ModuleBase<SocketCommandContext>
    {
        public PresenceService Presence { get; set; }

        // Change the delay time on the SetPresence Task.
        [Command("presencedelay")]
        [RequireOwner]
        public async Task SetPresenceDelay(string inputTime)
        {
            if (!int.TryParse(inputTime, out int time))
            {
                await ReplyAsync(Properties.Resources.InvalidNumberError);
                return;
            }
            Presence.SetDelay(time);
            await ReplyAsync(Properties.Resources.GenericSuccess);
        }

        // Switch to a new presence.
        [Command("nextpresence")]
        [RequireOwner]
        public async Task NextPresenceAsync()
        {
            await Presence.NextPresence();
        }
    }
}
