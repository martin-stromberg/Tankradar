# Bestandsaufnahme: Benachrichtigung nach Wiederherstellung vereinheitlichen

Bestandsaufnahme der Benachrichtigung geöffneter Seiten nach einer Backup-Wiederherstellung: heute zwei parallele Wege (ViewModel-Ereignis `BackupViewModel.Restored` und `IRestoreNotifier`), Ziel laut Anforderung nur `IRestoreNotifier`. Analysiert wurden Auslöser, Abonnenten, DI-Verdrahtung und die betroffenen Tests.

## Zusammenfassung

- **Zwei parallele Benachrichtigungswege bestätigt**: `BackupViewModel.ConfirmRestoreAsync` ruft bei `BackupResult.Ok` sowohl `Restored?.Invoke(this, EventArgs.Empty)` (`BackupViewModel.cs:259`) als auch `_restoreNotifier.NotifyRestored()` (`BackupViewModel.cs:260`) auf.
- **Eigenes Ereignis `BackupViewModel.Restored`** (`BackupViewModel.cs:53–56`): einziger Abonnent im Produktivcode ist `SettingsViewModel` (`SettingsViewModel.cs:45` — `backup.Restored += OnBackupRestored`, gekoppelt an `backup is not null`); Handler `OnBackupRestored` (`SettingsViewModel.cs:276–279`) ruft `_ = LoadAsync()`.
- **`IRestoreNotifier`/`RestoreNotifier`** ist vollständig vorhanden: `Restored`-Ereignis, `Generation`-Zähler, `NotifyRestored()`; schwach gehaltene Abonnenten über `WeakEventManager` (Instanzmethoden erforderlich, `RestoreNotifier.cs:4–5`). Abonnenten heute: `TankbookViewModel` (`TankbookViewModel.cs:51`, Handler `OnRestored` → `LastTask = LoadAsync()`) und `TankbookEntriesViewModel` (`TankbookEntriesViewModel.cs:74`, Handler `OnRestored` → `_selectionStore.Clear()` + `LastTask = LoadAsync()`).
- **`SettingsViewModel` besitzt keine `IRestoreNotifier`-Abhängigkeit** und kein `LastTask`-Feld (nur `LastSaveTask`); Konstruktorparameter heute: `ISettingsService`, `ILogger<SettingsViewModel>` (Pflicht) + `dataSource`, `backup`, `routeSearch` (optional).
- **DI-Registrierung vollständig vorhanden**: `AddSingleton<IRestoreNotifier, RestoreNotifier>()` (`MauiProgram.cs:85`), `AddTransient<SettingsViewModel>()` (`MauiProgram.cs:126`) — ein zusätzlicher Konstruktorparameter wird vom Container ohne weitere Änderung aufgelöst.
- **Betroffene Tests**: `BackupViewModelTests_Restore` abonniert `ViewModel.Restored` (Zeilen 57, 84); `BackupViewModelTests_RestoreNotification.Restore_Success_StillRaisesViewModelEvent` (Zeilen 44–56) prüft das entfallende Ereignis; `SettingsViewModelTests_Backup.Restored_ReloadsSettings` (Zeilen 54–73) fährt den End-to-End-Pfad über `CreateBackup()` mit internem `new RestoreNotifier()` (Zeile 18); 17 Aufrufstellen `new SettingsViewModel(...)` in 7 Testdateien.
- **Test-Ausgangszustand**: Alle 2552 .NET-Tests bestanden (2082 Unit, 297 Integration, 173 E2E/FlaUI); Node-Skript-Tests 32/32; iOS-Deployment-Prüfung OK. **Ein nachgewiesener bestehender Fehlschlag**: `ReleaseCommandTests.test_workflows_in_repository_are_clean` (`scripts/test_validate_workflows.py`) — der Workflow-Validator bemängelt den `e2e-dumps/`-Artefakt-Upload in `pr-staging-ci.yml` und `staging-ci.yml`; schlägt auf unverändertem Commit fehl. Details und Nachweise: [Tests](inventory/tests.md).

## Details

- [Datenmodell](inventory/models.md) — `BackupFileInfo` (Eintrag der Sicherungsliste im Wiederherstellungspfad)
- [Logik](inventory/logic.md) — `BackupViewModel`, `SettingsViewModel`, `TankbookViewModel`, `TankbookEntriesViewModel`, `RestoreNotifier`, `BackupItemViewModel`, DI-Registrierung in `MauiProgram`, `SettingsPage`/`TankradarContentPage`
- [Enums](inventory/enums.md) — `BackupResult` (nur `Ok` löst die Benachrichtigung aus)
- [Interfaces](inventory/interfaces.md) — `IRestoreNotifier`, `IBackupService`
- [Tests](inventory/tests.md) — Ausgangszustand der Testsuiten, betroffene Testklassen und Hilfsmethoden
