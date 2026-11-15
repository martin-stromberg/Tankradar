# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

- [x] `.github/workflows/review-architecture-security.yml` (Workflow-Datei) — angelegt; `name: Monthly Architecture & Security Review`, Cron `0 6 1 * *` + `workflow_dispatch`, `permissions: issues: write`, `concurrency`-Gruppe `review-architecture-security` mit `cancel-in-progress: false`, Job auf `ubuntu-latest` mit `timeout-minutes: 5`, kein Checkout
- [x] Schritt „Label sicherstellen" in `review-architecture-security.yml` — `gh label create review-architecture-security --color "B60205" ... --force` mit `GH_TOKEN: ${{ github.token }}` (Zeile 32)
- [x] Schritt „Issue bei Bedarf erzeugen" in `review-architecture-security.yml` — Dedup per `gh issue list --state open --label review-architecture-security --json number` + `jq length` (Zeilen 38–41); `gh issue create` mit Titel `Architektur- und Sicherheitsreview – $(date -u +%Y-%m)` und wörtlichem dekodiertem Body ab `@softwarewerkstatt` (Zeilen 42–64; Body stimmt exakt mit Plan-Abschnitt „Issue-Bodies" überein)
- [x] Erklärender Kopf-Kommentar in `review-architecture-security.yml` — Zweck, Dedup-Logik, Schedule-Aktivierung erst auf `main`, 60-Tage-Hinweis (Zeilen 3–11)
- [x] `.github/workflows/review-nuget-packages.yml` (Workflow-Datei) — angelegt; `name: Weekly NuGet Package Review`, Cron `0 5 * * 2` (Di 05:00 UTC, entschiedener Offener Punkt 1) + `workflow_dispatch`, identischer Aufbau; Label `review-nuget-packages` (`FBCA04`), Titel `NuGet-Paketprüfung – $(date -u +%Y-%m-%d)`, NuGet-Body wörtlich übernommen inkl. PR-Anweisung
- [x] `.github/workflows/review-compliance.yml` (Workflow-Datei) — angelegt; `name: Yearly Compliance & Copyright Review`, Cron `0 6 1 1 *` (1. Januar, 06:00 UTC) + `workflow_dispatch`, identischer Aufbau; Label `review-compliance` (`5319E7`), Titel `Compliance- und Copyright-Review – $(date -u +%Y)`, Compliance-Body wörtlich übernommen
- [x] `docs/help/ci-cd/workflows.md` — Überschrift „Die elf Workflows"; Tabelle um vier Zeilen erweitert (`pages.yml` plus drei `review-*.yml` = elf Einträge, entschiedener Offener Punkt 5); Kurzabsatz zum Issue-/Dedup-Muster (Label anlegen, `gh issue list`/`gh issue create`, keine Analyse im Workflow, `issues: write`, Schedule erst auf `main`) vorhanden
- [x] `docs/help/ci-cd/README.md` — neuer Ablaufpunkt 6 „Zeitgesteuert — Review-Issues" mit den drei Rhythmen, `@softwarewerkstatt`-Erwähnung, Dedup und `main`-Vorbehalt
- [x] `docs/help/ci-cd/einrichtung.md` — Abschnitt „3. Labels" um `review-architecture-security` (`B60205`), `review-nuget-packages` (`FBCA04`), `review-compliance` (`5319E7`) inkl. Hinweis auf automatisches Anlegen beim ersten Lauf ergänzt
- [x] `changes.log` — neuer Eintrag `## 2026-11-14 — Automatische Review-Issues per Cron (Issue #58)` oben, mit Stichpunkten zu Workflows, Dedup, Dokumentation und Abschluss „Keine Schemaänderung."
- [x] Validierungsregel eingehalten — `python scripts/validate-workflows.py` zeigt ausschließlich die zwei preexisting `e2e-dumps`-Befunde (`pr-staging-ci.yml`, `staging-ci.yml`), keine neuen Befunde aus den drei Workflows; `python scripts/test_validate_workflows.py` unverändert 13/14 (`FAILED (failures=1)`, preexisting)

## Offene Aufgaben

- [ ] `workflow_dispatch`-Probelauf je Workflow auf GitHub (Umsetzungsreihenfolge Schritt 7 / Task 10) — nicht umsetzbar vor der Promotion: `schedule`/`workflow_dispatch` wirken erst, wenn die Dateien auf dem Default-Branch `main` liegen (plangemäßes Plattformverhalten). Nach Merge `staging → main` je Workflow einmal manuell auslösen, Issue (Titel/Label/`@softwarewerkstatt`-Body) und Dedup beim zweiten Lauf prüfen, Ergebnis in `test-results.md` dokumentieren.

## Hinweise

- Es gibt keine Implementierungslücke: Alle im aktuellen Stand ausführbaren Planelemente (drei Workflow-Dateien, Dokumentation, Changelog, lokale Validierung) sind vollständig umgesetzt. Die einzige offene Aufgabe ist der plangemäß auf die Zeit nach der Promotion verschobene Funktionsnachweis — dafür ist keine Nachimplementierung nötig, sondern nur der dokumentierte Probelauf auf GitHub.
- Issue-Bodies sind korrekt als UTF-8-dekodiertes Deutsch eingebettet (Umlaute `führe`, `Änderungsvorschläge` vorhanden); das Heredoc steht auf Einrückungsebene des `run: |`-Blocks, sodass die Zeilen im ausgeführten Skript bündig beginnen und `@softwarewerkstatt` die erste Body-Zeile bleibt.
- Preexisting-Rot unverändert: `validate-workflows.py` und `test_workflows_in_repository_are_clean` schlagen weiter wegen `e2e-dumps` fehl (Scope-Ausschluss 5 / Issue #22) — im PR-Text als preexisting kennzeichnen.
- `docs/help/ci-cd/index.md` ist unverändert; die Tabelle in `workflows.md` zählt nun korrekt elf Workflows (7 bisherige + `pages.yml` + 3 neue).
