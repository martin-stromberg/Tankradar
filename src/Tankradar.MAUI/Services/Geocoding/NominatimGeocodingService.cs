using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.MAUI.Services.Geocoding;

/// <summary>
/// Wandelt eine Adresse, einen Ort oder eine Postleitzahl in eine Position um.
/// </summary>
public interface IGeocodingService
{
    /// <summary>
    /// Löst die Eingabe auf. Der Aufruf sendet höchstens eine Anfrage und ist nur auf ausdrückliche Veranlassung des Anwenders zu verwenden (keine Autovervollständigung).
    /// </summary>
    /// <param name="input">Die Eingabe des Anwenders; sie wird vor dem Aufruf des Dienstes geprüft.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis; Fehler werden als Status gemeldet, nicht als Ausnahme.</returns>
    Task<GeocodingResult> ResolveAsync(string? input, CancellationToken cancellationToken = default);
}

/// <summary>
/// Adressauflösung über OpenStreetMap-Nominatim gemäß Nutzungsrichtlinie: eine Anfrage je Aufruf ohne Wiederholung, Drosselung auf höchstens
/// eine Anfrage je Sekunde, identifizierender <c>User-Agent</c>, keine Weiterleitungen. Eingabe und Ergebnis werden nie protokolliert.
/// </summary>
public sealed class NominatimGeocodingService : IGeocodingService
{
    private const int MaxPlaceNameLength = 200;

    private readonly HttpClient _httpClient;
    private readonly GeocodingOptions _options;
    private readonly RequestThrottle _throttle;
    private readonly ILogger<NominatimGeocodingService> _logger;

    /// <summary>
    /// Erstellt den Dienst.
    /// </summary>
    /// <param name="httpClient">Der HTTP-Client (ohne automatische Weiterleitungen).</param>
    /// <param name="options">Die Einstellungen (werden geprüft).</param>
    /// <param name="throttle">Die Drosselung dieses Dienstes (nicht die des Preisdienstes).</param>
    /// <param name="logger">Logger (protokolliert nie Eingaben oder Positionen).</param>
    public NominatimGeocodingService(HttpClient httpClient, GeocodingOptions options, RequestThrottle throttle, ILogger<NominatimGeocodingService> logger)
    {
        options.Validate();
        _httpClient = httpClient;
        _options = options;
        _throttle = throttle;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<GeocodingResult> ResolveAsync(string? input, CancellationToken cancellationToken = default)
    {
        if (AddressInput.Validate(input, out var query) != AddressInputError.None)
        {
            return GeocodingResult.Failure(GeocodingStatus.InvalidInput);
        }

        if (_options.EndpointNotConfigured)
        {
            return GeocodingResult.Failure(GeocodingStatus.EndpointNotConfigured);
        }

        // Nur auf Deutschland beschränkt: Die Preisquelle (Tankerkönig) liefert ausschließlich deutsche Tankstellen.
        var uri = new Uri(_options.BaseUrl, "search?format=jsonv2&limit=1&countrycodes=de&accept-language=de&q=" + Uri.EscapeDataString(query));

        await _throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_options.RequestTimeout);
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.UserAgent.ParseAdd(_options.UserAgent);
            using var response = await _httpClient.SendAsync(request, timeout.Token).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                _logger.LogWarning("Die Ortssuche wurde mit Status {Status} beantwortet.", status);
                return GeocodingResult.Failure(status == 429 || status >= 500 ? GeocodingStatus.Unavailable : GeocodingStatus.Rejected);
            }

            var body = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
            return Parse(body);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Die Ortssuche hat nicht rechtzeitig geantwortet.");
            return GeocodingResult.Failure(GeocodingStatus.Unavailable);
        }
        catch (HttpRequestException ex)
        {
            // Nur der Typ wird protokolliert: Meldungen des Netzwerkstacks könnten die Anfrageadresse samt Suchbegriff enthalten.
            _logger.LogWarning("Die Ortssuche ist fehlgeschlagen ({ExceptionType}, {Error}).", ex.GetType().Name, ex.HttpRequestError);
            return GeocodingResult.Failure(GeocodingStatus.Unavailable);
        }
    }

    private static GeocodingResult Parse(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return GeocodingResult.Failure(GeocodingStatus.InvalidResponse);
            }

            if (document.RootElement.GetArrayLength() == 0)
            {
                return GeocodingResult.Failure(GeocodingStatus.NotFound);
            }

            var first = document.RootElement[0];
            if (first.ValueKind != JsonValueKind.Object
                || !TryGetCoordinate(first, "lat", out var latitude)
                || !TryGetCoordinate(first, "lon", out var longitude)
                || !GeoPosition.TryCreate(latitude, longitude, out var position)
                || position is null)
            {
                return GeocodingResult.Failure(GeocodingStatus.InvalidResponse);
            }

            return GeocodingResult.Success(position, GetPlaceName(first));
        }
        catch (JsonException)
        {
            return GeocodingResult.Failure(GeocodingStatus.InvalidResponse);
        }
    }

    private static string? GetPlaceName(JsonElement element)
    {
        if (!element.TryGetProperty("display_name", out var value) || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var text = new string(value.GetString()!.Where(character => !char.IsControl(character)).ToArray()).Trim();
        if (text.Length == 0)
        {
            return null;
        }

        return text.Length <= MaxPlaceNameLength ? text : text[..MaxPlaceNameLength];
    }

    private static bool TryGetCoordinate(JsonElement element, string property, out double value)
    {
        value = 0;
        if (!element.TryGetProperty(property, out var node))
        {
            return false;
        }

        return node.ValueKind switch
        {
            JsonValueKind.String => double.TryParse(node.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value) && double.IsFinite(value),
            JsonValueKind.Number => node.TryGetDouble(out value) && double.IsFinite(value),
            _ => false,
        };
    }
}
