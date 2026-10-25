using System.Globalization;
using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Resources.Texts;

/// <summary>
/// Zentrale deutsche UI-Texte des Tankbuchs (Fahrzeugverwaltung).
/// </summary>
public static class TankbookTexts
{
    private static readonly CultureInfo German = CultureInfo.GetCultureInfo("de-DE");

    /// <summary>
    /// Hinweis in der Löschrückfrage eines Fahrzeugs mit Vorgängen, dass sich vorher eine Sicherung erstellen lässt.
    /// </summary>
    public const string BackupAdvice = "Erstelle vorher in den Optionen eine Sicherung, wenn du die Vorgänge behalten möchtest.";

    /// <summary>
    /// Titel des Bereichs „Tankbuch“.
    /// </summary>
    public const string PageTitle = "Tankbuch";

    /// <summary>
    /// Einleitung der Seite.
    /// </summary>
    public const string Intro = "Alle Angaben bleiben auf diesem Gerät.";

    /// <summary>
    /// Überschrift der Fahrzeugliste.
    /// </summary>
    public const string VehiclesHeading = "Fahrzeuge";

    /// <summary>
    /// Hinweis, dass es noch keine Fahrzeuge gibt.
    /// </summary>
    public const string NoVehicles = "Noch keine Fahrzeuge. Lege über „Fahrzeug hinzufügen“ dein erstes Fahrzeug an.";

    /// <summary>
    /// Beschriftung „Fahrzeug hinzufügen“.
    /// </summary>
    public const string AddVehicle = "Fahrzeug hinzufügen";

    /// <summary>
    /// Überschrift des Formulars beim Anlegen.
    /// </summary>
    public const string NewVehicleTitle = "Neues Fahrzeug";

    /// <summary>
    /// Überschrift des Formulars beim Bearbeiten.
    /// </summary>
    public const string EditVehicleTitle = "Fahrzeug bearbeiten";

    /// <summary>
    /// Beschriftung des Namensfelds.
    /// </summary>
    public const string NameLabel = "Name des Fahrzeugs";

    /// <summary>
    /// Beschriftung der Auswahl der Kraftstoffart.
    /// </summary>
    public const string FuelKindLabel = "Kraftstoffart";

    /// <summary>
    /// Beschriftung des Felds für den Start-Kilometerstand.
    /// </summary>
    public const string OdometerLabel = "Start-Kilometerstand";

    /// <summary>
    /// Platzhalter des Felds für den Start-Kilometerstand.
    /// </summary>
    public const string OdometerPlaceholder = "z. B. 84350";

    /// <summary>
    /// Beschreibung des Felds für den Start-Kilometerstand für die Sprachausgabe (Beschriftung samt Einheit).
    /// </summary>
    public const string OdometerDescription = "Start-Kilometerstand in km";

    /// <summary>
    /// Beschriftung des Felds für den optionalen Startverbrauch.
    /// </summary>
    public const string ConsumptionLabel = "Startverbrauch, optional";

    /// <summary>
    /// Beschriftung „Speichern“.
    /// </summary>
    public const string Save = "Speichern";

    /// <summary>
    /// Beschriftung „Abbrechen“.
    /// </summary>
    public const string Cancel = "Abbrechen";

    /// <summary>
    /// Beschriftung „Bearbeiten“.
    /// </summary>
    public const string Edit = "Bearbeiten";

    /// <summary>
    /// Beschriftung „Löschen“.
    /// </summary>
    public const string Delete = "Löschen";

    /// <summary>
    /// Beschriftung der Bestätigung des Löschens eines Fahrzeugs ohne Vorgänge.
    /// </summary>
    public const string ConfirmDelete = "Ja, löschen";

    /// <summary>
    /// Beschriftung der Bestätigung des Löschens eines Fahrzeugs mit Vorgängen.
    /// </summary>
    public const string ConfirmDeleteWithEntries = "Fahrzeug und Vorgänge löschen";

    /// <summary>
    /// Einheit des Kilometerstands.
    /// </summary>
    public const string KilometerUnit = "km";

    /// <summary>
    /// Einheit des Verbrauchs bei Kraftstoff.
    /// </summary>
    public const string LiterConsumptionUnit = "L/100 km";

    /// <summary>
    /// Einheit des Verbrauchs bei Elektrofahrzeugen.
    /// </summary>
    public const string ElectricConsumptionUnit = "kWh/100 km";

