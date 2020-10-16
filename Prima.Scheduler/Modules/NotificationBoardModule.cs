using Discord;
using Discord.Commands;
using Prima.Attributes;
using Prima.Resources;
using Prima.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace Prima.Scheduler.Modules
{
    [Name("NotificationBoard")]
    [RequireContext(ContextType.Guild)]
    public class NotificationBoardModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; } // Just used to get the guild's configured command prefix

        [Command("announce")]
        [Description("Announce a Castrum Lacus Litore run. Usage: `~announce Time | Description`")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task Announce([Remainder]string args)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;
            if (Context.Channel.Id != guildConfig.CastrumScheduleInputChannel) return;
            var prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            var splitIndex = args.IndexOf("|", StringComparison.Ordinal);
            if (splitIndex == -1)
            {
                await ReplyAsync($"{Context.User.Mention}, please provide parameters with that command.\n" +
                                 "A well-formed command would look something like:\n" +
                                 $"`{prefix}announce 5:00PM | This is a fancy description!`");
                return;
            }

            var parameters = args.Substring(0, splitIndex).Trim();
            var description = args.Substring(splitIndex + 1).Trim();

            if (parameters.IndexOf(":", StringComparison.Ordinal) == -1)
            {
                await ReplyAsync($"{Context.User.Mention}, please specify a time for your run in your command!");
                return;
            }

            var time = Util.GetDateTime(parameters);

            var color = RunDisplayTypes.GetColorCastrum();
            var outputChannel = Context.Guild.GetTextChannel(guildConfig.CastrumScheduleOutputChannel);
            var embed = await outputChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithIconUrl(Context.User.GetAvatarUrl())
                    .WithName(Context.User.ToString()))
                .WithColor(new Color(color.RGB[0], color.RGB[1], color.RGB[2]))
                .WithTimestamp(time)
                .WithTitle($"Run scheduled by {Context.User} on {time.DayOfWeek} at {time.ToShortTimeString()} (PDT)!")
                .WithDescription(description)
                .Build());

            await ReplyAsync($"Run announced! Notification posted in <#{guildConfig.CastrumScheduleOutputChannel}>.");
            
            var deleteTime = time.AddHours(1);
            var timeDiff = deleteTime - DateTime.Now;
            (new Task(async () => {
                await Task.Delay((int)timeDiff.TotalMilliseconds);
                await embed.DeleteAsync();
            })).Start();
        }
    }
}
