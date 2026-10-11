# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-11, Start 18:46 +02:00 (Build), Ende 18:53 +02:00 (E2E)
- Branch und Commit-ID: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-13-tankvorgaenge`, `dc627bfe4081b09898605ea033f65d67a4e0bf7a`
- Uncommittete Änderungen im getesteten Stand: nur das unversionierte Verzeichnis `docs/features/` (Feature-Dokumentation,
  `todo.md` sowie die in diesem Durchlauf erzeugten Planungsdokumente); kein Produkt- oder Testcode verändert.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11 Pro 10.0.26200, .NET SDK 10.0.401, Konfiguration Release, xUnit/
  `dotnet test`; E2E mit FlaUI im Standard-Fenstermodus der Testbasis (off-screen, `TANKRADAR_E2E_WINDOW` nicht gesetzt).
  Die Variablen `FUEL_PRICE_API_KEY`, `TANKRADAR_FUEL_PRICE_API_KEY`, `TANKRADAR_PRICE_API_KEY` wurden per `env -u` entfernt
  (nicht gelesen).
- Ermittelte Testsuiten und Quellen der Testbefehle: `scripts/local-ci.ps1` (Build Release; `dotnet test` für
  `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration`, `src/Tankradar.Tests.E2E` mit `--no-build`). Ohne Coverage-
  Sammlung, ohne Node-/Workflow-/Sicherheits- und iOS-Prüfungen von `local-ci.ps1`.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Build | `dotnet build Tankradar.sln --configuration Release` | Repository-Root | 0 | – | – | – | [build.log](test-results/build.log) (0 Warnungen, 0 Fehler) |
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --logger "trx;LogFileName=unit.trx"` | Repository-Root | 0 | 889 | 0 | 0 | [unit.log](test-results/unit.log), [unit.trx](test-results/unit.trx) |
| Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --logger "trx;LogFileName=integration.trx"` | Repository-Root | 0 | 116 | 0 | 0 | [integration.log](test-results/integration.log), [integration.trx](test-results/integration.trx) |
| E2E (FlaUI) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --logger "trx;LogFileName=e2e.trx"` | Repository-Root | 0 | 80 | 0 | 0 | [e2e.log](test-results/e2e.log), [e2e.trx](test-results/e2e.trx) (Dauer 4 min 40 s) |

Exit-Codes zusätzlich in [exitcodes.txt](test-results/exitcodes.txt).

### Nachgewiesene bestehende Testfehler

Keine. Alle 1.085 ausgeführten Tests (889 Unit, 116 Integration, 80 E2E) waren grün.

### Testlücken und Ausführungsprobleme

- Keine abgebrochenen oder übersprungenen Tests.
- Nicht ausgeführt (nicht Teil dieser Bestandsaufnahme): Coverage-Schwelle, Node-Tests, Workflow-Validierung,
  Sicherheitsprüfung und iOS-Compile-Prüfung aus `local-ci.ps1` sowie die Pre-Push-Hooks.
- Fachliche Lücke: Für Tank-/Ladevorgänge (Erfassung, Verbrauch, Plausibilität, Belegfotos), für `Foreign Keys=True` und
  für die Neuzählung beim Bestätigen der Fahrzeug-Löschung gibt es keine Tests, weil die Funktionen fehlen.

## Testklassen (Bereich Tankbuch, Datenbank, Offline)

### Unit (`src/Tankradar.Tests.Unit/Unit/`)

#### `TankbookTextsTests_Labels`
- `GetFuelKindLabel_AllValues`, `GetFuelKindLabel_UnknownValue_Throws` — Beschriftungen der Kraftstoffarten
- `FormatVehicleDetails_WithAndWithoutConsumption` — Detailzeile
- `FormatDeleteQuestion_ByEntryCount` — Rückfrage für 0/1/3 Vorgänge
- `ButtonDescriptions_NameActionAndVehicle` — Sprachausgabe-Beschreibungen (u. a. „Ja, Fahrzeug „X“ löschen“)

#### `TankbookTextsTests_Messages`
- `FieldAndResultMessages_CoverAllValues` — Texte für alle `VehicleFieldError`/`VehicleResult`

#### `TankbookViewModelTests_Load`
- `Load_ShowsVehiclesWithDetails`, `Load_NoVehicles_ShowsEmptyHint`, `Load_StorageFailure_ShowsMessageWithoutLoggingNames`, `Load_AfterFailure_AppearingAgain_HidesMessage`

