# Umsetzungsplan: Deployment für GitHub Pages reparieren (Issue #105)

## Übersicht

Der Workflow „GitHub Pages" (`.github/workflows/pages.yml`) wird von einem
Ein-Job- auf ein Zwei-Job-Layout (`build` → `deploy` mit `needs`) umgebaut und
der `deploy`-Job erhält einen Polling-Schritt, der die run-scoped
Artifact-Sichtbarkeit vor dem `actions/deploy-pages`-Aufruf explizit abwartet.
Damit wird der dokumentierte intermittierende Race
(`actions/deploy-pages#451`: Metadaten-Abfrage ~0,5 s nach der
Artifact-Finalisierung findet 0 Artifacts) sowohl strukturell
(Job-Grenze als Zeitabstand) als auch deterministisch (Sichtbarkeits-Gate)
behoben. Betroffen ist ausschließlich die CI/CD-Definition; Anwendungscode und
`pages/`-Inhalt bleiben unverändert.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein Lauf des Workflows „GitHub Pages" (`.github/workflows/pages.yml`) — das Zusammenspiel von `actions/upload-pages-artifact@v5` (erzeugt Artifact `github-pages` aus `pages/`) und `actions/deploy-pages@v5` (`id: deployment`, liefert `outputs.page_url`) |
| Auslösende Ereignisse | Push auf `main` mit Änderungen an `pages/**` oder `.github/workflows/pages.yml`; manuell per `workflow_dispatch`. Die geänderte Workflow-Datei löst beim Erreichen von `main` selbst einen Verifikationslauf aus (Path-Filter). |
| Erwartete Wirkung | Workflow-Lauf endet grün: `deploy-pages` findet das Artifact `github-pages`, publiziert die Seite und setzt `steps.deployment.outputs.page_url`; die GitHub Page `https://martin-stromberg.github.io/Tankradar/` ist auf dem `main`-Stand. |
| Umgang mit fehlenden Objekten | Artifact `github-pages`: **wiederverwenden** — das vom Upload-Schritt finalisierte Artifact ist dasselbe Objekt, das `deploy-pages` per run-scoped Lookup sucht. Fehlt dem Lookup die Sichtbarkeit, wird sie per Polling abgewartet (weder neu erzeugt noch verworfen). Fehllauf `45541447133`: **verwerfen** — kein „Re-run failed jobs", weil dabei ein zweites `github-pages`-Artifact entstünde („Multiple artifacts", Upstream `#338`); Abhilfe nur über neue Runs. |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Keine inhaltliche/gestalterische Änderung an `pages/` | Anforderung begrenzt den Eingriff auf die Bereitstellung | bestätigt |
| 2 | Keine Änderung am Branching-/Promotionsprozess (`verify-pr-source.yml`, `staging-to-main-promotion.yml`, Required Checks) | Admin-Merges sind vom Auftraggeber vorgegebene operative Vorgehensweise, kein Feature-Eingriff | bestätigt |
| 3 | Kein Umbau der Pages-Infrastruktur; Source bleibt „GitHub Actions" | `gh api` zeigt `build_type: workflow`; Einrichtung korrekt und frühere Läufe erfolgreich | bestätigt |
| 4 | Kein nachträgliches Re-Deploy verpasster Zwischenstände; kein Re-Run des Fehllaufs `45541447133` | Ziel ist allein der aktuelle `main`-Stand; ein Job-Re-Run würde ein Duplikat-Artifact erzeugen | bestätigt |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Job-Struktur `pages.yml` | Zwei Jobs: `build` (Checkout, Setup Pages, Upload) und `deploy` (`needs: build`, Sichtbarkeits-Polling, `deploy-pages`) | Vom Melder des Upstream-Issues `actions/deploy-pages#451` als wirksam bestätigter Fix („Moving the upload into an earlier job fixed it for us"); zugleich das kanonische Layout aller Pages-Starter-Workflows außer `static` (der Single-Job-Starter ist der Auslöser des Race). Die Job-Grenze fügt einen strukturellen Sekundenabstand zwischen Finalisierung und Lookup ein. |
| Artifact-Sichtbarkeit | Polling-Schritt im `deploy`-Job unmittelbar vor `deploy-pages` | Deterministisches Gate auf exakt der Voraussetzung, die `deploy-pages` intern prüft (run-scoped `listArtifacts` über `GET /repos/{owner}/{repo}/actions/runs/{run_id}/artifacts`, gleiche Permission `actions: read`, gleiches Token). Macht den Fix verifizierbar: Der Schritt protokolliert die Sichtbarkeit explizit, statt auf ein strukturelles Zufalls-Timing zu vertrauen; bei anhaltender Unsichtbarkeit scheitert er mit klarer Meldung statt des kryptischen „Found 0 artifact(s)". |
| Alternative „Step-Retry" (`sleep` + zweiter `deploy-pages` nach `outcome == 'failure'`) | Verworfen | Behandelt den Fehler erst nach seinem Eintritt; erfordert `continue-on-error` am Erstschritt (jeder betroffene Lauf trüge eine fehlgeschlagene Schritt-Annotation) und eine Ausdrucks-Koaleszenz der Environment-URL über zwei Schritt-Ids. Prävention per Polling ist einfacher und hinterlässt keine roten Spuren in grünen Läufen. |
| Alternative „festes `sleep`" zwischen Upload und Deploy | Verworfen | Blindes Warten ohne Obergrenze für die Propagationsdauer; der beobachtete Race lag bei <0,5 s, ein fixes Sleep würde jeden Lauf verlangsamen und trotzdem nichts garantieren. Der Polling-Schritt wartet nur so lange wie nötig und ist selbstverifizierend. |
| Versionspin `deploy-pages` | Nicht möglich — verworfen | `v5` und `v5.0.1` zeigen auf denselben SHA `368f8252…` (der im Fehllauf geladene); es existiert kein neueres Release. Die upstreamen Fix-PRs `#455` (Retry im Lookup) und `#399` (`artifact_id`-Input) sind unmerged. |
| `actions/configure-pages` | `v6` beibehalten | Kein belegter Kausalzusammenhang zum Lookup-Fehler (Release-Notes: nur Runtime-/Dependency-Upgrades); `configure-pages` hat keinen Bezug zur Artifact-Erstellung. Ein Rollback auf `v5` wäre unbegründetes Churn — Dependabot (Gruppe `actions` inkl. `major`, `target-branch: staging`) würde v6 erneut vorschlagen. |
| Platzierung `configure-pages` | Im `build`-Job | Kanonisch (alle mehrstufigen Pages-Starter: `configure-pages` läuft im Build-Job); `deploy-pages` benötigt seine Outputs nicht. |
| `permissions` | Top-Level-Block unverändert beibehalten (`contents: read`, `pages: write`, `id-token: write`, `actions: read`) | Gilt damit für beide Jobs; der Upload lief unter exakt diesen Berechtigungen bereits erfolgreich — eine Aufsplittung pro Job wäre nicht verlangte Veränderung ohne Nutzen. `actions: read` deckt zudem die Polling-Abfrage ab. |
|| Verifikation vor `main`-Promotion | `workflow_dispatch` des Workflows auf `staging` nach dem Merge dorthin (`gh workflow run … --ref staging`) | Task-Branch-Dispatch scheidet aus — die Environment-Policy erlaubt nur `main`/`staging`. Die ausgeführte Workflow-Version folgt dem Dispatch-Ref; der publizierte `pages/`-Inhalt ist identisch mit `main` (die Änderung berührt nur die Workflow-Datei), das verfrühte Deployment ist daher fachlich unschädlich und sogar erwünscht (Ziel: aktueller Stand online). |

## Programmabläufe

### Workflow-Lauf „GitHub Pages" (neues Zwei-Job-Layout)

1. Auslöser: Push auf `main` (Path-Filter `pages/**`, `.github/workflows/pages.yml`) oder `workflow_dispatch`. Die `concurrency`-Gruppe `github-pages` (`cancel-in-progress: false`) serialisiert Läufe unverändert.
2. Job `build` startet: `actions/checkout@v7` → `actions/configure-pages@v6` → `actions/upload-pages-artifact@v5` mit `with: path: pages` finalisiert das Artifact `github-pages` (impliziter Standardname der Action).
3. Job `deploy` (`needs: build`) wird eingeplant — die Runner-Vermittlung erzeugt den strukturellen Zeitabstand zwischen Finalisierung und Lookup.
4. Schritt „Warten auf Artifact-Sichtbarkeit" (`run:`): Bash-Schleife fragt per `gh api` (env `GH_TOKEN: ${{ github.token }}`) den Endpoint `repos/$GITHUB_REPOSITORY/actions/runs/$GITHUB_RUN_ID/artifacts` ab und filtert auf Name `github-pages` und `expired == false`.
   - Zustände: *nicht sichtbar* → Intervall-Pause und erneute Abfrage; *sichtbar* → weiter bei zwei aufeinanderfolgenden positiven Abfragen (Absicherung gegen Lese-Replikat-Flackern); *Zeitbudget erschöpft* → Schrittfehler mit klarer Meldung (Artifact-ID/Stand der letzten Abfrage im Log).
   - Wirksamer Wiederaufnahmeweg bei erwartbarer Unterbrechung: Es gibt keine — der Schritt ist ein reiner Lese-Gate; scheitert er, bleibt der Lauf rot und die Diagnose ist eindeutig (kein halbdeployter Zustand, `deploy-pages` lief noch nicht).
5. `actions/deploy-pages@v5` (`id: deployment`) führt den run-scoped Lookup durch, findet das Artifact und publiziert; `outputs.page_url` füllt die Environment-URL `github-pages` (Job-Attribut unverändert).
6. Sichtbare Wirkung: grüner Workflow-Lauf; Environment `github-pages` zeigt die Deployment-URL; die Page ist aktuell.

Beteiligte Artefakte: `.github/workflows/pages.yml`, `actions/checkout@v7`, `actions/configure-pages@v6`, `actions/upload-pages-artifact@v5`, `actions/deploy-pages@v5`, `gh api` (auf ubuntu-Runnern vorinstalliert), `scripts/validate-workflows.py` (`check_workflow` validiert die `needs`-Referenz).

### Ausroll- und Verifikationsablauf

1. `pages.yml` auf dem Task-Branch umbauen (Zwei-Job-Layout + Polling-Schritt + Kopf-Kommentar zur Begründung des Splits, Verweis auf Upstream `#451`).
2. Lokale Strukturvalidierung: `python scripts/validate-workflows.py`, `python scripts/test_validate_workflows.py`, `npm test`.
3. Dokumentation: kurzer Hinweis in `pages/README.md` (Zwei-Job-Aufteilung und Grund), `changes.log`-Eintrag.
4. Commit auf dem Task-Branch mit Closing-Keyword `Closes #105` in der Message; Push; PR gegen `staging`.
5. Verifikation vor der `main`-Promotion: `gh workflow run` („GitHub Pages") mit `--ref staging` nach dem Merge auf `staging` (Task-Branch-Dispatch unmöglich — Environment-Policy erlaubt nur `main`/`staging`) → grüner Lauf beweist den Fix e2e, bevor `main` erreicht wird.
6. Admin-Merge des PRs nach `staging` (Benutzerkontextwechsel auf den Eigentümer-Account `martin-stromberg`, Keyring).
7. `staging-to-main-promotion.yml` legt den Draft-PR `staging` → `main` an; Admin-Merge nach `main`.
8. Der Push auf `main` löst `pages.yml` unmittelbar aus (die Workflow-Datei steht selbst im Path-Filter): grüner Lauf = Abnahmenachweis; anschließend Sichtprüfung von `https://martin-stromberg.github.io/Tankradar/`.
9. Explizit ausgeschlossen: „Re-run failed jobs" / `gh run rerun` auf Fehllauf `45541447133` (würde ein zweites `github-pages`-Artifact erzeugen und mit „Multiple artifacts" scheitern — Upstream `#338`).

Beteiligte Artefakte: `.github/workflows/pages.yml`, `.github/workflows/verify-pr-source.yml`, `.github/workflows/staging-to-main-promotion.yml` (beide unverändert, nur Ablaufkontext), `scripts/validate-workflows.py`, `pages/README.md`, `changes.log`, `gh`-CLI (Dispatch/PR/Admin-Merge, Account `martin-stromberg` per Keyring).

## Neue Klassen

Keine — der Eingriff betrifft ausschließlich eine Workflow-Definition, keinen Anwendungscode.

## Änderungen an bestehenden Klassen

### `.github/workflows/pages.yml` (Workflow-Definition)

- **Kopf-Kommentar:** Um einen Hinweis ergänzt, dass Upload und Deploy bewusst in getrennten Jobs laufen (Race `actions/deploy-pages#451`) — Entscheidungsbegründung am Ort der Wirkung, wie im Repo üblich.
- **Unverändert:** `name`, Trigger (`push` mit Path-Filter, `workflow_dispatch`), `permissions`, `concurrency`.
- **Job `deploy` → ersetzt durch zwei Jobs:**
  - `build`: `runs-on: ubuntu-latest`, `timeout-minutes`; Schritte `Checkout` (`actions/checkout@v7`), `Setup Pages` (`actions/configure-pages@v6`), `Upload Pages artifact` (`actions/upload-pages-artifact@v5`, `with: path: pages`).
  - `deploy`: `needs: build`, `runs-on: ubuntu-latest`, `timeout-minutes: 10`, `environment` unverändert (`name: github-pages`, `url: ${{ steps.deployment.outputs.page_url }}`); Schritte: neuer `run:`-Polling-Schritt „Warten auf Artifact-Sichtbarkeit" (env `GH_TOKEN: ${{ github.token }}`), danach `Deploy to GitHub Pages` (`id: deployment`, `actions/deploy-pages@v5`).

### `pages/README.md` (Dokumentation)

- Kurzer Hinweisabsatz: Das Deployment läuft seit Issue #105 in zwei Jobs (`build` → `deploy`), weil `deploy-pages` im selben Job das soeben hochgeladene Artifact intermittierend nicht sieht (Upstream `actions/deploy-pages#451`); der `deploy`-Job wartet die Artifact-Sichtbarkeit aktiv ab.

### `changes.log` (Changelog)

- Neuer Eintrag nach Konvention: `## <Datum> — GitHub-Pages-Deployment: Zwei-Job-Layout gegen intermittierenden Artifact-Lookup-Fehler (Issue #105)` mit Stichpunkten zu Job-Aufteilung, Polling-Gate und den bewusst unveränderten Teilen (`configure-pages@v6`, `deploy-pages@v5` = neuestes Release).

## Datenbankmigrationen

Keine.

## Validierungsregeln

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| `deploy.needs` | Muss auf den existierenden Job `build` verweisen | `check_workflow` (`scripts/validate-workflows.py`) meldet „needs verweist auf unbekannten Job" |
| Polling-Abfrage | Filtert exakt auf Artifact-Name `github-pages` und `expired == false` | Bei Namens-/Filterfehler läuft das Zeitbudget leer → Schrittfehlschlag statt stillem Durchlass |
| Durchlasskriterium Polling | Zwei aufeinanderfolgende positive Abfragen desselben run-scoped Endpoints | Ein einzelnes positives Leseresultat eines hinterherhinkenden Replikats gilt nicht als Sichtbarkeit |
| Zeitbudget Polling | Deutlich unter `deploy.timeout-minutes` (10 min) — vorgesehen ~2 min (24 Versuche à 5 s) | Bei Überschreitung: `exit 1` mit klarer Meldung; der Job bleibt rot, ohne dass `deploy-pages` gestartet wurde |
| YAML-Struktur | Gültiges YAML; Trigger-`on`-Block, `permissions`, `concurrency` unverändert | `check_workflow` meldet ungültiges Objekt bzw. fehlende Trigger |
| `.ipa`-Policy | Unverändert — `actions/upload-pages-artifact` fällt nicht unter die `actions/upload-artifact`-Prüfung von `check_key_policy`; `path: pages` bleibt | Kein neuer Befund in `validate-workflows.py` |

## Konfigurationsänderungen

Keine anwendungs- oder benutzerseitige Konfiguration. Die Härtungsparameter liegen als Konstanten in `pages.yml`:

| Eintrag | Typ | Standardwert | Zweck |
|---------|-----|--------------|-------|
| Poll-Intervall | Konstante im `run:`-Schritt | 5 s | Abstand zwischen den Sichtbarkeits-Abfragen |
| Poll-Max-Versuche | Konstante im `run:`-Schritt | 24 (≈2 min Budget) | Obergrenze, danach klarer Schrittfehlschlag |
| `deploy.timeout-minutes` | Job-Attribut | 10 (bestehend) | Deckt Polling + Deployment ab |

## Seiteneffekte und Risiken

- **`scripts/validate-workflows.py` / `test_validate_workflows.py`:** `check_workflow` validiert die neue `needs: build`-Referenz und die Job-Struktur automatisch mit; `test_workflows_in_repository_are_clean` ist der strukturelle Regressionstest. Beide müssen ohne neue Befunde grün bleiben.
- **Laufzeit:** Die Job-Trennung fügt jedem Lauf eine zweite Runner-Vermittlung hinzu (typischerweise einige Sekunden) — fachlich unerheblich, da der Workflow nur bei `pages/`-Änderungen auf `main` läuft.
- **Environment-Beschränkung:** Ist die Environment `github-pages` in den Repo-Settings auf `main` als Deployment-Branch beschränkt, schlägt die Pre-Merge-Dispatch-Verifikation am Environment-Gate fehl — kein Fehler der Änderung; Verifikation erfolgt dann erst nach dem Merge (der automatische Push-Lauf).
- **Restrisiko Replikat-Konsistenz:** Theoretisch könnte der run-scoped Lookup zwischen Polling und `deploy-pages` flackern; zwei aufeinanderfolgende positive Abfragen plus die Job-Grenz-Verzögerung machen das praktisch ausgeschlossen. Bliebe der Fehler trotzdem, wäre ein Upstream-Fix (`#455`) die einzige weitere Ebene — nicht Teil dieser Anforderung.
- **Alter Fehllauf `45541447133`:** Ein Re-Run würde wegen Duplikat-Artifact scheitern — im Plan und im Hinweistext des Fehlschlags explizit nicht vorgesehen; Abhilfe nur über neue Runs.
- **`DEP0040 punycode`-Warnung:** Bekanntes, kosmetisches Upstream-Problem (`#413`/`#433`); bleibt unbehandelt und taugt nicht als Erfolgs-/Fehlschlagskriterium.

## Umsetzungsreihenfolge

1. **`pages.yml` umbauen (Zwei-Job-Layout + Polling-Schritt + Kopf-Kommentar)**
   - Voraussetzungen: Keine — einzelne Datei, alle verwendeten Actions/Versionen und Permissions existieren bereits.
   - Beschreibung: Job `deploy` in `build` und `deploy` aufteilen wie unter „Programmabläufe" beschrieben; `needs: build`; `environment` am `deploy`-Job belassen; Polling-Schritt mit `GH_TOKEN`, Intervall 5 s, 24 Versuchen, Zwei-Positiv-Kriterium und klarer Fehlermeldung; Kommentar mit Verweis auf `actions/deploy-pages#451` ergänzen.

2. **Lokale Strukturvalidierung ausführen**
   - Voraussetzungen: Schritt 1; Python 3 + PyYAML und Node vorhanden (laut Bestandsaufnahme eingerichtet).
   - Beschreibung: `python scripts/validate-workflows.py` (Exit 0 erwartet, `needs`-Auflösung inklusive), `python scripts/test_validate_workflows.py` (14/14 grün), `npm test` (32/32 grün, unverändert).

3. **Dokumentation nachziehen**
   - Voraussetzungen: Schritt 1.
   - Beschreibung: Hinweisabsatz in `pages/README.md`; `changes.log`-Eintrag nach Konvention.

4. **Committen, pushen, PR gegen `staging`**
   - Voraussetzungen: Schritte 1–3 grün.
   - Beschreibung: Commit-Message nach Conventional-Commits-Konvention mit Closing-Keyword `Closes #105` (Issue schließt erst beim Erreichen von `main` — gewünscht, erst dann ist die Page tatsächlich repariert); Push des Task-Branches; PR gegen `staging` erstellen.

5. **Pre-Merge-Verifikation per `workflow_dispatch` — entfällt auf dem Task-Branch**
   - Voraussetzungen: Schritt 4 (Task-Branch gepusht). Die Environment `github-pages` ist per Branch-Policy auf `main` und `staging` beschränkt (geklärt, siehe „Offene Punkte").
   - Beschreibung: Dispatch auf dem Task-Branch ist nicht möglich (Environment-Gate). Stattdessen: nach dem Merge auf `staging` den Workflow per `gh workflow run … --ref staging` auslösen und auf Grün prüfen (`gh run watch`/`gh pr checks`-Polling; nicht blind warten — bei `fail` sofort Diagnose). Erwartung: `build` und `deploy` grün, Polling-Schritt protokolliert Sichtbarkeit, Page wird mit dem `main`-identischen `pages/`-Stand publiziert. Erst bei grünem Staging-Lauf wird die Promotion nach `main` angestoßen.

6. **Admin-Merge nach `staging`, Promotion, Admin-Merge nach `main`**
   - Voraussetzungen: Schritte 4–5; PR-Checks auf `staging` grün; Benutzerkontextwechsel auf den Eigentümer-Account (vom Auftraggeber vorgegebene operative Vorgehensweise).
   - Beschreibung: Admin-Merge des Task-PRs; `staging-to-main-promotion.yml` erzeugt den Draft-PR `staging` → `main`; Admin-Merge des Promotion-PRs. Kein „Re-run" des alten Fehllaufs `45541447133`.

7. **Abnahme über den automatischen `main`-Lauf**
   - Voraussetzungen: Schritt 6.
   - Beschreibung: Der Push auf `main` löst `pages.yml` selbst aus (Path-Filter enthält die Workflow-Datei): Lauf grün, beide Jobs erfolgreich, Environment `github-pages` zeigt `page_url`; Sichtprüfung der Page. Ergebnis in `test-results.md` dokumentieren.

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| — | — | Keine neuen Testmethoden: Die Änderung liegt in einer Workflow-Definition; das Laufzeitverhalten des Artifact-Lookups ist ausschließlich auf GitHub-Runnern reproduzierbar und nicht lokal testbar. Die strukturelle Regression ist bereits durch `test_workflows_in_repository_are_clean` (`scripts/test_validate_workflows.py`, wendet `check_workflow` auf alle Workflow-Dateien an) abgedeckt — dieser Test schlägt fehl, wenn die neue Job-Struktur oder die `needs`-Referenz invalide ist. |

### Betroffene bestehende Tests

Keine — `test_workflows_in_repository_are_clean` prüft den geänderten Workflow implizit weiter; keine Signatur-, Verhaltens- oder Datenstrukturänderung in getestetem Code.

### E2E-Tests (primärer Funktionsnachweis)

Die Anforderung berührt **keinen Benutzerfluss**: Es gibt keine UI, keinen anwendungsseitigen Ablauf und kein über die App erreichbares Feature — der Gegenstand ist ausschließlich eine CI/CD-Definition. E2E-Tests im Sinne der App-Testsuite können den Workflow nicht erreichen. Der primäre Funktionsnachweis ist deshalb der **reale Workflow-Lauf auf GitHub-Infrastruktur** (einzige Umgebung, in der der Race auftritt):

| Priorität | Szenario | Nachweisort | Abgedecktes Akzeptanzkriterium | Warum realer Lauf nötig ist |
|-----------|----------|-------------|-------------------------------|------------------------------|
|| Pflicht | `workflow_dispatch` auf `staging` nach dem Merge dorthin (Task-Branch-Dispatch unmöglich — Environment-Policy nur `main`/`staging`): `build` und `deploy` grün, Polling protokolliert Sichtbarkeit, Page publiziert | Actions-Tab / `gh run view`, dokumentiert in `test-results.md` | Fix funktioniert e2e vor der `main`-Promotion | Der Race existiert nur zwischen `upload-pages-artifact` und dem GitHub-Artifact-Backend — lokal nicht reproduzierbar |
| Pflicht | Automatischer Push-Lauf auf `main` nach der Promotion: Workflow grün, Page auf `main`-Stand | Actions-Tab + Sichtprüfung `https://martin-stromberg.github.io/Tankradar/` | End-to-End-Abnahme der Anforderung | Einzig mögliche Verifikation des tatsächlichen Deployments |

Bestehende E2E-Tests (App-Testsuite `Tankradar.Tests.E2E`) sind nicht betroffen: „Keine."

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
|| 1 | Happy Path: Verifikation vor `main`-Promotion | Manueller `workflow_dispatch` auf `staging` nach dem Merge dorthin | Artifact `github-pages` existiert noch nicht im Run; neuer Workflow-Aufbau auf `staging` | Dispatch → Job `build` (Checkout, Setup, Upload finalisiert Artifact) → Job `deploy` startet nach `needs` → Polling findet Artifact (i. d. R. beim ersten Versuch, ggf. nach Wartezyklen) → `deploy-pages` publiziert | Lauf grün; Environment zeigt `page_url`; Page zeigt `main`-identischen Inhalt | `gh run view` / Actions-Tab; dokumentiert in `test-results.md` |
| 2 | Abnahme auf `main` | Merge des Promotion-PRs (Push auf `main`) | `pages.yml` geändert → Path-Filter trifft; kein laufender Pages-Lauf (Concurrency-Gruppe frei) | Push löst Workflow automatisch aus → Ablauf wie Szenario 1 | Lauf grün; `https://martin-stromberg.github.io/Tankradar/` auf aktuellem `main`-Stand; Issue #105 schließt über `Closes #105` beim Erreichen von `main` | Actions-Tab + Sichtprüfung der Page |
| 3 | Wartefall: Artifact noch nicht sichtbar | Wie Szenario 1/2 | Job `deploy` startet, bevor das run-scoped `listArtifacts` das Artifact sieht (der dokumentierte Race-Zustand) | Polling-Abfrage negativ → Intervall 5 s → erneute Abfrage → positiv (zweimal folgend) → `deploy-pages` | Lauf grün; Polling-Log zeigt die Wartezyklen statt des früheren „Found 0 artifact(s)"-Fehlers | Job-Log des Laufes |
| 4 | Fehlerfall: Artifact bleibt unsichtbar (z. B. API-Störung) | Wie Szenario 1/2 | Artifact-Service gestört; Lookup findet dauerhaft nichts | Polling erschöpft Zeitbudget (~2 min) → Schrittfehlschlag mit klarer Meldung → `deploy-pages` läuft gar nicht erst | Lauf rot, aber mit eindeutiger Diagnose-Meldung; kein halbdeployter Zustand; Wiederaufnahme nur über einen **neuen** Run (Push/Dispatch) — nie über „Re-run failed jobs" (Duplikat-Artifact) | Job-Log; Verhalten aus Workflow-Definition ersichtlich |
| 5 | Verbotener Weg: Re-Run des alten Fehllaufs | „Re-run failed jobs" auf `45541447133` | Run enthält bereits Artifact `14989192493` | Erneuter Upload-Schritt erzeugt zweites `github-pages`-Artifact → `deploy-pages` meldet „Multiple artifacts" (Upstream `#338`) | Roter Lauf — bewusst aus dem Plan ausgeschlossen | Dokumentiert; kein Test |

## Offene Punkte

Keine — der einzige bei der Planung offene Punkt wurde geklärt (2026-11-25, per `gh api repos/martin-stromberg/Tankradar/environments/github-pages/deployment-branch-policies`): Die Deployment-Branch-Policy der Environment `github-pages` erlaubt **ausschließlich `main` und `staging`**. Ein `workflow_dispatch` auf dem Task-Branch würde am Environment-Gate scheitern. Konsequenz: Die Pre-Merge-Dispatch-Verifikation auf dem Task-Branch entfällt; stattdessen erfolgt die e2e-Verifikation **nach dem Merge auf `staging` per Dispatch mit `--ref staging`** (erlaubte Policy, `pages/`-Inhalt ist `main`-identisch → Deployment fachlich unschädlich) und die Endabnahme über den automatischen Push-Lauf auf `main`.
