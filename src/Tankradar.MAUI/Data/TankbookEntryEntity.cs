using Tankradar.MAUI.Models.Tankbook;

namespace Tankradar.MAUI.Data;

/// <summary>
/// Tabelle <c>TankbookEntries</c>: ein Tank- oder Ladevorgang eines Fahrzeugs. Strecke und Verbrauch werden nicht gespeichert, sondern beim Lesen berechnet.
/// </summary>
public class TankbookEntryEntity
{
    /// <summary>
    /// Primärschlüssel.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Fremdschlüssel auf <see cref="VehicleEntity"/>; beim Löschen des Fahrzeugs entfallen seine Vorgänge (<c>ON DELETE CASCADE</c>).
    /// </summary>
    public long VehicleId { get; set; }

    /// <summary>
    /// Zeitpunkt (UTC) des Anlegens.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// Datum und Uhrzeit des Vorgangs als lokale Wanduhrzeit (minutengenau, ohne Zeitzone).
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Name des <see cref="TankbookEntryKind"/>-Werts.
    /// </summary>
    public string Kind { get; set; } = "Fuel";

    /// <summary>
    /// Menge in Litern bzw. kWh, gespeichert als <c>TEXT</c>.
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Preis pro Einheit in Euro, gespeichert als <c>TEXT</c>.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Gesamtbetrag in Euro, gespeichert als <c>TEXT</c>.
    /// </summary>
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// Kilometerstand in ganzen Kilometern.
    /// </summary>
    public int OdometerKm { get; set; }

    /// <summary>
    /// Gibt an, ob es eine Teilbetankung bzw. Teilladung ist.
    /// </summary>
    public bool IsPartial { get; set; }

    /// <summary>
    /// Name der Tankstelle bzw. des Ladeorts (Momentaufnahme).
    /// </summary>
    public string StationName { get; set; } = string.Empty;

    /// <summary>
    /// Kennung einer ausgewählten Favoriten-Tankstelle (ohne Fremdschlüssel); <see langword="null"/> bei frei eingegebenem Namen.
    /// </summary>
    public string? StationId { get; set; }

    /// <summary>
    /// Optionale Notiz.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Dateiname des optionalen Belegfotos in der Ablage (nur der Name, kein Pfad).
    /// </summary>
    public string? ReceiptPhotoFileName { get; set; }

    /// <summary>
    /// Das Fahrzeug.
    /// </summary>
    public VehicleEntity? Vehicle { get; set; }

    /// <summary>
    /// Überträgt alle Angaben einer Eingabe auf die Entität (Load-and-Patch); Kennung, Fahrzeug und Anlagezeitpunkt bleiben unverändert.
    /// </summary>
    /// <param name="input">Die geprüfte Eingabe.</param>
    public void Apply(TankbookEntryInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        OccurredAt = input.OccurredAt;
        Kind = input.Kind.ToString();
        Quantity = input.Quantity;
        UnitPrice = input.UnitPrice;
        TotalAmount = input.TotalAmount;
        OdometerKm = input.OdometerKm;
        IsPartial = input.IsPartial;
        StationName = input.StationName.Trim();
        StationId = input.StationId;
        Note = string.IsNullOrWhiteSpace(input.Note) ? null : input.Note.Trim();
        ReceiptPhotoFileName = input.ReceiptPhotoFileName;
    }
}
