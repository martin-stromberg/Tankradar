# Bestandsaufnahme: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews (Issue #58)

Analysiert wurde die CI/CD-Ebene des Repositories (GitHub-Actions-Workflows, Workflow-Validierung, CI-Dokumentation und Changelog-Konventionen) bezogen auf die Anforderung, drei neue zeitgesteuerte Workflows anzulegen, die jeweils ein Issue mit `@softwarewerkstatt`-Erwähnung und Duplikatvermeidung erzeugen. Es sind keine Anwendungsklassen (Datenmodell, Services, UI) betroffen.

## Zusammenfassung

- Unter `.github/workflows/` existieren **acht Workflow-Dateien**; kein einziger erzeugt GitHub-Issues und kein einziger ruft `gh issue` auf.
- **`security-scan.yml`** ist der einzige bestehende `schedule`-Trigger (Cron `0 4 * * 1`, Montag 04:00 UTC) und der einzige Workflow mit `workflow_dispatch` neben `pages.yml`. Er führt eine reine Schwachstellensuche aus (Composite-Action `.github/actions/security-scan`), erzeugt kein Issue und soll laut Anforderung unverändert bleiben.
- **Dedup- und Label-Muster vorhanden, aber für PRs statt Issues:** `staging-to-main-promotion.yml` und `sync-staging-with-main.yml` legen Labels idempotent per `gh label create ... --force` an (`automated-promotion`, `automated-backmerge`) und prüfen Duplikate per `gh pr list ... --json number | jq length`. Ein Issue-Dedup-Muster muss neu aufgebaut werden; `permissions: issues: write` und `GH_TOKEN: ${{ github.token }}` sind bereits etabliert.
- **`scripts/validate-workflows.py` schlägt im Ausgangszustand bereits fehl (preexisting):** Die Schritte „Upload E2E crash dumps" in `pr-staging-ci.yml` und `staging-ci.yml` laden `e2e-dumps/` hoch, aber `SAFE_UPLOAD_DIRS` (`scripts/validate-workflows.py:41`) enthält `e2e-dumps` nicht. Folge: `validate-workflows.py` endet mit Exit-Code 1 und der Test `test_workflows_in_repository_are_clean` in `scripts/test_validate_workflows.py` schlägt fehl — nachweislich bereits vor dieser Änderung.
- **`pages.yml` fehlt in der Workflow-Tabelle:** `docs/help/ci-cd/workflows.md` führt „Die sieben Workflows" (Zeile 8) — die Tabelle enthält alle Workflows außer `pages.yml`. Bei einer Erweiterung um die drei neuen Workflows ist die Überschrift anzupassen.
- **Issue-Texte liegen wörtlich vor:** `issue.md` enthält die drei deutschsprachigen Issue-Bodies mit `@softwarewerkstatt`-Erwähnung und Checklisten je Prüftyp (Zeilen 29–45, 54–71, 80–96).
- **Dokumentationsstellen identifiziert:** `docs/help/ci-cd/workflows.md` (Workflow-Tabelle, Baustein- und Skripttabellen), `docs/help/ci-cd/README.md` (Ablaufbeschreibung, Punkt 5 „Wöchentlich — Security Scan"), `docs/help/ci-cd/einrichtung.md` (Abschnitt „3. Labels"). `changes.log` folgt dem Muster `## YYYY-MM-DD — Titel (Issue #N)`; `docs/RELEASE_NOTES.md` ist zweisprachig geführt und steht aktuell auf „keine Änderungen".
- **Plattformhinweis:** `schedule`-Trigger wirken erst, sobald die Workflow-Dateien auf dem Default-Branch `main` liegen — im Staging-Workflow also erst nach der Promotion `staging → main`.

Test-Ausgangszustand: Alle ausgeführten Suiten sind grün bis auf den oben genannten bereits vorhandenen Fehler in der Workflow-Validierung — `npm test` 32/32, `test_validate_workflows.py` 13/14 (1 preexisting Fehlschlag), `validate-workflows.py` Exit 1 (2 preexisting Befunde), iOS-Deployment-Skriptprüfung vollständig OK, .NET Unit 2097/2097, Integration 298/298, E2E 177/177. Details und Nachweise: [Tests](inventory/tests.md).

## Details

- [Workflows und gh-CLI-Muster](inventory/workflows.md) — Bestand der acht Workflows, Trigger, Permissions, Dedup-/Label-Konventionen
- [Workflow-Validierung und Skripte](inventory/validation.md) — `validate-workflows.py`, Prüflogik, preexisting `e2e-dumps`-Befund
- [Dokumentation und Konventionen](inventory/docs.md) — betroffene Hilfeseiten, `changes.log`- und `RELEASE_NOTES.md`-Konventionen, wörtliche Issue-Texte
- [Tests und Test-Ausgangszustand](inventory/tests.md) — alle Testläufe mit Nachweisen unter `inventory/test-results/`
