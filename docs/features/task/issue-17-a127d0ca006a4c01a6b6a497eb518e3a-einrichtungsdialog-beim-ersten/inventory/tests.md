# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): Bestandsaufnahme begonnen 2026-10-26 19:18:13 +01:00. Build und Testläufe fanden danach zwischen ca. 19:20 und 19:28 (+01:00) statt. Genaue Einzelzeiten wurden nicht protokolliert.
- Branch und Commit-ID: `task/issue-17-a127d0ca006a4c01a6b6a497eb518e3a-einrichtungsdialog-beim-ersten`, Commit `15b7aa5fa00cfc96b6c16c256f77a79d8332a96d`.
- Uncommittete Änderungen im getesteten Stand: Beim Start war nur `docs/features/` untracked (Anforderung, `todo.md`, dieses Inventar). Am Quellcode, an Tests und an der Testkonfiguration gab es keine Änderungen. Es wurde nichts repariert oder angepasst.
- Testumgebung und Runtime-/SDK-Versionen:
  - Windows 11 Pro 10.0.26300, Git-Bash-Shell
  - .NET SDK 10.0.402 (`dotnet --version`)
  - Zielframework `net10.0-windows10.0.19041.0`
  - Node.js und Python in der Version des Rechners (nicht separat erfasst)
  - Build-Umgebung wie die Windows-Jobs der CI (`IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false`, `IncludeWindowsTarget=true`), siehe `scripts/local-ci.ps1`
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - Quelle: `scripts/local-ci.ps1` und `.github/workflows/pr-staging-ci.yml`, `README.md` (Abschnitt Voraussetzungen).
  - Suiten: Unit (`src/Tankradar.Tests.Unit`), Integration (`src/Tankradar.Tests.Integration`), FlaUI-E2E (`src/Tankradar.Tests.E2E`), Node-Skripttests (`scripts/*.test.mjs` via `npm test`), Python-Tests (`scripts/test_validate_workflows.py`) und Workflow-Validierung (`scripts/validate-workflows.py`).

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Restore | `dotnet restore Tankradar.sln -p:Configuration=Release` | Repository-Wurzel | 0 | – | – | – | Konsolenausgabe, nicht als Datei gesichert |
| Build | `dotnet build Tankradar.sln --configuration Release --no-restore` | Repository-Wurzel | 0 | – | – | – | 0 Warnungen, 0 Fehler (Konsolenausgabe, nicht als Datei gesichert) |
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory …/inventory/test-results/unit --logger "trx;LogFileName=unit.trx"` | Repository-Wurzel | 0 | 1940 | 0 | 0 | [Log](test-results/unit/unit.log), [TRX](test-results/unit/unit.trx), Coverage unter `test-results/unit/` |
| Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --settings coverlet.runsettings --collect:"XPlat Code Coverage" --results-directory …/inventory/test-results/integration --logger "trx;LogFileName=integration.trx"` | Repository-Wurzel | 0 | 291 | 0 | 0 | [Log](test-results/integration/integration.log), [TRX](test-results/integration/integration.trx), Coverage unter `test-results/integration/` |
| Node-Skripttests | `npm test` (`node --test "scripts/*.test.mjs"`) | Repository-Wurzel | 0 | 32 | 0 | 0 | [Log](test-results/node-tests.log) |
| Python-Tests | `python scripts/test_validate_workflows.py` | Repository-Wurzel | 0 | 14 | 0 | 0 | [Log](test-results/python-workflow-tests.log) |
| Workflow-Validierung | `python scripts/validate-workflows.py` | Repository-Wurzel | 0 | – (Prüfung: 7 Workflows OK) | – | – | [Log](test-results/python-workflow-validation.log). Hinweis: actionlint war nicht im PATH, nur die eingebauten Prüfungen liefen. |
| E2E (FlaUI) | nicht ausgeführt | – | unbekannt | unbekannt | unbekannt | unbekannt | – |

Die Zahlen der Unit- und Integrationsläufe stammen aus den TRX-Dateien (`Counters`: total = executed = passed, failed = 0, notExecuted = 0).

### Nachgewiesene bestehende Testfehler

Keine. Im Ausgangslauf sind keine Testfehler nachgewiesen. Die Unit- und Integrationsläufe und die Skripttests waren vollständig grün.

### Testlücken und Ausführungsprobleme

