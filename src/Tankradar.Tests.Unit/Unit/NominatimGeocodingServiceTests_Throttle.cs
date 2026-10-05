using System.Net;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Prüft die Einhaltung der Anfragebegrenzung der Nominatim-Nutzungsrichtlinie (höchstens eine Anfrage je Sekunde).
/// </summary>
public class NominatimGeocodingServiceTests_Throttle : NominatimGeocodingServiceTestBase
{
    /// <summary>
    /// Prüft, dass die erste Anfrage sofort geht und eine zweite innerhalb einer Sekunde bis zum Mindestabstand wartet.
    /// </summary>
    [Fact]
    public async Task Resolve_TwoRequestsWithinSecond_SecondWaits()
    {
        Handler.RespondWith(HttpStatusCode.OK, FoundBody);
        var service = CreateService();

        await service.ResolveAsync("Frankfurt");
        Clock.Advance(TimeSpan.FromMilliseconds(250));
        await service.ResolveAsync("Hamburg");

        Assert.Equal([TimeSpan.FromMilliseconds(750)], Delay.Delays);
        Assert.Equal(2, Handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass nach Ablauf der Sekunde nicht gewartet wird.
    /// </summary>
    [Fact]
    public async Task Resolve_RequestsOneSecondApart_DoNotWait()
    {
        Handler.RespondWith(HttpStatusCode.OK, FoundBody);
        var service = CreateService();

        await service.ResolveAsync("Frankfurt");
        Clock.Advance(TimeSpan.FromSeconds(1));
        await service.ResolveAsync("Hamburg");

        Assert.Empty(Delay.Delays);
    }

    /// <summary>
    /// Prüft, dass ungültige Eingaben den Mindestabstand nicht verbrauchen (es ging keine Anfrage hinaus).
    /// </summary>
    [Fact]
    public async Task Resolve_InvalidInput_DoesNotConsumeInterval()
    {
        Handler.RespondWith(HttpStatusCode.OK, FoundBody);
        var service = CreateService();

        await service.ResolveAsync("Frankfurt");
        await service.ResolveAsync("a");
        Clock.Advance(TimeSpan.FromSeconds(1));
        await service.ResolveAsync("Hamburg");

        Assert.Empty(Delay.Delays);
        Assert.Equal(2, Handler.Requests.Count);
    }

    /// <summary>
    /// Prüft, dass mehrere gleichzeitig abgesendete Anfragen nacheinander mit Mindestabstand abgewickelt werden.
    /// </summary>
    [Fact]
    public async Task Resolve_ConcurrentRequests_AreSerialized()
    {
        Handler.RespondWith(HttpStatusCode.OK, FoundBody);
        var service = CreateService();

        await Task.WhenAll(service.ResolveAsync("Frankfurt"), service.ResolveAsync("Hamburg"), service.ResolveAsync("Dresden"));

        Assert.Equal(3, Handler.Requests.Count);
        Assert.Equal(2, Delay.Delays.Count);
        Assert.All(Delay.Delays, delay => Assert.Equal(TimeSpan.FromSeconds(1), delay));
    }
}
