using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
// ReSharper disable CollectionNeverUpdated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace Prima.Application.Scheduling.Calendar;

public class CalendarConfig
{
    public IDictionary<string, string> Calendars { get; set; }

    public CalendarConfig()
    {
        Calendars = new Dictionary<string, string>();
    }

    public static CalendarConfig FromStream(Stream data)
    {
        using var reader = new StreamReader(data);
        var deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        var config = deserializer.Deserialize<CalendarConfig>(reader);
        return config;
    }

    public static async Task<CalendarConfig> FromFile(string fileName)
    {
        await using var f = File.OpenRead(fileName);
        return FromStream(f);
    }

    public static async Task<(CalendarConfig?, Exception?)> FromFileSafely(string fileName)
    {
        try
        {
            await using var f = File.OpenRead(fileName);
            return (FromStream(f), null);
        }
        catch (Exception e)
        {
            return (null, e);
        }
    }
}