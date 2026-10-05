namespace Tankradar.MAUI.Data;

/// <summary>
/// Tabelle <c>Stations</c>: lokal bekannte Tankstellen. Gespeichert werden nur Stammdaten der Quelle, nie Positionen des Anwenders.
/// </summary>
public class StationEntity
{
    /// <summary>
    /// Primärschlüssel: Kennung der Tankstelle (UUID der Quelle).
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Name der Tankstelle.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Marke, sofern geliefert.
    /// </summary>
    public string? Brand { get; set; }

    /// <summary>
    /// Straße, sofern geliefert.
    /// </summary>
    public string? Street { get; set; }

    /// <summary>
    /// Hausnummer, sofern geliefert.
    /// </summary>
    public string? HouseNumber { get; set; }

    /// <summary>
    /// Postleitzahl, sofern geliefert.
    /// </summary>
    public string? PostCode { get; set; }

    /// <summary>
    /// Ort, sofern geliefert.
    /// </summary>
    public string? Place { get; set; }

    /// <summary>
    /// Breitengrad der Tankstelle.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Längengrad der Tankstelle.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Angabe der Quelle „durchgehend geöffnet“; <see langword="null"/>, wenn unbekannt.
    /// </summary>
    public bool? WholeDay { get; set; }

    /// <summary>
    /// Öffnungszeiten als JSON; <see langword="null"/>, wenn nie Details abgerufen wurden.
    /// </summary>
    public string? OpeningTimesJson { get; set; }

    /// <summary>
    /// Zeitpunkt (UTC) des letzten Detailabrufs.
    /// </summary>
    public DateTime? DetailsUpdatedUtc { get; set; }

    /// <summary>
    /// Die gespeicherten Preisstände der Tankstelle.
    /// </summary>
    public List<PriceEntryEntity> PriceEntries { get; set; } = [];
}
