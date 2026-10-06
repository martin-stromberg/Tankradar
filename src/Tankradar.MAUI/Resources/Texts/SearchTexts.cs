using System.Globalization;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte der Umkreissuche (Überschriften, Hinweise, Fehlermeldungen, Formate).
/// </summary>
public static class SearchTexts
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Überschrift der Karte „Suchart“.
    /// </summary>
    public const string ModeHeading = "Suchen nach";

    /// <summary>
    /// Beschriftung der Suchart „Aktueller Standort“.
    /// </summary>
    public const string ModeCurrentLocation = "Aktueller Standort";

    /// <summary>
    /// Beschriftung der Suchart „Adresse“.
    /// </summary>
    public const string ModeAddress = "Adresse, Ort oder PLZ";

    /// <summary>
    /// Platzhalter des Adressfelds.
    /// </summary>
    public const string AddressPlaceholder = "Adresse, PLZ oder Ort, z. B. Frankfurt oder 60311";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Löschen der Adresseingabe.
    /// </summary>
    public const string AddressClear = "Eingabe löschen";

    /// <summary>
    /// Quellenangabe für die Geodaten (Nutzungsbedingung von OpenStreetMap).
    /// </summary>
    public const string OsmAttribution = "Geodaten © OpenStreetMap-Mitwirkende";

    /// <summary>
    /// Hinweis bei leerer Adresseingabe.
    /// </summary>
    public const string AddressEmpty = "Bitte eine Adresse, einen Ort oder eine Postleitzahl eingeben.";

    /// <summary>
    /// Hinweis bei zu kurzer Adresseingabe.
    /// </summary>
    public const string AddressTooShort = "Die Eingabe ist zu kurz. Bitte mindestens 3 Zeichen eingeben.";

    /// <summary>
    /// Hinweis bei zu langer Adresseingabe.
    /// </summary>
    public const string AddressTooLong = "Die Eingabe ist zu lang. Bitte höchstens 120 Zeichen eingeben.";

    /// <summary>
    /// Hinweis bei unzulässigen Zeichen in der Adresseingabe.
    /// </summary>
    public const string AddressInvalidCharacters = "Die Eingabe enthält ungültige Zeichen. Erlaubt sind Buchstaben, Ziffern und gängige Satzzeichen.";

    /// <summary>
    /// Hinweis, wenn zur Eingabe kein Ort gefunden wurde.
    /// </summary>
    public const string AddressNotFound = "Zu dieser Eingabe wurde in Deutschland kein Ort gefunden. Bitte die Schreibweise prüfen oder genauer angeben.";

    /// <summary>
    /// Hinweis, wenn die Adresse wegen fehlender Verbindung nicht aufgelöst werden kann.
    /// </summary>
    public const string AddressOffline = "Keine Netzverbindung: Die Adresse kann nicht in einen Ort umgewandelt werden.";

    /// <summary>
    /// Hinweis, wenn der Ortssuchdienst nicht erreichbar ist.
    /// </summary>
    public const string GeocodingUnavailable = "Der Ortssuchdienst von OpenStreetMap ist nicht erreichbar. Bitte versuche es später erneut.";

    /// <summary>
    /// Hinweis, wenn der Ortssuchdienst die Anfrage abgelehnt hat.
    /// </summary>
    public const string GeocodingRejected = "Der Ortssuchdienst hat die Anfrage abgelehnt. Bitte versuche es später erneut.";

    /// <summary>
    /// Hinweis, wenn die Antwort des Ortssuchdienstes nicht auswertbar war.
    /// </summary>
    public const string GeocodingInvalidResponse = "Der Ortssuchdienst hat eine unerwartete Antwort geliefert.";

    /// <summary>
    /// Hinweis, wenn der Ortssuchdienst im Testmodus nicht eingerichtet ist.
    /// </summary>
    public const string GeocodingNotConfigured = "Der Ortssuchdienst ist im Testmodus nicht eingerichtet.";

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
    /// Beschriftung der Schaltfläche zum Nachladen weiterer Tankstellen.
    /// </summary>
    public const string ShowMore = "Weitere anzeigen";

    /// <summary>
    /// Formatiert die Beschriftung zum Nachladen („Weitere anzeigen (12 weitere)“).
    /// </summary>
    /// <param name="remaining">Die Anzahl noch nicht angezeigter Tankstellen.</param>
    /// <returns>Der Text.</returns>
    public static string FormatShowMore(int remaining)
    {
        return $"{ShowMore} ({remaining.ToString(German)} weitere)";
    }

    /// <summary>
    /// Formatiert die Adresszeile („Hauptstraße 1, 10115 Berlin“); fehlende Teile werden ausgelassen.
    /// </summary>
    /// <param name="street">Die Straße.</param>
    /// <param name="houseNumber">Die Hausnummer.</param>
    /// <param name="postCode">Die Postleitzahl.</param>
    /// <param name="place">Der Ort.</param>
    /// <returns>Der Text; leer, wenn keine Angabe vorliegt.</returns>
    public static string FormatAddress(string? street, string? houseNumber, string? postCode, string? place)
    {
        var line1 = string.Join(' ', new[] { street, houseNumber }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
        var line2 = string.Join(' ', new[] { postCode, place }.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));
        return string.Join(", ", new[] { line1, line2 }.Where(part => part.Length > 0));
    }

    /// <summary>
    /// Liefert die Beschriftung einer Suchart.
    /// </summary>
    /// <param name="mode">Die Suchart.</param>
    /// <returns>Der Text.</returns>
    public static string GetModeLabel(SearchMode mode)
    {
        return mode switch
        {
            SearchMode.Address => ModeAddress,
            _ => ModeCurrentLocation,
        };
    }

    /// <summary>
    /// Formatiert den Hinweis auf den aufgelösten Ort („Suche rund um: Berlin, Deutschland“).
    /// </summary>
    /// <param name="placeName">Der Name des Ortes.</param>
    /// <returns>Der Text.</returns>
    public static string FormatResolvedPlace(string placeName)
    {
        return "Suche rund um: " + placeName;
    }

    /// <summary>
    /// Liefert den Hinweistext zu einer ungültigen Adresseingabe.
    /// </summary>
    /// <param name="error">Der Grund.</param>
    /// <returns>Der Text; leer bei <see cref="AddressInputError.None"/>.</returns>
    public static string GetAddressInputMessage(AddressInputError error)
    {
        return error switch
        {
            AddressInputError.None => string.Empty,
            AddressInputError.Empty => AddressEmpty,
            AddressInputError.TooShort => AddressTooShort,
            AddressInputError.TooLong => AddressTooLong,
            _ => AddressInvalidCharacters,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einer nicht erfolgreichen Adressauflösung.
    /// </summary>
    /// <param name="status">Der Ausgang der Auflösung.</param>
    /// <param name="isOnline">Gibt an, ob eine Netzverbindung besteht (bei fehlender Verbindung wird der Offline-Hinweis gewählt).</param>
    /// <returns>Der Text; leer bei <see cref="GeocodingStatus.Found"/>.</returns>
    public static string GetGeocodingMessage(GeocodingStatus status, bool isOnline)
    {
        return status switch
        {
            GeocodingStatus.Found => string.Empty,
            GeocodingStatus.NotFound => AddressNotFound,
            GeocodingStatus.InvalidInput => AddressEmpty,
            GeocodingStatus.Unavailable => isOnline ? GeocodingUnavailable : AddressOffline,
            GeocodingStatus.Rejected => GeocodingRejected,
            GeocodingStatus.InvalidResponse => GeocodingInvalidResponse,
            _ => GeocodingNotConfigured,
        };
    }

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
