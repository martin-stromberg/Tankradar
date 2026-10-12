# Enums

## `VehicleFuelKind`
Datei: `src/Tankradar.MAUI/Models/Tankbook/VehicleFuelKind.cs` (Namen sind Persistenzvertrag, Spalte `Vehicles.FuelKind`)

| Wert | Bedeutung |
|------|-----------|
| `Petrol` | Benzin |
| `Diesel` | Diesel |
| `Electric` | Elektro; Verbrauch in kWh/100 km |
| `Hybrid` | Hybrid; Verbrauch wie Benzin in L/100 km, Vorgänge beider Arten erlaubt |

Die Einheit des Startverbrauchs liefert `TankbookTexts.GetConsumptionUnit(VehicleFuelKind)` (nur `Electric` ergibt `kWh/100 km`, sonst `L/100 km`).

## `TankbookEntryKind`
Datei: `src/Tankradar.MAUI/Models/Tankbook/TankbookEntryKind.cs` (Persistenzvertrag, Spalte `TankbookEntries.Kind`)

| Wert | Bedeutung |
|------|-----------|
| `Fuel` | Kraftstoff (L, L/100 km, €/L) |
| `Electricity` | Strom (kWh, kWh/100 km, €/kWh) |

## `TankbookConsumptionState`
Datei: `src/Tankradar.MAUI/Models/Tankbook/TankbookConsumptionState.cs`

| Wert | Bedeutung |
|------|-----------|
| `Calculated` | Verbrauch berechnet |
| `Partial` | Teilbetankung, kein eigener Wert |
| `NoPreviousFullEntry` | Erster Vollvorgang der Art |
| `NoDistance` | Strecke <= 0 seit dem vorherigen Vollvorgang |

## `TankbookEntryResult`
Datei: `src/Tankradar.MAUI/Models/Tankbook/TankbookEntryResult.cs`

Werte: `Ok`, `VehicleNotFound`, `EntryNotFound`, `InvalidOccurredAt`, `KindNotAllowed`, `InvalidQuantity`, `InvalidUnitPrice`, `InvalidTotalAmount`, `AmountMismatch`, `InvalidOdometer`, `OdometerBelowVehicleStart`, `OdometerBelowPrevious`, `OdometerAboveNext`, `InvalidStationName`, `NoteTooLong`.

Der Löschfehler-Zweig in `TankbookEntriesViewModel.DeleteAsync` (Ergebnis weder `Ok` noch `EntryNotFound`) zeigt `TankbookEntryTexts.GetResultMessage(result, null)`; für Werte ohne eigenen Text liefert dieser `TankbookTexts.SaveFailed`.

Es gibt keinen Enum für Statistik-Zustände, Trends oder Zeiträume.
