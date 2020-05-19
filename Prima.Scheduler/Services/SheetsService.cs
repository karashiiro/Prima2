using System;
using System.IO;

namespace Prima.Scheduler.Services
{
    public sealed class SheetsService : IDisposable
    {
        private const string ApplicationName = "Prima";

        private string GCredentialsFile { get => Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "credentials.json" // Only use Windows for testing.
            : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "credentials.json"); }
        private string GTokenFile { get => Environment.OSVersion.Platform == PlatformID.Win32NT
            ? "token.json"
            : Path.Combine(Environment.GetEnvironmentVariable("HOME"), "token.json"); }
        public void Dispose()
        {
        }
    }
}