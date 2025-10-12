namespace Prima.Game.FFXIV
{
    public class PotentialDbUser
    {
        public string Name { get; }
        public string World { get; }
        public DiscordXIVUser? User { get; set; }

        public PotentialDbUser(string name, string world, DiscordXIVUser? user = null)
        {
            Name = name;
            World = world;
            User = user;
        }

        public override string ToString()
        {
            return $"({World}) {Name}";
        }
    }
}