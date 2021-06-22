using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.EventArgs;
using Prima.Services;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Prima.Scheduler.Services
{
    public class CommandService
    {
        private readonly CommandsNextExtension _commands;
        private readonly IDbService _db;

        public CommandService(DiscordClient discord, IDbService db)
        {
            _db = db;
            _commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                UseDefaultCommandHandler = false,
            });
        }

        public Task Handler(DiscordClient client, MessageCreateEventArgs createEvent)
        {
            var message = createEvent.Message;

            var prefix = _db.Config.Prefix;
            if (message.Channel.Guild != null)
            {
                try
                {
                    // Get the guild's custom prefix, if it exists.
                    var guildPrefix = _db.Guilds.Single(g => g.Id == message.Channel.Guild.Id).Prefix;
                    prefix = guildPrefix == ' ' ? prefix : guildPrefix;
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to fetch guild prefix!");
                }
            }

            if (!message.Content.StartsWith(prefix))
            {
                return Task.CompletedTask;
            }

            // The command string after the prefix
            var commandString = message.Content[1..];

            var command = _commands.FindCommand(commandString, out var args);
            if (command == null)
            {
                return Task.CompletedTask;
            }

            var ctx = _commands.CreateContext(message, prefix.ToString(), command, args);
            Task.Run(async () => await _commands.ExecuteCommandAsync(ctx));

            return Task.CompletedTask;
        }

        public void UseCommandModule<TCommandModule>() where TCommandModule : BaseCommandModule
        {
            _commands.RegisterCommands<TCommandModule>();
        }
    }
}