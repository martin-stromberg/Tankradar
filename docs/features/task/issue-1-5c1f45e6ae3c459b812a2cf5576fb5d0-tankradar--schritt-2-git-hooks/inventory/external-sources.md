# Externe Vorlagen: Git Hooks und Qualitätschecks

Dokumentation der versionierten Template-Hooks und Python-Check-Skripte aus dem privaten Repository (martin-stromberg/Pattern-Collection), die als Grundlage für die Schritt-2-Implementierung dienen.

**Quellverzeichnis (schreibgeschützt):**  
`docs/projects/task/issue-1-5c1f45e6ae3c459b812a-2cf5576fb5d0-tankradar/inventory/external-sources/`

## Struktur der Template-Hooks

### Hook-Dateien

#### `pre-commit` (Shell-Hook)
**Datei:** `githooks/pre-commit`

**Zweck:** Läuft automatisch vor jedem Commit

**Funktionsweise:**
1. **Branch-Protection:** Blockiert direkte Commits auf `main` und `staging` (Exit 1)
2. **Python-Checks (in Warn-Modus):**
   - `translation-check.py` — Fehlerhafte/fehlende Übersetzungen
   - `csproj-xmldoc-check.py` — XML-Dokumentation in .cs/.csproj
   - `razor-l10n-check.py` — Hardcodierte UI-Strings in .razor (XAML-Äquivalent erforderlich)
   - `razor-usage-check.py` — Verwaiste .razor-Komponenten (Warnung nur, --strict in pre-push)
   - `no-notimplemented-check.py` — `NotImplementedException` und Stub-Methoden (Warnung nur)
   - `enum-coverage-check.py` — Testabdeckung von Enum-Werten (Warnung nur)
3. **Dynamische .NET-Checks (falls .sln vorhanden):**
   - `dotnet`-Verfügbarkeit prüfen
   - Solution-Datei suchen (.sln / .slnx)
   - Optionale Projekte: `SecretScan.csproj`, `MarkdownLinkCheck.csproj`

**Besonderheiten:**
- Nutzt `set -eu` für strikte Fehlerbehandlung
- Bestimmt Repo-Root mit `git rev-parse --show-toplevel`
- Alle Python-Skripte werden vom `repo_root/.githooks/`-Pfad aufgerufen

#### `pre-push` (Shell-Hook)
**Datei:** `githooks/pre-push`

**Zweck:** Läuft automatisch vor `git push`

**Funktionsweise:**
1. **Branch-Protection:** Blockiert direkte Pushes auf `main` und `staging`
   - Liest stdin für lokale/Remote-Refs
   - Prüft auf `refs/heads/main` oder `refs/heads/staging`
   - Blockiert mit Exit 1
2. **Strict Checks (alle --all --strict):**
   - `no-notimplemented-check.py --all --strict` — Blockiert unvollständige Implementierungen
   - `razor-usage-check.py --all --strict` — Blockiert verwaiste Komponenten
   - `enum-coverage-check.py --all --strict` — Blockiert Enum-Abdeckungslücken

**Besonderheit:** Liest Standard-Input für Pre-Push-Referenzen (Git-Hook-Protokoll)

### Installationsskripte

#### `install-hooks.cmd` (Windows)
**Datei:** `githooks/install-hooks.cmd`

**Funktionsweise:**
```cmd
git config --local core.hooksPath .githooks
```

**Effekt:**
- Setzt Git-Konfiguration lokal (nur für dieses Repository)
- Git sucht Hooks in `.githooks/` statt `.git/hooks/`
- Erfordert Git 2.9+

#### `install-hooks.sh` (Unix/Linux/macOS)
**Datei:** `githooks/install-hooks.sh`

**Funktionsweise:**
```sh
git config --local core.hooksPath .githooks
```

**Effekt:** Identisch mit Windows-Variante

---

## Python-Check-Skripte

Alle Python-Skripte folgen einem konsistenten Muster:
- `#!/usr/bin/env python3` Shebang
- UTF-8-Encoding mit Fehlerbehandlung
- Ausgeschlossene Verzeichnisse: `.git`, `bin`, `obj`, `TestResults`, `node_modules`, `.vs`, `packages`
- Exit-Codes: 0 (OK/Warnung), 1 (Fehler/strict mode)
- Standard-Modi: `--all` (ganzes Repo), `--strict` (blockierend)

### `csproj-xmldoc-check.py`
**Datei:** `githooks/csproj-xmldoc-check.py`

**Zweck:** Validiert XML-Dokumentation in C#-Code und Projektdateien

**Prüfungen:**
- `.csproj`-Dateien: 
  - `GenerateDocumentationFile` ist gesetzt
  - Keine `#pragma warning disable` für XML-Doc-Codes (CS1591, CS1572, etc.)
- `.cs`-Dateien:
  - Dokumentierte Member haben vollständige `<param>`, `<typeparam>`, `<returns>`, `<response>`-Tags
  - Korrekte Syntax und Vollständigkeit

**Parameter:**
- Keine Parameter: Nur gestaffelte Dateien prüfen
- `--all`: Ganzes Repo durchsuchen

**Exit-Code:** 0 (immer, nur Warnungen in pre-commit)

### `no-notimplemented-check.py`
**Datei:** `githooks/no-notimplemented-check.py`

**Zweck:** Verhindert `NotImplementedException` und Stub-Methoden

**Prüfungen:**
1. `NotImplementedException` im Code
2. Methoden/Konstruktoren mit reinem throw-Body: `{ throw new Exception(...); }`
3. Expression-bodied Methoden: `=> throw new Exception(...);`
4. Property-Accessoren: `get => throw new Exception(...);`

