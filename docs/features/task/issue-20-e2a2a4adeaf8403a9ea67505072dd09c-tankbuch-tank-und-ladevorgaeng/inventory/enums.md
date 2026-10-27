## `VehicleFuelKind`
Datei: `src/Tankradar.MAUI/Models/Tankbook/VehicleFuelKind.cs`

Die Namen der Werte sind Persistenzvertrag (Spalte `Vehicles.FuelKind`).

| Wert | Bedeutung |
|------|-----------|
| `Petrol` | Benzin (Verbrauch in L/100 km). |
| `Diesel` | Diesel (Verbrauch in L/100 km). |
| `Electric` | Elektro (Verbrauch in kWh/100 km). |
| `Hybrid` | Hybrid (Verbrauch wie Benzin in L/100 km). |

Verwendet von: `Vehicle.FuelKind`, `TankbookViewModel.FuelKindOptions`, `TankbookTexts.GetFuelKindLabel` (testabgedeckt durch `TankbookTextsTests_Labels.GetFuelKindLabel_AllValues`), `TankbookEntriesViewModel.LoadAsync` (Hybrid-Sonderfall).
