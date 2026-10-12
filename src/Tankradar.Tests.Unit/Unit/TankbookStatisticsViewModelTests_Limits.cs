using Tankradar.MAUI.Models.Tankbook;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Tankbook;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft Begrenzung der Monatsliste und das Verwerfen überholter Ladevorgänge im <see cref="TankbookStatisticsViewModel"/>.
/// </summary>
public class TankbookStatisticsViewModelTests_Limits : BaseTest
{
    private readonly TankbookEntryFixture _fixture = new();

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
    /// Prüft, dass bei weit zurückliegenden Vorgängen nur die letzten 24 Monate angezeigt werden, Gesamtkosten und Durchschnitt aber vollständig bleiben.
    /// </summary>
    [Fact]
    public async Task Months_VeryOldEntries_ListIsLimitedButTotalsAreComplete()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(new DateTime(1995, 3, 10, 8, 0, 0), 10_500, 40m));
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 11_000, 30m));
        var viewModel = _fixture.CreateStatistics(new FakeScreenReaderAnnouncer(), new RecordingLogger<TankbookStatisticsViewModel>());

        await viewModel.LoadAsync(vehicle.Id);

        var kind = viewModel.Kinds.Single();
        Assert.Equal(TankbookStatisticsLimits.MonthListMaxRows, kind.Months.Count);
        Assert.Equal(TankbookStatisticsTexts.MonthsLimitedNotice, kind.MonthsNotice);
        Assert.True(kind.HasMonthsNotice);
        Assert.Equal("130,13 €", kind.TotalCostText);
        Assert.Equal("6,00 L/100 km", kind.AverageText);
        Assert.StartsWith("Oktober 2026:", kind.Months[0].Text, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass eine kurze Monatsliste ohne Hinweis erscheint.
    /// </summary>
    [Fact]
    public async Task Months_FewEntries_NoLimitNotice()
    {
        var vehicle = await _fixture.CreateVehicleAsync();
        await _fixture.CreateEntryAsync(vehicle.Id, TankbookEntryFixture.Input(_fixture.Now.AddDays(-1), 11_000, 30m));
        var viewModel = _fixture.CreateStatistics(new FakeScreenReaderAnnouncer(), new RecordingLogger<TankbookStatisticsViewModel>());

        await viewModel.LoadAsync(vehicle.Id);

        Assert.False(viewModel.Kinds.Single().HasMonthsNotice);
    }

    /// <summary>
    /// Prüft, dass das späte Ergebnis eines überholten Ladevorgangs (anderes Fahrzeug, neuerer Aufruf) die Anzeige nicht überschreibt.
    /// </summary>
    [Fact]
    public async Task Load_SupersededResult_IsDiscarded()
    {
        var service = new GatedStatisticsService();
        var viewModel = new TankbookStatisticsViewModel(service, new FakeScreenReaderAnnouncer(), new RecordingLogger<TankbookStatisticsViewModel>());

        var first = viewModel.LoadAsync(1);
        var second = viewModel.LoadAsync(2);
        service.Complete(2, new TankbookStatisticsLoad(TankbookStatisticsStatus.Ok, new TankbookStatistics(2, [])));
        await second;
        service.Complete(1, new TankbookStatisticsLoad(TankbookStatisticsStatus.Ok, new TankbookStatistics(1, [Kind()])));
        await first;

        Assert.Empty(viewModel.Kinds);
        Assert.Equal(TankbookStatisticsTexts.NoEntriesNotice, viewModel.Notice);
    }

    /// <summary>
    /// Prüft, dass ein Zurücksetzen während des Ladens das späte Ergebnis verwirft.
    /// </summary>
    [Fact]
    public async Task Load_ResetWhileLoading_DiscardsResult()
    {
        var service = new GatedStatisticsService();
        var viewModel = new TankbookStatisticsViewModel(service, new FakeScreenReaderAnnouncer(), new RecordingLogger<TankbookStatisticsViewModel>());

        var load = viewModel.LoadAsync(1);
        viewModel.Reset();
        service.Complete(1, new TankbookStatisticsLoad(TankbookStatisticsStatus.Ok, new TankbookStatistics(1, [Kind()])));
        await load;

        Assert.False(viewModel.HasKinds);
        Assert.Null(viewModel.Notice);
    }

    private static TankbookKindStatistics Kind()
    {
        return new TankbookKindStatistics(TankbookEntryKind.Fuel, 6m, TankbookAverageSource.Calculated, 9m, 1, 500, 100m, [], []);
    }

    private sealed class GatedStatisticsService : ITankbookStatisticsService
    {
        private readonly Dictionary<long, TaskCompletionSource<TankbookStatisticsLoad>> _gates = [];

        public Task<TankbookStatisticsLoad> GetStatisticsAsync(long vehicleId, CancellationToken cancellationToken = default)
        {
            var gate = new TaskCompletionSource<TankbookStatisticsLoad>(TaskCreationOptions.RunContinuationsAsynchronously);
            _gates[vehicleId] = gate;
            return gate.Task;
        }

        public void Complete(long vehicleId, TankbookStatisticsLoad load)
        {
            _gates[vehicleId].SetResult(load);
        }
    }
}
