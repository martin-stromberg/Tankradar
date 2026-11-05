using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.ViewModels;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass Start, Ziel, Positionen und Route weder gespeichert noch protokolliert werden und nur im Arbeitsspeicher des <see cref="MapViewModel"/> liegen.
/// </summary>
public class MapViewModelTests_RoutePrivacy : MapViewModelRouteTestBase
{
    private const string SecretStart = "Geheimweg 7 Teststadt";
    private const string SecretDestination = "Heimlichgasse 9 Beispielort";

    /// <summary>
    /// Prüft, dass bei Erfolg und bei Fehlern weder Orte noch Koordinaten im Protokoll oder in Meldungen stehen und nichts gespeichert wird.
    /// </summary>
    [Fact]
    public async Task Search_NeverLogsOrStoresPlacesOrCoordinates()
    {
        await PrepareRouteAsync();
        ViewModel.StartInput = SecretStart;
        ViewModel.DestinationInput = SecretDestination;

        RouteSearch.Result = RouteSuccess("Teststadt", "Beispielort", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        await SearchAsync();
        RouteSearch.Exception = new InvalidOperationException($"{SecretStart} -> {SecretDestination} 52.52 13.405");
        await SearchAsync();
        RouteSearch.Exception = null;
        RouteSearch.Result = RouteSearchResult.FailRouting(RoutingFailure.Unreachable);
        await SearchAsync();

        foreach (var fragment in new[] { "Geheimweg", "Heimlichgasse", "52.52", "13.405", "52,52", "13,405" })
        {
            Assert.DoesNotContain(fragment, Logger.AllText, StringComparison.Ordinal);
            Assert.DoesNotContain(fragment, ViewModel.StatusMessage ?? string.Empty, StringComparison.Ordinal);
        }

        Assert.Empty(Settings.Saved);
    }

    /// <summary>
    /// Prüft, dass das ViewModel keine Position in einem Feld oder einer Eigenschaft ablegt (auch nicht im Routenmodus).
    /// </summary>
    [Fact]
    public void ViewModel_HasNoMemberHoldingAPositionOrLocation()
    {
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;

        Assert.DoesNotContain(typeof(MapViewModel).GetFields(flags), field => field.FieldType == typeof(GeoPosition) || field.FieldType == typeof(LocationResult) || field.FieldType == typeof(RouteSearchRequest));
        Assert.DoesNotContain(typeof(MapViewModel).GetProperties(flags), property => property.PropertyType == typeof(GeoPosition) || property.PropertyType == typeof(RouteProfile));
    }

    /// <summary>
    /// Prüft, dass die Textdarstellung der Routenobjekte weder Koordinaten noch Orte nennt.
    /// </summary>
    [Fact]
    public async Task RouteObjects_ToString_DoesNotLeak()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess("Teststadt", "Beispielort", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        await SearchAsync();

        Assert.DoesNotContain("52", ViewModel.MapRoute!.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("52", RouteSearch.Requests.Single().ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("Teststadt", RouteSearch.Result.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass die Einstellungen durch eine Routensuche nicht verändert werden (der Abstand ist nicht persistent).
    /// </summary>
    [Fact]
    public async Task Search_DoesNotChangeSettings()
    {
        await PrepareRouteAsync();
        var before = Settings.Stored;
        ViewModel.RouteDistanceKm = 10;
        RouteSearch.Result = RouteSuccess("A", "B");

        await SearchAsync();

        Assert.Same(before, Settings.Stored);
        Assert.Empty(Settings.Saved);
    }
}
