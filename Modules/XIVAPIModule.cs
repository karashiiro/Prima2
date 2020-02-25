using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Prima.Attributes;
using Prima.Contexts;
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
    [ConfigurationPreset(Preset.Clerical)]
    public class XIVAPIModule : ModuleBase<SocketCommandContext>
    {
        public ConfigurationService Config { get; set; }
        public XIVAPIService XIVAPI { get; set; }

        public DiscordXIVUserContext Users { get; set; }

        private readonly int messageDeleteDelay = 10000;

        // Declare yourself as a character.
        [Command("iam", RunMode = RunMode.Async)]
        [Alias("i am")]
        public async Task IAmAsync(params string[] parameters) // Sure are, huh
        {
            if (parameters.Length != 3)
            {
                IUserMessage reply = await ReplyAsync($"{Context.User.Mention}, please enter that command in the format `{Config.GetSection("prefix").Value}iam World Name Surname`.");
                await Task.Delay(messageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            (new Task(async () => {
                await Task.Delay(messageDeleteDelay);
                await Context.Message.DeleteAsync();
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
            SocketRole cleared;
            try
            {
                cleared = guild.GetRole(ulong.Parse(Config.GetSection(guild.Id.ToString(), "Roles", "Cleared").Value));
            }
            catch (ArgumentNullException)
            {
                cleared = null;
            }

            using IDisposable typing = Context.Channel.EnterTypingState();

            DiscordXIVUser foundCharacter;
            try
            {
                foundCharacter = await XIVAPI.GetDiscordXIVUser(world, name, Config.GetByte(Context.Guild.Id.ToString(), "MinimumLevel"));
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
                IUserMessage reply = await ReplyAsync($"This is a security notice. {Context.User.Mention}, that character does not have any combat jobs at Level {Config.GetByte(Context.Guild.Id.ToString(), "MinimumLevel")}.");
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
                DiscordXIVUser user = Users.Users
                    .Single(user => user.DiscordId == Context.User.Id);

                // If they're verified and aren't reregistering the same character, return.
                if (cleared != null && member.Roles.Contains(cleared))
                {
                    if (user.LodestoneId != foundCharacter.LodestoneId)
                    {
                        var message = await ReplyAsync($"{Context.User.Mention}, you have already verified your character.");
                        await Task.Delay(5000);
                        await message.DeleteAsync();
                        return;
                    }
                }

                user.Avatar = foundCharacter.Avatar;
                user.Name = foundCharacter.Name;
                user.World = foundCharacter.World;
            }
            catch (InvalidOperationException)
            {
                DiscordXIVUser user = foundCharacter;
                foundCharacter.DiscordId = Context.User.Id;
                await Users.Users.AddAsync(user);
            }
            await Users.SaveChangesAsync();

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
            if (userMention == null || parameters.Length != 3)
            {
                IUserMessage reply = await ReplyAsync($"{Context.User.Mention}, please enter that command in the format `{Config.GetSection("prefix").Value}iam Mention World Name Surname`.");
                await Task.Delay(messageDeleteDelay);
                await reply.DeleteAsync();
                return;
            }
            (new Task(async () => {
                await Task.Delay(messageDeleteDelay);
                await Context.Message.DeleteAsync();
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
            try
            {
                DiscordXIVUser user = Users.Users
                    .Single(user => user.DiscordId == userMention.Id);

                user.LodestoneId = foundCharacter.LodestoneId;
                user.Avatar = foundCharacter.Avatar;
                user.Name = foundCharacter.Name;
                user.World = foundCharacter.World;
            }
            catch (InvalidOperationException)
            {
                DiscordXIVUser user = foundCharacter;
                foundCharacter.DiscordId = userMention.Id;
                await Users.Users.AddAsync(user);
            }
            await Users.SaveChangesAsync();

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
            SocketGuild guild = Context.Guild ?? Context.User.MutualGuilds.First();
            SocketGuildUser member = guild.GetUser(Context.User.Id);
            SocketRole arsenalMaster = guild.GetRole(ulong.Parse(Config.GetSection(guild.Id.ToString(), "Roles", "Arsenal Master").Value));
            SocketRole cleared = guild.GetRole(ulong.Parse(Config.GetSection(guild.Id.ToString(), "Roles", "Cleared").Value));

            if (member.Roles.Contains(arsenalMaster))
            {
                await ReplyAsync(Properties.Resources.MemberAlreadyHasRoleError);
                return;
            }
            using IDisposable typing = Context.Channel.EnterTypingState();
            DiscordXIVUser user = Users.Users
                .Single(user => user.DiscordId == Context.User.Id);
            Character character = await XIVAPI.GetCharacter(user.LodestoneId);
            bool hasAchievement = false;
            bool hasMount = false;
            if (!character.GetBio().Contains("" + Context.User.Id))
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
                    Log.Information("Added role " + cleared.Name);
                    await member.AddRoleAsync(cleared);
                    await ReplyAsync(Properties.Resources.LodestoneBAMountSuccess);
                    hasMount = true;
                    break;
                }
            }

            if (!hasAchievement && !hasMount)
                await ReplyAsync(Properties.Resources.LodestoneMountAchievementNotFoundError);
        }
    }
}
