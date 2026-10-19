# Tests

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-10-19, 18:10 bis 18:11 (+02:00), Läufe 1 und 2.
- Branch und Commit-ID: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-16-export`, HEAD `39f6d7c`.
- Uncommittete Änderungen im getesteten Stand: nur nicht verfolgte Dateien unter `docs/features/` (Anforderung und Bestandsaufnahme); kein Produktiv- oder Testcode geändert.
- Testumgebung: Windows 11 Pro 10.0.26200, .NET SDK 10.0.402, Konfiguration `Release`, `FUEL_PRICE_API_KEY` per `env -u` entfernt, Zielframeworks wie im CI-Windows-Job ausgeblendet (`IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false`, `IncludeWindowsTarget=true`; Quelle `scripts/local-ci.ps1`).
- Ermittelte Testsuiten und Quellen: `Tankradar.Tests.Unit`, `Tankradar.Tests.Integration`, `Tankradar.Tests.E2E` (FlaUI), aus `scripts/local-ci.ps1` und `.github`-Workflows.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 0a Unit ohne Ausblenden | `dotnet test src/Tankradar.Tests.Unit/Tankradar.Tests.Unit.csproj -c Release` | Repository-Root | 1 | unbekannt | unbekannt | unbekannt | [Log](test-results/lauf0-unit-ohne-ausblenden.log) (Restore-Fehler `NETSDK1147`, Workload `mobile-librarybuilder` für `net10.0-android`; Umgebungsfehler, kein Testfehler) |
| 0b Integration ohne Ausblenden | wie 0a mit `Tankradar.Tests.Integration` | Repository-Root | 1 | unbekannt | unbekannt | unbekannt | [Log](test-results/lauf0-integration-ohne-ausblenden.log) (gleicher Umgebungsfehler) |
| 1 Unit | mit den Include-Variablen oben: `dotnet test src/Tankradar.Tests.Unit/Tankradar.Tests.Unit.csproj -c Release --logger "trx;LogFileName=Unit.trx"` | Repository-Root | 0 | 1345 | 0 | 0 | [Log](test-results/Unit.log), [TRX](test-results/Unit.trx) |
| 2 Integration | wie Lauf 1 mit `Tankradar.Tests.Integration` | Repository-Root | 0 | 241 | 0 | 0 | [Log](test-results/Integration.log), [TRX](test-results/Integration.trx) |

### Nachgewiesene bestehende Testfehler

Keine in den Läufen 1 und 2. Die Fehler der Läufe 0a/0b sind Restore-/Workload-Fehler der lokalen Umgebung bei nicht ausgeblendeten Zielframeworks, keine Testfehler.

### Testlücken und Ausführungsprobleme

- **E2E (`Tankradar.Tests.E2E`) nicht ausgeführt.** Grund: Die FlaUI-Suite braucht rund 11 Minuten und startet die App im Off-Screen-Fenster; für diese Bestandsaufnahme wurde sie nicht gelaufen. Zur Orientierung, ohne Beweiskraft für diesen Ausgangslauf: Die Abnahme von Schritt 15a meldet E2E 114/114 am Stand `64a06ef`. Über E2E-Fehler im aktuellen Stand wird keine Aussage getroffen.
- iOS-/Android-Build und `local-ci.ps1` nicht ausgeführt.
- Es gibt keine Tests zu Export, PDF, CSV oder Zeitraumfilter und keinen NUL-Regressionstest der Sicherung.

## Testklassen (für Schritt 16 relevant)

### Unit (`src/Tankradar.Tests.Unit/Unit/`)
- `TankbookStatisticsCalculatorTests_Average/_Cost/_FromStart/_Months/_Threshold/_Trend` – Durchschnitt, Kosten pro 100 km, Startintervall, Monatsreihe, Schwellen, Verlauf (Grundlage der Berichts-Kennzahlen).
- `TankbookStatisticsServiceTests_Read`, `TankbookStatisticsViewModelTests_Load/_Limits`, `TankbookStatisticsTextsTests_Format/_Messages` – Dienst, ViewModel, Textformate (de-DE).
- `TestBackupSharerTests`, `BackupSharerSelectorTests_TestMode` – Muster für Test-Sharer und Selektor.
- `BackupViewModelTests_ShareImport`, `BackupViewModelTestBase`, `Support/FakeBackupPlatform.cs` – Muster für ein ViewModel mit Fake-Sharer.
- `E2EInputPolicyTests_NoFocus` – Quelltextprüfung der E2E-Quellen (kein Fokus, keine Tastatur oder Maus).

### Integration (`src/Tankradar.Tests.Integration/Integration/`)
- `BackupServiceTests_*` (`Atomicity`, `Create`, `CustomName`, `ListDeleteImport`, `Rebuild`, `Resilience`, `Restore`, `Schema`, `SourceFile`, `Storage`, `Versions`), Support `BackupTestEnvironment`.

### E2E (`src/Tankradar.Tests.E2E/E2E/FlaUI/`)
- Basisklassen: `E2ETestBase` (Start, `RunWithDiagnostics`, `SetFieldValue` über `ValuePattern`, `TestDataDirectory`), `SettingsE2ETestBase`, `TankbookE2ETestBase`, `BackupE2ETestBase` (`SharedDirectory` = `test-shared`), `TankbookE2EDriver`.
- `BackupE2ETests_Share` – Teilen im Testmodus kopiert nach `test-shared`, ohne Dialog (Muster für den Export).
- `TankbookStatisticsE2ETests_TileNumbers` – Kachelzahlen einzeilig und im Fenster; prüft `BoundingRectangle.Height <= fontSize * 2.0` nach `EnsureVisible`, aber nicht die volle Sichtbarkeit der Elementhöhe (Befund H1).
- `TankbookEntriesE2ETests_*`, `TankbookStatisticsE2ETests_*`, `TankbookE2ETests_*`, `OfflineE2ETests_Tankbook` – Vorgangs- und Auswertungsabläufe.
- Die E2E-Projekte bedienen bisher keinen `DatePicker`; eine UIA-Hilfe zur Datumsauswahl fehlt.

## Hilfsmethoden

- `TankbookEntryFactory` (`Support`): `Start`, `CreateAt(id, occurredAt, odometerKm, quantity, totalAmount, isPartial, kind)`, `Statistics(entries, fuelKind, startConsumption, today, fromStart)`.
- `TempDirectory` (`Combine`), `FakeBackupPlatform`, `FakeBackupService`, `StubTankbookEntryService`, `ManualTimeProvider`, `InMemoryDbContextFactory`, `VehicleFixture`, `TankbookEntryFixture`.
- E2E `TankbookE2EDriver`: `CreateVehicle`, `OpenEntries`, `CreateEntry(station, quantity, unitPrice, total, odometer, isPartial)`, `Press`, `ReadTexts`, `WaitForStatistic(s)`, `StatisticsId`; `TankbookE2ETestBase.DatabasePath`.
