using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.MAUI.Services.Routing;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests des Routing-Clients: Fake-HTTP-Handler, aufzeichnende Wartezeiten und Testuhr; kein Test berührt das Netzwerk.
/// </summary>
public abstract class OpenRouteServiceClientTestBase : BaseTest
{
    /// <summary>
    /// Der Startpunkt der Tests (Berlin-Mitte).
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected static readonly GeoPosition Start = new(52.52, 13.405);

    /// <summary>
    /// Der Zielpunkt der Tests (Oranienburg).
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected static readonly GeoPosition Destination = new(52.88, 13.405);

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
    /// Erzeugt einen gültigen Antwortkörper mit einer kodierten Route.
    /// </summary>
    /// <param name="points">Die Punkte der Route; ohne Angabe eine gerade Nordroute.</param>
    /// <returns>Der Antwortkörper.</returns>
    protected static string RouteBody(params (double, double)[] points)
    {
        var line = points.Length == 0 ? [(52.52, 13.405), (52.70, 13.41), (52.88, 13.405)] : points;
        return "{\"routes\":[{\"summary\":{\"distance\":40000.0,\"duration\":2000.0},\"geometry\":\"" + PolylineEncoder.Encode(line).Replace("\\", "\\\\", StringComparison.Ordinal) + "\"}]}";
    }

    /// <summary>
    /// Erstellt den Client mit den angegebenen Einstellungen.
    /// </summary>
    /// <param name="options">Die Einstellungen oder <see langword="null"/> für Testwerte (https, drei Versuche, 1 s Basiswartezeit, keine Drosselung).</param>
    /// <returns>Der Client.</returns>
    protected OpenRouteServiceClient CreateClient(RoutingOptions? options = null)
    {
        options ??= new RoutingOptions { BaseUrl = new Uri("https://routing.example.test/"), AllowLoopbackHttp = true, MinRequestInterval = TimeSpan.Zero };
        return new OpenRouteServiceClient(
            new HttpClient(Handler),
            Keys,
            options,
            Delay,
            new RequestThrottle(options.MinRequestInterval, Delay, Clock),
            NullLogger<OpenRouteServiceClient>.Instance);
    }
}
