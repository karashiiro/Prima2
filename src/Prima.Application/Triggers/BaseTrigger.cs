using Discord.WebSocket;

namespace Prima.Application.Triggers;

public abstract class BaseTrigger
{
    private readonly ulong _guildId;

    public BaseTrigger()
    {
        _guildId = 0; // Sentinel value for no guild
    }

    protected BaseTrigger(ulong guildId)
    {
        _guildId = guildId;
    }

    /// <summary>
    /// Returns true if the conditions for the trigger are met.
    /// </summary>
    public abstract bool Condition(SocketMessage message);

    /// <summary>
    /// Runs the message trigger.
    /// </summary>
    public abstract Task Execute(DiscordSocketClient client, SocketMessage message);

    /// <summary>
    /// Returns the applicable guild ID for this trigger. A value of 0 means it can be used anywhere.
    /// </summary>
    public ulong GetApplicableGuildId()
    {
        return _guildId;
    }

    /// <summary>
    /// Returns true if the provided guild is supported by this trigger.
    /// </summary>
    public bool IsGuildApplicable(SocketGuild? guild)
    {
        return _guildId == 0 || _guildId == guild?.Id;
    }

    /// <summary>
    /// Returns true if the provided phrase contains the provided word.
    /// </summary>
    protected static bool HasWord(string phrase, string word)
    {
        var lowerPhrase = phrase.ToLower();
        return lowerPhrase.StartsWith($"{word} ") ||
               lowerPhrase.EndsWith($" {word}") ||
               lowerPhrase.Contains($" {word} ") ||
               lowerPhrase == word;
    }
}