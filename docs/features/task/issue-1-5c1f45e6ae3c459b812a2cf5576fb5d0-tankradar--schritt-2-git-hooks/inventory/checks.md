# Python-Check-Skripte

## `conventional-commits-check.py`
Datei: `.githooks/conventional-commits-check.py` (126 Zeilen)

### Zweck
Validiert Commit-Nachrichten gegen das Conventional-Commits-Format `type(scope): subject`.

### Unterstützte Commit-Typen (ALLOWED_TYPES)
```
feat, fix, docs, test, refactor, chore, perf, plan, merge
```

**Fehlende Typen (lt. Anforderung):** `ci`, `build`, `style`, `revert`

### Funktionen
| Funktion | Parameter | Rückgabe | Beschreibung |
|----------|-----------|----------|-------------|
| `validate_message(subject_line)` | `str` | `list[str]` | Validiert eine Commit-Nachricht, gibt Liste von Fehlermeldungen zurück |
| `commits_since_divergence(base_ref)` | `str` | `list[tuple]` | Liest Commits seit Divergenz von base_ref (z.B. `main..HEAD`) |
| `resolve_base_ref()` | — | `str \| None` | Findet `main` oder `origin/main` zur Divergenz-Bestimmung |

### Parameter & Verhalten
- `--base <ref>`: Basis-Referenz für Divergenz-Bestimmung (Standard: auto-detected)
- Exit 0: Alle Commits valide
- Exit 1: Mindestens ein Verstoß

### Bekannte Einschränkungen
- Breaking-Change-Notation (`feat!:`, `BREAKING CHANGE:`) nicht unterstützt
- Keine Großschreibungs-Prüfung (bewusst, wegen deutscher Satzanfänge)

---

## `forbidden-patterns-check.py`
Datei: `.githooks/forbidden-patterns-check.py` (183 Zeilen)

### Zweck
Blockiert Secrets, Zertifikate, Datenbank-Dumps und übergroße Logdateien.

### Geprüfte Dateitypen & Muster

#### Dateitypen (direkte Blockierung)
| Erweiterung | Beschreibung | Status |
|-------------|-------------|--------|
| `.pfx`, `.p12`, `.keystore`, `.jks`, `.pem`, `.cer` | Zertifikate/Schlüsseldateien | Implementiert |
| `.db`, `.sqlite`, `.sqlite3` | Datenbank-Dumps | Implementiert |
| `.mobileprovision`, `.p8` | iOS-Signierungsdateien | **Fehlt** |
| `.sql` | SQL-Dumps | **Fehlt** |

#### Text-Inhalte (Regex-Prüfung)
| Muster | Beispiel | Status |
|--------|---------|--------|
| API-Schlüssel (`=`-Notation) | `FUEL_API_KEY=secret` | Implementiert |
| API-Schlüssel (JSON-Notation) | `"TankerkoenigApiKey": "secret"` | **Fehlt** |
| Zertifikat-Inhalte | `-----BEGIN RSA PRIVATE KEY-----` | Implementiert |
| Signierungsdaten in .csproj | `<SigningKey>value</SigningKey>` | Implementiert |

#### Logdateien
| Regel | Status |
|------|--------|
| `.log` Dateien über 1 MB | Implementiert |
| `.log` Dateien unbegrenzt | **Nicht unterstützt** (1-MB-Limit existiert) |
| `changes.log` Ausnahme | **Nicht implementiert** |

### Funktionen
| Funktion | Parameter | Rückgabe | Beschreibung |
|----------|-----------|----------|-------------|
| `check_file(root, rel)` | `Path, str` | `list[str]` | Prüft eine Datei auf verbotene Muster |
| `check_api_keys(rel, content)` | `str, str` | `list[str]` | Sucht API-Schlüssel in Dateiinhalt |
| `check_certificate_content(rel, content)` | `str, str` | `list[str]` | Sucht Zertifikat-Inhalte |
| `check_signing_data(rel, content)` | `str, str` | `list[str]` | Sucht Signierungsdaten in .csproj |

