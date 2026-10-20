# Logik (Bestand)

## `NominatimGeocodingService` (implementiert `IGeocodingService`)
Datei: `src/Tankradar.MAUI/Services/Geocoding/NominatimGeocodingService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `ResolveAsync(string?, ct)` | public | Validierung vor dem Aufruf (`AddressInput.Validate` -> `InvalidInput`), `EndpointNotConfigured`-Prüfung, `RequestThrottle.WaitAsync` (Mindestabstand 1100 ms), GET `search?format=jsonv2&limit=1&countrycodes=de&accept-language=de&q=...` mit Zeitlimit und User-Agent; 429/5xx/Zeitlimit/`HttpRequestException` -> `Unavailable`, andere Status -> `Rejected`; leeres Array -> `NotFound`. Keine Wiederholung. Protokolliert nur Typ/Status, nie den Suchbegriff. |
| `Parse` / `GetPlaceName` / `TryGetCoordinate` | private | JSON-Auswertung, Anzeigename max. 200 Zeichen ohne Steuerzeichen. |

Registrierung: `MauiProgram.AddGeocodingServices` (eigener `HttpClient`, `AllowAutoRedirect=false`, eigener `RequestThrottle` mit `GeocodingOptions.MinRequestInterval`). Der Throttle ist ein eigener Singleton je Dienst; eine zweite Auflösung (Start, Ziel) hintereinander wird dadurch automatisch um >= 1,1 s verzögert. `AddressInput`: Länge 3 bis 120, erlaubte Zeichen, Normalisierung typografischer Zeichen.

## `TankerkoenigClient` (implementiert `ITankerkoenigClient`)
Datei: `src/Tankradar.MAUI/Services/Pricing/TankerkoenigClient.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchAsync` | public | `list.php?lat&lng&rad&sort=dist&type=all`, Rückgabe `StationInfo` inkl. `DistanceKm` |
| `GetDetailAsync` / `GetPricesAsync` | public | `detail.php`, `prices.php` (max. `MaxPriceIds` = 10) |
| `GetAsync` | private | Gemeinsame HTTP-Logik: `EndpointNotConfigured` und fehlender Schlüssel (`ApiKeyMissing`) vor dem Aufruf; je Versuch `RequestThrottle.WaitAsync`, Zeitlimit, bis `MaxAttempts` (3) Versuche, Wartezeit `RetryBaseDelay * 2^(n-1)` über `IDelay`; 429 und >= 500 wiederholbar, übrige 4xx -> `Rejected`; Fehlertexte enthalten nie Adresse/Schlüssel; Schlüssel als Query-Parameter `apikey`. |

`PriceApiException(PriceFailure, ...)` transportiert Fehler. Der `RequestThrottle` der Preis-API (`MinRequestInterval` 1 s) ist ein eigener Singleton, getrennt vom Nominatim-Throttle.

## `FuelPriceService` (implementiert `IFuelPriceService`, `IPriceCacheInvalidator`)
Datei: `src/Tankradar.MAUI/Services/Pricing/FuelPriceService.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `SearchNearbyAsync` | public | Validiert die Anfrage (Radius <= `MaxRadiusKm` 25), prüft Such-Cache (Schlüssel aus Position/Radius, `CacheLifetime` 5 min; `PruneSearchCache`), Offline -> `SearchFallbackAsync` (zuletzt bekannte Stationen aus `IPriceRepository.FindNearbyAsync`), sonst `_client.SearchAsync`, Speichern (`TrySaveAsync`), Anreicherung mit bekannten Details und Anzeigenamen, Sortierung nach `DistanceKm`, Filter nach Sorten (`FilterByFuelTypes`). Fehler -> Rückfall auf Cache mit `Failure`. |
| `GetStationDetailAsync`, `RefreshStationsAsync`, `GetKnownStationsAsync`, `SetCustomNameAsync`, `CheckAvailabilityAsync`, `InvalidateCaches` | public | siehe Interface |

