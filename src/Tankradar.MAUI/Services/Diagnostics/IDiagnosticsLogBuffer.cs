using Microsoft.Extensions.Logging;

namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Kapazitätsbegrenzter, flüchtiger Puffer für Protokolleinträge des laufenden App-Starts; Grundlage des Diagnosepakets.
/// </summary>
public interface IDiagnosticsLogBuffer
{
    /// <summary>
    /// Nimmt einen Eintrag auf, sofern er die Mindeststufe erreicht; bei vollem Puffer wird der älteste Eintrag verworfen.
    /// </summary>
    /// <param name="level">Die Protokollstufe.</param>
    /// <param name="category">Die Logger-Kategorie.</param>
    /// <param name="message">Die formatierte Meldung.</param>
    /// <param name="exceptionType">Der Typname einer zugehörigen Ausnahme oder <see langword="null"/>.</param>
    void Add(LogLevel level, string category, string message, string? exceptionType);

    /// <summary>
    /// Liefert eine Momentaufnahme der gepufferten Einträge in chronologischer Reihenfolge.
    /// </summary>
    /// <returns>Die gepufferten Einträge.</returns>
    IReadOnlyList<DiagnosticLogEntry> Snapshot();
}
