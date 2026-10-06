# Logik, ViewModels und Oberfläche

## `FavoritesService`
Datei: `src/Tankradar.MAUI/Services/Favorites/FavoritesService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| Methoden von `IFavoritesService` | public | EF-Core auf SQLite (`IDbContextFactory<TankradarDbContext>`, `IDatabaseInitializer`, `TimeProvider`) |
| `SaveAsync` | private | Speichert, löst `Changed` aus. Fängt `DbUpdateException` mit `SqliteException` Code 2067 (UNIQUE) **pauschal** als `FavoriteResult.DuplicateName` ab, auch wenn der Index `FavoriteEntries (GroupId, StationId)` verletzt wurde (gleichzeitiges Zuordnen derselben Tankstelle zur selben Gruppe). Die Vorabprüfung in `AddStationAsync` liefert `AlreadyMember` nur, wenn der Eintrag zum Prüfzeitpunkt schon gespeichert war |
| `CreateAsync`, `NameTakenAsync`, `Validate` | private | Anlegen mit optionaler Zuordnung, Namensprüfung mit `OrdinalIgnoreCase` |

Publizierte Events: `Changed`.

## `FavoritesViewModel` / `FavoritesPage` (derzeitige Seite „Favoriten“)
Dateien: `src/Tankradar.MAUI/ViewModels/FavoritesViewModel.cs`, `Views/FavoritesPage.xaml(.cs)`

Zeigt `Groups` (`FavoriteGroupItemViewModel`: Name, Beschreibung, Anzahl, `OpenCommand`), Hinweis `NoGroups` (`ShowEmptyHint` nach dem Laden), Formular „Neue Gruppe“ (`ShowCreateCommand`, `CreateCommand`, `CancelCreateCommand`), `StatusMessage`. Kein Preis, keine Entfernung, kein Offline-Hinweis, keine Aktualisierung, kein Standort. Lädt in `OnAppearing`; folgt `IFavoritesService.Changed` über `FavoritesChangeSubscription`. Konstruktor: `IFavoritesService`, `IFavoriteGroupNavigator`, `ILogger`. AutomationIds: `FavoritesPage.Headline`, `Favorites.Status`, `Favorites.Empty`, `Favorites.Group.Name|Description|Count|Open`, `Favorites.NewGroup`, `Favorites.NewName`, `Favorites.NewDescription`, `Favorites.NewCreate`, `Favorites.NewCancel`. Die Seite besteht aus einem `ScrollView` mit `VerticalStackLayout`.

## `FavoriteGroupViewModel` / `FavoriteEntryItemViewModel` / `FavoriteGroupPage`
Dateien: `src/Tankradar.MAUI/ViewModels/FavoriteGroupViewModel.cs`, `FavoriteEntryItemViewModel.cs`, `Views/FavoriteGroupPage.xaml(.cs)`

Gruppenansicht (Route `favoritegroup`, `IQueryAttributable`): Bearbeiten, Löschen mit Rückfrage, Einträge mit Notiz und Priorität (`FavoriteEntryItemViewModel`: `StationId`, `StationName`, `AddressText`, `Note`, `Priority`, `EditCommand`, `SaveCommand`, `CancelCommand`). Einträge haben **keinen Befehl zum Öffnen der Detailansicht**; `FavoriteGroupViewModel` hat keine `IStationNavigator`-Abhängigkeit. AutomationIds `FavoriteGroup.*`, `FavoriteGroup.Entry.*`.

## `StationDetailViewModel` (Vorbild für Offline-/Wiederverbindungsverhalten)
Datei: `src/Tankradar.MAUI/ViewModels/StationDetailViewModel.cs`

`OnAppearing` abonniert `ConnectionChanged` und `ConnectionRestored`, `OnDisappearing` meldet ab und bricht den Abruf ab. `OnConnectionRestored` lädt neu, wenn das letzte Ergebnis `PriceDataSource.OfflineFallback` war oder `Detail.HasStalePrice`. `IsOffline`/`OfflineMessage` (`SearchTexts.OfflineBanner`). Nimmt eine `StationListItem` über `ApplyQueryAttributes` (Parameter `station`) entgegen; bei `Show` wird `StationFavoritesViewModel.SetStationAsync(origin.Id)` aufgerufen.

## `MapViewModel` (Vorbild für Standort, Filter, Aufbereitung)
Datei: `src/Tankradar.MAUI/ViewModels/MapViewModel.cs`

Ermittelt den Standort über `ILocationService.GetCurrentLocationAsync(settings.GpsUsage)`, ruft `IFuelPriceService.SearchNearbyAsync`, bereitet mit `StationResultBuilder.Build` auf, kennt Sortierung, Filter-Chips (`FuelFilterOptionViewModel`), Offline-Hinweis (`ConnectionChanged`), Öffnen der Detailansicht über `IStationNavigator.OpenDetailAsync`. Die Karte aktualisiert nach Wiederverbindung **nicht** automatisch (nur `ConnectionChanged` abonniert).

## `StationResultBuilder` / `StationDetailBuilder`
Dateien: `src/Tankradar.MAUI/Services/Search/StationResultBuilder.cs`, `StationDetailBuilder.cs`

`StationResultBuilder.Build(stations, fuelTypes, filter, sortOrder, nowUtc)` liefert `IReadOnlyList<StationListItem>`; Preiszeilen nur für gewählte Sorten in der Einstellungs-Reihenfolge, Alter und `IsStale`, Hinweise über `StationHints`, Entfernung aus `StationInfo.DistanceKm`; `ResolveFuelType`. `StationDetailBuilder.Build` analog für die Detailansicht.

## `FuelPriceService`
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs`

