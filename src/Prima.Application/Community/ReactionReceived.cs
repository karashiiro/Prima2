using Discord;
using Discord.WebSocket;
using Prima.Game.FFXIV;
using Prima.Models;
using Prima.Services;
using Serilog;

namespace Prima.Application.Community;

public static class ReactionReceived
{
    // Even though most role reactions are currently handled by a separate deployment unit, roles
    // with validation are still handled here. This should eventually be migrated as well.

    private const ulong BozjaRole = 588913532410527754;
    private const ulong EurekaRole = 588913087818498070;

    public static async Task HandlerAdd(DiscordSocketClient client, IDbService db, CharacterLookup lodestone,
        Cacheable<IUserMessage, ulong> message, Cacheable<IMessageChannel, ulong> cchannel, SocketReaction reaction)
    {
        var ichannel = await cchannel.GetOrDownloadAsync();

        if (ichannel is SocketGuildChannel channel)
        {
            var guild = channel.Guild;
            var member = guild.GetUser(reaction.UserId) ??
                         (IGuildUser)await client.Rest.GetGuildUserAsync(guild.Id, reaction.UserId);
            var disConfig = db.Guilds.FirstOrDefault(g => g.Id == guild.Id);
            if (disConfig == null)
            {
                return;
            }

            if (ichannel.Id == 590757405927669769 && reaction.Emote is Emote emote &&
                disConfig.RoleEmotes.TryGetValue(emote.Id.ToString(), out var roleIdString))
            {
                var roleId = ulong.Parse(roleIdString);
                var role = member.Guild.GetRole(roleId);
                var dbEntry = db.Users.FirstOrDefault(u => u.DiscordId == reaction.UserId);

                if (roleId is BozjaRole or EurekaRole)
                {
                    if (dbEntry == null)
                    {
                        await member.SendMessageAsync(
                            "You are not currently registered in the system (potential data loss).\n" +
                            "Please register again in `#welcome` before adding one of these roles.");
                        Log.Information("User {DiscordName} tried to get a content role and is not registered",
                            member.ToString());
                        return;
                    }

                    var lodestoneId = ulong.Parse(dbEntry.LodestoneId);
                    var data = await lodestone.GetCharacter(lodestoneId);
                    if (data == null)
                    {
                        Log.Error("Failed to get Lodestone character (id={LodestoneId})", lodestoneId);
                        return;
                    }

                    var classJobs = data["ClassJobs"];
                    if (classJobs == null)
                    {
                        Log.Error("Failed to get ClassJobs from Lodestone character (id={LodestoneId})", lodestoneId);
                        return;
                    }

                    var classJobsObj = classJobs.ToObject<CharacterLookup.ClassJob[]>();
                    if (classJobsObj == null)
                    {
                        Log.Error("Failed to unmarshal ClassJobs from Lodestone character (id={LodestoneId})",
                            lodestoneId);
                        return;
                    }

                    var highestCombatLevel = 0;
                    foreach (var classJob in classJobsObj)
                    {
                        // Skip non-DoW/DoM or BLU
                        if (classJob.JobID is >= 8 and <= 18 or 36) continue;
                        if (classJob.Level > highestCombatLevel)
                        {
                            highestCombatLevel = classJob.Level;
                        }
                    }

                    if (roleId == BozjaRole && highestCombatLevel < 80)
                    {
                        await member.SendMessageAsync("You must be at least level 80 to access that category.");
                        return;
                    }

                    if (roleId == EurekaRole && highestCombatLevel < 70)
                    {
                        await member.SendMessageAsync("You must be at least level 70 to access that category.");
                        return;
                    }

                    // 60 is already the minimum requirement to register.

                    await member.AddRoleAsync(role);
                    Log.Information("Role {Role} was added to {DiscordUser}", role.Name, member.ToString());
                }
            }
            else if (guild.Id == 550702475112480769 && ichannel.Id is 552643167808258060 or 768886934084648960 &&
                     reaction.Emote.Name == "✅")
            {
                await member.SendMessageAsync(
                    $"You have begun the verification process. Your **Discord account ID** is `{member.Id}`.\n"
                    + "Please add this somewhere in your FFXIV Lodestone Character Profile.\n"
                    + "You can edit your account description here: https://na.finalfantasyxiv.com/lodestone/my/setting/profile/\n\n"
                    + $"After you have put your Discord account ID in your Lodestone profile, please use `{db.Config.Prefix}verify` to get your clear role.\n"
                    + "The Lodestone may not immediately update following updates to your achievements, so please wait a few hours and try again if this is the case.");
            }
        }
    }

    public static async Task HandlerRemove(IDbService db, Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> cchannel, SocketReaction reaction)
    {
        var ichannel = await cchannel.GetOrDownloadAsync();

        if (ichannel is SocketGuildChannel channel)
        {
            var guild = channel.Guild;
            var member = guild.GetUser(reaction.UserId);
            DiscordGuildConfiguration disConfig;
            try
            {
                disConfig = db.Guilds.Single(g => g.Id == guild.Id);
            }
            catch (InvalidOperationException)
            {
                return;
            }

            if (ichannel.Id == 590757405927669769 && reaction.Emote is Emote emote &&
                disConfig.RoleEmotes.TryGetValue(emote.Id.ToString(), out var roleIdString))
            {
                var roleId = ulong.Parse(roleIdString);
                var role = member.Guild.GetRole(roleId);
                await member.RemoveRoleAsync(role);
                Log.Information("Role {Role} was removed from {DiscordUser}", role.Name, member.ToString());
            }
        }
    }
}