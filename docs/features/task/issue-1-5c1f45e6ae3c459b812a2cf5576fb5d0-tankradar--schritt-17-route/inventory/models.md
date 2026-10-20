# Datenmodell (Bestand)

## `GeoPosition`
Datei: `src/Tankradar.MAUI/Models/Search/GeoPosition.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Latitude` / `Longitude` | `double` | Gültige Koordinaten; Erzeugung über Konstruktor oder `TryCreate` (Wertebereichsprüfung). Wird von Geokodierung, Standortdienst und Suche genutzt. |

## `StationSearchQuery` / `StationSearchResult`
Datei: `src/Tankradar.MAUI/Models/Pricing/StationQueryResults.cs`

| Typ | Felder | Zweck |
|-----|--------|-------|
| `StationSearchQuery` | `Latitude`, `Longitude`, `RadiusKm`, `FuelTypes` | Eine Umkreisabfrage (ein Mittelpunkt, ein Radius). |
| `StationSearchResult` | `Stations` (`StationInfo`-Liste), `Source` (`PriceDataSource`), `Failure` (`PriceFailure`) | Ergebnis von `IFuelPriceService.SearchNearbyAsync`. |

## `StationInfo`
Datei: `src/Tankradar.MAUI/Models/Pricing/StationInfo.cs`

Felder u. a. `Id`, `Name`, `Brand`, Adressteile, `Latitude`, `Longitude`, `DistanceKm` (Entfernung vom Suchmittelpunkt, von Tankerkönig geliefert), `IsOpen`, `WholeDay`, `OpeningTimes`, `Prices`, `DetailsUpdatedUtc`. Es gibt keine Eigenschaft für eine Position entlang einer Route oder einen Abstand zu einer Linie.

## `StationListItem`
Datei: `src/Tankradar.MAUI/Models/Search/StationListItem.cs`

Aufbereiteter Listeneintrag (Name, Unterzeile, Adresse, `DistanceKm`/`DistanceText`, Öffnungsstatus, Preiszeilen, Hinweise `HasUnconfirmedPrice`/`IsAutomatedStation`, `Latitude`/`Longitude`, `HasDistance` usw.). `DistanceKm` ist die einzige Entfernungsangabe; erzeugt von `StationResultBuilder`.

## `MapOrigin`, `MapOriginKind`, `MapMarker`
Dateien: `src/Tankradar.MAUI/Models/Map/MapOrigin.cs`, `MapMarker.cs`

| Typ | Inhalt |
|-----|--------|
| `MapOriginKind` | `CurrentLocation`, `SearchedPlace` |
| `MapOrigin` | `Kind`, `Latitude`, `Longitude` (ein einzelner Ursprungspunkt) |
| `MapMarker` | `Station`, `Latitude`, `Longitude`, `Level` (`PriceLevel`), `PriceText`, `Description` |

Es gibt kein Modell für eine Linie/Polylinie auf der Karte.

## `AppSettings`
Datei: `src/Tankradar.MAUI/Models/AppSettings.cs`

Eigenschaften: `FuelTypes`, `GpsUsage`, `ResultView`, `ResultSortOrder`, `ShowNearby`; `CreateDefault`, `Normalize`, `Validate`. Persistiert über `UserSettingsEntity` (`src/Tankradar.MAUI/Data/UserSettingsEntity.cs`). Kein Eintrag für Suchradius oder Routenabstand (der Radius lebt nur im `MapViewModel`).

## `GeocodingResult`
Datei: `src/Tankradar.MAUI/Services/Geocoding/GeocodingResult.cs`

`Status` (`GeocodingStatus`), `Position` (`GeoPosition?`), `PlaceName`; Fabriken `Success`, `Failure`.

## `LocationResult`
Datei: `src/Tankradar.MAUI/Models/Search/LocationResult.cs`

`Status` (`LocationStatus`), `Position`; Fabriken `Success`, `Failure`.

## Optionen
- `GeocodingOptions` (`Services/Geocoding/GeocodingOptions.cs`): `BaseUrl` (Standard `https://nominatim.openstreetmap.org/`), `EndpointNotConfigured`, `RequestTimeout` 10 s, `MinRequestInterval` 1100 ms (Validierung verlangt mindestens 1 s), `UserAgent`, `AllowLoopbackHttp`; `Validate`, `FromEnvironment`.
- `PriceApiOptions` (`Services/Pricing/PriceApiOptions.cs`): `BaseUrl` (Standard `https://creativecommons.tankerkoenig.de/json/`), `EndpointNotConfigured`, `RequestTimeout` 10 s, `MaxAttempts` 3, `RetryBaseDelay` 1 s, `MinRequestInterval` 1 s, `CacheLifetime` 5 min, `MaxRadiusKm` 25, `AllowLoopbackHttp`; `Validate`, `FromEnvironment`.
- `TileServerOptions` (`Services/Map/TileServerOptions.cs`): Kachelserver, analog aufgebaut.

## Datenbank und Migrationen
`src/Tankradar.MAUI/Data/Migrations/` enthält 8 Migrationen (jüngste `20261018094636_AddStationCustomName`), Snapshot `TankradarDbContextModelSnapshot.cs`. Für Routen, Adressen oder Standort existiert keine Tabelle; die Tests `SearchPrivacyTests_Persistence` und `AddressSearchPrivacyTests_Persistence` (Integration) belegen, dass Suchen nichts persistieren.
