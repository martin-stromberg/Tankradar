using Tankradar.MAUI.Services.Navigation;

namespace Tankradar.Tests.Unit.Unit.Support;

/// <summary>
/// <see cref="IFavoriteGroupNavigator"/> für Tests: protokolliert geöffnete Gruppen und das Schließen.
/// </summary>
public sealed class FakeFavoriteGroupNavigator : IFavoriteGroupNavigator
{
    /// <summary>
    /// Die geöffneten Gruppen in Reihenfolge.
    /// </summary>
    public List<long> Opened { get; } = [];

    /// <summary>
    /// Wie oft die Gruppenansicht geschlossen wurde.
    /// </summary>
    public int Closed { get; private set; }

    /// <inheritdoc />
    public Task OpenGroupAsync(long groupId)
    {
        Opened.Add(groupId);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CloseAsync()
    {
        Closed++;
        return Task.CompletedTask;
    }
}
