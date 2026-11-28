# Tests und Test-Ausgangszustand

## Test-Ausgangszustand vor der Umsetzung

- Zeitpunkt (mit Zeitzone): 2026-11-24, Läufe von ca. 17:41 Uhr bis 17:49 Uhr (UTC+01:00)
- Branch und Commit-ID: `task/issue-64-660ce9fb265f400aa6e64f6ac215eb07-eingebetteten-tankerkoenig-api` @ `0637207f946e042bacf6b46ee00727fc5c65ffe9` („feat: Tankerkönig-Antworten größenbegrenzt und streaming verarbeitet (#100)")
- Uncommittete Änderungen im getesteten Stand: nur das unversionierte Feature-Verzeichnis `docs/features/task/issue-64-660ce9fb265f400aa6e64f6ac215eb07-eingebetteten-tankerkoenig-api/` (Planungs-/Bestandsdokumente); keinerlei Änderungen an Produktivcode, Tests oder Konfiguration
- Testumgebung und Runtime-/SDK-Versionen: Windows (Entwicklungsrechner), .NET SDK 11.0.100 (`global.json`: `11.0.100`, `rollForward: latestPatch`; Workloads android/ios 27.2.11039/maccatalyst/maui-windows installiert), Node.js v24.15.0, Python 3.13.14, PowerShell 7.6.10; `actionlint` **nicht** im `PATH` (Workflow-Validierung daher nur mit eingebauten Prüfungen)
- Ermittelte Testsuiten und Quellen der Testbefehle:
  - `npm test` → `node --test "scripts/*.test.mjs"` (`package.json:10`; Skripttests der CI-Hilfsskripte — für diese Anforderung primär `resolve-release-version.test.mjs` und `versioning.test.mjs` relevant)
  - `python scripts/test_validate_workflows.py` (Unittests der Workflow-Validierung)
  - `python scripts/validate-workflows.py` (strukturelle Prüfung aller `.github/workflows/*.yml` und `.github/actions/*/action.yml`; Bestandteil von `scripts/local-ci.ps1`)
  - `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` (CI-Schritt „iOS deployment script checks" aus `staging-ci.yml`/`pr-staging-ci.yml`)
  - `dotnet restore`/`dotnet build`/`dotnet test` der drei Testprojekte mit Filter `Category!=OsInterface` (blockierende CI-Lane aus `staging-ci.yml`/`release.yml` Release-Gate), Konfiguration `Release`
  - `dotnet format Tankradar.sln --verify-no-changes --no-restore --severity error` (CI-Schritt „Format check")

## Testläufe

| Lauf | Befehl inkl. Filter | Arbeitsverzeichnis | Exit-Code | Erfolgreich | Fehlgeschlagen | Übersprungen | Nachweis |
|------|--------------------|--------------------|-----------|-------------|----------------|--------------|----------|
| Skripttests (Node) | `npm test` | Repo-Root | 0 | 32 | 0 | 0 | [npm-test.log](test-results/npm-test.log) |
| Unittests Workflow-Validierung | `python scripts/test_validate_workflows.py` | Repo-Root | 0 | 14 | 0 | 0 | [python-validate-workflows-tests.log](test-results/python-validate-workflows-tests.log) |
| Workflow-Validierung | `python scripts/validate-workflows.py` | Repo-Root | 0 | – (keine Testzahlen; „OK: 11 Workflows und alle lokalen Actions …") | 0 Befunde | – | [validate-workflows.log](test-results/validate-workflows.log) |
| iOS-Deployment-Skriptprüfung | `pwsh -NoProfile -File scripts/test-ios-deployment.ps1` | Repo-Root | 0 | 91 Einzelprüfungen | 0 | 0 | [ios-deployment-script-checks.log](test-results/ios-deployment-script-checks.log) |
| Restore + Build | `dotnet restore Tankradar.sln -p:Configuration=Release` + `dotnet build Tankradar.sln --configuration Release --no-restore` | Repo-Root | 0 | 0 Warnungen, 0 Fehler | – | – | [dotnet-restore-build.log](test-results/dotnet-restore-build.log) |
| Unit-Tests | `dotnet test src/Tankradar.Tests.Unit --configuration Release --no-build --filter "Category!=OsInterface"` | Repo-Root | 0 | 2155 | 0 | 0 | [dotnet-test-unit.log](test-results/dotnet-test-unit.log), [TRX](test-results/test-results-unit.trx) |
| Integrationstests | `dotnet test src/Tankradar.Tests.Integration --configuration Release --no-build --filter "Category!=OsInterface"` | Repo-Root | 0 | 199 | 0 | 0 | [dotnet-test-integration.log](test-results/dotnet-test-integration.log), [TRX](test-results/test-results-integration.trx) |
| E2E-Tests (blockierende Lane) | `dotnet test src/Tankradar.Tests.E2E --configuration Release --no-build --filter "Category!=OsInterface"` | Repo-Root | 0 | 2 | 0 | 0 | [dotnet-test-e2e.log](test-results/dotnet-test-e2e.log), [TRX](test-results/test-results-e2e.trx) |
| Formatprüfung | `dotnet format Tankradar.sln --verify-no-changes --no-restore --severity error` | Repo-Root | 0 | – (keine Verstöße) | 0 | – | [dotnet-format-verify.log](test-results/dotnet-format-verify.log) |

Alle `dotnet`-Aufrufe liefen mit `IncludeAndroidTarget=false`, `IncludeIosTarget=false`, `IncludeMacCatalystTarget=false` (Simulation der Windows-CI-Jobs). Die E2E-Lane enthält mit diesem Filter nur die nicht markierten Tests (`StartFailureCleanupE2ETests`, `FlakyConventionTests`) — alle FlaUI-Tests sind `Category=OsInterface` und laufen in der CI best-effort (Issue #93).

## Nachgewiesene bestehende Testfehler

Keine. Alle ausgeführten Suiten und Prüfungen waren erfolgreich. (Der in der Bestandsaufnahme zu Issue #58 dokumentierte preexisting Befund — `e2e-dumps/` nicht in `SAFE_UPLOAD_DIRS` — ist inzwischen behoben: `scripts/validate-workflows.py:41` enthält `e2e-dumps`, Validierung Exit 0.)

## Testlücken und Ausführungsprobleme

- **`actionlint` nicht installiert:** `validate-workflows.py` führte nur die eingebauten Prüfungen aus (Hinweiszeile im Log).
- **Nicht ausgeführt:**
  - `Category=OsInterface`-Tests aller drei Projekte (in der CI best-effort-Job; lokal nicht gestartet — über sie liegt kein Ausgangsnachweis vor),
  - Schwachstellenscan (`check-vulnerabilities.mjs`, benötigt NuGet-Netzwerkzugriff),
  - Coverage-Schwellenprüfung 70 % (Testläufe ohne Coverage-Collector),
  - iOS-Compile-Prüfung (`net11.0-ios`, macOS erforderlich; iOS-Workload lokal zwar installiert, aber kein Mac/Xcode),
  - Windows-Paketierung (`local-ci.ps1 -Package`, `package-windows.ps1`),
  - vollständiger `scripts/local-ci.ps1`-Gesamtlauf.
- Keine übersprungenen oder abgebrochenen Tests in den ausgeführten Suiten.

## Testklassen

Die Anforderung betrifft keine Anwendungsklassen; relevante bestehende Testartefakte auf Skript-/CI-Ebene:

### `scripts/resolve-release-version.test.mjs` (10 Tests, via `npm test`)
- `parseManualTag`/`classifyWorkflowRef` — Tag-Validierung und Ref-Klassifizierung (manual/automatic)
- `releaseHasExpectedAsset` — Asset-Vollständigkeit (`EXPECTED_ASSETS`)
- „Regression 11.1: Pre-Releases werden nie zur Reparatur ausgewählt" — `incompleteReleases`
- `resolveReleaseVersion`-Szenarien: new-release, already-released, repair-missing-assets (`upload-existing`), manual create, no-releasable-commits — **alle auf GitHub-Release-Objekten, müssen bei Umstellung auf Tag-Existenz angepasst werden**

### `scripts/versioning.test.mjs` (11 Tests) — Versionslogik (0.x-Regel, `nextRcNumber`, `displayVersion`); bleibt laut Anforderung unverändert

### `scripts/test_validate_workflows.py` (14 Tests)
- `UploadArtifactTests` — Artefakt-Pfadprüfung (`.ipa`-Verbot; enthält `artifacts/update.json` als erlaubten Pfad)
- `ReleaseCommandTests` — `gh release`-Asset-Regeln; `test_current_release_commands_are_allowed` trägt die heutigen Aufrufe wörtlich als erlaubt ein; `test_workflows_in_repository_are_clean` prüft alle Repo-Workflows gegen die Regeln
- `WindowsPackageKeyPolicyTests` — Schlüsselfreiheit des Windows-Pakets (Action-Inputs, leere Env, `package-windows.ps1` ohne Routing-Key-Handling)

### `scripts/test-ios-deployment.ps1` (91 Einzelprüfungen)
- Syntax, Comment-Based-Help, Abbruchverhalten ohne Mac, Hilfsfunktionen (Buildnummer, Transporter-Suche), Gleichlauf der IPA-Validierungs-Fehlertexte mit dem CI-Schritt, Ausblendbarkeit der Zielframeworks

### Anwendungstests mit Bezug zum Schlüsselmechanismus (unverändert)
- `src/Tankradar.Tests.Integration/Integration/ApiKeyBuildTests_Log.cs`, `ApiKeyBuildTests_Sources.cs` — Schlüssel nicht im Build-Log/Binlog, Quellenreihenfolge
- `src/Tankradar.Tests.Unit/Unit/ApiKeyProviderTests_Resolution.cs` — Laufzeit-Auflösung des API-Schlüssels

## Hilfsmethoden

- `upload_errors(path, name)` / `release_errors(run)` in `scripts/test_validate_workflows.py` (Z. 14-23): direkter Aufruf von `check_upload_artifact`/`check_release_commands` — Muster für neue Validierungstests.
- Injektion in `resolve-release-version.test.mjs`: `getRelease`/`listReleases`/`determineVersion` werden als Stubs übergeben (Z. 20) — Muster, in dem eine künftige Tag-basierte Existenzprüfung testbar bleibt.
