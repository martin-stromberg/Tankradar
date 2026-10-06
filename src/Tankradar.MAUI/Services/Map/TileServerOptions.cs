using Tankradar.MAUI.Models.Map;
using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Map;

/// <summary>
/// Einstellungen für den Abruf von Kartenkacheln. Produktiv gilt ausschließlich HTTPS und der Standard-Kachelserver von OpenStreetMap;
/// ein abweichender Endpunkt ist nur im Testmodus (gesetztes <c>TANKATLAS_TEST_DATA_PATH</c>) möglich. Die Nutzungsrichtlinie der Kachelserver
/// (https://operations.osmfoundation.org/policies/tiles/) fordert eine identifizierende Kennung, Zwischenspeicherung, höchstens zwei parallele Abrufe
/// und den Verzicht auf Massenabrufe.
/// </summary>
public sealed class TileServerOptions
{
    /// <summary>
    /// Die produktive Adressvorlage der Kachelserver von OpenStreetMap (<c>{z}</c> Zoomstufe, <c>{x}</c> Spalte, <c>{y}</c> Zeile).
    /// </summary>
    public const string DefaultUrlTemplate = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";

    /// <summary>
    /// Die Kennung, mit der sich die App gegenüber dem Kachelserver ausweist (Produktname, Version, App-Kennung).
    /// </summary>
    public const string DefaultUserAgent = "Tankatlas/0.1 " + AppConfiguration.DefaultBundleId;

    /// <summary>
    /// Die Adressvorlage der Kacheln.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public string UrlTemplate { get; init; } = DefaultUrlTemplate;

    /// <summary>
    /// Gibt an, dass der Abruf verweigert wird, weil im Testmodus keine Adresse angegeben ist (kein Rückfall auf den produktiven Kachelserver).
    /// </summary>
    public bool EndpointNotConfigured { get; init; }

    /// <summary>
    /// Die Kennung der App im Anfragekopf (<c>User-Agent</c>).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public string UserAgent { get; init; } = DefaultUserAgent;

    /// <summary>
    /// Zeitlimit je Anfrage.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Die Höchstzahl paralleler Abrufe (die Nutzungsrichtlinie erlaubt höchstens zwei).
    /// </summary>
    public int MaxConcurrentRequests { get; init; } = 2;

    /// <summary>
    /// So lange gilt eine zwischengespeicherte Kachel als aktuell (die Nutzungsrichtlinie fordert mindestens sieben Tage); danach wird neu abgerufen,
    /// bei einem Fehler aber die veraltete Kachel weiterverwendet.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan CacheLifetime { get; init; } = TimeSpan.FromDays(7);

    /// <summary>
    /// So lange wird eine Kachel nach einem Fehler nicht erneut abgerufen (verhindert wiederholte Anfragen ohne Verbindung).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan FailureBackoff { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Die größte zugelassene Antwortgröße einer Kachel in Bytes.
    /// </summary>
    public int MaxTileBytes { get; init; } = 512 * 1024;

    /// <summary>
    /// Die Obergrenze des Kachelspeichers auf dem Gerät in Bytes; darüber werden die ältesten Kacheln gelöscht.
    /// </summary>
    public long MaxCacheBytes { get; init; } = 100L * 1024 * 1024;

    /// <summary>
    /// Gibt an, ob unverschlüsseltes HTTP zu einer Loopback-Adresse zugelassen ist (nur Testmodus).
    /// </summary>
    public bool AllowLoopbackHttp { get; init; }

    /// <summary>
    /// Erzeugt die Adresse einer Kachel.
    /// </summary>
    /// <param name="key">Die Kachel.</param>
    /// <returns>Die Adresse.</returns>
    public Uri CreateUri(TileKey key)
    {
        var url = UrlTemplate
            .Replace("{z}", key.Zoom.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{x}", key.X.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{y}", key.Y.ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal);
        return new Uri(url, UriKind.Absolute);
    }

    /// <summary>
    /// Prüft die Einstellungen; HTTPS ist Pflicht, außer im Testmodus für Loopback-Adressen.
    /// </summary>
    /// <exception cref="InvalidOperationException">Die Einstellungen sind ungültig oder unsicher.</exception>
    public void Validate()
    {
        foreach (var placeholder in new[] { "{z}", "{x}", "{y}" })
        {
            if (!UrlTemplate.Contains(placeholder, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Die Adressvorlage der Kartenkacheln muss {z}, {x} und {y} enthalten.");
            }
        }

        if (!Uri.TryCreate(UrlTemplate.Replace("{z}", "1", StringComparison.Ordinal).Replace("{x}", "1", StringComparison.Ordinal).Replace("{y}", "1", StringComparison.Ordinal), UriKind.Absolute, out var sample))
        {
            throw new InvalidOperationException("Die Adressvorlage der Kartenkacheln muss absolut sein.");
        }

        var secure = sample.Scheme == Uri.UriSchemeHttps;
        var allowedLoopback = AllowLoopbackHttp && sample.Scheme == Uri.UriSchemeHttp && sample.IsLoopback;
        if (!secure && !allowedLoopback)
        {
            throw new InvalidOperationException("Der Kachelserver darf ausschließlich über HTTPS angesprochen werden.");
        }

        if (RequestTimeout <= TimeSpan.Zero || MaxConcurrentRequests is < 1 or > 2 || CacheLifetime < TimeSpan.FromDays(7)
            || FailureBackoff < TimeSpan.Zero || MaxTileBytes < 1 || MaxCacheBytes < 1 || string.IsNullOrWhiteSpace(UserAgent))
        {
            throw new InvalidOperationException("Zeitlimit, Parallelität (höchstens 2), Zwischenspeicherdauer (mindestens 7 Tage) und Kennung des Kachelservers sind ungültig.");
        }
    }

    /// <summary>
    /// Ermittelt die Einstellungen aus der Umgebung. Der Endpunkt lässt sich nur überschreiben, wenn das Testverzeichnis
    /// (<c>TANKATLAS_TEST_DATA_PATH</c>) gesetzt ist; ohne Endpunkt im Testmodus werden keine Kacheln abgerufen.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert den Wert einer Umgebungsvariable oder <see langword="null"/>.</param>
    /// <returns>Die validierten Einstellungen.</returns>
    public static TileServerOptions FromEnvironment(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var testMode = !string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable));
        var overrideUrl = getEnvironmentVariable(TestDataPaths.TileUrlEnvironmentVariable);
        TileServerOptions options;
        if (testMode && !string.IsNullOrWhiteSpace(overrideUrl) && Uri.TryCreate(overrideUrl, UriKind.Absolute, out var uri))
        {
            var baseUrl = uri.ToString();
            options = new TileServerOptions
            {
                UrlTemplate = (baseUrl.EndsWith('/') ? baseUrl : baseUrl + "/") + "{z}/{x}/{y}.png",
                AllowLoopbackHttp = true,
                RequestTimeout = TimeSpan.FromSeconds(3),
            };
        }
        else
        {
            options = new TileServerOptions { EndpointNotConfigured = testMode };
        }

        options.Validate();
        return options;
    }
}
