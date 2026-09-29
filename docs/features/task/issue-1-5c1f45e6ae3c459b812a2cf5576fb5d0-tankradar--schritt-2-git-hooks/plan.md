# Umsetzungsplan: Nachbesserung Lokale Git-Hooks zur Qualitätssicherung (Schritt 2)

## Übersicht

Dieser Plan beschreibt die Nachbesserung des Entwicklungsschritts 2 (Git-Hooks), um die Qualitätssicherung konsistent und blockierend zu implementieren. Die Umsetzung umfasst fünf Arbeitsbereiche: (1) Code-Formatierung blockierend machen, (2) XML-Dokumentation und Übersetzungsprüfungen blockierend machen, (3) Commit-Typ-Prüfung erweitern (neue Types und Breaking-Change-Notation), (4) Prüfung verbotener Muster erweitern (JSON-API-Keys, iOS-Dateien, SQL-Dumps, Logdateien ohne Limit), (5) Hook-Dateien mit korrekten Ausführungsrechten versehen. Alle Änderungen erfolgen in `.githooks/` (Hook- und Check-Skripte sowie Tests) und den betroffenen Quellcode-Dateien.

---

## Designentscheidungen

Keine — folgt bestehenden Mustern. Alle Anforderungen sind in den bestehenden Hook- und Check-Skript-Strukturen unmittelbar umsetzbar. Konventionen für Commit-Types, Prüf-Parameter und Rückgabecodes sind bereits etabliert.

---

## Programmabläufe

### Pre-Commit-Hook mit blockierenden Checks

1. Git pre-commit Hook wird ausgelöst
2. Hook ermittelt Projekt-Root mit `git rev-parse --show-toplevel`
3. Branch-Schutz-Prüfung: Blockiert direkte Commits auf `main` und `staging` (Exit 1)
4. **Blockierende Checks auf gestagte Dateien:**
   - `format-code-style-check.py --strict` — Code-Formatierungsfehler blockieren
   - `csproj-xmldoc-check.py` — Fehlende XML-Doku und ungültige Konfiguration blockieren
   - `translation-check.py` — Übersetzungs-Inkonsistenzen blockieren
   - `forbidden-patterns-check.py --strict` — Verbotene Muster blockieren
5. **Warnende Checks (nicht blockierend):**
   - `enum-coverage-check.py` — warnt bei Enum-Testlücken
   - `no-notimplemented-check.py` — warnt bei NotImplementedException
6. Alle Python-Skripte nutzen `python3` mit Fallback auf `python`
7. Exit-Code 0 bei Erfolg, Exit 1 bei Blockierung (Commit abgebrochen)

Beteiligte Klassen/Komponenten: `.githooks/pre-commit` (Shell), `format-code-style-check.py`, `csproj-xmldoc-check.py`, `translation-check.py`, `forbidden-patterns-check.py`, `_hook_common.py`

### Pre-Push-Hook mit Ref-basierter Prüfung

1. Git pre-push Hook wird ausgelöst, empfängt stdin im Format `<local_ref> <local_sha> <remote_ref> <remote_sha>`
2. Hook ermittelt Projekt-Root
3. Branch-Schutz-Prüfung: Blockiert direkte Pushes auf `main` und `staging` (Exit 1)
4. **Für jeden gepushten Ref aus stdin:**
   - Commit-Bereich ermitteln: `<local_ref>..<remote_sha>` (tatsächlich gepushte Commits)
   - `conventional-commits-check.py` mit `--base <remote_sha>` aufrufen (validiert Commits im Bereich)
   - `forbidden-patterns-check.py --all --strict` — ganzes Repo auf verbotene Muster prüfen
   - `format-code-style-check.py --strict` — ganzes Repo auf Formatierungsfehler prüfen
   - `csproj-xmldoc-check.py --all` — ganzes Repo auf XML-Doku-Fehler prüfen
   - `translation-check.py --all` — ganzes Repo auf Übersetzungsfehler prüfen
   - `enum-coverage-check.py --all --strict` — ganzes Repo auf Enum-Testlücken prüfen
   - `no-notimplemented-check.py --all --strict` — ganzes Repo auf NotImplementedException prüfen
   - `test-execution-check.py` — Projekt-Tests ausführen
