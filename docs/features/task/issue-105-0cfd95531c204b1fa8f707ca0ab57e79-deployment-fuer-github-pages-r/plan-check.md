# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

Die `requirement.md` enthält keinen explizit benannten Akzeptanzkriterien-Block; die Kriterien wurden aus „Fachlicher Scope" (erwartete Wirkung, Umgang mit fehlenden Objekten, Scope-Ausschlüsse) und dem vorgegebenen Ausrollpfad abgeleitet.

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| `actions/deploy-pages` findet das Artifact `github-pages` zuverlässig; Job läuft grün durch | Zwei-Job-Layout (`build` → `deploy` mit `needs`) plus Polling-Gate auf exakt dem Endpoint, den `deploy-pages` intern nutzt (`GET /repos/{repo}/actions/runs/{run_id}/artifacts`, `GH_TOKEN: ${{ github.token }}`, `actions: read` bereits in Top-Level-`permissions` vorhanden); Durchlasskriterium zwei aufeinanderfolgende positive Abfragen, Filter `name == github-pages && expired == false`, Budget ~2 min < `timeout-minutes: 10` | Pflicht-Szenario: `workflow_dispatch` auf `staging` (Environment-Policy `main`/`staging` verifiziert per `gh api .../deployment-branch-policies`) + Abnahme über automatischen Push-Lauf auf `main`; strukturelle Regression über `test_workflows_in_repository_are_clean` (`needs`-Referenz-Prüfung in `check_workflow`) | Abgedeckt |
| Kein neues Artifact erzeugen; das vom Upload finalisierte Artifact ist wiederzuverwenden | Polling wartet nur Sichtbarkeit ab; kein Re-Upload, kein zweiter `deploy-pages`-Schritt (Alternative Step-Retry begründet verworfen) | Akzeptanzszenario 3 (Wartefall) und 4 (Budget erschöpft → sauberer Schrittfehlschlag ohne halbdeployten Zustand) | Abgedeckt |
| Kein „Re-run failed jobs" auf Fehllauf `45541447133` (Duplikat-Artifact → „Multiple artifacts", Upstream `#338`) | Scope-Ausschluss 4 (bestätigt), Ausrollschritt 9 explizit ausgeschlossen, Akzeptanzszenario 5 dokumentiert den verbotenen Weg | Dokumentiert, bewusst kein Test | Abgedeckt |
| Ausrollung über bestehenden Branching-Workflow: Task-Branch → PR gegen `staging` (`Closes #105`) → Promotion `staging` → `main` mit Admin-Merges | Umsetzungsreihenfolge Schritte 4–7: PR gegen `staging`, Admin-Merge (Konto `martin-stromberg`, Keyring), `staging-to-main-promotion.yml`-Draft-PR, Admin-Merge nach `main` | PR-Checks auf `staging` als Voraussetzung; `verify-pr-source.yml`/`staging-to-main-promotion.yml` unverändert, nur Ablaufkontext | Abgedeckt |
| Geänderter Workflow löst auf `main` selbst den Verifikationslauf aus (Path-Filter) | Schritt 7: Push auf `main` triggert `pages.yml` (Workflow-Datei im eigenen Path-Filter) | Akzeptanzszenario 2: grüner Lauf + Environment `github-pages` mit `page_url` + Sichtprüfung `https://martin-stromberg.github.io/Tankradar/` | Abgedeckt |
| Keine inhaltliche/gestalterische Änderung an `pages/` | Scope-Ausschluss 1 (bestätigt); nur `pages/README.md`-Hinweisabsatz | — (Ausschluss, kein Test nötig) | Abgedeckt |
| Keine Änderung am Branching-/Promotionsprozess | Scope-Ausschluss 2 (bestätigt); beide Workflows nur Kontext | — | Abgedeckt |
| Kein Umbau der Pages-Infrastruktur; Source „GitHub Actions" bleibt | Scope-Ausschluss 3 (bestätigt); `build_type: workflow` per `gh api` belegt | — | Abgedeckt |
| Kein nachträgliches Re-Deploy verpasster Zwischenstände | Scope-Ausschluss 4 (bestätigt) | — | Abgedeckt |
| Offene Fragen der Anforderung entschieden | Zwei-Job-Layout (statt Retry) gewählt; `configure-pages@v6` bleibt (kein Kausalzusammenhang); Versionspin `deploy-pages` verworfen (`v5` = `v5.0.1`, gleicher SHA, kein neueres Release); Admin-Merges durch `martin-stromberg`; Dispatch-Kontrolle auf `staging` statt Task-Branch | Alle Entscheidungen in „Designentscheidungen" begründet; Environment-Policy in „Offene Punkte" verifiziert | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

