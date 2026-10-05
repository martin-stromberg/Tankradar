## `GpsUsage`
Datei: `src/Tankradar.MAUI/Models/GpsUsage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Always` | Standort immer verwendbar |
| `WhileInUse` | nur während der Nutzung (Standard) |
| `Never` | nie |

## `ResultSortOrder`
Datei: `src/Tankradar.MAUI/Models/ResultSortOrder.cs`

| Wert | Bedeutung |
|------|-----------|
| `Price` | nach Preis (Standard) |
| `Distance` | nach Entfernung |
| `Name` | nach Name |

## `ResultView`
Datei: `src/Tankradar.MAUI/Models/ResultView.cs`

| Wert | Bedeutung |
|------|-----------|
| `List` | Liste (Standard) |
| `Map` | Karte (Kartenansicht existiert noch nicht) |

## `FuelType`
Datei: `src/Tankradar.MAUI/Models/FuelType.cs`

| Wert | Bedeutung |
|------|-----------|
| `SuperE5`, `SuperE10`, `Diesel` | Spritsorten |

## `PriceDataSource`
Datei: `src/Tankradar.MAUI/Models/Pricing/PriceDataSource.cs`

| Wert | Bedeutung |
|------|-----------|
| `Live` | frisch abgerufen |
| `Cache` | aus lokalem Cache, jung genug |
| `OfflineFallback` | zuletzt bekannte Werte |

## `PriceFailure`
Datei: `src/Tankradar.MAUI/Models/Pricing/PriceFailure.cs`

| Wert | Bedeutung |
|------|-----------|
| `None`, `Offline`, `Unreachable`, `ApiKeyMissing`, `Rejected`, `InvalidResponse`, `EndpointNotConfigured` | Grund eines nicht erfolgreichen Abrufs |