Jede Umkreisabfrage ist ein eigener `SearchNearbyAsync`-Aufruf; es gibt kein Zusammenführen mehrerer Abfragen. Der Cache zählt Abfragen nicht und begrenzt keine Anzahl pro Zeit; die einzige Drosselung ist der Mindestabstand von `RequestThrottle` (1 s). Gespeichert werden Stationen und Preisstände (Datenbank), nicht die Suchposition (laut `SearchPrivacyTests_Persistence`).

## `StationResultBuilder`
Datei: `src/Tankradar.MAUI/Services/Search/StationResultBuilder.cs`

| Methode | Sichtbarkeit | Kurzbeschreibung |
|---------|-------------|------------------|
| `Build(stations, fuelTypes, filter, sortOrder, nowUtc)` | public static | Filter nach Sorte, Aufbereitung (`CreateItem`), `Sort`: `Distance` nach `DistanceKm` (fehlend = Maximum), `Name` (de-DE, dann Entfernung), `Price` (Sortsorte, dann Entfernung) |
| `ResolveFuelType`, `BuildItem` | public static | maßgebliche Sorte, einzelner Eintrag |

Preise mit Altersangabe, Hinweise (`HasUnconfirmedPrice`, `IsAutomatedStation`) und Entfernungstext (`SearchTexts.FormatDistance`) entstehen hier; es gibt keine alternative Entfernungsquelle (z. B. Streckenposition).

## `MapViewModel`
Datei: `src/Tankradar.MAUI/ViewModels/MapViewModel.cs` (904 Zeilen)

Abhängigkeiten (Konstruktor): `ISettingsService`, `ILocationService`, `IGeocodingService`, `IFuelPriceService`, `IConnectionMonitor`, `TimeProvider`, `IStationNavigator`, `ILogger<MapViewModel>`.

| Member | Sichtbarkeit | Kurzbeschreibung |
|--------|-------------|------------------|
| `ModeOptions` | public | baut Chips aus `Enum.GetValues<SearchMode>()`; ein neuer Enum-Wert erscheint automatisch als Chip, benötigt aber `SearchTexts.GetModeLabel`, `OnModeSelected`-Logik und Sichtbarkeit der Eingabefelder (`IsAddressMode`). |
| `SearchAsync` | public | bricht laufende Suche ab, validiert Radius (`SearchRadius.IsValid`) und im Adressmodus `AddressInput.Validate` vor externen Aufrufen, setzt `IsBusy`, ruft `RunSearchAsync`; unerwartete Ausnahmen -> `SearchTexts.SearchFailed`, Protokoll nur mit Typ. |
| `RunSearchAsync` (privat) | private | lädt Einstellungen, bestimmt Position (`ResolveAddressAsync` oder `ResolveCurrentLocationAsync`), baut `StationSearchQuery` und `MapOrigin`, ruft `_priceService.SearchNearbyAsync`, `ApplyResult`. |
| `ResolveCurrentLocationAsync` | private | `ILocationService.GetCurrentLocationAsync(settings.GpsUsage, ct)`; Statusmeldungen über `SearchTexts.GetLocationMessage`. |
| `ResolveAddressAsync` | private | Offline-Prüfung (`SearchTexts.AddressOffline`), `IGeocodingService.ResolveAsync`, Meldungen über `SearchTexts.GetGeocodingMessage`; `ResolvedPlace` nur als Anzeigetext. |
| `ApplyResult`, `ApplyFilterAndSort`, `PublishVisible` | private/public | Ergebnis, Filter (`FuelFilterOptions`), Sortierung (`SortOptions`), Seitenweise Anzeige (`PageSize` = 25, `ShowMore`, `ShowMoreText`), `MapMarkers` (über `MapMarkerBuilder`), `Origin`, `MapResultVersion`. |
| `OpenStationCommand` / `OpenStationAsync` | public/private | öffnet über `IStationNavigator` die Detailansicht. |
| `AttributionText` | public | `SearchTexts.OsmAttribution` ("Geodaten © OpenStreetMap-Mitwirkende"); Kartenquelle zusätzlich `MapTexts.Attribution`/`AttributionUrl`. |

Radiuswahl nur 1 bis 25 km (`SearchRadius`, `RadiusOptions`). Keine Route-, Start-/Ziel-Eigenschaften.

