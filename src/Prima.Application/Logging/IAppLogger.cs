namespace Prima.Application.Logging;

public interface IAppLogger
{
    void Verbose(Exception e, string messageTemplate, params object[] args);

    void Verbose(string messageTemplate, params object[] args);
    
    void Debug(Exception e, string messageTemplate, params object[] args);

    void Debug(string messageTemplate, params object[] args);
    
    void Info(Exception e, string messageTemplate, params object[] args);

    void Info(string messageTemplate, params object[] args);
    
    void Warn(Exception e, string messageTemplate, params object[] args);

    void Warn(string messageTemplate, params object[] args);
    
    void Error(Exception e, string messageTemplate, params object[] args);

    void Error(string messageTemplate, params object[] args);
    
    void Fatal(Exception e, string messageTemplate, params object[] args);

    void Fatal(string messageTemplate, params object[] args);
}