5. Exit-Code 0 bei Erfolg, Exit 1 bei einem fehlgeschlagenen Check (Push abgebrochen)

Beteiligte Klassen/Komponenten: `.githooks/pre-push` (Shell), `conventional-commits-check.py`, `forbidden-patterns-check.py`, `format-code-style-check.py`, `csproj-xmldoc-check.py`, `translation-check.py`, `enum-coverage-check.py`, `no-notimplemented-check.py`, `test-execution-check.py`, `_hook_common.py`

### Conventional-Commits-Validierung mit Breaking-Change-Notation

1. `validate_message(subject_line)` wird mit einer Commit-Nachricht aufgerufen
2. Erste Zeile wird nach Conventional-Commits-Pattern geparst: `<type>(<scope>)?: <subject>`
3. **Type-Prüfung:** Zulässige Types sind `feat`, `fix`, `docs`, `test`, `chore`, `plan`, `merge`, `ci`, `build`, `style`, `perf`, `refactor`, `revert`
4. **Breaking-Change-Notation erkannt:**
   - Form 1: `feat!: description` oder `fix!: description` (Type mit `!` vor dem Doppelpunkt)
   - Form 2: `BREAKING CHANGE: description` irgendwo im Commit-Body (Zeilen nach Subjekt)
5. Subjekt-Länge: Minimum 10 Zeichen
6. Rückgabe: Leere Liste bei Erfolg, Liste mit Fehlermeldungen bei Verstößen

Beteiligte Klassen/Komponenten: `conventional-commits-check.py`, `validate_message()`

### Forbidden-Patterns-Prüfung mit erweiterten Mustern

1. `forbidden-patterns-check.py` wird mit optionalen Flags `--all` und `--strict` aufgerufen
2. **Blockierte Dateitypen (direkt):**
   - Zertifikate: `.pfx`, `.p12`, `.keystore`, `.jks`, `.pem`, `.cer`
   - Datenbank-Dumps: `.db`, `.sqlite`, `.sqlite3`, `.sql` (neu)
   - iOS-Signierungsdateien: `.mobileprovision`, `.p8` (neu)
3. **Blockierte Inhalts-Muster (Regex):**
   - API-Schlüssel (=Notation): `FUEL_API_KEY=`, `API_KEY=` etc.
   - API-Schlüssel (JSON-Notation): `"TankerkoenigApiKey": "..."`, `".*[Kk]ey.*": ".*"`, `".*[Ss]ecret.*": ".*"` (neu)
   - Zertifikat-Inhalte: `-----BEGIN RSA PRIVATE KEY-----`
   - Signierungsdaten in `.csproj`: `<SigningKey>value</SigningKey>`
4. **Logdateien:**
   - `.log`-Dateien werden geprüft, aber **ohne Größenlimit** (entfernt: 1-MB-Grenze)
   - Ausnahme: `changes.log` ist explizit erlaubt (Whitelist-Eintrag)
5. Rückgabe: Leere Liste bei Erfolg, Liste mit Fehlermeldungen bei Verstößen

Beteiligte Klassen/Komponenten: `forbidden-patterns-check.py`, `check_api_keys()`, `check_certificate_content()`, `check_signing_data()`

---

## Neue Klassen

Keine — alle Erweiterungen erfolgen in bestehenden Skripten.

---

## Änderungen an bestehenden Klassen

### `.githooks/conventional-commits-check.py`

- **Geänderte Konstante:** `ALLOWED_TYPES` wird erweitert um `'ci'`, `'build'`, `'style'`, `'revert'`
  - Alter Wert: `{'feat', 'fix', 'docs', 'test', 'refactor', 'chore', 'perf', 'plan', 'merge'}`
  - Neuer Wert: `{'feat', 'fix', 'docs', 'test', 'refactor', 'chore', 'perf', 'plan', 'merge', 'ci', 'build', 'style', 'revert'}`
- **Geänderte Funktion:** `validate_message()` — Regex-Pattern für Type-Validierung wird angepasst, um Breaking-Change-Notation (`feat!:`, `fix!:` etc.) zu akzeptieren
  - Alt: `^(feat|fix|...)\((.*)?\): (.+)$`
  - Neu: `^(feat|fix|...)(!)?\((.*)?\): (.+)$` (optionales `!` vor dem Doppelpunkt)
