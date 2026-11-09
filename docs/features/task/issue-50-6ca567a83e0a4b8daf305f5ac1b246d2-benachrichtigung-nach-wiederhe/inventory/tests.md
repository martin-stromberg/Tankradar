# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-09, Läufe zwischen 20:33 und 20:53 Uhr +0100 (Mitteleuropäische Zeit)
- Branch und Commit-ID: `task/issue-50-6ca567a83e0a4b8daf305f5ac1b246d2-benachrichtigung-nach-wiederhe`, `902fd3b37de6c55515972fa3c206acea3ab5a5e7` (Merge pull request #47)
- Uncommittete Änderungen im getesteten Stand: nur das unversionierte Verzeichnis `docs/features/task/issue-50-6ca567a83e0a4b8daf305f5ac1b246d2-benachrichtigung-nach-wiederhe/` (requirement.md, todo.md, inventory/); keine Änderungen an versionierten Dateien (`git status` zeigt ausschließlich `??`-Eintrag)
- Testumgebung und Runtime-/SDK-Versionen: Windows (DESKTOP-CM8OBSG), .NET SDK 10.0.403; installierte Workloads: android 36.1.69/10.0.100, ios 26.5.10318/10.0.100, maccatalyst 26.5.10318/10.0.100, maui-windows 10.0.20/10.0.100; Node.js v24.15.0; Python 3.13.14; PowerShell (pwsh); Zielframework aller .NET-Testprojekte `net10.0-windows10.0.19041.0`, xUnit 2.9.3, Microsoft.NET.Test.Sdk 17.14.1, coverlet.collector 6.0.4, FlaUI 5.0.0 (E2E)
- Ermittelte Testsuiten und Quellen der Testbefehle: `.github/workflows/staging-ci.yml` (Jobs `static-checks`, `build-and-test`), `scripts/local-ci.ps1` (lokaler Prüflauf mit derselben Befehlsfolge). Umgebung wie in den Windows-Jobs der Pipeline: `IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false`, `IncludeWindowsTarget=true`. Vorbereitung: `dotnet restore Tankradar.sln -p:Configuration=Release` und `dotnet build Tankradar.sln --configuration Release --no-restore` (erfolgreich, 0 Warnungen, 0 Fehler, ~30 s).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit-Tests | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory <TR>/unit --logger "trx;LogFileName=test-results-unit.trx" --logger "console;verbosity=normal"` | Repo-Wurzel | 0 („Der Testlauf war erfolgreich.“) | 2082 | 0 | 0 | [Log](test-results/unit-console.log), [TRX](test-results/unit/test-results-unit.trx) |
| Integrationstests | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory <TR>/integration --logger "trx;LogFileName=test-results-integration.trx" --logger "console;verbosity=normal"` | Repo-Wurzel | 0 | 297 | 0 | 0 | [Log](test-results/integration-console.log), [TRX](test-results/integration/test-results-integration.trx) |
| E2E-Tests (FlaUI) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --results-directory <TR>/e2e --logger "trx;LogFileName=test-results-e2e.trx" --logger "console;verbosity=normal"` | Repo-Wurzel | 0 | 173 | 0 | 0 | [Log](test-results/e2e-console.log), [TRX](test-results/e2e/test-results-e2e.trx) |
| Pipeline-Skripte: Node-Tests | `npm test` (`node --test "scripts/*.test.mjs"`) | Repo-Wurzel | 0 | 32 | 0 | 0 | [Log](test-results/scripts-node-console.log) |
| Pipeline-Skripte: Workflow-Validator-Tests | `python scripts/test_validate_workflows.py` | Repo-Wurzel | 1 | 13 | 1 | 0 | [Log](test-results/scripts-python-console.log) |
| Pipeline-Skripte: Workflow-Validierung | `python scripts/validate-workflows.py` | Repo-Wurzel | 1 (Prüfung meldet 2 Befunde) | — | — | — | [Log](test-results/validate-workflows-console.log) |
| Pipeline-Skripte: iOS-Deployment-Prüfung | `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` | Repo-Wurzel | 0 | alle Prüfschritte OK | 0 | 0 | [Log](test-results/ios-deployment-script-console.log) |

`<TR>` = `docs/features/task/issue-50-6ca567a83e0a4b8daf305f5ac1b246d2-benachrichtigung-nach-wiederhe/inventory/test-results`. Maschinenlesbare Reports: TRX-Dateien und Cobertura-Coverage-XML unter `test-results/unit/`, `test-results/integration/` (Coverage-Ordner mit GUID-Namen), `test-results/e2e/`. Keine Geheimnisse in den gespeicherten Ausgaben enthalten.

### Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| `ReleaseCommandTests.test_workflows_in_repository_are_clean` | `scripts/test_validate_workflows.py` (Zeile 86) | `AssertionError: Lists differ: [] != [...]` — der Workflow-Validator meldet 2 Befunde: „Upload E2E crash dumps“ in `.github/workflows/pr-staging-ci.yml` und `.github/workflows/staging-ci.yml` lädt `e2e-dumps/` hoch, das eine `.ipa` enthalten könnte. Schlägt auf unverändertem Commit fehl (keine versionierten Dateien geändert). | Lauf `python scripts/test_validate_workflows.py`, [Log](test-results/scripts-python-console.log); identischer Befund in Lauf `python scripts/validate-workflows.py`, [Log](test-results/validate-workflows-console.log) |

Alle 2552 .NET-Tests (2082 Unit + 297 Integration + 173 E2E) bestanden; kein .NET-Testfehler nachgewiesen.

### Testlücken und Ausführungsprobleme

- **Nicht ausgeführte Prüfschritte der Pipeline** (keine Testsuiten im engeren Sinn): Formatprüfung (`dotnet format --verify-no-changes`), Sicherheitsprüfung der Abhängigkeiten (benötigt nuget.org-Zugriff), Coverage-Threshold-Prüfung (`reportgenerator` + `scripts/check-coverage.mjs`, Standard 70 %), Windows-Paket (optional per `-Package`), iOS-Compile-Prüfung (`net10.0-ios`, Simulator-RID; iOS-Workload zwar installiert, aber nicht Teil der Testsuiten). Der eigentliche Build (`dotnet build ... Release`) lief als Voraussetzung der `--no-build`-Tests erfolgreich.
- **actionlint nicht installiert**: Der Workflow-Validator meldet „nur die eingebauten Prüfungen wurden ausgeführt“ ([Log](test-results/validate-workflows-console.log)); die actionlint-basierte Workflow-Prüfung der CI lief lokal nicht.
- **E2E-Laufzeitumgebung**: Die FlaUI-Tests liefen ohne `TANKRADAR_E2E_WINDOW`-Variable (App im Off-Screen-Testmodus); Dauer ~17,8 Minuten, alle 173 Tests bestanden.
- **Auffälligkeit im E2E-Lauf**: Das Konsolenprotokoll enthält einmal die Diagnosemeldung `Application failed to exit` (ein App-Prozess endete nicht sauber nach einem Test); alle umliegenden Tests bestanden, der Lauf endete mit 173/173 erfolgreich und Exit-Code 0 — kein Testfehlschlag ([Log](test-results/e2e-console.log), Zeile 30).
- Übersprungene oder deaktivierte .NET-Tests: keine (0 in allen drei Suiten).

## Testklassen

### `BackupViewModelTests_Restore`
Datei: `src/Tankradar.Tests.Unit/Unit/BackupViewModelTests_Restore.cs` (Basis: `BackupViewModelTestBase`)

- `Restore_ShowsQuestionWithCountsBeforeAnyChange` — „Wiederherstellen“ zeigt erst die Rückfrage mit Datum, Version, Zählwerten; noch kein `RestoreAsync`-Aufruf.
- `Restore_Cancel_DoesNotCallService` — „Abbrechen“ schließt die Rückfrage ohne Dienstaufruf.
- `Restore_Confirm_RestoresSelectedFileAndRaisesRestored` — Bestätigung stellt die Datei wieder her, löst das ViewModel-Ereignis aus (`ViewModel.Restored += (_, _) => raised++`, Zeile 57) und zeigt die Meldung. **Betroffen von der Anforderung** (Abonnement des entfallenden Ereignisses).
- `Restore_NotOk_ShowsMessageAndDoesNotRaiseRestored` — `[Theory]` mit `NotFound`, `InvalidFile`, `TooNew`, `Busy`, `Failed`: kein `Restored`-Ereignis (`ViewModel.Restored += (_, _) => raised = true`, Zeile 84), zentrale Meldung. **Betroffen von der Anforderung.**
- `Restore_Exception_ShowsFailure` — Ausnahme beim Wiederherstellen → allgemeine Fehlermeldung.
- `Restore_TooNew_ShowsVersionAndNoQuestion` — zu neue Sicherung wird schon bei der Prüfung gemeldet.
- `Restore_UnreadableFile_ShowsInvalidWithoutService` — nicht lesbare Datei → Fehlermeldung ohne Dienstaufruf.
- `Restore_InspectNotFound_ReloadsList` — nicht gefundene Datei bei der Prüfung → Meldung + Listen-Reload.
- `Questions_OnlyOneOpenAtATime` — nur eine Rückfrage gleichzeitig offen.

### `BackupViewModelTests_RestoreNotification`
Datei: `src/Tankradar.Tests.Unit/Unit/BackupViewModelTests_RestoreNotification.cs` (Basis: `BackupViewModelTestBase`)

- `Restore_Success_NotifiesOnce` — erfolgreiche Wiederherstellung erhöht `Restore.Generation` auf 1 (Zeilen 14–23).
- `Restore_Failure_DoesNotNotify` — Fehlschlag lässt `Generation` bei 0 (Zeilen 29–39).
- `Restore_Success_StillRaisesViewModelEvent` — prüft das ViewModel-eigene `Restored`-Ereignis (`ViewModel.Restored +=`, Zeile 50; Zeilen 44–56). **Entfällt laut Anforderung ersatzlos.**

### `SettingsViewModelTests_Backup`
Datei: `src/Tankradar.Tests.Unit/Unit/SettingsViewModelTests_Backup.cs` (Basis: `BaseTest`)

- `WithoutBackup_HasBackupIsFalse` — ohne Karte „Sicherung“ ist `HasBackup` falsch (Zeilen 24–31).
- `OnAppearing_LoadsBackupList` — Erscheinen der Seite lädt die Sicherungsliste (Zeilen 36–48).
- `Restored_ReloadsSettings` — End-to-End: `backup.OnAppearing()`, `RestoreCommand`, `ConfirmRestoreCommand`, danach lädt `SettingsViewModel` neu (`WaitForAsync` auf `_settings.LoadCount`/`ShowNearby`, Zeilen 53–73). **Betroffen**: `CreateBackup()` erzeugt das `BackupViewModel` mit internem `new RestoreNotifier()` (Zeile 18) — der Notifier ist dem `SettingsViewModel` heute nicht zugänglich.

### `RestoreNotifierTests`
Datei: `src/Tankradar.Tests.Unit/Unit/RestoreNotifierTests.cs` (Basis: `BaseTest`)

- `NotifyRestored_RaisesEventForAllSubscribersAndCounts` — alle Abonnenten informiert, `Generation` zählt.
- `Unsubscribed_IsNotNotified` — abgemeldeter Abonnent wird nicht informiert.
- `NotifyRestored_WithoutSubscribers_OnlyCounts` — ohne Abonnent nur Zähler.
- `Subscriber_IsHeldWeakly` — schwach gehaltener Abonnent wird freigegeben (GC-Test, `[MethodImpl(MethodImplOptions.NoInlining)]`-Hilfsmethode `Subscribe`, Zeilen 77–83; private Hilfsklasse `Subscriber`, Zeilen 85–93).

### `TankbookViewModelTests_Restore`
Datei: `src/Tankradar.Tests.Unit/Unit/TankbookViewModelTests_Restore.cs` (Basis: `BaseTest`)

- `Restored_ReloadsVehicles` — nach `notifier.NotifyRestored()` erscheint ein inzwischen angelegtes Fahrzeug; `RestoreNotifier` wird direkt erzeugt und in den Konstruktor gegeben (Zeilen 33–34).

### `TankbookEntriesViewModelTests_Restore`
Datei: `src/Tankradar.Tests.Unit/Unit/TankbookEntriesViewModelTests_Restore.cs` (Basis: `TankbookEntriesViewModelTestBase`)

- `Restored_ReloadsEntriesAndStatistics` — Liste und Auswertung zeigen nach `Fixture.Restore.NotifyRestored()` den geänderten Bestand (Zeilen 15–30).
- `Restored_ClearsOldExportStatus` — alte Export-Rückmeldung wird entfernt (Zeilen 35–49).
- `Restored_VehicleGone_HidesExportAndBlocksAdd` — ohne Fahrzeug: Export ausgeblendet, Erfassen blockiert, Leerzustand (Zeilen 54–73).
- `Restored_WithoutVehicle_ShowsNoVehiclesHint` — nie geladene Seite zeigt Leerzustand und merkt nichts (Zeilen 78–90).
- `Restored_ClearsStoredSelection_AndSelectsFirstByName` — gemerkte Auswahl wird verworfen; erstes Fahrzeug nach Name (Zeilen 95–109).

### `TankbookEntriesE2ETests_RestoreReload` (E2E)
Datei: `src/Tankradar.Tests.E2E/E2E/FlaUI/TankbookEntriesE2ETests_RestoreReload.cs` (Basis: `BackupE2ETestBase`)

- `Restore_WhileEntriesPageIsOpen_ShowsRestoredState` — geöffneter Tankbuch-Tab zeigt nach „Wiederherstellen“ den gesicherten Stand.
- `Restore_WithoutSelectedVehicle_ShowsRestoredVehicle` — fehlendes gewähltes Fahrzeug → gesichertes Fahrzeug (ADR 0013).
- `Restore_ResetsSelection_ShowsFirstVehicleByName` — gemerkte Auswahl zurückgesetzt, erstes Fahrzeug nach Name.

### `BackupServiceTests_Restore` (Integration)
Datei: `src/Tankradar.Tests.Integration/Integration/BackupServiceTests_Restore.cs`

- `Restore_ReplacesChangedDataAndPhotos`, `Restore_UnknownFile_IsNotFound_AndKeepsData` — Wiederherstellung auf Diensteebene (`IBackupService`), ohne Benachrichtigungsmechanismus.

### Übrige `new SettingsViewModel(...)`-Aufrufstellen (17 gesamt)

| Datei | Zeilen | Test(s) |
|-------|--------|---------|
| `src/Tankradar.Tests.Unit/Unit/SettingsViewModelTests_Loading.cs` | 32, 52, 72, 86, 100 | `LoadAsync_PopulatesAllSections`, `LoadAsync_ServiceThrows_ShowsStatusMessageAndKeepsDefaults`, `LoadAsync_ServiceThrows_LogsException`, `OnAppearing_StartsLoad`, `LoadAsync_DoesNotTriggerSave` |
| `src/Tankradar.Tests.Unit/Unit/SettingsViewModelTests_Changes.cs` | 206, 219 | `Save_Fails_ShowsStatusMessage`, `Save_Fails_LogsException` |
| `src/Tankradar.Tests.Unit/Unit/SettingsViewModelTests_Nearby.cs` | 113 | `Save_Fails_ShowsStatusMessage_ForNearby` |
| `src/Tankradar.Tests.Unit/Unit/SettingsViewModelTests_Backup.cs` | 27, 40, 58 | s. o. (3 Tests) |
| `src/Tankradar.Tests.Unit/Unit/SettingsViewModelTests_RouteSearch.cs` | 26, 41, 66 | `WithoutRouteSearch_HasRouteSearchIsFalse`, `AppearingAndDisappearing_AreForwardedToCard`, `RouteSearchState_IsIndependentOfSettingsAndBackup` |
| `src/Tankradar.Tests.Unit/Unit/ViewModelTests_PageTitles.cs` | 79 | `SettingsViewModel_Constructor_SetsTitle` |
| `src/Tankradar.Tests.Unit/Unit/DataSourceViewModelTests_Check.cs` | 123, 124 | `SettingsViewModel_HasDataSource_OnlyWhenProvided` |

Alle 17 Stellen nutzen nur die Pflichtparameter (`settingsService`, `logger`) bzw. die optionalen Karten-Parameter; kein Aufruf übergibt einen `IRestoreNotifier` (existiert im `SettingsViewModel`-Konstruktor noch nicht).

## Hilfsmethoden

### `BackupViewModelTestBase`
Datei: `src/Tankradar.Tests.Unit/Unit/BackupViewModelTestBase.cs` (Basis: `BaseTest`)

- `Service` — `FakeBackupService` mit zwei Sicherungen (Zeilen 18–22, 30).
- `Picker` — `FakeBackupFilePicker` (Zeile 36).
- `Sharer` — `FakeBackupSharer` (Zeile 42).
- `Restore` — `RestoreNotifier` (echte Implementierung, Zeile 48); wird dem `BackupViewModel` im Konstruktor übergeben (Zeile 23).
- `Announcer` — `FakeScreenReaderAnnouncer` (Zeile 54).
- `ViewModel` — das zu testende `BackupViewModel` (Zeilen 23, 60).
- `AppearAsync()` — `OnAppearing()` + `await ViewModel.LastTask` (Zeilen 66–70).
- `PressAsync(command)` — `CanExecute`-Prüfung, `Execute`, `await ViewModel.LastTask` (Zeilen 77–82).

### `TankbookEntryFixture`
Datei: `src/Tankradar.Tests.Unit/Unit/Support/TankbookEntryFixture.cs`

- `Restore` — `RestoreNotifier` (Zeile 60); wird dem `TankbookEntriesViewModel` über `TankbookEntriesViewModelTestBase.CreateViewModel` (Zeile 133) injiziert — gemeinsame Instanz zwischen Test und ViewModel.
- `Vehicles`, `Service`, `Statistics` — echte Dienste auf In-Memory-SQLite (`InMemoryDbContextFactory`, `EnsureCreated`).
- `CreateForm` / `CreateStatistics` / `CreateExport` / `CreateExportService` — erzeugen die Teil-ViewModels des Tabs.
- `CreateVehicleAsync` / `CreateEntryAsync` / `Input` / `ReadEntryAsync` / `AddFavoriteStationAsync` — Testdaten.
- `Now`, `Clock` (`ManualTimeProvider`), `Photos`, `Temp`, `Factory` — Umgebung.

### `TankbookEntriesViewModelTestBase`
Datei: `src/Tankradar.Tests.Unit/Unit/TankbookEntriesViewModelTestBase.cs`

- `CreateViewModel(entryService?, vehicleService?)` — baut das `TankbookEntriesViewModel` mit `Fixture.Restore` (Zeilen 124–138).
- `OpenAsync(vehicleId[, entryService])` — merkt die Auswahl, öffnet den Tab, wartet `LastTask` (Zeilen 93–116).
- `PressAsync`, `PressFormAsync`, `CreateViaFormAsync`, `OpenFormAsync`, `CreateVehicleAsync` — Ablauf-Hilfen.
- `SelectionStore` (`PreferencesVehicleSelectionStore` auf `FakePreferences`), `Navigator`, `Announcer`, `Picker`, Logger-Eigenschaften.

### `FakeBackupService`
Datei: `src/Tankradar.Tests.Unit/Unit/Support/FakeBackupService.cs`

- `Files`, `RestoreResult`, `InspectResult`, `Manifest`, `CreateResult`, `DeleteResult`, `ImportResult`, `ImportedFile`, `Exception`, `PathAvailable`, `CreateGate` — Steuerung der Ergebnisse.
- `Restored` (`List<string>`, Zeile 83), `Inspected`, `Deleted`, `Imported`, `ListCount`, `CreateCount` — Aufzeichnung der Aufrufe.
- `RestoreAsync` (Zeilen 150–154): protokolliert `fileName` in `Restored`, liefert `RestoreResult` oder `Exception`.

### `BackupE2ETestBase` (E2E)
Datei: `src/Tankradar.Tests.E2E/E2E/FlaUI/BackupE2ETestBase.cs` (Basis: `TankbookE2ETestBase`)

- `OpenBackup()` (Zeile 56), `CreateBackup()` (Zeile 121), `RestoreBackup(int item)` (Zeile 131) — UI-Abläufe der Karte „Sicherung“; `RunWithDiagnostics` für Diagnose-Artefakte bei Fehlschlag.

### Weitere Stützen
- `VehicleFixture` (`src/Tankradar.Tests.Unit/Unit/Support/VehicleFixture.cs`) — echter `VehicleService` für `TankbookViewModelTests_Restore`.
- `FakeSettingsService`, `FakeScreenReaderAnnouncer`, `ListLogger<T>`, `RecordingLogger<T>` (`src/Tankradar.Tests.Unit/Unit/Support/`), `NullLogger<T>` (Microsoft.Extensions.Logging.Abstractions).
