# Interfaces (Bestand)

## `IGeocodingService`
Datei: `src/Tankradar.MAUI/Services/Geocoding/NominatimGeocodingService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ResolveAsync` | `string? input`, `CancellationToken` | `Task<GeocodingResult>` | Validiert (`AddressInput.Validate`), fragt Nominatim (`limit=1`, `countrycodes=de`), liefert den ersten Treffer |

## `ITankerkoenigClient`
Datei: `src/Tankradar.MAUI/Services/Pricing/TankerkoenigClient.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchAsync` | `latitude`, `longitude`, `radiusKm`, `ct` | `Task<IReadOnlyList<StationInfo>>` | `list.php` (Umkreis, `sort=dist`) |
| `GetDetailAsync` | `stationId`, `ct` | `Task<StationInfo>` | `detail.php` |
| `GetPricesAsync` | `IReadOnlyCollection<string>` (1 bis 10 Kennungen), `ct` | `Task<IReadOnlyDictionary<string, IReadOnlyList<FuelPrice>>>` | `prices.php` |

## `IFuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchNearbyAsync` | `StationSearchQuery`, `ct` | `Task<StationSearchResult>` | Umkreissuche mit Cache (`CacheLifetime`), Offline-Rückfall, Speichern, Sortierung nach `DistanceKm`, Filter nach Sorten |
| `GetStationDetailAsync` | `stationId`, `ct` | `Task<StationDetailResult>` | Detail |
| `RefreshStationsAsync` | `ids`, `force`, `ct` | `Task<StationRefreshResult>` | Preise aktualisieren |
| `GetKnownStationsAsync` | `ids`, `ct` | `Task<IReadOnlyList<StationInfo>>` | lokal bekannte Stationen |
| `CheckAvailabilityAsync` | `ct` | `Task<PriceFailure>` | Erreichbarkeit und Schlüssel |
| `SetCustomNameAsync` | `stationId`, `name`, `ct` | `Task<StationNameResult>` | Anzeigename |

## `IApiKeyProvider` und `IApiKeyStore`
Datei: `src/Tankradar.MAUI/Services/Pricing/ApiKeys.cs`

| Interface | Methode | Zweck |
|-----------|---------|-------|
| `IApiKeyProvider` | `GetApiKeyAsync()` | Schlüssel (Testmodus: Umgebungsvariable `TANKRADAR_PRICE_API_KEY`; sonst Build-Schlüssel, der die Ablage aktualisiert; sonst gespeicherter) |
| `IApiKeyStore` | `GetAsync()`, `SetAsync(string)` | sichere Ablage |

Implementierungen: `SecureStorageApiKeyStore` (Schlüsselname `tankatlas.price-api-key`, iOS Keychain), `Platforms/Windows/CredentialLockerApiKeyStore`, `InMemoryApiKeyStore` (Testmodus), Auswahl über `ApiKeyStoreSelector`. Die Verdrahtung ist auf genau einen Schlüssel festgelegt (Assembly-Metadatum `TankerkoenigApiKey`, `ApiKeyProvider.ReadBuildTimeKey` ist statisch).

## `ILocationService`
Datei: `src/Tankradar.MAUI/Services/Location/ILocationService.cs`: `GetCurrentLocationAsync(GpsUsage, ct)` -> `Task<LocationResult>`. Implementierungen `MauiLocationService`, `TestLocationService` (Testmodus, `TANKATLAS_TEST_LOCATION`); Auswahl über `LocationServiceSelector`.

## `ITileSource`
Datei: `src/Tankradar.MAUI/Services/Map/ITileSource.cs`; Implementierung `HttpTileSource`.

## `IDelay` und `RequestThrottle`
Datei: `src/Tankradar.MAUI/Services/Pricing/Throttling.cs`: `IDelay.DelayAsync`; `RequestThrottle.WaitAsync` erzwingt einen Mindestabstand zwischen Anfragen (monotone Uhr, serialisiert über Semaphor).

## `IStationNavigator`
Öffnet die Detailansicht (`ShellStationNavigator`); genutzt vom `MapViewModel` (`OpenStationCommand`).

Es gibt keine Interfaces für Routing oder Streckengeometrie.
