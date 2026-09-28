# Anforderungsübersetzung: Git Hooks für MAUI/XAML-Projekt

## Fachliche Zusammenfassung

Das Repository soll mit lokalen Git-Hooks ausgestattet werden, die automatisch vor Commits und Pushes Qualitätssicherungsprüfungen durchführen. Die Hooks werden versioniert im Repository unter `.githooks/` abgelegt und über Installationsskripte (Windows: `.cmd`, Unix: `.sh`) aktiviert. Sie verbinden projektspezifische Anforderungen (Formatierung, Sicherheit, Test-Validierung, Conventional Commits) mit den aus einer Template-Vorlage übernommenen Checks, die für MAUI/XAML anwendbar sind (XML-Dokumentation, Placeholder-Implementierungen, Enum-Coverage, Translations, Branch-Protection für main/staging).

## Betroffene Klassen und Komponenten

### Dateien und Verzeichnisse
- **`.githooks/`** (neu, versioniert)
  - `pre-commit` (Shell-Hook, Bash/sh)
  - `pre-push` (Shell-Hook, Bash/sh)
  - `install-hooks.cmd` (Windows-Installationsskript)
  - `install-hooks.sh` (Unix-Installationsskript)
  - Python-Prüfskripte (teilweise aus Template übernommen, teilweise neu):
    - `csproj-xmldoc-check.py` (Template, übernommen)
    - `no-notimplemented-check.py` (Template, übernommen)
    - `enum-coverage-check.py` (Template, übernommen)
    - `translation-check.py` (Template, übernommen)
    - `format-code-style-check.py` (neu: Prüfung auf Formatierung/Code-Stil)
    - `forbidden-patterns-check.py` (neu: Prüfung auf API-Schlüssel, Zertifikate, DB-Dumps, Logdateien)
    - `conventional-commits-check.py` (neu: Validierung von Commit-Nachrichten)
    - `test-execution-check.py` (neu: Validierung lokaler Testausführung vor Push)
  - Eventuell: `razor-l10n-check.py`, `razor-usage-check.py` aus Template (entfallen oder werden sinngemäß ersetzt für XAML)

### Git-Konfiguration
- `core.hooksPath` wird auf `.githooks` gesetzt (via Installationsskripte)

### Dokumentation
- `docs/help/git-hooks/` (neu): Beschreibung Installation, Wirkung, Troubleshooting
- Aktualisierung `README.md` mit Hinweis auf Hook-Installation

### Tests und Validierung
- Testabdeckung für Hook-Logik (ggf. Unit-Tests für Python-Skripte)
- Validierung, dass Hooks auf aktuellem Stand des Repositorys ohne Fehler durchlaufen

## Implementierungsansatz

### Pre-commit Hook
Läuft automatisch vor jedem `git commit`:

1. **Branch-Protection**: Blockiert direkte Commits auf `main` und `staging` (aus Template übernommen)
2. **Translation-Check**: Prüfung auf fehlende Übersetzungen in XAML/XML-Ressourcen (Template)
3. **XML-Dokumentation**: Validierung von `<summary>`, `<param>`, `<returns>` Tags in C#-Code (`csproj-xmldoc-check.py`, Template)
4. **Placeholder-Implementierungen**: Blockierung von `NotImplementedException` und Stub-Methoden, Warn-Modus für gestaffelte Commits (Template)
5. **Enum-Coverage**: Warnung bei fehlender Testabdeckung von Enum-Werten (Template)
6. **Code-Formatierung und Stil**: Prüfung mit `dotnet format --verify-no-changes` oder ähnlich
7. **Forbidden Patterns**: Prüfung auf API-Schlüssel (z.B. für Kraftstoffpreis-Service, Routing-Service), Zertifikate, Signierungsdaten, Datenbank-Dumps, Logdateien

### Pre-push Hook
Läuft automatisch vor `git push`:

1. **Branch-Protection**: Blockiert direkte Pushes auf `main` und `staging` (Template)
2. **Strict Stub Check**: `no-notimplemented-check.py --all --strict` (Template) — ganzes Repo durchsuchen, blockierend
3. **Strict Enum Coverage**: `enum-coverage-check.py --all --strict` (Template) — ganzes Repo durchsuchen, blockierend
4. **Conventional Commits Validation**: Prüfung aller Commit-Nachrichten des Branches auf `type(scope): message` Format (neu)
5. **Test Execution Validation**: Validierung, dass lokale Tests erfolgreich ausgeführt wurden (neu; ggf. über `dotnet test` oder Test-Result-Marker)

