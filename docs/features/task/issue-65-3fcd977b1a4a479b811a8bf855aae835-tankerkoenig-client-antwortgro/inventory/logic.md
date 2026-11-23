# Logik

## `TankerkoenigClient`
Datei: `src/Tankradar.MAUI/Services/Pricing/TankerkoenigClient.cs` (385 Zeilen)

`sealed`, implementiert `ITankerkoenigClient`. Wird von `FuelPriceService` (über `ITankerkoenigClient`) und in Integrations-Tests direkt erzeugt. Konstanten: `MaxPriceIds = 10` (public, Zeile 53), `CredentialParameter = "apikey"` (private, Zeile 55). Abhängigkeiten per Konstruktor: `HttpClient`, `IApiKeyProvider`, `PriceApiOptions` (im Konstruktor via `Validate()` geprüft), `IDelay`, `RequestThrottle`, `TimeProvider`, `ILogger<TankerkoenigClient>`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `.ctor(...)` | public | Prüft `options.Validate()`, speichert Abhängigkeiten (Zeile 75-92) |
| `SearchAsync(lat, lng, radiusKm, ct)` | public | Baut `list.php`-Query, ruft `GetAsync`, `ParseDocument`, `EnsureOk`; parst `stations`-Array via `TryParseStation` (Zeile 95-120) |
| `GetDetailAsync(stationId, ct)` | public | UUID-Prüfung, `detail.php`-Query, `ParseDocument`, `EnsureOk`; parst `station`-Objekt (Zeile 123-143) |
| `GetPricesAsync(stationIds, ct)` | public | Prüft 1-`MaxPriceIds` UUIDs, `prices.php`-Query, `ParseDocument`, `EnsureOk`; parst `prices`-Objekt je Kennung (Zeile 146-191) |
| `GetAsync(relativeQuery, endpointName, ct)` | private | **Einzige Stelle, die HTTP ausführt und den Antwortkörper liest** (Zeile 193-252): `EndpointNotConfigured`-/`ApiKeyMissing`-Vorabprüfung, `MaxAttempts`-Schleife mit `RequestThrottle.WaitAsync`, `CancelAfter(RequestTimeout)`, `_httpClient.GetAsync(uri, token)` (ResponseComplete, ohne CompletionOption), Erfolgskörper via `ReadAsStringAsync` (Zeile 222) — **unbegrenzt**. Status <500 (außer 429) → sofort `PriceApiException(Rejected)`; 429/5xx/`HttpRequestException`/Timeout → `lastError` + Wiederholung mit verdoppelter Wartezeit (`1L << (attempt-1)`); nach allen Versuchen `PriceApiException(Unreachable)` mit `lastError` als InnerException. Caller-Abbruch (`cancellationToken`) propagiert. `HttpRequestException`-Meldungen werden bewusst auf Typname + `HttpRequestError` reduziert, damit URL/Schlüssel nicht leaken (Zeile 239-241). |
| `ParseDocument(body)` | private static | `JsonDocument.Parse(string)`; Nicht-Objekt-Wurzel → `PriceApiException(InvalidResponse)`; `JsonException` → `PriceApiException(InvalidResponse)` mit InnerException (Zeile 254-271) |
| `EnsureOk(root)` | private static | `ok != true` → `PriceApiException(Rejected)` mit `message`-Text (Zeile 273-284) |
| `TryParseStation(element, retrievedUtc, detailed)` | private static | Parst eine Station inkl. Preise; `null` bei fehlenden Pflichtfeldern (Zeile 286-321) |
| `ParseOpeningTimes(element)` | private static | Liest `openingTimes`-Array (Zeile 323-342) |
| `AddPrice(prices, element, property, fuelType, retrievedUtc)` | private static | Übernimmt positive `decimal`-Preise je Sorte (Zeile 344-353) |
| `GetString` / `GetBool` / `TryGetDouble` | private static | JSON-Hilfsleser (Zeile 355-384) |

Abonnierte Events: keine. Publizierte Events: keine. Protokollierung: `LogWarning` je fehlgeschlagenem Versuch mit Endpoint-Name, Versuch, Ausnahmetyp — nie Adresse oder Schlüssel (Zeile 243).

Relevantes Fehlerverhalten für die Anforderung: Eine `HttpRequestException` aus `ReadAsStringAsync` (z. B. bei `MaxResponseContentBufferSize`-Überschreitung des DI-Clients) würde aktuell in den Wiederholungszweig laufen (`catch (HttpRequestException)`, Zeile 237-241) und nach `MaxAttempts` als `Unreachable` enden.

## `FuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs` (462 Zeilen)

