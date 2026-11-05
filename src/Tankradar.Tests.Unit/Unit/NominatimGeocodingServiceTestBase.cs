using Microsoft.Extensions.Logging.Abstractions;
using Tankradar.MAUI.Services.Geocoding;
using Tankradar.MAUI.Services.Pricing;
using Tankradar.Tests.Unit.Unit.Support;
using Tankradar.TestSupport;

namespace Tankradar.Tests.Unit.Unit;

/// <summary>
/// Gemeinsamer Aufbau der Tests des Ortssuchdienstes: Fake-HTTP-Handler, aufzeichnende Wartezeiten und Testuhr; kein Test berührt das Netzwerk.
/// </summary>
public abstract class NominatimGeocodingServiceTestBase : BaseTest
{
    /// <summary>
    /// Eine Antwort mit einem Treffer (Koordinaten wie bei Nominatim als Zeichenfolgen).
    /// </summary>
    protected const string FoundBody = """[{"place_id":1,"lat":"50.1109221","lon":"8.6821267","display_name":"Frankfurt am Main, Hessen, Deutschland","type":"city"}]""";

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
    /// Der Log-Sammler.
    /// </summary>
    /// <returns>Der Wert.</returns>
    protected RecordingLogger<NominatimGeocodingService> Logger { get; } = new();

    /// <summary>
    /// Erstellt den Dienst mit den angegebenen Einstellungen.
    /// </summary>
    /// <param name="options">Die Einstellungen oder <see langword="null"/> für die Standardwerte mit Testadresse (https, 1 s Mindestabstand).</param>
    /// <returns>Der Dienst.</returns>
    protected NominatimGeocodingService CreateService(GeocodingOptions? options = null)
    {
        options ??= new GeocodingOptions { BaseUrl = new Uri("https://geo.example.test/") };
        return new NominatimGeocodingService(
            new HttpClient(Handler),
            options,
            new RequestThrottle(options.MinRequestInterval, Delay, Clock),
            Logger);
    }

    /// <summary>
    /// Erstellt den Dienst ohne Protokollsammlung (für Tests, die nur das Ergebnis prüfen).
    /// </summary>
    /// <returns>Der Dienst.</returns>
    protected NominatimGeocodingService CreateQuietService()
    {
        var options = new GeocodingOptions { BaseUrl = new Uri("https://geo.example.test/") };
        return new NominatimGeocodingService(new HttpClient(Handler), options, new RequestThrottle(options.MinRequestInterval, Delay, Clock), NullLogger<NominatimGeocodingService>.Instance);
    }
}
