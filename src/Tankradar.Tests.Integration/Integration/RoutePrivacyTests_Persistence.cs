using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.ViewModels;
using Tankradar.TestSupport;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass nach einer Routensuche weder Start- und Zieltext noch Positionen oder Route dauerhaft gespeichert sind und Schema, Tabellen und Einstellungen unverändert bleiben
/// (gespeichert werden nur Tankstellen und Preise, wie bei jeder Suche).
/// </summary>
public class RoutePrivacyTests_Persistence : RouteSearchMockServerTestBase
{
    private const string UniqueStart = "10115 Teststadt-Geheimviertel";
    private const string UniqueDestination = "Oranienburg Heimlichgasse";

    private static readonly string[] Fragments = ["Teststadt", "Geheimviertel", "Heimlichgasse", "52.5200", "13.4050", "52,5200", "13,4050", "52.88", "52,88"];

    private async Task<MapViewModel> SearchAndSettleAsync()
    {
        var viewModel = await CreateRouteViewModelAsync();
        viewModel.StartInput = UniqueStart;
        viewModel.DestinationInput = UniqueDestination;
        await viewModel.SearchAsync();
        Assert.NotEmpty(viewModel.Stations);
        await viewModel.LastSettingsTask;
        return viewModel;
    }

    private async Task<(string[] Tables, string Settings)> SnapshotAsync()
    {
        var factory = Database.CreateFactory();
        await using var context = factory.CreateDbContext();
        var tables = new List<string>();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        var settings = JsonSerializer.Serialize(new
        {
            Settings = await context.UserSettings.AsNoTracking().ToListAsync(),
            FuelTypes = await context.FuelTypeSettings.AsNoTracking().ToListAsync(),
        });
        return (tables.ToArray(), settings);
    }

    /// <summary>
    /// Prüft, dass kein Datenbankinhalt Start, Ziel oder Positionen enthält.
    /// </summary>
    [Fact]
    public async Task AfterRouteSearch_DatabaseHoldsNoPlacesOrPositions()
    {
        await SearchAndSettleAsync();

        var factory = Database.CreateFactory();
        await using var context = factory.CreateDbContext();
        var rows = JsonSerializer.Serialize(new
        {
            Stations = await context.Stations.AsNoTracking().ToListAsync(),
            Prices = await context.PriceEntries.AsNoTracking().Select(p => new { p.StationId, p.FuelTypeKey, p.Price, p.RetrievedUtc }).ToListAsync(),
            Settings = await context.UserSettings.AsNoTracking().ToListAsync(),
            FuelTypes = await context.FuelTypeSettings.AsNoTracking().ToListAsync(),
        });
        foreach (var fragment in Fragments.Where(f => !f.StartsWith("52.88", StringComparison.Ordinal) && !f.StartsWith("52,88", StringComparison.Ordinal)))
        {
            Assert.DoesNotContain(fragment, rows, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Prüft, dass die Datenbankdatei selbst weder die Texte noch die Route (als Text oder kodierte Linie) enthält.
    /// </summary>
    [Fact]
    public async Task AfterRouteSearch_DatabaseFileDoesNotContainTextsOrRoute()
    {
        await SearchAndSettleAsync();
        var polyline = PolylineEncoder.Encode([(52.52, 13.405), (52.88, 13.405)]);

        await using var stream = new FileStream(Database.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes);
        var asText = Encoding.Latin1.GetString(bytes);
        foreach (var fragment in new[] { "Teststadt", "Geheimviertel", "Heimlichgasse", "directions", "driving-car", polyline[..8] })
        {
            Assert.DoesNotContain(fragment, asText, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Prüft, dass Schema (Tabellen) und Einstellungen durch die Routensuche unverändert bleiben und der Abstand zur Route nicht persistiert wird.
    /// </summary>
    [Fact]
    public async Task RouteSearch_DoesNotChangeSchemaOrSettings()
    {
        var viewModel = await CreateRouteViewModelAsync(distanceKm: 5);
        await viewModel.LastSettingsTask;
        var before = await SnapshotAsync();

        await viewModel.SearchAsync();
        await viewModel.LastSettingsTask;
        var after = await SnapshotAsync();

        Assert.Equal(before.Tables, after.Tables);
        Assert.Equal(before.Settings, after.Settings);
        Assert.DoesNotContain(after.Tables, table => table.Contains("Route", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Prüft, dass ein neu erstelltes ViewModel (frischer App-Start) keine Route, keinen Start und kein Ziel kennt.
    /// </summary>
    [Fact]
    public async Task NewViewModel_AfterRouteSearch_KnowsNoRouteOrInputs()
    {
        await SearchAndSettleAsync();

        var fresh = CreateViewModel(CreateService());
        fresh.OnAppearing();
        await fresh.LastSettingsTask;

        Assert.Equal(string.Empty, fresh.StartInput);
        Assert.Equal(string.Empty, fresh.DestinationInput);
        Assert.Null(fresh.MapRoute);
        Assert.Null(fresh.RouteSummary);
        Assert.Equal(RouteDistance.Default, fresh.RouteDistanceKm);
    }
}
