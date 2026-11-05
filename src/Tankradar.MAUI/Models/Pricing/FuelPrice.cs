namespace Tankradar.MAUI.Models.Pricing;

/// <summary>
/// Ein Preis einer Spritsorte mit dem Zeitpunkt, zu dem er abgerufen wurde. Die Aktualität ergibt sich ausschließlich aus diesem Zeitstempel.
/// </summary>
/// <param name="FuelType">Die Spritsorte.</param>
/// <param name="Price">Der Preis in Euro je Liter.</param>
/// <param name="RetrievedUtc">Abrufzeitpunkt in UTC.</param>
/// <returns>Der Wert.</returns>
public sealed record FuelPrice(FuelType FuelType, decimal Price, DateTime RetrievedUtc);
