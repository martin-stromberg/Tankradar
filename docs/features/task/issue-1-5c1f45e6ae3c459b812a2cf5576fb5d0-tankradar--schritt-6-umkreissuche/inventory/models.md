## `StationInfo`
Datei: `src/Tankradar.MAUI/Models/Pricing/StationInfo.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id`, `Name`, `Brand` | `string` / `string?` | Kennung (UUID), Name, Marke |
| `Street`, `HouseNumber`, `PostCode`, `Place` | `string?` | Adresse, nur wenn geliefert |
| `Latitude`, `Longitude` | `double` | Position der Tankstelle |
| `DistanceKm` | `double?` | Entfernung zur Suchposition; nur bei Umkreissuche, nie gespeichert |
| `IsOpen`, `WholeDay` | `bool?` | Öffnungsstatus, durchgehend geöffnet |
| `OpeningTimes` | `IReadOnlyList<OpeningTimeEntry>` | Öffnungszeiten |
| `Prices` | `IReadOnlyList<FuelPrice>` | Zuletzt bekannte Preise je Sorte |
| `DetailsUpdatedUtc` | `DateTime?` | Zeitpunkt der letzten Detailabfrage |

Methode `With(IReadOnlyList<FuelPrice>)` liefert eine Kopie mit anderen Preisen.

## `FuelPrice` (Record)
Datei: `src/Tankradar.MAUI/Models/Pricing/FuelPrice.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `FuelType` | `FuelType` | Sorte |
| `Price` | `decimal` | Preis in Euro je Liter |
| `RetrievedUtc` | `DateTime` | Abrufzeitpunkt, Grundlage der Aktualität |

## `StationSearchQuery`, `StationSearchResult`, `StationDetailResult` (Records)
Datei: `src/Tankradar.MAUI/Models/Pricing/StationQueryResults.cs`

| Record | Eigenschaften |
|--------|---------------|
| `StationSearchQuery` | `Latitude`, `Longitude`, `RadiusKm` (int), `FuelTypes` |
| `StationSearchResult` | `Stations`, `Source` (`PriceDataSource`), `Failure` (`PriceFailure`) |
| `StationDetailResult` | `Station`, `Source`, `Failure` |

## `AppSettings` (Record)
Datei: `src/Tankradar.MAUI/Models/AppSettings.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `FuelTypes` | `IReadOnlyList<FuelTypeSelection>` | Sorten in Prioritätsreihenfolge mit Auswahl |
| `GpsUsage` | `GpsUsage` | Standortnutzung |
| `ResultView` | `ResultView` | Standardansicht |
| `ResultSortOrder` | `ResultSortOrder` | Standardsortierung |

Standardwerte: alle Sorten ausgewählt, `WhileInUse`, `List`, `Price`. Es gibt keine Radius-Einstellung. `UserSettingsEntity` (`src/Tankradar.MAUI/Data/UserSettingsEntity.cs`) speichert `GpsUsage`, `ResultView`, `ResultSortOrder` als Namen; vorhandene Migrationen: `InitialCreate`, `AddFuelTypeSettings`, `AddPriceCache`.

## `PriceApiOptions`
Datei: `src/Tankradar.MAUI/Services/Pricing/PriceApiOptions.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `MaxRadiusKm` | `int` | 25 (Obergrenze der Quelle) |
| `CacheLifetime` | `TimeSpan` | 5 Minuten |
| `BaseUrl`, `EndpointNotConfigured`, `AllowLoopbackHttp` | | Endpunkt/Testmodus |
