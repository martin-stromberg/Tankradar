namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Liefert die aktuell aktiven Geheimnisse (z. B. API-Schlüssel), die im Diagnosepaket wörtlich redigiert werden müssen.
/// </summary>
public interface ISensitiveValueProvider
{
    /// <summary>
    /// Ermittelt die bekannten Geheimnisse; nicht gesetzte oder leere Werte werden ausgelassen.
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Liste der zu redigierenden Werte.</returns>
    Task<IReadOnlyList<string>> GetValuesAsync(CancellationToken cancellationToken = default);
}
