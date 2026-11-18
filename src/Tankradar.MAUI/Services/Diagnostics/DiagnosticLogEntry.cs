using Microsoft.Extensions.Logging;

namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Ein gepufferter Protokolleintrag für das Diagnosepaket: Zeitpunkt, Stufe, Kategorie, Meldung und optional der Typname einer Ausnahme.
/// </summary>
/// <param name="Timestamp">Der Zeitpunkt des Eintrags.</param>
/// <param name="Level">Die Protokollstufe.</param>
/// <param name="Category">Die Logger-Kategorie.</param>
/// <param name="Message">Die formatierte Meldung (wird erst beim Paketaufbau redigiert).</param>
/// <param name="ExceptionType">Der Typname einer zugehörigen Ausnahme oder <see langword="null"/>.</param>
/// <returns>Der Wert.</returns>
public sealed record DiagnosticLogEntry(
    DateTimeOffset Timestamp,
    LogLevel Level,
    string Category,
    string Message,
    string? ExceptionType);
