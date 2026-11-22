# Umsetzungsplan: Flaky-Test-Härtung — OsInterface-Lane-Trennung, Deadline-Angleichung, Cleanup, Flaky-Retry (Issue #93)

## Übersicht

Die Test- und CI-Infrastruktur wird um eine Lane-Trennung für umgebungssensitive Tests erweitert:
Tests mit echter Betriebssystem-Schnittstellen-Berührung (FlaUI/UI-Automation, echte Prozessstarts,
Loopback-`HttpListener`) erhalten die xUnit-Kategorie `Category=OsInterface` und laufen in einem neuen,
nicht blockierenden CI-Job (`os-interface-tests`, `continue-on-error`). Die blockierende Lane filtert
`Category!=OsInterface` und bleibt deterministisch. Ergänzt werden: Angleichung der E2E-Host-Poll-Deadlines
an das UIA-Kind-Budget, gehärtetes Temp-Verzeichnis-Cleanup (`TempDirectoryCleanup`), eine
`Flaky`-Markierung mit genau einem automatisierten Re-Run (`scripts/Retry-FlakyTests.ps1`) und eine
TRX-basierte Flake-Statistik (`scripts/Analyze-FlakyTestResults.ps1`). Umsetzung nach der Blaupause
`martin-stromberg/Softwareschmiede` PR #432, adaptiert auf xUnit 2.9.3 (kein `ITraitAttribute`,
eingebautes `[Trait("Category", …)]`) und auf die Tankradar-Projektstruktur.

## Scope-Klärung

Verbindliche fachliche Abgrenzung — übernommen und konkretisiert aus `requirement.md` → `Fachlicher Scope`:

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Der markierbare „umgebungssensitive Test" (xUnit-Testfall/-klasse) und der „CI-Testlauf" als Gesamtheit aus blockierender Lane und best-effort-Lane. Betroffen: alle drei Testprojekte, `src/TestSupport`, die Workflows `pr-staging-ci.yml`, `staging-ci.yml`, `release.yml` sowie `scripts/local-ci.ps1` und `.githooks/test-execution-check.py`. |
| Auslösende Ereignisse | PR → `staging` (`pr-staging-ci.yml`), Push → `staging` (`staging-ci.yml`), Push → `main`/Tag (`release.yml`, Release-Gate). Keine neue Nutzeraktion; die Trennung wirkt automatisch in den bestehenden Triggern. |
| Erwartete Wirkung | Transiente umgebungsbedingte Fehlschläge (UIA-Timeout, Port-Kollision, Dateisperre, langsamer Runner) lassen den PR-/Staging-/Release-Check nicht mehr fehlschlagen; sie bleiben im best-effort-Job sichtbar und über die Flake-Statistik beobachtbar. Deterministische Fehler blockieren unverändert. |
| Umgang mit fehlenden Objekten | `TestCategories` (Kategorien-Konstanten), `TempDirectoryCleanup`, `FlakyConventionTests`, die vier PowerShell-Dateien und `docs/help/stabilitaet/os-interface-tests.md` existieren nicht → werden **erzeugt**. Der best-effort-Job existiert nicht → wird in drei Workflows ergänzt (in `release.yml` als `continue-on-error`-Schritte). Vorhandene Löschstellen (`E2ETestBase.Dispose`, `TestDataContext.Cleanup`, `ApiKeyBuildRunner.Dispose`, `TempDirectory.Dispose`, `DiagnosticsCaptureE2ETests`, `TileSourceMockServerTests_Flow`, `BackupCryptoTests`, `HttpTileSourceTests_*`, `ReceiptPhotoStoreTests_*`) werden **wiederverwendet** (auf `TempDirectoryCleanup` umgestellt). Bestehende Bausteine (`TransientRetry`, `TransientWait`, `E2EStartupPolicy`, `LoopbackHttpListener`, Mock-Server, `E2EDiagnostics`) bleiben unverändert. |

### Scope-Ausschlüsse

