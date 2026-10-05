using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tankradar.MAUI.Models;
using Tankradar.MAUI.Models.Pricing;

namespace Tankradar.MAUI.Services.Pricing;

/// <summary>
/// Zugriff auf die Tankerkönig-API (Umkreissuche und Tankstellendetails).
/// </summary>
public interface ITankerkoenigClient
{
    /// <summary>
    /// Ruft die Tankstellen im Umkreis einer Position mit allen Sortenpreisen ab.
    /// </summary>
    /// <param name="latitude">Breitengrad.</param>
    /// <param name="longitude">Längengrad.</param>
    /// <param name="radiusKm">Radius in Kilometern.</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Tankstellen; die Preise tragen die Abrufzeit.</returns>
    /// <exception cref="PriceApiException">Der Abruf ist fehlgeschlagen.</exception>
    Task<IReadOnlyList<StationInfo>> SearchAsync(double latitude, double longitude, int radiusKm, CancellationToken cancellationToken);

    /// <summary>
    /// Ruft die Details einer Tankstelle ab.
    /// </summary>
    /// <param name="stationId">Die Kennung der Tankstelle (UUID).</param>
    /// <param name="cancellationToken">Abbruchsignal.</param>
    /// <returns>Die Tankstelle mit Öffnungszeiten und Preisen.</returns>
    /// <exception cref="PriceApiException">Der Abruf ist fehlgeschlagen.</exception>
    Task<StationInfo> GetDetailAsync(string stationId, CancellationToken cancellationToken);
}

/// <summary>
/// Tankerkönig-Client mit Zeitlimit je Anfrage, Wiederholung mit verdoppelter Wartezeit und Drosselung. Der API-Schlüssel wird nie protokolliert.
/// </summary>
public sealed class TankerkoenigClient : ITankerkoenigClient
{
    private const string CredentialParameter = "apikey";

    private readonly HttpClient _httpClient;
    private readonly IApiKeyProvider _apiKeyProvider;
    private readonly PriceApiOptions _options;
    private readonly IDelay _delay;
    private readonly RequestThrottle _throttle;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TankerkoenigClient> _logger;

    /// <summary>
    /// Erstellt den Client.
    /// </summary>
    /// <param name="httpClient">Der HTTP-Client.</param>
    /// <param name="apiKeyProvider">Liefert den API-Schlüssel.</param>
    /// <param name="options">Die Einstellungen (werden geprüft).</param>
    /// <param name="delay">Das Warteverfahren für Wiederholungen.</param>
    /// <param name="throttle">Die Drosselung.</param>
    /// <param name="timeProvider">Die Zeitquelle für den Abrufzeitstempel.</param>
    /// <param name="logger">Logger.</param>
    public TankerkoenigClient(
        HttpClient httpClient,
        IApiKeyProvider apiKeyProvider,
        PriceApiOptions options,
        IDelay delay,
        RequestThrottle throttle,
        TimeProvider timeProvider,
        ILogger<TankerkoenigClient> logger)
    {
        options.Validate();
        _httpClient = httpClient;
        _apiKeyProvider = apiKeyProvider;
        _options = options;
        _delay = delay;
        _throttle = throttle;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StationInfo>> SearchAsync(double latitude, double longitude, int radiusKm, CancellationToken cancellationToken)
    {
        var query = string.Create(
            CultureInfo.InvariantCulture,
            $"list.php?lat={latitude:0.######}&lng={longitude:0.######}&rad={radiusKm}&sort=dist&type=all");
        var body = await GetAsync(query, "list", cancellationToken).ConfigureAwait(false);
        var retrievedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        using var document = ParseDocument(body);
        EnsureOk(document.RootElement);

        if (!document.RootElement.TryGetProperty("stations", out var stations) || stations.ValueKind != JsonValueKind.Array)
        {
            throw new PriceApiException(PriceFailure.InvalidResponse, "Die Antwort enthält keine Tankstellenliste.");
        }

        var result = new List<StationInfo>();
        foreach (var element in stations.EnumerateArray())
        {
            if (TryParseStation(element, retrievedUtc, detailed: false) is { } station)
            {
                result.Add(station);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<StationInfo> GetDetailAsync(string stationId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(stationId, out var id))
        {
            throw new ArgumentException("Die Tankstellenkennung muss eine UUID sein.", nameof(stationId));
        }

        var body = await GetAsync($"detail.php?id={id:D}", "detail", cancellationToken).ConfigureAwait(false);
        var retrievedUtc = _timeProvider.GetUtcNow().UtcDateTime;
        using var document = ParseDocument(body);
        EnsureOk(document.RootElement);

        if (!document.RootElement.TryGetProperty("station", out var station)
            || station.ValueKind != JsonValueKind.Object
            || TryParseStation(station, retrievedUtc, detailed: true) is not { } parsed)
        {
            throw new PriceApiException(PriceFailure.InvalidResponse, "Die Antwort enthält keine auswertbare Tankstelle.");
        }

        return parsed;
    }

    private async Task<string> GetAsync(string relativeQuery, string endpointName, CancellationToken cancellationToken)
    {
        var credential = await _apiKeyProvider.GetApiKeyAsync().ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(credential))
        {
            throw new PriceApiException(PriceFailure.ApiKeyMissing, "Es ist kein API-Schlüssel hinterlegt.");
        }

        var uri = new Uri(_options.BaseUrl, $"{relativeQuery}&{CredentialParameter}={Uri.EscapeDataString(credential)}");
        Exception? lastError = null;

        for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
        {
            await _throttle.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(_options.RequestTimeout);
                using var response = await _httpClient.GetAsync(uri, timeout.Token).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync(timeout.Token).ConfigureAwait(false);
                }

                var status = (int)response.StatusCode;
                if (status != 429 && status < 500)
                {
                    throw new PriceApiException(PriceFailure.Rejected, $"Der Preisdienst hat die Anfrage mit Status {status} abgelehnt.");
                }

                lastError = new HttpRequestException($"Der Preisdienst antwortete mit Status {status}.");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = new TimeoutException("Zeitüberschreitung beim Abruf des Preisdienstes.");
            }
            catch (HttpRequestException ex)
            {
                // Nur der Typ wird weitergegeben: Meldungen des Netzwerkstacks könnten die Anfrageadresse samt Schlüssel enthalten.
                lastError = new HttpRequestException($"Verbindungsfehler ({ex.GetType().Name}, {ex.HttpRequestError}).");
            }

            _logger.LogWarning("Abruf '{Endpoint}' fehlgeschlagen (Versuch {Attempt} von {Max}): {Reason}", endpointName, attempt, _options.MaxAttempts, lastError.GetType().Name);
            if (attempt < _options.MaxAttempts)
            {
                var wait = TimeSpan.FromTicks(_options.RetryBaseDelay.Ticks * (1L << (attempt - 1)));
                await _delay.DelayAsync(wait, cancellationToken).ConfigureAwait(false);
            }
        }

        throw new PriceApiException(PriceFailure.Unreachable, "Der Preisdienst ist nicht erreichbar.", lastError);
    }

