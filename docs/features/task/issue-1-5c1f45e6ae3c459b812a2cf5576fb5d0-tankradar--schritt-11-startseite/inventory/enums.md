# Enums

## `GpsUsage`
Datei: `src/Tankradar.MAUI/Models/GpsUsage.cs`

| Wert | Bedeutung |
|------|-----------|
| `Always` | Standort immer verwenden (`AllowsLocation()` = wahr) |
| `WhileInUse` | Nur bei Nutzung (Standard; `AllowsLocation()` = wahr) |
| `Never` | Standort nie verwenden (`AllowsLocation()` = falsch) |

## `FuelType`
Datei: `src/Tankradar.MAUI/Models/FuelType.cs`

| Wert | Bedeutung |
|------|-----------|
| `SuperE5` | Super E5 |
| `SuperE10` | Super E10 |
| `Diesel` | Diesel |

## `FavoriteResult`
Datei: `src/Tankradar.MAUI/Models/Favorites/FavoriteResult.cs`

| Wert | Bedeutung |
|------|-----------|
| `Ok` | gespeichert |
| `InvalidName` | Name leer oder zu lang |
| `InvalidText` | Beschreibung/Notiz zu lang |
| `DuplicateName` | Gruppenname bereits vergeben (auch Ergebnis der Abfangbehandlung jeder Unique-Verletzung in `FavoritesService.SaveAsync`) |
| `GroupNotFound` | Gruppe fehlt |
| `StationUnknown` | Tankstelle lokal nicht bekannt |
| `AlreadyMember` | Tankstelle gehört der Gruppe an (nur durch Vorabprüfung in `AddStationAsync`) |
| `NotMember` | Tankstelle gehört der Gruppe nicht an |

## `FavoritePriority`
Datei: `src/Tankradar.MAUI/Models/Favorites/FavoritePriority.cs`

Werte `None`, `Low`, `Medium`, `High` (deutsche Bezeichnungen Keine, Niedrig, Mittel, Hoch laut `FavoritesTexts.GetPriorityLabel`); Listen sind nach Priorität hoch zuerst geordnet.

## `PriceDataSource` und `PriceFailure`
Dateien: `src/Tankradar.MAUI/Models/Pricing/PriceDataSource.cs`, `PriceFailure.cs`

`PriceDataSource`: `Live`, `Cache`, `OfflineFallback`. `PriceFailure`: `None`, `Offline`, `Unreachable`, `ApiKeyMissing`, `Rejected`, `InvalidResponse`, `EndpointNotConfigured`.

## `LocationStatus`
Datei: `src/Tankradar.MAUI/Models/Search/LocationStatus.cs`

`Available`, `DisabledBySetting`, `PermissionDenied`, `Unavailable`.

## `ResultView` und `ResultSortOrder`
Dateien: `src/Tankradar.MAUI/Models/ResultView.cs`, `ResultSortOrder.cs`

`ResultView`: `List`, `Map`. `ResultSortOrder`: `Price`, `Distance`, `Name`. Alle Enums werden als Name in der Datenbank gespeichert; `SettingsEnumTests_Persistence` und der Git-Hook `enum-coverage-check.py` verlangen Testabdeckung je Wert.
