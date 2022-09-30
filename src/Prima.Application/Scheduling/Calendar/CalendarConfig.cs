using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Prima.Application.Scheduling.Calendar;

public class CalendarConfig
{
    // ReSharper disable once CollectionNeverUpdated.Global
    [YamlMember(Alias = "calendars")] public IDictionary<string, string> CalendarEntries { get; }

    public CalendarConfig()
    {
        CalendarEntries = new Dictionary<string, string>();
    }

    public static CalendarConfig FromStream(Stream data)
    {
        using var reader = new StreamReader(data);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
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