using Tankradar.MAUI.ViewModels;
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
    /// Geschlossen wird nur die Seite, die das Fahrzeug anzeigt: Ein noch nicht eingesammeltes ViewModel einer längst geschlossenen Seite darf nicht
    /// die offene Seite eines anderen Fahrzeugs schließen. Die Seite wird im Navigationsstapel ihres eigenen Reiters geschlossen. <c>GoToAsync("..")</c>
    /// wirkt nur auf den aktiven Reiter und schließt die Seite daher nicht, wenn z. B. während einer Wiederherstellung der Reiter „Optionen“ aktiv ist.
    /// </remarks>
    public async Task CloseAsync(long vehicleId)
    {
        var shell = Shell.Current;
        var section = shell.Items
            .SelectMany(item => item.Items)
            .FirstOrDefault(candidate => SelectPages(candidate.Navigation.NavigationStack.OfType<TankbookEntriesPage>(), page => page.BindingContext, vehicleId).Count > 0);
        if (section is null)
        {
            return;
        }

        var stack = section.Navigation.NavigationStack;
        if (ReferenceEquals(shell.CurrentItem?.CurrentItem, section) && stack[^1] is TankbookEntriesPage top
            && SelectPages([top], page => page.BindingContext, vehicleId).Count == 1)
        {
            await shell.GoToAsync("..").ConfigureAwait(true);
            return;
        }

        foreach (var page in SelectPages(stack.OfType<TankbookEntriesPage>(), page => page.BindingContext, vehicleId))
        {
            section.Navigation.RemovePage(page);
        }
    }

    /// <summary>
    /// Wählt aus den Seiten diejenigen aus, deren ViewModel die Vorgänge des Fahrzeugs anzeigt.
    /// </summary>
    /// <typeparam name="T">Der Typ der Seite.</typeparam>
    /// <param name="pages">Die Seiten.</param>
    /// <param name="bindingContextOf">Liefert das ViewModel einer Seite.</param>
    /// <param name="vehicleId">Die Kennung des Fahrzeugs.</param>
    /// <returns>Die Seiten des Fahrzeugs in der Reihenfolge der Eingabe.</returns>
    public static IReadOnlyList<T> SelectPages<T>(IEnumerable<T> pages, Func<T, object?> bindingContextOf, long vehicleId)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(bindingContextOf);
        return pages.Where(page => bindingContextOf(page) is TankbookEntriesViewModel { VehicleId: { } id } && id == vehicleId).ToList();
    }
}
