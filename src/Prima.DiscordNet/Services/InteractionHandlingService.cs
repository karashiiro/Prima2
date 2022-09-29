using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Prima.DiscordNet.Services
{
    public class InteractionHandlingService
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _handler;
        private readonly ILogger<InteractionHandlingService> _logger;
        private readonly IServiceProvider _services;

        public InteractionHandlingService(DiscordSocketClient client, InteractionService handler,
            ILogger<InteractionHandlingService> logger, IServiceProvider services)
        {
            _client = client;
            _handler = handler;
            _logger = logger;
            _services = services;
        }

        public async Task InitializeAsync(Assembly assembly = null)
        {
            _client.InteractionCreated += HandleInteraction;
            await _handler.AddModulesAsync(assembly ?? Assembly.GetEntryAssembly(), _services);
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules.
                var context = new SocketInteractionContext(_client, interaction);

                // Execute the incoming command.
                var result = await _handler.ExecuteCommandAsync(context, _services);

                if (result.IsSuccess)
                {
                    return;
                }

                switch (result.Error)
                {
                    case InteractionCommandError.UnmetPrecondition:
                        _logger.LogWarning("Unmet precondition: {ErrorReason}", result.ErrorReason);
                        break;
                    case InteractionCommandError.UnknownCommand:
                        _logger.LogWarning("Unknown command: {ErrorReason}", result.ErrorReason);
                        break;
                    case InteractionCommandError.BadArgs:
                        _logger.LogWarning("Invalid number of arguments: {ErrorReason}", result.ErrorReason);
                        break;
                    case InteractionCommandError.Exception:
                        _logger.LogError("Interaction error: {ErrorReason}", result.ErrorReason);
                        break;
                    case InteractionCommandError.Unsuccessful:
                        _logger.LogWarning("Command could not be executed: {ErrorReason}", result.ErrorReason);
                        break;
                    case InteractionCommandError.ConvertFailed:
                        _logger.LogWarning("Failed to convert object: {ErrorReason}", result.ErrorReason);
                        break;
                    case InteractionCommandError.ParseFailed:
                        _logger.LogWarning("Failed to parse object: {ErrorReason}", result.ErrorReason);
                        break;
                    case null:
                        _logger.LogWarning("Null error type in unsuccessful result: {ErrorReason}", result.ErrorReason);
                        break;
                    default:
                        _logger.LogWarning("Unknown error type from unsuccessful result: {ErrorReason}",
                            result.ErrorReason);
                        break;
                }

                await context.Interaction.RespondAsync("Failed to process interaction.");
            }
            catch
            {
                // If Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (interaction.Type is InteractionType.ApplicationCommand)
                    await interaction.GetOriginalResponseAsync()
                        .ContinueWith(async msg => await msg.Result.DeleteAsync());
            }
        }
    }
}