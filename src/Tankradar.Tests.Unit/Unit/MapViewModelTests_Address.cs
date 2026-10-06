using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Resources.Texts;
using Tankradar.MAUI.Services.Geocoding;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Suche nach Adresse, Ort oder PLZ im <see cref="MAUI.ViewModels.MapViewModel"/>: Suchart, Eingabeprüfung vor dem Dienstaufruf,
/// Anfrage rund um die gefundene Position, Unabhängigkeit von der Standortnutzung und Zustandsänderungen beim Wechsel der Suchart.
/// </summary>
public class MapViewModelTests_Address : MapViewModelTestBase
{
    private async Task SelectAddressModeAsync()
    {
        await AppearAsync();
        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.Address).SelectCommand.Execute(null);
    }

    /// <summary>
    /// Prüft, dass die Suche zunächst am aktuellen Standort erfolgt und beide Sucharten angeboten werden.
    /// </summary>
    [Fact]
    public void Default_IsCurrentLocationMode()
    {
        Assert.False(ViewModel.IsAddressMode);
        Assert.Equal([SearchMode.CurrentLocation, SearchMode.Address], ViewModel.ModeOptions.Select(option => option.Value));
        Assert.Equal(SearchTexts.ModeCurrentLocation, ViewModel.ModeOptions[0].Label);
        Assert.Equal(SearchTexts.ModeAddress, ViewModel.ModeOptions[1].Label);
        Assert.True(ViewModel.ModeOptions[0].IsSelected);
        Assert.False(ViewModel.ModeOptions[1].IsSelected);
        Assert.Equal(SearchTexts.OsmAttribution, ViewModel.AttributionText);
    }

    /// <summary>
    /// Prüft, dass das Wählen der Suchart „Adresse“ den Adressmodus einschaltet.
    /// </summary>
    [Fact]
    public async Task SelectingAddressMode_SwitchesMode()
    {
        await SelectAddressModeAsync();

        Assert.True(ViewModel.IsAddressMode);
        Assert.True(ViewModel.ModeOptions[1].IsSelected);
        Assert.False(ViewModel.ModeOptions[0].IsSelected);
    }

    /// <summary>
    /// Prüft, dass im Adressmodus Position und Radius der Anfrage aus der aufgelösten Adresse stammen und kein Standort abgefragt wird.
    /// </summary>
    [Fact]
    public async Task AddressSearch_UsesResolvedPositionAndNeverAsksLocation()
    {
        Geocoding.Result = GeocodingResult.Success(new GeoPosition(50.1109, 8.6821), "Frankfurt am Main, Hessen, Deutschland");
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await SelectAddressModeAsync();
        ViewModel.AddressText = "Frankfurt";
        ViewModel.RadiusKm = 10;

        await SearchAsync();

        Assert.Equal(["Frankfurt"], Geocoding.Inputs);
        Assert.Empty(Location.Calls);
        var query = Assert.Single(Prices.Queries);
        Assert.Equal(50.1109, query.Latitude, 4);
        Assert.Equal(8.6821, query.Longitude, 4);
        Assert.Equal(10, query.RadiusKm);
        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Equal("Suche rund um: Frankfurt am Main, Hessen, Deutschland", ViewModel.ResolvedPlace);
        Assert.True(ViewModel.HasResolvedPlace);
        Assert.False(ViewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass die Adresssuche auch bei der Standortnutzung „Nie“ funktioniert.
    /// </summary>
    [Fact]
    public async Task AddressSearch_WorksWithGpsNever()
    {
        Settings.Stored = Settings.Stored with { GpsUsage = GpsUsage.Never };
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await SelectAddressModeAsync();
        ViewModel.AddressText = "Frankfurt";

        await SearchAsync();

        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Empty(Location.Calls);
        Assert.False(ViewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass ohne Anzeigenamen die normalisierte Eingabe als Ort angezeigt wird.
    /// </summary>
    [Fact]
    public async Task AddressSearch_WithoutPlaceName_ShowsNormalizedInput()
    {
        Geocoding.Result = GeocodingResult.Success(new GeoPosition(50.1, 8.6), null);
        await SelectAddressModeAsync();
        ViewModel.AddressText = "  Frankfurt   am Main ";

        await SearchAsync();

        Assert.Equal("Suche rund um: Frankfurt am Main", ViewModel.ResolvedPlace);
    }

    /// <summary>
    /// Prüft, dass ungültige Eingaben vor dem Dienstaufruf mit der passenden Meldung abgewiesen werden.
    /// </summary>
    /// <param name="input">Die Eingabe.</param>
    /// <param name="error">Der erwartete Grund.</param>
    [Theory]
    [InlineData("", AddressInputError.Empty)]
    [InlineData("   ", AddressInputError.Empty)]
    [InlineData("ab", AddressInputError.TooShort)]
    [InlineData("Berlin<b>", AddressInputError.InvalidCharacters)]
    public async Task AddressSearch_InvalidInput_CallsNeitherGeocodingNorPrices(string input, AddressInputError error)
    {
        await SelectAddressModeAsync();
        ViewModel.AddressText = input;

        await SearchAsync();

        Assert.Empty(Geocoding.Inputs);
        Assert.Empty(Prices.Queries);
        Assert.Empty(Location.Calls);
        Assert.Equal(SearchTexts.GetAddressInputMessage(error), ViewModel.StatusMessage);
        Assert.False(ViewModel.IsBusy);
    }

    /// <summary>
    /// Prüft, dass eine zu lange Eingabe abgewiesen wird.
    /// </summary>
    [Fact]
    public async Task AddressSearch_TooLongInput_IsRejected()
    {
        await SelectAddressModeAsync();
        ViewModel.AddressText = new string('a', AddressInput.MaxLength + 1);

        await SearchAsync();

        Assert.Empty(Geocoding.Inputs);
        Assert.Equal(SearchTexts.AddressTooLong, ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein ungültiger Radius auch im Adressmodus vor dem Dienstaufruf abgewiesen wird.
    /// </summary>
    [Fact]
    public async Task AddressSearch_InvalidRadius_CallsNoService()
    {
        await SelectAddressModeAsync();
        ViewModel.AddressText = "Frankfurt";
        ViewModel.RadiusKm = 26;

        await SearchAsync();

        Assert.Empty(Geocoding.Inputs);
        Assert.Equal(SearchTexts.RadiusInvalid, ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ohne Verbindung keine Anfrage an den Ortssuchdienst geht und eine verständliche Meldung erscheint.
    /// </summary>
    [Fact]
    public async Task AddressSearch_Offline_SendsNoGeocodingRequest()
    {
        Connection.IsOnline = false;
        await SelectAddressModeAsync();
        ViewModel.AddressText = "Frankfurt";

        await SearchAsync();

        Assert.Empty(Geocoding.Inputs);
        Assert.Empty(Prices.Queries);
        Assert.Equal(SearchTexts.AddressOffline, ViewModel.StatusMessage);
        Assert.False(ViewModel.ShowEmptyState);
    }

    /// <summary>
    /// Prüft die Meldungen zu jedem nicht erfolgreichen Ausgang der Adressauflösung; es folgt keine Preisabfrage.
    /// </summary>
    /// <param name="status">Der Ausgang.</param>
    [Theory]
    [InlineData(GeocodingStatus.NotFound)]
    [InlineData(GeocodingStatus.InvalidInput)]
    [InlineData(GeocodingStatus.Unavailable)]
    [InlineData(GeocodingStatus.Rejected)]
    [InlineData(GeocodingStatus.InvalidResponse)]
    [InlineData(GeocodingStatus.EndpointNotConfigured)]
    public async Task AddressSearch_GeocodingFailure_ShowsMessageAndSkipsPrices(GeocodingStatus status)
    {
        Geocoding.Result = GeocodingResult.Failure(status);
        await SelectAddressModeAsync();
        ViewModel.AddressText = "Frankfurt";

        await SearchAsync();

        Assert.Empty(Prices.Queries);
        Assert.Equal(SearchTexts.GetGeocodingMessage(status, isOnline: true), ViewModel.StatusMessage);
        Assert.NotEmpty(ViewModel.StatusMessage!);
        Assert.False(ViewModel.ShowEmptyState);
        Assert.Empty(ViewModel.Stations);
        Assert.False(ViewModel.HasResolvedPlace);
    }

    /// <summary>
    /// Prüft, dass ein Fehlschlag die zuvor angezeigten Ergebnisse und den Ort entfernt.
    /// </summary>
    [Fact]
    public async Task AddressSearch_FailureAfterSuccess_ClearsResults()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await SelectAddressModeAsync();
        ViewModel.AddressText = "Frankfurt";
        await SearchAsync();
        Assert.Equal(2, ViewModel.Stations.Count);

        Geocoding.Result = GeocodingResult.Failure(GeocodingStatus.NotFound);
        ViewModel.AddressText = "Nirgendwoburg";
        await SearchAsync();

        Assert.Empty(ViewModel.Stations);
        Assert.Null(ViewModel.ResolvedPlace);
        Assert.Equal(SearchTexts.AddressNotFound, ViewModel.StatusMessage);
    }

    /// <summary>
    /// Prüft, dass ein unerwarteter Fehler eine allgemeine Meldung ergibt, die weder Eingabe noch Ausnahmetext enthält.
    /// </summary>
    [Fact]
    public async Task AddressSearch_UnexpectedException_ShowsGenericMessageWithoutInput()
    {
        Geocoding.Exception = new InvalidOperationException("Fehler bei Geheimstrasse 7");
        await SelectAddressModeAsync();
        ViewModel.AddressText = "Geheimstrasse 7";

        await SearchAsync();

        Assert.Equal(SearchTexts.SearchFailed, ViewModel.StatusMessage);
        Assert.DoesNotContain("Geheimstrasse", Logger.AllText, StringComparison.Ordinal);
        Assert.DoesNotContain("Geheimstrasse", ViewModel.StatusMessage!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass der Wechsel der Suchart Ergebnisse, Ort und Meldung entfernt, die Eingabe aber stehen bleibt.
    /// </summary>
    [Fact]
    public async Task SwitchingMode_ClearsResultsAndStatusButKeepsInput()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await SelectAddressModeAsync();
        ViewModel.AddressText = "Frankfurt";
        await SearchAsync();
        Assert.NotEmpty(ViewModel.Stations);

        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.CurrentLocation).SelectCommand.Execute(null);

        Assert.False(ViewModel.IsAddressMode);
        Assert.Empty(ViewModel.Stations);
        Assert.Null(ViewModel.ResolvedPlace);
        Assert.Null(ViewModel.StatusMessage);
        Assert.Equal("Frankfurt", ViewModel.AddressText);
    }

    /// <summary>
    /// Prüft, dass nach dem Wechsel zurück zur Standortsuche wieder der Standort verwendet wird.
    /// </summary>
    [Fact]
    public async Task SwitchingBackToCurrentLocation_UsesLocationAgain()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await SelectAddressModeAsync();
        ViewModel.ModeOptions.Single(option => option.Value == SearchMode.CurrentLocation).SelectCommand.Execute(null);

        await SearchAsync();

        Assert.Single(Location.Calls);
        Assert.Empty(Geocoding.Inputs);
    }

    /// <summary>
    /// Prüft das Löschen der Eingabe und den Zustand der Löschen-Schaltfläche.
    /// </summary>
    [Fact]
    public async Task ClearAddress_EmptiesInputAndStatus()
    {
        await SelectAddressModeAsync();
        Assert.False(ViewModel.HasAddressText);
        ViewModel.AddressText = "ab";
        await SearchAsync();
        Assert.True(ViewModel.HasAddressText);
        Assert.True(ViewModel.HasStatusMessage);

        ViewModel.ClearAddressCommand.Execute(null);

        Assert.Equal(string.Empty, ViewModel.AddressText);
        Assert.False(ViewModel.HasAddressText);
        Assert.False(ViewModel.HasStatusMessage);
    }

    /// <summary>
    /// Prüft, dass die Filterung und Sortierung der Adresssuche wie bei der Standortsuche ohne erneuten Dienstaufruf arbeitet.
    /// </summary>
    [Fact]
    public async Task AddressSearch_FilterAndSort_DoNotCallServicesAgain()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await SelectAddressModeAsync();
        ViewModel.AddressText = "Frankfurt";
        await SearchAsync();

        ViewModel.SortOptions.Single(option => option.Value == ResultSortOrder.Name).SelectCommand.Execute(null);
        ViewModel.FuelFilterOptions.Single(option => option.FuelType == FuelType.Diesel).SelectCommand.Execute(null);

        Assert.Single(Geocoding.Inputs);
        Assert.Single(Prices.Queries);
        Assert.Single(ViewModel.Stations);
    }

    /// <summary>
    /// Prüft, dass die Adresseingabe und die aufgelöste Position nirgends gespeichert werden (keine Speicheraufrufe der Einstellungen).
    /// </summary>
    [Fact]
    public async Task AddressSearch_DoesNotPersistAnything()
    {
        Prices.Result = Result(PriceDataSource.Live, PriceFailure.None, TwoStations());
        await SelectAddressModeAsync();
        ViewModel.AddressText = "Frankfurt";

        await SearchAsync();

        Assert.Empty(Settings.Saved);
    }
}
