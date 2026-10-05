using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass nach einer Suche nur Tankstellen und Preise gespeichert sind, aber weder Suchposition noch Entfernungen.
/// </summary>
public class SearchPrivacyTests_Persistence : SearchMockServerTestBase
{
    private static readonly string[] PositionFragments = ["52.5123", "13.4123", "52,5123", "13,4123"];

    private async Task SearchAndSettleAsync()
    {
        var viewModel = CreateViewModel(CreateService());
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;
        viewModel.RadiusText = "25";
        await viewModel.SearchAsync();
        Assert.Equal(4, viewModel.Stations.Count);
        await viewModel.LastSettingsTask;
    }

    /// <summary>
    /// Prüft, dass kein Datenbankfeld eine Position oder Entfernung vorsieht und alle Zeilen keine Suchposition enthalten.
    /// </summary>
    [Fact]
    public async Task AfterSearch_DatabaseHoldsStationsAndPricesButNoPositionOrDistance()
    {
        await SearchAndSettleAsync();

        var factory = Database.CreateFactory();
        await using var context = factory.CreateDbContext();
        Assert.Equal(4, await context.Stations.CountAsync());
        Assert.True(await context.PriceEntries.CountAsync() >= 4);

        var propertyNames = context.Model.GetEntityTypes().SelectMany(entity => entity.GetProperties()).Select(property => property.Name).ToList();
        Assert.DoesNotContain(propertyNames, name => name.Contains("Distance", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(propertyNames, name => name.Contains("Search", StringComparison.OrdinalIgnoreCase));

        var rows = JsonSerializer.Serialize(new
        {
            Stations = await context.Stations.AsNoTracking().ToListAsync(),
            Prices = await context.PriceEntries.AsNoTracking().Select(p => new { p.StationId, p.FuelTypeKey, p.Price, p.RetrievedUtc }).ToListAsync(),
            Settings = await context.UserSettings.AsNoTracking().ToListAsync(),
            FuelTypes = await context.FuelTypeSettings.AsNoTracking().ToListAsync(),
        });
        foreach (var fragment in PositionFragments)
        {
            Assert.DoesNotContain(fragment, rows, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Prüft, dass auch die Datenbankdatei selbst weder die Suchposition als Text noch als Zahl enthält.
    /// </summary>
    [Fact]
    public async Task AfterSearch_DatabaseFileDoesNotContainSearchPosition()
    {
        await SearchAndSettleAsync();
        SqliteConnection.ClearAllPools();

        await using var stream = new FileStream(Database.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes);
        var asText = Encoding.Latin1.GetString(bytes);
        foreach (var fragment in PositionFragments)
        {
            Assert.DoesNotContain(fragment, asText, StringComparison.Ordinal);
        }

        Assert.False(ContainsSequence(bytes, BitConverter.GetBytes(52.5123456)));
        Assert.False(ContainsSequence(bytes, BitConverter.GetBytes(13.4123456)));
        Assert.False(ContainsSequence(bytes, BitConverter.GetBytes(52.5123456).Reverse().ToArray()));
        Assert.False(ContainsSequence(bytes, BitConverter.GetBytes(13.4123456).Reverse().ToArray()));
    }

    private static bool ContainsSequence(byte[] haystack, byte[] needle)
    {
        return haystack.AsSpan().IndexOf(needle) >= 0;
    }
}
