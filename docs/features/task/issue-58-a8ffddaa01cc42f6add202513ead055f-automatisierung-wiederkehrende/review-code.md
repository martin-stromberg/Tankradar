# Code-Review

## Ergebnis

**Status:** Keine Befunde

## Geprüfte Dateien

- `.github/actions/create-review-issue/action.yml` (neu)
- `.github/workflows/review-architecture-security.yml` (neu)
- `.github/workflows/review-nuget-packages.yml` (neu)
- `.github/workflows/review-compliance.yml` (neu)
- `docs/help/ci-cd/workflows.md` (geändert)
- `docs/help/ci-cd/README.md` (geändert)
- `docs/help/ci-cd/einrichtung.md` (geändert)
- `changes.log` (geändert)

Nicht Bestandteil des Code-Reviews (Prozess-Artefakte ohne Quellcode): `todo.md`, `review-code.1.md`, `review-usability.md`, `review.1.md`, `issue-58-…-tasks.md`.

## Positiv geprüft (keine Befunde)

- **Befund aus Iteration 1 behoben:** Der dreifach duplizierte `create-review-issue`-Job ist vollständig in die Composite Action `.github/actions/create-review-issue/action.yml` ausgelagert (Label-Erzeugung, Dedup-Prüfung, Issue-Erzeugung). Die drei Workflows enthalten nur noch Trigger, `permissions`, `concurrency`, Job-Gerüst und den `uses:`-Aufruf mit prüftypspezifischen `with:`-Werten — kein `run:`/`gh`-Code mehr in den Workflows (per grep verifiziert). Die verbleibende YAML-Deklaration ist nicht weiter auslagerbar (pro Workflow eigener Trigger und `name:`) und folgt der Repo-Konvention (`.github/actions/security-scan`, `build-and-package`, `package-ios`).
- **Inputs ohne Speculative Generality / toten Code:** Alle sechs Inputs (`label`, `color`, `description`, `title`, `date-format`, `body`) werden von allen drei Aufrufern befüllt und in der Action verwendet; `date-format` ist notwendig, da die drei Workflows unterschiedliche Formate nutzen (`%Y-%m`, `%Y-%m-%d`, `%Y`).
- **Issue-Bodies:** Byte-genaue Übereinstimmung mit der dekodierten Plan-Fassung (`plan.md` Zeilen 69–132) für alle drei Workflows — Verifikation per YAML-Parsing und Stringvergleich. Korrektes UTF-8-Deutsch ohne Mojibake und ohne BOM; Titel-Datumsteil wird per `date -u` gebildet, Trenner ` – ` (En-Dash) entspricht dem Plan-Muster.
- **Fehlerbehandlung:** Scheitert `gh issue list`, bricht `bash -e` (Runner-Default) den Schritt über den Exit-Status der Zuweisung ab — kein stilles Duplikat-Erzeugen. Das `jq length`-Dedup-Muster entspricht exakt der etablierten Konvention aus `staging-to-main-promotion.yml` (`gh pr list --json number | jq length`). `bash -n` bestätigt gültige Syntax beider `run`-Blöcke der Action.
- **Berechtigungen/Struktur:** Minimale `permissions` (`contents: read` für den Checkout der lokalen Action, `issues: write` für Label-/Issue-Aufrufe — Nachweis: `staging-to-main-promotion.yml`/`sync-staging-with-main.yml` legen Labels mit `issues: write` an). `actions/checkout@v7` ist die im gesamten Repo einheitlich verwendete Version. Checkout steht vor dem `uses:`-Schritt (zwingend für lokale Composite Actions). Disjunkte `concurrency`-Gruppen pro Workflow mit `cancel-in-progress: false`, `timeout-minutes: 5` gesetzt. `GH_TOKEN: ${{ github.token }}` je Schritt wie etabliert; Repo-Auflösung läuft jetzt über den Checkout-Remote.
- **Namenskonventionen:** Job-Name `create-review-issue` deckt sich mit dem Action-Verzeichnis; Datei-, Workflow-, Job-, Label- und Input-Namen folgen der Kebab-Case-Konvention des Bestands.
- **Cron/Konsistenz:** `0 6 1 * *`, `0 5 * * 2`, `0 6 1 1 *` stimmen mit Kommentaren, `changes.log` und Dokumentation überein; Label-Farben (`B60205`, `FBCA04`, `5319E7`) konsistent zwischen Workflows, `einrichtung.md` und `changes.log`.
- **Dokumentation:** `workflows.md`-Tabelle „Die elf Workflows" hat tatsächlich elf Zeilen (elf Dateien vorhanden); `pages.yml`-Zeile und der neue Absatz zum Issue-/Dedup-Muster beschreiben die Composite Action korrekt inklusive Input-Liste und der Checkout-/Permission-Begründung; `README.md` (neuer Punkt 6) und `einrichtung.md` (Abschnitt „3. Labels") konsistent ergänzt; `changes.log`-Eintrag folgt der Konvention inklusive „Keine Schemaänderung."
- **Validierung:** `python scripts/validate-workflows.py` meldet ausschließlich die zwei preexisting `e2e-dumps`-Befunde (Issue #22, Scope-Ausschluss); die neuen Dateien erzeugen keine neuen Befunde. `yaml.safe_load` bestätigt gültiges YAML und `on`-Blöcke mit `schedule` + `workflow_dispatch` in allen drei Workflows sowie der Action-Datei.
