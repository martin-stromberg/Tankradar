# Tasks: Flaky-Test-Härtung — OsInterface-Lane-Trennung, Deadline-Angleichung, Cleanup, Flaky-Retry (Issue #93)

| # | Bereich | Aufgabe | Status | Testnachweis |
|---|---------|---------|--------|--------------|
| 1 | Testinfrastruktur | `src/TestSupport/TestCategories.cs` anlegen (`OsInterface`, `Flaky`-Konstanten) | Offen | — |
| 2 | Testinfrastruktur | `src/TestSupport/TempDirectoryCleanup.cs` anlegen (`DeleteBestEffort`: Prozess-Kill, Retry/Backoff, Restfehler nur `Console.Error` — Blaupause-Port) | Offen | — |
| 3 | Testinfrastruktur | `src/TestSupport/FlakyConventionTests.cs` anlegen (Meta-Test: `Category=Flaky` ⇒ `Category=OsInterface`, scannt eigene Assembly) | Offen | — |
| 4 | Testinfrastruktur | `TestCategories.cs`, `TempDirectoryCleanup.cs`, `FlakyConventionTests.cs` in `Tankradar.Tests.Unit.csproj` linken | Offen | — |
| 5 | Testinfrastruktur | Dieselben drei Dateien in `Tankradar.Tests.Integration.csproj` linken | Offen | — |
| 6 | Testinfrastruktur | Dieselben drei Dateien in `Tankradar.Tests.E2E.csproj` linken | Offen | — |
| 7 | Testinfrastruktur | `E2EStartupPolicy.ResolveHostPollTimeout` ergänzen (`max(30 s, 2 × UIA-Timeout)`) | Offen | — |
| 8 | Markierung | `[Trait("Category", OsInterface)]` auf `E2ETestBase` (deckt per Vererbung alle ~110 Nachfahren; `StartFailureCleanupE2ETests` bleibt unmarkiert) | Offen | — |
| 9 | Markierung | `[Trait("Category", OsInterface)]` auf `SearchMockServerTestBase` (deckt ~13 `*MockServerTests_*` inkl. `RouteSearchMockServerTestBase`) | Offen | — |
| 10 | Markierung | `[Trait("Category", OsInterface)]` auf `LoopbackHttpListenerTests_Retry`, `ApiKeyBuildTests_Log`, `ApiKeyBuildTests_Sources` | Offen | — |
| 11 | Markierung | `[Trait("Category", OsInterface)]` auf `MockOpenRouteServiceServerTests`, `OpenRouteServiceClientTests_Key`, `OpenRouteServiceKeyCheckerTests` (Unit) | Offen | — |
| 12 | Markierung | `[Trait("Category", Flaky)]` auf `SearchE2ETests_LargeResult` (dokumentiertes Flake-Ereignis) | Offen | — |
| 13 | Markierung | Lane-Nachweis per `dotnet test --no-build --list-tests --filter "Category=OsInterface"`/`!=` je Projekt | Offen | — |
| 14 | Deadline-Angleichung | `E2ETestBase.NavigateToTab`: `attemptTimeout` → `HostPollTimeout`, `maxAttempts` 5 → 3 | Offen | — |
| 15 | Deadline-Angleichung | `E2ETestBase.WaitForTab`-Aufruf (Z. 143): 30 s → `HostPollTimeout` | Offen | — |
| 16 | Deadline-Angleichung | `E2ETestBase.WaitForPageTitle` (Z. 196): 30 s → `HostPollTimeout` | Offen | — |
| 17 | Deadline-Angleichung | `E2ETestBase.SkipSetup`: Sichtbarkeitswarte 30 s und Ausblendwarte 6 s → `HostPollTimeout` | Offen | — |
| 18 | Deadline-Angleichung | `SettingsE2ETestBase.DefaultTimeout` (Z. 18): 20 s → `HostPollTimeout` | Offen | — |
| 19 | Cleanup-Härtung | `E2ETestBase.Dispose`: `Directory.Delete` → `TempDirectoryCleanup.DeleteBestEffort` | Offen | — |
| 20 | Cleanup-Härtung | `TestDataContext.Cleanup`: `Directory.Delete` (ohne Catch) → `DeleteBestEffort` | Offen | — |
| 21 | Cleanup-Härtung | `ApiKeyBuildRunner.Dispose`: `Directory.Delete` → `DeleteBestEffort` | Offen | — |
| 22 | Cleanup-Härtung | `TempDirectory.Dispose` (Unit): `Directory.Delete` → `DeleteBestEffort` | Offen | — |
| 23 | Cleanup-Härtung | `DiagnosticsCaptureE2ETests`-`finally` (ohne Catch) → `DeleteBestEffort` | Offen | — |
| 24 | Cleanup-Härtung | `Directory.Delete` in `TileSourceMockServerTests_Flow`, `BackupCryptoTests`, `HttpTileSourceTests_*` (4 Dateien), `ReceiptPhotoStoreTests_*` (2 Dateien) → `DeleteBestEffort` | Offen | — |
| 25 | Skripte | `scripts/lib/Trx.ps1` anlegen (Blaupause-Port: TRX-Parsing-Helfer) | Offen | — |
| 26 | Skripte | `scripts/lib/TestDiscovery.ps1` anlegen (`Get-FlakyTestNames` via `--list-tests --filter "Category=Flaky"`) | Offen | — |
| 27 | Skripte | `scripts/Retry-FlakyTests.ps1` anlegen (genau ein Re-Run der Flaky-Fehlschläge aus dem Primär-TRX) | Offen | — |
| 28 | Skripte | `scripts/Analyze-FlakyTestResults.ps1` anlegen (Flake-Statistik, Quarantäne-Schwellwerte, report-only Exit 0) | Offen | — |
| 29 | Skripte | Beide Skripte mit synthetischen TRX-Sätzen und echtem `--list-tests` verifizieren | Offen | — |
| 30 | Workflows | `pr-staging-ci.yml`: `Category!=OsInterface`-Filter in den drei Testschritten; E2E-Abweichungs-Kommentar ersetzen | Offen | — |
| 31 | Workflows | `pr-staging-ci.yml`: neuen Job `os-interface-tests` (`continue-on-error`, 60 min, 3 Testschritte `Category=OsInterface`, Retry `if: failure()`, Analyze `if: always()`, OS-Artefakte) | Offen | — |
| 32 | Workflows | `staging-ci.yml`: identische Filter- und Job-Änderungen; `version`-`needs` unverändert | Offen | — |
| 33 | Workflows | `release.yml`: Release-Gate in blockierende `Category!=OsInterface`-Schritte + `continue-on-error`-`Category=OsInterface`-Schritte + TRX-Upload `test-results-release` | Offen | — |
| 34 | Workflows | `scripts/validate-workflows.py`: `e2e-dumps` in `SAFE_UPLOAD_DIRS`; `workflow_run`-Namensprüfung nur bei `workflow_run`-Trigger | Offen | — |
| 35 | Lokale Prüfkette | `scripts/local-ci.ps1`: `Category!=OsInterface`-Filter in den drei Testschritten; `-BestEffort`-Schritt „Tests: OS-Schnittstellen" (`Category=OsInterface`, E2E-Anteil entfällt bei `-SkipE2E`) | Offen | — |
| 36 | Lokale Prüfkette | `.githooks/test-execution-check.py`: `--filter "Category!=OsInterface"` für Standard-Testziele (`PRE_PUSH_E2E=1` unverändert) | Offen | — |
| 37 | Tests | `TempDirectoryCleanupTests` (Unit): Read-only/normalize, locked-wirft-nicht, missing-wirft-nicht, Prozess-Kill (`OsInterface`) | Offen | — |
| 38 | Tests | `E2EStartupPolicyTests_HostPollTimeout` (Unit): lokal/CI/Override/Mindestwert | Offen | — |
| 39 | Dokumentation | `docs/help/stabilitaet/os-interface-tests.md` anlegen (Klassifizierung, Kategorien, Retry-/Rerun-/Quarantäne-Konvention, lokale Befehle, Fehlerbilder, Audit-Heuristik) | Offen | — |
| 40 | Dokumentation | `docs/help/ci-cd/workflows.md`: Job-/Skript-Tabellen, Abschnitt „E2E-Tests als Auslieferungs-Gate" → Lane-Modell, Abweichungs-Liste, neue Artefaktnamen | Offen | — |
| 41 | Dokumentation | `docs/help/ci-cd/lokaler-pruefung.md` und `docs/help/git-hooks/checks.md` aktualisieren (Lane-Trennung, Hook-Filter) | Offen | — |
| 42 | Verifikation | Lokal: `dotnet test --filter "Category!=OsInterface"` je Projekt grün; OS-Lane best-effort ausführen; `local-ci.ps1` grün | Offen | — |
| 43 | Verifikation | `python scripts/validate-workflows.py` und `scripts/test_validate_workflows.py` grün | Offen | — |
| 44 | Verifikation | PR erstellen, ≥ 3 komplette grüne `PR CI for Staging`-Durchläufe überwachen (`gh pr checks --watch --fail-fast`) | Offen | — |
