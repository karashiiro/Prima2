using DSharpPlus;
using Microsoft.Extensions.DependencyInjection;
using Prima.Scheduler.GoogleApis.Services;
using Prima.Scheduler.Handlers;
using Prima.Scheduler.Services;
using Prima.Services;
using Serilog;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Prima.Scheduler.Modules;

namespace Prima.Scheduler
{
    class Program
    {
        static void Main() => MainAsync().GetAwaiter().GetResult();

        private static async Task MainAsync()
        {
            // Initialize the ASP.NET service provider and freeze this Task indefinitely.
            await using var services = ConfigureServices();

            var client = services.GetRequiredService<DiscordClient>();
            var events = services.GetRequiredService<EventService>();
            var db = services.GetRequiredService<IDbService>();
            var calendar = services.GetRequiredService<CalendarApi>();

            var commands = services.GetRequiredService<CommandService>();
            client.MessageCreated += commands.Handler;

            commands.UseCommandModule<DiagnosticModule>();
            commands.UseCommandModule<HelpModule>();
            commands.UseCommandModule<NotificationBoardModule>();
            commands.UseCommandModule<SchedulingModule>();

            client.MessageUpdated += events.OnMessageEdit;
            client.MessageReactionAdded += events.OnReactionAdd;
            client.MessageReactionRemoved += events.OnReactionRemove;

            client.MessageUpdated += (_, updateEvent)
                => AnnounceEdit.Handler(client, calendar, db, updateEvent);
            client.MessageReactionAdded += (_, reactionEvent)
                => AnnounceReact.HandlerAdd(client, db, reactionEvent);

            services.GetRequiredService<RunNotiferService>().Initialize();
            services.GetRequiredService<AnnounceMonitor>().Initialize();

            await client.ConnectAsync();

            Log.Information("Prima Scheduler logged in!");

            await Task.Delay(-1);
        }

        private static ServiceProvider ConfigureServices()
        {
            var sc = new ServiceCollection()
                .AddSingleton(new DiscordClient(new DiscordConfiguration
                {
                    Token = Environment.GetEnvironmentVariable("PRIMA_BOT_TOKEN"),
                    TokenType = TokenType.Bot,
                }))
                .AddSingleton<CommandService>()
                .AddSingleton<HttpClient>()
                .AddSingleton<IDbService, DbService>()
                .AddSingleton<FFXIVSheetService>()
                .AddSingleton<ITemplateProvider, TemplateProvider>();
            sc.AddSingleton<EventService>();
            sc.AddSingleton<RunNotiferService>();
            sc.AddSingleton<SpreadsheetService>();
            sc.AddSingleton<AnnounceMonitor>();
            sc.AddSingleton<CalendarApi>();
            return sc.BuildServiceProvider();
        }
    }
}