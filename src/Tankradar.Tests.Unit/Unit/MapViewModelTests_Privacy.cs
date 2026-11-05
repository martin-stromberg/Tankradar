using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass das <see cref="MAUI.ViewModels.MapViewModel"/> Koordinaten weder protokolliert noch in Meldungen oder Ausnahmetexten ausgibt.
/// </summary>
public class MapViewModelTests_Privacy : MapViewModelTestBase
{
    private const double Latitude = 52.5200123;
    private const double Longitude = 13.4050456;

    private static readonly string[] Forbidden = ["52.5200", "13.4050", "52,5200", "13,4050", "52.52001", "13.40504"];

    private void AssertNoCoordinates(string text)
    {
        foreach (var fragment in Forbidden)
        {
            Assert.DoesNotContain(fragment, text, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Prüft, dass weder bei Erfolg noch bei Fehlern Koordinaten in Logs oder im Status auftauchen.
    /// </summary>
    [Fact]
    public async Task Search_NeverLeaksCoordinatesIntoLogsOrStatus()
    {
        Location.Result = LocationResult.Success(new GeoPosition(Latitude, Longitude));
        await AppearAsync();

        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await SearchAsync();
        Prices.SearchException = new InvalidOperationException($"Anfrage lat={Latitude} lng={Longitude} fehlgeschlagen");
        await SearchAsync();
        Prices.SearchException = new ArgumentException($"{Latitude},{Longitude}");
        await SearchAsync();

        AssertNoCoordinates(Logger.AllText);
        AssertNoCoordinates(ViewModel.StatusMessage ?? string.Empty);
    }

    /// <summary>
    /// Prüft, dass das Ergebnis der Aufbereitung keine Suchposition enthält (nur Tankstellen, Preise, Entfernungen).
    /// </summary>
    [Fact]
    public async Task Stations_DoNotContainSearchPosition()
    {
        Location.Result = LocationResult.Success(new GeoPosition(Latitude, Longitude));
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await AppearAsync();

        await SearchAsync();

        var text = string.Join('\n', ViewModel.Stations.Select(item => item.ToString()));
        AssertNoCoordinates(text);
        Assert.DoesNotContain(typeof(GeoPosition), ViewModel.GetType().GetProperties().Select(property => property.PropertyType));
    }

    /// <summary>
    /// Prüft, dass das ViewModel keine Position in einem Feld ablegt.
    /// </summary>
    [Fact]
    public void ViewModel_HasNoFieldHoldingAPosition()
    {
        var fields = typeof(MAUI.ViewModels.MapViewModel).GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        Assert.DoesNotContain(fields, field => field.FieldType == typeof(GeoPosition) || field.FieldType == typeof(LocationResult));
    }
}
