# Enums (Bestand)

## `SearchMode`
Datei: `src/Tankradar.MAUI/Models/Search/SearchMode.cs`

| Wert | Bedeutung |
|------|-----------|
| `CurrentLocation` | Suche am aktuellen Standort |
| `Address` | Suche rund um eingegebene Adresse, Ort oder PLZ |

Verwendet von `MapViewModel` (`ModeOptions`, `OnModeSelected`), `SearchTexts.GetModeLabel`, `ChoiceOptionViewModel<SearchMode>`.

## `GpsUsage`
Datei: `src/Tankradar.MAUI/Models/GpsUsage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Always` | Standort immer erlaubt |
| `WhileInUse` | nur bei Nutzung der App |
| `Never` | nie |

Erweiterung `GpsUsageExtensions.AllowsLocation` (nur `Always`/`WhileInUse` erlauben; undefinierte Werte sperren). Die Namen sind Persistenzvertrag (`SettingsEnumTests_Persistence`).

## `ResultSortOrder`
Datei: `src/Tankradar.MAUI/Models/ResultSortOrder.cs`

Werte `Price`, `Distance`, `Name` (Persistenzvertrag). `StationResultBuilder` sortiert `Distance` nach `StationListItem.DistanceKm`.

## `ResultView`
Datei: `src/Tankradar.MAUI/Models/ResultView.cs`: Liste oder Karte.

## `PriceFailure`
Datei: `src/Tankradar.MAUI/Models/Pricing/PriceFailure.cs`

`None`, `Offline`, `Unreachable`, `ApiKeyMissing`, `Rejected`, `InvalidResponse`, `EndpointNotConfigured`.

## `GeocodingStatus`, `AddressInputError`
Dateien: `Services/Geocoding/GeocodingResult.cs`, `Services/Geocoding/AddressInput.cs`

- `GeocodingStatus`: `Found`, `NotFound`, `InvalidInput`, `Unavailable` (auch 429, 5xx, Zeitlimit), `Rejected`, `InvalidResponse`, `EndpointNotConfigured`.
- `AddressInputError`: `None`, `Empty`, `TooShort`, `TooLong`, `InvalidCharacters`.

## `LocationStatus`
Datei: `Models/Search/LocationStatus.cs` (u. a. `Available`, `Unavailable` sowie Status für Berechtigung bzw. Einstellung).

## `MapOriginKind`, `PriceLevel`
`MapOriginKind`: `CurrentLocation`, `SearchedPlace`. `PriceLevel` wird von `PriceLevelClassifier` aus Preisen abgeleitet (Farben in Liste und Karte).

Hinweis: Der Hook `.githooks/enum-coverage-check.py` verlangt Testabdeckung für Enum-Werte.
