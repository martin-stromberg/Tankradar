using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.MAUI.Models.Backup;

/// <summary>
/// Obergrenzen für frei befüllbare Textspalten beim Wiederherstellen. Eine externe Datei gilt als nicht vertrauenswürdig: Überlange Texte
/// (z. B. mehrere hunderttausend Zeichen in einem Namen) lassen die Oberfläche abstürzen und werden deshalb als ungültige Sicherung abgelehnt.
/// Die Grenzen liegen großzügig über dem, was die App und die Quelle liefern, aber weit unter Größen, die die Anzeige gefährden.
/// Der eigene Anzeigename folgt der Grenze der App (<see cref="StationNaming.MaxCustomNameLength"/>).
/// </summary>
public static class BackupTextLimits
{
    private const int ShortText = 100;
    private const int Name = 200;
    private const int LongText = 2_000;
    private const int Json = 20_000;

    /// <summary>
    /// Die Spalten mit Obergrenze: Tabelle, Spalte und höchste Länge in Zeichen.
    /// </summary>
    public static IReadOnlyList<(string Table, string Column, int MaxLength)> Columns { get; } =
    [
        ("UserSettings", "GpsUsage", ShortText),
        ("UserSettings", "ResultView", ShortText),
        ("UserSettings", "ResultSortOrder", ShortText),
        ("FuelTypeSettings", "FuelTypeKey", ShortText),
        ("Stations", "Id", ShortText),
        ("Stations", "Name", Name),
        ("Stations", "Brand", Name),
        ("Stations", "CustomName", StationNaming.MaxCustomNameLength),
        ("Stations", "Street", Name),
        ("Stations", "HouseNumber", ShortText),
        ("Stations", "PostCode", ShortText),
        ("Stations", "Place", Name),
        ("Stations", "OpeningTimesJson", Json),
        ("PriceEntries", "StationId", ShortText),
        ("PriceEntries", "FuelTypeKey", ShortText),
        ("FavoriteGroups", "Name", Name),
        ("FavoriteGroups", "Description", LongText),
        ("FavoriteEntries", "StationId", ShortText),
        ("FavoriteEntries", "Note", LongText),
        ("FavoriteEntries", "Priority", ShortText),
        ("Vehicles", "Name", Name),
        ("Vehicles", "FuelKind", ShortText),
        ("TankbookEntries", "Kind", ShortText),
        ("TankbookEntries", "StationName", Name),
        ("TankbookEntries", "StationId", ShortText),
        ("TankbookEntries", "Note", LongText),
        ("TankbookEntries", "ReceiptPhotoFileName", Name),
    ];
}
