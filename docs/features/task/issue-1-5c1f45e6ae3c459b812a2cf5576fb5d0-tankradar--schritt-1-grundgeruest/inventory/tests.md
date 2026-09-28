## Test-Ausgangszustand vor der Umsetzung

- **Zeitpunkt:** 2026-09-28, 22:26 Uhr (Europe/Berlin, UTC+02:00)
- **Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-1-grundgeruest`
- **Commit-ID:** `c9979c5d885f282122fb10dce2f1ac32df6fa4e4` ("feat: Tankradar Schritt 1 - MAUI-Grundgeruest mit Navigation und Design-System")
- **Uncommittete Änderungen im getesteten Stand:** Nur neue, untracked Dokumentationsdateien
  (kein Produktiv- oder Testcode geändert):
  - `docs/features/task/issue-1-5c1f45e6ae3c459b-812a-2cf5576fb5d0-tankradar--schritt-1-grundgeruest/requirement.md` (abweichender Pfad mit Bindestrich in der UUID)
  - `docs/features/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-1-grundgeruest/requirement.md`
  - `docs/features/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-1-grundgeruest/todo.md`
  - `docs/projects/task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar/acceptance-schritt-1.md`
  - `git diff` gegen `HEAD` für `src/` ist leer; der getestete Codestand entspricht exakt Commit `c9979c5`.
- **Testumgebung / Runtime- und SDK-Versionen:**
  - OS: Windows 11 Pro, Build 10.0.26200 (win-x64)
  - .NET SDK: `10.0.401` (Workload-Version `10.0.401`, MSBuild `18.9.11+e34a38d2a`)
  - Installierte MAUI-relevante Workloads: `android` (Manifest `36.1.69/10.0.100`), `ios`
    (`26.5.10318/10.0.100`), `maccatalyst` (`26.5.10318/10.0.100`), `maui-windows` (`10.0.20/10.0.100`)
  - Testframework: xUnit `2.9.3` mit `xunit.runner.visualstudio` `3.1.4`, `Microsoft.NET.Test.Sdk`
    `17.14.1` (alle drei Testprojekte)
  - E2E-Tests: FlaUI `5.0.0` (`FlaUI.Core`, `FlaUI.UIA3`) gegen die gebaute Windows-App
    (`net10.0-windows10.0.19041.0`, win-x64)
- **Ermittelte Testsuiten und Quellen der Testbefehle:** `README.md`, Abschnitt „Tests ausführen“
  gibt `dotnet test Tankradar.sln` als projektweiten Testbefehl vor; dieser deckt laut README alle
  drei Testprojekte ab (Unit, Integration, E2E). Es existiert kein separater CI-Workflow
  (`.github/workflows/` wurde geprüft — nicht vorhanden) und kein zusätzliches Test-Skript unter
  `scripts/`. Die Solution `Tankradar.sln` enthält vier Projekte: `Tankradar.MAUI`,
  `Tankradar.Tests.Unit`, `Tankradar.Tests.Integration`, `Tankradar.Tests.E2E`.

### Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| 1 (Vollständig, alle Projekte) | `dotnet test Tankradar.sln --logger "trx;LogFileName=full-test-run.trx" --logger "console;verbosity=normal"` | Repository-Root | 0 | 8 (3 Integration + 4 Unit + 1 E2E) | 0 | 0 | [Konsolen-Log](test-results/dotnet-test-full-run.txt), [Unit-TRX](test-results/unit-full-test-run.trx.xml), [Integration-TRX](test-results/integration-full-test-run.trx.xml), [E2E-TRX](test-results/e2e-full-test-run.trx.xml) |

Hinweis zur Dateibenennung der Nachweise: Das Repository-`.gitignore` schließt `*.trx` und `*.log`
global aus; damit die Nachweisdateien unter `inventory/test-results/` versioniert werden können,
wurden sie mit den Endungen `.txt` bzw. `.trx.xml` abgelegt (Inhalt unverändert: TRX-Dateien sind
Standard-XML, das Konsolen-Log ist reiner Text).

Der Lauf hat implizit auch `Tankradar.MAUI` für alle konfigurierten Zielplattformen
(`net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows10.0.19041.0`) gebaut
(sichtbar im Konsolen-Log), da `Tankradar.Tests.E2E` die gebaute Windows-`.exe` benötigt. Der Build
war für alle Zielplattformen erfolgreich.

### Nachgewiesene bestehende Testfehler

Keine. Alle 8 im Ausgangslauf ausgeführten Tests sind grün (0 Fehlschläge, Exit-Code 0).

### Testlücken und Ausführungsprobleme

- Keine übersprungenen, deaktivierten oder abgebrochenen Tests im Ausgangslauf.
- Keine Build-, Setup- oder Infrastrukturfehler.
- Für den in der Anforderung beschriebenen neuen Datenverzeichnis-Service existiert noch **keine**
  Testklasse (siehe unten) — das ist keine Testlücke im bestehenden Code, sondern der erwartete
  Zustand vor der Umsetzung, da der Service selbst noch nicht existiert.
- Für die geforderten seitenspezifischen `AutomationId`-Assertions in `NavigationE2ETests` existiert
  aktuell keine entsprechende Prüfung (siehe „Testklassen“ unten); der bestehende E2E-Test ist grün,
  prüft aber nur die schwächere Assertion `ByName(tabTitle)`.

## Testklassen

### `BaseViewModelTests_PropertyBinding`
Datei: `src/Tankradar.Tests.Unit/Unit/BaseViewModelTests_PropertyBinding.cs`
Basisklasse: `BaseTest` (`src/Tankradar.Tests.Unit/BaseTest.cs`)
- `Constructor_SetsTitle` — prüft, dass `FavoritesViewModel` im Konstruktor bereits `Title == "Favoriten"` setzt.
- `Constructor_StartsNotBusy` — prüft `IsBusy == false` und `IsNotBusy == true` nach Konstruktion.
- `SetIsBusy_RaisesPropertyChangedForIsBusyAndIsNotBusy` — prüft, dass Setzen von `IsBusy` `PropertyChanged` für `IsBusy` **und** `IsNotBusy` auslöst.
- `SetTitle_SameValue_DoesNotRaisePropertyChanged` — prüft, dass erneutes Setzen desselben `Title`-Werts kein `PropertyChanged` auslöst.

Kein Bezug zu Fonts, Shell-Styling oder Datenverzeichnis — einzige bestehende Testklasse im Projekt
`Tankradar.Tests.Unit`. Dient als Namensschema-Vorbild (`{Klasse}Tests_{Thema}`) für die in der
Anforderung geforderte neue Unit-Testklasse zum Datenverzeichnis-Service.

### `TestDataContextTests_Lifecycle`
Datei: `src/Tankradar.Tests.Integration/Integration/TestDataContextTests_Lifecycle.cs`
- `Constructor_CreatesIsolatedDataDirectory` — prüft, dass beim Erstellen von `TestDataContext` das Verzeichnis `DataDirectory` existiert.
- `Cleanup_RemovesDataDirectory` — prüft, dass `Cleanup()` das Verzeichnis wieder entfernt.
- `Constructor_CreatesUniqueDirectoryPerInstance` — prüft, dass zwei `TestDataContext`-Instanzen unterschiedliche `DataDirectory`-Werte erhalten.

Testet `TestDataContext` (Integrationstest-Infrastruktur, liest ebenfalls
`TestDataPaths.TestDataPathEnvironmentVariable`), nicht die MAUI-App selbst. Kein Bezug zu
`Tankradar.MAUI.Services`.

### `NavigationE2ETests`
Datei: `src/Tankradar.Tests.E2E/E2E/FlaUI/NavigationE2ETests.cs`
Basisklasse: `E2ETestBase` (`src/Tankradar.Tests.E2E/E2ETestBase.cs`)
- `AppStartsAndNavigatesThroughAllTabs` — startet die App, klickt nacheinander die vier Tabs
  (`Favoriten`, `Karte`, `Tankbuch`, `Optionen`) über `FindTab(title)` (sucht per `ByName(title)`
  oder `ByAutomationId(title)`), prüft nach jedem Klick
  `MainWindow.FindFirstDescendant(cf => cf.ByName(tabTitle))` (Namensgleichheit mit dem Tab-Titel,
  **nicht** `AutomationId`) und klickt abschließend erneut auf „Favoriten“ zur Rückkehr-Prüfung.
  Dies ist genau die in der Anforderung als schwach bezeichnete Assertion — sie kann sowohl das
  Tab-Element selbst als auch ein gleichnamiges Seiten-Element treffen, da beide denselben Namen
  („Favoriten“ etc.) tragen und es aktuell keine `AutomationId`-Differenzierung gibt.

## Hilfsmethoden

### `E2ETestBase`
Datei: `src/Tankradar.Tests.E2E/E2ETestBase.cs`
- Konstruktor (`protected E2ETestBase()`) — legt ein temporäres, GUID-benanntes Verzeichnis unter
  `%TEMP%\Tankradar.Tests.E2E\<guid>` an, setzt es vor App-Start über
  `startInfo.Environment[TestDataPaths.TestDataPathEnvironmentVariable]` und startet
  `Tankradar.MAUI.exe` per FlaUI (`Application.Launch`). Stellt `MainWindow` (`Window`) für Subklassen bereit.
- `Dispose()` — schließt die App, gibt `UIA3Automation` frei und löscht das temporäre Testdatenverzeichnis (Best-effort bei `IOException`).
- `ResolveAppPath()` (privat) — ermittelt den Pfad zur gebauten `Tankradar.MAUI.exe` relativ zu `AppContext.BaseDirectory`; wirft `FileNotFoundException` mit Hinweis auf notwendigen vorherigen Build, falls die Datei fehlt.

Wichtig für die Anforderung: `E2ETestBase` setzt die Umgebungsvariable `TEST_DATA_PATH` bereits
heute zuverlässig vor App-Start — die App selbst liest sie aber noch nicht (kein entsprechender
Service in `Tankradar.MAUI`, siehe [`logic.md`](logic.md)).

### `TestDataContext`
Datei: `src/Tankradar.Tests.Integration/TestDataContext.cs`
- Konstruktor — ruft `ResolveDataDirectory()` und `Initialize()` auf.
- `Initialize()` — legt `DataDirectory` an, falls nicht vorhanden.
- `Cleanup()` — löscht `DataDirectory` rekursiv.
- `Dispose()` — ruft `Cleanup()` auf (idempotent über `_disposed`-Flag).
- `ResolveDataDirectory()` (privat, `static`) — liest `TestDataPaths.TestDataPathEnvironmentVariable`;
  falls leer/nicht gesetzt, Fallback auf `%LocalAppData%\Tankradar.Tests`; hängt in jedem Fall ein
  neues `Guid.NewGuid().ToString("N")`-Unterverzeichnis an.

### `BaseTest`
Datei: `src/Tankradar.Tests.Unit/BaseTest.cs`
- Abstrakte `IDisposable`-Basisklasse mit Standard-Dispose-Pattern (`Dispose()` → `Dispose(bool)`,
  `_disposed`-Flag). Keine testdatenbezogenen Hilfsmethoden; aktuell nur Aufräum-Grundgerüst ohne
  weitere Funktionalität.
