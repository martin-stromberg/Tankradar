# Enums

## `TankbookEntryKind`
Datei: `src/Tankradar.MAUI/Models/Tankbook/TankbookEntryKind.cs`

| Wert | Bedeutung |
|------|-----------|
| `Fuel` | Kraftstoff, Einheit Liter. |
| `Electricity` | Strom, Einheit kWh. |

## `VehicleFuelKind`
Datei: `src/Tankradar.MAUI/Models/Tankbook/VehicleFuelKind.cs` – `Petrol`, `Diesel`, `Electric`, `Hybrid` (Hybrid führt beide Arten).

## `TankbookConsumptionState`
Datei: `src/Tankradar.MAUI/Models/Tankbook/TankbookConsumptionState.cs` – `Calculated`, `Partial`, `NoPreviousFullEntry`, `NoDistance`.

## `TankbookAverageSource`
Datei: `src/Tankradar.MAUI/Models/Tankbook/TankbookAverageSource.cs` – Herkunft des Durchschnitts: `None`, `Calculated`, `FromStart`, `StartConsumption` (Werte nach Verwendung in `TankbookStatisticsCalculator`).

Hinweis: Ein Pre-Commit-Hook (`enum-coverage-check.py`) verlangt Tests für jeden Enum-Wert; neue Enums (Exportformat, Zeitraumart) brauchen vollständige Testabdeckung. Es gibt keinen Export-Enum.
