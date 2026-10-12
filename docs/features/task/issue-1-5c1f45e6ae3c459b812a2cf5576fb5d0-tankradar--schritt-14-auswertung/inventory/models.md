# Datenmodell

Alle Modelle liegen unter `src/Tankradar.MAUI/Models/Tankbook/`. Es gibt noch kein Auswertungsmodell (keine Typen für Kennzahlen, Monatswerte oder Verlaufspunkte); `TankbookStatistics`, `MonthlyCostSummary` und `TrendPoint` aus der Anforderung existieren nicht.

## `Vehicle`
Datei: `src/Tankradar.MAUI/Models/Tankbook/Vehicle.cs` (`sealed record`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Kennung |
| `Name` | `string` | Fahrzeugname |
| `FuelKind` | `VehicleFuelKind` | Kraftstoffart |
| `StartOdometerKm` | `int` | Start-Kilometerstand |
| `StartConsumption` | `decimal?` | Optionaler Startverbrauch pro 100 km (L bzw. kWh bei Elektro); `null` ohne Angabe. Bereits vorhanden (Schritt 12). Eingabe wird auf > 0 und < 100 mit höchstens 2 Nachkommastellen validiert (`VehicleValidator`, `VehicleInputParser`). Wird bisher nur in `TankbookTexts.FormatVehicleDetails` (Anzeige „Startverbrauch …“) und im Fahrzeugformular verwendet, nicht in Berechnung oder Auswertung. |

## `VehicleEntity`
Datei: `src/Tankradar.MAUI/Data/VehicleEntity.cs` (EF-Entität, Tabelle `Vehicles`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Primärschlüssel |
| `Name` | `string` | Name |
| `FuelKind` | `string` | Name des `VehicleFuelKind`-Werts (Persistenzvertrag) |
| `StartOdometerKm` | `int` | Start-Kilometerstand |
| `StartConsumption` | `decimal?` | Startverbrauch (Spalte seit Migration `AddVehicles`) |
| `CreatedUtc` | `DateTime` | Anlagezeitpunkt |
| `Entries` | `List<TankbookEntryEntity>` | Navigation, Kaskade beim Löschen |

## `TankbookEntry`
Datei: `src/Tankradar.MAUI/Models/Tankbook/TankbookEntry.cs` (`sealed record`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id` | `long` | Kennung |
| `VehicleId` | `long` | Fahrzeug |
| `OccurredAt` | `DateTime` | Lokale Wanduhrzeit, minutengenau, ohne Zeitzone (ADR 0009); Monatszuordnung daher ohne Umrechnung möglich |
| `Kind` | `TankbookEntryKind` | Kraftstoff oder Strom |
| `Quantity` | `decimal` | Menge in L bzw. kWh |
| `UnitPrice` | `decimal` | Preis pro Einheit in Euro |
| `TotalAmount` | `decimal` | Gesamtbetrag in Euro (mögliche Kostenbasis) |
| `OdometerKm` | `int` | Kilometerstand |
| `IsPartial` | `bool` | Teilbetankung / Teilladung |
| `StationName` | `string` | Tankstelle (Momentaufnahme) |
| `StationId` | `string?` | Optionale Favoriten-Kennung |
| `Note` | `string?` | Notiz |
| `ReceiptPhotoFileName` | `string?` | Dateiname des Belegfotos |

## `TankbookEntryEntity`
Datei: `src/Tankradar.MAUI/Data/TankbookEntryEntity.cs` (EF-Entität, Tabelle `TankbookEntries`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id`, `VehicleId` | `long` | Schlüssel, Fahrzeug (Fremdschlüssel, Kaskade) |
| `CreatedUtc`, `OccurredAt` | `DateTime` | Anlage-/Vorgangszeitpunkt; Index (`VehicleId`, `OccurredAt`) |
| `Kind` | `string` | Name von `TankbookEntryKind` |
| `Quantity`, `UnitPrice`, `TotalAmount` | `decimal` | Mengen und Beträge |
| `OdometerKm` | `int` | Kilometerstand |
| `IsPartial` | `bool` | Teilbetankung |
| `StationName`, `StationId`, `Note`, `ReceiptPhotoFileName` | `string` / `string?` | wie im Record |
| `Vehicle` | `VehicleEntity?` | Navigation |
| `Apply(TankbookEntryInput)` | Methode | übernimmt Eingabewerte |

## `TankbookEntryOverview`
Datei: `src/Tankradar.MAUI/Models/Tankbook/TankbookEntryOverview.cs` (`sealed record`)

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Entry` | `TankbookEntry` | Der Vorgang |
| `DistanceKm` | `int?` | Strecke seit dem vorherigen Vollvorgang derselben Art; `null` bei Teilbetankung oder ohne Vorgänger; bei `NoDistance` der Wert <= 0 |
| `Consumption` | `decimal?` | Verbrauch pro 100 km (2 Nachkommastellen), nur bei `Calculated` |
| `ConsumptionState` | `TankbookConsumptionState` | Berechnungszustand |

## Weitere Tankbuch-Modelle (nur genannt)
`TankbookEntryInput`, `TankbookEntryLimits`, `TankbookEntryFieldError`, `TankbookEntryFormValidation`, `TankbookEntryResult`, `TankbookEntrySaveResult`, `KnownStation`, `AmountField`, `AmountCompletion`, `PickedReceiptPhoto`, `ReceiptPhotoError`, `ReceiptPhotoImportResult`, `VehicleInput`, `VehicleLimits`, `VehicleResult`, `VehicleSaveResult`, `VehicleDeleteResult`, `VehicleFieldError`, `VehicleFormValidation` (alle in `Models/Tankbook/`).
