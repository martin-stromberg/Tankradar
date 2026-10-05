namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Bewertet die Aktualität eines Preises ausschließlich anhand seines Zeitstempels.
/// </summary>
public static class PriceFreshness
{
    /// <summary>
    /// Ab diesem Alter gilt ein Preis als veraltet.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly TimeSpan StaleThreshold = TimeSpan.FromMinutes(60);

    /// <summary>
    /// Berechnet das Alter eines Preises; ein Zeitstempel in der Zukunft (Uhrenabweichung) ergibt das Alter null.
    /// </summary>
    /// <param name="retrievedUtc">Abrufzeitpunkt in UTC.</param>
    /// <param name="nowUtc">Aktueller Zeitpunkt in UTC.</param>
    /// <returns>Das Alter, mindestens null.</returns>
    public static TimeSpan GetAge(DateTime retrievedUtc, DateTime nowUtc)
    {
        var age = nowUtc - retrievedUtc;
        return age < TimeSpan.Zero ? TimeSpan.Zero : age;
    }

    /// <summary>
    /// Gibt an, ob ein Preis veraltet ist (Alter von mindestens 60 Minuten).
    /// </summary>
    /// <param name="retrievedUtc">Abrufzeitpunkt in UTC.</param>
    /// <param name="nowUtc">Aktueller Zeitpunkt in UTC.</param>
    /// <returns><see langword="true"/>, wenn der Preis mindestens 60 Minuten alt ist.</returns>
    public static bool IsStale(DateTime retrievedUtc, DateTime nowUtc)
    {
        return GetAge(retrievedUtc, nowUtc) >= StaleThreshold;
    }

    /// <summary>
    /// Formatiert das Alter als „vor X Min.“ (immer in vollen Minuten).
    /// </summary>
    /// <param name="retrievedUtc">Abrufzeitpunkt in UTC.</param>
    /// <param name="nowUtc">Aktueller Zeitpunkt in UTC.</param>
    /// <returns>Die Altersangabe.</returns>
    public static string FormatAge(DateTime retrievedUtc, DateTime nowUtc)
    {
        var minutes = (long)GetAge(retrievedUtc, nowUtc).TotalMinutes;
        return $"vor {minutes} Min.";
    }
}
