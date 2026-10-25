using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models.Routing;
using Tankradar.MAUI.Services.Pricing;

namespace Tankradar.MAUI.Services.Routing;

/// <summary>
/// Prüft einen OpenRouteService-Schlüssel auf ausdrücklichen Wunsch des Anwenders per kleiner Testanfrage.
/// </summary>
public interface IRoutingKeyChecker
{
    /// <summary>
    /// Schickt eine kleine Routenanfrage mit festen, nicht personenbezogenen Koordinaten an den Dienst und wertet nur den Status aus.
    /// </summary>
    /// <param name="key">Der zu prüfende Schlüssel (bereits auf zulässige Schreibweise geprüft).</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Das Ergebnis der Prüfung.</returns>
    Task<RoutingKeyCheckResult> CheckAsync(string key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Prüfung des Schlüssels gegen OpenRouteService (Directions): genau eine Anfrage mit Zeitlimit und ohne Wiederholung, damit die Rückmeldung zügig kommt und
/// das Kontingent nicht belastet wird. Der Schlüssel steht nur im Anfragekopf und wird nie protokolliert oder in Meldungen übernommen.
/// </summary>
public sealed class OpenRouteServiceKeyChecker : IRoutingKeyChecker
{
    private const string DirectionsPath = "v2/directions/driving-car";
    private const string ProbeBody = "{\"coordinates\":[[13.405,52.52],[13.415,52.525]],\"instructions\":false}";

    private readonly HttpClient _httpClient;
    private readonly RoutingOptions _options;
    private readonly RequestThrottle _throttle;
    private readonly ILogger<OpenRouteServiceKeyChecker> _logger;

    /// <summary>
    /// Erstellt die Prüfung.
    /// </summary>
    /// <param name="httpClient">Der HTTP-Client (ohne automatische Weiterleitungen).</param>
    /// <param name="options">Die Einstellungen des Routingdienstes (werden geprüft).</param>
    /// <param name="throttle">Die Drosselung des Routingdienstes (gemeinsam mit der Routenberechnung).</param>
    /// <param name="logger">Logger (protokolliert nie den Schlüssel).</param>
    public OpenRouteServiceKeyChecker(HttpClient httpClient, RoutingOptions options, RequestThrottle throttle, ILogger<OpenRouteServiceKeyChecker> logger)
    {
        options.Validate();
        _httpClient = httpClient;
        _options = options;
        _throttle = throttle;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<RoutingKeyCheckResult> CheckAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_options.EndpointNotConfigured || RoutingKeyFormat.Validate(key, out var normalized) != RoutingKeyError.None)
        {
            return RoutingKeyCheckResult.Unavailable;
        }

        await _throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(_options.RequestTimeout);
            using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(_options.BaseUrl, DirectionsPath));
            request.Headers.UserAgent.ParseAdd(_options.UserAgent);
            request.Headers.TryAddWithoutValidation("Authorization", normalized);
            request.Content = new StringContent(ProbeBody, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false);
            return Map((int)response.StatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Die Prüfung des Routing-Schlüssels ist abgelaufen (Zeitüberschreitung).");
            return RoutingKeyCheckResult.Unreachable;
        }
        catch (HttpRequestException ex)
        {
            // Nur der Typ wird protokolliert: Meldungen des Netzwerkstacks könnten die Anfrageadresse enthalten.
            _logger.LogWarning("Die Prüfung des Routing-Schlüssels ist fehlgeschlagen ({ExceptionType}, {Error}).", ex.GetType().Name, ex.HttpRequestError);
            return RoutingKeyCheckResult.Unreachable;
        }
    }

    private static RoutingKeyCheckResult Map(int status)
    {
        if (status is >= 200 and < 300)
        {
            return RoutingKeyCheckResult.Valid;
        }

        return status switch
        {
            401 or 403 => RoutingKeyCheckResult.Invalid,
            429 => RoutingKeyCheckResult.QuotaExceeded,
            >= 500 => RoutingKeyCheckResult.Unreachable,
            _ => RoutingKeyCheckResult.Unavailable,
        };
    }
}
