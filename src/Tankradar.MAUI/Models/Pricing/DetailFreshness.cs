namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Bewertet das Alter der Detailangaben einer Tankstelle (Öffnungszeiten, durchgehende Öffnung und davon abgeleitete Hinweise wie „Automatentankstelle“).
/// Diese Angaben stammen aus einer früheren Detailabfrage und ändern sich selten; über die Altersgrenze hinaus werden sie nicht mehr angezeigt
/// und die Detailansicht fragt sie neu ab.
/// </summary>
public static class DetailFreshness
{
    /// <summary>
    /// Bis zu diesem Alter gelten Detailangaben als verwendbar (24 Stunden).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    /// <summary>
    /// Gibt an, ob Detailangaben mit dem angegebenen Abrufzeitpunkt noch verwendbar sind. Ein unbekannter Zeitpunkt gilt als nicht verwendbar.
    /// </summary>
    /// <param name="detailsUpdatedUtc">Abrufzeitpunkt der Detailangaben in UTC; <see langword="null"/>, wenn unbekannt.</param>
    /// <param name="nowUtc">Aktueller Zeitpunkt in UTC.</param>
    /// <returns><see langword="true"/>, wenn das Alter die Grenze nicht überschreitet.</returns>
    public static bool IsUsable(DateTime? detailsUpdatedUtc, DateTime nowUtc)
    {
        return detailsUpdatedUtc is { } updated && PriceFreshness.GetAge(updated, nowUtc) <= MaxAge;
    }

    /// <summary>
    /// Formatiert das Alter der Detailangaben: unter einer Stunde „vor X Min.“, sonst „vor X Std.“.
    /// </summary>
    /// <param name="detailsUpdatedUtc">Abrufzeitpunkt der Detailangaben in UTC.</param>
    /// <param name="nowUtc">Aktueller Zeitpunkt in UTC.</param>
    /// <returns>Die Altersangabe.</returns>
    public static string FormatAge(DateTime detailsUpdatedUtc, DateTime nowUtc)
    {
        var age = PriceFreshness.GetAge(detailsUpdatedUtc, nowUtc);
        return age < TimeSpan.FromHours(1)
            ? PriceFreshness.FormatAge(detailsUpdatedUtc, nowUtc)
            : $"vor {(long)age.TotalHours} Std.";
    }
}
