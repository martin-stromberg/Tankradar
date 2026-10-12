# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-12, 08:52:13 bis 09:01:22 (+02:00, Mitteleuropäische Sommerzeit) – Build und alle drei Testläufe nacheinander; vor jeder Änderung an Code, Tests oder Testkonfiguration.
- Branch und Commit-ID: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-14-auswertung`, Commit `799efb31a8a8ebcf6fb2ee63d7ba652a11380442` (`chore: Schritt 13 - Arbeitsdateien des Feature-Verzeichnisses entfernt`).
- Uncommittete Änderungen im getesteten Stand: nur das untracked Verzeichnis `docs/features/` (`requirement.md`, `todo.md` des Schritts 14, danach auch diese Bestandsaufnahme samt `inventory/test-results/`). Kein geänderter Quell-, Test- oder Konfigurationsstand (`git status --short` = `?? docs/features/`).
- Testumgebung: Windows 11 Pro 10.0.26200, .NET SDK 10.0.401, Zielframework `net10.0-windows10.0.19041.0`, xUnit 2.9.3, `Microsoft.NET.Test.Sdk` 17.14.1, `xunit.runner.visualstudio` 3.1.4, FlaUI.Core/FlaUI.UIA3 5.0.0. Build mit `IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false`, `IncludeWindowsTarget=true` (wie die Windows-Job-Simulation in `scripts/local-ci.ps1`). Das Build war ein inkrementeller Release-Build (Ausgabeordner bereits vorhanden). Die E2E-Tests liefen mit dem Standardfenstermodus (Off-Screen, `TANKRADAR_E2E_WINDOW` nicht gesetzt); die App legt dafür selbst ein temporäres Datenverzeichnis über `TANKATLAS_TEST_DATA_PATH` an. Der Lauf erfolgte an der Entwicklermaschine, nicht auf dem CI-Runner.
- Ermittelte Testsuiten und Quellen der Testbefehle: `README.md` (Abschnitt „Tests ausführen“: `dotnet test Tankradar.sln`), `.github/workflows/staging-ci.yml` und `pr-staging-ci.yml` (je ein `dotnet test` für `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration`, `src/Tankradar.Tests.E2E`, Release, `--no-build`), `scripts/local-ci.ps1`. Weitere Prüfungen der CI (`dotnet format --verify-no-changes`, Sicherheitsprüfung der Pakete, Build mit `TreatWarningsAsErrors`, Coverage-Schwelle 70 %, Node-Tests `scripts/*.test.mjs`, Python-Workflowprüfung, iOS-Skripttest) wurden **nicht** ausgeführt (siehe Testlücken).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Build | `dotnet build Tankradar.sln --configuration Release` (mit den vier `Include*Target`-Variablen) | Repository-Root | 0 | – (0 Warnungen, 0 Fehler) | – | – | [build.log](test-results/build.log) |
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --results-directory <inventory/test-results> --logger "trx;LogFileName=test-results-Unit.trx" --logger "console;verbosity=normal"` (kein Filter, ohne Coverage-Collector) | Repository-Root | 0 | 1028 | 0 | 0 | [Unit.log](test-results/Unit.log), [TRX](test-results/test-results-Unit.trx) |
| Integration | `dotnet test src/Tankradar.Tests.Integration` mit denselben Optionen | Repository-Root | 0 | 120 | 0 | 0 | [Integration.log](test-results/Integration.log), [TRX](test-results/test-results-Integration.trx) |
| E2E (FlaUI) | `dotnet test src/Tankradar.Tests.E2E` mit denselben Optionen | Repository-Root | 0 | 90 | 0 | 0 | [E2E.log](test-results/E2E.log), [TRX](test-results/test-results-E2E.trx) |

Gesamt: 1238 Tests bestanden, 0 fehlgeschlagen, 0 übersprungen. Es gab keine Wiederholungsläufe. Laufzeit E2E 8,4 Minuten. Die Ausgabe des E2E-Laufs enthält mehrfach die Zeile „Application failed to exit“ (Meldung des Testrahmens beim Beenden der App, keine Testfehler; die jeweiligen Tests sind „Bestanden“). Die `start.txt`/`end.txt` im Ordner `test-results/` halten Beginn und Ende des Gesamtlaufs fest.

### Nachgewiesene bestehende Testfehler

Keine. Alle ausgeführten Tests der drei Suiten sind im Ausgangslauf erfolgreich.

### Testlücken und Ausführungsprobleme

- Nicht ausgeführt, nicht Teil dieses Laufs: Coverage-Messung und Schwelle 70 % (kein `--collect:"XPlat Code Coverage"`), `dotnet format --verify-no-changes`, Sicherheitsprüfung der NuGet-Pakete, Build mit `-p:TreatWarningsAsErrors=true`, Node-Skripttests (`npm test` mit `scripts/*.test.mjs`), Python-Workflowprüfung, iOS-Skripttests (`scripts/test-ios-deployment.ps1`). iOS-, MacCatalyst- und Android-Ziele wurden nicht gebaut (Windows-Maschine, Zielframeworks ausgeblendet); Verhalten unter iOS (Interpreter, Linker) ist durch keinen Test dieses Laufs abgedeckt.
- Keine deaktivierten oder übersprungenen Tests (0 Übersprungen in allen drei Läufen). Keine Build-, Setup- oder Infrastrukturfehler.
- Fachliche Lücken im Bestand (kein Fehlschlag, sondern fehlende Tests): kein Test für `ReceiptPhotoStore.DetectExtension` mit Marke `mif1`/`msf1` (getestet: `heic`, PNG, JPEG-Signatur über `Import_*`); kein Test für den Löschfehler-Zweig, in dem `DeleteEntryAsync` ein Ergebnis ungleich `Ok`/`EntryNotFound` liefert (getestet ist der Ausnahmepfad `Delete_StorageFailure_ShowsAndAnnouncesMessage`, der `TankbookTexts.SaveFailed` prüft); keine Tests für Kosten pro 100 km, Monatsaggregation, Durchschnittsverbrauch, Verlaufspunkte oder Startverbrauch-Fallback (die Funktionalität existiert nicht).

## Testprojekte

| Projekt | Pfad | Inhalt |
|---------|------|--------|
| `Tankradar.Tests.Unit` | `src/Tankradar.Tests.Unit/` (`BaseTest.cs`, `Unit/`, `Unit/Support/`) | xUnit, Basisklasse `BaseTest` (Disposable), EF-Core-In-Memory-/SQLite-Fixtures, Fake-Dienste |
| `Tankradar.Tests.Integration` | `src/Tankradar.Tests.Integration/` (`Integration/`, `TestDataContext.cs`) | echte SQLite-Datei, Migrationstests, Dienste gegen die Datenbankdatei (`Pooling=False`) |
| `Tankradar.Tests.E2E` | `src/Tankradar.Tests.E2E/` (`E2ETestBase.cs`, `E2EDiagnostics.cs`, `TankbookTestDatabase.cs`, `E2E/FlaUI/`) | FlaUI/UIA3 gegen die kompilierte Windows-App `Tankradar.MAUI.exe` (`net10.0-windows10.0.19041.0`) |
| gemeinsam | `src/TestSupport/` (`TestDataPaths.cs`, `TransientRetry.cs`, `E2EStartupPolicy.cs`, `MockTankerkoenigServer.cs`, `MockNominatimServer.cs`, `MockTileServer.cs`, `RecordingLogger.cs`) | per `Compile Include` verlinkt |

## Testklassen (Tankbuch-Bereich)

### Unit (`src/Tankradar.Tests.Unit/Unit/`)
- `ConsumptionCalculatorTests_FullEntries` – `Calculate_TwoFullEntries_ReturnsConsumptionPer100Km`, `Calculate_FirstEntry_HasNoConsumption`, `Calculate_SameOdometer_HasNoDistanceValue`, `Calculate_EntriesOutOfOrder_UsesChronologicalOrder`.
- `ConsumptionCalculatorTests_Partial` – `Calculate_PartialBeforeFull_AddsQuantityToNextFull`, `Calculate_SeveralPartials_AreSummed`, `Calculate_PartialBeforeFirstFull_IsIgnored`, `Calculate_TrailingPartial_HasNoValue`.
- `ConsumptionCalculatorTests_Electric` – `Calculate_ElectricEntries_ReturnsKwhPer100Km`, `Calculate_Hybrid_KeepsSeparateChainsPerKind`.
- `TankbookEntryServiceTests_Read` – `GetEntries_NewestFirstWithConsumption`, `GetEntries_OtherVehiclesAreIgnored`, `GetEntries_FirstCall_DeletesUnreferencedPhotos`; weitere: `TankbookEntryServiceTests_Create`, `_Change`, `_Odometer`, `_KnownStations`.
- `TankbookEntriesViewModelTests_Load` – `Load_ShowsEntriesNewestFirstWithConsumptionTexts`, `Load_NoEntries_ShowsEmptyHint`, `Load_VehicleMissing_ShowsMessageAndCloses`, `Load_StorageFailure_ShowsMessageWithoutLoggingValues`.
- `TankbookEntriesViewModelTests_Delete` – `Delete_AsksAndDeletesAfterConfirmation`, `Delete_Cancel_KeepsEntry`, `Buttons_NameIsVisibleTextAndHintGivesContext`, `Delete_StorageFailure_ShowsAndAnnouncesMessage`; weitere: `TankbookEntriesViewModelTests_Edit`.
- `TankbookEntryFormViewModelTests_Amounts`, `_Form`, `_Photo`, `_Station`; `TankbookEntryValidatorTests_Form`, `_Input`.
- `TankbookEntryTextsTests_Format`, `TankbookEntryTextsTests_Messages`, `TankbookTextsTests_Labels`, `TankbookTextsTests_Messages`.
- `ReceiptPhotoStoreTests_Files` – `Import_SupportedPhoto_CopiesAndProtects`, `Import_UnsupportedExtension_IsRejected`, `Import_TooLarge_IsRejectedAndLeavesNoFile`, `Delete_RemovesFile`, `Delete_InvalidFileName_IsIgnored`, `DeleteUnreferenced_KeepsReferencedFiles`, `Import_ContentWithoutPhotoSignature_IsRejected`, `Import_MisnamedPhoto_GetsExtensionOfDetectedFormat` (nutzt die Bytes `Png` und `Heic` mit Marke `heic`); `ReceiptPhotoPickerSelectorTests_TestMode`.
- Fahrzeug: `VehicleServiceTests_Manage`, `_Delete`, `VehicleValidatorTests_Form`, `_Input`, `VehicleInputParserTests_Consumption`, `_Odometer`, `TankbookViewModelTests_Delete`, `_Form`, `_Load`, `_Navigation`.

### Integration (`src/Tankradar.Tests.Integration/Integration/`)
- `TankbookEntriesMigrationTests_Upgrade` – `Initialize_FromSchemaBeforeEntryDetails_KeepsDataAndFillsDefaults`.
- `TankbookEntryServiceTests_RealDatabase` – `Entries_SurviveRestartOnRealDatabaseFile`, `DeleteVehicle_WithEntries_OnRealFile_RemovesEntriesAndPhotos`.
- `VehicleServiceTests_RealDatabase`.

### E2E (`src/Tankradar.Tests.E2E/E2E/FlaUI/`), Tankbuch
- `TankbookE2ETests_Flow` (`CreateEditDeleteVehicle_Flow`), `_Persistence` (`Vehicles_SurviveApplicationRestart`), `_Validation` (`InvalidInput_ShowsFieldErrorsAndSavesNothing`, `DuplicateName_ShowsNameError`, `StartOdometerAboveFirstEntry_ShowsOdometerError`), `_DeleteWithEntries` (`DeleteVehicleWithEntries_AsksExplicitlyAndDeletesEntries`, `EntryAddedBeforeConfirming_IsNamedBeforeDeleting`).
- `TankbookEntriesE2ETests_Flow` (`CreateTwoEntries_ShowsConsumption`), `_Partial` (`PartialBetweenFullEntries_IsAddedToNextFullEntry`), `_Electric` (`ChargingEntries_ShowKwhConsumption`), `_EditDelete` (`EditAndDeleteEntry_Flow`), `_Persistence` (`Entries_SurviveApplicationRestart`), `_Receipt` (`ReceiptPhoto_IsStoredShownAndDeletedWithEntry`), `_Validation` (`InvalidInput_ShowsFieldErrorsAndSavesNothing`).
- `OfflineE2ETests_Tankbook` (`UnreachableService_PricesFromCacheAndTankbookFullyUsable`, Basisklasse `HomeE2ETestBase`, mit Mock-Servern).

## Hilfsmethoden

### Unit-Test-Support (`src/Tankradar.Tests.Unit/Unit/Support/`)
- `TankbookEntryFactory` – `Start` (feste Basiszeit), `Create(id, odometerKm, quantity, isPartial, kind)`: einfache `TankbookEntry`-Datensätze für Rechenbausteine ohne Datenbank (Fahrzeug 1, Zeitpunkt = 2026-05-01 08:00 + `id` Tage, Preis 1,5 €/Einheit, Gesamtbetrag = Menge × 1,5, Tankstelle „Alpha Tankstelle“).
- `TankbookEntryFixture` – `CreateVehicleAsync(name, fuelKind, startOdometerKm)`, statisch `Input(...)`, `CreateEntryAsync(vehicleId, input)`, `ReadEntryAsync`, `AddFavoriteStationAsync`; hält `Service` (`TankbookEntryService`), `Factory` (mit `FailingCreations` für simulierte Speicherfehler), Fake-Foto-Ablage; `IDisposable`.
- `VehicleFixture` – Fahrzeugdienst auf Test-Datenbank; `FakeTankbookNavigator`, `FakeReceiptPhotoPicker`, `FakeReceiptPhotoStore`, `FakeScreenReaderAnnouncer`, `InMemoryDbContextFactory`, `ManualTimeProvider`, `NoOpDatabaseInitializer`, `ListLogger<T>`; `RecordingLogger<T>` aus `src/TestSupport/`.
- `TankbookEntriesViewModelTestBase` (abstrakt, `BaseTest`) – Eigenschaften `Fixture`, `Announcer`, `Picker`, `Navigator`, `Logger`, `FormLogger`; `OpenAsync(vehicleId)`, `PressFormAsync`, `PressAsync`, `CreateViaFormAsync(viewModel, quantity, unitPrice, total, odometer, station)`, `CreateVehicleAsync(fuelKind)`.

### E2E (`src/Tankradar.Tests.E2E/`)
- `E2ETestBase` – startet `Tankradar.MAUI.exe` per FlaUI; setzt `TANKATLAS_TEST_DATA_PATH` (`TestDataPaths.TestDataPathEnvironmentVariable`, isoliertes temporäres Datenverzeichnis, danach gelöscht) und das Fenstermodus-Flag `TANKATLAS_TEST_WINDOW` (aus `TANKRADAR_E2E_WINDOW`: `offscreen` Standard, `foreground`); zusätzliche Umgebungsvariablen über den Konstruktor (`additionalEnvironment`), `RestartApplication(...)`, `RunWithDiagnostics(...)` (Screenshot/UI-Baum bei Fehlern, `E2EDiagnostics`).
- `SettingsE2ETestBase` (Basis der Tankbuch-, Favoriten-, Such-Tests): `WaitForAutomationId(id)`, `FindByAutomationId(id)` (mit `TransientRetry`), `WaitUntil(condition, message)`, `NavigateToTab`, **`SetFieldValue(automationId, text)`** – setzt per UIA-`SetValue`, wartet bis das native Feld den Wert meldet, wechselt den Fokus (`field.Focus()`, `field.Parent?.Focus()`) und fragt den UI-Thread fünfmal im Abstand von 60 ms ab, damit die Bindung ans MAUI-Modell übernommen ist; `ToggleFuelType`, `MoveFuelType`, `ReadFuelTypeOrder`.
- `TankbookE2ETestBase : SettingsE2ETestBase` – Eigenschaften `Tankbook` (`TankbookE2EDriver`), `DatabasePath` (`<Testdatenverzeichnis>/tankatlas.db`); Methoden `OpenTankbook`, `Press`, `Type`, `ReadTexts`, `WaitForTexts`, `PressForVehicle`, `FillVehicleForm`, `SaveVehicleForm`, `CreateVehicle` u. a. (delegiert an den Treiber).
- `TankbookE2EDriver` – `OpenTankbook`, `Find`, `WaitFor`, `Exists`, `WaitUntil`, `Press`, `Type` (über `SetFieldValue`), `ToggleSwitch`, `FindAllOrdered`, `ReadTexts`, `WaitForTexts`, `ReadHint`, `PressForVehicle`, `PressForEntry`, `FillVehicleForm`, `SaveVehicleForm`, `CreateVehicle`, `OpenEntries(vehicleName)`, `LeaveEntries`, `FillEntryForm(station, quantity, unitPrice, total, odometer, isPartial)`, `SaveEntryForm`, `CreateEntry(...)`, `ReadEntryTexts(automationId)`.
- `TankbookTestDatabase` (statisch) – direkter SQLite-Zugriff auf die Testdatenbank: `AddEntries(databasePath, vehicleName, count, firstOdometerKm, firstOccurredAt)` (fügt vollständige Kraftstoffvorgänge ein: 40 L zu 1,859 €/L, 74,36 €, je +100 km, je +1 Minute, `Pooling` aus), `ReadEntries(databasePath)` u. a.
- `src/TestSupport/TestDataPaths.cs` – Konstanten der Testmodus-Umgebungsvariablen (`TANKATLAS_TEST_DATA_PATH`, `TANKATLAS_TEST_WINDOW`, `offscreen`/`foreground`); `TransientRetry`, `E2EStartupPolicy`, `Mock*Server` (Preisdienst, Geokodierung, Kacheln).
