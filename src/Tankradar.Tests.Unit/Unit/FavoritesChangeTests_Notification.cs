using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.MAUI.Services.Favorites;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Änderungsbenachrichtigung der Favoriten: Der Dienst meldet gespeicherte Änderungen, und geöffnete Ansichten (Detailansicht im Hintergrund, Gruppenliste)
/// aktualisieren sich ohne erneutes Erscheinen der Seite.
/// </summary>
public class FavoritesChangeTests_Notification : BaseTest
{
    private readonly FavoritesFixture _fixture = new();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fixture.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Prüft, dass jede gespeicherte Änderung genau ein Ereignis auslöst, Lesen und abgelehnte Änderungen aber keines.
    /// </summary>
    [Fact]
    public async Task Service_RaisesChangedOnlyForSavedChanges()
    {
        var count = 0;
        _fixture.Service.Changed += (_, _) => count++;

        var group = (await _fixture.Service.CreateGroupAsync("Heimat", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);
        await _fixture.Service.UpdateEntryAsync(group, FavoritesFixture.StationA, "x", FavoritePriority.Low);
        await _fixture.Service.UpdateGroupAsync(group, "Zuhause", null);
        await _fixture.Service.RemoveStationAsync(FavoritesFixture.StationA, [group]);
        await _fixture.Service.DeleteGroupAsync(group);
        Assert.Equal(6, count);

        await _fixture.Service.GetGroupsAsync();
        await _fixture.Service.CreateGroupAsync(" ", null);
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);
        await _fixture.Service.DeleteGroupAsync(group);
        Assert.Equal(6, count);
    }

    /// <summary>
    /// Prüft, dass eine im Hintergrund geöffnete Detailansicht die Umbenennung einer Gruppe übernimmt, ohne dass die Seite erneut erscheint.
    /// </summary>
    [Fact]
    public async Task StationFavorites_FollowsRenameAndDeleteMadeElsewhere()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);
        var viewModel = new StationFavoritesViewModel(_fixture.Service, NullLogger<StationFavoritesViewModel>.Instance);
        await viewModel.SetStationAsync(FavoritesFixture.StationA);
        Assert.Equal(["Arbeitsweg"], viewModel.AssignedGroups.Select(g => g.Name));

        await _fixture.Service.UpdateGroupAsync(group, "Pendeln", null);
        await viewModel.LastTask;
        Assert.Equal(["Pendeln"], viewModel.AssignedGroups.Select(g => g.Name));

        await _fixture.Service.DeleteGroupAsync(group);
        await viewModel.LastTask;
        Assert.False(viewModel.HasAssignedGroups);
    }

    /// <summary>
    /// Prüft, dass die Gruppenliste Änderungen aus anderen Ansichten übernimmt, aber erst nach dem ersten Laden reagiert.
    /// </summary>
    [Fact]
    public async Task FavoritesList_FollowsChangesAfterFirstLoad()
    {
        var viewModel = new FavoritesViewModel(_fixture.Service, new FakeFavoriteGroupNavigator(), NullLogger<FavoritesViewModel>.Instance);
        await _fixture.Service.CreateGroupAsync("Vorher", null);
        await viewModel.LastTask;
        Assert.Empty(viewModel.Groups);

        await viewModel.LoadAsync();
        await _fixture.Service.CreateGroupAsync("Nachher", null);
        await viewModel.LastTask;

        Assert.Equal(["Nachher", "Vorher"], viewModel.Groups.Select(g => g.Name));
    }

    /// <summary>
    /// Prüft, dass die Anmeldung den Besitzer nicht am Leben hält: Nach dem Freigeben meldet sie sich beim nächsten Ereignis ab, ohne Fehler.
    /// </summary>
    [Fact]
    public async Task Subscription_DoesNotKeepOwnerAlive()
    {
        var reference = CreateAbandonedViewModel();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        await _fixture.Service.CreateGroupAsync("Egal", null);

        Assert.False(reference.TryGetTarget(out _));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private WeakReference<StationFavoritesViewModel> CreateAbandonedViewModel()
    {
        return new WeakReference<StationFavoritesViewModel>(new StationFavoritesViewModel(_fixture.Service, NullLogger<StationFavoritesViewModel>.Instance));
    }
}
