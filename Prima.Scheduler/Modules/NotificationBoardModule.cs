using Discord;
using Discord.Commands;
using Prima.Attributes;
using Prima.Resources;
using Prima.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using TimeZoneNames;
using Color = Discord.Color;

namespace Prima.Scheduler.Modules
{
    [Name("NotificationBoard")]
    [RequireContext(ContextType.Guild)]
    public class NotificationBoardModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; } // Just used to get the guild's configured command prefix

        [Command("announce", RunMode = RunMode.Async)]
        [Description("Announce an event. Usage: `~announce Time | Description`")]
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
            if (time < DateTime.Now)
            {
                await ReplyAsync("You cannot announce an event in the past!");
                return;
            }

            var tzi = TimeZoneInfo.FindSystemTimeZoneById(Util.PstIdString());
            var tzAbbrs = TZNames.GetAbbreviationsForTimeZone(tzi.Id, "en-US");
            var tzAbbr = tzi.IsDaylightSavingTime(DateTime.Now) ? tzAbbrs.Daylight : tzAbbrs.Standard;

            var color = RunDisplayTypes.GetColorCastrum();
            var outputChannel = Context.Guild.GetTextChannel(guildConfig.CastrumScheduleOutputChannel);
            var embed = await outputChannel.SendMessageAsync(embed: new EmbedBuilder()
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithIconUrl(Context.User.GetAvatarUrl())
                    .WithName(Context.User.ToString()))
                .WithColor(new Color(color.RGB[0], color.RGB[1], color.RGB[2]))
                .WithTimestamp(time)
                .WithTitle($"Event scheduled by {Context.User} on {time.DayOfWeek} at {time.ToShortTimeString()} ({tzAbbr})!")
                .WithDescription(description)
                .Build());

            await ReplyAsync($"Event announced! Notification posted in <#{guildConfig.CastrumScheduleOutputChannel}>.");

            var deleteTime = time.AddHours(1);
            var timeDiff = deleteTime - DateTime.Now;
            new Task(async () => {
                await Task.Delay((int)timeDiff.TotalMilliseconds);
                await embed.DeleteAsync();
            }).Start();
        }

        [Command("unannounce", RunMode = RunMode.Async)]
        [Description("Cancel an event. Usage: `~unannounce Time`")]
        public async Task Unannounce([Remainder]string args)
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            if (guildConfig == null) return;
            if (Context.Channel.Id != guildConfig.CastrumScheduleInputChannel) return;

            var outputChannel = Context.Guild.GetTextChannel(guildConfig.CastrumScheduleOutputChannel);

            var username = Context.User.ToString();
            var time = Util.GetDateTime(args);
            if (time < DateTime.Now)
            {
                await ReplyAsync("That time is in the past!");
                return;
            }

            await foreach (var page in outputChannel.GetMessagesAsync())
            {
                foreach (var message in page)
                {
                    var restMessage = (IUserMessage)message;

                    var embed = restMessage.Embeds.FirstOrDefault();
                    if (embed == null) continue;

                    if (!(embed.Title.Contains(username) && embed.Title.Contains(time.ToShortTimeString()))) continue;

                    await restMessage.ModifyAsync(props =>
                    {
                        props.Embed = new EmbedBuilder()
                            .WithTitle(embed.Title)
                            // ReSharper disable once PossibleInvalidOperationException
                            .WithColor(embed.Color.Value)
                            .WithDescription("❌ Cancelled")
                            .Build();
                    });

                    new Task(async () => {
                        await Task.Delay(1000 * 60 * 60 * 2); // 2 hours
                        await restMessage.DeleteAsync();
                    }).Start();

                    await ReplyAsync("Event cancelled.");
                    return;
                }
            }

            await ReplyAsync("No event by you was found at that time!");
        }
    }
}
