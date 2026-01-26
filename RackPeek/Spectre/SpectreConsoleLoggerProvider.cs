using Microsoft.Extensions.Logging;

namespace RackPeek.Spectre;

public class SpectreConsoleLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new SpectreConsoleLogger();
    }

    public void Dispose()
    {
    }
}