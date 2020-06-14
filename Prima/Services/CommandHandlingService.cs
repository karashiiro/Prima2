using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Prima.Attributes;

namespace Prima.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DbService _db;
        private readonly IDictionary<string, long> _commandTimeouts; // Key: command name, value: use time
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _db = services.GetRequiredService<DbService>();
            _commandTimeouts = new ConcurrentDictionary<string, long>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Hook CommandExecuted to handle post-command-execution logic.
            _commands.CommandExecuted += CommandExecutedAsync;
            // Hook MessageReceived so we can process each message to see
            // if it qualifies as a command.
            _discord.MessageReceived += MessageReceivedAsync;
        }

        public Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>.
            return _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task ReinitalizeAsync()
        {
            await _commands.RemoveModuleAsync<ModuleBase>();
            await InitializeAsync();
        }

        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "<Pending>")]
        public async Task MessageReceivedAsync(SocketMessage rawMessage)
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
                if (rawMessage.Channel is SocketGuildChannel && message.Content.StartsWith("i") || message.Content.StartsWith("agree"))
                    argPos = 0;
                else
                    return;
            }

            Log.Information("({DiscordID}) {DiscordName}: {MessageContent}", rawMessage.Author.Id, rawMessage.Author.Username + "#" + rawMessage.Author.Discriminator, rawMessage.Content);

            // Check timeout info
            var endOfCommandNameIndex = context.Message.Content.IndexOf(' ');
            if (endOfCommandNameIndex == -1) endOfCommandNameIndex = context.Message.Content.Length - 1;
            var commandName = context.Message.Content.Substring(argPos, endOfCommandNameIndex);
            var commands = await _commands.GetExecutableCommandsAsync(context, _services);
            var command = commands.FirstOrDefault(c => c.Name == commandName);
            if (command != null)
            {
                if (command.Attributes.FirstOrDefault(a => a is RateLimitAttribute) is RateLimitAttribute rateLimitInfo)
                {
                    // TODO handle non-global rate limits
                    if (!_commandTimeouts.ContainsKey(commandName) || _commandTimeouts[commandName] < DateTimeOffset.Now.ToUnixTimeSeconds())
                    {
                        _commandTimeouts.Remove(commandName);
                        _commandTimeouts.Add(commandName, DateTimeOffset.Now.ToUnixTimeSeconds() + rateLimitInfo.TimeSeconds);
                    }
                    else
                    {
                        return;
                    }
                }
            }

            // Perform the execution of the command. In this method,
            // the command service will perform precondition and parsing check
            // then execute the command if one is matched.
            await _commands.ExecuteAsync(context, argPos, _services);
            // Note that normally a result will be returned by this format, but here
            // we will handle the result in CommandExecutedAsync,
        }

        public static async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            if (!command.IsSpecified)
                return;

            if (result != null && result.IsSuccess)
                return;

            Log.Error($"error: {result}");
            await Task.Delay(1);
        }
    }
}
