using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Models.Search;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Zugriff auf einen Routingdienst für Fahrten mit dem Auto.
/// </summary>
public interface IRoutingService
{
    /// <summary>
    /// Prüft ohne Netzzugriff, ob die Routenberechnung möglich ist (Endpunkt konfiguriert und Schlüssel vorhanden).
    /// </summary>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns><see langword="true"/>, wenn die Routenberechnung versucht werden kann.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Berechnet die Route zwischen zwei Positionen. Fehler werden als Ergebnis gemeldet, nicht als Ausnahme.
    /// </summary>
    /// <param name="start">Die Startposition.</param>
    /// <param name="destination">Die Zielposition.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis mit der Route oder dem Fehlergrund.</returns>
    Task<RoutingResult> GetRouteAsync(GeoPosition start, GeoPosition destination, CancellationToken cancellationToken = default);
}

/// <summary>
/// Client für OpenRouteService (Directions, Profil Auto) mit Zeitlimit je Anfrage, Wiederholung mit verdoppelter Wartezeit und Drosselung.
/// Der Schlüssel steht nur im Anfragekopf; Schlüssel, Koordinaten und Antworttexte werden nie protokolliert oder in Fehlermeldungen übernommen.
/// </summary>
public sealed class OpenRouteServiceClient : IRoutingService
{
    /// <summary>
    /// Höchstzahl der Punkte einer dekodierten Route.
    /// </summary>
    public const int MaxRoutePoints = 50_000;

    private const string DirectionsPath = "v2/directions/driving-car";
    private const int PolylinePrecision = 5;
    private static readonly TimeSpan MaxRetryAfter = TimeSpan.FromSeconds(10);

    private readonly HttpClient _httpClient;
    private readonly IApiKeyProvider _apiKeyProvider;
    private readonly RoutingOptions _options;
    private readonly IDelay _delay;
    private readonly RequestThrottle _throttle;
    private readonly ILogger<OpenRouteServiceClient> _logger;

    /// <summary>
    /// Erstellt den Client.
    /// </summary>
    /// <param name="httpClient">Der HTTP-Client (ohne automatische Weiterleitungen).</param>
    /// <param name="apiKeyProvider">Liefert den Schlüssel des Routingdienstes.</param>
    /// <param name="options">Die Einstellungen (werden geprüft).</param>
    /// <param name="delay">Das Warteverfahren für Wiederholungen.</param>
    /// <param name="throttle">Die Drosselung dieses Dienstes.</param>
    /// <param name="logger">Logger (protokolliert nie Schlüssel oder Positionen).</param>
    public OpenRouteServiceClient(
        HttpClient httpClient,
        IApiKeyProvider apiKeyProvider,
        RoutingOptions options,
        IDelay delay,
        RequestThrottle throttle,
        ILogger<OpenRouteServiceClient> logger)
    {
        options.Validate();
        _httpClient = httpClient;
        _apiKeyProvider = apiKeyProvider;
        _options = options;
        _delay = delay;
        _throttle = throttle;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        if (_options.EndpointNotConfigured)
        {
            return false;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return !string.IsNullOrWhiteSpace(await _apiKeyProvider.GetApiKeyAsync().ConfigureAwait(false));
    }

    /// <inheritdoc />
    public async Task<RoutingResult> GetRouteAsync(GeoPosition start, GeoPosition destination, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(destination);

        if (_options.EndpointNotConfigured)
        {
            return RoutingResult.Fail(RoutingFailure.EndpointNotConfigured);
        }

        var credential = await _apiKeyProvider.GetApiKeyAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(credential))
        {
            return RoutingResult.Fail(RoutingFailure.ApiKeyMissing);
        }

        var uri = new Uri(_options.BaseUrl, DirectionsPath);
        var body = string.Create(
            CultureInfo.InvariantCulture,
            $"{{\"coordinates\":[[{start.Longitude:0.######},{start.Latitude:0.######}],[{destination.Longitude:0.######},{destination.Latitude:0.######}]],\"instructions\":false}}");

        var failure = RoutingFailure.Unreachable;
        for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            await _throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
            var retryable = false;
            TimeSpan? retryAfter = null;
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(_options.RequestTimeout);
                using var request = new HttpRequestMessage(HttpMethod.Post, uri);
                request.Headers.UserAgent.ParseAdd(_options.UserAgent);
                if (!request.Headers.TryAddWithoutValidation("Authorization", credential))
                {
                    return RoutingResult.Fail(RoutingFailure.KeyRejected);
                }

                request.Content = new StringContent(body, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
                using var response = await _httpClient.SendAsync(request, timeout.Token).ConfigureAwait(false);
                var status = (int)response.StatusCode;
                if (response.IsSuccessStatusCode)
                {
                    var text = await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                    return Parse(text);
                }

                if (status == 429)
                {
                    failure = RoutingFailure.QuotaExceeded;
                    retryable = true;
                    retryAfter = GetRetryAfter(response);
                }
                else if (status >= 500)
                {
                    failure = RoutingFailure.Unreachable;
                    retryable = true;
                }
                else
                {
                    var text = await ReadErrorBodyAsync(response, timeout.Token).ConfigureAwait(false);
                    _logger.LogWarning("Der Routingdienst hat die Anfrage mit Status {Status} abgelehnt.", status);
                    return RoutingResult.Fail(MapClientError(status, GetErrorCode(text)));
                }

                _logger.LogWarning("Routenberechnung fehlgeschlagen (Versuch {Attempt} von {Max}, Status {Status}).", attempt, _options.MaxAttempts, status);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                failure = RoutingFailure.Unreachable;
                retryable = true;
                _logger.LogWarning("Routenberechnung fehlgeschlagen (Versuch {Attempt} von {Max}, Zeitüberschreitung).", attempt, _options.MaxAttempts);
            }
            catch (HttpRequestException ex)
            {
                // Nur der Typ wird protokolliert: Meldungen des Netzwerkstacks könnten die Anfrageadresse enthalten.
                failure = RoutingFailure.Unreachable;
                retryable = true;
                _logger.LogWarning("Routenberechnung fehlgeschlagen (Versuch {Attempt} von {Max}, {ExceptionType}, {Error}).", attempt, _options.MaxAttempts, ex.GetType().Name, ex.HttpRequestError);
            }

            if (!retryable)
            {
                break;
            }

            if (attempt < _options.MaxAttempts)
            {
                var wait = TimeSpan.FromTicks(_options.RetryBaseDelay.Ticks * (1L << (attempt - 1)));
                if (retryAfter is { } requested && requested > wait)
                {
                    wait = requested;
                }

                await _delay.DelayAsync(wait, cancellationToken).ConfigureAwait(false);
            }
        }