**Verwendung:**
- **Pre-Commit:** Warnt nur über gestaffelte Dateien (Work-in-Progress erlaubt)
- **Pre-Push:** `--all --strict` — Ganze Repo, blockierend

**Exit-Code:** 
- 0 (Warnung oder kein Fehler)
- 1 (mit --strict und Fehler gefunden)

### `enum-coverage-check.py`
**Datei:** `githooks/enum-coverage-check.py`

**Zweck:** Warnt vor fehlender Testabdeckung von Enum-Werten

**Funktionsweise:**
- Findet alle `public`/`internal` C#-Enums
- Prüft, ob Test-Dateien (in `*Test*/`-Verzeichnissen) alle Enum-Werte abdecken
- Flaggt Enum-Werte, die in keinem Test auftauchen

**Verwendung:**
- **Pre-Commit:** Warnt über aktuelle Lösung/Repo-Zustand
- **Pre-Push:** `--all --strict` — Blockiert fehlende Abdeckung

**Exit-Code:** 
- 0 (Warnung oder vollständig)
- 1 (mit --strict und Lücken)

### `translation-check.py`
**Datei:** `githooks/translation-check.py`

**Zweck:** Prüft auf fehlende/unvollständige Übersetzungen in Ressourcen

**Funktionsweise:**
- Sucht nach lokalisierbaren Ressourcendateien
- Prüft auf konsistente Schlüssel über Sprachvarianten
- Flaggt fehlende Einträge

**Verwendung:** Wird in pre-commit aufgerufen

**Exit-Code:** 0 (immer, nur Warnungen)

### `razor-l10n-check.py`
**Datei:** `githooks/razor-l10n-check.py`

**Zweck:** Flaggt hardcodierte, natural-language UI-Strings in `.razor`-Dateien, die lokalisierbar sein sollten

**Funktionsweise:**
- Sucht nach:
  - Hardcodierten Attributen: `title`, `placeholder`, `alt`, `aria-label`, `label`, `tooltip`
  - Multi-Wort-Text-Knoten in der Ansicht
- Empfielt Verwendung von `@L["Key"]` Lokalisierungs-Syntax

**Hinweis für MAUI/XAML:** Dieses Skript ist Razor-spezifisch. Eine XAML-Äquivalent (`xaml-l10n-check.py`) muss u.U. neu entwickelt werden.

**Exit-Code:** 0 (immer, nur Warnungen)

### `razor-usage-check.py`
**Datei:** `githooks/razor-usage-check.py`

**Zweck:** Findet verwaiste `.razor`-Komponenten (nicht referenziert im Projekt)

**Funktionsweise:**
- Indexiert alle `.razor`-Dateien
- Sucht Referenzen in `.cs` und anderen `.razor`-Dateien
- Flaggt Komponenten ohne Referenzen als "orphaned"

**Verwendung:**
- **Pre-Commit:** Warnt nur über gestaffelte `.razor`-Dateien
- **Pre-Push:** `--all --strict` — Blockiert verwaiste Komponenten

**Hinweis für MAUI:** Dieses Skript ist ebenfalls Razor-spezifisch. Eine XAML-Äquivalent (`xaml-usage-check.py`) ist erforderlich.

**Exit-Code:** 
- 0 (Warnung oder keine Waisen)
- 1 (mit --strict und Waisen gefunden)

---

## Installation und Konfiguration

### Aktivierung der Hooks

Die Hooks werden durch Ausführung eines Installationsskripts aktiviert:

**Windows:**
```cmd
.githooks\install-hooks.cmd
```

**Unix/macOS/Linux:**
```bash
.githooks/install-hooks.sh
```

**Manuell:**
```bash
git config --local core.hooksPath .githooks
```

### Voraussetzungen

- **Git:** Version 2.9+ (für `core.hooksPath`)
- **Python:** Version 3.x auf Developer-Maschinen
- **.NET SDK:** Erforderlich für `dotnet`-basierte Checks in pre-commit
- **Bash/sh:** Für Hook-Ausführung

### Deaktivierung (Notfall-Fallback)

Einzelne Hooks können über Umgebungsvariablen oder `.git/config` deaktiviert werden (exakte Mechanik unklar, in Requirement skizziert unter "Offene Fragen").

---

## Relevante Funde für MAUI/XAML-Projekt

| Check-Skript | Anwendbar auf Tankradar | Anmerkung |
|---|---|---|
| `csproj-xmldoc-check.py` | Ja | Direktes C#-Projekt, voll anwendbar |
| `no-notimplemented-check.py` | Ja | Anwendbar auf C#-Code |
| `enum-coverage-check.py` | Ja | Für C#-Enums anwendbar |
| `translation-check.py` | Teilweise | Muss auf XAML-Ressourcen adaptiert werden |
| `razor-l10n-check.py` | Nein | Razor-spezifisch, nicht anwendbar auf XAML |
| `razor-usage-check.py` | Nein | Razor-spezifisch, nicht direkt anwendbar (XAML-Äquivalent nötig) |

---

## Git Config und Hooks-Pfad

**Aktueller Zustand:**
- `.githooks/`-Verzeichnis existiert noch nicht
- `.git/hooks/` enthält nur Standard-Sample-Dateien (*.sample)
- `core.hooksPath` ist nicht konfiguriert

**Nach Installation:**
- `.githooks/` wird als versioniert abgelegt
- `git config --local core.hooksPath .githooks` wird gesetzt
- Git ruft Hooks aus `.githooks/` statt `.git/hooks/` auf
