using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace BreakfastProvider.Tests.Component.Shared.Common.Logging;

public sealed class InMemoryLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentBag<LogEntry> _entries = [];

    public IReadOnlyCollection<LogEntry> Entries => _entries;

    public ILogger CreateLogger(string categoryName) => new InMemoryLogger(categoryName, _entries);

    public void Dispose() { }

    private sealed class InMemoryLogger(string categoryName, ConcurrentBag<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            entries.Add(new LogEntry
            {
                CategoryName = categoryName,
                LogLevel = logLevel,
                Message = formatter(state, exception),
                Exception = exception
            });
        }
    }
}

public class LogEntry
{
    public string CategoryName { get; init; } = string.Empty;
    public LogLevel LogLevel { get; init; }
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
}
