## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-27, Lauf ab 06:35:11 +01:00, Ende 06:53:33 +01:00 (Protokoll `test-results/run-full.log`, Zeilen `START`/`END`).
- Branch und Commit-ID: `task/issue-20-e2a2a4adeaf8403a9ea67505072dd09c-tankbuch-tank-und-ladevorgaeng`, `8103d69d72d85ae4caa58a16368eecf3a5548fd9`.
- Uncommittete Änderungen im getesteten Stand: nur der untracked Ordner `docs/features/` (Anforderung, Inventar, Dokumente). Am Quellcode, an Tests und Testkonfiguration gibt es keine Änderungen (`git status --short`: `?? docs/features/`). Die Testläufe haben keine versionierten Dateien verändert.
- Testumgebung und Runtime-/SDK-Versionen: Windows 11 Pro 10.0.26300 (win-x64), .NET SDK 10.0.402 (Workload 10.0.401), Ziel `net10.0-windows10.0.19041.0`, xUnit VSTest Adapter 3.1.4, Konfiguration `Release`. Workloads installiert: android, ios, maccatalyst; Build mit `IncludeAndroidTarget=false IncludeIosTarget=false IncludeMacCatalystTarget=false IncludeWindowsTarget=true` (entspricht der Windows-Job-Simulation in `scripts/local-ci.ps1`). Node/npm für `npm test`: Version nicht ermittelt (unbekannt).
- Ermittelte Testsuiten und Quellen der Testbefehle: `README.md` („Tests ausführen“: `dotnet test Tankradar.sln`), `.github/workflows/staging-ci.yml` (Schritte `dotnet test src/Tankradar.Tests.Unit`, `...Integration`, `...E2E` mit `--configuration Release --no-build`, Node-Skripttests `npm test`), `scripts/local-ci.ps1`. Projekte: `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration`, `src/Tankradar.Tests.E2E` (FlaUI, Windows-UI). Ausgeführt wurde die gesamte Suite, nicht nur die von der Anforderung betroffenen Tests.

### Testläufe

