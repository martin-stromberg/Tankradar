using Tankradar.MAUI.Data;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Services;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Laden und Speichern von <see cref="SettingsService"/> auf einer SQLite-In-Memory-Datenbank mit echten Migrationen.
/// </summary>
public class SettingsServiceTests_LoadSave : BaseTest
{
    private readonly InMemoryDbContextFactory _factory = new();
    private readonly SettingsService _service;

    /// <summary>
    /// Erstellt Dienst und Datenbank für einen Test.
    /// </summary>
    public SettingsServiceTests_LoadSave()
    {
        var initializer = new DatabaseInitializer(
            _factory,
            new FakeAppDataPathProvider(Path.GetTempPath()),
            new FakeDatabaseFileProtector());
        _service = new SettingsService(_factory, initializer);
    }

    /// <summary>
    /// Prüft, dass ohne gespeicherte Daten die Standardwerte geliefert werden.
    /// </summary>
    [Fact]
    public async Task LoadAsync_WithoutData_ReturnsDefaults()
    {
        var settings = await _service.LoadAsync();

        var defaults = AppSettings.CreateDefault();
        Assert.Equal(defaults.FuelTypes, settings.FuelTypes);
        Assert.Equal(defaults.GpsUsage, settings.GpsUsage);
        Assert.Equal(defaults.ResultView, settings.ResultView);
        Assert.Equal(defaults.ResultSortOrder, settings.ResultSortOrder);
    }

    /// <summary>
    /// Prüft, dass jeder Enum-Wert gespeichert und wieder geladen wird.
    /// </summary>
    [Fact]
    public async Task SaveThenLoad_RoundTrips_EveryEnumValue()
    {
        foreach (var gps in Enum.GetValues<GpsUsage>())
        {
            foreach (var view in Enum.GetValues<ResultView>())
            {
                foreach (var sort in Enum.GetValues<ResultSortOrder>())
                {
                    await _service.SaveAsync(AppSettings.CreateDefault() with { GpsUsage = gps, ResultView = view, ResultSortOrder = sort });

                    var loaded = await _service.LoadAsync();

                    Assert.Equal(gps, loaded.GpsUsage);
                    Assert.Equal(view, loaded.ResultView);
                    Assert.Equal(sort, loaded.ResultSortOrder);
                }
            }
        }
    }

    /// <summary>
    /// Prüft, dass Auswahl und Reihenfolge der Spritsorten erhalten bleiben.
    /// </summary>
    [Fact]
    public async Task SaveThenLoad_RoundTrips_FuelTypeOrderAndSelection()
    {
        var saved = AppSettings.CreateDefault() with
        {
            FuelTypes =
            [
                new FuelTypeSelection(FuelType.Diesel, true),
                new FuelTypeSelection(FuelType.SuperE5, false),
                new FuelTypeSelection(FuelType.SuperE10, true),
            ],
        };

        await _service.SaveAsync(saved);
        var loaded = await _service.LoadAsync();

        Assert.Equal(saved.FuelTypes, loaded.FuelTypes);
    }

    /// <summary>
    /// Prüft, dass ein ungültig gespeicherter Text auf den Standardwert zurückfällt.
    /// </summary>
    [Fact]
    public async Task Load_InvalidStoredValue_FallsBackToDefault()
    {
        await _service.SaveAsync(AppSettings.CreateDefault());
        await using (var context = _factory.CreateDbContext())
        {
            var row = context.UserSettings.Single();
            row.GpsUsage = "Bogus";
            row.ResultView = "42";
            await context.SaveChangesAsync();
        }

        var loaded = await _service.LoadAsync();

        Assert.Equal(GpsUsage.WhileInUse, loaded.GpsUsage);
        Assert.Equal(ResultView.List, loaded.ResultView);
    }

    /// <summary>
    /// Prüft, dass unbekannte Spritsorten beim Laden ignoriert und beim Speichern unverändert erhalten werden.
    /// </summary>
    [Fact]
    public async Task Load_UnknownFuelTypeKey_IsIgnoredAndKept()
    {
        await _service.SaveAsync(AppSettings.CreateDefault());
        await using (var context = _factory.CreateDbContext())
        {
            context.FuelTypeSettings.Add(new FuelTypeSettingEntity { FuelTypeKey = "Hydrogen", SortOrder = 0, IsSelected = true });
            await context.SaveChangesAsync();
        }

        var loaded = await _service.LoadAsync();
        await _service.SaveAsync(loaded);

        Assert.Equal(AppSettings.CreateDefault().FuelTypes, loaded.FuelTypes);
        await using var verify = _factory.CreateDbContext();
        var hydrogen = verify.FuelTypeSettings.Single(row => row.FuelTypeKey == "Hydrogen");
        Assert.Equal(0, hydrogen.SortOrder);
        Assert.True(hydrogen.IsSelected);
    }

    /// <summary>
    /// Prüft, dass eine in der Datenbank fehlende Spritsorte unausgewählt ans Ende angehängt wird.
    /// </summary>
    [Fact]
    public async Task Load_NewFuelTypeMissingInDatabase_AppendedUnselected()
    {
        await using (var context = _factory.CreateDbContext())
        {
            await _service.SaveAsync(AppSettings.CreateDefault());
            context.FuelTypeSettings.RemoveRange(context.FuelTypeSettings.Where(row => row.FuelTypeKey == nameof(FuelType.SuperE10)));
            await context.SaveChangesAsync();
        }

        var loaded = await _service.LoadAsync();

        Assert.Equal([FuelType.SuperE5, FuelType.Diesel, FuelType.SuperE10], loaded.FuelTypes.Select(f => f.FuelType));
        Assert.False(loaded.FuelTypes[2].IsSelected);
    }

    /// <summary>
    /// Prüft, dass das Speichern ohne ausgewählte Spritsorte abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task Save_ZeroSelected_Throws()
    {
        var settings = AppSettings.CreateDefault() with
        {
            FuelTypes = AppSettings.CreateDefault().FuelTypes.Select(f => f with { IsSelected = false }).ToList(),
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.SaveAsync(settings));
    }

    /// <summary>
    /// Prüft, dass wiederholtes Speichern dieselben Zeilen aktualisiert statt neue anzulegen.
    /// </summary>
    [Fact]
    public async Task Save_Twice_UpdatesSameRows()
    {
        await _service.SaveAsync(AppSettings.CreateDefault());
        await _service.SaveAsync(AppSettings.CreateDefault() with { GpsUsage = GpsUsage.Never });

        await using var context = _factory.CreateDbContext();
        Assert.Equal(1, context.UserSettings.Count());
        Assert.Equal(3, context.FuelTypeSettings.Count());
        Assert.Equal("Never", context.UserSettings.Single().GpsUsage);
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _factory.Dispose();
        }

        base.Dispose(disposing);
    }
}
