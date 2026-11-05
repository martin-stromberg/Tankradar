namespace Tankradar.MAUI.Services;

/// <summary>
/// Initialisiert die lokale Datenbank (Migrationen, Dateischutz).
/// </summary>
public interface IDatabaseInitializer
{
    /// <summary>
    /// Stellt sicher, dass die Datenbank angelegt und auf dem aktuellen Schemastand ist. Mehrfache Aufrufe teilen sich denselben Lauf.
    /// </summary>
    /// <returns>Ein Task, der abgeschlossen ist, sobald die Datenbank bereit ist.</returns>
    Task InitializeAsync();
}
