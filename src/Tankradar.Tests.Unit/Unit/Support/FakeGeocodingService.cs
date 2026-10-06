using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IGeocodingService"/> für Tests mit vorgegebenem Ergebnis; protokolliert die empfangenen Eingaben. Es findet nie echter Netzwerkverkehr statt.
/// </summary>
public sealed class FakeGeocodingService : IGeocodingService
{
    /// <summary>
    /// Das gelieferte Ergebnis (Standard: Berlin-Mitte als „Berlin, Deutschland“).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public GeocodingResult Result { get; set; } = GeocodingResult.Success(new GeoPosition(StationFactory.CenterLatitude, StationFactory.CenterLongitude), "Berlin, Deutschland");

    /// <summary>
    /// Wenn gesetzt, wird sie von <see cref="ResolveAsync"/> ausgelöst.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Die empfangenen Eingaben in Reihenfolge.
    /// </summary>
    public List<string?> Inputs { get; } = [];

    /// <inheritdoc />
    public Task<GeocodingResult> ResolveAsync(string? input, CancellationToken cancellationToken = default)
    {
        Inputs.Add(input);
        return Exception is null ? Task.FromResult(Result) : Task.FromException<GeocodingResult>(Exception);
    }
}
