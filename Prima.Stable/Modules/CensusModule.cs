using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using Prima.Attributes;
using Prima.Models;
using Prima.Resources;
using Prima.Services;
using Prima.Stable.Resources;
using Prima.Stable.Services;
using Prima.XIVAPI;
using Serilog;
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

            var guild = Context.Guild ?? Context.User.MutualGuilds.First(g => Db.Guilds.Any(gc => gc.Id == g.Id));
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
                catch (HttpException) { } // Message was already deleted.
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
                world = ("" + world[0]).ToUpper() + world.Substring(1);
                if (world == "Courel" || world == "Couerl")
                {
                    world = "Coeurl";
                }
                else if (world == "Diablos")
                {
                    world = "Diabolos";
                }
            }

            var member = guild.GetUser(Context.User.Id);
            
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
                var reply = await ReplyAsync($"{Context.User.Mention}, no character matching that name and world was found. Are you sure you typed your world name correctly?");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            catch (NotMatchingFilter)
            {
                var reply = await ReplyAsync($"{Context.User.Mention}, that character does not have any combat jobs at Level {guildConfig.MinimumLevel}.");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            catch (ArgumentNullException)
            {
                return;
            }

            // Get the existing database entry, if it exists.
            var existingLodestoneId = Db.Users.FirstOrDefault(u => u.DiscordId == Context.User.Id)?.LodestoneId;
            var existingDiscordUser = Db.Users.FirstOrDefault(u => u.LodestoneId == foundCharacter.LodestoneId);

            // Disallow duplicate characters (CEM policy).
            if (existingDiscordUser != null && existingDiscordUser.DiscordId != member.Id)
            {
                await ReplyAsync("That character is already registered to another user.");
                return;
            }

            // Insert them into the DB.
            var user = foundCharacter;
            foundCharacter.DiscordId = Context.User.Id;
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
                    properties.Nickname = outputName.Length <= 32
                        ? outputName
                        : foundCharacter.Name;
                });
            }
            catch (HttpException) { }

            Log.Information("Registered character ({World}) {CharaName}", world, foundCharacter.Name);

            var finalReply = await Context.Channel.SendMessageAsync(embed: responseEmbed);
            if (member.Roles.All(r => r.Name != "Time Out"))
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
        public async Task TheyAreAsync(SocketUser userMention, params string[] parameters)
        {
            var guildConfig = Db.Guilds.Single(g => g.Id == Context.Guild.Id);
            var prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            if (userMention == null || parameters.Length < 3)
            {
                var reply = await ReplyAsync($"{Context.User.Mention}, please enter that command in the format `{prefix}iam Mention World Name Surname`.");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            (new Task(async () =>
            {
                await Task.Delay(MessageDeleteDelay);
                try
                {
                    await Context.Message.DeleteAsync();
                }
                catch (HttpException) { } // Message was already deleted.
            })).Start();
            var world = parameters[0].ToLower();
            var name = parameters[1] + " " + parameters[2];
            world = RegexSearches.NonAlpha.Replace(world, string.Empty);
            name = RegexSearches.AngleBrackets.Replace(name, string.Empty);
            name = RegexSearches.UnicodeApostrophe.Replace(name, string.Empty);
            world = world.ToLower();
            world = (world[0].ToString()).ToUpper() + world.Substring(1);
            if (world == "Courel" || world == "Couerl")
            {
                world = "Coeurl";
            }
            else if (world == "Diablos")
            {
                world = "Diabolos";
            }

            var force = parameters.Length >= 4 && parameters[3].ToLower() == "force";

            var guild = Context.Guild ?? userMention.MutualGuilds.First();
            var member = guild.GetUser(userMention.Id);
            if (member == null)
            {
                guild = userMention.MutualGuilds.First();
                member = guild.GetUser(userMention.Id);
            }

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
                var memberRole = member.Guild.GetRole(ulong.Parse(guildConfig.Roles["Member"]));
                var existingMember = member.Guild.GetUser(existingDiscordUser.DiscordId);
                await existingMember.RemoveRoleAsync(memberRole);
            }

            // Add the user and character to the database.
            var user = foundCharacter;
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

            var finalReply = await Context.Channel.SendMessageAsync(embed: responseEmbed);
            await ActivateUser(member, existingLodestoneId, foundCharacter, guildConfig);

            // Cleanup
            await Task.Delay(MessageDeleteDelay);
            await finalReply.DeleteAsync();
        }

        private const ulong BozjaRole = 588913532410527754;
        private const ulong EurekaRole = 588913087818498070;
        private const ulong DiademRole = 588913444712087564;

        private async Task ActivateUser(SocketGuildUser member, string oldLodestoneId, DiscordXIVUser dbEntry, DiscordGuildConfiguration guildConfig)
        {
            var memberRole = member.Guild.GetRole(ulong.Parse(guildConfig.Roles["Member"]));
            await member.AddRoleAsync(memberRole);
            Log.Information("Added {DiscordName} to {Role}.", member.ToString(), memberRole.Name);
            
            var contentRole = member.Guild.GetRole(ulong.Parse(guildConfig.Roles[MostRecentZoneRole]));
            if (contentRole != null && Worlds.List.Contains(dbEntry.World))
            {
                var data = await Lodestone.GetCharacter(ulong.Parse(dbEntry.LodestoneId));
                var highestCombatLevel = 0;
                foreach (var classJob in data["ClassJobs"].ToObject<CharacterLookup.ClassJob[]>())
                {
                    if (classJob.JobID >= 8 && classJob.JobID <= 18) continue;
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
            else if (!Worlds.List.Contains(dbEntry.World))
            {
                var guild = Context.Guild;
                var roles = new[]
                {
                    guild.GetRole(DiademRole),
                    guild.GetRole(EurekaRole),
                    guild.GetRole(BozjaRole),
                };

                if (oldLodestoneId != dbEntry.LodestoneId)
                {
                    roles = roles.Concat(new[]
                    {
                        guild.GetRole(ulong.Parse(guildConfig.Roles["Arsenal Master"])),
                        guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared"])),
                        guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared Castrum"])),
                        guild.GetRole(ulong.Parse(guildConfig.Roles["Siege Liege"])),
                        guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared Delubrum Savage"])),
                        guild.GetRole(ulong.Parse(guildConfig.Roles["Savage Queen"]))
                    }).ToArray();
                }

                await member.RemoveRolesAsync(roles);
            }
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

            var guild = Context.Guild ?? Context.User.MutualGuilds.First(g => Db.Guilds.Any(gc => gc.Id == g.Id));
            Log.Information("Mutual guild ID: {GuildId}", guild.Id);

            var guildConfig = Db.Guilds.First(g => g.Id == guild.Id);
            var prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            var member = guild.GetUser(Context.User.Id);
            var arsenalMaster = guild.GetRole(ulong.Parse(guildConfig.Roles["Arsenal Master"]));
            var cleared = guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared"]));
            var clearedCastrumLacusLitore = guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared Castrum"]));
            var siegeLiege = guild.GetRole(ulong.Parse(guildConfig.Roles["Siege Liege"]));
            var clearedDRS = guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared Delubrum Savage"]));
            var savageQueen = guild.GetRole(ulong.Parse(guildConfig.Roles["Savage Queen"]));

            if (member.HasRole(arsenalMaster) && member.HasRole(siegeLiege) && member.HasRole(savageQueen))
            {
                await ReplyAsync(Properties.Resources.MemberAlreadyHasRoleError);
                return;
            }

            using var typing = Context.Channel.EnterTypingState();

            var user = Db.Users.FirstOrDefault(u => u.DiscordId == Context.User.Id);
            if (args.Length == 0 && user == null)
            {
                await ReplyAsync($"Your Lodestone information doesn't seem to be stored. Please register it again with `{prefix}iam`.");
                return;
            }

            var lodestoneId = ulong.Parse(user?.LodestoneId ?? args[0]);
            var character = await Lodestone.GetCharacter(lodestoneId);
            var achievements = await Lodestone.GetCharacterAchievements(lodestoneId);
            var mounts = await Lodestone.GetCharacterMounts(lodestoneId);

            var hasAchievement = false;
            var hasMount = false;
            var hasCastrumLLAchievement1 = false;
            var hasCastrumLLAchievement2 = false;
            var hasDRSAchievement1 = false;
            var hasDRSAchievement2 = false;
            if (!character["Bio"].ToObject<string>().Contains(Context.User.Id.ToString()))
            {
                await ReplyAsync(Properties.Resources.LodestoneDiscordIdNotFoundError);
                return;
            }
            if (achievements.Any(achievement => achievement.ID == 2229)) // We're On Your Side III
            {
                Log.Information("Added role " + arsenalMaster.Name);
                await member.AddRoleAsync(arsenalMaster);
                await ReplyAsync(Properties.Resources.LodestoneBAAchievementSuccess);
                hasAchievement = true;
            }
            if (achievements.Any(achievement => achievement.ID == 2680)) // Operation: Eagle's Nest I
            {
                Log.Information("Added role " + clearedCastrumLacusLitore.Name);
                await member.AddRoleAsync(clearedCastrumLacusLitore);
                await ReplyAsync(Properties.Resources.LodestoneCastrumLLAchievement1Success); // Make these format strings
                hasCastrumLLAchievement1 = true;
            }
            if (achievements.Any(achievement => achievement.ID == 2682)) // Operation: Eagle's Nest III
            {
                Log.Information("Added role " + siegeLiege.Name);
                await member.AddRoleAsync(siegeLiege);
                await ReplyAsync(Properties.Resources.LodestoneCastrumLLAchievement2Success);
                hasCastrumLLAchievement2 = true;
            }
            if (achievements.Any(achievement => achievement.ID == 2765)) // Operation: Savage Queen of Swords I
            {
                Log.Information("Added role " + clearedDRS.Name);
                await member.AddRoleAsync(clearedDRS);
                await ReplyAsync(Properties.Resources.LodestoneDRSSuccess1);

                var queenProg = member.Guild.Roles.FirstOrDefault(r => r.Name == "The Queen Progression");
                var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(queenProg?.Id ?? 0);
                foreach (var crId in contingentRoles)
                {
                    var cr = member.Guild.GetRole(crId);
                    if (member.HasRole(cr)) continue;
                    await member.AddRoleAsync(cr);
                    Log.Information("Role {RoleName} added to {User}.", cr.Name, member.ToString());
                }

                hasDRSAchievement1 = true;
            }
            if (achievements.Any(achievement => achievement.ID == 2767)) // Operation: Savage Queen of Swords III
            {
                Log.Information("Added role " + savageQueen.Name);
                await member.AddRoleAsync(savageQueen);
                await ReplyAsync(Properties.Resources.LodestoneDRSSuccess2);

                var queenProg = member.Guild.Roles.FirstOrDefault(r => r.Name == "The Queen Progression");
                var contingentRoles = DelubrumProgressionRoles.GetContingentRoles(queenProg?.Id ?? 0);
                foreach (var crId in contingentRoles)
                {
                    var cr = member.Guild.GetRole(crId);
                    if (member.HasRole(cr)) continue;
                    await member.AddRoleAsync(cr);
                    Log.Information("Role {RoleName} added to {User}.", cr.Name, member.ToString());
                }

                hasDRSAchievement2 = true;
            }
            if (mounts.Any(m => m.Name == "Demi-Ozma"))
            {
                Log.Information("Added role {Role} to {DiscordName}.", cleared.Name, Context.User.ToString());
                await member.AddRoleAsync(cleared);
                await ReplyAsync(Properties.Resources.LodestoneBAMountSuccess);
                hasMount = true;
            }

            if (!hasAchievement && !hasMount && !hasCastrumLLAchievement1 && !hasCastrumLLAchievement2 && !hasDRSAchievement1 && !hasDRSAchievement2)
            {
                await ReplyAsync(Properties.Resources.LodestoneMountAchievementNotFoundError);
            }

            if (user == null)
            {
                await Db.AddUser(new DiscordXIVUser
                {
                    DiscordId = Context.User.Id,
                    LodestoneId = args[0],
                    Avatar = character["Avatar"].ToObject<string>(),
                    Name = character["Name"].ToObject<string>(),
                    World = character["World"].ToObject<string>(),
                });
            }
        }

        // Check who this user is.
        [Command("whoami", RunMode = RunMode.Async)]
        [Description("[FFXIV] Check what character you have registered.")]
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