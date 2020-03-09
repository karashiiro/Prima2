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

        private readonly int messageDeleteDelay = 10000;

        // Declare yourself as a character.
        [Command("iam", RunMode = RunMode.Async)]
        [Alias("i am")]
        public async Task IAmAsync(params string[] parameters) // Sure are, huh
        {
            DiscordGuildConfiguration guildConfig = Db.Guilds.Single(g => g.Id == Context.Guild.Id);
            char prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            if (parameters.Length != 3)
            {
                IUserMessage reply = await ReplyAsync($"{Context.User.Mention}, please enter that command in the format `{prefix}iam World Name Surname`.");
                await Task.Delay(messageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            (new Task(async () => {
                await Task.Delay(messageDeleteDelay);
                try
                {
                    await Context.Message.DeleteAsync();
                }
                catch (HttpException) {} // Message was already deleted.
            })).Start();
            string world = parameters[0].ToLower();
            string name = parameters[1] + " " + parameters[2];
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

            SocketGuild guild = Context.Guild ?? Context.User.MutualGuilds.First();
            SocketGuildUser member = guild.GetUser(Context.User.Id);
            SocketRole cleared = guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared"]));

            using IDisposable typing = Context.Channel.EnterTypingState();

            DiscordXIVUser foundCharacter;
            try
            {
                foundCharacter = await XIVAPI.GetDiscordXIVUser(world, name, guildConfig.MinimumLevel);
            }
            catch (XIVAPICharacterNotFoundException)
            {
                IUserMessage reply = await ReplyAsync($"{Context.User.Mention}, no character matching that name and world was found. Are you sure you typed your world name correctly?");
                await Task.Delay(messageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            catch (XIVAPINotMatchingFilterException)
            {
                IUserMessage reply = await ReplyAsync($"This is a security notice. {Context.User.Mention}, that character does not have any combat jobs at Level {guildConfig.MinimumLevel}.");
                await Task.Delay(messageDeleteDelay);
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
            DiscordXIVUser user = foundCharacter;
            foundCharacter.DiscordId = Context.User.Id;
            await Db.AddUser(user);

            // We use the user-provided parameter because the Lodestone format includes the data center.
            string outputName = $"({world}) {foundCharacter.Name}";
            Embed responseEmbed = new EmbedBuilder()
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
            IUserMessage finalReply = await ReplyAsync(embed: responseEmbed);
            await Task.Delay(messageDeleteDelay);
            await finalReply.DeleteAsync();
        }

        // Set someone else's character.
        [Command("theyare", RunMode = RunMode.Async)]
        [RequireUserPermission(GuildPermission.KickMembers)]
        public async Task TheyAreAsync(SocketUser userMention, params string[] parameters)
        {
            DiscordGuildConfiguration guildConfig = Db.Guilds.Single(g => g.Id == Context.Guild.Id);
            char prefix = guildConfig.Prefix == ' ' ? Db.Config.Prefix : guildConfig.Prefix;

            if (userMention == null || parameters.Length != 3)
            {
                IUserMessage reply = await ReplyAsync($"{Context.User.Mention}, please enter that command in the format `{prefix}iam Mention World Name Surname`.");
                await Task.Delay(messageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            (new Task(async () => {
                await Task.Delay(messageDeleteDelay);
                try
                {
                    await Context.Message.DeleteAsync();
                }
                catch (HttpException) {} // Message was already deleted.
            })).Start();
            string world = parameters[0].ToLower();
            string name = parameters[1] + " " + parameters[2];
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

            SocketGuild guild = Context.Guild ?? userMention.MutualGuilds.First();
            SocketGuildUser member = guild.GetUser(userMention.Id);
            if (member == null)
            {
                guild = userMention.MutualGuilds.First();
                member = guild.GetUser(userMention.Id);
            }

            // Fetch the character.
            using IDisposable typing = Context.Channel.EnterTypingState();
            
            DiscordXIVUser foundCharacter;
            try
            {
                foundCharacter = await XIVAPI.GetDiscordXIVUser(world, name, 0);
            }
            catch (XIVAPICharacterNotFoundException)
            {
                IUserMessage reply = await ReplyAsync($"{Context.User.Mention}, no character matching that name and world was found. Are you sure you typed your world name correctly?");
                await Task.Delay(messageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }

            // Add the user and character to the database.
            DiscordXIVUser user = foundCharacter;
            foundCharacter.DiscordId = userMention.Id;
            await Db.AddUser(user);

            // We use the user-provided parameter because the Lodestone format includes the data center.
            string outputName = $"({world}) {foundCharacter.Name}";
            Embed responseEmbed = new EmbedBuilder()
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
            IUserMessage finalReply = await ReplyAsync(embed: responseEmbed);
            await Task.Delay(messageDeleteDelay);
            await finalReply.DeleteAsync();
        }

        // Verify BA clear status.
        [Command("verify", RunMode = RunMode.Async)]
        [RequireUserInDatabase]
        public async Task VerifyAsync()
        {
            DiscordGuildConfiguration guildConfig = Db.Guilds.Single(g => g.Id == Context.Guild.Id);
            char prefix = guildConfig.Prefix == '\u0000' ? Db.Config.Prefix : guildConfig.Prefix;

            SocketGuild guild = Context.Guild ?? Context.User.MutualGuilds.First();
            SocketGuildUser member = guild.GetUser(Context.User.Id);
            SocketRole arsenalMaster = guild.GetRole(ulong.Parse(guildConfig.Roles["Arsenal Master"]));
            SocketRole cleared = guild.GetRole(ulong.Parse(guildConfig.Roles["Cleared"]));

            if (member.Roles.Contains(arsenalMaster))
            {
                await ReplyAsync(Properties.Resources.MemberAlreadyHasRoleError);
                return;
            }
            using IDisposable typing = Context.Channel.EnterTypingState();
            DiscordXIVUser user = Db.Users
                .Single(user => user.DiscordId == Context.User.Id);
            Character character = await XIVAPI.GetCharacter(user.LodestoneId);
            bool hasAchievement = false;
            bool hasMount = false;
            if (!character.GetBio().Contains(Context.User.Id.ToString()))
            {
                await ReplyAsync(Properties.Resources.LodestoneDiscordIdNotFoundError);
                return;
            }
            foreach (AchievementListEntry achievement in character.GetAchievements())
            {
                if (achievement.ID == 2229)
                {
                    Log.Information("Added role " + arsenalMaster.Name);
                    await member.AddRoleAsync(arsenalMaster);
                    await ReplyAsync(Properties.Resources.LodestoneBAAchievementSuccess);
                    hasAchievement = true;
                    break;
                }
            }
            foreach (MinionMount mimo in character.GetMiMo())
            {
                if (mimo.Name == "Demi-Ozma")
                {
                    Log.Information("Added role {Role} to {DiscordName}.", cleared.Name, Context.User.ToString());
                    await member.AddRoleAsync(cleared);
                    await ReplyAsync(Properties.Resources.LodestoneBAMountSuccess);
                    hasMount = true;
                    break;
                }
            }

            if (!hasAchievement && !hasMount)
                await ReplyAsync(Properties.Resources.LodestoneMountAchievementNotFoundError);
        }

        // If they've registered, this adds them to the Member group.
        [Command("agree")]
        [RequireUserInDatabase]
        public async Task AgreeAsync()
        {
            DiscordGuildConfiguration guildConfig = Db.Guilds.Single(g => g.Id == Context.Guild.Id);
            if (guildConfig.WelcomeChannel != Context.Channel.Id) return;
            SocketGuildUser user = Context.Guild.GetUser(Context.User.Id);
            SocketRole memberRole = Context.Guild.GetRole(ulong.Parse(guildConfig.Roles["Member"]));
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

            Embed responseEmbed = new EmbedBuilder()
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
        public async Task WhoIsAsync(IUser member) // Who knows?
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