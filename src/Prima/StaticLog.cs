using Serilog;
using Serilog.Events;
using System;

namespace Prima
{
    public static class StaticLog
    {
        public static void Initialize()
        {
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();
        }

        public static void Write(LogEventLevel level, Exception e, string messageTemplate, params object[] args)
        {
            // ReSharper disable once TemplateIsNotCompileTimeConstantProblem
            Log.Write(level, e, messageTemplate, args);
        }
    }
}