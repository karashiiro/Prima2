using Activity = System.Collections.Generic.KeyValuePair<string, Discord.ActivityType>;

namespace Prima.Stable.Resources
{
    public static class Presences
    {
        public static readonly Activity[] List = {
            // Playing
            new Activity("FINAL FANTASY XIV", Discord.ActivityType.Playing),
            new Activity("FINAL FANTASY XIII", Discord.ActivityType.Playing),
            new Activity("FINAL FANTASY XI", Discord.ActivityType.Playing),
            new Activity("FINAL FANTASY XXVI", Discord.ActivityType.Playing),
            new Activity("PHANTASY STAR ONLINE 2", Discord.ActivityType.Playing),
            new Activity("Fate/Extella", Discord.ActivityType.Playing),
            new Activity("Arknights", Discord.ActivityType.Playing),
            new Activity("Puzzle & Dragons", Discord.ActivityType.Playing),
            new Activity("Granblue Fantasy", Discord.ActivityType.Playing),
            new Activity("ラストイデア", Discord.ActivityType.Playing),
            new Activity("ワールドフリッパー", Discord.ActivityType.Playing),
            new Activity("Temtem", Discord.ActivityType.Playing),
            new Activity("Tetra Master", Discord.ActivityType.Playing),
            new Activity("PlayOnline Launcher", Discord.ActivityType.Playing),
            new Activity("Pokémon Shield", Discord.ActivityType.Playing),
            new Activity("Detroit: Become Human", Discord.ActivityType.Playing),
            new Activity("NieR: Automata", Discord.ActivityType.Playing),
            new Activity("Drakengard 3", Discord.ActivityType.Playing),
            new Activity("Fire Emblem: Three Houses", Discord.ActivityType.Playing),
            new Activity("The Baldesion Arsenal", Discord.ActivityType.Playing),
            new Activity("CLIP STUDIO PAINT", Discord.ActivityType.Playing),
            new Activity("MONSTER HUNTER: WORLD", Discord.ActivityType.Playing),
            new Activity("Microsoft Visual Studio", Discord.ActivityType.Playing),
            new Activity("League of Legends", Discord.ActivityType.Playing),
            new Activity("the piano", Discord.ActivityType.Playing),
            new Activity("Dragalia Lost", Discord.ActivityType.Playing),
            new Activity("Dragalia Found", Discord.ActivityType.Playing),
            new Activity("Pokémon Black 2", Discord.ActivityType.Playing),
            new Activity("Rune Factory 4", Discord.ActivityType.Playing),
            new Activity("Cytus 2", Discord.ActivityType.Playing),
            new Activity("Pastel Girl", Discord.ActivityType.Playing),
            new Activity("Groove Coaster 3", Discord.ActivityType.Playing),
            new Activity("Groove Coaster 4", Discord.ActivityType.Playing),
            new Activity("Dissidia Final Fantasy", Discord.ActivityType.Playing),
            new Activity("太鼓の達人", Discord.ActivityType.Playing),
            new Activity("Pokémon Tretta", Discord.ActivityType.Playing),
            new Activity("Maimai", Discord.ActivityType.Playing),
            new Activity("Destiny 2", Discord.ActivityType.Playing),
            new Activity("Pokémon Café", Discord.ActivityType.Playing),
            new Activity("NieR: Reincarnation", Discord.ActivityType.Playing),
            // Listening
            new Activity("Dorime 10-Hour Loop", Discord.ActivityType.Listening),
            new Activity("Pokémon ~ Jazz/Orchestra Mix", Discord.ActivityType.Listening),
            new Activity("Vaporwave Furret 10 Hours", Discord.ActivityType.Listening),
            new Activity("Super Touhou Eurobeat Mix", Discord.ActivityType.Listening),
            new Activity("エヌ", Discord.ActivityType.Listening),
            new Activity("アブジェ", Discord.ActivityType.Listening),
            // Streaming gets turned into "Playing" if there's no actual stream.
            // Watching
            new Activity("Live Vana'diel", Discord.ActivityType.Watching),
            new Activity("you", Discord.ActivityType.Watching),
        };
    }
}
