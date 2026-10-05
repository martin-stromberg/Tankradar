using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Location;
using Tankradar.Tests.Integration.Integration.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass Client, Preisdienst, Standortdienst und ViewModel bei Erfolg und bei Fehlern weder Koordinaten noch die Anfrageadresse protokollieren oder in Ausnahmen tragen.
/// </summary>
public class SearchPrivacyTests_Logging : SearchMockServerTestBase
{
    private static readonly string[] Forbidden = ["52.5123", "13.4123", "52,5123", "13,4123", "lat=", "lng=", "rad=", "list.php", MockServerKey];

    private const string MockServerKey = "api" + "key=";

    private void AssertLogIsClean()
    {
        var log = LogText;
        foreach (var fragment in Forbidden)
        {
            Assert.DoesNotContain(fragment, log, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static void AssertExceptionIsClean(Exception exception)
    {
        var text = exception.ToString();
        foreach (var fragment in Forbidden)
        {
            Assert.DoesNotContain(fragment, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Prüft den Erfolgsfall und mehrere Fehlerfälle (Serverfehler mit Wiederholung, abgelehnter Schlüssel, nicht erreichbar) über die komplette Kette.
    /// </summary>
    [Fact]
    public async Task SearchSuccessAndFailures_LogNoCoordinatesOrRequestAddress()
    {
        var viewModel = CreateViewModel(CreateService());
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;
        await viewModel.SearchAsync();

        Server.EnqueueStatuses(503, 503, 503);
        Clock.Advance(TimeSpan.FromMinutes(10));
        await viewModel.SearchAsync();

        var rejected = CreateViewModel(CreateService(key: "falscher-schluessel"));
        rejected.OnAppearing();
        await rejected.LastSettingsTask;
        Clock.Advance(TimeSpan.FromMinutes(10));
        await rejected.SearchAsync();

        var closed = StopServer();
        var unreachable = CreateViewModel(CreateService(baseUrl: closed));
        unreachable.OnAppearing();
        await unreachable.LastSettingsTask;
        Clock.Advance(TimeSpan.FromMinutes(10));
        await unreachable.SearchAsync();

        Assert.True(viewModel.HasResults || viewModel.HasStatusMessage);
        AssertLogIsClean();
        AssertNoPositionInMessages(viewModel.StatusMessage, rejected.StatusMessage, unreachable.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass Ausnahmen des Preisdienstes bei fehlendem Schlüssel oder ungültigen Eingaben keine Koordinaten oder Adressen enthalten.
    /// </summary>
    [Fact]
    public async Task ServiceExceptions_ContainNoCoordinatesOrRequestAddress()
    {
        var service = CreateService();
        var invalidRadius = await Assert.ThrowsAsync<ArgumentException>(() => service.SearchNearbyAsync(Query(99)));
        var invalidFuel = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SearchNearbyAsync(new StationSearchQuery(SearchLatitude, SearchLongitude, 5, [])));
        var outOfRange = await Assert.ThrowsAsync<ArgumentException>(
            () => service.SearchNearbyAsync(new StationSearchQuery(123.456, SearchLongitude, 5, [FuelType.Diesel])));

        AssertExceptionIsClean(invalidRadius);
        AssertExceptionIsClean(invalidFuel);
        AssertExceptionIsClean(outOfRange);
        AssertLogIsClean();
    }

    /// <summary>
    /// Prüft, dass der Plattform-Standortdienst bei Erfolg und bei Fehlern nur Statuswerte protokolliert.
    /// </summary>
    [Fact]
    public async Task LocationService_LogsNoCoordinates()
    {
        var sink = new List<string>();
        var logger = new RecordingLogger<MauiLocationService>(sink);
        var position = new GeoPosition(SearchLatitude, SearchLongitude);

        await new MauiLocationService(logger, _ => Task.FromResult(true), _ => Task.FromResult<GeoPosition?>(position)).GetCurrentLocationAsync(GpsUsage.WhileInUse);
        await new MauiLocationService(logger, _ => Task.FromResult(false), _ => Task.FromResult<GeoPosition?>(position)).GetCurrentLocationAsync(GpsUsage.WhileInUse);
        await new MauiLocationService(logger, _ => Task.FromResult(true), _ => Task.FromException<GeoPosition?>(new InvalidOperationException($"{SearchLatitude},{SearchLongitude}"))).GetCurrentLocationAsync(GpsUsage.WhileInUse);

        var log = string.Join('\n', sink);
        Assert.DoesNotContain("52.5123", log, StringComparison.Ordinal);
        Assert.DoesNotContain("13.4123", log, StringComparison.Ordinal);
    }

    private static void AssertNoPositionInMessages(params string?[] messages)
    {
        foreach (var message in messages.Where(m => m is not null))
        {
            Assert.DoesNotContain("52.5123", message!, StringComparison.Ordinal);
            Assert.DoesNotContain("13.4123", message!, StringComparison.Ordinal);
        }
    }
}
