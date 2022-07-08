using Discord.Commands;
using Lumina;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json.Linq;
using Prima.DiscordNet.Attributes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Prima.Application.Commands.FFXIV;

[Name("FFXIV Maps")]
public class MapCommands : ModuleBase<SocketCommandContext>
{
    private readonly HttpClient _http;
    private readonly GameData _lumina;

    public MapCommands(HttpClient http, GameData lumina)
    {
        _http = http;
        _lumina = lumina;
    }
    
    [Command("ffxivmap")]
    [Description("[FFXIV] Displays a map of the specified zone.")]
    public async Task MapAsync([Remainder] string zone = "")
    {
        var (mapFile, _) = await GetMapAndSizeFactor(zone);
        if (mapFile == null)
        {
            await ReplyAsync($"{Context.User.Mention}, no map was found with that name!");
            return;
        }
        
        await using var mapToSend = new MemoryStream();
        await mapFile.CopyToAsync(mapToSend);
        mapToSend.Seek(0, SeekOrigin.Begin);
        
        await Context.Channel.SendFileAsync(mapToSend, $"{zone}.jpg");
    }

    [Command("flag", RunMode = RunMode.Async)]
    [Description("[FFXIV] Show the specified map with a flag at the specified coordinates. Usage: `~flag 27 13.4 eureka pyros`")]
    public async Task FlagAsync(params string[] args)
    {
        if (args.Length < 3 || !float.TryParse(args[0], out var x) || !float.TryParse(args[1], out var y))
        {
            await ReplyAsync("Invalid coordinates! Usage: `~flag <x> <y> <zone name>`");
            return;
        }

        var zone = string.Join(' ', args[2..]);
        var (mapFile, sizeFactor) = await GetMapAndSizeFactor(zone);
        if (mapFile == null)
        {
            await ReplyAsync($"{Context.User.Mention}, no map was found with that name!");
            return;
        }

        var map = await Image.LoadAsync(mapFile);
        var redFlag = await Image.LoadAsync(await _http.GetStreamAsync(new Uri("http://heavenswhere.com/icons/redflag.png")));
        var flagWidth = map.Width / 18f;
        var flagHeight = map.Height / 18f;
        var mapScale = GetMapScale(map.Width, sizeFactor);

        redFlag.Mutate(ctx => ctx.Resize(new Size((int)flagWidth, (int)flagHeight)));

        using var canvas = new Image<Rgba32>(map.Width, map.Height);
        canvas.Mutate(ctx =>
        {
            ctx.DrawImage(map, new Point(0, 0), 1.0f);
            ctx.DrawImage(redFlag,
                new Point((int)(x * mapScale - flagWidth / 2), (int)(y * mapScale - flagHeight / 2)), 1.0f);
            
            map.Dispose();
            redFlag.Dispose();
        });

        await using var mapToSend = new MemoryStream();
        await canvas.SaveAsJpegAsync(mapToSend);
        mapToSend.Seek(0, SeekOrigin.Begin);

        await Context.Channel.SendFileAsync(mapToSend, $"{zone}.jpg");
    }

    private async Task<(Stream?, int)> GetMapAndSizeFactor(string zoneName)
    {
        if (string.IsNullOrEmpty(zoneName)) return (null, default);

        zoneName = zoneName.ToLowerInvariant();

        var place = _lumina.GetExcelSheet<PlaceName>()?.FirstOrDefault(pn => pn.Name.RawString.ToLowerInvariant() == zoneName);
        if (place == null) return (null, default);

        var terriType = _lumina.GetExcelSheet<TerritoryType>()?.FirstOrDefault(tt => tt.PlaceName.Row == place.RowId);
        if (terriType == null) return (null, default);

        var xivapiTtRaw = JObject.Parse(await _http.GetStringAsync(new Uri($"https://xivapi.com/TerritoryType/{terriType.RowId}")));
        var xivapiMapRaw = xivapiTtRaw["Map"];
        if (xivapiMapRaw == null) return (null, default);

        var filenameRaw = xivapiMapRaw["MapFilename"];
        var mapScaleRaw = xivapiMapRaw["SizeFactor"];
        if (filenameRaw == null || mapScaleRaw == null) return (null, default);
        
        var mapFilename = filenameRaw.ToObject<string>();

        var mapStream = await _http.GetStreamAsync(new Uri($"https://xivapi.com{mapFilename}"));
        var mapScale = mapScaleRaw.ToObject<int>();

        return (mapStream, mapScale);
    }

    private static float GetMapScale(int pxWidth, int sizeFactor)
        => pxWidth / (float)Math.Floor(41f / (sizeFactor / 100f));
}