    private static JsonDocument ParseDocument(string body)
    {
        try
        {
            var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                document.Dispose();
                throw new PriceApiException(PriceFailure.InvalidResponse, "Die Antwort des Preisdienstes hat ein unerwartetes Format.");
            }

            return document;
        }
        catch (JsonException ex)
        {
            throw new PriceApiException(PriceFailure.InvalidResponse, "Die Antwort des Preisdienstes ist kein gültiges JSON.", ex);
        }
    }

    private static void EnsureOk(JsonElement root)
    {
        if (root.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True)
        {
            return;
        }

        var message = root.TryGetProperty("message", out var text) && text.ValueKind == JsonValueKind.String
            ? text.GetString()
            : null;
        throw new PriceApiException(PriceFailure.Rejected, $"Der Preisdienst hat die Anfrage abgelehnt{(message is null ? "." : ": " + message)}");
    }

    private static StationInfo? TryParseStation(JsonElement element, DateTime retrievedUtc, bool detailed)
    {
        if (element.ValueKind != JsonValueKind.Object
            || GetString(element, "id") is not { } id
            || !Guid.TryParse(id, out _)
            || GetString(element, "name") is not { } name
            || !TryGetDouble(element, "lat", out var latitude)
            || !TryGetDouble(element, "lng", out var longitude))
        {
            return null;
        }

        var prices = new List<FuelPrice>();
        AddPrice(prices, element, "e5", FuelType.SuperE5, retrievedUtc);
        AddPrice(prices, element, "e10", FuelType.SuperE10, retrievedUtc);
        AddPrice(prices, element, "diesel", FuelType.Diesel, retrievedUtc);

        return new StationInfo
        {
            Id = id,
            Name = name,
            Brand = GetString(element, "brand"),
            Street = GetString(element, "street"),
            HouseNumber = GetString(element, "houseNumber"),
            PostCode = GetString(element, "postCode"),
            Place = GetString(element, "place"),
            Latitude = latitude,
            Longitude = longitude,
            DistanceKm = TryGetDouble(element, "dist", out var distance) ? distance : null,
            IsOpen = GetBool(element, "isOpen"),
            WholeDay = detailed ? GetBool(element, "wholeDay") : null,
            OpeningTimes = detailed ? ParseOpeningTimes(element) : [],
            Prices = prices,
            DetailsUpdatedUtc = detailed ? retrievedUtc : null,
        };
    }

    private static List<OpeningTimeEntry> ParseOpeningTimes(JsonElement element)
    {
        var result = new List<OpeningTimeEntry>();
        if (!element.TryGetProperty("openingTimes", out var times) || times.ValueKind != JsonValueKind.Array)
        {
            return result;
        }

        foreach (var entry in times.EnumerateArray())
        {
            if (entry.ValueKind == JsonValueKind.Object
                && GetString(entry, "start") is { } start
                && GetString(entry, "end") is { } end)
            {
                result.Add(new OpeningTimeEntry(GetString(entry, "text") ?? string.Empty, start, end));
            }
        }

        return result;
    }

    private static void AddPrice(List<FuelPrice> prices, JsonElement element, string property, FuelType fuelType, DateTime retrievedUtc)
    {
        if (element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetDecimal(out var price)
            && price > 0m)
        {
            prices.Add(new FuelPrice(fuelType, price, retrievedUtc));
        }
    }

    private static string? GetString(JsonElement element, string property)
    {
        return element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;
    }

    private static bool? GetBool(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static bool TryGetDouble(JsonElement element, string property, out double value)
    {
        value = 0;
        return element.TryGetProperty(property, out var number)
            && number.ValueKind == JsonValueKind.Number
            && number.TryGetDouble(out value)
            && double.IsFinite(value);
    }
}
