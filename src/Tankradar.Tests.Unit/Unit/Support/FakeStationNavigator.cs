using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IStationNavigator"/> für Tests: protokolliert die geöffneten Tankstellen.
/// </summary>
public sealed class FakeStationNavigator : IStationNavigator
{
    /// <summary>
    /// Die geöffneten Tankstellen in Reihenfolge.
    /// </summary>
    public List<StationListItem> Opened { get; } = [];

    /// <summary>
    /// Wenn gesetzt, wird sie beim Öffnen ausgelöst.
    /// </summary>
    public Exception? OpenException { get; set; }

    /// <summary>
    /// Wenn gesetzt, schließt das Öffnen erst ab, wenn dieses Signal gesetzt wird (für Tests gegen doppeltes Öffnen).
    /// </summary>
    public Task? Gate { get; set; }

    /// <inheritdoc />
    public async Task OpenDetailAsync(StationListItem station)
    {
        Opened.Add(station);
        if (Gate is { } gate)
        {
            await gate;
        }

        if (OpenException is not null)
        {
            throw OpenException;
        }
    }
}