Aus `requirement.md` übernommen; im autonomen Lauf per Orchestrator-Vorgabe als festgelegt behandelt
(kein interaktiver Anwender, daher direkt `bestätigt` statt `offen` — 2026-11-23):

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Kein Blanket-Retry: genau ein automatisierter Re-Run, ausschließlich `Flaky`-markierte Fehlschläge in der best-effort-Lane. | Anforderung; Blaupause identisch. | bestätigt |
| 2 | Keine Änderung der App-Logik (`src/Tankradar.MAUI` bleibt fachlich unverändert). | Feature liegt auf Testinfrastruktur-/CI-Ebene. | bestätigt |
| 3 | Keine Determinismus-Garantie für echte UI-/Prozess-Tests (Zielbild: „selten, lokalisiert, selbstdiagnostizierend"). | Anforderung; echte OS-Kontakte bleiben inhärent transient. | bestätigt |
| 4 | Kein Entfernen/Skippen umgebungssensitiver Tests — sie laufen weiter, nur entkoppelt. | Anforderung. | bestätigt |
| 5 | Keine Coverage-Erweiterung: Coverage bleibt auf Unit- und Integrationstests der blockierenden Lane beschränkt (E2E ohne Collector). | Anforderung; Blaupause identisch (nur `Category!=OsInterface` speist das Gate). | bestätigt |
| 6 | Kein `Run-AllTestsIndividually.ps1`-Port (Einzeltest-Läufer der Blaupause). | Von der Anforderung nicht verlangt; Orchestrator-Liste der Blaupausen-Elemente ohne dieses Skript. | bestätigt |
| 7 | Kein automatisches Quarantäne-Issue aus dem CI-Report-Schritt heraus. | Blaupause-Entscheidung: Der Analyzer ist report-only — ein Einzel-Run-TRX erreicht die Quarantäne-Schwellen (≥ 2 Flake-Ereignisse bzw. ≥ 5 Ausführungen) strukturell nicht; die Auswertung über das 20-Läufe-Fenster erfordert aggregierte Artefakte mehrerer Läufe (`gh run download`). Quarantäne-Issues werden dokumentiert **manuell** angelegt. | bestätigt |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Trait-Mechanik (xUnit v2) | Eingebautes `[Trait("Category", TestCategories.OsInterface)]` bzw. `[Trait("Category", TestCategories.Flaky)]`; zentrale Konstanten `TestCategories` in `src/TestSupport`. **Kein** eigenes Attribut, kein `ITraitDiscoverer`-Port. | Orchestrator-Vorgabe; Blaupause nutzt xUnit v3/`ITraitAttribute`, was in 2.9.3 nicht existiert. `[Trait]` ist der native v2-Mechanismus und wird von `--filter "Category=…"` direkt ausgewertet. |
| Markierung des E2E-Projekts | `[Trait("Category", TestCategories.OsInterface)]` **einmalig** auf `E2ETestBase` — nicht auf ~110 Einzelklassen. | Empirisch verifiziert (Scratch-Projekt, xUnit 2.9.3): Klassen-Traits werden an abgeleitete Testklassen vererbt (`--list-tests --filter "Category=OsInterface"` listet Tests der abgeleiteten Klasse). Ein Trait deckt alle `E2ETestBase`-Nachfahren inkl. `SettingsE2ETestBase`-, `SearchE2ETestBase`- usw.-Hierarchie ab. Fallback bei Nichterben: mechanisch pro Klasse taggen (Verifikationsschritt). |
| E2E-Lane-Zuordnung (Offene Frage 1 der Anforderung) | **Gesamtes** FlaUI-gebundene E2E-Projekt wandert in die best-effort-Lane. Die dokumentierte „bewusste Abweichung" (E2E blockierend) wird durch den projektübergreifenden Rollout abgelöst; Workflow-Kommentare und `docs/help/ci-cd/workflows.md` werden angepasst. | Orchestrator-Vorgabe + Issue-Wortlaut (FlaUI/UI-Automation ausdrücklich umgebungssensitiv) + Vorlagenkonformität (Vorlage sieht UI-Tests als best-effort). |
| `StartFailureCleanupE2ETests` | Bleibt **unmarkiert** → läuft in der blockierenden Lane. | Kein App-Start (`RuntimeHelpers.GetUninitializedObject`), rein deterministischer Meta-Test; dient zugleich als Beleg, dass die Filterung greift. |
| Mock-Server-/Loopback-Einordnung (Offene Frage 2) | **Markieren**: `SearchMockServerTestBase` (deckt per Vererbung alle ~13 `*MockServerTests_*` inkl. `RouteSearchMockServerTestBase`-Nachfahren), `LoopbackHttpListenerTests_Retry` sowie die drei Unit-Klassen mit echtem `HttpListener` (`MockOpenRouteServiceServerTests`, `OpenRouteServiceClientTests_Key`, `OpenRouteServiceKeyCheckerTests`). | Echte Loopback-Portbindung ist eine OS-Schnittstelle (Port-Kollision/Firewall möglich); der 10×-Retry von `LoopbackHttpListener` entschärft, eliminiert die Klassifikation aber nicht (Blaupause-Kriterium: „Assertion hängt am OS-Verhalten" — der Listener ist Prüfgegenstand bzw. Voraussetzung). Konservativ: lieber best-effort als PR-Blockade. |
| `ApiKeyBuildTests_*` | Klassenweise `[Trait("Category", "OsInterface")]` (`ApiKeyBuildTests_Log`, `ApiKeyBuildTests_Sources`). | Echte `dotnet build`-Kindprozesse mit eigenem 5-Minuten-Budget — klassischer Kandidat (prozessübergreifend, SDK-/Last-abhängig). |
| Reale-Dateisystem-/SQLite-Tests (`TestDataContext`/`TestDatabase`-Nutzer, `BackupServiceTests_*`, `MigrationsTests_*`, `EnvironmentCollection`) | **Nicht** markieren. | Blaupause-Kriterium „Assertion hängt am OS-Verhalten": isolierte GUID-Verzeichnisse, keine konkurrierenden Locks, historisch deterministisch grün. Härtung erfolgt über `TempDirectoryCleanup` statt Lane-Verschiebung — hält die blockierende Abdeckung hoch. |
| `Flaky`-Vergabe zum Umsetzungszeitpunkt | Minimal: `SearchE2ETests_LargeResult` (dokumentierter transiente `COMException 0x80131505`, s. `docs/help/ci-cd/workflows.md` „UIA-Timeouts bei Baumsuchen") erhält `[Trait("Category", TestCategories.Flaky)]`; `OsInterface` kommt per Basisklassen-Vererbung. | Konvention: `Flaky` nur bei dokumentiertem Flake-Ereignis. Weitere Markierungen folgen erst über Analyzer-Befunde — erfindungsgemäßes Taggen verboten. |
| Lane-Form im CI | Neuer **eigener Job** `os-interface-tests` (`continue-on-error: true`, Jobebene), parallel zu `build-and-test` (`needs: detect-backmerge`), nicht in der `needs`-Kette von `version`. | Blaupause identisch; Jobebene-`continue-on-error` hält die Lane sauber nicht-blockierend und die Laufzeit außerhalb des 45-min-Budgets des blockierenden Jobs. |
| OS-Job-Timeout | `timeout-minutes: 60`. | E2E-Suite lokal ~18,5 min seriell, in der CI mit 60-s-UIA-Timeout erfahrungsgemäß deutlich länger; plus `dotnet build`, OS-Subsets von Unit/Integration (ApiKeyBuild bis 5 min/Test), Retry- und Report-Schritt. Großzügig gegenüber dem heutigen 45-min-Gesamtjob, der alle drei Suiten enthielt. |
| `release.yml`-Filterung (Orchestrator-Punkt 4) | Ja — Blaupause-konform: `Release gate` wird in blockierende Schritte `Category!=OsInterface` (Unit, Integration) und `continue-on-error: true`-Schritte `Category=OsInterface` aufgeteilt, mit TRX-Logger + `Upload release-gate test results`-Artefakt. E2E bleibt im Release-Gate weiterhin ohne Lauf (wurde auf staging in der OS-Lane geprüft). | Die Blaupause filtert im Release-Gate exakt so; das Gate muss deterministisch bleiben — es prüft einen bereits vollständig validierten staging-Stand nur noch als Safety-Net. |
| Host-Frist-Konvention (Deadline-Angleichung) | Neuer auflösender Helfer `E2EStartupPolicy.ResolveHostPollTimeout` = `max(30 s, 2 × UIA-Timeout)` → 30 s lokal, 120 s CI. Alle Host-Poll-Fristen verwenden ihn. | Entspricht der bestehenden Konvention `totalLimit = max(60 s, 2 × uia)` in `E2ETestBase.StartAndConnect` und erzwingt Host-Frist > Kind-Budget (UIA-Timeout 15 s/60 s). Lokal bleibt das heutige 30-s-Verhalten identisch; nur die zu kurzen 20-s-/25-s-/6-s-Fristen werden angehoben. |
| Quarantäne-Mechanik (Offene Frage 3) | Report-only wie Blaupause: `Analyze-FlakyTestResults.ps1` klassifiziert (`Quarantäne-Kandidat`/`beobachten`/`stabil`), gibt `::warning::`-Empfehlung, Exit immer 0. Quarantäne-Issue wird **manuell** mit Label `flaky-quarantine` angelegt (Prozess + `gh`-Kommandos in der neuen Doku; Dedup per `gh issue list --state open --label flaky-quarantine`). | Ein Einzel-Run erreicht die Fenster-Schwellen nicht; automatische Issue-Erzeugung aus dem CI-Schritt würde nie feuern bzw. benötigte `issues: write` in einem best-effort-Job — Blaupause hat das bewusst verworfen. |
| `local-ci.ps1` (Offene Frage 5) | Lane-Trennung lokal spiegeln: Unit/Integration/E2E-Schritte erhalten `--filter "Category!=OsInterface"`; neuer `-BestEffort`-Schritt „Tests: OS-Schnittstellen" führt alle drei Projekte mit `--filter "Category=OsInterface"` aus (nutzt das bisher ungenutzte `Invoke-Step -BestEffort`-Flag). `-SkipE2E` überspringt nur den E2E-Anteil der OS-Lane. | Blaupausentreue lokale Parität; blockierender Exit-Code hängt weiter nur an der deterministischen Lane. |
| `.githooks/test-execution-check.py` | `--filter "Category!=OsInterface"` auf die Standard-Testziele (ohne E2E) ergänzen; `PRE_PUSH_E2E=1` bleibt unverändert (ganze Solution, kein Filter). | Pre-Push soll der blockierenden Lane entsprechen; OS-markierte Integrationstests (u. a. minutenlange `ApiKeyBuildTests_*`) bleiben so aus dem Push-Pfad. |
| Re-Run-Granularität (Offene Frage 6) | Genau ein Re-Run **je Lauf und Projekt**, gefiltert auf die `Flaky`-markierten Fehlschläge des Primär-TRX (`(Category=Flaky)&(FullyQualifiedName~…)`). | Wörtliche Blaupause-Semantik („genau ein automatisierter Re-Run nur der markierten Fehlschläge"): ein einziger `dotnet test`-Aufruf für die Schnittmenge, kein Blanket-Retry. |
| iOS/macOS-Jobs (Offene Frage 7) | Keine Lane-Trennung auf `ios-build`/`ios-prerelease`/`ios` — dort laufen keine Tests. | Bestätigt aus Inventory: die Jobs führen ausschließlich Builds aus. |
| Coverage-Wechselwirkung (Offene Frage 4) | `COVERAGE_THRESHOLD: 70` bleibt auf der blockierenden Lane unverändert; die Verifikation misst die neue Basis. Sollte die Schwelle durch den Entzug markierter Tests unterschritten werden, ist das ein eigenständiger Befund (nicht Teil dieses Plans: keine Schwellen-Senkung ohne gesonderte Entscheidung). | Blaupause-Konvention: nur `Category!=OsInterface` speist das 70-%-Gate. Die markierten Subsets sind klein (~25 Klassen), ein Unterschreiten ist nicht zu erwarten, wird aber verifiziert. |
| `validate-workflows.py`-Altbestand | Zwei Minimal-Fixes gehören zum Scope: `e2e-dumps` in `SAFE_UPLOAD_DIRS` aufnehmen (dediziertes Verzeichnis, enthält nur `DOTNET_Dbg*`-Crash-Dumps — nie `.ipa`) und die `workflow_run`-Namensprüfung nur noch ausführen, wenn `staging-to-main-promotion.yml` tatsächlich `workflow_run` triggert. | Der neue OS-Job repliziert die `e2e-dumps`-Uploads — ohne Fix kämen zwei neue Befunde hinzu (Anforderung: „keine neuen Befunde"). Der Fix heilt zugleich die zwei preexisting `e2e-dumps`-Befunde. Die Namensprüfung ist ein Stale-Check gegen den inzwischen `push`-getriggerten Workflow — ohne Fix bliebe `validate-workflows.py` dauerhaft rot (konterkariert „muss bestehen"). |
| Shell der neuen Skript-Schritte | Job-Default bleibt `bash`; die Schritte `Retry flaky tests once` und `Analyze flaky test results` setzen `shell: pwsh` und rufen die Skripte per `& scripts/….ps1` auf. | Blaupause identisch; `pwsh` liegt auf `windows-latest` vor; gemischte Defaults sind im Job unproblematisch. |

## Programmabläufe

### Blockierender PR-/Staging-Testlauf (bestehend, angepasst)

1. `detect-backmerge` → `build-and-test` (unveränderte `needs`-Kette).
2. `Test regular (unit)` bzw. `(integration)`: `dotnet test … --filter "Category!=OsInterface"` + Coverage-Collector → TRX `test-results-unit.trx`/`test-results-integration.trx`.
3. Coverage-Report + Schwellwertprüfung (unverändert, nur blockierende Lane).
4. `Test regular (E2E)`: `dotnet test src/Tankradar.Tests.E2E --filter "Category!=OsInterface"` → führt nur die unmarkierten Tests aus (aktuell ausschließlich `StartFailureCleanupE2ETests`).
5. Artefakt-Uploads wie bisher (`if: always()`).

Beteiligt: `.github/workflows/pr-staging-ci.yml`, `.github/workflows/staging-ci.yml`, `E2ETestBase`, `TestCategories`.

### Best-effort-OS-Testlauf (neu, beide Workflows identisch)

1. Job `os-interface-tests` (`needs: detect-backmerge`, `continue-on-error: true`, `timeout-minutes: 60`, gleiche `Include*Target=false`-Env) → Checkout, Setup .NET/Node, `dotnet workload restore`, `dotnet restore -p:Configuration=Release`, `dotnet build --configuration Release --no-restore`.
2. `Prepare E2E crash dump directory` (`mkdir -p e2e-dumps`).
3. `Test OS interface (unit)` / `(integration)` / `(E2E)`: `dotnet test … --filter "Category=OsInterface"` → TRX `test-results-os-interface-{unit,integration,e2e}.trx` unter `TestResults/`. Der E2E-Schritt erhält die Env `TANKRADAR_E2E_WINDOW` und `DOTNET_Dbg*` (wie heute der blockierende Schritt).
4. `Retry flaky tests once` (`if: failure()`, `shell: pwsh`): `scripts/Retry-FlakyTests.ps1` je Projekt mit `-Configuration Release` — parst das Primär-TRX, ermittelt die `Flaky`-Menge per `dotnet test --no-build --list-tests --filter "Category=Flaky"`, wiederholt die Schnittmenge genau einmal → `<primär>-flaky-retry.trx`; recovered → `::notice::`, persistent/indeterminate → `::warning::` + Exit 1 (Job bleibt per `continue-on-error` nicht-blockierend).
5. `Analyze flaky test results` (`if: always()`, `shell: pwsh`): `scripts/Analyze-FlakyTestResults.ps1 -Path TestResults` — reiner Report (Tabelle + Klassifikation), Exit immer 0.
6. Uploads (`if: always()`): `test-results-os-interface-{pr|staging}` (`TestResults/test-results-os-interface*.trx` — deckt `*-flaky-retry.trx` mit ab), `e2e-diagnostics-os-{pr|staging}` (`e2e-diagnostics/`, `if-no-files-found: ignore`), `e2e-dumps-os-{pr|staging}` (`e2e-dumps/`, `if-no-files-found: ignore`).

Beteiligt: `os-interface-tests`-Job, `scripts/lib/Trx.ps1`, `scripts/lib/TestDiscovery.ps1`, `scripts/Retry-FlakyTests.ps1`, `scripts/Analyze-FlakyTestResults.ps1`.

### Release-Gate (`release.yml`)

1. `Release gate - regular tests (unit)` / `(integration)`: `dotnet test … --configuration Release --filter "Category!=OsInterface"` (ohne `--no-build`, wie bisher) + TRX-Logger → blockierend.
2. `Release gate - OS interface tests (unit)` / `(integration)`: selbe Befehle mit `--filter "Category=OsInterface"` + `continue-on-error: true` auf Schrittebene.
3. `Upload release-gate test results` (`if: always()`): `test-results-release` aus `TestResults/*.trx`.

Beteiligt: `.github/workflows/release.yml`.

### Lokaler Prüflauf (`scripts/local-ci.ps1`)

1. Blockierende Schritte `Tests: Unit`/`Integration`/`FlaUI-E2E` → `--filter "Category!=OsInterface"`.
2. Neuer `Invoke-Step -BestEffort "Tests: OS-Schnittstellen"`: `dotnet test` aller drei Projekte mit `--filter "Category=OsInterface"` (E2E-Anteil entfällt bei `-SkipE2E`); Fehlschlag → Status `WARNUNG`, Exit-Code unverändert.
3. Optional dokumentierter Nachschritt: `Retry-FlakyTests.ps1`/`Analyze-FlakyTestResults.ps1` manuell gegen `TestResults/` (Doku, kein Skript-Schritt).

Beteiligt: `scripts/local-ci.ps1`, `docs/help/ci-cd/lokaler-pruefung.md`.

### Pre-Push-Hook (`.githooks/test-execution-check.py`)

1. Standardziele (alle `*tests*`-Projekte ohne `e2e`): `dotnet test --no-build --filter "Category!=OsInterface"`.
2. `PRE_PUSH_E2E=1`: Solution-Testlauf unverändert ohne Filter.

Beteiligt: `.githooks/test-execution-check.py`, `docs/help/git-hooks/checks.md` (falls Filter dort dokumentiert sind).

### Quarantäne-Prozess (dokumentiert, manuell)

1. Operateur lädt OS-TRX-Artefakte mehrerer Läufe: `gh run download <run-id> -n test-results-os-interface-{pr|staging} -D artifacts/os-*`.
2. `scripts/Analyze-FlakyTestResults.ps1 -Path artifacts/os-*` meldet Kandidaten (`::warning::`/Tabelle).
3. Bei Schwellwert-Überschreitung: `gh issue list --state open --label flaky-quarantine` (Dedup) → `gh issue create --label flaky-quarantine` mit Test-FQN, Fehlerbild, Lauf-/Artefakt-Verweis; Fehlerbild zusätzlich unter „Bekannte Fehlerbilder" in `docs/help/stabilitaet/os-interface-tests.md` dokumentieren.
4. Job-Rerun-Konvention: nur `gh run rerun --job <job-id>` des `os-interface-tests`-Jobs erlaubt, nie `gh run rerun --failed` und nie Jobs der blockierenden Lane.

Beteiligt: `docs/help/stabilitaet/os-interface-tests.md`, `scripts/Analyze-FlakyTestResults.ps1`.

## Neue Klassen

| Klasse | Typ | Zweck |
|--------|-----|-------|
| `Tankradar.TestSupport.TestCategories` (`src/TestSupport/TestCategories.cs`) | statische Klasse | Zentrale Kategorien-Konstanten `OsInterface = "OsInterface"`, `Flaky = "Flaky"` für `[Trait("Category", …)]` und Filterstrings (blaupausentreu). |
| `Tankradar.TestSupport.TempDirectoryCleanup` (`src/TestSupport/TempDirectoryCleanup.cs`) | statische Klasse | `DeleteBestEffort(root, processesToKill?, normalizeAttributes?)`: optionales Beenden registrierter Prozesse (`Kill(entireProcessTree: true)` + `WaitForExit(10 s)`), optionale Attribut-Normalisierung, `Directory.Delete` rekursiv mit max. 5 Versuchen und linearem Backoff (`100 ms × Versuch`); wirft nie — Restfehler nur nach `Console.Error` (Portierung der Blaupause). |
| `FlakyConventionTests` (`src/TestSupport/FlakyConventionTests.cs`, per `<Compile Include>` in alle drei Testprojekte gelinkt) | xUnit-Testklasse | Reflexions-Meta-Test: jede Methode/Klasse mit `Category=Flaky` muss `Category=OsInterface` tragen; scannt `typeof(FlakyConventionTests).Assembly` — jede Projektkopie prüft ihre eigene Assembly (Trait-Auslesung über `TraitAttribute`-Instanzen, `inherit: true`). |
| `TempDirectoryCleanupTests` (`src/Tankradar.Tests.Unit/Unit/Support/TempDirectoryCleanupTests.cs`) | xUnit-Testklasse | Deterministische Datei-I/O-Tests des Helfers (Read-only-Löschung mit `normalizeAttributes`, gesperrtes Verzeichnis wirft nicht, fehlendes Verzeichnis wirft nicht, `processesToKill` beendet Prozess — letzterer trägt selbst `[Trait("Category", "OsInterface")]`). |
| `E2EStartupPolicyTests_HostPollTimeout` (`src/Tankradar.Tests.Unit/…`) | xUnit-Testklasse | Tests von `ResolveHostPollTimeout` mit injiziertem Env-Getter (lokal 15 s → 30 s; CI 60 s → 120 s; Override 1 s → 30 s Untergrenze; Override 600 s → 1200 s), analog `E2EStartupPolicyTests_Backoff`. |

Neue Skript-Dateien (keine Klassen): `scripts/lib/Trx.ps1`, `scripts/lib/TestDiscovery.ps1`,
`scripts/Retry-FlakyTests.ps1`, `scripts/Analyze-FlakyTestResults.ps1` (1:1-Portierung der Blaupause;
Kommentare/Beispiel-Pfade auf Tankradar-Namen angepasst).

Neue Doku: `docs/help/stabilitaet/os-interface-tests.md` (Portierung der Blaupause-Doku auf
Tankradar-Verhältnisse: Heuristik `Process.Start|ProcessStartInfo|FlaUI|UIA3|HttpListener|TcpListener|Clipboard`,
Projektnamen, `release.yml`-Verhalten, Rerun-/Quarantäne-Konvention).

## Änderungen an bestehenden Klassen

### `E2EStartupPolicy` (`src/TestSupport/E2EStartupPolicy.cs`, statisch)

- **Neue Methode:** `ResolveHostPollTimeout(Func<string, string?> getEnvironmentVariable)` → `TimeSpan` —
  `max(30 s, 2 × ResolveUiaTimeout(getEnvironmentVariable))`; zentrale Host-Frist für alle Poll-Warten
  (lokal 30 s wie heute, CI 120 s > 60-s-UIA-Kind-Budget).

### `E2ETestBase` (`src/Tankradar.Tests.E2E/E2ETestBase.cs`)

- **Neues Klassenattribut:** `[Trait("Category", TestCategories.OsInterface)]` — per xUnit-v2-Trait-Vererbung
  gelten alle ~110 abgeleiteten E2E-Testklassen als `OsInterface` (empirisch verifiziert).
- **Geänderte Fristen (Deadline-Angleichung, Host > Kind):**
  - `NavigateToTab` (Z. 137–187): `attemptTimeout` 5 s → `HostPollTimeout`; `maxAttempts` 5 → 3
    (Worst-Case-Deckel: 3 × 120 s ≈ 6 min CI bei vollständig blockierter Navigation).
  - `WaitForTab`-Aufruf (Z. 143): 30 s → `HostPollTimeout`.
  - `WaitForPageTitle` (Z. 196): 30 s → `HostPollTimeout`.
  - `SkipSetup` (Z. 536–567): Sichtbarkeitswarte 30 s → `HostPollTimeout`; Ausblendwarte je Versuch
    6 s → `HostPollTimeout` (`maxAttempts` 5 bleibt; Invoke ist idempotent).
- **Geänderte Methode:** `Dispose` (Z. 295–305) — `Directory.Delete` + `catch (IOException)` →
  `TempDirectoryCleanup.DeleteBestEffort(_testDataDirectory)` (Retry/Backoff, wirft nie).
- **Unverändert:** `StartAndConnect`-`totalLimit` `max(60 s, 2 × uia)` (bereits konsistent),
  `CloseApplicationAndWait` (15 s + `Kill(entireProcessTree)` + 10 s — feste Fristen ohne Kind-Budget),
  `TransientRetry`-Nutzung (bereits Budget-bewusst).

### `SettingsE2ETestBase` (`src/Tankradar.Tests.E2E/E2E/FlaUI/SettingsE2ETestBase.cs`)

- **Geänderte Konstante:** `DefaultTimeout` (Z. 18) 20 s → `HostPollTimeout` via
  `E2EStartupPolicy.ResolveHostPollTimeout` (wirkt auf alle `WaitUntil`-Aufrufe).

### `SearchMockServerTestBase` (`src/Tankradar.Tests.Integration/Integration/Support/SearchMockServerTestBase.cs`)

- **Neues Klassenattribut:** `[Trait("Category", TestCategories.OsInterface)]` — deckt per Vererbung alle
  `*MockServerTests_*` inkl. `RouteSearchMockServerTestBase`-Nachfahren (Loopback-Portbindung).

### `LoopbackHttpListenerTests_Retry`, `ApiKeyBuildTests_Log`, `ApiKeyBuildTests_Sources` (Integration)

- **Neue Klassenattribute:** `[Trait("Category", TestCategories.OsInterface)]` (Port-Kollisionen bzw.
  echte `dotnet build`-Kindprozesse mit 5-Min-Budget).

### `MockOpenRouteServiceServerTests`, `OpenRouteServiceClientTests_Key`, `OpenRouteServiceKeyCheckerTests` (Unit)

- **Neue Klassenattribute:** `[Trait("Category", TestCategories.OsInterface)]` (echter `HttpListener`).

### `SearchE2ETests_LargeResult` (`src/Tankradar.Tests.E2E/…`)

- **Neues Klassenattribut:** `[Trait("Category", TestCategories.Flaky)]` — dokumentiertes Flake-Ereignis
  (transiente UIA-`COMException`; `OsInterface` per Vererbung vorhanden).

### `TestDataContext` (`src/Tankradar.Tests.Integration/TestDataContext.cs`)

- **Geänderte Methode:** `Cleanup` (Z. 37–43) — `Directory.Delete` ohne jede Fehlerbehandlung →
  `TempDirectoryCleanup.DeleteBestEffort(DataDirectory)`; behebt die Assert-Überdeckungs-Invariante.

### `ApiKeyBuildRunner` (`src/Tankradar.Tests.Integration/Integration/Support/ApiKeyBuildRunner.cs`)

- **Geänderte Methode:** `Dispose` (Z. 160–174) — `Directory.Delete` mit `IOException`/→ `TempDirectoryCleanup.DeleteBestEffort(_directory)` (Retry/Backoff, wirft nie).
- **Unverändert:** 5-Minuten-Kindbudget (`WaitForExit` + Prozessbaum-Kill); wirksames Host-Budget ist
  künftig das Job-`timeout-minutes: 60` der OS-Lane (60 min ≫ 5 min — konsistent).

### `TempDirectory` (`src/Tankradar.Tests.Unit/Unit/Support/TempDirectory.cs`)

- **Geänderte Methode:** `Dispose` — `Directory.Delete` + `catch (IOException)` →
  `TempDirectoryCleanup.DeleteBestEffort(Path)`.

### `DiagnosticsCaptureE2ETests` (`src/Tankradar.Tests.E2E/E2E/FlaUI/DiagnosticsCaptureE2ETests.cs`)

- **Geänderte Methode:** Testkörper-`finally` (Z. 38–41) — `Directory.Delete` ohne Fehlerbehandlung →
  `TempDirectoryCleanup.DeleteBestEffort(_diagnosticsDirectory)`.

### `TileSourceMockServerTests_Flow`, `BackupCryptoTests`, `HttpTileSourceTests_{AgeAndRevalidation,Fetch,HttpCaching,NoStoreAndExpires}`, `ReceiptPhotoStoreTests_{Detection,Files}`

- **Geänderte Dispose-/Löschstellen:** `Directory.Delete` → `TempDirectoryCleanup.DeleteBestEffort(…)`
  (einheitliche Invariante: Cleanup-Fehler überdeckt nie den Assert).

### `scripts/validate-workflows.py`

- **`SAFE_UPLOAD_DIRS`:** um `'e2e-dumps'` ergänzen (dediziertes Crash-Dump-Verzeichnis, nie `.ipa`-haltig;
  verhindert neue Befunde durch die OS-Job-Uploads und heilt die zwei preexisting Befunde).
- **`main()` `workflow_run`-Prüfung (Z. 226–232):** nur ausführen, wenn `trigger_block(promotion)` tatsächlich
  `workflow_run` enthält (der Workflow triggert inzwischen `push` → `staging`; heilt den dritten preexisting Befund).

### `scripts/local-ci.ps1`

- **Geänderte Schritte:** `Tests: Unit`/`Integration`/`FlaUI-E2E` um `--filter "Category!=OsInterface"` ergänzen.
- **Neuer Schritt:** `Invoke-Step -BestEffort "Tests: OS-Schnittstellen"` — alle drei Projekte mit
  `--filter "Category=OsInterface"` (E2E-Anteil entfällt bei `-SkipE2E`); Fehlschlag → `WARNUNG`, nicht `FEHLER`.
- **Geänderte Kopfdoku:** Lane-Trennung beschreiben.

### `.githooks/test-execution-check.py`

- **Geänderte Kommandos (Z. 89–90):** `--filter "Category!=OsInterface"` für die Standard-Testziele
  (ohne E2E); `PRE_PUSH_E2E=1`-Solution-Lauf unverändert ungefiltert.

### Workflow-Dateien

- **`.github/workflows/pr-staging-ci.yml` + `staging-ci.yml`** (identische Änderungen):
  - `Test unit`/`Test integration`: `--filter "Category!=OsInterface"` ergänzen; Schrittnamen auf
    `Test regular (…)` umbenennen (Blaupause-Konvention).
  - `Test E2E with FlaUI (blocking)` → `Test regular (E2E)` mit `--filter "Category!=OsInterface"`;
    Kommentar zur „bewussten Abweichung" durch Lane-Erklärung ersetzen (E2E-FlaUI-Tests laufen im
    `os-interface-tests`-Job best-effort; hier bleiben nur unmarkierte Rest-Tests).
  - Neuer Job `os-interface-tests` (siehe Programmablauf „Best-effort-OS-Testlauf").
  - `needs`-Kette von `version` (`staging-ci.yml`) unverändert — OS-Job geht nicht in die Blockade ein.
- **`.github/workflows/release.yml`:** `Release gate - regular tests` in zwei blockierende
  `Category!=OsInterface`-Schritte und zwei `continue-on-error`-`Category=OsInterface`-Schritte
  aufgeteilt; TRX-Logger + `Upload release-gate test results` (`if: always()`).

### Testprojekt-`.csproj`-Dateien (alle drei)

- **Neue `<Compile Include>`-Links:** `..\TestSupport\TestCategories.cs`, `..\TestSupport\TempDirectoryCleanup.cs`,
  `..\TestSupport\FlakyConventionTests.cs` — in `Tankradar.Tests.Unit.csproj`,
  `Tankradar.Tests.Integration.csproj`, `Tankradar.Tests.E2E.csproj` (Shared-Source-Mechanik; ohne Link
  wird die Datei nicht kompiliert).

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `TANKRADAR_E2E_UIA_TIMEOUT_SECONDS` | Bestehende `ResolveUiaTimeout`-Validierung (1–600 s, ungültig → ignoriert) gilt unverändert; `ResolveHostPollTimeout` reicht sie durch. | — |
| `Category=Flaky`-Vergabe | Nur in Kombination mit `Category=OsInterface` — abgesichert durch `FlakyConventionTests` je Testassembly. | Meta-Test schlägt fehl (blockierende Lane). |
| Quarantäne-Workflow-Eingaben | Keine neue Schnittstelle; Schwellwerte sind Skript-Parameter mit Blaupause-Defaults. | — |

## Konfigurationsänderungen

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| `os-interface-tests.timeout-minutes` | Workflow-Job-Eigenschaft | 60 | Großzügiges Zeitbudget der best-effort-Lane (E2E + `dotnet build` + OS-Subsets + Retry/Report). |
| `Category`-Filterstrings | Workflow-`run`-Argumente | `Category!=OsInterface` (blockierend), `Category=OsInterface` (best-effort), `Category=Flaky` (Retry) | Lane-Trennung. |
| `-QuarantineMinFlakeEvents` / `-QuarantineMinFailureRate` / `-QuarantineMinExecutions` / `-WatchMinFailureRate` / `-MaxRuns` | `Analyze-FlakyTestResults.ps1`-Parameter | 2 / 0.2 / 5 / 0.1 / 20 | Quarantäne-/Beobachtungs-Schwellwerte (Blaupause-Startwerte, kalibrierbar). |
| `COVERAGE_THRESHOLD` | Job-Env (unverändert) | 70 | Gilt weiterhin nur für die blockierende Lane. |
| Artefaktnamen | Workflow-`upload-artifact`-Namen | `test-results-os-interface-{pr,staging}`, `e2e-diagnostics-os-{pr,staging}`, `e2e-dumps-os-{pr,staging}`, `test-results-release` | Getrennte OS-Lane-Artefakte inkl. `*-flaky-retry.trx`. |

Bestehende Umgebungsvariablen gelten im OS-Job analog weiter: `TANKRADAR_E2E_WINDOW`,
`TANKRADAR_E2E_UIA_TIMEOUT_SECONDS`, `DOTNET_Dbg*`, `GITHUB_ACTIONS`/`CI` (UIA-Timeout 60 s).

## Seiteneffekte und Risiken

- **Coverage-Basis:** `Category!=OsInterface` entfernt die markierten Subsets (~3 Unit-Klassen,
  `ApiKeyBuildTests_*`, `LoopbackHttpListenerTests_Retry`, ~13 Mock-Server-Integrationstests) aus der
  Coverage-Messung → gemessene Abdeckung kann sinken. Verifikation gegen `COVERAGE_THRESHOLD: 70`
  ist Pflichtschritt; bei Unterschreitung eigenständiger Befund (keine stille Schwellen-Senkung).
- **Blockierende E2E-Rest-Suite:** läuft künftig nur `StartFailureCleanupE2ETests` — neue unmarkierte
  E2E-Tests würden dort landen und blockierend wirken; `E2ETestBase`-Vererbung fängt alle normalen
  Testklassen automatisch (Trait-Vererbung). Bewusst akzeptiert: vergessene Basisklassen-Vererbung
  bleibt sichtbar blockierend statt still best-effort.
- **Release-Gate verlängert sich** um die OS-Schritte (best-effort, `continue-on-error`) — informativ,
  blockiert nie.
- **PR-Checks-Liste:** neuer Check `os-interface tests` erscheint; wegen `continue-on-error` liefert er
  bei Fehlschlag ein neutrales/„warning"-Ergebnis — verändert keine Required-Check-Semantik.
- **`e2e-diagnostics`/`e2e-dumps` der OS-Lane** wandern in neue Artefaktnamen (`*-os-*`) — Verweis in
  `workflows.md` anzupassen.
- **Längere E2E-Einzelwartezeiten:** Host-Fristen lokal unverändert (30 s, nur 20 s→30 s und 6 s→30 s),
  in der CI auf 120 s angehoben — ein echter Totalausfall eines einzelnen Tests dauert in der CI länger,
  bleibt aber best-effort und unter dem 60-min-Job-Budget.
- **`validate-workflows.py`:** wird durch die zwei Fixes von Exit 1 (3 preexisting Befunde) auf Exit 0
  geheilt; `test_validate_workflows.py` 14/14 erwartet.
- **Kein Einfluss auf `src/Tankradar.MAUI`** (Scope-Ausschluss 2).

## Umsetzungsreihenfolge

1. **TestSupport-Bausteine anlegen und linken**
   - Voraussetzungen: Keine (xUnit 2.9.3/`TraitAttribute` bereits referenziert; Shared-Source-Mechanik vorhanden).
   - Beschreibung: `src/TestSupport/TestCategories.cs`, `TempDirectoryCleanup.cs` (Blaupause-Port),
     `FlakyConventionTests.cs` anlegen; in allen drei `.csproj` per `<Compile Include>` linken.

2. **`E2EStartupPolicy.ResolveHostPollTimeout` + Unit-Tests**
   - Voraussetzungen: Schritt 1 (Testprojekte kompilieren mit den neuen Links).
   - Beschreibung: Methode `max(30 s, 2 × uia)` ergänzen; `E2EStartupPolicyTests_HostPollTimeout` mit
     injiziertem Env-Getter.

3. **Deadline-Angleichung in `E2ETestBase` und `SettingsE2ETestBase`**
   - Voraussetzungen: Schritt 2.
   - Beschreibung: Fristen wie unter „Änderungen an bestehenden Klassen" konkret umstellen
     (`NavigateToTab`, `WaitForTab`, `WaitForPageTitle`, `SkipSetup`, `DefaultTimeout`).

4. **Cleanup-Härtung: alle `Directory.Delete`-Stellen auf `TempDirectoryCleanup` umstellen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `E2ETestBase.Dispose`, `TestDataContext.Cleanup`, `ApiKeyBuildRunner.Dispose`,
     `TempDirectory.Dispose`, `DiagnosticsCaptureE2ETests`-`finally`, `TileSourceMockServerTests_Flow`,
     `BackupCryptoTests`, `HttpTileSourceTests_*` (4), `ReceiptPhotoStoreTests_*` (2); dazu
     `TempDirectoryCleanupTests` (inkl. `[Trait OsInterface]` auf dem Prozess-Kill-Test).

5. **OsInterface-/Flaky-Markierung + Lane-Nachweis**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: `E2ETestBase` (OsInterface), `SearchMockServerTestBase` (OsInterface),
     `LoopbackHttpListenerTests_Retry`, `ApiKeyBuildTests_{Log,Sources}` (OsInterface),
     drei Unit-`HttpListener`-Klassen (OsInterface), `SearchE2ETests_LargeResult` (Flaky).
     Verifikation per `dotnet test --no-build --list-tests --filter "Category=OsInterface"` je Projekt:
     E2E muss alle `E2ETestBase`-Nachfahren listen (Trait-Vererbung greift — verifiziert; falls nicht,
     Fallback: Trait mechanisch auf jede Testklasse); blockierende Lane listet u. a. `StartFailureCleanupE2ETests`.
     `FlakyConventionTests` muss grün sein.

6. **PowerShell-Skripte portieren und mit synthetischen TRX verifizieren**
   - Voraussetzungen: Keine (Skripte unabhängig); inhaltlich auf Schritt 5 aufsetzend (Flaky-Menge).
   - Beschreibung: `scripts/lib/Trx.ps1`, `scripts/lib/TestDiscovery.ps1`, `scripts/Retry-FlakyTests.ps1`,
     `scripts/Analyze-FlakyTestResults.ps1`; Verifikation mit synthetischen TRX-Sätzen (Pass/Fail/Retry-TRX)
     und echtem `dotnet test --no-build --list-tests --filter "Category=Flaky"` gegen die gebauten Projekte.

7. **Workflows umbauen + Validator-Fixes**
   - Voraussetzungen: Schritte 1, 5, 6 (Markierung vorhanden, Skripte vorhanden — Validator prüft
     referenzierte `scripts/`-Dateien auf Existenz).
   - Beschreibung: `pr-staging-ci.yml` + `staging-ci.yml` (Regular-Filter, `os-interface-tests`-Job,
     Kommentar-Ersetzung), `release.yml` (Gate-Split), `SAFE_UPLOAD_DIRS` + `workflow_run`-Guard in
     `validate-workflows.py`; `python scripts/validate-workflows.py` und `scripts/test_validate_workflows.py`
     müssen grün sein.

8. **Lokale Prüfkette angleichen**
   - Voraussetzungen: Schritt 5.
   - Beschreibung: `local-ci.ps1` (Filter + `-BestEffort`-OS-Schritt), `.githooks/test-execution-check.py`
     (Filter in Standardzielen), Doku-Tabellen.

9. **Dokumentation**
   - Voraussetzungen: Schritte 5–8 (Beschreibung des Ist-Zustands).
   - Beschreibung: `docs/help/stabilitaet/os-interface-tests.md` (neu, Portierung), `workflows.md`
     (Job-/Skript-Tabellen, Abschnitt „E2E-Tests als Auslieferungs-Gate" → Lane-Modell, Abweichungs-Liste),
     `lokaler-pruefung.md`, `docs/help/git-hooks/checks.md` (Hook-Filter), Verlinkung aus `docs/help/ci-cd/index.md`
     bzw. `docs/help/index.md`.

10. **Verifikation (lokal + CI)**
    - Voraussetzungen: Schritte 1–9.
    - Beschreibung: lokal `dotnet test --filter "Category!=OsInterface"` je Projekt muss vollständig grün sein;
      OS-Lane `Category=OsInterface` best-effort lokal ausführen; `local-ci.ps1` grün (WARNUNG-Einträge der
      OS-Lane toleriert); Lane-Nachweis per `--list-tests`; danach PR und ≥ 3 komplette grüne
      `PR CI for Staging`-Durchläufe überwachen (`gh pr checks --watch --fail-fast`; OS-Job darf warnen,
      blockierende Checks müssen grün sein) — ein einzelner grüner Lauf gilt nicht als Nachweis.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `DeleteBestEffort` mit Read-only-Datei + `normalizeAttributes` | `TempDirectoryCleanupTests` (Unit) | Verzeichnis wird vollständig entfernt. |
| `DeleteBestEffort` bei gesperrtem Verzeichnis | `TempDirectoryCleanupTests` | Wirft nicht (Cleanup überdeckt nie den Assert). |
| `DeleteBestEffort` bei fehlendem Verzeichnis | `TempDirectoryCleanupTests` | Wirft nicht. |
| `DeleteBestEffort` mit `processesToKill` | `TempDirectoryCleanupTests` (`[Trait OsInterface]`) | Registrierte Prozesse werden vor dem Löschen beendet. |
| `FlakyMarkedTests_AlwaysCarryOsInterface` | `FlakyConventionTests` (in allen 3 Projekten gelinkt) | Jede `Category=Flaky`-Markierung trägt `Category=OsInterface`. |
| `ResolveHostPollTimeout`-Fälle (lokal/CI/Override/Mindestwert) | `E2EStartupPolicyTests_HostPollTimeout` (Unit) | Host-Frist = `max(30 s, 2 × UIA-Timeout)`. |
| — | Manuelle/Skript-Verifikation | `Retry-FlakyTests.ps1` + `Analyze-FlakyTestResults.ps1` mit synthetischen TRX-Sätzen (Blaupause-Testplan); Lane-Nachweis per `--list-tests`. |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `DiagnosticsCaptureE2ETests` | `finally`-Löschung → `TempDirectoryCleanup.DeleteBestEffort`. |
| `TileSourceMockServerTests_Flow`, `BackupCryptoTests`, `HttpTileSourceTests_*`, `ReceiptPhotoStoreTests_*` | `Directory.Delete` → `DeleteBestEffort`. |
| `test_validate_workflows.py` (`test_workflows_in_repository_are_clean`) | Erwartet grün nach `SAFE_UPLOAD_DIRS`-Fix (vorher 2 Befunde) — Anpassung nötig, falls ein Fixture die Befundanzahl erwartet (nein — Test assertiert leere Befundliste, wird automatisch grün). |
| Alle `E2ETestBase`-Nachfahren, `SearchMockServerTestBase`-Nachfahren, `ApiKeyBuildTests_*`, `LoopbackHttpListenerTests_Retry`, 3 Unit-`HttpListener`-Klassen | Erhalten `[Trait]`-Markierung → wechseln in die OS-Lane (reine Zusatzinformation, kein Verhaltensbruch). |

### E2E-Tests (primärer Funktionsnachweis)

**Keine neuen E2E-Tests erforderlich.** Die Anforderung berührt keinen über UI oder Nutzeraktion
erreichbaren Ablauf: Sie ändert ausschließlich Testinfrastruktur (Marker, Helfer, Deadlines, Cleanup)
und CI-/Skript-Orchestrierung. Der „Benutzerfluss" ist der CI-Pipeline-Lauf selbst; sein Nachweis erfolgt
über die Akzeptanzszenarien (Lane-Verhalten im Workflow) und die neuen Unit-/Meta-Tests — nicht über
FlaUI-Szenarien. Vorhandene E2E-Tests bleiben fachlich unverändert; sie wechseln lediglich die Lane.

Bestehende E2E-Tests, die angepasst werden (technisch, kein Szenario):

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `E2ETestBase` + sämtliche Nachfahren | OsInterface-Trait (Basisklasse), Deadline-Angleichung, `Dispose` → `DeleteBestEffort`. |
| `DiagnosticsCaptureE2ETests` | `finally`-Löschung → `DeleteBestEffort`. |
| `SearchE2ETests_LargeResult` | Zusätzliches `Flaky`-Trait (dokumentiertes Flake-Ereignis). |

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: grüner PR-Lauf mit Lane-Trennung | PR → `staging` | Neue Kategorien/Skripte/Job existieren (durch diesen Change erzeugt) | `detect-backmerge` → parallel `static-checks`, `build-and-test` (alle Testschritte `--filter "Category!=OsInterface"`), `os-interface-tests` (`Category=OsInterface`, E2E inkl. FlaUI) | PR-Checks grün; OS-Job grün/warnend, nicht blockierend; Artefakte `test-results-*` (regular) und `test-results-os-interface-*` getrennt | CI-Lauf + `gh pr checks`; lokale `--list-tests`-Nachweise |
| 2 | Transienter OS-Fehlschlag wird recovered | Flaky-markierter Test schlägt im OS-Job (UIA-Timeout, Port-Race) | OS-Job läuft | Primär-TRX enthält `Failed` → `Retry flaky tests once` (`if: failure()`) → genau ein gefilterter Re-Run → `Passed` | `::notice::` „recovered"; `*-flaky-retry.trx` im Artefakt; PR-Check bleibt grün | OS-Job-Protokoll + TRX-Artefakt; Skript-Verifikation mit synthetischem TRX |
| 3 | Persistenter OS-Fehlschlag blockiert nicht | OS-Test schlägt primär UND im Retry fehl | OS-Job läuft | Retry-Schritt Exit 1 → Job fehlgeschlagen, aber `continue-on-error` | Check neutral/warning, PR nicht blockiert; `::warning::` „persistent" sichtbar; `Analyze`-Report listet den Test | OS-Job-Protokoll; dokumentierte Rerun-Regel (`gh run rerun --job <id>` nur OS-Job) |
| 4 | Deterministischer Fehler blockiert weiterhin | Regulärer (unmarkierter) Test schlägt fehl | Blockierende Lane | `build-and-test` schlägt fehl | PR-Check rot — wie bisher; kein Retry, kein Ausweg | CI-Verhalten unverändert; `version`/`prerelease` werden nie erreicht |
| 5 | Release-Gate bleibt deterministisch | Push → `main` / Tag, `released == 'true'` | Release-Workflow | `Release gate` führt `Category!=OsInterface` blockierend und `Category=OsInterface` mit `continue-on-error` aus | OS-Fehlschlag sichtbar, Release läuft weiter; regulärer Fehlschlag bricht ab | `release.yml`-Lauf + `test-results-release`-Artefakt |
| 6 | Flaky-Test ohne OsInterface wird verhindert | Entwickler vergibt `[Trait("Category","Flaky")]` ohne `OsInterface` | Meta-Test vorhanden | `FlakyConventionTests` scannt die Assembly | Meta-Test schlägt in der blockierenden Lane fehl — Verstoß wird CI-erzwungen | Unit-/Integrations-/E2E-Testlauf |
| 7 | Quarantäne-Empfehlung über aggregierte Läufe | Analyzer-Befund über ≥ 2 Flake-Ereignisse oder ≥ 20 % Fehlrate (≥ 5 Ausführungen) im 20-Runs-Fenster | TRX-Artefakte mehrerer Läufe heruntergeladen | `gh run download …` → `Analyze-FlakyTestResults.ps1` meldet „Quarantäne-Kandidat" → manuelles Issue (`flaky-quarantine`, Dedup per `gh issue list`) + Fehlerbild in `os-interface-tests.md` | Issue sichtbar mit Label; Fehlerbild dokumentiert | Dokumentierter Prozess; Skript-Ausgabe |
| 8 | Cleanup-Fehler überdeckt keinen Assert | Dateisperre/Read-only im Temp-Verzeichnis beim Dispose | `TempDirectoryCleanup` vorhanden | Dispose → `DeleteBestEffort` → Retry/Backoff → Restfehler nur `Console.Error` | Testergebnis zeigt den eigentlichen Assert; kein `AggregateException`-Masking | `TempDirectoryCleanupTests` (locked/missing/read-only) |

## Offene Punkte

Keine.

— Alle fachlichen Ambiguitäten wurden im autonomen Lauf als getroffene Entscheidungen festgehalten
(siehe „Designentscheidungen" und „Scope-Ausschlüsse"); Blaupause-Stand: PR #432 gemergt, Remote `main` eingesehen.
