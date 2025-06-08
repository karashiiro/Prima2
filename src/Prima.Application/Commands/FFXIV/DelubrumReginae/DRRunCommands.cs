using Discord;
using Discord.Commands;
using Discord.Net;
using NetStone;
using Prima.DiscordNet;
using Prima.DiscordNet.Attributes;
using Prima.Game.FFXIV;
using Prima.Game.FFXIV.FFLogs;
using Prima.Game.FFXIV.FFLogs.Models;
using Prima.Game.FFXIV.FFLogs.Rules;
using Prima.Game.FFXIV.FFLogs.Services;
using Prima.Resources;
using Prima.Models.FFLogs;
using Prima.Services;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.Application.Commands.FFXIV.DelubrumReginae;

[Name("Delubrum Reginae Runs")]
public class DRRunCommands : ModuleBase<SocketCommandContext>
{
    private readonly IDbService _db;
    private readonly IFFLogsClient _ffLogs;
    private readonly LodestoneClient _lodestone;
    private readonly ILogParsingService _logParsingService;

    public DRRunCommands(
        IDbService db, 
        IFFLogsClient ffLogs, 
        LodestoneClient lodestone,
        ILogParsingService logParsingService)
    {
        _db = db;
        _ffLogs = ffLogs;
        _lodestone = lodestone;
        _logParsingService = logParsingService;
    }

