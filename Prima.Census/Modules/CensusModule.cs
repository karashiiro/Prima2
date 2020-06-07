using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Prima.Attributes;
using Prima.Models;
using Prima.Resources;
using Prima.Services;
using Prima.XIVAPI;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using Color = Discord.Color;

namespace Prima.Modules
{
    /// <summary>
    /// Includes XIVAPI access commands.
    /// </summary>
    [Name("XIVAPI")]
    public class CensusModule : ModuleBase<SocketCommandContext>
    {
        public DbService Db { get; set; }
        public XIVAPIService XIVAPI { get; set; }

        private const int MessageDeleteDelay = 10000;

        // Declare yourself as a character.
        [Command("iam", RunMode = RunMode.Async)]
        [Alias("i am")]
        public async Task IAmAsync(params string[] parameters)
        {
            var guild = Context.Guild ?? Context.User.MutualGuilds.First(g => Db.Guilds.Any(gc => gc.Id == g.Id));
            Log.Information("Mututal guild ID: {GuildId}", guild.Id);

            var guildConfig = Db.Guilds.Single(g => g.Id == guild.Id);
            var prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            if (parameters.Length != 3)
            {
                var reply = await ReplyAsync($"{Context.User.Mention}, please enter that command in the format `{prefix}iam World Name Surname`.");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            (new Task(async () => {
                await Task.Delay(MessageDeleteDelay);
                try
                {
                    await Context.Message.DeleteAsync();
                }
                catch (HttpException) {} // Message was already deleted.
            })).Start();
            var world = parameters[0].ToLower();
            var name = parameters[1] + " " + parameters[2];
            world = RegexSearches.NonAlpha.Replace(world, string.Empty);
            name = RegexSearches.AngleBrackets.Replace(name, string.Empty);
            name = RegexSearches.UnicodeApostrophe.Replace(name, string.Empty);
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

            var member = guild.GetUser(Context.User.Id);
            if (member.Roles.Any(r => r.Name == "Time Out"))
            {
                return;
            }

            var cleared = guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared"]));

            using var typing = Context.Channel.EnterTypingState();

            DiscordXIVUser foundCharacter;
            try
            {
                foundCharacter = await XIVAPI.GetDiscordXIVUser(world, name, guildConfig.MinimumLevel);
            }
            catch (XIVAPICharacterNotFoundException)
            {
                var reply = await ReplyAsync($"{Context.User.Mention}, no character matching that name and world was found. Are you sure you typed your world name correctly?");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            catch (XIVAPINotMatchingFilterException)
            {
                var reply = await ReplyAsync($"This is a security notice. {Context.User.Mention}, that character does not have any combat jobs at Level {guildConfig.MinimumLevel}.");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            catch (ArgumentNullException)
            {
                return;
            }

            // Add the user and character to the database.
            try
            {
                // Update an existing file.
                // If they're verified and aren't reregistering the same character, return.
                if (cleared != null && member.Roles.Contains(cleared))
                {
                    if (Db.Users.Single(user => user.DiscordId == Context.User.Id).LodestoneId != foundCharacter.LodestoneId)
                    {
                        var message = await ReplyAsync($"{Context.User.Mention}, you have already verified your character.");
                        await Task.Delay(5000);
                        await message.DeleteAsync();
                        return;
                    }
                }
            }
            catch (InvalidOperationException) {}
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
            catch (HttpException) {}

            Log.Information("Registered character ({World}) {CharaName}", world, foundCharacter.Name);

            // Cleanup
            var finalReply = await ReplyAsync(embed: responseEmbed);
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

            if (userMention == null || parameters.Length != 3)
            {
                var reply = await ReplyAsync($"{Context.User.Mention}, please enter that command in the format `{prefix}iam Mention World Name Surname`.");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            (new Task(async () => {
                await Task.Delay(MessageDeleteDelay);
                try
                {
                    await Context.Message.DeleteAsync();
                }
                catch (HttpException) {} // Message was already deleted.
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
                foundCharacter = await XIVAPI.GetDiscordXIVUser(world, name, 0);
            }
            catch (XIVAPICharacterNotFoundException)
            {
                var reply = await ReplyAsync($"{Context.User.Mention}, no character matching that name and world was found. Are you sure you typed your world name correctly?");
                await Task.Delay(MessageDeleteDelay);
                await reply.DeleteAsync();
                return;
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
            catch (HttpException) {}

            Log.Information("Registered character ({World}) {CharaName}", world, foundCharacter.Name);

            // Cleanup
            var finalReply = await ReplyAsync(embed: responseEmbed);
            await Task.Delay(MessageDeleteDelay);
            await finalReply.DeleteAsync();
        }

        // Verify BA clear status.
        [Command("verify", RunMode = RunMode.Async)]
        public async Task VerifyAsync(params string[] args)
        {
            var guild = Context.Guild ?? Context.User.MutualGuilds.First(g => Db.Guilds.Any(gc => gc.Id == g.Id));
            Log.Information("Mututal guild ID: {GuildId}", guild.Id);

            var guildConfig = Db.Guilds.First(g => g.Id == guild.Id);
            var prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            var member = guild.GetUser(Context.User.Id);
            var arsenalMaster = guild.GetRole(ulong.Parse(guildConfig.Roles["Arsenal Master"]));
            var cleared = guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared"]));
            
            if (member.Roles.Contains(arsenalMaster))
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

            var character = await XIVAPI.GetCharacter(user?.LodestoneId ?? ulong.Parse(args[0]));
            var hasAchievement = false;
            var hasMount = false;
            if (!character.GetBio().Contains(Context.User.Id.ToString()))
            {
                await ReplyAsync(Properties.Resources.LodestoneDiscordIdNotFoundError);
                return;
            }
            if (character.GetAchievements().Any(achievement => achievement.ID == 2229))
            {
                Log.Information("Added role " + arsenalMaster.Name);
                await member.AddRoleAsync(arsenalMaster);
                await ReplyAsync(Properties.Resources.LodestoneBAAchievementSuccess);
                hasAchievement = true;
            }
            if (character.GetMiMo().Any(mimo => mimo.Name == "Demi-Ozma"))
            {
                Log.Information("Added role {Role} to {DiscordName}.", cleared.Name, Context.User.ToString());
                await member.AddRoleAsync(cleared);
                await ReplyAsync(Properties.Resources.LodestoneBAMountSuccess);
                hasMount = true;
            }

            if (!hasAchievement && !hasMount)
                await ReplyAsync(Properties.Resources.LodestoneMountAchievementNotFoundError);

            if (user == null)
            {
                await Db.AddUser(new DiscordXIVUser
                {
                    DiscordId = Context.User.Id,
                    LodestoneId = ulong.Parse(args[0]),
                    Avatar = character.XivapiResponse["Character"]["Avatar"].ToObject<string>(),
                    Name = character.XivapiResponse["Character"]["Name"].ToObject<string>(),
                    World = character.XivapiResponse["Character"]["Server"].ToObject<string>(),
                });
            }
        }

        // If they've registered, this adds them to the Member group.
        [Command("agree")]
        [RequireUserInDatabase]
        public async Task AgreeAsync()
        {
            var guildConfig = Db.Guilds.Single(g => g.Id == Context.Guild.Id);
            if (guildConfig.WelcomeChannel != Context.Channel.Id) return;
            var user = Context.Guild.GetUser(Context.User.Id);
            var memberRole = Context.Guild.GetRole(ulong.Parse(guildConfig.Roles["Member"]));
            await user.AddRoleAsync(memberRole);
            await Context.Message.DeleteAsync();
            Log.Information("Added {DiscordName} to {Role}.", Context.User.ToString(), memberRole.Name);
        }

        // Check who this user is.
        [Command("whoami", RunMode = RunMode.Async)]
        public async Task WhoAmIAsync()
        {
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
        public async Task WhoIsAsync(IUser member)
        {
            if (member == null)
            {
                await ReplyAsync(Properties.Resources.MentionNotProvidedError);
                return;
            }

            DiscordXIVUser found;
            try
            {
                found = Db.Users
                    .Single(user => user.DiscordId == member.Id);
            }
            catch (InvalidOperationException)
            {
                await ReplyAsync(Properties.Resources.UserNotInDatabaseError);
                return;
            }

            Embed responseEmbed = new EmbedBuilder()
                .WithTitle($"({found.World}) {found.Name}")
                .WithUrl($"https://na.finalfantasyxiv.com/lodestone/character/{found.LodestoneId}/")
                .WithColor(Color.Blue)
                .WithThumbnailUrl(found.Avatar)
                .Build();

            await ReplyAsync(embed: responseEmbed);
            Log.Information("Successfully responded to whoami.");
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