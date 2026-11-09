# Tasks: Benachrichtigung nach Wiederherstellung vereinheitlichen

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | ViewModel | `SettingsViewModel`: `using Tankradar.MAUI.Services.Backup;` ergänzen, Pflichtparameter `IRestoreNotifier restoreNotifier` als dritten Konstruktorparameter einführen, unbedingtes Abonnement `restoreNotifier.Restored += OnRestored`, Handler `OnBackupRestored` in `OnRestored` umbenennen, Parameterdokumentation anpassen | Offen | — |
| 2 | ViewModel | `BackupViewModel`: Ereignis `Restored` inkl. XML-Kommentar (Zeilen 53–56) und `Restored?.Invoke(this, EventArgs.Empty)` (Zeile 259) entfernen | Offen | — |
| 3 | Tests | `SettingsViewModelTests_Backup`: geteilte `RestoreNotifier`-Instanz als Feld einführen, `CreateBackup()` darauf umstellen, die 3 `new SettingsViewModel(...)`-Aufrufstellen damit versorgen | Offen | — |
| 4 | Tests | `SettingsViewModelTests_Backup.Restored_ViaNotifier_ReloadsSettings` neu anlegen (direktes `NotifyRestored()` ohne Karte „Sicherung" lädt Einstellungen neu) | Offen | — |
| 5 | Tests | `SettingsViewModelTests_Loading`: 5 Aufrufstellen um `new RestoreNotifier()` als drittes Argument ergänzen, `using Tankradar.MAUI.Services.Backup;` hinzufügen | Offen | — |
| 6 | Tests | `SettingsViewModelTests_RouteSearch`: 3 Aufrufstellen um `new RestoreNotifier()` ergänzen, `using Tankradar.MAUI.Services.Backup;` hinzufügen | Offen | — |
| 7 | Tests | `SettingsViewModelTests_Changes`: 2 Aufrufstellen um `new RestoreNotifier()` ergänzen, `using Tankradar.MAUI.Services.Backup;` hinzufügen | Offen | — |
| 8 | Tests | `SettingsViewModelTests_Nearby`: 1 Aufrufstelle um `new RestoreNotifier()` ergänzen, `using Tankradar.MAUI.Services.Backup;` hinzufügen | Offen | — |
| 9 | Tests | `DataSourceViewModelTests_Check`: 2 Aufrufstellen um `new RestoreNotifier()` ergänzen, `using Tankradar.MAUI.Services.Backup;` hinzufügen | Offen | — |
| 10 | Tests | `ViewModelTests_PageTitles`: 1 Aufrufstelle um `new RestoreNotifier()` ergänzen (using bereits vorhanden) | Offen | — |
| 11 | Tests | `BackupViewModelTests_Restore`: Abonnements `ViewModel.Restored` (Zeilen 57, 84) auf `Restore.Restored` umstellen | Offen | — |
| 12 | Tests | `BackupViewModelTests_RestoreNotification.Restore_Success_StillRaisesViewModelEvent` ersatzlos entfernen | Offen | — |
| 13 | Verifikation | `dotnet build Tankradar.sln -c Release` und `dotnet test src/Tankradar.Tests.Unit` ausführen; E2E-Suite `TankbookEntriesE2ETests_RestoreReload` als DI-Nachweis laufen lassen | Offen | — |
