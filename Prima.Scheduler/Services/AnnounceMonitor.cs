using Prima.Resources;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Prima.Services;

namespace Prima.Scheduler.Services
{
    public class AnnounceMonitor : IDisposable
    {
        private readonly IDbService _db;
        private readonly DiscordClient _client;

        private readonly CancellationTokenSource _tokenSource;

        public AnnounceMonitor(IDbService db, DiscordClient client)
        {
            _db = db;
            _client = client;

            _tokenSource = new CancellationTokenSource();
        }

        public void Initialize()
        {
            Task.Run(() => CheckLoop(_tokenSource.Token));
        }

        private async Task CheckLoop(CancellationToken token)
        {
            var guildConfig = _db.Guilds.FirstOrDefault(g => g.Id == SpecialGuilds.CrystalExploratoryMissions);
            if (guildConfig == null)
            {
                Log.Error("No guild configuration found for the default guild!");
                return;
            }

            while (!token.IsCancellationRequested)
            {
                var guild = await _client.GetGuildAsync(guildConfig.Id);

                var drsCheck = CheckRuns(guild, guildConfig.DelubrumScheduleOutputChannel, 30, async (host, embedMessage, embed) =>
                {
                    var success = await AssignHostRole(guild, host);
                    if (!success) return;

                    await AssignExecutorRole(guild, host);
                    await NotifyMembers(host, embedMessage, embed, token);
                    await host.SendMessageAsync(
                        "You have been given the Delubrum Host role for 4 1/2 hours!\n" +
                        "You can now use the command `~setroler @User` to give them access to the progression " +
                        "role commands `~addprogrole @User Role Name` and `~removeprogrole @User Role Name`!\n" +
                        "You can also modify multiple users at once by using `~addprogrole @User1 @User2 Role Name`.\n\n" +
                        "Available roles:\n" +
                        "▫️ Trinity Seeker Progression\n" +
                        "▫️ Queen's Guard Progression\n" +
                        "▫️ Trinity Avowed Progression\n" +
                        "▫️ Stygimoloch Lord Progression\n" +
                        "▫️ The Queen Progression");
                }, token);

                var drnCheck = CheckRuns(guild, guildConfig.DelubrumNormalScheduleOutputChannel, 30, async (host, embedMessage, embed) =>
                {
                    var success = await AssignHostRole(guild, host);
                    if (!success) return;

                    await NotifyLead(host);
                    await NotifyMembers(host, embedMessage, embed, token);
                }, token);

                var clusterCheck = CheckRuns(guild, guildConfig.BozjaClusterScheduleOutputChannel, 30, async (host, embedMessage, embed) =>
                {
                    var success = await AssignHostRole(guild, host);
                    if (!success) return;

                    await NotifyLead(host);
                    await NotifyMembers(host, embedMessage, embed, token);
                }, token);

                var castrumCheck = CheckRuns(guild, guildConfig.CastrumScheduleOutputChannel, 30, async (host, embedMessage, embed) =>
                {
                    var success = await AssignHostRole(guild, host);
                    if (!success) return;

                    await NotifyLead(host);
                    await NotifyMembers(host, embedMessage, embed, token);
                }, token);

                var zadnorCheck = CheckRuns(guild, guildConfig.ZadnorThingScheduleOutputChannel, 30, async (host, embedMessage, embed) =>
                {
                    var success = await AssignHostRole(guild, host);
                    if (!success) return;

                    await NotifyLead(host);
                    await NotifyMembers(host, embedMessage, embed, token);
                }, token);

                var socialCheck = CheckRuns(guild, guildConfig.SocialScheduleOutputChannel, 30, async (host, embedMessage, embed) =>
                {
                    var success = await AssignSocialHostRole(guild, host);
                    if (!success) return;

                    await NotifyLead(host);
                    await NotifyMembers(host, embedMessage, embed, token);
                }, token);

                await Task.WhenAll(drsCheck, drnCheck, clusterCheck, castrumCheck, zadnorCheck, socialCheck);
#if DEBUG
                await Task.Delay(3000, token);
#else
                await Task.Delay(new TimeSpan(0, 5, 0), token);
#endif
            }
        }