## Karte
- `StationMapView` (`Views/Controls/StationMapView.xaml(.cs)`, `StationMapView.Pointer.cs`): bindbare Eigenschaften `Markers`, `Origin`, `ResultVersion`, `MarkerCommand`; `TileSource`. Zeichnet Kacheln, Marker, Ursprungsmarkierung; kein Zeichnen von Linien.
- `MapViewport`, `MapMarkerBuilder`, `MapMarkerSelection` (Begrenzung der Marker), `PriceLevelClassifier` (Preisniveau-Farben), `MapPointerTracker`, `HttpTileSource` (`TileCachePolicy`), `TileServerOptions` unter `Services/Map/`.
- Quellenangabe der Karte: `MapTexts.Attribution` mit Link `AttributionUrl`; Preisquelle `PriceTexts.Attribution` ("Daten: Tankerkönig / MTS-K").

## Standort
`MauiLocationService.GetCurrentLocationAsync(GpsUsage, ct)` prüft zuerst `GpsUsage.AllowsLocation()` (`Never` -> `LocationStatus.DisabledBySetting`-artiger Status), dann Berechtigung; `TestLocationService` liefert im Testmodus die Position aus `TANKATLAS_TEST_LOCATION` (`LocationServiceSelector.TryParseLocation`). Die Position wird nicht persistiert.

## Schlüsselablage und Build-Einbindung (Tankerkönig)
Der Tankerkönig-Schlüssel ist an mehreren Stellen fest verdrahtet. Für `ROUTING_API_KEY` existiert bereits Vorarbeit ausschließlich in CI-Dateien und Hook (siehe unten); in Code, MSBuild, csproj, Skripten und Laufzeit gibt es nichts. Werte wurden nicht gelesen.

