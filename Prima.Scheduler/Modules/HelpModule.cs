using Discord;
using Discord.Commands;
using Prima.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prima.Resources;
using Color = Discord.Color;

namespace Prima.Scheduler.Modules
{
    public class HelpModule : ModuleBase<SocketCommandContext>
    {
        public CommandService CommandManager { get; set; }
        public IServiceProvider Services { get; set; }

        [Command("help")]
        [Alias("?")]
        [Description("<:LappDumb:736310777463439422>")]
        public async Task HelpAsync()
        {
            var commands = (await CommandManager.GetExecutableCommandsAsync(Context, Services))
                .Where(command => command.Attributes.Any(attr => attr is DescriptionAttribute));

            var fields = new List<EmbedFieldBuilder>();
            foreach (var command in commands)
            {
                var restrictedToAttr = (RestrictToGuildsAttribute)command.Attributes.FirstOrDefault(attr => attr is RestrictToGuildsAttribute);
                if (restrictedToAttr != null && (Context.Guild == null || restrictedToAttr.GuildIds.Contains(Context.Guild.Id)))
                    continue;

                var restrictedFromAttr = (RestrictFromGuildsAttribute)command.Attributes.FirstOrDefault(attr => attr is RestrictFromGuildsAttribute);
                if (restrictedFromAttr != null && (Context.Guild != null && restrictedFromAttr.GuildIds.Contains(Context.Guild.Id)))
                    continue;

                var descAttr = (DescriptionAttribute)command.Attributes.First(attr => attr is DescriptionAttribute);

                var fieldBuilder = new EmbedFieldBuilder()
                    .WithIsInline(true)
                    .WithName(command.Name)
                    .WithValue((restrictedToAttr != null ? $"({Context.Guild.Name}) " : "") + descAttr.Description);
                fields.Add(fieldBuilder);
            }

            var embed = new EmbedBuilder()
                .WithTitle("These are the scheduling commands you can use with Prima in that server:")
                .WithColor(Color.DarkGreen)
                .WithFields(fields)
                .Build();

            await Context.User.SendMessageAsync(embed: embed);
        }

        [Command("refreshqueue")]
        [Alias("refresh")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task RefreshQueueAsync()
        {
            await ReplyAsync($"{Context.User.Mention}, that command has been removed, please see <https://discordapp.com/channels/550702475112480769/550706420543389696/733547955793166397>.");
        }
    }
}