- **Neue Funktion:** `check_breaking_change()` — prüft Body auf `BREAKING CHANGE:` Zeilen
  - Parameter: `commit_body` (str) — alle Zeilen nach Subjekt
  - Rückgabe: `bool` — True wenn Breaking-Change-Notation gefunden
- **Geänderte Funktion:** `commits_since_divergence()` — wird durch `--base <ref>`-Parameter gesteuert; akzeptiert externe Ref-Angabe aus stdin statt nur hardcodierter main/origin/main

### `.githooks/forbidden-patterns-check.py`

- **Neue Konstante:** `BLOCKED_EXTENSIONS` um `'.sql'`, `'.mobileprovision'`, `'.p8'` erweitern
  - Alt: `{'.pfx', '.p12', '.keystore', '.jks', '.pem', '.cer', '.db', '.sqlite', '.sqlite3'}`
  - Neu: `{'.pfx', '.p12', '.keystore', '.jks', '.pem', '.cer', '.db', '.sqlite', '.sqlite3', '.sql', '.mobileprovision', '.p8'}`
- **Geänderte Konstante:** `LOG_SIZE_LIMIT_BYTES` — auf `0` setzen (keine Größenlimit, alle Logdateien blockiert)
  - Alt: `1024 * 1024` (1 MB)
  - Neu: `0` oder Konstante entfernen und Prüfung anpassen
- **Neue Konstante:** `ALLOWED_LOG_FILES` — Whitelist für Logdateien
  - Wert: `{'changes.log'}`
- **Geänderte Funktion:** `check_api_keys()` — Regex-Pattern für JSON-Notation hinzufügen
  - Alt: Nur `=`-Notation (`FUEL_API_KEY=...`)
  - Neu: Zusätzlich JSON-Notation (`"TankerkoenigApiKey": "..."`, `".*[Kk]ey.*": "..."`)
- **Geänderte Funktion:** Logdatei-Prüfung — `changes.log` explizit ausschließen
  - Prüfungslogik: Wenn Dateiname in `ALLOWED_LOG_FILES`, übergehen; sonst blockieren

### `.githooks/format-code-style-check.py`

- **Geänderte Logik in Hook-Aufruf:** Pre-commit ruft Script mit `--strict` Flag auf (war: ohne Flag, nur Warnung)
  - Dies wird in der `.githooks/pre-commit` Shell-Datei konfiguriert, nicht im Skript selbst

### `.githooks/csproj-xmldoc-check.py`

- **Keine Code-Änderungen erforderlich** — Skript ist bereits funktionsfähig; Hook-Integration muss blockierend sein (siehe pre-commit Änderung)

### `.githooks/translation-check.py`

- **Keine Code-Änderungen erforderlich** — Skript ist bereits funktionsfähig; Hook-Integration muss blockierend sein (siehe pre-commit Änderung)

### `.githooks/pre-commit` (Shell-Skript)

- **Geänderte Hook-Logik:** Alle bisherigen Warnungs-Checks müssen blockierend aufgerufen werden
  - `format-code-style-check.py --strict` statt ohne Flag
  - `csproj-xmldoc-check.py` mit Fehler-Exit (bereits implementiert, aber prüfen)
  - `translation-check.py` mit Fehler-Exit (bereits implementiert, aber prüfen)
  - `forbidden-patterns-check.py --strict` statt ohne `--strict`
- **Verhalten:** Commit wird blockiert (Exit 1) bei Verstößen gegen blockierende Checks

### `.githooks/pre-push` (Shell-Skript)

- **Geänderte Ref-Verarbeitung:** stdin-Zeilen in `<local_ref> <local_sha> <remote_ref> <remote_sha>` Format parsen
- **Geänderte Commit-Bereich-Prüfung:** `conventional-commits-check.py --base <remote_sha>` aufrufen statt hardcodierter `main..HEAD`
  - Dies stellt sicher, dass nur die tatsächlich gepushten Commits geprüft werden
