using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Search;
using Tankradar.MAUI.ViewModels;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests des <see cref="StationDetailViewModel"/>: Fakes für Einstellungen, Preisdienst, Verbindung, Navigation und Uhr.
/// </summary>
public abstract class StationDetailViewModelTestBase : BaseTest
{
    /// <summary>
    /// Die Kennung der Teststation.
    /// </summary>
    protected const string StationId = "00000001-0000-4000-8000-000000000000";

    /// <summary>
    /// Erstellt den Aufbau mit einem ViewModel, das auf die Fakes zugreift.
    /// </summary>
    protected StationDetailViewModelTestBase()
    {
        Favorites = new StationFavoritesViewModel(FavoritesDb.CreateService(), NullLogger<StationFavoritesViewModel>.Instance);
        ViewModel = new StationDetailViewModel(Settings, Prices, Connection, Clock, Logger, Favorites);
    }

    /// <summary>
    /// Die Favoritendatenbank mit den Teststationen.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FavoritesFixture FavoritesDb { get; } = new();

    /// <summary>
    /// Das ViewModel der Karte „Favoritengruppen“.
    /// </summary>
    protected StationFavoritesViewModel Favorites { get; }

    /// <summary>
    /// Die Einstellungen.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeSettingsService Settings { get; } = new();

    /// <summary>
    /// Der Preisdienst.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeFuelPriceService Prices { get; } = new();

    /// <summary>
    /// Die Verbindungserkennung.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeConnectionMonitor Connection { get; } = new();

    /// <summary>
    /// Die Uhr.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected ManualTimeProvider Clock { get; } = new();

    /// <summary>
    /// Der Log-Sammler.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RecordingLogger<StationDetailViewModel> Logger { get; } = new();

    /// <summary>
    /// Das zu testende ViewModel.
    /// </summary>
    protected StationDetailViewModel ViewModel { get; }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            FavoritesDb.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Die aktuelle Uhrzeit der Testuhr in UTC.
    /// </summary>
    protected DateTime Now => Clock.UtcNow;

    /// <summary>
    /// Erzeugt die Tankstelle, wie die Detailabfrage sie liefert (alle Sorten, Öffnungszeiten, durchgehend geöffnet).
    /// </summary>
    /// <param name="retrievedUtc">Abrufzeitpunkt der Preise.</param>
    /// <param name="detailsUpdatedUtc">Abrufzeitpunkt der Detailangaben.</param>
    /// <returns>Die Tankstelle.</returns>
    protected static StationInfo CreateStation(DateTime retrievedUtc, DateTime detailsUpdatedUtc)
    {
        return new StationInfo
        {
            Id = StationId,
            Name = "Alpha Tankstelle",
            Street = "Hauptstraße",
            HouseNumber = "1",
            PostCode = "10115",
            Place = "Berlin",
            WholeDay = true,
            OpeningTimes = [new OpeningTimeEntry("Mo-So", "00:00:00", "24:00:00")],
            DetailsUpdatedUtc = detailsUpdatedUtc,
            Prices =
            [
                new FuelPrice(FuelType.SuperE5, 1.859m, retrievedUtc),
                new FuelPrice(FuelType.SuperE10, 1.799m, retrievedUtc),
                new FuelPrice(FuelType.Diesel, 1.699m, retrievedUtc),
            ],
        };
    }

    /// <summary>
    /// Erzeugt den Listeneintrag der Suche für die Teststation (Entfernung 1,8 km).
    /// </summary>
    /// <returns>Der Listeneintrag.</returns>
    protected StationListItem CreateOrigin()
    {
        var station = new StationInfo
        {
            Id = StationId,
            Name = "Alpha Tankstelle",
            DistanceKm = 1.8,
            Street = "Hauptstraße",
            HouseNumber = "1",
            PostCode = "10115",
            Place = "Berlin",
            Prices = [new FuelPrice(FuelType.SuperE5, 1.850m, Now.AddMinutes(-10))],
        };
        return StationResultBuilder.Build([station], SearchTestData.AllFuels, null, ResultSortOrder.Price, Now).Single();
    }

    /// <summary>
    /// Lässt die Seite erscheinen und wartet, bis der Ladevorgang abgeschlossen ist.
    /// </summary>
    /// <returns>Ein Task, der nach dem Laden abgeschlossen ist.</returns>
    protected async Task AppearAsync()
    {
        ViewModel.OnAppearing();
        await ViewModel.LastLoadTask;
    }
}
