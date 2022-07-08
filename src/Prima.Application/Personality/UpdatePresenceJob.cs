using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Prima.Application.Personality;

public class UpdatePresenceJob : IJob
{
    private readonly ILogger<UpdatePresenceJob> _logger;
    private readonly DiscordSocketClient _client;

    public UpdatePresenceJob(ILogger<UpdatePresenceJob> logger, DiscordSocketClient client)
    {
        _logger = logger;
        _client = client;
    }
    
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var (name, activityType) = Presences.List[new Random().Next(0, Presences.List.Length)];
            await _client.SetGameAsync(name, null, activityType);
            _logger.LogInformation("Updated presence to {Activity}", name);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to execute presence update");
        }
    }
}