- **Verhalten:** Push wird blockiert (Exit 1) bei Verstößen

### `.githooks/_hook_common.py`

- **Keine Code-Änderungen erforderlich** — Hilfsfunktionen sind bereits vorhanden und werden durch erweiterte Checks genutzt

---

## Datenbankmigrationen

Keine.

---

## Validierungsregeln

Keine neuen Validierungsregeln erforderlich — alle Prüfungen sind bereits in den Check-Skripten implementiert oder werden durch Regex-Erweiterungen hinzugefügt.

---

## Konfigurationsänderungen

| Eintrag | Ort | Standardwert/Änderung | Zweck |
|---------|-----|----------------------|-------|
| `ALLOWED_TYPES` | `conventional-commits-check.py` | Erweitern um `ci`, `build`, `style`, `revert` | Unterstützung fehlender Standard-Types |
| `BLOCKED_EXTENSIONS` | `forbidden-patterns-check.py` | Erweitern um `.sql`, `.mobileprovision`, `.p8` | Blockierung zusätzlicher sensiblen Dateitypen |
| `LOG_SIZE_LIMIT_BYTES` | `forbidden-patterns-check.py` | `0` (keine Größenlimit) | Alle Logdateien blockieren außer `changes.log` |
| `ALLOWED_LOG_FILES` | `forbidden-patterns-check.py` | `{'changes.log'}` | Explizite Ausnahme für versioniertes Änderungsprotokoll |
| `.editorconfig` | Projekt-Root | Neue oder angepasste Datei | Standardisierung von Code-Formatierung (optional, aber empfohlen) |
| `Tankradar.MAUI.csproj` — `<PropertyGroup>` | `.csproj` | `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` oder `<WarningsNotAsErrors>` anpassen | CS1591 als Fehler konfigurieren |

---

## Seiteneffekte und Risiken

- **Formatierungsfehler blockieren pre-commit:** Bevor Commits möglich sind, müssen 6 Platform-Dateien mit `dotnet format` bereinigt werden. Dies ist notwendig und erwünscht, blockiert aber die weitere Arbeit bis zur Bereinigung.
- **XML-Doku-Fehler blockieren pre-commit:** Tank Radar MAUI .csproj muss CS1591 als Fehler konfigurieren, und fehlende XML-Doku in öffentlichen Types muss ergänzt werden. Dies ist ebenfalls notwendig und blockiert pre-commit bis zur Behebung.
- **Neue API-Key-Muster in JSON:** Wenn absichtlich JSON mit Keys wie `ApiKey`, `Secret`, `Token` committet werden soll (z. B. für Tests oder Dokumentation), müssen diese Muster angepasst oder Commits mit `SKIP_HOOKS=1` übersprungen werden. Risiko: Unerwartete Blockierungen bei harmloser JSON.
- **Logdatei-Blockierung ohne Größenlimit:** Alle `.log`-Dateien außer `changes.log` werden blockiert, auch kleine Dateien. Falls Logging-Dateien in Tests oder Build-Artefakten committet werden, müssen diese bereinigt oder Commits übersprungen werden.
- **Breaking-Change-Notation:** Commits mit `feat!:` oder `BREAKING CHANGE:` sind jetzt möglich; müssen aber korrekt formatiert sein. Fehler bei der Notation können zu Hook-Blockierung führen.

---

## Umsetzungsreihenfolge

1. **Konfiguration CS1591 in Tankradar.MAUI.csproj setzen**
   - Voraussetzungen: Keine
   - Beschreibung: `<PropertyGroup>` in `src/Tankradar.MAUI/Tankradar.MAUI.csproj` öffnen und `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` oder angepasste `<WarningsNotAsErrors>` eintragen, um CS1591 als Fehler zu konfigurieren.

2. **Fehlende XML-Dokumentation in 6 Platform-Dateien ergänzen**
   - Voraussetzungen: Schritt 1 abgeschlossen
   - Beschreibung: Folgende Dateien auf fehlende XML-Dokumentation prüfen und ergänzen:
     - `src/Tankradar.MAUI/Platforms/Android/MainApplication.cs`
     - `src/Tankradar.MAUI/Platforms/MacCatalyst/AppDelegate.cs`
     - `src/Tankradar.MAUI/Platforms/MacCatalyst/Program.cs`
     - `src/Tankradar.MAUI/Platforms/Windows/App.xaml.cs`
     - `src/Tankradar.MAUI/Platforms/iOS/AppDelegate.cs`
     - `src/Tankradar.MAUI/Platforms/iOS/Program.cs`

