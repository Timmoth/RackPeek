using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace RackPeek.Spectre;

public class SpectreConsoleLogger : ILogger
{
    public IDisposable BeginScope<T>(T state)
    {
        return null!;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
        Func<TState, Exception, string> formatter)
    {
        var message = formatter(state, exception);
        var style = GetStyle(logLevel);

        AnsiConsole.MarkupLine($"[{style}] {message}[/]");
    }

    private Style GetStyle(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => new Style(Color.Grey),
            LogLevel.Debug => new Style(Color.Grey),
            LogLevel.Information => new Style(Color.Green),
            LogLevel.Warning => new Style(Color.Yellow),
            LogLevel.Error => new Style(Color.Red),
            LogLevel.Critical => new Style(Color.Red),
            _ => new Style(Color.White)
        };
    }
}