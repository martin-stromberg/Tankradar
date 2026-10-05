using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte zu Preisdaten, Datenquelle und Hinweisen.
/// </summary>
public static class PriceTexts
{
    /// <summary>
    /// Überschrift der Karte „Datenquelle“ in den Optionen.
    /// </summary>
    public const string DataSourceHeading = "Datenquelle";

    /// <summary>
    /// Quellenangabe gemäß Lizenz CC BY 4.0.
    /// </summary>
    public const string Attribution = "Daten: Tankerkönig / MTS-K";

    /// <summary>
    /// Erläuterung zur Lizenz der Preisdaten.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public const string AttributionLicense = "Die Kraftstoffpreise stammen von der Markttransparenzstelle für Kraftstoffe (MTS-K) und werden über Tankerkönig bereitgestellt (Lizenz CC BY 4.0).";

    /// <summary>
    /// Schaltfläche zum Prüfen der Verbindung zum Preisdienst.
    /// </summary>
    public const string CheckButton = "Verbindung zum Preisdienst prüfen";

    /// <summary>
    /// Status, solange die Prüfung läuft.
    /// </summary>
    public const string CheckRunning = "Verbindung wird geprüft …";

    /// <summary>
    /// Status, wenn ein API-Schlüssel hinterlegt ist.
    /// </summary>
    public const string KeyPresent = "API-Schlüssel: hinterlegt";

    /// <summary>
    /// Status, wenn kein API-Schlüssel hinterlegt ist.
    /// </summary>
    public const string KeyMissing = "API-Schlüssel: nicht hinterlegt";

    /// <summary>
    /// Hinweis „Preis unbestätigt“.
    /// </summary>
    public const string PriceUnconfirmed = "Preis unbestätigt";

    /// <summary>
    /// Hinweis „Automatentankstelle“.
    /// </summary>
    public const string AutomatedStation = "Automatentankstelle";

    /// <summary>
    /// Liefert die Statusmeldung zum Ergebnis der Verbindungsprüfung.
    /// </summary>
    /// <param name="failure">Das Ergebnis der Prüfung.</param>
    /// <returns>Die Meldung für den Anwender.</returns>
    public static string GetCheckMessage(PriceFailure failure)
    {
        return failure switch
        {
            PriceFailure.None => "Der Preisdienst ist erreichbar.",
            PriceFailure.Offline => "Keine Netzverbindung. Es werden die zuletzt bekannten Preise verwendet.",
            PriceFailure.Unreachable => "Der Preisdienst ist nicht erreichbar. Es werden die zuletzt bekannten Preise verwendet.",
            PriceFailure.ApiKeyMissing => "Es ist kein API-Schlüssel hinterlegt. Es werden die zuletzt bekannten Preise verwendet.",
            PriceFailure.Rejected => "Der Preisdienst hat die Anfrage abgelehnt. Bitte den API-Schlüssel prüfen.",
            PriceFailure.InvalidResponse => "Der Preisdienst hat eine unerwartete Antwort geliefert.",
            _ => failure.ToString(),
        };
    }
}
