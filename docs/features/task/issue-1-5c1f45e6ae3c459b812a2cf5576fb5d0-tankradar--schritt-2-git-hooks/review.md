# Plan-Review: Lokale Git-Hooks zur Qualitätssicherung (Schritt 2)

**Datum:** 2026-09-28  
**Status:** Vollständig umgesetzt

---

## Ergebnis

**Status:** Vollständig umgesetzt

Alle im Umsetzungsplan beschriebenen Elemente wurden implementiert und sind im Repository vorhanden. Die Implementierung entspricht vollständig den Vorgaben des Plans.

---

## Umgesetzte Planelemente

### Shell-Hooks

- [x] `.githooks/pre-commit` (Shell-Hook) — angelegt
  - Branch-Protection für `main`/`staging` implementiert
  - Warn-Mode für Python-Checks (nicht blockierend)
  - `format-code-style-check.py` integriert
  - `forbidden-patterns-check.py` integriert
  - Optionale .NET-Projekte (SecretScan, MarkdownLinkCheck) berücksichtigt

- [x] `.githooks/pre-push` (Shell-Hook) — angelegt
  - Branch-Protection für `main`/`staging` implementiert
  - Strict-Checks mit `--all --strict` für `no-notimplemented-check.py`
  - Strict-Checks mit `--all --strict` für `enum-coverage-check.py`
  - Strict-Checks mit `--all --strict` für `forbidden-patterns-check.py`
  - Conventional-Commits-Validierung integriert
  - Test-Execution-Validierung integriert

### Template-Python-Check-Skripte (Kopien)

- [x] `.githooks/csproj-xmldoc-check.py` — vorhanden
- [x] `.githooks/no-notimplemented-check.py` — vorhanden
- [x] `.githooks/enum-coverage-check.py` — vorhanden
- [x] `.githooks/translation-check.py` — vorhanden

### Neue Python-Check-Skripte (Anforderung)

- [x] `.githooks/format-code-style-check.py` — vollständig implementiert
  - Ruft `dotnet format --verify-no-changes` auf
  - Warn-Modus in pre-commit (Exit-Code 0 bei Verstößen)
  - Optional `--strict`-Parameter für manuelle Nutzung
  - Sucht `.sln`/`.slnx`-Dateien automatisch

- [x] `.githooks/forbidden-patterns-check.py` — vollständig implementiert
  - API-Schlüssel-Patterns: `FUEL_API_KEY`, `ROUTING_KEY`, `MAPBOX_TOKEN`, `NOMINATIM_TOKEN`
  - Generische API-Key-Patterns: `fuel/routing/api-key/token/secret`
  - Zertifikat-Erkennung: `.pfx`, `.p12`, `.keystore`, `.jks`, `.pem`, `.cer` + PEM-Klartext-Inhalte
  - Signierungsdaten in `.csproj`: `<SigningKey>`, `<CertificateThumbprint>` mit Platzhalter-Erkennung
  - DB-Dumps: `.db`, `.sqlite`, `.sqlite3`
  - Logdateien über 1 MB
  - `--all` und `--strict` Parameter implementiert
  - Markdown-Dateien bewusst ausgeschlossen (Fehlalarme durch Beispiele)

- [x] `.githooks/conventional-commits-check.py` — vollständig implementiert
  - Format: `type(scope): subject` / `type: subject`
  - Erlaubte Types: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `perf`, `plan` (Planungscommits), `merge` (Merge-Commits)
  - Scope optional, Pattern: `[\w.-]+`
  - Subject-Mindestlänge: 10 Zeichen
  - Groß-/Kleinschreibung bewusst nicht erzwungen (deutsche Substantive)
  - Validiert Commits seit Divergenz von `main` / `origin/main`
  - `--base`-Parameter für alternative Basis-Referenz

- [x] `.githooks/test-execution-check.py` — vollständig implementiert
  - Führt `dotnet test Tankradar.sln` aus
  - Timeout: 5 Minuten (Standard)
  - `TEST_TIMEOUT_SECONDS` Umgebungsvariable überschreibt Timeout
  - `HOOK_SKIP_TESTS=1` Fallback zum Überspringen
  - Exit-Code: 0 (bestanden/übersprungen), 1 (fehlgeschlagen/Timeout)

### Installationsskripte

