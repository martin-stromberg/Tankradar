# Plan-Gegenprüfung

## Ergebnis

**Status:** Plan vollständig

## Abgleich Akzeptanzkriterien

Akzeptanzkriterien aus `issue.md` (Zeilen 98–105) bzw. `requirement.md`:

| Akzeptanzkriterium | Umsetzung im Plan | Testnachweis im Plan | Status |
|--------------------|-------------------|----------------------|--------|
| Für jede Prüfung existiert eine separate GitHub Action | Drei eigene Workflow-Dateien `review-architecture-security.yml`, `review-nuget-packages.yml`, `review-compliance.yml` (Umsetzungsreihenfolge Schritte 1–3, Tabelle „Festlegungen je Workflow") | `python scripts/validate-workflows.py` (strukturelle Prüfung aller Workflows, Schritt 4); `workflow_dispatch`-Probe je Workflow (Schritt 7, Tests-Tabelle) | Abgedeckt |
| Ausführung zeitgesteuert über Cron-Trigger | `schedule`-Crons festgelegt: monatlich `0 6 1 * *` (1. des Monats), jährlich `0 6 1 1 *` (1. Januar), wöchentlich `0 5 * * 2` (entschieden, Offener Punkt 1); zusätzlich `workflow_dispatch` nach Konvention | Akzeptanzszenarien 1 und 3 (Cron als Auslöser); Probe auf GitHub nach Promotion (Schritt 7) | Abgedeckt |
| Erzeugte Issues verwenden aussagekräftige Titel | Titel-Schema mit Datumsteil festgelegt (Offener Punkt 2, entschieden): `Architektur- und Sicherheitsreview – <YYYY-MM>`, `NuGet-Paketprüfung – <YYYY-MM-DD>`, `Compliance- und Copyright-Review – <YYYY>` | Akzeptanzszenario 1 prüft Titel im sichtbaren Ergebnis | Abgedeckt |
| `@softwarewerkstatt` wird automatisch im Issue erwähnt | Alle drei Issue-Bodies beginnen wörtlich mit der Zeile `@softwarewerkstatt` (Abschnitt „Issue-Bodies", exakt einzubetten) | Akzeptanzszenario 1: „Body beginnt mit `@softwarewerkstatt`"; `workflow_dispatch`-Probe (Tests-Tabelle) | Abgedeckt |
| Duplikatprüfung offener Issues desselben Typs vor Erstellung | Labelbasierte Dedup: `gh issue list --state open --label <label> --json number` + `jq length`, Erzeugung nur bei 0 Treffern (Programmablauf Schritt 4, Designentscheidung) | Akzeptanzszenario 2 (Dedup-Fall, zweiter `workflow_dispatch`-Lauf); Tests-Tabelle | Abgedeckt |
| Lösung vollständig im Repository dokumentiert | `docs/help/ci-cd/workflows.md` (Überschrift „Die elf Workflows", `pages.yml`-Zeile + drei neue Zeilen, Dedup-Absatz), `README.md` (neuer Ablaufpunkt), `einrichtung.md` (Labels), `changes.log` (Schritte 5–6) | Dokumentationsumfang in Umsetzungsreihenfolge Schritt 5 explizit als ausführbarer Schritt | Abgedeckt |

## Fehlende oder unvollständige Testanforderungen

Keine.

## E2E-Abdeckung

| Benutzerfluss / Akzeptanzkriterium | Geplanter E2E-Test | Status |
|------------------------------------|--------------------|--------|
| Zeitgesteuerte Issue-Erzeugung (alle drei Prüftypen) | Keine FlaUI-/App-E2E-Tests; stattdessen `workflow_dispatch`-Probelauf auf GitHub je Workflow (Issue sichtbar im Repository, zweiter Lauf ohne Duplikat), dokumentiert in Umsetzungsreihenfolge Schritt 7 und Akzeptanzszenarien 1–3 | Nicht erforderlich mit Begründung: Die Anforderung berührt keinen Benutzerfluss der Anwendung (keine UI, kein App-Code); der „Benutzerfluss" liegt auf GitHub-Actions-Ebene und wird durch den realen Probelauf samt lokaler Workflow-Validierung (`validate-workflows.py`) nachgewiesen. Begründung im Plan-Abschnitt „E2E-Tests" nachvollziehbar vorhanden. |

## Fehlende oder unvollständige Planbestandteile

Keine.

## Hinweise

- **Vollständigkeitsmerkmale geprüft und erfüllt:** `Scope-Klärung` ist vollständig gefüllt (Verarbeitungseinheit, auslösende Ereignisse, erwartete Wirkung, Umgang mit fehlenden Objekten inkl. Schedule-Aktivierung erst auf `main`). Alle sechs Scope-Ausschlüsse tragen Begründung und Status (`bestätigt` bzw. `entfallen` nach Entscheidung zu `pages.yml`). Der Abschnitt `Akzeptanzszenarien` enthält einen vollständigen Happy Path (Szenario 1: Cron-Auslöser → fehlender Ausgangszustand → Schritte → sichtbare Wirkung → Nachweis) sowie Szenarien für fehlende Objekte (Szenario 5: Label existiert noch nicht; Szenario 2: offenes Issue vorhanden). Alle Offenen Punkte sind entschieden dokumentiert (Empfehlungen übernommen: `pages.yml` wird in die Übersicht aufgenommen → „Die elf Workflows"; preexisting `validate-workflows.py`-Fehlschlag wird nicht mitbehoben → Scope-Ausschluss 5).
- **Konsistenz geprüft:** Die wörtlichen Issue-Bodies im Plan stimmen mit den dekodierten Texten aus `issue.md` (Zeilen 29–45, 54–71, 80–96) überein; der Hinweis zur doppelten UTF-8-Kodierung (Mojibake) ist im Plan dokumentiert und die dekodierte Form explizit als verbindlich markiert. Bestandsbehauptungen verifiziert: acht Workflow-Dateien unter `.github/workflows/`, kein `gh issue`-Aufruf im Bestand, `issues: write` + `gh label create --force` in `staging-to-main-promotion.yml`/`sync-staging-with-main.yml` etabliert, Überschrift „Die sieben Workflows" in `docs/help/ci-cd/workflows.md:8`.
- **`docs/help/ci-cd/index.md`:** Im Plan nicht erwähnt; laut Bestandsaufnahme (`inventory/docs.md`) vermutlich keine Änderung nötig (reines Inhaltsverzeichnis). Bei der Umsetzung kurz gegenprüfen, ob ein Querverweis sinnvoll ist — kein Lückenbefund, da die Bestandsaufnahme keinen Änderungsbedarf festgestellt hat.
- **`test-results.md`:** Schritt 7 sieht die Dokumentation des Probelaufs in `test-results.md` vor; die Datei liegt außerhalb des Feature-Dokumentenpfads — Zielort beim Implementieren klären (z. B. Feature-Verzeichnis analog `inventory/test-results/`).
- **Risiko preexisting Rot:** Der Plan benennt korrekt, dass `validate-workflows.py` und `test_workflows_in_repository_are_clean` durch den `e2e-dumps`-Befund weiter rot bleiben und der PR-Text dies als preexisting kennzeichnen soll — bei der PR-Erstellung nicht vergessen.
