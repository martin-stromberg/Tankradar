using Microsoft.Extensions.Logging;

namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Logging-Provider, der Warnungen und Fehler aller Kategorien in den Diagnosepuffer schreibt; erzeugt keine Dateien und sendet nichts.
/// </summary>
public sealed class DiagnosticsLoggerProvider : ILoggerProvider
{
    private readonly IDiagnosticsLogBuffer _buffer;

    /// <summary>
    /// Erstellt den Provider.
    /// </summary>
    /// <param name="buffer">Der Zielpuffer.</param>
    public DiagnosticsLoggerProvider(IDiagnosticsLogBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        _buffer = buffer;
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
    {
        return new DiagnosticsLogger(categoryName, _buffer);
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private sealed class DiagnosticsLogger : ILogger
    {
        private readonly string _category;
        private readonly IDiagnosticsLogBuffer _buffer;

        public DiagnosticsLogger(string category, IDiagnosticsLogBuffer buffer)
        {
            _category = category;
            _buffer = buffer;
        }

        /// <inheritdoc />
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            return _buffer is DiagnosticsLogBuffer known
                ? logLevel >= known.MinimumLevel
                : logLevel >= LogLevel.Warning;
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrWhiteSpace(message) && exception is null)
            {
                return;
            }

            _buffer.Add(logLevel, _category, message ?? string.Empty, exception?.GetType().Name);
        }
    }
}
