# Workflow-Validierung und Skripte

## `scripts/validate-workflows.py`

Prüft alle `.github/workflows/*.yml` und `.github/actions/*/action.yml` ohne Netzwerkzugriff (Docstring Zeilen 1–19; Einstieg `main()` Zeile 216):

| Prüfung | Ort | Beschreibung |
|---|---|---|
| Gültiges YAML, `on`-Trigger, Jobs mit `runs-on`+`steps` oder `uses` | `check_workflow` (Z. 169–191) | Pflicht für jede neue Workflow-Datei; `on` wird als unquotierter YAML-1.1-Boolean-Schlüssel behandelt (`trigger_block`, Z. 56–58) |
| `needs`-Verweis auf existierende Jobs | Z. 185–188 | – |
| Lokale Composite-Actions (`uses: ./...`) existieren | `check_run_steps` (Z. 61–70) | erwartet `action.yml`/`action.yaml` |
| Aufgerufene lokale Skripte `node scripts/...` bzw. `./scripts/...` existieren | `SCRIPT_CALL_RE` (Z. 38), Z. 63–65 | betrifft nur Aufrufe mit diesen Präfixen; `python scripts/...` wird nicht geprüft |
| `workflow_run`-Name von `staging-to-main-promotion.yml` == Anzeigename von `staging-ci.yml` | Z. 224–232 | hartcodierte Sonderprüfung |
| Kein `.ipa` als Artefakt/Release-Asset | `check_key_policy` (Z. 153–166), `check_upload_artifact` (Z. 142–150), `check_release_commands` (Z. 126–139) | Verzeichnis-/Muster-Uploads und nicht statisch prüfbare Pfade (Variablen, `${{ ... }}`, Backticks) gelten als Verstoß |
| Windows-Paket-Action ohne Tankerkönig-Schlüssel | Z. 159–162, 205–212 | – |
| `actionlint` (optional) | Z. 234–240 | nur wenn im `PATH`; auf der Entwicklungsumgebung **nicht** installiert |

Whitelist-Listen (Zeilen 41–43):

- `SAFE_UPLOAD_DIRS = {'coverage-report', 'TestResults', 'e2e-diagnostics'}`
- `SAFE_FILE_EXTENSIONS = {'.json', '.trx', '.xml', '.html', '.txt', '.md', '.log', '.zip', '.sha256'}`

`gh issue create`/`gh issue list`/`gh label create` werden von den Prüfungen **nicht** erfasst — `check_release_commands` reagiert ausschließlich auf `gh release create|upload` (`gh_release_assets`, Z. 96–123). Neue Workflows mit `gh issue`-Aufrufen erzeugen also keine zusätzlichen Befunde, solange sie kein `actions/upload-artifact` mit problematischen Pfaden oder `gh release` nutzen.

## Preexisting Fehler im Ausgangszustand

`scripts/validate-workflows.py` schlägt **bereits vor der Änderung** mit Exit-Code 1 fehl:

- `pr-staging-ci.yml` Job `build-and-test`, Schritt „Upload E2E crash dumps" (`.github/workflows/pr-staging-ci.yml:221–222`, Artefakt `e2e-dumps-pr`): `path: e2e-dumps/` — `e2e-dumps` fehlt in `SAFE_UPLOAD_DIRS`.
- `staging-ci.yml` Job `build-and-test`, Schritt „Upload E2E crash dumps" (`.github/workflows/staging-ci.yml:207–214`, Artefakt `e2e-dumps-staging`): gleicher Befund.

Der Verzeichnis-Upload `e2e-dumps/` wurde für Issue #22 (E2E-Absturzdiagnose) eingeführt, ohne `SAFE_UPLOAD_DIRS` zu erweitern. Derselbe Befund lässt den Repository-Test `test_workflows_in_repository_are_clean` (`scripts/test_validate_workflows.py:80–86`) fehlschlagen — Nachweis in [tests.md](tests.md) und `test-results/validate-workflows.log` bzw. `test-results/python-validate-workflows-tests.log`.

Folge für die Umsetzung: Die neuen Workflows können die Validierung allein nicht „grün" machen — der preexisting Befund bleibt bestehen, bis `SAFE_UPLOAD_DIRS` um `e2e-dumps` erweitert oder der Upload-Pfad anders abgesichert wird. Das ist ein eigenständiger, bereits dokumentierter Defekt.

## `scripts/test_validate_workflows.py`

Unittest-Suite (`unittest.main()`, 14 Tests) für die `.ipa`-Prüfungen: `UploadArtifactTests` (Z. 26–50), `ReleaseCommandTests` (Z. 53–86) inkl. des Repository-weiten Tests `test_workflows_in_repository_are_clean`, `WindowsPackageKeyPolicyTests` (Z. 89–106). Ausführung: `python scripts/test_validate_workflows.py`.

## Weitere CI-Hilfsskripte (Kontext)

| Skript | Bezug zur Anforderung |
|---|---|
| `scripts/check-vulnerabilities.mjs` (+ `.test.mjs`) | Auswertung des NuGet-Schwachstellenscans; unverändert relevant für den monatlichen Review, aber nicht Teil der Issue-Erzeugung |
| `scripts/local-ci.ps1` | Lokaler Prüflauf; führt u. a. `npm test` und die Workflow-Validierung aus — spiegelt den oben genannten preexisting Fehlschlag |
| `scripts/test-ios-deployment.ps1` | Skriptprüfung ohne Mac; im Ausgangslauf vollständig grün |
| `.github/actions/security-scan/action.yml` | Composite-Action (`runs.using: composite`); Vorlage für den Scan, nicht für Issue-Workflows |