### Parameter & Verhalten
- `--all`: Prüft ganzes Repository statt nur gestaffelte Dateien
- `--strict`: Exit 1 bei Funden (default: nur Warnung, Exit 0)
- Exit 0: Keine Funde oder Warnung (ohne --strict)
- Exit 1: Funde mit --strict gesetzt

---

## `format-code-style-check.py`
Datei: `.githooks/format-code-style-check.py` (61 Zeilen)

### Zweck
Prüft Code-Formatierung via `dotnet format <Solution> --verify-no-changes`.

### Funktionen
| Funktion | Parameter | Rückgabe | Beschreibung |
|----------|-----------|----------|-------------|
| `find_solution(root)` | `Path` | `Path \| None` | Sucht .sln/.slnx Datei im Root |

### Parameter & Verhalten
- `--strict`: Exit 1 bei Formatierungsfehlern (default: nur Warnung, Exit 0)
- Default: Nur Warnung im pre-commit (Work-in-Progress-Flexibilität)
- Exit 0: Immer (außer mit --strict und Formatierungsfehlern)

### Hinweise
- Benötigt `dotnet` im PATH
- Führt `dotnet format --verify-no-changes` aus, behebt Fehler nicht selbst

---

## `csproj-xmldoc-check.py`
Datei: `.githooks/csproj-xmldoc-check.py` (468 Zeilen)

### Zweck
Validiert XML-Dokumentation in .csproj und .cs Dateien.

### Geprüfte Regeln

#### .csproj Validierung
| Regel | Status |
|------|--------|
| `<GenerateDocumentationFile>true</GenerateDocumentationFile>` | Implementiert |
| CS1591 als Fehler konfiguriert | **Teilweise** (derzeit nicht erzwungen) |
| XML-Doc-Codes nicht in `<NoWarn>` | Implementiert |
| XML-Doc-Codes nicht herabgestuft | Implementiert |

#### .cs Datei-Validierung
| Regel | Beschreibung | Status |
|------|------------|--------|
| `#pragma warning disable` für XML-Codes | Blockiert | Implementiert |
| `<param>` Tags für alle Parameter | Vollständigkeitsprüfung | Implementiert |
| `<typeparam>` Tags für Generics | Vollständigkeitsprüfung | Implementiert |
| `<returns>` Tag für Rückgabewerte | Vollständigkeitsprüfung | Implementiert |
| `<response code="...">` für HTTP-Codes | Für annotierte HTTP-Methods | Implementiert |

### Funktionen
| Funktion | Parameter | Rückgabe | Beschreibung |
|----------|-----------|----------|-------------|
| `check_csproj_for_xmldoc(csproj_path)` | `Path` | `list[str]` | Prüft .csproj XML-Doc-Konfiguration |
| `check_cs_xmldoc_completeness(content)` | `str` | `list[str]` | Validiert Vollständigkeit von XML-Kommentaren |
| `check_cs_pragma_violations(content)` | `str` | `list[tuple]` | Sucht verbotene #pragma Direktiven |
| `parse_documented_members(content)` | `str` | `list[dict]` | Parst dokumentierte Members aus .cs |

### Parameter & Verhalten
- `--all`: Prüft alle .cs/.csproj Dateien statt nur gestaffelte
- Exit 0: Keine Fehler
- Exit 1: Fehler gefunden

---

## `translation-check.py`
Datei: `.githooks/translation-check.py` (258 Zeilen)

### Zweck
Prüft Lokalisierungs-Konsistenz über .resx Dateien.

### Geprüfte Validierungen

