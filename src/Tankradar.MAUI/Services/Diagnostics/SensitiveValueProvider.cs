using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.MAUI.Services.Diagnostics;

/// <summary>
/// Sammelt die aktiven API-Schlüssel des Preisdienstes und der Routensuche aus den bestehenden Schlüsselablagen.
/// </summary>
public sealed class SensitiveValueProvider : ISensitiveValueProvider
{
    private readonly IApiKeyProvider _priceKeyProvider;
    private readonly IRouteSearchAccess _routeSearchAccess;

    /// <summary>
    /// Erstellt den Provider.
    /// </summary>
    /// <param name="priceKeyProvider">Schlüsselquelle des Preisdienstes (Tankerkönig).</param>
    /// <param name="routeSearchAccess">Schlüsselquelle der Routensuche (OpenRouteService).</param>
    public SensitiveValueProvider(IApiKeyProvider priceKeyProvider, IRouteSearchAccess routeSearchAccess)
    {
        ArgumentNullException.ThrowIfNull(priceKeyProvider);
        ArgumentNullException.ThrowIfNull(routeSearchAccess);
        _priceKeyProvider = priceKeyProvider;
        _routeSearchAccess = routeSearchAccess;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetValuesAsync(CancellationToken cancellationToken = default)
    {
        var values = new List<string>(2);
        var priceKey = await _priceKeyProvider.GetApiKeyAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(priceKey))
        {
            values.Add(priceKey);
        }

        var routeKey = await _routeSearchAccess.GetApiKeyAsync().ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(routeKey) && !values.Contains(routeKey, StringComparer.Ordinal))
        {
            values.Add(routeKey);
        }

        return values;
    }
}
