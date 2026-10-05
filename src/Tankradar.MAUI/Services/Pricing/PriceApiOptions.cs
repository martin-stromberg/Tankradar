using Tankradar.TestSupport;

namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Einstellungen für den Zugriff auf die Preis-API: Adresse, Zeitlimit, Wiederholung, Drosselung und Cache-Dauer.
/// Produktiv gilt ausschließlich HTTPS; ein abweichender Endpunkt ist nur im Testmodus (gesetztes <c>TEST_DATA_PATH</c>) möglich.
/// </summary>
public sealed class PriceApiOptions
{
    /// <summary>
    /// Produktive Basisadresse der Tankerkönig-API (Creative-Commons-Endpunkt).
    /// </summary>
    public const string DefaultBaseUrl = "https://creativecommons.tankerkoenig.de/json/";

    private static readonly Uri DefaultUri = new Uri(DefaultBaseUrl);

    /// <summary>
    /// Name der Umgebungsvariable, die im Testmodus den Endpunkt überschreibt (z. B. auf einen Mock-Server).
    /// </summary>
    public const string BaseUrlEnvironmentVariable = TestDataPaths.PriceApiUrlEnvironmentVariable;

    /// <summary>
    /// Name der Umgebungsvariable, die im Testmodus einen API-Schlüssel vorgibt (wird nie gespeichert).
    /// </summary>
    public const string ApiKeyEnvironmentVariable = TestDataPaths.PriceApiKeyEnvironmentVariable;

    /// <summary>
    /// Die Basisadresse der API mit abschließendem Schrägstrich.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public Uri BaseUrl { get; init; } = DefaultUri;

    /// <summary>
    /// Gibt an, dass der Abruf verweigert wird, weil im Testmodus kein Endpunkt angegeben ist (Fail Secure: kein Rückfall auf den produktiven Dienst).
    /// </summary>
    public bool EndpointNotConfigured { get; init; }

    /// <summary>
    /// Zeitlimit je einzelner Anfrage.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(10);

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
    /// Mindestabstand zwischen zwei Anfragen an den Preisdienst (Drosselung gemäß Nutzungsbedingungen).
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan MinRequestInterval { get; init; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// So lange gelten lokal gespeicherte Daten als ausreichend jung, um einen erneuten Abruf zu ersparen.
    /// </summary>
    /// <returns>Der Wert.</returns>
    public TimeSpan CacheLifetime { get; init; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Der größte von der Quelle zugelassene Suchradius in Kilometern.
    /// </summary>
    public int MaxRadiusKm { get; init; } = 25;

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
            throw new InvalidOperationException("Die Basisadresse der Preis-API muss absolut sein.");
        }

        var secure = BaseUrl.Scheme == Uri.UriSchemeHttps;
        var allowedLoopback = AllowLoopbackHttp && BaseUrl.Scheme == Uri.UriSchemeHttp && BaseUrl.IsLoopback;
        if (!secure && !allowedLoopback)
        {
            throw new InvalidOperationException("Die Preis-API darf ausschließlich über HTTPS angesprochen werden.");
        }

        if (RequestTimeout <= TimeSpan.Zero || MaxAttempts < 1 || RetryBaseDelay < TimeSpan.Zero
            || MinRequestInterval < TimeSpan.Zero || CacheLifetime < TimeSpan.Zero || MaxRadiusKm < 1)
        {
            throw new InvalidOperationException("Die Zeit- und Wiederholungseinstellungen der Preis-API sind ungültig.");
        }
    }

    /// <summary>
    /// Ermittelt die Einstellungen aus der Umgebung. Der Endpunkt lässt sich nur überschreiben, wenn das Testverzeichnis
    /// (<c>TEST_DATA_PATH</c>) gesetzt ist; dann gelten außerdem kurze Wartezeiten. Ohne Endpunkt im Testmodus wird der Abruf verweigert.
    /// </summary>
    /// <param name="getEnvironmentVariable">Liefert den Wert einer Umgebungsvariable oder <see langword="null"/>.</param>
    /// <returns>Die validierten Einstellungen.</returns>
    public static PriceApiOptions FromEnvironment(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        var testMode = !string.IsNullOrWhiteSpace(getEnvironmentVariable(TestDataPaths.TestDataPathEnvironmentVariable));
        var overrideUrl = getEnvironmentVariable(BaseUrlEnvironmentVariable);
        PriceApiOptions options;
        if (testMode && !string.IsNullOrWhiteSpace(overrideUrl) && Uri.TryCreate(EnsureTrailingSlash(overrideUrl), UriKind.Absolute, out var uri))
        {
            options = new PriceApiOptions
            {
                BaseUrl = uri,
                AllowLoopbackHttp = true,
                RequestTimeout = TimeSpan.FromSeconds(3),
                RetryBaseDelay = TimeSpan.FromMilliseconds(50),
                MinRequestInterval = TimeSpan.Zero,
            };
        }
        else
        {
            options = new PriceApiOptions { EndpointNotConfigured = testMode };
        }

        options.Validate();
        return options;
    }

    private static string EnsureTrailingSlash(string value)
    {
        return value.EndsWith('/') ? value : value + "/";
    }
}
