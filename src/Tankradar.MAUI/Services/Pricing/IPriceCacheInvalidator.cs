namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Verwirft die Zwischenspeicher des Preisdienstes im Arbeitsspeicher, z. B. nach einer Wiederherstellung der Datenbank.
/// </summary>
public interface IPriceCacheInvalidator
{
    /// <summary>
    /// Verwirft die Merker über bereits abgerufene Suchen und aktualisierte Stationen; die nächste Anfrage ruft die Preise wieder ab, soweit die Zeitstempel in der Datenbank es verlangen.
    /// </summary>
    void InvalidateCaches();
}
