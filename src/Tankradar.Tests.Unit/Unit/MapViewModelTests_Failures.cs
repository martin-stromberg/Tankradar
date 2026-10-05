using Tankradar.MAUI.Models.Pricing;
using Tankradar.MAUI.Resources.Texts;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Abbildung von Fehlern des Preisdienstes auf verständliche Meldungen im <see cref="MAUI.ViewModels.MapViewModel"/>.
/// </summary>
public class MapViewModelTests_Failures : MapViewModelTestBase
{
    /// <summary>
    /// Prüft, dass jeder Fehlergrund bei leerer Liste eine Meldung statt des Leerzustands ergibt.
    /// </summary>
    /// <param name="failure">Der Fehlergrund.</param>
    [Theory]
    [InlineData(PriceFailure.Offline)]
    [InlineData(PriceFailure.Unreachable)]
    [InlineData(PriceFailure.ApiKeyMissing)]
    [InlineData(PriceFailure.Rejected)]
    [InlineData(PriceFailure.InvalidResponse)]
    [InlineData(PriceFailure.EndpointNotConfigured)]
    public async Task Failure_WithEmptyList_ShowsMessageInsteadOfEmptyState(PriceFailure failure)
    {
        Prices.Result = Result(PriceDataSource.OfflineFallback, failure);
        await AppearAsync();

        await SearchAsync();

        Assert.True(ViewModel.HasStatusMessage);
        Assert.Equal(SearchTexts.GetFailureMessage(failure, hasStations: false), ViewModel.StatusMessage);
        Assert.False(ViewModel.ShowEmptyState);
        Assert.DoesNotContain(failure.ToString(), ViewModel.StatusMessage!, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft die einzelnen Meldungstexte.
    /// </summary>
    /// <param name="failure">Der Fehlergrund.</param>
    /// <param name="expected">Die erwartete Meldung.</param>
    [Theory]
    [InlineData(PriceFailure.Unreachable, "Der Preisdienst ist nicht erreichbar.")]
    [InlineData(PriceFailure.ApiKeyMissing, "Es ist kein API-Schlüssel hinterlegt.")]
    [InlineData(PriceFailure.Rejected, "Der Preisdienst hat die Anfrage abgelehnt. Bitte den API-Schlüssel prüfen.")]
    [InlineData(PriceFailure.InvalidResponse, "Der Preisdienst hat eine unerwartete Antwort geliefert.")]
    [InlineData(PriceFailure.Offline, "Keine Netzverbindung und keine gespeicherten Preise für diesen Umkreis.")]
    public void GetFailureMessage_MapsEachFailure(PriceFailure failure, string expected)
    {
        Assert.Equal(expected, SearchTexts.GetFailureMessage(failure, hasStations: false));
        Assert.Equal(string.Empty, SearchTexts.GetFailureMessage(PriceFailure.None, hasStations: false));
        Assert.Equal(SearchTexts.SearchFailed, SearchTexts.GetFailureMessage((PriceFailure)99, hasStations: false));
    }

    /// <summary>
    /// Prüft, dass bei zuletzt bekannten Preisen und Verbindungsfehler die Liste und der Fehlerhinweis erscheinen, bei „Offline“ aber nur der Banner.
    /// </summary>
    [Fact]
    public async Task Failure_WithStations_ShowsListAndMessage()
    {
        Prices.Result = Result(PriceDataSource.OfflineFallback, PriceFailure.Unreachable, TwoStations());
        await AppearAsync();
        await SearchAsync();

        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Equal("Der Preisdienst ist nicht erreichbar.", ViewModel.StatusMessage);

        Prices.Result = Result(PriceDataSource.OfflineFallback, PriceFailure.Offline, TwoStations());
        await SearchAsync();

        Assert.Equal(2, ViewModel.Stations.Count);
        Assert.Null(ViewModel.StatusMessage);
        Assert.True(ViewModel.IsOffline);
    }

    /// <summary>
    /// Prüft, dass eine ArgumentException des Dienstes abgefangen und als allgemeine Meldung ohne Ausnahmetext angezeigt wird.
    /// </summary>
    [Fact]
    public async Task ArgumentException_IsCaughtAndShownAsGeneralMessage()
    {
        Prices.SearchException = new ArgumentException("Geheimer Ausnahmetext");
        await AppearAsync();

        await SearchAsync();

        Assert.Equal(SearchTexts.SearchFailed, ViewModel.StatusMessage);
        Assert.False(ViewModel.IsBusy);
        Assert.False(ViewModel.ShowEmptyState);
        Assert.DoesNotContain("Geheimer", ViewModel.StatusMessage, StringComparison.Ordinal);
        Assert.DoesNotContain("Geheimer", Logger.AllText, StringComparison.Ordinal);
    }

    /// <summary>
    /// Prüft, dass unerwartete Ausnahmen die Anzeige nicht zerstören und ebenfalls nur die allgemeine Meldung ergeben.
    /// </summary>
    [Fact]
    public async Task UnexpectedException_ShowsGeneralMessageAndKeepsBusyFalse()
    {
        Prices.SearchException = new InvalidOperationException("intern");
        await AppearAsync();

        await SearchAsync();

        Assert.Equal(SearchTexts.SearchFailed, ViewModel.StatusMessage);
        Assert.False(ViewModel.IsBusy);
        Assert.Empty(ViewModel.Stations);
    }

    /// <summary>
    /// Prüft, dass ein Treffer ohne Fehler keinen Fehlerhinweis hat.
    /// </summary>
    [Fact]
    public async Task Success_HasNoStatusMessage()
    {
        Prices.Result = Result(PriceDataSource.Cache, PriceFailure.None, TwoStations());
        await AppearAsync();

        await SearchAsync();

        Assert.Null(ViewModel.StatusMessage);
        Assert.False(ViewModel.IsOffline);
    }
}
