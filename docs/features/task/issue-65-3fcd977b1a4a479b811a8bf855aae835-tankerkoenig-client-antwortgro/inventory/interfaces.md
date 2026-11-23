# Interfaces

## `ITankerkoenigClient`
Datei: `src/Tankradar.MAUI/Services/Pricing/TankerkoenigClient.cs` (Zeile 12-43)

Bleibt laut Anforderung unverändert; Fehlervertrag ist `PriceApiException`. Implementiert von `TankerkoenigClient`; Test-Double: `StubTankerkoenigClient`.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchAsync` | `latitude`, `longitude`, `radiusKm`, `cancellationToken` | `Task<IReadOnlyList<StationInfo>>` | Umkreissuche (`list.php`); `PriceApiException` bei Fehlschlag |
| `GetDetailAsync` | `stationId`, `cancellationToken` | `Task<StationInfo>` | Tankstellendetails (`detail.php`); `PriceApiException` bei Fehlschlag |
| `GetPricesAsync` | `stationIds` (1-`MaxPriceIds`), `cancellationToken` | `Task<IReadOnlyDictionary<string, IReadOnlyList<FuelPrice>>>` | Preise je Kennung (`prices.php`); `ArgumentException` bei ungültigen Kennungen/Mengen, `PriceApiException` bei Fehlschlag |

## `IFuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs` (Zeile 11-71)

Konsument des Clients; fängt `PriceApiException` intern ab und liefert Ergebnisrecords mit `PriceFailure` statt Ausnahmen.

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchNearbyAsync` | `query`, `cancellationToken` | `Task<StationSearchResult>` | Umkreissuche mit Cache-/Offline-Rückfall; `ArgumentException` bei ungültiger Anfrage |
| `IsSearchCached` | `query` | `bool` | Cache-Frischeprüfung |
| `GetStationDetailAsync` | `stationId`, `cancellationToken` | `Task<StationDetailResult>` | Stationsdetail mit Rückfall; `ArgumentException` bei ungültiger Kennung |
| `RefreshStationsAsync` | `stationIds`, `force`, `cancellationToken` | `Task<StationRefreshResult>` | Preisaktualisierung (≤ 10 Kennungen je Anfrage) |
| `GetKnownStationsAsync` | `stationIds`, `cancellationToken` | `Task<IReadOnlyList<StationInfo>>` | Nur lokale Daten |
| `CheckAvailabilityAsync` | `cancellationToken` | `Task<PriceFailure>` | Erreichbarkeitsprobe auf Veranlassung des Anwenders |
| `SetCustomNameAsync` | `stationId`, `name`, `cancellationToken` | `Task<StationNameResult>` | Eigener Anzeigename |

## `IApiKeyProvider` / `IApiKeyStore`
Datei: `src/Tankradar.MAUI/Services/Pricing/ApiKeys.cs`

| Interface | Methode | Parameter | Rückgabewert | Zweck |
|-----------|---------|-----------|--------------|-------|
| `IApiKeyProvider` | `GetApiKeyAsync` | — | `Task<string?>` | Liefert den Schlüssel oder `null` (vom `TankerkoenigClient` vor jeder Anfrage abgefragt; `null` → `PriceFailure.ApiKeyMissing`) |
| `IApiKeyStore` | `GetAsync` | — | `Task<string?>` | Gespeicherter Schlüssel |
| `IApiKeyStore` | `SetAsync` | `apiKey` | `Task` | Schlüssel speichern |
| `IApiKeyStore` | `RemoveAsync` | — | `Task` | Schlüssel entfernen |

## `IDelay`
Datei: `src/Tankradar.MAUI/Services/Pricing/Throttling.cs` (Zeile 6-15)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `DelayAsync` | `delay`, `cancellationToken` | `Task` | Abstraktion des Wartens (Wiederholungs- und Drosselungspausen; Test-Double: `RecordingDelay`) |

## `IPriceCacheInvalidator`
Datei: `src/Tankradar.MAUI/Services/Pricing/IPriceCacheInvalidator.cs`

Invalidiert die Such-/Refresh-Caches des `FuelPriceService` (`InvalidateCaches()`); für die Anforderung nur als DI-Registrierung relevant.