3. **Code-Formatierungsfehler beheben**
   - Voraussetzungen: Keine
   - Beschreibung: `dotnet format Tankradar.sln` ausführen, um Whitespace- und Formatierungsfehler in allen Dateien zu beheben. Verifizierung mit `dotnet format Tankradar.sln --verify-no-changes`.

4. **`.editorconfig` anlegen oder anpassen (optional)**
   - Voraussetzungen: Keine
   - Beschreibung: `.editorconfig` im Projekt-Root erstellen oder bestehende Datei anpassen, um Formatierungsstandards (Indentation, Zeilenlänge, Whitespace) festzulegen und zukünftige Formatierungsfehler zu vermeiden.

5. **`conventional-commits-check.py` erweitern**
   - Voraussetzungen: Keine
   - Beschreibung: 
     - `ALLOWED_TYPES` um `'ci'`, `'build'`, `'style'`, `'revert'` erweitern
     - Regex-Pattern für Type-Validierung anpassen, um Breaking-Change-Notation (`feat!:`, `fix!:` etc.) zu akzeptieren
     - `check_breaking_change()` Funktion hinzufügen zur Prüfung von `BREAKING CHANGE:` im Body
     - `commits_since_divergence()` Funktion anpassen, um `--base <ref>`-Parameter zu akzeptieren

6. **`forbidden-patterns-check.py` erweitern**
   - Voraussetzungen: Keine
   - Beschreibung:
     - `BLOCKED_EXTENSIONS` um `.sql`, `.mobileprovision`, `.p8` erweitern
     - `LOG_SIZE_LIMIT_BYTES` auf `0` setzen
     - `ALLOWED_LOG_FILES` Konstante hinzufügen mit `{'changes.log'}`
     - `check_api_keys()` Funktion anpassen, um JSON-Notation zu prüfen
     - Logdatei-Prüfungslogik anpassen, um `ALLOWED_LOG_FILES` zu berücksichtigen

7. **`.githooks/pre-commit` anpassen**
   - Voraussetzungen: Schritte 5 und 6 abgeschlossen
   - Beschreibung:
     - `format-code-style-check.py` mit `--strict` Flag aufrufen
     - `forbidden-patterns-check.py` mit `--strict` Flag aufrufen
     - Stellen Sie sicher, dass `csproj-xmldoc-check.py` und `translation-check.py` blockierend (mit fehlerausstieg) aufgerufen werden

8. **`.githooks/pre-push` anpassen**
   - Voraussetzungen: Schritte 5 und 6 abgeschlossen
   - Beschreibung:
     - stdin-Zeilen im Format `<local_ref> <local_sha> <remote_ref> <remote_sha>` parsen
     - Für jeden Ref `conventional-commits-check.py --base <remote_sha>` aufrufen statt hardcodierter `main..HEAD`
     - `forbidden-patterns-check.py` mit `--all --strict` aufrufen
     - `format-code-style-check.py` mit `--strict` aufrufen

9. **Git-Index Ausführungsrechte setzen**
   - Voraussetzungen: Alle Hook- und Check-Dateien im `.githooks/`-Verzeichnis müssen existieren
   - Beschreibung: Für jede Datei in `.githooks/` (pre-commit, pre-push, alle `.py`-Dateien) `git update-index --chmod=+x <Pfad>` ausführen, um Modus `100755` im Git-Index zu setzen.