#### `TankbookViewModelTests_Form`
- `Add_OpensFormWithPetrolPreselected`, `SelectElectric_SwitchesConsumptionUnit`, `Save_InvalidFields_ShowsFieldErrorsAndSavesNothing`, `Save_Valid_CreatesVehicleClosesFormAndReloads`, `Save_DuplicateName_ShowsNameError`, `Edit_PrefillsFormAndSavesChanges`, `Cancel_ClosesFormWithoutSaving`, `Save_StorageFailure_ShowsMessage`, `Save_VehicleDeletedMeanwhile_ClosesFormShowsMessageAndReloads`, `Save_WhileSaving_LocksSaveAndConfirmDeleteEvenDuringReload`

#### `TankbookViewModelTests_Delete`
- `Delete_WithoutEntries_AsksSimpleQuestion`, `Delete_WithEntries_AsksExplicitlyAndDeletesEntries`, `Delete_Cancel_KeepsVehicle`, `Delete_StorageFailure_ShowsMessageAndKeepsVehicle`, `Delete_ClosesOpenForm`, `Delete_SecondVehicle_ClosesFirstConfirmation`, `Edit_ClosesOpenConfirmation`, `Delete_VehicleDeletedMeanwhile_ClosesConfirmationAndReloads`, `Delete_AnnouncesQuestionAndDescribesButtonsWithName`

#### `VehicleServiceTests_Manage`
- `CreateVehicle_StoresAllFieldsAndTrimsName`, `CreateVehicle_DuplicateNameIgnoringCase_IsRejected`, `CreateVehicle_InvalidInput_IsRejectedWithoutSaving`, `GetVehicles_AreOrderedByNameGerman`, `UpdateVehicle_ChangesAllFields`, `UpdateVehicle_OwnNameOtherCase_IsAllowed_OtherVehiclesName_IsRejected`, `UpdateVehicle_Unknown_ReportsNotFound`, `Vehicles_SurviveRestart`, `GetVehicles_UnknownStoredFuelKind_FallsBackToPetrolAndLogsWithoutName`, `CreateVehicle_ConcurrentSameName_ReportsDuplicateName`

#### `VehicleServiceTests_Delete`
- `CountEntries_ReturnsOnlyEntriesOfVehicle`, `DeleteVehicle_WithoutEntries_RemovesVehicle`, `DeleteVehicle_WithEntries_DeletesItsEntriesOnly`, `DeleteVehicle_Unknown_ReportsNotFound`, `DeleteVehicle_FreesName`

#### `VehicleInputParserTests_Odometer` / `VehicleInputParserTests_Consumption`
- `ParseOdometer_ValidInput_ReturnsValue`, `ParseOdometer_InvalidInput_ReturnsError`, `ParseConsumption_ValidInput_ReturnsValue`, `ParseConsumption_InvalidInput_ReturnsError` (Theories)

#### `VehicleValidatorTests_Form` / `VehicleValidatorTests_Input`
- `ValidateForm_AllFieldsInvalid_ReportsEveryFieldAndNoInput`, `ValidateForm_ValidFields_BuildsTrimmedInput`, `ValidateForm_NameTooLong_ReportsTooLong`, `Validate_Input_ReturnsResult`

#### `DatabaseFileProtectorTests_Platform`
- `Protect_OnWindows_DoesNothingAndDoesNotThrow`

#### `PlatformManifestTests_Location`
- `InfoPlist_ContainsWhenInUseUsageDescription`, `AppxManifest_ContainsLocationCapability`, `PrivacyManifest_DeclaresPreciseLocationWithoutTracking` — Muster für Plist-Prüfungen

#### `LocationServiceSelectorTests_TestMode`
- Muster für die Auswahl Test-/Plattformimplementierung

### Integration (`src/Tankradar.Tests.Integration/Integration/`)

#### `VehiclesMigrationTests_Upgrade`
- `Initialize_FromSchemaBeforeVehicles_KeepsExistingDataAndAddsTables` — Upgrade mit Bestandsdaten
- `Database_CascadesVehicleDeleteToEntries` — SQL-Kaskade, `PRAGMA foreign_key_list`
- `Database_RejectsDuplicateVehicleNamesCaseInsensitive`

#### `VehicleServiceTests_RealDatabase`
- `Vehicles_SurviveRestartOnRealDatabaseFile`, `DeleteVehicle_WithEntries_OnRealFile_RemovesEntries`