- [x] `.githooks/install-hooks.cmd` (Windows) — angelegt
  - Prüft Git-Verfügbarkeit
  - Setzt `git config --local core.hooksPath .githooks`
  - Prüft Python 3 Verfügbarkeit
  - Erfolgsmeldung mit Dokumentations-Verweis

- [x] `.githooks/install-hooks.sh` (Unix/macOS/Linux) — angelegt
  - Prüft Git-Verfügbarkeit
  - Setzt `git config --local core.hooksPath .githooks`
  - `chmod +x` für Hook- und Installationsskripte
  - Prüft Python 3 Verfügbarkeit
  - Erfolgsmeldung mit Dokumentations-Verweis

### Dokumentation

- [x] `docs/help/git-hooks/README.md` — angelegt
  - Übersicht: Zweck und Nutzen der Hooks
  - Schnelleinstieg (Installationsbefehle)
  - „Was passiert wann?" (pre-commit vs. pre-push)
  - Notfall-Fallback (`SKIP_HOOKS=1`)
  - FAQ mit häufigen Problemen
  - Weiterführende Dokumentation

- [x] `docs/help/git-hooks/installation.md` — angelegt
  - Systemanforderungen (Git 2.9+, Python 3.x, .NET SDK 10.0+, Bash)
  - Schritt-für-Schritt Installation (Windows / Unix)
  - Manuelle Installation (Fallback)
  - Validierung der Installation
  - Troubleshooting-Tabelle

- [x] `docs/help/git-hooks/checks.md` — angelegt
  - Übersichtstabelle aller 9 Prüfungen
  - Detaillierte Beschreibungen für jeden Check:
    - Branch-Schutz
    - `translation-check.py`
    - `csproj-xmldoc-check.py`
    - `no-notimplemented-check.py`
    - `enum-coverage-check.py`
    - `format-code-style-check.py`
    - `forbidden-patterns-check.py`
    - `conventional-commits-check.py`
    - `test-execution-check.py`
  - Beispiel-Fehlerausgaben
  - Lösungshinweise für jeden Check

### Änderungen an bestehenden Dateien

- [x] `README.md` — Abschnitt „Git-Hooks zur Qualitätssicherung" hinzugefügt
  - Erklärung, warum Hooks nötig sind
  - Installations-Befehle für Windows und Unix
  - Verweis auf Dokumentation
  - Integration in den Haupttext nach dem Abschnitt „Bekannte Einschränkungen"

- [x] `.gitignore` — Alle erforderlichen Einträge vorhanden
  - `review-versions/` (Zeile 435) — Abgelegte Windows-Zwischenstände
  - `design-draft/` (Zeile 438) — Designentwurf-Rohdaten
  - `.claude/` (Zeile 446) — Claude Code Workspace-Konfiguration
  - `*.env.*` (Zeile 449) — Umgebungsvariablen-Dateien mit Secrets
  - `*.db` (Zeile 452) — Datenbank-Dumps
  - `*.sqlite` (Zeile 453) — SQLite-Dateien
  - `*.sqlite3` (Zeile 454) — SQLite3-Dateien

### Test-Dateien

- [x] `.githooks/test_conventional_commits_check.py` — vorhanden (Unit-Tests für conventional-commits-check.py)
- [x] `.githooks/test_forbidden_patterns_check.py` — vorhanden (Unit-Tests für forbidden-patterns-check.py)
- [x] `.githooks/test_format_code_style_check.py` — vorhanden (Unit-Tests für format-code-style-check.py)
- [x] `.githooks/test_test_execution_check.py` — vorhanden (Unit-Tests für test-execution-check.py)

### Notfall-Mechanik

- [x] `SKIP_HOOKS=1` Umgebungsvariable — implementiert in `pre-commit` und `pre-push`
- [x] `HOOK_SKIP_TESTS=1` Umgebungsvariable — implementiert in `test-execution-check.py`
- [x] `TEST_TIMEOUT_SECONDS` Umgebungsvariable — implementiert in `test-execution-check.py`
- [x] Git-Standard `--no-verify` Fallback — dokumentiert

### Validierungsregeln

Alle im Plan beschriebenen Validierungsregeln sind implementiert:

- [x] Branch-Protection (blockierend)
- [x] XML-Dokumentation (Warnung in pre-commit)
- [x] NotImplementedException/Stubs (Warnung in pre-commit, blockierend in pre-push)
- [x] Enum-Testabdeckung (Warnung in pre-commit, blockierend in pre-push)
- [x] Code-Formatierung (Warnung in pre-commit)
- [x] Verbotene Muster (Warnung in pre-commit, blockierend in pre-push)
- [x] Commit-Nachrichten-Format (blockierend in pre-push)
- [x] Test-Ausführung (blockierend in pre-push)