- **E2E-Tests nicht ausgeführt.** Die FlaUI-Tests (`src/Tankradar.Tests.E2E`) benötigen eine interaktive Desktop-Sitzung. Die Anzahl der E2E-Tests ist nicht ermittelt. Betroffen sind unter anderem die Einstellungs-Tests `SettingsE2ETests_Defaults` (u. a. `FirstStart_ShowsDefaultValues`), `SettingsE2ETests_FuelTypes` (u. a. `FuelTypeSelectionAndOrder_SurviveRestart`, `LastSelectedFuelType_CannotBeDeselected_ShowsMessage`) und `SettingsE2ETests_Persistence`. Für die Anforderung sind diese Tests relevant, weil ein Erststart-Dialog die Annahmen in `FirstStart_ShowsDefaultValues` berührt. Ein Ausgangsnachweis für den E2E-Bereich fehlt daher.
- **Build-Log nicht als Datei gesichert.** Restore und Build liefen grün, die Ausgabe wurde nur auf der Konsole gelesen.
- **Coverage-Schwelle nicht geprüft.** Die Coverage-Dateien liegen vor, `reportgenerator` und `scripts/check-coverage.mjs` wurden nicht ausgeführt.
- **Nicht ausgeführte Prüfungen außerhalb der Tests:** `dotnet format --verify-no-changes`, Sicherheitsprüfung (`check-vulnerabilities.mjs`) und iOS-Kompilierung (kein Mac vorhanden).
- **Pfadlänge in Testergebnissen.** Die Integrationsläufe erzeugen unter `test-results/integration/` sehr tiefe Verzeichnisse. Git meldet dafür `Filename too long`. Beim Einchecken des Inventar-Verzeichnisses kann das stören.
- **Keine Setup-Tests.** Für die Anforderung existieren noch keine Tests für Erststart-Erkennung, ein Setup-ViewModel oder einen Dialog.

Logs und Reports liegen unter `test-results/` (`unit/`, `integration/`, `node-tests.log`, `python-workflow-tests.log`, `python-workflow-validation.log`). Die Logs wurden auf Geheimnisse geprüft. Die einzigen Treffer waren Testnamen wie `ApiKeyProviderTests_Resolution`, keine Werte.

## Testklassen

### Bestehende Testklassen mit Bezug zu den Einstellungen (Unit)

Pfad: `src/Tankradar.Tests.Unit/Unit/`

- `AppSettingsTests_Defaults`
  - `CreateDefault_HasExpectedScalarValues`: Standardwerte der Skalare
  - `Defaults_ShowNearbyIsFalse`: „In der Nähe“ ist standardmäßig aus
  - `CreateDefault_SelectsAllFuelTypesInOrder`: alle Sorten ausgewählt, Reihenfolge E5, E10, Diesel
  - `CreateDefault_IsValid`: Standardwerte bestehen `Validate()`
- `AppSettingsTests_Validation`
  - `Normalize_AppendsMissingFuelTypesUnselected`, `NormalizeFuelTypes_KeepsOrderAndAppendsMissing`, `Normalize_CompleteList_StaysUnchanged`: Ergänzung fehlender Sorten
  - `Validate_DuplicateFuelType_Throws`, `Validate_NoSelectedFuelType_Throws`, `Validate_UndefinedFuelType_Throws`, `Validate_UndefinedEnumValues_Throw`: Validierungsregeln, inklusive „mindestens eine Sorte“
  - `Validate_ShowNearby_IsAccepted`: Theory für `ShowNearby`
- `SettingsServiceTests_LoadSave`
  - `LoadAsync_WithoutData_ReturnsDefaults`: Verhalten ohne gespeicherte Daten (Basis für die Erststart-Erkennung)
  - `SaveThenLoad_RoundTrips_*`: Round-Trip für Enums, `ShowNearby` und Sorten-Reihenfolge/Auswahl
  - `Load_WithoutStoredValue_ShowNearbyFalse`, `Load_InvalidStoredValue_FallsBackToDefault`, `Load_UnknownFuelTypeKey_IsIgnoredAndKept`, `Load_NewFuelTypeMissingInDatabase_AppendedUnselected`: Fallbacks
  - `Save_ZeroSelected_Throws`: Validierung beim Speichern
  - `Save_Twice_UpdatesSameRows`: kein Duplikat beim zweiten Speichern
