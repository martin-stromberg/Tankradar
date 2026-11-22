# Bestandsaufnahme: Flaky-Test-Härtung (Issue #93)

Bestandsaufnahme der Test- und CI-Infrastruktur (drei Testprojekte, `src/TestSupport`,
CI-Workflows, Prüfskripte) bezogen auf die übersetzte Anforderung „OsInterface-Lane-Trennung,
Deadline-Angleichung, gehärtetes Cleanup, Retry-/Quarantäne-Konvention, TRX-Flake-Statistik"
(Blaupause: `martin-stromberg/Softwareschmiede` PR #432).

## Zusammenfassung

- **Keine Trait-/Kategorie-Infrastruktur:** Das Repository nutzt keinerlei xUnit-Traits —
  weder `[Trait("Category", …)]` noch `--filter` in Workflows, `local-ci.ps1` oder Git-Hooks.
  Zentrale Kategorien-Konstanten (`TestCategories`), `Flaky`-Markierung und Meta-Test fehlen
  vollständig. Achtung: Die Blaupause setzt auf **xUnit v3** (`ITraitAttribute`), Tankradar auf
  **xUnit 2.9.3** — 1:1-Portierung der Attribute nicht möglich.
- **Vorhandene Härtungs-Bausteine (unverändert wiederverwendbar):** `TransientRetry`/
  `TransientWait` (UIA-Timeout-Retries mit Backoff/`totalLimit`), `E2EStartupPolicy`
  (15 s lokal/60 s CI, Env-Override, max. 2 App-Starts), `LoopbackHttpListener` (freier Port +
  10× Retry), vier Loopback-Mock-Server, `E2EDiagnostics` (Screenshot/UIA-Baum/error.txt),
  `Kill(entireProcessTree: true)` bereits in `E2ETestBase` und `ApiKeyBuildRunner`.
- **Fehlende Bausteine:** `TempDirectoryCleanup` (Directory.Delete mit Retry/Backoff, Fehler
  nur loggen), best-effort-CI-Job mit `continue-on-error`, `Retry-FlakyTests.ps1`,
  `Analyze-FlakyTestResults.ps1`, Stabilitäts-Doku (`docs/help/stabilitaet/` existiert nicht).
- **Deadline-Befund:** Mehrere E2E-Host-Poll-Fristen (20–30 s: `WaitForTab`,
  `WaitForPageTitle`, `SkipSetup`, `SettingsE2ETestBase.DefaultTimeout`, `NavigateToTab` 5×5 s)
  liegen **unter** dem CI-UIA-Timeout von 60 s — das Referenzfehler-Muster der Anforderung
  (Host-Deadline < Kind-Budget). Details: [audit.md](inventory/audit.md#deadline-matrix-host-poll-vs-kind-phasen-budget).
- **Cleanup-Befund:** `TestDataContext.Cleanup` löscht **ohne jede Fehlerbehandlung**
  (kann Asserts überdecken); `DiagnosticsCaptureE2ETests` löscht im `finally` ohne Catch;
  `E2ETestBase`/`ApiKeyBuildRunner`/`TempDirectory` fangen `IOException`(/`UnauthorizedAccess`)
  ohne Retry. `src/TestSupport` ist ein Shared-Source-Verzeichnis — neue Dateien müssen in
  jeder `.csproj` explizit gelinkt werden.
- **Audit-Vorabliste:** E2E-Projekt nahezu komplett OS-sensitiv (184 Tests, FlaUI +
  App-Prozess); `ApiKeyBuildTests_*` (echte `dotnet build`-Kindprozesse, 5-min-Budget);
  `LoopbackHttpListenerTests_Retry` + ~13 `*MockServerTests_*`-Integrationstests
  (echte Portbindung); drei Unit-Testklassen mit echtem `HttpListener`
  (`MockOpenRouteServiceServerTests`, `OpenRouteServiceClientTests_Key`,
  `OpenRouteServiceKeyCheckerTests`); ~9 Unit-/Integrationsdateien mit
  `Directory.Delete` im Dispose.
- **Dokumentierte „bewusste Abweichung":** E2E läuft in beiden CI-Workflows **blockierend**
  (Kommentar in `pr-staging-ci.yml` Z. 170–177/`staging-ci.yml` Z. 162–169, Abschnitt
  „E2E-Tests als Auslieferungs-Gate" in `docs/help/ci-cd/workflows.md`) — Fakt für die
  Lane-Entscheidung (Offene Frage 1). `version` hängt per `needs` an `build-and-test`.
- **`release.yml` führt ebenfalls Tests aus** (Unit+Integration im „Release gate",
  ohne `--no-build`, ohne Filter/TRX) — im Issue nicht genannt, aber von der Filterfrage berührt.
- **Weitere Ausführungsorte:** `scripts/local-ci.ps1` (alle drei Suiten blockierend, hat ein
  ungenutztes `-BestEffort`-Flag in `Invoke-Step`), `.githooks/test-execution-check.py`
  (pre-push: Unit+Integration ohne E2E, kein Filter, 10-min-Timeout).

**Test-Ausgangszustand:** Alle .NET-Testsuiten grün — Unit 2164/2164 (12 s),
Integration 311/311 (42 s), E2E 184/184 (18,5 min, lokal offscreen, UIA-Timeout 15 s);
npm test 32/32; iOS-Skriptprüfung OK. **Preexisting-Fehler:** `validate-workflows.py`
Exit 1 mit 3 Befunden (2× `e2e-dumps` nicht in `SAFE_UPLOAD_DIRS` — bekannter Alt-Befund —
plus `workflow_run`-Namensprüfung vs. inzwischen auf `push` umgestelltem
`staging-to-main-promotion.yml`); `test_validate_workflows.py` 13/14
(`test_workflows_in_repository_are_clean`, 2 `e2e-dumps`-Befunde). Nachweise:
[tests.md](inventory/tests.md).

## Details

- [TestSupport (geteilte Testinfrastruktur)](inventory/testsupport.md) — Helfer, Link-Mechanik, vorhandene/fehlende Bausteine
- [Testinfrastruktur der Testprojekte](inventory/test-infrastructure.md) — `E2ETestBase`, Testbasen, `TestDataContext`, `ApiKeyBuildRunner`, Umgebungsvariablen
- [Audit: OS-sensitive Tests, Deadlines, Cleanup](inventory/audit.md) — Kandidatenliste, Deadline-Matrix, `Directory.Delete`-Stellen
- [Workflows, CI-Skripte, lokale Prüfkette](inventory/workflows.md) — `pr-staging-ci.yml`, `staging-ci.yml`, `release.yml`, `local-ci.ps1`, `validate-workflows.py`, Hooks
- [Blaupause PR #432](inventory/blueprint.md) — Abgleich Bausteine vs. Tankradar-Stand, xUnit-v2-Hinweis
- [Tests](inventory/tests.md) — Test-Ausgangszustand, Lauf-Nachweise, preexisting Fehler