### Installationsskripte

**Windows (`install-hooks.cmd`)**:
- Kopiert `.githooks/` nach `.git/hooks/` ODER setzt `git config core.hooksPath .githooks`
- Macht Hook-Dateien ausführbar (ggf. relevant für WSL)
- Validiert Python-Abhängigkeiten (falls erforderlich)

**Unix/Linux/macOS (`install-hooks.sh`)**:
- Setzt `git config core.hooksPath .githooks` oder erstellt Symlinks
- Macht Hook-Dateien und Python-Skripte ausführbar (`chmod +x`)
- Validiert Python-Abhängigkeiten

### Abhängigkeiten und Hooks

- **Python-Abhängigkeiten**: Mögliche externe Libs (z.B. `lxml` für XML-Parsing, Regex-Libs)
- **dotnet**: Erforderlich für Formatierungsprüfungen (`.NET SDK`)
- **Git**: Erforderlich, versteht sich von selbst

## Konfiguration

### Konfigurierbare Aspekte
1. **Hook-Aktivierung**: Benutzer können einzelne Hooks deaktivieren, falls nötig (Notfallfallback), z.B. über `.git/config` Umgebungsvariablen
2. **Schweregrad**: `--strict` Modus in pre-push ist nicht konfigurierbar (Anforderung), aber pre-commit läuft im Warn-Modus für einige Checks
3. **Forbidden Patterns**: Liste der verbotenen Datei-/Mustereigenschaften konfigurierbar (z.B. `.env.secrets`, API-Key-Pattern)
4. **Conventional Commits Format**: Standard-Format ist konfigurierbar (z.B. erlaubte Scopes, Types)

## Offene Fragen und Annahmen

### Zu klären vor Implementierung

1. **Razor-Checks Replacement**: Die Template enthält `razor-l10n-check.py` und `razor-usage-check.py`. Für MAUI/XAML/C# entfallen diese. Soll es Äquivalente für XAML-Ressourcen geben, oder entfallen diese komplett?

2. **Test-Execution-Validierung**: Wie wird sichergestellt, dass Tests lokal erfolgreich waren?
   - Über `dotnet test --no-build` in pre-push?
   - Über Marker-Datei (z.B. `.test-passed`)?
   - Über Git-Hooks-Umgebungsvariablen?

3. **API-Schlüssel und Secrets**: Welche konkrete Muster für Kraftstoffpreis- und Routing-Service-Keys sind zu blockieren? (z.B. `FUEL_API_KEY=`, `ROUTING_KEY=`, oder regexe?)

4. **Python-Abhängigkeiten**: Sollen Python-Skripte in einem Virtual Environment (`venv`) laufen, oder Python-System-Installation verwenden?

5. **Installationsmodus**: Soll `core.hooksPath .githooks` oder Symlink-Variante verwendet werden?
   - `core.hooksPath` ist eleganter (kein Kopieren), aber erfordert Git 2.9+
   - Symlinks funktionieren auf Unix, sind aber auf Windows kompliziert

6. **Commit-Message-Format**: Welche Scopes und Types sind für dieses Projekt erlaubt?
   - Beispiele: `feat(auth)`, `fix(api)`, `docs(setup)`, `test(unit)`, `chore(deps)`, etc.?

7. **Fehlgeschlagene Hooks**: Dürfen User Hooks mit `--no-verify` überschreiben, oder soll das blockiert werden (z.B. über Pre-push)?

### Annahmen

- Das Projekt verwendet `.NET SDK` (vorhanden: `Tankradar.sln`)
- Python 3.x ist auf Developer-Maschinen installiert
- Die Hook-Dateien werden unter `.githooks/` versioniert (nicht `.git/hooks/`)
- Razor-Checks entfallen komplett (nicht relevant für MAUI/XAML)
- Forbidden-Patterns prüfen Dateizeilen, nicht nur Dateinamen
- Conventional Commits ist Voraussetzung für automatische Versionierung (semantisches Release)
- Hooks sind für alle Entwickler verpflichtend (keine optionale Installation)
