using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Prima.Scheduler.Attributes;
using Prima.Scheduler.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.Scheduler.Modules
{
    public class HelpModule : BaseCommandModule
    {
        public CommandService CommandManager { get; set; }

        [Command("help")]
        [Aliases("?")]
        [Description("<:LappDumb:736310777463439422>")]
        public async Task HelpAsync(CommandContext ctx)
        {
            var commands = ctx.GetExecutableCommandsAsync()
                .Where(command => !string.IsNullOrEmpty(command.Description));

            var fields = new List<DiscordEmbedField>();
            await foreach (var command in commands)
            {
                var restrictedToAttr = (RestrictToGuildsAttribute)command.CustomAttributes
                    .FirstOrDefault(attr => attr is RestrictToGuildsAttribute);
                
                var field = new DiscordEmbedField()
                    .WithIsInline(true)
                    .WithName(command.Name)
                    .WithValue((restrictedToAttr != null ? $"({ctx.Guild.Name}) " : "") + command.Description);
                fields.Add(field);
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle("These are the scheduling commands you can use with Prima in that server:")
                .WithColor(DiscordColor.DarkGreen)
                .WithFields(fields)
                .Build();

            if (ctx.Guild != null)
            {
                await ctx.Member.SendMessageAsync(embed);
            }
            else
            {
                await ctx.RespondAsync(embed);
            }
        }
    }
}
