using Microsoft.Extensions.Logging;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// Testlogger, der alle Einträge samt Ausnahme in einer Liste sammelt.
/// </summary>
/// <typeparam name="T">Die Kategorie des Loggers.</typeparam>
public class ListLogger<T> : ILogger<T>
{
    /// <summary>
    /// Die protokollierten Einträge als Stufe und Ausnahme.
    /// </summary>
    public List<(LogLevel Level, Exception? Exception)> Entries { get; } = [];

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Entries.Add((logLevel, exception));
    }
}
