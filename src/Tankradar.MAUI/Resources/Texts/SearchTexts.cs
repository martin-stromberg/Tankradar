using System.Globalization;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Routing;
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
    /// Kopfzeile der Trefferliste, solange kein Ergebnis mit Tankstellen vorliegt.
    /// </summary>
    public const string NoResults = "Keine Ergebnisse";

    /// <summary>
    /// Formatiert die Trefferanzahl in der Kopfzeile der Trefferliste („12 Tankstellen“, „1 Tankstelle“).
    /// </summary>
    /// <param name="count">Die Anzahl der Tankstellen.</param>
    /// <returns>Der Text.</returns>
    public static string FormatStationCount(int count)
    {
        return count == 1 ? "1 Tankstelle" : $"{count.ToString(German)} Tankstellen";
    }

    /// <summary>
    /// Formatiert die Beschriftung zum Nachladen („Weitere anzeigen (12 übrig)“); das Wort „weitere“ kommt nur einmal vor, damit die Sprachausgabe nichts doppelt liest.
    /// </summary>
    /// <param name="remaining">Die Anzahl noch nicht angezeigter Tankstellen.</param>
    /// <returns>Der Text.</returns>
    public static string FormatShowMore(int remaining)
    {
        return $"{ShowMore} ({remaining.ToString(German)} übrig)";
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
            SearchMode.Route => ModeRoute,
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

    /// <summary>
    /// Beschriftung der Suchart „Route“.
    /// </summary>
    public const string ModeRoute = "Route";

    /// <summary>
    /// Überschrift der Startauswahl der Routensuche.
    /// </summary>
    public const string RouteStartHeading = "Start";

    /// <summary>
    /// Beschriftung der Startart „Aktueller Standort“.
    /// </summary>
    public const string RouteStartCurrentLocation = "Aktueller Standort";

    /// <summary>
    /// Beschriftung der Startart „Adresse“.
    /// </summary>
    public const string RouteStartAddress = "Adresse, Ort oder PLZ";

    /// <summary>
    /// Platzhalter des Startfelds.
    /// </summary>
    public const string RouteStartPlaceholder = "Start: Adresse, PLZ oder Ort, z. B. Berlin";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Löschen des Starts.
    /// </summary>
    public const string RouteStartClear = "Start löschen";

    /// <summary>
    /// Überschrift des Zielfelds der Routensuche.
    /// </summary>
    public const string RouteDestinationHeading = "Ziel";

    /// <summary>
    /// Platzhalter des Zielfelds.
    /// </summary>
    public const string RouteDestinationPlaceholder = "Ziel: Adresse, PLZ oder Ort, z. B. Hamburg";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Löschen des Ziels.
    /// </summary>
    public const string RouteDestinationClear = "Ziel löschen";

    /// <summary>
    /// Überschrift der Abstandsauswahl der Routensuche.
    /// </summary>
    public const string RouteDistanceHeading = "Abstand zur Route";

    /// <summary>
    /// Quellenangabe für Routen (Nutzungsbedingung von OpenRouteService und OpenStreetMap).
    /// </summary>
    public const string RoutingAttribution = "Routen: OpenRouteService, Geodaten © OpenStreetMap-Mitwirkende";

    /// <summary>
    /// Beschriftung der Abstandsangabe zur Route in der Ergebnisliste (Zugänglichkeit).
    /// </summary>
    public const string RouteOffsetLabel = "Abstand zur Route";

    /// <summary>
    /// Hinweis bei ungültigem Abstand zur Route.
    /// </summary>
    public const string RouteDistanceInvalid = "Bitte einen Abstand von 1, 2, 5 oder 10 km zur Route wählen.";

    /// <summary>
    /// Hinweis, wenn die Routensuche wegen fehlender Verbindung nicht möglich ist.
    /// </summary>
    public const string RouteOffline = "Keine Netzverbindung: Die Routensuche braucht eine Verbindung zum Orts-, Routen- und Preisdienst.";

    /// <summary>
    /// Hinweis, wenn Start und Ziel am selben Ort liegen.
    /// </summary>
    public const string RouteSamePlace = "Start und Ziel liegen am selben Ort. Bitte ein anderes Ziel wählen.";

    /// <summary>
    /// Hinweis, wenn zum Start kein Ort gefunden wurde.
    /// </summary>
    public const string RouteStartNotFound = "Zum Start wurde in Deutschland kein Ort gefunden. Bitte die Schreibweise prüfen oder genauer angeben.";

    /// <summary>
    /// Hinweis, wenn zum Ziel kein Ort gefunden wurde.
    /// </summary>
    public const string RouteDestinationNotFound = "Zum Ziel wurde in Deutschland kein Ort gefunden. Bitte die Schreibweise prüfen oder genauer angeben.";

    /// <summary>
    /// Hinweis, wenn die Routensuche nicht eingerichtet ist (fehlender Schlüssel oder Testmodus ohne Adresse).
    /// </summary>
    public const string RoutingUnavailable = "Routensuche nicht verfügbar. Die übrige Suche funktioniert weiterhin.";

    /// <summary>
    /// Hinweis, wenn der Routendienst den Schlüssel abgelehnt hat.
    /// </summary>
    public const string RoutingKeyRejected = "Der Routendienst hat den Schlüssel abgelehnt. Die Routensuche ist nicht verfügbar.";

    /// <summary>
    /// Hinweis, wenn der Routendienst nicht erreichbar ist.
    /// </summary>
    public const string RoutingUnreachable = "Der Routendienst ist nicht erreichbar. Bitte versuche es später erneut.";

    /// <summary>
    /// Hinweis, wenn der Routendienst ausgelastet oder das Kontingent aufgebraucht ist.
    /// </summary>
    public const string RoutingQuotaExceeded = "Der Routendienst ist gerade ausgelastet oder das Tageskontingent ist aufgebraucht. Bitte später erneut versuchen.";

    /// <summary>
    /// Hinweis, wenn keine Route berechnet werden konnte.
    /// </summary>
    public const string RoutingRouteNotFound = "Zwischen Start und Ziel konnte keine Route berechnet werden.";

    /// <summary>
    /// Hinweis, wenn der Routendienst die Anfrage abgelehnt hat.
    /// </summary>
    public const string RoutingRejected = "Der Routendienst hat die Anfrage abgelehnt. Bitte versuche es später erneut.";

    /// <summary>
    /// Hinweis, wenn die Antwort des Routendienstes nicht auswertbar war.
    /// </summary>
    public const string RoutingInvalidResponse = "Der Routendienst hat eine unerwartete Antwort geliefert.";

    /// <summary>
    /// Hinweis im Leerzustand der Routensuche.
    /// </summary>
    public const string RouteEmptyState = "Keine Tankstellen entlang der Route gefunden. Ein größerer Abstand zur Route kann helfen.";

    /// <summary>
    /// Fortschrittstext, während Start und Ziel aufgelöst werden.
    /// </summary>
    public const string RouteProgressPlaces = "Orte werden gesucht …";

    /// <summary>
    /// Fortschrittstext, während die Route berechnet wird.
    /// </summary>
    public const string RouteProgressRoute = "Route wird berechnet …";

    /// <summary>
    /// Beschriftung der Schaltfläche, die eine laufende Routensuche abbricht.
    /// </summary>
    public const string RouteCancelButton = "Suche abbrechen";

    /// <summary>
    /// Hinweis, nachdem der Anwender eine laufende Routensuche abgebrochen hat.
    /// </summary>
    public const string RouteCancelled = "Die Routensuche wurde abgebrochen.";

    /// <summary>
    /// Formatiert die Entfernung in der Routenliste („12,3 km ab Start“).
    /// </summary>
    /// <param name="alongKm">Die Position entlang der Route in Kilometern.</param>
    /// <returns>Der Text.</returns>
    public static string FormatRouteDistance(double alongKm)
    {
        return FormatDistance(alongKm) + " ab Start";
    }

    /// <summary>
    /// Formatiert den Abstand zur Route („0,8 km abseits der Route“).
    /// </summary>
    /// <param name="offsetKm">Der Abstand in Kilometern.</param>
    /// <returns>Der Text.</returns>
    public static string FormatRouteOffset(double offsetKm)
    {
        return FormatDistance(offsetKm) + " abseits der Route";
    }

    /// <summary>
    /// Formatiert die Zusammenfassung einer Routensuche („Von A nach B, 123 km, 4 Tankstellen im Abstand von 2 km zur Route“).
    /// </summary>
    /// <param name="startName">Der Name des Starts.</param>
    /// <param name="destinationName">Der Name des Ziels.</param>
    /// <param name="lengthKm">Die Länge der Route in Kilometern.</param>
    /// <param name="stationCount">Die Anzahl der Tankstellen.</param>
    /// <param name="distanceKm">Der Abstand zur Route in Kilometern.</param>
    /// <returns>Der Text.</returns>
    public static string FormatRouteSummary(string startName, string destinationName, double lengthKm, int stationCount, int distanceKm)
    {
        var stations = stationCount == 1 ? "1 Tankstelle" : $"{stationCount.ToString(German)} Tankstellen";
        return $"Von {startName} nach {destinationName}, {FormatRouteLength(lengthKm)}, {stations} im Abstand von {distanceKm.ToString(German)} km zur Route";
    }

    /// <summary>
    /// Formatiert die Länge einer Route: unter 10 km mit einer Nachkommastelle („8,0 km“, „0,4 km“), sonst in ganzen Kilometern („40 km“).
    /// </summary>
    /// <param name="lengthKm">Die Länge in Kilometern.</param>
    /// <returns>Der Text mit Einheit.</returns>
    public static string FormatRouteLength(double lengthKm)
    {
        return lengthKm < 9.95 ? $"{lengthKm.ToString("0.0", German)} km" : $"{lengthKm.ToString("0", German)} km";
    }

    /// <summary>
    /// Formatiert den Zugänglichkeitstext der Karte mit eingezeichneter Route.
    /// </summary>
    /// <param name="startName">Der Name des Starts.</param>
    /// <param name="destinationName">Der Name des Ziels.</param>
    /// <param name="stationCount">Die Anzahl der Tankstellen.</param>
    /// <returns>Der Text.</returns>
    public static string FormatRouteMapHint(string startName, string destinationName, int stationCount)
    {
        var stations = stationCount == 1 ? "1 Tankstelle" : $"{stationCount.ToString(German)} Tankstellen";
        return $"Route von {startName} nach {destinationName} eingezeichnet, {stations}";
    }

    /// <summary>
    /// Formatiert den Fortschrittstext einer laufenden Routensuche.
    /// </summary>
    /// <param name="progress">Der Fortschritt.</param>
    /// <returns>Der Text.</returns>
    public static string FormatRouteProgress(RouteSearchProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);
        return progress.Phase switch
        {
            RouteSearchPhase.ResolvingPlaces => RouteProgressPlaces,
            RouteSearchPhase.CalculatingRoute => RouteProgressRoute,
            _ => $"Tankstellen werden gesucht ({progress.Current.ToString(German)} von {progress.Total.ToString(German)}) …",
        };
    }

    /// <summary>
    /// Hinweis, wenn der Routingdienst die Strecke selbst als zu lang ablehnt.
    /// </summary>
    public const string RouteTooLongForService = "Die Route ist zu lang für den Routendienst. Bitte wähle ein näheres Ziel.";

    /// <summary>
    /// Formatiert den Hinweis, dass die Route mehr Preisabfragen bräuchte, als erlaubt sind, mit der durchsuchbaren Länge bei dem gewählten Abstand.
    /// </summary>
    /// <param name="limit">Die Angaben zur Grenze.</param>
    /// <returns>Der Text.</returns>
    public static string FormatRouteLimit(RouteLimit limit)
    {
        ArgumentNullException.ThrowIfNull(limit);
        // Abgerundet: Die genannte Länge darf nie größer sein als die tatsächlich durchsuchbare.
        var searchableKm = Math.Floor(limit.MaxSearchLengthKm);
        var advice = limit.DistanceKm > 1 ? "Wähle eine kürzere Strecke oder einen kleineren Abstand." : "Wähle eine kürzere Strecke.";
        return $"Die Route ist {FormatRouteLength(limit.RouteLengthKm)} lang. Bei {limit.DistanceKm.ToString(German)} km Abstand zur Route kann nur bis {searchableKm.ToString("0", German)} km Länge gesucht werden, "
            + "weil die Preisabfragen begrenzt sind. " + advice;
    }

    /// <summary>
    /// Liefert den Hinweistext zu einer ungültigen Start- oder Zieleingabe.
    /// </summary>
    /// <param name="isStart"><see langword="true"/> für den Start, <see langword="false"/> für das Ziel.</param>
    /// <param name="error">Der Grund.</param>
    /// <returns>Der Text; leer bei <see cref="AddressInputError.None"/>.</returns>
    public static string GetRouteInputMessage(bool isStart, AddressInputError error)
    {
        var message = GetAddressInputMessage(error);
        return message.Length == 0 ? string.Empty : (isStart ? "Start: " : "Ziel: ") + message;
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler des Routingdienstes (nie Roh-Ausnahmetexte).
    /// </summary>
    /// <param name="failure">Der Fehlergrund.</param>
    /// <returns>Der Text; leer bei <see cref="RoutingFailure.None"/>.</returns>
    public static string GetRoutingMessage(RoutingFailure failure)
    {
        return failure switch
        {
            RoutingFailure.None => string.Empty,
            RoutingFailure.ApiKeyMissing => RoutingUnavailable,
            RoutingFailure.EndpointNotConfigured => RoutingUnavailable,
            RoutingFailure.KeyRejected => RoutingKeyRejected,
            RoutingFailure.Unreachable => RoutingUnreachable,
            RoutingFailure.QuotaExceeded => RoutingQuotaExceeded,
            RoutingFailure.RouteNotFound => RoutingRouteNotFound,
            RoutingFailure.RouteTooLong => RouteTooLongForService,
            RoutingFailure.Rejected => RoutingRejected,
            _ => RoutingInvalidResponse,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Ergebnis der Routensuche.
    /// </summary>
    /// <param name="result">Das Ergebnis.</param>
    /// <returns>Der Text; leer bei <see cref="RouteSearchStatus.Success"/>.</returns>
    public static string GetRouteMessage(RouteSearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return result.Status switch
        {
            RouteSearchStatus.Success => string.Empty,
            RouteSearchStatus.StartNotFound => RouteStartNotFound,
            RouteSearchStatus.DestinationNotFound => RouteDestinationNotFound,
            RouteSearchStatus.GeocodingFailed => GetGeocodingMessage(result.GeocodingStatus, isOnline: true),
            RouteSearchStatus.SamePlace => RouteSamePlace,
            RouteSearchStatus.RoutingUnavailable => RoutingUnavailable,
            RouteSearchStatus.RoutingFailed => GetRoutingMessage(result.RoutingFailure),
            RouteSearchStatus.RouteTooLong => result.Limit is { } limit ? FormatRouteLimit(limit) : RouteTooLongForService,
            _ => GetFailureMessage(result.PriceFailure, hasStations: false),
        };
    }
}
