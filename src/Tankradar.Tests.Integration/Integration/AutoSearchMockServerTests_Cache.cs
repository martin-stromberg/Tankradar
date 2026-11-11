using Tankradar.Tests.Integration.Integration.Support;

namespace Tankradar.Tests.Integration.Integration;

/// <summary>
/// Prüft, dass die Startsuche beim ersten Anzeigen der Karte denselben Schlüssel des Such-Zwischenspeichers trifft wie eine identische
/// vorherige Umkreisabfrage (z. B. des Bereichs „In der Nähe“) und dadurch ohne zweite Anfrage an den Preisdienst auskommt.
/// </summary>
public class AutoSearchMockServerTests_Cache : SearchMockServerTestBase
{
    /// <summary>
    /// Prüft, dass nach einer identischen Abfrage (Teststandort, 5 km) die Startsuche des ersten Erscheinens aus dem Zwischenspeicher
    /// bedient wird: Der Mock-Server erhält keine zweite Umkreisanfrage und das Ergebnis ist befüllt.
    /// </summary>
    [Fact]
    public async Task AutoSearch_UsesSearchCache_AfterIdenticalQuery()
    {
        var service = CreateService();
        var seeded = await service.SearchNearbyAsync(Query(5));
        Assert.True(service.IsSearchCached(Query(5)));
        Assert.Equal(1, Server.ListRequests);

        var viewModel = CreateViewModel(service);
        viewModel.OnAppearing();
        await viewModel.LastSettingsTask;
        await viewModel.LastSearchTask;

        Assert.Equal(1, Server.ListRequests);
        Assert.Equal(seeded.Stations.Count, viewModel.Stations.Count);
        Assert.NotEmpty(viewModel.Stations);
    }
}
