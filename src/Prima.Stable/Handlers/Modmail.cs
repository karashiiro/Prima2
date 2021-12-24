using Discord;
using Discord.WebSocket;
using Serilog;
using System.Threading.Tasks;

namespace Prima.Stable.Handlers
{
    public static class Modmail
    {
        public static async Task Handler(SocketMessageComponent component)
        {
            if (component.Data.CustomId != "cem-modmail") return;
            if (component.Message.Channel is not ITextChannel channel) return;

            var member = await channel.Guild.GetUserAsync(component.User.Id);
            var threadName = string.IsNullOrEmpty(member.Nickname) ? member.ToString() : member.Nickname;
            var thread = await channel.CreateThreadAsync(threadName, ThreadType.PrivateThread);
            await thread.AddUserAsync(member);

            Log.Information("Created thread \"{ThreadName}\" for user \"{User}\".", threadName, member.ToString());
        }
    }
}