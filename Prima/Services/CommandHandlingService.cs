using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Prima.Attributes;

namespace Prima.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly IDbService _db;
        private readonly IDictionary<string, long> _commandTimeouts; // Key: command name, value: use time
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _db = services.GetRequiredService<IDbService>();
            _commandTimeouts = new ConcurrentDictionary<string, long>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;
            
            _commands.CommandExecuted += CommandExecutedAsync;
            _discord.MessageReceived += MessageReceivedAsync;
        }

        public Task InitializeAsync()
        {
            return _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
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
                if (rawMessage.Channel is SocketGuildChannel channel && channel.Name == "welcome")
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

            // Attribute stuff
            var endOfCommandNameIndex = context.Message.Content.IndexOf(' ');
            if (endOfCommandNameIndex == -1) endOfCommandNameIndex = context.Message.Content.Length - 1;
            var commandName = context.Message.Content.Substring(argPos, endOfCommandNameIndex);
            var commands = await _commands.GetExecutableCommandsAsync(context, _services);
            var command = commands.FirstOrDefault(c => c.Name == commandName);
            var rateLimitInfo = (RateLimitAttribute) command?.Attributes.FirstOrDefault(attr => attr is RateLimitAttribute);
            if (rateLimitInfo != null)
            {
                // TODO handle non-global rate limits
                if (_commandTimeouts.ContainsKey(commandName) && _commandTimeouts[commandName] >= DateTimeOffset.Now.ToUnixTimeSeconds())
                    return;

                _commandTimeouts.Remove(commandName);
                _commandTimeouts.Add(commandName, DateTimeOffset.Now.ToUnixTimeSeconds() + rateLimitInfo.TimeSeconds);
            }
            
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        private static Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified)
                return Task.CompletedTask;

            if (result != null && result.IsSuccess)
                return Task.CompletedTask;

            Log.Error($"error: {result}");

            return Task.CompletedTask;
        }
    }
}
