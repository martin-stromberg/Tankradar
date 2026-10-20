using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft, dass die Routensuche alle Eingaben und Voraussetzungen vor jedem externen Aufruf prüft und dabei weder Standortdienst noch Routensuchdienst anspricht.
/// </summary>
public class MapViewModelTests_RouteValidation : MapViewModelRouteTestBase
{
    private void AssertNothingCalled()
    {
        Assert.Empty(RouteSearch.Requests);
        Assert.Empty(Location.Calls);
        Assert.Empty(Geocoding.Inputs);
        Assert.Empty(Prices.Queries);
        Assert.False(ViewModel.IsBusy);
        Assert.False(ViewModel.HasMapRoute);
    }

    /// <summary>
    /// Prüft, dass ein ungültiges Ziel mit einer Meldung zum Ziel abgelehnt wird.
    /// </summary>
    /// <param name="destination">Die Eingabe.</param>
    /// <param name="error">Der erwartete Eingabefehler.</param>
    [Theory]
    [InlineData("", AddressInputError.Empty)]
    [InlineData("   ", AddressInputError.Empty)]
    [InlineData("ab", AddressInputError.TooShort)]
    [InlineData("<script>", AddressInputError.InvalidCharacters)]
    public async Task Search_InvalidDestination_ShowsMessageWithoutCalls(string destination, AddressInputError error)
    {
        await PrepareRouteAsync();
        ViewModel.DestinationInput = destination;

        await SearchAsync();

        Assert.Equal(SearchTexts.GetRouteInputMessage(isStart: false, error), ViewModel.StatusMessage);
        Assert.StartsWith("Ziel: ", ViewModel.StatusMessage, StringComparison.Ordinal);
        AssertNothingCalled();
    }

    /// <summary>
    /// Prüft, dass ein zu langes Ziel abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task Search_TooLongDestination_ShowsMessageWithoutCalls()
    {
        await PrepareRouteAsync();
        ViewModel.DestinationInput = new string('a', 121);

        await SearchAsync();

        Assert.Equal(SearchTexts.GetRouteInputMessage(isStart: false, AddressInputError.TooLong), ViewModel.StatusMessage);
        AssertNothingCalled();
    }

    /// <summary>
    /// Prüft, dass ein ungültiger Start (Adresse) mit einer Meldung zum Start abgelehnt wird, auch wenn das Ziel gültig ist.
    /// </summary>
    [Fact]
    public async Task Search_InvalidStartAddress_ShowsMessageWithoutCalls()
    {
        await PrepareRouteAsync();
        ViewModel.StartInput = "x";

        await SearchAsync();

        Assert.Equal(SearchTexts.GetRouteInputMessage(isStart: true, AddressInputError.TooShort), ViewModel.StatusMessage);
        Assert.StartsWith("Start: ", ViewModel.StatusMessage, StringComparison.Ordinal);
        AssertNothingCalled();
    }

    /// <summary>
    /// Prüft, dass beim Start am aktuellen Standort der (leere) Starttext nicht geprüft wird.
    /// </summary>
    [Fact]
    public async Task Search_CurrentLocationStart_DoesNotValidateStartText()
    {
        await PrepareRouteAsync(startAddress: false);
        ViewModel.StartInput = "x";
        RouteSearch.Result = RouteSuccess(null, "Oranienburg");

        await SearchAsync();

        Assert.Single(RouteSearch.Requests);
        Assert.Null(ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein ungültiger Abstand abgelehnt wird.
    /// </summary>
    [Fact]
    public async Task Search_InvalidDistance_ShowsMessageWithoutCalls()
    {
        await PrepareRouteAsync();
        ViewModel.RouteDistanceKm = 3;

        await SearchAsync();

        Assert.Equal(SearchTexts.RouteDistanceInvalid, ViewModel.StatusMessage);
        AssertNothingCalled();
    }

    /// <summary>
    /// Prüft, dass ohne Verbindung die Meldung erscheint und nichts aufgerufen wird.
    /// </summary>
    [Fact]
    public async Task Search_Offline_ShowsOfflineMessageWithoutCalls()
    {
        await PrepareRouteAsync();
        Connection.IsOnline = false;

        await SearchAsync();

        Assert.Equal(SearchTexts.RouteOffline, ViewModel.StatusMessage);
        AssertNothingCalled();
    }

    /// <summary>
    /// Prüft, dass bei abgeschalteter Standortnutzung der Start am Standort gemeldet wird, ohne den Routensuchdienst aufzurufen.
    /// </summary>
    [Fact]
    public async Task Search_CurrentLocationWithGpsNever_ShowsLocationMessageWithoutRouteCall()
    {
        Settings.Stored = Settings.Stored with { GpsUsage = GpsUsage.Never };
        await PrepareRouteAsync(startAddress: false);
        Location.Result = LocationResult.Failure(LocationStatus.DisabledBySetting);

        await SearchAsync();

        Assert.Equal(SearchTexts.GetLocationMessage(LocationStatus.DisabledBySetting), ViewModel.StatusMessage);
        Assert.Equal([GpsUsage.Never], Location.Calls);
        Assert.Empty(RouteSearch.Requests);
        Assert.Empty(Geocoding.Inputs);
        Assert.False(ViewModel.HasMapRoute);
    }

    /// <summary>
    /// Prüft, dass ein verweigerter Standort ebenfalls ohne Routenaufruf gemeldet wird.
    /// </summary>
    [Fact]
    public async Task Search_CurrentLocationPermissionDenied_ShowsLocationMessage()
    {
        await PrepareRouteAsync(startAddress: false);
        Location.Result = LocationResult.Failure(LocationStatus.PermissionDenied);

        await SearchAsync();

        Assert.Equal(SearchTexts.LocationPermissionDenied, ViewModel.StatusMessage);
        Assert.Empty(RouteSearch.Requests);
    }

    /// <summary>
    /// Prüft, dass ein früheres Routenergebnis bei ungültiger Eingabe entfernt wird.
    /// </summary>
    [Fact]
    public async Task Search_InvalidInputAfterSuccess_ClearsPreviousResult()
    {
        await PrepareRouteAsync();
        RouteSearch.Result = RouteSuccess("A", "B", RouteStationAt(1, "Erste", 4.2, 0.8, 1.90m, 1.70m));
        await SearchAsync();
        Assert.True(ViewModel.HasMapRoute);

        ViewModel.DestinationInput = string.Empty;
        await SearchAsync();

        Assert.False(ViewModel.HasMapRoute);
        Assert.Empty(ViewModel.Stations);
        Assert.Null(ViewModel.RouteSummary);
        Assert.Single(RouteSearch.Requests);
    }
}
