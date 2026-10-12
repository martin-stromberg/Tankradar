# Interfaces

## `ITankbookEntryService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/ITankbookEntryService.cs` (Singleton, Umsetzung `TankbookEntryService`, registriert in `MauiProgram.cs`; konsumiert von `TankbookEntriesViewModel` und `TankbookEntryFormViewModel`)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetEntriesAsync` | `long vehicleId`, `CancellationToken` | `Task<IReadOnlyList<TankbookEntryOverview>>` | Alle Vorgänge eines Fahrzeugs, neueste zuerst, mit Strecke und Verbrauch aus `ConsumptionCalculator.Calculate(...).Reverse()`; bereinigt beim ersten Aufruf je Prozess verwaiste Belegfotos |
| `CreateEntryAsync` | `long vehicleId`, `TankbookEntryInput`, `CancellationToken` | `Task<TankbookEntrySaveResult>` | Anlegen |
| `UpdateEntryAsync` | `long entryId`, `TankbookEntryInput`, `CancellationToken` | `Task<TankbookEntrySaveResult>` | Ändern |
| `DeleteEntryAsync` | `long entryId`, `CancellationToken` | `Task<TankbookEntryResult>` | Löschen samt Foto |
| `GetKnownStationsAsync` | `CancellationToken` | `Task<IReadOnlyList<KnownStation>>` | Bekannte Tankstellen |

Es gibt keinen `ITankbookStatisticsService`.

## `IVehicleService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/IVehicleService.cs` (Singleton, `VehicleService`)

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetVehiclesAsync` | `CancellationToken` | `Task<IReadOnlyList<Vehicle>>` | Liste nach Namen |
| `GetVehicleAsync` | `long vehicleId`, `CancellationToken` | `Task<Vehicle?>` | Ein Fahrzeug (liefert `StartConsumption`) |
| `CreateVehicleAsync` | `VehicleInput`, `CancellationToken` | `Task<VehicleSaveResult>` | Anlegen |
| `UpdateVehicleAsync` | `long`, `VehicleInput`, `CancellationToken` | `Task<VehicleResult>` | Ändern |
| `CountEntriesAsync` | `long vehicleId`, `CancellationToken` | `Task<int>` | Zahl der Vorgänge |
| `DeleteVehicleAsync` | `long`, `int expectedEntryCount`, `CancellationToken` | `Task<VehicleDeleteResult>` | Löschen mit Neuzählung |

## `ITankbookNavigator`
Datei: `src/Tankradar.MAUI/Services/Navigation/ITankbookNavigator.cs` (Umsetzung `ShellTankbookNavigator`; Route `ShellTankbookNavigator.EntriesRoute` = `tankbookentries`, Parameter `ShellTankbookNavigator.VehicleIdParameter` = `vehicleId`; Route registriert in `AppShell.xaml.cs` für `TankbookEntriesPage`). `TankbookEntriesViewModel` nutzt `CloseAsync`.

## Weitere beteiligte Interfaces
`IReceiptPhotoStore`, `IReceiptPhotoPicker` (Belegfotos), `IScreenReaderAnnouncer` (Ansagen, `SemanticScreenReaderAnnouncer`), `IDatabaseInitializer`.
