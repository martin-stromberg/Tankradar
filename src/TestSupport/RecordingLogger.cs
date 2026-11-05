using Microsoft.Extensions.Logging;

namespace Tankradar.TestSupport;

/// <summary>
/// Testlogger, der den vollständigen Text aller Einträge (Meldung und Ausnahme) sammelt, um z. B. das Fehlen von Koordinaten zu prüfen.
/// </summary>
/// <typeparam name="T">Die Kategorie des Loggers.</typeparam>
public sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<string> _sink;

    /// <summary>
    /// Erstellt den Logger mit eigener Liste.
    /// </summary>
    public RecordingLogger()
        : this([])
    {
    }

    /// <summary>
    /// Erstellt den Logger mit einer gemeinsamen Liste, in die mehrere Logger schreiben.
    /// </summary>
    /// <param name="sink">Die gemeinsame Liste.</param>
    public RecordingLogger(List<string> sink)
    {
        _sink = sink;
    }

    /// <summary>
    /// Alle Einträge als Text.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public IReadOnlyList<string> Messages
    {
        get
        {
            lock (_sink)
            {
                return _sink.ToList();
            }
        }
    }

    /// <summary>
    /// Alle Einträge zu einem Text verbunden.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public string AllText => Messages.Count == 0 ? string.Empty : Messages.Aggregate((a, b) => a + "\n" + b);

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
        lock (_sink)
        {
            _sink.Add(formatter(state, exception) + (exception is null ? string.Empty : "\n" + exception));
        }
    }
}
