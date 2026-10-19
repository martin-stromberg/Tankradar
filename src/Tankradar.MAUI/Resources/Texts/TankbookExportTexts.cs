using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche Texte des Exports der Tank- und Ladevorgänge (Bereich auf der Seite und PDF-Bericht).
/// </summary>
public static class TankbookExportTexts
{
    /// <summary>
    /// Überschrift des Bereichs.
    /// </summary>
    public const string Heading = "Export";

    /// <summary>
    /// Hinweis zur Weitergabe und Unverschlüsselung.
    /// </summary>
    public const string PrivacyNotice = "Der Export enthält deine Fahrdaten, Tankstellen und Notizen unverschlüsselt, aber keine Belegfotos. Die Datei verlässt dieses Gerät nur, wenn du sie über „Teilen“ oder „Speichern“ weitergibst.";

    /// <summary>
    /// Beschriftung der Formatwahl.
    /// </summary>
    public const string FormatLabel = "Format";

    /// <summary>
    /// Beschriftung der Zeitraumwahl.
    /// </summary>
    public const string RangeLabel = "Zeitraum";

    /// <summary>
    /// Beschriftung des Starttags des eigenen Zeitraums.
    /// </summary>
    public const string FromLabel = "Von";

    /// <summary>
    /// Beschriftung des Endtags des eigenen Zeitraums.
    /// </summary>
    public const string ToLabel = "Bis";

    /// <summary>
    /// Beschriftung der Schaltfläche.
    /// </summary>
    public const string StartButton = "Exportieren";

    /// <summary>
    /// Titel der Systemfunktion Teilen/Speichern.
    /// </summary>
    public const string ShareTitle = "Tankbuch-Export";

    /// <summary>
    /// Text für „alle Vorgänge“ (Chip und Zeitraumangabe im Bericht).
    /// </summary>
    public const string RangeAllText = "Alle Vorgänge";

    /// <summary>
    /// Meldung, wenn der Zeitraum keine Vorgänge enthält.
    /// </summary>
    public const string NoEntriesInRange = "Im gewählten Zeitraum gibt es keine Vorgänge.";

    /// <summary>
    /// Meldung, wenn das Fahrzeug noch keine Vorgänge hat.
    /// </summary>
    public const string NoEntriesAtAll = "Für dieses Fahrzeug gibt es noch keine Vorgänge zum Exportieren.";

    /// <summary>
    /// Meldung, wenn das Fahrzeug nicht mehr existiert.
    /// </summary>
    public const string VehicleNotFound = "Das Fahrzeug wurde nicht gefunden.";

    /// <summary>
    /// Fehlermeldung, wenn der Starttag nach dem Endtag liegt.
    /// </summary>
    public const string StartAfterEnd = "Das Startdatum liegt nach dem Enddatum.";

    /// <summary>
    /// Fehlermeldung, wenn der Endtag in der Zukunft liegt.
    /// </summary>
    public const string EndInFuture = "Das Enddatum liegt in der Zukunft.";

    /// <summary>
    /// Meldung des Dienstes bei einem ungültigen Zeitraum (die Oberfläche prüft vorher und nennt den genauen Fehler).
    /// </summary>
    public const string InvalidRange = "Der Zeitraum ist ungültig.";

    /// <summary>
    /// Beschriftung der Notiz in der Vorgangstabelle des Berichts.
    /// </summary>
    public const string ReportNote = "Notiz";

    /// <summary>
    /// Fehlermeldung, wenn die Datei nicht erzeugt werden konnte.
    /// </summary>
    public const string CreateFailed = "Der Export konnte nicht erstellt werden.";

    /// <summary>
    /// Fehlermeldung, wenn die Datei nicht weitergegeben werden konnte.
    /// </summary>
    public const string ShareFailed = "Der Export konnte nicht weitergegeben werden.";

    /// <summary>
    /// Titel des PDF-Berichts.
    /// </summary>
    public const string ReportTitle = "Tankbuch";