        return RoutingResult.Fail(failure);
    }

    private static TimeSpan? GetRetryAfter(HttpResponseMessage response)
    {
        var delta = response.Headers.RetryAfter?.Delta;
        if (delta is null || delta.Value <= TimeSpan.Zero)
        {
            return null;
        }

        return delta.Value > MaxRetryAfter ? MaxRetryAfter : delta.Value;
    }

    private static async Task<string> ReadErrorBodyAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            return string.Empty;
        }
    }

    private static int? GetErrorCode(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("error", out var error)
                && error.ValueKind == JsonValueKind.Object
                && error.TryGetProperty("code", out var code)
                && code.ValueKind == JsonValueKind.Number
                && code.TryGetInt32(out var value))
            {
                return value;
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static RoutingFailure MapClientError(int status, int? errorCode)
    {
        if (status is 401 or 403)
        {
            return RoutingFailure.KeyRejected;
        }

        return errorCode switch
        {
            2004 => RoutingFailure.RouteTooLong,
            2009 or 2010 => RoutingFailure.RouteNotFound,
            _ => status == 404 ? RoutingFailure.RouteNotFound : RoutingFailure.Rejected,
        };
    }

    private static RoutingResult Parse(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object
                || !document.RootElement.TryGetProperty("routes", out var routes)
                || routes.ValueKind != JsonValueKind.Array
                || routes.GetArrayLength() == 0
                || routes[0].ValueKind != JsonValueKind.Object
                || !routes[0].TryGetProperty("geometry", out var geometry)
                || geometry.ValueKind != JsonValueKind.String)
            {
                return RoutingResult.Fail(RoutingFailure.InvalidResponse);
            }

            if (!PolylineDecoder.TryDecode(geometry.GetString(), PolylinePrecision, MaxRoutePoints, out var points) || points.Count < 2)
            {
                return RoutingResult.Fail(RoutingFailure.InvalidResponse);
            }

            var route = new RouteProfile(points);
            return route.LengthKm > 0 ? RoutingResult.Success(route) : RoutingResult.Fail(RoutingFailure.InvalidResponse);
        }
        catch (JsonException)
        {
            return RoutingResult.Fail(RoutingFailure.InvalidResponse);
        }
    }
}