10. **Tests erweitern für neue Patterns**
    - Voraussetzungen: Schritte 5 und 6 abgeschlossen
    - Beschreibung:
        - `test_conventional_commits_check.py` um Tests für Breaking-Change-Notation erweitern (`test_conventional_commits_breaking_change_notation_feat`, `test_conventional_commits_breaking_change_in_body`)
        - `test_conventional_commits_check.py` um Tests für neue Types erweitern (`test_conventional_commits_allows_ci_build_style_revert_types`)
        - `test_forbidden_patterns_check.py` um Tests für JSON-API-Key-Notation erweitern (`test_forbidden_patterns_detects_api_key_json_notation`)
        - `test_forbidden_patterns_check.py` um Tests für iOS-Signierungsdateien erweitern (`test_forbidden_patterns_detects_mobileprovision`, `test_forbidden_patterns_detects_p8_file`)
        - `test_forbidden_patterns_check.py` um Tests für SQL-Dumps erweitern (`test_forbidden_patterns_detects_sql_dump`)
        - `test_forbidden_patterns_check.py` um Tests für Logdatei-Whitelist erweitern (`test_forbidden_patterns_allows_changes_log`, `test_forbidden_patterns_blocks_other_log_files`)

11. **Alle Tests ausführen und verifizieren**
    - Voraussetzungen: Schritte 1-10 abgeschlossen
    - Beschreibung: Alle Test-Suites ausführen (`python3 test_conventional_commits_check.py -v`, `python3 test_forbidden_patterns_check.py -v`, etc.) und verifizieren, dass alle Tests bestehen.

12. **Pre-Commit und Pre-Push Hook-Funktionalität manuell testen**
    - Voraussetzungen: Schritte 1-11 abgeschlossen
    - Beschreibung:
        - Einen Test-Commit mit gültiger Conventional-Commit-Nachricht erstellen → pre-commit sollte blockieren, dann fehlende Formatierungen beheben
        - Einen Commit mit ungültigem Type erstellen → pre-commit sollte blockieren
        - Einen Commit mit Breaking-Change-Notation erstellen → pre-commit sollte akzeptieren
        - Einen Push durchführen → pre-push sollte alle Checks ausführen und blockieren bei Verstößen
        - Feature-Branch auf main mergen → pre-push sollte zulassen (main ist in origin vorhanden)

---

## Tests

### Neue Tests

| Test / Hilfsmethode | Testklasse | Was wird geprüft / bereitgestellt? |
|--------------------|------------|-------------------------------------|
| `test_conventional_commits_breaking_change_notation_feat` | `ConventionalCommitsCheckTests_Format` | Breaking-Change-Notation `feat!:` wird akzeptiert |
| `test_conventional_commits_breaking_change_notation_fix` | `ConventionalCommitsCheckTests_Format` | Breaking-Change-Notation `fix!:` wird akzeptiert |
| `test_conventional_commits_breaking_change_in_body` | `ConventionalCommitsCheckTests_Format` | `BREAKING CHANGE:` in Commit-Body wird erkannt |
| `test_conventional_commits_allows_ci_build_style_revert_types` | `ConventionalCommitsCheckTests_Format` | Types `ci`, `build`, `style`, `revert` werden akzeptiert |
| `test_forbidden_patterns_detects_api_key_json_notation` | `ForbiddenPatternsCheckTests_ApiKeys` | JSON-Notation `"TankerkoenigApiKey": "secret"` wird erkannt |
| `test_forbidden_patterns_detects_generic_json_api_keys` | `ForbiddenPatternsCheckTests_ApiKeys` | Generische JSON-Pattern `".*[Kk]ey.*": "..."` wird erkannt |
| `test_forbidden_patterns_detects_mobileprovision` | `ForbiddenPatternsCheckTests_Files` | `.mobileprovision` Dateien werden blockiert |
| `test_forbidden_patterns_detects_p8_file` | `ForbiddenPatternsCheckTests_Files` | `.p8` Dateien werden blockiert |
| `test_forbidden_patterns_detects_sql_dump` | `ForbiddenPatternsCheckTests_Files` | `.sql` Dateien werden blockiert |
| `test_forbidden_patterns_allows_changes_log` | `ForbiddenPatternsCheckTests_Files` | `changes.log` ist explizit erlaubt |
| `test_forbidden_patterns_blocks_other_log_files` | `ForbiddenPatternsCheckTests_Files` | Alle anderen `.log` Dateien (beliebige Größe) werden blockiert |

### Betroffene bestehende Tests

Keine. Die neuen Parameter und Funktionen erweitern nur die bestehenden Tests; bestehende Tests bleiben gültig.

