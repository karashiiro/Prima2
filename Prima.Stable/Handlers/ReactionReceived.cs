using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Prima.Models;
using Prima.Services;
using Prima.Stable.Resources;
using Prima.Stable.Services;
using Serilog;

namespace Prima.Stable.Handlers
{
    public static class ReactionReceived
    {
        private const ulong BozjaRole = 588913532410527754;
        private const ulong EurekaRole = 588913087818498070;
        private const ulong DiademRole = 588913444712087564;

        public static Task HandlerAdd(IDbService db, CharacterLookup lodestone, Cacheable<IUserMessage, ulong> message, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            Task.Run(() => HandlerAddAsync(db, lodestone, message, ichannel, reaction));
            return Task.CompletedTask;
        }

        private static async Task HandlerAddAsync(IDbService db, CharacterLookup lodestone, Cacheable<IUserMessage, ulong> message, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            if (ichannel is SocketGuildChannel channel)
            {
                var guild = channel.Guild;
                var member = guild.GetUser(reaction.UserId);
                var disConfig = db.Guilds.FirstOrDefault(g => g.Id == guild.Id);
                if (disConfig == null)
                {
                    return;
                }
                if ((ichannel.Id == 551584585432039434 || ichannel.Id == 590757405927669769 || ichannel.Id == 765748243367591936) && reaction.Emote is Emote emote && disConfig.RoleEmotes.TryGetValue(emote.Id.ToString(), out var roleIdString))
                {
                    var roleId = ulong.Parse(roleIdString);
                    var role = member.Guild.GetRole(roleId);
                    var dbEntry = db.Users.FirstOrDefault(u => u.DiscordId == reaction.UserId);

                    if (roleId == BozjaRole || roleId == EurekaRole || roleId == DiademRole)
                    {
                        if (dbEntry == null)
                        {
                            await member.SendMessageAsync("You are not currently registered in the system (potential data loss).\n" +
                                                          "Please register again in `#welcome` before adding one of these roles.");
                            Log.Information("User {User} tried to get a content role and is not registered.");
                            return;
                        }

                        if (!Worlds.List.Contains(dbEntry.World))
                        {
                            await member.SendMessageAsync("Off-DC characters may not access our organization tools.");
                            return;
                        }

                        var data = await lodestone.GetCharacter(ulong.Parse(dbEntry.LodestoneId));
                        var highestCombatLevel = 0;
                        foreach (var classJob in data["ClassJobs"].ToObject<CharacterLookup.ClassJob[]>())
                        {
                            if (classJob.JobID >= 8 && classJob.JobID <= 18) continue;
                            if (classJob.Level > highestCombatLevel)
                            {
                                highestCombatLevel = classJob.Level;
                            }
                        }

                        if (roleId == BozjaRole && highestCombatLevel < 80)
                        {
                            await member.SendMessageAsync("You must be at least level 80 to access that category.");
                            return;
                        }

                        if (roleId == EurekaRole && highestCombatLevel < 70)
                        {
                            await member.SendMessageAsync("You must be at least level 70 to access that category.");
                            return;
                        }

                        // 60 is already the minimum requirement to register.
                    }

                    await member.AddRoleAsync(role);
                    Log.Information("Role {Role} was added to {DiscordUser}", role.Name, member.ToString());
                }
                else if (guild.Id == 550702475112480769 && (ichannel.Id == 552643167808258060 || ichannel.Id == 768886934084648960) && reaction.Emote.Name == "✅")
                {
                    await member.SendMessageAsync($"You have begun the verification process. Your **Discord account ID** is `{member.Id}`.\n"
                                                  + "Please add this somewhere in your FFXIV Lodestone Character Profile.\n"
                                                  + "You can edit your account description here: https://na.finalfantasyxiv.com/lodestone/my/setting/profile/\n\n"
                                                  + $"After you have put your Discord account ID in your Lodestone profile, please use `{db.Config.Prefix}verify` to get your clear role.\n"
                                                  + "The Lodestone may not immediately update following updates to your achievements, so please wait a few hours and try again if this is the case.");
                }
            }
        }

        public static Task HandlerRemove(IDbService db, Cacheable<IUserMessage, ulong> message, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            Task.Run(() => HandlerRemoveAsync(db, message, ichannel, reaction));
            return Task.CompletedTask;
        }

        private static async Task HandlerRemoveAsync(IDbService db, Cacheable<IUserMessage, ulong> message, ISocketMessageChannel ichannel, SocketReaction reaction)
        {
            if (ichannel is SocketGuildChannel channel)
            {
                var guild = channel.Guild;
                var member = guild.GetUser(reaction.UserId);
                DiscordGuildConfiguration disConfig;
                try
                {
                    disConfig = db.Guilds.Single(g => g.Id == guild.Id);
                }
                catch (InvalidOperationException)
                {
                    return;
                }
                if ((ichannel.Id == 551584585432039434 || ichannel.Id == 590757405927669769 || ichannel.Id == 765748243367591936) && reaction.Emote is Emote emote && disConfig.RoleEmotes.TryGetValue(emote.Id.ToString(), out var roleIdString))
                {
                    var roleId = ulong.Parse(roleIdString);
                    var role = member.Guild.GetRole(roleId);
                    await member.RemoveRoleAsync(role);
                    Log.Information("Role {Role} was removed from {DiscordUser}", role.Name, member.ToString());
                }
            }
        }
    }
}