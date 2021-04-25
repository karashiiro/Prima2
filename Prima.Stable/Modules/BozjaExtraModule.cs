using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Prima.Attributes;
using Prima.Models;
using Prima.Resources;
using Prima.Services;
using Prima.Stable.Models.FFLogs;
using Prima.Stable.Services;
using Serilog;
using Color = Discord.Color;
// ReSharper disable MemberCanBePrivate.Global

namespace Prima.Stable.Modules
{
    [Name("Bozja Extra Module")]
    public class BozjaExtraModule : ModuleBase<SocketCommandContext>
    {
        public IDbService Db { get; set; }
        public HttpClient Http { get; set; }
        public IServiceProvider Services { get; set; }
        public FFLogsAPI FFLogsAPI { get; set; }
        public CharacterLookup Lodestone { get; set; }

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
        [Alias("addprogroles")]
        [Description("Adds progression roles to server members from a log. Rolers can also manually add roles using this command.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task AddDelubrumProgRoleAsync([Remainder]string args)
        {
            if (Context.Guild == null) return;

            var isFFLogs = FFLogs.IsLogLink.Match(args).Success;
            if (isFFLogs)
            {
                await Context.Guild.DownloadUsersAsync();
                await ReadLog(args);
                return;
            }

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

            await Context.Guild.DownloadUsersAsync();
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

            await Context.Guild.DownloadUsersAsync();
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

        private class PotentialDbUser
        {
            public string Name { get; set; }
            public string World { get; set; }
            public DiscordXIVUser User { get; set; }
        }

        private async Task RegisterUser(IEnumerable<IGuildUser> members, PotentialDbUser potentialUser)
        {
            if (potentialUser.User != null)
                return;
            var member = members.FirstOrDefault(m => m.Nickname == $"({potentialUser.World}) {potentialUser.Name}");
            if (member == null) return;
            var userInfo = await Lodestone.GetDiscordXIVUser(potentialUser.World, potentialUser.Name, 0);
            if (userInfo != null)
            {
                userInfo.DiscordId = member.Id;
                await Db.AddUser(userInfo);
                potentialUser.User = userInfo;
            }
        }

        private async Task ReadLog(string logLink)
        {
            using var typing = Context.Channel.EnterTypingState();

            var logMatch = FFLogs.LogLinkToIdRegex.Match(logLink);
            if (!logMatch.Success)
            {
                await ReplyAsync("That doesn't look like a log link!");
                return;
            }

            var logId = logMatch.Value;
            var req = FFLogs.BuildLogRequest(logId);
            var res = (await FFLogsAPI.MakeGraphQLRequest<LogInfo>(req)).Content.Data.ReportInfo;
            if (res == null)
            {
                await ReplyAsync("That log is private; please make it unlisted or public.");
                return;
            }

            var encounters = res.Fights
                .Where(f => f.Kill != null && f.FriendlyPlayers != null);
            var originalUsers = res.MasterData.Actors
                .Where(a => a.Server != null)
                .ToList();
            var members = Context.Guild.Users;
            var potentialUsers = originalUsers.ToDictionary(a => a.Id, a => a)
                .Select(kvp => new KeyValuePair<int, PotentialDbUser>(kvp.Key, new PotentialDbUser
                {
                    Name = kvp.Value.Name,
                    World = kvp.Value.Server,
                    User = Db.Users.FirstOrDefault(u => string.Equals(u.Name, kvp.Value.Name, StringComparison.InvariantCultureIgnoreCase)
                                                        && string.Equals(u.World, kvp.Value.Server, StringComparison.InvariantCultureIgnoreCase)),
                }))
                .Select(async kvp =>
                {
                    var (id, potentialUser) = kvp;
                    await RegisterUser(members, potentialUser);
                    return new KeyValuePair<int, DiscordXIVUser>(id, potentialUser.User);
                })
                .ToList();
            // We can't cleanly go from a KeyValuePair<int, Task<DiscordXIVUser>>
            // to a KeyValuePair<int, DiscordXIVUser>, so let's break it up into
            // multiple queries.
            var users = (await Task.WhenAll(potentialUsers))
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);;
            var missedUsers = new List<LogInfo.ReportDataWrapper.ReportData.Report.Master.Actor>();

            var addedAny = false;
            foreach (var encounter in encounters)
            {
                var roleName = encounter.Name;
                if (roleName == "The Queen's Guard")
                    roleName = "Queen's Guard";
                roleName += " Progression";
                var role = Context.Guild.Roles.FirstOrDefault(r => r.Name == roleName);
                if (role == null)
                {
                    Log.Error("Role {RoleName} does not exist!", roleName);
                    continue;
                }

                var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(role.Id)
                    .Select(r => Context.Guild.GetRole(r))
                    .ToList();

                var killRole = Context.Guild.GetRole(DelubrumProgressionRoles.GetKillRole(role.Name));

                foreach (var id in encounter.FriendlyPlayers)
                {
                    if (!users.ContainsKey(id))
                    {
                        var actor = originalUsers.Find(a => a.Id == id);
                        if (actor != null)
                        {
                            missedUsers.Add(actor);
                        }
                        continue;
                    }
                    
                    var user = Context.Guild.GetUser(users[id].DiscordId);
                    if (user == null) continue;

                    foreach (var progRole in contingentRoles)
                    {
                        Log.Information("Checking role {RoleName} on user {User}", progRole.Name, user);
                        if (!user.HasRole(progRole))
                        {
                            addedAny = true;
                            await user.AddRoleAsync(progRole);
                            Log.Information("Added role {RoleName} to user {User}", progRole.Name, user);
                        }
                    }

                    if (encounter.Kill == true)
                    {
                        Log.Information("Checking role {RoleName} on user {User}", killRole.Name, user);
                        if (!user.HasRole(killRole))
                        {
                            addedAny = true;
                            await user.AddRoleAsync(killRole);
                            Log.Information("Added role {RoleName} to {User}", killRole.Name, user);
                        }
                    }
                }
            }

            if (addedAny)
                await ReplyAsync("Roles added!");
            else
                await ReplyAsync("No roles to add.");

            if (missedUsers.Any())
                await ReplyAsync($"Missed users: ```{missedUsers.Select(a => $"({a.Server}) {a.Name}").Distinct().Aggregate("", (agg, next) => agg + $"{next}\n") + "```"}\nThey may need to re-register with `~iam`.");
        }

        [Command("progcounts", RunMode = RunMode.Async)]
        [Description("Get the progression counts of all guild members for Delubrum Reginae (Savage).")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task GetProgressionCounts()
        {
            const ulong clearedRole = DelubrumProgressionRoles.ClearedDelubrumSavage;
            var queenRole = DelubrumProgressionRoles.Roles.First(r => r.Value == "The Queen Progression").Key;
            var stygLordRole = DelubrumProgressionRoles.Roles.First(r => r.Value == "Stygimoloch Lord Progression").Key;
            var trinityAvowedRole = DelubrumProgressionRoles.Roles.First(r => r.Value == "Trinity Avowed Progression").Key;
            var queensGuardRole = DelubrumProgressionRoles.Roles.First(r => r.Value == "Queen's Guard Progression").Key;
            var trinitySeekerRole = DelubrumProgressionRoles.Roles.First(r => r.Value == "Trinity Seeker Progression").Key;
            const ulong lfgRole = 806290575086845962;

            var members = Context.Guild.Users;

            var freshProgMembers = members.Where(m => m.HasRole(lfgRole) && !m.HasRole(trinitySeekerRole));
            var trinitySeekerMembers = members.Where(m => m.HasRole(trinitySeekerRole) && !m.HasRole(queensGuardRole));
            var queensGuardMembers = members.Where(m => m.HasRole(queensGuardRole) && !m.HasRole(trinityAvowedRole));
            var trinityAvowedMembers = members.Where(m => m.HasRole(trinityAvowedRole) && !m.HasRole(stygLordRole));
            var stygLordMembers = members.Where(m => m.HasRole(stygLordRole) && !m.HasRole(queenRole));
            var queenMembers = members.Where(m => m.HasRole(queenRole) && !m.HasRole(clearedRole));
            var clearedMembers = members.Where(m => m.HasRole(clearedRole));

            const string outFormat = "Fresh Progression (Estimated): {0}\n" +
                                     "Trinity Seeker Progression: {1}\n" +
                                     "Queen's Guard Progression: {2}\n" +
                                     "Trinity Avowed Progression: {3}\n" +
                                     "Stygimoloch Lord Progression: {4}\n" +
                                     "The Queen Progression: {5}\n" +
                                     "Cleared: {6}";
            return ReplyAsync(string.Format(
                outFormat,
                freshProgMembers.Count(),
                trinitySeekerMembers.Count(),
                queensGuardMembers.Count(),
                trinityAvowedMembers.Count(),
                stygLordMembers.Count(),
                queenMembers.Count(),
                clearedMembers.Count()));
        }

        [Command("lfgcountsdrs", RunMode = RunMode.Async)]
        [Description("Get the LFG role counts of all guild members for Delubrum Reginae (Savage).")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task GetBAProgressionCounts()
        {
            var members = Context.Guild.Users;

            var lfgDrsMembers = members.Where(m => m.HasRole(806290575086845962));
            var lfgFragMembers = members.Where(m => m.HasRole(810201516291653643));
            var lfgTrinitySeekerMembers = members.Where(m => m.HasRole(810201667814948877));
            var lfgQueensGuardMembers = members.Where(m => m.HasRole(810201946249232384));
            var lfgStygLordMembers = members.Where(m => m.HasRole(810202020279615520));
            var lfgTrinityAvowedMembers = members.Where(m => m.HasRole(810201890775629877));
            var lfgQueenMembers = members.Where(m => m.HasRole(810201946249232384));
            var lfgReclearMembers = members.Where(m => m.HasRole(829005698322661447));

            const string outFormat = "LFG Counts (includes overlap):\n" +
                                     "LFG Delubrum Reginae (Savage): {0}\n" +
                                     "LFG DRS Fresh Prog: {1}\n" +
                                     "LFG DRS Trinity Seeker Prog: {2}\n" +
                                     "LFG DRS Queen's Guard Prog: {3}\n" +
                                     "LFG DRS Stygimoloch Lord Prog: {4}\n" +
                                     "LFG DRS Trinity Avowed Prog: {5}\n" +
                                     "LFG DRS The Queen Prog: {6}\n" +
                                     "LFG DRS Reclear: {7}";
            return ReplyAsync(string.Format(
                outFormat,
                lfgDrsMembers.Count(),
                lfgFragMembers.Count(),
                lfgTrinitySeekerMembers.Count(),
                lfgQueensGuardMembers.Count(),
                lfgStygLordMembers.Count(),
                lfgTrinityAvowedMembers.Count(),
                lfgQueenMembers.Count(),
                lfgReclearMembers.Count()));
        }

        [Command("star", RunMode = RunMode.Async)]
        [Description("Shows the Bozjan Southern Front star mob guide.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task StarMobsAsync() => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/muvBR1Z.png");

        [Command("cluster", RunMode = RunMode.Async)]
        [Alias("clusters")]
        [Description("Shows the Bozjan Southern Front cluster path guide.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task BozjaClustersAsync() => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/FuG4wDK.png");
        
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

        [Command("fatefulwords", RunMode = RunMode.Async)]
        [Description("Shows the Fateful Words guide.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task FatefulWordsAsync() => DiscordUtilities.PostImage(Http, Context, "https://cdn.discordapp.com/attachments/808869784195563521/813152064342589443/Fateful_Words_debuffs.png");

        [Command("brands", RunMode = RunMode.Async)]
        [Alias("hotcold")]
        [Description("Shows the Trinity Avowed debuff guide. Also `~hotcold`.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task BrandsHotColdAsync() => DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/un5nvg4.png");

        [Command("pipegame", RunMode = RunMode.Async)]
        [Alias("ladders")]
        [Description("Shows the Trinity Avowed ladder guide. Also `~ladders`.")]
        [RateLimit(TimeSeconds = 1, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task PipeGameAsync()
        {
            await DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/i2ms13x.png");
            await DiscordUtilities.PostImage(Http, Context, "https://i.imgur.com/JgiTTK9.png");
        }
    }
}
