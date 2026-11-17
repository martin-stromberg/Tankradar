# Interfaces: Sicherung und angrenzende Verträge

## `IBackupService`

Datei: `src/Tankradar.MAUI/Services/Backup/IBackupService.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `CreateAsync` | `CancellationToken` | `Task<BackupOutcome>` | Sicherung aus konsistentem DB-Stand und allen Belegfotos; `Ok`/`Busy`/`Failed`, keine halbe Datei. **Kein Passwortparameter.** |
| `ListAsync` | `CancellationToken` | `Task<IReadOnlyList<BackupFileInfo>>` | Liste der Sicherungen, neueste zuerst; nicht lesbare gekennzeichnet. |
| `InspectAsync` | `string fileName`, `CancellationToken` | `Task<BackupOutcome>` | Strukturprüfung + Manifest für die Rückfrage; verändert nichts. |
| `RestoreAsync` | `string fileName`, `CancellationToken` | `Task<BackupResult>` | Vollständige Prüfung, Staging, atomarer Austausch; außer bei `Ok` bleiben Daten unverändert. |
| `ImportAsync` | `PickedBackupFile file`, `CancellationToken` | `Task<BackupOutcome>` | Externe Datei nach voller Prüfung in die Liste; `TooNew` mit Manifest. |
| `DeleteAsync` | `string fileName`, `CancellationToken` | `Task<BackupResult>` | Löscht eine Sicherung; `NotFound` auch bei unzulässigem Namen. |
| `GetFullPath` | `string fileName` | `string?` | Vollständiger Pfad (für „Teilen“) oder `null`. |

Implementiert von `BackupService` (Produktiv) und `FakeBackupService` (Unit-Tests). **Keine Signatur nimmt ein Passwort oder liefert eine Verschlüsselungserkennung.**

## `IBackupFilePicker`

Datei: `src/Tankradar.MAUI/Services/Backup/IBackupFilePicker.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `PickAsync` | `CancellationToken` | `Task<PickedBackupFile?>` | Externe Sicherungsdatei auswählen; `null` bei Abbruch. |

Implementierungen: `MauiBackupFilePicker` (Produktiv), `TestBackupFilePicker` (Testmodus, `TANKATLAS_TEST_BACKUP_IMPORT`), `FakeBackupFilePicker` (Unit-Tests). Auswahl über `BackupFilePickerSelector.Create`.

## `IBackupSharer`

Datei: `src/Tankradar.MAUI/Services/Backup/IBackupSharer.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `ShareAsync` | `string fullPath`, `string title`, `CancellationToken` | `Task` | Datei über die Systemfunktion „Teilen/Speichern“ weitergeben. |

Implementierungen: `MauiBackupSharer`, `TestBackupSharer`, `FakeBackupSharer`. Auswahl über `BackupSharerSelector.Create`.

## `IRestoreNotifier`

Datei: `src/Tankradar.MAUI/Services/Backup/IRestoreNotifier.cs`

| Mitglied | Parameter | Rückgabewert | Zweck |
|----------|-----------|--------------|-------|
| `Restored` (Event) | – | – | Nach erfolgreicher Wiederherstellung; Abonnenten schwach gehalten. |
| `Generation` | – | `int` | Zahl der gemeldeten Wiederherstellungen. |
| `NotifyRestored` | – | `void` | Meldet eine Wiederherstellung an alle Abonnenten. |

Implementiert von `RestoreNotifier`. Aufgerufen in `BackupViewModel.ConfirmRestoreAsync`; abonniert von `SettingsViewModel`, `TankbookViewModel`, `TankbookEntriesViewModel`.

## `IRestoreRecovery`

Datei: `src/Tankradar.MAUI/Services/Backup/IRestoreRecovery.cs`

| Methode | Parameter | Rückgabewert | Zweck |
|---------|-----------|--------------|-------|
| `RecoverIfNeeded` | `string dataDirectory` | `void` | Unterbrochenen Austausch beim Start zurücknehmen (`IOException`, wenn unmöglich). |

Implementiert von `RestoreRecovery`; aufgerufen von `DatabaseInitializer.RunAsync`.

## Angrenzende Verträge des Dienstes

| Interface | Datei | Mitglieder | Verwendung im Sicherungsbereich |
|-----------|-------|------------|--------------------------------|
| `IDatabaseFileProtector` | `src/Tankradar.MAUI/Services/IDatabaseFileProtector.cs` | `Protect(string)`, `ProtectFile(string)` | Dateischutz auf Sicherungsdatei, Datenbank und Belegfotos. |
| `IAppDataPathProvider` | `src/Tankradar.MAUI/Services/IAppDataPathProvider.cs` | `GetDataDirectory()`, `GetCacheDirectory()` | Liefert das Datenverzeichnis (`backups/`, Staging, Rollback, Journal). |
| `IDatabaseInitializer` | `src/Tankradar.MAUI/Services/IDatabaseInitializer.cs` | `InitializeAsync()` | Migrierte Datenbank vor Zugriff sicherstellen; ruft `IRestoreRecovery`. |
| `IPriceCacheInvalidator` | `src/Tankradar.MAUI/Services/Pricing/IPriceCacheInvalidator.cs` | `InvalidateCaches()` | Verwirft Preis-Zwischenspeicher nach Wiederherstellung (optional im `BackupService`-Konstruktor). |
| `IScreenReaderAnnouncer` | `src/Tankradar.MAUI/Services/IScreenReaderAnnouncer.cs` | `Announce(string)` | Sprachausgabe der Statusmeldungen/Rückfragen im `BackupViewModel`. |

**Nicht vorhanden:** eine Abstraktion für die Passwortabfrage (kein `IBackupPasswordPrompt` o. ä.) sowie ein Krypto-Vertrag für Schlüsselableitung/Ver- und Entschlüsselung. In der Anforderung als zu schaffende Erweiterung benannt (analog `IBackupFilePicker`/`IBackupSharer` mit Testimplementierungen).