`sealed`, implementiert `IFuelPriceService` und `IPriceCacheInvalidator`. Einziger Produktiv-Aufrufer des `ITankerkoenigClient`.

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchNearbyAsync(query, ct)` | public | Suchcache → Offline-Check → `_client.SearchAsync`; fängt `PriceApiException` und liefert `SearchFallbackAsync(query, ex.Failure, ct)` — Cache-Rückfall mit Grund (Zeile 116-152) |
| `IsSearchCached(query)` | public | Cache-Frischeprüfung ohne Abruf (Zeile 155-160) |
| `GetStationDetailAsync(stationId, ct)` | public | Cache → Offline → `_client.GetDetailAsync`; fängt `PriceApiException` → `StationDetailResult(known, OfflineFallback, ex.Failure)` (Zeile 163-195) |
| `RefreshStationsAsync(stationIds, force, ct)` | public | Chunked `_client.GetPricesAsync` (je `TankerkoenigClient.MaxPriceIds`); fängt `PriceApiException` → `failure = ex.Failure`, Abbruch der Schleife (Zeile 198-256) |
| `GetKnownStationsAsync(stationIds, ct)` | public | Nur lokaler Cache, kein Abruf (Zeile 259-264) |
| `SetCustomNameAsync(stationId, name, ct)` | public | Delegiert an `IPriceRepository` (Zeile 267-272) |
| `CheckAvailabilityAsync(ct)` | public | Probe-`SearchAsync` (Berlin-Mitte, r=1); `PriceApiException` → `ex.Failure` als Rückgabe (Zeile 275-291) |
| `InvalidateCaches()` | public | Leert Such- und Refresh-Cache (`IPriceCacheInvalidator`, Zeile 359-363) |
| `WithKnownDetailsAsync` / `WithCustomNamesAsync` / `TrySaveAsync` / `TrySavePricesAsync` / `PruneSearchCache` / `NormalizeIds` / `WithFetchedPrices` / `FilterByFuelTypes` | private | Cache-/Merge-Hilfen; Lesefehler werden geloggt und verschluckt |

Bedeutung für die Anforderung: Jeder neue `PriceFailure`-Wert bzw. jede neue `PriceApiException` aus dem Client wird an allen drei Stellen automatisch in den bestehenden Rückfall (Cache/OfflineFallback + Anzeigetext) überführt; kein weiterer Code nötig.

## `PriceApiException`
Datei: `src/Tankradar.MAUI/Services/Pricing/PriceApiException.cs`

`sealed : Exception` mit `Failure`-Eigenschaft (`PriceFailure`), Konstruktor `(failure, message, innerException?)`. Meldungen enthalten nie Schlüssel oder vollständige Adresse (Klassenkommentar Zeile 6).

## `RequestThrottle` / `TaskDelay`
Datei: `src/Tankradar.MAUI/Services/Pricing/Throttling.cs`

| Typ / Methode | Sichtbarkeit | Kurzbeschreibung |
|---------------|-------------|------------------|
| `IDelay.DelayAsync(delay, ct)` | public (Interface) | Abstraktion des Wartens für Tests |
| `TaskDelay.DelayAsync` | public | `Task.Delay` bzw. `CompletedTask` bei ≤ 0 |
| `RequestThrottle.WaitAsync(ct)` | public | `SemaphoreSlim`-Serialisierung + Mindestabstand über `IDelay`/`TimeProvider` (monotone Uhr); wird von `TankerkoenigClient.GetAsync` vor jedem Versuch aufgerufen |

## `ApiKeyProvider` / Schlüsselablage
Datei: `src/Tankradar.MAUI/Services/Pricing/ApiKeys.cs`

`ApiKeyProvider` (`sealed`, `IApiKeyProvider`): Testmodus → nur Umgebungsvariable; sonst Build-Schlüssel (Assembly-Metadatum `TankerkoenigApiKey`, `ReadBuildTimeKey`) maßgeblich mit Übernahme in die sichere Ablage, sonst gespeicherter Schlüssel. `InMemoryApiKeyStore` (`sealed`, `IApiKeyStore`): Testmodus-Ablage im Speicher. `ApiKeyStoreSelector.Create(getEnvironmentVariable, createPlatformStore)`: wählt Testmodus-In-Memory vs. Plattform-Ablage.

## `MauiProgram` — DI-Registrierungen
Datei: `src/Tankradar.MAUI/MauiProgram.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `AddPriceServices(services)` | private | Zeile 302-339: `PriceApiOptions.FromEnvironment`, `TimeProvider.System`, `IDelay`→`TaskDelay`, `RequestThrottle`, **HttpClient-Registrierung Zeile 312-318: `new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = InfiniteTimeSpan }` + `User-Agent` — kein `MaxResponseContentBufferSize`**; danach `IApiKeyStore` (plattformabhängig), `IApiKeyProvider`→`ApiKeyProvider`, `ITankerkoenigClient`→`TankerkoenigClient`, `IPriceRepository`, `INetworkStatusSource`, `IConnectionMonitor`, `IFuelPriceService`→`FuelPriceService`, `IPriceCacheInvalidator` |
| `AddGeocodingServices(services)` | private | Zeile 199-216: HttpClient mit `AllowAutoRedirect = false`, `Timeout = InfiniteTimeSpan`, **`MaxResponseContentBufferSize = 256 * 1024`** (Zeile 211) → `NominatimGeocodingService` |
| `AddRoutingServices(services)` | public | Zeile 222-280: `RoutingTransport`-Singleton mit HttpClient `AllowAutoRedirect = false`, `Timeout = InfiniteTimeSpan`, **`MaxResponseContentBufferSize = 4 * 1024 * 1024`** (Zeile 236), geteilt von `OpenRouteServiceClient` und `OpenRouteServiceKeyChecker` |
| `AddMapServices(services)` | private | Zeile 282-300: HttpClient ohne Pufferlimit für `HttpTileSource` (Limit wird dort selbst durchgesetzt) |

Sonstige HTTP-Bezugspunkte im Produktivcode: `OpenRouteServiceKeyChecker` nutzt `SendAsync` mit `HttpCompletionOption.ResponseHeadersRead` (`src/Tankradar.MAUI/Services/Routing/OpenRouteServiceKeyChecker.cs:70`); `HttpTileSource` ebenfalls (`src/Tankradar.MAUI/Services/Map/HttpTileSource.cs:181`) und setzt dort das eigene Limit zweistufig um — `Content-Length`-Vorabprüfung (Zeile 206) plus begrenzendes Lesen des Streams (Zeile 211-218). Kein `SocketsHttpHandler`, kein `IHttpClientFactory`/`AddHttpClient` im gesamten `src/`-Baum.