### E2E-Tests (primärer Funktionsnachweis)

Diese Umsetzung betrifft die automatisierte Qualitätssicherung durch Git-Hooks. Die primären Funktionsnachweise sind **manuelle Hook-Tests**, nicht E2E-Tests im UI-Sinne, da es keinen Benutzerabfluss über die Anwendungs-UI gibt. Die Hook-Funktionalität wird durch folgende manuelle Szenarien nachgewiesen:

| Priorität | Szenario | Testdatei / Testklasse | Abgedecktes Akzeptanzkriterium | Warum E2E nötig ist |
|-----------|----------|------------------------|-------------------------------|-------------------|
| Pflicht | Pre-Commit blockiert Formatierungsfehler | Manual test `.githooks/pre-commit` | Format-Fehler werden blockiert | Hook-Verhalten ist nur durch tatsächliche Git-Aktion überprüfbar |
| Pflicht | Pre-Commit akzeptiert gültige Commits | Manual test `.githooks/pre-commit` | Gültige Commits werden akzeptiert | Hook-Verhalten ist nur durch tatsächliche Git-Aktion überprüfbar |
| Pflicht | Pre-Commit blockiert fehlende XML-Doku | Manual test `.githooks/pre-commit` | XML-Doku-Fehler werden blockiert | Hook-Verhalten ist nur durch tatsächliche Git-Aktion überprüfbar |
| Pflicht | Pre-Push blockiert ungültige Commit-Types | Manual test `.githooks/pre-push` | Ungültige Types werden blockiert | Hook-Verhalten ist nur durch tatsächliche Git-Aktion überprüfbar |
| Pflicht | Pre-Push akzeptiert Breaking-Change-Notation | Manual test `.githooks/pre-push` | `feat!:` und `BREAKING CHANGE:` werden akzeptiert | Hook-Verhalten ist nur durch tatsächliche Git-Aktion überprüfbar |
| Pflicht | Pre-Push blockiert JSON-API-Keys | Manual test `.githooks/pre-push` | JSON-API-Keys werden erkannt und blockiert | Hook-Verhalten ist nur durch tatsächliche Git-Aktion überprüfbar |
| Pflicht | Pre-Push akzeptiert changes.log | Manual test `.githooks/pre-push` | `changes.log` wird akzeptiert | Hook-Verhalten ist nur durch tatsächliche Git-Aktion überprüfbar |
| Pflicht | Pre-Push blockiert andere Logdateien | Manual test `.githooks/pre-push` | Andere `.log` Dateien werden blockiert | Hook-Verhalten ist nur durch tatsächliche Git-Aktion überprüfbar |

**Begründung:** Die Hook-Prüfungen werden direkt im Git-Workflow ausgelöst und sind nicht über UI-Aktionen erreichbar. Unit-Tests (in `test_*.py`) prüfen einzelne Funktionen; Hook-Tests prüfen das Gesamtverhalten von `pre-commit` und `pre-push` Shell-Skripten. E2E-Tests als automatisierte Szenarien sind nicht notwendig, da die manuelle Validierung ausreichend ist und die Hook-Aufrufe nicht durch automatisierte Workflows ersetzt werden können.

Betroffene bestehende E2E-Tests: Keine — es gibt keine bestehenden E2E-Tests für Git-Hooks in diesem Projekt.

---

## Offene Punkte

Keine. Alle in der Anforderung genannten Punkte sind geklärt:

1. ✓ CS1591-Konfiguration: Wird als Fehler konfiguriert mit eng gefassten Ausnahmen für generierten Code
2. ✓ Breaking-Change-Notation: Beide Notationen (`feat!:` und `BREAKING CHANGE:`) werden akzeptiert
3. ✓ API-Schlüssel in JSON: Generische Regex mit JSON-Notation werden implementiert
4. ✓ .editorconfig: Anlage ist optional empfohlen
5. ✓ Logdateien-Ausnahme: `changes.log` wird als Whitelist-Eintrag auf Dateiname implementiert
6. ✓ Commit-Typen in Breaking-Change-Kontext: Alle erlaubten Types können Breaking-Change-Notation tragen
