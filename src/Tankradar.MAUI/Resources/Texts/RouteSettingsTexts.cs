using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Deutsche UI-Texte der Karte „Routensuche“ in den Optionen (Schlüssel eintragen, prüfen, entfernen und die Routensuche aktivieren).
/// </summary>
public static class RouteSettingsTexts
{
    /// <summary>
    /// Die Adresse, unter der man einen kostenlosen Schlüssel bei OpenRouteService beantragt.
    /// </summary>
    public const string SignupUrl = "https://openrouteservice.org/dev/#/signup";

    /// <summary>
    /// Überschrift der Karte.
    /// </summary>
    public const string Heading = "Routensuche";

    /// <summary>
    /// Erläuterung zur Karte: eigener Schlüssel, Kosten und Datenweitergabe.
    /// </summary>
    public const string Hint = "Die Suche entlang einer Route braucht einen eigenen, kostenlosen Schlüssel von OpenRouteService. Er bleibt auf diesem Gerät in der sicheren Ablage, wird nie angezeigt und ist nicht Teil der Sicherung. "
        + "Ist die Routensuche aktiviert, werden Start und Ziel deiner Suchen an OpenRouteService übertragen.";

    /// <summary>
    /// Hinweis, wo man den Schlüssel erhält.
    /// </summary>
    public const string SignupHint = "Einen Schlüssel erhältst du nach einer kostenlosen Anmeldung bei OpenRouteService: openrouteservice.org/dev/#/signup";

    /// <summary>
    /// Beschriftung der Schaltfläche, die die Anmeldeseite öffnet.
    /// </summary>
    public const string SignupButton = "Schlüssel bei OpenRouteService beantragen";

    /// <summary>
    /// Platzhalter des verdeckten Schlüsselfelds.
    /// </summary>
    public const string KeyPlaceholder = "OpenRouteService-Schlüssel";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Speichern.
    /// </summary>
    public const string SaveButton = "Schlüssel speichern";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Prüfen.
    /// </summary>
    public const string CheckButton = "Schlüssel prüfen";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Entfernen.
    /// </summary>
    public const string RemoveButton = "Schlüssel entfernen";

    /// <summary>
    /// Beschriftung des Schalters.
    /// </summary>
    public const string EnableLabel = "Routensuche aktivieren";

    /// <summary>
    /// Hinweis am Schalter, solange kein Schlüssel gespeichert ist.
    /// </summary>
    public const string EnableNeedsKey = "Der Schalter ist erst bedienbar, wenn ein Schlüssel gespeichert ist.";

    /// <summary>
    /// Status, wenn ein Schlüssel gespeichert ist.
    /// </summary>
    public const string KeyPresent = "Schlüssel: gespeichert";

    /// <summary>
    /// Status, wenn kein Schlüssel gespeichert ist.
    /// </summary>
    public const string KeyMissing = "Schlüssel: nicht gespeichert";

    /// <summary>
    /// Rückmeldung nach dem Speichern.
    /// </summary>
    public const string Saved = "Der Schlüssel wurde gespeichert. Du kannst die Routensuche jetzt aktivieren.";

    /// <summary>
    /// Rückmeldung, wenn das Speichern scheiterte.
    /// </summary>
    public const string SaveFailed = "Der Schlüssel konnte nicht gespeichert werden.";

    /// <summary>
    /// Rückmeldung nach dem Entfernen.
    /// </summary>
    public const string Removed = "Der Schlüssel wurde entfernt. Die Routensuche ist ausgeschaltet.";

    /// <summary>
    /// Rückmeldung, wenn das Entfernen scheiterte.
    /// </summary>
    public const string RemoveFailed = "Der Schlüssel konnte nicht entfernt werden. Die Routensuche ist ausgeschaltet.";

    /// <summary>
    /// Rückmeldung, wenn der Schalter nicht geändert werden konnte.
    /// </summary>
    public const string SwitchFailed = "Die Einstellung konnte nicht gespeichert werden.";

    /// <summary>
    /// Rückmeldung, wenn ohne Schlüssel geprüft werden soll.
    /// </summary>
    public const string NothingToCheck = "Bitte zuerst einen Schlüssel eingeben.";

    /// <summary>
    /// Rückmeldung während der Prüfung.
    /// </summary>
    public const string Checking = "Der Schlüssel wird geprüft …";

    /// <summary>
    /// Rückmeldung, wenn die Prüfung wegen fehlender Verbindung nicht möglich ist.
    /// </summary>
    public const string CheckOffline = "Keine Internetverbindung: Der Schlüssel kann gerade nicht geprüft werden.";

    /// <summary>
    /// Zusatz, wenn ein geprüfter Schlüssel noch nicht gespeichert ist.
    /// </summary>
    public const string NotSavedYet = " Er ist noch nicht gespeichert.";

    /// <summary>
    /// Liefert den Hinweis zu einer unzulässigen Schreibweise des eingegebenen Schlüssels.
    /// </summary>
    /// <param name="error">Der Grund.</param>
    /// <returns>Der Text; leer bei <see cref="RoutingKeyError.None"/>.</returns>
    public static string GetKeyErrorMessage(RoutingKeyError error)
    {
        return error switch
        {
            RoutingKeyError.None => string.Empty,
            RoutingKeyError.Empty => "Bitte einen Schlüssel eingeben.",
            RoutingKeyError.TooLong => $"Der Schlüssel ist zu lang (höchstens {RoutingKeyFormat.MaxLength} Zeichen).",
            _ => "Der Schlüssel enthält Leerzeichen oder Sonderzeichen. Bitte genau so einfügen, wie er angezeigt wurde.",
        };
    }

    /// <summary>
    /// Liefert die verständliche Rückmeldung zum Ergebnis einer Schlüsselprüfung.
    /// </summary>
    /// <param name="result">Das Ergebnis.</param>
    /// <returns>Der Text.</returns>
    public static string GetCheckMessage(RoutingKeyCheckResult result)
    {
        return result switch
        {
            RoutingKeyCheckResult.Valid => "Der Schlüssel ist gültig.",
            RoutingKeyCheckResult.Invalid => "Der Schlüssel wurde abgelehnt. Bitte Eingabe und Schlüssel bei OpenRouteService prüfen.",
            RoutingKeyCheckResult.Unreachable => "Der Routendienst ist nicht erreichbar. Bitte Internetverbindung prüfen und später erneut versuchen.",
            RoutingKeyCheckResult.QuotaExceeded => "Das Kontingent ist aufgebraucht oder der Dienst ausgelastet. Der Schlüssel konnte deshalb nicht geprüft werden. Bitte später erneut versuchen.",
            _ => "Die Prüfung ist gerade nicht möglich. Bitte später erneut versuchen.",
        };
    }
}
