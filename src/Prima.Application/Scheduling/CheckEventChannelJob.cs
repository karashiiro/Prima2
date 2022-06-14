using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Prima.Services;
using Quartz;

namespace Prima.Application.Scheduling;

public abstract class CheckEventChannelJob : IJob
{
    protected readonly ILogger Logger;
    protected readonly DiscordSocketClient Client;
    protected readonly IDbService Db;

    protected SocketGuildUser? HostUser;
    protected IMessage? EmbedMessage;
    protected IEmbed? Embed;

    protected CheckEventChannelJob(ILogger logger, DiscordSocketClient client, IDbService db)
    {
        Logger = logger;
        Client = client;
        Db = db;
    }
    
    public async Task Execute(IJobExecutionContext ctx)
    {
        Logger.LogInformation("Executing event channel check");
        try
        {
            var guildId = await GetGuildId();
            var channelId = await GetChannelId();
            var guild = Client.GetGuild(guildId);
            await CheckRuns(guild, channelId, 30, OnMatch);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to execute event channel check");
        }
    }

    protected abstract Task<ulong> GetGuildId();
    
    protected abstract Task<ulong> GetChannelId();

    protected abstract Task OnMatch();
    
    protected async Task NotifyHost(string message)
    {
        if (HostUser == null)
        {
            Logger.LogWarning("Host user is null; cannot send notification");
            return;
        }
        
        try
        {
            await HostUser.SendMessageAsync(message);
        }
        catch (HttpException e) when (e.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            Logger.LogWarning("Can't send direct message to user {User}", HostUser.Username + '#' + HostUser.Discriminator);
        }
    }
    
    protected async Task NotifyMembers(string message)
    {
        Logger.LogInformation("Notifying {Count} reactors...", EmbedMessage?.Reactions.Count ?? -1);
        
        if (Embed == null)
        {
            Logger.LogWarning("Embed is null; cannot send notification");
            return;
        }

        if (!Embed.Footer.HasValue) return;
        var eventId = ulong.Parse(Embed.Footer.Value.Text);

        await foreach (var userTask in GetRunReactors(eventId))
        {
            var user = await userTask;

            if (user.IsBot) continue;

            try
            {
                await user.SendMessageAsync(message);
            }
            catch (HttpException e) when (e.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
            {
                Logger.LogWarning("Can't send direct message to user {User}", user.Username + '#' + user.Discriminator);
            }
        }

        await Db.RemoveAllEventReactions(eventId);
    }
    
    private async Task CheckRuns(SocketGuild guild, ulong channelId, int minutesBefore, Func<Task> onMatch)
    {
        var channel = guild.GetTextChannel(channelId);
        if (channel == null)
        {
            await Task.Delay(3000);
            return;
        }

        Logger.LogInformation("Checking runs...");

        await foreach (var page in channel.GetMessagesAsync())
        {
            foreach (var message in page)
            {
                var embed = message.Embeds.FirstOrDefault(e => e.Type == EmbedType.Rich);

                var nullableTimestamp = embed?.Timestamp;
                if (!nullableTimestamp.HasValue) continue;

                var timestamp = nullableTimestamp.Value;

                // Remove expired posts
                if (timestamp.AddMinutes(60) < DateTimeOffset.Now)
                {
                    await message.DeleteAsync();
                    continue;
                }

                // Hacky solution to avoid rare situations where people get spammed with notifications
                if (timestamp.AddMinutes(-22) < DateTimeOffset.Now)
                {
                    continue;
                }

                Logger.LogInformation("{Username} - ETA {TimeUntil} hrs.", embed?.Author?.Name ?? "", (timestamp - DateTimeOffset.Now).TotalHours);

                // ReSharper disable once InvertIf
                if (timestamp.AddMinutes(-minutesBefore) <= DateTimeOffset.Now && embed?.Author.HasValue == true)
                {
                    Logger.LogInformation("Run matched!");

                    var host = guild.Users.FirstOrDefault(u => u.ToString() == embed.Author.Value.Name)
                               ?? guild.Users.FirstOrDefault(u => u.ToString() == embed.Author.Value.Name);

                    HostUser = host;
                    EmbedMessage = message;
                    Embed = embed;
                    
                    try
                    {
                        await onMatch();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "error: uncaught exception in onMatch");
                    }
                }
            }
        }
    }
    
    private IAsyncEnumerable<ValueTask<IUser>> GetRunReactors(ulong eventId)
    {
        return Db.EventReactions
            .Where(er => er.EventId == eventId)
            .Select(er => Client.GetUserAsync(er.UserId));
    }
}