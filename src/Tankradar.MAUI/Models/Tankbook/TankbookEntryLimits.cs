namespace Tankradar.MAUI.Models.Tankbook;

/// <summary>
/// Grenzen der Angaben eines Tank- oder Ladevorgangs und seines Belegfotos.
/// </summary>
public static class TankbookEntryLimits
{
    /// <summary>
    /// Höchstzahl der Zeichen des Namens der Tankstelle bzw. des Ladeorts.
    /// </summary>
    public const int MaxStationNameLength = 60;

    /// <summary>
    /// Höchstzahl der Zeichen der Notiz.
    /// </summary>
    public const int MaxNoteLength = 500;

    /// <summary>
    /// Obergrenze (ausschließlich) der Menge in Litern bzw. kWh.
    /// </summary>
    public const decimal MaxQuantityExclusive = 1_000m;

    /// <summary>
    /// Höchstzahl der Nachkommastellen der Menge.
    /// </summary>
    public const int MaxQuantityDecimals = 3;

    /// <summary>
    /// Obergrenze (ausschließlich) des Preises pro Einheit in Euro.
    /// </summary>
    public const decimal MaxUnitPriceExclusive = 10m;

    /// <summary>
    /// Höchstzahl der Nachkommastellen des Preises pro Einheit (auch eines berechneten Preises).
    /// </summary>
    public const int MaxUnitPriceDecimals = 3;

    /// <summary>
    /// Obergrenze (ausschließlich) des Gesamtbetrags in Euro.
    /// </summary>
    public const decimal MaxTotalAmountExclusive = 10_000m;

    /// <summary>
    /// Höchstzahl der Nachkommastellen des Gesamtbetrags (auch eines berechneten Gesamtbetrags).
    /// </summary>
    public const int MaxTotalAmountDecimals = 2;

    /// <summary>
    /// Nachkommastellen einer berechneten Menge.
    /// </summary>
    public const int ComputedQuantityDecimals = 2;

    /// <summary>
    /// Erlaubte Abweichung in Euro zwischen Menge × Preis pro Einheit und dem Gesamtbetrag.
    /// </summary>
    public const decimal AmountTolerance = 0.02m;

    /// <summary>
    /// Höchstzahl der angebotenen bekannten Tankstellen.
    /// </summary>
    public const int MaxKnownStations = 30;

    /// <summary>
    /// Höchstgröße eines Belegfotos in Bytes (20 MB).
    /// </summary>
    public const long MaxReceiptPhotoBytes = 20L * 1024 * 1024;

    /// <summary>
    /// Toleranz, um die ein Zeitpunkt nach „jetzt“ liegen darf, ehe er als Zeitpunkt in der Zukunft gilt.
    /// </summary>
    public static TimeSpan FutureTolerance
    {
        get { return TimeSpan.FromMinutes(1); }
    }

    /// <summary>
    /// Die erlaubten Dateiendungen eines Belegfotos (klein geschrieben, samt Punkt).
    /// </summary>
    public static readonly IReadOnlyList<string> ReceiptPhotoExtensions = [".jpg", ".jpeg", ".png", ".heic", ".heif"];
}
