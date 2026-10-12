namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Stellt nach einer unterbrochenen Wiederherstellung den bisherigen Stand wieder her (Aufruf beim Start vor der Migration).
/// </summary>
public interface IRestoreRecovery
{
    /// <summary>
    /// Nimmt einen unvollständigen Austausch zurück, falls das Journal existiert, und entfernt verwaiste Staging- und Rücksicherungsverzeichnisse.
    /// </summary>
    /// <param name="dataDirectory">Das Datenverzeichnis.</param>
    void RecoverIfNeeded(string dataDirectory);
}