- `SettingsViewModelTests_Loading`
  - `LoadAsync_PopulatesAllSections`, `LoadAsync_ServiceThrows_ShowsStatusMessageAndKeepsDefaults`, `LoadAsync_ServiceThrows_LogsException`, `OnAppearing_StartsLoad`, `LoadAsync_DoesNotTriggerSave`
- `SettingsViewModelTests_Changes`
  - `SelectGps_SavesImmediately`, `SelectView_SavesImmediately`, `SelectSort_SavesImmediately`, `ToggleFuel_SavesImmediately`: sofortiges Speichern
  - `MoveUp_ChangesOrderAndSaves`, `MoveDown_ChangesOrderAndSaves`, `CanMove_FirstAndLast`: Reihenfolge der Spritsorten
  - `DeselectLastFuel_IsRevertedWithMessageAndNotSaved`: letzte Sorte bleibt ausgewählt, mit Meldung
  - `Save_Fails_ShowsStatusMessage`, `Save_Fails_LogsException`: Fehlerpfad
- `SettingsViewModelTests_Nearby`, `SettingsViewModelTests_Backup`, `SettingsViewModelTests_RouteSearch`: angrenzende Karten der Optionen-Seite (nicht im Detail untersucht)
- `SettingsEnumTests_Persistence`
  - `FuelType_HasStablePersistenceName`, `GpsUsage_HasStablePersistenceName`, `ResultView_HasStablePersistenceName`, `ResultSortOrder_HasStablePersistenceName`: Theories mit den Persistenznamen
  - `Enums_HaveExpectedValueCounts`: Anzahl der Werte
- `SettingsTextsTests_Labels`: Texte der Einstellungen (nicht im Detail untersucht)

### Bestehende Integrationstests (Integration)

Pfad: `src/Tankradar.Tests.Integration/Integration/`

- `SettingsServiceTests_RestartPersistence`
  - `Save_ThenNewServiceInstance_LoadsSameValues`: Werte überstehen den Neustart (neue Factory und Dienst nach Leeren der Pools)
  - `Save_GpsUsage_RoundTripsThroughFile`, `Save_ShowNearby_RoundTripsThroughFile`, `Save_ResultView_RoundTripsThroughFile`, `Save_ResultSortOrder_RoundTripsThroughFile`: Theories über die Datei
  - `Save_SingleSelectedFuelType_RoundTripsThroughFile`: jede einzelne Sorte bleibt erhalten
- `DatabaseInitializerTests_FirstStart`
  - `Initialize_FirstStart_CreatesDatabaseFileAndSchema`: Erststart der Datenbank (Datei und Schema)
- `DatabaseInitializerTests_Upgrade`
  - `Initialize_FromOlderSchemaVersion_KeepsData`, `Initialize_Twice_IsIdempotent`
- Weitere Migrationstests mit Bezug zu `UserSettings`: `NearbySettingMigrationTests_Upgrade`

## Hilfsmethoden

### `FakeSettingsService` (Unit, `src/Tankradar.Tests.Unit/Unit/Support/FakeSettingsService.cs`)
- Hält einen Stand im Speicher (Standard `AppSettings.CreateDefault()`), protokolliert `LoadCount` und `Saved`.
- Kann `LoadException` und `SaveException` auslösen, um Fehlerpfade zu testen.

### `TestDatabase` (Integration, `src/Tankradar.Tests.Integration/Integration/Support/TestDatabase.cs`)
- Testdatenbank, die die Klasse per `IDisposable` nach dem Test aufräumt. Wird von `SettingsServiceTests_RestartPersistence` verwendet. Details der Implementierung nicht untersucht.

### `TestDataContext` (Integration, `src/Tankradar.Tests.Integration/TestDataContext.cs`)
- Gemeinsamer Testdatenkontext für Integrationstests (Details nicht untersucht).

### E2E-Hilfsklasse `SettingsE2ETestBase` (`src/Tankradar.Tests.E2E/E2E/FlaUI/SettingsE2ETestBase.cs`)
- Öffnet die Optionen-Seite und wartet auf geladene Einstellungen. Stellt Bedienhilfen für Schalter, Auswahlfelder und Schaltflächen bereit. Enthält `DefaultFuelTypeOrder` (`SuperE5`, `SuperE10`, `Diesel`). Nicht ausgeführt.
