# Interfaces

## `IFavoritesService`
Datei: `src/Tankradar.MAUI/Services/Favorites/IFavoritesService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetGroupsAsync` | `CancellationToken` | `IReadOnlyList<FavoriteGroup>` | alle Gruppen nach Namen |
| `GetGroupAsync` | `groupId` | `FavoriteGroup?` | eine Gruppe |
| `CreateGroupAsync` | `name`, `description` | `FavoriteGroupResult` | Gruppe anlegen |
| `UpdateGroupAsync` | `groupId`, `name`, `description` | `FavoriteResult` | umbenennen/Beschreibung |
| `DeleteGroupAsync` | `groupId` | `FavoriteResult` | löschen samt Zuordnungen |
| `GetGroupsOfStationAsync` | `stationId` | `IReadOnlyList<FavoriteGroup>` | Gruppen einer Tankstelle |
| `AddStationAsync` | `groupId`, `stationId` | `FavoriteResult` | zuordnen |
| `AddStationToNewGroupAsync` | `name`, `stationId` | `FavoriteGroupResult` | Gruppe anlegen und zuordnen |
| `RemoveStationAsync` | `stationId`, `groupIds` | `FavoriteResult` | entfernen |
| `GetEntriesAsync` | `groupId` | `IReadOnlyList<FavoriteEntry>` | Einträge einer Gruppe (Priorität, Name); **keine Methode für alle Gruppen mit Einträgen auf einmal** |
| `UpdateEntryAsync` | `groupId`, `stationId`, `note`, `priority` | `FavoriteResult` | Notiz/Priorität |

Ereignis `Changed` (nach jeder gespeicherten Änderung), Anmeldung schwach über `FavoritesChangeSubscription.Attach`.

## `IFuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchNearbyAsync` | `StationSearchQuery` (Position, Radius 1–25, Sorten) | `StationSearchResult` | Umkreissuche; 5-Minuten-Cache je Schlüssel (`PriceApiOptions.CacheLifetime`); Offline-/Fehler-Fallback aus `IPriceRepository.FindNearbyAsync` |
| `GetStationDetailAsync` | `stationId` (UUID) | `StationDetailResult` | Details mit Preisen; Cache nach `DetailsUpdatedUtc` |
| `CheckAvailabilityAsync` | – | `PriceFailure` | Verfügbarkeitsprüfung (Einstellungen) |

Es gibt keine Methode, die die Preise mehrerer bekannter Tankstellen nach Kennungen aktualisiert.

## `IPriceRepository`
Datei: `src/Tankradar.MAUI/Services/Pricing/PriceRepository.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SaveAsync` | `IReadOnlyCollection<StationInfo>` | `Task` | Stammdaten aktualisieren, Preise als neue Zeilen anhängen |
| `GetStationAsync` | `stationId` | `StationInfo?` | eine Tankstelle mit jüngsten Preisen |
| `GetKnownDetailsAsync` | `stationIds` | `IReadOnlyDictionary<string, StationInfo>` | Detailangaben ohne Preise |
| `FindNearbyAsync` | `latitude`, `longitude`, `radiusKm` | `IReadOnlyList<StationInfo>` | lokale Tankstellen im Radius mit Entfernung und jüngsten Preisen |

Statisch und öffentlich: `PriceRepository.DistanceKm(lat1, lon1, lat2, lon2)`. Es gibt keine Methode, die mehrere Tankstellen nach Kennungen mit Preisen liefert, und keine, die nur Preise speichert.

## `ITankerkoenigClient`
Datei: `src/Tankradar.MAUI/Services/Pricing/TankerkoenigClient.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchAsync` | `latitude`, `longitude`, `radiusKm` | `IReadOnlyList<StationInfo>` | `list.php` |
| `GetDetailAsync` | `stationId` | `StationInfo` | `detail.php` |

Gemeinsames `GetAsync` mit Schlüssel, Drosselung (`RequestThrottle`, Standard 1 s), Wiederholung (3 Versuche), Zeitlimit. Kein Abruf per `prices.php`.

## `IConnectionMonitor` / `INetworkStatusSource`
Datei: `src/Tankradar.MAUI/Services/Pricing/ConnectionMonitor.cs`

`IConnectionMonitor`: `IsOnline`, Ereignisse `ConnectionChanged(bool)` und `ConnectionRestored`. `INetworkStatusSource`: `IsConnected`, `StatusChanged`. Testdoubles `FakeConnectionMonitor`, `FakeNetworkStatusSource`.

## `ILocationService`
Datei: `src/Tankradar.MAUI/Services/Location/ILocationService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetCurrentLocationAsync` | `GpsUsage`, `CancellationToken` | `LocationResult` | aktueller Standort; bei `GpsUsage.Never` Status `DisabledBySetting`; im Testmodus `TestLocationService` (Variable `TANKATLAS_TEST_LOCATION`) |

## `ISettingsService`
Datei: `src/Tankradar.MAUI/Services/ISettingsService.cs`

`LoadAsync()` → `AppSettings`, `SaveAsync(AppSettings)`.

## `IStationNavigator` und `IFavoriteGroupNavigator`
Dateien: `src/Tankradar.MAUI/Services/Navigation/`

`IStationNavigator.OpenDetailAsync(StationListItem)` (Route `stationdetail`, Parameter `station`). `IFavoriteGroupNavigator.OpenGroupAsync(long)` und `CloseAsync()` (Route `favoritegroup`, Parameter `groupId`). `FavoriteGroupViewModel` kennt `IStationNavigator` bisher nicht.