    /// <summary>
    /// Beschriftung des Fahrzeugs im Bericht.
    /// </summary>
    public const string ReportVehicle = "Fahrzeug";

    /// <summary>
    /// Beschriftung des Zeitraums im Bericht.
    /// </summary>
    public const string ReportRange = "Zeitraum";

    /// <summary>
    /// Beschriftung des Erstellungsdatums im Bericht.
    /// </summary>
    public const string ReportCreated = "Erstellt am";

    /// <summary>
    /// Beschriftung der Zahl der Vorgänge im Bericht.
    /// </summary>
    public const string ReportEntryCount = "Vorgänge im Bericht";

    /// <summary>
    /// Überschrift der Kennzahlen je Art.
    /// </summary>
    public const string ReportKeyFigures = "Kennzahlen";

    /// <summary>
    /// Überschrift der Monatskosten.
    /// </summary>
    public const string ReportMonthlyCosts = "Monatskosten";

    /// <summary>
    /// Überschrift der Vorgangstabelle.
    /// </summary>
    public const string ReportEntries = "Vorgänge";

    /// <summary>
    /// Beschriftung der Gesamtkosten im Bericht.
    /// </summary>
    public const string ReportTotalCost = "Gesamtkosten";

    /// <summary>
    /// Beschriftung der Strecke im Bericht.
    /// </summary>
    public const string ReportDistance = "Strecke der berechneten Intervalle";

    /// <summary>
    /// Hinweis zu den Kennzahlen im Zeitraum.
    /// </summary>
    public const string ReportRangeNote = "Die Kennzahlen umfassen die Intervalle, deren Abschluss im Zeitraum liegt; die Strecke zum Vorgänger vor dem Zeitraum zählt mit.";

    /// <summary>
    /// Spaltenüberschrift Datum.
    /// </summary>
    public const string ColumnDate = "Datum";

    /// <summary>
    /// Spaltenüberschrift Tankstelle.
    /// </summary>
    public const string ColumnStation = "Tankstelle";

    /// <summary>
    /// Spaltenüberschrift Menge.
    /// </summary>
    public const string ColumnQuantity = "Menge";

    /// <summary>
    /// Spaltenüberschrift Preis pro Einheit.
    /// </summary>
    public const string ColumnUnitPrice = "Preis";

    /// <summary>
    /// Spaltenüberschrift Betrag.
    /// </summary>
    public const string ColumnAmount = "Betrag";

    /// <summary>
    /// Spaltenüberschrift Kilometerstand.
    /// </summary>
    public const string ColumnOdometer = "km-Stand";

    /// <summary>
    /// Spaltenüberschrift Verbrauch.
    /// </summary>
    public const string ColumnConsumption = "Verbrauch";

    /// <summary>
    /// Fußnote zu Verbrauchswerten ab dem Startstand.
    /// </summary>
    public const string FromStartFootnote = "* ab Startstand berechnet, angenommen wird ein voller Tank bzw. Akku beim Start.";

    /// <summary>
    /// Platzhalter für einen fehlenden Wert.
    /// </summary>
    public const string NoValue = "–";

    /// <summary>
    /// Liefert die Beschriftung eines Formats.
    /// </summary>
    /// <param name="format">Das Format.</param>
    /// <returns>„CSV“ oder „PDF“.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Das Format ist kein bekannter Wert.</exception>
    public static string GetFormatLabel(TankbookExportFormat format)
    {
        return format switch
        {
            TankbookExportFormat.Csv => "CSV",
            TankbookExportFormat.Pdf => "PDF",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unbekanntes Format."),
        };
    }

    /// <summary>
    /// Liefert die Dateiendung eines Formats ohne Punkt.
    /// </summary>
    /// <param name="format">Das Format.</param>
    /// <returns>„csv“ oder „pdf“.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Das Format ist kein bekannter Wert.</exception>
    public static string GetFileExtension(TankbookExportFormat format)
    {
        return format switch
        {
            TankbookExportFormat.Csv => "csv",
            TankbookExportFormat.Pdf => "pdf",
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unbekanntes Format."),
        };
    }

