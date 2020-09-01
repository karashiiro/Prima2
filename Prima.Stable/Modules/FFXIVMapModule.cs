using Discord.Commands;
using Newtonsoft.Json.Linq;
using Prima.Attributes;
using Prima.Models;
using Prima.Services;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Prima.Stable.Modules
{
    public class FFXIVMapModule : ModuleBase<SocketCommandContext>
    {
        public HttpClient Http { get; set; }
        public FFXIVSheetService Sheets { get; set; }

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
            await Context.Channel.SendFileAsync(mapFile, $"{zone}.jpg");
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

            var zone = string.Join(' ', args[2..^0]);
            var (mapFile, sizeFactor) = await GetMapAndSizeFactor(zone);
            if (mapFile == null)
            {
                await ReplyAsync($"{Context.User.Mention}, no map was found with that name!");
                return;
            }

            using var map = Image.FromStream(mapFile);
            using var redFlag = Image.FromStream(await Http.GetStreamAsync(new Uri("http://heavenswhere.com/icons/redflag.png")));
            var flagWidth = map.Width / 18f;
            var flagHeight = map.Height / 18f;
            var mapScale = GetMapScale(map.Width, sizeFactor);

            using var drawing = new Bitmap(map.Width, map.Height, PixelFormat.Format32bppArgb);
            using var g = Graphics.FromImage(drawing);
            g.DrawImage(map, 0, 0);
            g.DrawImage(redFlag, x * mapScale - flagWidth / 2, y * mapScale - flagHeight / 2, flagWidth, flagHeight);
            using var mapToSend = new MemoryStream();
            drawing.Save(mapToSend, ImageFormat.Jpeg);
            mapToSend.Seek(0, SeekOrigin.Begin);

            await Context.Channel.SendFileAsync(mapToSend, $"{zone}.jpg");
        }

        private async Task<(Stream, int)> GetMapAndSizeFactor(string zoneName)
        {
            if (string.IsNullOrEmpty(zoneName)) return (null, default);

            zoneName = zoneName.ToLowerInvariant();

            var place = Sheets.GetSheet<PlaceName>().FirstOrDefault(pn => pn.Name.ToLowerInvariant() == zoneName);
            if (place == null) return (null, default);
            var terriType = Sheets.GetSheet<TerritoryType>().FirstOrDefault(tt => tt.PlaceName == place.RowId);
            if (terriType == null) return (null, default);

            var xivapiTTRaw = JObject.Parse(await Http.GetStringAsync(new Uri($"https://xivapi.com/TerritoryType/{terriType.RowId}")));
            var mapFilename = xivapiTTRaw["Map"]["MapFilename"].ToObject<string>();

            var mapStream = await Http.GetStreamAsync(new Uri($"https://xivapi.com{mapFilename}"));
            var mapScale = xivapiTTRaw["Map"]["SizeFactor"].ToObject<int>();

            return (mapStream, mapScale);
        }

        private static float GetMapScale(int pxWidth, int sizeFactor)
            => pxWidth / (float)Math.Floor(41f / (sizeFactor / 100f));
    }
}
