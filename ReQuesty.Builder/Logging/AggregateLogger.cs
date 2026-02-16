using Microsoft.Extensions.Logging;

namespace ReQuesty.Builder.Logging;

public class AggregateLogger<T>(params ILogger<T>[] loggers) : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return new AggregateScope(loggers.Select(l => l.BeginScope(state)).Where(static s => s is not null).Select(static x => x!).ToArray());
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return loggers.Any(l => l.IsEnabled(logLevel));
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        foreach (ILogger<T> logger in loggers)
        {
            logger.Log(logLevel, eventId, state, exception, formatter);
        }
    }
}