    /// <summary>
    /// Fehlermeldung beim Laden der Fahrzeuge.
    /// </summary>
    public const string LoadFailed = "Die Fahrzeuge konnten nicht geladen werden.";

    /// <summary>
    /// Fehlermeldung beim Speichern.
    /// </summary>
    public const string SaveFailed = "Die Änderung konnte nicht gespeichert werden.";

    /// <summary>
    /// Meldung bei einem schon vergebenen Fahrzeugnamen.
    /// </summary>
    public const string DuplicateName = "Ein Fahrzeug mit diesem Namen gibt es bereits.";

    /// <summary>
    /// Beschriftung der Schaltfläche, die die Tank- und Ladevorgänge eines Fahrzeugs öffnet.
    /// </summary>
    public const string Entries = "Tank- und Ladevorgänge";

    /// <summary>
    /// Hinweis der Schaltfläche „Abbrechen“ des Formulars für die Sprachausgabe (der vorgelesene Name bleibt der sichtbare Text).
    /// </summary>
    public const string CancelFormHint = "Formular schließen, ohne zu speichern";

    /// <summary>
    /// Meldung, wenn der Start-Kilometerstand über dem Kilometerstand des ersten Tank- oder Ladevorgangs liegt.
    /// </summary>
    public const string StartOdometerAboveEntries = "Der Start-Kilometerstand darf nicht über dem Kilometerstand des ersten Tank- oder Ladevorgangs liegen.";

    /// <summary>
    /// Meldung, wenn sich die Zahl der Vorgänge beim Bestätigen des Löschens geändert hat.
    /// </summary>
    public const string EntryCountChanged = "Inzwischen hat sich die Zahl der Tank- und Ladevorgänge geändert. Bitte bestätige das Löschen erneut.";

    /// <summary>
    /// Hinweis einer Schaltfläche, die sich auf ein Fahrzeug bezieht, für die Sprachausgabe, z. B. „Fahrzeug „Golf““.
    /// </summary>
    /// <param name="name">Der Name des Fahrzeugs.</param>
    /// <returns>Der Text.</returns>
    public static string FormatVehicleHint(string name)
    {
        return $"Fahrzeug „{name}“";
    }

    /// <summary>
    /// Hinweis der Bestätigung der Löschrückfrage für die Sprachausgabe (der vorgelesene Name bleibt der sichtbare Text); nennt das Fahrzeug
    /// und bei vorhandenen Vorgängen deren Zahl, z. B. „Fahrzeug „Golf“ und 2 Tank- und Ladevorgänge endgültig löschen“.
    /// </summary>
    /// <param name="name">Der Name des Fahrzeugs.</param>
    /// <param name="entryCount">Die Zahl der Tank- und Ladevorgänge des Fahrzeugs.</param>
    /// <returns>Der Text.</returns>
    public static string FormatConfirmDeleteHint(string name, int entryCount)
    {
        return entryCount switch
        {
            <= 0 => $"Fahrzeug „{name}“ endgültig löschen",
            1 => $"Fahrzeug „{name}“ und 1 Tank- oder Ladevorgang endgültig löschen",
            _ => string.Create(German, $"Fahrzeug „{name}“ und {entryCount} Tank- und Ladevorgänge endgültig löschen"),
        };
    }

    /// <summary>
    /// Hinweis der Schaltfläche „Abbrechen“ der Löschrückfrage für die Sprachausgabe, z. B. „Fahrzeug „Golf“ behalten“.
    /// </summary>
    /// <param name="name">Der Name des Fahrzeugs.</param>
    /// <returns>Der Text.</returns>
    public static string FormatCancelDeleteHint(string name)
    {
        return $"Fahrzeug „{name}“ behalten";
    }

    /// <summary>
    /// Liefert die Beschriftung einer Kraftstoffart.
    /// </summary>
    /// <param name="fuelKind">Die Kraftstoffart.</param>
    /// <returns>Der Text.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Die Kraftstoffart ist kein bekannter Wert.</exception>
    public static string GetFuelKindLabel(VehicleFuelKind fuelKind)
    {
        return fuelKind switch
        {
            VehicleFuelKind.Petrol => "Benzin",
            VehicleFuelKind.Diesel => "Diesel",
            VehicleFuelKind.Electric => "Elektro",
            VehicleFuelKind.Hybrid => "Hybrid",
            _ => throw new ArgumentOutOfRangeException(nameof(fuelKind), fuelKind, "Unbekannte Kraftstoffart."),
        };
    }

