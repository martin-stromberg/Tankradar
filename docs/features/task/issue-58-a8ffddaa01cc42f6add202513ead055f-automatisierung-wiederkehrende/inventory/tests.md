# Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-14, Läufe ab 12:36 Uhr bis ca. 13:20 Uhr (UTC+01:00)
- Branch und Commit-ID: `task/issue-58-a8ffddaa01cc42f6add202513ead055f-automatisierung-wiederkehrende` @ `affd1a84d5c5b8ea1aebf48fb61538852c07132e` (2026-11-12 19:17:36 +0100)
- Uncommittete Änderungen im getesteten Stand: nur das unversionierte Feature-Verzeichnis `docs/features/task/issue-58-a8ffddaa01cc42f6add202513ead055f-automatisierung-wiederkehrende/` (Planungs-/Bestandsdokumente); keinerlei Änderungen an Produktivcode, Tests oder Konfiguration
- Testumgebung und Runtime-/SDK-Versionen: Windows (Entwicklungsrechner), .NET SDK 10.0.403 (MAUI-Workloads android/ios/maccatalyst/maui-windows installiert), Node.js v24.15.0, Python 3.13.14 (PyYAML vorhanden), PowerShell 7.6.10; `actionlint` **nicht** im `PATH` (Workflow-Validierung daher nur mit eingebauten Prüfungen)
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `npm test` → `node --test "scripts/*.test.mjs"` (`package.json`, Skripttests der CI-Hilfsskripte)
  - `python scripts/test_validate_workflows.py` (Unittests der Workflow-Validierung)
  - `python scripts/validate-workflows.py` (strukturelle Prüfung aller `.github/workflows/*.yml` und `.github/actions/*/action.yml`; Bestandteil von `scripts/local-ci.ps1` und dokumentiert in `docs/help/ci-cd/lokaler-pruefung.md`)
  - `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` (CI-Schritt „iOS deployment script checks" aus `staging-ci.yml`/`pr-staging-ci.yml`)
  - `dotnet test` für `src/Tankradar.Tests.Unit`, `src/Tankradar.Tests.Integration`, `src/Tankradar.Tests.E2E` (CI-Schritte `Test unit`, `Test integration`, `Test E2E with FlaUI (blocking)`); jeweils mit `IncludeAndroidTarget=false IncludeIosTarget=false IncludeMacCatalystTarget=false` (Windows-Job-Simulation wie in der CI), Konfiguration `Release`
  - `dotnet format Tankradar.sln --verify-no-changes --severity error` (CI-Schritt „Format check")
- Nicht ausgeführte CI-Prüfungen: Schwachstellenscan (`check-vulnerabilities.mjs`/`dotnet list package --vulnerable`, benötigt NuGet-Netzwerkzugriff), Coverage-Schwellenprüfung 70 % (Tests liefen ohne Coverage-Collector), iOS-Compile-Prüfung und Paketierung (macOS-spezifisch bzw. nur mit `-Package`), vollständiger `scripts/local-ci.ps1`-Gesamtlauf

## Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Skripttests (Node) | `npm test` | Repo-Root | 0 | 32 | 0 | 0 | [npm-test.log](test-results/npm-test.log) |
| Unittests Workflow-Validierung | `python scripts/test_validate_workflows.py` | Repo-Root | 1 | 13 | 1 | 0 | [python-validate-workflows-tests.log](test-results/python-validate-workflows-tests.log) |
| Workflow-Validierung | `python scripts/validate-workflows.py` | Repo-Root | 1 | – (keine Testzahlen; 2 Befunde) | – | – | [validate-workflows.log](test-results/validate-workflows.log) |
| iOS-Deployment-Skriptprüfung | `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` | Repo-Root | 0 | alle Prüfgruppen OK (ca. 90 Einzelprüfungen) | 0 | 0 | [ios-deployment-script-checks.log](test-results/ios-deployment-script-checks.log) |
| Build | `dotnet build Tankradar.sln --configuration Release --no-restore` | Repo-Root | 0 | 0 Warnungen, 0 Fehler | – | – | Konsolenausgabe (kein Bericht artefaktiert) |
| Unit-Tests | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build` | Repo-Root | 0 | 2097 | 0 | 0 | [dotnet-test-unit.log](test-results/dotnet-test-unit.log) |
| Integrationstests | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build` | Repo-Root | 0 | 298 | 0 | 0 | [dotnet-test-integration.log](test-results/dotnet-test-integration.log) |
| E2E-Tests (FlaUI) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build` | Repo-Root | 0 | 177 | 0 | 0 | [dotnet-test-e2e.log](test-results/dotnet-test-e2e.log) |
| Formatprüfung | `dotnet format Tankradar.sln --verify-no-changes --no-restore --severity error` | Repo-Root | 0 | – (keine Verstöße) | 0 | – | [dotnet-format-verify.log](test-results/dotnet-format-verify.log) |

Alle `dotnet`-Aufrufe liefen mit den Umgebungsvariablen `IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false` (Simulation der Windows-CI-Jobs).

## Nachgewiesene bestehende Testfehler

| Test-ID inkl. Testfall | Suite / Dateipfad | Fehlerbild / Fehlermeldung | Lauf und Nachweis |
|-----------------------|------------------|---------------------------|-------------------|
| `ReleaseCommandTests.test_workflows_in_repository_are_clean` | `scripts/test_validate_workflows.py` (Z. 80–86) | `AssertionError: Lists differ: [] != [...]` — zwei Befunde: Schritt „Upload E2E crash dumps" lädt `e2e-dumps/` hoch („lädt ein Verzeichnis hoch, das eine .ipa enthalten könnte") in `pr-staging-ci.yml` Job `build-and-test` und `staging-ci.yml` Job `build-and-test` | `python scripts/test_validate_workflows.py`, [Log](test-results/python-validate-workflows-tests.log) |

Dieselbe Ursache lässt `python scripts/validate-workflows.py` mit Exit-Code 1 und identischen zwei Fehlermeldungen enden ([Log](test-results/validate-workflows.log)). Grund: `e2e-dumps` fehlt in `SAFE_UPLOAD_DIRS` (`scripts/validate-workflows.py:41`); die Uploads wurden für Issue #22 eingeführt. **Der Fehler ist preexisting** — er tritt auf dem unveränderten Basisstand (Commit `affd1a8`) auf, ohne dass ein Feature-Code geändert wurde.

## Testlücken und Ausführungsprobleme

- **`actionlint` nicht installiert:** `validate-workflows.py` führte nur die eingebauten Prüfungen aus (Hinweiszeile im Log). Tiefergehende actionlint-Prüfung entfällt lokal wie dokumentiert.
- **Nicht ausgeführt:** Schwachstellenscan (Netzwerkbedarf), Coverage-Prüfung (kein Coverage-Collector im Lauf), iOS-Build/Compile-Prüfung (macOS erforderlich), Windows-Paketierung, vollständiger `local-ci.ps1`-Gesamtlauf. Über diese Bereiche liegt kein Ausgangsnachweis vor.
- **Bekannte, nicht testrelevante Protokolleinträge im E2E-Lauf:** „Fatal error." und „Application failed to exit" erscheinen im Log — dokumentierte Folgen des seit Issue #22 beobachteten sporadischen Absturz-/Beendigungsproblems der App; alle 177 Tests bestanden dennoch.
- Keine übersprungenen oder abgebrochenen Tests in den ausgeführten Suiten.

## Testklassen

Die Anforderung betrifft keine Anwendungsklassen; relevante bestehende Testartefakte auf CI-Ebene:

### `ReleaseCommandTests` (`scripts/test_validate_workflows.py`)
- `test_workflows_in_repository_are_clean` — prüft alle Workflows/Actions des Repos gegen die Validierungsregeln (aktuell rot, s. o.)
- `test_current_release_commands_are_allowed`, `test_ipa_asset_is_rejected`, `test_directory_pattern_and_variable_assets_are_rejected`, `test_label_suffix_and_other_commands_are_handled` — `.ipa`-Schutzprüfungen

### `UploadArtifactTests` (`scripts/test_validate_workflows.py`)
- `test_known_safe_uploads_are_allowed`, `test_explicit_ipa_is_rejected`, `test_directories_that_could_contain_an_ipa_are_rejected`, `test_patterns_and_unknown_extensions_are_rejected`, `test_multiline_paths_are_checked_individually`, `test_unverifiable_and_escaping_paths_are_rejected` — Artefakt-Pfadprüfung

### `WindowsPackageKeyPolicyTests` (`scripts/test_validate_workflows.py`)
- `test_windows_package_action_clears_all_key_variables`, `test_windows_package_action_has_no_key_inputs`, `test_package_script_has_no_routing_key_handling` — Schlüsselschutz des Windows-Pakets

### Skripttests (`scripts/*.test.mjs`, via `npm test`)
- `check-coverage.test.mjs` (3 Tests), `check-vulnerabilities.test.mjs` (4), `detect-backmerge.test.mjs` (4), `resolve-release-version.test.mjs` (9), `versioning.test.mjs` (12) — 32 Tests, alle grün

## Hilfsmethoden

- `upload_errors(path, name)` / `release_errors(run)` in `scripts/test_validate_workflows.py` (Z. 14–23): rufen `check_upload_artifact` bzw. `check_release_commands` des Validierungsmoduls direkt auf und sammeln Fehlerlisten — Muster für etwaige neue Validierungstests.
