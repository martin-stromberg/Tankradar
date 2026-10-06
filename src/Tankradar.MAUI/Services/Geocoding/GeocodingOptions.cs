using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Geocoding;

/// <summary>
/// Einstellungen für die Adressauflösung über OpenStreetMap-Nominatim. Produktiv gilt ausschließlich HTTPS; ein abweichender
/// Endpunkt ist nur im Testmodus (gesetztes <c>TANKATLAS_TEST_DATA_PATH</c>) möglich. Die Nutzungsrichtlinie fordert höchstens eine Anfrage je Sekunde und eine identifizierende Kennung.
/// </summary>
public sealed class GeocodingOptions
{
    /// <summary>
    /// Produktive Basisadresse von Nominatim.
    /// </summary>
    public const string DefaultBaseUrl = "https://nominatim.openstreetmap.org/";

    /// <summary>
    /// Die Kennung, mit der sich die App gegenüber Nominatim ausweist (Produktname, Version, App-Kennung).
    /// </summary>
    public const string DefaultUserAgent = ProductToken + " " + AppConfiguration.DefaultBundleId;

    private const string ProductToken = "Tankatlas/0.1";

    private static readonly Uri DefaultUri = new Uri(DefaultBaseUrl);

    /// <summary>
    /// Die Basisadresse des Dienstes.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public Uri BaseUrl { get; init; } = DefaultUri;

    /// <summary>
    /// Gibt an, dass die Auflösung verweigert wird, weil im Testmodus keine Adresse angegeben ist (kein Rückfall auf den produktiven Dienst).
    /// </summary>
    public bool EndpointNotConfigured { get; init; }

    /// <summary>
    /// Zeitlimit je Anfrage.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Mindestabstand zwischen zwei Anfragen. Die Nutzungsrichtlinie erlaubt höchstens eine Anfrage je Sekunde, gemessen beim Dienst; der Standard
    /// enthält einen Sicherheitsabstand von 100 ms, damit Netzwerk- und Scheduling-Schwankungen den Abstand beim Empfänger nicht unter eine Sekunde drücken.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan MinRequestInterval { get; init; } = DefaultMinRequestInterval;

    /// <summary>
    /// Der Standard-Mindestabstand zwischen zwei Anfragen (1,1 Sekunden).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public static readonly TimeSpan DefaultMinRequestInterval = TimeSpan.FromMilliseconds(1100);

    /// <summary>
    /// Die Kennung der App im Anfragekopf (<c>User-Agent</c>).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public string UserAgent { get; init; } = DefaultUserAgent;

    /// <summary>
    /// Gibt an, ob unverschlüsseltes HTTP zu einer Loopback-Adresse zugelassen ist (nur Testmodus).
    /// </summary>
    public bool AllowLoopbackHttp { get; init; }

    /// <summary>
    /// Prüft die Einstellungen; HTTPS ist Pflicht, außer im Testmodus für Loopback-Adressen. Der Mindestabstand darf die Richtlinie (1 s) nicht unterschreiten.
    /// </summary>
    /// <exception cref="InvalidOperationException">Die Einstellungen sind ungültig oder unsicher.</exception>
    public void Validate()
    {
        if (!BaseUrl.IsAbsoluteUri)
        {
            throw new InvalidOperationException("Die Basisadresse des Ortssuchdienstes muss absolut sein.");
        }

        var secure = BaseUrl.Scheme == Uri.UriSchemeHttps;
        var allowedLoopback = AllowLoopbackHttp && BaseUrl.Scheme == Uri.UriSchemeHttp && BaseUrl.IsLoopback;
        if (!secure && !allowedLoopback)
        {
            throw new InvalidOperationException("Der Ortssuchdienst darf ausschließlich über HTTPS angesprochen werden.");
        }

        if (RequestTimeout <= TimeSpan.Zero || MinRequestInterval < TimeSpan.FromSeconds(1) || string.IsNullOrWhiteSpace(UserAgent))
        {
            throw new InvalidOperationException("Zeitlimit, Mindestabstand (mindestens 1 s) und Kennung des Ortssuchdienstes sind ungültig.");
        }
    }

    /// <summary>
    /// Ermittelt die Einstellungen aus der Umgebung. Der Endpunkt lässt sich nur überschreiben, wenn das Testverzeichnis
    /// (<c>TANKATLAS_TEST_DATA_PATH</c>) gesetzt ist; ohne Endpunkt im Testmodus wird die Auflösung verweigert.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert den Wert einer Umgebungsvariable oder <see langword="null"/>.</param>
    /// <returns>Die validierten Einstellungen.</returns>
    public static GeocodingOptions FromEnvironment(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var testMode = !string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable));
        var overrideUrl = getEnvironmentVariable(TestDataPaths.GeocodingUrlEnvironmentVariable);
        GeocodingOptions options;
        if (testMode && !string.IsNullOrWhiteSpace(overrideUrl) && Uri.TryCreate(EnsureTrailingSlash(overrideUrl), UriKind.Absolute, out var uri))
        {
            options = new GeocodingOptions { BaseUrl = uri, AllowLoopbackHttp = true, RequestTimeout = TimeSpan.FromSeconds(3) };
        }
        else
        {
            options = new GeocodingOptions { EndpointNotConfigured = testMode };
        }

        options.Validate();
        return options;
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith('/') ? value : value + "/";
    }
}
