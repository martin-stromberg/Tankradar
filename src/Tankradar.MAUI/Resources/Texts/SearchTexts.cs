using System.Globalization;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte der Umkreissuche (Überschriften, Hinweise, Fehlermeldungen, Formate).
/// </summary>
public static class SearchTexts
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Überschrift der Karte „Suchradius“.
    /// </summary>
    public const string RadiusHeading = "Suchradius";

    /// <summary>
    /// Einheit des Suchradius.
    /// </summary>
    public const string RadiusUnit = "km";

    /// <summary>
    /// Beschriftung der Suchschaltfläche.
    /// </summary>
    public const string SearchButton = "Suchen";

    /// <summary>
    /// Überschrift der Spritsortenfilter-Gruppe.
    /// </summary>
    public const string FilterHeading = "Spritsorte";

    /// <summary>
    /// Filteroption für alle gewählten Sorten.
    /// </summary>
    public const string FilterAll = "Alle";

    /// <summary>
    /// Überschrift der Sortier-Gruppe.
    /// </summary>
    public const string SortHeading = "Sortierung";

    /// <summary>
    /// Meldung bei ungültigem Radius.
    /// </summary>
    public const string RadiusInvalid = "Bitte einen Radius von 1 bis 25 km eingeben.";

    /// <summary>
    /// Hinweis, wenn die Standortnutzung in den Optionen auf „Nie“ steht.
    /// </summary>
    public const string LocationDisabledBySetting = "Die Standortnutzung ist ausgeschaltet. Du kannst sie unter „Optionen“ ändern.";

    /// <summary>
    /// Hinweis bei verweigerter Standortberechtigung.
    /// </summary>
    public const string LocationPermissionDenied = "Ohne Zugriff auf den Standort ist keine Umkreissuche möglich. Bitte erlaube den Standortzugriff in den Systemeinstellungen.";

    /// <summary>
    /// Hinweis, wenn der Standort nicht ermittelt werden konnte.
    /// </summary>
    public const string LocationUnavailable = "Der Standort konnte nicht ermittelt werden. Bitte versuche es später erneut.";

    /// <summary>
    /// Kurzer Hinweis in der Kopfzeile, wenn keine Verbindung besteht.
    /// </summary>
    public const string OfflineBanner = "Offline: keine Verbindung zum Preisdienst.";

    /// <summary>
    /// Hinweis, dass zuletzt bekannte Preise angezeigt werden.
    /// </summary>
    public const string OfflineFallbackNote = "Es werden die zuletzt bekannten Preise angezeigt.";

    /// <summary>
    /// Leerzustand ohne Treffer.
    /// </summary>
    public const string EmptyState = "Keine Tankstellen im Umkreis gefunden";

    /// <summary>
    /// Allgemeine Fehlermeldung, wenn die Suche nicht durchgeführt werden konnte.
    /// </summary>
    public const string SearchFailed = "Die Suche konnte nicht durchgeführt werden.";

    /// <summary>
    /// Status während die Suche läuft.
    /// </summary>
    public const string SearchRunning = "Suche läuft …";

    /// <summary>
    /// Hinweis „geöffnet“.
    /// </summary>
    public const string Open = "Geöffnet";

    /// <summary>
    /// Hinweis „geschlossen“.
    /// </summary>
    public const string Closed = "Geschlossen";

    /// <summary>
    /// Liefert den Hinweistext zu einem Standortstatus ohne Position.
    /// </summary>
    /// <param name="status">Der Status.</param>
    /// <returns>Der Text; leer bei <see cref="LocationStatus.Available"/>.</returns>
    public static string GetLocationMessage(LocationStatus status)
    {
        return status switch
        {
            LocationStatus.Available => string.Empty,
            LocationStatus.DisabledBySetting => LocationDisabledBySetting,
            LocationStatus.PermissionDenied => LocationPermissionDenied,
            _ => LocationUnavailable,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler des Preisdienstes (nie Roh-Ausnahmetexte).
    /// </summary>
    /// <param name="failure">Der Fehlergrund.</param>
    /// <param name="hasStations">Gibt an, ob trotz Fehler Tankstellen (zuletzt bekannte Preise) angezeigt werden.</param>
    /// <returns>Die Meldung; leer, wenn kein Hinweis nötig ist.</returns>
    public static string GetFailureMessage(PriceFailure failure, bool hasStations)
    {
        return failure switch
        {
            PriceFailure.None => string.Empty,
            PriceFailure.Offline => hasStations ? string.Empty : "Keine Netzverbindung und keine gespeicherten Preise für diesen Umkreis.",
            PriceFailure.Unreachable => "Der Preisdienst ist nicht erreichbar.",
            PriceFailure.ApiKeyMissing => "Es ist kein API-Schlüssel hinterlegt.",
            PriceFailure.Rejected => "Der Preisdienst hat die Anfrage abgelehnt. Bitte den API-Schlüssel prüfen.",
            PriceFailure.InvalidResponse => "Der Preisdienst hat eine unerwartete Antwort geliefert.",
            PriceFailure.EndpointNotConfigured => "Der Preisdienst ist nicht eingerichtet (Testmodus ohne Adresse).",
            _ => SearchFailed,
        };
    }

    /// <summary>
    /// Formatiert eine Entfernung („1,4 km“, deutsche Schreibweise).
    /// </summary>
    /// <param name="distanceKm">Die Entfernung in Kilometern.</param>
    /// <returns>Der Text.</returns>
    public static string FormatDistance(double distanceKm)
    {
        return distanceKm.ToString("0.0", German) + " km";
    }

    /// <summary>
    /// Formatiert einen Preis („1,859 €“, deutsche Schreibweise).
    /// </summary>
    /// <param name="price">Der Preis in Euro je Liter.</param>
    /// <returns>Der Text.</returns>
    public static string FormatPrice(decimal price)
    {
        return price.ToString("0.000", German) + " €";
    }
}
