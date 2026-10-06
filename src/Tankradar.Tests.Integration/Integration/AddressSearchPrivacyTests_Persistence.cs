using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Tankradar.MAUI.Models.Search;
using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass nach einer Adresssuche weder die eingegebene Adresse noch die daraus ermittelte Position dauerhaft gespeichert oder protokolliert sind.
/// </summary>
public class AddressSearchPrivacyTests_Persistence : SearchMockServerTestBase
{
    private const string UniqueInput = "10115 Teststadt-Geheimviertel";

    private static readonly string[] PositionFragments = ["52.5200", "13.4050", "52,5200", "13,4050"];

    private async Task SearchAndSettleAsync()
    {
        var viewModel = CreateViewModel(CreateService());
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;
        viewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);
        viewModel.AddressText = UniqueInput;
        viewModel.RadiusKm = 5;
        await viewModel.SearchAsync();
        Assert.Equal(2, viewModel.Stations.Count);
        await viewModel.LastSettingsTask;
    }

    /// <summary>
    /// Prüft, dass kein Datenbankinhalt die Adresse oder die aufgelöste Position enthält.
    /// </summary>
    [Fact]
    public async Task AfterAddressSearch_DatabaseHoldsNoAddressOrPosition()
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
        Assert.DoesNotContain("Teststadt", rows, StringComparison.Ordinal);
        Assert.DoesNotContain("Geheimviertel", rows, StringComparison.Ordinal);
        foreach (var fragment in PositionFragments)
        {
            Assert.DoesNotContain(fragment, rows, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Prüft, dass auch die Datenbankdatei selbst weder die Adresse noch die Position (als Text oder Zahl) enthält.
    /// </summary>
    [Fact]
    public async Task AfterAddressSearch_DatabaseFileDoesNotContainAddressOrPosition()
    {
        await SearchAndSettleAsync();
        SqliteConnection.ClearAllPools();

        await using var stream = new FileStream(Database.DatabasePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var bytes = new byte[stream.Length];
        await stream.ReadExactlyAsync(bytes);
        var asText = Encoding.Latin1.GetString(bytes);
        Assert.DoesNotContain("Teststadt", asText, StringComparison.Ordinal);
        Assert.DoesNotContain("Geheimviertel", asText, StringComparison.Ordinal);
        foreach (var fragment in PositionFragments)
        {
            Assert.DoesNotContain(fragment, asText, StringComparison.Ordinal);
        }

        Assert.False(bytes.AsSpan().IndexOf(BitConverter.GetBytes(52.52)) >= 0);
        Assert.False(bytes.AsSpan().IndexOf(BitConverter.GetBytes(13.405)) >= 0);
        Assert.False(bytes.AsSpan().IndexOf(BitConverter.GetBytes(52.52).Reverse().ToArray()) >= 0);
        Assert.False(bytes.AsSpan().IndexOf(BitConverter.GetBytes(13.405).Reverse().ToArray()) >= 0);
    }

    /// <summary>
    /// Prüft, dass das Protokoll aller beteiligten Dienste weder Adresse noch Ortsnamen noch Koordinaten enthält, auch nicht bei Fehlern des Ortssuchdienstes.
    /// </summary>
    [Fact]
    public async Task AddressSearch_NeverLogsAddressOrPosition()
    {
        await SearchAndSettleAsync();
        Nominatim.EnqueueStatuses(500);
        var viewModel = CreateViewModel(CreateService());
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;
        viewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);
        viewModel.AddressText = UniqueInput;
        await viewModel.SearchAsync();

        Assert.DoesNotContain("Teststadt", LogText, StringComparison.Ordinal);
        Assert.DoesNotContain("Geheimviertel", LogText, StringComparison.Ordinal);
        Assert.DoesNotContain("Mitte", LogText, StringComparison.Ordinal);
        foreach (var fragment in PositionFragments)
        {
            Assert.DoesNotContain(fragment, LogText, StringComparison.Ordinal);
        }
    }
}
