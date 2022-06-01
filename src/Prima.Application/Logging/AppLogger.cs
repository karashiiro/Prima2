using System.Diagnostics.CodeAnalysis;
using Serilog;

namespace Prima.Application.Logging;

[SuppressMessage("Performance", "CA1822:Mark members as static")]
[SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
public class AppLogger : IAppLogger
{
    private static bool _serilogInitialized;
    
    public AppLogger()
    {
        if (!_serilogInitialized)
        {
            _serilogInitialized = true;
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();
        }
    }
    
    public void Verbose(Exception e, string messageTemplate, params object[] args)
    {
        Log.Verbose(e, messageTemplate, args);
    }

    public void Verbose(string messageTemplate, params object[] args)
    {
        Log.Verbose(messageTemplate, args);
    }
    
    public void Debug(Exception e, string messageTemplate, params object[] args)
    {
        Log.Debug(e, messageTemplate, args);
    }

    public void Debug(string messageTemplate, params object[] args)
    {
        Log.Debug(messageTemplate, args);
    }
    
    public void Info(Exception e, string messageTemplate, params object[] args)
    {
        Log.Information(e, messageTemplate, args);
    }

    public void Info(string messageTemplate, params object[] args)
    {
        Log.Information(messageTemplate, args);
    }
    
    public void Warn(Exception e, string messageTemplate, params object[] args)
    {
        Log.Warning(e, messageTemplate, args);
    }
    
    public void Warn(string messageTemplate, params object[] args)
    {
        Log.Warning(messageTemplate, args);
    }
    
    public void Error(Exception e, string messageTemplate, params object[] args)
    {
        Log.Error(e, messageTemplate, args);
    }
    
    public void Error(string messageTemplate, params object[] args)
    {
        Log.Error(messageTemplate, args);
    }
    
    public void Fatal(Exception e, string messageTemplate, params object[] args)
    {
        Log.Fatal(e, messageTemplate, args);
    }
    
    public void Fatal(string messageTemplate, params object[] args)
    {
        Log.Fatal(messageTemplate, args);
    }
}