        private static async Task CheckRuns(DiscordGuild guild, ulong channelId, int minutesBefore, Func<DiscordMember, DiscordMessage, DiscordEmbed, Task> onMatch, CancellationToken token)
        {
            var channel = guild?.GetChannel(channelId);
            if (channel == null)
            {
                await Task.Delay(3000, token);
                return;
            }

            Log.Information("Checking runs...");

            foreach (var message in await channel.GetMessagesAsync())
            {
                var embed = message.Embeds.FirstOrDefault();

                var nullableTimestamp = embed?.Timestamp;
                if (!nullableTimestamp.HasValue) continue;

                var timestamp = nullableTimestamp.Value;

                // Remove expired posts
                if (timestamp.AddMinutes(60) < DateTimeOffset.Now)
                {
                    await message.DeleteAsync();
                    continue;
                }

                Log.Information("{Username} - ETA {TimeUntil} hrs.", embed.Author?.Name, (timestamp - DateTimeOffset.Now).TotalHours);

                // ReSharper disable once InvertIf
                if (timestamp.AddMinutes(-minutesBefore) <= DateTimeOffset.Now && embed.Author != null)
                {
                    Log.Information("Run matched!");

                    var host = guild.Members.Values.FirstOrDefault(u => u.ToString() == embed.Author.Name);
                    if (host == null)
                    {
                        host = (await guild.GetAllMembersAsync()).FirstOrDefault(u => u.ToString() == embed.Author.Name);
                    }

                    try
                    {
                        await onMatch(host, message, embed);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "error: uncaught exception in onMatch");
                    }
                }
            }
        }

        private async Task<bool> AssignSocialHostRole(DiscordGuild guild, DiscordMember host)
        {
            var socialHost = guild.GetRole(RunHostData.SocialHostRoleId);

            Log.Information("Assigning roles...");
            if (host == null || host.HasRole(socialHost)) return false;

            try
            {
                await _db.AddTimedRole(socialHost.Id, guild.Id, host.Id, DateTime.UtcNow.AddHours(4.5));
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to add host role to {User}!", socialHost?.ToString() ?? "null");
                return false;
            }
        }

        private async Task<bool> AssignHostRole(DiscordGuild guild, DiscordMember host)
        {
            var currentHost = guild.GetRole(RunHostData.RoleId);
            var runPinner = guild.GetRole(RunHostData.PinnerRoleId);

            Log.Information("Assigning roles...");
            if (host == null || host.HasRole(currentHost)) return false;

            try
            {
                await host.GrantRoleAsync(currentHost);
                await host.GrantRoleAsync(runPinner);
                await _db.AddTimedRole(currentHost.Id, guild.Id, host.Id, DateTime.UtcNow.AddHours(4.5));
                await _db.AddTimedRole(runPinner.Id, guild.Id, host.Id, DateTime.UtcNow.AddHours(4.5));
                return true;
            }
            catch (Exception e)
            {
                Log.Error(e, "Failed to add host role to {User}!", currentHost?.ToString() ?? "null");
                return false;
            }
        }

        private async Task AssignExecutorRole(DiscordGuild guild, DiscordMember host)
        {
            var executor = guild.GetRole(DelubrumProgressionRoles.Executor);
            await host.GrantRoleAsync(executor);
            await _db.AddTimedRole(executor.Id, guild.Id, host.Id, DateTime.UtcNow.AddHours(4.5));
        }

        private static async Task NotifyLead(DiscordMember host)
        {
            try
            {
                await host.SendMessageAsync("The run you scheduled is set to begin in 30 minutes!");
            }
            catch (Exception e)
            {
                Log.Warning(e, "Can't send direct message to user {User}.", host.ToString());
            }
        }

        private IAsyncEnumerable<DiscordMember> GetRunReactors(DiscordGuild guild, ulong eventId)
        {
            return _db.EventReactions
                .Where(er => er.EventId == eventId)
                .Select(er => guild.GetMemberAsync(er.UserId).GetAwaiter().GetResult());
        }

        private async Task NotifyMembers(DiscordMember host, DiscordMessage embedMessage, DiscordEmbed embed, CancellationToken token)
        {
            Log.Information("Notifying reactors...", embedMessage.Reactions.Count);

            if (embed.Footer == null) return;
            var eventId = ulong.Parse(embed.Footer.Text);

            var reactors = GetRunReactors(host.Guild, eventId);
            await foreach (var user in reactors.WithCancellation(token))
            {
                if (user == null || user.IsBot) continue;

                try
                {
                    await user.SendMessageAsync($"The run you reacted to (hosted by {host.Nickname ?? host.Username}) is beginning in 30 minutes!");
                }
                catch (Exception e)
                {
                    Log.Warning(e, "Can't send direct message to user {User}.", host.ToString());
                }
            }

            await _db.RemoveAllEventReactions(eventId);
        }

        private bool _disposed;
        public void Dispose()
        {
            if (_disposed)
                return;
            _tokenSource?.Cancel();
            _tokenSource?.Dispose();
            _disposed = true;
        }
    }
}