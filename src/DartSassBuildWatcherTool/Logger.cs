namespace DartSassBuildWatcherTool;

internal class Logger
{
    readonly LogLevels _logLevel;

    public Logger(LogLevels logLevel)
    {
        _logLevel = logLevel;
    }

    public void Log(string message)
    {
        Console.WriteLine(message);
    }

    public void LogVerbose(string message)
    {
        if (_logLevel == LogLevels.Verbose)
            Log(message);
    }
}
