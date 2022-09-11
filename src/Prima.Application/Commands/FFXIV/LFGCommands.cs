using Discord.Commands;
using Prima.DiscordNet.Attributes;

namespace Prima.Application.Commands.FFXIV;

[Name("FFXIV LFG")]
public class LFGCommands : ModuleBase<SocketCommandContext>
{
    [Command("whatdoineed")]
    [Description("[FFXIV] Tells you how many more people of each role you need.")]
    public Task WhatDoINeedAsync([Remainder] string args = "")
    {
        var vargs = args.Split(' ');
        if (vargs.Length != 2)
        {
            return ReplyAsync("Wrong argument count!\n" +
                              "Command syntax: `~whatdoineed <Current Composition> <Target Composition>`\n" +
                              "Example: `~whatdoineed 4d4h4t 7d7h7t` => `3d3h3t`");
        }

        var currentComp = vargs[0];
        var targetComp = vargs[1];

        var (cd, ch, ct) = QueueUtil.GetDesiredRoleCounts(currentComp);
        var (td, th, tt) = QueueUtil.GetDesiredRoleCounts(targetComp);
        var (nd, nh, nt) = (td - cd, th - ch, tt - ct);

        if (nd >= 0 && nh >= 0 && nt >= 0)
        {
            return ReplyAsync($"{nd}d{nh}h{nt}t");
        }
        else
        {
            return ReplyAsync($"{nd}d{nh}h{nt}t ({Math.Max(nd, 0)}d{Math.Max(nh, 0)}h{Math.Max(nt, 0)}t)");
        }
    }
}