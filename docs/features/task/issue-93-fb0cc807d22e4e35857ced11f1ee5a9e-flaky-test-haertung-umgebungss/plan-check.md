# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

Die Anforderung definiert sieben Maßnahmen (Implementierungsansatz Schritte 1–7) plus die
Scope-Festlegungen; diese werden als Akzeptanzkriterien abgeglichen.

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| 1. Audit umgebungssensitiver Tests (Heuristik: `Process.Start`, FlaUI/UIA, Loopback, FS-Szenarien, Kindprozess-Budgets; Fundliste festhalten) | Fundliste liegt in `inventory/audit.md` (Kandidaten A–D, Deadline-Matrix, Cleanup-Tabelle); Markierungsentscheidungen je Klasse/Familie in „Designentscheidungen" festgelegt und begründet (E2E per `E2ETestBase`-Trait, `SearchMockServerTestBase`, `LoopbackHttpListenerTests_Retry`, `ApiKeyBuildTests_{Log,Sources}`, 3 Unit-`HttpListener`-Klassen; Dateisystem/SQLite bewusst **nicht** markiert) | Lane-Nachweis per `dotnet test --no-build --list-tests --filter` je Projekt (Umsetzungsschritt 5); Meta-Test `FlakyConventionTests` | Abgedeckt |
| 2. Markierung: `Category=OsInterface` + `Flaky` nur mit `OsInterface`, zentrale Konstanten | `TestCategories` in `src/TestSupport`; natives `[Trait("Category", …)]` (xUnit 2.9.3, keine `ITraitAttribute`-Portierung — korrekte v2-Mechanik); `Flaky` nur auf `SearchE2ETests_LargeResult` (dokumentiertes Flake-Ereignis) | `FlakyConventionTests` (Reflexions-Meta-Test, per `<Compile Include>` in alle 3 Projekte gelinkt — Shared-Source-Mechanik berücksichtigt); Akzeptanzszenario 6 | Abgedeckt |
| 3. Lane-Trennung: blockierende Lane `Category!=OsInterface`, best-effort-Job `Category=OsInterface` (`continue-on-error`, großzügiges Timeout), `needs`-Kette unberührt, TRX separat | Neuer Job `os-interface-tests` (`needs: detect-backmerge`, `continue-on-error: true`, `timeout-minutes: 60`) in `pr-staging-ci.yml` + `staging-ci.yml`; `version`-Kette verifiziert unverändert (`needs: [detect-backmerge, static-checks, build-and-test]`); Artefakte `test-results-os-interface-{pr,staging}`; zusätzlich `release.yml`-Gate-Split (blockierend/`continue-on-error`-Schritte) | Akzeptanzszenarien 1 (Happy Path), 3, 4, 5; lokale `--list-tests`-Nachweise; ≥ 3 CI-Durchläufe (Umsetzungsschritt 10) | Abgedeckt |
| 4. Deadline-Angleichung: Host-Poll-Frist > Kind-Budget | `E2EStartupPolicy.ResolveHostPollTimeout` = `max(30 s, 2 × UIA)` → 30 s lokal / 120 s CI; konkrete Umstellung in `E2ETestBase` (`NavigateToTab`, `WaitForTab`, `WaitForPageTitle`, `SkipSetup`) und `SettingsE2ETestBase.DefaultTimeout`; `ApiKeyBuildRunner`-5-min-Budget vs. 60-min-Job als konsistent bewertet; `StartAndConnect`-`totalLimit` verifiziert unverändert konsistent | `E2EStartupPolicyTests_HostPollTimeout` (lokal/CI/Override/Mindestwert); Akzeptanzszenario-Implikation über Lane-Runs | Abgedeckt |
| 5. Cleanup-Härtung: Prozessbaum-Kill, `Directory.Delete` mit Retry/Backoff, Restfehler nur protokollieren, kein Assert-Masking | `TempDirectoryCleanup.DeleteBestEffort` (Portierung der Blaupause: 5 Versuche, linearer Backoff, `processesToKill`, `normalizeAttributes`, wirft nie → `Console.Error`); vollständige Umstellungsliste aller `Directory.Delete`-Stellen aus dem Audit (`E2ETestBase.Dispose`, `TestDataContext.Cleanup`, `ApiKeyBuildRunner.Dispose`, `TempDirectory.Dispose`, `DiagnosticsCaptureE2ETests`, `TileSourceMockServerTests_Flow`, `BackupCryptoTests`, `HttpTileSourceTests_*` ×4, `ReceiptPhotoStoreTests_*` ×2) | `TempDirectoryCleanupTests` (read-only/gesperrt/fehlend/Prozess-Kill); Akzeptanzszenario 8 | Abgedeckt |
| 6. Retry-/Quarantäne-Konvention: `Flaky`-Markierung, genau ein automatisierter Re-Run markierter Fehlschläge, dokumentierte `gh run rerun --job <id>`-Regel | `scripts/Retry-FlakyTests.ps1` + `scripts/lib/{Trx,TestDiscovery}.ps1` (Blaupause-Port); Re-Run-Schritt `if: failure()`, `shell: pwsh`, genau ein Re-Run je Lauf und Projekt auf die Schnittmenge `Category=Flaky` ∩ Fehlschläge; Rerun-Regel dokumentiert (nur OS-Job, nie `--failed`, nie blockierende Lane) | Akzeptanzszenarien 2 (recovered), 3 (persistent, nicht blockierend); Skript-Verifikation mit synthetischen TRX-Sätzen (Umsetzungsschritt 6) | Abgedeckt |
| 7. Flake-Statistik: TRX-basierter Report-Schritt, nicht blockierend | `scripts/Analyze-FlakyTestResults.ps1` (`if: always()`, Exit immer 0, Klassifikation Quarantäne-Kandidat/beobachten/stabil, `::warning::`); Schwellwerte als Skript-Parameter mit Blaupause-Defaults (2 / 0,2 / 5 / 0,1 / 20) | Akzeptanzszenario 7 (Quarantäne-Empfehlung über aggregierte Läufe); synthetische TRX-Verifikation | Abgedeckt |
| 8. Verifikation & Doku: ≥ 3 CI-Durchläufe, `validate-workflows.py` muss bestehen, `workflows.md`/Skript-Tabelle/Stabilitäts-Doku | Umsetzungsschritte 9–10: `docs/help/stabilitaet/os-interface-tests.md` (neu), `workflows.md` (Job-/Skript-Tabellen, „E2E-Tests als Auslieferungs-Gate" → Lane-Modell), `lokaler-pruefung.md`, `checks.md`; ≥ 3 grüne PR-CI-Läufe (`gh pr checks --watch --fail-fast`); `validate-workflows.py` + `test_validate_workflows.py` müssen grün sein — dazu zwei begründete Minimal-Fixes am Validator (`SAFE_UPLOAD_DIRS` + `e2e-dumps`, `workflow_run`-Guard), die den preexisting Rot-Stand (3 Befunde) heilen | Umsetzungsschritt 7 (Validator grün), Schritt 10 (3 Läufe); Coverage-Verifikation als Pflichtschritt gegen `COVERAGE_THRESHOLD: 70` | Abgedeckt |
| Scope-Ausschluss „Kein Blanket-Retry" | Ausschluss 1 `bestätigt`; Re-Run semantisch auf Schnittmenge begrenzt (Offene Frage 6 entschieden: je Lauf und Projekt, ein Aufruf) | Szenarien 2–3 | Abgedeckt |
| Scope-Ausschluss „Keine App-Logik-Änderung" | Ausschluss 2 `bestätigt`; sämtliche Änderungen in `src/TestSupport`, Testprojekten, Workflows, Skripten, Hooks, Doku | — | Abgedeckt |
| Scope-Ausschluss „Keine Determinismus-Garantie" | Ausschluss 3 `bestätigt` | — | Abgedeckt |
| Scope-Ausschluss „Kein Entfernen/Skippen umgebungssensitiver Tests" | Ausschluss 4 `bestätigt`; markierte Tests laufen weiter in der OS-Lane | Szenario 1 | Abgedeckt |
| Scope-Ausschluss „Keine Coverage-Erweiterung" | Ausschluss 5 `bestätigt`; E2E bleibt ohne Collector, Schwelle nur blockierende Lane | Coverage-Verifikation Pflichtschritt | Abgedeckt |
| Kein `Run-AllTestsIndividually.ps1`-Port | Ausschluss 6 `bestätigt` (nicht angefordert) | — | Abgedeckt |
| Kein automatisches Quarantäne-Issue aus dem CI-Report | Ausschluss 7 `bestätigt` mit struktureller Begründung (Einzel-Run erreicht Fenster-Schwellen nicht; `issues: write` im best-effort-Job unerwünscht — Blaupause-Entscheidung); manueller Prozess mit Label `flaky-quarantine` + Dedup per `gh issue list` dokumentiert | Szenario 7 | Abgedeckt |

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Es gibt keinen über UI/Nutzeraktion erreichbaren Ablauf — die Anforderung liegt vollständig auf Testinfrastruktur-/CI-Ebene; der „Benutzerfluss" ist der CI-Pipeline-Lauf selbst | Keine neuen FlaUI-Tests; Funktionsnachweis über Akzeptanzszenarien (Lane-Verhalten, Retry, Quarantäne), `--list-tests`-Lane-Nachweise, Meta-/Unit-Tests und ≥ 3 CI-Durchläufe | Nicht erforderlich mit Begründung (im Plan explizit begründet, Abschnitt „E2E-Tests") |
| Bestehende E2E-Tests (Lane-Wechsel, Deadline-/Cleanup-Anpassung) | Anpassung technisch ohne Szenario-Änderung; `E2ETestBase`-Trait deckt alle ~110 Nachfahren per xUnit-v2-Vererbung ab (planseitig empirisch verifiziert, Fallback mechanisches Taggen + `--list-tests`-Verifikation) | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine — Status `Plan vollständig`.

