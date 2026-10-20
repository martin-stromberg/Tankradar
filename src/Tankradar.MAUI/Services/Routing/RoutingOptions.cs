using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Einstellungen für den Zugriff auf den Routingdienst OpenRouteService: Adresse, Zeitlimit, Wiederholung und Drosselung.
/// Produktiv gilt ausschließlich HTTPS; ein abweichender Endpunkt ist nur im Testmodus (gesetztes <c>TANKATLAS_TEST_DATA_PATH</c>) möglich.
/// </summary>
public sealed class RoutingOptions
{
    /// <summary>
    /// Produktive Basisadresse von OpenRouteService.
    /// </summary>
    public const string DefaultBaseUrl = "https://api.openrouteservice.org/";

    /// <summary>
    /// Name der Umgebungsvariable, die im Testmodus den Endpunkt überschreibt (z. B. auf einen Mock-Server).
    /// </summary>
    public const string BaseUrlEnvironmentVariable = TestDataPaths.RoutingUrlEnvironmentVariable;

    /// <summary>
    /// Name der Umgebungsvariable, die im Testmodus einen API-Schlüssel vorgibt (wird nie gespeichert).
    /// </summary>
    public const string ApiKeyEnvironmentVariable = TestDataPaths.RoutingTestApiKeyEnvironmentVariable;

    private static readonly Uri DefaultUri = new Uri(DefaultBaseUrl);
    private static readonly string UnknownVersionUserAgent = AppIdentity.BuildUserAgent(null);

    /// <summary>
    /// Die Basisadresse des Dienstes mit abschließendem Schrägstrich.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public Uri BaseUrl { get; init; } = DefaultUri;

    /// <summary>
    /// Gibt an, dass der Abruf verweigert wird, weil im Testmodus kein Endpunkt angegeben ist (kein Rückfall auf den produktiven Dienst).
    /// </summary>
    public bool EndpointNotConfigured { get; init; }

    /// <summary>
    /// Zeitlimit je einzelner Anfrage.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Höchstzahl der Versuche je Abruf (erster Versuch eingeschlossen).
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// Wartezeit vor dem ersten erneuten Versuch; sie verdoppelt sich mit jedem weiteren Versuch.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan RetryBaseDelay { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Mindestabstand zwischen zwei Anfragen an den Routingdienst (Schutz des Kontingents).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan MinRequestInterval { get; init; } = TimeSpan.FromMilliseconds(1500);

    /// <summary>
    /// Die Kennung der App im Anfragekopf (<c>User-Agent</c>).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public string UserAgent { get; init; } = UnknownVersionUserAgent;

    /// <summary>
    /// Gibt an, ob unverschlüsseltes HTTP zu einer Loopback-Adresse zugelassen ist (nur Testmodus).
    /// </summary>
    public bool AllowLoopbackHttp { get; init; }

    /// <summary>
    /// Prüft die Einstellungen; HTTPS ist Pflicht, außer im Testmodus für Loopback-Adressen.
    /// </summary>
    /// <exception cref="InvalidOperationException">Die Einstellungen sind ungültig oder unsicher.</exception>
    public void Validate()
    {
        if (!BaseUrl.IsAbsoluteUri)
        {
            throw new InvalidOperationException("Die Basisadresse des Routingdienstes muss absolut sein.");
        }

        var secure = BaseUrl.Scheme == Uri.UriSchemeHttps;
        var allowedLoopback = AllowLoopbackHttp && BaseUrl.Scheme == Uri.UriSchemeHttp && BaseUrl.IsLoopback;
        if (!secure && !allowedLoopback)
        {
            throw new InvalidOperationException("Der Routingdienst darf ausschließlich über HTTPS angesprochen werden.");
        }

        if (RequestTimeout <= TimeSpan.Zero || MaxAttempts < 1 || RetryBaseDelay < TimeSpan.Zero || string.IsNullOrWhiteSpace(UserAgent))
        {
            throw new InvalidOperationException("Zeitlimit, Wiederholung und Kennung des Routingdienstes sind ungültig.");
        }

        if (MinRequestInterval < (AllowLoopbackHttp ? TimeSpan.Zero : TimeSpan.FromSeconds(1)))
        {
            throw new InvalidOperationException("Der Mindestabstand der Anfragen an den Routingdienst muss mindestens 1 s betragen.");
        }
    }

    /// <summary>
    /// Ermittelt die Einstellungen aus der Umgebung. Der Endpunkt lässt sich nur überschreiben, wenn das Testverzeichnis
    /// (<c>TANKATLAS_TEST_DATA_PATH</c>) gesetzt ist; dann gelten außerdem kurze Wartezeiten. Ohne Endpunkt im Testmodus wird der Abruf verweigert.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert den Wert einer Umgebungsvariable oder <see langword="null"/>.</param>
    /// <param name="appVersion">Die tatsächliche App-Version für die Kennung im Anfragekopf; <see langword="null"/> führt zu <see cref="AppIdentity.UnknownVersion"/>.</param>
    /// <returns>Die validierten Einstellungen.</returns>
    public static RoutingOptions FromEnvironment(Func<string, string?> getEnvironmentVariable, string? appVersion = null)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var testMode = !string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable));
        var overrideUrl = getEnvironmentVariable(BaseUrlEnvironmentVariable);
        RoutingOptions options;
        if (testMode && !string.IsNullOrWhiteSpace(overrideUrl) && Uri.TryCreate(EnsureTrailingSlash(overrideUrl), UriKind.Absolute, out var uri))
        {
            options = new RoutingOptions
            {
                BaseUrl = uri,
                AllowLoopbackHttp = true,
                RequestTimeout = TimeSpan.FromSeconds(3),
                RetryBaseDelay = TimeSpan.FromMilliseconds(50),
                MinRequestInterval = TimeSpan.Zero,
                UserAgent = AppIdentity.BuildUserAgent(appVersion),
            };
        }
        else
        {
            options = new RoutingOptions { EndpointNotConfigured = testMode, UserAgent = AppIdentity.BuildUserAgent(appVersion) };
        }

        options.Validate();
        return options;
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith('/') ? value : value + "/";
    }
}
