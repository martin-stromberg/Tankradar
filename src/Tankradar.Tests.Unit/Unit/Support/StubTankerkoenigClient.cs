using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="ITankerkoenigClient"/> für Tests mit vorgegebenen Antworten oder Fehlern.
/// </summary>
public sealed class StubTankerkoenigClient : ITankerkoenigClient
{
    /// <summary>
    /// Die Tankstellen, die eine Umkreissuche liefert.
    /// </summary>
    public List<StationInfo> SearchResult { get; } = [];

    /// <summary>
    /// Die Tankstelle, die ein Detailabruf liefert.
    /// </summary>
    public StationInfo? DetailResult { get; set; }

    /// <summary>
    /// Wenn gesetzt, lösen alle Abrufe diese Ausnahme aus.
    /// </summary>
    public PriceApiException? Failure { get; set; }

    /// <summary>
    /// Anzahl der Umkreissuchen.
    /// </summary>
    public int SearchCalls { get; private set; }

    /// <summary>
    /// Anzahl der Detailabrufe.
    /// </summary>
    public int DetailCalls { get; private set; }

    /// <inheritdoc />
    public Task<IReadOnlyList<StationInfo>> SearchAsync(double latitude, double longitude, int radiusKm, CancellationToken cancellationToken)
    {
        SearchCalls++;
        return Failure is not null ? throw Failure : Task.FromResult<IReadOnlyList<StationInfo>>(SearchResult.ToList());
    }

    /// <inheritdoc />
    public Task<StationInfo> GetDetailAsync(string stationId, CancellationToken cancellationToken)
    {
        DetailCalls++;
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(DetailResult ?? throw new InvalidOperationException("Kein Detail vorgegeben."));
    }
}
