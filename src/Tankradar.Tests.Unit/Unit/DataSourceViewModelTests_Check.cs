using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Karte „Datenquelle“: Quellenangabe, Schlüsselstatus und Verbindungsprüfung.
/// </summary>
public class DataSourceViewModelTests_Check : BaseTest
{
    private readonly FakeApiKeyProvider _keys = new();
    private readonly StubPriceService _service = new();

    private DataSourceViewModel CreateViewModel()
    {
        return new DataSourceViewModel(_service, _keys, NullLogger<DataSourceViewModel>.Instance);
    }

    /// <summary>
    /// Prüft die sichtbare Quellenangabe gemäß CC BY 4.0.
    /// </summary>
    [Fact]
    public void Attribution_NamesTankerkoenigAndMtsK()
    {
        var viewModel = CreateViewModel();

        Assert.Equal("Daten: Tankerkönig / MTS-K", viewModel.Attribution);
        Assert.Contains("CC BY 4.0", viewModel.License);
    }

    /// <summary>
    /// Prüft die Anzeige des Schlüsselstatus ohne den Schlüssel selbst preiszugeben.
    /// </summary>
    [Fact]
    public async Task RefreshKeyStatusAsync_ShowsPresenceOnly()
    {
        var viewModel = CreateViewModel();

        await viewModel.RefreshKeyStatusAsync();
        Assert.Equal(PriceTexts.KeyPresent, viewModel.KeyStatus);
        Assert.DoesNotContain(_keys.Value!, viewModel.KeyStatus);

        _keys.Value = null;
        await viewModel.RefreshKeyStatusAsync();
        Assert.Equal(PriceTexts.KeyMissing, viewModel.KeyStatus);
    }

    /// <summary>
    /// Prüft, dass das Ergebnis jeder Prüfung als verständliche Meldung erscheint.
    /// </summary>
    /// <param name="failure">Das Ergebnis der Prüfung.</param>
    [Theory]
    [InlineData(PriceFailure.None)]
    [InlineData(PriceFailure.Offline)]
    [InlineData(PriceFailure.Unreachable)]
    [InlineData(PriceFailure.ApiKeyMissing)]
    [InlineData(PriceFailure.Rejected)]
    [InlineData(PriceFailure.InvalidResponse)]
    public async Task CheckAsync_ShowsMessageForOutcome(PriceFailure failure)
    {
        _service.Outcome = failure;
        var viewModel = CreateViewModel();

        await viewModel.CheckAsync();

        Assert.Equal(PriceTexts.GetCheckMessage(failure), viewModel.CheckStatus);
        Assert.True(viewModel.HasCheckStatus);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass eine unerwartete Ausnahme der Prüfung als „nicht erreichbar“ gemeldet wird statt abzustürzen.
    /// </summary>
    [Fact]
    public async Task CheckAsync_ServiceThrows_ShowsUnreachable()
    {
        _service.Exception = new InvalidOperationException("kaputt");
        var viewModel = CreateViewModel();

        await viewModel.CheckAsync();

        Assert.Equal(PriceTexts.GetCheckMessage(PriceFailure.Unreachable), viewModel.CheckStatus);
        Assert.False(viewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass der Befehl die Prüfung auslöst.
    /// </summary>
    [Fact]
    public async Task CheckCommand_RunsCheck()
    {
        var viewModel = CreateViewModel();

        viewModel.CheckCommand.Execute(null);
        await viewModel.LastCheckTask;

        Assert.Equal(1, _service.Checks);
    }

    /// <summary>
    /// Prüft, dass jede Fehlermeldung einen eigenen, nicht leeren Text hat.
    /// </summary>
    [Fact]
    public void GetCheckMessage_AllOutcomes_HaveDistinctMessages()
    {
        var messages = Enum.GetValues<PriceFailure>().Select(PriceTexts.GetCheckMessage).ToList();

        Assert.All(messages, m => Assert.False(string.IsNullOrWhiteSpace(m)));
        Assert.Equal(messages.Count, messages.Distinct().Count());
    }

    /// <summary>
    /// Prüft, dass die Optionen die Karte nur anzeigen, wenn ein Preisdienst eingebunden ist.
    /// </summary>
    [Fact]
    public void SettingsViewModel_HasDataSource_OnlyWhenProvided()
    {
        var without = new SettingsViewModel(new FakeSettingsService(), NullLogger<SettingsViewModel>.Instance);
        var with = new SettingsViewModel(new FakeSettingsService(), NullLogger<SettingsViewModel>.Instance, CreateViewModel());

        Assert.False(without.HasDataSource);
        Assert.True(with.HasDataSource);
    }

    private sealed class StubPriceService : IFuelPriceService
    {
        public PriceFailure Outcome { get; set; }

        public Exception? Exception { get; set; }

        public int Checks { get; private set; }

        public Task<PriceFailure> CheckAvailabilityAsync(CancellationToken cancellationToken = default)
        {
            Checks++;
            return Exception is not null ? throw Exception : Task.FromResult(Outcome);
        }

        public Task<StationSearchResult> SearchNearbyAsync(StationSearchQuery query, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StationSearchResult([], PriceDataSource.OfflineFallback, PriceFailure.None));
        }

        public Task<StationDetailResult> GetStationDetailAsync(string stationId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new StationDetailResult(null, PriceDataSource.OfflineFallback, PriceFailure.None));
        }
    }
}
