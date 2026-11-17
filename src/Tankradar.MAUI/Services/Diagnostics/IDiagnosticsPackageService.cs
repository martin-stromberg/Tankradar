namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Erzeugt das Opt-in-Diagnosepaket: eine lokale ZIP-Datei mit Rahmendaten und redigierten Protokollauszügen.
/// </summary>
public interface IDiagnosticsPackageService
{
    /// <summary>
    /// Erstellt den lesbaren Vorschautext des Paketinhalts (Beschreibung, Metadaten und redigierter Protokollauszug), ohne eine Datei zu schreiben.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Der Vorschautext.</returns>
    Task<string> CreatePreviewTextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Schreibt das Diagnosepaket als ZIP-Datei in das Diagnoseverzeichnis des App-Zwischenspeichers und liefert den Pfad.
    /// Ältere Diagnosepakete werden dabei entfernt (Retention: nur das neueste Paket bleibt bestehen).
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Der vollständige Pfad der erzeugten Datei.</returns>
    Task<string> CreatePackageAsync(CancellationToken cancellationToken = default);
}
