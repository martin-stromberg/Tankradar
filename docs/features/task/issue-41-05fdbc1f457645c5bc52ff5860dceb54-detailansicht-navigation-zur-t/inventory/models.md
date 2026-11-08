# Datenmodell

## `StationInfo`
Datei: `src/Tankradar.MAUI/Models/Pricing/StationInfo.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` (required) | Kennung der Tankstelle (UUID der Quelle) |
| `Name` | `string` (required) | Name der Tankstelle |
| `Brand` | `string?` | Marke, sofern geliefert |
| `CustomName` | `string?` | Eigener Anzeigename des Anwenders |
| `DisplayName` | `string` (berechnet) | Prominent angezeigter Name (eigener Name > Marke > Quellname) |
| `SubName` | `string` (berechnet) | Stammdatenname; leer bei Gleichheit mit `DisplayName` |
| `Street` | `string?` | Straße |
| `HouseNumber` | `string?` | Hausnummer |
| `PostCode` | `string?` | Postleitzahl |
| `Place` | `string?` | Ort |
| `Latitude` | `double` | Breitengrad |
| `Longitude` | `double` | Längengrad |
| `DistanceKm` | `double?` | Entfernung zur Suchposition |
| `IsOpen` | `bool?` | Öffnungsstatus laut Quelle |
| `WholeDay` | `bool?` | Durchgehend geöffnet laut Quelle |
| `OpeningTimes` | `IReadOnlyList<OpeningTimeEntry>` | Öffnungszeiten |
| `Prices` | `IReadOnlyList<FuelPrice>` | Zuletzt bekannte Preise |
| `DetailsUpdatedUtc` | `DateTime?` | Abrufzeitpunkt der Detailangaben |

Methoden: `WithDetails(StationInfo)`, `WithoutStaleDetails(DateTime)`, `WithDistance(double?)`, `With(IReadOnlyList<FuelPrice>)`, `WithCustomName(string?)` — alle liefern Kopien und übernehmen `Latitude`/`Longitude` unverändert.

Hinweis: `Latitude`/`Longitude` sind `double` ohne Gültigkeitsprüfung — eine „ungültige Position" ist hier nur als 0/0 oder Bereichsverletzung denkbar; die Quelle liefert praktisch immer Werte. `StationListItem` behandelt Position dagegen als optional (`double?`).

## `StationListItem`
Datei: `src/Tankradar.MAUI/Models/Search/StationListItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Kennung |
| `Name` | `string` | Anzeigename |
| `DistanceKm` | `double?` | Entfernung |
| `DistanceText` | `string` | Entfernung als Text |
| `PriceLines` | `IReadOnlyList<StationPriceLine>` | Preiszeilen |
| `HasUnconfirmedPrice` | `bool` | Hinweis „Preis unbestätigt" |
| `IsAutomatedStation` | `bool` | Hinweis „Automatentankstelle" |
| `OpeningStatusText` | `string` | Öffnungsstatus als Text |
| `AddressText` | `string` | Adresszeile; leer bei fehlender Adresse |
| `Latitude` | `double?` | Breitengrad; `null` bei ungültiger Position |
| `Longitude` | `double?` | Längengrad; `null` bei ungültiger Position |
| `IsOpen` | `bool?` | Öffnungsstatus |
| `SubName` | `string` | Stammdatenname |
| `RouteOffsetKm` | `double?` | Abstand zur Route (nur Routensuche) |
| `RouteOffsetText` | `string` | Abstand zur Route als Text |

Abgeleitet: `HasPosition` (`Latitude is not null && Longitude is not null`), `HasAddress`, `HasDistance`, `HasOpeningStatus`, `HasRouteOffset`, `HasSubName`, `HasHints`.

`StationListItem` wird als Navigationsparameter (`ShellStationNavigator.StationParameter`) an `StationDetailViewModel.ApplyQueryAttributes`/`Show` übergeben und ist damit die Quelle der Anfangsanzeige.

## `StationDetailItem`
Datei: `src/Tankradar.MAUI/Models/Search/StationDetailItem.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `string` | Kennung |
| `Name` | `string` | Anzeigename |
| `SubNameText` | `string` | Stammdatenname |
| `AddressText` | `string` | Adresszeile; leer bei fehlender Adresse |
| `DistanceText` | `string` | Entfernung als Text |
| `OpeningStatusText` | `string` | Öffnungsstatus als Text |
| `PriceLines` | `IReadOnlyList<StationPriceLine>` | Preiszeilen der aktivierten Sorten |
| `HasUnconfirmedPrice` | `bool` | Hinweis „Preis unbestätigt" |
| `IsAutomatedStation` | `bool` | Hinweis „Automatentankstelle" |
| `OpeningHours` | `IReadOnlyList<OpeningHoursLine>` | Öffnungszeiten |
| `OpeningHoursAgeText` | `string` | Alter der Öffnungszeiten |
| `CustomName` | `string` (optional, Default `""`) | Eigener Anzeigename |

Abgeleitet: `HasSubName`, `HasCustomName`, `HasAddress`, `HasDistance`, `HasOpeningStatus`, `HasPrices`, `HasOpeningStatusOrAutomat`, `HasInfoBox`, `HasStalePrice`, `HasLivePrices`, `PriceStatusText`, `HasOpeningHours`.

**Keine Positionsangaben vorhanden** — die Koordinaten müssten als `double? Latitude`/`double? Longitude` ergänzt werden (Muster wie `StationListItem`).
