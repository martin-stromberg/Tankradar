# Umsetzungsplan: Automatisierung wiederkehrender Architektur-, Sicherheits- und Compliance-Reviews (Issue #58)

## Übersicht

Drei neue, eigenständige GitHub-Actions-Workflows unter `.github/workflows/` erzeugen zeitgesteuert (Cron `schedule` plus manuell per `workflow_dispatch`) jeweils ein GitHub-Issue mit wörtlich vorgegebenem deutschem Prüfauftrag, der mit der Zeile `@softwarewerkstatt` beginnt. Vor jeder Erzeugung prüft der Workflow labelbasiert, ob bereits ein offenes Issue desselben Prüftyps existiert, und überspringt die Erstellung dann. Betroffen sind ausschließlich CI-/Repository-Dateien: drei neue Workflow-Dateien sowie die CI-Dokumentation (`docs/help/ci-cd/`) und `changes.log`. Es werden keine Anwendungsklassen angelegt oder geändert.

## Scope-Klärung

| Feld | Festlegung |
|------|------------|
| Verarbeitungseinheit | Ein „Review-Zyklus" je Prüftyp; drei disjunkte Prüftypen mit eigenem Rhythmus und eigenem Issue-Text: (1) Architektur- und Sicherheitsreview monatlich am 1. Tag, (2) NuGet-Paketprüfung wöchentlich, (3) Compliance- und Copyright-Review jährlich am 1. Januar. |
| Auslösende Ereignisse | `schedule`-Trigger (Cron) pro Workflow plus `workflow_dispatch` (Konvention aus `security-scan.yml`). |
| Erwartete Wirkung | Neues offenes Issue in `martin-stromberg/Tankradar` mit aussagekräftigem Titel, wörtlichem Body ab `@softwarewerkstatt` und einem prüftypspezifischen Label; die Erwähnung stößt die nachgelagerte Bearbeitung an. |
| Umgang mit fehlenden Objekten | **Offenes Issue desselben Prüftyps:** wiederverwenden — kein neues erzeugen, Lauf endet erfolgreich ohne Aktion (Dedup über `gh issue list --state open --label <label>`). **Prüftyp-Label:** idempotent erzeugen (`gh label create ... --force`), Muster aus `staging-to-main-promotion.yml`. **Checkout/Arbeitsverzeichnis:** nicht erforderlich — die Issue-Texte liegen inline im Workflow. **Schedule-Aktivierung:** Cron wirkt erst, sobald die Datei auf `main` liegt (Plattformverhalten; bis dahin nur Datei-Bestand). |

### Scope-Ausschlüsse

| # | Einschränkung / Ausschluss | Begründung | Status |
|---|----------------------------|------------|--------|
| 1 | Die Workflows führen keine Reviews, Analysen oder Paket-Updates durch; sie erzeugen ausschließlich Issues. | Anforderung: die eigentliche Prüfung (inkl. PR für unbedenkliche NuGet-Updates) obliegt `@softwarewerkstatt`. | bestätigt (2026-11-14, automatisierte Bearbeitung: Empfehlung des Plans übernommen) |
| 2 | Kein automatisches Schließen, Eskalieren oder Nachfassen bei unbearbeiteten Issues; bei offenem Vorgänger-Issue wird schlicht kein neues erzeugt. | Anforderung (Duplikatvermeidung ist die einzige Steuerung). | bestätigt (2026-11-14, automatisierte Bearbeitung: Empfehlung des Plans übernommen) |
| 3 | Keine Änderung am wöchentlichen `security-scan.yml`. | Reine Schwachstellensuche, kein Issue-Mechanismus; Anforderung schließt Änderung aus. | bestätigt (2026-11-14, automatisierte Bearbeitung: Empfehlung des Plans übernommen) |
| 4 | Kein konfigurierbarer Intervall-Mechanismus; die drei Rhythmen sind fest in Cron-Ausdrücken verdrahtet. | Anforderung; Wartung durch Editieren der Workflow-Dateien. | bestätigt (2026-11-14, automatisierte Bearbeitung: Empfehlung des Plans übernommen) |
| 5 | Der preexisting Fehlschlag von `scripts/validate-workflows.py` (Upload-Pfad `e2e-dumps/` fehlt in `SAFE_UPLOAD_DIRS`, `scripts/validate-workflows.py:41`; Befunde in `pr-staging-ci.yml` und `staging-ci.yml`) wird **nicht** behoben. | Eigenständiger, bereits dokumentierter Defekt aus Issue #22 — nicht Teil dieser Anforderung. Die neuen Workflows werden so geplant, dass sie selbst keine Validierungsbefunde erzeugen; der Gesamtlauf bleibt bis zur separaten Behebung rot. | bestätigt (2026-11-14, automatisierte Bearbeitung: Empfehlung des Plans übernommen) |
| 6 | ~~`pages.yml` bleibt in der Workflow-Übersichtstabelle unberücksichtigt (preexisting Dokumentationslücke).~~ | Entfallen — Offener Punkt 5 wurde mit „ja" entschieden: `pages.yml` wird in die Übersicht aufgenommen (Überschrift „Die elf Workflows"). | entfallen (2026-11-14) |

