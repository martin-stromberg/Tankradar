# Code-Review

## Ergebnis

**Status:** Befunde vorhanden

## Befunde

### `.github/workflows/review-architecture-security.yml`, `review-nuget-packages.yml`, `review-compliance.yml`

- **Doppelter Code** — Der komplette Job `create-review-issue` (beide Schritte: „Ensure … label exists" und „Create review issue if none is open", zusammen ~30 Zeilen Bash-Logik inklusive Dedup-Prüfung und Heredoc-Body) ist in allen drei neuen Workflow-Dateien nahezu identisch dupliziert; es unterscheiden sich nur Skalare (Label-Name, Farbe, Beschreibung, Issue-Titel/-Body, Cron). Eine Korrektur an der Dedup- oder Erzeugungslogik müsste künftig dreifach gepflegt werden, mit der Gefahr auseinanderlaufender Implementierungen (z. B. `review-architecture-security.yml:29–65` vs. `review-nuget-packages.yml:29–66` vs. `review-compliance.yml:29–65`).

  Empfehlung: Die gemeinsame Logik in eine Composite Action unter `.github/actions/create-review-issue/` auslagern (Konvention liegt mit `.github/actions/security-scan`, `build-and-package`, `package-ios` bereits vor) — Inputs: `label`, `color`, `description`, `title`, `body`. Die drei `review-*.yml` behalten dann jeweils nur Trigger (`schedule`/`workflow_dispatch`), `permissions`, `concurrency` und einen `uses:`-Schritt mit den prüftypspezifischen Werten. Alternativ ein wiederverwendbarer Workflow per `workflow_call`.

## Geprüfte Dateien

- `.github/workflows/review-architecture-security.yml` (neu)
- `.github/workflows/review-nuget-packages.yml` (neu)
- `.github/workflows/review-compliance.yml` (neu)
- `docs/help/ci-cd/workflows.md` (geändert)
- `docs/help/ci-cd/README.md` (geändert)
- `docs/help/ci-cd/einrichtung.md` (geändert)
- `changes.log` (geändert)

## Positiv geprüft (keine Befunde)

- **Issue-Bodies:** Exakte Übereinstimmung mit der dekodierten Form von `issue.md` (Zeilen 29–45, 54–71, 80–96); korrektes UTF-8-Deutsch ohne Mojibake und ohne BOM — Verifikation per YAML-Parsing und Stringvergleich.
- **Heredoc-Mechanik:** Nach dem YAML-Dedent beginnen Body-Zeilen und das `EOF`-Ende korrekt in Spalte 0; keine führenden Leerzeichen im Body, keine Leerzeichen auf den Leerzeilen; `bash -n` bestätigt gültige Syntax aller drei `run`-Skripte.
- **Fehlerbehandlung:** Scheitert `gh issue list`, bricht `bash -e` den Schritt über den Exit-Status der Zuweisung ab — kein stilles Duplikat-Erzeugen. `jq length > 0` behandelt den Grenzfall „ein offenes Issue" korrekt.
- **Berechtigungen/Struktur:** Minimale `permissions: issues: write`, kein unnötiger Checkout (Repo-Auflösung per `GITHUB_REPOSITORY`), `concurrency`-Gruppe serialisiert `schedule`- und `workflow_dispatch`-Läufe, `timeout-minutes` gesetzt.
- **Cron/Konsistenz:** `0 6 1 * *`, `0 5 * * 2`, `0 6 1 1 *` stimmen mit Kommentaren, Dokumentation und `changes.log` überein; Datei-, Workflow-, Job- und Label-Namen folgen der Kebab-Case-Konvention des Bestands.
- **Dokumentation:** Tabelle „Die elf Workflows" hat tatsächlich elf Zeilen (elf Dateien vorhanden); `pages.yml`-Zeile beschreibt Trigger und Pfade korrekt; `einrichtung.md`/`README.md` konsistent ergänzt; `changes.log`-Eintrag folgt der Konvention inklusive „Keine Schemaänderung."
- **Validierung:** `python scripts/validate-workflows.py` meldet nur die zwei preexisting `e2e-dumps`-Befunde (Issue #22); die neuen Workflows erzeugen keine neuen Befunde.
