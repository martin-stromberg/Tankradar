# Hook-Dateien

## `.githooks/pre-commit`
Datei: `.githooks/pre-commit` (Shell-Skript, 85 Zeilen)

### Funktionsweise
- **Branch-Schutz (blockierend):** Blockiert direkte Commits auf `main` und `staging`
- **Warn-Modus:** Alle weiteren Prüfungen laufen ohne Blockierung, damit Work-in-Progress committet werden kann
- **Python-Fallback:** Nutzt `python3` bevorzugt, fällt auf `python` zurück
- **Notfall-Fallback:** `SKIP_HOOKS=1 git commit ...` überspringt alle Hooks

### Ausgeführte Checks (Warn-Modus, nicht blockierend)
| Check | Skript | Beschreibung |
|-------|--------|-------------|
| Übersetzungs-Konsistenz | `translation-check.py` | Validiert Lokalisierungs-Keys und .resx-Konsistenz |
| XML-Dokumentation | `csproj-xmldoc-check.py` | Prüft XML-Doc-Konfiguration und -Vollständigkeit |
| Platzhalter-Implementierungen | `no-notimplemented-check.py` | Blockt NotImplementedException ohne --strict |
| Enum-Testabdeckung | `enum-coverage-check.py` | Prüft Testabdeckung von Enums ohne --strict |
| Verbotene Muster | `forbidden-patterns-check.py` | Prüft auf Secrets, Zertifikate, DB-Dumps, Logdateien (ohne --strict) |
| Code-Formatierung | `format-code-style-check.py` | Prüft `dotnet format --verify-no-changes` |

### Optionale Checks (falls .NET-Projekte vorhanden)
- `SecretScan.csproj` (falls vorhanden) — wird mit `dotnet run` ausgeführt
- `MarkdownLinkCheck.csproj` (falls vorhanden) — wird mit `dotnet run` ausgeführt

### Exit-Code
- `0` - Pre-Commit erfolgreich (Commit erlaubt)
- `1` - Branch-Schutz verletzt (direkte Commits auf main/staging blockiert)

---

## `.githooks/pre-push`
Datei: `.githooks/pre-push` (Shell-Skript, 78 Zeilen)

### Funktionsweise
- **Branch-Schutz (blockierend):** Blockiert direkte Pushes auf `main` und `staging`
- **Strikte Checks:** Alle Prüfungen laufen mit `--all --strict` Flags (blockierend)
- **Ref-basierte Verarbeitung:** Liest stdin im Git-Hook-Format (`<local_ref> <local_sha> <remote_ref> <remote_sha>`)
- **Python-Fallback:** Nutzt `python3` bevorzugt, fällt auf `python` zurück

### Ausgeführte Checks (blockierend)
| Check | Skript | Parameter | Beschreibung |
|-------|--------|-----------|-------------|
| Platzhalter-Implementierungen | `no-notimplemented-check.py` | `--all --strict` | Blockt jede NotImplementedException |
| Enum-Testabdeckung | `enum-coverage-check.py` | `--all --strict` | Blockt unvollständige Testabdeckung |
| Verbotene Muster | `forbidden-patterns-check.py` | `--all --strict` | Blockt Secrets, Zertifikate, DB-Dumps, Logdateien |
| Conventional Commits | `conventional-commits-check.py` | (keine Flags) | Validiert Commit-Nachrichten |
| Testausführung | `test-execution-check.py` | (keine Flags) | Führt Tests aus, blockt bei Fehlern |

### Hinweise zu Commit-Bereich-Prüfung
- **Aktueller Stand:** Keine explizite Bereichs-Prüfung in `conventional-commits-check.py` basierend auf stdin-Refs
- **Hardcodiert:** Die Prüfung nutzt `resolve_base_ref()` zur Ermittlung von `main`/`origin/main`, nicht die aus stdin gelesenen Refs

### Exit-Code
- `0` - Alle Pre-Push-Checks erfolgreich, Push erlaubt
- `1` - Branch-Schutz verletzt oder Prüfung fehlgeschlagen

---

## Gemeinsame Struktur
Beide Hook-Dateien nutzen das Shell-Skript-Header-Pattern:
```sh
#!/usr/bin/env sh
set -u
```

Dies setzt strikte Error-Handling (Exit bei undefinierter Variable).

Die Hooks nutzen `git rev-parse --show-toplevel` zur Ermittlung des Repository-Root, um unabhängig vom aktuellen Arbeitsverzeichnis zu funktionieren.

---

## Git-Index Berechtigungen

Alle Hook- und Check-Dateien sind derzeit im Git-Index mit Modus `100644` (reguläre Datei) eingetragen:

```
100644 ... .githooks/_hook_common.py
100644 ... .githooks/conventional-commits-check.py
100644 ... .githooks/csproj-xmldoc-check.py
100644 ... .githooks/enum-coverage-check.py
100644 ... .githooks/forbidden-patterns-check.py
100644 ... .githooks/format-code-style-check.py
100644 ... .githooks/no-notimplemented-check.py
100644 ... .githooks/pre-commit
100644 ... .githooks/pre-push
100644 ... .githooks/test-execution-check.py
100644 ... .githooks/test_*.py
100644 ... .githooks/translation-check.py
```

**Defizit lt. Anforderung:** Diese Dateien sollten Modus `100755` (Ausführungsrecht) haben, um ordnungsgemäß als ausführbare Dateien installiert zu werden. Dies ist ein bekanntes Defizit, das in der Nachbesserung behoben werden muss.