Die Befehle liefen einmalig in dieser Reihenfolge aus dem Repo-Wurzelverzeichnis `D:\Repositories\softwareschmiede\e2a2a4ad-eaf8-403a-9ea6-7505072dd09c`; das Gesamtprotokoll steht in `test-results/run-full.log`.

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Restore | `dotnet restore Tankradar.sln -p:Configuration=Release` | Repo-Wurzel | 0 | – | – | – | [Log](test-results/run-full.log) |
| Build | `dotnet build Tankradar.sln --configuration Release --no-restore` | Repo-Wurzel | 0 | – (0 Warnungen, 0 Fehler) | – | – | [Log](test-results/run-full.log) |
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build` (ohne Filter) | Repo-Wurzel | 0 | 1949 von 1949 | 0 | 0 | [TRX](test-results/test-results-unit.trx), [Log](test-results/run-full.log) |
| Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build` (ohne Filter) | Repo-Wurzel | 0 | 294 von 294 | 0 | 0 | [TRX](test-results/test-results-integration.trx), [Log](test-results/run-full.log) |
| E2E (FlaUI) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build` (ohne Filter) | Repo-Wurzel | 0 | 156 von 156 | 0 | 0 | [TRX](test-results/test-results-e2e.trx), [Log](test-results/run-full.log) |
| Pipeline-Skripte | `npm test` | Repo-Wurzel | 0 | 32 von 32 | 0 | 0 | [Log](test-results/run-full.log) |

Hinweise:
- Der E2E-Lauf nutzte den Standardmodus (App im Testmodus außerhalb des sichtbaren Bildschirms, gemäß README). Eine Umgebungsvariable `TANKATLAS_TEST_DATA_PATH` wurde durch den Testlauf automatisch gesetzt.
- Die Zählung „1949“ bezieht sich auf die TRX-Zusammenfassung `Counters total/executed/passed`. `notExecuted`, `failed`, `error`, `timeout`, `aborted` sind jeweils 0.
- Die Zählung der Pipeline-Skripttests (`npm test`) stammt aus der TAP-Zusammenfassung (`ℹ tests 32`, `ℹ pass 32`, `ℹ fail 0`, `ℹ skipped 0`).
- Es gab keinen Wiederholungslauf, weil der Ausgangslauf vollständig grün war.

### Nachgewiesene bestehende Testfehler

Keine. In allen Läufen gab es keinen fehlgeschlagenen Test und keinen Build-, Setup- oder Infrastrukturfehler.

### Testlücken und Ausführungsprobleme

- Übersprungene Tests: keine (`notExecuted=0`; im Quellcode keine `Skip =`-Attribute in `src/Tankradar.Tests.*`).
- Build-/Setup-Fehler: keine.
- Lücken für die Anforderung (heute nicht abgedeckt, weil das Verhalten nicht existiert):
  - Kein Test für eine gemerkte „zuletzt gewählte“ Fahrzeugauswahl, für den Fahrzeugwechsel im Kopf, für die Ansage des gewählten Fahrzeugs beim Öffnen und nach Wechsel, für den Rückfall nach Löschen des gewählten Fahrzeugs und für die Speicherung der Auswahl. Sie werden von der Anforderung als neu benannt.
  - Der Einstieg über den Tab (`TankbookPage` zeigt direkt die Vorgänge) ist in keinem Test abgedeckt.
  - `TankbookViewModelTests_Navigation.OpenEntries_NavigatesWithVehicleId` und `ShellTankbookNavigatorTests_SelectPages` setzen den heutigen Einstieg über die Fahrzeugkarte voraus. Sie werden durch die Anforderung betroffen.

## Testklassen (betroffener Bereich)

Unit-Tests (`src/Tankradar.Tests.Unit/Unit/`):

### `TankbookViewModelTests_Load`
- `Load_ShowsVehiclesWithDetails` — Fahrzeugliste mit Angaben.
- `Load_NoVehicles_ShowsEmptyHint` — Leerzustand (heutiger Hinweis `NoVehicles`).
- `Load_StorageFailure_ShowsMessageWithoutLoggingNames` — Fehlermeldung ohne Namen im Log.
- `Load_AfterFailure_AppearingAgain_HidesMessage` — Erneutes Laden blendet Meldung aus.

### `TankbookViewModelTests_Navigation`
- `OpenEntries_NavigatesWithVehicleId` — „Tank- und Ladevorgänge“ öffnet über `FakeTankbookNavigator` das gewählte Fahrzeug; Hinweis `Fahrzeug „Polo“`.

### `TankbookViewModelTests_Delete`
- `Delete_WithoutEntries_AsksSimpleQuestion`, `Delete_WithEntries_AsksExplicitlyAndDeletesEntries`, `Delete_Cancel_KeepsVehicle`, `Delete_StorageFailure_ShowsMessageAndKeepsVehicle`, `Delete_ClosesOpenForm`, `Delete_SecondVehicle_ClosesFirstConfirmation`, `Edit_ClosesOpenConfirmation`, `Delete_VehicleDeletedMeanwhile_ClosesConfirmationAndReloads`, `Delete_ButtonsNameIsVisibleTextAndHintNamesVehicle`, `Delete_EntryAddedMeanwhile_ShowsUpdatedQuestionAndKeepsVehicle` — Löschrückfrage mit Zählung und Neuzählung.

### `TankbookViewModelTests_Restore`
- `Restored_ReloadsVehicles` — Neuladen nach Wiederherstellung.

### `TankbookEntriesViewModelTests_Load`
- `Load_ShowsEntriesNewestFirstWithConsumptionTexts`, `Load_NoEntries_ShowsEmptyHint`, `Load_VehicleMissing_ShowsMessageAndCloses`, `Load_StorageFailure_ShowsMessageWithoutLoggingValues`.

### `TankbookEntriesViewModelTests_Restore`
- `Restored_ReloadsEntriesAndStatistics`, `Restored_ClearsOldExportStatus`, `Restored_VehicleGone_ClosesPage`, `Restored_VehicleGone_HidesExportAndBlocksAdd`, `Restored_VehicleGone_CloseFails_RetriesOnAppearing`, `Restored_WithoutVehicle_DoesNothing`.

### `ShellTankbookNavigatorTests_SelectPages`
- `SelectPages_OwnVehicle_SelectsPage`, `SelectPages_OtherVehicleOrOtherContext_SelectsNothing`.

### `ViewModelTests_PageTitles`
- `TankbookViewModel_Constructor_SetsTitle`, `TankbookEntriesViewModel_Constructor_SetsTitle`, u. a. — Titelprüfung.

### `PageTitleRedundancyTests`
- `Page_HasTitle_AndDoesNotRepeatItAsHeading` (Theory über `TankbookPage.xaml`/`TankbookEntriesPage.xaml`) — prüft Titelredundanz mit dem Shell-Titel (Offene Frage 6 der Anforderung).

### `TankbookTextsTests_Labels`
- `GetFuelKindLabel_AllValues`, `GetFuelKindLabel_UnknownValue_Throws`, `FormatVehicleDetails_WithAndWithoutConsumption`, `FormatDeleteQuestion_ByEntryCount`, `ButtonHints_NameActionAndVehicle`.

### `TankbookTextsTests_Messages`
- `FieldAndResultMessages_CoverAllValues`.

Integrationstests (`src/Tankradar.Tests.Integration/Integration/`), relevant:
- `VehicleServiceTests_RealDatabase`, `VehiclesMigrationTests_Upgrade`, `SetupStatusServiceTests_FirstStart`, `SettingsServiceTests_RestartPersistence`, `DatabaseInitializerTests_FirstStart`.

E2E-Tests (`src/Tankradar.Tests.E2E/E2E/FlaUI/`), die den heutigen Einstieg über die Fahrzeugkarte nutzen (`Tankbook.Vehicle.Entries`):
- `TankbookEntriesE2ETests_Flow.cs` — prüft Text und Hinweis der Schaltfläche `Tankbook.Vehicle.Entries` (`Fahrzeug „Golf“`).
- `TankbookE2EDriver.OpenEntries` (Zeile 251) — zentrale Hilfsmethode, von allen weiteren Tests über `Tankbook.OpenEntries(...)` genutzt. Aufrufe in u. a. `BackupE2ETests_Photo`, `BackupE2ETests_TamperedSchema`, `FavoritesE2ETests_DisplayName`, `OfflineE2ETests_Tankbook`, `TankbookEntriesE2ETests_EditDelete`, `..._Electric`, `..._Partial`, `..._PartialHint`, `..._Persistence`, `..._Receipt`, `..._RestoreReload`, `..._Validation`, `TankbookExportE2ETestBase`, `TankbookStatisticsE2ETests_*`.
- `TankbookE2ETests_Flow`, `TankbookE2ETests_Validation`, `TankbookE2ETests_DeleteWithEntries` — warten auf `Tankbook.Empty` und `Tankbook.Vehicle.*` der Fahrzeugliste.
- `NavigationE2ETests` — Navigation der Tabs.

## Hilfsmethoden

### `TankbookEntriesViewModelTestBase` (Basisklasse, `Unit/TankbookEntriesViewModelTestBase.cs`)
- Stellt `Fixture` (`TankbookEntryFixture`, echte Dienste auf In-Memory-SQLite), `Announcer` (`FakeScreenReaderAnnouncer`), `Picker` (`FakeReceiptPhotoPicker`), `Navigator` (`FakeTankbookNavigator`) und Logger bereit.

### `Unit/Support`
- `VehicleFixture` — Testumgebung für `IVehicleService` (Verwendung u. a. in `TankbookViewModelTests_Navigation`, `TankbookViewModelTests_Load`).
- `FakeTankbookNavigator` — zeichnet `OpenEntriesAsync`/`CloseAsync`-Aufrufe auf (Eigenschaft `Opened`).
- `FakeScreenReaderAnnouncer` — zeichnet Ansagen auf.
- `StubTankbookEntryService`, `TankbookEntryFactory`, `TankbookEntryFixture`, `TankbookExportTestData` — Testdaten für Vorgänge und Export.

### Integration
- `Integration/Support/BackupTestEnvironment.cs` — Testumgebung für Sicherung und Wiederherstellung mit Bezug zu Tankbook-Tabellen.

### E2E
- `TankbookE2EDriver` (Zeile 12 ff.) — bedient Tankbuch über UI-Automation (`PressForVehicle`, `CreateVehicle`, `OpenEntries`, `LeaveEntries`, `CreateEntry`).
- `TankbookTestDatabase` (`src/Tankradar.Tests.E2E/TankbookTestDatabase.cs`) — eigene Test-Datenbank für die UI-Tests.