## Fehlende oder unvollständige Planbestandteile

Keine — Status `Plan vollständig`.

Geprüfte Pflichtpunkte im Einzelnen:

- **Scope-Klärung:** vollständig gefüllt (Verarbeitungseinheit, auslösende Ereignisse, erwartete
  Wirkung, Umgang mit fehlenden Objekten inkl. expliziter Erzeugen-/Wiederverwenden-Liste).
- **Scope-Ausschlüsse:** alle 7 mit Begründung und Status `bestätigt` (autonomer Lauf,
  Orchestrator-Vorgabe — nachvollziehbar dokumentiert).
- **Akzeptanzszenarien:** 8 Szenarien; Happy Path (Nr. 1) führt vom Auslöser (PR → `staging`)
  bis zur sichtbaren Wirkung (grüne Checks, getrennte Artefakte); Ausgangszustände benannt.
- **Wartesituationen/async:** `if: failure()`/`if: always()`-Orchestrierung, Job-Timeout 60 min,
  `continue-on-error` auf Jobebene (korrekt: schlägt der Retry-Schritt fehl, bleibt der Job
  nicht-blockierend), Rerun-Wiederaufnahmeweg `gh run rerun --job <id>` dokumentiert.
- **Betroffene bestehende Tests:** vollständig benannt inkl. Begründung je Änderung.
- **Technik-Spotchecks gegen den Repo-Stand (verifiziert):**
  - `StartFailureCleanupE2ETests` erbt **nicht** von `E2ETestBase` (`src/Tankradar.Tests.E2E/E2E/FlaUI/StartFailureCleanupE2ETests.cs:9`) → bleibt unmarkiert in der blockierenden Lane, wie geplant.
  - Alle E2E-Testbasisklassen (`SettingsE2ETestBase`, `SearchE2ETestBase`, `RouteE2ETestBase`,
    `TankbookE2ETestBase`, `BackupE2ETestBase`, `HomeE2ETestBase`, `MapE2ETestBase`,
    `TankbookExportE2ETestBase`, `PriceServiceE2ETestBase`, `FavoritesE2ETestBase`) hängen
    transitiv an `E2ETestBase` → ein Klassen-Trait genügt; kein `[Fact]`/`[Theory]`-tragender
    Typ außerhalb dieser Hierarchie außer `StartFailureCleanupE2ETests`.
  - `version` in `staging-ci.yml` (Z. 220): `needs: [detect-backmerge, static-checks, build-and-test]` — OS-Job mit `needs: detect-backmerge` geht nicht in die Blockade ein.
  - `local-ci.ps1` besitzt das ungenutzte `Invoke-Step -BestEffort`-Flag (Z. 68) → geplanter
    OS-Schritt nutzt vorhandene Mechanik.
  - `release.yml` „Release gate - regular tests" (Z. 75–81) läuft ohne `--no-build` und ohne
    Filter → Gate-Split-Plan konsistent.
  - `docs/help/stabilitaet/` existiert nicht → „neu erzeugen" korrekt; Zieldateien der
    Doku-Anpassungen (`workflows.md`, `lokaler-pruefung.md`, `git-hooks/checks.md`) existieren.
  - `--filter "Category=…"` ist die korrekte vstest-Syntax für `[Trait("Category", …)]`;
    `--list-tests --filter` wird für den Lane-/Flaky-Nachweis unterstützt.

