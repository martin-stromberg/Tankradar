# Interfaces

## `ITankbookEntryService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/ITankbookEntryService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetEntriesAsync` | `long vehicleId`, `CancellationToken` | `Task<IReadOnlyList<TankbookEntryOverview>>` | Alle Vorgänge des Fahrzeugs, neueste zuerst, mit berechnetem Verbrauch (nutzt `ConsumptionCalculator` mit Startstand). Kein Zeitraumfilter. |
| `CreateEntryAsync`, `UpdateEntryAsync`, `DeleteEntryAsync` | siehe Datei | Ergebnis-Typen | Erfassen, Korrigieren, Löschen. |
| `GetKnownStationsAsync` | `CancellationToken` | `Task<IReadOnlyList<KnownStation>>` | Bekannte Tankstellen (Anzeigename nach ADR 0012). |

## `ITankbookStatisticsService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/ITankbookStatisticsService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `GetStatisticsAsync` | `long vehicleId`, `CancellationToken` | `Task<TankbookStatisticsLoad>` | Kennzahlen über alle Vorgänge (`Ok`/`VehicleNotFound`); kein Zeitraum. |

## `IVehicleService`
Datei: `src/Tankradar.MAUI/Services/Tankbook/IVehicleService.cs` – u. a. `GetVehicleAsync(long, CancellationToken)`.

## `IBackupSharer`
Datei: `src/Tankradar.MAUI/Services/Backup/IBackupSharer.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ShareAsync` | `string fullPath`, `string title`, `CancellationToken` | `Task` | Übergibt eine Datei an die Systemfunktion Teilen/Speichern. |

Implementierungen: `MauiBackupSharer` (`Share.Default.RequestAsync(new ShareFileRequest(title, new ShareFile(fullPath)))`), `TestBackupSharer` (kopiert nach `<TANKATLAS_TEST_DATA_PATH>/test-shared`), Auswahl über `BackupSharerSelector.Create(getEnvironmentVariable, createPlatformSharer)`. Der Name und die Sicherungsbezogenheit (`Models.Backup.BackupFormat.TestSharedDirectoryName`) machen eine direkte Wiederverwendung für Exporte unschön; ein Export-Gegenstück existiert nicht.

## `IBackupService`
Datei: `src/Tankradar.MAUI/Services/Backup/IBackupService.cs` – Sicherung erstellen/auflisten/löschen/einspielen/wiederherstellen. Ein Ereignis „Daten wiederhergestellt“ für andere Seiten gibt es nicht (nur `BackupViewModel.Restored`, siehe logic.md).

## `IAppDataPathProvider`
Datei: `src/Tankradar.MAUI/Services/IAppDataPathProvider.cs` – `GetDataDirectory()`, `GetCacheDirectory()`; Testmodus per `TANKATLAS_TEST_DATA_PATH`.

## Nicht vorhanden
Kein Export-Dienst, kein Export-Sharer, keine Abstraktion der Dateiausgabe.
