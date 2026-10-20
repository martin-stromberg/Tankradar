namespace Tankradar.MAUI.Services.Backup;

/// <summary>
/// Benachrichtigt geöffnete Seiten, dass die Daten aus einer Sicherung wiederhergestellt wurden und neu geladen werden müssen.
/// Abonnenten werden schwach gehalten, damit kurzlebige ViewModels nicht über den Dienst am Leben bleiben.
/// </summary>
public interface IRestoreNotifier
{
    /// <summary>
    /// Wird ausgelöst, nachdem die Daten erfolgreich wiederhergestellt wurden.
    /// </summary>
    event EventHandler? Restored;

    /// <summary>
    /// Die Zahl der bisher gemeldeten Wiederherstellungen.
    /// </summary>
    int Generation { get; }

    /// <summary>
    /// Meldet eine erfolgreiche Wiederherstellung an alle Abonnenten.
    /// </summary>
    void NotifyRestored();
}
