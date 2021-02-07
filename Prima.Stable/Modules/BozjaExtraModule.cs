using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Prima.Attributes;
using Prima.Resources;
using Prima.Services;
using Prima.Stable.Resources;
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

        [Command("addprogrole", RunMode = RunMode.Async)]
        [Description("Adds a progression role to a user.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task AddDelubrumProgRoleAsync(IUser user, IRole role)
        {
            if (Context.Guild == null) return;
            if (!DelubrumProgressionRoles.Ids.Contains(role.Id)) return;
            if (!Context.User.HasRole(DelubrumProgressionRoles.Executor, Context)) return;

            var member = Context.Guild.GetUser(user.Id);
            if (member.HasRole(role, Context))
            {
                await ReplyAsync($"{user.Mention} already has that role!");
            }
            else
            {
                await member.AddRoleAsync(role);
                await ReplyAsync($"Role added to {user.Mention}.");
            }
        }

        [Command("removeprogrole", RunMode = RunMode.Async)]
        [Description("Removes a progression role from a user.")]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public async Task RemoveDelubrumProgRoleAsync(IUser user, IRole role)
        {
            if (Context.Guild == null) return;
            if (!DelubrumProgressionRoles.Ids.Contains(role.Id)) return;
            if (!Context.User.HasRole(DelubrumProgressionRoles.Executor, Context)) return;

            var member = Context.Guild.GetUser(user.Id);
            if (!member.HasRole(role, Context))
            {
                await ReplyAsync($"{user.Mention} already does not have that role!");
            }
            else
            {
                await member.RemoveRoleAsync(role);
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