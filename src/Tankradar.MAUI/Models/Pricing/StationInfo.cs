namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Eine Tankstelle mit den Angaben, die die Quelle liefert. Fehlende Angaben bleiben <see langword="null"/> bzw. leer und werden nie erfunden.
/// </summary>
public sealed class StationInfo
{
    /// <summary>
    /// Die Kennung der Tankstelle (UUID der Quelle).
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Der Name der Tankstelle.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Die Marke, sofern geliefert.
    /// </summary>
    public string? Brand { get; init; }

    /// <summary>
    /// Die Straße, sofern geliefert.
    /// </summary>
    public string? Street { get; init; }

    /// <summary>
    /// Die Hausnummer, sofern geliefert.
    /// </summary>
    public string? HouseNumber { get; init; }

    /// <summary>
    /// Die Postleitzahl, sofern geliefert.
    /// </summary>
    public string? PostCode { get; init; }

    /// <summary>
    /// Der Ort, sofern geliefert.
    /// </summary>
    public string? Place { get; init; }

    /// <summary>
    /// Der Breitengrad der Tankstelle.
    /// </summary>
    public double Latitude { get; init; }

    /// <summary>
    /// Der Längengrad der Tankstelle.
    /// </summary>
    public double Longitude { get; init; }

    /// <summary>
    /// Die Entfernung zur Suchposition in Kilometern; nur bei Umkreissuchen gesetzt und nie gespeichert.
    /// </summary>
    public double? DistanceKm { get; init; }

    /// <summary>
    /// Gibt an, ob die Tankstelle laut Quelle geöffnet ist; <see langword="null"/>, wenn unbekannt.
    /// </summary>
    public bool? IsOpen { get; init; }

    /// <summary>
    /// Gibt an, ob die Tankstelle laut Quelle durchgehend geöffnet ist; <see langword="null"/>, wenn unbekannt.
    /// </summary>
    public bool? WholeDay { get; init; }

    /// <summary>
    /// Die Öffnungszeiten; leer, wenn die Quelle keine liefert.
    /// </summary>
    public IReadOnlyList<OpeningTimeEntry> OpeningTimes { get; init; } = [];

    /// <summary>
    /// Die zuletzt bekannten Preise je Sorte mit Abrufzeitpunkt.
    /// </summary>
    public IReadOnlyList<FuelPrice> Prices { get; init; } = [];

    /// <summary>
    /// Zeitpunkt (UTC), zu dem die Detailangaben zuletzt abgerufen wurden; <see langword="null"/>, wenn nur aus Umkreissuchen bekannt.
    /// </summary>
    public DateTime? DetailsUpdatedUtc { get; init; }

    /// <summary>
    /// Liefert eine Kopie, die die Detailangaben (Öffnungszeiten, durchgehende Öffnung) einer früheren Detailabfrage übernimmt.
    /// Die Umkreissuche der Quelle liefert diese Angaben nicht.
    /// </summary>
    /// <param name="details">Die lokal bekannte Tankstelle mit Detailangaben.</param>
    /// <returns>Die Kopie.</returns>
    public StationInfo WithDetails(StationInfo details)
    {
        ArgumentNullException.ThrowIfNull(details);
        return new StationInfo
        {
            Id = Id,
            Name = Name,
            Brand = Brand,
            Street = Street,
            HouseNumber = HouseNumber,
            PostCode = PostCode,
            Place = Place,
            Latitude = Latitude,
            Longitude = Longitude,
            DistanceKm = DistanceKm,
            IsOpen = IsOpen,
            WholeDay = details.WholeDay,
            OpeningTimes = details.OpeningTimes,
            Prices = Prices,
            DetailsUpdatedUtc = details.DetailsUpdatedUtc,
        };
    }

    /// <summary>
    /// Liefert die Tankstelle ohne Detailangaben (Öffnungszeiten, durchgehende Öffnung), wenn diese älter als <see cref="DetailFreshness.MaxAge"/> oder ohne bekannten Zeitpunkt sind.
    /// Sind sie noch verwendbar oder gar nicht vorhanden, wird dieselbe Instanz geliefert.
    /// </summary>
    /// <param name="nowUtc">Aktueller Zeitpunkt in UTC.</param>
    /// <returns>Die Tankstelle mit verwendbaren Detailangaben.</returns>
    public StationInfo WithoutStaleDetails(DateTime nowUtc)
    {
        var hasDetails = WholeDay is not null || OpeningTimes.Count > 0 || DetailsUpdatedUtc is not null;
        if (!hasDetails || DetailFreshness.IsUsable(DetailsUpdatedUtc, nowUtc))
        {
            return this;
        }

        return new StationInfo
        {
            Id = Id,
            Name = Name,
            Brand = Brand,
            Street = Street,
            HouseNumber = HouseNumber,
            PostCode = PostCode,
            Place = Place,
            Latitude = Latitude,
            Longitude = Longitude,
            DistanceKm = DistanceKm,
            IsOpen = IsOpen,
            Prices = Prices,
        };
    }

    /// <summary>
    /// Liefert eine Kopie mit anderen Preisen.
    /// </summary>
    /// <param name="prices">Die neuen Preise.</param>
    /// <returns>Die Kopie.</returns>
    public StationInfo With(IReadOnlyList<FuelPrice> prices)
    {
        return new StationInfo
        {
            Id = Id,
            Name = Name,
            Brand = Brand,
            Street = Street,
            HouseNumber = HouseNumber,
            PostCode = PostCode,
            Place = Place,
            Latitude = Latitude,
            Longitude = Longitude,
            DistanceKm = DistanceKm,
            IsOpen = IsOpen,
            WholeDay = WholeDay,
            OpeningTimes = OpeningTimes,
            Prices = prices,
            DetailsUpdatedUtc = DetailsUpdatedUtc,
        };
    }
}
