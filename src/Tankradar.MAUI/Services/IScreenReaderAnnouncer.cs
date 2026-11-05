namespace Tankradar.MAUI.Services;

/// <summary>
/// Sagt kurze Hinweise über die Sprachausgabe des Betriebssystems an (z. B. eine erscheinende Rückfrage oder eine Fehlermeldung).
/// </summary>
public interface IScreenReaderAnnouncer
{
    /// <summary>
    /// Sagt den Text an; ohne aktive Sprachausgabe geschieht nichts.
    /// </summary>
    /// <param name="text">Der anzusagende Text.</param>
    void Announce(string text);
}
