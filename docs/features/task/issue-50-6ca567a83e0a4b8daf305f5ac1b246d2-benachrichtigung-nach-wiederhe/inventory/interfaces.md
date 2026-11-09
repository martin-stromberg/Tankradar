# Interfaces

## `IRestoreNotifier`
Datei: `src/Tankradar.MAUI/Services/Backup/IRestoreNotifier.cs`

| Member | Parameter | Rückgabewert | Zweck |
|--------|-----------|--------------|-------|
| `Restored` (Ereignis) | — | `event EventHandler?` | Wird ausgelöst, nachdem die Daten erfolgreich wiederhergestellt wurden. Abonnenten werden schwach gehalten (siehe `RestoreNotifier`). |
| `Generation` | — | `int` | Zahl der bisher gemeldeten Wiederherstellungen (nur lesend). |
| `NotifyRestored` | — | `void` | Meldet eine erfolgreiche Wiederherstellung an alle Abonnenten. |

- Implementiert von `RestoreNotifier` (einzige Umsetzung im Produktivcode).
- Registriert als Singleton: `MauiProgram.cs:85` (`AddSingleton<IRestoreNotifier, RestoreNotifier>()`).
- Wird aufgerufen von `BackupViewModel.ConfirmRestoreAsync` (`BackupViewModel.cs:260`).
- Abonniert von `TankbookViewModel` (`TankbookViewModel.cs:51`) und `TankbookEntriesViewModel` (`TankbookEntriesViewModel.cs:74`).

## `IBackupService`
Datei: `src/Tankradar.MAUI/Services/Backup/IBackupService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CreateAsync` | `CancellationToken` | `Task<BackupOutcome>` | Erstellt eine Sicherung (`BackupResult.Ok`/`Busy`/`Failed`). |
| `ListAsync` | `CancellationToken` | `Task<IReadOnlyList<BackupFileInfo>>` | Listet vorhandene Sicherungen, neueste zuerst. |
| `InspectAsync` | `string fileName`, `CancellationToken` | `Task<BackupOutcome>` | Prüft eine Sicherung und liefert ihr Manifest für die Rückfrage; verändert nichts. |
| `RestoreAsync` | `string fileName`, `CancellationToken` | `Task<BackupResult>` | Stellt die Daten wieder her (Prüfung, Entpacken, Migration, atomarer Austausch mit Rollback); löst bei `Ok` die Benachrichtigung in `ConfirmRestoreAsync` aus. |
| `ImportAsync` | `PickedBackupFile file`, `CancellationToken` | `Task<BackupOutcome>` | Übernimmt eine externe Datei in die Liste. |
| `DeleteAsync` | `string fileName`, `CancellationToken` | `Task<BackupResult>` | Löscht eine Sicherung. |
| `GetFullPath` | `string fileName` | `string?` | Vollständiger Pfad für „Teilen“. |

- Implementiert von `BackupService`; in Tests durch `FakeBackupService` (`src/Tankradar.Tests.Unit/Unit/Support/FakeBackupService.cs`) ersetzt.
- Registriert als Singleton: `MauiProgram.cs:86`.
