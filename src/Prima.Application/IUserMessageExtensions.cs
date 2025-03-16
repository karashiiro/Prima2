using Discord;
using Microsoft.Extensions.Logging;

namespace Prima.Application;

public static class IUserMessageExtensions
{
    public static async Task CrosspostSafeAsync(
        this IUserMessage message,
        ILogger logger,
        RequestOptions? options = null)
    {
        try
        {
            await message.CrosspostAsync(options);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to crosspost message");
        }
    }
}