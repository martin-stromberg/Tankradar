using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IStationNavigator"/> für Tests: protokolliert die geöffneten Tankstellen und Rücksprünge.
/// </summary>
public sealed class FakeStationNavigator : IStationNavigator
{
    /// <summary>
    /// Die geöffneten Tankstellen in Reihenfolge.
    /// </summary>
    public List<StationListItem> Opened { get; } = [];

    /// <summary>
    /// Anzahl der Rücksprünge.
    /// </summary>
    public int BackCount { get; private set; }

    /// <summary>
    /// Wenn gesetzt, wird sie beim Öffnen ausgelöst.
    /// </summary>
    public Exception? OpenException { get; set; }

    /// <summary>
    /// Wenn gesetzt, wird sie beim Rücksprung ausgelöst.
    /// </summary>
    public Exception? BackException { get; set; }

    /// <inheritdoc />
    public Task OpenDetailAsync(StationListItem station)
    {
        Opened.Add(station);
        return OpenException is null ? Task.CompletedTask : Task.FromException(OpenException);
    }

    /// <inheritdoc />
    public Task GoBackAsync()
    {
        BackCount++;
        return BackException is null ? Task.CompletedTask : Task.FromException(BackException);
    }
}