| Stelle | Datei | Verhalten |
|--------|-------|-----------|
| MSBuild-Ziel | `src/Tankradar.MAUI/MSBuild/TankerkoenigApiKey.targets` | Inline-Task `WriteTankerkoenigKeyAttribute` (RoslynCodeTaskFactory): Quellen nacheinander `TANKRADAR_FUEL_PRICE_API_KEY`, `FUEL_PRICE_API_KEY`, `tankerkoenig.local.props` (Element `TankerkoenigApiKey`, Pfad `TankerkoenigLocalPropsFile`, Standard Repository-Root); schreibt `obj/.../TankerkoenigApiKey.g.cs` mit `[assembly: AssemblyMetadata("TankerkoenigApiKey", "...")]` (nicht-alphanumerische Zeichen als `\uXXXX`); Ziel `EmbedTankerkoenigApiKey` läuft vor `CoreCompile`; Log nur "vorhanden: ja/nein"; kein Import der props-Datei, keine MSBuild-Eigenschaft mit dem Wert. |
| csproj | `src/Tankradar.MAUI/Tankradar.MAUI.csproj` Zeile 112 | `<Import Project="MSBuild\TankerkoenigApiKey.targets" />`; iOS: `MtouchInterpreter` = `all` (Zeile 70), `TrimmerRootAssembly` für Tankradar.MAUI, EF Core, Sqlite (Zeilen 75 bis 79) - nicht ändern. |
| Laufzeit | `ApiKeys.cs` (`ApiKeyProvider`, `BuildKeyMetadataName = "TankerkoenigApiKey"`), `SecureStorageApiKeyStore` (iOS), `Platforms/Windows/CredentialLockerApiKeyStore`, `ApiKeyStoreSelector` | Testmodus (`TANKATLAS_TEST_DATA_PATH` gesetzt): Schlüssel nur aus `TANKRADAR_PRICE_API_KEY`, Ablage im Speicher; sonst Build-Schlüssel maßgeblich und in Ablage übernommen, fehlt er: gespeicherter Schlüssel; fehlt beides: `null` -> `PriceFailure.ApiKeyMissing`. |
| DI | `MauiProgram.AddPriceServices` (ca. Zeilen 166 bis 201) | `IApiKeyStore`, `IApiKeyProvider`, `PriceApiOptions`, `RequestThrottle`, `HttpClient`, `ITankerkoenigClient`, `IPriceRepository`, `IConnectionMonitor`, `IFuelPriceService`. |
| CI-Aktionen | `.github/actions/build-and-package/action.yml` (Eingabe `routing-api-key`, Zeilen 11/12; Zeilen 43 bis 45: `TANKRADAR_FUEL_PRICE_API_KEY` und `FUEL_PRICE_API_KEY` auf `''` gesetzt, `TANKRADAR_ROUTING_API_KEY: ${{ inputs.routing-api-key }}`), `.github/actions/package-ios/action.yml` (Eingaben `fuel-price-api-key` -> `TANKRADAR_FUEL_PRICE_API_KEY`, Zeile 217; `routing-api-key` Zeilen 66/67 -> `TANKRADAR_ROUTING_API_KEY`, Zeile 218) | Die Umgebungsvariable `TANKRADAR_ROUTING_API_KEY` wird bereits gesetzt, aber vom Build nicht gelesen (kein MSBuild-Ziel). Im Windows-Paket wird der Routing-Schlüssel (anders als der Tankerkönig-Schlüssel) nicht geleert. |
| Workflows | `.github/workflows/staging-ci.yml` (Zeilen 254 und 298: `routing-api-key: ${{ secrets.ROUTING_API_KEY }}`; Zeile 297: `fuel-price-api-key`), `.github/workflows/release.yml` (Zeilen 89 und 150 für `routing-api-key`; Zeile 149 `fuel-price-api-key`) | Das Secret `ROUTING_API_KEY` wird bereits referenziert (Windows-Paket und iOS). Weitere Workflows: `pr-staging-ci.yml`, `security-scan.yml`, `staging-to-main-promotion.yml`, `sync-staging-with-main.yml`, `verify-pr-source.yml` (ohne Schlüsselbezug). |
| Skripte | `scripts/package-windows.ps1` (Zeilen 52/53 entfernen nur die beiden Tankerkönig-Variablen aus der Umgebung; kein Bezug zu Routing), `scripts/local-ci.ps1`, `scripts/iOS-Deployment.ps1`, `scripts/create-review-version.ps1` | Windows-Pakete enthalten bewusst keinen Tankerkönig-Schlüssel (öffentliches Repository). |
| Ignorierte Datei | `.gitignore` Zeilen 483/484: `tankerkoenig.local.props`, `*.local.props` | Eine lokale Routing-Datei (z. B. `routing.local.props`) würde unter `*.local.props` fallen. Im Repo-Root liegt keine `*.props`-Datei. |
| Tests des Musters | `src/Tankradar.Tests.Integration/Integration/ApiKeyBuildTests_Sources.cs`, `ApiKeyBuildTests_Log.cs`, `Support/ApiKeyBuildRunner.cs` (bauen echt mit MSBuild und prüfen Quellenreihenfolge und Log); Unit: `ApiKeyProviderTests_Resolution`, `ApiKeyStoreSelectorTests_TestMode` | |
| Dokumentation | `README.md` (Zeilen 174/175), `docs/help/Preisdaten/api-schluessel.md`, `docs/help/ci-cd/einrichtung.md` (Zeilen 58, 69), `changes.log` | |

Hilfe und README nennen Reihenfolge und Variablen für Tankerkönig. `docs/help/ci-cd/einrichtung.md` Zeile 59 führt `ROUTING_API_KEY` als "optional, bis die App ihn nutzt"; `.githooks/forbidden-patterns-check.py` (Zeilen 7, 48, 49, 57) erkennt bereits Routing-/OpenRouteService-Schlüsselmuster (mit Test `test_forbidden_patterns_check.py`). README und `docs/help/Preisdaten/api-schluessel.md` erwähnen Routing nicht.

## Datenschutz-Muster in der Suche
- Adresse, Ortsname und Position werden nicht gespeichert und nicht protokolliert (ADR 0003; Tests `SearchPrivacyTests_Logging`, `SearchPrivacyTests_Persistence`, `AddressSearchPrivacyTests_Persistence`, `MapViewModelTests_Privacy`).
- Fehler werden nur mit Typ protokolliert, Ausnahmetexte nicht.
