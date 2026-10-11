using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte und Formatierungen (de-DE) der Tank- und Ladevorgänge.
/// </summary>
public static class TankbookEntryTexts
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Titel der Seite der Vorgänge eines Fahrzeugs.
    /// </summary>
    public const string PageTitle = "Tank- und Ladevorgänge";

    /// <summary>
    /// Einleitung der Seite.
    /// </summary>
    public const string Intro = "Alle Angaben und Belegfotos bleiben auf diesem Gerät.";

    /// <summary>
    /// Überschrift der Liste der Vorgänge.
    /// </summary>
    public const string EntriesHeading = "Vorgänge, neueste zuerst";

    /// <summary>
    /// Hinweis, dass es noch keine Vorgänge gibt.
    /// </summary>
    public const string NoEntries = "Noch keine Tank- oder Ladevorgänge. Erfasse über „Vorgang erfassen“ den ersten.";

    /// <summary>
    /// Beschriftung „Vorgang erfassen“.
    /// </summary>
    public const string AddEntry = "Vorgang erfassen";

    /// <summary>
    /// Überschrift des Formulars beim Erfassen.
    /// </summary>
    public const string NewEntryTitle = "Neuer Vorgang";

    /// <summary>
    /// Überschrift des Formulars beim Korrigieren.
    /// </summary>
    public const string EditEntryTitle = "Vorgang bearbeiten";

    /// <summary>
    /// Beschriftung des Datumsfelds.
    /// </summary>
    public const string DateLabel = "Datum";

    /// <summary>
    /// Beschriftung des Uhrzeitfelds.
    /// </summary>
    public const string TimeLabel = "Uhrzeit";

    /// <summary>
    /// Beschriftung der Art des Vorgangs.
    /// </summary>
    public const string KindLabel = "Art";

    /// <summary>
    /// Beschriftung des Felds für die Tankstelle bzw. den Ladeort.
    /// </summary>
    public const string StationLabel = "Tankstelle oder Ladeort";

    /// <summary>
    /// Platzhalter des Felds für die Tankstelle bzw. den Ladeort.
    /// </summary>
    public const string StationPlaceholder = "z. B. Alpha Tankstelle oder Zuhause";

    /// <summary>
    /// Überschrift der Auswahl bekannter Tankstellen.
    /// </summary>
    public const string KnownStationsLabel = "Bekannte Tankstellen";

    /// <summary>
    /// Beschriftung des Mengenfelds.
    /// </summary>
    public const string QuantityLabel = "Menge";

    /// <summary>
    /// Beschriftung des Felds für den Gesamtbetrag.
    /// </summary>
    public const string TotalAmountLabel = "Gesamtbetrag";

    /// <summary>
    /// Erklärung der drei sich ergänzenden Werte.
    /// </summary>
    public const string AmountsExplanation = "Zwei der drei Werte Menge, Preis und Gesamtbetrag genügen, der dritte wird berechnet.";

    /// <summary>
    /// Beschriftung des Kilometerstands.
    /// </summary>
    public const string OdometerLabel = "Kilometerstand";

    /// <summary>
    /// Beschriftung des Schalters „Teilbetankung“.
    /// </summary>
    public const string PartialLabel = "Teilbetankung – nicht voll getankt bzw. geladen";

    /// <summary>
    /// Kennzeichnung einer Teilbetankung in der Liste.
    /// </summary>
    public const string PartialBadge = "Teilbetankung";

    /// <summary>
    /// Beschriftung der Notiz.
    /// </summary>
    public const string NoteLabel = "Notiz, optional";

    /// <summary>
    /// Beschriftung des Belegfotos.
    /// </summary>
    public const string PhotoLabel = "Belegfoto, optional";

    /// <summary>
    /// Beschriftung „Foto auswählen“.
    /// </summary>
    public const string PickPhoto = "Foto auswählen";

    /// <summary>
    /// Hinweis der Schaltfläche „Foto auswählen“ für die Sprachausgabe.
    /// </summary>
    public const string PickPhotoHint = "Belegfoto aus der Fotomediathek übernehmen";

    /// <summary>
    /// Beschriftung „Foto aufnehmen“.
    /// </summary>
    public const string CapturePhoto = "Foto aufnehmen";

    /// <summary>
    /// Hinweis der Schaltfläche „Foto aufnehmen“ für die Sprachausgabe.
    /// </summary>
    public const string CapturePhotoHint = "Belegfoto mit der Kamera aufnehmen";

    /// <summary>
    /// Beschriftung „Foto entfernen“.
    /// </summary>
    public const string RemovePhoto = "Foto entfernen";

    /// <summary>
    /// Hinweis der Schaltfläche „Foto entfernen“ für die Sprachausgabe.
    /// </summary>
    public const string RemovePhotoHint = "Belegfoto von diesem Vorgang entfernen";

    /// <summary>
    /// Beschreibung der Vorschau des Belegfotos für die Sprachausgabe.
    /// </summary>
    public const string PhotoPreviewDescription = "Vorschau des Belegfotos";

    /// <summary>
    /// Beschriftung „Beleg anzeigen“.
    /// </summary>
    public const string ShowReceipt = "Beleg anzeigen";

    /// <summary>
    /// Beschriftung „Beleg ausblenden“.
    /// </summary>
    public const string HideReceipt = "Beleg ausblenden";

    /// <summary>
    /// Meldung, wenn die Datei eines Belegfotos fehlt.
    /// </summary>
    public const string ReceiptMissing = "Das Belegfoto ist nicht mehr vorhanden.";

    /// <summary>
    /// Hinweis der Schaltfläche „Abbrechen“ der Löschrückfrage eines Vorgangs für die Sprachausgabe.
    /// </summary>
    public const string CancelDeleteHint = "Vorgang behalten";

    /// <summary>
    /// Meldung, wenn das Fahrzeug nicht mehr existiert.
    /// </summary>
    public const string VehicleNotFound = "Das Fahrzeug gibt es nicht mehr.";

    /// <summary>
    /// Fehlermeldung beim Laden der Vorgänge.
    /// </summary>
    public const string LoadFailed = "Die Tank- und Ladevorgänge konnten nicht geladen werden.";

    /// <summary>
    /// Fehlermeldung, wenn ein Foto nicht übernommen werden konnte.
    /// </summary>
    public const string PhotoFailed = "Das Foto konnte nicht übernommen werden.";

    /// <summary>
    /// Liefert die Beschriftung einer Art.
    /// </summary>
    /// <param name="kind">Die Art.</param>
    /// <returns>„Kraftstoff“ oder „Strom“.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Die Art ist kein bekannter Wert.</exception>
    public static string GetKindLabel(TankbookEntryKind kind)
    {
        return kind switch
        {
            TankbookEntryKind.Fuel => "Kraftstoff",
            TankbookEntryKind.Electricity => "Strom",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unbekannte Art."),
        };
    }

    /// <summary>
    /// Liefert die Einheit der Menge: „L“ bei Kraftstoff, „kWh“ bei Strom.
    /// </summary>
    /// <param name="kind">Die Art.</param>
    /// <returns>Die Einheit.</returns>
    public static string GetQuantityUnit(TankbookEntryKind kind)
    {
        return kind == TankbookEntryKind.Electricity ? "kWh" : "L";
    }

    /// <summary>
    /// Liefert die Einheit des Preises pro Einheit: „€/L“ bzw. „€/kWh“.
    /// </summary>
    /// <param name="kind">Die Art.</param>
    /// <returns>Die Einheit.</returns>
    public static string GetUnitPriceUnit(TankbookEntryKind kind)
    {
        return $"€/{GetQuantityUnit(kind)}";
    }

    /// <summary>
    /// Liefert die Beschriftung des Preisfelds: „Preis pro Liter“ bzw. „Preis pro kWh“.
    /// </summary>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Text.</returns>
    public static string GetUnitPriceLabel(TankbookEntryKind kind)
    {
        return kind == TankbookEntryKind.Electricity ? "Preis pro kWh" : "Preis pro Liter";
    }

    /// <summary>
    /// Liefert die Einheit des Verbrauchs: „L/100 km“ bzw. „kWh/100 km“.
    /// </summary>
    /// <param name="kind">Die Art.</param>
    /// <returns>Die Einheit.</returns>
    public static string GetConsumptionUnit(TankbookEntryKind kind)
    {
        return kind == TankbookEntryKind.Electricity ? TankbookTexts.ElectricConsumptionUnit : TankbookTexts.LiterConsumptionUnit;
    }

    /// <summary>
    /// Formatiert einen Betrag in Euro, z. B. „74,36 €“.
    /// </summary>
    /// <param name="amount">Der Betrag.</param>
    /// <returns>Der Text.</returns>
    public static string FormatAmount(decimal amount)
    {
        return string.Create(German, $"{amount:N2} €");
    }

    /// <summary>
    /// Formatiert einen Preis pro Einheit mit zwei bis drei Nachkommastellen, z. B. „1,859 €/L“ oder „0,59 €/kWh“.
    /// </summary>
    /// <param name="unitPrice">Der Preis.</param>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Text.</returns>
    public static string FormatUnitPrice(decimal unitPrice, TankbookEntryKind kind)
    {
        return string.Create(German, $"{unitPrice:#,##0.00#} {GetUnitPriceUnit(kind)}");
    }

    /// <summary>
    /// Formatiert eine Menge mit zwei bis drei Nachkommastellen, z. B. „40,00 L“ oder „12,345 kWh“.
    /// </summary>
    /// <param name="quantity">Die Menge.</param>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Text.</returns>
    public static string FormatQuantity(decimal quantity, TankbookEntryKind kind)
    {
        return string.Create(German, $"{quantity:#,##0.00#} {GetQuantityUnit(kind)}");
    }

    /// <summary>
    /// Formatiert einen Kilometerstand oder eine Strecke mit Tausenderpunkten, z. B. „10.500 km“.
    /// </summary>
    /// <param name="kilometers">Die Kilometer.</param>
    /// <returns>Der Text.</returns>
    public static string FormatKilometers(int kilometers)
    {
        return string.Create(German, $"{kilometers:N0} {TankbookTexts.KilometerUnit}");
    }

    /// <summary>
    /// Formatiert einen Verbrauch mit zwei Nachkommastellen, z. B. „6,00 L/100 km“.
    /// </summary>
    /// <param name="consumption">Der Verbrauch pro 100 km.</param>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Text.</returns>
    public static string FormatConsumption(decimal consumption, TankbookEntryKind kind)
    {
        return string.Create(German, $"{consumption:N2} {GetConsumptionUnit(kind)}");
    }

    /// <summary>
    /// Formatiert Datum und Uhrzeit eines Vorgangs, z. B. „18.05.2026, 14:30“.
    /// </summary>
    /// <param name="occurredAt">Der Zeitpunkt.</param>
    /// <returns>Der Text.</returns>
    public static string FormatDateTime(DateTime occurredAt)
    {
        return occurredAt.ToString("dd.MM.yyyy, HH:mm", German);
    }

    /// <summary>
    /// Formatiert die Mengenzeile eines Eintrags, z. B. „Menge 40,00 L“.
    /// </summary>
    /// <param name="entry">Der Vorgang.</param>
    /// <returns>Der Text.</returns>
    public static string FormatQuantityLine(TankbookEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return $"{QuantityLabel} {FormatQuantity(entry.Quantity, entry.Kind)}";
    }

    /// <summary>
    /// Formatiert die Streckenzeile eines Eintrags, z. B. „Strecke 500 km“.
    /// </summary>
    /// <param name="distanceKm">Die Strecke.</param>
    /// <returns>Der Text.</returns>
    public static string FormatDistanceLine(int distanceKm)
    {
        return $"Strecke {FormatKilometers(distanceKm)}";
    }

    /// <summary>
    /// Liefert die Verbrauchszeile eines Eintrags: den Verbrauch, z. B. „Verbrauch 6,00 L/100 km“, oder den Grund, warum es keinen gibt.
    /// </summary>
    /// <param name="overview">Der Vorgang mit Berechnungsergebnis.</param>
    /// <returns>Der Text.</returns>
    public static string GetConsumptionLine(TankbookEntryOverview overview)
    {
        ArgumentNullException.ThrowIfNull(overview);
        return overview.ConsumptionState switch
        {
            TankbookConsumptionState.Calculated when overview.Consumption is { } consumption => $"Verbrauch {FormatConsumption(consumption, overview.Entry.Kind)}",
            TankbookConsumptionState.Partial => "Teilbetankung: zählt beim nächsten vollen Vorgang mit",
            TankbookConsumptionState.NoPreviousFullEntry => "Noch kein Verbrauch: erster voller Vorgang",
            TankbookConsumptionState.NoDistance => "Kein Verbrauch: keine Strecke seit dem letzten vollen Vorgang",
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Formatiert eine Notiz in Anführungszeichen, z. B. „„Urlaubsfahrt““.
    /// </summary>
    /// <param name="note">Die Notiz.</param>
    /// <returns>Der Text; leer ohne Notiz.</returns>
    public static string FormatNote(string? note)
    {
        return string.IsNullOrWhiteSpace(note) ? string.Empty : $"„{note.Trim()}“";
    }

    /// <summary>
    /// Formatiert den Rechenhinweis für den berechneten dritten Wert, z. B. „Ergibt 74,36 €“.
    /// </summary>
    /// <param name="field">Der berechnete Wert.</param>
    /// <param name="value">Das Ergebnis.</param>
    /// <param name="kind">Die Art.</param>
    /// <returns>Der Text.</returns>
    public static string FormatComputedHint(AmountField field, decimal value, TankbookEntryKind kind)
    {
        var text = field switch
        {
            AmountField.Quantity => FormatQuantity(value, kind),
            AmountField.UnitPrice => FormatUnitPrice(value, kind),
            AmountField.TotalAmount => FormatAmount(value),
            _ => string.Empty,
        };
        return $"Ergibt {text}";
    }

    /// <summary>
    /// Formatiert den Platzhalter des Kilometerstands; nennt den letzten Stand, z. B. „Letzter Stand 10.500 km“.
    /// </summary>
    /// <param name="lastOdometerKm">Der letzte bekannte Kilometerstand.</param>
    /// <returns>Der Text.</returns>
    public static string FormatOdometerPlaceholder(int lastOdometerKm)
    {
        return $"Letzter Stand {FormatKilometers(lastOdometerKm)}";
    }

    /// <summary>
    /// Formatiert eine Menge oder einen Preis für das Eingabefeld mit Dezimalkomma, z. B. „40,00“ oder „1,859“.
    /// </summary>
    /// <param name="value">Der Wert.</param>
    /// <returns>Der Text.</returns>
    public static string FormatDecimalInput(decimal value)
    {
        return value.ToString("0.00#", German);
    }

    /// <summary>
    /// Formatiert einen Gesamtbetrag für das Eingabefeld mit Dezimalkomma, z. B. „74,36“.
    /// </summary>
    /// <param name="value">Der Betrag.</param>
    /// <returns>Der Text.</returns>
    public static string FormatAmountInput(decimal value)
    {
        return value.ToString("0.00", German);
    }

    /// <summary>
    /// Formatiert die Bezeichnung eines Vorgangs, z. B. „Vorgang vom 18.05.2026, 14:30, Alpha Tankstelle“ (Hinweis von „Bearbeiten“ und „Löschen“).
    /// </summary>
    /// <param name="entry">Der Vorgang.</param>
    /// <returns>Der Text.</returns>
    public static string FormatEntryHint(TankbookEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return $"Vorgang vom {FormatDateTime(entry.OccurredAt)}, {entry.StationName}";
    }

    /// <summary>
    /// Formatiert den Hinweis der Bestätigung der Löschrückfrage, z. B. „Vorgang vom 18.05.2026, 14:30, Alpha Tankstelle endgültig löschen“.
    /// </summary>
    /// <param name="entry">Der Vorgang.</param>
    /// <returns>Der Text.</returns>
    public static string FormatConfirmDeleteHint(TankbookEntry entry)
    {
        return $"{FormatEntryHint(entry)} endgültig löschen";
    }

    /// <summary>
    /// Formatiert den Hinweis von „Beleg anzeigen“, z. B. „Beleg des Vorgangs vom 18.05.2026, 14:30“.
    /// </summary>
    /// <param name="entry">Der Vorgang.</param>
    /// <returns>Der Text.</returns>
    public static string FormatReceiptHint(TankbookEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return $"Beleg des Vorgangs vom {FormatDateTime(entry.OccurredAt)}";
    }

    /// <summary>
    /// Formatiert die Rückfrage vor dem Löschen eines Vorgangs.
    /// </summary>
    /// <param name="entry">Der Vorgang.</param>
    /// <returns>Der Text.</returns>
    public static string FormatDeleteQuestion(TankbookEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return $"Vorgang vom {FormatDateTime(entry.OccurredAt)} ({entry.StationName}) löschen? Das lässt sich nicht rückgängig machen.";
    }

    /// <summary>
    /// Formatiert die Beschriftung einer bekannten Tankstelle: der Name, bei mehrfach vorkommendem Namen zusätzlich der Ort, z. B. „Alpha Tankstelle (Berlin)“.
    /// </summary>
    /// <param name="station">Die Tankstelle.</param>
    /// <param name="isAmbiguous">Gibt an, ob der Name mehrfach vorkommt.</param>
    /// <returns>Der Text.</returns>
    public static string FormatKnownStationLabel(KnownStation station, bool isAmbiguous)
    {
        ArgumentNullException.ThrowIfNull(station);
        return isAmbiguous && !string.IsNullOrWhiteSpace(station.Place) ? $"{station.Name} ({station.Place})" : station.Name;
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler an Datum und Uhrzeit.
    /// </summary>
    /// <param name="error">Der Feldfehler.</param>
    /// <returns>Der Text; leer bei <see cref="TankbookEntryFieldError.None"/> und nicht passenden Werten.</returns>
    public static string GetOccurredAtError(TankbookEntryFieldError error)
    {
        return error switch
        {
            TankbookEntryFieldError.Required => "Bitte wähle Datum und Uhrzeit.",
            TankbookEntryFieldError.InFuture => "Der Zeitpunkt darf nicht in der Zukunft liegen.",
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler an der Tankstelle bzw. dem Ladeort.
    /// </summary>
    /// <param name="error">Der Feldfehler.</param>
    /// <returns>Der Text; leer bei <see cref="TankbookEntryFieldError.None"/> und nicht passenden Werten.</returns>
    public static string GetStationError(TankbookEntryFieldError error)
    {
        return error switch
        {
            TankbookEntryFieldError.Required => "Bitte gib eine Tankstelle oder einen Ladeort ein oder wähle eine bekannte Tankstelle.",
            TankbookEntryFieldError.TooLong => string.Create(German, $"Der Name der Tankstelle darf höchstens {TankbookEntryLimits.MaxStationNameLength} Zeichen lang sein."),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler an der Menge.
    /// </summary>
    /// <param name="error">Der Feldfehler.</param>
    /// <param name="kind">Die Art (für die Einheit).</param>
    /// <returns>Der Text; leer bei <see cref="TankbookEntryFieldError.None"/> und nicht passenden Werten.</returns>
    public static string GetQuantityError(TankbookEntryFieldError error, TankbookEntryKind kind)
    {
        var unit = GetQuantityUnit(kind);
        return error switch
        {
            TankbookEntryFieldError.NotANumber => $"Bitte gib die Menge in {unit} als Zahl ein, z. B. 40,5.",
            TankbookEntryFieldError.Negative => "Die Menge darf nicht negativ sein.",
            TankbookEntryFieldError.OutOfRange => string.Create(German, $"Die Menge muss größer als 0 und kleiner als {TankbookEntryLimits.MaxQuantityExclusive:N0} {unit} sein."),
            TankbookEntryFieldError.TooManyDecimals => string.Create(German, $"Die Menge darf höchstens {TankbookEntryLimits.MaxQuantityDecimals} Nachkommastellen haben."),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler am Preis pro Einheit.
    /// </summary>
    /// <param name="error">Der Feldfehler.</param>
    /// <param name="kind">Die Art (für die Einheit).</param>
    /// <returns>Der Text; leer bei <see cref="TankbookEntryFieldError.None"/> und nicht passenden Werten.</returns>
    public static string GetUnitPriceError(TankbookEntryFieldError error, TankbookEntryKind kind)
    {
        var unit = GetUnitPriceUnit(kind);
        return error switch
        {
            TankbookEntryFieldError.NotANumber => $"Bitte gib den Preis in {unit} als Zahl ein, z. B. 1,859.",
            TankbookEntryFieldError.Negative => "Der Preis darf nicht negativ sein.",
            TankbookEntryFieldError.OutOfRange => string.Create(German, $"Der Preis muss größer als 0 und kleiner als {TankbookEntryLimits.MaxUnitPriceExclusive:N0} {unit} sein."),
            TankbookEntryFieldError.TooManyDecimals => string.Create(German, $"Der Preis darf höchstens {TankbookEntryLimits.MaxUnitPriceDecimals} Nachkommastellen haben."),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler am Gesamtbetrag, auch zu fehlenden Werten und zur Abweichung der drei Werte.
    /// </summary>
    /// <param name="error">Der Feldfehler.</param>
    /// <param name="kind">Die Art (für die Bezeichnung des Preises).</param>
    /// <param name="expectedTotal">Der rechnerische Gesamtbetrag bei einer Abweichung.</param>
    /// <param name="enteredTotal">Der eingegebene Gesamtbetrag bei einer Abweichung.</param>
    /// <returns>Der Text; leer bei <see cref="TankbookEntryFieldError.None"/> und nicht passenden Werten.</returns>
    public static string GetTotalAmountError(TankbookEntryFieldError error, TankbookEntryKind kind, decimal? expectedTotal, decimal? enteredTotal)
    {
        return error switch
        {
            TankbookEntryFieldError.NotANumber => "Bitte gib den Gesamtbetrag in € als Zahl ein, z. B. 74,36.",
            TankbookEntryFieldError.Negative => "Der Gesamtbetrag darf nicht negativ sein.",
            TankbookEntryFieldError.OutOfRange => string.Create(German, $"Der Gesamtbetrag muss größer als 0 und kleiner als {TankbookEntryLimits.MaxTotalAmountExclusive:N0} € sein."),
            TankbookEntryFieldError.TooManyDecimals => string.Create(German, $"Der Gesamtbetrag darf höchstens {TankbookEntryLimits.MaxTotalAmountDecimals} Nachkommastellen haben."),
            TankbookEntryFieldError.MissingAmounts => $"Bitte gib mindestens zwei der drei Werte Menge, {GetUnitPriceLabel(kind)} und Gesamtbetrag an.",
            TankbookEntryFieldError.AmountMismatch when expectedTotal is { } expected && enteredTotal is { } entered
                => $"Menge × Preis ergibt {FormatAmount(expected)}, nicht {FormatAmount(entered)}. Bitte prüfe die Werte.",
            TankbookEntryFieldError.AmountMismatch => "Menge × Preis ergibt einen anderen Gesamtbetrag. Bitte prüfe die Werte.",
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler am Kilometerstand (Eingabefehler).
    /// </summary>
    /// <param name="error">Der Feldfehler.</param>
    /// <returns>Der Text; leer bei <see cref="TankbookEntryFieldError.None"/> und nicht passenden Werten.</returns>
    public static string GetOdometerError(TankbookEntryFieldError error)
    {
        return error switch
        {
            TankbookEntryFieldError.Required => "Bitte gib den Kilometerstand ein.",
            TankbookEntryFieldError.NotANumber => "Bitte gib den Kilometerstand als ganze Zahl ein, z. B. 10500.",
            TankbookEntryFieldError.Negative => "Der Kilometerstand darf nicht negativ sein.",
            TankbookEntryFieldError.OutOfRange => string.Create(German, $"Der Kilometerstand darf höchstens {VehicleLimits.MaxOdometerKm:N0} km betragen."),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler an der Notiz.
    /// </summary>
    /// <param name="error">Der Feldfehler.</param>
    /// <returns>Der Text; leer bei <see cref="TankbookEntryFieldError.None"/> und nicht passenden Werten.</returns>
    public static string GetNoteError(TankbookEntryFieldError error)
    {
        return error == TankbookEntryFieldError.TooLong
            ? string.Create(German, $"Die Notiz darf höchstens {TankbookEntryLimits.MaxNoteLength} Zeichen lang sein.")
            : string.Empty;
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler beim Übernehmen eines Belegfotos.
    /// </summary>
    /// <param name="error">Der Fehler.</param>
    /// <returns>Der Text; leer bei <see cref="ReceiptPhotoError.None"/>.</returns>
    public static string GetPhotoError(ReceiptPhotoError error)
    {
        return error switch
        {
            ReceiptPhotoError.None => string.Empty,
            ReceiptPhotoError.UnsupportedFormat => "Dieses Dateiformat wird nicht unterstützt. Bitte wähle ein JPG-, PNG- oder HEIC-Foto.",
            ReceiptPhotoError.TooLarge => string.Create(German, $"Das Foto ist zu groß (höchstens {TankbookEntryLimits.MaxReceiptPhotoBytes / (1024 * 1024)} MB)."),
            ReceiptPhotoError.Failed => PhotoFailed,
            _ => PhotoFailed,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Ergebnis einer Änderung; Kilometerfehler nennen den Grenzwert, z. B.
    /// „Der Kilometerstand darf nicht unter 10.500 km (vorheriger Vorgang) liegen.“
    /// </summary>
    /// <param name="result">Das Ergebnis.</param>
    /// <param name="odometerBoundKm">Der verletzte Grenzwert des Kilometerstands, sofern vorhanden.</param>
    /// <returns>Der Text; leer bei <see cref="TankbookEntryResult.Ok"/>.</returns>
    public static string GetResultMessage(TankbookEntryResult result, int? odometerBoundKm)
    {
        var bound = odometerBoundKm is { } km ? FormatKilometers(km) : string.Empty;
        return result switch
        {
            TankbookEntryResult.Ok => string.Empty,
            TankbookEntryResult.VehicleNotFound => VehicleNotFound,
            TankbookEntryResult.EntryNotFound => "Den Vorgang gibt es nicht mehr.",
            TankbookEntryResult.InvalidOccurredAt => "Bitte wähle Datum und Uhrzeit, die nicht in der Zukunft liegen.",
            TankbookEntryResult.KindNotAllowed => "Diese Art passt nicht zur Kraftstoffart des Fahrzeugs.",
            TankbookEntryResult.InvalidQuantity => "Die Menge ist ungültig.",
            TankbookEntryResult.InvalidUnitPrice => "Der Preis pro Einheit ist ungültig.",
            TankbookEntryResult.InvalidTotalAmount => "Der Gesamtbetrag ist ungültig.",
            TankbookEntryResult.AmountMismatch => "Menge × Preis ergibt einen anderen Gesamtbetrag. Bitte prüfe die Werte.",
            TankbookEntryResult.InvalidOdometer => GetOdometerError(TankbookEntryFieldError.OutOfRange),
            TankbookEntryResult.OdometerBelowVehicleStart => $"Der Kilometerstand darf nicht unter dem Start-Kilometerstand ({bound}) liegen.",
            TankbookEntryResult.OdometerBelowPrevious => $"Der Kilometerstand darf nicht unter {bound} (vorheriger Vorgang) liegen.",
            TankbookEntryResult.OdometerAboveNext => $"Der Kilometerstand darf nicht über {bound} (nächster Vorgang) liegen.",
            TankbookEntryResult.InvalidStationName => GetStationError(TankbookEntryFieldError.Required),
            TankbookEntryResult.NoteTooLong => GetNoteError(TankbookEntryFieldError.TooLong),
            _ => TankbookTexts.SaveFailed,
        };
    }
}
