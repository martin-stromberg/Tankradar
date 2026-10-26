# Enums

## `FuelType`
Datei: `src/Tankradar.MAUI/Models/FuelType.cs`

| Wert | Bedeutung |
|------|-----------|
| `SuperE5` | Super E5 |
| `SuperE10` | Super E10 |
| `Diesel` | Diesel |

Die Namen sind ein Persistenzvertrag (Schlüssel in `FuelTypeSettings.FuelTypeKey`). Kein Elektro- oder Ladewert vorhanden. Tests: `SettingsEnumTests_Persistence` (`FuelType_HasStablePersistenceName`).

## `GpsUsage`
Datei: `src/Tankradar.MAUI/Models/GpsUsage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Always` | Standort immer nutzen |
| `WhileInUse` | Standort nur bei Nutzung der App (Standard) |
| `Never` | Standort nicht nutzen |

Gespeichert als Name in `UserSettings.GpsUsage`. Tests: `SettingsEnumTests_Persistence`.

## `ResultView`
Datei: `src/Tankradar.MAUI/Models/ResultView.cs`

| Wert | Bedeutung |
|------|-----------|
| `List` | Listenansicht (Standard) |
| `Map` | Kartenansicht |

Gespeichert als Name in `UserSettings.ResultView`. Tests: `SettingsEnumTests_Persistence`.

## `ResultSortOrder`
Datei: `src/Tankradar.MAUI/Models/ResultSortOrder.cs`

| Wert | Bedeutung |
|------|-----------|
| `Price` | nach Preis (Standard) |
| `Distance` | nach Entfernung |
| `Name` | nach Name |

Gespeichert als Name in `UserSettings.ResultSortOrder`. Tests: `SettingsEnumTests_Persistence`.

Neue Enums sind für die Anforderung nicht nötig. `VehicleFuelKind` (ADR 0008) ist ein eigener Persistenzvertrag und wird nicht verwendet.
