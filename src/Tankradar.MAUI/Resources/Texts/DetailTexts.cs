namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte der Tankstellen-Detailansicht.
/// </summary>
public static class DetailTexts
{
    /// <summary>
    /// Titel der Detailseite.
    /// </summary>
    public const string PageTitle = "Stationsdetails";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Öffnen der Details in der Ergebnisliste.
    /// </summary>
    public const string OpenDetails = "Details";

    /// <summary>
    /// Überschrift der Preise.
    /// </summary>
    public const string PricesHeading = "Kraftstoffe";

    /// <summary>
    /// Überschrift der Öffnungszeiten.
    /// </summary>
    public const string OpeningHoursHeading = "Öffnungszeiten";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Aktualisieren der Preise.
    /// </summary>
    public const string Refresh = "Preise aktualisieren";

    /// <summary>
    /// Hinweis, dass keine Preise der aktivierten Sorten vorliegen.
    /// </summary>
    public const string NoPrices = "Für die aktivierten Spritsorten liegen keine Preise vor.";

    /// <summary>
    /// Hinweis, dass die Tankstelle nicht geladen werden konnte und lokal nicht bekannt ist.
    /// </summary>
    public const string NotAvailable = "Die Tankstelle konnte nicht geladen werden.";

    /// <summary>
    /// Hinweis, dass die zuletzt bekannten Daten angezeigt werden.
    /// </summary>
    public const string LastKnownNote = "Es werden die zuletzt bekannten Daten angezeigt.";

    /// <summary>
    /// Einheit des Kraftstoffpreises.
    /// </summary>
    public const string PriceUnit = "€/L";

    /// <summary>
    /// Chip bei frischen Preisen (alle unter 60 Minuten alt).
    /// </summary>
    public const string LivePrices = "Live-Preise";

    /// <summary>
    /// Chip für Tankstellen, die rund um die Uhr als Automat betrieben werden.
    /// </summary>
    public const string Automated247 = "Automat 24/7";

    /// <summary>
    /// Formatiert die Preisaktualität bei veralteten Preisen („Preise: vor 135 Min.“).
    /// </summary>
    /// <param name="ageText">Die Altersangabe des ältesten Preises („vor 135 Min.“).</param>
    /// <returns>Der Text.</returns>
    public static string FormatPriceAge(string ageText)
    {
        return $"Preise: {ageText}";
    }

    /// <summary>
    /// Formatiert die Altersangabe der Öffnungszeiten („Stand: vor 5 Min.“).
    /// </summary>
    /// <param name="ageText">Die Altersangabe („vor 5 Min.“).</param>
    /// <returns>Der Text.</returns>
    public static string FormatOpeningHoursAge(string ageText)
    {
        return $"Stand: {ageText}";
    }

    /// <summary>
    /// Formatiert eine Zeitspanne der Öffnungszeiten („06:00 – 22:00 Uhr“).
    /// </summary>
    /// <param name="start">Der Beginn („06:00“).</param>
    /// <param name="end">Das Ende („22:00“).</param>
    /// <returns>Der Text.</returns>
    public static string FormatTimeRange(string start, string end)
    {
        return $"{start} – {end} Uhr";
    }
}
