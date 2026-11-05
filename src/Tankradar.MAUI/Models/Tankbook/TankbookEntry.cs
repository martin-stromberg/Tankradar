namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Ein gespeicherter Tank- oder Ladevorgang eines Fahrzeugs.
/// </summary>
/// <param name="Id">Die Kennung des Vorgangs.</param>
/// <param name="VehicleId">Die Kennung des Fahrzeugs.</param>
/// <param name="OccurredAt">Datum und Uhrzeit als lokale Wanduhrzeit (minutengenau).</param>
/// <param name="Kind">Die Art des Vorgangs (Kraftstoff oder Strom).</param>
/// <param name="Quantity">Die Menge in Litern bzw. kWh.</param>
/// <param name="UnitPrice">Der Preis pro Einheit in Euro.</param>
/// <param name="TotalAmount">Der Gesamtbetrag in Euro.</param>
/// <param name="OdometerKm">Der Kilometerstand in ganzen Kilometern.</param>
/// <param name="IsPartial">Gibt an, ob es eine Teilbetankung bzw. Teilladung ist.</param>
/// <param name="StationName">Der Name der Tankstelle bzw. des Ladeorts (Momentaufnahme).</param>
/// <param name="StationId">Die Kennung einer ausgewählten Favoriten-Tankstelle; <see langword="null"/> bei frei eingegebenem Namen. Wird nie angezeigt.</param>
/// <param name="Note">Die optionale Notiz.</param>
/// <param name="ReceiptPhotoFileName">Der Dateiname des optionalen Belegfotos in der Ablage.</param>
/// <returns>Der Wert.</returns>
public sealed record TankbookEntry(
    long Id,
    long VehicleId,
    DateTime OccurredAt,
    TankbookEntryKind Kind,
    decimal Quantity,
    decimal UnitPrice,
    decimal TotalAmount,
    int OdometerKm,
    bool IsPartial,
    string StationName,
    string? StationId,
    string? Note,
    string? ReceiptPhotoFileName);
