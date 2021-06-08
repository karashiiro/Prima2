using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.Services;
using Serilog;

namespace Prima.Stable.Handlers
{
    public class MemberJoin
    {
        public static async Task Handler(IDbService db, SocketGuildUser member)
        {
            Log.Information("User {UserName} joined {GuildName}", member, member.Guild.Name);

            var guildConfig = db.Guilds.FirstOrDefault(g => g.Id == member.Guild.Id);
            if (guildConfig == null) return;

            var reportChannel = member.Guild.GetTextChannel(guildConfig.ReportChannel);
            foreach (var regex in guildConfig.BannedNameRegexes)
            {
                if (Regex.IsMatch(member.Username, regex))
                {
                    await member.Guild.AddBanAsync(member);
                    if (reportChannel != null)
                    {
                        await reportChannel.SendMessageAsync(embed: new EmbedBuilder()
                            .WithColor(Color.Red)
                            .WithCurrentTimestamp()
                            .WithThumbnailUrl(member.GetAvatarUrl())
                            .WithTitle($"Banned user {member}")
                            .WithDescription($"Username matched pattern:\n```\n{regex}\n```")
                            .Build());
                    }
                    Log.Information("Banned {UserName} from {GuildName} because their username matched {Regex}.", member.ToString(), member.Guild.Name, regex);
                }
            }
        }
    }
}