    /// <summary>
    /// Liefert die Beschriftung einer Zeitraumart.
    /// </summary>
    /// <param name="kind">Die Zeitraumart.</param>
    /// <returns>Der Text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Die Zeitraumart ist kein bekannter Wert.</exception>
    public static string GetRangeLabel(TankbookExportRangeKind kind)
    {
        return kind switch
        {
            TankbookExportRangeKind.All => RangeAllText,
            TankbookExportRangeKind.ThisYear => "Dieses Jahr",
            TankbookExportRangeKind.LastYear => "Letztes Jahr",
            TankbookExportRangeKind.Last12Months => "Letzte 12 Monate",
            TankbookExportRangeKind.Custom => "Eigener Zeitraum",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unbekannte Zeitraumart."),
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler des Zeitraums.
    /// </summary>
    /// <param name="error">Der Fehler.</param>
    /// <returns>Der Text; leer bei <see cref="TankbookExportRangeError.None"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Der Fehler ist kein bekannter Wert.</exception>
    public static string GetRangeErrorMessage(TankbookExportRangeError error)
    {
        return error switch
        {
            TankbookExportRangeError.None => string.Empty,
            TankbookExportRangeError.StartAfterEnd => StartAfterEnd,
            TankbookExportRangeError.EndInFuture => EndInFuture,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error, "Unbekannter Fehler."),
        };
    }

    /// <summary>
    /// Liefert die Meldung für ein Ergebnis ohne Datei.
    /// </summary>
    /// <param name="status">Das Ergebnis.</param>
    /// <param name="isAllRange">Gibt an, ob alle Vorgänge ohne Einschränkung gewählt waren.</param>
    /// <returns>Der Text; leer bei <see cref="TankbookExportStatus.Exported"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Das Ergebnis ist kein bekannter Wert.</exception>
    public static string GetStatusMessage(TankbookExportStatus status, bool isAllRange)
    {
        return status switch
        {
            TankbookExportStatus.Exported => string.Empty,
            TankbookExportStatus.NoEntries => isAllRange ? NoEntriesAtAll : NoEntriesInRange,
            TankbookExportStatus.VehicleNotFound => VehicleNotFound,
            TankbookExportStatus.InvalidRange => InvalidRange,
            TankbookExportStatus.Failed => CreateFailed,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unbekanntes Ergebnis."),
        };
    }

    /// <summary>
    /// Formatiert die Meldung nach erfolgreichem Export, z. B. „Export bereit: Tankbuch_Golf_2026-10-19.csv.“
    /// </summary>
    /// <param name="fileName">Der Dateiname.</param>
    /// <returns>Der Text.</returns>
    public static string FormatReady(string fileName)
    {
        return $"Export bereit: {fileName}.";
    }

    /// <summary>
    /// Formatiert die Seitenzahl der Fußzeile, z. B. „Seite 1 von 3“.
    /// </summary>
    /// <param name="page">Die Seitennummer.</param>
    /// <param name="total">Die Zahl der Seiten.</param>
    /// <returns>Der Text.</returns>
    public static string FormatPageFooter(int page, int total)
    {
        return $"Seite {page} von {total}";
    }

    /// <summary>
    /// Formatiert den Hinweis der Schaltfläche für die Sprachausgabe, z. B. „Fahrzeug „Golf“, CSV, Alle Vorgänge“.
    /// </summary>
    /// <param name="vehicleName">Der Name des Fahrzeugs.</param>
    /// <param name="format">Das gewählte Format.</param>
    /// <param name="range">Die gewählte Zeitraumart.</param>
    /// <returns>Der Text.</returns>
    public static string FormatStartHint(string vehicleName, TankbookExportFormat format, TankbookExportRangeKind range)
    {
        return $"Fahrzeug „{vehicleName}“, {GetFormatLabel(format)}, {GetRangeLabel(range)}";
    }
}
