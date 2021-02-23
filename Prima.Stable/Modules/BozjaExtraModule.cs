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
        [Description("(Hosts only) Gives the Delubrum Roler role to the specified user.")]
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
                    "You have been given the Delubrum Roler role for 4 1/2 hours!\n" +
                    "You can now use the commands `~addprogrole @User Role Name` and `~removeprogrole @User Role Name` to change " +
                    "the progression roles of run members!\n" +
                    "You can also modify multiple users at once by using `~addprogrole @User1 @User2 Role Name`.\n\n" +
                    "Available roles:\n" +
                    "▫️ Trinity Seeker Progression\n" +
                    "▫️ Queen's Guard Progression\n" +
                    "▫️ Trinity Avowed Progression\n" +
                    "▫️ Stygimoloch Lord Progression\n" +
                    "▫️ The Queen Progression");
            }
            catch (HttpException e) when (e.DiscordCode == 50007)
            {
                Log.Warning("Can't send direct message to user {User}.", member.ToString());
            }

            await Db.AddTimedRole(role.Id, Context.Guild.Id, member.Id, DateTime.UtcNow.AddHours(4.5));
        }

        [Command("addprogrole", RunMode = RunMode.Async)]
        [Description("(Rolers only) Adds a progression role to a user.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task AddDelubrumProgRoleAsync([Remainder]string args)
        {
            if (Context.Guild == null) return;

            var executor = Context.Guild.GetUser(Context.User.Id);
            if (!executor.HasRole(DelubrumProgressionRoles.Executor, Context)
                && !executor.HasRole(579916868035411968, Context) // or Mentor
                && !executor.GuildPermissions.KickMembers) // or can kick users
            {
                var res = await ReplyAsync($"{Context.User.Mention}, you don't have the roler role!");
                await Task.Delay(5000);
                await res.DeleteAsync();
                return;
            }

            var words = args.Split(' ');

            var members = words
                .Where(w => w.StartsWith('<'))
                .Select(idStr => RegexSearches.NonNumbers.Replace(idStr, ""))
                .Select(ulong.Parse)
                .Select(id => Context.Guild.GetUser(id));
            
            var roleName = string.Join(' ', words.Where(w => !w.StartsWith('<')));
            roleName = RegexSearches.UnicodeApostrophe.Replace(roleName, "'");

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

            if (!DelubrumProgressionRoles.Roles.Keys.Contains(role.Id)) return;
            var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(role.Id)
                .Select(Context.Guild.GetRole)
                .ToList();

            await Task.WhenAll(members
                .Select(m =>
                {
                    try
                    {
                        return m.AddRolesAsync(contingentRoles.Where(r => !m.HasRole(r)));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Failed to add roles to user {User}.", m.ToString());
                        return Task.CompletedTask;
                    }
                }));

            await ReplyAsync("Roles added!");
        }

        [Command("removeprogrole", RunMode = RunMode.Async)]
        [Description("(Rolers only) Removes a progression role from a user.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task RemoveDelubrumProgRoleAsync([Remainder] string args)
        {
            if (Context.Guild == null) return;

            var executor = Context.Guild.GetUser(Context.User.Id);
            if (!executor.HasRole(DelubrumProgressionRoles.Executor, Context)
                && !executor.HasRole(579916868035411968, Context) // or Mentor
                && !executor.GuildPermissions.KickMembers) // or can kick users
            {
                var res = await ReplyAsync($"{Context.User.Mention}, you don't have the roler role!");
                await Task.Delay(5000);
                await res.DeleteAsync();
                return;
            }

            var words = args.Split(' ');

            var members = words
                .Where(w => w.StartsWith('<'))
                .Select(idStr => RegexSearches.NonNumbers.Replace(idStr, ""))
                .Select(ulong.Parse)
                .Select(id => Context.Guild.GetUser(id));

            var roleName = string.Join(' ', words.Where(w => !w.StartsWith('<')));
            roleName = RegexSearches.UnicodeApostrophe.Replace(roleName, "'");

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

            if (!DelubrumProgressionRoles.Roles.Keys.Contains(role.Id)) return;

            await Task.WhenAll(members
                .Select(m =>
                {
                    try
                    {
                        return m.RemoveRoleAsync(role);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Failed to remove role from user {User}.", m.ToString());
                        return Task.CompletedTask;
                    }
                }));

            await ReplyAsync("Roles removed!");
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
        
        [Command("qgreflect", RunMode = RunMode.Async)]
        [Description("Shows Queen's Guard reflect positions.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task QueensGuardReflectAsync() => DiscordUtilities.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/808869784195563521/809107279697150012/robotstemplate2.png");

        [Command("chess", RunMode = RunMode.Async)]
        [Description("Shows Queen Chess strat.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task QueenChessStratAsync() => DiscordUtilities.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/808869784195563521/809107442793185310/nJ4vHiK.png");
    }
}
