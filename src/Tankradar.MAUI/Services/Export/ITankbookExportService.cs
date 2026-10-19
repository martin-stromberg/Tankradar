using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Services.Export;

/// <summary>
/// Erzeugt den Export des Tankbuchs eines Fahrzeugs als Datei im Zwischenspeicher (CSV oder PDF, optional auf einen Zeitraum eingeschränkt).
/// Die Weitergabe der Datei übernimmt der Aufrufer über die Systemfunktion Teilen/Speichern; der Dienst überträgt selbst nichts.
/// </summary>
public interface ITankbookExportService
{
    /// <summary>
    /// Erzeugt die Datei. Vorher werden alte Exportdateien gelöscht. Fehler beim Lesen oder Schreiben werden als <see cref="TankbookExportStatus.Failed"/> gemeldet.
    /// </summary>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <param name="request">Format und Zeitraum.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis; nur bei <see cref="TankbookExportStatus.Exported"/> mit Datei.</returns>
    Task<TankbookExportResult> CreateAsync(long vehicleId, TankbookExportRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Löscht eine Exportdatei (z. B. eine angefangene nach einem Fehler); Fehler werden ignoriert. Dateien nach einer erfolgreichen Weitergabe bleiben bis zum nächsten Export liegen, weil die Zielanwendung sie womöglich erst später liest.
    /// </summary>
    /// <param name="path">Der vollständige Pfad der Datei aus <see cref="TankbookExportResult.FilePath"/>.</param>
    void Discard(string path);
}