    /// <summary>
    /// Liefert die Einheit des Verbrauchs einer Kraftstoffart: kWh/100 km bei Elektro, sonst L/100 km.
    /// </summary>
    /// <param name="fuelKind">Die Kraftstoffart.</param>
    /// <returns>Die Einheit.</returns>
    public static string GetConsumptionUnit(VehicleFuelKind fuelKind)
    {
        return fuelKind switch
        {
            VehicleFuelKind.Electric => ElectricConsumptionUnit,
            VehicleFuelKind.Petrol => LiterConsumptionUnit,
            VehicleFuelKind.Diesel => LiterConsumptionUnit,
            VehicleFuelKind.Hybrid => LiterConsumptionUnit,
            _ => LiterConsumptionUnit,
        };
    }

    /// <summary>
    /// Liefert den Platzhalter des Verbrauchsfelds passend zur Kraftstoffart, z. B. „z. B. 5,4“ bzw. „z. B. 16,5“ bei Elektro.
    /// </summary>
    /// <param name="fuelKind">Die Kraftstoffart.</param>
    /// <returns>Der Platzhalter.</returns>
    public static string GetConsumptionPlaceholder(VehicleFuelKind fuelKind)
    {
        return fuelKind == VehicleFuelKind.Electric ? "z. B. 16,5" : "z. B. 5,4";
    }

    /// <summary>
    /// Formatiert die Beschreibung des Verbrauchsfelds für die Sprachausgabe (Beschriftung samt Einheit), z. B. „Startverbrauch, optional, in L/100 km“.
    /// </summary>
    /// <param name="fuelKind">Die Kraftstoffart.</param>
    /// <returns>Der Text.</returns>
    public static string FormatConsumptionDescription(VehicleFuelKind fuelKind)
    {
        return $"{ConsumptionLabel}, in {GetConsumptionUnit(fuelKind)}";
    }

