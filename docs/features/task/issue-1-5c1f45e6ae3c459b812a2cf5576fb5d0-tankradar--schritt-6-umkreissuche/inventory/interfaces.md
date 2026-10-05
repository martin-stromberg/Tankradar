## `IFuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchNearbyAsync` | `StationSearchQuery`, `CancellationToken` | `Task<StationSearchResult>` | Umkreissuche mit Rückfall |
| `GetStationDetailAsync` | `string stationId`, `CancellationToken` | `Task<StationDetailResult>` | Details |
| `CheckAvailabilityAsync` | `CancellationToken` | `Task<PriceFailure>` | Erreichbarkeitsprüfung |

## `IConnectionMonitor` / `INetworkStatusSource`
Datei: `src/Tankradar.MAUI/Services/Pricing/ConnectionMonitor.cs`

| Mitglied | Zweck |
|----------|-------|
| `IsOnline` | aktueller Verbindungszustand |
| `ConnectionChanged` (`EventHandler<bool>`), `ConnectionRestored` | Ereignisse |
| `INetworkStatusSource.IsConnected`, `StatusChanged` | Quelle |

## `IPriceRepository`
Datei: `src/Tankradar.MAUI/Services/Pricing/PriceRepository.cs`: `SaveAsync`, `GetStationAsync`, `FindNearbyAsync(latitude, longitude, radiusKm, ct)`.

## `ITankerkoenigClient`
Datei: `src/Tankradar.MAUI/Services/Pricing/TankerkoenigClient.cs`: `SearchAsync(latitude, longitude, radiusKm, ct)`, `GetDetailAsync(id, ct)`.

## `ISettingsService`, `IAppDataPathProvider`, `IApiKeyStore`, `IApiKeyProvider`
`ISettingsService.LoadAsync/SaveAsync` (`src/Tankradar.MAUI/Services/ISettingsService.cs`); `IAppDataPathProvider.GetDataDirectory()`; Schlüsselablage und -provider in `Services/Pricing/ApiKeys.cs`.

Ein Interface für den Gerätestandort existiert nicht.
