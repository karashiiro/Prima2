using Discord;
using Activity = System.Collections.Generic.KeyValuePair<string, Discord.ActivityType>;

namespace Prima.Stable.Resources
{
    public static class Presences
    {
        public static readonly Activity[] List = {
            // Playing
            new("FINAL FANTASY XII", ActivityType.Playing),
            new("FINAL FANTASY XIII", ActivityType.Playing),
            new("FINAL FANTASY XIII-2", ActivityType.Playing),
            new("FINAL FANTASY XIII: LIGHTNING RETURNS", ActivityType.Playing),
            new("FINAL FANTASY XIV", ActivityType.Playing),
            new("FINAL FANTASY XV", ActivityType.Playing),
            new("FINAL FANTASY XVI", ActivityType.Playing),
            new("FINAL FANTASY XXVI", ActivityType.Playing),
            new("STRANGER OF PARADISE: FINAL FANTASY ORIGINS", ActivityType.Playing),
            new("PHANTASY STAR ONLINE 2", ActivityType.Playing),
            new("Arknights", ActivityType.Playing),
            new("Puzzle & Dragons", ActivityType.Playing),
            new("Granblue Fantasy", ActivityType.Playing),
            new("ラストイデア", ActivityType.Playing),
            new("ワールドフリッパー", ActivityType.Playing),
            new("Tetra Master", ActivityType.Playing),
            new("PlayOnline Launcher", ActivityType.Playing),
            new("Detroit: Become Human", ActivityType.Playing),
            new("NieR: Automata", ActivityType.Playing),
            new("Drakengard 3", ActivityType.Playing),
            new("Fire Emblem: Three Houses", ActivityType.Playing),
            new("The Baldesion Arsenal", ActivityType.Playing),
            new("Delubrum Reginae", ActivityType.Playing),
            new("Delubrum Reginae (Savage)", ActivityType.Playing),
            new("MONSTER HUNTER: WORLD", ActivityType.Playing),
            new("Microsoft Visual Studio", ActivityType.Playing),
            new("League of Legends", ActivityType.Playing),
            new("Pokémon Black 2", ActivityType.Playing),
            new("Rune Factory 4", ActivityType.Playing),
            new("Cytus", ActivityType.Playing),
            new("Cytus 2", ActivityType.Playing),
            new("Groove Coaster 3", ActivityType.Playing),
            new("Groove Coaster 4", ActivityType.Playing),
            new("Groove Coaster 5", ActivityType.Playing),
            new("Dissidia Final Fantasy", ActivityType.Playing),
            new("太鼓の達人", ActivityType.Playing),
            new("Pokémon Tretta", ActivityType.Playing),
            new("Maimai", ActivityType.Playing),
            new("NieR: Reincarnation", ActivityType.Playing),
            new("Minceraft", ActivityType.Playing),
            new("Portal", ActivityType.Playing),
            new("Portal 2", ActivityType.Playing),
            new("Genshin Impact", ActivityType.Playing),
            new("Honkai Impact 3rd", ActivityType.Playing),
            new("Honkai: Star Rail", ActivityType.Playing),
            new("スクスタ", ActivityType.Playing),
            // Listening
            new("Vaporwave Furret 10 Hours", ActivityType.Listening),
            new("Super Touhou Eurobeat Mix", ActivityType.Listening),
            // Streaming gets turned into "Playing" if there's no actual stream.
            // Watching
            new("Live Vana'diel", ActivityType.Watching),
            new("you", ActivityType.Watching),
        };
    }
}
