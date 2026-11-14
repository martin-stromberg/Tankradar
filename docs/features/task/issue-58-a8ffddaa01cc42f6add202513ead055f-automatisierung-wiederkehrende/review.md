# Plan-Review

## Ergebnis

**Status:** Offene Aufgaben vorhanden

## Umgesetzte Planelemente

- [x] `.github/workflows/review-architecture-security.yml` (Workflow-Datei) — angelegt; `name: Monthly Architecture & Security Review`, Cron `0 6 1 * *` (1. des Monats, 06:00 UTC) + `workflow_dispatch`, `concurrency`-Gruppe `review-architecture-security` mit `cancel-in-progress: false`, Job auf `ubuntu-latest` mit `timeout-minutes: 5`
- [x] `.github/workflows/review-nuget-packages.yml` (Workflow-Datei) — angelegt; `name: Weekly NuGet Package Review`, Cron `0 5 * * 2` (Di 05:00 UTC, entschiedener Offener Punkt 1) + `workflow_dispatch`, `concurrency`-Gruppe `review-nuget-packages`, identischer Aufbau
- [x] `.github/workflows/review-compliance.yml` (Workflow-Datei) — angelegt; `name: Yearly Compliance & Copyright Review`, Cron `0 6 1 1 *` (1. Januar, 06:00 UTC) + `workflow_dispatch`, `concurrency`-Gruppe `review-compliance`, identischer Aufbau
- [x] `.github/actions/create-review-issue/action.yml` (Composite Action, neu seit Iteration 2) — gemeinsame Job-Logik aller drei Workflows: Schritt „Ensure label exists" (`gh label create "$LABEL" --color "$LABEL_COLOR" --description "$LABEL_DESCRIPTION" --force`, `GH_TOKEN: ${{ github.token }}`) und Schritt „Create review issue if none is open" (Dedup per `gh issue list --state open --label "$LABEL" --json number` + `jq length`, sonst `gh issue create --title "$TITLE – $(date -u +"$DATE_FORMAT")" --label "$LABEL" --body "$ISSUE_BODY"`); Inputs `label`, `color`, `description`, `title`, `date-format`, `body`; beide `run`-Schritte mit `shell: bash`
- [x] Label-Bilder je Workflow — `review-architecture-security` (`B60205`), `review-nuget-packages` (`FBCA04`), `review-compliance` (`5319E7`), jeweils mit deutscher Beschreibung, werden idempotent beim ersten Lauf angelegt
- [x] Issue-Titel mit Datumsteil — `Architektur- und Sicherheitsreview – %Y-%m`, `NuGet-Paketprüfung – %Y-%m-%d`, `Compliance- und Copyright-Review – %Y`; Trennzeichen ist das im Plan geforderte En-Dash (U+2013, verifiziert)
- [x] Issue-Bodies wörtlich — alle drei Bodies wurden per YAML-Parsing gegen die dekodierten Plan-Texte verglichen: exakte Übereinstimmung, erste Zeile jeweils `@softwarewerkstatt`, korrektes UTF-8-Deutsch ohne Mojibake/BOM
- [x] Dedup-Logik — Erzeugung wird übersprungen, wenn `jq length > 0`; Lauf endet erfolgreich mit Log-Hinweis (im Action-Schritt implementiert)
- [x] `GH_TOKEN: ${{ github.token }}` je Schritt; kein PAT, keine neuen Secrets; kein `--assignee` (entschiedener Offener Punkt 3)
- [x] Erklärende Kopf-Kommentare in allen drei Workflows — Zweck (nur Issue, Prüfung durch `@softwarewerkstatt`), Verweis auf die Composite Action inkl. Checkout-Begründung, `schedule` wirkt erst auf `main` (nach Promotion `staging → main`), Hinweis auf verzögerte/deaktivierte Scheduled Runs (60 Tage Inaktivität)
- [x] `docs/help/ci-cd/workflows.md` — Überschrift „Die elf Workflows"; Tabelle mit elf Zeilen (`pages.yml`-Zeile ergänzt plus drei `review-*.yml`-Zeilen mit Anzeigename, Trigger, Aufgabe); Kurzabsatz zum Issue-/Dedup-Muster inkl. Composite Action, `issues: write` + `contents: read` und `main`-Vorbehalt; zusätzlich Zeile für `.github/actions/create-review-issue` in der Tabelle „Gemeinsame Bausteine"
- [x] `docs/help/ci-cd/README.md` — neuer Ablaufpunkt 6 „Zeitgesteuert — Review-Issues" mit den drei Rhythmen, `@softwarewerkstatt`-Erwähnung, labelbasierter Dedup und `main`-Vorbehalt
- [x] `docs/help/ci-cd/einrichtung.md` — Abschnitt „3. Labels" um `review-architecture-security` (`B60205`), `review-nuget-packages` (`FBCA04`), `review-compliance` (`5319E7`) ergänzt, inkl. Hinweis auf automatisches Anlegen durch die Workflows/Action beim ersten Lauf
- [x] `changes.log` — neuer Eintrag `## 2026-11-14 — Automatische Review-Issues per Cron (Issue #58)` oben, Stichpunkte zu Workflows, Composite Action, Dedup, Permissions und Dokumentation, Abschluss „Keine Schemaänderung."; kein `RELEASE_NOTES.md`-Eintrag (plangemäß)
- [x] Validierungsregel eingehalten — `python scripts/validate-workflows.py` (heute erneut ausgeführt) meldet ausschließlich die zwei preexisting `e2e-dumps`-Befunde (`pr-staging-ci.yml`, `staging-ci.yml`); keine neuen Befunde aus den drei Workflows oder der Action; `check_run_steps` löst `uses: ./.github/actions/create-review-issue` korrekt gegen die vorhandene `action.yml` auf

