using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace Tests;

public sealed class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _output;

    public XUnitLoggerProvider(ITestOutputHelper output)
    {
        _output = output;
    }

    public ILogger CreateLogger(string categoryName)
        => new XUnitLogger(_output, categoryName);

    public void Dispose() { }

    private sealed class XUnitLogger : ILogger
    {
        private readonly ITestOutputHelper _output;
        private readonly string _category;

        public XUnitLogger(ITestOutputHelper output, string category)
        {
            _output = output;
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId,
            TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            _output.WriteLine($"[{logLevel}] {_category}: {formatter(state, exception)}");
        }
    }
}