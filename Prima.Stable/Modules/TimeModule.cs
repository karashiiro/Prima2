using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Prima.DiscordNet.Attributes;
using Prima.DiscordNet.Services;

namespace Prima.Stable.Modules
{
    [Name("Time")]
    public class TimeModule : ModuleBase<SocketCommandContext>
    {
        public IDbService Db { get; set; }

        [Command("settimezone")]
        [Description("Sets your own timezone for localized DMs and personal messages.")]
        public async Task SetTimezone([Remainder] string timezone)
        {
            if (Context.Channel.Name == "welcome") // This should really be a precondition...
            {
                var r = await ReplyAsync("That command cannot be used in this channel.");
                await Task.Delay(5000);
                await r.DeleteAsync();
                return;
            }

            var dbUser = Db.Users.FirstOrDefault(u => u.DiscordId == Context.User.Id);
            if (dbUser == null)
            {
                await ReplyAsync("Your database information seems to be missing. Please use `~iam World FirstName LastName` again to regenerate it.");
                return;
            }

            try
            {
                TimeZoneInfo.FindSystemTimeZoneById(timezone);
            }
            catch (TimeZoneNotFoundException)
            {
                await ReplyAsync("Unable to find that timezone. Please refer to the **TZ database name** column of https://en.wikipedia.org/wiki/List_of_tz_database_time_zones for a list of valid options.");
                return;
            }
            catch (InvalidTimeZoneException)
            {
                await ReplyAsync("Registry data on that timezone has been corrupted.");
                return;
            }

            dbUser.Timezone = timezone;
            await Db.UpdateUser(dbUser);
            await ReplyAsync("Timezone updated successfully!");
        }
    }
}