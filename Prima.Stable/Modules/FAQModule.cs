using System.Threading.Tasks;
using Discord.Commands;
using Prima.DiscordNet.Attributes;
using Prima.Resources;

namespace Prima.Stable.Modules
{
    [Name("FAQ Module")]
    public class FAQModule : ModuleBase<SocketCommandContext>
    {
        [Command("how2lodestone", RunMode = RunMode.Async)]
        [Description("Lodestone linking FAQ.")]
        [RateLimit(TimeSeconds = 10, Global = true)]
        [RestrictToGuilds(SpecialGuilds.CrystalExploratoryMissions)]
        public Task How2Lodestone()
        {
            return ReplyAsync("You login to your FFXIV lodestone, you can do it from your phone as well. " +
                              "You login, click on your character, then it brings you to a page filled with activity, " +
                              "at the top-ish you'll see your character and click that it'll bring you to your character " +
                              "profile. Scroll down a bit and you'll see \"Character Profile\" (it'll be under a bunch of " +
                              "stuff -- gotta go past the mounts, minions, orchestration list, etc) there will be a \"edit\" " +
                              "pencil like thing, click that and paste the ID number that prima sent you but just in " +
                              "case for you it's this: `123456789012345678` (the numbers) into the \"Character Profile\". " +
                              "Then follow the steps on doing the `~iam \"Server name\" \"First name\" \"Last name\"` " +
                              "of your character command. And that should get you verified. You can delete it afterwards " +
                              "but if you ever need to reverify or something you'll need to do it again with inputting the numbers.");
        }
    }
}