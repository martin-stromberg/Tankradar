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
    /// Die Preise, die ein Abruf nach Kennungen je Kennung liefert (fehlende Kennungen ergeben keine Zeilen).
    /// </summary>
    public Dictionary<string, IReadOnlyList<FuelPrice>> PricesResult { get; } = [];

    /// <summary>
    /// Wenn gesetzt, enthält die Preisantwort nur Kennungen aus <see cref="PricesResult"/> (statt leerer Einträge für unbekannte Kennungen).
    /// </summary>
    public bool OmitUnknownPriceIds { get; set; }

    /// <summary>
    /// Die Kennungen je Abruf nach Kennungen, in Reihenfolge der Abrufe.
    /// </summary>
    public List<IReadOnlyList<string>> PricesRequests { get; } = [];

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

    /// <summary>
    /// Wenn gesetzt, löst der Abruf nach Kennungen ab dem angegebenen (nullbasierten) Abruf diese Ausnahme aus.
    /// </summary>
    public int PricesFailureFromCall { get; set; } = int.MaxValue;

    /// <summary>
    /// Die Ausnahme, die ab <see cref="PricesFailureFromCall"/> ausgelöst wird.
    /// </summary>
    public PriceApiException? PricesFailure { get; set; }

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

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, IReadOnlyList<FuelPrice>>> GetPricesAsync(IReadOnlyCollection<string> stationIds, CancellationToken cancellationToken)
    {
        var call = PricesRequests.Count;
        PricesRequests.Add(stationIds.ToList());
        if (Failure is not null)
        {
            throw Failure;
        }

        if (PricesFailure is not null && call >= PricesFailureFromCall)
        {
            throw PricesFailure;
        }

        IReadOnlyDictionary<string, IReadOnlyList<FuelPrice>> result = stationIds.Where(id => !OmitUnknownPriceIds || PricesResult.ContainsKey(id)).ToDictionary(
            id => id,
            id => PricesResult.TryGetValue(id, out var lines) ? lines : (IReadOnlyList<FuelPrice>)[]);
        return Task.FromResult(result);
    }
}
