using Tankradar.MAUI.Views;

namespace Tankradar.MAUI.Services.Navigation;

/// <summary>
/// Umsetzung von <see cref="ITankbookNavigator"/> über die Shell-Navigation der App.
/// </summary>
public sealed class ShellTankbookNavigator : ITankbookNavigator
{
    /// <summary>
    /// Die Route der Seite der Tank- und Ladevorgänge.
    /// </summary>
    public const string EntriesRoute = "tankbookentries";

    /// <summary>
    /// Der Name des Navigationsparameters, der die Kennung des Fahrzeugs trägt.
    /// </summary>
    public const string VehicleIdParameter = "vehicleId";

    /// <inheritdoc />
    public Task OpenEntriesAsync(long vehicleId)
    {
        var parameters = new Dictionary<string, object> { [VehicleIdParameter] = vehicleId };
        return Shell.Current.GoToAsync(EntriesRoute, parameters);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Die Seite wird im Navigationsstapel ihres eigenen Reiters geschlossen. <c>GoToAsync("..")</c> wirkt nur auf den aktiven Reiter und
    /// schließt die Seite daher nicht, wenn z. B. während einer Wiederherstellung der Reiter „Optionen“ aktiv ist.
    /// </remarks>
    public async Task CloseAsync()
    {
        var shell = Shell.Current;
        var section = shell.Items
            .SelectMany(item => item.Items)
            .FirstOrDefault(candidate => candidate.Navigation.NavigationStack.OfType<TankbookEntriesPage>().Any());
        if (section is null)
        {
            return;
        }

        if (ReferenceEquals(shell.CurrentItem?.CurrentItem, section) && section.Navigation.NavigationStack[^1] is TankbookEntriesPage)
        {
            await shell.GoToAsync("..").ConfigureAwait(true);
            return;
        }

        foreach (var page in section.Navigation.NavigationStack.OfType<TankbookEntriesPage>().ToList())
        {
            section.Navigation.RemovePage(page);
        }
    }
}
