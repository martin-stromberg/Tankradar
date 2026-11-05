using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Routing;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IRouteSearchService"/> für Tests mit vorgegebenem Ergebnis; protokolliert die Aufträge und meldet auf Wunsch Fortschritt. Es findet nie echter Netzwerkverkehr statt.
/// </summary>
public sealed class FakeRouteSearchService : IRouteSearchService
{
    /// <summary>
    /// Das gelieferte Ergebnis (Standard: Routing nicht verfügbar).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public RouteSearchResult Result { get; set; } = RouteSearchResult.Fail(RouteSearchStatus.RoutingUnavailable);

    /// <summary>
    /// Wenn gesetzt, wird sie von <see cref="SearchAsync"/> ausgelöst.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Der Fortschritt, den <see cref="SearchAsync"/> vor dem Ergebnis meldet.
    /// </summary>
    public List<RouteSearchProgress> ProgressToReport { get; } = [];

    /// <summary>
    /// Wenn gesetzt, wartet <see cref="SearchAsync"/> (abbrechbar) auf dieses Signal, bevor es das Ergebnis liefert.
    /// </summary>
    public Task? Gate { get; set; }

    /// <summary>
    /// Die empfangenen Aufträge in Reihenfolge.
    /// </summary>
    public List<RouteSearchRequest> Requests { get; } = [];

    /// <inheritdoc />
    public async Task<RouteSearchResult> SearchAsync(RouteSearchRequest request, IProgress<RouteSearchProgress>? progress, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        foreach (var step in ProgressToReport)
        {
            progress?.Report(step);
        }

        if (Gate is { } gate)
        {
            await gate.WaitAsync(cancellationToken);
        }

        return Exception is null ? Result : throw Exception;
    }
}
