using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft das <see cref="TankbookStatisticsViewModel"/>: Darstellung der Kennzahlen, Hinweise, Fehlerfall und Barrierefreiheits-Texte.
/// </summary>
public class TankbookStatisticsViewModelTests_Load : BaseTest
{
    private readonly TankbookEntryFixture _fixture = new();
    private readonly FakeScreenReaderAnnouncer _announcer = new();
    private readonly RecordingLogger<TankbookStatisticsViewModel> _logger = new();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fixture.Dispose();
        }

        base.Dispose(disposing);
    }

    private TankbookStatisticsViewModel CreateViewModel()
    {
        return _fixture.CreateStatistics(_announcer, _logger);
    }

    private async Task AddFuelEntriesAsync(long vehicleId, params (int DaysAgo, int Odometer, decimal Quantity)[] entries)
    {
        foreach (var (daysAgo, odometer, quantity) in entries)
        {
            await _fixture.CreateEntryAsync(vehicleId, TankbookEntryFixture.Input(_fixture.Now.AddDays(-daysAgo), odometer, quantity));
        }
    }

    /// <summary>
    /// Prüft Durchschnitt, Kosten pro 100 km, Gesamtkosten und Herkunft.
    /// </summary>
    [Fact]
    public async Task Load_ShowsAverageCostAndSource()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await AddFuelEntriesAsync(vehicle.Id, (3, 10_500, 40m), (2, 11_000, 30m));
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync(vehicle.Id);

        var kind = Assert.Single(viewModel.Kinds);
        Assert.Equal("Fuel", kind.AutomationKey);
        Assert.Equal("Kraftstoff", kind.Heading);
        Assert.Equal("6,00 L/100 km", kind.AverageText);
        Assert.Equal(TankbookStatisticsTexts.CalculatedSource, kind.AverageSourceText);
        Assert.Equal("11,15 € / 100 km", kind.CostPer100KmText);
        Assert.Equal("130,13 €", kind.TotalCostText);
        Assert.False(kind.HasNotice);
        Assert.Null(viewModel.Notice);
        Assert.False(viewModel.HasNotice);
        Assert.True(viewModel.HasKinds);
    }

    /// <summary>
    /// Prüft, dass ein Hybrid-Fahrzeug zwei getrennte Blöcke mit eigenen Einheiten erhält.
    /// </summary>
    [Fact]
    public async Task Load_Hybrid_ShowsTwoBlocks()
    {
        var vehicle = await _fixture.CreateVehicleAsync(fuelKind: VehicleFuelKind.Hybrid);
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-4), 10_500, 40m));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-3), 10_600, 15m, TankbookEntryKind.Electricity));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-2), 10_700, 20m, TankbookEntryKind.Electricity));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 11_000, 30m));
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync(vehicle.Id);

        Assert.Equal(["Fuel", "Electricity"], viewModel.Kinds.Select(kind => kind.AutomationKey));
        Assert.Equal("6,00 L/100 km", viewModel.Kinds[0].AverageText);
        Assert.Equal("Strom", viewModel.Kinds[1].Heading);
        Assert.Equal("20,00 kWh/100 km", viewModel.Kinds[1].AverageText);
    }

    /// <summary>
    /// Prüft den Hinweis ohne Vorgänge.
    /// </summary>
    [Fact]
    public async Task Load_NoEntries_ShowsNoEntriesNotice()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync(vehicle.Id);

        Assert.Empty(viewModel.Kinds);
        Assert.False(viewModel.HasKinds);
        Assert.Equal(TankbookStatisticsTexts.NoEntriesNotice, viewModel.Notice);
        Assert.True(viewModel.HasNotice);
    }

    /// <summary>
    /// Prüft den Hinweis bei nur einem vollen Vorgang ohne Startverbrauch.
    /// </summary>
    [Fact]
    public async Task Load_TooFewEntries_ShowsNotice()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await AddFuelEntriesAsync(vehicle.Id, (1, 10_500, 40m));
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync(vehicle.Id);

        var kind = Assert.Single(viewModel.Kinds);
        Assert.Contains("mindestens zwei volle Vorgänge", kind.Notice, StringComparison.Ordinal);
        Assert.Equal(TankbookStatisticsTexts.NoAverage, kind.AverageText);
        Assert.Equal(TankbookStatisticsTexts.NotCalculable, kind.CostPer100KmText);
        Assert.False(kind.HasAverageSource);
    }

    /// <summary>
    /// Prüft, dass der Startverbrauch gekennzeichnet angezeigt wird.
    /// </summary>
    [Fact]
    public async Task Load_StartConsumption_ShowsSourceLabel()
    {
        var created = await _fixture.Vehicles.CreateVehicleAsync(new VehicleInput("Golf", VehicleFuelKind.Diesel, 10_000, 5.4m));
        var vehicle = created.Vehicle!;
        await AddFuelEntriesAsync(vehicle.Id, (1, 10_500, 40m));
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync(vehicle.Id);

        var kind = Assert.Single(viewModel.Kinds);
        Assert.Equal("5,40 L/100 km", kind.AverageText);
        Assert.Equal("Startverbrauch – noch keine zwei vollen Vorgänge", kind.AverageSourceText);
        Assert.True(kind.HasAverageSource);
        Assert.False(kind.HasNotice);
        Assert.Equal(TankbookStatisticsTexts.NotCalculable, kind.CostPer100KmText);
    }

    /// <summary>
    /// Prüft, dass ein Ladefehler eine Meldung ohne Werte ergibt, angesagt und ohne Werte protokolliert wird.
    /// </summary>
    [Fact]
    public async Task Load_Failure_ShowsMessageWithoutValues()
    {
        var viewModel = new TankbookStatisticsViewModel(new ThrowingStatisticsService(), _announcer, _logger);

        await viewModel.LoadAsync(1);

        Assert.Empty(viewModel.Kinds);
        Assert.Equal(TankbookStatisticsTexts.LoadFailed, viewModel.Notice);
        Assert.Equal([TankbookStatisticsTexts.LoadFailed], _announcer.Announcements);
        Assert.Contains(nameof(InvalidOperationException), _logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("Geheim", _logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass das Zurücksetzen Blöcke und Hinweis leert.
    /// </summary>
    [Fact]
    public async Task Reset_ClearsState()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await AddFuelEntriesAsync(vehicle.Id, (2, 10_500, 40m), (1, 11_000, 30m));
        var viewModel = CreateViewModel();
        await viewModel.LoadAsync(vehicle.Id);

        viewModel.Reset();

        Assert.Empty(viewModel.Kinds);
        Assert.False(viewModel.HasKinds);
        Assert.Null(viewModel.Notice);
    }

    /// <summary>
    /// Prüft die Textzusammenfassungen der Diagramme und die Diagrammdaten.
    /// </summary>
    [Fact]
    public async Task ChartSummary_DescribesValues()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await AddFuelEntriesAsync(vehicle.Id, (3, 10_500, 40m), (2, 11_000, 30m), (1, 11_500, 25m));
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync(vehicle.Id);

        var kind = viewModel.Kinds.Single();
        Assert.Equal("Verlauf, 2 Werte, zuletzt 5,00 L/100 km, niedrigster 5,00, höchster 6,00", kind.ConsumptionChartSummary);
        Assert.StartsWith("Monatskosten, 1 Wert, zuletzt 176,61 €", kind.CostChartSummary, StringComparison.Ordinal);
        Assert.Equal(ChartKind.Line, kind.ConsumptionChart.Kind);
        Assert.Equal(2, kind.ConsumptionChart.Points.Count);
        Assert.Equal(ChartKind.Bar, kind.CostChart.Kind);
        Assert.True(kind.CostChart.HasData);
        Assert.Equal("L/100 km", kind.ConsumptionChart.Unit);
    }

    /// <summary>
    /// Prüft, dass die Monatszeilen den Monat ausgeschrieben nennen und eine gemeinsame AutomationId tragen.
    /// </summary>
    [Fact]
    public async Task Months_ShowMonthNameAndAutomationId()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await AddFuelEntriesAsync(vehicle.Id, (2, 10_500, 40m));
        var viewModel = CreateViewModel();

        await viewModel.LoadAsync(vehicle.Id);

        var row = viewModel.Kinds.Single().Months.Single();
        Assert.Equal("Oktober 2026: 74,36 €", row.Text);
        Assert.Equal("TankbookEntries.Statistics.Fuel.Month", row.AutomationId);
    }

    private sealed class ThrowingStatisticsService : ITankbookStatisticsService
    {
        public Task<TankbookStatisticsLoad> GetStatisticsAsync(long vehicleId, CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Geheim 10500");
        }
    }
}