## Designentscheidungen

| Komponente / Bereich | Gewählter Ansatz | Begründung |
|----------------------|-----------------|------------|
| Dedup-Mechanismus | Labelbasiert: `gh issue list --state open --label <prüftyp-label> --json number` gefolgt von `jq length` — Erzeugung nur bei `0` | Entspricht der Empfehlung der Anforderung und der etablierten Konvention (`automated-promotion`, `automated-backmerge` per `gh pr list ... | jq length`); robust gegen Titeländerungen. Bekannte Einschränkung: manuell angelegte, ungelabelte Issues desselben Typs werden nicht erkannt — derzeit existieren keine solchen Issues; alle automatisch erzeugten tragen das Label. |
| Ablage der Issue-Texte | Inline im Workflow als Bash-Heredoc in `--body "$(cat <<'EOF' ... EOF)"` | Kein Checkout nötig, Text steht 1:1 prüfbar in der Workflow-Datei, keine zusätzlichen Dateien; `validate-workflows.py` prüft nur `node scripts/`-, `./scripts/`-Aufrufe und lokale Actions — Heredocs sind unproblematisch. |
| Berechtigungen / Checkout | Kein `actions/checkout`; `permissions:` nur `issues: write` | Der Workflow braucht keinen Arbeitsbaum. `gh label create`, `gh issue list`, `gh issue create` sind durch `issues: write` abgedeckt (Nachweis: `staging-to-main-promotion.yml` legt Labels mit genau dieser Permission an). `GH_TOKEN: ${{ github.token }}` je Schritt (Konvention aus `release.yml`). |
| Laufzeitumgebung | `ubuntu-latest`, Bash-Default-Shell, `timeout-minutes: 5`, `concurrency`-Gruppe je Workflow mit `cancel-in-progress: false` | Leichtgewichtige Schritte laufen laut `docs/help/ci-cd/workflows.md` („Runner") auf `ubuntu-latest`; `concurrency`-Muster aus `staging-to-main-promotion.yml` verhindert Doppelläufe bei zeitnahem `schedule` + `workflow_dispatch`. |
| Kodierung der Issue-Texte | Bodies werden als korrektes UTF-8-Deutsch eingebettet | `issue.md` Zeilen ~14–105 sind doppelt UTF-8-kodiert (Mojibake wie `EinfÃ¼hrung`, `Ã„nderungen`); die wörtlich gemeinten Texte sind die dekodierte Form (`Einführung`, `Änderungen`). Die im Plan unten festgelegten Bodies sind bereits dekodiert und beim Implementieren exakt zu übernehmen. |
| Cron-Zeitpunkte | Monatlich `0 6 1 * *` (1. des Monats, 06:00 UTC); jährlich `0 6 1 1 *` (1. Januar, 06:00 UTC); wöchentlich siehe Offener Punkt 1 | Vom Issue vorgegebene Kalendertermine; Uhrzeit 06:00 UTC versetzt zum `security-scan.yml` (Mo 04:00 UTC). |

## Programmabläufe

Alle drei Workflows folgen demselben Ablauf (Transaction Script auf CI-Ebene); sie unterscheiden sich nur in Dateiname, Anzeigename, Cron, Label, Issue-Titel und Issue-Body.

### Ablauf „Review-Issue erzeugen" (identisch in allen drei Workflows)

1. Trigger feuert: `schedule` (Cron) oder `workflow_dispatch`.
2. Job auf `ubuntu-latest` startet ohne Checkout.
3. Schritt „Label sicherstellen": `gh label create <label> --color <farbe> --description "<beschreibung>" --force` mit `GH_TOKEN: ${{ github.token }}` — idempotent, legt das Label beim ersten Lauf an.
4. Schritt „Issue bei Bedarf erzeugen" mit `GH_TOKEN: ${{ github.token }}`:
   a. `existing="$(gh issue list --state open --label <label> --json number)"`
   b. `echo "$existing" | jq length` > 0 → Hinweis ins Log („Offenes Issue vorhanden, kein neues Issue erzeugt"), Schritt und Job enden erfolgreich.
   c. `jq length` == 0 → `gh issue create --title "<titel mit Datumsteil>" --label <label> --body "$(cat <<'EOF' ... <body> ... EOF)"`.
5. Das erzeugte Issue ist offen, trägt das Prüftyp-Label und beginnt im Body mit der Zeile `@softwarewerkstatt`.

Beteiligte Komponenten: GitHub Actions Runner (`ubuntu-latest`), `gh` CLI (vorinstalliert), `jq` (vorinstalliert), `GITHUB_TOKEN`.

### Festlegungen je Workflow

| Datei | `name:` (Anzeigename) | Cron | Label (Farbe) | Issue-Titel (Muster) |
|---|---|---|---|---|
| `.github/workflows/review-architecture-security.yml` | `Monthly Architecture & Security Review` | `0 6 1 * *` | `review-architecture-security` (`B60205`) | `Architektur- und Sicherheitsreview – $(date -u +%Y-%m)` |
| `.github/workflows/review-nuget-packages.yml` | `Weekly NuGet Package Review` | `0 5 * * 2` *(Vorschlag, Offener Punkt 1)* | `review-nuget-packages` (`FBCA04`) | `NuGet-Paketprüfung – $(date -u +%Y-%m-%d)` |
| `.github/workflows/review-compliance.yml` | `Yearly Compliance & Copyright Review` | `0 6 1 1 *` | `review-compliance` (`5319E7`) | `Compliance- und Copyright-Review – $(date -u +%Y)` |

Die Titel enthalten einen dynamischen Datumsteil für Eindeutigkeit und Wiedererkennbarkeit (Offener Punkt 2); die Dedup hängt ausschließlich am Label, nicht am Titel.

### Issue-Bodies (wörtlich, bereits dekodiert — exakt so einbetten)

**`review-architecture-security.yml`** (Quelle: `issue.md` Zeilen 29–45):

```
@softwarewerkstatt

Bitte führe ein Architektur- und Sicherheitsreview durch.

Prüfe insbesondere:

- Neue Sicherheitsrisiken in verwendeten Frameworks und Bibliotheken
- Bekannte Schwachstellen (CVEs) in NuGet-Abhängigkeiten
- Neue Empfehlungen für ASP.NET Core, Blazor, .NET und verwandte Technologien
- Änderungen bei Authentifizierung und Autorisierung
- Neue oder inzwischen bevorzugte Design Patterns
- Veraltete, abgekündigte oder problematische APIs
- Verbesserungsmöglichkeiten hinsichtlich Wartbarkeit, Erweiterbarkeit und Testbarkeit
- Aktuelle Empfehlungen zu Logging, Telemetrie und Monitoring
- Relevante Erkenntnisse aus der Community, Microsoft-Dokumentation und Sicherheitsmeldungen

Erstelle eine priorisierte Liste konkreter Handlungsempfehlungen.
```

**`review-nuget-packages.yml`** (Quelle: `issue.md` Zeilen 54–71):

```
@softwarewerkstatt

Bitte analysiere alle verwendeten NuGet-Pakete.

Prüfe:

- Verfügbare Updates
- Security-Updates
- Pakete mit bekannt gewordenen Schwachstellen
- Veraltete oder nicht mehr gepflegte Pakete
- Empfohlene Nachfolgepakete
- Mögliche Breaking Changes bei Updates

Erstelle konkrete Update-Empfehlungen inklusive Risikobewertung.

Setze die unbedenklichen Empfehlungen (niedriges bis mittleres Risiko)
direkt in einem Pull Request um. Risikoreiche oder aufgeschobene
Empfehlungen dokumentiere im PR-Body bzw. als eigenes Folge-Issue.
```

**`review-compliance.yml`** (Quelle: `issue.md` Zeilen 80–96):

```
@softwarewerkstatt

Bitte führe das jährliche Compliance-Review durch.

Prüfe:

- Copyright-Hinweise im Quellcode
- Copyright-Angaben auf Webseiten
- Copyright-Angaben in About-Dialogen
- Impressum
- Datenschutzerklärung
- Drittanbieter-Lizenzen
- Open-Source-Verpflichtungen
- Unternehmens- und Kontaktinformationen
- App-Store-Metadaten (falls vorhanden)

Erstelle konkrete Änderungsvorschläge und liste erkannte Risiken oder Handlungsbedarfe auf.
```

### Kopf-Kommentar der Workflow-Dateien

Wie `security-scan.yml` und `staging-to-main-promotion.yml` erhalten die neuen Dateien einen erklärenden Kopf-Kommentar: Zweck (nur Issue erzeugen, Prüfung durch `@softwarewerkstatt`), Dedup-Logik über das Label und der Hinweis, dass der `schedule`-Trigger erst wirkt, sobald die Datei auf dem Default-Branch `main` liegt (Plattformverhalten; im Staging-Workflow erst nach der Promotion `staging → main`).

## Neue Klassen

Keine Anwendungsklassen. Neu sind drei Workflow-Dateien:

| Datei | Typ | Zweck |
|-------|-----|-------|
| `.github/workflows/review-architecture-security.yml` | Workflow-Datei | Monatlicher Cron-Lauf; erzeugt bei Bedarf das Architektur-/Sicherheitsreview-Issue. |
| `.github/workflows/review-nuget-packages.yml` | Workflow-Datei | Wöchentlicher Cron-Lauf; erzeugt bei Bedarf das NuGet-Prüfungs-Issue. |
| `.github/workflows/review-compliance.yml` | Workflow-Datei | Jährlicher Cron-Lauf (1. Januar); erzeugt bei Bedarf das Compliance-Review-Issue. |

## Änderungen an bestehenden Klassen

Keine Anwendungsklassen. Änderungen an bestehenden Dateien (Dokumentation):

### `docs/help/ci-cd/workflows.md` (Dokumentation)

- Überschrift „Die sieben Workflows" → „Die elf Workflows" (elf Tabellenzeilen: sieben bisherige plus `pages.yml` plus drei neue; siehe Offener Punkt 5, entschieden mit „ja").
- Drei neue Tabellenzeilen im Schema `Datei | Anzeigename | Trigger | Aufgabe`, z. B. „`review-architecture-security.yml` | Monthly Architecture & Security Review | monatlich (1., 06:00 UTC), manuell | Erzeugt bei Bedarf ein Issue mit dem Review-Auftrag für `@softwarewerkstatt` (Label `review-architecture-security`)."
- Kurzer Absatz zum Issue-/Dedup-Muster (Label je Prüftyp, `gh issue list`/`gh issue create`, keine Review-Ausführung im Workflow).

### `docs/help/ci-cd/README.md` (Dokumentation)

- Ablaufliste: Punkt 5 bleibt (`Security Scan`); neuer Punkt 6 „Zeitgesteuert — Review-Issues" beschreibt die drei Rhythmen, die `@softwarewerkstatt`-Erwähnung und die Duplikatvermeidung.

### `docs/help/ci-cd/einrichtung.md` (Dokumentation)

- Abschnitt „3. Labels": die drei neuen Labels (`review-architecture-security` `B60205`, `review-nuget-packages` `FBCA04`, `review-compliance` `5319E7`) mit dem Hinweis ergänzen, dass die Workflows sie beim ersten Lauf selbst anlegen. Vorhandener `issues: write`-bzw. „Read and write"-Hinweis (Abschnitt 2) deckt die Berechtigung bereits ab — keine neue Einrichtungsvoraussetzung.

### `changes.log` (Changelog)

- Neuer Eintrag oben im Muster `## <Datum> — Automatische Review-Issues per Cron (Issue #58)` mit Stichpunkten zu den drei Workflows, Dedup und Dokumentation; Abschluss „Keine Schemaänderung." (Konvention). Kein `RELEASE_NOTES.md`-Eintrag: Die Änderung ist CI-Infrastruktur ohne nutzersichtbare App-Wirkung; die Datei steht konventionsgemäß weiter auf „keine Änderungen".

## Datenbankmigrationen

Keine.

## Validierungsregeln

Es gibt keine Anwendungseingaben. Die neuen Workflow-Dateien müssen die strukturellen Regeln von `scripts/validate-workflows.py` erfüllen:

| Feld / Objekt | Regel | Fehlerfall |
|---------------|-------|------------|
| Jede neue `.github/workflows/*.yml` | Gültiges YAML, nichtleerer `on`-Block (`schedule` + `workflow_dispatch`), Jobs mit `runs-on` + `steps` | `check_workflow` meldet fehlenden Trigger/Jobs |
| `run`-Schritte | Keine Aufrufe von `node scripts/...` oder `./scripts/...` auf nicht existierende Dateien; keine `uses: ./...`-Referenzen | `check_run_steps` meldet fehlende Skripte/Actions |
| Artefakte/Releases | Kein `actions/upload-artifact`, kein `gh release create/upload` in den neuen Workflows | `check_key_policy`/`check_release_commands` würden `.ipa`-Risiken bemängeln — entfällt durch Verzicht |
| `gh`-Aufrufe | `gh issue`/`gh label` sind von der Validierung nicht erfasst | — |

Nachweis: `python scripts/validate-workflows.py` darf nach der Umsetzung **keine neuen Befunde** zeigen — die zwei preexisting `e2e-dumps`-Befunde (Scope-Ausschluss 5) bleiben unverändert bestehen.

## Konfigurationsänderungen

Keine (`appsettings`, Konfigurationsklassen unberührt). Die Intervalle sind nicht konfigurierbar, sondern fest über die Cron-Ausdrücke in den Workflow-Dateien definiert (Anforderung).

## Seiteneffekte und Risiken

- **Workflow-Validierung bleibt rot (preexisting):** `validate-workflows.py` und `test_workflows_in_repository_are_clean` schlagen weiter fehl, bis der `e2e-dumps`-Befund separat behoben ist. Risiko: In PR-Checks könnte der Altfehlschlag dem neuen Feature zugerechnet werden — im PR-Text als preexisting kennzeichnen (Nachweis: `inventory/test-results/validate-workflows.log`).
- **Schedule-Aktivierung erst nach Promotion:** Die Cron-Trigger wirken erst auf `main`. Bis dahin ist der Funktionsnachweis nur strukturell möglich (`validate-workflows.py`, `actionlint` falls vorhanden); ein realer `workflow_dispatch`-Lauf ist auf GitHub erst sinnvoll auslösbar, wenn die Workflows auf dem Default-Branch liegen.
- **Plattformverhalten:** Scheduled Runs können bei hoher GitHub-Last verzögert starten und werden nach 60 Tagen Repository-Inaktivität deaktiviert (kein Umsetzungspunkt, im Kopf-Kommentar erwähnenswert).
- **`@softwarewerkstatt`-Bearbeitung:** Erfolgt das Issue-Create, während das Vorgänger-Issue zwar geschlossen, aber fachlich noch unbearbeitet ist, entsteht ein neuer Zyklus — gewünschtes Verhalten laut Anforderung (Dedup nur über offene Issues).
- **`gh`-CLI-Authentifizierung:** Die neuen Workflows nutzen `GH_TOKEN: ${{ github.token }}` wie etabliert; kein PAT, keine neuen Secrets nötig.
- **Dokumentationszählung:** Mit `pages.yml` und drei neuen Zeilen hat die Tabelle elf Einträge (Offener Punkt 5 wurde mit „ja" entschieden; Scope-Ausschluss 6 entfallen).

## Umsetzungsreihenfolge

1. **`.github/workflows/review-architecture-security.yml` anlegen**
   - Voraussetzungen: Keine (Runner bringt `gh` und `jq` mit; Muster aus `staging-to-main-promotion.yml`/`security-scan.yml` liegen vor).
   - Beschreibung: Workflow mit `schedule` `0 6 1 * *` + `workflow_dispatch`, `permissions: issues: write`, `concurrency`-Gruppe, Job auf `ubuntu-latest`: Label `review-architecture-security` idempotent anlegen, Dedup per `gh issue list --label`, Issue mit dekodiertem Body (s. o.) und Titel `Architektur- und Sicherheitsreview – <YYYY-MM>` erzeugen.

2. **`.github/workflows/review-nuget-packages.yml` anlegen**
   - Voraussetzungen: Schritt 1 als Vorlage; Zeitpunkt aus Offenem Punkt 1 (Standard: `0 5 * * 2`).
   - Beschreibung: Gleiche Struktur; Label `review-nuget-packages`, NuGet-Body, Titel `NuGet-Paketprüfung – <YYYY-MM-DD>`.

3. **`.github/workflows/review-compliance.yml` anlegen**
   - Voraussetzungen: Schritt 1 als Vorlage.
   - Beschreibung: Gleiche Struktur; Cron `0 6 1 1 *`, Label `review-compliance`, Compliance-Body, Titel `Compliance- und Copyright-Review – <YYYY>`.

4. **Lokale Workflow-Validierung ausführen**
   - Voraussetzungen: Python + PyYAML (in der Entwicklungsumgebung vorhanden); die drei Dateien aus Schritten 1–3.
   - Beschreibung: `python scripts/validate-workflows.py` — erwartet werden ausschließlich die zwei preexisting `e2e-dumps`-Befunde, keine neuen; `python scripts/test_validate_workflows.py` bleibt bei 13/14 (der eine Fehlschlag ist preexisting). YAML der neuen Dateien zusätzlich sichten (Einrückung des Heredocs, korrekte UTF-8-Umlaute).

5. **CI-Dokumentation aktualisieren**
   - Voraussetzungen: Schritte 1–3 (Dateinamen, Anzeigenamen, Labels, Crons stehen fest).
   - Beschreibung: `docs/help/ci-cd/workflows.md` (Überschrift „Die elf Workflows", `pages.yml`-Zeile plus drei neue Zeilen, Kurzabsatz zum Issue-/Dedup-Muster), `docs/help/ci-cd/README.md` (neuer Ablaufpunkt „Zeitgesteuert — Review-Issues"), `docs/help/ci-cd/einrichtung.md` (Abschnitt „3. Labels" um die drei Review-Labels ergänzen).

6. **`changes.log`-Eintrag ergänzen**
   - Voraussetzungen: Schritte 1–5.
   - Beschreibung: Neuer Eintrag `## <Datum> — <Titel> (Issue #58)` nach Konvention.

7. **Manueller Funktionsnachweis auf GitHub (nach Merge/Promotion)**
   - Voraussetzungen: Workflows liegen auf `main` (nach Promotion `staging → main`).
   - Beschreibung: Je Workflow einmal `workflow_dispatch` auslösen; prüfen, dass das Issue mit korrektem Titel, Label und `@softwarewerkstatt`-Body entsteht; erneuter Lauf erzeugt kein Duplikat. Ergebnis in `test-results.md` dokumentieren. Auf `staging` ist nur der strukturelle Nachweis (Schritt 4) möglich.

## Tests

### Neue Tests

Keine neuen Unit- oder Integrationstests — die Änderung liegt vollständig auf CI-Ebene ohne Anwendungscode. Die bestehende Repository-Prüfung `test_workflows_in_repository_are_clean` in `scripts/test_validate_workflows.py` erfasst neue Workflow-Dateien automatisch; sie ist aktuell preexisting rot (Scope-Ausschluss 5) und prüft dann auch die neuen Dateien mit.

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `python scripts/validate-workflows.py` (manueller Lauf) | — | Neue Workflows erzeugen keine zusätzlichen Befunde (nur die 2 preexisting `e2e-dumps`-Meldungen) |
| `python scripts/test_validate_workflows.py` (manueller Lauf) | — | Ergebnis unverändert 13/14 — kein neuer Fehlschlag durch die neuen Dateien |
| `workflow_dispatch`-Probe je Workflow (auf GitHub) | — | Issue wird erzeugt (erster Lauf) bzw. nicht erzeugt (Dedup, zweiter Lauf); Label existiert danach |

### Betroffene bestehende Tests

| Test / Testklasse | Grund der Anpassung |
|-------------------|---------------------|
| `ReleaseCommandTests.test_workflows_in_repository_are_clean` (`scripts/test_validate_workflows.py:80–86`) | **Keine Anpassung** — bleibt durch den preexisting `e2e-dumps`-Befund rot; die neuen Workflows tragen keine Befunde bei. Im PR ist der preexisting Status zu vermerken. |

### E2E-Tests (primärer Funktionsnachweis)

Keine E2E-Tests erforderlich: Die Anforderung berührt keinen Benutzerfluss der Anwendung — es gibt keine UI, keine App-Interaktion und keinen über die App erreichbaren Ablauf. Der „Benutzerfluss" liegt auf GitHub-Actions-Ebene; der wirksame Funktionsnachweis ist der `workflow_dispatch`-Probelauf auf GitHub (Issue erscheint sichtbar im Repository), ergänzt durch die lokale Workflow-Validierung. FlaUI-Tests können einen Workflow-Auslöser nicht abdecken.

## Akzeptanzszenarien

| # | Szenario | Auslöser | Ausgangszustand | Schritte | Sichtbare Wirkung | Nachweis |
|---|----------|----------|------------------|----------|-------------------|----------|
| 1 | Happy Path: monatliches Review-Issue | Cron `0 6 1 * *` am Monatsersten (nach Promotion auf `main`) | Kein offenes Issue mit Label `review-architecture-security`; Label existiert evtl. noch nicht | Label wird angelegt → Dedup liefert 0 Treffer → `gh issue create` | Neues offenes Issue „Architektur- und Sicherheitsreview – <YYYY-MM>", Body beginnt mit `@softwarewerkstatt`, Label gesetzt | `workflow_dispatch`-Probe auf GitHub; Issue sichtbar im Repository |
| 2 | Dedup: offenes Issue vorhanden | Cron oder `workflow_dispatch` | Offenes Issue mit dem Prüftyp-Label existiert | Label sicherstellen → Dedup liefert ≥1 → Erzeugung übersprungen | Kein neues Issue; Lauf endet grün mit Log-Hinweis | Zweiter `workflow_dispatch`-Lauf direkt nach Szenario 1 |
| 3 | Jährlicher und wöchentlicher Rhythmus | Crons `0 6 1 1 *` bzw. `0 5 * * 2` | Wie Szenario 1, je Prüftyp | Identischer Ablauf mit eigenem Label/Body/Titel | Je Prüftyp genau ein offenes Issue; die drei Typen beeinflussen sich nicht gegenseitig | `workflow_dispatch`-Proben je Workflow |
| 4 | Vor der Promotion (staging) | — | Workflow-Dateien liegen nur auf `staging`/Feature-Branch | `schedule` feuert nicht (Plattformverhalten: Trigger wirkt nur auf Default-Branch) | Keine Läufe, keine Issues — der Bestand ist inert bis `main` | `python scripts/validate-workflows.py` (keine neuen Befunde); Hinweis im Kopf-Kommentar |
| 5 | Label fehlt beim allerersten Lauf | Erster `workflow_dispatch` | Label `review-*` existiert noch nicht im Repository | `gh label create --force` legt es an → Dedup → Issue-Erzeugung | Label und Issue existieren anschließend | Issue-/Label-Sicht auf GitHub |

## Offene Punkte

Alle Punkte wurden in der automatisierten Bearbeitung entschieden — jeweils gemäß dem in der Tabelle empfohlenen Vorschlag (2026-11-14). Punkt 5: `pages.yml` wird in der Workflow-Übersicht ergänzt (Überschrift „Die elf Workflows"). Punkt 6: Der preexisting `validate-workflows.py`-Fehlschlag wird nicht mitbehoben (separater Defekt aus Issue #22).

| # | Offener Punkt | Empfohlener Vorschlag |
|---|---------------|----------------------|
| 1 | Zeitpunkt des wöchentlichen NuGet-Laufs (Wochentag/Uhrzeit nicht in der Anforderung festgelegt) | Dienstag 05:00 UTC (`0 5 * * 2`) — versetzt zum `security-scan.yml` (Montag 04:00 UTC), damit beide Wochenläufe nicht am selben Morgen konkurrieren. |
| 2 | Konkretes Titel-Schema der erzeugten Issues | Wie oben festgelegt: `Architektur- und Sicherheitsreview – <YYYY-MM>`, `NuGet-Paketprüfung – <YYYY-MM-DD>`, `Compliance- und Copyright-Review – <YYYY>` (Datumsteil via `date -u` im Schritt). Dedup bleibt labelbasiert, der Titel dient nur der Lesbarkeit. |
| 3 | Soll das Issue zusätzlich per `--assignee` an `@softwarewerkstatt` zugewiesen werden? | Nein — die Anforderung verlangt nur die Erwähnung im Body; `--assignee` kann scheitern, falls der Account nicht zuweisbar ist (z. B. App-/Bot-Account ohne Collaborator-Status). |
| 4 | Label-Namen und -Farben | `review-architecture-security` (`B60205`), `review-nuget-packages` (`FBCA04`), `review-compliance` (`5319E7`) — konsistent zum Schema der Bestandslabels (`automated-*`, Hex-Farbe, deutsche Beschreibung). |
| 5 | Soll die Dokumentationslücke `pages.yml` in `docs/help/ci-cd/workflows.md` mitgeschlossen werden? | Empfohlen: ja — eine zusätzliche Tabellenzeile für `pages.yml` (Trigger „Push → `main` (docs), manuell"; Aufgabe „GitHub-Pages-Deployment"); Überschrift dann „Die elf Workflows". Fällt der Punkt weg, bleibt es bei „Die zehn Workflows". |
| 6 | Soll der preexisting `validate-workflows.py`-Fehlschlag (`e2e-dumps` in `SAFE_UPLOAD_DIRS`) in dieser Aufgabe mitbehoben werden? | Nein — als separates Issue/Folgeaufgabe behandeln (Scope-Ausschluss 5). Minimaler Fix wäre `'e2e-dumps'` in `SAFE_UPLOAD_DIRS` aufzunehmen, ist aber nicht Teil von Issue #58. |
