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

    /// <inheritdoc />
    public Task OpenDetailAsync(StationListItem station)
    {
        Opened.Add(station);
        return OpenException is null ? Task.CompletedTask : Task.FromException(OpenException);
    }
}
