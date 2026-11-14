# Test-Ergebnisse

## Ergebnis

**Status:** Fehler vorhanden

Der einzige fehlgeschlagene Test (`test_workflows_in_repository_are_clean`) sowie die zwei
Befunde des manuellen Validierungslaufs sind **preexisting** und **nicht durch diese Änderung
verursacht** (Ursache: `e2e-dumps`-Upload in `pr-staging-ci.yml`/`staging-ci.yml`, bekannter
Defekt aus Issue #22, Scope-Ausschluss 5 des Plans). Die neuen Workflow-Dateien
(`review-architecture-security.yml`, `review-nuget-packages.yml`, `review-compliance.yml`)
und die Composite Action (`create-review-issue`) erzeugen **keine** eigenen Befunde.

## Fehlgeschlagene Tests

### scripts/test_validate_workflows.py

- **ReleaseCommandTests.test_workflows_in_repository_are_clean** — PREEXISTING (nicht durch diese Änderung verursacht): `assertEqual([], errors)` schlägt fehl mit genau den zwei bekannten `e2e-dumps`-Befunden in `pr-staging-ci.yml` und `staging-ci.yml` (Upload-Pfad fehlt in `SAFE_UPLOAD_DIRS`, `scripts/validate-workflows.py:41`; separater Defekt aus Issue #22).

## Ausgeführte Prüfungen

| Prüfung | Ergebnis | Bewertung |
|---------|----------|-----------|
| `python scripts/validate-workflows.py` | Exit 1; exakt die 2 preexisting `e2e-dumps`-Befunde (`pr-staging-ci.yml`, `staging-ci.yml`), keine neuen Befunde | Erwartungsgemäß (preexisting rot; neue Dateien befundefrei) |
| `python scripts/test_validate_workflows.py` | 14 Tests: 13 bestanden, 1 fehlgeschlagen (preexisting, s. o.) | Erwartungsgemäß unverändert 13/14 |
| YAML-Syntax (PyYAML `safe_load`) | `review-architecture-security.yml`, `review-nuget-packages.yml`, `review-compliance.yml`, `actions/create-review-issue/action.yml` — alle OK; `on:`-Blöcke enthalten `schedule` + `workflow_dispatch`; Crons verifiziert: `0 6 1 * *`, `0 5 * * 2`, `0 6 1 1 *` | Bestanden |
| `npm test` (`node --test "scripts/*.test.mjs"`) | **Nicht einschlägig** — nicht ausgeführt: Die Suite prüft ausschließlich die `.mjs`-Hilfsskripte unter `scripts/` (coverage, vulnerabilities, backmerge, versioning). Diese Änderung berührt keine `scripts/*.mjs`-Datei, sondern nur `.github/` und `docs/`/`changes.log`. | Nicht einschlägig |
| `dotnet test` (Tankradar.sln) | **Nicht einschlägig** — nicht ausgeführt: Keine Änderung an `src/`, `pages/` oder `*.csproj`; die Anforderung ist rein CI-seitig (Plan: „Keine neuen Unit- oder Integrationstests"). | Nicht einschlägig |

Hinweis: `actionlint` ist im PATH nicht verfügbar — die eingebauten Prüfungen von
`validate-workflows.py` liefen vollständig (Hinweiszeile im Lauf-Output).

## E2E-Abdeckung

Keine geplanten E2E-Tests: Der Plan (Abschnitt „E2E-Tests") legt fest, dass die Anforderung
keinen Benutzerfluss der Anwendung berührt; der Funktionsnachweis ist der
`workflow_dispatch`-Probelauf auf GitHub (siehe nächster Abschnitt) plus lokale
Workflow-Validierung.

## Reale End-to-End-Abnahme

Erforderlich (berührt externe Dienste/Integrationen: GitHub Issues/Labels via `gh`).

Vorab verifiziert, ob ein Auslösen möglich ist:

- `gh auth status`: authentifiziert (`workflow`-Scope vorhanden). Repo: `martin-stromberg/Tankradar`, Default-Branch `main`.
- `gh workflow list`: zeigt nur die 8 Bestands-Workflows — die drei `review-*`-Workflows sind auf GitHub nicht registriert.
- `gh api repos/martin-stromberg/Tankradar/contents/.github/workflows/review-*.yml?ref=main`: HTTP 404 für alle drei Dateien — sie liegen nicht auf dem Default-Branch.
- `git ls-remote origin`: der Task-Branch existiert remote nicht; die Workflow-Dateien sind lokal noch untracked (uncommitted).
- Probeaufruf `gh workflow run review-architecture-security.yml --ref <task-branch>`: HTTP 404 „workflow ... not found on the default branch" — `gh workflow run` funktioniert nur für Workflows, die auf dem Default-Branch existieren.

| Szenario (aus `plan.md`, Akzeptanzszenarien) | Durchgeführt über | Ergebnis |
|----------------------------------------------|-------------------|----------|
| 1 — Happy Path: monatliches Review-Issue (Cron/`workflow_dispatch` → Issue „Architektur- und Sicherheitsreview – <YYYY-MM>", Body ab `@softwarewerkstatt`, Label `review-architecture-security`) | `gh workflow run` auf Task-Branch geprüft | **Nicht durchführbar** — `schedule`/`workflow_dispatch` wirken nur auf dem Default-Branch `main`; Workflow-Datei dort nicht vorhanden (HTTP 404 verifiziert). Nachweis nach Promotion `staging → main` vorgesehen (Plan, Umsetzungsschritt 7). |
| 2 — Dedup: offenes Issue vorhanden → kein neues Issue, Lauf grün | Wie Szenario 1 (zweiter `workflow_dispatch`-Lauf) | **Nicht durchführbar** — gleiche Begründung; setzt Szenario 1 voraus. Dedup-Logik (`gh issue list --state open --label` + `jq length`) ist in der Composite Action vorhanden, aber erst auf `main` auslösbar. |
| 3 — Wöchentlicher (`review-nuget-packages`) und jährlicher (`review-compliance`) Rhythmus | `gh workflow run` analog | **Nicht durchführbar** — gleiche Begründung (HTTP 404 auf Default-Branch). |
| 4 — Vor der Promotion: Workflows inert, keine Läufe/Issues | `gh workflow list`, `gh api contents?ref=main`, `validate-workflows.py` | **Bestanden** — Workflows auf GitHub nicht registriert (nicht in `gh workflow list`, 404 auf `main`); lokale Validierung erzeugt keine neuen Befunde. |
| 5 — Label fehlt beim allerersten Lauf → `gh label create --force` legt es an | `gh workflow run` analog | **Nicht durchführbar** — gleiche Begründung wie Szenario 1. |

Die `Nicht durchführbar`-Befunde beruhen auf Plattformverhalten (GitHub Actions aktiviert
`schedule`/`workflow_dispatch` erst für Workflows auf dem Default-Branch) — sie sind **keine**
Fehlschläge der Implementierung, gelten aber gemäß Vorgehen nicht als Erfolg. Auflösung:
manueller Funktionsnachweis nach Promotion `staging → main` (Plan, Umsetzungsschritt 7) oder
dokumentierter Verzicht durch den Anwender.

## Zusammenfassung

- Gesamt: 14 (Unit-Tests `test_validate_workflows.py`)
- Bestanden: 13
- Fehlgeschlagen: 1 (preexisting, nicht durch diese Änderung verursacht)
- Übersprungen: 0

Zusätzlich: 4/4 YAML-Dateien syntaktisch valide; `validate-workflows.py` ohne neue Befunde;
reale Abnahme: 1 Szenario bestanden, 4 Szenarien nicht durchführbar (Plattformverhalten,
Nachweis nach Promotion vorgesehen).

## Testabdeckung

**Abdeckung:** Nicht messbar — die Änderung enthält keinen Anwendungscode (nur
`.github/workflows/review-*.yml`, `.github/actions/create-review-issue/action.yml`, Doku,
`changes.log`); für Workflow-Dateien existiert kein Coverage-Werkzeug.

## Fehlende Tests

Keine. Die bestehende Repository-Prüfung `test_workflows_in_repository_are_clean` erfasst
neue Workflow-Dateien automatisch und meldet für die drei neuen Dateien keine Befunde
(verifiziert: Fehlermeldung enthält ausschließlich `pr-staging-ci.yml` und `staging-ci.yml`).
