## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt: 2026-10-12, ca. 13:15 (lokale Zeit des Rechners, MEZ/MESZ laut Systemuhr)
- Branch und Commit-ID: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-15-sicherung`, `42b47107731a4028ba5cda382dd96a0e51007a42`
- Uncommittete Änderungen im getesteten Stand: nur das unversionierte Verzeichnis `docs/features/` (Dokumentation); kein Code und keine Tests verändert
- Testumgebung: Windows 11 Pro 10.0.26200, .NET SDK 10.0.401, Konfiguration Release, Ziel `net10.0-windows10.0.19041.0`
- Ermittelte Testsuiten und Quellen: `scripts/local-ci.ps1` (Schritte „Tests“ je Projekt Unit, Integration und FlaUI-E2E mit `dotnet test --configuration Release`), README „Tests ausführen“

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Unit | `dotnet test src/Tankradar.Tests.Unit --configuration Release --logger "trx;LogFileName=Unit.trx"` (ohne Filter) | Repository-Root | 0 | 1126 | 0 | 0 | [Log](test-results/Unit.log), [TRX](test-results/Unit.trx) |
| Integration | `dotnet test src/Tankradar.Tests.Integration --configuration Release --logger "trx;LogFileName=Integration.trx"` (ohne Filter) | Repository-Root | 0 | 122 | 0 | 0 | [Log](test-results/Integration.log), [TRX](test-results/Integration.trx) |
| FlaUI-E2E | nicht ausgeführt | – | – | unbekannt | unbekannt | unbekannt | – |

### Nachgewiesene bestehende Testfehler

Keine: In den ausgeführten Läufen (Unit, Integration) ist kein Test fehlgeschlagen. Über die E2E-Suite wird keine Aussage getroffen.

### Testlücken und Ausführungsprobleme

- FlaUI-E2E-Suite (`src/Tankradar.Tests.E2E`) nicht ausgeführt: Sie benötigt einen Release-Build der MAUI-Windows-App und eine Desktop-Sitzung und läuft mehrere Minuten; für die Bestandsaufnahme zurückgestellt. Referenz ohne Beweiskraft für diesen Lauf: Abnahme Schritt 14 meldete E2E 97/98 (einziger Fehlschlag „Screenshot einfarbig“ wegen ausgeschaltetem Bildschirm). Ein E2E-Fehler darf daher nur als vorhanden gelten, wenn ein späterer Ausgangslauf ihn belegt.
- Keine Build-, Setup- oder Infrastrukturfehler in den ausgeführten Läufen.
- Für Sicherung/Wiederherstellung existieren keine Tests (kein Code vorhanden).

## Testklassen (relevant für den Bereich)

### Datenbank und Migration (Integration, `src/Tankradar.Tests.Integration/Integration/`)
- `DatabaseInitializerTests_FirstStart`, `DatabaseInitializerTests_Upgrade` — Erststart und Update bestehender Installationen (Daten bleiben erhalten).
- `VehiclesMigrationTests_Upgrade`, `MigrationsTests_ModelSync` — Migration auf älteren Stand und Abgleich Modell/Snapshot (Muster für den Test „Sicherung älterer Version“).
- `VehicleServiceTests_RealDatabase`, `TankbookEntryServiceTests_RealDatabase`, `TankbookStatisticsServiceTests_RealDatabase`, `SettingsServiceTests_RestartPersistence` — echte SQLite-Datei.
- `TestDatabaseTests_Pooling`, `TestDataContextTests_Lifecycle` — Hilfsklassen.

### Texte, Selektoren, ViewModels (Unit, `src/Tankradar.Tests.Unit/Unit/`)
- `ReceiptPhotoPickerSelectorTests_TestMode` — Muster für Selector-Tests.
- `TankbookTextsTests_Labels`, `TankbookViewModelTests_Delete` — Text und Ablauf der Fahrzeug-Löschrückfrage (werden durch den Sicherungshinweis berührt).
- `TankbookEntryTextsTests_Format`, `TankbookEntriesViewModelTests_Delete`.
- `ReceiptPhotoStore`-Tests (u. a. `DetectExtension`) im Unit-Projekt.

### E2E (`src/Tankradar.Tests.E2E/E2E/FlaUI/`)
- `SettingsE2ETests_Defaults`, `_FuelTypes`, `_Persistence`, `_NearbyPersistence` — Optionen-Seite.
- `TankbookE2ETests_DeleteWithEntries`, `TankbookE2ETests_Flow` — Löschrückfrage mit Vorgängen (Textprüfung).
- `TankbookEntriesE2ETests_Receipt` — Belegfoto über `TANKATLAS_TEST_RECEIPT_PHOTO`.
- `TankbookStatisticsE2ETests_Flow`, `_Months`, `_Hybrid`, `_Fallback`, `_Partial`, `_Update`, `_Persistence` — Auswertung/Kacheln.
- `StartFailureCleanupE2ETests`, `NavigationE2ETests`, `OfflineE2ETests_Tankbook`.

## Hilfsmethoden

### `E2ETestBase` (`src/Tankradar.Tests.E2E/E2ETestBase.cs`)
- Startet die App mit isoliertem `TestDataDirectory` (`%TEMP%\Tankradar.Tests.E2E\<Guid>`), `NavigateToTab`, `RunWithDiagnostics`, `RestartApplication()` (optional mit zusätzlichen Umgebungsvariablen), Konstruktor mit `additionalEnvironment`.

### `SettingsE2ETestBase` (`.../FlaUI/SettingsE2ETestBase.cs`)
- `OpenSettings`, `WaitForAutomationId`, `Exists`, `SelectRadio`, `ToggleFuelType`, `SetFieldValue(automationId, text)` (Eingabe per UI Automation mit Absicherung vor dem Speichern).

### `TankbookE2ETestBase` / `TankbookE2EDriver` / `TankbookTestDatabase`
- `OpenTankbook`, `CreateVehicle`, `Press`, `PressForVehicle`; `TankbookTestDatabase.AddEntries/AddEntry/CountEntries/Open` greifen ohne Pool direkt auf die SQLite-Testdatenbank zu (`DatabasePath`).

### Integration: `Support/TestDatabase`
- Isoliertes Datenverzeichnis mit `DatabasePath`, `CreateFactory()` (Kontext ohne Pool), Initializer-Fabrik; `TestDataContext` für das Verzeichnis.

### `src/TestSupport`
- `TestDataPaths` (Namen der Testmodus-Variablen), `MockTankerkoenigServer`, `MockNominatimServer`, `MockTileServer`, `TransientRetry`, `RecordingLogger`.