—

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| End-to-End-Ablauf „Workflow-Lauf publiziert Page" (einziger externer Auslöser → sichtbare Wirkung) | Realer `workflow_dispatch`-Lauf auf `staging` nach dem Merge (Task-Branch-Dispatch unmöglich — verifizierte Environment-Policy `main`/`staging`): `build` + `deploy` grün, Polling protokolliert Sichtbarkeit, Page publiziert; Nachweis in `test-results.md` | Abgedeckt (realer Workflow-Lauf — einzig mögliche Umgebung für den Race) |
| Endabnahme der Anforderung | Automatischer Push-Lauf auf `main` nach Promotion (Path-Filter enthält `pages.yml`) + Sichtprüfung der Page | Abgedeckt |
| App-Benutzerfluss (`Tankradar.Tests.E2E`) | Keiner geplant | Nicht erforderlich — Begründung im Plan nachvollziehbar: keine UI, kein anwendungsseitiger Ablauf; die App-Testsuite kann eine Workflow-Definition nicht erreichen |

## Fehlende oder unvollständige Planbestandteile

—

## Hinweise

- **Reihenfolge Schritt 5 ↔ 6 präzisieren:** Schritt 5 setzt den Merge des Task-PRs nach `staging` voraus („nach dem Merge auf `staging`"), dieser Merge ist aber erst Aktion in Schritt 6, dessen Voraussetzungen wiederum „Schritte 4–5" nennen. Tatsächliche Ausführungsreihenfolge ist 4 → Admin-Merge nach `staging` → Dispatch-Verifikation → Promotion + Admin-Merge nach `main`. Die Beschreibungstexte lösen das eindeutig auf („Erst bei grünem Staging-Lauf wird die Promotion nach `main` angestoßen"); bei der Umsetzung darf Schritt 5 nicht wörtlich vor Schritt 6 abgearbeitet werden.
- **`docs/help/ci-cd/workflows.md` Zeile 19:** Die Bestandsaufnahme vermerkt „ggf. nachzuziehen". Da die Tabellenzeile nur Trigger (unverändert) und nicht das Job-Layout beschreibt, ist keine Änderung nötig — im Umsetzungsschritt 3 kurz gegenprüfen, damit nichts veraltet dokumentiert bleibt.
- **`build`-Job `timeout-minutes`:** Der Plan nennt das Attribut ohne Wert; bei der Umsetzung einen kleinen Wert wählen (z. B. 5–10 min), konsistent zum `deploy`-Job.
- **Polling-Schritt-Implementierung:** `gh api` paginiert standardmäßig 30 Einträge — für den Run mit einem einzigen Artifact unerheblich, kein `--paginate` nötig. `GH_TOKEN` via `${{ github.token }}` ist korrekt; `actions: read` steht bereits im Top-Level-`permissions`-Block und gilt damit für beide Jobs.
- **`pages/`-Drift-Annahme:** Die Begründung „Staging-Dispatch deployt `main`-identischen Inhalt" gilt zum Planungsstand; ändert sich `pages/` auf `main` vor der Promotion noch einmal, publiziert der Staging-Lauf den (dann älteren) `staging`-Stand — fachlich weiterhin unschädlich, da der finale `main`-Lauf ohnehin den letzten Stand bringt.
- **Kosmetik:** In den Markdown-Tabellen von `plan.md` beginnen zwei Datenzeilen mit einer leeren Zelle (`||`), was den Header-Versatz erzeugt — bei nächster Plan-Revision optional aufräumen.