## Hinweise

Optionale Nachschärfungen für die Umsetzung (keine Vollständigkeitslücken):

- **`is_backmerge`-Guard:** `build-and-test` trägt `if: needs.detect-backmerge.outputs.is_backmerge != 'true'` (pr-staging-ci.yml Z. 109). Für den neuen `os-interface-tests`-Job ist die Bedingung nicht explizit im Plan benannt — bei der Umsetzung spiegeln, damit der OS-Job auf Backmerge-PRs nicht allein läuft.
- **Job-Setup-Details:** `runs-on: windows-latest`, `defaults.run.shell: bash` und die Schritt-Env (`vars.TANKRADAR_E2E_WINDOW`, `DOTNET_Dbg*`, `Include*Target=false`) sind im Programmablauf implizit („identischem Setup", „wie heute") — bei der Umsetzung 1:1 vom blockierenden Job übernehmen.
- **Trait-Vererbung:** Die Kernannahme (xUnit-v2-Klassen-Trait vererbt auf abgeleitete Testklassen) ist planseitig per Scratch-Projekt verifiziert und mit Fallback + `--list-tests`-Verifikation abgesichert — die Verifikation in Umsetzungsschritt 5 nicht auslassen.
- **`local-ci.ps1`-E2E-Schritt:** `-SkipE2E` überspringt nur den E2E-Anteil der OS-Lane; der bestehende blockierende E2E-Schritt führt künftig faktisch nur `StartFailureCleanupE2ETests` aus — bewusste, im Risikoabschnitt dokumentierte Konsequenz.
- **Quarantäne-Deviation:** Der Plan weicht bewusst vom Requirement-Wortlaut ab (kein automatisches Issue aus dem CI-Schritt; report-only + dokumentierter manueller Prozess). Als `bestätigter` Scope-Ausschluss 7 mit stichhaltiger Blaupause-Begründung dokumentiert — vom Orchestrator so vorgegeben.
