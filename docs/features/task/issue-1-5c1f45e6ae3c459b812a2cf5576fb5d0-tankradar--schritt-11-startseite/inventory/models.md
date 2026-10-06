# Datenmodell

## `AppSettings`
Datei: `src/Tankradar.MAUI/Models/AppSettings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `FuelTypes` | `IReadOnlyList<FuelTypeSelection>` | Spritsorten in der gewählten Reihenfolge mit Auswahl (mindestens eine gewählt, geprüft in `Validate`) |
| `GpsUsage` | `GpsUsage` | Standortnutzung (`Always`, `WhileInUse`, `Never`) |
| `ResultView` | `ResultView` | Standardansicht der Suchergebnisse |
| `ResultSortOrder` | `ResultSortOrder` | Standardsortierung |

Positionaler Konstruktor mit vier Parametern; `CreateDefault()` (alle Sorten gewählt, `WhileInUse`, `List`, `Price`), `Normalize()`, `Validate()`. Eine Einstellung zum Bereich „In der Nähe“ gibt es nicht. Der Konstruktor wird in `SettingsService.LoadAsync`, `SettingsViewModel.SaveAsync` und `AppSettings.CreateDefault` sowie in Testhilfen aufgerufen.

## `UserSettingsEntity`
Datei: `src/Tankradar.MAUI/Data/UserSettingsEntity.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `int` | Singleton-Schlüssel (`SingletonId` = 1) |
| `GpsUsage` | `string` | Enum-Name |
| `ResultView` | `string` | Enum-Name |
| `ResultSortOrder` | `string` | Enum-Name |

`Update(AppSettings)` schreibt die drei Werte. Keine Spalte für „In der Nähe“. Migrationen: `InitialCreate`, `AddFuelTypeSettings`, `AddPriceCache`, `AddFavorites` (Verzeichnis `src/Tankradar.MAUI/Data/Migrations/`, jüngste `20261006080459_AddFavorites`); `TankradarDbContextModelSnapshot` muss mit dem Modell übereinstimmen (`MigrationsTests_ModelSync`).

## `StationEntity` / `PriceEntryEntity`
Dateien: `src/Tankradar.MAUI/Data/StationEntity.cs`, `PriceEntryEntity.cs`

`StationEntity`: `Id` (UUID als Text), `Name`, `Brand`, `Street`, `HouseNumber`, `PostCode`, `Place`, `Latitude`, `Longitude` (Position ist lokal gespeichert), `WholeDay`, `OpeningTimesJson`, `DetailsUpdatedUtc`, `PriceEntries`. `PriceEntryEntity`: `StationId`, `FuelTypeKey`, `Price`, `RetrievedUtc` (jeder Abruf erzeugt eine neue Zeile; `PriceRepository` liest je Sorte die jüngste).

## `FavoriteGroupEntity` / `FavoriteEntryEntity`
Dateien: `src/Tankradar.MAUI/Data/FavoriteGroupEntity.cs`, `FavoriteEntryEntity.cs`

`FavoriteGroupEntity`: `Id`, `Name` (eindeutig, `NOCASE`), `Description`, `CreatedUtc`, `Entries`. `FavoriteEntryEntity`: `Id`, `GroupId`, `StationId`, `Note`, `Priority` (Enum-Name), `AddedUtc`, `Group`, `Station`; eindeutiger Index je (`GroupId`, `StationId`); Löschen einer Gruppe kaskadiert, `Station` mit `Restrict`.

## Favoriten-Modelle
Dateien: `src/Tankradar.MAUI/Models/Favorites/`

| Typ | Eigenschaften | Zweck |
|-----|---------------|-------|
| `FavoriteGroup` (record) | `Id`, `Name`, `Description`, `StationCount` | Gruppe mit Anzahl |
| `FavoriteEntry` (record) | `GroupId`, `StationId`, `StationName`, `AddressText`, `Note`, `Priority` | Tankstelle in einer Gruppe; **ohne Preise, Position oder Entfernung** |
| `FavoriteGroupResult` (record) | `Result`, `Group` | Ergebnis mit angelegter Gruppe |
| `FavoriteLimits` | `MaxNameLength` 40, `MaxDescriptionLength` 200, `MaxNoteLength` 500 | Grenzen |

## Preis- und Tankstellenmodelle
Dateien: `src/Tankradar.MAUI/Models/Pricing/`

| Typ | Eigenschaften / Mitglieder | Zweck |
|-----|----------------------------|-------|
| `StationInfo` | `Id`, `Name`, `Brand`, Adressfelder, `Latitude`, `Longitude`, `DistanceKm`, `IsOpen`, `WholeDay`, `OpeningTimes`, `Prices`, `DetailsUpdatedUtc`; `WithDetails`, `WithoutStaleDetails`, `With(prices)` | Tankstelle mit Preisen aus Quelle oder Cache |
| `FuelPrice` (record) | `FuelType`, `Price`, `RetrievedUtc` | Preis mit Abrufzeit |
| `StationSearchQuery` / `StationSearchResult` / `StationDetailResult` | Anfrage bzw. Ergebnis mit `Source` (`PriceDataSource`) und `Failure` (`PriceFailure`) | Ergebnisse von `IFuelPriceService`; Abruf nach Kennungen für mehrere Tankstellen existiert nicht |
| `PriceFreshness` | `StaleThreshold` 60 min, `GetAge`, `IsStale`, `FormatAge` („vor X Min.“) | Preisalter |
| `StationHints` | `IsAutomatedStation`, `HasUnconfirmedPrice` | Hinweise |
| `DetailFreshness` | `MaxAge` 24 h | Alter der Öffnungszeiten |

## Such-/Anzeigemodelle
Dateien: `src/Tankradar.MAUI/Models/Search/`

| Typ | Eigenschaften | Zweck |
|-----|---------------|-------|
| `StationListItem` (record) | `Id`, `Name`, `DistanceKm`, `DistanceText`, `PriceLines`, `HasUnconfirmedPrice`, `IsAutomatedStation`, `OpeningStatusText`, `AddressText`, `Latitude`, `Longitude`, `IsOpen`; `HasDistance`, `HasHints` … | Zeile der Ergebnisliste und Navigationsparameter der Detailansicht (`ShellStationNavigator.StationParameter`) |
| `StationPriceLine` (record) | `FuelType`, `FuelLabel`, `Price`, `PriceText`, `AgeText`, `IsStale`, `Age`, `PriceMainText`, `PriceFractionText`, … | Preiszeile mit Alter und Amber-Kennzeichen |
| `GeoPosition`, `LocationResult`, `LocationStatus` | Position und Status der Standortermittlung | Standort |
| `SearchRadius` | `Min` 1, `Max` 25, `Default` 5, `Steps` [1, 2, 5, 10, 15, 25] | Radius |

## `TileMetadata`
Datei: `src/Tankradar.MAUI/Services/Map/HttpTileSource.cs`

Record `TileMetadata(ETag, LastModified, ExpiresUtc)`, als JSON in `<Kachel>.meta` gespeichert (`System.Text.Json`). Die bei der Speicherung gültige Lebensdauer ist nicht enthalten.