#### `MigrationsTests_ModelSync`
- `Model_HasNoPendingChanges` — Modell und Snapshot synchron

#### `DatabaseInitializerTests_FirstStart` / `DatabaseInitializerTests_Upgrade`
- `Initialize_FirstStart_CreatesDatabaseFileAndSchema`, `Initialize_FromOlderSchemaVersion_KeepsData` (erwartet **6** Migrationen), `Initialize_Twice_IsIdempotent`

### E2E (`src/Tankradar.Tests.E2E/E2E/FlaUI/`)

#### `TankbookE2ETests_Flow`
- `CreateEditDeleteVehicle_Flow` — erwartet `DeleteConfirm`-Name „Ja, Fahrzeug „Golf Variant“ löschen“

#### `TankbookE2ETests_DeleteWithEntries`
- `DeleteVehicleWithEntries_AsksExplicitlyAndDeletesEntries` — erwartet Name „Fahrzeug „Golf“ und Vorgänge löschen“

#### `TankbookE2ETests_Validation`
- `InvalidInput_ShowsFieldErrorsAndSavesNothing`, `DuplicateName_ShowsNameError`

#### `TankbookE2ETests_Persistence`
- `Vehicles_SurviveApplicationRestart`

#### `HomeE2ETests_Offline`
- `UnreachableService_ShowsLastKnownPricesAndBanner` — Startseite offline (`ClosedPortUrl()`), Banner, Preis, Alter

#### `DetailE2ETests_Open`
- `OpenDetail_FromList_ShowsStationDetails` (prüft `Detail.Age.SuperE5`, kein `Detail.OfflineBanner`), `OpenDetail_MissingSourceData_IsOmittedAndBackReturnsToList`

## Hilfsmethoden

### `VehicleFixture` (Unit)
- `Factory` (`InMemoryDbContextFactory`), `Clock` (`ManualTimeProvider`), `Logger` (`RecordingLogger<VehicleService>`), `Service`, `CreateService()`, `AddEntriesAsync(vehicleId, count)` (rohe Minimal-Vorgänge), `CountAllEntriesAsync()`

### `InMemoryDbContextFactory` (Unit)
- SQLite im Speicher, `FailingCreations`, `Interceptor`, `CreatedCount`

### `FakeDatabaseFileProtector`, `FakeScreenReaderAnnouncer`, `FakeAppDataPathProvider`, `ManualTimeProvider`, `ListLogger` (Unit)
- Testdoubles für Dateischutz, Ansagen, Pfade, Zeit, Protokoll

### `FavoritesFixture`, `StationFactory` (Unit)
- Favoriten und Tankstellen in der Testdatenbank

### `TestDatabase` (Integration)
- Datei-DB im isolierten Verzeichnis (`Data Source=…;Pooling=False`), `CreateFactory()`, `CreateInitializer()`

### `TankbookE2ETestBase` (E2E, abgeleitet von `SettingsE2ETestBase`)
- `DatabasePath`, `OpenTankbook()`, `Press`, `Type`, `ReadTexts`, `WaitForTexts`, `PressForVehicle(automationId, vehicleName)`, `FillVehicleForm`, `SaveVehicleForm`, `CreateVehicle`, `FindAllOrdered`

### `TankbookTestDatabase` (E2E)
- `AddEntries(databasePath, vehicleName, count)` (nur `VehicleId`, `CreatedUtc`), `CountEntries(databasePath)`

### `HomeE2ETestBase` → `FavoritesE2ETestBase` → `SearchE2ETestBase` → `SettingsE2ETestBase` → `E2ETestBase` (E2E)
- `CreateGroupWithStation`, `OpenHome`, `LeaveDetail`, `OpenStationDetail`, `AddToNewGroup`, `PressNamed`, `SafeTexts`, `WaitForText`, `CreateEnvironment(baseUrl, location, apiKey, …)`, `ClosedPortUrl()`, `RestartApplication(env)`, `RunWithDiagnostics`, `NavigateToTab`, `WaitUntil` (20 s), `FindByAutomationId`, `Exists`

### `MockTankerkoenigServer`, `MockNominatimServer`, `MockTileServer`, `TestDataPaths` (`src/TestSupport/`)
- Mock-Dienste und Testmodus-Variablen (`TANKATLAS_TEST_DATA_PATH`, `TANKATLAS_TEST_LOCATION`, …)
