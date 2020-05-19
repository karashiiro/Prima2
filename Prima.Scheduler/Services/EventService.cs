using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;

namespace Prima.Scheduler.Services
{
    public class EventService
    {
        private readonly RunService _schedule;

        public EventService(RunService schedule)
        {
            _schedule = schedule;
        }

        public async Task OnMessageEdit(IMessage newMessage)
        {
            if (!(newMessage.Channel is SocketGuildChannel channel))
                return;

            var run = _schedule.GetScheduledRuns().FirstOrDefault(sr => sr.MessageId == newMessage.Id);
            if (run == null)
                return;

            run.Description = newMessage.Content.Substring("~schedule XX ".Length);

            var guild = channel.Guild;
            foreach (var guildChannel in guild.Channels)
            {
                if (guildChannel is SocketTextChannel textChannel)
                {
                    if (!(await textChannel.GetMessageAsync(run.EmbedMessageId) is IUserMessage message))
                        continue;
                    var embed = message.Embeds.FirstOrDefault()?.ToEmbedBuilder()
                        .WithDescription(run.Description)
                        .Build();
                    if (embed == null)
                        return;
                    await message.ModifyAsync(properties => { properties.Embed = embed; });
                }
            }
        }
    }
}
