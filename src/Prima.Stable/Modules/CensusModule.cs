using Discord;
using Discord.Commands;
using Discord.Net;
using Prima.DiscordNet;
using Prima.DiscordNet.Attributes;
using Prima.Game.FFXIV;
using Prima.Models;
using Prima.Resources;
using Prima.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace Prima.Stable.Modules
{
    [Name("Census")]
    public class CensusModule : ModuleBase<SocketCommandContext>
    {
        public IDbService Db { get; set; }
        public CharacterLookup Lodestone { get; set; }

        private const int MessageDeleteDelay = 10000;

        private const string MostRecentZoneRole = "Bozja";
        
        // Declare yourself as a character.
        [Command("iam", RunMode = RunMode.Async)]
        [Alias("i am")]
        [Description("[FFXIV] Register a character to yourself.")]
        public async Task IAmAsync(params string[] parameters)
        {
#if !DEBUG
            if (Context.Guild != null && Context.Guild.Id == SpecialGuilds.CrystalExploratoryMissions)
            {
                const ulong welcome = 573350095903260673;
                const ulong botSpam = 551586630478331904;
                const ulong timeOut = 651966972132851712;
                if (Context.Channel.Id != welcome && Context.Channel.Id != botSpam && Context.Channel.Id != timeOut)
                {
                    await Context.Message.DeleteAsync();
                    var reply = await ReplyAsync("That command is disabled in this channel.");
                    await Task.Delay(10000);
                    await reply.DeleteAsync();
                    return;
                }
            }
#endif

            var guild = Context.Guild ?? Context.Client.Guilds
#if !DEBUG
                .Where(g => g.Id != SpecialGuilds.PrimaShouji && g.Id != SpecialGuilds.EmoteStorage1)
#endif
                .First(g => Context.Client.Rest.GetGuildUserAsync(g.Id, Context.User.Id).GetAwaiter().GetResult() != null);
            Log.Information("Mutual guild ID: {GuildId}", guild.Id);

            var guildConfig = Db.Guilds.Single(g => g.Id == guild.Id);
            var prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            ulong lodestoneId = 0;
            if (parameters.Length != 3)
            {
                if (parameters.Length == 1)
                {
                    if (!ulong.TryParse(parameters[0], out lodestoneId))
                    {
                        var reply = await ReplyAsync($"{Context.User.Mention}, please enter that command in the format `{prefix}iam World Name Surname`.");
                        await Task.Delay(MessageDeleteDelay);
                        await reply.DeleteAsync();
                        return;
                    }
                }
                else
                {
                    var reply = await ReplyAsync($"{Context.User.Mention}, please enter that command in the format `{prefix}iam World Name Surname`.");
                    await Task.Delay(MessageDeleteDelay);
                    await reply.DeleteAsync();
                    return;
                }
            }
            new Task(async () =>
            {
                await Task.Delay(MessageDeleteDelay);
                try
                {
                    await Context.Message.DeleteAsync();
                }
                catch (Exception) { } // Message was already deleted.
            }).Start();

            var world = "";
            var name = "";
            if (parameters.Length == 3)
            {
                world = parameters[0].ToLower();
                name = parameters[1] + " " + parameters[2];
                world = RegexSearches.NonAlpha.Replace(world, string.Empty);
                name = RegexSearches.AngleBrackets.Replace(name, string.Empty);
                name = RegexSearches.UnicodeApostrophe.Replace(name, "'");
                world = world.ToLower();
                world = ("" + world[0]).ToUpper() + world[1..];
                if (world == "Courel" || world == "Couerl")
                {
                    world = "Coeurl";
                }
                else if (world == "Diablos")
                {
                    world = "Diabolos";
                }
            }

            var member = guild.GetUser(Context.User.Id) ?? (IGuildUser)await Context.Client.Rest.GetGuildUserAsync(guild.Id, Context.User.Id);

            using var typing = Context.Channel.EnterTypingState();

            DiscordXIVUser foundCharacter;
            try
            {
                if (parameters.Length == 3)
                {
                    foundCharacter = await Lodestone.GetDiscordXIVUser(world, name, guildConfig.MinimumLevel);
                }
                else
                {
                    foundCharacter = await Lodestone.GetDiscordXIVUser(lodestoneId, guildConfig.MinimumLevel);
                    world = foundCharacter.World;
                }
            }
            catch (CharacterNotFound)
            {
                var reply = await ReplyAsync(
                    $"{Context.User.Mention}, no character matching that name and world was found. Are you sure you typed your world name correctly?");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            catch (NotMatchingFilter)
            {
                var reply = await ReplyAsync(
                    $"{Context.User.Mention}, that character does not have any combat jobs at Level {guildConfig.MinimumLevel}.");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            catch (CharacterLookupError)
            {
                var reply = await ReplyAsync($"{Context.User.Mention}, failed to search for character.");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            catch (ArgumentNullException)
            {
                return;
            }

            if (!await LodestoneUtils.VerifyCharacter(Lodestone, ulong.Parse(foundCharacter.LodestoneId),
                Context.User.Id.ToString()))
            {
                try
                {
                    await Context.User.SendMessageAsync(
                        "We now require that users verify ownership of their FFXIV accounts when using `~iam`. " +
                        "Your Discord ID is:");
                    await Context.User.SendMessageAsync(Context.User.Id
                        .ToString()); // Send this in a separate message to make things easier for mobile users
                    await Context.User.SendMessageAsync(
                        "Please paste this number somewhere into your Lodestone bio here: <https://na.finalfantasyxiv.com/lodestone/my/setting/profile/> and `~iam` again.");
                }
                catch (HttpException e) when (e.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
                {
                    var errReply = await ReplyAsync($"{Context.User.Mention} - character verification failed. Please temporarily enable direct messages and try again for further assistance.");
                    await Task.Delay(MessageDeleteDelay);
                    await errReply.DeleteAsync();
                    return;
                } 
                var reply = await ReplyAsync($"{Context.User.Mention}, your Discord ID could not be found in your Lodestone bio. " +
                                             "Please add the number DM'd to you here: <https://na.finalfantasyxiv.com/lodestone/my/setting/profile/>.");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }

            Log.Information("Verified user {UserId}", Context.User.Id);

            // Get the existing database entry, if it exists.
            var existingLodestoneId = Db.Users.FirstOrDefault(u => u.DiscordId == Context.User.Id)?.LodestoneId;
            var existingDiscordUser = Db.Users.FirstOrDefault(u => u.LodestoneId == foundCharacter.LodestoneId);

            Log.Information("Fetched database entry for user {UserId}. null: {DbEntryIsNull}", Context.User.Id, existingDiscordUser == null);

            // Disallow duplicate characters (CEM policy).
            if (existingDiscordUser != null)
            {
                if (existingDiscordUser.DiscordId != member.Id)
                {
                    var res = await ReplyAsync("That character is already registered to another user.");
                    await Task.Delay(new TimeSpan(0, 0, 30));
                    await res.DeleteAsync();
                    return;
                }

                // TODO: allow if the Discord account is deleted?
            }

            // Insert them into the DB.
            var user = foundCharacter;
            user.Verified = true;
            foundCharacter.DiscordId = Context.User.Id;
            await Db.AddUser(user);

            Log.Information("Added user {UserId} to the database", Context.User.Id);

            // We use the user-provided parameter because the Lodestone format includes the data center.
            var outputName = $"({world}) {foundCharacter.Name}";
            var responseEmbed = new EmbedBuilder()
                .WithTitle(outputName)
                .WithUrl($"https://na.finalfantasyxiv.com/lodestone/character/{foundCharacter.LodestoneId}/")
                .WithColor(Color.Blue)
                .WithDescription("Query matched!")
                .WithThumbnailUrl(foundCharacter.Avatar)
                .Build();

            // Set their nickname.
            try
            {
                await member.ModifyAsync(properties =>
                {
                    properties.Nickname = outputName.Length <= 32
                        ? outputName
                        : foundCharacter.Name;
                });
            }
            catch (HttpException) { }

            Log.Information("Registered character ({World}) {CharaName}", world, foundCharacter.Name);

            var finalReply = await Context.Channel.SendMessageAsync(embed: responseEmbed);
            if (!member.MemberHasRole(573340288815333386, Context))
            {
                await ActivateUser(member, existingLodestoneId, foundCharacter, guildConfig);
            }

            // Cleanup
            await Task.Delay(MessageDeleteDelay);
            await finalReply.DeleteAsync();
        }

        // Set someone else's character.
        [Command("theyare", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task TheyAreAsync(string userMentionStr, params string[] parameters)
        {
            var guildConfig = Db.Guilds.Single(g => g.Id == Context.Guild.Id);
            var prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            if (userMentionStr == null || parameters.Length < 3)
            {
                await ReplyAsync($"{Context.User.Mention}, please enter that command in the format `{prefix}theyare Mention World Name Surname`.");
                return;
            }

            var userMention = await DiscordUtilities.GetUserFromMention(userMentionStr, Context);

            var world = parameters[0].ToLower();
            var name = parameters[1] + " " + parameters[2];
            world = RegexSearches.NonAlpha.Replace(world, string.Empty);
            name = RegexSearches.AngleBrackets.Replace(name, string.Empty);
            name = RegexSearches.UnicodeApostrophe.Replace(name, string.Empty);
            world = world.ToLower();
            world = world[0].ToString().ToUpper() + world[1..];
            if (world == "Courel" || world == "Couerl")
            {
                world = "Coeurl";
            }
            else if (world == "Diablos")
            {
                world = "Diabolos";
            }

            var force = parameters.Length >= 4 && parameters[3].ToLower() == "force";

            var guild = Context.Guild ?? Context.Client.Guilds
#if !DEBUG
                .Where(g => g.Id != SpecialGuilds.PrimaShouji && g.Id != SpecialGuilds.EmoteStorage1)
#endif
                .First(g => Context.Client.Rest.GetGuildUserAsync(g.Id, userMention.Id).GetAwaiter().GetResult() != null);

            var member = guild.GetUser(userMention.Id) ?? (IGuildUser)await Context.Client.Rest.GetGuildUserAsync(guild.Id, userMention.Id);

            // Fetch the character.
            using var typing = Context.Channel.EnterTypingState();

            DiscordXIVUser foundCharacter;
            try
            {
                foundCharacter = await Lodestone.GetDiscordXIVUser(world, name, 0);
            }
            catch (CharacterNotFound)
            {
                await ReplyAsync($"{Context.User.Mention}, no character matching that name and world was found. Are you sure you typed their world name correctly?");
                return;
            }

            if (!force && !await LodestoneUtils.VerifyCharacter(Lodestone, ulong.Parse(foundCharacter.LodestoneId),
                member.Id.ToString()))
            {
                await ReplyAsync("That character does not have their Lodestone ID in their bio; please have them add it. " +
                                 "Alternatively, append `force` to the end of the command to skip this check.");
                return;
            }

            // Get the existing database entries, if they exist.
            var existingLodestoneId = Db.Users.FirstOrDefault(u => u.DiscordId == Context.User.Id)?.LodestoneId;
            var existingDiscordUser = Db.Users.FirstOrDefault(u => u.LodestoneId == foundCharacter.LodestoneId);

            // Disallow duplicate characters (CEM policy).
            if (existingDiscordUser != null && existingDiscordUser.DiscordId != member.Id)
            {
                if (!force)
                {
                    await ReplyAsync($"That character is already registered to <@{existingDiscordUser.DiscordId}>. " +
                                     "If you would like to register this character to someone else, please add the `force` parameter to the end of this command.");
                    return;
                }

                Log.Information("Lodestone character forced off of {UserId}.", existingDiscordUser.DiscordId);
                var memberRole = guild.GetRole(ulong.Parse(guildConfig.Roles["Member"]));
                var existingMember = guild.GetUser(existingDiscordUser.DiscordId);
                await existingMember.RemoveRoleAsync(memberRole);
            }

            // Add the user and character to the database.
            var user = foundCharacter;
            user.Verified = true;
            foundCharacter.DiscordId = userMention.Id;
            await Db.AddUser(user);

            // We use the user-provided parameter because the Lodestone format includes the data center.
            var outputName = $"({world}) {foundCharacter.Name}";
            var responseEmbed = new EmbedBuilder()
                .WithTitle(outputName)
                .WithUrl($"https://na.finalfantasyxiv.com/lodestone/character/{foundCharacter.LodestoneId}/")
                .WithColor(Color.Blue)
                .WithDescription("Query matched!")
                .WithThumbnailUrl(foundCharacter.Avatar)
                .Build();

            // Set their nickname.
            try
            {
                await member.ModifyAsync(properties =>
                {
                    if (outputName.Length <= 32) // Coincidentally both the maximum name length in XIV and on Discord.
                    {
                        properties.Nickname = outputName;
                    }
                    else
                    {
                        properties.Nickname = foundCharacter.Name;
                    }
                });
            }
            catch (HttpException) { }

            Log.Information("Registered character ({World}) {CharaName}", world, foundCharacter.Name);

            await ActivateUser(member, existingLodestoneId, foundCharacter, guildConfig);

            await Context.Channel.SendMessageAsync(embed: responseEmbed);
        }

        private const ulong BozjaRole = 588913532410527754;
        private const ulong EurekaRole = 588913087818498070;
        private const ulong DiademRole = 588913444712087564;

        private async Task ActivateUser(IGuildUser member, string oldLodestoneId, DiscordXIVUser dbEntry, DiscordGuildConfiguration guildConfig)
        {
            var memberRole = member.Guild.GetRole(ulong.Parse(guildConfig.Roles["Member"]));
            await member.AddRoleAsync(memberRole);
            Log.Information("Added {DiscordName} to {Role}", member.ToString(), memberRole.Name);

            Log.Information("Checking Lodestone ID for user {DiscordName}", member.ToString());
            if (oldLodestoneId != dbEntry.LodestoneId)
            {
                var guild = Context.Guild;
                IEnumerable<IRole> roles = new[]
                    {
                        guild.GetRole(DiademRole),
                        guild.GetRole(EurekaRole),
                        guild.GetRole(BozjaRole),
                    }
                    .Concat(new[]
                    {
                        guild.GetRole(ulong.Parse(guildConfig.Roles["Arsenal Master"])),
                        guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared"])),
                        guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared Delubrum Savage"])),
                        guild.GetRole(ulong.Parse(guildConfig.Roles["Savage Queen"])),
                    })
                    .Where(r => r != null);

                await member.RemoveRolesAsync(roles);
                Log.Information("Removed achievement roles from {DiscordName}", member.ToString());
            }
            else
            {
                Log.Information("Nothing to do");
            }

            Log.Information("Checking default content level for user {DiscordName}", member.ToString());

            var contentRole = member.Guild.GetRole(ulong.Parse(guildConfig.Roles[MostRecentZoneRole]));
            var data = await Lodestone.GetCharacter(ulong.Parse(dbEntry.LodestoneId));
            var highestCombatLevel = 0;
            foreach (var classJob in data["ClassJobs"].ToObject<CharacterLookup.ClassJob[]>())
            {
                // Skip non-DoW/DoM or BLU
                if (classJob.JobID is >= 8 and <= 18 or 36) continue;
                if (classJob.Level > highestCombatLevel)
                {
                    highestCombatLevel = classJob.Level;
                }
            }

            if (highestCombatLevel < 80)
            {
                return;
            }

            await member.AddRoleAsync(contentRole);
            Log.Information("Added {DiscordName} to {Role}.", member.ToString(), contentRole.Name);
        }

        [Command("unlink", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task UnlinkCharacter(params string[] args)
        {
            if (args.Length == 1)
            {
                if (!ulong.TryParse(args[0], out var lodestoneId))
                {
                    await ReplyAsync("The Lodestone ID provided is poorly-formatted. Please make sure it is only numbers and try again.");
                    return;
                }

                if (!await Db.RemoveUser(lodestoneId))
                {
                    await ReplyAsync("No user matching that Lodestone ID was found.");
                    return;
                }
            }
            else
            {
                var world = args[0].ToLower();
                var name = Util.Capitalize(args[1]) + " " + Util.Capitalize(args[2]);
                world = RegexSearches.NonAlpha.Replace(world, string.Empty);
                world = Util.Capitalize(world);
                name = RegexSearches.AngleBrackets.Replace(name, string.Empty);
                name = RegexSearches.UnicodeApostrophe.Replace(name, string.Empty);

                if (!await Db.RemoveUser(world, name))
                {
                    await ReplyAsync(
                        "No user matching that world and name was found. Please double-check the spelling of the world and name.");
                    return;
                }
            }

            await ReplyAsync("User unlinked.");
        }

        // Verify BA clear status.
        [Command("verify", RunMode = RunMode.Async)]
        [Description("[FFXIV] Get content completion vanity roles.")]
        public async Task VerifyAsync(params string[] args)
        {
#if !DEBUG
            if (Context.Guild != null && Context.Guild.Id == SpecialGuilds.CrystalExploratoryMissions)
            {
                const ulong welcome = 573350095903260673;
                const ulong botSpam = 551586630478331904;
                if (Context.Channel.Id == welcome || Context.Channel.Id != botSpam)
                {
                    await Context.Message.DeleteAsync();
                    var reply = await ReplyAsync("That command is disabled in this channel.");
                    await Task.Delay(10000);
                    await reply.DeleteAsync();
                    return;
                }
            }
#endif

            var guild = Context.Guild ?? Context.Client.Guilds
#if !DEBUG
                .Where(g => g.Id != SpecialGuilds.PrimaShouji && g.Id != SpecialGuilds.EmoteStorage1)
#endif
                .First(g => Context.Client.Rest.GetGuildUserAsync(g.Id, Context.User.Id).GetAwaiter().GetResult() != null);
            Log.Information("Mutual guild ID: {GuildId}", guild.Id);

            var guildConfig = Db.Guilds.First(g => g.Id == guild.Id);
            var prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            var member = await Context.Client.Rest.GetGuildUserAsync(guild.Id, Context.User.Id);
            var arsenalMaster = guild.GetRole(ulong.Parse(guildConfig.Roles["Arsenal Master"]));
            var cleared = guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared"]));
            var clearedDRS = guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared Delubrum Savage"]));
            var savageQueen = guild.GetRole(ulong.Parse(guildConfig.Roles["Savage Queen"]));

            using var typing = Context.Channel.EnterTypingState();

            var user = Db.Users.FirstOrDefault(u => u.DiscordId == Context.User.Id);
            if (user == null)
            {
                await ReplyAsync($"Your Lodestone information doesn't seem to be stored. Please register it again with `{prefix}iam`.");
                return;
            }

            var lodestoneId = ulong.Parse(user?.LodestoneId ?? args[0]);

            AchievementInfo[] achievements;
            try
            {
                achievements = await Lodestone.GetCharacterAchievements(lodestoneId);
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to fetch user achievements.");
                await ReplyAsync("You don't seem to have your achievements public. " +
                                 "Please temporarily make them public at <https://na.finalfantasyxiv.com/lodestone/my/setting/account/>.");
                return;
            }

            var hasAchievement = false;
            var hasMount = false;
            var hasCastrumLLAchievement1 = false;
            var hasCastrumLLAchievement2 = false;
            var hasDRSAchievement1 = false;
            var hasDRSAchievement2 = false;
            var hasDalriadaAchievement1 = false;
            var hasDalriadaAchievement2 = false;
            
            if (!user.Verified)
            {
                if (!await LodestoneUtils.VerifyCharacter(Lodestone, ulong.Parse(user.LodestoneId), Context.User.Id.ToString()))
                {
                    await ReplyAsync(Properties.Resources.LodestoneDiscordIdNotFoundError);
                    return;
                }

                user.Verified = true;
                await Db.UpdateUser(user);
            }

            if (achievements.Any(achievement => achievement.ID == 2227)) // We're On Your Side I
            {
                Log.Information("Added role " + cleared.Name);
                await member.AddRoleAsync(cleared);
                await ReplyAsync(string.Format(Properties.Resources.LodestoneAchievementRoleSuccess, cleared.Name));
                hasMount = true;
            }
            if (achievements.Any(achievement => achievement.ID == 2229)) // We're On Your Side III
            {
                Log.Information("Added role " + arsenalMaster.Name);
                await member.AddRoleAsync(arsenalMaster);
                await ReplyAsync(string.Format(Properties.Resources.LodestoneAchievementRoleSuccess, arsenalMaster.Name));
                hasAchievement = true;
            }
            if (achievements.Any(achievement => achievement.ID == 2765)) // Operation: Savage Queen of Swords I
            {
                Log.Information("Added role " + clearedDRS.Name);
                await member.AddRoleAsync(clearedDRS);
                await ReplyAsync(string.Format(Properties.Resources.LodestoneAchievementRoleSuccess, clearedDRS.Name));

                var queenProg = guild.Roles.FirstOrDefault(r => r.Name == "The Queen Progression");
                var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(queenProg?.Id ?? 0);
                foreach (var crId in contingentRoles)
                {
                    var cr = guild.GetRole(crId);
                    if (!member.HasRole(cr)) continue;
                    await member.RemoveRoleAsync(cr);
                    Log.Information("Role {RoleName} removed from {User}.", cr.Name, member.ToString());
                }

                hasDRSAchievement1 = true;
            }
            if (achievements.Any(achievement => achievement.ID == 2767)) // Operation: Savage Queen of Swords III
            {
                Log.Information("Added role " + savageQueen.Name);
                await member.AddRoleAsync(savageQueen);
                await ReplyAsync(string.Format(Properties.Resources.LodestoneAchievementRoleSuccess, savageQueen.Name));
                hasDRSAchievement2 = true;
            }

            if (!hasAchievement && !hasMount && !hasCastrumLLAchievement1 && !hasCastrumLLAchievement2 && !hasDRSAchievement1 && !hasDRSAchievement2 && !hasDalriadaAchievement1 && !hasDalriadaAchievement2)
            {
                await ReplyAsync(Properties.Resources.LodestoneMountAchievementNotFoundError);
            }
            else
            {
                await ReplyAsync(
                    "If any achievement role was not added, please check <https://na.finalfantasyxiv.com/lodestone/my/setting/account/> and ensure that your achievements are public.");
            }
        }

        // Check who this user is.
        [Command("whoami", RunMode = RunMode.Async)]
        [Description("[FFXIV] Check the character registered to you.")]
        public async Task WhoAmIAsync()
        {
            if (Context.Guild != null && Context.Guild.Id == SpecialGuilds.CrystalExploratoryMissions)
            {
                const ulong welcome = 573350095903260673;
                if (Context.Channel.Id == welcome)
                {
                    await Context.Message.DeleteAsync();
                    var reply = await ReplyAsync("That command is disabled in this channel.");
                    await Task.Delay(10000);
                    await reply.DeleteAsync();
                    return;
                }
            }

            DiscordXIVUser found;
            try
            {
                found = Db.Users
                    .Single(user => user.DiscordId == Context.User.Id);
            }
            catch (InvalidOperationException)
            {
                await ReplyAsync(Properties.Resources.UserNotInDatabaseError);
                return;
            }

            var responseEmbed = new EmbedBuilder()
                .WithTitle($"({found.World}) {found.Name}")
                .WithUrl($"https://na.finalfantasyxiv.com/lodestone/character/{found.LodestoneId}/")
                .WithColor(Color.Blue)
                .WithThumbnailUrl(found.Avatar)
                .WithDescription($"Verified: {(found.Verified ? "✅" : "❌")}")
                .Build();

            Log.Information("Answered whoami from ({World}) {Name}.", found.World, found.Name);

            await ReplyAsync(embed: responseEmbed);
        }

        // Check who a user is.
        [Command("whois", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task WhoIsAsync([Remainder] string user = "")
        {
            if (!ulong.TryParse(Util.CleanDiscordMention(user), out var uid))
            {
                await ReplyAsync(Properties.Resources.MentionNotProvidedError);
                return;
            }

            var found = Db.Users.SingleOrDefault(u => u.DiscordId == uid);
            if (found == null)
            {
                await ReplyAsync(Properties.Resources.UserNotInDatabaseError);
                return;
            }

            var responseEmbed = new EmbedBuilder()
                .WithTitle($"({found.World}) {found.Name}")
                .WithUrl($"https://na.finalfantasyxiv.com/lodestone/character/{found.LodestoneId}/")
                .WithColor(Color.Blue)
                .WithThumbnailUrl(found.Avatar)
                .WithDescription($"Verified: {(found.Verified ? "✅" : "❌")}")
                .Build();

            await ReplyAsync(embed: responseEmbed);
        }

        // Check who a character is owned by.
        [Command("lwhois", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task LodestoneWhoIsAsync([Remainder] string lodestoneId = "")
        {
            var found = Db.Users.SingleOrDefault(u => u.LodestoneId == lodestoneId);
            if (found == null)
            {
                await ReplyAsync(Properties.Resources.UserNotInDatabaseError);
                return;
            }

            var user = await Context.Client.GetUserAsync(found.DiscordId);

            var responseEmbed = new EmbedBuilder()
                .WithTitle(user.ToString())
                .WithColor(Color.Blue)
                .WithThumbnailUrl(user.GetAvatarUrl())
                .WithDescription(user.Mention + $"\nVerified: {(found.Verified ? "✅" : "❌")}")
                .Build();

            await ReplyAsync(embed: responseEmbed);
        }

        // Check the number of database entries.
        [Command("indexcount")]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task IndexCountAsync()
        {
            await ReplyAsync(Properties.Resources.DBUserCountInProgress);
            await ReplyAsync($"There are {Db.Users.Count()} users in the database.");
            Log.Information("There are {DBEntryCount} users in the database.", Db.Users.Count());
        }

    }
}