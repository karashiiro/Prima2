using System;
using System.IO;
using Serilog;
using Serilog.Events;

namespace Prima
{
    public static class StaticLog
    {
        public static void Initialize()
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.SQLite(Environment.OSVersion.Platform == PlatformID.Win32NT
                    ? "Log.db" // Only use Windows for testing.
                    : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "log/Log.db"))
                .CreateLogger();
        }

        public static void Write(LogEventLevel level, Exception e, string messageTemplate, params object[] args)
        {
            Log.Write(level, e, messageTemplate, args);
        }
    }
}