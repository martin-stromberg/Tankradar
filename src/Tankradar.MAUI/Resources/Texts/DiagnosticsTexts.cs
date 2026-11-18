namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte der Diagnose-Karte in den Optionen.
/// </summary>
public static class DiagnosticsTexts
{
    /// <summary>
    /// Überschrift der Karte „Diagnose“.
    /// </summary>
    public const string Heading = "Diagnose";

    /// <summary>
    /// Erklärtext der Karte: Zweck und Umfang des Diagnosepakets.
    /// </summary>
    public const string Hint = "Erzeugt auf Wunsch eine kleine Datei mit technischen Hinweisen für die Fehlersuche: App-Version, Betriebssystem, Warnungen und Fehler seit dem letzten Start.";

    /// <summary>
    /// Hinweis, was das Paket nicht enthält.
    /// </summary>
    public const string PrivacyHint = "Nicht enthalten sind API-Schlüssel, Standortdaten, Tankstellen- oder Suchbegriffe, Dateipfade, Fotos und deine Einträge. Die Datei wird nur lokal erzeugt; du entscheidest, ob und wohin sie geteilt wird.";

    /// <summary>
    /// Beschriftung der Schaltfläche, die die Paketinhalte als Vorschau zeigt.
    /// </summary>
    public const string PreviewText = "Inhalt anzeigen";

    /// <summary>
    /// Beschriftung der Schaltfläche zum Erstellen und Teilen des Pakets.
    /// </summary>
    public const string ShareText = "Paket erstellen und teilen";

    /// <summary>
    /// Titel des Teilen-Dialogs.
    /// </summary>
    public const string ShareTitle = "Diagnosepaket teilen";

    /// <summary>
    /// Statusmeldung nach dem Erstellen des Pakets.
    /// </summary>
    public const string CreatedMessage = "Das Diagnosepaket wurde erstellt.";

    /// <summary>
    /// Statusmeldung bei einem Fehler beim Erstellen oder Teilen.
    /// </summary>
    public const string FailedMessage = "Das Diagnosepaket konnte nicht erstellt oder geteilt werden.";

    /// <summary>
    /// Überschrift über der Inhaltsvorschau.
    /// </summary>
    public const string PreviewHeading = "Inhalt des Pakets";
}
