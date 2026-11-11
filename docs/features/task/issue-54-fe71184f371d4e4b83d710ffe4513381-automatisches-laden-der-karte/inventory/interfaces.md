# Interfaces

Betroffene Schnittstellen der Anforderung „Automatisches Laden der Karte“. Registriert sind alle vier als **Singletons** in `MauiProgram.cs`.

## `ILocationService`
Datei: `src/Tankradar.MAUI/Services/Location/ILocationService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetCurrentLocationAsync` | `gpsUsage` (`GpsUsage`), `cancellationToken` (`CancellationToken = default`) | `Task<LocationResult>` | Ermittelt den Standort nur auf Anforderung und gemäß Einstellung; bei `GpsUsage.Never` weder Berechtigung noch Position abgefragt. Fehler führen zu einem Status, nicht zu einer Ausnahme. Positionen werden nie gespeichert. |

Implementierungen: `MauiLocationService` (Essentials: `Permissions.LocationWhenInUse`, `Geolocation`, 10 s Timeout), `TestLocationService` (fester Standort aus `TANKATLAS_TEST_LOCATION`); Auswahl über `LocationServiceSelector`. Aufrufer: `MapViewModel.ResolveCurrentLocationAsync`, `FavoritesViewModel.GetLocationAsync`. Es gibt keinen weiteren Contract zum Teilen eines `LocationResult` zwischen Komponenten.

## `IFuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs` (Zeilen 11–71)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchNearbyAsync` | `query` (`StationSearchQuery`), `cancellationToken` | `Task<StationSearchResult>` | Umkreissuche; validiert die Anfrage, bedient den Such-Cache oder ruft die API ab, Fehler → `OfflineFallback` statt Ausnahme. |
| `IsSearchCached` | `query` (`StationSearchQuery`) | `bool` | Gibt an, ob `SearchNearbyAsync` für diese Anfrage aus dem Cache bedient würde (Schlüssel: gerundete Koordinaten + Radius). |
| `GetStationDetailAsync` | `stationId` (`string`), `cancellationToken` | `Task<StationDetailResult>` | Detailabfrage einer Tankstelle (UUID) mit Cache/Fallback. |
| `RefreshStationsAsync` | `stationIds`, `force`, `cancellationToken` | `Task<StationRefreshResult>` | Preisaktualisierung nach Kennungen (max. 10 je Anfrage). |
| `GetKnownStationsAsync` | `stationIds`, `cancellationToken` | `Task<IReadOnlyList<StationInfo>>` | Lokal bekannte Tankstellen ohne Abruf. |
| `CheckAvailabilityAsync` | `cancellationToken` | `Task<PriceFailure>` | Erreichbarkeits-/Schlüsselprobe auf Veranlassung des Anwenders. |
| `SetCustomNameAsync` | `stationId`, `name`, `cancellationToken` | `Task<StationNameResult>` | Eigenen Anzeigenamen vergeben/entfernen. |

Implementierung: `FuelPriceService` (Singleton; `_searchCache` mit `CacheLifetime` 5 min). Für die Anforderung maßgeblich sind `SearchNearbyAsync` und `IsSearchCached`; Aufrufer von `SearchNearbyAsync`: `MapViewModel.RunSearchAsync`, `MapViewModel.ReloadVisibleAreaAsync`, `NearbySectionViewModel.LoadAsync`, `RouteStationSearchService`.

## `ISettingsService`
Datei: `src/Tankradar.MAUI/Services/ISettingsService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `LoadAsync` | `cancellationToken` | `Task<AppSettings>` | Lädt die gespeicherten Einstellungen; fehlende/ungültige Werte werden durch Standardwerte ersetzt. |
| `SaveAsync` | `settings` (`AppSettings`), `cancellationToken` | `Task` | Speichert die Einstellungen atomar (`ArgumentException` bei ungültigen Werten). |

Aufrufer im Kontext: `MapViewModel.LoadSettingsAsync`/`GetSettingsOrReportAsync`, `FavoritesViewModel.LoadAsync`/`RefreshAsync`. Es existiert keine Einstellung für eine automatische Startsuche.

## `IConnectionMonitor`
Datei: `src/Tankradar.MAUI/Services/Pricing/ConnectionMonitor.cs` (Zeilen 22–38)

| Member | Typ | Zweck |
|--------|-----|-------|
| `IsOnline` | `bool` | Meldet, ob aktuell eine Netzverbindung besteht (Offline-Banner; Adresssuche erfordert Verbindung). |
| `ConnectionChanged` | `event EventHandler<bool>?` | Änderung des Verbindungszustands; `MapViewModel` und `FavoritesViewModel` abonnieren es in `OnAppearing` und melden sich in `OnDisappearing` ab. |
| `ConnectionRestored` | `event EventHandler?` | Verbindung nach Ausfall wiederhergestellt; nur `FavoritesViewModel` nutzt es (automatische Aktualisierung). |
