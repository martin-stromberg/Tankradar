using Microsoft.Extensions.Logging;

namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Thread-sicherer Ringpuffer: hält nur Warnungen und Fehler des laufenden Starts, begrenzt auf die neuesten Einträge.
/// </summary>
public sealed class DiagnosticsLogBuffer : IDiagnosticsLogBuffer
{
    /// <summary>
    /// Die Standardkapazität des Puffers.
    /// </summary>
    public const int DefaultCapacity = 200;

    private readonly object _gate = new();
    private readonly Queue<DiagnosticLogEntry> _entries = new();
    private readonly TimeProvider _timeProvider;
    private readonly int _capacity;
    private readonly LogLevel _minimumLevel;

    /// <summary>
    /// Erstellt den Puffer.
    /// </summary>
    /// <param name="timeProvider">Die Zeitquelle für die Zeitstempel.</param>
    /// <param name="capacity">Die Höchstzahl gehaltener Einträge (mindestens 1).</param>
    /// <param name="minimumLevel">Die Mindeststufe für die Aufnahme.</param>
    public DiagnosticsLogBuffer(TimeProvider timeProvider, int capacity = DefaultCapacity, LogLevel minimumLevel = LogLevel.Warning)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        if (capacity < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Die Kapazität muss mindestens 1 betragen.");
        }

        _timeProvider = timeProvider;
        _capacity = capacity;
        _minimumLevel = minimumLevel;
    }

    /// <summary>
    /// Die Mindeststufe, ab der Einträge aufgenommen werden.
    /// </summary>
    public LogLevel MinimumLevel => _minimumLevel;

    /// <inheritdoc />
    public void Add(LogLevel level, string category, string message, string? exceptionType)
    {
        if (level < _minimumLevel)
        {
            return;
        }

        lock (_gate)
        {
            while (_entries.Count >= _capacity)
            {
                _entries.Dequeue();
            }

            _entries.Enqueue(new DiagnosticLogEntry(_timeProvider.GetUtcNow(), level, category, message, exceptionType));
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<DiagnosticLogEntry> Snapshot()
    {
        lock (_gate)
        {
            return _entries.ToArray();
        }
    }
}
