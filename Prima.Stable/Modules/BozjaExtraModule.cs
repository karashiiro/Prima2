using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Prima.Attributes;
using Prima.Resources;
using Prima.Services;
using Serilog;
using Color = Discord.Color;

namespace Prima.Stable.Modules
{
    [Name("Bozja Extra Module")]
    public class BozjaExtraModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; }
        public HttpClient Http { get; set; }
        public IServiceProvider Services { get; set; }

        [Command("bozhelp", RunMode = RunMode.Async)]
        [Description("Shows help information for the extra Bozja commands.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task BozjaHelpAsync()
        {
            var guildConfig = Db.Guilds.FirstOrDefault(g => g.Id == Context.Guild.Id);
            var prefix = Db.Config.Prefix.ToString();
            if (guildConfig != null && guildConfig.Prefix != ' ')
                prefix = guildConfig.Prefix.ToString();

            var commands = await DiscordUtilities.GetFormattedCommandList(Services, Context, prefix,
                "Bozja Extra Module", except: new List<string> {"bozhelp"});

            var embed = new EmbedBuilder()
                .WithTitle("Useful Commands (Bozja)")
                .WithColor(Color.LightOrange)
                .WithDescription(commands)
                .Build();

            await ReplyAsync(embed: embed);
        }

        [Command("setroler", RunMode = RunMode.Async)]
        [Description("Gives the Delubrum Roler role to the specified user.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task SetLeadAsync(IUser user)
        {
            if (!Context.User.HasRole(RunHostData.RoleId, Context))
            {
                var res = await ReplyAsync($"{Context.User.Mention}, you don't have a run host role!");
                await Task.Delay(5000);
                await res.DeleteAsync();
                return;
            }

            var role = Context.Guild.GetRole(DelubrumProgressionRoles.Executor);
            var member = Context.Guild.GetUser(user.Id);
            await member.AddRoleAsync(role);
            await ReplyAsync($"Added roler role to {member.Mention}!");

            try
            {
                await member.SendMessageAsync(
                    "You have been given the Delubrum Roler role for 3 1/2 hours!\n" +
                    "You can now use the commands `~addprogrole @User Role Name` and `~removeprogrole @User Role Name` to change " +
                    "the progression roles of run members!\n\n" +
                    "Available roles:\n" +
                    "▫️ Trinity Seeker Progression\n" +
                    "▫️ Dahu Progression\n" +
                    "▫️ Queen's Guard Progression\n" +
                    "▫️ Phantom Progression\n" +
                    "▫️ Trinity Avowed Progression\n" +
                    "▫️ Stygimoloch Lord Progression\n" +
                    "▫️ The Queen Progression");
            }
            catch (HttpException e) when (e.DiscordCode == 50007)
            {
                Log.Warning("Can't send direct message to user {User}.", member.ToString());
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(new TimeSpan(3, 30, 0));
                await member.RemoveRoleAsync(role);
            });
        }

        [Command("addprogrole", RunMode = RunMode.Async)]
        [Description("Adds a progression role to a user.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task AddDelubrumProgRoleAsync(IUser user, [Remainder]string roleName)
        {
            if (Context.Guild == null) return;

            roleName = roleName.Trim();
            var role = Context.Guild.Roles.FirstOrDefault(r =>
                string.Equals(r.Name.ToLowerInvariant(), roleName.ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase));
            if (role == null)
            {
                var res = await ReplyAsync($"{Context.User.Mention}, no role by that name exists! Make sure you spelled it correctly.");
                await Task.Delay(5000);
                await res.DeleteAsync();
                return;
            }

            if (!DelubrumProgressionRoles.Ids.Contains(role.Id)) return;

            if (!Context.User.HasRole(DelubrumProgressionRoles.Executor, Context))
            {
                var res = await ReplyAsync($"{Context.User.Mention}, you don't have the roler role!");
                await Task.Delay(5000);
                await res.DeleteAsync();
                return;
            }

            var member = Context.Guild.GetUser(user.Id);
            if (member.HasRole(role, Context))
            {
                await ReplyAsync($"{user.Mention} already has that role!");
            }
            else
            {
                await member.AddRoleAsync(role);
                Log.Information("Role {RoleName} added to {User}.", role.Name, user.ToString());
                await ReplyAsync($"Role added to {user.Mention}.");
            }
        }

        [Command("removeprogrole", RunMode = RunMode.Async)]
        [Description("Removes a progression role from a user.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task RemoveDelubrumProgRoleAsync(IUser user, [Remainder]string roleName)
        {
            if (Context.Guild == null) return;

            roleName = roleName.Trim();
            var role = Context.Guild.Roles.FirstOrDefault(r =>
                string.Equals(r.Name.ToLowerInvariant(), roleName.ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase));
            if (role == null)
            {
                var res = await ReplyAsync($"{Context.User.Mention}, no role by that name exists! Make sure you spelled it correctly.");
                await Task.Delay(5000);
                await res.DeleteAsync();
                return;
            }

            if (!DelubrumProgressionRoles.Ids.Contains(role.Id)) return;

            if (!Context.User.HasRole(DelubrumProgressionRoles.Executor, Context))
            {
                var res = await ReplyAsync($"{Context.User.Mention}, you don't have the roler role!");
                await Task.Delay(5000);
                await res.DeleteAsync();
                return;
            }

            var member = Context.Guild.GetUser(user.Id);
            if (!member.HasRole(role, Context))
            {
                await ReplyAsync($"{user.Mention} already does not have that role!");
            }
            else
            {
                await member.RemoveRoleAsync(role);
                Log.Information("Role {RoleName} removed from {User}.", role.Name, user.ToString());
                await ReplyAsync($"Role removed from {user.Mention}.");
            }
        }

        [Command("star", RunMode = RunMode.Async)]
        [Description("Shows the Bozjan Southern Front star mob guide.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task StarMobsAsync() => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/muvBR1Z.png");

        [Command("cluster", RunMode = RunMode.Async)]
        [Description("Shows the Bozjan Southern Front cluster path guide.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task BozjaClustersAsync() => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/WANkcVe.jpeg");
    }
}