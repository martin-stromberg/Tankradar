using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das Zusammenspiel der Detailansicht mit der Karte „Favoritengruppen“: Die Tankstelle aus der Ergebnisliste bestimmt die angezeigten Gruppen, auch ohne Verbindung.
/// </summary>
public class StationDetailViewModelTests_Favorites : StationDetailViewModelTestBase
{
    /// <summary>
    /// Prüft, dass die Detailansicht die Gruppen der geöffneten Tankstelle zeigt und das Hinzufügen und Entfernen über die Karte funktioniert.
    /// </summary>
    [Fact]
    public async Task Show_LoadsGroupsOfStation_AddAndRemoveWork()
    {
        var group = (await FavoritesDb.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        await FavoritesDb.Service.AddStationAsync(group, StationId);
        Prices.DetailResult = new StationDetailResult(CreateStation(Now.AddMinutes(-5), Now.AddMinutes(-5)), PriceDataSource.Live, PriceFailure.None);

        ViewModel.Show(CreateOrigin());
        await ViewModel.Favorites.LastTask;
        await AppearAsync();

        Assert.Equal(["Arbeitsweg"], ViewModel.Favorites.AssignedGroups.Select(g => g.Name));

        ViewModel.Favorites.RemoveCommand.Execute(null);
        await ViewModel.Favorites.LastTask;

        Assert.Empty(await FavoritesDb.Service.GetGroupsOfStationAsync(StationId));
        Assert.Equal(FavoriteResult.Ok, await FavoritesDb.Service.AddStationAsync(group, StationId));
    }

    /// <summary>
    /// Prüft, dass die Favoriten auch angezeigt werden, wenn die Details nicht geladen werden können (offline): Sie liegen lokal vor.
    /// </summary>
    [Fact]
    public async Task Show_WithoutConnection_StillShowsGroups()
    {
        var group = (await FavoritesDb.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await FavoritesDb.Service.AddStationAsync(group, StationId);
        Connection.IsOnline = false;

        ViewModel.Show(CreateOrigin());
        await ViewModel.Favorites.LastTask;

        Assert.Equal(["Heimat"], ViewModel.Favorites.AssignedGroups.Select(g => g.Name));
    }
}
