using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Client-Tests: Fake-HTTP-Handler, aufzeichnende Wartezeiten und Testuhr; kein Test berührt das Netzwerk.
/// </summary>
public abstract class TankerkoenigClientTestBase : BaseTest
{
    /// <summary>
    /// Ein Antwortkörper mit zwei Tankstellen (eine ohne Diesel-Preis) und einer unbrauchbaren Station.
    /// </summary>
    protected const string ListBody = """
        {"ok":true,"status":"ok","stations":[
          {"id":"11111111-1111-4111-8111-111111111111","name":"Alpha","brand":"A","street":"Weg","houseNumber":"1","postCode":"10115","place":"Berlin","lat":52.5201,"lng":13.4051,"dist":0.1,"isOpen":true,"e5":1.859,"e10":1.799,"diesel":1.699},
          {"id":"22222222-2222-4222-8222-222222222222","name":"Beta","lat":52.53,"lng":13.41,"dist":1.4,"isOpen":false,"e5":1.879,"e10":null,"diesel":false},
          {"id":"33333333-3333-4333-8333-333333333333","name":"Ohne Position"}
        ]}
        """;

    /// <summary>
    /// Der Fake-HTTP-Handler.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeHttpMessageHandler Handler { get; } = new();

    /// <summary>
    /// Die aufgezeichneten Wartezeiten.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RecordingDelay Delay { get; } = new();

    /// <summary>
    /// Die Testuhr.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected ManualTimeProvider Clock { get; } = new();

    /// <summary>
    /// Der Schlüsselgeber.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected FakeApiKeyProvider Keys { get; } = new();

    /// <summary>
    /// Erstellt den Client mit den angegebenen Einstellungen.
    /// </summary>
    /// <param name="options">Die Einstellungen oder <see langword="null"/> für Testwerte (https, drei Versuche, 1 s Basiswartezeit).</param>
    /// <returns>Der Client.</returns>
    protected TankerkoenigClient CreateClient(PriceApiOptions? options = null)
    {
        options ??= new PriceApiOptions { BaseUrl = new Uri("https://prices.example.test/json/"), MinRequestInterval = TimeSpan.Zero };
        return new TankerkoenigClient(
            new HttpClient(Handler),
            Keys,
            options,
            Delay,
            new RequestThrottle(options.MinRequestInterval, Delay, Clock),
            Clock,
            NullLogger<TankerkoenigClient>.Instance);
    }
}
