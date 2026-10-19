# Datenmodell

## `TankbookEntry` (Record)
Datei: `src/Tankradar.MAUI/Models/Tankbook/TankbookEntry.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Id`, `VehicleId` | `long` | Kennungen. |
| `OccurredAt` | `DateTime` | Lokale Wanduhrzeit ohne Zeitzone, minutengenau (ADR 0009). |
| `Kind` | `TankbookEntryKind` | `Fuel` (Liter) oder `Electricity` (kWh). |
| `Quantity` | `decimal` | Menge in Litern bzw. kWh. |
| `UnitPrice` | `decimal` | Preis pro Einheit (3 Nachkommastellen). |
| `TotalAmount` | `decimal` | Gesamtbetrag in Euro. |
| `OdometerKm` | `int` | Kilometerstand. |
| `IsPartial` | `bool` | Teilbetankung bzw. Teilladung. |
| `StationName` | `string` | Anzeigename als Momentaufnahme (ADR 0009/0012), 1 bis 60 Zeichen. |
| `StationId` | `string?` | Kennung der Favoriten-Tankstelle, ohne Fremdschlüssel. |
| `Note` | `string?` | Notiz. |
| `ReceiptPhotoFileName` | `string?` | Dateiname des Belegfotos. |

## `TankbookEntryOverview` (Record)
Datei: `src/Tankradar.MAUI/Models/Tankbook/TankbookEntryOverview.cs`

| Eigenschaft | Typ | Beschreibung / Zweck |
|-------------|-----|----------------------|
| `Entry` | `TankbookEntry` | Der Vorgang. |
| `DistanceKm` | `int?` | Strecke seit dem vorherigen Vollvorgang der Art. |
| `Consumption` | `decimal?` | Verbrauch je 100 km. |
| `ConsumptionState` | `TankbookConsumptionState` | Grund, falls kein Verbrauch vorliegt. |
| `IntervalQuantity`, `IntervalCost` | `decimal?` | Menge bzw. Kosten des Intervalls (nur bei berechnetem Verbrauch). |
| `IsFromStart` | `bool` | Intervall ab Start-Kilometerstand (Annahme: Tank beim Start voll). |

## `Vehicle` (Record)
Datei: `src/Tankradar.MAUI/Models/Tankbook/Vehicle.cs` – `Id`, `Name`, `FuelKind` (`VehicleFuelKind`), `StartOdometerKm`, `StartConsumption`.

## `TankbookStatistics` / `TankbookKindStatistics` / `MonthlyCostSummary` / `TrendPoint`
Dateien: `Models/Tankbook/TankbookStatistics.cs`, `TankbookKindStatistics.cs`, `MonthlyCostSummary.cs`, `TrendPoint.cs`

| Typ | Eigenschaften |
|-----|---------------|
| `TankbookStatistics` | `VehicleId`, `Kinds` (je Art ein Block), `HasEntries`. |
| `TankbookKindStatistics` | `Kind`, `AverageConsumption`, `AverageSource` (`TankbookAverageSource`), `CostPer100Km`, `OffersStartConsumption`, `CalculatedIntervals`, `TotalDistanceKm`, `TotalCost`, `Months`, `ConsumptionTrend`, `IsStartKind`. |
| `MonthlyCostSummary` | `Year`, `Month`, `TotalCost`, `TotalQuantity`, `EntryCount` (neuester Monat zuerst, lückenlos). |
| `TrendPoint` | `OccurredAt`, `Value`, `IsFromStart`. |

## `StationEntity` (Datenbank)
Datei: `src/Tankradar.MAUI/Data/StationEntity.cs` – u. a. `Name`, `Brand`, `CustomName` (neu seit 15a, höchstens 60 Zeichen). `StationNaming` (`Models/Pricing/StationNaming.cs`) leitet den Anzeigenamen ab (siehe logic.md).

## Nicht vorhanden
Es gibt keine Modelle für Export, Exportformat, Exportzeitraum oder Exportergebnis. `TankbookEntryLimits`, `TankbookStatisticsLimits` begrenzen Eingaben bzw. die Anzeige (Monatsliste 24 Zeilen, Diagramm 6/12 Punkte); diese Anzeigegrenzen gelten nicht automatisch für einen Export.
