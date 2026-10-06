using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Integration.Integration.Support;

/// <summary>
/// <see cref="IStationNavigator"/> für Integrationstests ohne Oberfläche: führt keine Navigation aus.
/// </summary>
public sealed class NullStationNavigator : IStationNavigator
{
    /// <inheritdoc />
    public Task OpenDetailAsync(StationListItem station)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task GoBackAsync()
    {
        return Task.CompletedTask;
    }
}
