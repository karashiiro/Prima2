using Lumina;

namespace Prima.Application;

public static class LuminaLoader
{
    public static GameData Load(string dataPath)
    {
        var gameData = new GameData(dataPath, new LuminaOptions { PanicOnSheetChecksumMismatch = false });
        if (gameData == null)
        {
            throw new InvalidOperationException("Failed to load Lumina");
        }

        return gameData;
    }

    public static string GetGameDataPath()
    {
        if (Environment.GetEnvironmentVariable("FFXIV_HOME") == null)
        {
            return Environment.OSVersion.Platform == PlatformID.Win32NT
                ? @"C:\Program Files (x86)\SquareEnix\FINAL FANTASY XIV - A Realm Reborn\game\sqpack"
                : Path.Combine(Environment.GetEnvironmentVariable("HOME")
                               ?? throw new ArgumentException("No HOME variable set!"), "sqpack");
        }

        return Environment.GetEnvironmentVariable("FFXIV_HOME") ??
               throw new ArgumentException("No FFXIV_HOME variable set!");
    }
}