using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Prima.Services;
using Serilog;
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Prima.DiscordNet.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly IDbService _db;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _db = services.GetRequiredService<IDbService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            _commands.CommandExecuted += CommandExecutedAsync;
            _discord.MessageReceived += MessageReceivedAsync;
        }

        public Task InitializeAsync(Assembly assembly = null)
        {
            return _commands.AddModulesAsync(assembly ?? Assembly.GetEntryAssembly(), _services);
        }

        private async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

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
                Log.Warning("Message received in {GuildName}, but no configuration exists! Message: {MessageContent}", ((SocketGuildChannel)rawMessage.Channel).Name, rawMessage.Content);
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

            Log.Information("({DiscordID}) {DiscordName}: {MessageContent}", rawMessage.Author.Id, rawMessage.Author.Username + "#" + rawMessage.Author.Discriminator, rawMessage.Content);

            await _commands.ExecuteAsync(context, argPos, _services);
        }

        private static Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified)
                return Task.CompletedTask;

            if (result != null && result.IsSuccess)
                return Task.CompletedTask;

            Log.Error("Error: {ErrorMessage}", result);

            return Task.CompletedTask;
        }
    }
}