| Validierung | Beschreibung | Status |
|-------------|-------------|--------|
| Verwendete Schlüssel existieren | Findet fehlende .resx Keys | Implementiert |
| .resx Package-Konsistenz | Alle Sprach-Varianten haben gleiche Keys | Implementiert |
| .resx Header-Validierung | Korrekte resmimetype, reader, writer | Implementiert |
| Deutsche Umlaut-Transliterationen | Blockt `ue/oe/ae` Varianten | Implementiert |

### Funktionen
| Funktion | Parameter | Rückgabe | Beschreibung |
|----------|-----------|----------|-------------|
| `resx_keys(path)` | `Path` | `set[str]` | Extrahiert Key-Namen aus .resx |
| `resx_header_errors(path)` | `Path` | `list[str]` | Validiert .resx Header |
| `resx_german_value_errors(path)` | `Path` | `list[str]` | Prüft Deutsche Werte auf Transliterationen |

### Parameter & Verhalten
- `--all`: Prüft alle .cs/.razor/.cshtml Dateien statt nur gestaffelte
- Exit 0: Keine Fehler
- Exit 1: Fehler gefunden

---

## `enum-coverage-check.py`
Datei: `.githooks/enum-coverage-check.py` (181 Zeilen)

### Zweck
Prüft, dass alle Enum-Werte in Unit-Tests abgedeckt sind.

### Funktionsweise
- Sucht nach Enum-Definitionen in .cs Dateien
- Prüft entsprechende Test-Dateien auf Testabdeckung
- Kann mit `--all --strict` für vollständige Repo-Prüfung aufgerufen werden

### Parameter & Verhalten
- `--all`: Prüft alle Dateien statt nur gestaffelte
- `--strict`: Exit 1 bei Fehlern (blockierend)
- Exit 0: Immer (außer mit --strict und unvollständiger Testabdeckung)

---

## `no-notimplemented-check.py`
Datei: `.githooks/no-notimplemented-check.py` (184 Zeilen)

### Zweck
Blockt Platzhalter-Implementierungen mit `throw new NotImplementedException()`.

### Funktionsweise
- Sucht nach `NotImplementedException` in .cs Dateien
- Meldet Vorkommen mit Datei und Zeilennummer
- Kann mit `--all --strict` für vollständige Repo-Prüfung aufgerufen werden

### Parameter & Verhalten
- `--all`: Prüft alle Dateien statt nur gestaffelte
- `--strict`: Exit 1 bei Funden (blockierend)
- Exit 0: Immer (außer mit --strict und Funden)

---

## `test-execution-check.py`
Datei: `.githooks/test-execution-check.py` (78 Zeilen)

### Zweck
Führt Projekt-Tests aus und blockiert Push bei Fehlern oder Timeout.

### Funktionsweise
- Sucht nach Test-Projekt-Dateien (.csproj mit Test-Indikator)
- Führt `dotnet test` aus
- Prüft Exit-Code auf Erfolg
- Implementiert Timeout-Handling

### Parameter & Verhalten
- Exit 0: Alle Tests erfolgreich
- Exit 1: Testfehler oder Timeout

---

## `_hook_common.py`
Datei: `.githooks/_hook_common.py` (38 Zeilen)

### Zweck
Gemeinsame Hilfsfunktionen für alle Check-Skripte.

### Funktionen
| Funktion | Parameter | Rückgabe | Beschreibung |
|----------|-----------|----------|-------------|
| `run(*args)` | `tuple` | `subprocess.CompletedProcess` | Führt Shell-Befehl aus, erfasst Output |
| `repo_root()` | — | `Path \| None` | Ermittelt Git-Repository-Root |
| `staged_files()` | — | `list[str]` | Listet gestaffelte Dateien |
| `find_solution(root)` | `Path` | `Path \| None` | Sucht .sln/.slnx Datei |

### Konstanten
```python
EXCLUDED_DIRS = {'.git', 'bin', 'obj', 'TestResults', 'node_modules', '.vs', '.idea', 'packages'}
```

Diese Verzeichnisse werden bei Dateisuche ignoriert.
