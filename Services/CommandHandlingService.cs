using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Prima.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;

        public CommandHandlingService(IServiceProvider services)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            // Hook CommandExecuted to handle post-command-execution logic.
            _commands.CommandExecuted += CommandExecutedAsync;
            // Hook MessageReceived so we can process each message to see
            // if it qualifies as a command.
            _discord.MessageReceived += MessageReceivedAsync;
        }

        public async Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
        }

        public async Task ReinitalizeAsync()
        {
            await _commands.RemoveModuleAsync<ModuleBase>();
            await InitializeAsync();
        }

        public async Task MessageReceivedAsync(SocketMessage rawMessage)
        {
            // Ignore system messages, or messages from other bots
            if (!(rawMessage is SocketUserMessage message)) return;
            if (message.Source != MessageSource.User) return;

            var context = new SocketCommandContext(_discord, message);

            var argPos = 0;
            char prefix = _services.GetRequiredService<ConfigurationService>().GetSection("Prefix").Value[0];
            // Check if the guild has a custom command prefix specified.
            if (_services.GetRequiredService<ConfigurationService>().GetSection($"{context.Guild.Id}").GetSection("Prefix").Exists())
            {
                prefix = _services.GetRequiredService<ConfigurationService>().GetSection($"{context.Guild.Id}").GetSection("Prefix").Value[0];
            }
            if (!message.HasCharPrefix(prefix, ref argPos)) return;

#pragma warning disable CA1062 // Validate arguments of public methods
            Log.Information("({DiscordID}) {DiscordName}: {MessageContent}", rawMessage.Author.Id, rawMessage.Author.Username + "#" + rawMessage.Author.Discriminator, rawMessage.Content);
#pragma warning restore CA1062 // Validate arguments of public methods

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