    [Command("setroler", RunMode = RunMode.Async)]
    [Description("(Hosts only) Gives the Delubrum Roler and Run Pinner roles to the specified user.")]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    [CEMRequireRoleOrMentorPlus(RunHostData.RoleId)]
    public async Task SetLeadAsync(IUser user)
    {
        var executorRole = Context.Guild.GetRole(DelubrumProgressionRoles.Executor);
        var runPinner = Context.Guild.GetRole(RunHostData.PinnerRoleId);

        var member = Context.Guild.GetUser(user.Id);
        await member.AddRolesAsync(new[] { executorRole, runPinner });
        await _db.AddTimedRole(executorRole.Id, Context.Guild.Id, member.Id, DateTime.UtcNow.AddHours(4.5));
        await _db.AddTimedRole(runPinner.Id, Context.Guild.Id, member.Id, DateTime.UtcNow.AddHours(4.5));

        await ReplyAsync($"Added lead roles to {member.Mention}!");

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
                "▫️ The Queen Progression");
        }
        catch (HttpException e) when (e.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            Log.Warning("Can't send direct message to user {DiscordName}", member.ToString());
        }
    }

    [Command("rprgdrs", RunMode = RunMode.Async)]
    [RequireOwner]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public async Task RPrgDrs()
    {
        // Removes DRS prog roles from cleared people
        var queenProg = Context.Guild.Roles.FirstOrDefault(r => r.Name == "The Queen Progression");
        var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(queenProg?.Id ?? 0)
            .Select(r => Context.Guild.GetRole(r))
            .ToList();

        var clearedRole = Context.Guild.GetRole(806362589134454805);
        var cleared = Context.Guild.Users
            .Where(u => u.HasRole(clearedRole));

        foreach (var member in cleared)
        {
            await member.RemoveRolesAsync(contingentRoles);
        }

        await ReplyAsync("Done!");
    }

    [Command("addprogrole", RunMode = RunMode.Async)]
    [Alias("addprogroles")]
    [Description(
        "Adds progression roles to server members from a log. Rolers can also manually add roles using this command.")]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    public async Task AddDelubrumProgRoleAsync([Remainder] string args)
    {
        var isFFLogs = FFLogsUtils.IsLogLink(args);
        Log.Information("FFLogs link provided: {IsFFLogsLinkProvided}", isFFLogs);

        if (isFFLogs)
        {
            await ProcessLogAsync(args);
            return;
        }

        await ProcessManualRoleAssignmentAsync(args);
    }

    [Command("removeprogrole", RunMode = RunMode.Async)]
    [Description("(Rolers only) Removes a progression role from a user.")]
    [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
    [CEMRequireRoleOrMentorPlus(DelubrumProgressionRoles.Executor)]
    public async Task RemoveDelubrumProgRoleAsync([Remainder] string args)
    {
        var words = args.Split(' ');

        var members = words
            .Where(w => w.StartsWith('<'))
            .Select(idStr => RegexSearches.NonNumbers.Replace(idStr, ""))
            .Select(ulong.Parse)
            .Select(id =>
                Context.Guild.GetUser(id) ?? (IGuildUser)Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, id)
                    .GetAwaiter().GetResult());

        var roleName = string.Join(' ', words.Where(w => !w.StartsWith('<')));
        roleName = RegexSearches.UnicodeApostrophe.Replace(roleName, "'");

        roleName = roleName.Trim();
        var role = Context.Guild.Roles.FirstOrDefault(r =>
            string.Equals(r.Name.ToLowerInvariant(), roleName.ToLowerInvariant(),
                StringComparison.InvariantCultureIgnoreCase));
        if (role == null)
        {
            var res = await ReplyAsync(
                $"{Context.User.Mention}, no role by that name exists! Make sure you spelled it correctly.");
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
                    Log.Error(e, "Failed to remove role from user {DiscordName}", m.ToString());
                    return Task.CompletedTask;
                }
            }));

        await ReplyAsync("Roles removed!");
    }

    private async Task ProcessLogAsync(string logUrl)
    {
        using var typing = Context.Channel.EnterTypingState();

        try
        {
            Log.Information("Processing log with new service: {LogUrl}", logUrl);

            var request = new LogParsingRequest
            {
                LogUrl = logUrl,
                Rules = new DelubrumReginaeSavageRules()
            };

            var result = await _logParsingService.ParseLogAsync(request);

            if (!result.Success)
            {
                await ReplyAsync($"Error processing log: {result.ErrorMessage}");
                return;
            }

            if (!result.HasAnyChanges)
            {
                await ReplyAsync("No roles to add.");
                if (result.MissedUsers.Any())
                {
                    await ReplyAsync(
                        $"Missed users: ```{string.Join('\n', result.MissedUsers.Distinct())}```\n" +
                        "They may need to re-register with `~iam`.");
                }
                return;
            }

            // Apply role changes through Discord
            var addedAny = await ApplyRoleChangesAsync(result);

            if (addedAny)
                await ReplyAsync("Roles added!");
            else
                await ReplyAsync("No roles to add.");

            if (result.MissedUsers.Any())
                await ReplyAsync(
                    $"Missed users: ```{string.Join('\n', result.MissedUsers.Distinct())}```\n" +
                    "They may need to re-register with `~iam`.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing log in command");
            await ReplyAsync("An error occurred while processing the log. Please try again.");
        }
    }

    private async Task<bool> ApplyRoleChangesAsync(LogParsingResult result)
    {
        var addedAny = false;

        foreach (var assignment in result.RoleAssignments)
        {
            if (!assignment.User.DiscordId.HasValue) continue;

            var discordUser = Context.Guild.GetUser(assignment.User.DiscordId.Value);
            if (discordUser == null)
            {
                Log.Warning("Could not find Discord user with ID: {DiscordId}", assignment.User.DiscordId.Value);
                continue;
            }

            // Skip users who already have the final clear role
            if (discordUser.HasRole(806362589134454805)) // DRS Cleared role
                continue;

            foreach (var roleAction in assignment.RoleActions)
            {
                var role = Context.Guild.Roles.FirstOrDefault(r => r.Name == roleAction.RoleName);
                if (role == null)
                {
                    Log.Warning("Could not find role: {RoleName}", roleAction.RoleName);
                    continue;
                }

                try
                {
                    if (roleAction.ActionType == RoleActionType.Add && !discordUser.HasRole(role))
                    {
                        await discordUser.AddRoleAsync(role);
                        Log.Information("Added role {RoleName} to {DiscordName} - {Reason}", 
                            roleAction.RoleName, discordUser.ToString(), roleAction.Reason);
                        addedAny = true;
                    }
                    else if (roleAction.ActionType == RoleActionType.Remove && discordUser.HasRole(role))
                    {
                        await discordUser.RemoveRoleAsync(role);
                        Log.Information("Removed role {RoleName} from {DiscordName} - {Reason}", 
                            roleAction.RoleName, discordUser.ToString(), roleAction.Reason);
                        addedAny = true;
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to apply role action {ActionType} for role {RoleName} to user {DiscordName}", 
                        roleAction.ActionType, roleAction.RoleName, discordUser.ToString());
                }
            }
        }

        return addedAny;
    }

    private async Task ProcessManualRoleAssignmentAsync(string args)
    {
        var executor = Context.Guild.GetUser(Context.User.Id);
        if (!executor.HasRole(DelubrumProgressionRoles.Executor, Context)
            && !executor.HasRole(579916868035411968, Context) // or Mentor
            && !executor.GuildPermissions.KickMembers) // or can kick users
        {
            Log.Information("User does not have roler role");
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
            .Select(id =>
                Context.Guild.GetUser(id) ?? (IGuildUser)Context.Client.Rest.GetGuildUserAsync(Context.Guild.Id, id)
                    .GetAwaiter().GetResult());

        var roleName = string.Join(' ', words.Where(w => !w.StartsWith('<')));
        roleName = RegexSearches.UnicodeApostrophe.Replace(roleName, "'");

        roleName = roleName.Trim();
        var role = Context.Guild.Roles.FirstOrDefault(r =>
            string.Equals(r.Name.ToLowerInvariant(), roleName.ToLowerInvariant(),
                StringComparison.InvariantCultureIgnoreCase));
        if (role == null)
        {
            Log.Information("Role name {RoleName} is invalid", roleName);
            var res = await ReplyAsync(
                $"{Context.User.Mention}, no role by that name exists! Make sure you spelled it correctly.");
            await Task.Delay(5000);
            await res.DeleteAsync();
            return;
        }

        if (!DelubrumProgressionRoles.Roles.ContainsKey(role.Id))
        {
            Log.Information("Role key {RoleKey} is invalid", role.Id);
            return;
        }

        var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(role.Id)
            .Select(Context.Guild.GetRole)
            .ToList();

        await Task.WhenAll(members
            .Select(m =>
            {
                try
                {
                    return m.AddRolesAsync(contingentRoles.Where(r => !m.MemberHasRole(r, Context)));
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to add roles to user {DiscordName}", m.ToString());
                    return Task.CompletedTask;
                }
            }));

        await ReplyAsync("Roles added!");
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
    public Task GetLFGProgressionCounts()
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
}
