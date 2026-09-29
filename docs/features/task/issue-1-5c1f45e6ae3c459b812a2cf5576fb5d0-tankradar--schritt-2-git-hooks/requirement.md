# Anforderungsübersetzung: Nachbesserung Lokale Git-Hooks (Schritt 2, Runde 1)

**Branch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar--schritt-2-git-hooks`  
**Basisbranch:** `task/issue-1-5c1f45e6ae3c459b812a2cf5576fb5d0-tankradar`

---

## Fachliche Zusammenfassung

Der Entwicklungsschritt 2 (lokale Git-Hooks zur Qualitätssicherung) wird nachgebessert, um folgende Defizite zu beheben: (1) Der aktuelle Repository-Stand erfüllt die Formatierungsprüfungen nicht; (2) Formatierungs- und Code-Stil-Verstöße werden nur gemeldet, nicht blockiert; (3) XML-Dokumentations- und Übersetzungsprüfungen werden nicht blockierend durchgesetzt; (4) Die Prüfung verbotener Muster (Secrets, Zertifikate, Datenbank-Dumps, Logdateien) hat Lücken; (5) Die Commit-Typ-Prüfung berücksichtigt nicht alle Standard-Types und Breaking-Change-Notationen; (6) Die Commit-Prüfung im pre-push nutzt hardcodierte Refs statt der tatsächlich gepushten Bereiche; (7) Hook-Dateien haben nicht die erforderlichen Ausführungsrechte im Repository. Das Ziel ist ein konsistenter, blockierender Qualitätssicherungs-Workflow in pre-commit und pre-push, der dem Repository-Stand entspricht und die automatisierten Commit-Types des Projekts nicht blockiert.

---

## Betroffene Klassen und Komponenten

### Hook-Dateien (Shell-Skripte)
- `.githooks/pre-commit` — Pre-Commit-Hook mit blockierend/Warn-Modus-Logik
- `.githooks/pre-push` — Pre-Push-Hook mit Ref-basierter Prüfung statt hardcodierter Refs

### Check-Skripte (Python)
- `.githooks/format-code-style-check.py` — Formatierungsprüfung (`dotnet format --verify-no-changes`), mit Erweiterung für blockierenden Modus in pre-push
- `.githooks/forbidden-patterns-check.py` — Prüfung verbotener Muster, mit Erweiterungen für API-Schlüssel (JSON-Notation), iOS-Signierungsdateien (.mobileprovision, .p8), Datenbank-Dumps (.sql) und Logdateien (ohne Größengrenze)
- `.githooks/conventional-commits-check.py` — Commit-Format-Validierung, mit Erweiterung um Types `ci`, `build`, `style`, `perf`, `refactor`, `revert` und Breaking-Change-Notation
- `.githooks/csproj-xmldoc-check.py` — XML-Dokumentationsprüfung
- `.githooks/translation-check.py` — Übersetzungs-Konsistenzprüfung
- `.githooks/_hook_common.py` — Gemeinsame Hilfsfunktionen (bei Bedarf erweitern)

### Test-Dateien (Python)
- `.githooks/test_format_code_style_check.py` — Tests für Formatierungsprüfung
- `.githooks/test_forbidden_patterns_check.py` — Tests für verbotene Muster (mit neuen Testfällen)
- `.githooks/test_conventional_commits_check.py` — Tests für Commit-Validierung
- `.githooks/test_hook_common.py` — Tests für gemeinsame Funktionen

### Konfigurationsdateien
- `.editorconfig` — Neue EditorConfig-Datei zur Standardisierung von Code-Formatierung (optional, aber empfohlen)
- `.gitattributes` — Bestehende Attribute für Hook-Dateien (um Ausführungsrechte zu setzen)

### Quellcode (zur Behebung von Formatierungsfehlern)
- `src/Tankradar.MAUI/Platforms/Android/MainApplication.cs`
- `src/Tankradar.MAUI/Platforms/MacCatalyst/AppDelegate.cs`
- `src/Tankradar.MAUI/Platforms/MacCatalyst/Program.cs`
- `src/Tankradar.MAUI/Platforms/Windows/App.xaml.cs`
- `src/Tankradar.MAUI/Platforms/iOS/AppDelegate.cs`
- `src/Tankradar.MAUI/Platforms/iOS/Program.cs`
- `.csproj`-Dateien (für CS1591-Fehler und XML-Dokumentation)

---

## Implementierungsansatz

### 1. Code-Formatierungsfehler beheben
- Fehlende WHITESPACE-Fehler in den aufgelisteten 6 Dateien mit `dotnet format` beheben
- Optional: `.editorconfig` anlegen oder bestehende EditorConfig-Einstellungen verfeinern, um zukünftige Formatierungsfehler zu vermeiden

### 2. Formatierungsprüfung blockierend machen
- `format-code-style-check.py` erhält zusätzlichen Kontext (z. B. `--push`-Flag) oder wird in pre-commit mit blockierendem Verhalten für gestagte Dateien aufgerufen
- Alternative: `pre-commit`-Hook überprüft gestagten Code und blockt bei Verstößen, oder warnt begründet; `pre-push`-Hook ruft `format-code-style-check.py --strict --all` auf und blockiert bei Verstößen

### 3. XML-Dokumentation und Übersetzungsprüfungen blockierend machen
- `pre-commit`-Hook: `translation-check.py` und `csproj-xmldoc-check.py` blockierend aufrufen (Exit 1 bei Verstoß), nicht nur warnen
- `pre-push`-Hook: beide Prüfungen mit `--all --strict` aufrufen
- Alternativ: Hook-Logik mit `set -eu` (wie in der Vorlage) nutzen, um blockierendes Verhalten zu erzwingen
- **Konkreter Sachstand:** `csproj-xmldoc-check.py --all` meldet "CS1591 ist nicht als Fehler konfiguriert" für Tankradar.MAUI.csproj; CS1591 sollte als Fehler konfiguriert oder mit eng gefasster Ausnahme (z. B. für generierten Code) versehen werden

### 4. Prüfung verbotener Muster erweitern
**In `forbidden-patterns-check.py`:**
- **API-Schlüssel in JSON-Notation:** Regex-Muster hinzufügen für `"TankerkoenigApiKey": "..."`, `"RoutingKey": "..."`, etc. (nicht nur `=`-Notation)
- **iOS-Signierungsdateien:** Dateitypen `.mobileprovision` und `.p8` zur Prüfung hinzufügen
- **Datenbank-Dumps:** Dateityp `.sql` zur Prüfung hinzufügen
- **Logdateien:** `LOG_SIZE_LIMIT_BYTES` entfernen oder auf 0 setzen (keine Größenbeschränkung); Ausnahme für `changes.log` (versioniertes Änderungsprotokoll) explizit eintragen
- **Tests:** Jeweils mit Testfällen in `test_forbidden_patterns_check.py` absichern

### 5. Conventional-Commits-Prüfung erweitern
**In `conventional-commits-check.py`:**
- `ALLOWED_TYPES` um `ci`, `build`, `style`, `perf`, `refactor`, `revert` erweitern (zusätzlich zu `feat`, `fix`, `docs`, `test`, `chore`, `plan`, `merge`)
- Breaking-Change-Notation unterstützen: `feat!:`, `fix!:` etc. (Regex anpassen) sowie `BREAKING CHANGE:` in Commit-Body
- **Pre-Push-Spezifik:** Commit-Prüfung nicht auf `main..HEAD` beschränken, sondern auf den tatsächlich gepushten Bereich (aus stdin-Refs) prüfen

### 6. Pre-Push Hook Ref-basiert anpassen
- Pre-Push-Hook liest stdin-Refs (Standard-Format: `<local_ref> <local_sha> <remote_ref> <remote_sha>`)
- Commit-Prüfung nutzt `<local_ref>..<remote_sha>` statt hardcodierter `main..HEAD`
- So werden nur die tatsächlich gepushten Commits geprüft, nicht alle Commits seit Fork

### 7. Hook-Dateien mit Ausführungsrecht setzen
- `.githooks/pre-commit`, `.githooks/pre-push` und alle `.py`-Check-Skripte müssen im Git-Index mit Modus 100755 (Ausführungsrecht) eingetragen sein
- Nutzen: `git update-index --chmod=+x <Pfad>` für jede Hook- und Python-Datei in `.githooks/`

---

## Konfiguration

### Repository-Level
- **Hook-Installation:** `.githooks/install-hooks.sh` / `.install-hooks.cmd` (bestehend) bleibt unverändert
- **Python-Interpreter-Fallback:** `pre-commit` und `pre-push` nutzen `python3` (bevorzugt) oder `python` als Fallback (bereits implementiert)
- **Git-Config:** `core.hooksPath` ist auf `.githooks` gesetzt (bestehend)

### Code-Stil (optional)
- `.editorconfig` mit Basis-Einstellungen (Indentation, Zeilenlänge, Whitespace) für .NET/C# und weitere Dateitypen
- `.csproj`: CS1591-Regel als Fehler konfigurieren oder mit Exceptions für generierten Code versehen

### Excluded Directories (in `_hook_common.py`)
- Bestehende Ausschlüsse: `.git`, `bin`, `obj`, `TestResults`, `node_modules`, `.vs`, `.idea`, `packages`
- Ggf. ergänzen, z. B. um Build-Ausgabeverzeichnisse

---

## Offene Fragen und Annahmen

1. **CS1591-Konfiguration:** Sollen alle public Typen und Member XML-Dokumentation haben, oder gibt es Ausnahmen (z. B. für plattformspezifischen generierten Code unter `Platforms/`)?  
   *Annahme: CS1591 als Fehler konfigurieren und fehlende XML-Dokumentation in betroffenen Dateien ergänzen, mit Ausnahmen nur für generierten Code.*

2. **Breaking-Change-Notation:** Soll die Notation `feat!:` und `BREAKING CHANGE:` in Commit-Body akzeptiert werden, oder nur eine der beiden?  
   *Annahme: Beide Notationen akzeptieren (Standard-Conventional-Commits).*

3. **API-Schlüssel in JSON:** Welche weiteren JSON-Keys sind relevant (z. B. `ApiKey`, `Secret`, `Token`)?  
   *Annahme: Generische Regex (`.*[Kk]ey.*`, `.*[Ss]ecret.*`, `.*[Tt]oken.*`) mit JSON-Notation (`"key": "..."`).*

4. **.editorconfig:** Soll eine .editorconfig-Datei neu angelegt werden, oder bestehen bereits EditorConfig-Einstellungen?  
   *Annahme: Neue .editorconfig anlegen oder bestehende anpassen, um zukünftige Formatierungsfehler zu vermeiden.*

5. **Logdateien-Ausnahme:** Die Ausnahme für `changes.log` soll explizit erlaubt sein — ist dies eine Whitelist-Regel pro Dateiname oder per Pfad?  
   *Annahme: Whitelist auf Dateiname (`changes.log`) mit vollständiger Pfadprüfung.*

6. **Commit-Typen in Breaking-Change-Kontext:** Sollen Breaking Changes auf alle erlaubten Types angewendet werden (z. B. `fix!:`, `refactor!:`) oder nur auf `feat!:`?  
   *Annahme: Alle erlaubten Types können Breaking-Change-Notation tragen.*

---

## Prüfkriterien für Abschluss

- [ ] **Code-Formatierung:** `dotnet format Tankradar.sln --verify-no-changes` meldet keine Fehler auf allen betroffenen Dateien
- [ ] **Pre-Commit Hook:** Läuft ohne Blockierung auf Feature-Branch; blockiert nur direkte Commits auf main/staging
- [ ] **Pre-Push Hook:** Läuft ohne Blockierung auf Feature-Branch; blockiert Verstöße gegen Formatierung (strict), XML-Doku, Übersetzungen, verbotene Muster und Conventional Commits
- [ ] **Conventional Commits:** Types `plan`, `feat`, `fix`, `chore`, `docs`, `test`, `merge`, `ci`, `build`, `style`, `perf`, `refactor`, `revert` sowie Breaking-Change-Notation sind akzeptiert; keine Blockierung automatisierter Commits des Projekts
- [ ] **Verbotene Muster:** Prüfung umfasst API-Schlüssel (JSON), iOS-Signierungsdateien, SQL-Dumps, Logdateien (unbegrenzte Größe) und `changes.log`-Ausnahme
- [ ] **Hook-Ausführungsrechte:** Alle Hook- und Check-Skript-Dateien haben Modus 100755 im Git-Index
- [ ] **Tests:** Alle neuen Prüfungen haben entsprechende Testfälle mit positiven und negativen Szenarien
