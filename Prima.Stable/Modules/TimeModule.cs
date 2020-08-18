using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Prima.Attributes;
using Prima.Services;

namespace Prima.Stable.Modules
{
    [Name("Time")]
    public class TimeModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; }

        [Command("settimezone")]
        [Description("Sets your own timezone for localized DMs and personal messages.")]
        public async Task SetTimezone(string timezone)
        {
            var dbUser = Db.Users.FirstOrDefault(u => u.DiscordId == Context.User.Id);
            if (dbUser == null)
            {
                await ReplyAsync(Properties.Resources.UserNotInDatabaseError);
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