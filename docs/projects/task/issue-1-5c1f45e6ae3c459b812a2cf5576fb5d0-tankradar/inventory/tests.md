# Tests: Tankradar

## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt (mit Zeitzone):** 2026-09-28 18:36 UTC+02:00
- **Branch und Commit-ID:** Branch: `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar`, Commit: `2aebc74` (Initial commit)
- **Uncommittete Änderungen im getesteten Stand:** `.gitignore` (modified), `design-draft/` (untracked), `docs/` (untracked)
- **Testumgebung und Runtime-/SDK-Versionen:**
  - OS: Windows 11 Pro 10.0.26200
  - Shell: PowerShell und Bash verfügbar
  - dotnet --info wird überprüft (siehe unten)
  - MAUI Workload-Status wird überprüft (siehe unten)

- **Ermittelte Testsuiten und Quellen der Testbefehle:**
  - Es gibt keine Test-Projekte im Repository
  - Keine *.csproj oder *.sln Dateien vorhanden
  - Keine Test-Runner-Konfiguration (pytest, NUnit, xUnit, etc.) vorhanden
  - Keine CI/CD-Workflows im `.github/workflows/` vorhanden

### Verfügbare Build- und Runtime-Umgebung

```powershell
PS > dotnet --info
```

(Wird zur Dokumentation der verfügbaren .NET-Version und installierten Workloads ausgeführt)

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1    | N/A - Keine Testprojekte vorhanden | N/A | N/A | 0 | 0 | 0 | [Ermittlung](#ermittlung) |

### Ermittlung

Das Repository enthält **keine Test-Projekte** und keine ausführbaren Tests:

- Keine Dateien mit Mustern `*.Tests.csproj`, `*.Test.csproj`
- Keine Testklassen in `Tests/`, `test/`, `*.Tests/` Verzeichnissen
- Keine Test-Runner-Konfigurationsdateien (`.nunit`, `xunit.runner.json`, etc.)
- Keine NUnit-, xUnit-, Moq- oder ähnliche Test-Framework-Abhängigkeiten deklariert

**Feststellung:** Das Projekt befindet sich im Initialzustand. Die Implementierung von Tests folgt erst nach der Implementierung der Produktivcode-Klassen.

### Nachgewiesene bestehende Testfehler

**Keine Tests vorhanden** — daher können keine Testfehler nachgewiesen werden. Dies ist kein Fehlerzustand, sondern der erwartete Zustand eines neu initialisierten Projekts vor der Implementierung.

### Testlücken und Ausführungsprobleme

#### Keine Tests vorhanden

Die gesamte Test-Suite ist noch nicht implementiert. Geplante Test-Suiten gemäß Anforderung:

- **Unit Tests:**
  - ConsumptionCalculationTests
  - StatisticsServiceTests
  - FavoriteGroupServiceTests
  - PriceCacheServiceTests
  - ValidationTests
  - OfflineModeTests
  - BackupRestoreTests

- **Integration Tests:**
  - FuelPriceApiIntegrationTests
  - ElectricityChargeApiIntegrationTests
  - DatabaseIntegrationTests
  - LocationServiceIntegrationTests
  - BackupRestoreIntegrationTests

- **E2E Tests (FlaUI – Windows only):**
  - SearchFlowTests
  - FavoritesManagementTests
  - LogbookRecordingTests
  - StatisticsExportTests
  - OfflineModeScenarioTests

**Grund:** Das Projekt wurde gerade initialisiert und befasst sich zunächst mit der Anforderungsanalyse. Testprojekte und Test-Implementierungen folgen in der Phase 6 der Phasierung (siehe requirement.md).

## Testklassen

**Keine Testklassen vorhanden.**

## Hilfsmethoden

**Keine Hilfsmethoden vorhanden.**
