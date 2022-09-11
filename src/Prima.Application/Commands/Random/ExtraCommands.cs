using Discord.Commands;
using Prima.DiscordNet.Attributes;
using Prima.Game.FFXIV.XIVAPI;
using Prima.Services;

namespace Prima.Application.Commands.Random;

[Name("Extra")]
public class ExtraCommands : ModuleBase<SocketCommandContext>
{
    public IDbService Db { get; set; }
    public HttpClient Http { get; set; }
    public XIVAPIClient Xivapi { get; set; }

    [Command("roll", RunMode = RunMode.Async)]
    [Description("T̵̪͖̎̈̍ḛ̷̤͑̚ș̴̔͑̾ͅͅt̸͔͜͝ ̶̡̨̪͌̉͠ỷ̵̺̕o̴̞̍ū̴͚̣̤r̵͚͎͔͘ ̴̨̬̿ḷ̷͖̀̚u̴͖̲͌́c̴̲̣͙͑̈͝k̸͍͖̿̆̓!̶̢̅̀")]
    public async Task RollAsync([Remainder] string args = "")
    {
        var res = (int)Math.Floor(new System.Random().NextDouble() * 4);
        switch (res)
        {
            case 0:
                await ReplyAsync(
                    $"BINGO! You matched {(int)Math.Floor(new System.Random().NextDouble() * 11) + 1} lines! Congratulations!");
                break;
            case 1:
                var opt = (int)Math.Floor(new System.Random().NextDouble() * 2598960) + 1;
                switch (opt)
                {
                    case <= 4:
                        await ReplyAsync("JACK**P**O*T!* Roya**l flush!** You __won__ [%#*(!@] credits*!*");
                        break;
                    case > 4 and <= 40:
                        await ReplyAsync(
                            "Straight flush! You won [TypeError: Cannot read property 'MUNZ' of undefined] credits!");
                        break;
                    case > 40 and <= 664:
                        await ReplyAsync(
                            "Four of a kind! You won [TypeError: Cannot read property 'thinking' of undefined] credits!");
                        break;
                    case > 664 and <= 4408:
                        await ReplyAsync(
                            "Full house! You won [TypeError: Cannot read property '<:GWgoaThinken:582982105282379797>' of undefined] credits!");
                        break;
                    case > 4408 and <= 9516:
                        await ReplyAsync("Flush! You won -1 credits!");
                        break;
                    case > 9516 and <= 19716:
                        await ReplyAsync("Straight! You won -20 credits!");
                        break;
                    case > 19716 and <= 74628:
                        await ReplyAsync("Two pairs...? You won -500 credits!");
                        break;
                    case > 198180 and <= 1296420:
                        await ReplyAsync("One pair. You won -2500 credits.");
                        break;
                    default:
                        await ReplyAsync("No pairs. You won -10000 credits.");
                        break;
                }
                break;
            case 2:
                await ReplyAsync(
                    $"Critical hit! You dealt {(int)Math.Floor(new System.Random().NextDouble() * 9999) + 9999} damage!");
                break;
            case 3:
                await ReplyAsync("You took 9999999 points of damage. You lost the game.");
                break;
        }
    }
}