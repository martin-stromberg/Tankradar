# Enums

## `VehicleFuelKind`
Datei: `src/Tankradar.MAUI/Models/Tankbook/VehicleFuelKind.cs` – gespeichert als Name in `Vehicles.FuelKind`.

| Wert | Bedeutung |
|------|-----------|
| `Petrol` | Benzin (L/100 km) |
| `Diesel` | Diesel (L/100 km) |
| `Electric` | Elektro (kWh/100 km) |
| `Hybrid` | Hybrid (L/100 km laut `TankbookTexts.GetConsumptionUnit`) |

## `VehicleFieldError`
Datei: `src/Tankradar.MAUI/Models/Tankbook/VehicleFieldError.cs`

| Wert | Bedeutung |
|------|-----------|
| `None` | kein Fehler |
| `Required` | Pflichtfeld leer |
| `NotANumber` | keine gültige Zahl |
| `Negative` | negativ |
| `OutOfRange` | außerhalb des Bereichs |
| `TooManyDecimals` | zu viele Nachkommastellen |
| `TooLong` | Text zu lang |

## `VehicleResult`
Datei: `src/Tankradar.MAUI/Models/Tankbook/VehicleResult.cs`

| Wert | Bedeutung |
|------|-----------|
| `Ok` | erfolgreich |
| `InvalidName` | Name leer/zu lang |
| `DuplicateName` | Name vergeben |
| `InvalidFuelKind` | unbekannte Kraftstoffart |
| `InvalidOdometer` | Start-Kilometerstand außerhalb 0–9.999.999 |
| `InvalidConsumption` | Startverbrauch ungültig |
| `VehicleNotFound` | Fahrzeug existiert nicht (mehr) |

Für Vorgänge (Art Kraftstoff/Strom, Ergebnis) gibt es keine Enums. `FuelType` (Spritsorten der Preisquelle) ist laut
ADR 0008 ein anderer Persistenzvertrag. Die Pre-Push-Prüfung `enum-coverage-check.py --strict` prüft die Abdeckung von Enums.
