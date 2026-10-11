using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models.Favorites;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass bei gleichzeitigem Speichern derselben Zuordnung bzw. desselben Namens das passende Ergebnis geliefert wird (Verletzung des eindeutigen Index).
/// </summary>
public class FavoritesServiceTests_Concurrency : BaseTest
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

    private void RivalBeforeSave(Action<TankradarDbContext> insertRival)
    {
        _fixture.Factory.Interceptor = new RivalInsertInterceptor(_fixture.Factory, insertRival);
    }

    /// <summary>
    /// Prüft, dass die zweite gleichzeitige Zuordnung derselben Tankstelle zur selben Gruppe als bereits zugeordnet gemeldet wird.
    /// </summary>
    [Fact]
    public async Task AddStation_ConcurrentSameGroup_ReturnsAlreadyMember()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        var changed = 0;
        _fixture.Service.Changed += (_, _) => changed++;
        RivalBeforeSave(context =>
        {
            context.FavoriteEntries.Add(new FavoriteEntryEntity { GroupId = group, StationId = FavoritesFixture.StationA, Priority = nameof(FavoritePriority.None), AddedUtc = DateTime.UtcNow });
        });

        var result = await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);

        Assert.Equal(FavoriteResult.AlreadyMember, result);
        Assert.Equal(0, changed);
    }

    /// <summary>
    /// Prüft, dass die Zuordnung dabei nicht doppelt gezählt wird.
    /// </summary>
    [Fact]
    public async Task AddStation_ConcurrentSameGroup_DoesNotCountTwice()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        RivalBeforeSave(context =>
        {
            context.FavoriteEntries.Add(new FavoriteEntryEntity { GroupId = group, StationId = FavoritesFixture.StationA, Priority = nameof(FavoritePriority.None), AddedUtc = DateTime.UtcNow });
        });

        await _fixture.Service.AddStationAsync(group, FavoritesFixture.StationA);

        Assert.Equal(1, (await _fixture.Service.GetGroupAsync(group))!.StationCount);
    }

    /// <summary>
    /// Prüft, dass ein gleichzeitig angelegter gleicher Gruppenname weiterhin als doppelter Name gemeldet wird.
    /// </summary>
    [Fact]
    public async Task CreateGroup_ConcurrentSameName_StillDuplicateName()
    {
        RivalBeforeSave(context => context.FavoriteGroups.Add(new FavoriteGroupEntity { Name = "Heimat", CreatedUtc = DateTime.UtcNow }));

        var result = await _fixture.Service.CreateGroupAsync("Heimat", null);

        Assert.Equal(FavoriteResult.DuplicateName, result.Result);
        Assert.Null(result.Group);
    }

    /// <summary>
    /// Prüft, dass ein gleichzeitig vergebener gleicher Name beim Umbenennen weiterhin als doppelter Name gemeldet wird.
    /// </summary>
    [Fact]
    public async Task UpdateGroup_ConcurrentSameName_StillDuplicateName()
    {
        var group = (await _fixture.Service.CreateGroupAsync("Arbeitsweg", null)).Group!.Id;
        RivalBeforeSave(context => context.FavoriteGroups.Add(new FavoriteGroupEntity { Name = "Heimat", CreatedUtc = DateTime.UtcNow }));

        var result = await _fixture.Service.UpdateGroupAsync(group, "Heimat", null);

        Assert.Equal(FavoriteResult.DuplicateName, result);
    }

    private sealed class RivalInsertInterceptor : SaveChangesInterceptor
    {
        private readonly InMemoryDbContextFactory _factory;
        private readonly Action<TankradarDbContext> _insertRival;
        private bool _fired;

        public RivalInsertInterceptor(InMemoryDbContextFactory factory, Action<TankradarDbContext> insertRival)
        {
            _factory = factory;
            _insertRival = insertRival;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (!_fired)
            {
                _fired = true;
                using var rival = _factory.CreateDbContext();
                _insertRival(rival);
                rival.SaveChanges();
            }

            return ValueTask.FromResult(result);
        }
    }
}
