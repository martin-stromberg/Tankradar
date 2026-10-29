# Interfaces (Kartenansicht)

## `IFuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchNearbyAsync` | `StationSearchQuery query`, `CancellationToken` | `Task<StationSearchResult>` | Umkreissuche (Mittelpunkt, Radius, Kraftstoffarten); aufgerufen von `MapViewModel.RunSearchAsync`, `RouteStationSearchService` und `NearbySectionViewModel` |
| `IsSearchCached` | `StationSearchQuery query` | `bool` | Prüft, ob eine Suche aus dem Cache beantwortet werden kann |
| `GetStationDetailAsync` | `string stationId`, `CancellationToken` | `Task<StationDetailResult>` | Details einer Tankstelle |
| `RefreshStationsAsync` | `IReadOnlyCollection<string> stationIds`, `bool force`, `CancellationToken` | `Task<StationRefreshResult>` | Preisaktualisierung für Kennungen |

Die Schnittstelle kennt nur die Umkreissuche über einen Mittelpunkt und einen Radius; eine Suche nach Kartenausschnitt ist nicht vorhanden.

## `IRouteSearchAccess`
Datei: `src/Tankradar.MAUI/Services/Routing/RouteSearchAccess.cs` (erbt von `IApiKeyProvider`)

| Methode / Ereignis | Parameter | Rückgabewert | Zweck |
|--------------------|-----------|--------------|-------|
| `Changed` (Ereignis) | – | `EventHandler?` | Meldet Änderungen von Schlüssel oder Schalter; `MapViewModel` abonniert es (`OnRouteAccessChanged`) |
| `HasKeyAsync` | – | `Task<bool>` | Prüft, ob ein Schlüssel hinterlegt ist |
| `IsEnabledAsync` | – | `Task<bool>` | Prüft, ob die Routensuche aktiviert ist (Sichtbarkeit der Suchart `Route`) |
| `GetStoredKeyAsync` | – | `Task<string?>` | Liest den gespeicherten Schlüssel |
| `SaveKeyAsync` | `string key` | `Task` | Speichert den Schlüssel |
| `RemoveKeyAsync` | – | `Task` | Entfernt den Schlüssel |

## `IRouteSearchService`
Datei: `src/Tankradar.MAUI/Services/Routing/RouteSearchService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `SearchAsync` | `RouteSearchRequest request`, `IProgress<RouteSearchProgress>? progress`, `CancellationToken` | `Task<RouteSearchResult>` | Routensuche mit Fortschritt; aufgerufen aus `MapViewModel.ExecuteRouteSearchAsync` |

## `IStationNavigator`
Datei: `src/Tankradar.MAUI/Services/Navigation/IStationNavigator.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `OpenDetailAsync` | `StationListItem station` | `Task` | Öffnet die Tankstellen-Detailansicht; aufgerufen aus `MapViewModel.OpenStationAsync` (über `OpenStationCommand`, auch aus Karten-Markierungen) |

## `ITileSource`
Datei: `src/Tankradar.MAUI/Services/Map/ITileSource.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetTileAsync` | `TileKey key`, `CancellationToken` | `Task<byte[]?>` | Liefert die Bilddaten einer Kachel; wird von `StationMapView` über die Eigenschaft `TileSource` genutzt, gesetzt im Konstruktor von `MapPage` |

## `ILocationService`
Datei: `src/Tankradar.MAUI/Services/Location/ILocationService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetCurrentLocationAsync` | `GpsUsage gpsUsage`, `CancellationToken` | `Task<LocationResult>` | Aktueller Standort für die Suche "Standort" und den Routenstart |