Siehe `interfaces.md`; Cache-Lebensdauer `PriceApiOptions.CacheLifetime` (5 min), Offline über `IConnectionMonitor.IsOnline`, Fehler (`PriceApiException`) führen zum Fallback aus `PriceRepository`.

## `TankerkoenigClient`
Datei: `src/Tankradar.MAUI/Services/Pricing/TankerkoenigClient.cs`

`list.php`, `detail.php`; `TryParseStation` liest `e5`, `e10`, `diesel` (nur Zahlen > 0), `dist`, `isOpen`, `wholeDay`, `openingTimes`.

## `SettingsService` / `SettingsViewModel` / `SettingsPage`
Dateien: `src/Tankradar.MAUI/Services/SettingsService.cs`, `ViewModels/SettingsViewModel.cs`, `Views/SettingsPage.xaml`

Lädt/speichert Spritsorten (Tabelle `FuelTypeSettings`) und die Einzelwahlgruppen `GpsOptions`, `ViewOptions`, `SortOptions` als `ChoiceOptionViewModel<T>`; jede Änderung speichert sofort (`SaveAsync` mit `SemaphoreSlim`). Außer den Spritsorten-Schaltern (`FuelTypeItemViewModel`, AutomationId `Settings.FuelType.<Key>.Switch`) gibt es keinen Ein/Aus-Schalter. `SettingsTexts` hält die Überschriften.

## `TileCachePolicy` und `HttpTileSource`
Dateien: `src/Tankradar.MAUI/Services/Map/TileCachePolicy.cs`, `HttpTileSource.cs`

`TileCachePolicy.Evaluate(cacheControl, expires, serverDate, now, fallback)` liefert `TileFreshness(ExpiresUtc, NoStore)`: `no-store` verwirft sofort, `no-cache` läuft sofort ab, `max-age` gilt `now + maxAge`, `Expires` gilt `now + (Expires − Date)`, sonst `now + fallback` (`TileServerOptions.CacheLifetime`, Standard 7 Tage). Der Header `Age` wird nicht berücksichtigt. `HttpTileSource.DownloadAsync`: bei `304` ruft `EvaluateFreshness(response, now)` dieselbe Policy für die 304-Antwort auf; fehlen dort Caching-Angaben, gilt der Rückfall (`_options.CacheLifetime`) statt der zuvor gültigen Lebensdauer. `TileMetadata` kennt die ursprüngliche Lebensdauer nicht (nur `ExpiresUtc`). Kacheln ohne Metadaten gelten ab Dateizeit für die Rückfallfrist.

## `MauiProgram` und `AppShell`
Dateien: `src/Tankradar.MAUI/MauiProgram.cs`, `AppShell.xaml(.cs)`

Registriert `IFavoritesService` (Singleton), `FavoritesViewModel` (Transient), `IStationNavigator`, `IFavoriteGroupNavigator`, `ILocationService` (über `LocationServiceSelector`), Preisdienste. `AppShell` registriert die Routen `stationdetail`, `favoritegroup`; die Registerkarte „Favoriten“ (Route `favorites`) ist `FavoritesPage`.

## Texte
Dateien: `src/Tankradar.MAUI/Resources/Texts/FavoritesTexts.cs` (`NoGroups`, `OpenGroup`, …), `SearchTexts.cs` (`OfflineBanner`, `FormatDistance`, `FormatPrice`, `FormatAddress`, `GetFailureMessage`, `GetLocationMessage`), `SettingsTexts.cs`, `DetailTexts.cs`, `PriceTexts.cs`.

## Testunterstützung außerhalb der Testprojekte
`src/TestSupport/MockTankerkoenigServer.cs`: Endpunkte `list.php`, `detail.php` (Stationen Alpha, Beta, Gamma, Delta, Epsilon, generierte), Zähler `ListRequests`, `DetailRequests`, `TotalRequests`, erzwungene Statuscodes, `ResponseDelay`; kein `prices.php`. `MockTileServer` (inkl. `If-None-Match` mit `304`), `MockNominatimServer`, `TestDataPaths` (Umgebungsvariablen `TANKATLAS_TEST_DATA_PATH`, `TANKRADAR_PRICE_API_URL`, `TANKRADAR_PRICE_API_KEY`, `TANKRADAR_TILE_URL`, `TANKATLAS_TEST_LOCATION`, `TANKATLAS_TEST_WINDOW`).

## Dokumentation
`docs/help/Favoriten/` (Beschreibung, Abläufe), `docs/help/Einstellungen/`, `docs/help/Preisdaten/`, `docs/help/Tankstellendetails/`, `docs/help/Suche/`, `docs/help/index.md`; `docs/adr/0006-favorite-groups.md` (Abschnitt zur Gruppenübersicht und „Folgen“ verweisen auf Schritt 11); `docs/adr/0001-no-electricity-prices-in-v1.md`. `docs/features.md` existiert nicht.