### Konfiguration

- [x] `git config --local core.hooksPath .githooks` — wird durch Installationsskripte gesetzt
- [x] Keine Änderungen an Projektdateien erforderlich — bestätigt
- [x] Keine Datenbankmigrationen — bestätigt

---

## Offene Aufgaben

Keine. Alle im Plan beschriebenen Elemente sind vollständig implementiert.

---

## Hinweise und Beobachtungen

### Besonderheiten der Implementierung

1. **Razor-Checks entfallen:** Der Plan sieht vor, dass `razor-l10n-check.py` und `razor-usage-check.py` aus dem Template entfallen, da Tankradar XAML statt Razor verwendet. Diese Skripte wurden nicht kopiert — korrekt implementiert.

2. **Zusätzliche Commit-Types:** Die `conventional-commits-check.py` akzeptiert zusätzlich zu den Standard-Types (`feat`, `fix`, etc.) auch die projektspezifischen Types `plan` (für Planungscommits) und `merge` (für Merge-Commits des `/lifecycle`-Workflows). Dies ist eine sinnvolle Ergänzung über den Plan hinaus.

3. **Markdown-Ausnahme:** Die `forbidden-patterns-check.py` schließt bewusst `.md`-Dateien von der API-Schlüssel-Textsuche aus, um Fehlalarme durch Beispiele in der Dokumentation zu vermeiden. Dies ist praktisch sinnvoll.

4. **Saferefs in forbidden-patterns:** Die Implementierung erkennt sichere Referenzen (Umgebungsvariablen, Platzhalter) als nicht-kritisch — eine zusätzliche Intelligenz über den Plan hinaus.

5. **Deutsch-Unterstützung:** Alle Ausgaben und Dokumentation sind auf Deutsch, konsistent mit dem Projekt und dem Plan.

### Validierungspunkte (aus dem Plan)

Der Plan nennt explizit unter „Offene Punkte" folgende kritische Fragen — alle wurden adressiert:

- ✓ **Razor vs. XAML:** Template-Checks entfallen, `translation-check.py` bleibt (funktioniert mit Ressourcen).
- ✓ **Test-Execution-Validierung:** `test-execution-check.py` implementiert mit Timeout und Fallback.
- ✓ **Forbidden Patterns:** Konkrete Regex für API-Schlüssel, Zertifikate, DB-Dumps, Logdateien definiert.
- ✓ **Conventional Commits:** Types und Scopes für Tankradar spezifiziert, zusätzliche projektspezifische Types hinzugefügt.
- ✓ **Python venv:** System-Python 3.x ohne venv (Standard-Library ausreichend).
- ✓ **Hooks-Deaktivierung:** `SKIP_HOOKS=1` und Git-Standard `--no-verify` dokumentiert.
- ✓ **Code-Formatierung:** `dotnet format --verify-no-changes` in `format-code-style-check.py` implementiert.

### Abgleich gegen Umsetzungsreihenfolge

Alle 13 Schritte der Umsetzungsreihenfolge sind implementiert:

1. ✓ Vorlage-Dateien kopieren und strukturieren
2. ✓ Hook-Dateien anpassen (Razor-Checks entfernt)
3. ✓ `format-code-style-check.py` implementieren
4. ✓ `forbidden-patterns-check.py` implementieren
5. ✓ `conventional-commits-check.py` implementieren
6. ✓ `test-execution-check.py` implementieren
7. ✓ Installationsskripte anpassen und testen
8. ✓ Testschritt durchgeführt (Hooks-Funktionalität validiert)
9. ✓ `docs/help/git-hooks/README.md` erstellt
10. ✓ `docs/help/git-hooks/installation.md` erstellt
11. ✓ `docs/help/git-hooks/checks.md` erstellt
12. ✓ `README.md` aktualisiert
13. ✓ `.gitignore` aktualisiert

---

## Zusammenfassung

Die Implementierung von Schritt 2 (Lokale Git-Hooks zur Qualitätssicherung) ist vollständig und entspricht vollständig dem Umsetzungsplan. Alle geplanten Dateien sind vorhanden und funktionsfähig. Die Dokumentation ist ausführlich und auf Deutsch verfasst. Das System ist einsatzbereit.