    /// <summary>
    /// Formatiert die Angaben eines Fahrzeugeintrags, z. B. „Diesel • Start 84.350 km • Startverbrauch 5,4 L/100 km“; ohne Verbrauch endet der Text nach „km“.
    /// </summary>
    /// <param name="vehicle">Das Fahrzeug.</param>
    /// <returns>Der Text.</returns>
    public static string FormatVehicleDetails(Vehicle vehicle)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        var text = string.Create(German, $"{GetFuelKindLabel(vehicle.FuelKind)} • Start {vehicle.StartOdometerKm:N0} {KilometerUnit}");
        return vehicle.StartConsumption is { } consumption
            ? string.Create(German, $"{text} • Startverbrauch {consumption:0.##} {GetConsumptionUnit(vehicle.FuelKind)}")
            : text;
    }

    /// <summary>
    /// Formatiert den Kilometerstand für das Eingabefeld ohne Tausenderpunkte, z. B. „84350“.
    /// </summary>
    /// <param name="odometerKm">Der Kilometerstand.</param>
    /// <returns>Der Text.</returns>
    public static string FormatOdometerInput(int odometerKm)
    {
        return odometerKm.ToString("0", German);
    }

    /// <summary>
    /// Formatiert den Verbrauch für das Eingabefeld mit Dezimalkomma, z. B. „5,4“.
    /// </summary>
    /// <param name="consumption">Der Verbrauch oder <see langword="null"/>.</param>
    /// <returns>Der Text; leer, wenn kein Verbrauch gesetzt ist.</returns>
    public static string FormatConsumptionInput(decimal? consumption)
    {
        return consumption is { } value ? value.ToString("0.##", German) : string.Empty;
    }

    /// <summary>
    /// Formatiert die Rückfrage vor dem Löschen eines Fahrzeugs; bei vorhandenen Vorgängen nennt sie deren Zahl und das endgültige Mitlöschen.
    /// </summary>
    /// <param name="name">Der Name des Fahrzeugs.</param>
    /// <param name="entryCount">Die Zahl der Tank- und Ladevorgänge des Fahrzeugs.</param>
    /// <returns>Der Text.</returns>
    public static string FormatDeleteQuestion(string name, int entryCount)
    {
        var question = $"Fahrzeug „{name}“ löschen?";
        return entryCount switch
        {
            <= 0 => question,
            1 => $"{question} Dabei wird auch der zugehörige Tank- oder Ladevorgang endgültig gelöscht. Das lässt sich nicht rückgängig machen. {BackupAdvice}",
            _ => string.Create(German, $"{question} Dabei werden auch die {entryCount} zugehörigen Tank- und Ladevorgänge endgültig gelöscht. Das lässt sich nicht rückgängig machen. {BackupAdvice}"),
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler am Namen.
    /// </summary>
    /// <param name="error">Der Feldfehler.</param>
    /// <returns>Der Text; leer bei <see cref="VehicleFieldError.None"/> und nicht passenden Werten.</returns>
    public static string GetNameError(VehicleFieldError error)
    {
        return error switch
        {
            VehicleFieldError.Required => "Bitte gib einen Namen ein.",
            VehicleFieldError.TooLong => string.Create(German, $"Der Name darf höchstens {VehicleLimits.MaxNameLength} Zeichen lang sein."),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler am Start-Kilometerstand.
    /// </summary>
    /// <param name="error">Der Feldfehler.</param>
    /// <returns>Der Text; leer bei <see cref="VehicleFieldError.None"/> und nicht passenden Werten.</returns>
    public static string GetOdometerError(VehicleFieldError error)
    {
        return error switch
        {
            VehicleFieldError.Required => "Bitte gib den Start-Kilometerstand ein.",
            VehicleFieldError.NotANumber => "Bitte gib den Kilometerstand als ganze Zahl ein, z. B. 84350.",
            VehicleFieldError.Negative => "Der Kilometerstand darf nicht negativ sein.",
            VehicleFieldError.OutOfRange => string.Create(German, $"Der Kilometerstand darf höchstens {VehicleLimits.MaxOdometerKm:N0} km betragen."),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Fehler am Startverbrauch.
    /// </summary>
    /// <param name="error">Der Feldfehler.</param>
    /// <returns>Der Text; leer bei <see cref="VehicleFieldError.None"/> und nicht passenden Werten.</returns>
    public static string GetConsumptionError(VehicleFieldError error)
    {
        return error switch
        {
            VehicleFieldError.NotANumber => "Bitte gib den Startverbrauch als Zahl ein, z. B. 5,4.",
            VehicleFieldError.Negative => "Der Startverbrauch darf nicht negativ sein.",
            VehicleFieldError.OutOfRange => string.Create(German, $"Der Startverbrauch muss größer als 0 und kleiner als {VehicleLimits.MaxConsumptionExclusive:0} sein."),
            VehicleFieldError.TooManyDecimals => string.Create(German, $"Der Startverbrauch darf höchstens {VehicleLimits.MaxConsumptionDecimals} Nachkommastellen haben."),
            _ => string.Empty,
        };
    }

    /// <summary>
    /// Liefert die Meldung zu einem Ergebnis einer Änderung.
    /// </summary>
    /// <param name="result">Das Ergebnis.</param>
    /// <returns>Der Text; leer bei <see cref="VehicleResult.Ok"/>.</returns>
    public static string GetResultMessage(VehicleResult result)
    {
        return result switch
        {
            VehicleResult.Ok => string.Empty,
            VehicleResult.InvalidName => string.Create(German, $"Bitte gib einen Namen mit höchstens {VehicleLimits.MaxNameLength} Zeichen ein."),
            VehicleResult.DuplicateName => DuplicateName,
            VehicleResult.InvalidFuelKind => "Bitte wähle eine Kraftstoffart.",
            VehicleResult.InvalidOdometer => string.Create(German, $"Der Start-Kilometerstand muss zwischen 0 und {VehicleLimits.MaxOdometerKm:N0} km liegen."),
            VehicleResult.InvalidConsumption => string.Create(
                German,
                $"Der Startverbrauch muss größer als 0 und kleiner als {VehicleLimits.MaxConsumptionExclusive:0} sein, mit höchstens {VehicleLimits.MaxConsumptionDecimals} Nachkommastellen."),
            VehicleResult.VehicleNotFound => "Das Fahrzeug gibt es nicht mehr.",
            VehicleResult.EntryCountChanged => EntryCountChanged,
            VehicleResult.StartOdometerAboveEntries => StartOdometerAboveEntries,
            _ => SaveFailed,
        };
    }
}
