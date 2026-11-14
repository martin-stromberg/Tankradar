# Dokumentation und Konventionen

## Zu aktualisierende Dokumentationsstellen

### `docs/help/ci-cd/workflows.md`

- Zeile 8: Überschrift **„Die sieben Workflows"** — Tabelle (Zeilen 10–18) listet sieben Workflows; `pages.yml` fehlt bereits heute. Drei neue Workflows erfordern eine Anpassung der Überschrift und drei neue Tabellenzeilen.
- Spalten der Tabelle: `Datei | Anzeigename | Trigger | Aufgabe`.
- Hinweisblock (Zeilen 20–21) zum `workflow_run`-Anzeigenamen; Skripttabelle (Zeilen 33–43) nennt `scripts/validate-workflows.py`.
- Abschnitt „Runner" (Zeilen 47–51): leichtgewichtige Schritte laufen auf `ubuntu-latest` — passt zum vorgesehenen Ansatz der neuen Workflows.

### `docs/help/ci-cd/README.md`

- Ablaufliste (Zeilen 15–28): Punkt 5 „**Wöchentlich** — `Security Scan` prüft die Abhängigkeiten auch ohne Codeänderung…" — einzige Stelle, die zeitgesteuerte Läufe beschreibt; die drei neuen Rhythmen wären hier sinnvoll ergänzbar.
- Zeilen 60–62: Hinweis, dass Workflows bisher nur syntaktisch geprüft wurden.

### `docs/help/ci-cd/einrichtung.md`

- Abschnitt „## 3. Labels" (Zeilen 26–29): listet `automated-promotion` (Farbe `0E8A16`) und `automated-backmerge` (Farbe `1D76DB`) samt Hinweis, dass die Workflows sie beim ersten Lauf selbst anlegen, plus manueller `gh label create`-Befehl. Neue Review-Labels wären hier zu ergänzen.
- Zeile 21–22: Actions-Berechtigung „Read and write" + „Allow GitHub Actions to create and approve pull requests" als Einrichtungsvoraussetzung — deckt auch `issues: write` ab.

### `docs/help/ci-cd/index.md`

- Inhaltsverzeichnis der CI/CD-Doku (Zeilen 10–16); keine direkte Workflow-Aufzählung, vermutlich keine Änderung nötig.

## `changes.log`-Konvention

- Pro Eintrag: `## YYYY-MM-DD — Titel (Issue #N)`, darunter Stichpunkte mit sichtbarem Verhalten und Technik; viele Einträge enden mit „Keine Schemaänderung."
- Aktuellster Eintrag: `## 2026-11-11 — Automatische Startsuche beim Öffnen der Karte (Issue #54)`.
- CI-/Infrastruktur-Einträge existieren bereits, z. B. `## 2026-11-09 — CI: Diagnose für sporadischen Absturz 0xC0000005 im WinRT-Finalizer (Issue #22)`.

## `docs/RELEASE_NOTES.md`-Konvention

- Zweisprachig: englische Abschnitte „Important Notes Before Update" / „What's New", deutsche Pendants „Wichtige Hinweise vor dem Update" / „Neuerungen". Aktuell überall „keine Änderungen/besonderen Hinweise".

## Wörtliche Issue-Texte (Quelle: `issue.md`)

`issue.md` (Aufgabenbeschreibung, kodiert mit sichtbaren UTF-8-Umbrüchen in den gelesenen Zeilen) enthält die drei wörtlich zu übernehmenden Issue-Bodies. Jeder Body beginnt mit der Zeile `@softwarewerkstatt`:

| Prüftyp | Zeilen | Rhythmus laut Issue |
|---|---|---|
| Architektur- und Sicherheitsreview | 29–45 (9 Bulletpunkte, endet „Erstelle eine priorisierte Liste konkreter Handlungsempfehlungen.") | monatlich, am 1. Tag des Monats |
| NuGet-Paketprüfung | 54–71 (6 Bulletpunkte + Anweisung, unbedenkliche Updates direkt als PR umzusetzen) | wöchentlich |
| Compliance- und Copyright-Review | 80–96 (9 Bulletpunkte, endet „Erstelle konkrete Änderungsvorschläge…") | jährlich, am 1. Januar |

Akzeptanzkriterien (Zeilen 98–105): separate Action je Prüfung, Cron-Trigger, aussagekräftige Titel, `@softwarewerkstatt`-Erwähnung, Duplikatprüfung offener Issues desselben Typs, vollständige Dokumentation im Repository.

## Repository-Kontext

- Repository laut Anforderung: `martin-stromberg/Tankradar`, Basisbranch `staging`, Default-Branch `main` (Staging-Workflow: Feature → `staging` → `main`).
- `schedule`-Trigger wirken erst auf dem Default-Branch — die neuen Cron-Workflows werden erst nach der Promotion `staging → main` aktiv (Plattformverhalten; analoger Warnhinweis existiert bereits im Kopf von `staging-to-main-promotion.yml` für `workflow_run`).
- `README.md` (Repo-Root) verweist nicht auf einzelne Workflows (geprüft); die CI-Doku ist der dokumentarische Ort.
