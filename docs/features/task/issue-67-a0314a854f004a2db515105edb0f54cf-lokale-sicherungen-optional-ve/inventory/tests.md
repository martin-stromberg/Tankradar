# Tests: Bestand und Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-16, Läufe zwischen 17:02 und 17:21 Uhr +0100 (MEZ)
- Branch und Commit-ID: `task/issue-67-a0314a854f004a2db515105edb0f54cf-lokale-sicherungen-optional-ve` @ `61272e5cb72b3e8a169a201e015143cb2fa69e95` („fix: NuGet-Pakete auf November-2026-Patchstände aktualisiert (#72)")
- Uncommittete Änderungen im getesteten Stand: nur das unversionierte Feature-Verzeichnis `docs/features/task/issue-67-a0314a854f004a2db515105edb0f54cf-lokale-sicherungen-optional-ve/` (Arbeitsdokumente der Aufgabe); keinerlei Änderungen an Produktiv- oder Testcode. `git status --porcelain` zeigt ausschließlich `??` für dieses Verzeichnis.
- Testumgebung und Runtime-/SDK-Versionen: .NET SDK 10.0.403 (einzige installierte SDK-Version), Windows (net10.0-windows10.0.19041.0), xUnit; installierte Workloads: maui-windows 10.0.20, android, ios, maccatalyst (iOS/MacCatalyst/Android beim Build ausgeblendet wie in der CI).
- Ermittelte Testsuiten und Quellen der Testbefehle: `.github/workflows/staging-ci.yml` (Job `build-and-test`) führt drei Suiten blockierend aus — `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration`, `src/Tankradar.Tests.E2E` (FlaUI, App im Testmodus off-screen). Vorbereitung wie CI: `IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false`, `dotnet restore Tankradar.sln`, `dotnet build Tankradar.sln --configuration Release --no-restore` (erfolgreich, 0 Warnungen, 0 Fehler, ~25 s). Die Coverage-Auswertung (`coverlet.runsettings`, Schwelle 70 %) wurde lokal nicht wiederholt; sie ändert nichts am Testergebnis. E2E-Hinweis: Die App lief im Testmodus (`TANKATLAS_TEST_DATA_PATH`, `TANKATLAS_TEST_WINDOW=offscreen` automatisch), ohne Speicherabzug-Umgebung (`DOTNET_Dbg*`) — nur für Absturzdiagnose relevant.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --logger "trx;LogFileName=test-results-unit.trx"` | Repository-Stamm | 0 | 2097 | 0 | 0 | [trx](test-results/test-results-unit.trx), [Log](test-results/console-unit.log) |
| Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --logger "trx;LogFileName=test-results-integration.trx"` | Repository-Stamm | 0 | 298 | 0 | 0 | [trx](test-results/test-results-integration.trx), [Log](test-results/console-integration.log) |
| E2E (FlaUI) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --logger "trx;LogFileName=test-results-e2e.trx"` | Repository-Stamm | 0 | 177 | 0 | 0 | [trx](test-results/test-results-e2e.trx), [Log](test-results/console-e2e.log) |

TRX-Zusammenfassungen (`<Counters>`): Unit `total=2097 executed=2097 passed=2097 failed=0`, Integration `total=298 executed=298 passed=298 failed=0`, E2E `total=177 executed=177 passed=177 failed=0`; jeweils `notExecuted=0`, `error=0`, `timeout=0`, `aborted=0`. E2E-Gesamtdauer ~18 Minuten.

### Nachgewiesene bestehende Testfehler

Keine. In allen drei Läufen sind alle ausgeführten Tests bestanden; es wurden keine Fehlschläge, keine übersprungenen und keine nicht ausgeführten Tests nachgewiesen.

### Testlücken und Ausführungsprobleme

- Keine. Build, alle drei Suiten und alle Einzeltests liefen ohne Infrastrukturfehler.
- Hinweis zur E2E-Abdeckung: iOS-Verhalten (Teilen-Blatt, Dateiauswahl) ist plattformbedingt nur über die Testdoubles abgesichert (vermerkt in ADR 0011); die E2E-Suite läuft ausschließlich auf der Windows-App.
- Zur Anforderung selbst: Es existieren **keine** Tests für Verschlüsselung, Passworteingabe oder Passwortfehler — der gesamte Themenbereich ist testseitig unbesetzt.

## Testklassen

### Unit-Tests (`src/Tankradar.Tests.Unit/Unit/`)

#### `BackupArchiveValidatorTests_EntryNames` (`BackupArchiveValidatorTests_EntryNames.cs`)
- `IsValidEntryName_Allowed` / `IsValidEntryName_Rejected` / `IsValidEntryName_Null_Rejected` — Whitelist der Eintragsnamen.
- `AreEntryNamesValid_DuplicatesAndMissingParts` — Duplikate und Pflichtteile.
- `Validate_ArchiveWithBadEntry_IsInvalid` / `Validate_DirectoryEntryAndBadPhotoName_AreInvalid` / `Validate_ValidArchive_IsOk` — Strukturprüfung ganzer Archive.
- `BackupFormat_IsValidFileName` — Dateinamenmuster `tankatlas-backup-*.zip`.

#### `BackupArchiveValidatorTests_Limits` (`BackupArchiveValidatorTests_Limits.cs`)
- `Limits_HaveDocumentedValues` — Werte von `BackupLimits`.
- `Validate_TooManyEntries_IsInvalid` / `Validate_PhotoOverLimit_IsInvalid` / `Validate_ManifestOverLimit_IsInvalid` — Grenzen.

#### `BackupArchiveValidatorTests_Manifest` (`BackupArchiveValidatorTests_Manifest.cs`)
- `Validate_MissingEmptyOrNoJsonManifest_IsInvalid`, `Validate_EmptyTruncatedOrNotAZip_IsInvalid`, `Validate_FormatAndSchemaVersion`, `Validate_FileListDeviatesOrWrongSize_IsInvalid` — Manifest- und Strukturprüfung.
- `WrongHash_IsDetectedByVerifyAndExtract`, `ValidArchive_VerifiesAndExtracts`, `Extract_PhotoWithWrongSignature_IsInvalid`, `Verify_MoreBytesThanDeclared_IsInvalid` — Prüfsummen-/Signaturprüfung.
- `ReadManifest_Unreadable_ReturnsNull` — Verhalten bei nicht lesbarer Datei.

#### `BackupVersionRuleTests` (`BackupVersionRuleTests.cs`)
- `CheckVersion_KnownMigration_IsOk`, `CheckVersion_NewerMigration_IsTooNew`, `CheckVersion_UnknownOrEmptyMigration_IsInvalid`, `CheckVersion_SchemaVersion`, `CheckVersion_WrongFormat_IsInvalid` — Versionsregeln.

#### `BackupViewModelTests_*` (Basis `BackupViewModelTestBase`)
- `_Create`: `Create_Success_ReloadsListAndAnnounces`, `Create_WhileRunning_LocksAllCommands`, `Create_NotOk_ShowsMessage` (Theory über `BackupResult`), `Create_Exception_ShowsFailureAndUnlocks`.
- `_Delete`: `Delete_AsksFirstAndCancelKeepsFile`, `Delete_Confirm_DeletesAndUpdatesList`, `Delete_LastItem_ShowsEmptyList`, `Delete_NotFound_ReloadsList`, `Delete_Failed_KeepsListEntry`, `Delete_Exception_ShowsFailure`.
- `_List`: `OnAppearing_LoadsItemsInOrderWithOneBasedKeys`, `EmptyList_ShowsEmptyHint`, `UnreadableItem_IsMarked`, `ListFailure_ShowsMessage`, `Texts_ComeFromCentralClass`.
- `_Restore`: `Restore_ShowsQuestionWithCountsBeforeAnyChange`, `Restore_Cancel_DoesNotCallService`, `Restore_Confirm_RestoresSelectedFileAndRaisesRestored`, `Restore_NotOk_ShowsMessageAndDoesNotRaiseRestored` (Theory), `Restore_Exception_ShowsFailure`, `Restore_TooNew_ShowsVersionAndNoQuestion`, `Restore_UnreadableFile_ShowsInvalidWithoutService` (relevant: `IsReadable=false` führt direkt zu `InvalidFile`, kein Service-Aufruf), `Restore_InspectNotFound_ReloadsList`, `Questions_OnlyOneOpenAtATime`.
- `_RestoreNotification`: `Restore_Success_NotifiesOnce`, `Restore_Failure_DoesNotNotify`.
- `_ShareImport`: `Share_PassesFullPathOfSelectedFile`, `Share_Failures_ShowMessages`, `Import_Cancelled_DoesNothing`, `Import_InvalidAndTooNew_ShowMessages` (Theory), `Import_Success_OpensRestoreQuestionForNewItem`, `Import_PickerFails_ShowsMessage`.

#### `BackupTextsTests_Format` / `BackupTextsTests_Results`
- `FormatDate_UsesLocalTime`, `FormatSize_GermanFormat` (Theory), `FormatCounts_SingularAndPlural`, `FormatRestoreQuestion_ContainsAllParts` / `FormatRestoreQuestion_WithoutVersionAndCounts`, `DeleteQuestionTitleAndHints`.
- `GetMessage_AllResults_HaveText`, `GetMessage_ErrorCases_AreExact`, `Hint_NamesScopeAndEncryption` (prüft u. a. den Hinweis „nicht verschlüsselt“).

#### `BackupFileCleanupTests`, `BackupFilePickerSelectorTests_TestMode`, `BackupSharerSelectorTests_TestMode`, `TestBackupSharerTests`
- Aufräum-Helfer, Auswahl der Testmodus-Implementierungen (`TANKATLAS_TEST_DATA_PATH`, `TANKATLAS_TEST_BACKUP_IMPORT`, `test-shared`), `PickerTitle_IsCentralText`, `ShareAsync`-Kopie/Fehlerfall.

#### `SettingsViewModelTests_Backup`
- `WithoutBackup_HasBackupIsFalse`, `OnAppearing_LoadsBackupList`, `Restored_ReloadsSettings`, `Restored_ViaNotifier_ReloadsSettings`.

#### `RestoreNotifierTests`, `RestoreRecoveryTests`, `RestoreSwapUndoTests`
- Event-Verhalten (`NotifyRestored_*`, `Subscriber_IsHeldWeakly`), Journal-R/W/L und Recovery-Szenarien (`Recover_*`), `Undo_FailingBetweenMainAndWal_IsCompletedBySecondRun`.

### Integrationstests (`src/Tankradar.Tests.Integration/Integration/`)

Sammlung `BackupCollection` („BackupRestoreSharedPools", `Integration/Support/BackupCollection.cs`): serialisiert alle Wiederherstellungs-Tests, weil `ClearAllPools` prozessweit wirkt.

#### `BackupServiceTests_*`
- `_Create`: `Create_ContainsOnlyManifestDatabaseAndPhotos_WithMatchingManifest`, `Create_DatabaseHasOnlyKnownTables` (Wächter für den Sicherungsumfang), `Create_Twice_CreatesTwoFilesAndLeavesLiveDataUnchanged`, `Completeness_AllEntitiesTimestampsAndPhotoBytes_SurviveRestore`.
- `_Atomicity`: `Restore_FailureAtStep_RestoresOldStateCompletely` (Theory über alle `RestoreSwap`-Schritte), `Crash_DuringSwap_IsRecoveredAtNextStart`.
- `_CustomName`: `Restore_BringsBackCustomName`, `Restore_BackupBeforeCustomName_IsMigratedWithoutCustomName`, `Restore_CustomNameAtLimit_IsAccepted`.
- `_EmbeddedNul`: `Restore_EmbeddedNul_IsRejectedOrTruncated_AndKeepsOtherData` (Theory über 4 Textfelder), `Import_EmbeddedNul_IsRejectedOrListed_WithoutLeftovers`.
- `_ListDeleteImport`: `List_OrdersNewestFirst_MarksUnreadable_IgnoresAndCleansTemporaryFiles`, `List_WithoutBackupDirectory_IsEmpty`, `Delete_RemovesFile_AndRejectsPaths`, `Import_ValidFile_IsAddedToList_WithCounterOnCollision`, `Import_InvalidAndTooNewFiles_AreRejected_WithoutLeftovers`.
- `_Rebuild`: `Restore_PreparedDatabase_IsRebuiltCleanly` (Theory über `BackupTamper`), `Import_PreparedDatabase_IsAcceptedAndRestoredCleanly`, `TamperedArchive_ReallyContainsTheManipulation`, `Inspect_CountsComeFromTheVerifiedDatabase_NotFromTheManifest`, `Inspect_UnusableDatabase_IsInvalid`, `Restore_Version0114Schema_WithLockRow_IsMigratedAndComplete`.
- `_Resilience`: `Restore_WithLeftoverJournal_UndoesFirstThenSucceeds`, `Restore_WhenUndoFails_KeepsRollbackAndJournal`, `Recovery_WhenUndoFails_ThrowsAndKeepsRollback`, `Undo_WithUnreadableJournal_KeepsLiveData` / `_ReturnsRollbackOnly`, `Restore_WhenJournalWriteFails_KeepsLiveData`, `Restore_DatabaseBrieflyLocked_SucceedsByRetry`, `Restore_WithPooledConnectionHoldingWal_Succeeds`, `List_WithLockedFile_StillListsAllFiles`.
- `_Restore`: `Restore_ReplacesChangedDataAndPhotos`, `Restore_UnknownFile_IsNotFound_AndKeepsData`.
- `_RouteKey`: `Create_DoesNotContainRouteKey`, `Restore_LeavesRouteSearchStateUntouched_AndNewDeviceHasItOff`.
- `_Schema`: `Restore_UnusableDatabase_IsRejected_AndKeepsLiveData` (Theory), `Import_UnusableDatabase_IsRejected_WithoutLeftovers` (Theory), `Restore_PrepareTimeout_FailsWithoutChangingLiveData`, `Restore_CancelledByCaller_KeepsLiveDataAndLeavesNoLeftovers`, `Import_UntamperedFile_IsAccepted`, `Import_UnusableDatabaseBytes_AreRejectedBeforeEnteringTheList`.
- `_SourceFile`: `Restore_TruncatedDatabase_IsInvalidFile_AndKeepsLiveData` (Theory).
- `_Storage`: `Restore_LeavesDatabaseInWalMode`, `Create_DoesNotContainDeletedContent` (VACUUM-INTO-Nachweis), `Create_PhotoOverRestoreLimit_FailsWithoutLeavingFiles`.
- `_Versions`: `Restore_OlderMigration_IsMigratedAndKeepsData`, `Restore_TooNewBackup_IsRejected_AndKeepsLiveData` (Theory), `Restore_InvalidFiles_AreRejected_AndKeepLiveData`.

### E2E-Tests (`src/Tankradar.Tests.E2E/E2E/FlaUI/`)

#### `BackupE2ETests_*` (Basis `BackupE2ETestBase`)
- `_Flow`: `CreateChangeRestoreDelete_FullFlow`.
- `_Import`: `ImportExternalFile_AddsToListAndRestores`, `ImportWithoutFile_IsCancelledSilently` (Testpicker über `TANKATLAS_TEST_BACKUP_IMPORT`).
- `_InvalidFiles`: `BrokenAndManipulatedFiles_AreRejected_WithoutChangingData`, `TooNewBackup_IsRejected_WithUpdateHint`.
- `_List`: `TwoBackups_AreOrderedNewestFirst_AndRestoreUsesSelectedOne`.
- `_Persistence`: `BackupAndRestoredData_SurviveRestart`.
- `_Photo`: `RestoreBringsBackEntriesPhotoAndStatistics`.
- `_Share`: `Share_CopiesFileInTestMode_WithoutDialog`.
- `_TamperedSchema`: `PrefixedTrigger_HasNoEffectAfterRestore`, `MigrationLockRow_DoesNotBlockImportAndRestore`, `InvalidDate_IsRejected_WithoutChangingData`.
- `SetupE2ETests_Backup`: `RestoreBackupWithSettings_NextStart_ShowsNoSetup`.
- `TankbookEntriesE2ETests_RestoreReload`: `Restore_WhileEntriesPageIsOpen_ShowsRestoredState`, `Restore_WithoutSelectedVehicle_ShowsRestoredVehicle`, `Restore_ResetsSelection_ShowsFirstVehicleByName`.

## Hilfsmethoden

### `BackupTestEnvironment` (`src/Tankradar.Tests.Integration/Integration/Support/BackupTestEnvironment.cs`)
- `CreateService(swapHook, prepareTimeout)` — echten `BackupService` auf `TestDatabase` mit aufzeichnendem `IDatabaseFileProtector` und `IPriceCacheInvalidator`-Zähler.
- `SeedAsync(photoBytes, vehicleName)` — Beispieldaten in allen Tabellen + JPEG-Belegfoto.
- `GetVehicleNamesAsync`, `Snapshot()` (gehashte Tabellen-/Fotoinhalte zum Vorher/Nachher-Vergleich), `BackupDirectory`, `ReceiptsDirectory`, `ProtectedFiles`, `ProtectedDatabases`, `CacheInvalidations`.

### `BackupTestArchive` (`src/TestSupport/BackupTestArchive.cs`)
- `Create(...)` — Sicherungs-ZIP unabhängig vom Produktivcode (Manifest, DB, Fotos; Optionen: falsche Prüfsumme, `schemaVersion`, `format`, Zusatzeinträge inkl. Zip-Slip, deklarierte Größe).
- `CreateWithoutManifest`, `CreateWithManifestBytes`, `Truncate`, `Hash`, `DefaultMigration`.

### `BackupTestTampering` (`src/TestSupport/BackupTestTampering.cs`)
- `GetSql(BackupTamper)`, `IsRejected(BackupTamper)`, `CreateTamperedCopy(sourceZip, targetZip, tamper|sql)` — manipulierte Sicherungen mit gültigem Manifest/Prüfsummen.

### `BackupViewModelTestBase` (`src/Tankradar.Tests.Unit/Unit/BackupViewModelTestBase.cs`)
- Standardaufbau: `FakeBackupService` (2 Einträge), `FakeBackupFilePicker`, `FakeBackupSharer`, `RestoreNotifier`, `FakeScreenReaderAnnouncer`; `AppearAsync`, `PressAsync(ICommand)`.

### `FakeBackupService` (`src/Tankradar.Tests.Unit/Unit/Support/FakeBackupService.cs`)
- In-Memory-`IBackupService`: einstellbare Ergebnisse (`CreateResult`, `RestoreResult`, `DeleteResult`, `InspectResult`, `ImportResult`), Aufrufprotokoll (`Restored`, `Deleted`, `Inspected`, `Imported`, `CreateGate` für Sperrtests), `Exception`-Injektion, Standardmanifest/`ImportedFile`. **Keine Passwortbezüge** — müsste bei Signaturerweiterung angepasst werden.

### `FakeBackupFilePicker` / `FakeBackupSharer` (`src/Tankradar.Tests.Unit/Unit/Support/FakeBackupPlatform.cs`)
- `FakeBackupFilePicker`: vorgegebene `PickedBackupFile` oder `null`, `Exception`, `PickCount`.
- `FakeBackupSharer`: protokolliert `Shared`, `Exception`.

### `BackupE2ETestBase` (`src/Tankradar.Tests.E2E/E2E/FlaUI/BackupE2ETestBase.cs`)
- `OpenBackup`, `ItemId`, `WaitForStatus`/`WaitForStatusContaining`, `WaitUntilGone`, `BackupFiles`, `WaitForBackupFileCount`, `CreateBackup`, `RestoreBackup`, `IsNearbyOn`, `NewExternalPath`, `ImportEnvironment` (setzt `TANKATLAS_TEST_BACKUP_IMPORT`).

### `E2ETestBase` (`src/Tankradar.Tests.E2E/E2ETestBase.cs`)
- Startet die kompilierte App mit `TANKATLAS_TEST_DATA_PATH` (isoliertes Verzeichnis), `TANKATLAS_TEST_WINDOW` (off-screen bzw. `TANKRADAR_E2E_WINDOW` steuerbar) plus klassenspezifischen Umgebungsvariablen; `RestartApplication(...)`, Diagnoseartefakte bei Fehlschlag.