## Offene Aufgaben

- [ ] `workflow_dispatch`-Probelauf je Workflow auf GitHub (Umsetzungsreihenfolge Schritt 7 / Task 10) — nicht umsetzbar vor der Promotion: `schedule`- und `workflow_dispatch`-Auslösung neuer Workflows wirken erst, wenn die Dateien auf dem Default-Branch `main` liegen (plangemäßes Plattformverhalten). Nach Merge `staging → main` je Workflow einmal manuell auslösen, Issue (Titel/Label/`@softwarewerkstatt`-Body) und Dedup beim zweiten Lauf prüfen, Ergebnis in `test-results.md` dokumentieren.

## Hinweise

- Es gibt keine Implementierungslücke: Alle im aktuellen Stand ausführbaren Planelemente (drei Workflow-Dateien, Composite Action, Dokumentation, Changelog, lokale Validierung) sind vollständig umgesetzt. Die einzige offene Aufgabe ist der plangemäß auf die Zeit nach der Promotion verschobene Funktionsnachweis — keine Nachimplementierung nötig.
- **Dokumentierte Strukturabweichung gegenüber zwei Designentscheidungen des Plans (aus Iteration-1-Code-Review, Befund „Doppelter Code"):** Statt inline eingebetteter Bash-Schritte mit Heredoc-Body ohne Checkout ruft jetzt jeder Workflow die Composite Action `.github/actions/create-review-issue` per `uses:` auf. Folge: `actions/checkout@v7` (Repo-Konvention) und zusätzlich `contents: read` neben `issues: write`. Die beobachtbaren Planfestlegungen bleiben erfüllt: gleiche Trigger, Labels, Titel, wörtliche Bodies, identischer `gh`-Aufrufsatz, `ubuntu-latest`, `timeout-minutes: 5`, `concurrency`, keine neuen Validierungsbefunde. Die Abweichung ist in den Kopf-Kommentaren, `workflows.md`, `einrichtung.md`, `changes.log` und der Tasks-Datei (Task 11) dokumentiert.
- Issue-Bodies liegen jetzt als `with: body`-Literalblöcke in den Workflow-Dateien — weiterhin 1:1 im jeweiligen Workflow prüfbar; `@softwarewerkstatt` bleibt die erste Body-Zeile (Literal-Block ohne führende Einrückung nach YAML-Dedent, verifiziert).
- Preexisting-Rot unverändert: `validate-workflows.py` und `test_workflows_in_repository_are_clean` schlagen weiter wegen `e2e-dumps` fehl (Scope-Ausschluss 5 / Issue #22) — im PR-Text als preexisting kennzeichnen.
