## `ITankbookNavigator`
Datei: `src/Tankradar.MAUI/Services/Navigation/ITankbookNavigator.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `OpenEntriesAsync` | `long vehicleId` | `Task` | Öffnet die Vorgänge eines Fahrzeugs (Shell-Route `tankbookentries`). |
| `CloseAsync` | `long vehicleId` | `Task` | Schließt die Seite der Vorgänge dieses Fahrzeugs. |

Implementierung: `ShellTankbookNavigator`. Aufrufer: `TankbookViewModel`, `TankbookEntriesViewModel`.

## `IVehicleService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/IVehicleService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetVehiclesAsync` | `CancellationToken` (optional) | `Task<IReadOnlyList<Vehicle>>` | Alle Fahrzeuge, nach Namen sortiert (de-DE, ohne Groß-/Kleinschreibung). |
| `GetVehicleAsync` | `long vehicleId`, `CancellationToken` | `Task<Vehicle?>` | Ein Fahrzeug oder `null`. |
| `CreateVehicleAsync` | `VehicleInput input`, `CancellationToken` | `Task<VehicleSaveResult>` | Legt ein Fahrzeug an. |
| `UpdateVehicleAsync` | `long vehicleId`, `VehicleInput input`, `CancellationToken` | `Task<VehicleResult>` | Ändert Angaben (Start-km darf nicht über dem ersten Vorgang liegen). |
| `CountEntriesAsync` | `long vehicleId`, `CancellationToken` | `Task<int>` | Zahl der Vorgänge (0 bei unbekanntem Fahrzeug). |
| `DeleteVehicleAsync` | `long vehicleId`, `int expectedEntryCount`, `CancellationToken` | `Task<VehicleDeleteResult>` | Löscht Fahrzeug samt Vorgängen und Belegen; zählt in derselben Transaktion neu. |

Aufrufer: `TankbookViewModel`, `TankbookEntriesViewModel`. Die Auswahl „zuletzt genutzt“ ist im Dienst nicht vorhanden.

## `IScreenReaderAnnouncer`
Datei: `src/Tankradar.MAUI/Services/IScreenReaderAnnouncer.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `Announce` | `string text` | `void` | Sagt Text über die Sprachausgabe an; ohne aktive Sprachausgabe geschieht nichts. |

Implementierung: `SemanticScreenReaderAnnouncer` (`src/Tankradar.MAUI/Services/SemanticScreenReaderAnnouncer.cs`). Verwendet von `TankbookViewModel` und `TankbookEntriesViewModel`.

## `ISettingsService`
Datei: `src/Tankradar.MAUI/Services/ISettingsService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `LoadAsync` | `CancellationToken` (optional) | `Task<AppSettings>` | Lädt die Einstellungen aus `UserSettings`. |
| `SaveAsync` | `AppSettings settings`, `CancellationToken` (optional) | `Task` | Speichert die Einstellungen (legt die Zeile `Id = 1` an). |

Implementierung: `SettingsService` (`src/Tankradar.MAUI/Services/SettingsService.cs`). Der Dienst kennt keine Fahrzeugauswahl.

## `ISetupStatusService`
Datei: `src/Tankradar.MAUI/Services/ISetupStatusService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `IsSetupRequiredAsync` | `CancellationToken` (optional) | `Task<bool>` | `true`, solange keine `UserSettings`-Zeile existiert (Erststart). |

Implementierung: `SetupStatusService`. Aufrufer: `AppShell`.

## `IRestoreNotifier`
Datei: `src/Tankradar.MAUI/Services/Backup/IRestoreNotifier.cs`

Event `Restored` (`EventHandler?`). Meldet eine erfolgte Wiederherstellung aus einer Sicherung. Abonnenten: `TankbookViewModel`, `TankbookEntriesViewModel`, `BackupViewModel` (Umkehrung: `BackupViewModel` löst aus). Implementierung: `RestoreNotifier`.
