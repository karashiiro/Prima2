using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Prima.Services;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Prima.DiscordNet.Services
{
    public class CommandHandlingService
    {
        private readonly ILogger<CommandHandlingService> _logger;
        private readonly CommandService _commands;
        private readonly IDbService _db;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _logger = services.GetRequiredService<ILogger<CommandHandlingService>>();
            _commands = services.GetRequiredService<CommandService>();
            _db = services.GetRequiredService<IDbService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;
        }

        public Task InitializeAsync(Assembly assembly = null)
        {
            _commands.CommandExecuted += CommandExecutedAsync;
            _discord.MessageReceived += MessageReceivedAsync;

            return _commands.AddModulesAsync(assembly ?? Assembly.GetEntryAssembly(), _services);
        }

        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (rawMessage is not SocketUserMessage { Source: MessageSource.User } message) return;

            var context = new SocketCommandContext(_discord, message);

            var argPos = 0;
            var prefix = _db.Config.Prefix;
            try
            {
                if (rawMessage.Channel is SocketGuildChannel channel)
                {
                    var guildPrefix = _db.Guilds.Single(g => g.Id == channel.Guild.Id).Prefix;
                    prefix = guildPrefix == ' ' ? prefix : guildPrefix;
                }
            }
            catch (InvalidOperationException)
            {
                _logger.LogWarning(
                    "Message received in {GuildName}, but no configuration exists! Message: {MessageContent}",
                    ((SocketGuildChannel)rawMessage.Channel).Name, rawMessage.Content);
            }

            if (!message.HasCharPrefix(prefix, ref argPos))
            {
                // Hacky bit to get this working with fewer headaches upfront for new users
                if (rawMessage.Channel is SocketGuildChannel { Name: "welcome" })
                {
                    if (message.Content.StartsWith("i") || message.Content.StartsWith("agree"))
                    {
                        argPos = 0;
                    }
                    else if (message.Content.StartsWith("-"))
                    {
                        argPos = 1;
                    }
                }
                else return;
            }

            _logger.LogInformation("({DiscordID}) {DiscordName}: {MessageContent}", rawMessage.Author.Id,
                rawMessage.Author.Username + '#' + rawMessage.Author.Discriminator, rawMessage.Content);

            await _commands.ExecuteAsync(context, argPos, _services);
        }

        private Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified)
                return Task.CompletedTask;

            if (result is { IsSuccess: true })
                return Task.CompletedTask;

            _logger.LogError("Error: {ErrorMessage}", result);

            return Task.CompletedTask;
        }
    }
}